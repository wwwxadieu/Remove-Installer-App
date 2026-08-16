using System.Runtime.InteropServices;

namespace UnInstall.Helpers;

/// <summary>
/// Sends files and folders to the Recycle Bin via shell32!SHFileOperationW, so a leftover the
/// scanner matched by mistake can still be recovered. This is the counterpart to
/// <see cref="RecycleBinInterop"/>, which only reports on and empties the bin for Disk Cleanup —
/// the two are kept apart because they serve opposite purposes.
/// </summary>
public static class RecycleBin
{
    private const uint FO_DELETE = 0x0003;

    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;

    /// <summary>
    /// Moves a single file or folder to the Recycle Bin. Returns false when the shell refused
    /// the operation (for example the item is on a volume with no Recycle Bin, or is larger
    /// than the bin's quota) so the caller can decide whether to fall back to a hard delete.
    /// </summary>
    public static bool TrySend(string path, out string? error)
    {
        error = null;

        try
        {
            var fullPath = Path.GetFullPath(path);

            var op = new SHFILEOPSTRUCTW
            {
                wFunc = FO_DELETE,
                // SHFileOperationW takes a *list* of paths and reads until it sees an empty
                // entry, so the buffer must end with a second NUL. Missing it makes the API
                // read past the string.
                pFrom = fullPath + "\0\0",
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT,
            };

            var result = SHFileOperationW(ref op);

            if (result != 0)
            {
                error = $"SHFileOperationW returned 0x{result:X8}.";
                return false;
            }

            if (op.fAnyOperationsAborted)
            {
                error = "The shell aborted the operation.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCTW lpFileOp);
}
