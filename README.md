# Remove Installer App

Ứng dụng Windows (WinUI 3) giúp gỡ cài đặt ứng dụng và dọn dẹp file/registry còn sót lại.
A Windows desktop app (WinUI 3) for uninstalling applications and cleaning up their
leftover files and registry entries.

## Tính năng / Features

- Liệt kê toàn bộ ứng dụng đã cài trên máy (đọc từ registry `Uninstall`, cả HKLM/HKCU,
  32-bit và 64-bit).
  Lists every installed application (read from the `Uninstall` registry keys — HKLM/HKCU,
  32-bit and 64-bit views).
- Gỡ cài đặt bằng cách bấm nút trên từng ứng dụng: chạy trình uninstaller riêng của ứng
  dụng đó; nếu ứng dụng không có uninstaller (hoặc uninstaller lỗi), ứng dụng sẽ tự gỡ
  thủ công (xoá thư mục cài đặt, shortcut, khoá registry).
  Click-to-uninstall: runs the app's own uninstaller; if none exists (or it fails), falls
  back to manual removal (install folder, shortcuts, registry key).
- Sau khi gỡ, tự động quét file rác (AppData, ProgramData, Program Files, Start Menu,
  Desktop) và registry rác (SOFTWARE keys, Run/RunOnce, mục Uninstall còn sót) liên quan
  đến ứng dụng vừa gỡ, cho phép người dùng xem trước và chọn xoá.
  After uninstalling, scans for leftover files (AppData, ProgramData, Program Files, Start
  Menu, Desktop) and registry junk (SOFTWARE keys, Run/RunOnce, orphaned Uninstall entries)
  and lets you review/select before deleting.
- Trang "Dọn dẹp file rác" riêng để quét các mục mồ côi (orphaned) trên toàn máy bất cứ
  lúc nào, không cần vừa gỡ ứng dụng.
  A standalone "Leftover Cleaner" page to sweep the whole machine for orphaned entries at
  any time.
- Giao diện WinUI 3 (Fluent Design, hỗ trợ dark/light theo hệ thống).
  WinUI 3 UI (Fluent Design, follows system light/dark theme).
- Hỗ trợ song ngữ Việt/Anh, chuyển đổi ngay trong Settings.
  Bilingual VN/EN, switchable from Settings.
- Tự động (hoặc theo yêu cầu ở Settings) kiểm tra phiên bản mới trên GitHub Releases của
  dự án; nếu có bản mới sẽ hiện thông báo trong app kèm nút mở link tải.
  Checks GitHub Releases for a newer version — automatically on launch (toggle in
  Settings) or on demand — and shows an in-app banner with a download link when one is
  found.

## Yêu cầu / Requirements

- Windows 10 version 1809 (17763) trở lên, hoặc Windows 11.
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.9+ với workload **.NET
  desktop development** và **Windows App SDK C# Templates** (cài qua Visual Studio
  Installer, mục "Individual components" → tìm "Windows App SDK").
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

> Dự án này dùng WinUI 3 (Windows App SDK) — chỉ build và chạy được trên Windows.
> This project targets WinUI 3 (Windows App SDK) — it can only be built and run on Windows.

## Build & chạy / Build & run

```powershell
git clone <repo-url>
cd Remove-Installer-App
dotnet restore
dotnet build -c Debug -p:Platform=x64
```

Chạy trực tiếp bằng Visual Studio: mở `RemoveInstallerApp.sln`, chọn cấu hình
`Debug | x64`, nhấn F5. Ứng dụng sẽ yêu cầu quyền Administrator khi khởi động (cần thiết
để gỡ ứng dụng và xoá khoá registry HKLM).

Open `RemoveInstallerApp.sln` in Visual Studio, select `Debug | x64`, and press F5. The
app requests Administrator privileges on launch (required to uninstall apps and delete
HKLM registry keys).

### Đóng gói bản phát hành / Publishing a release build

```powershell
dotnet publish src/RemoveInstallerApp/RemoveInstallerApp.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:WindowsAppSDKSelfContained=true
```

