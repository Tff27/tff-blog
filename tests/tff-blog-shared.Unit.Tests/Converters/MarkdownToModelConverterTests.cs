using AwesomeAssertions;
using Tff.Blog.Shared.Converters;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Shared.Unit.Tests.Converters;

public class MarkdownToModelConverterTests
{
    [Fact]
    public void CreateModelFromMarkdown_ParsesYamlFrontMatter()
    {
        const string markdown = "---\nname: Tiago\nphoto: /images/photo.jpg\n---\n\nHello world";

        var model = MarkdownToModelConverter.CreateModelFromMarkdown<AuthorModel>(markdown);

        model.Name.Should().Be("Tiago");
        model.Photo.Should().Be("/images/photo.jpg");
        model.Text.Should().Be("Hello world");
    }

    [Fact]
    public void CreateModelFromMarkdown_WithoutFrontMatter_ReturnsFullText()
    {
        const string markdown = "Just **markdown** content.";

        var model = MarkdownToModelConverter.CreateModelFromMarkdown<AuthorModel>(markdown);

        model.Name.Should().BeNull();
        model.Text.Should().Be(markdown);
    }

    [Fact]
    public void CreateModelFromMarkdown_HandlesCrlfLineEndings()
    {
        const string markdown = "---\r\nname: Tiago\r\nphoto: /images/photo.jpg\r\n---\r\n\r\nBody";

        var model = MarkdownToModelConverter.CreateModelFromMarkdown<AuthorModel>(markdown);

        model.Name.Should().Be("Tiago");
        model.Text.Should().Be("Body");
    }

    [Fact]
    public void CreateModelFromMarkdown_WithEmptyInput_ReturnsEmptyText()
    {
        var model = MarkdownToModelConverter.CreateModelFromMarkdown<AuthorModel>(string.Empty);

        model.Text.Should().BeEmpty();
    }
}
