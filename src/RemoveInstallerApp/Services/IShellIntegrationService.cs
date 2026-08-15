namespace RemoveInstallerApp.Services;

public interface IShellIntegrationService
{
    /// <summary>Whether the "Uninstall with Remove Installer App" verb is currently registered.</summary>
    bool IsRegistered { get; }

    /// <summary>Adds the context-menu verb to .exe files and .lnk shortcuts for the current user.</summary>
    void Register();

    /// <summary>Removes the context-menu verb.</summary>
    void Unregister();

    /// <summary>Re-writes the verb's display text (e.g. after a language change), if currently registered.</summary>
    void RefreshMenuText();
}
