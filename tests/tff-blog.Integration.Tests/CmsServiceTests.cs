using System.Net;
using AwesomeAssertions;
using GraphQL.Client.Http;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tff.Blog.Integration.Tests;

public class CmsServiceTests : IDisposable
{
    private const string PublicationId = "publication-id";
    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;
    private readonly CmsService _sut;

    public CmsServiceTests()
    {
        _server = WireMockServer.Start();
        _httpClient = new HttpClient();
        _sut = new CmsService(_httpClient, new CmsSettings(_server.Url!, PublicationId));
    }

    [Fact]
    public async Task GetPostsAsync_MapsPostsFromGraphQlResponse()
    {
        var response = """
            {
              "data": {
                "publication": {
                  "posts": {
                    "pageInfo": { "hasNextPage": false, "endCursor": null },
                    "edges": [
                      {
                        "node": {
                          "id": "post-1",
                          "slug": "first-post",
                          "title": "First post",
                          "publishedAt": "2024-04-01T10:00:00Z",
                          "coverImage": { "url": "https://example.com/cover.png" },
                          "brief": "A brief",
                          "url": "https://example.com/first-post",
                          "content": { "markdown": "## Hello" },
                          "readTimeInMinutes": 4,
                          "tags": [ { "name": "dotnet" }, { "name": "blazor" } ]
                        }
                      }
                    ]
                  }
                }
              }
            }
            """;

        StubGraphQl(response);

        var posts = await _sut.GetPostsAsync(PublicationId);

        posts.Should().ContainSingle();
        var post = posts[0];
        post.Title.Should().Be("First post");
        post.Slug.Should().Be("first-post");
        post.Description.Should().Be("A brief");
        post.Image.Should().Be("https://example.com/cover.png");
        post.Text.Should().Be("## Hello");
        post.ReadTimeInMinutes.Should().Be(4);
        post.Date.Should().Be(new DateTime(2024, 4, 1, 10, 0, 0, DateTimeKind.Utc));
        post.Tags.Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public async Task GetSinglePostsAsync_MapsPostFromGraphQlResponse()
    {
        var response = """
            {
              "data": {
                "publication": {
                  "post": {
                    "id": "post-1",
                    "slug": "first-post",
                    "title": "First post",
                    "publishedAt": "2024-04-01T10:00:00Z",
                    "coverImage": { "url": "https://example.com/cover.png" },
                    "brief": "A brief",
                    "url": "https://example.com/first-post",
                    "content": { "markdown": "Body" },
                    "readTimeInMinutes": 2,
                    "tags": [ { "name": "dotnet" } ]
                  }
                }
              }
            }
            """;

        StubGraphQl(response);

        var posts = await _sut.GetSinglePostsAsync(PublicationId, "first-post");

        posts.Should().ContainSingle();
        posts[0].Title.Should().Be("First post");
        posts[0].Tags.Should().ContainSingle().Which.Should().Be("dotnet");
    }

    [Fact]
    public async Task GetPageAsync_MapsMarkdownFromGraphQlResponse()
    {
        var response = """
            {
              "data": {
                "publication": {
                  "staticPage": {
                    "id": "page-1",
                    "slug": "about",
                    "content": { "markdown": "# About" }
                  }
                }
              }
            }
            """;

        StubGraphQl(response);

        var page = await _sut.GetPageAsync(PublicationId, "about");

        page.Text.Should().Be("# About");
    }

    [Fact]
    public async Task GetPostsAsync_WhenUpstreamFails_ThrowsGraphQlHttpRequestException()
    {
        _server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var act = () => _sut.GetPostsAsync(PublicationId);

        await act.Should().ThrowAsync<GraphQLHttpRequestException>();
    }

    [Fact]
    public async Task GetPostsAsync_SendsPublicationIdInTheQuery()
    {
        StubGraphQl("""{ "data": { "publication": { "posts": { "edges": [] } } } }""");

        await _sut.GetPostsAsync(PublicationId);

        var requests = _server.LogEntries;
        requests.Should().ContainSingle();
        requests[0].RequestMessage!.Body!.Should().Contain(PublicationId);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _httpClient.Dispose();
        _server.Dispose();
    }

    private void StubGraphQl(string body)
    {
        _server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }
}
