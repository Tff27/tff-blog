using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tff.Blog.Api.Cms.Storyblok;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(new StoryblokSettings(
    Settings.GetStoryblokEndpoint(),
    Settings.GetStoryblokPublicToken(),
    Settings.GetStoryblokPreviewToken(),
    Settings.GetShowDrafts()));
builder.Services.AddSingleton(new PreviewSettings(Settings.GetPreviewSecret()));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<StoryblokContentProvider>();
builder.Services.AddTransient<IContentProvider>(services => new CachedContentProvider(
    services.GetRequiredService<StoryblokContentProvider>(),
    services.GetRequiredService<IMemoryCache>(),
    TimeSpan.FromMinutes(Settings.GetCacheTtlMinutes())));

builder.Build().Run();
