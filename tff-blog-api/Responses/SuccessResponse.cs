using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Responses
{
    internal static class SuccessResponse
    {
        internal static async Task<HttpResponseData> CreateResponseAsync(
            HttpRequestData req,
            HttpStatusCode httpStatusCode,
            List<PostModel> posts,
            bool isPreview = false,
            CancellationToken cancellationToken = default)
        {
            var response = req.CreateResponse(httpStatusCode);
            await response.WriteAsJsonAsync(posts, serializer: new JsonObjectSerializer(JsonSettings.Options), cancellationToken);

            if (isPreview)
            {
                response.Headers.Add("Cache-Control", "no-store");
            }

            return response;
        }
    }
}