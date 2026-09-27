# Implementation Plan: Startup Rebuild

**Branch**: `010-startup-rebuild` | **Date**: 2026-09-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-startup-rebuild/spec.md`

**Scale note**: This is the largest change in the repository so far. It spans eight areas: the
`MTM_Waitlist.Startup` project (rebuilt in place), a new `MTM_Waitlist.Logging` class library, the
`Module_Startup/Views` XAML surface, the app host and its DI registration, `MTM_Waitlist.Core` contracts and
models, `MTM_Waitlist.Settings` (the log panel and the ignored-locations editor), the `Database/` tree (two new
tables, one repurposed table, ten new procedures, four new permission keys, ten procedures deleted — the nine
startup-only ones plus `sp_core_buildings_upsert` — and three tables deleted), and
`MTM_Waitlist.Tests`. A reader should watch three things above all: the ordering (the old surface is deleted
and replaced by a placeholder before anything new is built), the two deliberate deferrals (`StartupDebugLog`
and `StartupState` cannot be deleted in the removal phase or the tree stops compiling), and the protected
artifacts the dead-weight audit must leave alone — the list T024 carries and brief S3.2 enumerates: five shared
tables, eleven shared procedures plus the retained `fn_server_utc_now`, three shared seeds, and the five
aggregate and descriptions files.

## Summary

The entire startup surface is deleted and rebuilt: the launch pipeline, sign-in, the splash, the machine gate,
the startup logging, the startup contracts and models, and the startup tests. The work is sequenced so that
removal is provable rather than asserted: a guard test fails the build if a removed symbol returns, the old
surface is deleted, and a minimal placeholder window runs as the only launched surface. Only then is the new
pipeline built.

The rebuilt launch is a data-driven pipeline of named steps that announces each step before running it and
renders an append-only activity feed, with an error toast anchored to the bottom of the window. Two read-only
contracts, person identity and machine facts, replace the single `StartupState` object. Sessions move into a
new `user_active_sessions` table judged on the store's clock, remembered sign-in moves into the store encrypted
with a pre-existing shared key read from a UNC path, and every diagnostic goes to the store on
`ops_startup_logs` through one unconditional logging module with a developer panel in Settings. Machine
configuration becomes a hard gate satisfied by a new pre-sign-in setup surface that only `IT Department` or
`Developer` may open and that cannot be bypassed by any route.

The stack is unchanged: WinUI 3 on .NET 10 (`net10.0-windows10.0.19041.0`, win-x64), MVVM through
CommunityToolkit.Mvvm, DI through `Microsoft.Extensions.Hosting`, MySQL through MySqlConnector with every data
operation behind a stored procedure. Two additions are worth naming because they are new here rather than
familiar: a new class library `MTM_Waitlist.Logging` holding the logging module and its `ILogger` provider, and
AES decryption of the remembered sign-in using `System.Security.Cryptography` with a 32-byte key read from the
shared key file.

The logging this plan already builds is specified in more detail than the brief carried. Every diagnostic records
the fault whole — its type, message, stack and complete exception chain — with a fingerprint that groups repeated
occurrences, the application's version and the machine's runtime context, the action and view it happened under,
and, for a store fault, the provider's own diagnostics with no parameter values. All of it is gathered by the
logging module rather than handed in by a call site, and none of it is written to the machine when the store
refuses it. `MTM_Waitlist_Exception_Logging_Reference.md` is the reconciled reference for that detail, and the
four decisions it produced are D25 to D28 in `research.md`, with the panel-copy decision D29 added at the owner's
direction.

The panel that reads those entries can copy one of them, or the view on screen, to the clipboard as text a person
can paste elsewhere — to the clipboard and nowhere else, writing no file, with the copy excluded from clipboard
history and device syncing (D29).

The owner's own `STARTUP-REBUILD-CHECKLIST.md` (181 tasks, 11 phases, 11 gates) is the execution instrument for
this plan. `tasks.md` carries its content and its gates and is sequenced by user-story priority.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0-windows10.0.19041.0`, win-x64)
**Primary Dependencies**: Microsoft.WindowsAppSDK 2.3.1, WinUIEx 2.9.2, CommunityToolkit.Mvvm 8.4.2,
Microsoft.Extensions.Hosting 10.0.10, MySqlConnector 2.6.1
**Storage**: MySQL `mtm_waitlist`. New table `user_active_sessions`; new table for the remembered sign-in;
`ops_startup_logs` extended and repurposed as the one log store; existing `config_settings_values` and
`core_computers_registry` carry the scoped preferences and the machine configuration
**Testing**: xUnit in `MTM_Waitlist.Tests`, plus the build-gate audit tests (`RetiredSymbolAuditTests`,
`InlineSqlAuditTests`, `PermissionGateSiteAuditTests`, `PageActivationAuditTests`)
**Target Platform**: Windows desktop, unpackaged and MSIX
**Project Type**: Desktop app plus per-module class libraries, views in the app as the composition root
**Performance Goals**: No wait on the launch path exceeds 30 seconds; the picture refresh and the Infor Visual
priming are bounded more tightly because both are best effort
**Constraints**: The machine gate cannot be bypassed; logging is unconditional so Release builds record; only
connection strings and the reviewed `MockServiceClient` exception stay on the machine
**Scale/Scope**: Eight areas, roughly 25 production files touched in `MTM_Waitlist.Core`,
`MTM_Waitlist.Settings` and `MTM_Waitlist.Setup` for the local-state migrations alone, 489 static log call
sites across 83 files, a test surface of eleven deletions and eleven re-points (the authoritative list is
`specs/010-startup-rebuild/local-state-test-migration.md`), and 26 database artifacts touched: two tables and ten
procedures added, one table repurposed, and three tables and ten procedures deleted, with the aggregates, the
descriptions file, the validation scripts and the seeds moving with them

