using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Services;
using Tff.Blog.Api.Unit.Tests.Infrastructure;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests;

public class BlogFunctionTests
{
    private const string PublicationId = "publication-id";
    private static readonly CmsSettings Settings = new("https://gql.hashnode.com", PublicationId);
    private readonly ICmsService _cmsService = Substitute.For<ICmsService>();
    private readonly ILogger<BlogFunction> _logger = Substitute.For<ILogger<BlogFunction>>();
    private readonly BlogFunction _sut;

    public BlogFunctionTests()
    {
        _sut = new BlogFunction(_logger, _cmsService, Settings);
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortOrder_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortOrder=Sideways");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _cmsService.DidNotReceive().GetPostsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortFieldAndNoSortOrder_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortField=Tags");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _cmsService.DidNotReceive().GetPostsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortField_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortOrder=Ascending&sortField=Tags");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _cmsService.DidNotReceive().GetPostsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithoutSortParameters_ReturnsPostsSortedByDateDescending()
    {
        var posts = CreatePosts();
        _cmsService.GetPostsAsync(PublicationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var response = await _sut.RunAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = ReadBody(response);
        body.Select(post => post.Title).Should().ContainInOrder("Newest", "Oldest");
    }

    [Fact]
    public async Task RunAsync_WithSortParameters_SortsPosts()
    {
        var posts = CreatePosts();
        _cmsService.GetPostsAsync(PublicationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var response = await _sut.RunAsync(CreateRequest("?sortOrder=Ascending&sortField=Title"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Select(post => post.Title).Should().ContainInOrder("Newest", "Oldest");
    }

    [Fact]
    public async Task RunAsync_WithPostName_ReturnsSinglePost()
    {
        var posts = new List<PostModel> { CreatePosts()[1] };
        _cmsService.GetSinglePostsAsync(PublicationId, "my-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var response = await _sut.RunAsync(CreateRequest("?postName=my-post"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().ContainSingle().Which.Slug.Should().Be("newest");
        await _cmsService.Received(1).GetSinglePostsAsync(PublicationId, "my-post", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReadsParametersFromJsonBody()
    {
        var posts = CreatePosts();
        _cmsService.GetPostsAsync(PublicationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var request = new TestHttpRequestData("{\"sortOrder\":\"Ascending\",\"sortField\":\"Date\"}");
        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Select(post => post.Title).Should().ContainInOrder("Oldest", "Newest");
    }

    [Fact]
    public async Task RunAsync_WhenCmsThrows_ReturnsInternalServerError()
    {
        _cmsService.GetPostsAsync(PublicationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("CMS is down"));

        var response = await _sut.RunAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RunAsync_WhenCmsThrowsArgumentException_ReturnsBadRequest()
    {
        _cmsService.GetPostsAsync(PublicationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("bad field"));

        var response = await _sut.RunAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RunAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var act = () => _sut.RunAsync(CreateRequest(), cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _cmsService.DidNotReceive().GetPostsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static TestHttpRequestData CreateRequest(string query = "")
    {
        return new TestHttpRequestData(url: $"http://localhost/api/BlogPosts{query}");
    }

    private static List<PostModel> ReadBody(Microsoft.Azure.Functions.Worker.Http.HttpResponseData response)
    {
        var testResponse = response.Should().BeOfType<TestHttpResponseData>().Subject;

        return JsonSerializer.Deserialize<List<PostModel>>(testResponse.ReadBodyAsString(), JsonSettings.Options)!;
    }

    private static List<PostModel> CreatePosts()
    {
        return new List<PostModel>
        {
            new()
            {
                Title = "Oldest",
                Slug = "oldest",
                Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Title = "Newest",
                Slug = "newest",
                Date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        };
    }
}
