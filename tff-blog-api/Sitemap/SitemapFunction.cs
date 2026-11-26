#nullable enable annotations

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;

namespace Tff.Blog.Api.Sitemap;

/// <summary>
/// Serves <c>sitemap.xml</c> for search engines. Anonymous because crawlers cannot send a function
/// key, and published-only because a sitemap must never advertise drafts.
/// </summary>
public class SitemapFunction
{
    private const string CacheControlValue = "public, max-age=3600, stale-while-revalidate=60";
    private const string ContentType = "application/xml";

    private readonly ILogger<SitemapFunction> _logger;
    private readonly IContentProvider _contentProvider;
    private readonly FeedSettings _feedSettings;

    public SitemapFunction(ILogger<SitemapFunction> logger, IContentProvider contentProvider, FeedSettings feedSettings)
    {
        _logger = logger;
        _contentProvider = contentProvider;
        _feedSettings = feedSettings;
    }

    [Function("Sitemap")]
    public async Task<HttpResponseData> RunSitemapAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sitemap.xml")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var posts = await _contentProvider.GetPostsAsync(preview: false, cancellationToken);
            var payload = SitemapBuilder.Build(posts, _feedSettings.SiteUrl);

            var response = await ConditionalResponse.WriteAsync(
                req,
                payload,
                ContentType,
                CacheControlValue,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Served sitemap.xml with {PostCount} posts.", posts.Count);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sitemap.xml: {Message}", ex.Message);

            return await ConditionalResponse.WriteFailureAsync(
                req,
                "The sitemap is temporarily unavailable.",
                cancellationToken);
        }
    }
}
