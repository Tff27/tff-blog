using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tff.Blog.Shared.Converters;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Services;

public sealed class BlogContentService : IBlogContentService
{
    internal const string ApiClientName = "WebAPI";
    internal const string ContentClientName = "Client";
    public const string UnavailableMessage = "This content is unavailable right now. Please try again.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlogContentService> _logger;

    public BlogContentService(IHttpClientFactory httpClientFactory, ILogger<BlogContentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<BlogContentResult<IReadOnlyList<PostModel>>> GetPostsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetJsonAsync<List<PostModel>>(
            ApiClientName,
            string.Empty,
            "load blog posts",
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.IsNotFound
                ? BlogContentResult<IReadOnlyList<PostModel>>.NotFound()
                : BlogContentResult<IReadOnlyList<PostModel>>.Failure(result.ErrorMessage!);
        }

        return BlogContentResult<IReadOnlyList<PostModel>>.Success(result.Content!);
    }

    public async Task<BlogContentResult<PostModel>> GetPostAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var result = await GetJsonAsync<List<PostModel>>(
            ApiClientName,
            $"?postName={Uri.EscapeDataString(slug)}",
            $"load post '{slug}'",
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.IsNotFound
                ? BlogContentResult<PostModel>.NotFound()
                : BlogContentResult<PostModel>.Failure(result.ErrorMessage!);
        }

        var post = result.Content?.FirstOrDefault();

        return post is null
            ? BlogContentResult<PostModel>.NotFound()
            : BlogContentResult<PostModel>.Success(post);
    }

    public async Task<BlogContentResult<T>> GetMarkdownPageAsync<T>(
        string pageName,
        CancellationToken cancellationToken = default)
        where T : MarkdownModel, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageName);

        var result = await GetStringAsync(
            ContentClientName,
            $"pages/{Uri.EscapeDataString(pageName)}.md",
            $"load page '{pageName}'",
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.IsNotFound
                ? BlogContentResult<T>.NotFound()
                : BlogContentResult<T>.Failure(result.ErrorMessage!);
        }

        try
        {
            return BlogContentResult<T>.Success(
                MarkdownToModelConverter.CreateModelFromMarkdown<T>(result.Content!));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to parse page '{PageName}'", pageName);

            return BlogContentResult<T>.Failure(UnavailableMessage);
        }
    }

    private async Task<BlogContentResult<T>> GetJsonAsync<T>(
        string clientName,
        string relativeUrl,
        string operation,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient(clientName);
            using var response = await client.GetAsync(relativeUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return BlogContentResult<T>.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to {Operation}: request to {Url} returned {StatusCode}",
                    operation,
                    relativeUrl,
                    (int)response.StatusCode);

                return BlogContentResult<T>.Failure(UnavailableMessage);
            }

            var content = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

            return content is null
                ? BlogContentResult<T>.Failure(UnavailableMessage)
                : BlogContentResult<T>.Success(content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Timed out while trying to {Operation} from {Url}", operation, relativeUrl);

            return BlogContentResult<T>.Failure(UnavailableMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or InvalidOperationException)
        {
            _logger.LogError(exception, "Failed to {Operation} from {Url}", operation, relativeUrl);

            return BlogContentResult<T>.Failure(UnavailableMessage);
        }
    }

    private async Task<BlogContentResult<string>> GetStringAsync(
        string clientName,
        string relativeUrl,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(clientName);
            using var response = await client.GetAsync(relativeUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return BlogContentResult<string>.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to {Operation}: request to {Url} returned {StatusCode}",
                    operation,
                    relativeUrl,
                    (int)response.StatusCode);

                return BlogContentResult<string>.Failure(UnavailableMessage);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(content)
                ? BlogContentResult<string>.Failure(UnavailableMessage)
                : BlogContentResult<string>.Success(content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Timed out while trying to {Operation} from {Url}", operation, relativeUrl);

            return BlogContentResult<string>.Failure(UnavailableMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or NotSupportedException or InvalidOperationException)
        {
            _logger.LogError(exception, "Failed to {Operation} from {Url}", operation, relativeUrl);

            return BlogContentResult<string>.Failure(UnavailableMessage);
        }
    }
}
