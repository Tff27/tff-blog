using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ganss.XSS;
using Microsoft.Extensions.DependencyInjection;
using System;
using tffBlog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("Client", sp => sp.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddHttpClient("WebAPI", sp => sp.BaseAddress = new Uri(builder.Configuration["ApiUrl"]));

builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(x =>
{
    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedAttributes.Add("class");
    return sanitizer;
});

await builder.Build().RunAsync();