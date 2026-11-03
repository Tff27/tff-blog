#nullable enable annotations

using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Services;

public interface IContentProvider
{
    Task<List<PostModel>> GetPostsAsync(bool preview = false, CancellationToken cancellationToken = default);

    Task<PostModel?> GetPostAsync(string slug, bool preview = false, CancellationToken cancellationToken = default);

    Task<PageModel?> GetPageAsync(string slug, bool preview = false, CancellationToken cancellationToken = default);
}
