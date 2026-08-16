namespace RemoveInstallerApp.Models;

/// <summary>
/// One progress tick from a long-running scan, so the UI can show what is being examined
/// rather than an anonymous spinner. Reported through <see cref="System.IProgress{T}"/>, which
/// marshals back to the UI thread on its own.
/// </summary>
public sealed class ScanProgress
{
    public required string StepName { get; init; }
    public required int CurrentStep { get; init; }
    public required int TotalSteps { get; init; }
    public int ItemsFound { get; init; }

    /// <summary>0-100, for a determinate ProgressBar.</summary>
    public double PercentComplete => TotalSteps <= 0
        ? 0
        : Math.Clamp(CurrentStep * 100.0 / TotalSteps, 0, 100);
}
