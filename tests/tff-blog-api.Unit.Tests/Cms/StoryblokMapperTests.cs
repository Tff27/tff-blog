using AwesomeAssertions;
using Tff.Blog.Api.Cms.Storyblok;

namespace Tff.Blog.Api.Unit.Tests.Cms;

public class StoryblokMapperTests
{
    [Fact]
    public void MapToPostModel_MapsAllFields()
    {
        var story = new StoryblokStory
        {
            Name = "Story name",
            Slug = "first-post",
            FullSlug = "posts/first-post",
            TagList = ["dotnet", "blazor"],
            PublishedAt = new DateTime(2024, 4, 1, 10, 0, 0, DateTimeKind.Utc),
            FirstPublishedAt = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            Content = new StoryblokContent
            {
                Title = "First post",
                Body = "## Hello",
                Description = "A brief",
                Cover = new StoryblokAsset { Filename = "https://example.com/cover.png" },
            },
        };

        var model = StoryblokMapper.MapToPostModel(story);

        model.Title.Should().Be("First post");
        model.Slug.Should().Be("first-post");
        model.Description.Should().Be("A brief");
        model.Text.Should().Be("## Hello");
        model.Image.Should().Be("https://example.com/cover.png");
        model.ReadTimeInMinutes.Should().Be(1);
        model.Date.Should().Be(new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc));
        model.Tags.Should().ContainInOrder("dotnet", "blazor");
        model.Draft.Should().BeFalse();
    }

    [Fact]
    public void MapToPostModel_WhenFieldsAreMissing_UsesFallbacks()
    {
        var story = new StoryblokStory
        {
            Name = "Story name",
            FullSlug = "posts/fallback-slug",
            CreatedAt = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Content = null,
        };

        var model = StoryblokMapper.MapToPostModel(story);

        model.Title.Should().Be("Story name");
        model.Slug.Should().Be("fallback-slug");
        model.Description.Should().BeEmpty();
        model.Text.Should().BeEmpty();
        model.Image.Should().BeEmpty();
        model.ReadTimeInMinutes.Should().Be(0);
        model.Date.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        model.Tags.Should().BeEmpty();
        model.Draft.Should().BeTrue();
    }

    [Fact]
    public void MapToPostModel_UsesFirstPublishedAtOverPublishedAt()
    {
        var story = new StoryblokStory
        {
            Slug = "post",
            PublishedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            FirstPublishedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Content = new StoryblokContent
            {
                Body = "Body",
            },
        };

        StoryblokMapper.MapToPostModel(story).Date.Should().Be(new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapToPostModel_StripsAlignAttributes()
    {
        var story = new StoryblokStory
        {
            Slug = "post",
            Content = new StoryblokContent
            {
                Body = "<img src=\"image.png\" align=\"center\" />",
            },
        };

        var model = StoryblokMapper.MapToPostModel(story);

        model.Text.Should().NotContain("align=");
    }

    [Fact]
    public void MapToPostModel_CalculatesReadTimeFromBody()
    {
        var body = string.Join(' ', Enumerable.Repeat("word", 450));
        var story = new StoryblokStory
        {
            Slug = "post",
            Content = new StoryblokContent { Body = body },
        };

        var model = StoryblokMapper.MapToPostModel(story);

        model.ReadTimeInMinutes.Should().Be(3);
    }

    [Fact]
    public void MapToPostModel_WhenTheStoryIsUnpublished_MarksItAsDraft()
    {
        var story = new StoryblokStory
        {
            Slug = "draft",
            PublishedAt = null,
            Content = new StoryblokContent { Body = "Body" },
        };

        StoryblokMapper.MapToPostModel(story).Draft.Should().BeTrue();
    }

    [Fact]
    public void MapToPageModel_MapsTheMarkdownBody()
    {
        var story = new StoryblokStory
        {
            Slug = "about",
            Content = new StoryblokContent { Body = "# About" },
        };

        var page = StoryblokMapper.MapToPageModel(story);

        page.Text.Should().Be("# About");
    }
}
