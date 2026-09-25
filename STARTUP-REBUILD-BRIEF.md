# Startup Rebuild — Design Brief

Source of truth for `STARTUP-REBUILD-CHECKLIST.md`. Section numbers (S1–S16) are the refs used by every
task line in that checklist. Nothing here is implemented yet; the removal checklist is the first work.

---

## S1. Purpose and scope

The entire startup surface is deleted and rebuilt from scratch: the `MTM_Waitlist.Startup` project, the
`Module_Startup` views, the launch orchestration in `App.xaml.cs`, the activation surface, the startup
contracts, models and options in `MTM_Waitlist.Core`, startup logging, the startup tests, the
`capabilities/startup` living spec and its registry entry, and the docs that describe the old splash.

The old logic is replaced first by a **placeholder window** so the removal is provable: if the app can no
longer reach the shell, the old logic is gone. Only then is the new module built.

## S2. Decisions locked (2026-09-25)

| # | Decision |
|---|---|
| 1 | Rebuild `MTM_Waitlist.Startup` **in place**, same name; views stay in `Module_Startup/Views`. |
| 2 | Old material is **deleted outright** — git history is the archive. No `_legacy/` parking. |
| 3 | Sign-in is rebuilt too; it counts as startup. |
| 4 | The placeholder splash is **splash only** — the app stops there and nothing else loads. |
| 5 | Steps become data with a derived count, and the splash shows **everything the process is doing**. |
| 6 | Errors surface as a **toast anchored to the bottom of the splash window**. |
| 7 | The session moves into the store. **Only connection strings may remain local, plus one reviewed exception: `MockServiceClient` (S11.5.4).** |
| 8 | Machine configuration stays a **hard gate**, satisfied by a new machine-setup step. **Amended 2026-09-25: there is no logging destination to configure, because records live only in the store (S10).** |
| 9 | Machine configuration is reachable **before any operator sign-in**, unlocked by an IT Department or Developer sign-in. **It cannot be bypassed: the only outcomes are a configured machine that continues, or the process ending (S10.1).** |
| 10 | All logging goes to the **database**; logging becomes its own module with a **developer log panel**. |
| 11 | One blocked-state surface; **Restore Defaults states exactly what it will reset and resets only what is broken**. |
| 12 | Where a reset can be done silently without hurting the operator, **do it silently**. |
| 13 | **Roles come from the store only** — the `appsettings.json` developer allow-list is gone. |
| 14 | `StartupState` is replaced by **two narrow contracts**: person identity, and machine facts. |
| 15 | Startup keeps: the picture-cache refresh, the Infor Visual verdict priming, the forced password change, and the computer-registration gate. |
| 16 | The log-folder prompt is gone entirely. |
| 17 | Remember-me survives, stored in the store against the person, encrypted. |
| 18 | The encryption key is read from a pre-existing shared key file, addressed by its UNC path (S8.3). |
| 19 | The session lives in a new **`user_active_sessions`** table. |
| 20 | Every deployed table, procedure, function and view is audited for dead weight; dead SQL files are deleted. |
| 21 | How long a session lasts is a setting, changeable from the settings panel by IT Department or Developer only. |
| 22 | Every setting that is role-restricted today keeps the same restriction after the rebuild. |
| 23 | Machine configuration carries no destination for records, because records live only in the store. |
| 24 | The development seed grants `Developer` to `JKoll` and `JohnK`, so each of the two seeded machines has a developer who can sign in. |

## S3. What is being removed

### S3.1 Code, tests, docs
- `MTM_Waitlist.Startup/**` (all services, view models, DI extension, csproj) — including the sln entry, the
  app's `ProjectReference` and `Compile Remove`, and the test project's `ProjectReference`.
- `Module_Startup/Views/**` (SplashWindow, SplashView, SplashPage, LoginWindow, LoginPage).
- `App.xaml.cs`: the splash/login window statics, `ShowSplashWindow`, `ShowMainWindowAndCloseSplash`,
  `ShowLoginWindowAndCloseSplash`, `ShowMainWindowAndCloseLoginWindow`, `CloseSplashWindow`, and the three
  window-closed handlers.