File thực thi (`RemoveInstallerApp.exe`) và toàn bộ dependency sẽ nằm trong thư mục
`publish` — có thể copy sang máy khác chạy trực tiếp mà không cần cài .NET runtime.

The resulting `RemoveInstallerApp.exe` (in the `publish` folder) is self-contained and
can be copied to another machine without installing the .NET runtime separately.

## Kiến trúc / Architecture

```
src/RemoveInstallerApp/
├── Models/         InstalledAppInfo, ResidueItem, UninstallResult, AppSettings
├── Services/        InstalledAppsService (registry enumeration)
│                     UninstallService (run uninstaller / force remove)
│                     ResidueScanService (leftover file & registry scan)
│                     UpdateService (GitHub Releases version check)
│                     LocalizationService, SettingsService
├── ViewModels/      MVVM view models (CommunityToolkit.Mvvm)
├── Views/            AppListPage, ResidueScanPage, SettingsPage (WinUI 3 XAML)
├── Strings/          AppStrings.cs — bảng chuỗi song ngữ EN/VI
└── Helpers/          PathSafety — chặn xoá nhầm thư mục hệ thống
```

MVVM đơn giản với dependency injection (`Microsoft.Extensions.DependencyInjection`), đăng
ký trong `App.xaml.cs`. Không dùng MSIX — ứng dụng chạy dạng unpackaged
(`WindowsPackageType=None`) để build/chạy không cần cấu hình identity phức tạp.

Lightweight MVVM with DI (`Microsoft.Extensions.DependencyInjection`), wired up in
`App.xaml.cs`. The app is unpackaged (`WindowsPackageType=None`, no MSIX) so it builds and
runs without needing package-identity setup.

### Cơ chế kiểm tra cập nhật / How update checking works

`UpdateService` gọi `https://api.github.com/repos/<owner>/<repo>/releases/latest` (repo
mặc định: `wwwxadieu/Remove-Installer-App`), so sánh `tag_name` (dạng `vX.Y.Z`) với
`<Version>` khai báo trong `RemoveInstallerApp.csproj`. Khi phát hành bản mới trên GitHub
Releases, hãy: (1) tăng `<Version>` trong csproj, (2) tạo tag/release tương ứng (ví dụ
`v1.1.0`) kèm file `.exe`/`.zip` đính kèm cho Windows. Nếu bạn fork repo, đổi
`RepoOwner`/`RepoName` trong `Services/UpdateService.cs` cho khớp.

`UpdateService` calls `https://api.github.com/repos/<owner>/<repo>/releases/latest`
(default repo: `wwwxadieu/Remove-Installer-App`) and compares the release's `tag_name`
(`vX.Y.Z`) against `<Version>` in `RemoveInstallerApp.csproj`. When cutting a release:
(1) bump `<Version>` in the csproj, (2) tag/publish a GitHub release (e.g. `v1.1.0`) with a
Windows `.exe`/`.zip` asset attached. If you fork the repo, update `RepoOwner`/`RepoName`
in `Services/UpdateService.cs` accordingly.

## Lưu ý quan trọng / Important notes

- **Luôn xem lại danh sách trước khi xoá.** Việc dò tìm file/registry rác dựa trên so
  khớp tên ứng dụng (heuristic), không đảm bảo tuyệt đối chính xác 100%. Hãy kiểm tra kỹ
  danh sách được đề xuất trước khi bấm "Xoá mục đã chọn".
  **Always review before deleting.** Leftover detection is name-based heuristic matching,
  not guaranteed 100% precise. Review the suggested list carefully before hitting "Delete
  selected".
- Ứng dụng yêu cầu quyền Administrator vì phần lớn thao tác gỡ cài đặt / xoá registry
  HKLM cần quyền này.
  The app requires Administrator rights because most uninstall/HKLM cleanup operations
  need them.
- `PathSafety` chặn xoá đệ quy các thư mục hệ thống quan trọng (Windows, Program Files,
  AppData gốc, ổ đĩa gốc, v.v.) để giảm rủi ro xoá nhầm.
  `PathSafety` refuses to recursively delete critical system folders (Windows, Program
  Files, root of AppData, drive roots, etc.) to reduce the risk of accidental deletion.

## License

Chưa xác định / Not specified.
