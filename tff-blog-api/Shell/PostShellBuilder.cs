#nullable enable annotations

using System.Net;
using System.Text;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Shell;

/// <summary>
/// Rewrites the SPA shell so link unfurlers — which never execute Blazor — receive the post's
/// title, summary and cover image inside the served HTML instead of the generic site metadata.
/// Pure and side-effect free: the shell goes in, the shell with metadata comes out.
/// </summary>
public static class PostShellBuilder
{
    internal const string SiteName = "Tiago Fonseca - Blog";
    internal const string SiteNameSuffix = " - Tiago Fonseca";
    internal const string FallbackImagePath = "/images/logo_icon.png";

    private const string HeadClose = "</head>";
    private const string TitleOpen = "<title";
    private const string TitleClose = "</title>";

    /// <summary>
    /// Returns <paramref name="shell"/> with the document title replaced and the description,
    /// canonical, Open Graph and Twitter tags injected before <c>&lt;/head&gt;</c>. A shell with
    /// no <c>&lt;/head&gt;</c> is returned untouched rather than corrupted.
    /// </summary>
    public static string Build(string shell, PostModel post, string siteUrl)
    {
        if (string.IsNullOrEmpty(shell))
        {
            return shell;
        }

        var headCloseIndex = shell.IndexOf(HeadClose, StringComparison.OrdinalIgnoreCase);

        if (headCloseIndex < 0)
        {
            return shell;
        }

        var root = siteUrl.TrimEnd('/');
        var title = post.Title ?? string.Empty;
        var description = string.IsNullOrWhiteSpace(post.Description) ? title : post.Description;
        var image = string.IsNullOrWhiteSpace(post.Image)
            ? $"{root}{FallbackImagePath}"
            : post.Image;
        var url = $"{root}/post/{Uri.EscapeDataString(post.Slug ?? string.Empty)}";

        var withTitle = ReplaceTitle(shell, title);
        var headCloseAfterTitle = withTitle.IndexOf(HeadClose, StringComparison.OrdinalIgnoreCase);

        if (headCloseAfterTitle < 0)
        {
            return withTitle;
        }

        var metadata = BuildMetadata(description, url, title, image);

        return withTitle[..headCloseAfterTitle] + metadata + withTitle[headCloseAfterTitle..];
    }

    private static string ReplaceTitle(string shell, string title)
    {
        var closeIndex = shell.IndexOf(TitleClose, StringComparison.OrdinalIgnoreCase);

        if (closeIndex < 0)
        {
            return shell;
        }

        var openIndex = shell.LastIndexOf(TitleOpen, closeIndex, StringComparison.OrdinalIgnoreCase);

        if (openIndex < 0)
        {
            return shell;
        }

        var element = $"<title>{Encode(title + SiteNameSuffix)}</title>";

        return shell[..openIndex] + element + shell[(closeIndex + TitleClose.Length)..];
    }

    private static string BuildMetadata(string description, string url, string title, string image)
    {
        var encodedTitle = Encode(title);
        var encodedDescription = Encode(description);
        var encodedUrl = Encode(url);
        var encodedImage = Encode(image);

        var builder = new StringBuilder();

        AppendTag(builder, "link", "rel=\"canonical\"", $"href=\"{encodedUrl}\"");
        AppendTag(builder, "meta", $"name=\"description\"", $"content=\"{encodedDescription}\"");

        AppendTag(builder, "meta", "property=\"og:type\"", "content=\"article\"");
        AppendTag(builder, "meta", "property=\"og:site_name\"", $"content=\"{Encode(SiteName)}\"");
        AppendTag(builder, "meta", "property=\"og:locale\"", "content=\"en_US\"");
        AppendTag(builder, "meta", "property=\"og:url\"", $"content=\"{encodedUrl}\"");
        AppendTag(builder, "meta", "property=\"og:title\"", $"content=\"{encodedTitle}\"");
        AppendTag(builder, "meta", "property=\"og:description\"", $"content=\"{encodedDescription}\"");
        AppendTag(builder, "meta", "property=\"og:image\"", $"content=\"{encodedImage}\"");
        AppendTag(builder, "meta", "property=\"og:image:alt\"", $"content=\"{encodedTitle}\"");

        AppendTag(builder, "meta", "name=\"twitter:card\"", "content=\"summary_large_image\"");
        AppendTag(builder, "meta", "name=\"twitter:creator\"", "content=\"@tff_27\"");
        AppendTag(builder, "meta", "name=\"twitter:url\"", $"content=\"{encodedUrl}\"");
        AppendTag(builder, "meta", "name=\"twitter:title\"", $"content=\"{encodedTitle}\"");
        AppendTag(builder, "meta", "name=\"twitter:description\"", $"content=\"{encodedDescription}\"");
        AppendTag(builder, "meta", "name=\"twitter:image\"", $"content=\"{encodedImage}\"");
        AppendTag(builder, "meta", "name=\"twitter:image:alt\"", $"content=\"{encodedTitle}\"");

        return builder.Append('\n').ToString();
    }

    private static void AppendTag(StringBuilder builder, string element, string attribute, string value)
    {
        builder.Append('\n').Append("    <").Append(element).Append(' ').Append(attribute).Append(' ').Append(value).Append(" />");
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