- `MTM_Waitlist.Core`: `ActivationService`, `Activation/**`, every `IStartup*` contract, `StartupState`,
  `StartupResult`, `StartupSessionSnapshot`, `StartupCredentialCheckResult`,
  `StartupPasswordResetRequirement`, `IComputerGateService`, `IComputerRegistryService`, `ISignOutService`,
  `SignOutResult`, `IAppProcessRestarter`, `IStartupWindowService`, `IStartupShellStateService`,
  `IStartupLogService`, `IStartupLogForwarder`, and the four `Startup*Options` models.
- `Services/AppLifecycleService.cs` and the `IAppLifecycleService` seam it implements.
- `StartupDebugLog` — see S9 for its 491 call sites.
- All startup tests, including `SplashViewModelTests`, `LoginViewModelTests`, `StartupCoordinatorTests`,
  `StartupLogServiceTests`, `StartupRecoveryServiceTests`, `ComputerGateServiceTests`,
  `ComputerRegistryServiceTests`, `TemporaryCredentialLimitTests`, `StartupArchiveCleanupTests`,
  `SignOutServiceTests`, `StartupModelsTests`, `StartupOptionsModelsTests`, `StartupDebugLogTests`, and
  `NoOpAppLifecycleService`.
- `capabilities/startup/**`, the `startup` entry in `living-specs.yml`, and the `startup` row in
  `capabilities/DRIFT.md`.
- `STARTUP-FLOW.md`, `STARTUP-FLOW.html`, `STARTUP-FLOW.pdf`; the splash/sign-in steps in
  `.github/instructions/winui3-ui-automation.instructions.md`; the startup mentions in
  `.github/instructions/test-conventions.instructions.md`; the module list in `README.md`.
- The eight `Shell_SignOut.*` / `Startup_SignIn.*` resource keys and the fourteen `Startup_*` tooltip keys
  (in **both** `TooltipResources.resw` and `TooltipResources.developer.resw`).

### S3.2 Shared artifacts that must survive untouched
**Tables**: `core_users_profiles`, `core_computers_registry` (three foreign keys point at it),
`auth_roles_catalog`, `auth_roles_assignments`, `config_settings_values`.
**Procedures**: `sp_auth_user_row_get` (also serves the cache-service API), `sp_auth_temporary_credential_attempt_record`
(also user management), `sp_core_computers_registry_upsert|update|delete|get_all|registered_get|lookup_by_name_get`
(Settings and Work Center Catalogue), `sp_auth_roles_list`, `sp_config_settings_values_get|upsert`.
**Seeds and aggregates**: `seed_dev_masked_baseline`, `seed_username_upper_normalization`,
`seed_role_admin_to_it_department`, `AllSeeds.sql`, `update_table_descriptions.sql`, `AllTables.sql`,
`AllSPs.sql`, `AllFunct.sql`.
**Core services**: `MySqlHostFallback`, `ILocalSettingsService`, `IFileService`, `MySqlHelperServer`,
`fn_server_utc_now`.

### S3.3 Startup-only database artifacts (removable in isolation)
`sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`,
`sp_auth_computer_registered_get`, `sp_auth_session_expiry_get`, `sp_auth_password_reset_required_get`,
`sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`,
`sp_core_computers_registry_update_by_mac`, and `Database/Validation/startup_schema/`.
`fn_server_utc_now` is **retained** — the session rule in S8 depends on it.

## S4. Target architecture

- `MTM_Waitlist.Startup` rebuilt as the launch pipeline library; XAML stays in `Module_Startup/Views`.
- The app host keeps a thin seam so `App.xaml.cs` does not grow a second orchestration layer: the new module
  exposes one entry point that the host starts, and one event the host observes when the shell is ready.
- `IAppLifecycleService` is replaced by that seam; the host no longer owns window handoff.
- The shell is reached only through the new pipeline. There is no bypass and no "continue anyway" path, and an
  unconfigured machine cannot reach it by any route (S10.1).

## S5. Splash and the activity feed

- The splash is one surface. It renders a **live, append-only activity feed**: one line per thing the process
  does, with a timestamp, so a lock-up is attributable to a named line rather than to a step number.
- Progress is still announced **before** each check runs.
- An **error toast** is anchored to the bottom of the splash window. It carries the plain-language diagnosis
  and never covers the feed.
- The feed is the debugging surface for a live fault: it must name the operation, its target (store, share,
  server) and its outcome.
- Window sizing stays configurable and the sizing failure path stays non-fatal.

## S6. The step model

