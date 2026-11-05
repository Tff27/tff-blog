using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tff.Tools.StoryblokImport;

var options = Options.Parse(args);

if (options is null)
{
    Options.PrintUsage();
    return 1;
}

var source = Path.GetFullPath(options.Source);
var stateDirectory = Path.GetFullPath(options.StateDirectory);

if (!Directory.Exists(source))
{
    Console.Error.WriteLine($"Source folder not found: {source}");
    return 1;
}

IEnumerable<string> files = Directory.GetFiles(source, "*.md")
    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

if (options.Post is not null)
{
    files = files.Where(file => string.Equals(
        Path.GetFileNameWithoutExtension(file),
        options.Post,
        StringComparison.OrdinalIgnoreCase));
}

if (options.Limit is int limit)
{
    files = files.Take(limit);
}

var planList = new List<ImportPlan>();
var failures = new List<string>();

foreach (var file in files)
{
    try
    {
        planList.Add(BuildPlan(file));
    }
    catch (Exception exception)
    {
        failures.Add($"{Path.GetFileName(file)}: {exception.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("The backup could not be parsed:");

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"  - {failure}");
    }

    return 1;
}

if (planList.Count == 0)
{
    Console.Error.WriteLine("No posts found.");
    return 1;
}

var duplicates = planList
    .GroupBy(plan => plan.Slug, StringComparer.OrdinalIgnoreCase)
    .Where(group => group.Count() > 1)
    .Select(group => group.Key)
    .ToList();

if (duplicates.Count > 0)
{
    Console.Error.WriteLine($"Duplicate slugs: {string.Join(", ", duplicates)}");
    return 1;
}

PrintSummary(planList, options);

if (!options.Apply)
{
    WriteDryRun(planList, stateDirectory);
    return 0;
}

var token = options.ResolveToken();

if (token is null)
{
    Console.Error.WriteLine(
        "No management token found. Set STORYBLOK_MGMT_TOKEN, configure the 'Storyblok:ManagementToken' user secret, or pass --token-file.");
    return 1;
}

using var client = new StoryblokClient(options.BaseUrl, options.SpaceId, token);
var postsFolderId = await client.GetStoriesFolderIdAsync(options.StoriesFolderSlug, CancellationToken.None);
var assetFolderId = await client.GetAssetFolderIdAsync(options.AssetFolderName, CancellationToken.None);
var manifest = AssetManifest.Load(stateDirectory);
using var downloader = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

Console.WriteLine($"posts folder id={postsFolderId}, asset folder '{options.AssetFolderName}' id={assetFolderId}, publish={options.Publish}");

for (var index = 0; index < planList.Count; index++)
{
    var plan = planList[index];
    Console.WriteLine($"[{index + 1}/{planList.Count}] {plan.Slug}");

    var cover = plan.CoverUrl is null
        ? null
        : await EnsureAssetAsync(client, downloader, manifest, assetFolderId, plan.CoverUrl, $"{plan.Slug}-cover");

    var replacements = new Dictionary<string, string>(StringComparer.Ordinal);

    for (var imageIndex = 0; imageIndex < plan.Images.Count; imageIndex++)
    {
        var image = plan.Images[imageIndex];
        var uploaded = await EnsureAssetAsync(
            client,
            downloader,
            manifest,
            assetFolderId,
            image.Url,
            $"{plan.Slug}-image-{imageIndex + 1}");
        replacements[image.Url] = uploaded.Filename;
    }

    var body = PostTransformer.ReplaceImages(plan.Body, replacements);
    var fullSlug = $"{options.StoriesFolderSlug}/{plan.Slug}";
    var result = await client.UpsertStoryAsync(
        fullSlug,
        postsFolderId,
        plan,
        body,
        cover,
        options.Publish,
        CancellationToken.None);

    Console.WriteLine($"      story id={result.Id} full_slug={result.FullSlug} published={result.Published}");
    Console.WriteLine($"      {await client.DescribeStoryAsync(result.Id, CancellationToken.None)}");
}

Console.WriteLine($"Asset manifest: {manifest.FilePath}");
return 0;

