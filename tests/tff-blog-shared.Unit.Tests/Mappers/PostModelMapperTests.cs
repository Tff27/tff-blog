using AwesomeAssertions;
using Tff.Blog.Shared.Mappers;
using Tff.Blog.Shared.Models;
using Tff.Blog.Shared.Models.Posts;

namespace Tff.Blog.Shared.Unit.Tests.Mappers;

public class PostModelMapperTests
{
    [Fact]
    public void MapToBlogPostModel_FromPostNode_MapsAllFields()
    {
        var node = CreatePostNode();

        var model = PostModelMapper.MapToBlogPostModel(node);

        model.Title.Should().Be("Post title");
        model.Description.Should().Be("Brief");
        model.Date.Should().Be(new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc));
        model.Tags.Should().ContainInOrder("dotnet", "blazor");
        model.Slug.Should().Be("post-slug");
        model.Image.Should().Be("https://example.com/cover.png");
        model.ReadTimeInMinutes.Should().Be(7);
        model.Text.Should().Be("## Heading");
    }

    [Fact]
    public void MapToBlogPostModel_FromPostNode_RemovesAlignAttributes()
    {
        var node = CreatePostNode();
        node.Content.Markdown = "<img src=\"cover.png\" align=\"center\" />\nBody";

        var model = PostModelMapper.MapToBlogPostModel(node);

        model.Text.Should().NotContain("align=");
        model.Text.Should().Contain("<img src=\"cover.png\"");
    }

    [Fact]
    public void MapToBlogPostModel_FromPostNode_HandlesNullOptionalFields()
    {
        var node = CreatePostNode();
        node.CoverImage = null!;
        node.Tags = null!;

        var model = PostModelMapper.MapToBlogPostModel(node);

        model.Image.Should().BeNull();
        model.Tags.Should().BeEmpty();
    }

    [Fact]
    public void MapToBlogPostModel_FromPost_ReturnsNull_WhenPostIsNull()
    {
        var model = PostModelMapper.MapToBlogPostModel((Post)null!);

        model.Should().BeNull();
    }

    [Fact]
    public void MapToBlogPostModel_FromPost_MapsAllFields()
    {
        var post = new Post
        {
            Slug = "single-slug",
            Title = "Single title",
            PublishedAt = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            CoverImage = new CoverImage { Url = "https://example.com/single.png" },
            Brief = "Single brief",
            Url = "https://example.com/single",
            Content = new Content { Markdown = "Single body" },
            ReadTimeInMinutes = 3,
            Tags = new List<Tag> { new() { Name = "api" } },
        };

        var model = PostModelMapper.MapToBlogPostModel(post);

        model.Should().NotBeNull();
        model!.Title.Should().Be("Single title");
        model.Description.Should().Be("Single brief");
        model.Date.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        model.Tags.Should().ContainSingle().Which.Should().Be("api");
        model.Slug.Should().Be("single-slug");
        model.Image.Should().Be("https://example.com/single.png");
        model.ReadTimeInMinutes.Should().Be(3);
        model.Text.Should().Be("Single body");
    }

    private static PostNode CreatePostNode()
    {
        return new PostNode
        {
            Slug = "post-slug",
            Title = "Post title",
            PublishedAt = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            CoverImage = new CoverImage { Url = "https://example.com/cover.png" },
            Brief = "Brief",
            Url = "https://example.com/post",
            Content = new Content { Markdown = "## Heading" },
            ReadTimeInMinutes = 7,
            Tags = new List<Tag>
            {
                new() { Name = "dotnet" },
                new() { Name = "blazor" },
            },
        };
    }
}
