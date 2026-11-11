# AGENTS.md

Personal blog: Blazor WebAssembly client + Azure Functions API, both on .NET 10.

## Projects and boundaries

- `tff-blog/` — Blazor WASM client. Assembly/namespace is `Tff.Blog.Client` (not the folder name).
- `tff-blog-api/` — Azure Functions v4 **isolated worker**. Assembly/namespace is `Tff.Blog.Api`.
- `tff-blog-shared/` — DTOs, mappers, converters. Assembly/namespace is `Tff.Blog.Shared`; referenced by both other projects.
- New client code must use `Tff.Blog.Client.*` namespaces; `tff-blog/_Imports.razor` and Razor resolution depend on this.
- There is no database or EF. Posts come from the Storyblok CDN API (`https://api.storyblok.com`). Pages are Markdown files in `tff-blog/wwwroot/pages/`; `IContentProvider.GetPageAsync` exists as groundwork for moving pages to Storyblok later.

## Commands

- Build everything: `dotnet build tff-blog.sln`
- Run all tests: `dotnet test tff-blog.sln --no-build` (run `dotnet build` first)
- Run client: `dotnet run --project tff-blog` (http://localhost:5000, https://localhost:5001)
- Run API: `dotnet run --project tff-blog-api` (port 7071; launch profile adds `--cors *`)
- CI (`.github/workflows/azure-static-web-apps-black-tree-01f647703.yml`) runs on pushes/PRs to `master` and deploys via Azure Static Web Apps (`app_location: tff-blog`, `api_location: tff-blog-api`, `output_location: wwwroot`). The API is hosted as managed Functions, so its runtime is selected by `platform.apiRuntime` in `tff-blog/staticwebapp.config.json` and must match the API's `TargetFramework`.

## Client ↔ API wiring

- Two named `HttpClient`s in `tff-blog/Program.cs`:
  - `"Client"` — app base address; used for `wwwroot/pages/*.md`.
  - `"WebAPI"` — base address from `ApiUrl` in `tff-blog/wwwroot/appsettings.json` (`api/BlogPosts`). Dev override is in `appsettings.Development.json` (`http://localhost:7071/api/BlogPosts`).
- `FeedUrl` in the same two files sets the URL behind the footer RSS icon. Production keeps the relative `rss.xml`, which `Program.cs` resolves against the app base address and the Static Web App rewrites to the API; `appsettings.Development.json` uses the absolute `http://localhost:7071/api/rss.xml` so the icon also works when the client runs alone (no rewrite on port 5000). `Program.cs` registers the resolved value as `FeedLinkSettings` (`tff-blog/Configuration/`), which `Social.razor` injects.
- Client pages and components do not use `IHttpClientFactory` directly. `tff-blog/Services/BlogContentService.cs` (`IBlogContentService`) owns post/page loading, slug encoding, logging, and maps expected HTTP/JSON/markdown failures to `BlogContentResult<T>` (`IsSuccess`, `IsNotFound`, `ErrorMessage`) instead of throwing. Caller cancellation is rethrown.
- Expected failures degrade one region only: `ContentError` shows a retryable message, a missing post/page navigates to `404`, and `AuthorComponent` hides itself. `MainLayout` wraps `<main>` and `<aside>` in per-region `ErrorBoundary` as a last resort; there is no global boundary replacing `@Body`.
- Changing the API route requires updating both `appsettings.json` and `appsettings.Development.json`.
- `"WebAPI"` is only registered when `ApiUrl` is non-empty. If it is missing, failures appear later at request time.
- `BlogContentService` forwards a `preview` query parameter from the current page URL to `api/BlogPosts`, which lets Storyblok editor preview URLs load drafts.
- Single API entrypoint: function `BlogPosts` in `tff-blog-api/BlogFunction.cs`, `AuthorizationLevel.Function`, route `api/BlogPosts`. It accepts `postName`, `sortField`, `sortOrder` from query string or JSON body. Invalid `sortOrder` or `sortField` returns `400` before any CMS call.
- `BlogFunction` depends on `IContentProvider` (`tff-blog-api/Services/IContentProvider.cs`). The registered implementation is `CachedContentProvider` (`tff-blog-api/Services/CachedContentProvider.cs`), an `IMemoryCache` decorator over `StoryblokContentProvider` (`tff-blog-api/Cms/Storyblok/StoryblokContentProvider.cs`), a typed `HttpClient` client using `StoryblokSettings`; DI lives in `tff-blog-api/Program.cs`. Mapping from Storyblok stories to `PostModel`/`PageModel` is in `StoryblokMapper`.
- Caching is keyed by content (`content:posts`, `content:posts:{slug}`, `content:pages:{slug}`) with an absolute TTL. Missing posts/pages and failed requests are not cached. A TTL of `0` or less bypasses the cache entirely.
- Storyblok content model: a `post` content type with `title`, `body` (Markdown), `description`, `cover`; categories come from the story `tag_list`; posts live under the `posts/` folder. `Date` is derived from the story `first_published_at` (falling back to `published_at` and `created_at`), `Draft` is derived from `published_at` being null, and `ReadTimeInMinutes` is always computed from the body at 200 wpm.
- Sorting lives in `tff-blog-api/Services/PostSorter.cs`; the sortable-field whitelist is case-insensitive and includes `Date`, `Title`, `Slug`, `Description`, `ReadTimeInMinutes`, and `Draft` (not `Tags`, `Image`, or `Text`).
- Feeds: `tff-blog-api/FeedFunction.cs` exposes two **anonymous** functions, `RssFeed` (route `api/rss.xml`) and `AtomFeed` (route `api/atom.xml`), because feed readers cannot send a function key. Both reuse `IContentProvider` with `preview: false`, filter `Draft` again in `FeedItemSelector`, and serialize the latest 15 posts with `Description` as the item summary. Builders in `tff-blog-api/Feeds/` are pure `posts + SiteUrl → XML` (hand-rolled `XDocument`, no `System.ServiceModel.Syndication`). Item links are `{SiteUrl}/post/{slug}`. Responses carry `Cache-Control: public, max-age=300, stale-while-revalidate=60` and a SHA-256 `ETag` derived from the payload, answering `If-None-Match` with `304`; failures return `500` with `no-store`. The Functions host always prefixes routes with `api/`, so `/rss.xml` and `/atom.xml` exist only through the SWA rewrites.
- Sitemap: `tff-blog-api/Sitemap/SitemapFunction.cs` exposes an **anonymous** `Sitemap` function (route `api/sitemap.xml`) backed by the pure `SitemapBuilder`. It lists the three static routes (`/`, `/about`, `/cookie-policy`) plus **every** published post (no 15-item cap), newest first, and emits `<lastmod>` from `PostModel.Date` in RFC 3339; static pages have no `<lastmod>`. It always uses `preview: false` and excludes drafts and blank slugs. Response: `application/xml; charset=utf-8`, `Cache-Control: public, max-age=3600, stale-while-revalidate=60`, SHA-256 `ETag` with `304` support, `500` + `no-store` on failure. The shared ETag/304/error plumbing for the feeds, sitemap and post shell lives in `tff-blog-api/Responses/ConditionalResponse.cs`; pass extra headers through its `params` argument, because headers added after the body write are lost when the host flushes the response.
- Post shell (link previews): `tff-blog-api/Shell/PostShellFunction.cs` exposes an **anonymous** `PostShell` function (route `api/post-shell`) that serves the SPA shell with the post's `title`, `description`, canonical, `og:*` and `twitter:*` tags spliced in before `</head>` by the pure `PostShellBuilder`. `name="description"` keeps the post's full description, while `og:description` and `twitter:description` use `PostSummary.ForSocialShare` (`tff-blog-shared/Formatting/PostSummary.cs`), the same 200-character word-boundary truncation the client's `PostViewModel.ShareDescription` applies, so crawlers and browsers see identical summaries. The shell is the client's real `wwwroot/index.html`, embedded into the API assembly via `<EmbeddedResource ... LogicalName="Tff.Blog.Api.Shell.index.html" />` (`Shell/AppShell.cs`), so it cannot drift from the app the SPA boots from. Tags are injected only for link unfurlers (`LinkUnfurlers`: `facebookexternalhit`, `Twitterbot`, `LinkedInBot`, `Slackbot`, `Discordbot`, `WhatsApp`, …), because they never run JavaScript; search engines are deliberately excluded since they render the SPA and would otherwise see duplicate tags after hydration. Browsers and unrecognised agents get the plain shell. The slug comes from the `x-ms-original-url` header Static Web Apps adds on rewritten requests, then the request path, then `?slug=`. Responses are `text/html; charset=utf-8`, `Cache-Control: public, max-age=300, stale-while-revalidate=60`, `Vary: User-Agent`, SHA-256 `ETag` with `304`; a missing slug, missing post or CMS failure all fall back to the plain shell (never a `500`). `tff-blog/staticwebapp.config.json` rewrites `/post/*` to `/api/post-shell`, so **every** post URL now depends on the Functions host — verify on a preview deployment that `x-ms-original-url` survives the rewrite; if it does not, crawlers silently get the plain shell.
- Discovery: `tff-blog/wwwroot/robots.txt` allows everything except `/api/` and points crawlers at `https://www.tiagofonseca.dev/sitemap.xml`. After a deploy, submit `sitemap.xml` in Google Search Console → Sitemaps. `tff-blog/staticwebapp.config.json` rewrites `/sitemap.xml` to the API and excludes it from the SPA fallback, same as the feed URLs.

## Config and secrets

- The API reads environment variables directly via `tff-blog-api/Configuration/Settings.cs` (`ShowDrafts`, `StoryblokEndpoint`, `StoryblokPublicToken`, `StoryblokPreviewToken`, `CacheTtlMinutes`, `PreviewSecret`), not `IConfiguration` or the Options pattern. `StoryblokEndpoint` defaults to `https://api.storyblok.com`.
- `ShowDrafts=true` selects the preview token and `version=draft`; production must keep it `false` so only published stories are served. Add `StoryblokPublicToken` and `StoryblokPreviewToken` to `local.settings.json` for local runs.
- `CacheTtlMinutes` defaults to `0`, which disables caching (intended for local development). Set a positive value in Azure, for example `10`. An invalid or negative value also disables caching.
- `PreviewSecret` enables request-scoped preview. A request with `?preview=<PreviewSecret>` serves draft stories, bypasses the content cache, and returns `Cache-Control: no-store`; a wrong secret (or a preview request while no secret is configured) returns `401`. It is independent of `ShowDrafts` and the Storyblok tokens, and previews stay disabled while it is empty.
- `SiteUrl` is the absolute site root used to build feed links. It defaults to `https://www.tiagofonseca.dev` and trailing slashes are trimmed; only set it on the Static Web App if the custom domain changes. Locally the feeds are served by the Functions host at `http://localhost:7071/api/rss.xml` and `api/atom.xml`, and the client's `FeedUrl` setting points the footer icon there.
- Configure the Storyblok editor preview URL with the same parameter, for example `<site>/?preview=<PreviewSecret>`. Internal Blazor navigation drops the query parameter, so deep previews should include it on the target URL.
- Local API settings live in `tff-blog-api/local.settings.json`. It is gitignored and untracked; it may contain a plaintext token in comments. Never commit, print, or copy its contents.
- Client secrets belong in User Secrets (`UserSecretsId` is configured). `appsettings.Development.json` is tracked and only contains the local API and feed URLs.
- Production needs no CORS config: Static Web Apps proxies `/api` same-origin. Local dev relies on `--cors *` from the launch profile.
- On Azure, set only the app's own settings (`StoryblokPublicToken`, `ShowDrafts`, `CacheTtlMinutes`, `StoryblokPreviewToken`, `PreviewSecret`) on the Static Web App. `AzureWeb*` and `FUNCTIONS_*` prefixes are reserved by Static Web Apps; for example `AzureWebJobsStorage=UseDevelopmentStorage=true` is a local-only value and can stop the Functions host from starting.

## Testing

- Tests live in `tests/` and are part of `tff-blog.sln`:
  - `tff-blog.Unit.Tests` — bUnit component tests for the client.
  - `tff-blog-api.Unit.Tests` — `PostSorter`, `BlogFunction`, `FeedFunction` and the feed builders, `StoryblokMapper`, and `CachedContentProvider` tests.
  - `tff-blog-shared.Unit.Tests` — converter tests.
  - `tff-blog.Integration.Tests` — `StoryblokContentProvider` against WireMock.Net.
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
- SPA routing is handled by `tff-blog/staticwebapp.config.json` (`navigationFallback` → `index.html`); client routes only need a Blazor `@page` directive. The same file pins the managed Functions runtime with `platform.apiRuntime: "dotnet-isolated:10.0"`, which must match `tff-blog-api`'s `TargetFramework`. Without it the API runs on an older .NET runtime and every `/api` call returns `500 Backend call failure` after the ~45 s gateway timeout, while the static site keeps working and the deploy still reports success. The feed and sitemap URLs need both a `routes` rewrite to the API and an entry in `navigationFallback.exclude`, otherwise the fallback would answer them with `index.html`. `trailingSlash: "never"` only normalizes requests that resolve to a real file (for example `/favicon.ico/`); SPA routes such as `/about/` are served by `navigationFallback`, which bypasses route rules, so the requested URL is kept. Canonical tags are what tell search engines the preferred URL: every page puts `<CanonicalLink />` (`tff-blog/Shared/Atoms/CanonicalLink.razor`) inside its `HeadContent`, and it normalizes `NavigationManager.Uri` through the pure `CanonicalUrl` helper (`tff-blog/Services/CanonicalUrl.cs`): query string and fragment dropped, trailing slash trimmed. Keep one `HeadContent` per page: when a page renders two, only one reaches the head and the other's tags (including `og:*`/`twitter:*`) silently disappear.
- Client content loading goes through `IBlogContentService`; do not reintroduce `try/catch → NavigateTo("404")` or swallow failures into empty lists. `404` is only for genuinely missing content.

## Release convention

- `master` is the deploy branch. `.github/workflows/tagversion.yml` derives version tags from commit messages; history uses conventional commits with `#minor` / `#patch` markers (e.g. `chore: update to dotnet10 #minor`). Match that style if asked to commit.
