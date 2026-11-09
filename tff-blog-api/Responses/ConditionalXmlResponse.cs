#nullable enable annotations

using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Tff.Blog.Api.Responses;

/// <summary>
/// Response plumbing shared by the XML endpoints (feeds and sitemap): hashes the payload into an
/// ETag, answers matching conditional requests with <c>304</c>, and writes failures uncached.
/// </summary>
internal static class ConditionalXmlResponse
{
    internal const string IfNoneMatchHeader = "If-None-Match";
    internal const string NoCacheValue = "no-store";

    /// <summary>
    /// Writes <paramref name="payload"/> as <c>200 OK</c> with an ETag, or as <c>304 Not Modified</c>
    /// when the request already carries the current ETag.
    /// </summary>
    internal static async Task<HttpResponseData> WriteAsync(
        HttpRequestData request,
        string payload,
        string contentType,
        string cacheControl,
        CancellationToken cancellationToken)
    {
        var etag = CreateETag(payload);

        if (MatchesETag(GetIfNoneMatch(request.Headers), etag))
        {
            var notModified = request.CreateResponse(HttpStatusCode.NotModified);
            notModified.Headers.Add("Cache-Control", cacheControl);
            notModified.Headers.Add("ETag", etag);

            return notModified;
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", $"{contentType}; charset=utf-8");
        response.Headers.Add("Cache-Control", cacheControl);
        response.Headers.Add("ETag", etag);

        await response.WriteStringAsync(payload, cancellationToken);

        return response;
    }

    /// <summary>
    /// Writes an uncached <c>500</c> with a short human-readable message.
    /// </summary>
    internal static async Task<HttpResponseData> WriteFailureAsync(
        HttpRequestData request,
        string message,
        CancellationToken cancellationToken)
    {
        var failure = request.CreateResponse(HttpStatusCode.InternalServerError);
        failure.Headers.Add("Cache-Control", NoCacheValue);
        await failure.WriteStringAsync(message, cancellationToken);

        return failure;
    }

    internal static string? GetIfNoneMatch(HttpHeadersCollection headers)
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

    internal static string CreateETag(string payload)
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
