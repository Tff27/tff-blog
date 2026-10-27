using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api;

public class BlogFunction
{
    private const string SuccessMessage = "Success retrieving info for {Info}";
    private const string ErrorMessage = "Error fetching info: {Message}";
    private readonly ILogger<BlogFunction> _logger;
    private readonly ICmsService _cmsService;
    private readonly CmsSettings _settings;

    private sealed record BlogRequest(string PostName, string SortField, string SortOrder);

    public BlogFunction(ILogger<BlogFunction> logger, ICmsService cmsService, CmsSettings settings)
    {
        _logger = logger;
        _cmsService = cmsService;
        _settings = settings;
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

                var posts = await _cmsService.GetPostsAsync(_settings.PublicationId, cancellationToken);

                posts = string.IsNullOrEmpty(sortOrder)
                    ? PostSorter.Sort(posts, "Date", "Descending")
                    : PostSorter.Sort(posts, sortField, sortOrder);

                _logger.LogInformation(SuccessMessage, "all posts");

                return await SuccessResponse.CreateResponseAsync(req, HttpStatusCode.OK, posts, cancellationToken);
            }

            var post = await _cmsService.GetSinglePostsAsync(_settings.PublicationId, postName, cancellationToken);

            _logger.LogInformation(SuccessMessage, postName);

            return await SuccessResponse.CreateResponseAsync(req, HttpStatusCode.OK, post, cancellationToken);
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
}
