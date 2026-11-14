#nullable enable annotations

namespace Tff.Blog.Api.Shell;

/// <summary>
/// The client's <c>wwwroot/index.html</c>, embedded into this assembly at build time. Reusing the
/// real shell means the post page served to crawlers can never drift from the app the SPA boots
/// from, and the endpoint needs no network call or copy kept in sync by hand.
/// </summary>
public static class AppShell
{
    internal const string ResourceName = "Tff.Blog.Api.Shell.index.html";

    private static readonly Lazy<string> Shell = new(Load);

    public static string Html => Shell.Value;

    private static string Load()
    {
        using var stream = typeof(AppShell).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded app shell '{ResourceName}' was not found.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
