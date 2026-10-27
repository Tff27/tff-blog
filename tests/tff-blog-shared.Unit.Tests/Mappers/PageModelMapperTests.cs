using AwesomeAssertions;
using Tff.Blog.Shared.Mappers;
using Tff.Blog.Shared.Models;
using Tff.Blog.Shared.Models.Pages;

namespace Tff.Blog.Shared.Unit.Tests.Mappers;

public class PageModelMapperTests
{
    [Fact]
    public void MapToPageModel_MapsMarkdownText()
    {
        var staticPage = new StaticPage
        {
            Slug = "about",
            Content = new Content { Markdown = "# About\n\nHello" },
        };

        PageModel model = PageModelMapper.MapToPageModel(staticPage);

        model.Text.Should().Be("# About\n\nHello");
    }
}
