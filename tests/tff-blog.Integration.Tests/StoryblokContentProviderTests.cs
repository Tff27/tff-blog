using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Tff.Blog.Api.Cms.Storyblok;
using Tff.Blog.Api.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tff.Blog.Integration.Tests;

public class StoryblokContentProviderTests : IDisposable
{
    private const string PublicToken = "public-token";
    private const string PreviewToken = "preview-token";
    private const int PageSize = 100;

    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;
    private readonly StoryblokContentProvider _sut;

    public StoryblokContentProviderTests()
    {
        _server = WireMockServer.Start();
        _httpClient = new HttpClient();
        _sut = new StoryblokContentProvider(
            _httpClient,
            new StoryblokSettings(_server.Url!, PublicToken, PreviewToken, ShowDrafts: false));
    }

    [Fact]
    public async Task GetPostsAsync_MapsPostsFromStoryblokResponse()
    {
        StubStories(CreateStoriesResponse(CreateStory("first-post", "First post", "## Hello", tags: ["dotnet", "blazor"])));

        var posts = await _sut.GetPostsAsync();

        posts.Should().ContainSingle();
        var post = posts[0];
        post.Title.Should().Be("First post");
        post.Slug.Should().Be("first-post");
        post.Description.Should().Be("A brief");
        post.Image.Should().Be("https://example.com/cover.png");
        post.Text.Should().Be("## Hello");
        post.ReadTimeInMinutes.Should().Be(1);
        post.Date.Should().Be(new DateTime(2024, 4, 1, 10, 0, 0, DateTimeKind.Utc));
        post.Tags.Should().ContainInOrder("dotnet", "blazor");
        post.Draft.Should().BeFalse();
    }

    [Fact]
    public async Task GetPostsAsync_PaginatesUntilItReceivesFewerThanPageSizeStories()
    {
        var firstPage = CreateStoriesResponse(
            Enumerable.Range(0, PageSize)
                .Select(index => CreateStory($"post-{index}", $"Post {index}"))
                .ToArray());
        var secondPage = CreateStoriesResponse(CreateStory("last-post", "Last post"));

        StubStoriesPage(1, firstPage);
        StubStoriesPage(2, secondPage);

        var posts = await _sut.GetPostsAsync();

        posts.Should().HaveCount(PageSize + 1);
        posts[^1].Slug.Should().Be("last-post");
        _server.LogEntries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPostsAsync_IncludesContentTypeAndPaginationParameters()
    {
        StubStories(CreateStoriesResponse());

        await _sut.GetPostsAsync();

        var url = _server.LogEntries.Single().RequestMessage!.Url;

        url.Should().Contain("content_type=post");
        url.Should().Contain($"per_page={PageSize}");
        url.Should().Contain("page=1");
        url.Should().Contain("version=published");
        url.Should().Contain($"token={PublicToken}");
    }

    [Fact]
    public async Task GetPostsAsync_WhenDraftsAreEnabled_UsesTheDraftVersionAndPreviewToken()
    {
        var sut = new StoryblokContentProvider(
            _httpClient,
            new StoryblokSettings(_server.Url!, PublicToken, PreviewToken, ShowDrafts: true));
        StubStories(CreateStoriesResponse());

        await sut.GetPostsAsync();

        var url = _server.LogEntries.Single().RequestMessage!.Url;

        url.Should().Contain("version=draft");
        url.Should().Contain($"token={PreviewToken}");
    }

    [Fact]
    public async Task GetPostsAsync_WhenPreviewIsRequested_UsesTheDraftVersionAndPreviewToken()
    {
        var sut = new StoryblokContentProvider(
            _httpClient,
            new StoryblokSettings(_server.Url!, PublicToken, PreviewToken, ShowDrafts: false));
        StubStories(CreateStoriesResponse());

        await sut.GetPostsAsync(preview: true);

        var url = _server.LogEntries.Single().RequestMessage!.Url;

        url.Should().Contain("version=draft");
        url.Should().Contain($"token={PreviewToken}");
    }

    [Fact]
    public async Task GetPostsAsync_WhenTheTokenIsMissing_ThrowsInvalidOperationException()
    {
        var sut = new StoryblokContentProvider(
            _httpClient,
            new StoryblokSettings(_server.Url!, string.Empty, string.Empty, ShowDrafts: false));

        var act = () => sut.GetPostsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetPostsAsync_WhenUpstreamFails_ThrowsHttpRequestException()
    {
        _server
            .Given(Request.Create().WithPath("/v2/cdn/stories").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var act = () => _sut.GetPostsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPostsAsync_WhenTheResponseHasNoStories_ThrowsInvalidOperationException()
    {
        StubStories("""{ "stories": null }""");

        var act = () => _sut.GetPostsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetPostAsync_MapsPostFromStoryblokResponse()
    {
        StubStory(
            "/v2/cdn/stories/posts/first-post",
            CreateStoryResponse(CreateStory("first-post", "First post")));

        var post = await _sut.GetPostAsync("first-post");

        post.Should().NotBeNull();
        post!.Title.Should().Be("First post");
        post.Tags.Should().ContainSingle().Which.Should().Be("dotnet");
    }

    [Fact]
    public async Task GetPostAsync_WhenTheStoryIsMissing_ReturnsNull()
    {
        StubStory("/v2/cdn/stories/posts/missing", string.Empty, HttpStatusCode.NotFound);

        var post = await _sut.GetPostAsync("missing");

        post.Should().BeNull();
    }

    [Fact]
    public async Task GetPageAsync_MapsMarkdownFromStoryblokResponse()
    {
        var page = new
        {
            name = "About",
            slug = "about",
            full_slug = "pages/about",
            content = new
            {
                component = "page",
                title = "About",
                body = "# About",
            },
            published_at = "2024-01-01T00:00:00.000Z",
        };

        StubStory("/v2/cdn/stories/pages/about", CreateStoryResponse(page));

        var result = await _sut.GetPageAsync("about");

        result.Should().NotBeNull();
        result!.Text.Should().Be("# About");
    }

    [Fact]
    public async Task GetPageAsync_WhenTheStoryIsMissing_ReturnsNull()
    {
        StubStory("/v2/cdn/stories/pages/missing", string.Empty, HttpStatusCode.NotFound);

        var page = await _sut.GetPageAsync("missing");

        page.Should().BeNull();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _server.Dispose();
    }

    private static string CreateStoriesResponse(params object[] stories)
    {
        return JsonSerializer.Serialize(new { stories });
    }

    private static string CreateStoryResponse(object story)
    {
        return JsonSerializer.Serialize(new { story });
    }

    private static object CreateStory(
        string slug,
        string title,
        string body = "Post body",
        string? cover = "https://example.com/cover.png",
        bool published = true,
        string[]? tags = null)
    {
        object? coverValue = cover is null
            ? null
            : new { filename = cover, alt = "Cover" };

        return new
        {
            name = title,
            slug,
            full_slug = $"posts/{slug}",
            content = new
            {
                component = "post",
                title,
                body,
                description = "A brief",
                cover = coverValue,
            },
            tag_list = tags ?? ["dotnet"],
            published_at = published ? "2024-04-01T10:00:00.000Z" : (string?)null,
            first_published_at = "2024-04-01T10:00:00.000Z",
            created_at = "2024-04-01T10:00:00.000Z",
        };
    }

    private void StubStories(string body)
    {
        _server
            .Given(Request.Create().WithPath("/v2/cdn/stories").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubStoriesPage(int page, string body)
    {
        _server
            .Given(Request.Create().WithPath("/v2/cdn/stories").UsingGet().WithParam("page", page.ToString()))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubStory(string path, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }
}
