using System.Net.Sockets;
using System.Text;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using HyundaiTransys.VisionInspection.Core.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyundaiTransys.VisionInspection.Infrastructure.Keyence;

/// <summary>
/// TCP client for the Keyence IV4-500CA. Commands are serialized through a semaphore;
/// asynchronous inspection results arrive through <see cref="ResultReceived"/>.
/// </summary>
public sealed class KeyenceTcpClient : IKeyenceClient
{
    private readonly IOptionsMonitor<AppSettings> _options;
    private readonly ILogger<KeyenceTcpClient> _logger;

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _readerLoop;
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public KeyenceTcpClient(
        IOptionsMonitor<AppSettings> options,
        ILogger<KeyenceTcpClient> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConnected => _tcp?.Connected == true;

    public event EventHandler<KeyenceResponse>? ResultReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ConnectAsync(_cts.Token);
        _readerLoop = Task.Run(() => ReadLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null) await _cts.CancelAsync();
        if (_readerLoop is not null)
        {
            try { await _readerLoop; } catch (OperationCanceledException) { }
        }
        DisposeConnection();
    }

    public async Task<KeyenceResponse> ChangeJobAsync(JobId jobId, CancellationToken cancellationToken = default) =>
        await SendAndReadAckAsync(new ChangeJobCommand(jobId), cancellationToken);

    public async Task<KeyenceResponse> TriggerAsync(CancellationToken cancellationToken = default) =>
        await SendAndReadAckAsync(new TriggerCommand(), cancellationToken);

    public async Task<KeyenceResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
        await SendAndReadAckAsync(new GetStatusCommand(), cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
        _ioLock.Dispose();
    }

    // ---------- internals ----------

    private async Task ConnectAsync(CancellationToken ct)
    {
        var opt = _options.CurrentValue.Keyence;
        _tcp = new TcpClient { NoDelay = true, ReceiveTimeout = opt.ResultTimeoutMs, SendTimeout = opt.CommandTimeoutMs };
        await _tcp.ConnectAsync(opt.IpAddress, opt.Port, ct);
        _stream = _tcp.GetStream();
        _logger.LogInformation("Keyence connected to {Ip}:{Port}.", opt.IpAddress, opt.Port);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true, null));
    }

    private async Task<KeyenceResponse> SendAndReadAckAsync(KeyenceCommand command, CancellationToken ct)
    {
        if (_stream is null || _tcp is null || !_tcp.Connected)
            throw new InvalidOperationException("Keyence camera is not connected.");

        var payload = Encoding.ASCII.GetBytes(KeyenceProtocol.Encode(command));

        await _ioLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(payload, ct);
            await _stream.FlushAsync(ct);
            // NOTE: the async result path is the reader loop (below); here we only read the immediate ACK.
            var ackLine = await ReadLineAsync(_stream, _options.CurrentValue.Keyence.CommandTimeoutMs, ct);
            return KeyenceProtocol.DecodeAck(ackLine);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _stream is not null)
        {
            try
            {
                var line = await ReadLineAsync(_stream, _options.CurrentValue.Keyence.ResultTimeoutMs, ct);
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("RS", StringComparison.OrdinalIgnoreCase)) continue;

                var response = KeyenceProtocol.DecodeResult(line);
                ResultReceived?.Invoke(this, response);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keyence read loop error; will stop.");
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false, ex.Message));
                break;
            }
        }
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, int timeoutMs, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        var buffer = new byte[256];
        var sb = new StringBuilder();
        while (!timeoutCts.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, timeoutCts.Token);
            if (read == 0) break;
            sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
            var text = sb.ToString();
            var nl = text.IndexOf(KeyenceProtocol.Terminator, StringComparison.Ordinal);
            if (nl >= 0) return text[..nl];
        }
        return sb.ToString();
    }

    private void DisposeConnection()
    {
        try { _stream?.Dispose(); } catch { /* swallow */ }
        try { _tcp?.Dispose(); } catch { /* swallow */ }
        _stream = null;
        _tcp = null;
    }
}
