# Storyblok import tool

Console app that imports Hashnode Markdown posts into Storyblok.

It is not part of the deployed app; it is included in the solution so it keeps compiling with the rest of the repo. Everything it needs stays inside this folder, apart from the management token, which comes from an environment variable or user secrets.

## Layout

| Path | Purpose | Committed |
| --- | --- | --- |
| `posts/` | Markdown sources to import | No (gitignored) |
| `.state/storyblok-assets.json` | Uploaded asset IDs, so re-runs reuse assets | No (gitignored) |
| `.state/dry-run/` | Generated payloads from a dry run | No (gitignored) |

Copy the Markdown files into `posts/` before running.

## Usage

Run from the repository root:

```powershell
# Dry run (default): parse the posts and write payloads under .state/dry-run
dotnet run --project tools/storyblok-import

# Upload assets and upsert draft stories
dotnet run --project tools/storyblok-import -- --apply

# Single post
dotnet run --project tools/storyblok-import -- --apply --post initial-blog
```

`--help` is not implemented; run without arguments to print the option summary.

## Token

The management token is resolved in this order:

1. `STORYBLOK_MGMT_TOKEN` environment variable.
2. User secret `Storyblok:ManagementToken`:

   ```powershell
   dotnet user-secrets set "Storyblok:ManagementToken" "<token>" --project tools/storyblok-import
   ```

3. `--token-file <path>` when provided.

The token needs Stories, Assets, Asset folders, and Components write scopes on the target space.

## What it does

- Reads YAML front matter (`title`, `date`, `slug`, `tags`, `coverImage`) and the Markdown body.
- Upserts stories under the `posts/` folder using the `post` component (drafts by default, `--publish` to publish).
- Writes the backup date to `first_published_at` and `sort_by_date`.
- Derives `description` from the first real paragraph.
- Uploads covers and inline images to the `blog-posts` asset folder and rewrites the Markdown URLs.
- Links cover assets by ID and stores them in `.state/storyblok-assets.json`.
- Shifts headings down one level for posts that contain an H1.
