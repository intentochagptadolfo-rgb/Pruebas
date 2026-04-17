using HyundaiTransys.VisionInspection.Core.Configuration;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

public interface IConfigurationService
{
    AppSettings Current { get; }

    event EventHandler<AppSettings>? SettingsChanged;

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
