using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Domain.Entities;

/// <summary>
/// Record that captures a single inspection cycle from MES frame to completion.
/// Immutable for identity and base data; mutable for runtime state updates during orchestration.
/// </summary>
public sealed class InspectionRecord
{
    /// <summary>
    /// Unique identifier for this inspection instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Model code from MES frame (e.g., "HY_MODEL_2024_A").
    /// Base data, immutable after creation.
    /// </summary>
    public required string ModelCode { get; init; }

    /// <summary>
    /// Serial number / line identifier from MES.
    /// Base data, immutable after creation.
    /// </summary>
    public required string SerialNumber { get; init; }

    /// <summary>
    /// Dynamic job ID resolved via JobResolver against local database.
    /// Mutable: updated by InspectionOrchestrator during JobSwitching state.
    /// </summary>
    public required int JobId { get; set; }

    /// <summary>
    /// Inspection result from Keyence camera (Ok, Ng, Unknown).
    /// Updated in Evaluating state.
    /// </summary>
    public required InspectionResult Result { get; set; }

    /// <summary>
    /// Full path to saved inspection image following Hyundai Transys traceability rules.
    /// Format: C:\HTVision\Images\{OK|NG}\yyyy\MM_Mes\yyyy_MM_dd\HHMMSS_[Result]_[Car]_[Side]_[JobId].jpg
    /// Updated when result image is captured and persisted.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Raw MES frame payload for audit / replay purposes.
    /// Captured during Parsing state.
    /// </summary>
    public string? RawMesFrame { get; init; }

    /// <summary>
    /// Operator login name if manual authentication is used.
    /// Null in Kiosk mode (auto-login).
    /// </summary>
    public string? OperatorUserName { get; set; }

    /// <summary>
    /// Number of retests performed on this unit before final result.
    /// Incremented when Retest trigger fires.
    /// </summary>
    public int RetestCount { get; set; }

    /// <summary>
    /// UTC timestamp when inspection record was created.
    /// Immutable reference point for traceability.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when inspection result was finalized and persisted.
    /// Null until Evaluating → ResultOk/ResultNg transition.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Exception or operational error context if inspection faulted.
    /// Set in Faulted state handler for root-cause traceability.
    /// </summary>
    public string? ErrorMessage { get; set; }
}