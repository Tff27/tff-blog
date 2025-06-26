using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tff.Blog.Client;
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

builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(x =>
{
    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedAttributes.Add("class");
    return sanitizer;
});

await builder.Build().RunAsync();