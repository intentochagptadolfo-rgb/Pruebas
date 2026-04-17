using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace HyundaiTransys.VisionInspection.Infrastructure.Configuration;

/// <summary>
/// JSON-backed configuration service with DPAPI-at-rest encryption for the sensitive blob.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class JsonConfigurationService : IConfigurationService
{
    private readonly string _path;
    private readonly ILogger<JsonConfigurationService> _logger;
    private AppSettings _current = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonConfigurationService(string path, ILogger<JsonConfigurationService> logger)
    {
        _path = path;
        _logger = logger;
    }

    public AppSettings Current => _current;

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path))
            {
                _current = new AppSettings();
                await SaveInternalAsync(_current, ct);
                return _current;
            }

            var encrypted = await File.ReadAllBytesAsync(_path, ct);
            var plain = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.LocalMachine);
            var json = Encoding.UTF8.GetString(plain);
            _current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            return _current;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try { await SaveInternalAsync(settings, ct); }
        finally { _fileLock.Release(); }

        _current = settings;
        SettingsChanged?.Invoke(this, settings);
    }

    private async Task SaveInternalAsync(AppSettings settings, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json),
            optionalEntropy: null,
            DataProtectionScope.LocalMachine);

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllBytesAsync(_path, encrypted, ct);
        _logger.LogInformation("Configuration saved to {Path}.", _path);
    }
}
