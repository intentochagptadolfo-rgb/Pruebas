using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Domain.Entities;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserName { get; init; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public required UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}
