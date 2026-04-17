using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Domain.Entities;

public sealed class InspectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ModelCode { get; init; }
    public required string SerialNumber { get; init; }
    public required int JobId { get; init; }
    public required InspectionResult Result { get; set; }
    public string? ImagePath { get; set; }
    public string? RawMesFrame { get; init; }
    public string? OperatorUserName { get; set; }
    public int RetestCount { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
