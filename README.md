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
- Tuỳ chọn thêm mục **"Gỡ bằng Remove Installer App"** vào menu chuột phải của Windows
  Explorer, trên bất kỳ file `.exe` hay shortcut (Start Menu, Desktop) nào — bật/tắt trong
  Settings. Bấm vào đó sẽ mở app và tự động khớp file đã chọn với ứng dụng tương ứng trong
  danh sách đã cài, rồi vào thẳng luồng xác nhận gỡ cài đặt.
  Optional **"Uninstall with Remove Installer App"** entry on the right-click menu of any
  `.exe` file or shortcut (Start menu, Desktop) — toggle in Settings. Clicking it opens the
  app, matches the file to the corresponding installed app, and jumps straight into the
  confirm-and-uninstall flow.
- Trước mỗi lần gỡ cài đặt, app luôn hỏi có muốn tạo **điểm khôi phục hệ thống (System
  Restore)** trước không — dùng đúng cơ chế backup gốc của Windows, không phải định dạng
  riêng của app. Nếu tạo lỗi (System Restore đang tắt), app báo rõ lý do và hỏi tiếp có
  muốn tiếp tục gỡ cài đặt hay không.
  Before every uninstall, the app always asks whether to create a **System Restore**
  point first — using Windows' own backup mechanism, not a custom format. If creation
  fails (System Restore disabled), it reports why and asks whether to continue anyway.
- Trang **"Xoá ép buộc" (Force Delete)** độc lập: chọn (duyệt hoặc kéo-thả) bất kỳ file/thư
  mục nào để ép buộc xoá (kể cả đang bị khoá bởi tiến trình khác, hoặc read-only) và/hoặc
  xoá không thể khôi phục (ghi đè dữ liệu ngẫu nhiên trước khi xoá).
  Standalone **"Force Delete"** page: browse or drag-and-drop any file/folder to force-delete
  it (even if locked by another process or read-only) and/or delete it unrecoverably
  (overwrites the contents with random data first).
- Tuỳ chọn thêm mục **"Gỡ nhanh..."** vào menu chuột phải, song song với mục gỡ cài đặt
  hiện có — chạy toàn bộ luồng gỡ cài đặt (kèm hỏi backup) chỉ bằng hộp thoại hệ thống nhỏ,
  không mở giao diện chính của app.
  Optional **"Quick uninstall..."** entry on the right-click menu, alongside the existing
  uninstall entry — runs the entire uninstall flow (including the backup prompt) using only
  small native system dialogs, without ever opening the app's main window.
- Trang **"Dọn ổ đĩa" (Disk Cleanup)** — hoạt động giống công cụ Disk Cleanup có sẵn của
  Windows: quét dung lượng từng nhóm (tệp tin tạm, Thùng rác, bộ nhớ đệm hình thu nhỏ, gói
  Windows Update đã cài xong, tệp Delivery Optimization, báo cáo lỗi Windows, memory dump),
  cho chọn nhóm muốn dọn rồi xoá — Thùng rác được dọn qua đúng API `SHEmptyRecycleBinW` của
  Windows thay vì tự xoá file.
  A **"Disk Cleanup"** page that works like Windows' own Disk Cleanup tool: scans the size
  of each category (temp files, Recycle Bin, thumbnail cache, already-installed Windows
  Update packages, Delivery Optimization files, Windows Error Reporting files, memory
  dumps), lets you pick which to clean, then deletes them — the Recycle Bin is emptied via
  Windows' own `SHEmptyRecycleBinW` API rather than deleting files manually.

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
  -c Release -r win-x64 -p:Platform=x64 --self-contained true `
  -p:WindowsAppSDKSelfContained=true
```

