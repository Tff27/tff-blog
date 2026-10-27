# AGENTS.md

Personal blog: Blazor WebAssembly client + Azure Functions API, both on .NET 10.

## Projects and boundaries

- `tff-blog/` — Blazor WASM client. Assembly/namespace is `Tff.Blog.Client` (not the folder name).
- `tff-blog-api/` — Azure Functions v4 **isolated worker**. Assembly/namespace is `Tff.Blog.Api`.
- `tff-blog-shared/` — DTOs, mappers, converters. Assembly/namespace is `Tff.Blog.Shared`; referenced by both other projects.
- New client code must use `Tff.Blog.Client.*` namespaces; `tff-blog/_Imports.razor` and Razor resolution depend on this.
- There is no database or EF. All post/page content comes from the Hashnode GraphQL API (`https://gql.hashnode.com`). A few static pages are Markdown files in `tff-blog/wwwroot/pages/`.

## Commands

- Build everything: `dotnet build tff-blog.sln`
- Run all tests: `dotnet test tff-blog.sln --no-build` (run `dotnet build` first)
- Run client: `dotnet run --project tff-blog` (http://localhost:5000, https://localhost:5001)
- Run API: `dotnet run --project tff-blog-api` (port 7071; launch profile adds `--cors *`)
- CI (`.github/workflows/azure-static-web-apps-black-tree-01f647703.yml`) runs on pushes/PRs to `master` and deploys via Azure Static Web Apps (`app_location: tff-blog`, `api_location: tff-blog-api`, `output_location: wwwroot`).

## Client ↔ API wiring

- Two named `HttpClient`s in `tff-blog/Program.cs`:
  - `"Client"` — app base address; used for `wwwroot/pages/*.md`.
  - `"WebAPI"` — base address from `ApiUrl` in `tff-blog/wwwroot/appsettings.json` (`api/BlogPosts`). Dev override is in `appsettings.Development.json` (`http://localhost:7071/api/BlogPosts`).
- Changing the API route requires updating both `appsettings.json` and `appsettings.Development.json`.
- `"WebAPI"` is only registered when `ApiUrl` is non-empty. If it is missing, failures appear later at request time.
- Single API entrypoint: function `BlogPosts` in `tff-blog-api/BlogFunction.cs`, `AuthorizationLevel.Function`, route `api/BlogPosts`. It accepts `postName`, `sortField`, `sortOrder` from query string or JSON body. Invalid `sortOrder` or `sortField` returns `400` before any CMS call.
- `BlogFunction` depends on `ICmsService` and `CmsSettings` (registered in `tff-blog-api/Program.cs`). `CmsService` (`tff-blog-api/Services/CmsService.cs`) is an instance service using an injected `HttpClient`, and interpolates values into GraphQL query strings. Treat refactors here carefully.
- Sorting lives in `tff-blog-api/Services/PostSorter.cs`; the sortable-field whitelist is case-insensitive and includes `Date`, `Title`, `Slug`, `Description`, `ReadTimeInMinutes`, and `Draft` (not `Tags`, `Image`, or `Text`).

## Config and secrets

- The API reads environment variables directly via `tff-blog-api/Configuration/Settings.cs` (`ShowDrafts`, `HashnodePublicationId`, `HashnodeGraphQlEndpoint`), not `IConfiguration` or the Options pattern. `HashnodeGraphQlEndpoint` defaults to `https://gql.hashnode.com`.
- Local API settings live in `tff-blog-api/local.settings.json`. It is gitignored and untracked; it may contain a plaintext token in comments. Never commit, print, or copy its contents.
- Client secrets belong in User Secrets (`UserSecretsId` is configured). `appsettings.Development.json` is tracked and only contains the local API URL.
- Production needs no CORS config: Static Web Apps proxies `/api` same-origin. Local dev relies on `--cors *` from the launch profile.

## Testing

- Tests live in `tests/` and are part of `tff-blog.sln`:
  - `tff-blog.Unit.Tests` — bUnit component tests for the client.
  - `tff-blog-api.Unit.Tests` — `PostSorter` and `BlogFunction` tests.
  - `tff-blog-shared.Unit.Tests` — mapper and converter tests.
  - `tff-blog.Integration.Tests` — `CmsService` against WireMock.Net.
- Stack: xUnit, **AwesomeAssertions** (`.Should()` instead of `Assert.*`), **NSubstitute** for mocks/substitutes, **bUnit** for components, **WireMock.Net** for HTTP-level tests.
- Unit tests must never call the real network; stub HTTP with WireMock and external services with NSubstitute.
- Verification: `dotnet build tff-blog.sln` then `dotnet test tff-blog.sln --no-build`. Focused runs: `dotnet test tests/<project> --no-build`.
- `BlogFunction` tests use in-memory `HttpRequestData`/`HttpResponseData` doubles in `tests/tff-blog-api.Unit.Tests/Infrastructure/`; keep the worker serializer registered there when adding response-body assertions.
- See `tests/README.md` for conventions and examples.

## Build and code gotchas

- `TreatWarningsAsErrors=true` in Client and API. `Nullable` is `enable` in Client/Shared and `warnings` in API.
- `tff-blog-shared.csproj` has `TreatWarningsAsErrors` commented out, which is why its ~38 CS8618/CS8601 nullable warnings don't fail the build. Enabling it means fixing those warnings first.
- NuGet audit is configured to fail on vulnerable packages (`NU1901`–`NU1904` are warnings-as-errors). A dependency bump can break restore until vulnerabilities are resolved. Dependabot updates NuGet daily.
- No `.editorconfig`, `Directory.Build.props`, or central package management. Keep the duplicated property blocks per `.csproj` consistent.
- Markdown rendering is Markdig → HtmlSanitizer → `MarkupString` in `tff-blog/Shared/Atoms/Markdown.razor`. The sanitizer allows `class` in addition to its defaults (`tff-blog/Program.cs`). Do not bypass sanitization; extend the allowlist instead.
- SPA routing is handled by `tff-blog/staticwebapp.config.json` (`navigationFallback` → `index.html`); client routes only need a Blazor `@page` directive.
- Client pages commonly wrap all exceptions into `NavigateTo("404")` or an empty list without logging. Don't assume that pattern is a good model.

## Release convention

- `master` is the deploy branch. `.github/workflows/tagversion.yml` derives version tags from commit messages; history uses conventional commits with `#minor` / `#patch` markers (e.g. `chore: update to dotnet10 #minor`). Match that style if asked to commit.
