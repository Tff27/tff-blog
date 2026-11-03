using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Services;

public class CachedContentProviderTests : IDisposable
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(1);

    private readonly IContentProvider _inner = Substitute.For<IContentProvider>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task GetPostsAsync_CachesResults()
    {
        var posts = new List<PostModel> { new() { Slug = "post" } };
        _inner.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(posts));
        var sut = CreateProvider(DefaultTtl);

        var first = await sut.GetPostsAsync();
        var second = await sut.GetPostsAsync();

        second.Should().BeSameAs(first);
        await _inner.Received(1).GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPostsAsync_WhenTtlIsNotPositive_BypassesTheCache(int ttlMinutes)
    {
        _inner.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<PostModel>()));
        var sut = CreateProvider(TimeSpan.FromMinutes(ttlMinutes));

        await sut.GetPostsAsync();
        await sut.GetPostsAsync();

        await _inner.Received(2).GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostAsync_CachesEachSlugSeparately()
    {
        var firstPost = new PostModel { Slug = "first" };
        var secondPost = new PostModel { Slug = "second" };
        _inner.GetPostAsync("first", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(firstPost));
        _inner.GetPostAsync("second", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(secondPost));
        var sut = CreateProvider(DefaultTtl);

        (await sut.GetPostAsync("first")).Should().BeSameAs(firstPost);
        (await sut.GetPostAsync("second")).Should().BeSameAs(secondPost);
        (await sut.GetPostAsync("first")).Should().BeSameAs(firstPost);

        await _inner.Received(1).GetPostAsync("first", Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _inner.Received(1).GetPostAsync("second", Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostAsync_DoesNotCacheMissingPosts()
    {
        _inner.GetPostAsync("missing", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(null));
        var sut = CreateProvider(DefaultTtl);

        (await sut.GetPostAsync("missing")).Should().BeNull();
        (await sut.GetPostAsync("missing")).Should().BeNull();

        await _inner.Received(2).GetPostAsync("missing", Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageAsync_CachesResults()
    {
        var page = new PageModel { Text = "# About" };
        _inner.GetPageAsync("about", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PageModel?>(page));
        var sut = CreateProvider(DefaultTtl);

        (await sut.GetPageAsync("about")).Should().BeSameAs(page);
        (await sut.GetPageAsync("about")).Should().BeSameAs(page);

        await _inner.Received(1).GetPageAsync("about", Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageAsync_DoesNotCacheMissingPages()
    {
        _inner.GetPageAsync("missing", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PageModel?>(null));
        var sut = CreateProvider(DefaultTtl);

        (await sut.GetPageAsync("missing")).Should().BeNull();
        (await sut.GetPageAsync("missing")).Should().BeNull();

        await _inner.Received(2).GetPageAsync("missing", Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostsAsync_DoesNotCacheFailures()
    {
        var posts = new List<PostModel>();
        _inner.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<List<PostModel>>(new HttpRequestException("Storyblok is unavailable")),
                Task.FromResult(posts));
        var sut = CreateProvider(DefaultTtl);

        Func<Task> act = () => sut.GetPostsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
        (await sut.GetPostsAsync()).Should().BeSameAs(posts);
        await _inner.Received(2).GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostsAsync_WhenPreviewIsRequested_BypassesTheCache()
    {
        _inner.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<PostModel>()));
        var sut = CreateProvider(DefaultTtl);

        await sut.GetPostsAsync();
        await sut.GetPostsAsync(cancellationToken: default, preview: true);
        await sut.GetPostsAsync();

        await _inner.Received(1).GetPostsAsync(false, Arg.Any<CancellationToken>());
        await _inner.Received(1).GetPostsAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostsAsync_WhenTheTtlExpires_FetchesAgain()
    {
        _inner.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new List<PostModel>()));
        var sut = CreateProvider(TimeSpan.FromMilliseconds(20));

        await sut.GetPostsAsync();
        await Task.Delay(250);
        await sut.GetPostsAsync();

        await _inner.Received(2).GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private CachedContentProvider CreateProvider(TimeSpan cacheTtl)
    {
        return new CachedContentProvider(_inner, _cache, cacheTtl);
    }
}