- Steps are **data**, not a hard-coded five: each step has a name, a description and a category.
- The displayed count is **derived** from the step list, so adding or removing a step never leaves a stale
  "of 5".
- The feed shows started / completed / failed transitions per step, plus sub-operations inside a step.
- Retry re-runs the failed step and the steps after it — never the whole pipeline, and never step 1 for a
  database-only retry.

## S7. Identity contracts

Two interfaces replace `StartupState`:

1. **Person identity** — read-only: user id, sign-in name, display name, employee number, current role code,
   and the role codes the person holds. No writer other than the pipeline.
2. **Machine facts** — read-only: normalised hostname, normalised MAC address, the registered computer row
   (id, display name, description), and whether this machine is registered.

Consumers to migrate (~20): permissions, user management, waitlist attribution, tooltips and the control
inspector, Settings computer screens, picture preferences, and the shell's user badge.

`PermissionGateSiteAuditTests` currently pins `MTM_Waitlist.Core/Models/StartupState.cs` and its `IsDeveloper`
body; it is rewritten against the new contracts.

## S8. Session, remember-me, and secret material

### S8.1 Session
- The session lives in a new `user_active_sessions` table: one active row per person per machine, with the
  token stored as a **salted hash**, an expiry, an issued time, and the machine key.
- Session validity is judged on the **store's clock** (`fn_server_utc_now`), never the workstation's.
- The app writes the row when sign-in completes (only after the machine gate passes) and clears it on sign-out.
- The eight-hour lifetime is the default, not a fixed value: it is changeable from the settings panel by
  IT Department or Developer only (S2 row 21).

### S8.2 Remember-me
- Stored in the store against the person, never in a local file.
- **Encrypted, not hashed** — it must be decryptable. See S8.3 for the key.
- A failed decrypt falls back to the normal sign-in form and records the fault; it never blocks startup.

### S8.3 The key file

- **Path, resolved and verified 2026-09-25:**
  `\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\MTM_Application_Keys\MTM_AUTH_USER_SECRET_KEY.txt`
- **The UNC form is the setting, never the `X:` drive letter.** `X:` is a per-user mapped drive: it does not
  exist for a different account, a different session, or a service. Confirmed by reading the mapping — `X:`
  resolves to `\\mtmanu-fs01\expo drive` — and both forms reach the same file today.
- **The file already exists.** 44 bytes, last written 2026-03-23, so this is a pre-existing shared secret rather
  than one to generate. Its name and location suggest it is shared with other MTM applications' auth: **read
  it, never write or rotate it as part of this work**, and treat a rotation as a separate change with its own
  blast radius.
- 44 bytes is consistent with a 32-byte key in base64, which is what AES-256 expects. The value itself was
  deliberately not read while confirming this.
- The key is never logged, never copied into the repository, and never echoed by the developer log panel.
- If the share cannot be read, remember-me falls back to the ordinary sign-in form and records the fault. It
  never blocks startup and never stores anything locally instead.
- A `Security Review` task covers key handling and the implications of a shared secret, owned by a
  `Security Engineer`.

### S8.4 Local state that may remain
Only the connection strings used to reach the store. Everything else — theme, remember-me, waitlist sort
order, ignored locations, new-request alerts, waitlist message-seen — moves into the store. A double pass
(S11.4) must prove nothing else is left.

## S9. Logging

- Logging becomes **its own module**, separate from startup.
- Every entry goes to the **database**. There are no local log files and no log-folder prompt.
- `ops_startup_logs` already carries `correlation_id`, `previous_hash` and `entry_hash` and has no writer
  today; it (or a renamed successor) becomes the store, with create/rollback procedures, an index set for the
  panel's filters, and a retention routine.
- Every entry carries: timestamp, severity, area/module, machine, user, error type, message, exception detail,
  and the hash chain link.
- A **developer log panel** in Settings lists and filters by criticality, machine, user, module and error type.
- When the store is unreachable the app refuses to start, so nothing is persisted for that case — the splash
  shows the diagnosis and the operator reports it (decision 12 of S2).

### S9.1 Four logging surfaces exist today, and only one is script-shaped

Measured 2026-09-25 across every `.cs` file outside `bin`/`obj`:

