using System.Net;
using System.Net.Sockets;
using System.Text;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using HyundaiTransys.VisionInspection.Core.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyundaiTransys.VisionInspection.Infrastructure.Mes;

/// <summary>
/// TCP client/server for the MES link. Keeps a single active connection and
/// re-establishes it automatically (watchdog + exponential backoff).
/// </summary>
public sealed class MesTcpClient : IMesClient
{
    private readonly IOptionsMonitor<AppSettings> _options;
    private readonly IMesMessageParser _parser;
    private readonly ILogger<MesTcpClient> _logger;

    private CancellationTokenSource? _cts;
    private Task? _runLoop;
    private TcpClient? _activeConnection;
    private NetworkStream? _activeStream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public MesTcpClient(
        IOptionsMonitor<AppSettings> options,
        IMesMessageParser parser,
        ILogger<MesTcpClient> logger)
    {
        _options = options;
        _parser = parser;
        _logger = logger;
    }

    public bool IsConnected => _activeConnection?.Connected == true;

    public event EventHandler<MesMessage>? MessageReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runLoop = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }
        if (_runLoop is not null)
        {
            try { await _runLoop; } catch (OperationCanceledException) { }
        }
        DisposeConnection("stop requested");
    }

    public async Task SendAsync(MesAck ack, CancellationToken cancellationToken = default)
    {
        if (_activeStream is null || !IsConnected)
        {
            throw new InvalidOperationException("MES link is not connected.");
        }

        var payload = $"{MesFrame.Stx}ACK;{ack.CorrelationId};{(ack.Success ? "OK" : "NG")}{MesFrame.Etx}";
        var bytes = Encoding.ASCII.GetBytes(payload);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _activeStream.WriteAsync(bytes, cancellationToken);
            await _activeStream.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
        _writeLock.Dispose();
    }

    // ---------- internals ----------

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var backoff = _options.CurrentValue.Mes.ReconnectInitialDelayMs;
        var max = _options.CurrentValue.Mes.ReconnectMaxDelayMs;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectOnceAsync(ct);
                RaiseConnectionChanged(true, null);
                backoff = _options.CurrentValue.Mes.ReconnectInitialDelayMs;

                await ReadFramesAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MES link failure; reconnecting in {Delay} ms.", backoff);
                RaiseConnectionChanged(false, ex.Message);
                DisposeConnection(ex.Message);
                try { await Task.Delay(backoff, ct); } catch (OperationCanceledException) { break; }
                backoff = Math.Min(backoff * 2, max);
            }
        }

        DisposeConnection("loop stopped");
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        var opt = _options.CurrentValue.Mes;

        if (opt.Role == MesRole.Client)
        {
            var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(IPAddress.Parse(opt.IpAddress), opt.Port, ct);
            _activeConnection = client;
            _activeStream = client.GetStream();
            _logger.LogInformation("MES (client) connected to {Ip}:{Port}.", opt.IpAddress, opt.Port);
            return;
        }

        // Server role: accept exactly one MES peer at a time.
        var listener = new TcpListener(IPAddress.Parse(opt.IpAddress), opt.Port);
        listener.Start();
        _logger.LogInformation("MES (server) listening on {Ip}:{Port}.", opt.IpAddress, opt.Port);
        try
        {
            _activeConnection = await listener.AcceptTcpClientAsync(ct);
            _activeConnection.NoDelay = true;
            _activeStream = _activeConnection.GetStream();
            _logger.LogInformation("MES peer connected: {Endpoint}.", _activeConnection.Client.RemoteEndPoint);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ReadFramesAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        var accumulator = new StringBuilder();
        var stream = _activeStream ?? throw new InvalidOperationException("Stream not ready.");

        while (!ct.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read == 0) throw new IOException("MES peer closed the connection.");

            accumulator.Append(Encoding.ASCII.GetString(buffer, 0, read));
            ExtractFrames(accumulator);
        }
    }

    private void ExtractFrames(StringBuilder accumulator)
    {
        while (true)
        {
            var content = accumulator.ToString();
            var stx = content.IndexOf(MesFrame.Stx);
            if (stx < 0) { accumulator.Clear(); return; }

            var etx = content.IndexOf(MesFrame.Etx, stx + 1);
            if (etx < 0) { if (stx > 0) accumulator.Remove(0, stx); return; }

            var payload = content.Substring(stx + 1, etx - stx - 1);
            accumulator.Remove(0, etx + 1);

            try
            {
                var frame = _parser.Parse(payload);
                var correlationId = Guid.NewGuid().ToString("N")[..8];
                MessageReceived?.Invoke(this, new MesMessage(frame, correlationId));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Malformed MES frame discarded: {Payload}", payload);
            }
        }
    }

    private void RaiseConnectionChanged(bool isConnected, string? reason) =>
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(isConnected, reason));

    private void DisposeConnection(string? reason)
    {
        try { _activeStream?.Dispose(); } catch { /* swallow */ }
        try { _activeConnection?.Dispose(); } catch { /* swallow */ }
        _activeStream = null;
        _activeConnection = null;
        if (reason is not null) _logger.LogDebug("MES connection disposed: {Reason}.", reason);
    }
}
