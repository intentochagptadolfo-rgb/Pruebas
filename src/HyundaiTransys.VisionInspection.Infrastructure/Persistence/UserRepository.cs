using HyundaiTransys.VisionInspection.Application.Services;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyundaiTransys.VisionInspection.Infrastructure.Persistence;

public sealed class UserRepository : IUserStore
{
    private readonly InspectionDbContext _db;

    public UserRepository(InspectionDbContext db) => _db = db;

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users.FindAsync(new object[] { id }, ct);
    }

    public async Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Users.ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}