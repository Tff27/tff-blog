using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Tff.Blog.Api.Responses
{
    internal static class ErrorResponse
    {
        internal static async Task<HttpResponseData> CreateResponseAsync(HttpRequestData req, HttpStatusCode httpStatusCode, CancellationToken cancelationToken, string responseMessage = "An unexpected error occurred")
        {
            var response = req.CreateResponse(httpStatusCode);
            await response.WriteAsJsonAsync(responseMessage, cancelationToken);

            return response;
        }
    }
}