| Surface | Size | What it needs |
|---|---|---|
| `StartupDebugLog` (static) | **489 references in 83 files** — 335 `Info`, 147 `Error`, 7 `Configure` | A call-site rewrite, because the type is being replaced. |
| `ILogger<T>` | **226 calls in 26 files**; 31 files import `Microsoft.Extensions.Logging` | A **provider** that writes to the store. No call-site edits at all. |
| `ServiceLog` (helper service host) | **25 calls in 6 files** | A separate decision in a separate host. |
| `Debug.WriteLine` | ad hoc | Debug-only; left alone. |

This matters because it splits the work in two: the static surface is a 489-line mechanical edit, and the
`ILogger` surface needs no edit whatsoever once a store-backed provider is registered. Nobody should attempt to
script the `ILogger` calls.

### S9.2 The whole application currently logs only in DEBUG builds

`StartupDebugLog.Info` and `.Error` are both marked `[Conditional("DEBUG")]`. In a Release build the compiler
removes every one of those 482 calls **and their arguments**, so the application ships with no logging at all.
The same attribute marks `Debug.WriteLine`, so the immediate-output path is gone too.

Two consequences:

- A store-backed seam must be **unconditional**. If the replacement keeps the attribute, the database table
  stays empty in Release and the new developer panel shows nothing on a real shop-floor machine.
- Any log content produced by a Release machine today is lost, not merely unforwarded. Where an operator
  reports a fault that only reproduces in production, there is currently nothing to read.

### S9.3 The migration script

`tools/migrate-logging-callsites.ps1` performs the static-call-site rewrite. Dry run is the default; nothing is
written without `-Apply`.

- It handles the fully-qualified form first (one production file calls
  `MTM_Waitlist.Module_Core.Helpers.StartupDebugLog.Info(...)`), consuming the qualifier rather than leaving it
  dangling in front of the new name.
- It **does not rewrite using directives**. `MTM_Waitlist.Module_Core.Helpers` holds ten other types and is
  imported by 134 files, so the new type is made reachable with one global using instead.
- It touches only the type token, so arguments, line structure and multi-line call formatting are untouched.
- It reports the 7 `Configure` calls separately, because that signature changes and needs a human.
- Dry-run result on 2026-09-25: 83 files, 489 call sites, 1 fully-qualified, 7 `Configure` — matching an
  independent count taken by hand.

### S9.4 Ordering consequence for the removal

`StartupDebugLog` cannot be deleted in Phase 2 while 489 call sites still reference it: the tree would not
build. It is a Core helper rather than a startup type, so its deletion is **deferred to Phase 6**, where the
replacement exists and the script can run first. `StartupState` is deferred the same way, to **Phase 4**,
because roughly twenty consumers compile against it (S7). Every other item in the S3.1 removal list is deleted
in Phase 2.

## S10. New machine configuration

- A **machine-setup screen** appears when the machine is not configured, reachable **before any operator
  sign-in**.
- It is unlocked by an **IT Department or Developer** sign-in, which authorises configuration only and does
  not open the shell.
- It captures the machine's display name and description and the shared picture sources. **There is no record
  destination to capture**, because records live only in the store (S2 row 23). The old prompt for a log folder
  therefore disappears for a second reason: not just because logging moved, but because a folder was never the
  destination in the first place.
- The privilege surface must be updated in the same change: new permission keys in the permission catalogue,
  the Settings privileges page, the role baselines, and the seeds. The user expects the Settings page to
  break without this work.

### S10.1 The step cannot be bypassed

- The step has exactly two outcomes: the machine is configured and startup continues, or the process ends.
  There is no third outcome in which the app runs unconfigured.
- Every abort route **ends the process** — the window's close box, Escape, Alt+F4, a cancel or back control,
  declining the IT/Developer sign-in, or any other way out. None of them returns to a previous screen, leaves a
  window standing, or lets startup proceed in any form.
- **Enforcement lives in the pipeline, not only in the screen.** The readiness check refuses to continue while
  the machine is unconfigured, so a dismissal route that the UI misses still cannot reach the shell. The screen
  is the invitation; the pipeline is the gate.
- A machine whose configuration is removed, revoked or becomes unreadable is treated as unconfigured at the
  next check, and the step returns. Losing configuration never degrades into running without it.
- When the process ends because the step was aborted, it says why before it goes, so an operator does not read
  it as a crash.
- Verification is explicit: no input sequence reaches the shell from an unconfigured machine, and every abort
  sequence ends the process.

