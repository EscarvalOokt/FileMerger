# FileMerger Profiles

This document describes how to add, duplicate, import, export, manually create, and manually edit FileMerger profile files.

Profiles define how FileMerger discovers, filters, transforms, and writes files into the final merged output.

## Recommended way to create profiles

The safest way to create a profile is through the application UI.

Use the Profile Manager to:

1. open the profile library;
2. select a built-in or user profile;
3. duplicate it or use **Save As**;
4. adjust file types, filter rules, encoding options, metadata options, and C# options;
5. save the profile;
6. apply it to the current workspace.

Built-in profiles are read-only. To customize a built-in profile, duplicate it or save it as a new user profile.

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
5. Verify it in the Profile Manager.
6. Apply it to a test workspace and build preview.

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
* a non-empty `metadata` object;
* a non-empty `profile` object.

The app currently serializes enums as numbers, not strings. For example:

```json
"lineEndingMode": 0
```

not:

```json
"lineEndingMode": "Preserve"
```

If a profile file is malformed, unreadable, uses an unsupported schema version, or does not contain required objects, it is ignored.

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
| `removeUsingDirectives`                   |     boolean | C# option: removes `using` directives from C# files. |
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
| `skippedFilesMetadataMode`                |      number | See enum table below.                                |
| `includeSourceExcludedFiles`              |     boolean | Includes source-excluded files in skipped-file metadata when enabled. Defaults to `false`. |

`includeSourceExcludedFiles` is an opt-in output metadata setting. It only contributes source-specific exclusions to skipped-file metadata when `skippedFilesMetadataMode` is `1` (`Simple`) or `2` (`Detailed`). When the mode is `0` (`None`), source-excluded files are not reported through skipped-file metadata even if the flag is `true`.

This setting does not make source-excluded files merge candidates and does not add them to the normal discovered/session file inventory. Missing `includeSourceExcludedFiles` values are treated as `false`.

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
| `isEnabled`                          | boolean | Whether this file type is included by the profile.                |
| `supportsLanguageSpecificProcessing` | boolean | `true` for file types with language-specific options, such as C#. |

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
| `isUserEditable` |     boolean | Whether the rule can be edited in UI. Use `true` for manual profiles. |

Common examples:

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
    "removeUsingDirectives": false,
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
    "skippedFilesMetadataMode": 0,
    "includeSourceExcludedFiles": false
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
* filter rule patterns are not empty;
* regex filter rules are valid regular expressions;
* encoding names are valid .NET encoding names;
* unsupported text fallback is enabled only when intentionally needed;
* `profile.includeSourceExcludedFiles` is enabled only when source-specific exclusions should be included in skipped-file metadata, with `skippedFilesMetadataMode` set to `Simple` or `Detailed`.

After editing:

1. Restart FileMerger or reopen the Profile Manager.
2. Check that the profile appears in the user profiles list.
3. Open the profile and verify file types and rules.
4. Apply it to a test workspace.
5. Build preview and inspect discovered/skipped files.

## Editing existing profile files

When editing an existing user profile directly:

1. Close FileMerger or make sure the profile is not being saved at the same time.
2. Back up the original `.filemerger.profile.json` file.
3. Edit the JSON file.
4. Keep `schemaVersion` unchanged unless the application schema changes.
5. Update `updatedAtUtc`.
6. Save the file as UTF-8.
7. Reopen Profile Manager and verify the result.

If the profile disappears after editing, the file is probably invalid, unreadable, has an unsupported schema version, or no longer matches the expected document structure.

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
* C# transformation options

Application preferences are for UI/workflow/display behavior only and should not be used to control generated output.