namespace Tff.Blog.Shared.Formatting;

/// <summary>
/// Builds the short summary shown in link previews. Social cards truncate long descriptions
/// themselves — often mid-word — so the text is capped at the length X/Twitter and LinkedIn
/// display and cut at a word boundary instead.
/// </summary>
public static class PostSummary
{
    /// <summary>Maximum length of a social-card description, matching the tightest common limit.</summary>
    public const int SocialDescriptionMaxLength = 200;

    /// <summary>
    /// Returns <paramref name="description"/> trimmed to
    /// <see cref="SocialDescriptionMaxLength"/> at a word boundary, with an ellipsis when it was
    /// shortened. Blank input returns an empty string.
    /// </summary>
    public static string ForSocialShare(string? description)
    {
        var text = description?.Trim() ?? string.Empty;

        if (text.Length <= SocialDescriptionMaxLength)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', SocialDescriptionMaxLength);

        if (cut <= 0)
        {
            cut = SocialDescriptionMaxLength;
        }

        return text[..cut].TrimEnd(' ', ',', ';', ':', '-', '.') + "…";
    }
}