## S11. Database changes

### S11.1 New
- `user_active_sessions` — table, create + rollback, procedures for write/validate/clear, master-list
  registration, and an entry in a validation script.

### S11.2 Repurposed
- The log store (S9) gains its writer, its reader procedures, its retention routine and its indexes.

### S11.3 Dead-weight audit
- Every deployed table, procedure, function and view is checked for any consumer. Artifacts with none have
  their SQL files **deleted** — `auth_sessions_tokens` is the first candidate, and `ops_startup_logs` is
  repurposed rather than deleted. Candidates already known: `core_buildings_catalog`, `core_buildings_history`,
  `sp_core_buildings_upsert`.
- Each deletion updates the aggregates (`AllTables.sql`, `AllSPs.sql`, `AllFunct.sql`, `AllViews.sql`,
  `AllSeeds.sql`), the validation scripts, and any doc that names the artifact.
- A SHARED artifact must never be deleted by this audit (S3.2).

### S11.4 The local-settings double pass
- Pass 1: every `ILocalSettingsService` call site, every `LocalSettings.json` key in a live file, and every
  `ApplicationData`/`ApplicationDataContainer` use.
- Pass 2: every `appsettings.json` section read by the app, and every file the app writes outside the image
  cache.
- Result recorded in this brief, with the remaining local surface reduced to connection strings and a task
  per remaining key to move or delete it.

### S11.5 The local-state inventory

Measured 2026-09-25 across every `.cs` file outside `bin`/`obj`: **85 references in production, 115 in tests,
across 11 live local-settings keys and 18 test files that fake or drive the store.** The rule is that only
connection strings may remain local, so everything below is in scope.

#### S11.5.1 Production consumers outside startup — seven items to move, one duplicate to delete

| # | File | Lines | Method(s) | What it holds | Key | Disposition |
|---|---|---|---|---|---|---|
| 1 | `MTM_Waitlist.Core/Services/ThemeSelectorService.cs` | 14, 17, 51, 63 | `LoadThemeFromSettingsAsync`, `SaveThemeInSettingsAsync` | the chosen background theme | `AppBackgroundRequestedTheme` | move to the store, person scope |
| 2 | `MTM_Waitlist.Core/Services/WaitlistSortPreferenceService.cs` | 18, 20, 32, 45 | `GetSortOrderAsync`, `SetSortOrderAsync` | waitlist sort order | `Waitlist.SortOrder` | move to the store, person scope |
| 3 | `MTM_Waitlist.Core/Services/IgnoredLocationsService.cs` | 27, 29, 38 | `GetIgnoredLocationsAsync` | inventory locations to hide | `Feature.IgnoredLocations` | move to the store at **plant scope**; writable only by IT Department and Developer (S11.5.6) |
| 4 | `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` | 97, 715, 1288, 1357 | `InitializeIgnoredLocations`, `SaveIgnoredLocationsAsync` | the same key, read and written directly rather than through the service | `Feature.IgnoredLocations` | delete the duplicate path, call the service instead, and gate the editing control on the same two roles |
| 5 | `MTM_Waitlist.Core/Services/NewRequestAlertService.cs` | 12, 14, 24, 30 | `GetEnabledAsync`, `SetEnabledAsync` | new-request alerts on or off | `User.NewRequestAlertsEnabled` | move to the store, person scope |
| 6 | `MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs` | 22, 26, 59, 76 | `MarkSeenAsync`, `LoadAsync` | which requests have been seen | `Waitlist.MessageSeen` | move to the store, person scope |
| 7 | `MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs` | 23, 65, 162, 185 | `LoadShowPartsPreferenceAsync`, `PersistShowPartsPreferenceAsync` | whether parts without pictures are listed | `Setup.DunnageImageSearch.ShowPartsWithoutImages` | move to the store, person scope |

**No new storage is needed for these seven.** The scoped settings store already exists: `config_settings_values`
with `fn_config_settings_scope_rank`, reached through `IConfigSettingsValueService` and
`sp_config_settings_values_get` / `sp_config_settings_values_upsert`. `PictureEnlargePreference` already uses it
and is the pattern to copy. The work is one substitution per item, plus a stored-procedure call at runtime
rather than a file read.

#### S11.5.2 Startup consumers — these die with the module

