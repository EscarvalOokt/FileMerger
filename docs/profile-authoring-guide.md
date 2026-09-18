# FileMerger Profiles

This document describes how to add, duplicate, import, export, manually create, and manually edit FileMerger profile files.

Profiles define how FileMerger discovers, filters, transforms, and writes files into the final merged output.

## Recommended way to create profiles

The safest way to create a profile is through the application UI.

Use the Profile Manager to:

1. open the profile library;
2. select a built-in or user profile;
3. duplicate it or use **Save As**;
4. adjust file types, filter rules, encoding options, and metadata options, and correct any rule errors;
5. save the profile;
6. apply it to the current workspace.

Built-in library entries are read-only, but the Profile Manager allows editing their working copies. **Save** saves an edited built-in profile as a new user profile rather than overwriting the built-in entry. **Duplicate** starts a separate draft, and **Save As** is another way to save a new user profile.

## Import and export

Profiles can be exported to standalone JSON files and imported later.

Use this workflow when you want to:

* back up a profile;
* copy a profile between machines;
* inspect or manually edit a profile;
* keep a profile under version control.

Recommended workflow for manual editing:

1. Export an existing profile from the Profile Manager.
2. Make a backup copy of the exported JSON file.
3. Edit the copy manually.
4. Import the edited file back into FileMerger.
5. Open it in the Profile Manager, inspect the rules, and correct any validation errors before saving the draft.
6. Save the corrected profile, apply it to a test workspace, and build preview.

This is safer than creating a large profile JSON document from scratch.

## User profile storage

User-defined profiles are stored in:

```text
%LocalAppData%\FileMerger\Profiles
```

FileMerger loads user profile files matching this pattern:

```text
*.filemerger.profile.json
```

Example:

```text
%LocalAppData%\FileMerger\Profiles\My Profile.1a2b3c4d.filemerger.profile.json
```

Built-in profiles are provided by the application and are not stored in this folder as editable files.

## Profile file format

A profile file is a JSON document with three top-level properties:

```json
{
  "schemaVersion": 1,
  "metadata": {},
  "profile": {}
}
```

The current schema version is:

```json
"schemaVersion": 1
```

Files with unsupported schema versions are ignored by the profile loader.

## Important JSON rules

FileMerger profile JSON uses:

* camelCase property names;
* numeric enum values;
* `.filemerger.profile.json` file name suffix;
* `schemaVersion: 1`;
* a present, non-null `metadata` object;
* a present, non-null `profile` object.

The empty objects in the format outline show the document structure, not a recommended complete profile. Populate the fields using the examples and authoring checklist below.

The app currently serializes enums as numbers, not strings. For example:

```json
"lineEndingMode": 0
```

not:

```json
"lineEndingMode": "Preserve"
```

If a profile file cannot be read or deserialized, uses an unsupported schema version, or lacks a non-null `metadata` or `profile` object, it is ignored by the loader. Requiring these objects is not a check that every recommended field has been filled in correctly.

### Loading a file and using its profile

Structural loading and filter-rule validation are separate. The loader checks that it can read and deserialize the document, that the schema version is supported, and that the two required objects are present. It does not run the Profile Editor's filter-rule validation.

Consequently, a structurally readable profile containing an empty or otherwise invalid rule pattern can appear in the library. Import can also store such a profile as a new library entry. Import success does not certify that its rules are valid.

When that profile is opened, the editor keeps the rule for correction. **Apply**, **Save**, **Save As**, and **Create**, including **Save** from the unsaved-changes dialog, reject an editable draft with invalid filter rules. A rejected save keeps the draft in the editor and prevents the requested switch or close. In Workspace Configuration, **OK** rejects invalid rules as well.

A workspace that already contains invalid rules cannot produce a new preview: the error is reported before the merge pipeline starts. The rules are not silently dropped, and a previous successful result is retained when available. Correct the draft before saving it through the Profile Manager or applying it, then rebuild preview.