## Project Structure

### Documentation (this feature)

```text
specs/010-startup-rebuild/
├── plan.md              # This file
├── spec.md              # The specification this plan satisfies
├── research.md          # Phase 0 output: decisions with rationale
├── data-model.md        # Phase 1 output: entities, fields, transitions
├── contracts/           # Phase 1 output: the interfaces consumers code against
│   ├── launch-step-contract.md
│   ├── identity-contracts.md
│   ├── logging-contract.md
│   ├── machine-configuration-contract.md
│   └── sql-contracts.md
├── checklists/
│   └── requirements.md  # Written by the specify step
├── local-state-test-migration.md               # The authoritative test-surface map (finding I5)
├── MTM_Waitlist_Exception_Logging_Reference.md # The reconciled diagnostic-capture reference (D25–D28)
└── tasks.md             # Written by the tasks step, not by this plan
```

### Source code (repository root)

The tree below is the delivered layout. `REBUILT` means the directory survives with new contents, `NEW` means
it does not exist today, `DELETED` means it is removed.

```text
MTM_Waitlist.Startup/                                  # REBUILT in place, same project name and sln entry
├── Services/
│   ├── LaunchPipeline.cs                              # NEW  the single entry point the host calls
│   ├── LaunchStepCatalog.cs                           # NEW  steps as data: name, description, category
│   ├── LaunchStepRunner.cs                            # NEW  announce, run, report started/completed/failed
│   ├── LaunchActivityFeed.cs                          # NEW  the append-only line sink the splash binds
│   ├── MachineConfigurationService.cs                 # NEW  read/write this machine's configuration
│   ├── MachineSetupGate.cs                            # NEW  IT Department / Developer unlock, never the shell
│   ├── PersonIdentityService.cs                       # NEW  read-only person identity contract
│   ├── MachineFactsService.cs                         # NEW  read-only machine facts contract
│   ├── LaunchSessionService.cs                        # NEW  user_active_sessions, store clock
│   ├── RememberedSignInService.cs                     # NEW  AES over the shared key file
│   ├── PictureCacheStep.cs                            # NEW  best-effort refresh with a bounded wait
│   ├── VisualVerdictPrimingStep.cs                    # NEW  bounded reachability priming
│   ├── StartupRecoveryService.cs                      # REBUILT  targeted reset plus silent repair
│   └── DependencyInjection/ModuleDependencyInjectionExtensions.cs
├── ViewModels/                                        # Splash, SignIn, MachineSetup, BlockedState
└── MTM_Waitlist.Startup.csproj

MTM_Waitlist.Logging/                                  # NEW class library, its own module
├── ILogService.cs                                     # the seam call sites use
├── LogEntry.cs                                        # severity, module, action, outcome, error type, detail
├── ExceptionDetailSerializer.cs                       # the exception chain, and the fingerprint that groups it
├── DiagnosticContext.cs                               # runtime, view and store context, gathered by the seam
├── StoreLogWriter.cs                                  # queue, batch, bounded flush, hash chain
├── StoreLoggerProvider.cs                             # the ILogger provider: 226 tree-wide `ILogger` call sites stay untouched, 162 of them the app's own
└── MTM_Waitlist.Logging.csproj

Module_Startup/Views/                                  # DELETED, then REPLACED with the new surfaces
├── SplashWindow.xaml, SplashPage.xaml                 # REBUILT  activity feed plus bottom error toast
├── SignInWindow.xaml                                  # REBUILT  carries the hint that states what is missing
├── MachineSetupWindow.xaml                            # NEW  pre-sign-in, display name, description, pictures
├── BlockedStateWindow.xaml                            # NEW  diagnosis, Retry, Restore Defaults, Close
└── SplashView, LoginWindow, LoginPage                # DELETED — the old window and page pairs. SplashWindow and SplashPage are REBUILT in place, and SignInWindow replaces LoginWindow

MTM_Waitlist.Core/
├── Contracts/Services/                                # IPersonIdentity, IMachineFacts NEW
│                                                      # IStartup*, IActivationService, IComputerGateService,
│                                                      # ISignOutService, IAppProcessRestarter,
│                                                      # IAppLifecycleService, ILocalSettingsService,
│                                                      # IFileService, IComputerRegistryService DELETED
│                                                      # (the registry capability is rebuilt inside
│                                                      # MTM_Waitlist.Settings — research D30, T187)
├── Activation/                                        # DELETED with ActivationService
├── Models/                                            # StartupResult, StartupSessionSnapshot,
│                                                      # StartupCredentialCheckResult, StartupPasswordReset-
│                                                      # Requirement, the four Startup*Options,
│                                                      # LocalSettingsOptions, StartupState DELETED
├── Helpers/StartupDebugLog.cs                         # DELETED in Phase 6, after the call-site migration
├── Permissions/PermissionKeys.cs                      # four new keys
└── Services/                                          # ThemeSelectorService, WaitlistSortPreferenceService,
                                                       # IgnoredLocationsService, NewRequestAlertService
                                                       # re-pointed at the scoped store

MTM_Waitlist.Settings/
├── Services/LocalSettingsService.cs                   # DELETED
├── Services/ComputerRegistryService.cs                # REBUILT here from MTM_Waitlist.Startup, over the rebuilt machine contracts (D30)
└── ViewModels/SettingsViewModel.cs                    # ignored-locations editor read-only for all but two roles
MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs   # re-pointed at the store
MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs # re-pointed at the store

App.xaml.cs                                            # window handoff removed, one host seam instead
Services/AppLifecycleService.cs                        # DELETED
Services/DependencyInjection/ServiceRegistrationExtensions.cs         # startup registrations removed
Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs   # startup plus logging modules

Database/
├── Tables/34_user_active_sessions/                    # NEW  create.sql, rollback.sql
├── Tables/35_auth_remembered_sign_ins/                # NEW  create.sql, rollback.sql
├── Tables/05_auth_sessions_tokens/                    # DELETED, superseded
├── Tables/06_core_buildings_catalog/                  # DELETED as dead weight
├── Tables/07_core_buildings_history/                  # DELETED as dead weight
├── Tables/10_ops_startup_logs/                        # EXTENDED  module, error type, exception detail, error fingerprint
├── Tables/01_core_users_profiles/                     # unchanged
├── Tables/02_core_computers_registry/                 # unchanged in shape; carries the machine's identity, while the picture sources live in `config_images_locations` at computer scope
├── Tables/08_config_settings_values/                  # unchanged, now carries the seven scoped preferences
├── StoredProcedures/                                  # session, remembered sign-in and log procedures NEW
│                                                      # the nine startup-only procedures DELETED
├── Seeds/seed_permission_role_baselines/              # four new keys baselined per role
├── Seeds/seed_dev_masked_baseline/                    # Developer granted to JKoll and JohnK
├── Validation/startup_schema/                         # reworked to cover shared tables only
├── Bootstrap/update_table_descriptions.sql            # the guarded additions and the retired-objects sections
├── Tables/AllTables.sql                               # kept in sync by hand
├── StoredProcedures/AllSPs.sql                        # kept in sync by hand
├── Functions/AllFunct.sql                             # kept in sync by hand
├── Views/AllViews.sql                                 # kept in sync by hand
└── Seeds/AllSeeds.sql                                 # kept in sync by hand

MTM_Waitlist.Tests/
├── Module_Startup/                                    # startup tests deleted, new ones written
├── Module_Mock/RetiredSymbolAuditTests.cs             # extended with the whole removed surface
├── Module_Mock/InlineSqlAuditTests.cs                 # unchanged, it is the gate for this plan
├── Module_Core/Permissions/PermissionGateSiteAuditTests.cs   # rewritten against the new contract
└── Module_Waitlist/Views/PageActivationAuditTests.cs  # exemption list re-pinned to the new page set

capabilities/startup/                                  # DELETED with its registry row, then re-adopted
capabilities/startup-diagnostics/                      # DELETED with its matched code and its registry row
STARTUP-FLOW.md, .html, .pdf                           # DELETED
tools/migrate-logging-callsites.ps1                    # already written, used in Phase 6.3
```

