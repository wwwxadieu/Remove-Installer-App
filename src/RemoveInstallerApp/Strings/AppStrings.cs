using System.Globalization;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Strings;

/// <summary>
/// Hand-rolled EN/VI string table. A plain static lookup (instead of .resx + generated
/// designer classes) so the whole app builds with a single `dotnet build`, with no
/// Visual Studio single-file-generator step required.
/// </summary>
public static class AppStrings
{
    private static readonly Dictionary<string, string> En = new()
    {
        ["AppTitle"] = "Remove Installer App",
        ["NavInstalledApps"] = "Installed Apps",
        ["NavLeftoverCleaner"] = "Leftover Cleaner",
        ["NavSettings"] = "Settings",

        ["AppList_SearchPlaceholder"] = "Search installed apps...",
        ["AppList_Refresh"] = "Refresh",
        ["AppList_ColumnName"] = "Name",
        ["AppList_ColumnPublisher"] = "Publisher",
        ["AppList_ColumnVersion"] = "Version",
        ["AppList_ColumnSize"] = "Size",
        ["AppList_Uninstall"] = "Uninstall",
        ["AppList_Loading"] = "Loading installed apps...",
        ["AppList_Empty"] = "No apps found.",
        ["AppList_ConfirmTitle"] = "Uninstall app?",
        ["AppList_ConfirmMessage"] = "This will uninstall \"{0}\" and then scan for leftover files and registry entries. Continue?",
        ["AppList_ResultTitle"] = "Uninstall result",
        ["AppList_ResultSucceeded"] = "\"{0}\" was uninstalled successfully.",
        ["AppList_ResultForceRemoved"] = "\"{0}\" had no uninstaller, so it was removed manually.",
        ["AppList_ResultFailed"] = "Failed to uninstall \"{0}\" (exit code {1}).",
        ["AppList_ResultError"] = "An error occurred while uninstalling \"{0}\": {1}",
        ["AppList_ResidueFound"] = "Found {0} leftover item(s). Review them on the Leftover Cleaner page?",
        ["AppList_GoToResidue"] = "Review leftovers",
        ["AppList_ContextMenuNoMatchTitle"] = "No matching app found",
        ["AppList_ContextMenuNoMatchMessage"] = "\"{0}\" doesn't match any app in the installed list, so there's nothing to uninstall here.",

        ["Residue_Title"] = "Leftover Cleaner",
        ["Residue_ScanOrphans"] = "Scan for orphaned entries",
        ["Residue_Scanning"] = "Scanning...",
        ["Residue_SelectAll"] = "Select all",
        ["Residue_ClearSelection"] = "Clear selection",
        ["Residue_DeleteSelected"] = "Delete selected",
        ["Residue_Empty"] = "Nothing found. Uninstall an app, or run a scan for orphaned entries.",
        ["Residue_ColumnType"] = "Type",
        ["Residue_ColumnPath"] = "Location",
        ["Residue_ColumnSize"] = "Size",
        ["Residue_ConfirmDeleteTitle"] = "Delete selected items?",
        ["Residue_ConfirmDeleteMessage"] = "This will permanently delete {0} selected item(s). This cannot be undone.",
        ["Residue_DeleteErrorsTitle"] = "Some items could not be deleted",

        ["Kind_Folder"] = "Folder",
        ["Kind_File"] = "File",
        ["Kind_Shortcut"] = "Shortcut",
        ["Kind_RegistryKey"] = "Registry key",
        ["Kind_OrphanedUninstallEntry"] = "Orphaned uninstall entry",
        ["Kind_OrphanedRunEntry"] = "Orphaned startup entry",

        ["Settings_Title"] = "Settings",
        ["Settings_Language"] = "Language",
        ["Settings_LanguageEnglish"] = "English",
        ["Settings_LanguageVietnamese"] = "Tiếng Việt",
        ["Settings_SilentUninstall"] = "Prefer silent uninstall",
        ["Settings_SilentUninstallDescription"] = "Use each app's quiet/unattended uninstall option when available, to avoid extra prompts.",

        ["Settings_ContextMenu"] = "Add \"Uninstall with Remove Installer App\" to the right-click menu",
        ["Settings_ContextMenuDescription"] = "Adds this option to the right-click menu of .exe files and shortcuts (Start menu, Desktop), so you can start an uninstall straight from Explorer. Applies to your Windows user account only.",
        ["ContextMenu_UninstallVerb"] = "Uninstall with Remove Installer App",

        ["Settings_UpdateSection"] = "Updates",
        ["Settings_CurrentVersion"] = "Current version: {0}",
        ["Settings_CheckForUpdate"] = "Check for updates",
        ["Settings_Checking"] = "Checking for updates...",
        ["Settings_UpToDate"] = "You're on the latest version.",
        ["Settings_UpdateAvailable"] = "A new version ({0}) is available.",
        ["Settings_UpdateCheckFailed"] = "Couldn't check for updates: {0}",
        ["Settings_DownloadUpdate"] = "Download update",
        ["Settings_ViewRelease"] = "View release",
        ["Settings_AutoCheckUpdate"] = "Automatically check for updates on launch",
        ["Settings_AutoCheckUpdateDescription"] = "Silently checks GitHub for a newer version each time the app starts. No data is sent besides the request itself.",

        ["Common_Yes"] = "Yes",
        ["Common_No"] = "No",
        ["Common_Cancel"] = "Cancel",
        ["Common_Close"] = "Close",
        ["Common_OK"] = "OK",
    };

