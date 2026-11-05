namespace Tff.Tools.StoryblokImport;

internal sealed class ImportPlan
{
    public required string SourcePath { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset Date { get; init; }

    public required string Body { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required IReadOnlyList<MarkdownImage> Images { get; init; }

    public required bool HeadingsShifted { get; init; }

    public string? CoverUrl { get; init; }
}
