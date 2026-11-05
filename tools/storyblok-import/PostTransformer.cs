using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tff.Tools.StoryblokImport;

internal sealed record MarkdownImage(string Alt, string Url);

internal static partial class PostTransformer
{
    private const int DescriptionLength = 160;

    public static string NormalizeBody(string body, out bool headingsShifted)
    {
        var withoutAlign = AlignAttribute().Replace(body, string.Empty);
        var lines = withoutAlign.Split('\n');
        var result = new List<string>(lines.Length);
        var inCodeFence = false;
        var hasTopLevelHeading = lines.Any(line => line.StartsWith("# ", StringComparison.Ordinal));

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
            }

            if (!inCodeFence && hasTopLevelHeading)
            {
                var heading = HeadingLine().Match(line);

                if (heading.Success && heading.Groups["hashes"].Length < 6)
                {
                    line = "#" + line;
                }
            }

            if (line.Length == 0 && result.Count > 0 && result[^1].Length == 0)
            {
                continue;
            }

            result.Add(line);
        }

        headingsShifted = hasTopLevelHeading;

        return string.Join("\n", result).Trim('\n');
    }

    public static IReadOnlyList<MarkdownImage> FindImages(string body)
    {
        return Image().Matches(body)
            .Select(match => new MarkdownImage(match.Groups["alt"].Value, match.Groups["url"].Value))
            .DistinctBy(image => image.Url)
            .ToList();
    }

    public static string ReplaceImages(string body, IReadOnlyDictionary<string, string> replacements)
    {
        return Image().Replace(body, match =>
            replacements.TryGetValue(match.Groups["url"].Value, out var replacement)
                ? $"![{match.Groups["alt"].Value}]({replacement})"
                : match.Value);
    }

    public static string BuildDescription(string body)
    {
        var lines = body.Split('\n');
        var paragraph = new List<string>();
        var inCodeFence = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            if (IsParagraphLine(line))
            {
                paragraph.Add(line);

                if (paragraph.Count > 0 && paragraph[^1].EndsWith("  ", StringComparison.Ordinal))
                {
                    break;
                }

                continue;
            }

            if (paragraph.Count > 0)
            {
                break;
            }
        }

        var text = StripInlineMarkdown(string.Join(' ', paragraph));

        if (text.Length <= DescriptionLength)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', DescriptionLength);

        if (cut <= 0)
        {
            cut = DescriptionLength;
        }

        return text[..cut].TrimEnd(' ', ',', ';', ':', '.') + "…";
    }

    private static bool IsParagraphLine(string line)
    {
        if (line.Length == 0)
        {
            return false;
        }

        if (line.StartsWith('#') ||
            line.StartsWith('*') && line.Length > 1 && line[1] == ' ' ||
            line.StartsWith('-') && line.Length > 1 && line[1] == ' ' ||
            line.StartsWith('>') ||
            line.StartsWith('|') ||
            line.StartsWith('<') ||
            line.StartsWith("![", StringComparison.Ordinal) ||
            line.StartsWith("---", StringComparison.Ordinal))
        {
            return false;
        }

        // Bold-only lines such as "**TL;DR**" are not real intros.
        if (BoldOnly().IsMatch(line))
        {
            return false;
        }

        return true;
    }

    private static string StripInlineMarkdown(string text)
    {
        var stripped = Image().Replace(text, string.Empty);
        stripped = Link().Replace(stripped, "$1");
        stripped = Code().Replace(stripped, "$1");
        stripped = Emphasis().Replace(stripped, string.Empty);
        stripped = WebUtility.HtmlDecode(stripped);

        return Whitespace().Replace(stripped, " ").Trim();
    }

    [GeneratedRegex(" align=\"[^\"]*\"")]
    private static partial Regex AlignAttribute();

    [GeneratedRegex(@"(?m)^(?<hashes>#{1,6}) ")]
    private static partial Regex HeadingLine();

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?<url>https?://[^)\s]+)\)")]
    private static partial Regex Image();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex Code();

    [GeneratedRegex(@"\*\*|__|\*|_")]
    private static partial Regex Emphasis();

    [GeneratedRegex(@"^\*\*[^*]+\*\*$")]
    private static partial Regex BoldOnly();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
