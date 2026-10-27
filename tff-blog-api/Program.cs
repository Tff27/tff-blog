using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Api.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(new CmsSettings(
    Settings.GetHashnodeGraphQlEndpoint(),
    Settings.GetHashnodePublicationId()));
builder.Services.AddHttpClient<ICmsService, CmsService>();

builder.Build().Run();
