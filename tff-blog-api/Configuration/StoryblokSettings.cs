namespace Tff.Blog.Api.Configuration;

public sealed record StoryblokSettings(
    string Endpoint,
    string PublicToken,
    string PreviewToken,
    bool ShowDrafts);
