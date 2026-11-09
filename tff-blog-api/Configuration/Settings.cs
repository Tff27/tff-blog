#nullable enable annotations

namespace Tff.Blog.Api.Configuration;

public static class Settings
{
    private const string DefaultStoryblokEndpoint = "https://api.storyblok.com";
    private const string DefaultSiteUrl = "https://www.tiagofonseca.dev";

    public static bool GetShowDrafts()
    {
        return ParseBoolean(GetEnvironmentVariable("ShowDrafts"));
    }

    internal static bool ParseBoolean(string? value)
    {
        return bool.TryParse(value, out var parsed) && parsed;
    }

    public static string GetStoryblokEndpoint()
    {
        return GetEnvironmentVariable("StoryblokEndpoint") ?? DefaultStoryblokEndpoint;
    }

    public static string GetStoryblokPublicToken()
    {
        return GetEnvironmentVariable("StoryblokPublicToken") ?? string.Empty;
    }

    public static string GetStoryblokPreviewToken()
    {
        return GetEnvironmentVariable("StoryblokPreviewToken") ?? string.Empty;
    }

    public static int GetCacheTtlMinutes()
    {
        return int.TryParse(GetEnvironmentVariable("CacheTtlMinutes"), out var minutes) && minutes > 0
            ? minutes
            : 0;
    }

    public static string GetPreviewSecret()
    {
        return GetEnvironmentVariable("PreviewSecret") ?? string.Empty;
    }

    public static string GetSiteUrl()
    {
        return NormalizeSiteUrl(GetEnvironmentVariable("SiteUrl"));
    }

    internal static string NormalizeSiteUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultSiteUrl;
        }

        var normalized = value.Trim().TrimEnd('/');

        return normalized.Length == 0 ? DefaultSiteUrl : normalized;
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
