using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IPasswordHasher _hasher;
    private readonly IUserStore _store;

    public UserService(IPasswordHasher hasher, IUserStore store)
    {
        _hasher = hasher;
        _store = store;
    }

    public async Task<User?> AuthenticateAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await _store.FindByUserNameAsync(userName, ct);
        if (user is null || !user.IsActive) return null;
        if (!_hasher.Verify(password, user.PasswordHash, user.PasswordSalt)) return null;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _store.UpdateAsync(user, ct);
        return user;
    }

    public async Task<User> CreateAsync(string userName, string password, UserRole role, CancellationToken ct = default)
    {
        var (hash, salt) = _hasher.Hash(password);
        var user = new User
        {
            UserName = userName,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role
        };
        await _store.AddAsync(user, ct);
        return user;
    }

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default) => _store.ListAsync(ct);

    public async Task ChangePasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _store.FindByIdAsync(userId, ct)
                   ?? throw new InvalidOperationException($"User {userId} not found.");
        var (hash, salt) = _hasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _store.UpdateAsync(user, ct);
    }

    public async Task SetRoleAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        var user = await _store.FindByIdAsync(userId, ct)
                   ?? throw new InvalidOperationException($"User {userId} not found.");
        user.Role = role;
        await _store.UpdateAsync(user, ct);
    }

    public async Task DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _store.FindByIdAsync(userId, ct)
                   ?? throw new InvalidOperationException($"User {userId} not found.");
        user.IsActive = false;
        await _store.UpdateAsync(user, ct);
    }
}

/// <summary>Thin persistence port for users (implemented in Infrastructure).</summary>
public interface IUserStore
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