These are filter-rule checks, not a claim that every setting is fully validated during import or in every editor. The authoring checklist remains necessary.

## Metadata object

The `metadata` object describes the profile library entry.

Example:

```json
{
  "id": "custom.docs-config",
  "name": "Custom Docs and Config",
  "description": "Documentation and configuration files for project dumps.",
  "createdAtUtc": "2026-07-07T00:00:00Z",
  "updatedAtUtc": "2026-07-07T00:00:00Z",
  "isBuiltIn": false,
  "isReadOnly": false
}
```

Fields:

| Field          |        Type | Notes                                                       |
| -------------- | ----------: | ----------------------------------------------------------- |
| `id`           |      string | Unique profile id. Avoid `builtin.*` ids for user profiles. |
| `name`         |      string | Display name shown in the Profile Manager.                  |
| `description`  | string/null | Optional profile description.                               |
| `createdAtUtc` | string/null | ISO-8601 UTC timestamp.                                     |
| `updatedAtUtc` | string/null | ISO-8601 UTC timestamp.                                     |
| `isBuiltIn`    |     boolean | Use `false` for manual user profiles.                       |
| `isReadOnly`   |     boolean | Use `false` for manual user profiles.                       |

For manually authored profiles, use a stable non-empty `id` and a non-empty `name`. The current loader is tolerant when reading user profile files directly from the local profile folder: a missing or blank `metadata.id` is normalized to a newly generated id, and a missing or blank `metadata.name` is normalized to `Profile`. This tolerance is recovery behavior rather than the recommended authoring format.

The editor has a separate normalization rule: assigning a blank working profile name normalizes it to `Default`. This does not change the file loader's `Profile` fallback and does not make an empty name the recommended JSON format.

When importing a profile through the UI, FileMerger creates a new user profile entry and assigns a new id. When loading user profiles directly from the local profile folder, user files are treated as user-defined profiles.

## Profile object

The `profile` object contains merge behavior.

Main fields:

| Field                                     |        Type | Notes                                                |
| ----------------------------------------- | ----------: | ---------------------------------------------------- |
| `includeHeaderComment`                    |     boolean | Adds an auto-generated header to output.             |
| `includeFileSeparators`                   |     boolean | Adds separators between file sections.               |
| `includeRelativePathInSeparator`          |     boolean | Uses relative path in section separator text.        |
| `trimTrailingEmptyLines`                  |     boolean | Trims trailing empty lines from file content.        |
| `fileTypes`                               |       array | Supported file type selection.                       |
| `lineEndingMode`                          |      number | See enum table below.                                |
| `sortMode`                                |      number | See enum table below.                                |
| `inputEncodingMode`                       |      number | See enum table below.                                |
| `preferredInputEncodingName`              | string/null | Used when input encoding mode is Specific.           |
| `fallbackInputEncodingName`               | string/null | Used by Auto mode when UTF-8 decoding fails.         |
| `filterRules`                             |  array/null | Ordered include/exclude filter rules.                |
| `includeUnsupportedTextFiles`             |     boolean | Enables unsupported text fallback.                   |
| `unsupportedTextMaxFileSizeBytes`         |      number | Max fallback file size.                              |
| `unsupportedTextProbeSizeBytes`           |      number | Probe size for fallback text detection.              |
| `unsupportedTextMaxControlCharacterRatio` |      number | Allowed control character ratio, 0.0–1.0.            |
| `includeBuildTimestampMetadata`           |     boolean | Adds build timestamp metadata if header is enabled.  |
| `includeSessionNameMetadata`              |     boolean | Adds session name metadata if header is enabled.     |
| `includeOutputPathMetadata`               |     boolean | Adds output path metadata if header is enabled.      |
| `includeFileSummaryMetadata`              |     boolean | Adds file summary metadata if header is enabled.     |
| `skippedFilesMetadataMode`                |      number | Controls skipped-file metadata representation. See enum table below. |
| `includeSourceExcludedFiles`              |     boolean | Legacy compatibility fallback for source-exclusion reporting. Defaults to `false` when missing. |
| `skippedFileCategories`                   | object/null | Explicit semantic category selection for skipped-file metadata. `null` or missing uses legacy-compatible fallback behavior. |

