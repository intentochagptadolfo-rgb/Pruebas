using HyundaiTransys.VisionInspection.Core.Messaging;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

/// <summary>
/// Bi-directional channel to the MES. Implementations MUST:
///  - auto-reconnect on drop (watchdog with exponential backoff)
///  - surface connectivity changes via <see cref="ConnectionStateChanged"/>
///  - deliver one <see cref="MessageReceived"/> event per STX…ETX frame
/// </summary>
public interface IMesClient : IAsyncDisposable
{
    bool IsConnected { get; }

    event EventHandler<MesMessage>? MessageReceived;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendAsync(MesAck ack, CancellationToken cancellationToken = default);
}

public sealed record ConnectionStateChangedEventArgs(bool IsConnected, string? Reason);
