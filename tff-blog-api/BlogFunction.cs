using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Responses;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api;

public class BlogFunction
{
    private const string SucessMessage = "Success retrieving info for {0}";
    private const string ErrorMessage = "Error fetching info: {0}";
    private readonly ILogger<BlogFunction> _logger;

    public BlogFunction(ILogger<BlogFunction> logger)
    {
        _logger = logger;
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
            dynamic parsedBody = string.IsNullOrEmpty(requestBody) ? null : JsonSerializer.Deserialize<dynamic>(requestBody, JsonSettings.Options);
            postName ??= parsedBody?.postName;
            sortField ??= parsedBody?.sortField;
            sortOrder ??= parsedBody?.sortOrder;

            _logger.LogInformation($"Retrieve info for {postName ?? "all posts"}");

            if (string.IsNullOrEmpty(postName))
            {
                var posts = await CmsService.GetPostsAsync(Settings.GetHashnodePublicationId());

                if (sortOrder != null)
                {
                    if (!(string.Equals(sortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(sortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _logger.LogWarning($"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");

                        return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken, $"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");
                    }

                    posts = SortPostList(posts, sortField, sortOrder);
                }
                else
                {
                    posts = SortPostList(posts, "Date", "Descending");
                }

                _logger.LogInformation(SucessMessage, "all posts");

                return await SuccessResponse.CreateResponseAsync(req, HttpStatusCode.OK, posts, cancellationToken);
            }

            var post = await CmsService.GetSinglePostsAsync(Settings.GetHashnodePublicationId(), postName);

            _logger.LogInformation(SucessMessage, postName);

            return await SuccessResponse.CreateResponseAsync(req, HttpStatusCode.OK, post, cancellationToken);
        }
        catch (ArgumentException argumentException)
        {
            _logger.LogError(ErrorMessage, argumentException.Message);

            return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken, argumentException.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ErrorMessage, ex.Message);

            return await ErrorResponse.CreateResponseAsync(req, HttpStatusCode.BadRequest, cancellationToken);
        }
    }

    private static List<PostModel> SortPostList(List<PostModel> postList, string SortField, string SortOrder)
    {
        try
        {
            if (string.Equals(SortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase))
            {
                return postList.OrderBy(post => post.GetType().GetProperty(SortField)?.GetValue(post)).ToList();
            }
            else if (string.Equals(SortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase))
            {
                return postList.OrderByDescending(post => post.GetType().GetProperty(SortField)?.GetValue(post)).ToList();
            }

            return postList;
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException(message: $"The sort field \"{SortField}\" doesn't exists on the current object.");
        }
    }
}