| File | Lines | Method(s) | What it holds | Key(s) |
|---|---|---|---|---|
| `MTM_Waitlist.Startup/Services/StartupCoordinator.cs` | 15–17, 40, 51, 128, 138, 139, 226, 227, 456 | `RunAsync`, `ResolveCentralizedDestinationAsync` | the read probe, the session token and expiry, the log destination | `Developer.RecoveryProbe`, `Startup.Session.Token`, `Startup.Session.ExpiresUtc`, `Startup.Logging.CentralizedDestination` |
| `MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs` | 41, 205, 269, 277, 278, 498, 502, 503, 507, 508, 640, 641 | `InitializeAsync`, `CompleteLoginAsync`, `FinishLoginNavigationAsync` | remember-me and the session token | `Login.RememberPassword`, `Login.RememberedUsername`, `Login.RememberedPassword`, `Startup.Session.*` |
| `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs` | 19, 72, 295, 296 | `RunStartupAsync` | the log folder chosen in the developer prompt | `Startup.Logging.CentralizedDestination`, `Startup.Logging.HostedVmLogDirectory` |
| `MTM_Waitlist.Startup/Services/StartupRecoveryService.cs` | 11, 14, 22, 26, 40 | `ResetSettingAsync`, `ResetToDefaultsAsync`, `CorruptAndRestartAsync` | the targeted repair and the reset-all path | any key |
| `MTM_Waitlist.Startup/Services/StartupLogService.cs` | 20, 26, 241 | `ResolveHostedVmLogDirectoryAsync` | where the daily log is written | `Startup.Logging.HostedVmLogDirectory` |
| `MTM_Waitlist.Startup/Services/StartupLogForwarder.cs` | 13, 15, 58 | `ResolveDestinationAsync` | where the forwarded copy goes | `Startup.Logging.CentralizedDestination` |
| `MTM_Waitlist.Startup/Services/StartupRegistrationService.cs` | 10, 12, 31 | `SubmitNewUserRequestAsync` | a New User request that nothing ever reads back | `Startup.NewUserRegistrationRequest` |
| `MTM_Waitlist.Startup/Services/SignOutService.cs` | 26, 31, 54 | `SignOutAsync` | clears the five sign-in keys | the `SignInSessionKeys` set |

#### S11.5.3 The mechanism itself — deleted, not migrated

| Artifact | Reference | Disposition |
|---|---|---|
| `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs` | 3, 5, 7, 9, 13 | delete — no caller remains once S11.5.1 is done |
| `MTM_Waitlist.Settings/Services/LocalSettingsService.cs` | 14, 21, 24, 32, 53, 55, 57, 76, 81, 84, 103, 105, 115, 119, 121, 130, 131, 138, 140, 147, 149, 151, 153 | delete — including the test-only `CorruptForTestAsync` |
| `MTM_Waitlist.Core/Contracts/Services/IFileService.cs` and `MTM_Waitlist.Core/Services/FileService.cs` | 3 and 6 | delete — `LocalSettingsService` is the only consumer |
| `LocalSettingsOptions` and its `appsettings.json` section | bound at `Services/DI/ServiceRegistrationExtensions.cs:268` | delete |
| `MTM_Waitlist.Core/Helpers/SettingsStorageExtensions.cs` | 10 | delete — already dead, no caller found |
| `MTM_Waitlist.Core/Helpers/RuntimeHelper.cs` (`IsMSIX`) | `AppNotificationService` 34, 52, 78; `SettingsViewModel` 531, 1804; `WaitlistRequestService` 1002 | **keep** — it describes the packaging mode, not stored state |
| `MTM_Waitlist.Shared/Helpers/ImageCachePaths.cs` | 52, 121 | **keep** — the image cache is the one permitted local thing |

#### S11.5.4 appsettings.json — reduced to connection strings plus one reviewed exception

