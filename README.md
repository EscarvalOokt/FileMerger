# FileMerger

FileMerger is a WPF desktop application for combining multiple text-based source and project files into a single output file.

It is intended for scenarios where you need to collect project files into one document for review, analysis, archiving, or sharing. The application can scan folders and individual files, apply merge rules, build a preview, inspect the result, and save the final merged output to disk.

## Highlights

* Merge content from multiple folders and individual files into one output file
* Use both **folder** and **file** sources in the same workspace
* Control recursion and enabled/disabled state per source
* Exclude nested folders and files per folder source
* Build and review a preview before saving
* Track when the current preview is outdated after source, session, profile, or file inclusion changes
* Review validation issues before preview generation
* Inspect the full normal discovery inventory with path, type, size, inclusion state, fallback status, and skip reason
* Override inclusion for eligible merge candidates
* Filter the discovered files list with search, **Only selected**, and grouped multi-select facets
* Save and load complete workspaces
* Work with multiple workspace tabs
* Reopen recent workspaces
* Manage reusable merge profiles through the Profile Manager
* Import, export, duplicate, save, and apply profiles
* Edit supported file types, filter rules, encoding options, metadata options, and C# options in profiles
* Use built-in presets for common project types
* Use guided first-workspace actions and empty states to get started faster
* Configure preview display and diagnostics behavior through Preferences
* Open and clean crash logs from the Preferences dialog
* Monitor build progress and cancel a preview build while it is running

## Documentation

- [Profile Authoring Guide](docs/profile-authoring-guide.md) — creating, importing, exporting, and manually editing profile JSON files

## Typical workflow

1. Create a new workspace or open an existing one.
2. Add one or more folder and/or file sources.
3. Optionally configure source-specific file or folder exclusions.
4. Choose a built-in profile or adjust the current profile settings.
5. Select an output file path.
6. Click **Build Preview**.
7. Review validation issues, discovered files, file inclusion states, and preview content.
8. Optionally override file inclusion for selected eligible merge candidates.
9. Click **Save** to write the merged output to disk.

## Merge capabilities

### Sources

A session can include multiple sources:

* folders
* individual files

For folder sources, recursion can be toggled per source. Sources can also be enabled or disabled without removing them from the workspace.

Folder sources support source-specific exclusions. This allows excluding nested folders or files from the normal discovery/session inventory for a specific source without changing global profile filter rules. When profile output metadata enables source-excluded reporting, those files can still be included in skipped-file audit metadata without becoming normal discovered/session files.

Typical examples:

* exclude `bin`
* exclude `obj`
* exclude `node_modules`
* exclude Unity `Library`
* exclude generated files inside a selected source folder

### File discovery and filtering

FileMerger discovers files from enabled sources and retains the full normal discovery inventory. Supported enabled files can become merge candidates, while known-but-disabled and unsupported files remain visible with explicit reasons for not participating in the merge.

The active merge profile is then applied to eligible merge candidates.

The profile controls:

* supported file types
* enabled/disabled file types
* include/exclude filter rules
* unsupported text fallback behavior
* sorting
* input encoding behavior
* output metadata behavior
* content transformations

After discovery and filtering, eligible merge candidates can still be manually included or excluded for the current workspace. Known-but-disabled and unsupported non-candidates remain visible for inspection but cannot be manually included.

### File inclusion workflow

After preview generation, FileMerger shows the discovered file list and lets you:

* review automatically included files
* review skipped files and skip reasons
* see files included through unsupported text fallback
* manually include or exclude selected eligible merge candidates
* reset selected overrides or all overrides
* combine Search and **Only selected** with grouped multi-select facets
* filter by inclusion (`Included`, `Not included`), reason/type (`Profile rule`, `Disabled type`, `Unsupported`, `Fallback`), and workflow (`Overridden`, `Not applied`) facets
* use contextual facet counts, the `N of M files shown` summary, and **Clear filters** to inspect or reset the current filtered result

Facet values within the same group are combined with OR, while active groups are combined with AND. Search and **Only selected** remain additional constraints.

This makes it possible to keep profile rules broad and still fine-tune eligible merge candidates for a specific workspace, while non-candidate files remain available for audit and inspection.

### Preview and save

The application builds a merged preview before saving output.

During preview generation, it reports progress for:

* validation
* file discovery
* file filtering
* manual inclusion override application
* file reading
* output building

Unsupported-text probing reports discovery progress while unknown files are being classified.

The generation summary reports discovered, included, and not-included files, with separate counts for disabled file types, unsupported files, profile exclusions, manual exclusions, other exclusion reasons, included fallback text files, and source-excluded audit entries when available.

If the preview is very large, the UI shows a truncated preview for responsiveness. Saving still writes the full merged output.

