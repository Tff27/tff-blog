# Tests

All tests live under `tests/` and are part of `tff-blog.sln`.

| Project | Scope | Key packages |
| --- | --- | --- |
| `tests/tff-blog.Unit.Tests` | Blazor components (`tff-blog`) | bUnit, AwesomeAssertions |
| `tests/tff-blog-api.Unit.Tests` | `BlogFunction`, `PostSorter` (`tff-blog-api`) | NSubstitute, AwesomeAssertions |
| `tests/tff-blog-shared.Unit.Tests` | mappers and converters (`tff-blog-shared`) | AwesomeAssertions |
| `tests/tff-blog.Integration.Tests` | `CmsService` over HTTP (`tff-blog-api`) | WireMock.Net, AwesomeAssertions |

Assembly and root namespaces follow the solution: `Tff.Blog.Client.Unit.Tests`, `Tff.Blog.Api.Unit.Tests`, `Tff.Blog.Shared.Unit.Tests`, `Tff.Blog.Integration.Tests`.

## Running

```powershell
dotnet build tff-blog.sln
dotnet test tff-blog.sln --no-build

# focused
dotnet test tests/tff-blog-api.Unit.Tests --no-build
```

CI runs `dotnet test --no-build` after building the solution, so every project added to `tff-blog.sln` is part of the build gate.

## Conventions

- xUnit for discovery and assertions orchestration.
- **AwesomeAssertions** for all assertions: use `.Should()` instead of `Assert.*`.
- **NSubstitute** for mocks and substitutes, including `ILogger<T>` and `ICmsService`.
- **WireMock.Net** for anything that goes through `HttpClient`; unit tests must never reach the real network or Hashnode.
- **bUnit** for client components. Test classes derive from `BunitContext`.
- File names are `<TypeUnderTest>Tests.cs`, in a folder mirroring the production namespaces.
- Test projects use `<Nullable>enable</Nullable>` and `TreatWarningsAsErrors=true`; use `null!` only when a test intentionally exercises a null argument.
- Do not assert on log message formatting. Assert behaviour (status code, CMS calls, rendered markup).

## Examples

### NSubstitute + AwesomeAssertions

```csharp
var cmsService = Substitute.For<ICmsService>();
cmsService.GetPostsAsync("publication-id", Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(posts));

var response = await sut.RunAsync(request, CancellationToken.None);

response.StatusCode.Should().Be(HttpStatusCode.OK);
await cmsService.Received(1).GetPostsAsync("publication-id", Arg.Any<CancellationToken>());
```

`BlogFunction` takes `HttpRequestData`/`HttpResponseData`, which are abstract worker types with no usable default constructor. Use the in-memory doubles in `tests/tff-blog-api.Unit.Tests/Infrastructure/TestHttpDoubles.cs`; NSubstitute is still used for the service and logger dependencies. The doubles register `IOptions<WorkerOptions>` with a serializer so `WriteAsJsonAsync` can produce a readable response body.

### bUnit

```csharp
public class MarkdownTests : BunitContext
{
    [Fact]
    public void RendersHtml()
    {
        var cut = Render<Markdown>(parameters => parameters
            .Add(component => component.MarkdownContent, "# Hello"));

        cut.Markup.Should().Contain("<h1").And.Contain("Hello");
    }
}
```

bUnit 2.x uses `Render<T>(...)`; `RenderComponent<T>` is obsolete. Register required services through `Services.AddSingleton(...)`. For components without parameters use an explicitly typed builder lambda or a small helper:

```csharp
private IRenderedComponent<NavMenu> RenderNavMenu()
    => Render<NavMenu>((ComponentParameterCollectionBuilder<NavMenu> _) => { });
```

### WireMock.Net

```csharp
_server = WireMockServer.Start();
_sut = new CmsService(new HttpClient(), new CmsSettings(_server.Url!, "publication-id"));

_server
    .Given(Request.Create().UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody("""{ "data": { "publication": { "posts": { "edges": [] } } } }"""));

var posts = await _sut.GetPostsAsync("publication-id");
```

Always dispose the server, `HttpClient`, and `CmsService` in the test class. Keep response fixtures close to the test that uses them.

## API behaviour under test

- `BlogFunction` validates `sortOrder` and `sortField` before calling `ICmsService`.
- Invalid input returns `400` and makes no CMS call.
- Unknown `sortField` values (for example `Tags`) return `400`.
- Supported fields are defined by the case-insensitive whitelist in `tff-blog-api/Services/PostSorter.cs`: `Date`, `Title`, `Slug`, `Description`, `Image`, `Text`, `ReadTimeInMinutes`, `Draft`.
- `CmsService` takes an injected `HttpClient` and `CmsSettings`, so integration tests point it at WireMock instead of Hashnode.
