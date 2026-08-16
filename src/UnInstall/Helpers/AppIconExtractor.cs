using Windows.Storage;
using Windows.Storage.FileProperties;

namespace UnInstall.Helpers;

/// <summary>
/// Resolves an installed app's own icon via the WinRT thumbnail API (the same mechanism
/// Explorer uses to show file icons), returning a stream ready for
/// <c>BitmapImage.SetSourceAsync</c>.
///
/// This deliberately does NOT use System.Drawing.Common/GDI+ HICON extraction (an earlier
/// version of this file did). That caused an intermittent access violation inside the CLR
/// itself in real-world use (crash signature: coreclr.dll, 0xc0000005) — almost certainly
/// native heap corruption from GDI+'s internal icon/bitmap handling interacting badly with
/// this app's manual icon-handle cleanup, only discovered later by the GC and so seemingly
/// unrelated to icon loading at the point it actually surfaced. This WinRT path touches no raw
/// HICON handles and makes no GDI/GDI+ calls, so that whole class of native memory-corruption
/// risk is gone.
///
/// The trade-off: a <c>DisplayIcon</c> value that points at a specific icon index inside a
/// DLL/EXE resource (e.g. "shell32.dll,41") can no longer be honored — <c>GetThumbnailAsync</c>
/// only returns "the file's icon", not a specific resource index — so a handful of apps may show
/// a generic file icon instead of their real one. That's an acceptable cosmetic regression for
/// eliminating a random background-thread crash.
/// </summary>
public static class AppIconExtractor
{
    private const uint RequestedSize = 32;

    public static async Task<StorageItemThumbnail?> TryGetIconThumbnailAsync(string? displayIcon, string? installLocation)
    {
        var path = ResolveIconPath(displayIcon, installLocation);
        if (path is null)
        {
            return null;
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            // No ThumbnailOptions needed: an .exe/.dll has no picture/video thumbnail to prefer
            // over its icon in the first place, so the plain (mode, size) overload already
            // returns the file's icon.
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, RequestedSize);
            return thumbnail is { Size: > 0 } ? thumbnail : null;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Icon extraction failed for \"{path}\": {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// DisplayIcon from the registry is usually "C:\path\app.exe,0" (path + comma + icon index).
    /// The index can't be honored here (see class remarks), so only the path portion is kept.
    /// Falls back to the main exe in InstallLocation when DisplayIcon is missing or not a real
    /// icon-capable file.
    /// </summary>
    private static string? ResolveIconPath(string? displayIcon, string? installLocation)
    {
        if (!string.IsNullOrWhiteSpace(displayIcon))
        {
            var value = displayIcon.Trim('"');
            var commaIndex = value.LastIndexOf(',');
            var candidatePath = commaIndex > 0 ? value[..commaIndex] : value;

            if (File.Exists(candidatePath) && HasIconCapableExtension(candidatePath))
            {
                return candidatePath;
            }
        }

        if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
        {
            var exe = Directory.EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (exe is not null)
            {
                return exe;
            }
        }

        return null;
    }

    private static bool HasIconCapableExtension(string path) =>
        path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
}
