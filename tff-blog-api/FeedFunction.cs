#nullable enable annotations

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tff.Blog.Api.Feeds;
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
    private const string NoCacheValue = "no-store";
    private const string IfNoneMatchHeader = "If-None-Match";

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
            var etag = CreateETag(payload);

            if (MatchesETag(GetIfNoneMatch(req.Headers), etag))
            {
                var notModified = req.CreateResponse(HttpStatusCode.NotModified);
                notModified.Headers.Add("Cache-Control", CacheControlValue);
                notModified.Headers.Add("ETag", etag);

                return notModified;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", $"{contentType}; charset=utf-8");
            response.Headers.Add("Cache-Control", CacheControlValue);
            response.Headers.Add("ETag", etag);

            await response.WriteStringAsync(payload, cancellationToken);

            _logger.LogInformation("Served {FeedPath} with {PostCount} posts.", feedPath, posts.Count);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating the feed {FeedPath}: {Message}", feedPath, ex.Message);

            var failure = req.CreateResponse(HttpStatusCode.InternalServerError);
            failure.Headers.Add("Cache-Control", NoCacheValue);
            await failure.WriteStringAsync("The feed is temporarily unavailable.", cancellationToken);

            return failure;
        }
    }

    private static string? GetIfNoneMatch(HttpHeadersCollection headers)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, IfNoneMatchHeader, StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(",", header.Value);
            }
        }

        return null;
    }

    private static string CreateETag(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    internal static bool MatchesETag(string? ifNoneMatch, string etag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch))
        {
            return false;
        }

        foreach (var candidate in ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (candidate == "*")
            {
                return true;
            }

            var normalized = candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase)
                ? candidate[2..].Trim()
                : candidate;

            if (string.Equals(normalized, etag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