**Structure Decision**: The existing repo shape is kept unchanged. Non-view code stays in
`MTM_Waitlist.*` class libraries, XAML stays in the app under `Module_*/Views` as the composition root, and the
rebuilt startup pipeline stays in `MTM_Waitlist.Startup` under its own name so the solution entry, the app's
`ProjectReference` and the test project's reference can all be re-pointed rather than re-invented. The only new
project is `MTM_Waitlist.Logging`, because logging is deliberately not part of startup (S9) and must be usable
by the shell, Settings and the module libraries that never reference startup.

## Constitution Check

Gate assessed against the constitution at v1.2.0. Seven principles plus three additional constraints.

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery (NON-NEGOTIABLE) | **PASS.** The spec precedes this plan, and this plan precedes tasks and implementation. The owner's `STARTUP-REBUILD-CHECKLIST.md` is the execution instrument and is consumed through the `checklist-execution` skill, where a task is ticked only with build or test proof. The plan has no step that ticks work on assertion alone. |
| II. Live Data Integrity, internal stores never mocked | **PASS.** Every one of the seven preference migrations writes to `config_settings_values` in the live `mtm_waitlist` store; nothing is redirected to a file, a cache or a sample. The new session, remembered sign-in and log stores are the live store. No demo or mock mode is introduced, and FR-028 plus the extended `RetiredSymbolAuditTests` fail the build if the retired surface returns. `MockServiceClient` is retained in `appsettings.json` as a deployment setting, which is the endpoint for the on-host cache service and not a data substitution, so it does not touch this principle. |
| III. Stored-Procedure-First database discipline | **PASS.** Every new data operation is a stored procedure. The two new tables and every new procedure ship `create.sql` plus `rollback.sql` and are registered in the hand-maintained aggregates. The log retention routine is a stored procedure, not application-side SQL, so the retention cadence does not put statement text in the app. `InlineSqlAuditTests` is the gate. FR-025 (only what is needed to reach the store or the external read-only system stays local) directly serves this principle by removing the settings file that bypassed the store. |
| IV. MCP-First, grounded decisions (NON-NEGOTIABLE) | **PASS, with a named obligation on implementation.** This plan is grounded in the repository's own authoritative sources: `STARTUP-REBUILD-BRIEF.md` (the owner's design brief, S1 to S16), the living spec `capabilities/startup/spec.md`, the constitution, and the actual schemas and seeds read from `Database/`. Two implementation areas are exactly the case the principle covers and MUST consult documentation before code is written: the WinUI 3 API surface for the new splash and setup windows, and the `System.Security.Cryptography` AES APIs for the remembered sign-in together with the `ILogger` provider pattern. Both are listed as tasks rather than left to recall. |
| V. WinUI 3 platform conformance | **PASS.** All new views live in the app under `Module_Startup/Views` and `Module_Settings/Views`, use `Microsoft.UI.Xaml`, follow the existing MVVM generators, and route navigation through the existing shell services. User-facing strings are localised through `.resw` and `GetLocalized`, including the new machine-setup and blocked-state wording, because S15 records that an unlocalised hard close reads as a crash. The placeholder window and the machine-setup window each carry an `AutomationProperties.AutomationId` so UI automation never depends on a window title, which the `winui3-ui-automation` instructions require for this app. No hardcoded pixel boundaries are introduced; the splash keeps its configurable size and its non-fatal sizing failure path. |
| VI. Evidence-based verification gates | **PASS.** The plan's phase gates are the constitution's gates: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` at zero warnings and zero errors, and a green `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`. Three sequencing decisions exist only to keep that gate reachable during removal: `StartupDebugLog` is deleted in Phase 6 rather than Phase 2 because 489 call sites still compile against it, `StartupState` is deleted in Phase 4 for the same reason, and the two files stay in the tree until their replacements land. `WMC9999` is treated as a masked XAML error and surfaced rather than ignored. |
| VII. Readable, end-user-facing delivery | **PASS.** Every step of this work reports in plain English, what changed and what is still open. The 181-task checklist is executed through `checklist-execution`, so progress is reported as short sentences and never as a task-list dump. Anything the person must do after a session is the last section of the reply. |
| Additional: Security & Secrets | **PASS with one recorded deviation.** The shared key file is read only. It is never written, rotated, logged, copied into the repository or echoed by the log panel, and this is asserted by a named security review task (Phase 6.1). Session tokens are stored as salted hashes, and no credential reaches the log store; the key file path is the only secret-adjacent value ever written, which the same security task proves. The remembered sign-in is stored as decryption-capable ciphertext rather than DPAPI-protected, and that single departure from this constraint is recorded in Complexity Tracking below. The constraint's other clause — that a destructive operation must be explicitly confirmed — is addressed in the same place and is **not** deviated from. |
| Additional: External Integration & Cache Boundaries | **PASS.** Infor Visual stays read-only. The reachability verdict is still settled before a screen opens (FR-027) and is unchanged in kind, with a bounded wait added; an unreachable source remains best effort and never blocks the start. The machine-configuration picture sources are machine configuration, not external data, and no cached copy becomes authoritative. No AI endpoint is introduced. |
| Additional: Documentation & Extensibility | **PASS.** Phase 10 adds the `.github/instructions` embed guide for the new module with an `applyTo`, covering how to embed the module, how to add a step, how to add a log entry and how to add a machine-configuration field, plus the invariants a later change must not break. The changelog, the README module list, the UI-automation instructions and the `test-conventions` instructions are updated in the same change, and stale references to the retired splash and sign-in surfaces are removed. |
| Design invariants from the living spec `startup` | **PASS, with one deliberate reversal recorded.** The living spec's requirements are carried forward: steps announced before they run, a session judged on the store clock, an unreadable hardware identity admitted, remedies limited to what can work, retry repeating only the failed phase, and the picture refresh best effort with its own reported line. The one requirement of the living spec that this plan deliberately reverses is that a damaged local setting is repaired and retried: the local settings file is removed entirely, so its targeted-repair behaviour is replaced by store-side repair and silent repair, and the six test files that assert the file mechanism are deleted with it (S11.5.5). That reversal is required by FR-025 and is recorded in `research.md`. The living spec's "one destination out of three" is the second count this plan supersedes rather than carries forward: FR-001 now defines five terminal outcomes, and `LaunchOutcome` in `contracts/launch-step-contract.md` §5 is their single statement. |

### Complexity Tracking

One departure from a constitution constraint is necessary, and the Review rule requires it to be recorded here
rather than left as an unqualified PASS.

| Deviation | Constraint | Why it is necessary | Alternatives, and why they were rejected |
|---|---|---|---|
| The remembered sign-in is stored in the store as an AES-256 payload (`payload_ciphertext` plus `payload_iv`) with a key fingerprint, instead of DPAPI-protected (`ProtectedData`, `CurrentUser` scope) | Additional: Security & Secrets — "service credentials MUST be stored DPAPI-protected (`ProtectedData`, `CurrentUser` scope)" | The payload has to be decrypted on the person's behalf on whatever machine they next use. DPAPI's `CurrentUser` scope binds a value to one Windows account on one machine, so a value another machine must decrypt cannot be protected with it. Keeping the payload on the machine so DPAPI could protect it is forbidden by FR-025 and would defeat FR-014's "held against the person in the store" | (a) A local DPAPI-protected file — rejected: FR-025 leaves nothing on the machine but what reaches the store, and the choice is per machine. (b) Columns on `core_users_profiles` — rejected: a shared profile row cannot hold a per-machine choice. (c) Columns on `user_active_sessions` — rejected: FR-011 clears sessions on sign-out while a remembered sign-in must survive it. (d) Hashing rather than encrypting — rejected: FR-014 requires the value to be decryption-capable. Full reasoning in `research.md` D12 |

**Second clause, checked and not deviated from.** The same constraint requires that a destructive operation — a
database restore or a bulk delete — be explicitly confirmed. This feature adds neither to the running
application: its store changes are developer-run migrations, and the three tables and ten procedures it drops
are migration-time. Each ships the `rollback.sql` that lets a DBA promote or reverse it, the removal is gated by
the phase checkpoints, and the approval is recorded before each deletion set runs — at T020 for the nine
startup-only procedures and at T056 for the dead-weight set. That is why the
row above is not a second deviation.

No other constitution violation is necessary. Two plan-level resolutions are worth the owner's eye even though
neither is a violation: the ignored-locations editor gains a distinct edit key rather than narrowing the
existing key (so FR-030 keeps holding while FR-024 is satisfied), and the remembered sign-in needs a table the
brief did not name. Both are recorded in `research.md`.

Re-checked after Phase 1 design: no new violation. The design adds one table the brief did not name, which
touches Principle III only in the ordinary way, and it ships `create.sql` plus `rollback.sql` and its aggregate
entries like every other artifact.

Re-checked after the exception-logging reconciliation (2026-09-26): still no violation, and one principle is
served more directly than before.

- **II. Live Data Integrity** is unaffected: nothing local is introduced. The reference document's proposed
  durable local queue is explicitly rejected (D27), and the cost — an unrecorded diagnostic is lost — is stated
  in `spec.md`'s Assumptions rather than hidden.
- **III. Stored-Procedure-First** is unchanged in shape and now covers one more field: `error_fingerprint` is a
  column written by `sp_ops_startup_logs_insert` and read by `sp_ops_startup_logs_filter`, never by a statement
  issued from C#.
- **V. WinUI 3 platform conformance** gains one obligation: a user-facing failure message is localised, so an
  operator never reads an exception message. The reference's generic example showed exactly that, and it was
  corrected.
- **V again, for the panel's copy**: the clipboard is `Windows.ApplicationModel.DataTransfer`, which is usable
  from desktop apps and is not an MSIX-only API, so it needs no `RuntimeHelper.IsMSIX` guard. The panel reports a
  refused copy rather than raising one, which is what the documented `SetContentWithOptions` overload is for.
- **Security & Secrets** gains a concrete redaction rule rather than a general one: parameter names and counts
  may be recorded, parameter values may not, and a provider message is truncated at the first `=` after a
  parameter name. `contracts/logging-contract.md` §6 remains the authority for what is never written, and the
  panel's copy carries nothing beyond it. The copy also excludes clipboard history and device syncing, because a
  diagnosis names a machine and a person and both options default to on.

## Delivery phases

The order below is the owner's checklist order. `tasks.md` carries its content, its phases and its gates
without contradiction, but is sequenced by the specification's user-story priority; the three divergences that
follow from that are named in its Dependencies & Execution Order section.

| Phase | What it does | Gate |
|---|---|---|
| 1. Guard and baseline | Retired-symbol patterns for the whole old surface, plus the pre-removal build and suite result and the current splash and sign-in geometry | The guard fails the build when a removed symbol returns, and the baseline is recorded |
| 2. Remove the old surface | The `MTM_Waitlist.Startup` project, `Module_Startup/Views`, the `App.xaml.cs` window handoff, the Core activation and startup contracts, the startup-only SQL, the startup tests, the living spec and the docs. `StartupDebugLog` and `StartupState` deliberately stay | The solution builds, the guard passes, no removed symbol resolves |
| 3. Placeholder and proof | A minimal placeholder window is the only launched surface, with a stable automation id and a Close button | The app launches to the placeholder, reaches the shell by no path, and the suite is green with every deleted test accounted for |
| 4. The two identity contracts | Person identity and machine facts, implemented and registered, the roughly twenty `StartupState` consumers migrated, then `StartupState` deleted | Both contracts exist and are registered, every consumer reads them, the audit passes |
| 5. Local state removal and store foundation | The seven preferences into `config_settings_values`, the local-settings mechanism deleted, `appsettings.json` reduced, the mechanism-only tests deleted, `user_active_sessions` created, the dead-weight SQL audit | No production code references a local settings type, only connection strings and the reviewed exception remain, the schema validator passes |
| 6. Logging module and panel | The log store on `ops_startup_logs`, the unconditional seam, the exception chain and the fingerprint it groups by, the gathered runtime, view and store context, the 489 call-site migration, the `ILogger` provider, the developer panel, the panel's copy of one entry or the listed view, the file-based path removed | A release build writes entries to the store carrying the fault whole, the panel reads, groups and filters them, a developer can copy one entry as pasteable text without writing a file, and no local log file is produced |
| 7. Machine configuration and privileges | The pre-sign-in setup screen, the gate that unlocks it, the pipeline refusal, the abort routes, the four permission keys, the seeds | An unconfigured machine stops at setup and cannot be bypassed by any input or code path |
| 8. The new pipeline | Steps as data, the runner and the feed, the splash and toast, the host seam, sign-in with the machine gate, forced password change, remember-me, the picture step and the verdict priming, the blocked state and targeted reset | The pipeline reaches the shell, the gate and setup in the right order, every block states its cause, no wait is unbounded |
| 9. Acceptance surface | A test for every reconstructed edge case, and live runs against a fresh store, an unreachable store and a slow store | Every applicable edge case has a test, the live runs pass, the difference against the baseline is explained |
| 10. Embed instructions and close-out | The module instruction file, living-spec re-adoption, changelog, README, the final audit | The embed guide exists, the registry matches the tree, the audit is clean |

Two orderings inside that table are load-bearing and must survive into `tasks.md`: `StartupDebugLog` is deleted
in Phase 6 and `StartupState` in Phase 4, both after their replacements exist, because deleting either in Phase
2 leaves the tree uncompilable and the Phase 2 gate unreachable.

## Key decisions

Phase 0 decisions with their rationale and the alternatives rejected are in [research.md](./research.md).
