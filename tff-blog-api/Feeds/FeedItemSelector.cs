using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Feeds;

/// <summary>
/// Picks the posts that go into a feed: published only, newest first, capped at
/// <see cref="FeedConstants.ItemLimit"/>.
/// </summary>
internal static class FeedItemSelector
{
    internal static IReadOnlyList<PostModel> Select(IEnumerable<PostModel> posts)
    {
        ArgumentNullException.ThrowIfNull(posts);

        // Drafts are filtered here as well as by asking for published content, so a misconfigured
        // ShowDrafts setting can never publish a draft through a feed.
        var publishedPosts = posts
            .Where(post => post is not null && !post.Draft && !string.IsNullOrWhiteSpace(post.Slug))
            .ToList();

        return PostSorter
            .Sort(publishedPosts, "Date", "Descending")
            .Take(FeedConstants.ItemLimit)
            .ToList();
    }
}
