using System.Collections.Generic;

namespace tffBlog.ViewModels
{
    public class PostListViewModel
    {
        public PostListViewModel()
        {
            Posts = new List<PostViewModel>();
        }

        public IEnumerable<PostViewModel> Posts { get; set; }
    }
}