After changing sources, session settings, profile settings, or manual file overrides, the application marks the current preview as outdated until it is rebuilt.

Preview display settings do not affect generated output, copied preview content, or saved output.

## Workspaces

A workspace stores the current merge session state.

Workspace files use the `.filemerger.workspace.json` format.

A workspace stores:

* session name
* output path
* sources
* source-specific exclusions
* active profile snapshot
* supported file type selection
* profile filter rules
* active library profile linkage, when present
* saved profile origin/provenance metadata
* manual file inclusion overrides

### Workspace configuration

Session settings, Sources, and the workspace-local Profile are edited through the workspace-owned **Workspace Configuration** dialog.

The dialog uses a staged working copy:

* changes do not affect the live workspace until **OK** is confirmed
* **Cancel** discards staged changes without changing workspace or preview dirty state
* output path browsing, source changes, and workspace-local profile edits are performed against the staged copy

Workspace-local profile editing changes only the profile snapshot stored in that workspace. It does not implicitly write changes back to the corresponding Profile Library entry.

Profile linkage and origin are tracked separately:

* explicitly applying a library profile sets the active library linkage and records that profile as the current origin
* confirming local changes that diverge from a linked library profile clears the active linkage but keeps the saved origin identifier and display name
* a custom snapshot is not automatically relinked merely because its contents later match a library profile
* duplicating a workspace clears the active library linkage while preserving saved origin/provenance metadata

This allows the UI and persisted workspace state to distinguish a linked library profile, a custom workspace profile based on a library profile, and a fully custom workspace profile.

The application supports:

* save workspace
* save workspace as
* open workspace
* multiple workspace tabs
* duplicate tab
* rename tab
* close tab
* close other tabs
* next/previous tab shortcuts
* dirty-state guards before destructive actions

## Recent workspaces

FileMerger stores a recent workspace list locally and exposes it through the application menu and first-workspace guidance.

Recent workspace behavior includes:

* persistence between runs
* deduplication by normalized path
* max-entry trimming
* missing file handling
* open recent workspace
* remove missing recent workspace
* clear recent workspaces

Recent workspaces are not automatically reopened on startup.

## Profiles

FileMerger includes a profile library for storing reusable merge configurations.

Profiles define how files are discovered, filtered, transformed, and represented in the generated output.

### Built-in profiles

The current baseline includes these built-in presets:

* **Default** — balanced default profile for everyday source merging
* **C# Minimal** — only C# source files with common generated/build artifacts excluded
* **Docs and Config** — documentation, text, and configuration-oriented files
* **JavaScript Project** — JavaScript/Node project files with dependency folders, caches, and build outputs excluded
* **TypeScript Project** — TypeScript project files with dependency folders, caches, and build outputs excluded
* **WPF Application** — WPF/.NET application files with common generated and build artifacts excluded
* **Unity Project** — Unity C#, project settings, and serialized asset files; source exclusions are intended for folders such as `Library`, `Temp`, and build outputs
* **Full Source Dump** — all supported file types with minimal filtering

Built-in profiles are read-only. To edit a built-in profile, duplicate it or save it as a user-defined profile.

### Profile operations

From the Profile Manager, you can:

* browse built-in and user profiles
* create a new profile
* edit a profile
* save changes
* save as a new profile
* duplicate an existing profile
* delete user-defined profiles
* import profile files
* export profile files
* apply a profile to the current workspace

User-defined profiles are stored in the local application data folder.


The Profile Library and each workspace-local profile snapshot are separate. Applying a library profile copies its configuration into the current workspace and establishes the active linkage/origin described above. Later workspace-local edits are staged and stored with the workspace; they do not modify the reusable library profile unless the user explicitly edits and saves that library entry through the Profile Manager.

For details about creating, importing, exporting, and manually editing profile JSON files, see [Profile Authoring Guide](docs/profile-authoring-guide.md).

## Supported file types

The file type catalog includes support for common source, configuration, documentation, .NET/WPF, JavaScript, TypeScript, and Unity-oriented files.

### Common source and config files

* `.cs`
* `.xml`
* `.xaml`
* `.json`
* `.jsonc`
* `.txt`
* `.md`
* `.yml`
* `.yaml`
* `.toml`
* `.ini`
* `.cfg`
* `.conf`
* `.editorconfig`
* `.lock`

### Web and JavaScript/TypeScript files

* `.js`
* `.jsx`
* `.mjs`
* `.cjs`
* `.ts`
* `.tsx`
* `.mts`
* `.cts`
* `.html`
* `.htm`
* `.css`
* `.scss`
* `.sass`
* `.less`
* `.graphql`
* `.gql`

### .NET / WPF project files

