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
- Run client: `dotnet run --project tff-blog` (http://localhost:5000, https://localhost:5001)
- Run API: `dotnet run --project tff-blog-api` (port 7071; launch profile adds `--cors *`)
- There are **no test projects**. `dotnet test` exits 0 with zero tests — it is not verification.
- CI (`.github/workflows/azure-static-web-apps-black-tree-01f647703.yml`) runs on pushes/PRs to `master` and deploys via Azure Static Web Apps (`app_location: tff-blog`, `api_location: tff-blog-api`, `output_location: wwwroot`).

## Client ↔ API wiring

- Two named `HttpClient`s in `tff-blog/Program.cs`:
  - `"Client"` — app base address; used for `wwwroot/pages/*.md`.
  - `"WebAPI"` — base address from `ApiUrl` in `tff-blog/wwwroot/appsettings.json` (`api/BlogPosts`). Dev override is in `appsettings.Development.json` (`http://localhost:7071/api/BlogPosts`).
- Changing the API route requires updating both `appsettings.json` and `appsettings.Development.json`.
- `"WebAPI"` is only registered when `ApiUrl` is non-empty. If it is missing, failures appear later at request time.
- Single API entrypoint: function `BlogPosts` in `tff-blog-api/BlogFunction.cs`, `AuthorizationLevel.Function`, route `api/BlogPosts`. It accepts `postName`, `sortField`, `sortOrder` from query string or JSON body.
- `CmsService` (`tff-blog-api/Services/CmsService.cs`) is static, not DI-registered, creates `new HttpClient()` per call, and interpolates values into GraphQL query strings. Treat refactors here carefully.

## Config and secrets

- The API reads environment variables directly via `tff-blog-api/Configuration/Settings.cs` (`ShowDrafts`, `HashnodePublicationId`), not `IConfiguration` or the Options pattern.
- Local API settings live in `tff-blog-api/local.settings.json`. It is gitignored and untracked; it may contain a plaintext token in comments. Never commit, print, or copy its contents.
- Client secrets belong in User Secrets (`UserSecretsId` is configured). `appsettings.Development.json` is tracked and only contains the local API URL.
- Production needs no CORS config: Static Web Apps proxies `/api` same-origin. Local dev relies on `--cors *` from the launch profile.

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