> `-p:Platform=x64` là bắt buộc — thiếu tham số này MSBuild sẽ mặc định dùng `AnyCPU`, và
> Windows App SDK từ chối build self-contained với `AnyCPU` (`error: The platform 'AnyCPU'
> is not supported for Self Contained mode.`). Đổi thành `arm64` nếu build cho `win-arm64`.
>
> `-p:Platform=x64` is required — without it MSBuild defaults to `AnyCPU`, which the
> Windows App SDK rejects for self-contained builds (`error: The platform 'AnyCPU' is not
> supported for Self Contained mode.`). Use `arm64` when publishing for `win-arm64`.

File thực thi (`RemoveInstallerApp.exe`) và toàn bộ dependency sẽ nằm trong thư mục
`publish` — có thể copy sang máy khác chạy trực tiếp mà không cần cài .NET runtime.

The resulting `RemoveInstallerApp.exe` (in the `publish` folder) is self-contained and
can be copied to another machine without installing the .NET runtime separately.

## Kiến trúc / Architecture

```
src/RemoveInstallerApp/
├── Models/         InstalledAppInfo, ResidueItem, UninstallResult, AppSettings,
│                     BackupResult, ForceDeleteOutcome, BulkForceDeleteResult,
│                     ForceDeleteQueueItem
├── Services/        InstalledAppsService (registry enumeration)
│                     UninstallService (run uninstaller / force remove)
│                     ResidueScanService (leftover file & registry scan)
│                     UninstallOrchestrator (shared uninstall pipeline — windowed + headless)
│                     SystemRestoreBackupService (System Restore point before uninstall)
│                     ForceDeleteService (force-delete / secure-delete queue)
│                     DiskCleanupService (Disk Cleanup-style temp/cache sweep)
│                     UpdateService (GitHub Releases version check)
│                     ShellIntegrationService (both right-click verbs)
│                     LocalizationService, SettingsService
├── ViewModels/      MVVM view models (CommunityToolkit.Mvvm)
├── Views/            AppListPage, ResidueScanPage, ForceDeletePage, DiskCleanupPage,
│                     SettingsPage (WinUI 3 XAML)
├── Strings/          AppStrings.cs — bảng chuỗi song ngữ EN/VI
└── Helpers/          PathSafety — chặn xoá nhầm thư mục hệ thống
                       ForceDelete, SecureFileWiper — pipeline xoá ép buộc / không thể khôi phục
                       RecycleBinInterop — SHQueryRecycleBinW/SHEmptyRecycleBinW cho Dọn ổ đĩa
                       InstalledAppMatcher, UninstallResultFormatter, NativeMessageBox —
                       dùng chung giữa luồng có giao diện và luồng "Gỡ nhanh" headless
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

## Build bản beta tự động / Automated beta releases

Workflow `.github/workflows/release-beta.yml` build ứng dụng trên `windows-latest`
(publish self-contained cho `win-x64`, kèm `win-arm64` dạng best-effort), đóng gói thành
file `.zip`, rồi tự động tạo một **GitHub Release đánh dấu prerelease** kèm các file đó.
Có hai cách kích hoạt:

> **Vì sao repo có `global.json`**: file này ghim .NET SDK về nhánh 8.x. Nếu không có nó,
> MSBuild sẽ tự chọn SDK mới nhất có sẵn trên máy/runner (ví dụ 10.x), mà Windows App SDK
> 1.5 không đọc được layout `Microsoft\VisualStudio\v18.0` của SDK đó — build sẽ chết với
> lỗi `MSB4062 ... Microsoft.Build.Packaging.Pri.Tasks.dll`. Đừng xoá `global.json` trừ khi
> đồng thời nâng Windows App SDK lên phiên bản hỗ trợ SDK mới.
>
> Nhánh `win-arm64` được đánh dấu `continue-on-error` (best-effort): nếu nó lỗi thì release
> vẫn được tạo với file x64.

- **Tự động**: push một tag khớp mẫu `v*-beta*`, ví dụ:
  ```bash
  git tag v1.1.0-beta1
  git push origin v1.1.0-beta1
  ```
- **Thủ công**: vào tab **Actions** trên GitHub → chọn workflow **Build beta release** →
  **Run workflow**. Có thể nhập version cụ thể (ví dụ `1.1.0-beta2`), hoặc để trống để
  workflow tự sinh version dạng `0.0.0-beta.<số lần chạy>`.

Vì release được tạo với `prerelease: true`, endpoint `releases/latest` của GitHub API sẽ
**bỏ qua** các bản beta này — nghĩa là `UpdateService` (mục kiểm tra cập nhật trong app) sẽ
không tự động đề nghị người dùng lên bản beta, chỉ đề nghị lên bản release chính thức
(non-prerelease). Đây là hành vi có chủ đích, để tránh đẩy bản thử nghiệm cho người dùng
thường.

The `.github/workflows/release-beta.yml` workflow builds the app on `windows-latest`
(self-contained publish for `win-x64`, plus a best-effort `win-arm64`), zips the output,
and automatically creates a **prerelease GitHub Release** with those files attached. Two
ways to trigger it:

> **Why this repo has a `global.json`**: it pins the .NET SDK to the 8.x band. Without it,
> MSBuild picks the newest SDK installed on the machine/runner (e.g. 10.x), whose
> `Microsoft\VisualStudio\v18.0` layout the Windows App SDK 1.5 PRI targets cannot resolve
> — the build then dies with `MSB4062 ... Microsoft.Build.Packaging.Pri.Tasks.dll`. Don't
> delete `global.json` unless you also move to a Windows App SDK version that supports the
> newer SDK.
>
> The `win-arm64` leg is marked `continue-on-error` (best-effort): if it breaks, the
> release still ships with the x64 asset.

- **Automatically**: push a tag matching `v*-beta*`, e.g. `git tag v1.1.0-beta1 && git push
  origin v1.1.0-beta1`.
- **Manually**: GitHub → **Actions** tab → **Build beta release** → **Run workflow**.
  Optionally specify a version (e.g. `1.1.0-beta2`), or leave it blank to auto-generate
  `0.0.0-beta.<run number>`.

Because the release is created with `prerelease: true`, GitHub's `releases/latest` API
endpoint **excludes** it — so the in-app `UpdateService` will never prompt regular users to
move to a beta build, only to an official (non-prerelease) release. That's intentional.

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
- Mục menu chuột phải (nếu bật) được ghi vào
  `HKEY_CURRENT_USER\Software\Classes\exefile\shell\RemoveInstallerAppUninstall` và
  `...\lnkfile\shell\RemoveInstallerAppUninstall` — chỉ ảnh hưởng tài khoản Windows hiện
  tại, không đụng đến HKLM. Tắt toggle trong Settings (hoặc gỡ hẳn app) sẽ xoá các khoá này;
  nếu bạn xoá thủ công thư mục cài đặt mà quên tắt toggle trước, mục menu sẽ trỏ đến file
  không còn tồn tại — xoá tay hai khoá trên trong `regedit` để dọn sạch.
  The right-click menu entry (when enabled) is written to
  `HKEY_CURRENT_USER\Software\Classes\exefile\shell\RemoveInstallerAppUninstall` and
  `...\lnkfile\shell\RemoveInstallerAppUninstall` — current Windows account only, never
  HKLM. Turning the Settings toggle off (or uninstalling the app) removes these keys; if
  you manually delete the install folder without turning the toggle off first, the menu
  entry will point at a missing file — delete those two keys by hand in `regedit` to clean up.
- Cùng toggle menu chuột phải ở trên cũng ghi thêm mục **"Gỡ nhanh..."** vào
  `...\shell\RemoveInstallerAppQuickUninstall` (cả `exefile` và `lnkfile`) — chạy
  `RemoveInstallerApp.exe --quick-uninstall "<path>"`, không mở cửa sổ chính, chỉ dùng
  `MessageBox` gốc của Windows. Đây là một verb registry riêng biệt, không phải chế độ khác
  của verb gỡ cài đặt hiện có.
  The same right-click toggle above also writes a **"Quick uninstall..."** entry at
  `...\shell\RemoveInstallerAppQuickUninstall` (both `exefile` and `lnkfile`) — it runs
  `RemoveInstallerApp.exe --quick-uninstall "<path>"`, never opens the main window, and uses
  only native Windows `MessageBox` dialogs. It's a separate registry verb, not a mode of the
  existing uninstall verb.
- **Sao lưu trước khi gỡ** tạo một **System Restore point** thật của Windows (qua
  `SRSetRestorePointW`), xem được bằng `rstrui.exe` hoặc PowerShell
  `Get-ComputerRestorePoint`. Nếu System Restore đang tắt trên máy/ổ đĩa, việc tạo sẽ thất
  bại — app báo lỗi và vẫn cho phép tiếp tục gỡ cài đặt.
  **The pre-uninstall backup** creates a real Windows **System Restore point** (via
  `SRSetRestorePointW`), visible through `rstrui.exe` or PowerShell's
  `Get-ComputerRestorePoint`. If System Restore is disabled on the machine/drive, creation
  fails — the app reports the error and still lets the uninstall continue.
- **Force Delete** dùng `MoveFileEx`/`MOVEFILE_DELAY_UNTIL_REBOOT` khi một file vẫn bị khoá
  sau khi đã thử xoá bình thường và reset quyền/ACL — mục đó sẽ nằm trong
  `PendingFileRenameOperations` (regedit,
  `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager`) và bị xoá thật ở
  lần khởi động lại máy tiếp theo, không có nút tự khởi động lại. `PathSafety` vẫn áp dụng ở
  chế độ ép buộc — không thể xoá bất cứ gì bên trong thư mục Windows hay chính các thư mục
  hệ thống được bảo vệ, kể cả khi bật force.
  **Force Delete** falls back to `MoveFileEx`/`MOVEFILE_DELAY_UNTIL_REBOOT` when a file is
  still locked after a normal delete attempt and an ownership/ACL reset — that entry lands
  in `PendingFileRenameOperations` (regedit,
  `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager`) and is actually
  removed on the next restart; there's no auto-restart button. `PathSafety` still applies in
  force mode — nothing under the Windows folder or any protected system folder can be
  deleted, even with force enabled.
- **Xoá không thể khôi phục** chỉ ghi đè nội dung file bằng dữ liệu ngẫu nhiên trước khi
  xoá. Trên ổ SSD, cơ chế wear-leveling/TRIM có thể khiến dữ liệu gốc vẫn còn ở vị trí vật
  lý khác — tính năng này **không đảm bảo** không thể khôi phục tuyệt đối trên SSD.
  **Delete unrecoverably** only overwrites file contents with random data before deleting.
  On SSDs, wear-leveling/TRIM can leave the original data intact in a different physical
  location — this feature does **not** guarantee the data is truly unrecoverable on an SSD.
- **Dọn ổ đĩa** chỉ động vào những thư mục hệ thống cố định đã biết trước (Temp, thùng rác,
  cache thumbnail, `SoftwareDistribution\Download`, Delivery Optimization, WER, memory dump)
  — không nhận đường dẫn tuỳ ý từ người dùng như Force Delete. File đang bị khoá bởi tiến
  trình khác (thường gặp với file tạm) sẽ được bỏ qua âm thầm và tính vào số lượng "đã bỏ
  qua" trong kết quả, không báo lỗi riêng cho từng file.
  **Disk Cleanup** only ever touches a fixed set of known system folders (Temp, Recycle Bin,
  thumbnail cache, `SoftwareDistribution\Download`, Delivery Optimization, WER, memory
  dumps) — it doesn't accept arbitrary user-supplied paths the way Force Delete does. Files
  locked by another process (common for temp files) are silently skipped and counted in the
  result's "skipped" total rather than reported as individual errors.

## License

Chưa xác định / Not specified.
