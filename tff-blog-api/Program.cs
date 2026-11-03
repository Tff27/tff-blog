using Microsoft.Azure.Functions.Worker.Builder;
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
builder.Services.AddHttpClient<IContentProvider, StoryblokContentProvider>();

builder.Build().Run();
