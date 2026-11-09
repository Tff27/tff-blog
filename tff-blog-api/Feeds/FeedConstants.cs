namespace Tff.Blog.Api.Feeds;

internal static class FeedConstants
{
    /// <summary>
    /// Number of posts published in each feed.
    /// </summary>
    internal const int ItemLimit = 15;

    internal const string Title = "Tiago Fonseca - Blog";
    internal const string Description = "Posts by Tiago Fonseca on .NET, Blazor, Azure and software engineering.";
    internal const string Author = "Tiago Fonseca";
    internal const string Language = "en";

    internal const string RssPath = "/rss.xml";
    internal const string AtomPath = "/atom.xml";
}
