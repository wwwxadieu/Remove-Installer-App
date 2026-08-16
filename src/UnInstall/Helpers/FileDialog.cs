using System.Runtime.InteropServices;

namespace UnInstall.Helpers;

/// <summary>
/// File and folder pickers built on the classic shell COM dialog (IFileOpenDialog).
///
/// The WinRT <c>Windows.Storage.Pickers</c> types are not usable here: this app runs elevated
/// (see app.manifest, requireAdministrator), and those pickers are brokered through a service
/// that fails in an elevated process — which left the Force Delete page unable to add anything
/// at all. IFileOpenDialog is the plain Win32 dialog and works regardless of elevation.
///
/// Declared in the minimal-interop style already used by <see cref="ShortcutResolver"/>: only
/// the vtable slots actually called are declared, and their order must match the interface.
/// </summary>
public static class FileDialog
{
    private const uint FOS_PICKFOLDERS = 0x00000020;
    private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    private const uint FOS_PATHMUSTEXIST = 0x00000800;
    private const uint FOS_FILEMUSTEXIST = 0x00001000;

    private const uint SIGDN_FILESYSPATH = 0x80058000;

    // Returned when the user cancels — not an error worth surfacing.
    private const int ERROR_CANCELLED = unchecked((int)0x800704C7);

    public static string? PickFile(IntPtr owner) =>
        Show(owner, FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_FILEMUSTEXIST);

    public static string? PickFolder(IntPtr owner) =>
        Show(owner, FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);

    private static string? Show(IntPtr owner, uint options)
    {
        IFileOpenDialog? dialog = null;
        try
        {
            dialog = (IFileOpenDialog)new FileOpenDialogRcw();
            dialog.SetOptions(options);
            dialog.Show(owner);

            dialog.GetResult(out var item);
            item.GetDisplayName(SIGDN_FILESYSPATH, out var path);
            return path;
        }
        catch (COMException ex) when (ex.HResult == ERROR_CANCELLED)
        {
            return null;
        }
        catch (Exception ex)
        {
            AppLog.Error("Shell file dialog failed.", ex);
            return null;
        }
        finally
        {
            if (dialog is not null)
            {
                Marshal.ReleaseComObject(dialog);
            }
        }
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialogRcw
    {
    }

    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        // IModalWindow
        [PreserveSig] int Show(IntPtr parent);

        // IFileDialog — every slot up to the ones used must be declared so the vtable lines up.
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
}
