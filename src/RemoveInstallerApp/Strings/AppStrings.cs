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
        ["NavForceDelete"] = "Force Delete",
        ["NavDiskCleanup"] = "Disk Cleanup",
        ["NavSettings"] = "Settings",

        ["Welcome_Title"] = "Welcome to Remove Installer App!",
        ["Welcome_Intro"] = "This app helps you uninstall Windows applications cleanly, with a few extra tools for the leftovers a normal uninstall doesn't catch:",
        ["Welcome_FeatureList"] =
            "• Installed Apps — one-click uninstall, with an optional System Restore point created first.\n" +
            "• Leftover Cleaner — scans for files and registry entries an uninstall left behind.\n" +
            "• Force Delete — force-delete or securely wipe files/folders that won't delete normally.\n" +
            "• Disk Cleanup — clears temp files, the Recycle Bin, and other OS-wide junk, like Windows' own Disk Cleanup.\n" +
            "• Optional right-click entries in File Explorer to uninstall an app, with or without opening this window.",
        ["Welcome_UpdatedTitle"] = "Updated to version {0}",
        ["Welcome_UpdatedGenericMessage"] = "The app was updated to a new version. No release notes were available for it.",

        ["License_SectionTitle"] = "Pro features",
        ["License_SectionDescription"] = "A few advanced tools (secure delete, Disk Cleanup) are being trialed as possible paid features. This is a local, personal trial only — there's no purchase flow yet.",
        ["License_StatusFree"] = "Free tier.",
        ["License_StatusTrial"] = "Pro trial active — {0} day(s) left.",
        ["License_StatusTrialExpired"] = "Your Pro trial has ended.",
        ["License_StartTrialButton"] = "Start 30-day Pro trial",
        ["License_EndTrialButton"] = "End trial / back to Free",
        ["License_UpgradeTitle"] = "Pro feature",
        ["License_UpgradeMessage"] = "\"{0}\" is a Pro feature. Start the 30-day Pro trial to unlock it?",

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
        ["AppList_ResultNoUninstaller"] = "\"{0}\" has no uninstaller of its own. Nothing was removed, because \"Always use the app's own uninstaller\" is turned on in Settings.",
        ["AppList_ResidueFound"] = "Found {0} leftover item(s). Review them on the Leftover Cleaner page?",
        ["AppList_GoToResidue"] = "Review leftovers",
        ["AppList_ContextMenuNoMatchTitle"] = "No matching app found",
        ["AppList_ContextMenuNoMatchMessage"] = "\"{0}\" doesn't match any app in the installed list, so there's nothing to uninstall here.",

        ["Backup_ConfirmTitle"] = "Create a restore point?",
        ["Backup_ConfirmMessage"] = "Create a System Restore point before uninstalling \"{0}\"? This lets you undo the uninstall from Windows if something goes wrong.",
        ["Backup_RestorePointDescription"] = "Before uninstalling {0} (Remove Installer App)",
        ["Backup_Creating"] = "Creating restore point...",
        ["Backup_Failed"] = "Couldn't create a restore point: {0}",
        ["Backup_ContinueAnyway"] = "Continue with the uninstall anyway?",

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

        ["ForceDelete_Title"] = "Force Delete",
        ["ForceDelete_BrowseFile"] = "Browse file...",
        ["ForceDelete_BrowseFolder"] = "Browse folder...",
        ["ForceDelete_SecureDelete"] = "Delete unrecoverably",
        ["ForceDelete_SecureDeleteDescription"] = "Overwrites file contents with random data before deleting. Note: on SSDs, wear-leveling/TRIM means this does not guarantee the original data can never be recovered.",
        ["ForceDelete_DeleteButton"] = "Delete",
        ["ForceDelete_Empty"] = "Nothing queued. Browse for a file/folder, or drag and drop one here.",
        ["ForceDelete_ColumnType"] = "Type",
        ["ForceDelete_ColumnPath"] = "Location",
        ["ForceDelete_ColumnSize"] = "Size",
        ["ForceDelete_RemoveFromQueue"] = "Remove",
        ["ForceDelete_ConfirmTitle"] = "Delete queued items?",
        ["ForceDelete_ConfirmMessage"] = "This will force-delete {0} selected item(s). This cannot be undone.",
        ["ForceDelete_ConfirmMessageSecure"] = "This will overwrite and force-delete {0} selected item(s) unrecoverably. This cannot be undone.",
        ["ForceDelete_ResultTitle"] = "Delete result",
        ["ForceDelete_ResultSummary"] = "{0} item(s) deleted, {1} scheduled for deletion.",
        ["ForceDelete_RebootRequiredNotice"] = "{0} item(s) are still locked by another process and will be deleted automatically the next time you restart your computer.",
        ["ForceDelete_ErrorsTitle"] = "Some items could not be deleted",
        ["ForceDelete_AddErrorTitle"] = "Couldn't add item",
        ["ForceDelete_PathNotFound"] = "\"{0}\" no longer exists.",
        ["ForceDelete_UnsafePath"] = "\"{0}\" is a protected system location and can't be added here.",

        ["DiskCleanup_Title"] = "Disk Cleanup",
        ["DiskCleanup_Scan"] = "Scan",
        ["DiskCleanup_Scanning"] = "Scanning...",
        ["DiskCleanup_SelectAll"] = "Select all",
        ["DiskCleanup_ClearSelection"] = "Clear selection",
        ["DiskCleanup_Clean"] = "Clean up",
        ["DiskCleanup_Empty"] = "Nothing scanned yet. Click \"Scan\" to check how much space each category is using.",
        ["DiskCleanup_ConfirmTitle"] = "Clean up selected categories?",
        ["DiskCleanup_ConfirmMessage"] = "This will permanently delete {0} of files across {1} selected category(ies). This cannot be undone.",
        ["DiskCleanup_ResultTitle"] = "Disk Cleanup result",
        ["DiskCleanup_ResultSummary"] = "Freed up {0}.",
        ["DiskCleanup_ResultSkipped"] = "{0} file(s) were skipped because they were in use by another program.",
        ["DiskCleanup_ErrorsTitle"] = "Some categories could not be cleaned",

        ["DiskCleanupCategory_TemporaryFiles_Name"] = "Temporary files",
        ["DiskCleanupCategory_TemporaryFiles_Description"] = "Files apps left behind in your user Temp folder and C:\\Windows\\Temp that they no longer need.",
        ["DiskCleanupCategory_RecycleBin_Name"] = "Recycle Bin",
        ["DiskCleanupCategory_RecycleBin_Description"] = "Files you've deleted that are still recoverable from the Recycle Bin.",
        ["DiskCleanupCategory_ThumbnailCache_Name"] = "Thumbnail cache",
        ["DiskCleanupCategory_ThumbnailCache_Description"] = "Cached thumbnail previews. Windows regenerates these automatically as needed.",
        ["DiskCleanupCategory_WindowsUpdateCleanup_Name"] = "Windows Update cleanup",
        ["DiskCleanupCategory_WindowsUpdateCleanup_Description"] = "Downloaded Windows Update packages that have already been installed.",
        ["DiskCleanupCategory_DeliveryOptimizationFiles_Name"] = "Delivery Optimization files",
        ["DiskCleanupCategory_DeliveryOptimizationFiles_Description"] = "Locally cached update/app files used to share downloads with other devices on your network.",
        ["DiskCleanupCategory_WindowsErrorReports_Name"] = "Windows Error Reporting files",
        ["DiskCleanupCategory_WindowsErrorReports_Description"] = "Crash/error report files queued or archived for diagnostics.",
        ["DiskCleanupCategory_MemoryDumpFiles_Name"] = "Memory dump files",
        ["DiskCleanupCategory_MemoryDumpFiles_Description"] = "Diagnostic dump files left behind by past system crashes.",

        ["Kind_Folder"] = "Folder",
        ["Kind_File"] = "File",
        ["Kind_Shortcut"] = "Shortcut",
        ["Kind_RegistryKey"] = "Registry key",
        ["Kind_OrphanedUninstallEntry"] = "Orphaned uninstall entry",
        ["Kind_OrphanedRunEntry"] = "Orphaned startup entry",
        ["Kind_ServiceEntry"] = "Windows service",
        ["Kind_ScheduledTask"] = "Scheduled task",

        ["ScanStep_InstallFolders"] = "Install and data folders",
        ["ScanStep_TempFolders"] = "Temp folders",
        ["ScanStep_Shortcuts"] = "Shortcuts",
        ["ScanStep_StartupFolders"] = "Startup folders",
        ["ScanStep_SoftwareKeys"] = "SOFTWARE registry keys",
        ["ScanStep_ClassesRoot"] = "File associations (HKEY_CLASSES_ROOT)",
        ["ScanStep_AppPaths"] = "App Paths entries",
        ["ScanStep_RunKeys"] = "Startup registry entries",
        ["ScanStep_Services"] = "Windows services",
        ["ScanStep_ScheduledTasks"] = "Scheduled tasks",
        ["ScanStep_UninstallEntry"] = "Add/Remove Programs entry",
        ["ScanStep_Done"] = "Scan complete",
        ["ScanProgress_Status"] = "Scanning: {0} — {1} item(s) found",

        ["PostUninstall_Title"] = "Cleaning up leftovers",
        ["PostUninstall_NothingFound"] = "No leftover files or registry entries were found.",
        ["PostUninstall_ScanFailed"] = "The leftover scan could not be completed. See error.log for details.",
        ["PostUninstall_Deleting"] = "Deleting selected items...",
        ["PostUninstall_DeletedToRecycleBin"] = "Moved {0} item(s) to the Recycle Bin.",
        ["PostUninstall_DeletedPermanently"] = "Permanently deleted {0} item(s).",

        ["Settings_Title"] = "Settings",
        ["Settings_Language"] = "Language",
        ["Settings_LanguageEnglish"] = "English",
        ["Settings_LanguageVietnamese"] = "Tiếng Việt",
        ["Settings_SilentUninstall"] = "Prefer silent uninstall",
        ["Settings_SilentUninstallDescription"] = "Use each app's quiet/unattended uninstall option when available, to avoid extra prompts.",
        ["Settings_AlwaysUseAppUninstaller"] = "Always use the app's own uninstaller",
        ["Settings_AlwaysUseAppUninstallerDescription"] = "Never fall back to removing an app manually. If it has no uninstaller, or the uninstaller fails, nothing is deleted and you're told why — safer, but some apps will be left installed.",
        ["Settings_PermanentlyDelete"] = "Delete leftovers permanently (skip the Recycle Bin)",
        ["Settings_PermanentlyDeleteDescription"] = "Off by default: leftover files go to the Recycle Bin so you can restore them if something was matched by mistake. Registry keys have no Recycle Bin, so a .reg backup is exported before deleting either way.",

        ["Settings_ContextMenu"] = "Add \"Uninstall with Remove Installer App\" to the right-click menu",
        ["Settings_ContextMenuDescription"] = "Adds this option to the right-click menu of .exe files and shortcuts (Start menu, Desktop), so you can start an uninstall straight from Explorer. Applies to your Windows user account only.",
        ["ContextMenu_UninstallVerb"] = "Uninstall with Remove Installer App",
        ["ContextMenu_QuickUninstallVerb"] = "Quick uninstall...",

        ["QuickUninstall_ConfirmUninstall"] = "Uninstall \"{0}\"?",
        ["QuickUninstall_ConfirmBackup"] = "Create a System Restore point before uninstalling \"{0}\"?",
        ["QuickUninstall_AppNotFound"] = "\"{0}\" doesn't match any installed app, so there's nothing to uninstall.",
        ["QuickUninstall_ResultTitle"] = "Remove Installer App",

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
        ["NavForceDelete"] = "Xoá ép buộc",
        ["NavDiskCleanup"] = "Dọn ổ đĩa",
        ["NavSettings"] = "Cài đặt",

        ["Welcome_Title"] = "Chào mừng đến với Remove Installer App!",
        ["Welcome_Intro"] = "Ứng dụng này giúp bạn gỡ cài đặt ứng dụng Windows một cách sạch sẽ, kèm vài công cụ xử lý những thứ còn sót lại mà gỡ cài đặt thông thường không dọn tới:",
        ["Welcome_FeatureList"] =
            "• Ứng dụng đã cài — gỡ cài đặt chỉ với một cú bấm, có thể tạo điểm khôi phục hệ thống trước.\n" +
            "• Dọn dẹp file rác — quét file và registry còn sót lại sau khi gỡ cài đặt.\n" +
            "• Xoá ép buộc — ép buộc xoá hoặc xoá không thể khôi phục file/thư mục không xoá được bình thường.\n" +
            "• Dọn ổ đĩa — dọn file tạm, Thùng rác và các rác khác trên toàn máy, giống Disk Cleanup của Windows.\n" +
            "• Tuỳ chọn thêm mục vào menu chuột phải trong File Explorer để gỡ cài đặt, có hoặc không cần mở cửa sổ này.",
        ["Welcome_UpdatedTitle"] = "Đã cập nhật lên phiên bản {0}",
        ["Welcome_UpdatedGenericMessage"] = "Ứng dụng đã được cập nhật lên phiên bản mới. Không có ghi chú phát hành nào cho phiên bản này.",

        ["License_SectionTitle"] = "Tính năng Pro",
        ["License_SectionDescription"] = "Một số công cụ nâng cao (xoá không thể khôi phục, Dọn ổ đĩa) đang được thử nghiệm để cân nhắc tính phí. Đây chỉ là bản dùng thử cục bộ cho cá nhân — chưa có luồng mua thực sự.",
        ["License_StatusFree"] = "Đang dùng bản Miễn phí.",
        ["License_StatusTrial"] = "Đang dùng thử Pro — còn {0} ngày.",
        ["License_StatusTrialExpired"] = "Bản dùng thử Pro đã kết thúc.",
        ["License_StartTrialButton"] = "Bắt đầu dùng thử Pro 30 ngày",
        ["License_EndTrialButton"] = "Kết thúc dùng thử / về Miễn phí",
        ["License_UpgradeTitle"] = "Tính năng Pro",
        ["License_UpgradeMessage"] = "\"{0}\" là tính năng Pro. Bắt đầu dùng thử Pro 30 ngày để mở khoá?",

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
        ["AppList_ResultNoUninstaller"] = "\"{0}\" không có trình gỡ cài đặt riêng. Không có gì bị xoá, vì tuỳ chọn \"Luôn dùng trình gỡ của ứng dụng\" đang bật trong Cài đặt.",
        ["AppList_ResidueFound"] = "Tìm thấy {0} mục còn sót lại. Xem chi tiết ở trang Dọn dẹp file rác?",
        ["AppList_GoToResidue"] = "Xem file rác",
        ["AppList_ContextMenuNoMatchTitle"] = "Không tìm thấy ứng dụng phù hợp",
        ["AppList_ContextMenuNoMatchMessage"] = "\"{0}\" không khớp với ứng dụng nào trong danh sách đã cài, nên không có gì để gỡ ở đây.",

        ["Backup_ConfirmTitle"] = "Tạo điểm khôi phục?",
        ["Backup_ConfirmMessage"] = "Tạo điểm khôi phục hệ thống (System Restore) trước khi gỡ cài đặt \"{0}\"? Nhờ đó bạn có thể hoàn tác việc gỡ cài đặt từ Windows nếu có sự cố.",
        ["Backup_RestorePointDescription"] = "Trước khi gỡ {0} (Remove Installer App)",
        ["Backup_Creating"] = "Đang tạo điểm khôi phục...",
        ["Backup_Failed"] = "Không thể tạo điểm khôi phục: {0}",
        ["Backup_ContinueAnyway"] = "Vẫn tiếp tục gỡ cài đặt?",

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

        ["ForceDelete_Title"] = "Xoá ép buộc",
        ["ForceDelete_BrowseFile"] = "Chọn tệp tin...",
        ["ForceDelete_BrowseFolder"] = "Chọn thư mục...",
        ["ForceDelete_SecureDelete"] = "Xoá không thể khôi phục",
        ["ForceDelete_SecureDeleteDescription"] = "Ghi đè nội dung file bằng dữ liệu ngẫu nhiên trước khi xoá. Lưu ý: với ổ SSD, cơ chế wear-leveling/TRIM khiến việc này không đảm bảo dữ liệu gốc không thể khôi phục tuyệt đối.",
        ["ForceDelete_DeleteButton"] = "Xoá",
        ["ForceDelete_Empty"] = "Chưa có mục nào. Hãy chọn file/thư mục, hoặc kéo-thả vào đây.",
        ["ForceDelete_ColumnType"] = "Loại",
        ["ForceDelete_ColumnPath"] = "Vị trí",
        ["ForceDelete_ColumnSize"] = "Dung lượng",
        ["ForceDelete_RemoveFromQueue"] = "Bỏ khỏi hàng đợi",
        ["ForceDelete_ConfirmTitle"] = "Xoá các mục đã chọn?",
        ["ForceDelete_ConfirmMessage"] = "Thao tác này sẽ ép buộc xoá {0} mục đã chọn. Không thể hoàn tác.",
        ["ForceDelete_ConfirmMessageSecure"] = "Thao tác này sẽ ghi đè và ép buộc xoá không thể khôi phục {0} mục đã chọn. Không thể hoàn tác.",
        ["ForceDelete_ResultTitle"] = "Kết quả xoá",
        ["ForceDelete_ResultSummary"] = "Đã xoá {0} mục, {1} mục đã lên lịch xoá.",
        ["ForceDelete_RebootRequiredNotice"] = "{0} mục vẫn đang bị khoá bởi tiến trình khác và sẽ được tự động xoá vào lần khởi động lại máy tiếp theo.",
        ["ForceDelete_ErrorsTitle"] = "Một số mục không thể xoá",
        ["ForceDelete_AddErrorTitle"] = "Không thể thêm mục",
        ["ForceDelete_PathNotFound"] = "\"{0}\" không còn tồn tại.",
        ["ForceDelete_UnsafePath"] = "\"{0}\" là vị trí hệ thống được bảo vệ, không thể thêm vào đây.",

        ["DiskCleanup_Title"] = "Dọn ổ đĩa",
        ["DiskCleanup_Scan"] = "Quét",
        ["DiskCleanup_Scanning"] = "Đang quét...",
        ["DiskCleanup_SelectAll"] = "Chọn tất cả",
        ["DiskCleanup_ClearSelection"] = "Bỏ chọn tất cả",
        ["DiskCleanup_Clean"] = "Dọn dẹp",
        ["DiskCleanup_Empty"] = "Chưa quét. Bấm \"Quét\" để xem mỗi mục đang chiếm bao nhiêu dung lượng.",
        ["DiskCleanup_ConfirmTitle"] = "Dọn dẹp các mục đã chọn?",
        ["DiskCleanup_ConfirmMessage"] = "Thao tác này sẽ xoá vĩnh viễn {0} dữ liệu thuộc {1} mục đã chọn. Không thể hoàn tác.",
        ["DiskCleanup_ResultTitle"] = "Kết quả dọn ổ đĩa",
        ["DiskCleanup_ResultSummary"] = "Đã giải phóng {0}.",
        ["DiskCleanup_ResultSkipped"] = "{0} tệp tin đã bị bỏ qua vì đang được chương trình khác sử dụng.",
        ["DiskCleanup_ErrorsTitle"] = "Một số mục không thể dọn dẹp",

        ["DiskCleanupCategory_TemporaryFiles_Name"] = "Tệp tin tạm",
        ["DiskCleanupCategory_TemporaryFiles_Description"] = "Các tệp ứng dụng để lại trong thư mục Temp của bạn và C:\\Windows\\Temp mà chúng không còn cần dùng.",
        ["DiskCleanupCategory_RecycleBin_Name"] = "Thùng rác",
        ["DiskCleanupCategory_RecycleBin_Description"] = "Các tệp bạn đã xoá nhưng vẫn có thể khôi phục từ Thùng rác.",
        ["DiskCleanupCategory_ThumbnailCache_Name"] = "Bộ nhớ đệm hình thu nhỏ",
        ["DiskCleanupCategory_ThumbnailCache_Description"] = "Hình thu nhỏ được lưu đệm. Windows sẽ tự tạo lại khi cần.",
        ["DiskCleanupCategory_WindowsUpdateCleanup_Name"] = "Dọn dẹp Windows Update",
        ["DiskCleanupCategory_WindowsUpdateCleanup_Description"] = "Các gói Windows Update đã tải về và đã được cài đặt xong.",
        ["DiskCleanupCategory_DeliveryOptimizationFiles_Name"] = "Tệp Delivery Optimization",
        ["DiskCleanupCategory_DeliveryOptimizationFiles_Description"] = "Tệp cập nhật/ứng dụng được lưu đệm cục bộ để chia sẻ với thiết bị khác trong mạng của bạn.",
        ["DiskCleanupCategory_WindowsErrorReports_Name"] = "Tệp báo lỗi Windows",
        ["DiskCleanupCategory_WindowsErrorReports_Description"] = "Các tệp báo cáo lỗi/sự cố đang chờ hoặc đã lưu trữ để chẩn đoán.",
        ["DiskCleanupCategory_MemoryDumpFiles_Name"] = "Tệp memory dump",
        ["DiskCleanupCategory_MemoryDumpFiles_Description"] = "Các tệp dump chẩn đoán để lại từ những lần hệ thống bị lỗi trước đây.",

        ["Kind_Folder"] = "Thư mục",
        ["Kind_File"] = "Tệp tin",
        ["Kind_Shortcut"] = "Lối tắt",
        ["Kind_RegistryKey"] = "Khoá registry",
        ["Kind_OrphanedUninstallEntry"] = "Mục gỡ cài đặt còn sót",
        ["Kind_OrphanedRunEntry"] = "Mục khởi động còn sót",
        ["Kind_ServiceEntry"] = "Dịch vụ Windows",
        ["Kind_ScheduledTask"] = "Tác vụ theo lịch",

        ["ScanStep_InstallFolders"] = "Thư mục cài đặt và dữ liệu",
        ["ScanStep_TempFolders"] = "Thư mục tạm",
        ["ScanStep_Shortcuts"] = "Lối tắt",
        ["ScanStep_StartupFolders"] = "Thư mục khởi động",
        ["ScanStep_SoftwareKeys"] = "Khoá registry SOFTWARE",
        ["ScanStep_ClassesRoot"] = "Liên kết tệp (HKEY_CLASSES_ROOT)",
        ["ScanStep_AppPaths"] = "Mục App Paths",
        ["ScanStep_RunKeys"] = "Mục khởi động trong registry",
        ["ScanStep_Services"] = "Dịch vụ Windows",
        ["ScanStep_ScheduledTasks"] = "Tác vụ theo lịch",
        ["ScanStep_UninstallEntry"] = "Mục trong Add/Remove Programs",
        ["ScanStep_Done"] = "Quét xong",
        ["ScanProgress_Status"] = "Đang quét: {0} — tìm thấy {1} mục",

        ["PostUninstall_Title"] = "Dọn dẹp phần còn sót lại",
        ["PostUninstall_NothingFound"] = "Không tìm thấy file rác hay mục registry nào còn sót lại.",
        ["PostUninstall_ScanFailed"] = "Không hoàn tất được việc quét. Xem chi tiết trong error.log.",
        ["PostUninstall_Deleting"] = "Đang xoá các mục đã chọn...",
        ["PostUninstall_DeletedToRecycleBin"] = "Đã chuyển {0} mục vào Thùng rác.",
        ["PostUninstall_DeletedPermanently"] = "Đã xoá vĩnh viễn {0} mục.",

        ["Settings_Title"] = "Cài đặt",
        ["Settings_Language"] = "Ngôn ngữ",
        ["Settings_LanguageEnglish"] = "English",
        ["Settings_LanguageVietnamese"] = "Tiếng Việt",
        ["Settings_SilentUninstall"] = "Ưu tiên gỡ cài đặt im lặng",
        ["Settings_SilentUninstallDescription"] = "Dùng chế độ gỡ cài đặt im lặng/tự động của từng ứng dụng khi có thể, để tránh hộp thoại phát sinh.",
        ["Settings_AlwaysUseAppUninstaller"] = "Luôn dùng trình gỡ của ứng dụng",
        ["Settings_AlwaysUseAppUninstallerDescription"] = "Không bao giờ tự xoá thủ công. Nếu ứng dụng không có trình gỡ, hoặc trình gỡ lỗi, thì không xoá gì cả và báo rõ lý do — an toàn hơn, nhưng một số ứng dụng sẽ vẫn còn trên máy.",
        ["Settings_PermanentlyDelete"] = "Xoá vĩnh viễn file rác (bỏ qua Thùng rác)",
        ["Settings_PermanentlyDeleteDescription"] = "Mặc định tắt: file rác được đưa vào Thùng rác để bạn khôi phục nếu app quét nhầm. Registry không có Thùng rác nên dù bật hay tắt, app đều tự xuất file .reg sao lưu trước khi xoá.",

        ["Settings_ContextMenu"] = "Thêm \"Gỡ bằng Remove Installer App\" vào menu chuột phải",
        ["Settings_ContextMenuDescription"] = "Thêm lựa chọn này vào menu chuột phải của file .exe và shortcut (Start Menu, Desktop), để gỡ cài đặt ngay từ File Explorer. Chỉ áp dụng cho tài khoản Windows hiện tại.",
        ["ContextMenu_UninstallVerb"] = "Gỡ bằng Remove Installer App",
        ["ContextMenu_QuickUninstallVerb"] = "Gỡ nhanh...",

        ["QuickUninstall_ConfirmUninstall"] = "Gỡ cài đặt \"{0}\"?",
        ["QuickUninstall_ConfirmBackup"] = "Tạo điểm khôi phục hệ thống trước khi gỡ \"{0}\"?",
        ["QuickUninstall_AppNotFound"] = "\"{0}\" không khớp với ứng dụng nào đã cài, nên không có gì để gỡ.",
        ["QuickUninstall_ResultTitle"] = "Remove Installer App",

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
    public static string NavForceDelete => Get("NavForceDelete");
    public static string NavDiskCleanup => Get("NavDiskCleanup");
    public static string NavSettings => Get("NavSettings");

    public static string Welcome_Title => Get("Welcome_Title");
    public static string Welcome_Intro => Get("Welcome_Intro");
    public static string Welcome_FeatureList => Get("Welcome_FeatureList");
    public static string Welcome_UpdatedTitle(string version) => string.Format(Get("Welcome_UpdatedTitle"), version);
    public static string Welcome_UpdatedGenericMessage => Get("Welcome_UpdatedGenericMessage");

    public static string License_SectionTitle => Get("License_SectionTitle");
    public static string License_SectionDescription => Get("License_SectionDescription");
    public static string License_StatusFree => Get("License_StatusFree");
    public static string License_StatusTrial(int daysLeft) => string.Format(Get("License_StatusTrial"), daysLeft);
    public static string License_StatusTrialExpired => Get("License_StatusTrialExpired");
    public static string License_StartTrialButton => Get("License_StartTrialButton");
    public static string License_EndTrialButton => Get("License_EndTrialButton");
    public static string License_UpgradeTitle => Get("License_UpgradeTitle");
    public static string License_UpgradeMessage(string featureName) => string.Format(Get("License_UpgradeMessage"), featureName);

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
    public static string AppList_ResultNoUninstaller(string appName) => string.Format(Get("AppList_ResultNoUninstaller"), appName);
    public static string AppList_ResidueFound(int count) => string.Format(Get("AppList_ResidueFound"), count);
    public static string AppList_GoToResidue => Get("AppList_GoToResidue");
    public static string AppList_ContextMenuNoMatchTitle => Get("AppList_ContextMenuNoMatchTitle");
    public static string AppList_ContextMenuNoMatchMessage(string fileName) => string.Format(Get("AppList_ContextMenuNoMatchMessage"), fileName);

    public static string Backup_ConfirmTitle => Get("Backup_ConfirmTitle");
    public static string Backup_ConfirmMessage(string appName) => string.Format(Get("Backup_ConfirmMessage"), appName);
    public static string Backup_RestorePointDescription(string appName) => string.Format(Get("Backup_RestorePointDescription"), appName);
    public static string Backup_Creating => Get("Backup_Creating");
    public static string Backup_Failed(string reason) => string.Format(Get("Backup_Failed"), reason);
    public static string Backup_ContinueAnyway => Get("Backup_ContinueAnyway");

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

    public static string ForceDelete_Title => Get("ForceDelete_Title");
    public static string ForceDelete_BrowseFile => Get("ForceDelete_BrowseFile");
    public static string ForceDelete_BrowseFolder => Get("ForceDelete_BrowseFolder");
    public static string ForceDelete_SecureDelete => Get("ForceDelete_SecureDelete");
    public static string ForceDelete_SecureDeleteDescription => Get("ForceDelete_SecureDeleteDescription");
    public static string ForceDelete_DeleteButton => Get("ForceDelete_DeleteButton");
    public static string ForceDelete_Empty => Get("ForceDelete_Empty");
    public static string ForceDelete_ColumnType => Get("ForceDelete_ColumnType");
    public static string ForceDelete_ColumnPath => Get("ForceDelete_ColumnPath");
    public static string ForceDelete_ColumnSize => Get("ForceDelete_ColumnSize");
    public static string ForceDelete_RemoveFromQueue => Get("ForceDelete_RemoveFromQueue");
    public static string ForceDelete_ConfirmTitle => Get("ForceDelete_ConfirmTitle");
    public static string ForceDelete_ConfirmMessage(int count) => string.Format(Get("ForceDelete_ConfirmMessage"), count);
    public static string ForceDelete_ConfirmMessageSecure(int count) => string.Format(Get("ForceDelete_ConfirmMessageSecure"), count);
    public static string ForceDelete_ResultTitle => Get("ForceDelete_ResultTitle");
    public static string ForceDelete_ResultSummary(int deleted, int scheduled) => string.Format(Get("ForceDelete_ResultSummary"), deleted, scheduled);
    public static string ForceDelete_RebootRequiredNotice(int count) => string.Format(Get("ForceDelete_RebootRequiredNotice"), count);
    public static string ForceDelete_ErrorsTitle => Get("ForceDelete_ErrorsTitle");
    public static string ForceDelete_AddErrorTitle => Get("ForceDelete_AddErrorTitle");
    public static string ForceDelete_PathNotFound(string path) => string.Format(Get("ForceDelete_PathNotFound"), path);
    public static string ForceDelete_UnsafePath(string path) => string.Format(Get("ForceDelete_UnsafePath"), path);

    public static string DiskCleanup_Title => Get("DiskCleanup_Title");
    public static string DiskCleanup_Scan => Get("DiskCleanup_Scan");
    public static string DiskCleanup_Scanning => Get("DiskCleanup_Scanning");
    public static string DiskCleanup_SelectAll => Get("DiskCleanup_SelectAll");
    public static string DiskCleanup_ClearSelection => Get("DiskCleanup_ClearSelection");
    public static string DiskCleanup_Clean => Get("DiskCleanup_Clean");
    public static string DiskCleanup_Empty => Get("DiskCleanup_Empty");
    public static string DiskCleanup_ConfirmTitle => Get("DiskCleanup_ConfirmTitle");
    public static string DiskCleanup_ConfirmMessage(string sizeText, int categoryCount) => string.Format(Get("DiskCleanup_ConfirmMessage"), sizeText, categoryCount);
    public static string DiskCleanup_ResultTitle => Get("DiskCleanup_ResultTitle");
    public static string DiskCleanup_ResultSummary(string sizeText) => string.Format(Get("DiskCleanup_ResultSummary"), sizeText);
    public static string DiskCleanup_ResultSkipped(int count) => string.Format(Get("DiskCleanup_ResultSkipped"), count);
    public static string DiskCleanup_ErrorsTitle => Get("DiskCleanup_ErrorsTitle");

    public static string DiskCleanupCategoryName(DiskCleanupCategoryKind kind) => Get($"DiskCleanupCategory_{kind}_Name");
    public static string DiskCleanupCategoryDescription(DiskCleanupCategoryKind kind) => Get($"DiskCleanupCategory_{kind}_Description");

    public static string Settings_Title => Get("Settings_Title");
    public static string Settings_Language => Get("Settings_Language");
    public static string Settings_LanguageEnglish => Get("Settings_LanguageEnglish");
    public static string Settings_LanguageVietnamese => Get("Settings_LanguageVietnamese");
    public static string Settings_SilentUninstall => Get("Settings_SilentUninstall");
    public static string Settings_SilentUninstallDescription => Get("Settings_SilentUninstallDescription");
    public static string Settings_AlwaysUseAppUninstaller => Get("Settings_AlwaysUseAppUninstaller");
    public static string Settings_AlwaysUseAppUninstallerDescription => Get("Settings_AlwaysUseAppUninstallerDescription");
    public static string Settings_PermanentlyDelete => Get("Settings_PermanentlyDelete");
    public static string Settings_PermanentlyDeleteDescription => Get("Settings_PermanentlyDeleteDescription");

    public static string Settings_ContextMenu => Get("Settings_ContextMenu");
    public static string Settings_ContextMenuDescription => Get("Settings_ContextMenuDescription");
    public static string ContextMenu_UninstallVerb => Get("ContextMenu_UninstallVerb");
    public static string ContextMenu_QuickUninstallVerb => Get("ContextMenu_QuickUninstallVerb");

    public static string QuickUninstall_ConfirmUninstall(string appName) => string.Format(Get("QuickUninstall_ConfirmUninstall"), appName);
    public static string QuickUninstall_ConfirmBackup(string appName) => string.Format(Get("QuickUninstall_ConfirmBackup"), appName);
    public static string QuickUninstall_AppNotFound(string fileName) => string.Format(Get("QuickUninstall_AppNotFound"), fileName);
    public static string QuickUninstall_ResultTitle => Get("QuickUninstall_ResultTitle");

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

    public static string ScanStep_InstallFolders => Get("ScanStep_InstallFolders");
    public static string ScanStep_TempFolders => Get("ScanStep_TempFolders");
    public static string ScanStep_Shortcuts => Get("ScanStep_Shortcuts");
    public static string ScanStep_StartupFolders => Get("ScanStep_StartupFolders");
    public static string ScanStep_SoftwareKeys => Get("ScanStep_SoftwareKeys");
    public static string ScanStep_ClassesRoot => Get("ScanStep_ClassesRoot");
    public static string ScanStep_AppPaths => Get("ScanStep_AppPaths");
    public static string ScanStep_RunKeys => Get("ScanStep_RunKeys");
    public static string ScanStep_Services => Get("ScanStep_Services");
    public static string ScanStep_ScheduledTasks => Get("ScanStep_ScheduledTasks");
    public static string ScanStep_UninstallEntry => Get("ScanStep_UninstallEntry");
    public static string ScanStep_Done => Get("ScanStep_Done");
    public static string ScanProgress_Status(string step, int itemsFound) => string.Format(Get("ScanProgress_Status"), step, itemsFound);

    public static string PostUninstall_Title => Get("PostUninstall_Title");
    public static string PostUninstall_NothingFound => Get("PostUninstall_NothingFound");
    public static string PostUninstall_ScanFailed => Get("PostUninstall_ScanFailed");
    public static string PostUninstall_Deleting => Get("PostUninstall_Deleting");
    public static string PostUninstall_DeletedToRecycleBin(int count) => string.Format(Get("PostUninstall_DeletedToRecycleBin"), count);
    public static string PostUninstall_DeletedPermanently(int count) => string.Format(Get("PostUninstall_DeletedPermanently"), count);

    public static string KindLabel(ResidueKind kind) => Get(kind switch
    {
        ResidueKind.Folder => "Kind_Folder",
        ResidueKind.File => "Kind_File",
        ResidueKind.Shortcut => "Kind_Shortcut",
        ResidueKind.RegistryKey => "Kind_RegistryKey",
        ResidueKind.OrphanedUninstallEntry => "Kind_OrphanedUninstallEntry",
        ResidueKind.OrphanedRunEntry => "Kind_OrphanedRunEntry",
        ResidueKind.ServiceEntry => "Kind_ServiceEntry",
        ResidueKind.ScheduledTask => "Kind_ScheduledTask",
        _ => "Kind_File",
    });
}