    private static readonly Dictionary<string, string> Vi = new()
    {
        ["AppTitle"] = "Remove Installer App",
        ["NavInstalledApps"] = "Ứng dụng đã cài",
        ["NavLeftoverCleaner"] = "Dọn dẹp file rác",
        ["NavSettings"] = "Cài đặt",

        ["AppList_SearchPlaceholder"] = "Tìm ứng dụng đã cài...",
        ["AppList_Refresh"] = "Làm mới",
        ["AppList_ColumnName"] = "Tên",
        ["AppList_ColumnPublisher"] = "Nhà phát hành",
        ["AppList_ColumnVersion"] = "Phiên bản",
        ["AppList_ColumnSize"] = "Dung lượng",
        ["AppList_Uninstall"] = "Gỡ cài đặt",
        ["AppList_Loading"] = "Đang tải danh sách ứng dụng...",
        ["AppList_Empty"] = "Không tìm thấy ứng dụng nào.",
        ["AppList_ConfirmTitle"] = "Gỡ cài đặt ứng dụng?",
        ["AppList_ConfirmMessage"] = "Thao tác này sẽ gỡ cài đặt \"{0}\" và quét file/registry còn sót lại. Tiếp tục?",
        ["AppList_ResultTitle"] = "Kết quả gỡ cài đặt",
        ["AppList_ResultSucceeded"] = "Đã gỡ cài đặt \"{0}\" thành công.",
        ["AppList_ResultForceRemoved"] = "\"{0}\" không có trình gỡ cài đặt nên đã được xoá thủ công.",
        ["AppList_ResultFailed"] = "Gỡ cài đặt \"{0}\" thất bại (mã thoát {1}).",
        ["AppList_ResultError"] = "Có lỗi xảy ra khi gỡ cài đặt \"{0}\": {1}",
        ["AppList_ResidueFound"] = "Tìm thấy {0} mục còn sót lại. Xem chi tiết ở trang Dọn dẹp file rác?",
        ["AppList_GoToResidue"] = "Xem file rác",
        ["AppList_ContextMenuNoMatchTitle"] = "Không tìm thấy ứng dụng phù hợp",
        ["AppList_ContextMenuNoMatchMessage"] = "\"{0}\" không khớp với ứng dụng nào trong danh sách đã cài, nên không có gì để gỡ ở đây.",

        ["Residue_Title"] = "Dọn dẹp file rác",
        ["Residue_ScanOrphans"] = "Quét mục còn sót lại",
        ["Residue_Scanning"] = "Đang quét...",
        ["Residue_SelectAll"] = "Chọn tất cả",
        ["Residue_ClearSelection"] = "Bỏ chọn tất cả",
        ["Residue_DeleteSelected"] = "Xoá mục đã chọn",
        ["Residue_Empty"] = "Không có mục nào. Hãy gỡ một ứng dụng, hoặc quét mục còn sót lại.",
        ["Residue_ColumnType"] = "Loại",
        ["Residue_ColumnPath"] = "Vị trí",
        ["Residue_ColumnSize"] = "Dung lượng",
        ["Residue_ConfirmDeleteTitle"] = "Xoá các mục đã chọn?",
        ["Residue_ConfirmDeleteMessage"] = "Thao tác này sẽ xoá vĩnh viễn {0} mục đã chọn. Không thể hoàn tác.",
        ["Residue_DeleteErrorsTitle"] = "Một số mục không thể xoá",

        ["Kind_Folder"] = "Thư mục",
        ["Kind_File"] = "Tệp tin",
        ["Kind_Shortcut"] = "Lối tắt",
        ["Kind_RegistryKey"] = "Khoá registry",
        ["Kind_OrphanedUninstallEntry"] = "Mục gỡ cài đặt còn sót",
        ["Kind_OrphanedRunEntry"] = "Mục khởi động còn sót",

        ["Settings_Title"] = "Cài đặt",
        ["Settings_Language"] = "Ngôn ngữ",
        ["Settings_LanguageEnglish"] = "English",
        ["Settings_LanguageVietnamese"] = "Tiếng Việt",
        ["Settings_SilentUninstall"] = "Ưu tiên gỡ cài đặt im lặng",
        ["Settings_SilentUninstallDescription"] = "Dùng chế độ gỡ cài đặt im lặng/tự động của từng ứng dụng khi có thể, để tránh hộp thoại phát sinh.",

        ["Settings_ContextMenu"] = "Thêm \"Gỡ bằng Remove Installer App\" vào menu chuột phải",
        ["Settings_ContextMenuDescription"] = "Thêm lựa chọn này vào menu chuột phải của file .exe và shortcut (Start Menu, Desktop), để gỡ cài đặt ngay từ File Explorer. Chỉ áp dụng cho tài khoản Windows hiện tại.",
        ["ContextMenu_UninstallVerb"] = "Gỡ bằng Remove Installer App",

        ["Settings_UpdateSection"] = "Cập nhật",
        ["Settings_CurrentVersion"] = "Phiên bản hiện tại: {0}",
        ["Settings_CheckForUpdate"] = "Kiểm tra cập nhật",
        ["Settings_Checking"] = "Đang kiểm tra cập nhật...",
        ["Settings_UpToDate"] = "Bạn đang dùng phiên bản mới nhất.",
        ["Settings_UpdateAvailable"] = "Đã có phiên bản mới ({0}).",
        ["Settings_UpdateCheckFailed"] = "Không thể kiểm tra cập nhật: {0}",
        ["Settings_DownloadUpdate"] = "Tải bản cập nhật",
        ["Settings_ViewRelease"] = "Xem trang phát hành",
        ["Settings_AutoCheckUpdate"] = "Tự động kiểm tra cập nhật khi khởi động",
        ["Settings_AutoCheckUpdateDescription"] = "Âm thầm kiểm tra GitHub xem có phiên bản mới hơn mỗi khi mở ứng dụng. Không gửi dữ liệu nào khác ngoài yêu cầu kiểm tra.",

        ["Common_Yes"] = "Có",
        ["Common_No"] = "Không",
        ["Common_Cancel"] = "Huỷ",
        ["Common_Close"] = "Đóng",
        ["Common_OK"] = "Đồng ý",
    };

