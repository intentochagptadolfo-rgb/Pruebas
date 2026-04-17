using HyundaiTransys.VisionInspection.Core.Domain.Entities;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

public interface IInspectionRepository
{
    Task AddAsync(InspectionRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(InspectionRecord record, CancellationToken cancellationToken = default);
    Task<InspectionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InspectionRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}

public interface IJobMappingRepository
{
    Task<JobMapping?> FindAsync(string modelCode, string? variantKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobMapping>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(JobMapping mapping, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
