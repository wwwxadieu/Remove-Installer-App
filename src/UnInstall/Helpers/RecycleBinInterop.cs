using System.Runtime.InteropServices;

namespace UnInstall.Helpers;

/// <summary>
/// Wraps the Recycle Bin's own shell32 APIs (SHQueryRecycleBinW/SHEmptyRecycleBinW) rather than
/// deleting files under $Recycle.Bin directly — that folder's per-SID layout and ACLs make manual
/// deletion fragile, whereas these are the exact APIs Windows' own Disk Cleanup / "Empty Recycle
/// Bin" menu command use.
/// </summary>
public static class RecycleBinInterop
{
    private const uint SHERB_NOCONFIRMATION = 0x1;
    private const uint SHERB_NOPROGRESSUI = 0x2;
    private const uint SHERB_NOSOUND = 0x4;

    // Returned by SHEmptyRecycleBinW when the bin is already empty — not a real failure.
    private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

    /// <summary>Total size across all drives' Recycle Bins, in bytes. Returns 0 on failure.</summary>
    public static long GetSizeBytes()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        var hr = SHQueryRecycleBinW(null, ref info);
        return hr == 0 ? info.i64Size : 0;
    }

    public static bool Empty()
    {
        var hr = SHEmptyRecycleBinW(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
        return hr == 0 || hr == E_UNEXPECTED;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBinW(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBinW(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}
