using System.Net;
using System.Security.Claims;
using System.Text;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tff.Blog.Api.Unit.Tests.Infrastructure;

/// <summary>
/// Minimal in-memory implementations of the Azure Functions HTTP abstractions.
/// The worker types are abstract classes without usable default constructors, which makes
/// hand-written doubles clearer than substitutes here.
/// </summary>
internal sealed class TestHttpRequestData : HttpRequestData
{
    private readonly MemoryStream _body;

    internal TestHttpRequestData(string body = "", string url = "http://localhost/api/BlogPosts")
        : this(CreateFunctionContext(), body, url)
    {
    }

    private TestHttpRequestData(FunctionContext functionContext, string body, string url)
        : base(functionContext)
    {
        _body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        Url = new Uri(url);
    }

    public override Stream Body => _body;

    public override HttpHeadersCollection Headers { get; } = new();

    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();

    public override string Method => "GET";

    public override HttpResponseData CreateResponse() => new TestHttpResponseData(FunctionContext);

    internal static FunctionContext CreateFunctionContext()
    {
        var services = new ServiceCollection()
            .AddSingleton<IOptions<WorkerOptions>>(Options.Create(new WorkerOptions
            {
                Serializer = new JsonObjectSerializer(Tff.Blog.Api.Configuration.JsonSettings.Options),
            }))
            .BuildServiceProvider();

        return new TestFunctionContext { InstanceServices = services };
    }
}

internal sealed class TestHttpResponseData : HttpResponseData
{
    internal TestHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    {
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; } = new();

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies => null!;

    internal string ReadBodyAsString()
    {
        Body.Position = 0;

        using var reader = new StreamReader(Body, Encoding.UTF8, leaveOpen: true);

        return reader.ReadToEnd();
    }
}

internal sealed class TestFunctionContext : FunctionContext
{
    public override string InvocationId => "test-invocation";

    public override string FunctionId => "test-function";

    public override TraceContext TraceContext => null!;

    public override BindingContext BindingContext => null!;

    public override RetryContext RetryContext => null!;

    public override IServiceProvider InstanceServices { get; set; } = null!;

    public override FunctionDefinition FunctionDefinition => null!;

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IInvocationFeatures Features => null!;
}
