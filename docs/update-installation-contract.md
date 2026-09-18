# FileMerger Update and Installation Contract

This document defines the technical contract for the implemented FileMerger application update workflow and the accepted production distribution direction for `0.2.0-alpha`.

It is the authoritative specification for `UPD-D1`–`UPD-D5`. It defines the supported release metadata, package format, version-selection rules, package verification, guarded shutdown, out-of-process installation, rollback, restart verification, preservation of user data, and process-boundary ownership.

The core workflow is implemented: explicit update checking, package download and validation, verified-package installation, application-wide guarded shutdown, the out-of-process updater helper, installation receipts, restart verification, and rollback. Trusted delivery of versioned update packages through GitHub Releases for `EscarvalOokt/FileMerger` is also implemented. The manifest contract remains provider-independent, and the concrete production manifest URI is release/build configuration rather than part of this architectural contract.

---

## 1. Scope and invariants

The supported workflow is:

`check -> select release/package -> download -> validate -> prepare shutdown -> install out of process -> restart -> verify -> complete or roll back`

The following invariants apply to the entire workflow:

* Update state belongs to the application as a whole and is independent of any individual workspace, merge session, preview, or profile draft.
* Check, download, validation, and installation are separate explicit operations. A successful check must not start a download, and a successful download must not start an installation automatically.
* Only a package in the `Verified` state may be installed.
* User cancellation must never be interpreted as permission to close the application, discard unsaved work, or continue installation.
* Active merge/preview work must not be force-terminated by the update subsystem.
* User data is outside the installation transaction and must not be replaced or rolled back with application binaries.
* The update subsystem must not change merge behavior, workspace format, profile schema version `1`, or profile linkage/origin semantics.
* Update behavior must reuse existing application command, status, prompt, close-guard, workspace-lifecycle, and profile-lifecycle mechanisms where they apply instead of introducing parallel UI infrastructure.

---

## 2. Terminology and state model

### Terms

| Term | Meaning |
| --- | --- |
| **Current version** | The semantic version of the running FileMerger build. |
| **Release manifest** | Versioned JSON metadata used by FileMerger to discover an available release and its packages. |
| **Package** | A complete framework-dependent FileMerger publish output distributed as a ZIP archive. |
| **Downloaded** | The complete package archive has been downloaded, but installation is not yet allowed. |
| **Verified** | The downloaded package has passed all validation required by this contract and may be offered for installation. |
| **Installation directory** | The directory containing the currently running FileMerger application files. It is treated as application-owned storage, not user-data storage. |
| **Staging directory** | Update working storage outside the installation directory. It contains downloads, extracted files, updater execution files, rollback data, and installation receipts. |
| **Updater helper** | A separate process that performs replacement and rollback after the main FileMerger process exits. |
| **Installation request** | Persistent updater input stored outside the installation directory for one installation attempt. It carries the paths, identifiers, expected version, entry executable, and verification context used by the updater helper. |
| **Installation receipt** | Persistent status and recovery state stored outside the installation directory for an installation attempt. It is paired with the persisted installation request. |

### Logical states

The implementation must preserve the distinction between these logical states:

`Idle -> Checking -> UpdateAvailable -> Downloading -> Downloaded -> Validating -> Verified -> PreparingInstall -> Installing -> RestartVerification -> Installed`

Failures and cancellation return the application to a safe state appropriate to the last valid artifact:

* check failure -> `Idle` with an error result;
* download cancellation/failure -> no installable artifact;
* validation failure -> downloaded artifact is invalidated and must not be installable;
* shutdown preparation cancellation/failure -> application remains running and the verified package may remain available for a later attempt;
* installation failure -> rollback is attempted before the update is reported as complete;
* restart verification failure -> the update is not considered installed and recovery/rollback rules apply.

A state name is a behavioral contract, not a requirement for a specific enum or class layout.

---

## 3. UPD-D1 — Release source and metadata

### Release source

FileMerger uses a provider-neutral release manifest:

