using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HyundaiTransys.VisionInspection.Application.Services;

/// <summary>
/// Default resolver: looks up the truth table in the DB keyed by (ModelCode, VariantKey).
/// VariantKey is derived from the remaining MES fields (join with '|'); adjust per contract.
/// </summary>
public sealed class JobResolver : IJobResolver
{
    private readonly IJobMappingRepository _repo;
    private readonly ILogger<JobResolver> _logger;

    public JobResolver(IJobMappingRepository repo, ILogger<JobResolver> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<JobId> ResolveAsync(MesFrame frame, CancellationToken ct = default)
    {
        var variantKey = frame.Fields.Count > 2
            ? string.Join('|', frame.Fields.Skip(2))
            : null;

        var mapping = await _repo.FindAsync(frame.ModelCode, variantKey, ct)
                      ?? await _repo.FindAsync(frame.ModelCode, null, ct);

        if (mapping is null)
        {
            _logger.LogError("No JOB mapping for model {Model} (variant={Variant}).", frame.ModelCode, variantKey);
            throw new InvalidOperationException($"No JOB mapping found for model '{frame.ModelCode}'.");
        }

        return new JobId(mapping.JobId);
    }
}
