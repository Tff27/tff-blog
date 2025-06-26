using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.ViewModels;

public class PostListViewModel
{
    public required IEnumerable<PostModel> Posts { get; set; } = [];
}
