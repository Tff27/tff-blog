using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Text;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api
{
    public static class BlogFunction
    {
        static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private static readonly string appName = "tff-blog";
        private static readonly string repoOwner = "tff27";
        private static readonly string repoName = "demo-netlify-blazor";
        private static readonly string repoPostsPath = "wwwroot/posts";

        [FunctionName("BlogPosts")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string postName = req.Query["postName"];
                string sortField = req.Query["sortField"];
                string sortOrder = req.Query["sortOrder"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic parsedBody = string.IsNullOrEmpty(requestBody) ? null : JsonSerializer.Deserialize<dynamic>(requestBody, Options);
                postName ??= parsedBody?.postName;
                sortField ??= parsedBody?.sortField;
                sortOrder ??= parsedBody?.sortOrder;

                log.LogInformation($"Retrieve info for {postName ?? "all posts"}");

                var github = new GitHubClient(new ProductHeaderValue(appName));

                var docs = await github
                    .Repository
                    .Content
                    .GetAllContents(repoOwner, repoName, repoPostsPath);

                var postList = new List<PostModel>();

                if (string.IsNullOrEmpty(postName))
                {
                    foreach (var post in docs)
                    {
                        postList.Add(CreateMetadataForMdFile(Encoding.Default.GetString(await github.Repository.Content.GetRawContent(repoOwner, repoName, post.Path))));
                    }
                }
                else
                {
                    postList.Add(CreateMetadataForMdFile(Encoding.Default.GetString(await github.Repository.Content.GetRawContent(repoOwner, repoName, $"{repoPostsPath}/{postName}.md"))));
                }

                if (sortOrder != null)
                {
                    if (!(string.Equals(sortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(sortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        log.LogWarning($"The sort order \"{ sortOrder}\" is invalid, please use Ascending/Descending.");
                        return new BadRequestObjectResult($"The sort order \"{sortOrder}\" is invalid, please use Ascending/Descending.");
                    }

                    SortMetadata(ref postList, sortField, sortOrder);
                }

                var responseMessage = JsonSerializer.Serialize(postList, Options);

                log.LogInformation($"Success retrieving info for {postName ?? "all posts"}");
                return new OkObjectResult(responseMessage);
            }
            catch (ArgumentException argumentException)
            {
                log.LogError($"Error fetching info: {argumentException.Message}");
                return new BadRequestObjectResult(argumentException.Message);
            }
            catch (Exception)
            {
                log.LogError($"Error fetching info");
                return new BadRequestResult();
            }
        }

        static PostModel CreateMetadataForMdFile(string markdown)
        {
            var YamlDeserializer = new DeserializerBuilder()
              .WithNamingConvention(CamelCaseNamingConvention.Instance)
              .IgnoreUnmatchedProperties()
              .Build();

            var expression = "(?:---\\n)(?<frontmatter>[\\s\\S]*?)(?:---\\n)";

            var result = Regex.Match(markdown, expression).Groups.GetValueOrDefault("frontmatter").Value;
            var post = YamlDeserializer.Deserialize<PostModel>(result);

            Regex regex = new(expression);
            post.Text = (regex.Replace(markdown, string.Empty)).Trim();

            return post;
        }

        private static void SortMetadata(ref List<PostModel> postList, string SortField, string SortOrder)
        {
            try
            {
                if (string.Equals(SortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase))
                {
                    postList = postList.OrderBy(post => post.GetType().GetProperty(SortField)).ToList();
                }
                else if (string.Equals(SortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase))
                {
                    postList = postList.OrderByDescending(post => post.GetType().GetProperty(SortField)).ToList();
                }
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException(message: $"The sort field \"{SortField}\" doesn't exists on the current object.");
            }
        }
    }
}
