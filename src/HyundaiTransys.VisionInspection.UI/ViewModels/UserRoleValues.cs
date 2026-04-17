using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.UI.ViewModels;

public static class UserRoleValues
{
    public static IReadOnlyList<UserRole> All { get; } =
        Enum.GetValues<UserRole>();
}