* the application is configured with an absolute HTTPS URI for a JSON release manifest;
* the manifest model and Application-layer update use cases do not depend on GitHub Releases, GitLab Releases, or another hosting-provider API;
* the production manifest URI and trusted package download policy are build/release configuration, not workspace or profile settings;
* changing hosting provider must not require changing the manifest schema or provider-independent Application contracts;
* HTTP is not supported for production update metadata or package downloads.

For `0.2.0-alpha`, the accepted production binary distribution is versioned ZIP assets published through GitHub Releases for the `EscarvalOokt/FileMerger` repository. GitHub REST API discovery is not part of the runtime update contract; FileMerger continues to discover releases through its own manifest. The exact production manifest URI remains release/build configuration and is not defined by this contract.

The implemented trust policy keeps the manifest on the configured HTTPS origin and permits manifest redirects only within that same origin. Package delivery uses a separate GitHub-specific boundary: the initial package URL must be a versioned GitHub Release asset for `EscarvalOokt/FileMerger`, and the controlled HTTPS redirect chain is limited to the trusted GitHub release-asset delivery host required to serve that asset. Arbitrary GitHub URLs or arbitrary redirect targets are not trusted. Package size, SHA-256, compatibility, archive-safety, and required-payload checks remain mandatory regardless of the delivery redirect.

### Manifest version

The initial manifest contract uses:

* JSON;
* camelCase property names;
* `schemaVersion: 1`.

An unsupported `schemaVersion` is an update-check error. It must not be treated as "no update available".

### Manifest fields

The manifest must contain the following information.

#### Top-level fields

| Field | Required | Contract |
| --- | --- | --- |
| `schemaVersion` | yes | Integer. Must be `1` for this contract. |
| `product` | yes | Must identify `FileMerger`. A different product is rejected. |
| `release` | yes | The release descriptor described below. |

#### Release descriptor

| Field | Required | Contract |
| --- | --- | --- |
| `version` | yes | SemVer 2.0 release version. |
| `publishedAtUtc` | yes | ISO-8601 UTC timestamp. Used for display/diagnostics, not version ordering. |
| `releaseNotesUrl` | no | Absolute HTTPS URI for release notes. |
| `packages` | yes | Non-empty collection of package descriptors. |

#### Package descriptor

| Field | Required | Contract |
| --- | --- | --- |
| `id` | yes | Non-empty package identifier unique within the release. |
| `version` | yes | Must equal `release.version`. |
| `os` | yes | Supported value for this release line: `windows`. |
| `architecture` | yes | Supported value for the current Any CPU framework-dependent package: `any`. |
| `framework` | yes | Supported value for the current application baseline: `net10.0-windows`. |
| `deployment` | yes | Supported value: `frameworkDependent`. |
| `format` | yes | Supported value: `zip`. |
| `url` | yes | Absolute HTTPS package URI accepted by the configured HTTPS trust policy. |
| `sizeBytes` | yes | Expected archive size in bytes; must be greater than zero. |
| `sha256` | yes | SHA-256 digest of the exact ZIP bytes, encoded as 64 hexadecimal characters. |
| `entryExecutable` | yes | Relative path to the executable that must be launched after installation. |
| `minimumSourceVersion` | no | Minimum FileMerger version from which this package may be installed automatically. |
| `minimumUpdaterVersion` | no | Minimum updater-helper protocol/version required by this package. |

Additional fields may be added in a backward-compatible manner, but a client implementing schema version `1` must not infer behavior from unknown fields.

### Release selection result

Update checking must distinguish at least these outcomes:

* a newer compatible release/package is available;
* no newer eligible release is available;
* the manifest was reached but is invalid/incompatible;
* the release source could not be reached.

An error must never be reported as "up to date".

---

## 4. UPD-D2 — Distribution and package contract

### Supported distribution

For `0.2.0-alpha`, the supported automatic-update package is:

* Windows only;
* framework-dependent;
* target framework `net10.0-windows`;
* architecture `any`, matching the existing Any CPU publish baseline;
* a complete FileSystem publish output;
* distributed as a ZIP archive;
* installed in place into the existing installation directory.

