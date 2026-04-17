using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyundaiTransys.VisionInspection.Infrastructure.Storage;

public sealed class LocalImageStorage : IImageStorage
{
    private readonly IOptionsMonitor<AppSettings> _options;
    private readonly ILogger<LocalImageStorage> _logger;

    public LocalImageStorage(
        IOptionsMonitor<AppSettings> options,
        ILogger<LocalImageStorage> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        byte[] imageBytes,
        InspectionResult result,
        string serialNumber,
        DateTimeOffset timestamp,
        CancellationToken ct = default)
    {
        var s = _options.CurrentValue.Storage;
        var root = result == InspectionResult.Ok ? s.OkImagesPath : s.NgImagesPath;
        var folder = Path.Combine(root, timestamp.ToString("yyyyMMdd"));
        Directory.CreateDirectory(folder);

        var safeSerial = string.Concat(serialNumber.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var fileName = $"{timestamp:yyyyMMdd_HHmmss_fff}_{safeSerial}.jpg";
        var fullPath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(fullPath, imageBytes, ct);
        return fullPath;
    }

    public async Task<byte[]?> LoadAsync(string path, CancellationToken ct = default) =>
        File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;

    public Task ApplyRetentionPolicyAsync(int retentionDays, int maxDiskUsageGb, CancellationToken ct = default)
    {
        var s = _options.CurrentValue.Storage;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        foreach (var root in new[] { s.OkImagesPath, s.NgImagesPath })
        {
            if (!Directory.Exists(root)) continue;
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff.UtcDateTime) File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete image {Path}.", file);
                }
            }
        }
        // TODO: enforce maxDiskUsageGb by deleting oldest first once quota is exceeded.
        return Task.CompletedTask;
    }
}
