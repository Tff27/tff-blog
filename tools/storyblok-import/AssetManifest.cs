using System.Text.Json;

namespace Tff.Tools.StoryblokImport;

internal sealed record AssetReference(long Id, string Filename);

internal sealed class AssetManifest
{
    private const string FileName = "storyblok-assets.json";

    private readonly Dictionary<string, AssetReference> _items;

    private AssetManifest(string path, Dictionary<string, AssetReference> items)
    {
        FilePath = path;
        _items = items;
    }

    public string FilePath { get; }

    public static AssetManifest Load(string directory)
    {
        var path = Path.Combine(directory, FileName);

        if (!File.Exists(path))
        {
            return new AssetManifest(path, new Dictionary<string, AssetReference>(StringComparer.Ordinal));
        }

        var json = File.ReadAllText(path);
        var items = new Dictionary<string, AssetReference>(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(json);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value;

            if (value.ValueKind == JsonValueKind.String)
            {
                // Legacy entries only stored the uploaded filename.
                items[property.Name] = new AssetReference(0, value.GetString() ?? string.Empty);
                continue;
            }

            if (value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = value.TryGetProperty("id", out var idValue) && idValue.TryGetInt64(out var parsedId)
                ? parsedId
                : 0;
            var filename = value.TryGetProperty("filename", out var filenameValue)
                ? filenameValue.GetString() ?? string.Empty
                : string.Empty;

            items[property.Name] = new AssetReference(id, filename);
        }

        return new AssetManifest(path, items);
    }

    public bool TryGet(string sourceUrl, out AssetReference asset)
    {
        var found = _items.TryGetValue(sourceUrl, out var value);
        asset = value ?? new AssetReference(0, string.Empty);

        return found;
    }

    public void Set(string sourceUrl, AssetReference asset)
    {
        _items[sourceUrl] = asset;
    }

    public void Save()
    {
        var directory = System.IO.Path.GetDirectoryName(FilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(FilePath, JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true }));
    }
}