    private static Dictionary<string, string> ActiveTable =>
        CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ? Vi : En;

    private static string Get(string key) => ActiveTable.TryGetValue(key, out var value) ? value : key;

    public static string AppTitle => Get("AppTitle");
    public static string NavInstalledApps => Get("NavInstalledApps");
    public static string NavLeftoverCleaner => Get("NavLeftoverCleaner");
    public static string NavSettings => Get("NavSettings");

    public static string AppList_SearchPlaceholder => Get("AppList_SearchPlaceholder");
    public static string AppList_Refresh => Get("AppList_Refresh");
    public static string AppList_ColumnName => Get("AppList_ColumnName");
    public static string AppList_ColumnPublisher => Get("AppList_ColumnPublisher");
    public static string AppList_ColumnVersion => Get("AppList_ColumnVersion");
    public static string AppList_ColumnSize => Get("AppList_ColumnSize");
    public static string AppList_Uninstall => Get("AppList_Uninstall");
    public static string AppList_Loading => Get("AppList_Loading");
    public static string AppList_Empty => Get("AppList_Empty");
    public static string AppList_ConfirmTitle => Get("AppList_ConfirmTitle");
    public static string AppList_ConfirmMessage(string appName) => string.Format(Get("AppList_ConfirmMessage"), appName);
    public static string AppList_ResultTitle => Get("AppList_ResultTitle");
    public static string AppList_ResultSucceeded(string appName) => string.Format(Get("AppList_ResultSucceeded"), appName);
    public static string AppList_ResultForceRemoved(string appName) => string.Format(Get("AppList_ResultForceRemoved"), appName);
    public static string AppList_ResultFailed(string appName, int? exitCode) => string.Format(Get("AppList_ResultFailed"), appName, exitCode?.ToString() ?? "?");
    public static string AppList_ResultError(string appName, string error) => string.Format(Get("AppList_ResultError"), appName, error);
    public static string AppList_ResidueFound(int count) => string.Format(Get("AppList_ResidueFound"), count);
    public static string AppList_GoToResidue => Get("AppList_GoToResidue");
    public static string AppList_ContextMenuNoMatchTitle => Get("AppList_ContextMenuNoMatchTitle");
    public static string AppList_ContextMenuNoMatchMessage(string fileName) => string.Format(Get("AppList_ContextMenuNoMatchMessage"), fileName);

