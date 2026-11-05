using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tff.Tools.StoryblokImport;

internal sealed record UploadedAsset(long Id, string Filename);

internal sealed record StoryWriteResult(long Id, string FullSlug, bool Published);

internal sealed class StoryblokClient : IDisposable
{
    private readonly string _baseUrl;
    private readonly long _spaceId;
    private readonly HttpClient _http;
    private readonly HttpClient _uploadHttp = new();
    private static readonly TimeSpan MinimumRequestInterval = TimeSpan.FromMilliseconds(250);
    private DateTime _lastRequestAt = DateTime.MinValue;

    public StoryblokClient(string baseUrl, long spaceId, string token)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _spaceId = spaceId;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<long> GetStoriesFolderIdAsync(string folderSlug, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(
            HttpMethod.Get,
            $"/spaces/{_spaceId}/stories?with_slug={Uri.EscapeDataString(folderSlug)}",
            null,
            cancellationToken);

        foreach (var story in document.RootElement.GetProperty("stories").EnumerateArray())
        {
            if (story.TryGetProperty("full_slug", out var fullSlug) &&
                story.TryGetProperty("is_folder", out var isFolder) &&
                fullSlug.GetString() == folderSlug &&
                isFolder.GetBoolean())
            {
                return story.GetProperty("id").GetInt64();
            }
        }

        throw new InvalidOperationException($"Storyblok story folder '{folderSlug}' was not found.");
    }

    public async Task<long> GetAssetFolderIdAsync(string name, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(HttpMethod.Get, $"/spaces/{_spaceId}/asset_folders", null, cancellationToken);

        foreach (var folder in document.RootElement.GetProperty("asset_folders").EnumerateArray())
        {
            if (folder.GetProperty("name").GetString() == name)
            {
                return folder.GetProperty("id").GetInt64();
            }
        }

        throw new InvalidOperationException($"Storyblok asset folder '{name}' was not found.");
    }

    public async Task<UploadedAsset> UploadAssetAsync(
        long assetFolderId,
        string fileName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["filename"] = fileName,
            ["asset_folder_id"] = assetFolderId,
        };

        using var signed = await SendAsync(HttpMethod.Post, $"/spaces/{_spaceId}/assets/", payload, cancellationToken);
        var root = signed.RootElement;
        var assetId = root.GetProperty("id").GetInt64();
        var prettyUrl = root.GetProperty("pretty_url").GetString()
            ?? throw new InvalidOperationException("Storyblok did not return a pretty_url for the signed upload.");
        var finalFilename = prettyUrl.StartsWith("//", StringComparison.Ordinal) ? $"https:{prettyUrl}" : prettyUrl;
        var postUrl = root.GetProperty("post_url").GetString()
            ?? throw new InvalidOperationException("Storyblok did not return a post_url for the signed upload.");

        using var form = new MultipartFormDataContent();

        foreach (var field in root.GetProperty("fields").EnumerateObject())
        {
            form.Add(new StringContent(field.Value.GetString() ?? string.Empty), field.Name);
        }

        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        using var uploadResponse = await _uploadHttp.PostAsync(postUrl, form, cancellationToken);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Uploading '{fileName}' failed ({(int)uploadResponse.StatusCode}): {uploadBody}");
        }

        return new UploadedAsset(assetId, finalFilename);
    }

    public async Task<long?> FindStoryIdAsync(string fullSlug, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(
            HttpMethod.Get,
            $"/spaces/{_spaceId}/stories?with_slug={Uri.EscapeDataString(fullSlug)}",
            null,
            cancellationToken);

        foreach (var story in document.RootElement.GetProperty("stories").EnumerateArray())
        {
            if (story.GetProperty("full_slug").GetString() == fullSlug)
            {
                return story.GetProperty("id").GetInt64();
            }
        }

        return null;
    }

    public async Task<long?> FindAssetIdAsync(string filename, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(HttpMethod.Get, $"/spaces/{_spaceId}/assets?per_page=100", null, cancellationToken);

        foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
        {
            if (asset.TryGetProperty("filename", out var candidate) && candidate.GetString() == filename)
            {
                return asset.GetProperty("id").GetInt64();
            }
        }

        return null;
    }

    public async Task<StoryWriteResult> UpsertStoryAsync(
        string fullSlug,
        long folderId,
        ImportPlan plan,
        string body,
        AssetReference? cover,
        bool publish,
        CancellationToken cancellationToken)
    {
        var existingId = await FindStoryIdAsync(fullSlug, cancellationToken);
        var content = new Dictionary<string, object?>
        {
            ["component"] = "post",
            ["title"] = plan.Title,
            ["description"] = plan.Description,
            ["body"] = body,
        };

        if (cover is not null)
        {
            content["cover"] = new Dictionary<string, object?>
            {
                ["id"] = cover.Id,
                ["filename"] = cover.Filename,
                ["alt"] = plan.Title,
                ["name"] = Path.GetFileName(new Uri(cover.Filename).AbsolutePath),
                ["focus"] = string.Empty,
                ["title"] = string.Empty,
                ["copyright"] = string.Empty,
                ["fieldtype"] = "asset",
                ["is_external_url"] = false,
            };
        }

        var story = new Dictionary<string, object?>
        {
            ["name"] = plan.Title,
            ["slug"] = plan.Slug,
            ["parent_id"] = folderId,
            ["content"] = content,
            ["tag_list"] = plan.Tags,
            ["first_published_at"] = plan.Date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            ["sort_by_date"] = plan.Date.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };

        var payload = new Dictionary<string, object?> { ["story"] = story };
        var query = publish ? "?publish=1" : string.Empty;

        using var document = existingId is null
            ? await SendAsync(HttpMethod.Post, $"/spaces/{_spaceId}/stories{query}", payload, cancellationToken)
            : await SendAsync(HttpMethod.Put, $"/spaces/{_spaceId}/stories/{existingId}{query}", payload, cancellationToken);

        var result = document.RootElement.GetProperty("story");

        return new StoryWriteResult(
            result.GetProperty("id").GetInt64(),
            result.GetProperty("full_slug").GetString() ?? fullSlug,
            result.TryGetProperty("published_at", out var publishedAt) && publishedAt.ValueKind != JsonValueKind.Null);
    }

    public async Task<string> DescribeStoryAsync(long storyId, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(HttpMethod.Get, $"/spaces/{_spaceId}/stories/{storyId}", null, cancellationToken);
        var story = document.RootElement.GetProperty("story");
        var content = story.GetProperty("content");

        var date = story.TryGetProperty("first_published_at", out var firstPublishedAt) ? firstPublishedAt.ToString() : "null";
        var cover = content.TryGetProperty("cover", out var coverValue) &&
                    coverValue.ValueKind == JsonValueKind.Object &&
                    coverValue.TryGetProperty("filename", out var coverFilename)
            ? coverFilename.GetString()
            : "null";
        var tags = story.TryGetProperty("tag_list", out var tagList)
            ? string.Join(",", tagList.EnumerateArray().Select(tag => tag.GetString()))
            : string.Empty;

        return $"date={date} cover={cover} tags=[{tags}]";
    }

    public void Dispose()
    {
        _http.Dispose();
        _uploadHttp.Dispose();
    }

    private async Task<JsonDocument> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var elapsed = DateTime.UtcNow - _lastRequestAt;

        if (elapsed < MinimumRequestInterval)
        {
            await Task.Delay(MinimumRequestInterval - elapsed, cancellationToken);
        }

        _lastRequestAt = DateTime.UtcNow;

        using var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{method} {path} failed ({(int)response.StatusCode}): {text}");
        }

        return JsonDocument.Parse(text);
    }
}
