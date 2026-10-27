using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Services;

public interface ICmsService
{
    Task<List<PostModel>> GetPostsAsync(string publicationId, CancellationToken cancellationToken = default);

    Task<List<PostModel>> GetSinglePostsAsync(string publicationId, string postSlug, CancellationToken cancellationToken = default);

    Task<PageModel> GetPageAsync(string publicationId, string pageSlug, CancellationToken cancellationToken = default);
}
