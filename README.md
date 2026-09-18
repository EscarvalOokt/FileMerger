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
* Reject invalid filter-rule drafts before applying or saving them, or building a new preview
* Inspect the full normal discovery inventory with path, type, size, inclusion state, fallback status, and skip reason
* Override inclusion for eligible merge candidates
* Filter the discovered files list with search, **Only selected**, and grouped multi-select facets
* Save and load complete workspaces
* Work with multiple workspace tabs
* Reopen recent workspaces
* Manage reusable merge profiles through the Profile Manager
* Import, export, duplicate, save, and apply profiles
* Edit supported file types, filter rules, encoding options, and metadata options in profiles
* Use built-in presets for common project types
* Use guided first-workspace actions and empty states to get started faster
* Configure preview display and diagnostics behavior through Preferences
* Open and clean crash logs from the Preferences dialog
* Monitor build progress and cancel a preview build while it is running
* Check for newer compatible FileMerger versions, download and validate update packages, and install verified updates from the application UI

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
8. Optionally override file inclusion for selected eligible merge candidates. After changing inclusion, click **Build Preview** again and review the updated result.
9. Click **Save** to write the latest built output, or **Save Last Build** when the retained preview is outdated.

## Application updates

FileMerger exposes an explicit application update workflow through **Help > Check for Updates**. Update state belongs to the application as a whole and is independent of the active workspace.

The update workflow is deliberately separated into user-controlled actions:

1. **Check for Updates** reads the configured HTTPS release manifest and compares the running application version with the newer compatible release/package described by that manifest.
2. **Download** downloads the selected package into update staging and validates its compatibility, exact size, SHA-256 digest, ZIP safety, and required application payload.
3. **Install** is available only after the package reaches the verified state.
4. Installation preparation revalidates the package and installation directory before any shutdown begins. Active merge/preview work blocks installation instead of being force-canceled.
5. Unsaved workspaces and other guarded application windows keep their existing save/discard/cancel behavior. Canceling a guard leaves FileMerger running and does not start installation.
6. After guarded shutdown is approved, a separate updater process replaces the application files, restarts FileMerger, and waits for the restarted application to complete startup, open its main window, and confirm the expected version. If replacement, restart, startup verification, or version verification fails, the updater attempts to restore the previous application installation.

Checking does not start a download automatically, and downloading a package does not start installation automatically. The release manifest URI is supplied by build/release configuration rather than by a workspace or profile.

## Merge capabilities

### Sources

A session can include multiple sources:

* folders
* individual files

For folder sources, recursion can be toggled per source. Sources can also be enabled or disabled without removing them from the workspace.

Folder sources support source-specific exclusions. This allows excluding nested folders or files from the normal discovery/session inventory for a specific source without changing global profile filter rules. Source-excluded files remain a separate audit path. When skipped-file metadata is enabled and the effective category policy includes **Source exclusions**, those files can be reported in skipped-file metadata without becoming normal discovered/session files.

Typical examples:

* exclude `bin`
* exclude `obj`
* exclude `node_modules`
* exclude Unity `Library`
* exclude generated files inside a selected source folder

### File discovery and filtering

FileMerger discovers files from enabled sources and retains the full normal discovery inventory. Supported enabled files can become merge candidates, while known-but-disabled and unsupported files remain visible with explicit reasons for not participating in the merge.

For recursive folder discovery, nested directory reparse points, including junctions and symbolic links, are traversal boundaries and are not followed. An explicitly selected root folder may itself be a directory link/reparse point and is still scanned as the source root. Source-exclusion audit traversal likewise does not cross directory reparse points.

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

After discovery and filtering, eligible merge candidates can still be manually included or excluded for the current workspace. Known-but-disabled and unsupported non-candidates remain visible for inspection but cannot be manually included. If an intended-included file cannot be read during preview generation, it is accounted for as a skipped processing failure and the read failure is also reported as a validation issue.

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

Invalid filter rules block a new preview before session creation, file discovery, or output building. The Validation pane and operation status report the rule error; the application does not silently omit the invalid rule and build a different result. An empty output path is likewise reported through the Validation pane and operation status before session creation. A Regex rule that exceeds the runtime evaluation timeout fails the preview explicitly during filtering. A failed preview does not replace the existing discovered-file state or pending manual inclusion overrides, and any previous successful result and preview remain retained.

During preview generation, it reports progress for:

* validation
* file discovery
* file filtering
* manual inclusion override application
* file reading
* output building

Unsupported-text probing reports discovery progress while unknown files are being classified.

The generation summary reports discovered, included, and not-included files, with separate counts for disabled file types, unsupported files, profile exclusions, manual exclusions, **Other**, included fallback text files, and source-excluded audit entries when available. Processing failures are included in **Other** in this summary; they do not have a separate summary counter. They remain a distinct category in skipped-file metadata and retain the validation issue for the read error.

If the preview is very large, the UI shows a truncated preview for responsiveness. Saving still writes the full merged output.

When source, session, profile, or file inclusion settings differ from those used for the last build, the application marks the retained preview as outdated.

**Save writes the last successfully built output, not the current edited configuration or pending file selection.** It does not rebuild automatically. When the retained preview is outdated, the action is labeled **Save Last Build** and its tooltip explains that changes made since that build are not included. A successful stale save reports the same distinction in the operation status, and the preview remains outdated until **Build Preview** succeeds again. Run **Build Preview** to include pending changes in the saved result.

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
* invalid filter rules block **OK**; its tooltip explains the first rule error, including while the button is disabled
* **Cancel** discards staged changes without changing workspace or preview dirty state
* output path browsing and source changes are performed against the staged copy
* the compact profile overview exposes **Edit local profile** and **Choose profile** as separate actions
* **Edit local profile** opens a dedicated editor for a detached working copy; its **Cancel** leaves the staged workspace configuration unchanged, while its **OK** updates only the staged profile until the outer dialog is confirmed
* **Choose profile** reuses the Profile Manager against the staged workspace configuration; a library profile applied there is shown as **Selected** and updates only the staged configuration until the outer **OK** is confirmed