The current `FolderProfile.pubxml` with `SelfContained=false` is the baseline for application package contents. The WPF project integrates the updater runtime into build/publish output so the supported full package contains both FileMerger and the helper required for subsequent automatic updates.

### Complete package

The ZIP is a full application package, not a binary patch or delta update. It must contain the complete publish tree required to run FileMerger, including the configured entry executable and the runtime metadata/dependencies produced by the framework-dependent publish. It must also contain the updater runtime required by the current protocol: `FileMerger.Updater.exe`, `FileMerger.Updater.dll`, `FileMerger.Updater.deps.json`, `FileMerger.Updater.runtimeconfig.json`, and `FileMerger.UpdateProtocol.dll`.

The installation directory is treated as application-owned. Application binaries are replaced as one release unit; user profiles, preferences, recent-workspace data, workspace files, downloads, staging files, and rollback data must not be stored as application-owned files inside that replacement set.

### Runtime prerequisite

The updater does not install or update the .NET Desktop Runtime. A machine must already satisfy the runtime requirement of the target package. Runtime acquisition/elevation is outside this update contract.

### Installation-directory preflight

Automatic installation is supported only when the current installation directory can be safely updated by the current user.

Before any guarded shutdown begins, installation preparation must verify that:

* the installation directory can be identified reliably;
* the updater can create required staging/rollback data outside it;
* the current user has the filesystem permissions required to replace the application installation;
* the target package is still present and verified.

If these conditions are not met, installation must stop before closing FileMerger. The updater must not request elevation or attempt to bypass filesystem ACLs.

### Bootstrap from versions without an updater

Automatic self-update is guaranteed only from a FileMerger version that already contains the update subsystem and compatible updater helper.

A version that predates that mechanism requires a one-time manual installation/update to a version that contains it. The release manifest may use `minimumSourceVersion` and/or `minimumUpdaterVersion` to reject an automatic path that is not supported.

The current updater protocol version exposed by `FileMerger.UpdateProtocol` is `1.0.0`. A package that declares a higher `minimumUpdaterVersion` is not eligible for installation by the current updater protocol.

---

## 5. UPD-D3 — Version and prerelease rules

### Current application version

The canonical update version is the running FileMerger application's assembly informational version. The implementation must expose one normalized SemVer value to the application update workflow.

The update subsystem must not use the profile schema version, workspace schema, file timestamps, assembly file version, or release publication date as a substitute for the application SemVer.

If the running build does not expose a valid semantic version, update checking must fail with a configuration/metadata error rather than inventing a fallback version.

### Comparison

Version precedence follows SemVer 2.0:

* compare `major.minor.patch-prerelease` by SemVer precedence;
* build metadata does not affect precedence;
* an equal version is not an update;
* an older version is never selected by the normal update workflow;
* release publication timestamps do not override semantic-version precedence.

The automatic update workflow does not support downgrade.

### Prerelease eligibility

The `0.2.0-alpha` release line requires explicit prerelease semantics:

* a stable current build must not be offered a prerelease build;
* a prerelease current build may be offered a newer prerelease build according to SemVer precedence;
* a prerelease current build may be offered the newer stable release of the same or a later version;
* an equal or older prerelease must not be offered;
* multiple user-configurable update channels are not introduced by this contract.

The current build's own SemVer stability/prerelease status determines prerelease eligibility; no profile or workspace setting controls it.

---

## 6. UPD-D4 — Package download, validation, and trust

### Download staging

Downloads and all update working data must be stored outside the installation directory under application-local user data, with `%LocalAppData%\FileMerger\Updates` as the update staging root.

The concrete subdirectory naming is an implementation detail, but it must keep distinct release/package attempts isolated and must not collide with profile/preferences storage.

A package is downloaded to a temporary/partial artifact first. The final downloaded artifact becomes eligible for validation only after the transfer completes successfully.

An incomplete or canceled transfer must never be treated as `Downloaded` or `Verified` merely because a file exists on disk.

### Required validation order

Before a package can enter `Verified`, the implementation must validate all of the following:

