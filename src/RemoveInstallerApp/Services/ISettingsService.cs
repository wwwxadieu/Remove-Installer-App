using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
}
