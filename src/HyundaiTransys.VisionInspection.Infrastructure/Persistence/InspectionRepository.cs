using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyundaiTransys.VisionInspection.Infrastructure.Persistence;

public sealed class InspectionRepository : IInspectionRepository
{
    private readonly InspectionDbContext _db;

    public InspectionRepository(InspectionDbContext db) => _db = db;

    public async Task AddAsync(InspectionRecord record, CancellationToken ct = default)
    {
        _db.Inspections.Add(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InspectionRecord record, CancellationToken ct = default)
    {
        _db.Inspections.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public Task<InspectionRecord?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Inspections.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<InspectionRecord>> GetRecentAsync(int count, CancellationToken ct = default) =>
        await _db.Inspections.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
}

public sealed class JobMappingRepository : IJobMappingRepository
{
    private readonly InspectionDbContext _db;

    public JobMappingRepository(InspectionDbContext db) => _db = db;

    public Task<JobMapping?> FindAsync(string modelCode, string? variantKey, CancellationToken ct = default) =>
        _db.JobMappings.AsNoTracking()
            .Where(x => x.IsEnabled && x.ModelCode == modelCode && x.VariantKey == variantKey)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<JobMapping>> GetAllAsync(CancellationToken ct = default) =>
        await _db.JobMappings.AsNoTracking().ToListAsync(ct);

    public async Task UpsertAsync(JobMapping mapping, CancellationToken ct = default)
    {
        var existing = await _db.JobMappings.FirstOrDefaultAsync(x => x.Id == mapping.Id, ct);
        if (existing is null) _db.JobMappings.Add(mapping);
        else _db.Entry(existing).CurrentValues.SetValues(mapping);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _db.JobMappings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return;
        _db.JobMappings.Remove(row);
        await _db.SaveChangesAsync(ct);
    }
}
