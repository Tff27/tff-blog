using Tff.Blog.Shared.Models.Pages;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Shared.Mappers
{
    public class PageModelMapper
    {
        public static PageModel MapToPageModel(StaticPage staticPage)
        {
            return new PageModel()
            {
                Text = staticPage.Content.Markdown
            };
        }
    }
}
