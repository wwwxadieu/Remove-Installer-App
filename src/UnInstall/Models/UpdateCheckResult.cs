namespace UnInstall.Models;

/// <summary>Outcome of asking GitHub Releases for the latest published version.</summary>
public sealed class UpdateCheckResult
{
    public required bool Success { get; init; }
    public bool IsUpdateAvailable { get; init; }

    /// <summary>Raw tag text from the release, e.g. "v1.2.0".</summary>
    public string? LatestVersionText { get; init; }

    /// <summary>Release page on GitHub — always safe to open even if no matching asset was found.</summary>
    public string? ReleaseUrl { get; init; }

    /// <summary>Direct download link for a Windows asset attached to the release, if one was published.</summary>
    public string? DownloadUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public static UpdateCheckResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}