### Legacy `removeUsingDirectives` compatibility

Profile schema version `1` is unchanged. A schema-1 profile that still contains the legacy `removeUsingDirectives` property remains readable: the property is ignored when the profile is loaded, and it is not serialized when the profile is saved again.

### Skipped-file metadata policy

Skipped-file metadata has two independent profile settings:

* `skippedFilesMetadataMode` controls **representation**:
  * `0` (`None`) — no skipped-file section is written;
  * `1` (`Simple`) — selected skipped entries are listed by path;
  * `2` (`Detailed`) — selected skipped entries also include skip reasons and filter-rule details when available.
* `skippedFileCategories` controls **membership**: which semantic categories of already-skipped entries are allowed to appear in that section.

An explicit `skippedFileCategories` object has this shape:

```json
{
  "includeDisabledFileTypes": true,
  "includeUnsupportedFiles": true,
  "includeProfileExclusions": true,
  "includeManualExclusions": true,
  "includeSourceExclusions": false,
  "includeProcessingFailures": true,
  "includeOther": true
}
```

The fields correspond to the categories exposed by the Profile Editor:

| Field | Category | Meaning |
| ----- | -------- | ------- |
| `includeDisabledFileTypes` | Disabled file types | Known file types disabled by the current profile. |
| `includeUnsupportedFiles` | Unsupported files | Files that are not supported and did not become fallback text candidates. |
| `includeProfileExclusions` | Profile exclusions | Files skipped by profile filter rules. |
| `includeManualExclusions` | Manual exclusions | Eligible merge candidates excluded by a workspace manual override. |
| `includeSourceExclusions` | Source exclusions | Source-specific audit exclusions kept outside the normal discovered/session inventory. |
| `includeProcessingFailures` | Processing failures | Files intended for inclusion that could not be read during processing. |
| `includeOther` | Other | Fallback for skipped reasons that do not map to one of the known semantic categories. |

Category selection does not change whether a file is included or skipped. It only controls metadata membership. `None` suppresses the skipped-file section regardless of category values, while `Simple` and `Detailed` format the selected entries.

Source exclusions remain a separate audit path. Enabling `includeSourceExclusions` allows those audit entries to appear in skipped-file metadata, but does not make them merge candidates, add them to the normal discovered/session inventory, or make them eligible for manual inclusion overrides.

A processing failure remains a skipped file and also retains its validation signal for the read error.

### Backward compatibility for skipped-file categories

The category-selection feature is a backward-compatible additive extension of profile schema version `1`; the schema version remains `1`.

When `skippedFileCategories` is missing or `null`, FileMerger preserves legacy-compatible behavior:

* Disabled file types — enabled;
* Unsupported files — enabled;
* Profile exclusions — enabled;
* Manual exclusions — enabled;
* Processing failures — enabled;
* Other — enabled;
* Source exclusions — controlled by `includeSourceExcludedFiles`.

If `includeSourceExcludedFiles` is also missing, its legacy default is `false`.

When `skippedFileCategories` is present, all seven values in that object are authoritative. In particular, `skippedFileCategories.includeSourceExclusions` takes precedence over the legacy `includeSourceExcludedFiles` value. The legacy field remains part of schema version `1` for compatibility but is not a second source of truth when an explicit category object exists.

Loading and saving a legacy profile does not by itself materialize an explicit category object. A missing or `null` `skippedFileCategories` value remains legacy state until the category policy is actually changed through the Profile Editor or an explicit object is authored manually.

## File type entries

Each `fileTypes` item has this shape:

```json
{
  "extension": ".cs",
  "displayName": "C# source",
  "kind": 1,
  "isEnabled": true,
  "supportsLanguageSpecificProcessing": true
}
```

