using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

namespace HyundaiTransys.VisionInspection.Core.Messaging;

public abstract record KeyenceCommand;

public sealed record ChangeJobCommand(JobId JobId) : KeyenceCommand;
public sealed record TriggerCommand : KeyenceCommand;
public sealed record GetStatusCommand : KeyenceCommand;

public sealed record KeyenceResponse(
    bool Success,
    InspectionResult Result,
    string? RawResponse,
    byte[]? ImageBytes,
    string? ErrorMessage);
