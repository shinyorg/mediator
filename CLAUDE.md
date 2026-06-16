# Shiny Mediator — Working Notes

Guidance for maintaining this repo. Code lives in `src/`, tests in `tests/`, the published Claude
Code skill in `skills/`, and the public documentation site in a **separate** repo at
`~/Desktop/dev/documentation` (rendered to https://shinylib.net/mediator).

## After every new feature or fix

A change is not "done" until the four artifacts below are in sync. Do all of them in the same
change unless there's a reason not to.

1. **Code + tests** (`src/`, `tests/`)
   - Source-generator snapshot tests use [Verify]. After a generator change, run the affected
     tests, review the `*.received.txt` output, and only then promote it to `*.verified.txt`.
   - Run the full suite (`dotnet test tests/Shiny.Mediator.Tests/Shiny.Mediator.Tests.csproj`)
     before considering the change complete.

2. **Documentation site** (`~/Desktop/dev/documentation/src/content/docs/mediator/`)
   - Update the relevant feature page (e.g. `sourcegeneration.mdx`, `requests.mdx`, `http/…`).
   - Add a **release note** — see the release-note rules below.
   - Pages are `.mdx`; release notes use the `<RN>` component
     (`import RN from '/src/components/ReleaseNote.astro'`), with `type="feature|enhancement|fix"`
     and an optional `breaking` flag.

3. **Skill** (`skills/shiny-mediator/`)
   - This directory is the source of the published `shiny-mediator` Claude Code plugin
     (see `.claude-plugin/plugin.json`, `"skills": "./skills/"`).
   - Keep `SKILL.md` and the `reference/*.md` files aligned with the code. Update the trigger
     keyword list near the top when a new public MSBuild property / attribute / API is introduced.
   - The skill is the agent-facing "how to generate correct code" doc — if the default or
     recommended pattern changes, the skill's default guidance must change too.

4. **readme.md** (repo root)
   - This file is packed into the NuGet package (`PackageReadmeFile`). Update the feature list and
     any inline guidance (e.g. the JSON serialization blurb) when behavior changes.

## Release notes

Release notes live in the documentation repo at
`~/Desktop/dev/documentation/src/content/docs/mediator/release-notes.mdx`.

**Which version does a note go against?** Use `<PackageVersion>` from `Directory.Build.props` —
**the raw version portion only** (strip any prerelease/build-metadata suffix, e.g.
`6.7.0-beta.1` → `6.7.0`).

**If the version is a beta / prerelease (or the section is still marked `TBD`):**
- If a `## <version> TBD` heading already exists in `release-notes.mdx`, **add the note under that
  existing section**. If you are modifying a feature that hasn't shipped yet (it's already an entry
  under a `TBD` section), edit that existing entry in place rather than adding a duplicate.
- If no section exists for that version yet, **create a new `## <version> TBD` heading** at the top
  and add the note there.

**If the version is a final release**, the section is dated (`## 6.4.0 - May 17, 2026`); add the
note under the matching dated section (or promote the `TBD` section to a dated one when cutting the
release).

Each note is a single `<RN>` line, newest version section at the top of the file.

## Source-generator opt-ins (escape hatches)

Generator behavior that can break a consumer's build/runtime should be **opt-in** with an MSBuild
property so there's always an escape hatch. Wiring for a new property:
- Read it in the generator via `build_property.<Name>` (default to OFF / safe value).
- Declare it as `<CompilerVisibleProperty>` in
  `src/Shiny.Mediator.SourceGenerators/SourceGenerators.targets` — **without this the generator
  never sees the consumer's setting.**
- Add a `Default*` constant in `Constants.cs`.
- Document it in the skill, the source-generation doc page, and a release note.

Example: `ShinyMediatorGenerateJsonContext` (request/result auto JSON serialization) is opt-in
(default off) — enable with `<ShinyMediatorGenerateJsonContext>true</ShinyMediatorGenerateJsonContext>`.

## Blog posts (only when explicitly requested)

Do **not** write blog posts automatically as part of a fix/feature. Write them **only when the user asks**. When asked to blog a feature, produce **two** posts — first the docs-site version, then adapt it for the personal blog.

### 1. Docs site — `~/Desktop/dev/documentation`

- File: `src/content/docs/blog/YYYY/MM/<slug>.mdx` (current year/month folders; create the month folder if needed).
- Frontmatter:
  ```yaml
  ---
  title: '...'
  description: '...'
  date: YYYY-MM-DD
  authors:
    - allanritchie
  tags:
    - Release        # or Feature, AI, etc.
  ---
  ```
- Body is MDX. Reuse components where relevant, e.g. `import NugetBadge from '/src/components/NugetBadge.astro';` then `<NugetBadge name="Shiny.Mediator" />`.
- Voice: product/release-note tone — what shipped, breaking changes, code samples, how to use it. **No hero image** on this site.

### 2. Personal blog — `~/Desktop/dev/blog` (adapt the docs post)

- File: `src/content/blog/YYYY/MM/<slug>.mdx` (note: `content/blog`, not `content/docs/blog`).
- Frontmatter (different schema — see `src/content.config.ts`):
  ```yaml
  ---
  title: '...'
  description: '...'
  pubDate: 'Mon DD YYYY'                          # e.g. 'Jun 15 2026'
  heroImage: '../../../../assets/<slug>-hero.svg'
  tags: ['Shiny', '.NET']
  ---
  ```
- Voice: rework the docs post into a personal, first-person narrative ("Here's something that shouldn't be hard but is…", "So I built…") — story/motivation up front, not a dry changelog.
- **Hero image is required.** Create `src/assets/<slug>-hero.svg`:
  - SVG, `viewBox="0 0 1200 630"`, `width="1200" height="630"`.
  - Match the house style: dark navy/indigo gradient background (`#0f172a` → `#1e1b4b`), cyan/green/violet accent gradients, subtle glow filters, the feature name as the headline. Crib an existing one (e.g. `datasync-hero.svg`, `documentdb-orleans-hero.svg`) as a starting template.