Workspace-local profile editing changes only the profile snapshot stored in that workspace. It does not implicitly write changes back to the corresponding Profile Library entry.

Profile linkage and origin are tracked separately:

* applying an unchanged saved profile from the Profile Manager sets the active library linkage and records that profile as the current origin
* applying an edited or unsaved Profile Manager draft copies it as a custom workspace profile without active library linkage and clears any previous origin; save the draft first to apply it as a saved library entry
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
* dirty-state guards when closing workspace tabs, including **Close other tabs**
* application-wide dirty-workspace protection during normal exit

Normal application exit coordinates the existing workspace close guards across all tabs. Dirty workspaces use the same **Save / Discard / Cancel** flow as other guarded close operations; canceling a prompt or failing to save keeps FileMerger open instead of silently discarding changes.

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

Built-in library entries are read-only, but their working copies can be edited in the Profile Manager. **Save** creates a user-defined copy instead of overwriting the built-in entry. **Duplicate** starts a separate draft, and **Save As** saves a new user profile.

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

**Apply**, **Save**, **Save As**, and **Create** reject a draft with invalid filter rules. The same check is used when choosing **Save** in the unsaved-changes dialog. A rejected save leaves the draft in the editor and cancels the requested profile switch or close; **Discard** and **Cancel** retain their existing meanings. The current draft error is kept visible near the relevant actions, while Apply/OK tooltips also explain the first rule error; a rejected save through the dialog reports its reason in the Profile Manager status.

The Profile Library and each workspace-local profile snapshot are separate. Applying a saved, unchanged library entry establishes the linkage/origin described above; applying an edited or unsaved draft does not. Later workspace-local edits are staged and stored with the workspace; they do not modify the reusable library profile unless the user explicitly edits and saves that library entry through the Profile Manager.

Loading or importing structurally readable profile JSON is separate from validating an editable draft. A previously saved or imported profile with invalid rules can still be opened for correction. Its presence in the library does not mean it is ready to apply, save through the editor, or use for a new preview.

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

  * `None` — do not write a skipped-file section
  * `Simple` — list the paths of selected skipped files
  * `Detailed` — list selected skipped files with skip reasons and filter-rule details when available
* skipped-file categories

  * Disabled file types
  * Unsupported files
  * Profile exclusions
  * Manual exclusions
  * Source exclusions
  * Processing failures
  * Other

The skipped-files mode controls **representation**, while the category selection controls **membership**. `None` suppresses the skipped-file section regardless of the selected categories. `Simple` and `Detailed` apply only to skipped entries whose semantic categories are enabled.

Category selection does not change whether a file is included or skipped. It only controls which already-skipped entries may appear in generated metadata. A processing failure is a file that was intended for inclusion but could not be read; the read error is also reported through validation.

Source exclusions remain a separate audit path rather than part of the normal discovered/session inventory. Enabling the **Source exclusions** category allows those audit entries to appear in skipped-file metadata, but does not make them merge candidates or make them eligible for manual inclusion overrides. **Other** is the fallback category for skipped reasons that do not map to one of the known semantic categories.

Output metadata changes generated output and is therefore profile-level behavior.

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

Rule validation covers the entire rule collection, including disabled rules and rules hidden by the editor's search or filters. Fix invalid patterns rather than disabling or hiding the affected rows. The [Profile Authoring Guide](docs/profile-authoring-guide.md#filter-rules) describes the existing pattern constraints and how to correct older profiles.

## Encoding behavior

When input encoding mode is set to **Auto**, the reader:

1. detects UTF-8, UTF-16 LE/BE, and UTF-32 LE/BE encodings from BOM when present;
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

Preview display preferences affect only the UI display of preview text. They do not affect generated output, copied preview content, or saved output. **Wrap preview lines** in the preview toolbar changes the current active preview and saves the same choice as the default for new workspace tabs. Changing **Wrap preview lines by default** in Preferences changes the default for future tabs without retroactively changing already-open tabs. Invalid preview display limits are shown beside the affected setting and block saving Preferences until corrected.

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
%LocalAppData%\FileMerger\Updates
%LocalAppData%\FileMerger\application-preferences.json
```

Workspace files are saved wherever the user chooses and use the `.filemerger.workspace.json` extension.

## Requirements

### Running the application

* Windows
* .NET 10 Desktop Runtime for a framework-dependent build

The existing folder publish profile is framework-dependent (`SelfContained=false`). A runtime installation is for running the application; it does not replace the SDK needed to build from source.
The automatic updater replaces FileMerger application files only; it does not install or update the .NET Desktop Runtime.

### Building from source

* Windows
* .NET 10 SDK
* The external `Escarval.Wpf.Windowing` project at the path referenced by the solution and WPF project

Relative to the FileMerger repository root, the external project is expected at:

```text
../Libs/Escarval.Wpf.Windowing/Escarval.Wpf.Windowing/Escarval.Wpf.Windowing.csproj
```

The projects target `net10.0-windows`. An older SDK that cannot target .NET 10 is not sufficient. The external project must be available when building the solution or its WPF-dependent tests.

## Notes and current limitations

* Invalid filter rules block a new preview before its pipeline starts. Errors reported by session validation also stop generation; warnings can still be shown as part of a successful build.
* Very large previews are truncated in the UI for readability, while saving still writes the full output.
* A retained preview can become outdated after source, session, profile, or manual inclusion changes. **Save Last Build** still uses the last successful build; rebuild explicitly to include the changes.