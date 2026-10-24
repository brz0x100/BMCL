namespace BMCL.Core.Configuration;

public enum LauncherTheme
{
    System,
    Light,
    Dark,
}

public sealed record LauncherSettings
{
    public string? JavaPath { get; init; }

    public int MinMemoryMb { get; init; } = 512;

    public int MaxMemoryMb { get; init; } = 4096;

    public int MaxConcurrentDownloads { get; init; } = 8;

    public bool UseBmclApi { get; init; }

    public LauncherTheme Theme { get; init; } = LauncherTheme.System;

    public string? SelectedInstanceId { get; init; }
}
