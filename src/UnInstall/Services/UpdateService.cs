using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnInstall.Helpers;
using UnInstall.Models;

namespace UnInstall.Services;

/// <summary>
/// Checks the project's GitHub Releases for a newer version than the one currently running.
/// Unpackaged Win32 apps have unrestricted outbound network access, so no manifest capability
/// is required for this (unlike an MSIX-packaged app).
///
/// Every release this project has ever published is a beta, marked <c>prerelease: true</c> (see
/// .github/workflows/release-beta.yml). GitHub's <c>/releases/latest</c> endpoint always excludes
/// prereleases, so calling it here would never find anything to compare against — it isn't a
/// "give regular users only stable builds" filter in this repo, it's "never detect an update at
/// all". This uses the full releases list instead and picks whichever entry parses to the
/// highest version, prerelease or not; once a real (non-beta) release exists, its bare
/// "1.0.0"-style tag naturally outranks any "1.0.0-betaN" of the same core version (see
/// <see cref="ParsedVersion"/>), so beta users still get pointed at the stable build then.
/// </summary>
public sealed class UpdateService : IUpdateService, IDisposable
{
    private const string RepoOwner = "wwwxadieu";
    private const string RepoName = "Remove-Installer-App";

    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UnInstall-UpdateChecker");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases?per_page=20";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failed($"HTTP {(int)response.StatusCode}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return UpdateCheckResult.Failed("Unexpected response from GitHub.");
            }

            JsonElement? latestRelease = null;
            ParsedVersion? latestParsed = null;

            foreach (var release in doc.RootElement.EnumerateArray())
            {
                // Drafts aren't a published version at all — GitHub only includes them here for
                // callers with push access, which this unauthenticated request never has, but
                // skip them defensively anyway.
                if (release.TryGetProperty("draft", out var draftEl) && draftEl.GetBoolean())
                {
                    continue;
                }

                var tagName = release.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty;
                var parsed = ParsedVersion.TryParse(tagName);
                if (parsed is null)
                {
                    continue;
                }

                if (latestParsed is null || parsed.Value.CompareTo(latestParsed.Value) > 0)
                {
                    latestParsed = parsed;
                    latestRelease = release;
                }
            }

            if (latestRelease is null || latestParsed is null)
            {
                return UpdateCheckResult.Failed("No release has been published yet.");
            }

            var releaseEl = latestRelease.Value;
            var latestTagName = releaseEl.TryGetProperty("tag_name", out var latestTagEl) ? latestTagEl.GetString() ?? string.Empty : string.Empty;
            var releaseUrl = releaseEl.TryGetProperty("html_url", out var htmlUrlEl) ? htmlUrlEl.GetString() : null;
            var downloadUrl = FindWindowsAssetUrl(releaseEl);

            var currentVersion = ParsedVersion.TryParse(AppVersionInfo.CurrentInformationalVersionText);
            var isUpdateAvailable = currentVersion is null || latestParsed.Value.CompareTo(currentVersion.Value) > 0;

            return new UpdateCheckResult
            {
                Success = true,
                IsUpdateAvailable = isUpdateAvailable,
                LatestVersionText = latestTagName,
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
            var tag = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
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

    public void Dispose() => _httpClient.Dispose();

    /// <summary>
    /// A tag like "v1.0.0-beta11" split into a comparable core version plus an optional
    /// prerelease label/number. <see cref="Version"/> alone can't represent this: it rejects any
    /// non-numeric suffix outright, which is exactly why the update check used to silently fail
    /// to compare beta tags at all.
    /// </summary>
    private readonly record struct ParsedVersion(Version Core, string? PrereleaseLabel, int? PrereleaseNumber)
        : IComparable<ParsedVersion>
    {
        public int CompareTo(ParsedVersion other)
        {
            var coreCompare = Core.CompareTo(other.Core);
            if (coreCompare != 0)
            {
                return coreCompare;
            }

            // No prerelease label means a stable release, which always outranks a prerelease of
            // the same core version (e.g. "1.0.0" > "1.0.0-beta11").
            if (PrereleaseLabel is null && other.PrereleaseLabel is null)
            {
                return 0;
            }
            if (PrereleaseLabel is null)
            {
                return 1;
            }
            if (other.PrereleaseLabel is null)
            {
                return -1;
            }

            var labelCompare = string.Compare(PrereleaseLabel, other.PrereleaseLabel, StringComparison.OrdinalIgnoreCase);
            return labelCompare != 0 ? labelCompare : (PrereleaseNumber ?? 0).CompareTo(other.PrereleaseNumber ?? 0);
        }

        public static ParsedVersion? TryParse(string tag)
        {
            var cleaned = tag.TrimStart('v', 'V');
            var dashIndex = cleaned.IndexOf('-');
            var corePart = dashIndex >= 0 ? cleaned[..dashIndex] : cleaned;
            var prereleasePart = dashIndex >= 0 ? cleaned[(dashIndex + 1)..] : null;

            if (!Version.TryParse(corePart, out var core))
            {
                return null;
            }

            if (string.IsNullOrEmpty(prereleasePart))
            {
                return new ParsedVersion(core, null, null);
            }

            // "beta11" -> label "beta", number 11. A suffix with no trailing digits (or none at
            // all) still compares fine — it just always loses ties on PrereleaseNumber.
            var digitsStart = prereleasePart.Length;
            while (digitsStart > 0 && char.IsDigit(prereleasePart[digitsStart - 1]))
            {
                digitsStart--;
            }

            var label = prereleasePart[..digitsStart];
            var numberText = prereleasePart[digitsStart..];
            var number = numberText.Length > 0 && int.TryParse(numberText, out var n) ? n : (int?)null;

            return new ParsedVersion(core, label, number);
        }
    }
}
