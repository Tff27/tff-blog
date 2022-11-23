using System.Collections.Generic;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.ViewModels;

public class PostListViewModel
{
    public PostListViewModel()
    {
        Posts = new List<PostModel>();
    }

    public IEnumerable<PostModel> Posts { get; set; }
}
