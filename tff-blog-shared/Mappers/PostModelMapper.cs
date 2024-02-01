using Tff.Blog.Shared.Models;
using Tff.Blog.Shared.Models.Posts;

namespace Tff.Blog.Shared.Mappers
{
    public class PostModelMapper
    {
        public static PostModel MapToBlogPostModel(PostNode node)
        {
            return new PostModel()
            {
                Title = node.Title,
                Description = node.Brief,
                Text = node.Content.Markdown,
                Date = node.PublishedAt,
                //TODO: refactor tags field to be a list of string - Tags = node.Tags?.Select(tag => tag.Name) ?? new List<string>(),
                Slug = node.Slug,
                Image = node.CoverImage?.Url,
                ReadTimeInMinutes = node.ReadTimeInMinutes,
            };
        }

        public static PostModel MapToBlogPostModel(Post post)
        {
            return new PostModel()
            {
                Title = post.Title,
                Description = post.Brief,
                Text = post.Content.Markdown,
                Date = post.PublishedAt,
                //TODO: refactor tags field to be a list of string - Tags = post.Tags?.Select(tag => tag.Name) ?? new List<string>(),
                Slug = post.Slug,
                Image = post.CoverImage?.Url,
                ReadTimeInMinutes = post.ReadTimeInMinutes,
            };
        }
    }
}
