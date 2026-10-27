using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tff.Blog.Client.Services;
using Tff.Blog.Shared.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tff.Blog.Client.Unit.Tests.Services;

public class BlogContentServiceTests : IDisposable
{
    private const string PostsJson = """
        [
          {
            "title": "Post one",
            "slug": "post-one",
            "description": "A description",
            "image": "https://example.com/cover.png",
            "date": "2024-01-01T00:00:00Z",
            "readTimeInMinutes": 3,
            "tags": [ "dotnet" ],
            "text": "Post body"
          }
        ]
        """;

    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;
    private readonly BlogContentService _sut;

    public BlogContentServiceTests()
    {
        _server = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri($"{_server.Url}/") };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        _sut = new BlogContentService(httpClientFactory, NullLogger<BlogContentService>.Instance);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsPosts()
    {
        StubJson("/", PostsJson);

        var result = await _sut.GetPostsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().ContainSingle();
        result.Content![0].Title.Should().Be("Post one");
    }

    [Fact]
    public async Task GetPostAsync_ReturnsFirstPost()
    {
        StubJson("/", PostsJson);

        var result = await _sut.GetPostAsync("post-one");

        result.IsSuccess.Should().BeTrue();
        result.Content!.Slug.Should().Be("post-one");
    }

    [Fact]
    public async Task GetPostAsync_WhenApiReturnsEmptyList_ReturnsNotFound()
    {
        StubJson("/", "[]");

        var result = await _sut.GetPostAsync("missing-post");

        result.IsNotFound.Should().BeTrue();
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task GetPostAsync_UrlEncodesTheSlug()
    {
        Uri? requestUri = null;
        var handler = new CapturingHandler(uri => requestUri = uri);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        var service = new BlogContentService(httpClientFactory, NullLogger<BlogContentService>.Instance);

        await service.GetPostAsync("hello world");

        requestUri.Should().NotBeNull();
        requestUri!.Query.Should().Be("?postName=hello%20world");
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiReturnsNotFound_ReturnsNotFound()
    {
        StubJson("/", string.Empty, HttpStatusCode.NotFound);

        var result = await _sut.GetPostsAsync();

        result.IsNotFound.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiFails_ReturnsErrorMessage()
    {
        StubJson("/", string.Empty, HttpStatusCode.InternalServerError);

        var result = await _sut.GetPostsAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(BlogContentService.UnavailableMessage);
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiClientIsNotRegistered_ReturnsErrorMessage()
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("The client is not registered."));
        var service = new BlogContentService(httpClientFactory, NullLogger<BlogContentService>.Instance);

        var result = await service.GetPostsAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(BlogContentService.UnavailableMessage);
    }

    [Fact]
    public async Task GetPostsAsync_WhenRequestTimesOut_ReturnsErrorMessage()
    {
        _server
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(PostsJson)
                .WithDelay(TimeSpan.FromSeconds(5)));

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{_server.Url}/"),
            Timeout = TimeSpan.FromMilliseconds(100),
        };
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var service = new BlogContentService(httpClientFactory, NullLogger<BlogContentService>.Instance);

        var result = await service.GetPostsAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(BlogContentService.UnavailableMessage);
    }

    [Fact]
    public async Task GetPostsAsync_WhenResponseIsMalformed_ReturnsErrorMessage()
    {
        StubJson("/", "{ not valid json");

        var result = await _sut.GetPostsAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(BlogContentService.UnavailableMessage);
    }

    [Fact]
    public async Task GetMarkdownPageAsync_ReturnsConvertedModel()
    {
        StubText("/pages/about.md", "---\ntitle: About\n---\n\n# About");

        var result = await _sut.GetMarkdownPageAsync<PageModel>("about");

        result.IsSuccess.Should().BeTrue();
        result.Content!.Text.Should().Be("# About");
    }

    [Fact]
    public async Task GetMarkdownPageAsync_WhenPageIsMissing_ReturnsNotFound()
    {
        StubText("/pages/missing.md", string.Empty, HttpStatusCode.NotFound);

        var result = await _sut.GetMarkdownPageAsync<PageModel>("missing");

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task GetPostsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        StubJson("/", PostsJson);

        var act = () => _sut.GetPostsAsync(cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _server.Dispose();
    }

    private void StubJson(string path, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubText(string path, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "text/plain")
                .WithBody(body));
    }

    private sealed class CapturingHandler(Action<Uri?> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            capture(request.RequestUri);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
