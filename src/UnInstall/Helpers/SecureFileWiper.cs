using System.Security.Cryptography;

namespace UnInstall.Helpers;

/// <summary>
/// Overwrites file contents with random data before deletion, for the Force Delete tool's
/// "delete unrecoverably" option. NOTE: on SSDs, wear-leveling/TRIM mean the physical cells
/// actually holding the old data may not be the ones overwritten, so this does not guarantee
/// the original data is unrecoverable — the UI must disclose that caveat, not just this comment.
/// </summary>
public static class SecureFileWiper
{
    private const int MaxChunkSize = 1024 * 1024;

    public static void OverwriteWithRandomData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var length = new FileInfo(filePath).Length;
        if (length == 0)
        {
            return;
        }

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
        var buffer = new byte[Math.Min(MaxChunkSize, length)];
        long written = 0;
        while (written < length)
        {
            var chunkSize = (int)Math.Min(buffer.Length, length - written);
            RandomNumberGenerator.Fill(buffer.AsSpan(0, chunkSize));
            stream.Write(buffer, 0, chunkSize);
            written += chunkSize;
        }
        stream.Flush();
    }

    public static void OverwriteDirectoryContentsWithRandomData(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);
        }
        catch
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                OverwriteWithRandomData(file);
            }
            catch
            {
                // Best-effort: the subsequent ForceDelete pass still tries to remove the file
                // even if it couldn't be wiped (e.g. locked by another process).
            }
        }
    }
}
