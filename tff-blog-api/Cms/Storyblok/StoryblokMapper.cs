#nullable enable annotations

using System.Text.RegularExpressions;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Cms.Storyblok;

internal static partial class StoryblokMapper
{
    private const int WordsPerMinute = 200;

    public static PostModel MapToPostModel(StoryblokStory story)
    {
        var content = story.Content ?? new StoryblokContent();
        var body = content.Body ?? string.Empty;

        return new PostModel
        {
            Title = content.Title ?? story.Name ?? string.Empty,
            Description = content.Description ?? string.Empty,
            Text = SanitizeStyling(body),
            Date = content.Date ?? story.FirstPublishedAt ?? story.PublishedAt ?? story.CreatedAt ?? default,
            Tags = story.TagList ?? [],
            Slug = story.Slug ?? GetSlugFromFullSlug(story.FullSlug),
            Image = content.Cover?.Filename ?? string.Empty,
            ReadTimeInMinutes = CalculateReadTime(body),
            Draft = story.PublishedAt is null,
        };
    }

    public static PageModel MapToPageModel(StoryblokStory story)
    {
        return new PageModel
        {
            Text = story.Content?.Body ?? string.Empty,
        };
    }

    private static string GetSlugFromFullSlug(string? fullSlug)
    {
        if (string.IsNullOrWhiteSpace(fullSlug))
        {
            return string.Empty;
        }

        var segments = fullSlug.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? string.Empty : segments[^1];
    }

    private static int CalculateReadTime(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return 0;
        }

        var wordCount = markdown.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    private static string SanitizeStyling(string markdown)
    {
        // Matches align=" followed by any characters until the next double quote.
        return AlignAttributeRegex().Replace(markdown, string.Empty);
    }

    [GeneratedRegex("align=\"[^\"]*\"")]
    private static partial Regex AlignAttributeRegex();
}
