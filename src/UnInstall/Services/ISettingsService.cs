using UnInstall.Models;

namespace UnInstall.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
}
