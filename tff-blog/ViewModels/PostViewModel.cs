using System.Globalization;
using Tff.Blog.Shared.Formatting;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.ViewModels;

/// <summary>
/// Presentation view of a <see cref="PostModel"/>: exposes the values the UI actually renders,
/// including the formatted date and read time and the client-side post URL, so components never
/// format or build links themselves.
/// </summary>
public sealed record PostViewModel(PostModel Post)
{
    public string Title => Post.Title;

    public string Description => Post.Description;

    /// <summary>
    /// Description trimmed for <c>og:description</c> and <c>twitter:description</c>. Keep
    /// <see cref="Description"/> for <c>meta name="description"</c>, where search engines apply
    /// their own snippet length.
    /// </summary>
    public string ShareDescription => PostSummary.ForSocialShare(Description);

    public string Image => Post.Image;

    public string Text => Post.Text;

    public IEnumerable<string> Tags => Post.Tags;

    public string Url => $"post/{Post.Slug}";

    public string IsoDate => Post.Date.ToString("O", CultureInfo.InvariantCulture);

    public string FormattedDate => Post.Date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    public string ReadTime => $"{Math.Max(1, Post.ReadTimeInMinutes)} min read";
}
