using System.Runtime.InteropServices;
using Microsoft.Win32;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Reads hardware/OS specs from the registry and a few static .NET APIs — deliberately no
/// P/Invoke and no WMI/System.Management dependency, matching the fully-managed-only approach
/// adopted after the GDI+ icon-extraction crash (see AppIconExtractor's history). Every
/// registry read is independently best-effort: a missing/unreadable value degrades that one
/// field to null rather than failing the whole page.
/// </summary>
public sealed class DeviceInfoService : IDeviceInfoService
{
    public DeviceSpecsInfo GetDeviceSpecs()
    {
        var (osDisplayName, osVersionText) = GetOsInfo();

        return new DeviceSpecsInfo
        {
            OsDisplayName = osDisplayName,
            OsVersionText = osVersionText,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            MachineName = Environment.MachineName,
            CpuName = GetRegistryValue(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString")?.Trim(),
            LogicalProcessorCount = Environment.ProcessorCount,
            TotalRamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            Manufacturer = GetRegistryValue(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer"),
            Model = GetRegistryValue(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName"),
        };
    }

    /// <summary>
    /// Windows 11 still reports "Windows 10 ..." in ProductName — a long-standing, never-fixed
    /// registry quirk — so the actual OS generation has to be inferred from the build number
    /// (11's first build was 22000) and substituted in for display.
    /// </summary>
    private static (string DisplayName, string VersionText) GetOsInfo()
    {
        const string versionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        var productName = GetRegistryValue(Registry.LocalMachine, versionKey, "ProductName") ?? "Windows";
        var displayVersion = GetRegistryValue(Registry.LocalMachine, versionKey, "DisplayVersion");
        var buildNumberText = GetRegistryValue(Registry.LocalMachine, versionKey, "CurrentBuildNumber");
        var ubr = GetRegistryDwordValue(Registry.LocalMachine, versionKey, "UBR");

        if (int.TryParse(buildNumberText, out var buildNumber) && buildNumber >= 22000 && productName.Contains("Windows 10"))
        {
            productName = productName.Replace("Windows 10", "Windows 11");
        }

        var buildText = buildNumberText is null
            ? null
            : ubr is { } ubrValue ? $"{buildNumberText}.{ubrValue}" : buildNumberText;

        var versionText = (displayVersion, buildText) switch
        {
            (not null, not null) => $"{displayVersion} (Build {buildText})",
            (not null, null) => displayVersion,
            (null, not null) => $"Build {buildText}",
            (null, null) => string.Empty,
        };

        return (productName, versionText);
    }

    private static string? GetRegistryValue(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }

    private static int? GetRegistryDwordValue(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) is int value ? value : null;
        }
        catch
        {
            return null;
        }
    }
}
