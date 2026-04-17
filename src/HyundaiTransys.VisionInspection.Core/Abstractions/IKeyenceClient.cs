using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using HyundaiTransys.VisionInspection.Core.Messaging;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

/// <summary>
/// Command channel to the Keyence IV4-500CA camera.
/// The camera is asynchronous: after <see cref="TriggerAsync"/> the result arrives
/// through <see cref="ResultReceived"/>.
/// </summary>
public interface IKeyenceClient : IAsyncDisposable
{
    bool IsConnected { get; }

    event EventHandler<KeyenceResponse>? ResultReceived;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    Task<KeyenceResponse> ChangeJobAsync(JobId jobId, CancellationToken cancellationToken = default);
    Task<KeyenceResponse> TriggerAsync(CancellationToken cancellationToken = default);
    Task<KeyenceResponse> GetStatusAsync(CancellationToken cancellationToken = default);
}
