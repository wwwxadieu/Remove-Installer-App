using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Resolves a .lnk shortcut's target path via the classic IShellLinkW COM object, so the
/// Explorer context-menu handler can match Start Menu tiles and Desktop icons (not just raw
/// .exe files) against an installed app.
/// </summary>
public static class ShortcutResolver
{
    public static string? ResolveTarget(string shortcutPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            ((IPersistFile)link).Load(shortcutPath, 0);

            var buffer = new StringBuilder(260);
            link.GetPath(buffer, buffer.Capacity, out _, 0);

            var target = buffer.ToString();
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch (Exception)
        {
            // Best-effort: a shortcut we can't read (missing file, malformed .lnk, access
            // denied) just means the caller falls back to treating the .lnk path as-is.
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        // Only GetPath (the first vtable slot after IUnknown) is declared — it's all this
        // resolver calls, and its position must stay first for the interop vtable to line up.
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath,
            out WIN32_FIND_DATAW pfd,
            int fFlags);
    }
}