Fields:

| Field                                |    Type | Notes                                                             |
| ------------------------------------ | ------: | ----------------------------------------------------------------- |
| `extension`                          |  string | File extension or special dot-file name such as `.editorconfig`.  |
| `displayName`                        |  string | Display name shown in UI.                                         |
| `kind`                               |  number | File kind enum value.                                             |
| `isEnabled`                          | boolean | Whether this known file type is enabled for merge candidacy by the profile. |
| `supportsLanguageSpecificProcessing` | boolean | Catalog metadata retained in profile files. `true` does not imply that a current user-facing language-specific transformation option is available. |

For predictable results, export an existing profile and edit its `fileTypes` list. Exported profiles contain the catalog-compatible structure expected by the current app.

If a profile contains only a subset of known file types, missing known catalog types are treated as disabled when the profile is loaded in the editor.

## Filter rules

Each filter rule has this shape:

```json
{
  "mode": 1,
  "target": 3,
  "patternType": 0,
  "pattern": "bin",
  "isEnabled": true,
  "description": "Exclude bin directory",
  "isUserEditable": true
}
```

Fields:

| Field            |        Type | Notes                                                                 |
| ---------------- | ----------: | --------------------------------------------------------------------- |
| `mode`           |      number | Include or exclude.                                                   |
| `target`         |      number | What value the rule checks.                                           |
| `patternType`    |      number | Exact, contains, wildcard, or regex.                                  |
| `pattern`        |      string | Match pattern. Must not be empty.                                     |
| `isEnabled`      |     boolean | Whether the rule is active.                                           |
| `description`    | string/null | Optional description shown in UI.                                     |
| `isUserEditable` |     boolean | Controls whether the editor can remove the rule, individually or when clearing rules. It does not make the row fields read-only. Use `true` for manually authored rules. |

### Rule validity in the editor

The current checks apply to the trimmed pattern:

