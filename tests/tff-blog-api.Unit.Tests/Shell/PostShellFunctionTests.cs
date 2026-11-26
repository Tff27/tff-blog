using System.Net;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Services;
using Tff.Blog.Api.Shell;
using Tff.Blog.Api.Unit.Tests.Infrastructure;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Shell;

public class PostShellFunctionTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private const string Slug = "test-post";
    private const string CrawlerUserAgent = "facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)";
    private const string CacheControlValue = "public, max-age=300, stale-while-revalidate=60";

    private readonly IContentProvider _contentProvider = Substitute.For<IContentProvider>();
    private readonly ILogger<PostShellFunction> _logger = Substitute.For<ILogger<PostShellFunction>>();
    private readonly PostShellFunction _sut;

    public PostShellFunctionTests()
    {
        _sut = new PostShellFunction(_logger, _contentProvider, new FeedSettings(SiteUrl));
    }

    [Fact]
    public async Task RunPostShellAsync_ForACrawler_ServesThePostMetadata()
    {
        GivenPost();

        var response = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetHeader(response, "Content-Type").Should().Be("text/html; charset=utf-8");
        var body = ReadBody(response);
        body.Should().Contain("<meta property=\"og:title\" content=\"Test post\" />");
        body.Should().Contain("<meta property=\"og:description\" content=\"A short description\" />");
        body.Should().Contain($"<meta property=\"og:image\" content=\"https://example.com/cover.png\" />");
        body.Should().Contain($"<link rel=\"canonical\" href=\"{SiteUrl}/post/{Slug}\" />");
    }

    [Fact]
    public async Task RunPostShellAsync_ForACrawler_SetsCacheHeadersAndVariesByUserAgent()
    {
        GivenPost();

        var response = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), CancellationToken.None);

        GetHeader(response, "Cache-Control").Should().Be(CacheControlValue);
        GetHeader(response, "ETag").Should().StartWith("\"").And.EndWith("\"");
        GetHeader(response, "Vary").Should().Be("User-Agent");
    }

    [Fact]
    public async Task RunPostShellAsync_ForABrowser_ServesThePlainShellWithoutFetchingThePost()
    {
        var response = await _sut.RunPostShellAsync(
            CreateRequest("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0", Slug),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = ReadBody(response);
        body.Should().Contain("<title>Tiago Fonseca - Blog</title>");
        body.Should().NotContain("property=\"og:title\"");
        await _contentProvider.DidNotReceive().GetPostAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunPostShellAsync_WithoutASlug_ServesThePlainShell()
    {
        var response = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, slug: null), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().Contain("<title>Tiago Fonseca - Blog</title>");
        await _contentProvider.DidNotReceive().GetPostAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunPostShellAsync_WhenThePostIsMissing_ServesThePlainShell()
    {
        _contentProvider.GetPostAsync(Slug, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(null));

        var response = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().Contain("<title>Tiago Fonseca - Blog</title>");
    }

    [Fact]
    public async Task RunPostShellAsync_WhenTheCmsThrows_ServesThePlainShellInsteadOfFailing()
    {
        _contentProvider.GetPostAsync(Slug, false, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("CMS is down"));

        var response = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().Contain("<title>Tiago Fonseca - Blog</title>");
    }

    [Fact]
    public async Task RunPostShellAsync_WithMatchingETag_ReturnsNotModified()
    {
        GivenPost();

        var firstResponse = await _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), CancellationToken.None);
        var etag = GetHeader(firstResponse, "ETag");

        var request = CreateRequest(CrawlerUserAgent, Slug);
        request.Headers.Add("If-None-Match", etag);

        var response = await _sut.RunPostShellAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        ReadBody(response).Should().BeEmpty();
        GetHeader(response, "ETag").Should().Be(etag);
    }

    [Fact]
    public async Task RunPostShellAsync_WithCancelledToken_ThrowsAndDoesNotFetchThePost()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var act = () => _sut.RunPostShellAsync(CreateRequest(CrawlerUserAgent, Slug), cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _contentProvider.DidNotReceive().GetPostAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ResolveSlug_PrefersTheOriginalUrlHeader()
    {
        var request = CreateRequest(CrawlerUserAgent, slug: null);
        request.Headers.Add("x-ms-original-url", $"https://www.tiagofonseca.dev/post/from-header?preview=true");

        PostShellFunction.ResolveSlug(request).Should().Be("from-header");
    }

    [Fact]
    public void ResolveSlug_FallsBackToTheQueryString()
    {
        var request = new TestHttpRequestData(url: "http://localhost/api/post-shell?slug=from-query");

        PostShellFunction.ResolveSlug(request).Should().Be("from-query");
    }

    [Fact]
    public void ResolveSlug_FallsBackToTheRequestPath()
    {
        var request = new TestHttpRequestData(url: "http://localhost/post/from-path");

        PostShellFunction.ResolveSlug(request).Should().Be("from-path");
    }

    [Theory]
    [InlineData("http://localhost/api/post-shell")]
    [InlineData("http://localhost/post")]
    [InlineData("http://localhost/about/team")]
    public void ResolveSlug_WithAnUnrelatedPath_ReturnsNull(string url)
    {
        PostShellFunction.ResolveSlug(new TestHttpRequestData(url: url)).Should().BeNull();
    }

    private void GivenPost()
    {
        _contentProvider.GetPostAsync(Slug, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(new PostModel
            {
                Slug = Slug,
                Title = "Test post",
                Description = "A short description",
                Image = "https://example.com/cover.png",
            }));
    }

    private static TestHttpRequestData CreateRequest(string? userAgent, string? slug = Slug)
    {
        var request = new TestHttpRequestData(url: "http://localhost/api/post-shell");

        if (userAgent is not null)
        {
            request.Headers.Add("User-Agent", userAgent);
        }

        if (slug is not null)
        {
            request.Headers.Add("x-ms-original-url", $"http://localhost/post/{slug}");
        }

        return request;
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
