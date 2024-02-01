namespace Tff.Blog.Shared.Models.Posts
{
    public class HashnodePostListApiResponse
    {
        public PostListPublication Publication { get; set; }
    }

    public class PostListPublication
    {
        public PostsConnection Posts { get; set; }
    }

    public class PostsConnection
    {
        public PageInfo PageInfo { get; set; }
        public List<PostEdge> Edges { get; set; }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }
        public string EndCursor { get; set; }
    }

    public class PostEdge
    {
        public PostNode Node { get; set; }
    }

    public class PostNode
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public DateTime PublishedAt { get; set; }
        public CoverImage CoverImage { get; set; }
        public string Brief { get; set; }
        public string Url { get; set; }
        public Content Content { get; set; }
        public int ReadTimeInMinutes { get; set; }
        public List<Tag> Tags { get; set; }
    }
}