1. **Manifest contract** — supported schema, correct product, valid release/package structure.
2. **Version consistency** — package version equals release version and is the release selected by the version rules.
3. **Compatibility** — `windows`, `any`, `net10.0-windows`, `frameworkDependent`, `zip`, plus any declared minimum source/updater version.
4. **Trusted source policy** — the manifest URI and the initial package URL/redirect chain satisfy the configured HTTPS trust policy; for the accepted GitHub Releases distribution, the initial package URL must identify a trusted versioned release asset for `EscarvalOokt/FileMerger`.
5. **Completed transfer** — the package download completed normally.
6. **Expected size** — archive byte length equals `sizeBytes`.
7. **SHA-256** — digest of the exact archive bytes equals `sha256` from the manifest.
8. **ZIP path safety** — every archive entry is a safe relative path contained by the extraction root; absolute paths, rooted paths, `..` traversal, and any normalized escape from the extraction root are rejected.
9. **Required application payload** — the archive contains the declared `entryExecutable` and the framework-dependent runtime/application files required by the supported package contract.
10. **Safe extraction/staging** — extraction completes into a clean package-specific staging directory outside the installation directory.

Validation must fail closed. A package that fails any check must be marked invalid and installation must remain unavailable.

### Trust model for `0.2.0-alpha`

The trusted package identity for this release is based on all of the following together:

* release metadata obtained from the configured trusted HTTPS manifest source;
* package download through an allowed HTTPS URL and redirect chain;
* exact byte length from the manifest;
* exact SHA-256 digest from the manifest;
* compatibility and archive-safety checks.

Operating-system code signing or a separate package-signature PKI is not introduced by this contract. It may be added by a later explicit decision without weakening the checks above.

### Reuse of cached downloads

A previously downloaded archive may be reused only after its manifest identity, size, digest, compatibility, and extracted staging data have been validated for the current installation attempt. Existence of a cached file is never sufficient proof of validity.

---

## 7. UPD-D5 — Installation, shutdown, restart, and recovery

### Installation boundary

The running FileMerger process must not replace its own installation files directly.

Installation uses an updater helper running from staging outside the installation directory. The main process performs preparation and guarded shutdown; the helper owns filesystem replacement, rollback, restart, and restart-verification coordination.

### Main-process preparation

Given a `Verified` package, FileMerger performs this sequence:

1. Reconfirm package/installation preflight requirements.
2. Refuse to start guarded shutdown while a merge/preview operation is active. The update subsystem must not cancel that operation automatically; the user may finish or cancel it through the existing workflow and retry installation.
3. Coordinate application-wide close guards for unsaved state.
4. If every guard accepts closure, prepare the verified package, rollback location, receipt, and updater invocation data.
5. Start the updater helper successfully while FileMerger is still running.
6. Only after the helper has started successfully, complete the already-approved graceful application shutdown.

If helper startup fails, FileMerger remains running and installation does not begin.

### Application-wide guarded shutdown

Existing window/tab guards are inputs to shutdown coordination, not a complete application-wide solution by themselves.

Installation preparation must preserve the existing semantics of affected workflows:

* dirty workspaces use the existing `Save / Discard / Cancel` behavior;
* unsaved Profile Manager drafts use the existing close-guard behavior;
* save failure or rejected validation prevents that guarded item from closing;
* `Cancel` from any guarded item aborts installation preparation;
* no remaining windows/tabs may be force-closed to make installation proceed.

A canceled shutdown leaves FileMerger running. The verified package may remain staged for a later explicit install attempt.

### Updater invocation data

The helper must receive or be able to resolve, at minimum:

* PID of the FileMerger process that must exit;
* installation directory;
* verified extracted package/staging location;
* package/release identifier;
* expected application version;
* entry executable to restart;
* rollback location;
* installation receipt/verification token location.

The transport format is an implementation detail, but update input must be validated by the helper before destructive filesystem work begins.

### Replacement transaction

After the main process exits, the helper must:

