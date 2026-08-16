namespace UnInstall.Models;

/// <summary>The fixed set of whole-disk cleanup categories, mirroring Windows' own Disk Cleanup tool.</summary>
public enum DiskCleanupCategoryKind
{
    /// <summary>Current user's %TEMP% folder plus C:\Windows\Temp.</summary>
    TemporaryFiles,

    /// <summary>The Recycle Bin, emptied via the OS's own SHEmptyRecycleBinW.</summary>
    RecycleBin,

    /// <summary>Explorer's thumbnail/icon cache database files.</summary>
    ThumbnailCache,

    /// <summary>Downloaded Windows Update packages no longer needed once installed.</summary>
    WindowsUpdateCleanup,

    /// <summary>Windows Update Delivery Optimization's local peer-cache files.</summary>
    DeliveryOptimizationFiles,

    /// <summary>Windows Error Reporting queued/archived crash report files.</summary>
    WindowsErrorReports,

    /// <summary>Memory dump files from past system crashes (minidumps and MEMORY.DMP).</summary>
    MemoryDumpFiles,
}
