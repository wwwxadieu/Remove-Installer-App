using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

/// <summary>
/// Checks the project's GitHub "latest release" for a newer version than the one currently
/// running. Unpackaged Win32 apps have unrestricted outbound network access, so no manifest
/// capability is required for this (unlike an MSIX-packaged app).
/// </summary>
public sealed class UpdateService : IUpdateService, IDisposable
{
    private const string RepoOwner = "wwwxadieu";
    private const string RepoName = "Remove-Installer-App";

    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RemoveInstallerApp-UpdateChecker");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return UpdateCheckResult.Failed("No release has been published yet.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failed($"HTTP {(int)response.StatusCode}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty;
            var releaseUrl = root.TryGetProperty("html_url", out var htmlUrlEl) ? htmlUrlEl.GetString() : null;
            var downloadUrl = FindWindowsAssetUrl(root);

            var latestVersion = ParseVersion(tagName);
            var isUpdateAvailable = latestVersion is not null && latestVersion > AppVersionInfo.CurrentVersion;

            return new UpdateCheckResult
            {
                Success = true,
                IsUpdateAvailable = isUpdateAvailable,
                LatestVersionText = tagName,
                ReleaseUrl = releaseUrl,
                DownloadUrl = downloadUrl,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    public async Task<ReleaseNotesResult> GetReleaseNotesAsync(string version, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = version.StartsWith('v', StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/tags/{tag}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ReleaseNotesResult.Failed($"No published release found for {tag}.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ReleaseNotesResult.Failed($"HTTP {(int)response.StatusCode}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            return new ReleaseNotesResult
            {
                Success = true,
                Body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null,
                HtmlUrl = root.TryGetProperty("html_url", out var htmlUrlEl) ? htmlUrlEl.GetString() : null,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ReleaseNotesResult.Failed(ex.Message);
        }
    }

    private static string? FindWindowsAssetUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("win-x64", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase))
            {
                return asset.TryGetProperty("browser_download_url", out var urlEl) ? urlEl.GetString() : null;
            }
        }

        return null;
    }

    private static Version? ParseVersion(string tag)
    {
        var cleaned = tag.TrimStart('v', 'V');
        return Version.TryParse(cleaned, out var version) ? version : null;
    }

    public void Dispose() => _httpClient.Dispose();
}
