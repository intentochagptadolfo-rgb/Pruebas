using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

/// <summary>
/// Drives the overall inspection flow (finite state machine).
/// The UI subscribes to <see cref="StateChanged"/> and <see cref="InspectionCompleted"/>.
/// </summary>
public interface IInspectionOrchestrator : IAsyncDisposable
{
    SystemState CurrentState { get; }
    InspectionRecord? CurrentInspection { get; }

    event EventHandler<SystemState>? StateChanged;
    event EventHandler<InspectionRecord>? InspectionCompleted;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Operator-invoked re-inspection after an NG.</summary>
    Task RetestAsync(CancellationToken cancellationToken = default);

    /// <summary>Administrative reset (clears NG alert, returns to Idle).</summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
