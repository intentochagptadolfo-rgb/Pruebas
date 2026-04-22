using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.Core.Configuration;

public sealed class AppSettings
{
    public const string SectionName = "App";

    /// <summary>
    /// When TRUE the HMI runs in Developer Mode (normal resizable window, standard
    /// close button, no keyboard lockdown) so it can be tested on a developer
    /// laptop. In production (Lenovo ThinkCentre line-side PC) this MUST be false.
    /// </summary>
    public bool IsDeveloperMode { get; set; } = false;

    public MesOptions Mes { get; set; } = new();
    public KeyenceOptions Keyence { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
    public DatabaseOptions Database { get; set; } = new();
    public StationOptions Station { get; set; } = new();
}

public sealed class MesOptions
{
    /// <summary>Whether this side acts as TCP server or as client.</summary>
    public MesRole Role { get; set; } = MesRole.Server;
    public string IpAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 9000;
    public int ReadTimeoutMs { get; set; } = 5_000;
    public int KeepAliveMs { get; set; } = 10_000;
    public int ReconnectInitialDelayMs { get; set; } = 1_000;
    public int ReconnectMaxDelayMs { get; set; } = 30_000;
}

public enum MesRole { Server, Client }

public sealed class KeyenceOptions
{
    public string IpAddress { get; set; } = "192.168.0.10";
    public int Port { get; set; } = 8500;
    public int CommandTimeoutMs { get; set; } = 3_000;
    public int ResultTimeoutMs { get; set; } = 5_000;
    public int ReconnectInitialDelayMs { get; set; } = 1_000;
    public int ReconnectMaxDelayMs { get; set; } = 30_000;
}

public sealed class StorageOptions
{
    public string OkImagesPath { get; set; } = @"C:\HTVision\Images\OK";
    public string NgImagesPath { get; set; } = @"C:\HTVision\Images\NG";
    public int RetentionDays { get; set; } = 30;
    public int MaxDiskUsageGb { get; set; } = 50;
}

public sealed class DatabaseOptions
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public string ConnectionString { get; set; } =
        @"Data Source=C:\HTVision\data\inspections.db";
}

public enum DatabaseProvider { Sqlite, PostgreSql }

public sealed class StationOptions
{
    public string StationId { get; set; } = "LINE01-ST01";
    public string LineName { get; set; } = "Assembly Line 01";
}