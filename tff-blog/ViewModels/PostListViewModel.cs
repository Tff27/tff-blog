namespace Tff.Blog.Client.ViewModels;

public class PostListViewModel
{
    public required IReadOnlyList<PostViewModel> Posts { get; set; } = [];
}
