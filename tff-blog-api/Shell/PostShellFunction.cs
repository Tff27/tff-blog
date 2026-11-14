#nullable enable annotations

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;

namespace Tff.Blog.Api.Shell;

/// <summary>
/// Serves the SPA shell for <c>/post/{slug}</c> so link unfurlers can read per-post metadata from
/// the HTML. Browsers get the untouched shell, and every failure path falls back to it as well, so
/// this endpoint can never break a page — in the worst case it behaves exactly like the static
/// file it replaces.
/// </summary>
/// <remarks>
/// The slug comes from the <c>x-ms-original-url</c> header Static Web Apps adds when it proxies a
/// rewritten request, then from the request path, then from a <c>?slug=</c> query parameter for
/// direct calls. When none yields a slug the plain shell is served, which is why the wiring has to
/// be verified on a deployment.
/// </remarks>
public class PostShellFunction
{
    private const string CacheControlValue = "public, max-age=300, stale-while-revalidate=60";
    private const string ContentType = "text/html";
    private const string OriginalUrlHeader = "x-ms-original-url";
    private const string SlugQueryKey = "slug";
    private const string PostPathSegment = "post";

    private readonly ILogger<PostShellFunction> _logger;
    private readonly IContentProvider _contentProvider;
    private readonly FeedSettings _feedSettings;

    public PostShellFunction(
        ILogger<PostShellFunction> logger,
        IContentProvider contentProvider,
        FeedSettings feedSettings)
    {
        _logger = logger;
        _contentProvider = contentProvider;
        _feedSettings = feedSettings;
    }

    [Function("PostShell")]
    public async Task<HttpResponseData> RunPostShellAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "post-shell")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var slug = ResolveSlug(req);

        if (slug is null || !LinkUnfurlers.IsMatch(GetUserAgent(req)))
        {
            return await WriteShellAsync(req, AppShell.Html, cancellationToken);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var post = await _contentProvider.GetPostAsync(slug, preview: false, cancellationToken);

            if (post is null)
            {
                _logger.LogInformation("No published post for {Slug}; serving the plain shell.", slug);

                return await WriteShellAsync(req, AppShell.Html, cancellationToken);
            }

            var payload = PostShellBuilder.Build(AppShell.Html, post, _feedSettings.SiteUrl);
            var response = await ConditionalResponse.WriteAsync(
                req,
                payload,
                ContentType,
                CacheControlValue,
                cancellationToken,
                ("Vary", LinkUnfurlers.UserAgentHeader));

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Served the post shell for {Slug}.", slug);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building the post shell for {Slug}; serving the plain shell.", slug);

            return await WriteShellAsync(req, AppShell.Html, cancellationToken);
        }
    }

    private static async Task<HttpResponseData> WriteShellAsync(
        HttpRequestData request,
        string payload,
        CancellationToken cancellationToken)
    {
        var response = await ConditionalResponse.WriteAsync(
            request,
            payload,
            ContentType,
            CacheControlValue,
            cancellationToken,
            ("Vary", LinkUnfurlers.UserAgentHeader));

        return response;
    }

    /// <summary>
    /// Resolves the requested post slug, preferring the path the browser asked for over the
    /// rewritten one this function actually receives. Static Web Apps reports the original path in
    /// <c>x-ms-original-url</c>; the URL and query-string checks are fallbacks for direct calls and
    /// for hosts that forward the original path unchanged.
    /// </summary>
    internal static string? ResolveSlug(HttpRequestData request)
    {
        return SlugFromOriginalUrl(request)
            ?? SlugFromPath(request.Url.AbsolutePath)
            ?? GetQueryValue(request.Url, SlugQueryKey);
    }

    internal static string? SlugFromOriginalUrl(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues(OriginalUrlHeader, out var values))
        {
            return null;
        }

        var originalUrl = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return null;
        }

        return Uri.TryCreate(originalUrl, UriKind.Absolute, out var absolute)
            ? SlugFromPath(absolute.AbsolutePath)
            : SlugFromPath(originalUrl.Split('?', '#')[0]);
    }

    internal static string? SlugFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 2 || !segments[0].Equals(PostPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var slug = Uri.UnescapeDataString(segments[1]);

        return string.IsNullOrWhiteSpace(slug) ? null : slug;
    }

    internal static string? GetUserAgent(HttpRequestData request)
    {
        return request.Headers.TryGetValues(LinkUnfurlers.UserAgentHeader, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static string? GetQueryValue(Uri url, string key)
    {
        foreach (var pair in url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var name = separator < 0 ? pair : pair[..separator];

            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            var decoded = Uri.UnescapeDataString(value.Replace('+', ' '));

            return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
        }

        return null;
    }
}
