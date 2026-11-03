using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Tff.Blog.Api.Responses
{
    internal static class ErrorResponse
    {
        internal static async Task<HttpResponseData> CreateResponseAsync(
            HttpRequestData req,
            HttpStatusCode httpStatusCode,
            string responseMessage = "An unexpected error occurred",
            CancellationToken cancellationToken = default)
        {
            var response = req.CreateResponse(httpStatusCode);
            await response.WriteAsJsonAsync(responseMessage, cancellationToken);

            return response;
        }
    }
}
