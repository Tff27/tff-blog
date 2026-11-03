#nullable enable annotations

using Microsoft.Extensions.Caching.Memory;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Services;

/// <summary>
/// Caches <see cref="IContentProvider"/> results in memory. A TTL that is zero or negative disables
/// caching entirely, which is the default.
/// </summary>
public sealed class CachedContentProvider : IContentProvider
{
    private const string PostsCacheKey = "content:posts";

    private readonly IContentProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl;

    public CachedContentProvider(IContentProvider inner, IMemoryCache cache, TimeSpan cacheTtl)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
        _cacheTtl = cacheTtl;
    }

    private bool IsCacheEnabled => _cacheTtl > TimeSpan.Zero;

    public async Task<List<PostModel>> GetPostsAsync(CancellationToken cancellationToken = default, bool preview = false)
    {
        if (preview || !IsCacheEnabled)
        {
            return await _inner.GetPostsAsync(cancellationToken, preview);
        }

        if (_cache.TryGetValue(PostsCacheKey, out List<PostModel>? cachedPosts) && cachedPosts is not null)
        {
            return cachedPosts;
        }

        var posts = await _inner.GetPostsAsync(cancellationToken, preview);

        _cache.Set(PostsCacheKey, posts, _cacheTtl);

        return posts;
    }

    public async Task<PostModel?> GetPostAsync(string slug, CancellationToken cancellationToken = default, bool preview = false)
    {
        if (preview || !IsCacheEnabled)
        {
            return await _inner.GetPostAsync(slug, cancellationToken, preview);
        }

        var cacheKey = GetPostCacheKey(slug);

        if (_cache.TryGetValue(cacheKey, out PostModel? cachedPost) && cachedPost is not null)
        {
            return cachedPost;
        }

        var post = await _inner.GetPostAsync(slug, cancellationToken, preview);

        if (post is not null)
        {
            _cache.Set(cacheKey, post, _cacheTtl);
        }

        return post;
    }

    public async Task<PageModel?> GetPageAsync(string slug, CancellationToken cancellationToken = default, bool preview = false)
    {
        if (preview || !IsCacheEnabled)
        {
            return await _inner.GetPageAsync(slug, cancellationToken, preview);
        }

        var cacheKey = GetPageCacheKey(slug);

        if (_cache.TryGetValue(cacheKey, out PageModel? cachedPage) && cachedPage is not null)
        {
            return cachedPage;
        }

        var page = await _inner.GetPageAsync(slug, cancellationToken, preview);

        if (page is not null)
        {
            _cache.Set(cacheKey, page, _cacheTtl);
        }

        return page;
    }

    private static string GetPostCacheKey(string slug) => $"content:posts:{slug}";

    private static string GetPageCacheKey(string slug) => $"content:pages:{slug}";
}
