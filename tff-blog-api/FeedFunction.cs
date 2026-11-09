#nullable enable annotations

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api;

/// <summary>
/// Serves the RSS 2.0 and Atom feeds. Both are anonymous because feed readers cannot send a
/// function key, and both only ever expose published posts.
/// </summary>
public class FeedFunction
{
    private const string CacheControlValue = "public, max-age=300, stale-while-revalidate=60";

    private readonly ILogger<FeedFunction> _logger;
    private readonly IContentProvider _contentProvider;
    private readonly FeedSettings _feedSettings;

    public FeedFunction(ILogger<FeedFunction> logger, IContentProvider contentProvider, FeedSettings feedSettings)
    {
        _logger = logger;
        _contentProvider = contentProvider;
        _feedSettings = feedSettings;
    }

    [Function("RssFeed")]
    public Task<HttpResponseData> RunRssAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rss.xml")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        return RunAsync(
            req,
            FeedConstants.RssPath,
            "application/rss+xml",
            RssFeedBuilder.Build,
            cancellationToken);
    }

    [Function("AtomFeed")]
    public Task<HttpResponseData> RunAtomAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "atom.xml")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        return RunAsync(
            req,
            FeedConstants.AtomPath,
            "application/atom+xml",
            AtomFeedBuilder.Build,
            cancellationToken);
    }

    private async Task<HttpResponseData> RunAsync(
        HttpRequestData req,
        string feedPath,
        string contentType,
        Func<IEnumerable<PostModel>, string, string> buildFeed,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Feeds are public, so drafts must never leak through ShowDrafts or a preview parameter.
            var posts = await _contentProvider.GetPostsAsync(preview: false, cancellationToken);
            var payload = buildFeed(posts, _feedSettings.SiteUrl);

            var response = await ConditionalXmlResponse.WriteAsync(
                req,
                payload,
                contentType,
                CacheControlValue,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Served {FeedPath} with {PostCount} posts.", feedPath, posts.Count);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating the feed {FeedPath}: {Message}", feedPath, ex.Message);

            return await ConditionalXmlResponse.WriteFailureAsync(
                req,
                "The feed is temporarily unavailable.",
                cancellationToken);
        }
    }
}
