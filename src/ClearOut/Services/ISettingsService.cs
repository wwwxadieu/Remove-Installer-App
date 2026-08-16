using ClearOut.Models;

namespace ClearOut.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
}
