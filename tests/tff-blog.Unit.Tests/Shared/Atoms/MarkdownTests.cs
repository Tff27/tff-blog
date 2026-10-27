using AwesomeAssertions;
using Bunit;
using Ganss.Xss;
using Microsoft.Extensions.DependencyInjection;
using Tff.Blog.Client.Shared.Atoms;

namespace Tff.Blog.Client.Unit.Tests.Shared.Atoms;

public class MarkdownTests : BunitContext
{
    public MarkdownTests()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        Services.AddSingleton<IHtmlSanitizer>(sanitizer);
    }

    [Fact]
    public void Markdown_RendersMarkdownAsHtml()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, "# Hello"));

        cut.Markup.Should().Contain("<h1").And.Contain("Hello");
    }

    [Fact]
    public void Markdown_RendersInlineFormatting()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, "This is **bold** and `code`."));

        cut.Markup.Should().Contain("<strong>bold</strong>").And.Contain("<code>code</code>");
    }

    [Fact]
    public void Markdown_RemovesUnsafeHtml()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, "<script>alert('xss')</script>\n\nSafe text"));

        cut.Markup.Should().NotContain("<script").And.Contain("Safe text");
    }

    [Fact]
    public void Markdown_KeepsAllowedClassAttribute()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, "<div class=\"note\">Note</div>"));

        cut.Markup.Should().Contain("class=\"note\"");
    }

    [Fact]
    public void Markdown_WithEmptyContent_RendersOnlyTheStyleBlock()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, string.Empty));

        cut.Markup.Should().NotContain("<p>");
    }
}
