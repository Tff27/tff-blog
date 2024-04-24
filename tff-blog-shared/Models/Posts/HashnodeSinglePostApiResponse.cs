namespace Tff.Blog.Shared.Models.Posts
{
    public class HashnodeSinglePostApiResponse
    {
        public SinglePostPublication Publication { get; set; }
    }

    public class SinglePostPublication
    {
        public Post Post { get; set; }
    }

    public class Post
    {
        public string Slug { get; set; }
        public List<Tag> Tags { get; set; }
        public string Title { get; set; }
        public DateTime PublishedAt { get; set; }
        public CoverImage CoverImage { get; set; }
        public string Brief { get; set; }
        public string Url { get; set; }
        public Content Content { get; set; }
        public int ReadTimeInMinutes { get; set; }
    }
}
