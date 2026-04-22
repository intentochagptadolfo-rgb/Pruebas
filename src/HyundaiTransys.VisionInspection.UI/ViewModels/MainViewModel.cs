using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.UI.Services;
using HyundaiTransys.VisionInspection.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System.IO;

namespace HyundaiTransys.VisionInspection.UI.ViewModels;

/// <summary>
/// Operator-facing HMI. Large OK/NG indicator + image + RETEST button.
/// The exit command is gated by the kiosk service: in production (kiosk)
/// it requires an administrator login; in Developer Mode it just shuts down.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IInspectionOrchestrator _orchestrator;
    private readonly IImageStorage _imageStorage;
    private readonly INavigationService _navigation;
    private readonly IKioskModeService _kiosk;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty] private SystemState _state = SystemState.Idle;
    [ObservableProperty] private InspectionResult _lastResult = InspectionResult.Unknown;
    [ObservableProperty] private string _modelCode = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private int _jobId;
    [ObservableProperty] private string _operatorName = "operator";
    [ObservableProperty] private BitmapImage? _lastImage;
    [ObservableProperty] private bool _isMesConnected;
    [ObservableProperty] private bool _isCameraConnected;

    public MainViewModel(
        IInspectionOrchestrator orchestrator,
        IImageStorage imageStorage,
        IMesClient mes,
        IKeyenceClient keyence,
        INavigationService navigation,
        IKioskModeService kiosk,
        IUiDispatcher dispatcher,
        ILogger<MainViewModel> logger)
    {
        _orchestrator = orchestrator;
        _imageStorage = imageStorage;
        _navigation = navigation;
        _kiosk = kiosk;
        _dispatcher = dispatcher;
        _logger = logger;

        _orchestrator.StateChanged += (_, s) => _dispatcher.Invoke(() => State = s);
        _orchestrator.InspectionCompleted += (_, r) => _dispatcher.Invoke(async () => await OnInspectionCompletedAsync(r));

        mes.ConnectionStateChanged += (_, c) => _dispatcher.Invoke(() => IsMesConnected = c.IsConnected);
        keyence.ConnectionStateChanged += (_, c) => _dispatcher.Invoke(() => IsCameraConnected = c.IsConnected);
    }

    public bool IsRetestEnabled => State == SystemState.NgAlert;
    partial void OnStateChanged(SystemState value) => OnPropertyChanged(nameof(IsRetestEnabled));

    /// <summary>Shown in the footer so the operator sees "Exit (Admin)" on the line
    /// and just "Exit" on a developer laptop.</summary>
    public string ExitButtonText => _kiosk.IsDeveloperMode ? "Exit" : "Exit (Admin)";

    public bool IsDeveloperMode => _kiosk.IsDeveloperMode;

    [RelayCommand]
    private async Task RetestAsync()
    {
        try { await _orchestrator.RetestAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Retest failed."); }
    }

    [RelayCommand]
    private void OpenConfiguration()
    {
        try
        {
            _navigation.ShowConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open configuration.");
            System.Windows.MessageBox.Show($"Error opening configuration: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RequestExit()
    {
        // Developer Mode: close cleanly without the admin dialog.
        if (_kiosk.IsDeveloperMode)
        {
            _kiosk.IsLocked = false;
            System.Windows.Application.Current?.Shutdown();
            return;
        }

        // Production kiosk: only an authenticated Administrator can unlock.
        if (!_navigation.RequestAdminAccess()) return;
        _kiosk.IsLocked = false;
        System.Windows.Application.Current?.Shutdown();
    }

    private async Task OnInspectionCompletedAsync(InspectionRecord record)
    {
        LastResult = record.Result;
        ModelCode = record.ModelCode;
        SerialNumber = record.SerialNumber;
        JobId = record.JobId;

        if (!string.IsNullOrWhiteSpace(record.ImagePath))
        {
            try
            {
                var bytes = await _imageStorage.LoadAsync(record.ImagePath);
                if (bytes is not null) LastImage = LoadBitmap(bytes);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not load image {Path}.", record.ImagePath); }
        }
    }

    private static BitmapImage LoadBitmap(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
