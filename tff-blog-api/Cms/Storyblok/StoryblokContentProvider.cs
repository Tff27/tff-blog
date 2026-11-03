#nullable enable annotations

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Cms.Storyblok;

public sealed class StoryblokContentProvider : IContentProvider
{
    internal const int PageSize = 100;
    internal const int MaxPages = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly StoryblokSettings _settings;
    private readonly ILogger<StoryblokContentProvider> _logger;

    public StoryblokContentProvider(
        HttpClient httpClient,
        StoryblokSettings settings,
        ILogger<StoryblokContentProvider>? logger = null)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger ?? NullLogger<StoryblokContentProvider>.Instance;
    }

    public async Task<List<PostModel>> GetPostsAsync(bool preview = false, CancellationToken cancellationToken = default)
    {
        var stories = await GetStoriesAsync("post", preview, cancellationToken);

        return stories.Select(StoryblokMapper.MapToPostModel).ToList();
    }

    public async Task<PostModel?> GetPostAsync(string slug, bool preview = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var story = await GetStoryAsync($"posts/{slug}", preview, cancellationToken);

        return story is null ? null : StoryblokMapper.MapToPostModel(story);
    }

    public async Task<PageModel?> GetPageAsync(string slug, bool preview = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var story = await GetStoryAsync($"pages/{slug}", preview, cancellationToken);

        return story is null ? null : StoryblokMapper.MapToPageModel(story);
    }

    private async Task<List<StoryblokStory>> GetStoriesAsync(string contentType, bool preview, CancellationToken cancellationToken)
    {
        var stories = new List<StoryblokStory>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var requestUri = BuildStoriesUri(contentType, page, preview);
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return stories;
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<StoryblokStoriesResponse>(JsonOptions, cancellationToken);

            if (payload?.Stories is null)
            {
                throw new InvalidOperationException($"Storyblok returned an empty response for content type '{contentType}'.");
            }

            stories.AddRange(payload.Stories);

            if (payload.Stories.Count < PageSize)
            {
                return stories;
            }
        }

        _logger.LogWarning(
            "Storyblok pagination stopped after {MaxPages} pages for content type {ContentType}.",
            MaxPages,
            contentType);

        return stories;
    }

    private async Task<StoryblokStory?> GetStoryAsync(string fullSlug, bool preview, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(BuildStoryUri(fullSlug, preview), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StoryblokStoryResponse>(JsonOptions, cancellationToken);

        return payload?.Story;
    }

    private string BuildStoriesUri(string contentType, int page, bool preview)
    {
        return $"{Endpoint}/v2/cdn/stories" +
               $"?token={Uri.EscapeDataString(GetToken(preview))}" +
               $"&content_type={Uri.EscapeDataString(contentType)}" +
               $"&version={GetVersion(preview)}" +
               $"&per_page={PageSize}" +
               $"&page={page}";
    }

    private string BuildStoryUri(string fullSlug, bool preview)
    {
        var escapedSlug = string.Join('/', fullSlug.Split('/').Select(Uri.EscapeDataString));

        return $"{Endpoint}/v2/cdn/stories/{escapedSlug}" +
               $"?token={Uri.EscapeDataString(GetToken(preview))}" +
               $"&version={GetVersion(preview)}";
    }

    private string Endpoint => _settings.Endpoint.TrimEnd('/');

    private bool UseDraftContent(bool preview) => _settings.ShowDrafts || preview;

    private string GetToken(bool preview)
    {
        var useDrafts = UseDraftContent(preview);
        var token = useDrafts ? _settings.PreviewToken : _settings.PublicToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                useDrafts
                    ? "The Storyblok preview token is not configured."
                    : "The Storyblok public token is not configured.");
        }

        return token;
    }

    private string GetVersion(bool preview) => UseDraftContent(preview) ? "draft" : "published";
}