static ImportPlan BuildPlan(string path)
{
    var document = PostDocument.Load(path);

    if (!Regex.IsMatch(document.Slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
    {
        throw new InvalidDataException($"invalid slug '{document.Slug}'");
    }

    var body = PostTransformer.NormalizeBody(document.Body, out var headingsShifted);
    var description = PostTransformer.BuildDescription(document.Body);

    if (string.IsNullOrWhiteSpace(description))
    {
        description = document.Title;
    }

    return new ImportPlan
    {
        SourcePath = path,
        Slug = document.Slug,
        Title = document.Title,
        Date = document.Date,
        Body = body,
        Description = description,
        Tags = document.Tags,
        Images = PostTransformer.FindImages(body),
        HeadingsShifted = headingsShifted,
        CoverUrl = document.CoverUrl,
    };
}

static void PrintSummary(IReadOnlyList<ImportPlan> plans, Options options)
{
    Console.WriteLine($"Source: {Path.GetFullPath(options.Source)} ({plans.Count} post(s))");
    Console.WriteLine($"{"slug",-62} {"date",-20} {"tags",-4} {"img",-3} {"h1",-3} desc");

    foreach (var plan in plans)
    {
        var date = plan.Date.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        Console.WriteLine($"{plan.Slug,-62} {date,-20} {plan.Tags.Count,-4} {plan.Images.Count,-3} {(plan.HeadingsShifted ? "yes" : "no"),-3} {plan.Description.Length}");
    }
}

static void WriteDryRun(IReadOnlyList<ImportPlan> plans, string stateDirectory)
{
    var directory = Path.Combine(stateDirectory, "dry-run");
    Directory.CreateDirectory(directory);
    var serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    foreach (var plan in plans)
    {
        var payload = new Dictionary<string, object?>
        {
            ["source"] = plan.SourcePath,
            ["headingsShifted"] = plan.HeadingsShifted,
            ["assetsToUpload"] = new Dictionary<string, object?>
            {
                ["cover"] = plan.CoverUrl,
                ["inline"] = plan.Images.Select(image => new { image.Alt, image.Url }).ToList(),
            },
            ["story"] = new Dictionary<string, object?>
            {
                ["name"] = plan.Title,
                ["slug"] = plan.Slug,
                ["parent_id"] = "<posts folder id>",
                ["content"] = new Dictionary<string, object?>
                {
                    ["component"] = "post",
                    ["title"] = plan.Title,
                    ["description"] = plan.Description,
                    ["body"] = plan.Body,
                    ["cover"] = plan.CoverUrl is null
                        ? null
                        : new Dictionary<string, object?>
                        {
                            ["id"] = "<asset id>",
                            ["filename"] = plan.CoverUrl,
                            ["alt"] = plan.Title,
                            ["fieldtype"] = "asset",
                        },
                },
                ["tag_list"] = plan.Tags,
                ["first_published_at"] = plan.Date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                ["sort_by_date"] = plan.Date.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
        };

        var target = Path.Combine(directory, $"{plan.Slug}.json");
        File.WriteAllText(target, JsonSerializer.Serialize(payload, serializerOptions));
        Console.WriteLine($"dry run: {target}");
    }
}

static async Task<AssetReference> EnsureAssetAsync(
    StoryblokClient client,
    HttpClient downloader,
    AssetManifest manifest,
    long assetFolderId,
    string sourceUrl,
    string baseName)
{
    if (manifest.TryGet(sourceUrl, out var existing))
    {
        if (existing.Id == 0 && existing.Filename.Length > 0)
        {
            var resolvedId = await client.FindAssetIdAsync(existing.Filename, CancellationToken.None);

            if (resolvedId is long assetId)
            {
                existing = existing with { Id = assetId };
                manifest.Set(sourceUrl, existing);
                manifest.Save();
            }
        }

        Console.WriteLine($"      reuse {existing.Filename}");
        return existing;
    }

    var (bytes, contentType, extension) = await DownloadAsync(downloader, sourceUrl);
    var fileName = baseName + extension;
    var asset = await client.UploadAssetAsync(assetFolderId, fileName, bytes, contentType, CancellationToken.None);
    var reference = new AssetReference(asset.Id, asset.Filename);
    manifest.Set(sourceUrl, reference);
    manifest.Save();
    Console.WriteLine($"      uploaded {fileName} ({bytes.Length} bytes)");

    return reference;
}

static async Task<(byte[] Bytes, string ContentType, string Extension)> DownloadAsync(HttpClient http, string url)
{
    using var response = await http.GetAsync(url, CancellationToken.None);
    response.EnsureSuccessStatusCode();

    var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
    var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
    var extension = Path.GetExtension(new Uri(url).AbsolutePath);

    if (string.IsNullOrWhiteSpace(extension) || extension.Length > 5)
    {
        extension = contentType switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg",
        };
    }

    return (bytes, contentType, extension);
}