    public static string Residue_Title => Get("Residue_Title");
    public static string Residue_ScanOrphans => Get("Residue_ScanOrphans");
    public static string Residue_Scanning => Get("Residue_Scanning");
    public static string Residue_SelectAll => Get("Residue_SelectAll");
    public static string Residue_ClearSelection => Get("Residue_ClearSelection");
    public static string Residue_DeleteSelected => Get("Residue_DeleteSelected");
    public static string Residue_Empty => Get("Residue_Empty");
    public static string Residue_ColumnType => Get("Residue_ColumnType");
    public static string Residue_ColumnPath => Get("Residue_ColumnPath");
    public static string Residue_ColumnSize => Get("Residue_ColumnSize");
    public static string Residue_ConfirmDeleteTitle => Get("Residue_ConfirmDeleteTitle");
    public static string Residue_ConfirmDeleteMessage(int count) => string.Format(Get("Residue_ConfirmDeleteMessage"), count);
    public static string Residue_DeleteErrorsTitle => Get("Residue_DeleteErrorsTitle");

    public static string Settings_Title => Get("Settings_Title");
    public static string Settings_Language => Get("Settings_Language");
    public static string Settings_LanguageEnglish => Get("Settings_LanguageEnglish");
    public static string Settings_LanguageVietnamese => Get("Settings_LanguageVietnamese");
    public static string Settings_SilentUninstall => Get("Settings_SilentUninstall");
    public static string Settings_SilentUninstallDescription => Get("Settings_SilentUninstallDescription");

    public static string Settings_ContextMenu => Get("Settings_ContextMenu");
    public static string Settings_ContextMenuDescription => Get("Settings_ContextMenuDescription");
    public static string ContextMenu_UninstallVerb => Get("ContextMenu_UninstallVerb");

    public static string Settings_UpdateSection => Get("Settings_UpdateSection");
    public static string Settings_CurrentVersion(string version) => string.Format(Get("Settings_CurrentVersion"), version);
    public static string Settings_CheckForUpdate => Get("Settings_CheckForUpdate");
    public static string Settings_Checking => Get("Settings_Checking");
    public static string Settings_UpToDate => Get("Settings_UpToDate");
    public static string Settings_UpdateAvailable(string version) => string.Format(Get("Settings_UpdateAvailable"), version);
    public static string Settings_UpdateCheckFailed(string error) => string.Format(Get("Settings_UpdateCheckFailed"), error);
    public static string Settings_DownloadUpdate => Get("Settings_DownloadUpdate");
    public static string Settings_ViewRelease => Get("Settings_ViewRelease");
    public static string Settings_AutoCheckUpdate => Get("Settings_AutoCheckUpdate");
    public static string Settings_AutoCheckUpdateDescription => Get("Settings_AutoCheckUpdateDescription");

    public static string Common_Yes => Get("Common_Yes");
    public static string Common_No => Get("Common_No");
    public static string Common_Cancel => Get("Common_Cancel");
    public static string Common_Close => Get("Common_Close");
    public static string Common_OK => Get("Common_OK");

    public static string KindLabel(ResidueKind kind) => Get(kind switch
    {
        ResidueKind.Folder => "Kind_Folder",
        ResidueKind.File => "Kind_File",
        ResidueKind.Shortcut => "Kind_Shortcut",
        ResidueKind.RegistryKey => "Kind_RegistryKey",
        ResidueKind.OrphanedUninstallEntry => "Kind_OrphanedUninstallEntry",
        ResidueKind.OrphanedRunEntry => "Kind_OrphanedRunEntry",
        _ => "Kind_File",
    });
}
