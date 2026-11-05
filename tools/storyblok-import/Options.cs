using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Tff.Tools.StoryblokImport;

internal sealed class Options
{
    private const string TokenSecretKey = "Storyblok:ManagementToken";

    private const string DefaultSource = "tools/storyblok-import/posts";

    private const string DefaultStateDirectory = "tools/storyblok-import/.state";

    public bool Apply { get; private init; }

    public int? Limit { get; private init; }

    public string? Post { get; private init; }

    public bool Publish { get; private init; }

    public long SpaceId { get; private init; } = 296325972589163;

    public string? TokenFile { get; private init; }

    public string Source { get; private init; } = DefaultSource;

    public string StateDirectory { get; private init; } = DefaultStateDirectory;

    public string BaseUrl { get; private init; } = "https://mapi.storyblok.com/v1";

    public string AssetFolderName { get; private init; } = "blog-posts";

    public string StoriesFolderSlug { get; private init; } = "posts";

    public static Options? Parse(string[] args)
    {
        var options = new Builder();

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--apply":
                    options.Apply = true;
                    break;
                case "--publish":
                    options.Publish = true;
                    break;
                case "--limit":
                    if (!TryReadValue(args, ref index, out var limit) ||
                        !int.TryParse(limit, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit))
                    {
                        return null;
                    }

                    options.Limit = parsedLimit;
                    break;
                case "--post":
                    if (!TryReadValue(args, ref index, out var post))
                    {
                        return null;
                    }

                    options.Post = post;
                    break;
                case "--space":
                    if (!TryReadValue(args, ref index, out var space) ||
                        !long.TryParse(space, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSpace))
                    {
                        return null;
                    }

                    options.SpaceId = parsedSpace;
                    break;
                case "--token-file":
                    if (!TryReadValue(args, ref index, out var tokenFile))
                    {
                        return null;
                    }

                    options.TokenFile = tokenFile;
                    break;
                case "--source":
                    if (!TryReadValue(args, ref index, out var source))
                    {
                        return null;
                    }

                    options.Source = source;
                    break;
                case "--state":
                    if (!TryReadValue(args, ref index, out var state))
                    {
                        return null;
                    }

                    options.StateDirectory = state;
                    break;
                case "--base-url":
                    if (!TryReadValue(args, ref index, out var baseUrl))
                    {
                        return null;
                    }

                    options.BaseUrl = baseUrl;
                    break;
                case "--asset-folder":
                    if (!TryReadValue(args, ref index, out var assetFolder))
                    {
                        return null;
                    }

                    options.AssetFolderName = assetFolder;
                    break;
                case "--stories-folder":
                    if (!TryReadValue(args, ref index, out var storiesFolder))
                    {
                        return null;
                    }

                    options.StoriesFolderSlug = storiesFolder;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[index]}");
                    return null;
            }
        }

        return options.Build();
    }

    public static void PrintUsage()
    {
        Console.WriteLine(
            """
            Usage: dotnet run --project tools/storyblok-import -- [options]

            Dry run (default): parses the posts and writes the payloads under the state folder.
            Add --apply to upload assets and upsert draft stories.

              --apply                  Upload assets and write stories (default is dry run)
              --limit <n>              Process only the first n posts
              --post <slug>            Process a single post by slug
              --publish                Publish stories after writing (default: drafts)
              --space <id>             Storyblok space id
              --token-file <path>      Optional file containing the management token
              --source <dir>           Markdown folder (default: tools/storyblok-import/posts)
              --state <dir>            State folder (default: tools/storyblok-import/.state)
              --base-url <url>         Management API base URL
              --asset-folder <name>    Asset folder (default: blog-posts)
              --stories-folder <slug>  Folder slug for posts (default: posts)

            The token is read from STORYBLOK_MGMT_TOKEN, then from user secrets
            (Storyblok:ManagementToken), then from --token-file when provided.
            """);
    }

    public string? ResolveToken()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("STORYBLOK_MGMT_TOKEN");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        var fromUserSecrets = ResolveTokenFromUserSecrets();

        if (!string.IsNullOrWhiteSpace(fromUserSecrets))
        {
            return fromUserSecrets;
        }

        if (!string.IsNullOrWhiteSpace(TokenFile) && File.Exists(TokenFile))
        {
            return File.ReadAllText(TokenFile).Trim();
        }

        return null;
    }

    private static string? ResolveTokenFromUserSecrets()
    {
        var id = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "UserSecretsId")
            ?.Value;

        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "UserSecrets",
            id,
            "secrets.json");

        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.TryGetProperty(TokenSecretKey, out var value)
            ? value.GetString()
            : null;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        value = string.Empty;

        if (index + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Missing value for {args[index]}");
            return false;
        }

        value = args[++index];
        return true;
    }

    private sealed class Builder
    {
        public bool Apply { get; set; }

        public int? Limit { get; set; }

        public string? Post { get; set; }

        public bool Publish { get; set; }

        public long SpaceId { get; set; } = 296325972589163;

        public string? TokenFile { get; set; }

        public string Source { get; set; } = DefaultSource;

        public string StateDirectory { get; set; } = DefaultStateDirectory;

        public string BaseUrl { get; set; } = "https://mapi.storyblok.com/v1";

        public string AssetFolderName { get; set; } = "blog-posts";

        public string StoriesFolderSlug { get; set; } = "posts";

        public Options Build()
        {
            return new Options
            {
                Apply = Apply,
                Limit = Limit,
                Post = Post,
                Publish = Publish,
                SpaceId = SpaceId,
                TokenFile = TokenFile,
                Source = Source,
                StateDirectory = StateDirectory,
                BaseUrl = BaseUrl,
                AssetFolderName = AssetFolderName,
                StoriesFolderSlug = StoriesFolderSlug,
            };
        }
    }
}
