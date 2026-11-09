namespace Tff.Blog.Client.Configuration;

/// <summary>
/// Absolute URL of the RSS feed, resolved from <c>FeedUrl</c> in the app settings. Production
/// keeps the relative <c>rss.xml</c> (the Static Web App rewrites it to the API), while local
/// development points at the Functions host.
/// </summary>
public sealed record FeedLinkSettings(string Url);
