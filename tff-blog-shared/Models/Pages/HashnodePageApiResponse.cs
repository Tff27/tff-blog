namespace Tff.Blog.Shared.Models.Pages
{
    public class HashnodePageApiResponse
    {
        public Publication Publication { get; set; }
    }

    public class Publication
    {
        public StaticPage StaticPage { get; set; }
    }

    public class StaticPage
    {
        public string Slug { get; set; }
        public Content Content { get; set; }
    }
}
