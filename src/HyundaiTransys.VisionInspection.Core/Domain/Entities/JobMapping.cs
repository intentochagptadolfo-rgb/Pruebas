namespace HyundaiTransys.VisionInspection.Core.Domain.Entities;

/// <summary>
/// Row of the "truth table" that maps an MES model code (and optional variant flags)
/// to the Keyence JOB to be loaded.
/// </summary>
public sealed class JobMapping
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ModelCode { get; init; }
    public string? VariantKey { get; init; }
    public required int JobId { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}
