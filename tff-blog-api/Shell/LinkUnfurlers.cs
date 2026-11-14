#nullable enable annotations

namespace Tff.Blog.Api.Shell;

/// <summary>
/// User agents that fetch a page only to build a link preview. They never execute JavaScript, so
/// the metadata has to be in the HTML they receive. Search engines are deliberately absent: they
/// render the SPA, and injecting the tags for them too would duplicate every one of them.
/// </summary>
public static class LinkUnfurlers
{
    internal const string UserAgentHeader = "User-Agent";

    private static readonly string[] Tokens =
    [
        "facebookexternalhit",
        "facebookcatalog",
        "twitterbot",
        "linkedinbot",
        "slackbot",
        "slack-imgproxy",
        "discordbot",
        "telegrambot",
        "whatsapp",
        "pinterest",
        "redditbot",
        "embedly",
        "iframely",
        "mastodon",
        "bluesky",
        "skypeuripreview",
        "vkshare",
    ];

    public static bool IsMatch(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        foreach (var token in Tokens)
        {
            if (userAgent.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
