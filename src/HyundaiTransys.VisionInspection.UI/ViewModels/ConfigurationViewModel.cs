using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.UI.Services;
using HyundaiTransys.VisionInspection.UI.ViewModels.Base;

namespace HyundaiTransys.VisionInspection.UI.ViewModels;

/// <summary>
/// Administrator-only screen: IP/Port of MES and Keyence, storage paths,
/// retention policy, database connection and user management.
/// </summary>
public sealed partial class ConfigurationViewModel : ViewModelBase
{
    private readonly IConfigurationService _config;
    private readonly IUserService _users;
    private readonly IJobMappingRepository _jobMappings;
    private readonly IDialogService _dialog;

    [ObservableProperty] private AppSettings _settings = new();
    [ObservableProperty] private string? _newUserName;
    [ObservableProperty] private string? _newUserPassword;
    [ObservableProperty] private UserRole _newUserRole = UserRole.Operator;

    public System.Collections.ObjectModel.ObservableCollection<User> Users { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<JobMapping> JobMappings { get; } = new();

    public ConfigurationViewModel(
        IConfigurationService config,
        IUserService users,
        IJobMappingRepository jobMappings,
        IDialogService dialog)
    {
        _config = config;
        _users = users;
        _jobMappings = jobMappings;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Settings = await _config.LoadAsync();
        Users.Clear();
        foreach (var u in await _users.ListAsync()) Users.Add(u);
        JobMappings.Clear();
        foreach (var j in await _jobMappings.GetAllAsync()) JobMappings.Add(j);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _config.SaveAsync(Settings);
        _dialog.Info("Configuration saved. Some changes require an application restart.");
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUserName) || string.IsNullOrWhiteSpace(NewUserPassword)) return;
        var user = await _users.CreateAsync(NewUserName!, NewUserPassword!, NewUserRole);
        Users.Add(user);
        NewUserName = NewUserPassword = null;
    }

    [RelayCommand]
    private async Task DeactivateUserAsync(User user) => await _users.DeactivateAsync(user.Id);

    [RelayCommand]
    private async Task SaveJobMappingAsync(JobMapping mapping) => await _jobMappings.UpsertAsync(mapping);

    [RelayCommand]
    private async Task DeleteJobMappingAsync(JobMapping mapping)
    {
        await _jobMappings.DeleteAsync(mapping.Id);
        JobMappings.Remove(mapping);
    }
}