* Every rule needs a non-empty, non-whitespace pattern.
* A `DirectorySegment` pattern must not contain `/` or `\`, regardless of its pattern type.
* An `Extension` pattern must start with `.`, regardless of its pattern type.
* A `Regex` pattern must be a valid .NET regular expression.

These checks cover the complete rule collection, including disabled rules and rows hidden by the editor's search or status filters. `isEnabled` controls whether a valid rule participates in filtering; it is not a way to bypass validation. An invalid row blocks applying or saving the draft and building a new preview.

`DirectorySegment` matching evaluates directory components only; the file name itself is never treated as a directory segment.

Regex syntax validation and runtime evaluation are separate. A syntactically valid Regex rule is still evaluated with a 250 ms per-match timeout during preview filtering. If that timeout is exceeded, preview generation fails with an explicit filter-rule validation error instead of treating the expression as a match or no-match.

`isUserEditable: false` prevents the editor's remove/clear actions from deleting that rule. It is not a general edit lock: the current UI still allows changing the row's pattern and other editable fields.

### Common examples

Exclude `bin` directory:

```json
{
  "mode": 1,
  "target": 3,
  "patternType": 0,
  "pattern": "bin",
  "isEnabled": true,
  "description": "Exclude bin directory",
  "isUserEditable": true
}
```

Exclude designer C# files:

```json
{
  "mode": 1,
  "target": 0,
  "patternType": 2,
  "pattern": "*.Designer.cs",
  "isEnabled": true,
  "description": "Exclude designer files",
  "isUserEditable": true
}
```

Include Markdown extension:

```json
{
  "mode": 0,
  "target": 1,
  "patternType": 0,
  "pattern": ".md",
  "isEnabled": true,
  "description": "Include Markdown files",
  "isUserEditable": true
}
```

## Enum values

Profile JSON currently uses numeric enum values.

### FileKind

| Value | Name       |
| ----: | ---------- |
|     0 | Unknown    |
|     1 | CSharp     |
|     2 | Xml        |
|     3 | Xaml       |
|     4 | Json       |
|     5 | Text       |
|     6 | JavaScript |
|     7 | TypeScript |
|     8 | Html       |
|     9 | Stylesheet |
|    10 | GraphQl    |

### LineEndingMode

| Value | Name     |
| ----: | -------- |
|     0 | Preserve |
|     1 | CRLF     |
|     2 | LF       |

### SortMode

| Value | Name                     |
| ----: | ------------------------ |
|     0 | None                     |
|     1 | ByRelativePathAscending  |
|     2 | ByRelativePathDescending |

### InputEncodingMode

| Value | Name     |
| ----: | -------- |
|     0 | Auto     |
|     1 | Utf8     |
|     2 | Specific |

### FilterMode

| Value | Name    |
| ----: | ------- |
|     0 | Include |
|     1 | Exclude |

### FilterTarget

| Value | Name             |
| ----: | ---------------- |
|     0 | FileName         |
|     1 | Extension        |
|     2 | RelativePath     |
|     3 | DirectorySegment |

### RulePatternType

| Value | Name     |
| ----: | -------- |
|     0 | Exact    |
|     1 | Contains |
|     2 | Wildcard |
|     3 | Regex    |

### SkippedFilesMetadataMode

| Value | Name     |
| ----: | -------- |
|     0 | None     |
|     1 | Simple   |
|     2 | Detailed |

## Minimal example profile

This example enables common documentation/config files and excludes common generated folders.

Save it as:

```text
%LocalAppData%\FileMerger\Profiles\Custom Docs Config.custom-docs.filemerger.profile.json
```

Example JSON:

```json
{
  "schemaVersion": 1,
  "metadata": {
    "id": "custom.docs-config",
    "name": "Custom Docs and Config",
    "description": "Documentation, JSON, XML, YAML, TOML, and text/config files.",
    "createdAtUtc": "2026-07-07T00:00:00Z",
    "updatedAtUtc": "2026-07-07T00:00:00Z",
    "isBuiltIn": false,
    "isReadOnly": false
  },
  "profile": {
    "includeHeaderComment": true,
    "includeFileSeparators": true,
    "includeRelativePathInSeparator": true,
    "trimTrailingEmptyLines": true,
    "fileTypes": [
      {
        "extension": ".txt",
        "displayName": "Text",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".md",
        "displayName": "Markdown",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".json",
        "displayName": "JSON",
        "kind": 4,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".xml",
        "displayName": "XML",
        "kind": 2,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".yml",
        "displayName": "YAML",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".yaml",
        "displayName": "YAML",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".toml",
        "displayName": "TOML",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".ini",
        "displayName": "INI",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".cfg",
        "displayName": "CFG",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".conf",
        "displayName": "CONF",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      },
      {
        "extension": ".editorconfig",
        "displayName": "EditorConfig",
        "kind": 5,
        "isEnabled": true,
        "supportsLanguageSpecificProcessing": false
      }
    ],
    "lineEndingMode": 0,
    "sortMode": 1,
    "inputEncodingMode": 0,
    "preferredInputEncodingName": null,
    "fallbackInputEncodingName": "windows-1251",
    "filterRules": [
      {
        "mode": 1,
        "target": 3,
        "patternType": 0,
        "pattern": "bin",
        "isEnabled": true,
        "description": "Exclude bin directory",
        "isUserEditable": true
      },
      {
        "mode": 1,
        "target": 3,
        "patternType": 0,
        "pattern": "obj",
        "isEnabled": true,
        "description": "Exclude obj directory",
        "isUserEditable": true
      },
      {
        "mode": 1,
        "target": 3,
        "patternType": 0,
        "pattern": ".git",
        "isEnabled": true,
        "description": "Exclude Git directory",
        "isUserEditable": true
      }
    ],
    "includeUnsupportedTextFiles": false,
    "unsupportedTextMaxFileSizeBytes": 1048576,
    "unsupportedTextProbeSizeBytes": 16384,
    "unsupportedTextMaxControlCharacterRatio": 0.1,
    "includeBuildTimestampMetadata": true,
    "includeSessionNameMetadata": true,
    "includeOutputPathMetadata": true,
    "includeFileSummaryMetadata": true,
    "skippedFilesMetadataMode": 2,
    "includeSourceExcludedFiles": false,
    "skippedFileCategories": {
      "includeDisabledFileTypes": true,
      "includeUnsupportedFiles": true,
      "includeProfileExclusions": true,
      "includeManualExclusions": true,
      "includeSourceExclusions": false,
      "includeProcessingFailures": true,
      "includeOther": true
    }
  }
}
```

## Manual creation checklist

Before adding a manually created profile file, verify:

* file name ends with `.filemerger.profile.json`;
* file is located in `%LocalAppData%\FileMerger\Profiles`;
* `schemaVersion` is `1`;
* `metadata.id` is stable, unique, and not empty;
* `metadata.name` is not empty;
* `metadata.isBuiltIn` is `false`;
* `metadata.isReadOnly` is `false`;
* `profile.fileTypes` exists and is an array;
* each file type has a valid extension starting with `.`;
* enum values are numeric;
* all filter rule patterns, including disabled rules, remain non-empty after trimming;
* `DirectorySegment` patterns contain neither `/` nor `\`;
* `Extension` patterns start with `.`;
* regex filter rules are valid regular expressions and account for the 250 ms per-match runtime timeout;
* `isUserEditable` is not being relied on as a general lock on editing rule fields;
* encoding names are valid .NET encoding names;
* unsupported text fallback is enabled only when intentionally needed;
* `profile.skippedFilesMetadataMode` uses a valid numeric `SkippedFilesMetadataMode` value;
* if `profile.skippedFileCategories` is explicit, it contains all seven boolean category flags documented above;
* use `profile.includeSourceExcludedFiles` only as the legacy/null-category fallback for source exclusions; an explicit `profile.skippedFileCategories.includeSourceExclusions` value takes precedence.

After editing:

1. Restart FileMerger or reopen the Profile Manager.
2. Check that the profile appears in the user profiles list.
3. Open the profile and verify file types and all rules, including disabled or filtered-out rows.
4. Correct any rule errors and save the corrected draft before applying it to a test workspace.
5. Build preview and inspect discovered/skipped files. Rebuild after any later configuration or inclusion changes before saving the updated merged output.

## Editing existing profile files

When editing an existing user profile directly:

1. Close FileMerger or make sure the profile is not being saved at the same time.
2. Back up the original `.filemerger.profile.json` file.
3. Edit the JSON file.
4. Keep `schemaVersion` unchanged unless the application schema changes.
5. Update `updatedAtUtc`.
6. Save the file as UTF-8.
7. Reopen Profile Manager and verify the result.

If the profile disappears after editing, check that the file is readable, can be deserialized, has the supported schema version, and contains the required objects.

If the profile appears but Apply or Save is disabled, inspect its rules in the editor. An older or imported profile can be structurally readable while containing invalid rules. Fix the affected rows and save again; the application does not repair the profile by silently deleting those rules. If a save attempt through the unsaved-changes dialog is rejected, the same draft remains open for correction.

## Recommended profile ids

Use stable custom ids for manually maintained profiles.

Good examples:

```text
custom.docs-config
custom.unity-lite
custom.wpf-review
custom.full-source-with-metadata
```

Avoid:

```text
builtin.default
builtin.unity-project
```

User profiles should not reuse built-in ids.

## Notes about output-affecting settings

Profiles affect generated output.

The following settings belong to profiles rather than application preferences:

* supported file types
* filter rules
* unsupported text fallback
* input encoding behavior
* line ending mode
* sort mode
* output metadata options

Application preferences are for UI/workflow/display behavior only and should not be used to control generated output.