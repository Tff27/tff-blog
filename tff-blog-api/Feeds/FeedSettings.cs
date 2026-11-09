namespace Tff.Blog.Api.Feeds;

/// <summary>
/// Absolute site root used to build the links inside the feeds. It never has a trailing slash.
/// </summary>
public sealed record FeedSettings(string SiteUrl);
