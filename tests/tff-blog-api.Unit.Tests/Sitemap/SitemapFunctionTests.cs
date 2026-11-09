using System.Net;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Services;
using Tff.Blog.Api.Sitemap;
using Tff.Blog.Api.Unit.Tests.Feeds;
using Tff.Blog.Api.Unit.Tests.Infrastructure;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Sitemap;

public class SitemapFunctionTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private const string CacheControlValue = "public, max-age=3600, stale-while-revalidate=60";

    private readonly IContentProvider _contentProvider = Substitute.For<IContentProvider>();
    private readonly ILogger<SitemapFunction> _logger = Substitute.For<ILogger<SitemapFunction>>();
    private readonly SitemapFunction _sut;

    public SitemapFunctionTests()
    {
        _contentProvider.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreatePosts()));

        _sut = new SitemapFunction(_logger, _contentProvider, new FeedSettings(SiteUrl));
    }

    [Fact]
    public async Task RunSitemapAsync_ReturnsTheSitemapDocument()
    {
        var response = await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetHeader(response, "Content-Type").Should().Be("application/xml; charset=utf-8");
        ReadBody(response).Should().Contain("http://www.sitemaps.org/schemas/sitemap/0.9");
    }

    [Fact]
    public async Task RunSitemapAsync_SetsCacheHeaders()
    {
        var response = await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None);

        GetHeader(response, "Cache-Control").Should().Be(CacheControlValue);
        GetHeader(response, "ETag").Should().StartWith("\"").And.EndWith("\"");
    }

    [Fact]
    public async Task RunSitemapAsync_WithMatchingETag_ReturnsNotModified()
    {
        var firstResponse = await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None);
        var etag = GetHeader(firstResponse, "ETag");

        var request = CreateRequest();
        request.Headers.Add("If-None-Match", etag);
        var response = await _sut.RunSitemapAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        ReadBody(response).Should().BeEmpty();
        GetHeader(response, "ETag").Should().Be(etag);
        GetHeader(response, "Cache-Control").Should().Be(CacheControlValue);
    }

    [Fact]
    public async Task RunSitemapAsync_WithStaleETag_ReturnsTheSitemap()
    {
        var request = CreateRequest();
        request.Headers.Add("If-None-Match", "\"stale-etag\"");

        var response = await _sut.RunSitemapAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().Contain("<urlset");
    }

    [Fact]
    public async Task RunSitemapAsync_IsStableBetweenRequests()
    {
        var first = GetHeader(await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None), "ETag");
        var second = GetHeader(await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None), "ETag");

        first.Should().Be(second);
    }

    [Fact]
    public async Task RunSitemapAsync_WithAPreviewParameter_StillRequestsPublishedContent()
    {
        var response = await _sut.RunSitemapAsync(CreateRequest("api/sitemap.xml?preview=preview-secret"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _contentProvider.Received(1).GetPostsAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunSitemapAsync_ExcludesDrafts()
    {
        _contentProvider.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PostModel>
            {
                FeedItemSelectorTests.CreatePost("published", new DateTime(2026, 11, 5, 9, 30, 0, DateTimeKind.Utc)),
                FeedItemSelectorTests.CreatePost("draft", new DateTime(2026, 11, 6, 9, 30, 0, DateTimeKind.Utc), draft: true),
            }));

        var response = await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None);

        var body = ReadBody(response);
        body.Should().Contain("/post/published");
        body.Should().NotContain("/post/draft");
    }

    [Fact]
    public async Task RunSitemapAsync_WhenTheCmsThrows_ReturnsServerErrorWithoutCaching()
    {
        _contentProvider.GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("CMS is down"));

        var response = await _sut.RunSitemapAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        GetHeader(response, "Cache-Control").Should().Be("no-store");
    }

    [Fact]
    public async Task RunSitemapAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var act = () => _sut.RunSitemapAsync(CreateRequest(), cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _contentProvider.DidNotReceive().GetPostsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private static TestHttpRequestData CreateRequest(string path = "api/sitemap.xml")
    {
        return new TestHttpRequestData(url: $"http://localhost/{path}");
    }

    private static List<PostModel> CreatePosts()
    {
        return new List<PostModel>
        {
            FeedItemSelectorTests.CreatePost("initial-blog", new DateTime(2022, 9, 12, 8, 0, 0, DateTimeKind.Utc)),
            FeedItemSelectorTests.CreatePost("auto-generate-c-sharp-client", new DateTime(2022, 9, 26, 8, 0, 0, DateTimeKind.Utc)),
        };
    }

    private static string ReadBody(HttpResponseData response)
    {
        return response.Should().BeOfType<TestHttpResponseData>().Subject.ReadBodyAsString();
    }

    private static string GetHeader(HttpResponseData response, string name)
    {
        response.Headers.TryGetValues(name, out var values).Should().BeTrue();
        values!.Should().ContainSingle();

        return values!.Single();
    }
}
