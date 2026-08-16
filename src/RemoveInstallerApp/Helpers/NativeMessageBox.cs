using System.Runtime.InteropServices;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Thin wrapper over user32!MessageBoxW for the headless "Quick uninstall" context-menu verb,
/// which runs with no Window/XamlRoot and so can't use ContentDialog.
/// </summary>
public static class NativeMessageBox
{
    private const uint MB_YESNO = 0x4;
    private const uint MB_ICONQUESTION = 0x20;
    private const uint MB_ICONINFORMATION = 0x40;
    private const uint MB_TOPMOST = 0x40000;
    private const uint MB_SETFOREGROUND = 0x10000;
    private const int IDYES = 6;

    public static bool Confirm(string text, string caption) =>
        MessageBoxW(IntPtr.Zero, text, caption, MB_YESNO | MB_ICONQUESTION | MB_TOPMOST | MB_SETFOREGROUND) == IDYES;

    public static void ShowInfo(string text, string caption) =>
        MessageBoxW(IntPtr.Zero, text, caption, MB_ICONINFORMATION | MB_TOPMOST | MB_SETFOREGROUND);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