* `.config`
* `.props`
* `.targets`
* `.csproj`
* `.sln`
* `.slnx`
* `.resx`
* `.settings`
* `.manifest`
* `.pubxml`
* `.ruleset`

### Unity project files

* `.asmdef`
* `.asmref`
* `.unity`
* `.prefab`
* `.asset`
* `.meta`
* `.mat`
* `.controller`
* `.anim`
* `.overrideController`
* `.playable`
* `.physicMaterial`
* `.physicsMaterial2D`
* `.spriteatlas`
* `.inputactions`
* `.shader`
* `.compute`
* `.hlsl`
* `.cginc`
* `.uxml`
* `.uss`
* `.shadergraph`
* `.vfx`

The default profile enables a narrower subset focused on common source, markup, JSON, and text/config files. Project-specific presets enable broader sets for their target project types.

## Available merge profile options

### General options

Profiles can control:

* include header comment
* include file separators
* include relative path in separators
* trim trailing empty lines
* line ending mode

  * preserve
  * CRLF
  * LF
* sort mode

  * none
  * relative path ascending
  * relative path descending
* input encoding mode

  * auto
  * UTF-8
  * specific encoding
* preferred input encoding name
* fallback input encoding name

### Unsupported text fallback

Unsupported text fallback is an opt-in profile-level feature.

When enabled, FileMerger can include files with unsupported extensions if they look like text files and pass fallback safety checks.

Fallback behavior can be limited by:

* max file size
* probe size
* max control character ratio

This setting changes which files can be included in generated output, so it belongs to profiles rather than application preferences.

### Output metadata options

Profiles can control generated output metadata:

* include build timestamp
* include session name
* include output path
* include file summary
* skipped files metadata mode

  * none
  * simple
  * detailed
* include source-excluded files in skipped-file metadata

Source-excluded reporting is opt-in and only contributes to skipped-file metadata when the skipped-files mode is `simple` or `detailed`. It does not make source-excluded files merge candidates or add them to the normal discovered/session inventory.

Output metadata changes generated output and is therefore profile-level behavior.

### C# transformations

Profiles can enable C#-specific processing:

* remove `using` directives

Line ending normalization and trailing-empty-line trimming are also represented as profile-driven transformations internally.

### Filter rules

Profiles can contain ordered include/exclude rules applied after file discovery and file type selection.

Each rule can define:

* mode

  * include
  * exclude
* target

  * file name
  * extension
  * relative path
  * directory segment
* match type

  * exact
  * contains
  * wildcard
  * regex
* pattern
* optional description
* enabled/disabled state

Common examples:

* exclude `bin` directories
* exclude `obj` directories
* exclude `node_modules`
* exclude `*.Designer.cs`
* exclude `AssemblyInfo.cs`

## Encoding behavior

When input encoding mode is set to **Auto**, the reader:

1. tries to detect encoding from BOM;
2. falls back to strict UTF-8 decoding;
3. uses the configured fallback encoding if UTF-8 decoding fails.

The default fallback encoding in the current default profile is `windows-1251`.

The main session UI exposes the output path. Advanced output encoding is currently not exposed as a full user-facing main-session setting.

## Application preferences

Application preferences are separate from merge profiles.

Preferences store UI/workflow/display settings that do not change generated output.

Current preferences include:

* preview line wrap default
* preview display character limit
* crash log retention limit

Preview display preferences affect only the UI display of preview text. They do not affect generated output, copied preview content, or saved output.

Preferences are stored locally in:

```text
%LocalAppData%\FileMerger\application-preferences.json
```

## Diagnostics

FileMerger writes crash logs for startup, dispatcher, domain, and task-level unhandled exceptions.

Crash logs are stored locally in:

```text
%LocalAppData%\FileMerger\CrashLogs
```

The Preferences dialog exposes diagnostics maintenance actions:

* view crash log folder path
* open crash log folder
* configure crash log retention limit
* clear old logs according to retention
* clear all logs

Crash log maintenance does not change crash capture behavior.

## Local storage locations

FileMerger uses local application data for user-specific files:

```text
%LocalAppData%\FileMerger\
```

Important subpaths:

```text
%LocalAppData%\FileMerger\Profiles
%LocalAppData%\FileMerger\CrashLogs
%LocalAppData%\FileMerger\application-preferences.json
```

Workspace files are saved wherever the user chooses and use the `.filemerger.workspace.json` extension.

## Requirements

* Windows
* .NET 10 SDK / runtime

## Notes and current limitations

* Preview generation stops on validation errors; warnings can still be shown as part of a successful build.
* Very large previews are truncated in the UI for readability, while saving still writes the full output.
* After changing sources, session settings, profile settings, or manual file overrides, the application marks the preview as outdated until it is rebuilt.