1. verify that the target process is no longer running;
2. create/complete a rollback copy of the existing application installation;
3. replace the application-owned installation tree with the verified staged package as a complete release unit;
4. detect replacement/copy failures;
5. if replacement fails, restore the previous installation from rollback data before attempting to restart FileMerger;
6. record the resulting state in the installation receipt outside the installation directory.

The helper must never include `%LocalAppData%\FileMerger` user data or external workspace files in the application binary rollback transaction.

### Restart verification

A filesystem replacement alone is not a successful update.

After replacement, the helper starts the package's declared entry executable with a one-time restart-verification context. During early startup, the restarted FileMerger process must validate that context and compare its actual canonical application version with the expected version from the pending installation receipt/context. Passing this early validation is not yet a successful update.

A positive verification acknowledgement may be written only after the restarted process has successfully completed the startup initialization required to initialize the application state and open the main window. The acknowledgement must still confirm the expected application version and is stored outside the installation directory so it survives replacement/rollback.

The updater must retain rollback data until restart verification is resolved. If the expected version cannot be launched, startup fails before the positive acknowledgement point, or a valid acknowledgement is not obtained within the updater's bounded verification window, the installation is failed and rollback must be attempted.

The exact IPC/argument/receipt serialization and verification timeout are implementation details, but the success criterion is not: the expected version must reach the accepted startup point, open the main window, and acknowledge the expected version before the update is considered complete.

### Rollback

Rollback restores the previous application installation from the backup created for the installation attempt.

Required behavior:

* replacement failure -> rollback before restart;
* target process cannot be launched -> rollback;
* restarted process fails before successful startup acknowledgement or does not confirm the expected version -> rollback;
* successful rollback -> record `RolledBack`/failed-update result and launch the restored FileMerger version when possible;
* rollback failure -> preserve all available backup/staging/receipt diagnostics, do not report update success, and surface recovery information on the next available application start or through the updater failure result.

Rollback protects application files. It must not roll back profiles, preferences, recent-workspace data, workspace files, or other user documents.

---

## 8. Installation request, receipt, and recovery state

Update state that must survive process replacement is stored outside the installation directory under the update staging root. An installation attempt persists both the installation request and a mutable installation receipt.

The persisted installation request carries the data required to execute and recover the attempt, including:

* main-process id;
* installation and verified payload directories;
* release/package id and expected version;
* entry executable;
* rollback, receipt, and acknowledgement locations;
* attempt identity and verification token.

The persisted receipt carries attempt status and recovery/diagnostic state, including:

* release/package id and expected version;
* installation directory;
* attempt identity/verification token;
* current attempt status (`Pending`, `Installing`, `PendingVerification`, `Installed`, `Failed`, or `RolledBack`, or equivalent semantics);
* rollback and acknowledgement locations while they remain relevant;
* failure or status information required for diagnostics/recovery.

The exact file names and serialization format are implementation details.

The existence of a stale or pending receipt must never make normal startup assume update success. When FileMerger is launched with the one-time restart-verification receipt and token arguments, it validates that referenced verification context and compares the running application version with the expected version before it may acknowledge the installation as successful.

---

## 9. User data preservation

The update transaction applies to application installation files only.

The following data is explicitly outside application replacement and rollback:

* user profiles under `%LocalAppData%\FileMerger\Profiles`;
* application preferences and diagnostics data under `%LocalAppData%\FileMerger`;
* recent-workspace state under application-local user data;
* workspace files saved by the user at arbitrary external paths;
* merge output files and source files;
* update staging/receipt/rollback data under `%LocalAppData%\FileMerger\Updates`.

The updater must not migrate, rewrite, delete, or restore these data merely as a side effect of replacing the application package.

Profile schema version `1` remains independent from the FileMerger application release version.

---

## 10. Layer ownership

The update feature must follow the existing project boundaries.

### Domain

No update-specific Domain model is required by this contract. Merge-domain types, profile semantics, and session behavior remain unchanged.

### Application

The Application layer owns provider-independent update workflow contracts and orchestration, including concepts needed to:

