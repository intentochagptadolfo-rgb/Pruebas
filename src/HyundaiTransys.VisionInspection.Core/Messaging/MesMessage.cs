using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

namespace HyundaiTransys.VisionInspection.Core.Messaging;

public sealed record MesMessage(MesFrame Frame, string CorrelationId);

public sealed record MesAck(string CorrelationId, bool Success, string? Reason = null);
