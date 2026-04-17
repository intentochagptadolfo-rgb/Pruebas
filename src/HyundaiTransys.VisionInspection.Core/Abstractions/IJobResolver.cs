using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

/// <summary>
/// Resolves the Keyence JOB to execute for a given MES frame, using the truth table.
/// </summary>
public interface IJobResolver
{
    Task<JobId> ResolveAsync(MesFrame frame, CancellationToken cancellationToken = default);
}