| Section | Bound at | Disposition |
|---|---|---|
| `StartupDatabaseOptions` | DI 269 | keep the connection string, in the new module's own options |
| `ReceivingDatabaseOptions` | DI 270 | keep — connection string |
| `InforVisualDatabaseOptions` | read directly, not via `Configure<>` | keep — connection string |
| `LocalSettingsOptions` | DI 268 | delete |
| `StartupDevelopmentOptions` | DI 271 | delete — the developer allow-list is retired |
| `StartupLoggingOptions` | DI 272 | delete — logging goes to the store |
| `StartupWindowOptions` | DI 273 | delete, or move into the new module's window options |
| `ImageStorageOptions` (`SharedFolderPath`, `KeysFolderPath`) | `ImageLocationServiceCollectionExtensions` 65, 105 | the picture sources are machine configuration: move to the store and capture them in the new machine setup (S10) |
| `DunnageImageOptions.RootFolder` | read directly | same — a UNC share path is machine configuration |
| `MockServiceClient` (`Endpoint`, `UserName`) | — | **stays a deployment setting** (decided 2026-09-25). It carries no secret and no per-person or per-machine state, so it is configuration rather than local state — the one reviewed exception to the connection-strings-only rule |
| `ModuleSharedOptions`, `ModuleCoreSettingsOptions` | `MTM_Waitlist.Shared/DI/ModuleDependencyInjectionExtensions.cs:12`, `Services/DI/ModuleDependencyInjectionExtensions.cs:18` | **verify** — both are bound, but neither key appears in `appsettings.json`, so they currently bind to defaults. Confirm what they were meant to carry before deleting anything |

#### S11.5.5 The test surface — 115 references across 18 files

The split matters, because most of these tests assert feature behaviour and only used the local file as the
storage behind it. Deleting them wholesale would delete coverage that has nothing to do with local state.

**Deleted, because they exist only for the local-file mechanism:**

| File | References | Why it goes |
|---|---|---|
| `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs` | 13 | tests the store itself, including `CorruptForTestAsync_WritesInvalidJsonPayloadAsync` and both reset paths |
| `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs` | 7 | tests the targeted repair and the reset-all path |
| `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs` | 6 | the reset-to-defaults and retry cases, plus `NoOpStartupRecoveryService` and `NoOpLocalSettingsService` |
| `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` | 6 | the settings-path guard, the read probe, the repair and the repair-failed block |
| `MTM_Waitlist.Tests/Services/StartupLogServiceTests.cs` | 5 | dies with the file-based logging path |
| `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs` | 5 | dies with the module; its key-clearing assertion is replaced by the store session's clear |

**Kept and re-pointed at the store seam** — the fake `ILocalSettingsService` in each is replaced, the
assertions stay: `Core/Services/IgnoredLocationsServiceTests.cs` (7), `Core/Services/NewRequestAlertServiceTests.cs`
(5), `Core/Services/NewRequestAlertNotifierTests.cs` (5), `Core/Services/UrgencySettingsServiceTests.cs` (1),
`Module_Core/Services/WaitlistSortPreferenceServiceTests.cs` (6), `Module_Settings/SettingsViewModelTests.cs` (9),
`Module_Settings/TestDoubles.cs` (5), `Module_Setup/Services/SetupPersistenceServiceTests.cs` (5),
`Module_Setup/Services/SetupWorkflowServiceTests.cs` (5), `Module_Waitlist/Services/WaitlistInventoryServiceTests.cs`
(6), `Module_Waitlist/Services/WaitlistRequestServiceTests.cs` (5), `ViewModels/LoginViewModelTests.cs` (13).

`MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` (1) needs no change: it names a retired key inside a
pattern string, not a compile-time reference.

#### S11.5.6 One plant-wide setting, editable by two roles

The ignored-locations list is **plant-wide**: it applies to every machine and every person, and only IT
Department and Developer may change it. Three consequences, all of which are work rather than notes:

- **Scope.** It belongs at the store's plant-wide scope (the `all_users` rank in `fn_config_settings_scope_rank`),
  not per machine and not per person. The reading side does not change: the waitlist inventory filter consumes
  the list as it does today.
- **Permission.** Editing needs its own permission key, added to the catalogue, the role baselines and the seeds,
  and enforced where the list is edited — not only hidden in the UI. A hidden control is not a permission.
- **The Settings page.** The ignored-locations editor becomes read-only for every role except those two, and the
  privileges page must show the new key. This is the same privilege-surface work decision 8 already requires, so
  it lands in one change rather than two.

## S12. Failure semantics

- A blocking condition states a diagnosis specific to its cause; a blocked startup never reaches the shell.
- An unconfigured machine is a blocking condition with no remedy offered beyond completing it: the step ends in
  a configured machine or an ended process (S10.1).
