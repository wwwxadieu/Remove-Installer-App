namespace RemoveInstallerApp.Models;

public enum ResidueKind
{
    Folder,
    File,
    Shortcut,
    RegistryKey,
    OrphanedUninstallEntry,
    OrphanedRunEntry,
}
