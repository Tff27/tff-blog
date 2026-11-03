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
    private readonly IContentProvider _contentProvider = Substitute.For<IContentProvider>();
    private readonly ILogger<BlogFunction> _logger = Substitute.For<ILogger<BlogFunction>>();
    private readonly BlogFunction _sut;

    public BlogFunctionTests()
    {
        _sut = new BlogFunction(_logger, _contentProvider);
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortOrder_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortOrder=Sideways");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _contentProvider.DidNotReceive().GetPostsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortFieldAndNoSortOrder_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortField=Tags");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _contentProvider.DidNotReceive().GetPostsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithInvalidSortField_ReturnsBadRequest_WithoutCallingCms()
    {
        var request = CreateRequest("?sortOrder=Ascending&sortField=Tags");

        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _contentProvider.DidNotReceive().GetPostsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithoutSortParameters_ReturnsPostsSortedByDateDescending()
    {
        var posts = CreatePosts();
        _contentProvider.GetPostsAsync(Arg.Any<CancellationToken>())
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
        _contentProvider.GetPostsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var response = await _sut.RunAsync(CreateRequest("?sortOrder=Ascending&sortField=Title"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Select(post => post.Title).Should().ContainInOrder("Newest", "Oldest");
    }

    [Fact]
    public async Task RunAsync_WithPostName_ReturnsSinglePost()
    {
        var post = CreatePosts()[1];
        _contentProvider.GetPostAsync("my-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(post));

        var response = await _sut.RunAsync(CreateRequest("?postName=my-post"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().ContainSingle().Which.Slug.Should().Be("newest");
        await _contentProvider.Received(1).GetPostAsync("my-post", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithUnknownPostName_ReturnsEmptyList()
    {
        _contentProvider.GetPostAsync("missing-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PostModel?>(null));

        var response = await _sut.RunAsync(CreateRequest("?postName=missing-post"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_ReadsParametersFromJsonBody()
    {
        var posts = CreatePosts();
        _contentProvider.GetPostsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(posts));

        var request = new TestHttpRequestData("{\"sortOrder\":\"Ascending\",\"sortField\":\"Date\"}");
        var response = await _sut.RunAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadBody(response).Select(post => post.Title).Should().ContainInOrder("Oldest", "Newest");
    }

    [Fact]
    public async Task RunAsync_WhenCmsThrows_ReturnsInternalServerError()
    {
        _contentProvider.GetPostsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("CMS is down"));

        var response = await _sut.RunAsync(CreateRequest(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RunAsync_WhenCmsThrowsArgumentException_ReturnsBadRequest()
    {
        _contentProvider.GetPostsAsync(Arg.Any<CancellationToken>())
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
        await _contentProvider.DidNotReceive().GetPostsAsync(Arg.Any<CancellationToken>());
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
