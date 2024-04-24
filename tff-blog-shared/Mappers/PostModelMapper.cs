using System.Text.RegularExpressions;
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
                Text = SanitizeStyling(node.Content.Markdown),
                Date = node.PublishedAt,
                Tags = node.Tags?.Select(tag => tag.Name) ?? new List<string>(),
                Slug = node.Slug,
                Image = node.CoverImage?.Url,
                ReadTimeInMinutes = node.ReadTimeInMinutes,
            };
        }

        public static PostModel? MapToBlogPostModel(Post post)
        {
            if (post == null)
            {
                return null;
            }

            return new PostModel()
            {
                Title = post.Title,
                Description = post.Brief,
                Text = SanitizeStyling(post.Content.Markdown),
                Date = post.PublishedAt,
                Tags = post.Tags?.Select(tag => tag.Name) ?? new List<string>(),
                Slug = post.Slug,
                Image = post.CoverImage?.Url,
                ReadTimeInMinutes = post.ReadTimeInMinutes,
            };
        }

        private static string SanitizeStyling(string markdown)
        {
            // Matches align=" followed by any characters until the next double quote.
            var pattern = @"align=""[^""]*""";
            var santizedMarkdown = Regex.Replace(markdown, pattern, string.Empty);

            return santizedMarkdown;
        }
    }
}