- **Restore Defaults** first shows a popup listing exactly what will be reset, then resets only what is broken
  — this machine's store-side configuration rows. It never touches people, roles or permissions.
- Where a fault can be repaired silently without hurting the operator, it is repaired silently (decision 12).
- Retry repeats the failed step; a database fault offers no reset that cannot repair it.
- Unreachable picture sources and unreachable Infor Visual remain **best effort** and never block the start.
- The picture-cache step and the Infor Visual priming each get a **bounded** wait; the two unbounded waits
  found in the sweep (an unloaded splash view, and a stalled share with no cancellation) must not be re-created.

## S13. Edge cases to preserve

The 111 cases found by the three sweeps are the acceptance surface for the rebuild. Grouped:

1. Settings/config guards and targeted repair (6).
2. Connection string, store reads, session precedence and clock rules (13).
3. Identity, role resolution and the (now removed) developer override (3 retained, 2 dropped).
4. Password and temporary-credential rules, including the five-attempt limit (13).
5. Computer gate and registration outcomes, including the duplicate-name and no-MAC paths (13).
6. Window, splash and shell-state behaviour, including maximize fallback (14).
7. Blocked-state remedies, now with the targeted reset (9).
8. Picture-cache best-effort behaviour and archive retention (11).
9. Diagnostic guarantees: never waits, never fails, survives shutdown, bounded retention (12).
10. Reachability probing and hysteresis (5).
11. Sign-out and relaunch, including a refused relaunch (6).
12. Activation, notifications and deep-link routing (4).

Cases that **change** under these decisions: the developer allow-list override, the log-folder prompt, local
session precedence, local-file repair, and Restore-Defaults-offers (now always offered, but scoped).

Cases that are **new** under these decisions: every abort route out of machine configuration ends the process
(S10.1), and the pipeline — not the screen — refuses to continue without configuration.

## S14. Constraints and non-goals

- No self-service password reset; no reset offered on the sign-in screen.
- A reset issues a one-time four-digit PIN; only a salted hash is stored; the legacy `0000` stays recognised.
- A reset must not end an existing session.
- The attempt limit applies only to accounts holding a temporary credential; ordinary sign-in has no limit.
- No "continue anyway" past the startup gate, no optional connection check, no dismissible banner.
- No manual demo/mock mode, ever.
- Plaintext credentials and tokens are never stored; tokens are held as salted hashes.
- Every schema artifact ships `create.sql` + `rollback.sql` and is registered in the aggregate files.

## S15. Risks and known conflicts

| Risk | Why it matters |
|---|---|
| The key path used `X:` | **Resolved 2026-09-25.** `X:` is a per-user mapped drive and would not resolve for another account, session or service. The UNC form is now the setting, and the file is confirmed present (S8.3). |
| Roles from the store only | Kills the no-sign-in developer walkthrough that `READINESS-CHECKLIST.md` and the UI-automation instructions rely on. |
| "Nothing local" touches seven production consumers and 18 test files | Inventoried in full in S11.5 with file, line and method: theme, sort order, ignored locations, new-request alerts, message-seen, show-parts-without-images, and remember-me. The scoped store already exists, so each is a substitution rather than new storage. |
| Logging to the store cannot record a store outage | Accepted in S9; the splash is the only record for that case. |
| Two audit tests pin the old shapes | `PermissionGateSiteAuditTests` and `PageActivationAuditTests` must be rewritten in the same change. |
| The corrupt-settings repair path loses its cause | It existed because settings were a local JSON file, so its tests assert a mechanism that will not exist. The six files that hold them are listed for deletion in S11.5.5 and tasked in Phase 5.4. |
| Logging is compiled out of Release builds today | `[Conditional("DEBUG")]` removes all 482 static log calls in Release, so a production fault currently leaves no record anywhere. The replacement seam must be unconditional (S9.2). |
| A hard-close gate can read as a crash | An operator who aborts machine configuration sees the app vanish. The step must state why it is closing, and the message must be localised, or the gate gets reported as instability. |

## S16. Verification

- The retired-symbol audit fails the build if any removed name returns.
- After the removal and placeholder, the app must not reach the shell by any path.
- The suite's startup tests are deleted, not disabled; a green run afterwards must be attributable to the new
  tests alone.
- The database schema validator runs clean after the dead-weight removals.
- Each reconstructed edge case in S13 gets a test in the new module, or a written reason why it is gone.
