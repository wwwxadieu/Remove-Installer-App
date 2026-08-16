namespace UnInstall.Models;

public sealed class BackupResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
