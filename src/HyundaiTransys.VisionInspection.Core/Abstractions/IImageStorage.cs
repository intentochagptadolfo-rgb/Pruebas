using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

public interface IImageStorage
{
    /// <summary>
    /// Persists the captured image to the configured folder and returns the absolute path.
    /// </summary>
    Task<string> SaveAsync(
        byte[] imageBytes,
        InspectionResult result,
        string serialNumber,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default);

    Task<byte[]?> LoadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes images older than <paramref name="retentionDays"/> and enforces the disk quota.
    /// </summary>
    Task ApplyRetentionPolicyAsync(
        int retentionDays,
        int maxDiskUsageGb,
        CancellationToken cancellationToken = default);
}
