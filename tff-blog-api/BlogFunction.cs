using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Requests;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api;

public class BlogFunction
{
    private const string SuccessMessage = "Success retrieving info for {Info}";
    private const string ErrorMessage = "Error fetching info: {Message}";
    private readonly ILogger<BlogFunction> _logger;
    private readonly IContentProvider _contentProvider;
    private readonly PreviewSettings _previewSettings;

    public BlogFunction(ILogger<BlogFunction> logger, IContentProvider contentProvider, PreviewSettings previewSettings)
    {
        _logger = logger;
        _contentProvider = contentProvider;
        _previewSettings = previewSettings;
    }

    [Function("BlogPosts")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var queryString = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            string postName = queryString["postName"];
            string sortField = queryString["sortField"];
            string sortOrder = queryString["sortOrder"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var parsedBody = string.IsNullOrEmpty(requestBody)
                ? null
                : JsonSerializer.Deserialize<BlogRequest>(requestBody, JsonSettings.Options);
            postName ??= parsedBody?.PostName;
            sortField ??= parsedBody?.SortField;
            sortOrder ??= parsedBody?.SortOrder;

            if (!TryResolvePreview(queryString, out var isPreview))
            {
                _logger.LogWarning("Rejected a request with an invalid preview secret.");

                return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.Unauthorized, cancellationToken, "The preview secret is invalid.");
            }

            _logger.LogInformation("Retrieve info for {PostName}", postName ?? "all posts");

            if (string.IsNullOrEmpty(postName))
            {
                if (!PostSorter.IsValidSortOrder(sortOrder))
                {
                    _logger.LogWarning("The sort order \"{SortOrder}\" is invalid, please use Ascending/Descending.", sortOrder);

                    return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken, $"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");
                }

                if (!string.IsNullOrEmpty(sortField) && !PostSorter.IsValidSortField(sortField))
                {
                    _logger.LogWarning("The sort field \"{SortField}\" is invalid.", sortField);

                    return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken, $"The sort field \"{sortField}\" is invalid.");
                }

                var posts = await _contentProvider.GetPostsAsync(cancellationToken, isPreview);

                posts = string.IsNullOrEmpty(sortOrder)
                    ? PostSorter.Sort(posts, "Date", "Descending")
                    : PostSorter.Sort(posts, sortField, sortOrder);

                _logger.LogInformation(SuccessMessage, "all posts");

                return await CreateSuccessResponseAsync(req, HttpStatusCode.OK, posts, cancellationToken, isPreview);
            }

            var post = await _contentProvider.GetPostAsync(postName, cancellationToken, isPreview);
            var matchingPosts = post is null ? new List<PostModel>() : new List<PostModel> { post };

            _logger.LogInformation(SuccessMessage, postName);

            return await CreateSuccessResponseAsync(req, HttpStatusCode.OK, matchingPosts, cancellationToken, isPreview);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException argumentException)
        {
            _logger.LogError(argumentException, ErrorMessage, argumentException.Message);

            return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken, argumentException.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessage, ex.Message);

            return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private bool TryResolvePreview(NameValueCollection queryString, out bool isPreview)
    {
        isPreview = false;
        var presentedSecret = queryString["preview"];

        if (string.IsNullOrEmpty(presentedSecret))
        {
            return true;
        }

        var expectedSecret = _previewSettings.Secret;

        if (string.IsNullOrEmpty(expectedSecret) || !FixedTimeEquals(presentedSecret, expectedSecret))
        {
            return false;
        }

        isPreview = true;

        return true;
    }

    private static bool FixedTimeEquals(string presented, string expected)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presented),
            Encoding.UTF8.GetBytes(expected));
    }

    private static async Task<HttpResponseData> CreateSuccessResponseAsync(
        HttpRequestData req,
        HttpStatusCode statusCode,
        List<PostModel> posts,
        CancellationToken cancellationToken,
        bool isPreview)
    {
        var response = await SuccessResponse.CreateResponseAsync(req, statusCode, posts, cancellationToken);

        if (isPreview)
        {
            response.Headers.Add("Cache-Control", "no-store");
        }

        return response;
    }
}
