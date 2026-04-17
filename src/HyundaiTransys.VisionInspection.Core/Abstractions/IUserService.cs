using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string userName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
    Task SetRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    (string Hash, string Salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
}
