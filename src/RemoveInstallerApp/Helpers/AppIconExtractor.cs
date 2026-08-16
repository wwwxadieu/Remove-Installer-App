using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Extracts an installed app's icon as PNG bytes, from either its registry
/// <c>DisplayIcon</c> value (typically "C:\path\app.exe,0", sometimes with a negative resource
/// ID after the comma) or the main .exe under its install folder as a fallback.
///
/// Synchronous and does real file/GDI work — callers must run this off the UI thread (see
/// <see cref="ViewModels.AppListViewModel"/>, which loads icons in the background after the app
/// list is already showing).
/// </summary>
public static class AppIconExtractor
{
    public static byte[]? ExtractPngBytes(string? displayIcon, string? installLocation)
    {
        var (path, index) = ResolveIconSource(displayIcon, installLocation);
        if (path is null)
        {
            return null;
        }

        var handles = new nint[1];
        try
        {
            var extracted = ExtractIconEx(path, index, handles, null, 1);
            if (extracted == 0 || handles[0] == 0)
            {
                return null;
            }

            using var icon = Icon.FromHandle(handles[0]);
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Icon extraction failed for \"{path}\": {ex.Message}");
            return null;
        }
        finally
        {
            if (handles[0] != 0)
            {
                DestroyIcon(handles[0]);
            }
        }
    }

    private static (string? Path, int Index) ResolveIconSource(string? displayIcon, string? installLocation)
    {
        if (!string.IsNullOrWhiteSpace(displayIcon))
        {
            var value = displayIcon.Trim('"');
            var commaIndex = value.LastIndexOf(',');
            var candidatePath = value;
            var index = 0;

            if (commaIndex > 0 && int.TryParse(value[(commaIndex + 1)..], out var parsedIndex))
            {
                candidatePath = value[..commaIndex];
                index = parsedIndex;
            }

            if (File.Exists(candidatePath) && HasIconCapableExtension(candidatePath))
            {
                return (candidatePath, index);
            }
        }

        if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
        {
            var exe = Directory.EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (exe is not null)
            {
                return (exe, 0);
            }
        }

        return (null, 0);
    }

    private static bool HasIconCapableExtension(string path) =>
        path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, nint[] phiconLarge, nint[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);
}
