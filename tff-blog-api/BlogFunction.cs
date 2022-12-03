using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Text;
using Tff.Blog.Shared.Models;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Shared.Converters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Azure.Core.Serialization;
using System.Threading;

namespace Tff.Blog.Api;

public class BlogFunction
{
    private const string SucessMessage = "Success retrieving info for {0}";
    private const string ErrorMessage = "Error fetching info: {0}";
    private readonly ILogger<BlogFunction> _logger;

    private static readonly string appName = "tff-blog";
    private static readonly string repoOwner = "tff27";

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

            var repoName = Settings.GetRepoName();
            var repoPostsPath = Settings.GetRepoPostsPath();

            var queryString = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            string postName = queryString["postName"];
            string sortField = queryString["sortField"];
            string sortOrder = queryString["sortOrder"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic parsedBody = string.IsNullOrEmpty(requestBody) ? null : JsonSerializer.Deserialize<dynamic>(requestBody, JsonSettings.Options);
            postName ??= parsedBody?.postName;
            sortField ??= parsedBody?.sortField;
            sortOrder ??= parsedBody?.sortOrder;

            _logger.LogInformation($"Retrieve info for {postName ?? "all posts"}");

            var github = new GitHubClient(new ProductHeaderValue(appName));
            var tokenAuth = new Credentials(Settings.GetGitToken());
            github.Credentials = tokenAuth;

            var docs = await github
                .Repository
                .Content
                .GetAllContents(repoOwner, repoName, repoPostsPath);

            var postList = new List<PostModel>();

            if (string.IsNullOrEmpty(postName))
            {
                foreach (var post in docs)
                {
                    postList.Add(MarkdownToModelConverter.CreateModelFromMarkdown<PostModel>(Encoding.Default.GetString(await github.Repository.Content.GetRawContent(repoOwner, repoName, post.Path))));
                }
            }
            else
            {
                postList.Add(MarkdownToModelConverter.CreateModelFromMarkdown<PostModel>(Encoding.Default.GetString(await github.Repository.Content.GetRawContent(repoOwner, repoName, $"{repoPostsPath}/{postName}.md"))));
            }

            if (!Settings.GetShowDrafts())
            {
                postList.RemoveAll(post => post.Draft);
            }

            if (postList.Count > 1)
            {
                if (sortOrder != null)
                {
                    if (!(string.Equals(sortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(sortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _logger.LogWarning($"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");

                        return await CreateResponseAsync(req, HttpStatusCode.BadRequest, $"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");
                    }

                    postList = SortPostList(postList, sortField, sortOrder);
                }
                else
                {
                    postList = SortPostList(postList, "Date", "Descending");
                }
            }

            _logger.LogInformation(SucessMessage, postName ?? "all posts");

            return await CreateResponseAsync(req, HttpStatusCode.OK, postList);
        }
        catch (ArgumentException argumentException)
        {
            _logger.LogError(ErrorMessage, argumentException.Message);

            return await CreateResponseAsync(req, HttpStatusCode.BadRequest, argumentException.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ErrorMessage, ex.Message);

            return await CreateResponseAsync(req, HttpStatusCode.BadRequest);
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

    private static async Task<HttpResponseData> CreateResponseAsync(HttpRequestData req, HttpStatusCode httpStatusCode, List<PostModel> responseMessage)
    {
        var response = req.CreateResponse(httpStatusCode);
        await response.WriteAsJsonAsync(responseMessage, serializer: new JsonObjectSerializer(JsonSettings.Options), httpStatusCode);

        return response;
    }

    private static async Task<HttpResponseData> CreateResponseAsync(HttpRequestData req, HttpStatusCode httpStatusCode, string responseMessage = "An unexpected error occurred")
    {
        var response = req.CreateResponse(httpStatusCode);
        await response.WriteAsJsonAsync(responseMessage, httpStatusCode);

        return response;
    }
}
