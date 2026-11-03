#nullable enable annotations

namespace Tff.Blog.Api.Cms.Storyblok;

internal sealed class StoryblokStoriesResponse
{
    public List<StoryblokStory> Stories { get; set; } = [];
}

internal sealed class StoryblokStoryResponse
{
    public StoryblokStory? Story { get; set; }
}

internal sealed class StoryblokStory
{
    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? FullSlug { get; set; }

    public StoryblokContent? Content { get; set; }

    public List<string>? TagList { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? FirstPublishedAt { get; set; }

    public DateTime? CreatedAt { get; set; }
}

internal sealed class StoryblokContent
{
    public string? Component { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    public string? Description { get; set; }

    public StoryblokAsset? Cover { get; set; }

    public DateTime? Date { get; set; }

    public int? ReadTime { get; set; }

    public StoryblokAsset? Photo { get; set; }

    public string? Name { get; set; }
}

internal sealed class StoryblokAsset
{
    public string? Filename { get; set; }

    public string? Alt { get; set; }
}
