using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Services;

public interface IBlogContentService
{
    Task<BlogContentResult<IReadOnlyList<PostModel>>> GetPostsAsync(CancellationToken cancellationToken = default);

    Task<BlogContentResult<PostModel>> GetPostAsync(string slug, CancellationToken cancellationToken = default);

    Task<BlogContentResult<T>> GetMarkdownPageAsync<T>(string pageName, CancellationToken cancellationToken = default)
        where T : MarkdownModel, new();
}
