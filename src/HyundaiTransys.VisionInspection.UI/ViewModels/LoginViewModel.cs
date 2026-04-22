using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.UI.ViewModels.Base;

namespace HyundaiTransys.VisionInspection.UI.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IUserService _users;

    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private User? _authenticatedUser;

    public LoginViewModel(IUserService users) => _users = users;

    public async Task<bool> LoginAsync(string user, string pass)
    {
        ErrorMessage = null;
        AuthenticatedUser = await _users.AuthenticateAsync(user, pass);
        return AuthenticatedUser is not null;
    }
}
