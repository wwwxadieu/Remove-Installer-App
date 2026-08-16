namespace ClearOut.Models;

/// <summary>Outcome of asking GitHub for a specific release's notes (used by the "what's new" screen).</summary>
public sealed class ReleaseNotesResult
{
    public required bool Success { get; init; }

    /// <summary>The release's Markdown body/notes, if any were written for it.</summary>
    public string? Body { get; init; }

    public string? HtmlUrl { get; init; }
    public string? ErrorMessage { get; init; }

    public static ReleaseNotesResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}
