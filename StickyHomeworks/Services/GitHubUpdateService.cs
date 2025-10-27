using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; set; } = new();
}

public class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";
}

public class GitHubUpdateService
{
    private const string GitHubApiBaseUrl = "https://api.github.com/repos/Sticky-attention/Sticky-attention/releases";
    private readonly HttpClient _httpClient;
    private readonly Settings _settings;

    public GitHubUpdateService(Settings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "StickyHomeworks-Update-Checker");
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            var releases = await _httpClient.GetFromJsonAsync<GitHubRelease[]>(GitHubApiBaseUrl);
            if (releases == null || releases.Length == 0)
                return null;

            // 过滤掉草稿版本
            var publishedReleases = releases.Where(r => !r.Draft).ToList();
            if (publishedReleases.Count == 0)
                return null;

            // 根据用户选择的更新通道返回相应的版本
            switch (_settings.UpdateChannel)
            {
                case UpdateChannel.Nightly:
                    // 暂时禁用Nightly通道，将其视为PreRelease通道
                    goto case UpdateChannel.PreRelease;
                    
                case UpdateChannel.PreRelease:
                    // PreRelease通道：返回最新的预发布版本或稳定版本
                    var preReleaseOrStable = publishedReleases.Where(r => r.Prerelease || !r.Prerelease).ToList();
                    return preReleaseOrStable.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
                    
                case UpdateChannel.Release:
                default:
                    // Release通道：只返回最新的稳定版本
                    var stableReleases = publishedReleases.Where(r => !r.Prerelease).ToList();
                    return stableReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<GitHubRelease?> GetLatestStableReleaseAsync()
    {
        try
        {
            var releases = await _httpClient.GetFromJsonAsync<GitHubRelease[]>(GitHubApiBaseUrl);
            if (releases == null || releases.Length == 0)
                return null;

            // 过滤掉草稿和预发布版本
            var stableReleases = releases.Where(r => !r.Draft && !r.Prerelease).ToList();
            if (stableReleases.Count == 0)
                return null;

            // 返回最新的稳定版本
            return stableReleases.OrderByDescending(r => r.PublishedAt).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        try
        {
            // 移除版本号前的 "v" 字符（如果存在）
            if (remoteVersion.StartsWith("v"))
                remoteVersion = remoteVersion.Substring(1);

            if (currentVersion.StartsWith("v"))
                currentVersion = currentVersion.Substring(1);

            // 分离主版本号和预发布标签
            var remoteParts = ParseVersionWithPreRelease(remoteVersion);
            var currentParts = ParseVersionWithPreRelease(currentVersion);

            // 比较主版本号
            var remoteMain = new Version(remoteParts.mainVersion);
            var currentMain = new Version(currentParts.mainVersion);

            if (remoteMain > currentMain)
                return true;
            
            if (remoteMain < currentMain)
                return false;

            // 主版本号相同，比较预发布标签
            // 如果远程版本没有预发布标签，而当前版本有，则远程版本更新
            if (string.IsNullOrEmpty(remoteParts.preRelease) && !string.IsNullOrEmpty(currentParts.preRelease))
                return true;
            
            // 如果远程版本有预发布标签，而当前版本没有，则当前版本更新（更稳定）
            if (!string.IsNullOrEmpty(remoteParts.preRelease) && string.IsNullOrEmpty(currentParts.preRelease))
                return false;
            
            // 如果两者都有预发布标签，则按字典序比较
            if (!string.IsNullOrEmpty(remoteParts.preRelease) && !string.IsNullOrEmpty(currentParts.preRelease))
                return string.Compare(remoteParts.preRelease, currentParts.preRelease, StringComparison.Ordinal) > 0;
            
            // 两者都没有预发布标签，版本相同
            return false;
        }
        catch
        {
            return false;
        }
    }

    private (string mainVersion, string preRelease) ParseVersionWithPreRelease(string version)
    {
        // 使用正则表达式分离版本号和预发布标签
        var match = Regex.Match(version, @"^([0-9]+\.?[0-9]*\.?[0-9]*\.?[0-9]*)(?:-(.*))?$");
        if (match.Success)
        {
            var mainVersion = match.Groups[1].Value;
            var preRelease = match.Groups[2].Success ? match.Groups[2].Value : "";
            return (mainVersion, preRelease);
        }
        
        // 如果不匹配正则表达式，则将整个字符串视为主版本号
        return (version, "");
    }

    public string FormatVersionDisplay(string versionTag)
    {
        // 移除版本号前的 "v" 字符（如果存在）
        if (versionTag.StartsWith("v"))
            versionTag = versionTag.Substring(1);

        // 分离主版本号和预发布标签
        var parts = ParseVersionWithPreRelease(versionTag);
        
        if (string.IsNullOrEmpty(parts.preRelease))
        {
            // 稳定版本
            return $"版本 {parts.mainVersion}";
        }
        else
        {
            // 预发布版本
            var preReleaseParts = parts.preRelease.Split('.');
            if (preReleaseParts.Length >= 2)
            {
                // 格式如: rc.0 或 rc.0.1
                var type = preReleaseParts[0]; // rc
                var candidate = preReleaseParts[1]; // 0
                
                string typeText = type switch
                {
                    "alpha" => "Alpha",
                    "beta" => "Beta",
                    "rc" => "候选版本",
                    _ => type
                };
                
                if (preReleaseParts.Length >= 3 && !string.IsNullOrEmpty(preReleaseParts[2]))
                {
                    // 包含错误修复版本号
                    var fix = preReleaseParts[2]; // 1
                    return $"版本 {parts.mainVersion} {typeText} {candidate} 错误修复版本 {fix}";
                }
                else
                {
                    // 不包含错误修复版本号
                    return $"版本 {parts.mainVersion} {typeText} {candidate}";
                }
            }
            else
            {
                // 其他格式的预发布版本
                return $"版本 {parts.mainVersion} ({parts.preRelease})";
            }
        }
    }

    public string GetCurrentVersion()
    {
        // 获取当前应用版本
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version != null ? version.ToString() : "0.0.0.0";
    }
    
    public string ReplaceWithMirrorUrl(string originalUrl, string mirrorUrl)
    {
        // 如果没有设置镜像地址，则返回原始URL
        if (string.IsNullOrEmpty(mirrorUrl))
            return originalUrl;
            
        // 替换GitHub下载地址为镜像地址
        // 原始URL格式: https://github.com/user/repo/releases/download/tag/file.zip
        // 镜像URL格式: https://mirror.example.com/github.com/user/repo/releases/download/tag/file.zip
        if (originalUrl.StartsWith("https://github.com/"))
        {
            var relativePath = originalUrl.Substring("https://github.com/".Length);
            // 确保镜像URL末尾没有多余的斜杠
            var cleanMirrorUrl = mirrorUrl.TrimEnd('/');
            return $"{cleanMirrorUrl}/github.com/{relativePath}";
        }
        
        return originalUrl;
    }
    
    public string FindAppropriateAsset(List<GitHubReleaseAsset> assets)
    {
        // 根据当前系统架构选择合适的下载资源
        var architecture = Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86";
        
        // 根据是否需要包含运行时来选择资产
        // 优先选择包含运行时的版本（自包含版本）
        var assetWithRuntime = assets.FirstOrDefault(a => 
            a.Name.Contains($"sticky-attentions-forked") &&
            a.Name.Contains(architecture) && 
            !a.Name.Contains("no-runtime") && 
            a.Name.EndsWith(".zip"));
        
        if (assetWithRuntime != null)
            return assetWithRuntime.BrowserDownloadUrl;
        
        // 如果没有包含运行时的版本，则选择不包含运行时的版本
        var assetWithoutRuntime = assets.FirstOrDefault(a => 
            a.Name.Contains($"sticky-attentions-forked") &&
            a.Name.Contains(architecture) && 
            a.Name.Contains("no-runtime") && 
            a.Name.EndsWith(".zip"));
        
        return assetWithoutRuntime?.BrowserDownloadUrl ?? string.Empty;
    }
}