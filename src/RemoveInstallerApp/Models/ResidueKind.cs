namespace RemoveInstallerApp.Models;

public enum ResidueKind
{
    Folder,
    File,
    Shortcut,
    RegistryKey,
    OrphanedUninstallEntry,
    OrphanedRunEntry,

    /// <summary>A Windows service whose ImagePath pointed inside the removed install folder.</summary>
    ServiceEntry,

    /// <summary>A scheduled task referencing the removed install folder.</summary>
    ScheduledTask,

    /// <summary>
    /// A folder under AppData/ProgramData/Program Files that doesn't match any currently
    /// installed app by name, publisher, or install location. Inferred purely by exclusion, so
    /// it is the highest false-positive-risk kind in the app — see
    /// <see cref="ResidueScanService.ScanOrphanedFolders"/>.
    /// </summary>
    OrphanedFolder,
}

public static class ResidueKindExtensions
{
    /// <summary>
    /// Services and scheduled tasks are inferred from a path reference rather than a name
    /// match, and removing the wrong one can break an unrelated program or leave Windows with
    /// a half-registered service. They are listed for review but left unchecked so the user
    /// has to opt into each one; everything else is checked by default as before.
    /// </summary>
    public static bool IsHighRisk(this ResidueKind kind) =>
        kind is ResidueKind.ServiceEntry or ResidueKind.ScheduledTask or ResidueKind.OrphanedFolder;
}
