#nullable enable annotations

using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Services;

public interface IContentProvider
{
    Task<List<PostModel>> GetPostsAsync(CancellationToken cancellationToken = default, bool preview = false);

    Task<PostModel?> GetPostAsync(string slug, CancellationToken cancellationToken = default, bool preview = false);

    Task<PageModel?> GetPageAsync(string slug, CancellationToken cancellationToken = default, bool preview = false);
}
