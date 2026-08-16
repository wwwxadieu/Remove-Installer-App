namespace ClearOut.Services;

public interface IShellIntegrationService
{
    /// <summary>Whether the "Uninstall with ClearOut" verb is currently registered.</summary>
    bool IsRegistered { get; }

    /// <summary>Adds the context-menu verb to .exe files and .lnk shortcuts for the current user.</summary>
    void Register();

    /// <summary>Removes the context-menu verb.</summary>
    void Unregister();

    /// <summary>Re-writes the verb's display text (e.g. after a language change), if currently registered.</summary>
    void RefreshMenuText();

    /// <summary>
    /// Removes any context-menu verb registered under the app's old pre-rename name and, if one
    /// was found, re-registers it fresh under the current name/executable path. Safe to call
    /// unconditionally on every launch.
    /// </summary>
    void MigrateLegacyVerbs();
}
