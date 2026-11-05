using System.Globalization;

namespace Tff.Tools.StoryblokImport;

internal sealed class PostDocument
{
    public required string SourcePath { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset Date { get; init; }

    public required string Body { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public string? CoverUrl { get; init; }

    public static PostDocument Load(string path)
    {
        var raw = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!raw.StartsWith("---\n", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path}: missing YAML front matter.");
        }

        var closing = raw.IndexOf("\n---", 4, StringComparison.Ordinal);

        if (closing < 0)
        {
            throw new InvalidDataException($"{path}: unterminated YAML front matter.");
        }

        var frontMatter = raw[4..closing];
        var body = raw[(closing + 4)..].TrimStart('\n');
        var fields = ParseFields(frontMatter, path);

        var slug = Require(fields, "slug", path);
        var title = Require(fields, "title", path);
        var dateValue = Require(fields, "date", path);

        if (!DateTimeOffset.TryParse(
                dateValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var date))
        {
            throw new InvalidDataException($"{path}: invalid date '{dateValue}'.");
        }

        return new PostDocument
        {
            SourcePath = path,
            Slug = slug,
            Title = title,
            Date = date,
            Body = body,
            Tags = fields.TryGetValue("tags", out var tags) && tags.Count > 0
                ? tags
                : Array.Empty<string>(),
            CoverUrl = fields.TryGetValue("coverImage", out var cover) && cover.Count > 0
                ? cover[0]
                : null,
        };
    }

    private static string Require(IReadOnlyDictionary<string, List<string>> fields, string key, string path)
    {
        if (!fields.TryGetValue(key, out var values) || values.Count == 0 || string.IsNullOrWhiteSpace(values[0]))
        {
            throw new InvalidDataException($"{path}: missing front matter value '{key}'.");
        }

        return values[0];
    }

    private static Dictionary<string, List<string>> ParseFields(string frontMatter, string path)
    {
        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lines = frontMatter.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith(' '))
            {
                throw new InvalidDataException($"{path}: unexpected indentation in front matter: '{line}'.");
            }

            var separator = line.IndexOf(':');

            if (separator <= 0)
            {
                throw new InvalidDataException($"{path}: malformed front matter line: '{line}'.");
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (value is ">-" or ">" or "|" or "|-")
            {
                var isLiteral = value.StartsWith('|');
                var parts = new List<string>();

                while (index + 1 < lines.Length && lines[index + 1].StartsWith("  ", StringComparison.Ordinal))
                {
                    parts.Add(lines[++index].Trim());
                }

                var joined = string.Join(isLiteral ? "\n" : " ", parts);
                fields[key] = [joined];
                continue;
            }

            if (value.Length == 0)
            {
                var items = new List<string>();

                while (index + 1 < lines.Length && lines[index + 1].StartsWith("  - ", StringComparison.Ordinal))
                {
                    items.Add(Unquote(lines[++index][4..].Trim()));
                }

                fields[key] = items;
                continue;
            }

            fields[key] = [Unquote(value)];
        }

        return fields;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"')))
        {
            return value[1..^1];
        }

        return value;
    }
}
