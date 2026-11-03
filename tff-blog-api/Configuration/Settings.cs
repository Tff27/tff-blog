#nullable enable annotations

namespace Tff.Blog.Api.Configuration;

public static class Settings
{
    private const string DefaultStoryblokEndpoint = "https://api.storyblok.com";

    public static bool GetShowDrafts()
    {
        return Convert.ToBoolean(GetEnvironmentVariable("ShowDrafts"));
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

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