* check/select a release;
* represent compatible package metadata;
* compare/select application versions;
* download/validate a package through abstractions;
* request installation preparation through abstractions.

Application contracts must not depend on WPF types or a specific release-hosting provider API.

### UpdateProtocol

`FileMerger.UpdateProtocol` owns the provider-independent process boundary shared by the WPF application and the updater helper. It contains the updater protocol/schema version, validated installation request/receipt/verification data, shared updater runtime file names, and the file-store/serialization contract required across process replacement.

The current updater protocol version is `1.0.0`. The protocol project must not depend on WPF presentation types or a release-hosting provider API.

### Infrastructure

Infrastructure owns external/system implementations such as:

* HTTPS manifest retrieval;
* package download and staging;
* size/hash verification;
* ZIP inspection/extraction;
* filesystem/update path handling;
* updater-helper process launch and other OS/filesystem integration required by the accepted contract.

### WPF

WPF owns presentation and user interaction:

* explicit check/download/install commands;
* statuses, progress, cancellation, and error presentation;
* update dialogs/prompts;
* coordination of application-wide guarded shutdown using existing workspace/profile lifecycle semantics.

Where applicable, implementation should reuse the existing `RelayCommand` / `AsyncRelayCommand`, status/prompt services, workspace lifecycle, profile close guards, and shared styles/tokens instead of creating parallel mechanisms.

### Updater helper

The updater helper is a separate out-of-process component responsible only for installation concerns that cannot safely occur inside the running WPF process:

* waiting for the main process to exit;
* backing up/replacing application files;
* rollback;
* restart;
* restart-verification coordination and installation receipt updates.

It must not become a second implementation of FileMerger application/domain behavior.

---

## 11. Failure and cancellation contract

| Scenario | Required result |
| --- | --- |
| Release source unavailable | Check fails explicitly; do not report "up to date". |
| Unsupported/invalid manifest | Check fails explicitly; no package selected. |
| No compatible newer package | Report no eligible update; no download/install starts. |
| Download canceled | Partial artifact is not installable; application remains usable. |
| Download interrupted/incomplete | Treat as failed/incomplete; never `Verified`. |
| Size or SHA-256 mismatch | Invalidate package; installation unavailable. |
| Unsafe ZIP path/payload | Invalidate package; do not extract/install unsafe entries. |
| Package incompatible with current app/platform | Reject before installation. |
| Installation directory not writable | Fail preflight before guarded shutdown. |
| Merge/preview operation active | Do not force-cancel/close; installation does not enter guarded shutdown. |
| Workspace save canceled or fails | Abort installation preparation; application remains running. |
| Profile-draft close canceled/rejected | Abort installation preparation; application remains running. |
| Updater helper cannot start | Application remains running; installation does not begin. |
| Application exits but replacement fails | Helper attempts rollback and restarts restored version when possible. |
| New executable cannot start | Helper attempts rollback. |
| Restarted app fails before successful startup acknowledgement or does not confirm expected version | Update is not successful; helper attempts rollback. |
| Rollback succeeds | Previous application version is restored; record failed/rolled-back result. |
| Rollback fails | Never report success; preserve recovery/diagnostic artifacts. |

Cancellation is meaningful only before the main application has approved shutdown and the out-of-process filesystem installation transaction has begun. After that boundary, safety is provided by completion/rollback rather than an in-process Cancel command.

---

## 12. Explicit non-goals

This contract does not introduce:

* background or automatic update checks without an explicit user action;
* automatic package download after a check;
* automatic installation after download/validation;
* multiple user-configurable update channels;
* downgrade through the normal update workflow;
* delta/binary-patch packages;
* elevation/UAC-based installation or ACL bypass;
* installation/update of the .NET Desktop Runtime;
* code-signing/PKI requirements beyond the accepted HTTPS source policy + exact-size + SHA-256 trust model;
* changes to merge behavior, workspace format, profile schema, or `Escarval.Wpf.Windowing`;
* user-document migration as part of application binary replacement.

Any of these requires a separate explicit decision and must not be inferred from `UPD-01`.