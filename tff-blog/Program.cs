using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tff.Blog.Client;
using Tff.Blog.Client.Configuration;
using Tff.Blog.Client.Services;
using Ganss.Xss;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("Client", sp => sp.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

string? apiUrl = $"{builder.HostEnvironment.BaseAddress}{builder.Configuration["ApiUrl"]}";

if (builder.HostEnvironment.IsDevelopment())
{
    apiUrl = builder.Configuration["ApiUrl"];
}

if (!string.IsNullOrEmpty(apiUrl))
{
    builder.Services.AddHttpClient("WebAPI", sp => sp.BaseAddress = new Uri(apiUrl));
}

string feedUrl = builder.Configuration["FeedUrl"] ?? "rss.xml";

if (!Uri.IsWellFormedUriString(feedUrl, UriKind.Absolute))
{
    // Relative values resolve against the app base address; local development sets an absolute
    // URL so the footer link reaches the Functions host, which has no Static Web App rewrite.
    feedUrl = new Uri(new Uri(builder.HostEnvironment.BaseAddress), feedUrl).ToString();
}

builder.Services.AddSingleton(new FeedLinkSettings(feedUrl));

builder.Services.AddScoped<IBlogContentService, BlogContentService>();

builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(x =>
{
    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedAttributes.Add("class");
    return sanitizer;
});

await builder.Build().RunAsync();