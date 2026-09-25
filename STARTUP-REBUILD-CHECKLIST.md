# Startup Rebuild — Removal and Rebuild Checklist

Source of truth: `STARTUP-REBUILD-BRIEF.md`. Every `(Ref: S<n>)` below resolves to a section of that brief.
Consume with the `checklist-execution` skill; each task names exactly one persona.

Order is deliberate: the old surface is deleted and replaced by a placeholder **before** anything new is
built, so that "the old logic is gone" is provable rather than asserted. From Phase 4 onward the rebuild is
fully enumerated: the two identity contracts come first, because the local-state migrations, the logging seam
and the pipeline all wait on them.

---

## Phase 0: Inventory and decisions (complete)

### Subphase 0.1: Evidence
- [x] **Documentation: Record every behaviour the startup path performs, in launch order, with file evidence.** (Ref: S13) | **Persona: Tech Lead** — verified 2026-09-25: three independent read-only sweeps (code+tests, docs/history+DB, integration/impact); 86 behaviours, 100 edge cases, 10 local settings keys, 4 unbounded waits.
- [x] **Documentation: Record every edge case, branch, guard, retry and fallback across code, tests and documents.** (Ref: S13) | **Persona: Tech Lead** — verified 2026-09-25: 111 cases including 43 recorded only in docs, plus 12 documented contradictions.
- [x] **Documentation: Map database artifact ownership and flag every shared artifact.** (Ref: S3.2, S3.3) | **Persona: Database Engineer** — verified 2026-09-25: 10 startup-only procedures identified; 12 shared procedures, 5 shared tables and 4 shared seeds flagged.
- [x] **Documentation: Write the design brief that the rebuild and this checklist both reference.** (Ref: S1) | **Persona: Tech Lead** — verified 2026-09-25: `STARTUP-REBUILD-BRIEF.md`, sections S1–S16.
- [x] **Documentation: Lock the design decisions with the owner.** (Ref: S2) | **Persona: Tech Lead** — verified 2026-09-25: 20 decisions across three question rounds.

Next task: **none — this phase is complete.**

**GATE: Phase 0 is complete when the brief exists and the owner has answered all design questions. Both hold.**

---

## Phase 1: Guard and baseline

Nothing is deleted until the guard exists and the baseline is recorded. The guard is what stops the old
symbols creeping back during the rebuild.

### Subphase 1.1: Retired-symbol guard
- [ ] **Testing: Add retired-symbol patterns for the entire old startup surface to `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`.** (Ref: S3.1) | **Persona: QA Engineer**
- [ ] **Testing: Add a self-test proving each new pattern bites when its symbol is reintroduced.** (Ref: S3.1) *Depends on: the pattern-set task above* | **Persona: QA Engineer**
- [ ] **Testing: Assert no file remains under `Module_Startup/Views` and no type remains in namespace `MTM_Waitlist.Module_Startup`.** (Ref: S3.1) *Depends on: the pattern-set task above* | **Persona: QA Engineer**
- [ ] **Testing: Assert no member of the removed `MTM_Waitlist.Core` startup contracts, models or options survives.** (Ref: S3.1) *Depends on: the pattern-set task above* | **Persona: QA Engineer**

Next task: **Testing: Add retired-symbol patterns for the entire old startup surface to `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`.** | **Persona: QA Engineer**

### Subphase 1.2: Baseline
- [ ] **CI/CD: Record the pre-removal build and suite result plus the skipped count, for comparison after the gate.** (Ref: S16) | **Persona: DevOps Engineer**
- [ ] **Testing: Capture the current splash and sign-in window geometry, and their automation names, for the post-removal comparison.** (Ref: S16) | **Persona: QA Engineer**

Next task: **CI/CD: Record the pre-removal build and suite result plus the skipped count, for comparison after the gate.** | **Persona: DevOps Engineer**

**GATE: the guard fails the build when a removed symbol is reintroduced, and the baseline is recorded. No deletion before this holds.**

---

## Phase 2: Remove the old startup surface

> Two types are deliberately **not** deleted here, because other code still compiles against them and this
> phase's gate needs a green build:
>
> - `StartupDebugLog` — 489 call sites across 83 files. Its deletion moves to Phase 6, after
>   `tools/migrate-logging-callsites.ps1` has repointed every call site at the replacement (Ref: S9.4).
> - `StartupState` — roughly 20 consumers in Settings, Waitlist, Shared and Core. Its deletion moves to
>   Phase 4, the phase that defines the contracts replacing it and migrates those consumers (Ref: S7).
>
> Everything else in the S3.1 removal list is deleted here.

### Subphase 2.1: Host and orchestration
- [ ] **Service Layer: Remove the splash and sign-in window statics and the five window-handoff methods from `App.xaml.cs`.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Remove the three window-closed handlers and the splash/login exit safety nets from `App.xaml.cs`.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete `Services/AppLifecycleService.cs` and retire the `IAppLifecycleService` seam.** (Ref: S3.1, S4) | **Persona: Backend Engineer**
- [ ] **Service Layer: Remove every startup registration from `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, including the hosted log service.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Remove the startup module DI call from `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`.** (Ref: S3.1) *Depends on: the DI-registration task above* | **Persona: Backend Engineer**

Next task: **Service Layer: Remove the splash and sign-in window statics and the five window-handoff methods from `App.xaml.cs`.** | **Persona: Backend Engineer**

### Subphase 2.2: Core contracts and the activation surface
- [ ] **Service Layer: Delete `MTM_Waitlist.Core/Services/ActivationService.cs` and the whole `MTM_Waitlist.Core/Activation/` folder.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete every `IStartup*` contract, `IComputerGateService`, `IComputerRegistryService`, `ISignOutService`, `SignOutResult` and `IAppProcessRestarter` from `MTM_Waitlist.Core/Contracts`.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete `StartupResult`, `StartupSessionSnapshot`, `StartupCredentialCheckResult` and `StartupPasswordResetRequirement` from `MTM_Waitlist.Core/Models`, leaving `StartupState` standing until Phase 4.** (Ref: S3.1, S7) | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete the four `Startup*Options` models and their `appsettings.json` sections.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **Configuration: Remove the startup sections from `appsettings.json` and confirm no other section is read at launch.** (Ref: S3.1, S11.4) *Depends on: the options-model task above* | **Persona: Backend Engineer**

Next task: **Service Layer: Delete `MTM_Waitlist.Core/Services/ActivationService.cs` and the whole `MTM_Waitlist.Core/Activation/` folder.** | **Persona: Backend Engineer**

### Subphase 2.3: The module and its views
- [ ] **Service Layer: Delete the `MTM_Waitlist.Startup` project directory.** (Ref: S3.1) | **Persona: Backend Engineer**
- [ ] **CI/CD: Remove the project entry and its twelve configuration rows from `MTM_Waitlist.sln`.** (Ref: S3.1) *Depends on: the project-deletion task above* | **Persona: DevOps Engineer**
- [ ] **CI/CD: Remove the `ProjectReference` and the `Compile Remove` entry from `MTM_Waitlist.csproj`, and the `ProjectReference` from `MTM_Waitlist.Tests.csproj`.** (Ref: S3.1) *Depends on: the sln task above* | **Persona: DevOps Engineer**
- [ ] **Frontend: Delete the whole `Module_Startup/Views` folder, including the dead `SplashPage`.** (Ref: S3.1) | **Persona: Frontend Engineer**

Next task: **Service Layer: Delete the `MTM_Waitlist.Startup` project directory.** | **Persona: Backend Engineer**

### Subphase 2.4: Data
- [ ] **Database Migration: Delete the nine startup-only stored procedures listed in the brief.** (Ref: S3.3) | **Persona: Database Engineer**
- [ ] **Database Migration: Remove those procedures from `Database/StoredProcedures/AllSPs.sql` and from any validation script that names them.** (Ref: S3.3) *Depends on: the procedure-deletion task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Review `Database/Validation/startup_schema/validate.sql`, keeping only the checks that cover shared tables and removing the rest.** (Ref: S3.3) *Depends on: the procedure-deletion task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Confirm `fn_server_utc_now` is retained and that nothing in the deleted set is referenced by a surviving artifact.** (Ref: S3.3) | **Persona: Database Engineer**
- [ ] **Database Migration: Confirm no shared artifact from the brief's protected list was touched by this phase.** (Ref: S3.2) *Depends on: the four tasks above* | **Persona: Database Engineer**

Next task: **Database Migration: Delete the nine startup-only stored procedures listed in the brief.** | **Persona: Database Engineer**

### Subphase 2.5: Tests, spec registry and documents
- [ ] **Testing: Delete the startup test files and every recording fake that exists only to serve them.** (Ref: S3.1) | **Persona: QA Engineer**
- [ ] **Testing: Rewrite the `StartupDebugLog` and activation tests so they no longer require `IStartupLogService`, and record what replaces that assertion.** (Ref: S9) | **Persona: QA Engineer**
- [ ] **Documentation: Retire the `startup` capability — delete `capabilities/startup/**`, its `living-specs.yml` entry, and its row in `capabilities/DRIFT.md`.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Documentation: Delete `STARTUP-FLOW.md`, `.html` and `.pdf`.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Documentation: Rewrite the splash and sign-in steps in `.github/instructions/winui3-ui-automation.instructions.md` against the placeholder, including the now-wrong "reaches the shell with no sign-in" claim.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Documentation: Update the module list in `README.md`, the startup module mentions in `.github/instructions/test-conventions.instructions.md`, and the naming-backlog report.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Frontend: Remove the eight sign-out/sign-in resource keys and the fourteen `Startup_*` tooltip keys from `Resources.resw`, `TooltipResources.resw` and `TooltipResources.developer.resw`.** (Ref: S3.1) | **Persona: Frontend Engineer**
- [ ] **Frontend: Remove the deleted-file names from the two `TooltipBehavior.AssociatedFiles` strings in `ShellPage.xaml` and correct the mis-pathed `StartupState.cs` entry.** (Ref: S3.1) *Depends on: the resource-key task above* | **Persona: Frontend Engineer**

Next task: **Testing: Delete the startup test files and every recording fake that exists only to serve them.** | **Persona: QA Engineer**

**GATE: the solution builds, the guard passes, and no removed symbol resolves anywhere. Nothing here is "done" until that build is green.**

---

## Phase 3: Placeholder and proof of removal

### Subphase 3.1: The placeholder window
- [ ] **Frontend: Create a minimal placeholder window under `Module_Startup/Views` that states startup was removed, with one Close button.** (Ref: S4) | **Persona: Frontend Engineer**
- [ ] **Frontend: Give the window a stable automation id and a plain title so UI automation can find it without relying on a window title.** (Ref: S4) | **Persona: Frontend Engineer**
- [ ] **Service Layer: Wire the placeholder as the only window the host shows at launch, with no shell navigation and no store reads.** (Ref: S4) *Depends on: the placeholder-window task above* | **Persona: Backend Engineer**
- [ ] **Frontend: Make the Close button end the process cleanly, leaving no orphan holding store connections.** (Ref: S4) *Depends on: the placeholder wiring task above* | **Persona: Frontend Engineer**

Next task: **Frontend: Create a minimal placeholder window under `Module_Startup/Views` that states startup was removed, with one Close button.** | **Persona: Frontend Engineer**

### Subphase 3.2: Prove the removal
- [ ] **Testing: Prove by automation that no launch path reaches the shell, the sign-in form, or a store read.** (Ref: S16) | **Persona: QA Engineer**
- [ ] **Testing: Update `PageActivationAuditTests`' pinned exemption list so it matches the placeholder's page set.** (Ref: S15) | **Persona: QA Engineer**
- [ ] **Testing: Run the full suite and record the difference against the Phase 1 baseline, accounting for every deleted test.** (Ref: S16) *Depends on: the proof task above* | **Persona: QA Engineer**
- [ ] **Database Migration: Run the schema validator against a live store and confirm it passes after the removals.** (Ref: S11.3, S16) | **Persona: Database Engineer**

Next task: **Testing: Prove by automation that no launch path reaches the shell, the sign-in form, or a store read.** | **Persona: QA Engineer**

**GATE: the app launches to the placeholder, reaches the shell by no path, and the suite is green with every deleted test accounted for. Only then does the rebuild begin.**

---

## Phase 4: The two identity contracts

> Built before everything else in the rebuild, because three later phases wait on it: the local-state
> migrations need it to resolve per-person and per-machine scope keys, the logging seam needs it to name the
> signed-in person, and the pipeline is built on it. It is also the phase that deletes `StartupState`, which
> Phase 2 deliberately leaves standing so the tree keeps building (Ref: S7, S3.1).

### Subphase 4.1: Define the two contracts
- [ ] **Data Model: Define the person identity contract — read-only, with no writer but the pipeline.** (Ref: S7) | **Persona: Backend Engineer**
- [ ] **Data Model: Define the machine facts contract — hostname, MAC, the registry row and the registered verdict.** (Ref: S7) | **Persona: Backend Engineer**

Next task: **Data Model: Define the person identity contract — read-only, with no writer but the pipeline.** | **Persona: Backend Engineer**

### Subphase 4.2: Implement and register
- [ ] **Service Layer: Implement both contracts against the store and register them in the host.** (Ref: S7) *Depends on: the two contract tasks above* | **Persona: Backend Engineer**

Next task: **Service Layer: Implement both contracts against the store and register them in the host.** | **Persona: Backend Engineer**

### Subphase 4.3: Migrate the consumers and delete `StartupState`
- [ ] **Service Layer: Migrate the ~20 consumers off `StartupState`, in the order the brief lists.** (Ref: S7) *Depends on: the contract implementations above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete `StartupState` once no consumer references it.** (Ref: S3.1, S7) *Depends on: the consumer migration above* | **Persona: Backend Engineer**

Next task: **Service Layer: Migrate the ~20 consumers off `StartupState`, in the order the brief lists.** | **Persona: Backend Engineer**

### Subphase 4.4: The audit and the proof
- [ ] **Testing: Rewrite `PermissionGateSiteAuditTests` so the developer gate it asserts points at the new contract rather than the deleted `StartupState`.** (Ref: S7, S15) *Depends on: Subphase 4.3* | **Persona: QA Engineer**
- [ ] **Testing: Confirm no consumer still reads the deleted object, and that the build is clean.** (Ref: S7) *Depends on: the audit rewrite above* | **Persona: QA Engineer**

Next task: **Testing: Rewrite `PermissionGateSiteAuditTests` so the developer gate it asserts points at the new contract rather than the deleted `StartupState`.** | **Persona: QA Engineer**

**GATE: both contracts exist and are registered, every consumer reads them, `StartupState` is gone, and the audit passes.**

---

## Phase 5: Local state removal and the store foundation

> The inventory behind every task here is brief section S11.5, which names the file, the line and the method for each item.

### Subphase 5.1: Move the seven feature preferences into the store
- [ ] **Service Layer: Replace the settings read and write in `MTM_Waitlist.Core/Services/ThemeSelectorService.cs` (14, 17, 51, 63) with the scoped store setting.** (Ref: S11.5.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Replace the settings read and write in `MTM_Waitlist.Core/Services/WaitlistSortPreferenceService.cs` (18, 20, 32, 45) with the scoped store setting.** (Ref: S11.5.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Replace the settings read in `MTM_Waitlist.Core/Services/IgnoredLocationsService.cs` (27, 29, 38) with the scoped store setting, at plant scope.** (Ref: S11.5.1, S11.5.6) | **Persona: Backend Engineer**
- [ ] **Service Layer: Replace the settings read and write in `MTM_Waitlist.Core/Services/NewRequestAlertService.cs` (12, 14, 24, 30) with the scoped store setting.** (Ref: S11.5.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: Replace the settings read and write in `MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs` (22, 26, 59, 76) with the scoped store setting.** (Ref: S11.5.1) | **Persona: Backend Engineer**
- [ ] **Full Stack: Delete the duplicate `Feature.IgnoredLocations` read at `SettingsViewModel.cs:1288` and write at `:1357`, calling the service instead, and make the editor read-only for every role but IT Department and Developer.** (Ref: S11.5.1, S11.5.6) | **Persona: Full Stack Engineer**
- [ ] **Full Stack: Replace the settings read and write in `MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs` (23, 65, 162, 185) with the scoped store setting.** (Ref: S11.5.1) | **Persona: Full Stack Engineer**
- [ ] **Database Migration: Confirm `sp_config_settings_values_upsert` and the scope rank function cover the plant scope these keys need, and add the seed rows they require plus the role baseline for the ignored-locations edit key.** (Ref: S11.5.1, S11.5.6) *Depends on: the seven substitutions above* | **Persona: Database Engineer**

Next task: **Service Layer: Replace the settings read and write in `MTM_Waitlist.Core/Services/ThemeSelectorService.cs` (14, 17, 51, 63) with the scoped store setting.** | **Persona: Backend Engineer**

### Subphase 5.2: Delete the local-settings mechanism
- [ ] **Service Layer: Delete `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs` and `MTM_Waitlist.Settings/Services/LocalSettingsService.cs`, including the test-only `CorruptForTestAsync`.** (Ref: S11.5.3) *Depends on: every task in Subphase 5.1* | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete `IFileService` and `FileService` and their DI registration, since `LocalSettingsService` was their only consumer.** (Ref: S11.5.3) *Depends on: the local-settings deletion above* | **Persona: Backend Engineer**
- [ ] **Configuration: Delete `LocalSettingsOptions` and its `appsettings.json` section, and the `Configure<LocalSettingsOptions>` binding at `ServiceRegistrationExtensions.cs:268`.** (Ref: S11.5.3) *Depends on: the local-settings deletion above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete the already-dead `MTM_Waitlist.Core/Helpers/SettingsStorageExtensions.cs`.** (Ref: S11.5.3) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `RuntimeHelper.IsMSIX` call sites and `ImageCachePaths` are untouched — packaging mode and the image cache both stay.** (Ref: S11.5.3) | **Persona: Backend Engineer**
- [ ] **CI/CD: Confirm the solution builds with no reference left to the deleted settings types.** (Ref: S11.5.3) *Depends on: the four deletions above* | **Persona: DevOps Engineer**

Next task: **Service Layer: Delete `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs` and `MTM_Waitlist.Settings/Services/LocalSettingsService.cs`, including the test-only `CorruptForTestAsync`.** | **Persona: Backend Engineer**

### Subphase 5.3: Reduce `appsettings.json` to connection strings
- [ ] **Configuration: Verify what `ModuleSharedOptions` and `ModuleCoreSettingsOptions` were meant to carry, since both are bound and neither appears in `appsettings.json`.** (Ref: S11.5.4) | **Persona: Backend Engineer**
- [ ] **Configuration: Move `ImageStorageOptions.SharedFolderPath` and `KeysFolderPath` into the machine configuration captured by the new machine setup.** (Ref: S11.5.4) | **Persona: Backend Engineer**
- [ ] **Configuration: Move `DunnageImageOptions.RootFolder` into the machine configuration with the other share paths.** (Ref: S11.5.4) | **Persona: Backend Engineer**
- [ ] **Configuration: Delete the `StartupDevelopmentOptions`, `StartupLoggingOptions` and `StartupWindowOptions` sections and their `Configure<>` bindings.** (Ref: S11.5.4) | **Persona: Backend Engineer**
- [ ] **Configuration: Confirm `MockServiceClient` stays in `appsettings.json` as the reviewed deployment-setting exception, and that nothing per-person or per-machine hides in it.** (Ref: S11.5.4) | **Persona: Backend Engineer**
- [ ] **Configuration: Confirm the only sections left are connection strings and the reviewed exception.** (Ref: S11.5.4) *Depends on: the four tasks above* | **Persona: Backend Engineer**

Next task: **Configuration: Verify what `ModuleSharedOptions` and `ModuleCoreSettingsOptions` were meant to carry, since both are bound and neither appears in `appsettings.json`.** | **Persona: Backend Engineer**

### Subphase 5.4: Delete the tests that exist only for the local-file mechanism
- [ ] **Testing: Delete `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs`, which tests the deleted store and its corruption helper.** (Ref: S11.5.5) *Depends on: Subphase 5.2* | **Persona: QA Engineer**
- [ ] **Testing: Delete `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs`, which tests the targeted repair and the reset-all path.** (Ref: S11.5.5) | **Persona: QA Engineer**
- [ ] **Testing: Delete the settings-path guard, read-probe, targeted-repair, repair-failed and database-phase-retry cases from `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`.** (Ref: S11.5.5) | **Persona: QA Engineer**
- [ ] **Testing: Delete the reset-to-defaults and retry cases from `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs`, together with its two no-op settings fakes.** (Ref: S11.5.5) | **Persona: QA Engineer**
- [ ] **Testing: Replace the fake `ILocalSettingsService` in the twelve behaviour-test files listed in S11.5.5 with the store seam, keeping every existing assertion.** (Ref: S11.5.5) *Depends on: Subphase 5.1* | **Persona: QA Engineer**
- [ ] **Testing: Confirm no test in the suite still references a local settings type or a local settings key.** (Ref: S11.5.5) *Depends on: the five tasks above* | **Persona: QA Engineer**

Next task: **Testing: Delete `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs`, which tests the deleted store and its corruption helper.** | **Persona: QA Engineer**

### Subphase 5.5: `user_active_sessions`
- [ ] **Database Table: Create `user_active_sessions` — one active row per person per machine, the token held as a salted hash, with expiry, issued time and the machine key.** (Ref: S8.1, S11.1) | **Persona: Database Engineer**
- [ ] **Database Table: Ship `create.sql` and `rollback.sql` for `user_active_sessions` and register both in the table aggregate.** (Ref: S11.1) *Depends on: the table task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add the write, validate, clear and list procedures for `user_active_sessions`, with rollback and aggregate registration.** (Ref: S11.1) *Depends on: the table task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add a validation script for `user_active_sessions` and wire it into the schema validator.** (Ref: S11.1) *Depends on: the procedures above* | **Persona: Database Engineer**

Next task: **Database Table: Create `user_active_sessions` — one active row per person per machine, the token held as a salted hash, with expiry, issued time and the machine key.** | **Persona: Database Engineer**

### Subphase 5.6: Dead-weight SQL audit and deletion
- [ ] **Database Migration: Enumerate every deployed table, procedure, function and view and record which have no consumer.** (Ref: S11.3) | **Persona: Database Engineer**
- [ ] **Database Migration: Delete `auth_sessions_tokens`, superseded by `user_active_sessions`, with its aggregate entries.** (Ref: S11.3) *Depends on: the enumeration above* | **Persona: Database Engineer**
- [ ] **Database Migration: Delete the orphaned `core_buildings_catalog`, `core_buildings_history` and `sp_core_buildings_upsert`.** (Ref: S11.3) *Depends on: the enumeration above* | **Persona: Database Engineer**
- [ ] **Database Migration: Update `AllTables.sql`, `AllSPs.sql`, `AllFunct.sql`, `AllViews.sql` and `AllSeeds.sql` for every deletion.** (Ref: S11.3) *Depends on: the two deletions above* | **Persona: Database Engineer**
- [ ] **Testing: Run the schema validator against a live store and confirm it passes after the deletions.** (Ref: S11.3, S16) *Depends on: the aggregate update above* | **Persona: QA Engineer**

Next task: **Database Migration: Enumerate every deployed table, procedure, function and view and record which have no consumer.** | **Persona: Database Engineer**

**GATE: no production code references a local settings type, `appsettings.json` holds only connection strings and the reviewed exception, the store holds the session, the schema validator passes, and the suite is green with every deleted test accounted for.**


## Phase 6: Logging module and developer log panel

> `tools/migrate-logging-callsites.ps1` is written and dry-run verified (489 sites in 83 files, 1 fully
> qualified, 7 `Configure`). It is used in Subphase 6.3.
> The person contract from Phase 4 is already in place, so every entry can name the signed-in person from the
> seam rather than reserving a field for later.

### Subphase 6.1: The store log table, procedures, indexes and retention
- [ ] **Database Table: Extend the log store on `ops_startup_logs` with every field the panel filters on — severity, module, machine, user, error type, message, exception detail, correlation id and the chain hashes.** (Ref: S9, S11.2) | **Persona: Database Engineer**
- [ ] **Database Table: Ship `create.sql` and `rollback.sql` for the log store and register both in the table aggregate.** (Ref: S9) *Depends on: the schema task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add the single-entry write procedure, keeping the hash chain intact under concurrent writers.** (Ref: S9) *Depends on: the schema task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add the reader procedures for the panel's filter set — criticality, machine, user, module, error type and a time window.** (Ref: S9) *Depends on: the schema task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add the retention routine, bounded by age and by size, removing the oldest first.** (Ref: S9) *Depends on: the schema task above* | **Persona: Database Engineer**
- [ ] **Database Migration: Add the indexes the filter set needs and a validation script wired into the schema validator.** (Ref: S9) *Depends on: the reader task above* | **Persona: Database Engineer**
- [ ] **Security Review: Prove no credential, key material or remembered secret can reach the log store, and that the key file's path is the only secret-adjacent value ever written.** (Ref: S8.3, S9) | **Persona: Security Engineer**

Next task: **Database Table: Extend the log store on `ops_startup_logs` with every field the panel filters on — severity, module, machine, user, error type, message, exception detail, correlation id and the chain hashes.** | **Persona: Database Engineer**

### Subphase 6.2: The unconditional logging seam
- [ ] **Service Layer: Create the logging module and its seam, unconditional — the type it replaces is `[Conditional("DEBUG")]`, so today nothing is logged in Release.** (Ref: S9.2) | **Persona: Backend Engineer**
- [ ] **Service Layer: Capture severity, module, machine, error type and exception detail in the seam, so no call site supplies them.** (Ref: S9) *Depends on: the seam task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Queue writes so a slow or unavailable store never delays a caller, dropping the oldest when the queue is full.** (Ref: S9, S13) *Depends on: the seam task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Read the signed-in person from the Phase 4 person contract, so every entry names them.** (Ref: S9, S7) *Depends on: the seam task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Bound the shutdown flush so queued entries are written before the process ends.** (Ref: S9.2) *Depends on: the queue task above* | **Persona: Backend Engineer**

Next task: **Service Layer: Create the logging module and its seam, unconditional — the type it replaces is `[Conditional("DEBUG")]`, so today nothing is logged in Release.** | **Persona: Backend Engineer**

### Subphase 6.3: Migrate the static call sites, then delete the old type
- [ ] **Service Layer: Run `tools/migrate-logging-callsites.ps1` in dry run and reconcile its counts against 489 sites in 83 files.** (Ref: S9.3) *Depends on: Subphase 6.2* | **Persona: Backend Engineer**
- [ ] **Service Layer: Apply the migration and add the global using so every call site resolves without a per-file using edit.** (Ref: S9.3) *Depends on: the dry-run task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Fix the 7 `Configure` call sites by hand — one in the host and six in tests.** (Ref: S9.3) *Depends on: the migration task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Delete `StartupDebugLog` once the script reports zero remaining references.** (Ref: S9.4) *Depends on: the hand fixes above* | **Persona: Backend Engineer**
- [ ] **CI/CD: Build the solution and confirm no call site was left pointing at the deleted type.** (Ref: S9.4) *Depends on: the deletion above* | **Persona: DevOps Engineer**

Next task: **Service Layer: Run `tools/migrate-logging-callsites.ps1` in dry run and reconcile its counts against 489 sites in 83 files.** | **Persona: Backend Engineer**

### Subphase 6.4: A store-backed `ILogger` provider
- [ ] **Service Layer: Add a logging provider that writes `ILogger` output to the same store, so the 226 existing `ILogger` calls need no edits.** (Ref: S9.1) *Depends on: Subphase 6.1* | **Persona: Backend Engineer**
- [ ] **Configuration: Register the provider in the generic host and remove the file-based provider.** (Ref: S9.1) *Depends on: the provider task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm with a count that no `ILogger` call site had to change.** (Ref: S9.1) *Depends on: the registration task above* | **Persona: Backend Engineer**

Next task: **Service Layer: Add a logging provider that writes `ILogger` output to the same store, so the 226 existing `ILogger` calls need no edits.** | **Persona: Backend Engineer**

### Subphase 6.5: The developer log panel
- [ ] **Settings Page: Add the log panel to Settings, newest first, with the filters from the brief.** (Ref: S9) *Depends on: Subphase 6.1* | **Persona: Frontend Engineer**
- [ ] **Settings Page: Gate the panel on its own permission key, enforced in the view model rather than only hidden.** (Ref: S9, S10) *Depends on: the panel task above* | **Persona: Full Stack Engineer**
- [ ] **Settings Card: Add the entry detail view showing the full message, the exception detail and the chain link.** (Ref: S9) *Depends on: the panel task above* | **Persona: Frontend Engineer**
- [ ] **Full Stack: Bound every panel query by time window and page size, so a large store cannot freeze the screen.** (Ref: S9) *Depends on: the panel task above* | **Persona: Full Stack Engineer**

Next task: **Settings Page: Add the log panel to Settings, newest first, with the filters from the brief.** | **Persona: Frontend Engineer**

### Subphase 6.6: Remove the file-based logging path
- [ ] **Service Layer: Delete `StartupLogService`, `StartupLogForwarder` and the logging settings keys.** (Ref: S9) *Depends on: Subphases 6.3 and 6.4* | **Persona: Backend Engineer**
- [ ] **Testing: Delete `MTM_Waitlist.Tests/Services/StartupLogServiceTests.cs` with the path it tests.** (Ref: S11.5.5) *Depends on: the deletion above* | **Persona: QA Engineer**
- [ ] **Documentation: Replace the log-directory instructions in the UI-automation and diagnostics docs with the panel.** (Ref: S3.1) *Depends on: the deletion above* | **Persona: Tech Lead**

Next task: **Service Layer: Delete `StartupLogService`, `StartupLogForwarder` and the logging settings keys.** | **Persona: Backend Engineer**

**GATE: a release build writes entries to the store, the panel reads and filters them, no call site references the deleted type, and no local log file is produced.**

---

## Phase 7: New machine configuration and privileges

> The permission keys this phase adds. Each needs a catalogue row, a role baseline, a seed and a place on the
> Settings privileges page: the machine-configuration key (IT Department and Developer, S10), the plant-wide
> ignored-locations edit key (IT Department and Developer, S11.5.6), and the log-panel key (S9).
> This phase is self-contained: the setup gate authenticates the person itself, so it does not depend on the
> pipeline's identity handling.

### Subphase 7.1: The pre-sign-in machine-setup screen
- [ ] **Frontend: Build the machine-setup screen, shown when the machine is unconfigured and before any operator sign-in.** (Ref: S10) | **Persona: Frontend Engineer**
- [ ] **Service Layer: Add the gate that unlocks setup — an IT Department or Developer sign-in, authorising configuration only and never the shell.** (Ref: S10, S10.1) *Depends on: the screen task above* | **Persona: Backend Engineer**
- [ ] **Frontend: Capture the machine's display name and description, the shared picture sources and the logging destination on that screen.** (Ref: S10) *Depends on: the screen task above* | **Persona: Frontend Engineer**
- [ ] **Service Layer: Write the captured configuration to the store against the machine.** (Ref: S10) *Depends on: the capture task above* | **Persona: Backend Engineer**
- [ ] **Full Stack: Return the step whenever the machine is unconfigured, or its configuration is removed, revoked or unreadable.** (Ref: S10.1) *Depends on: the write task above* | **Persona: Full Stack Engineer**

Next task: **Frontend: Build the machine-setup screen, shown when the machine is unconfigured and before any operator sign-in.** | **Persona: Frontend Engineer**

### Subphase 7.2: Pipeline enforcement — no bypass, and every abort ends the process
- [ ] **Service Layer: Refuse to continue while the machine is unconfigured, so a dismissal route the screen misses still cannot reach the shell.** (Ref: S10.1) | **Persona: Backend Engineer**
- [ ] **Frontend: End the process on every abort route — close box, Escape, Alt+F4, a cancel control, declining the setup sign-in.** (Ref: S10.1) *Depends on: the enforcement task above* | **Persona: Frontend Engineer**
- [ ] **Frontend: State why the process is ending before it ends, so the gate is never reported as a crash.** (Ref: S10.1, S15) *Depends on: the abort task above* | **Persona: Frontend Engineer**
- [ ] **Testing: Prove no input sequence reaches the shell from an unconfigured machine.** (Ref: S10.1) *Depends on: the enforcement task above* | **Persona: QA Engineer**
- [ ] **Testing: Prove every abort sequence ends the process, and that none of them leaves a window standing.** (Ref: S10.1) *Depends on: the abort task above* | **Persona: QA Engineer**

Next task: **Service Layer: Refuse to continue while the machine is unconfigured, so a dismissal route the screen misses still cannot reach the shell.** | **Persona: Backend Engineer**

### Subphase 7.3: Permission keys and the privileges page
- [ ] **Database Table: Add the three permission keys to the catalogue.** (Ref: S9, S10, S11.5.6) | **Persona: Database Engineer**
- [ ] **Settings Page: Add all three keys to the privileges page.** (Ref: S10) *Depends on: the catalogue task above* | **Persona: Frontend Engineer**
- [ ] **Service Layer: Enforce each key where the action happens, not only where the control is drawn.** (Ref: S11.5.6) *Depends on: the catalogue task above* | **Persona: Backend Engineer**
- [ ] **Full Stack: Make the ignored-locations editor read-only for every role but IT Department and Developer.** (Ref: S11.5.6) *Depends on: the enforcement task above* | **Persona: Full Stack Engineer**

Next task: **Database Table: Add the three permission keys to the catalogue.** | **Persona: Database Engineer**

### Subphase 7.4: Seeds and role baselines
- [ ] **Database Migration: Baseline the three keys per role, including IT Department.** (Ref: S10, S11.5.6) *Depends on: Subphase 7.3* | **Persona: Database Engineer**
- [ ] **Database Migration: Extend the development seed so a fresh store grants IT Department and Developer the three keys.** (Ref: S10) *Depends on: the baseline task above* | **Persona: Database Engineer**
- [ ] **Testing: Confirm a fresh store seeds the keys, and that a role without them cannot use the features.** (Ref: S10, S11.5.6) *Depends on: the seed task above* | **Persona: QA Engineer**

Next task: **Database Migration: Baseline the three keys per role, including IT Department.** | **Persona: Database Engineer**

**GATE: an unconfigured machine stops at the setup screen, it cannot be bypassed by any input or by any code path, and the three keys are seeded and enforced.**

---

## Phase 8: The new startup pipeline

> Sign-in reads its encryption key from the shared key file at the verified UNC path in S8.3. Read only — the
> file pre-exists and is likely shared with other MTM applications, so it is never written or rotated here.

### Subphase 8.1: Steps as data
- [ ] **Data Model: Define the step model — name, description, category — so the displayed count is derived rather than hard-coded.** (Ref: S6) | **Persona: Backend Engineer**
- [ ] **Service Layer: Build the runner that announces each step before it runs and reports started, completed and failed.** (Ref: S6) *Depends on: the step model above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Make retry re-run the failed step and the steps after it — never the whole pipeline, and never step 1 for a database-only retry.** (Ref: S6) *Depends on: the runner task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Report sub-operations inside a step, naming the operation, its target and its outcome.** (Ref: S5) *Depends on: the runner task above* | **Persona: Backend Engineer**

Next task: **Data Model: Define the step model — name, description, category — so the displayed count is derived rather than hard-coded.** | **Persona: Backend Engineer**

### Subphase 8.2: The splash, the activity feed and the error toast
- [ ] **Frontend: Build the splash window with the live, append-only activity feed.** (Ref: S5) | **Persona: Frontend Engineer**
- [ ] **Frontend: Add the error toast anchored to the bottom of the splash, never covering the feed.** (Ref: S5) | **Persona: Frontend Engineer**
- [ ] **Frontend: Keep the window size configurable and sizing failures non-fatal.** (Ref: S5) | **Persona: Frontend Engineer**
- [ ] **Service Layer: Give the picture-cache step and the reachability priming each a bounded wait, so neither can hold the splash open.** (Ref: S12, S13) | **Persona: Backend Engineer**
- [ ] **Frontend: Show each diagnosis on the feed line that caused it and in the toast, never as a bare step number.** (Ref: S5, S12) | **Persona: Frontend Engineer**

Next task: **Frontend: Build the splash window with the live, append-only activity feed.** | **Persona: Frontend Engineer**

### Subphase 8.3: The launch pipeline and the host seam
- [ ] **Service Layer: Build the launch pipeline behind a single entry point that the host calls.** (Ref: S4) *Depends on: Subphase 8.1* | **Persona: Backend Engineer**
- [ ] **Service Layer: Replace the app-lifecycle seam so the host no longer owns window handoff.** (Ref: S4) *Depends on: the pipeline task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Reveal the shell only through the pipeline, with no bypass and no continue-anyway path.** (Ref: S4) *Depends on: the seam task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Keep the shell hidden — navigation hidden, window at splash size — until the pipeline routes to it.** (Ref: S12) *Depends on: the pipeline task above* | **Persona: Backend Engineer**

Next task: **Service Layer: Build the launch pipeline behind a single entry point that the host calls.** | **Persona: Backend Engineer**

### Subphase 8.4: Sign-in, the machine gate, the forced password change and remember-me
- [ ] **Frontend: Build the sign-in surface, carrying the hint that states what is missing.** (Ref: S13) *Depends on: Subphase 8.3* | **Persona: Frontend Engineer**
- [ ] **Service Layer: Implement the credential check with the five-attempt limit on temporary credentials only.** (Ref: S13) *Depends on: the sign-in task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Implement the forced password change, opened only once the temporary credential has been accepted.** (Ref: S13) *Depends on: the credential check task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Implement the machine gate with its four outcomes, and admit a machine whose hardware identity cannot be read.** (Ref: S13) *Depends on: the credential check task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Implement the session against `user_active_sessions`, judged on the store's clock.** (Ref: S8.1) *Depends on: Subphase 5.5* | **Persona: Backend Engineer**
- [ ] **Security Review: Implement and review remember-me against the store, using the shared key file at the verified UNC path, read only.** (Ref: S8.2, S8.3) *Depends on: the session task above* | **Persona: Security Engineer**
- [ ] **Service Layer: Fall back to the ordinary sign-in form when the key file cannot be read, recording the fault and storing nothing locally instead.** (Ref: S8.3) *Depends on: the remember-me task above* | **Persona: Backend Engineer**
- [ ] **Testing: Prove the sixth attempt on a temporary credential is refused even with the correct value, and stays refused after a restart.** (Ref: S13) *Depends on: the credential check task above* | **Persona: QA Engineer**

Next task: **Frontend: Build the sign-in surface, carrying the hint that states what is missing.** | **Persona: Frontend Engineer**

### Subphase 8.5: The picture-cache step and the verdict priming
- [ ] **Service Layer: Keep the picture-cache refresh in the sequence, best effort, reporting its own feed line.** (Ref: S13) *Depends on: Subphase 8.1* | **Persona: Backend Engineer**
- [ ] **Service Layer: Keep the Infor Visual verdict settled before a screen opens, and await it only on the run that opens one.** (Ref: S12, S13) *Depends on: Subphase 8.1* | **Persona: Backend Engineer**
- [ ] **Testing: Prove neither step can block the start, and that an unreachable picture source is recorded and left in place.** (Ref: S13) *Depends on: the two service tasks above* | **Persona: QA Engineer**

Next task: **Service Layer: Keep the picture-cache refresh in the sequence, best effort, reporting its own feed line.** | **Persona: Backend Engineer**

### Subphase 8.6: Blocked state, the targeted reset and silent repair
- [ ] **Frontend: Build the single blocked-state surface, with a diagnosis specific to the cause.** (Ref: S12) *Depends on: Subphase 8.2* | **Persona: Frontend Engineer**
- [ ] **Frontend: Offer Retry, Restore Defaults and Close, and hide Reset when the store is unreachable.** (Ref: S12) *Depends on: the surface task above* | **Persona: Frontend Engineer**
- [ ] **Frontend: Show the popup that lists exactly what will be reset before anything is reset.** (Ref: S12) *Depends on: the surface task above* | **Persona: Frontend Engineer**
- [ ] **Service Layer: Make Restore Defaults touch only this machine's store-side configuration rows — never people, roles or permissions.** (Ref: S12, S11.5.6) *Depends on: the popup task above* | **Persona: Backend Engineer**
- [ ] **Service Layer: Repair a fault silently wherever it can be repaired without hurting the operator.** (Ref: S12) | **Persona: Backend Engineer**
- [ ] **Service Layer: Make Retry repeat the failed step only.** (Ref: S12) *Depends on: the repair task above* | **Persona: Backend Engineer**

Next task: **Frontend: Build the single blocked-state surface, with a diagnosis specific to the cause.** | **Persona: Frontend Engineer**

**GATE: the pipeline reaches the shell, the machine gate and the setup screen in the right order, every block states its cause, and no wait on the path is unbounded.**

---

## Phase 9: Reconstruction of the acceptance surface

### Subphase 9.1: A test for every reconstructed edge case
- [ ] **Testing: Walk the 111 cases in S13 and write a test for each one that still applies.** (Ref: S13) | **Persona: QA Engineer**
- [ ] **Testing: For each case that no longer applies, record the reason in the brief rather than leaving it untested and unexplained.** (Ref: S13) | **Persona: QA Engineer**
- [ ] **Testing: Confirm the suite holds no disabled, skipped or commented-out startup test.** (Ref: S16) *Depends on: the two tasks above* | **Persona: QA Engineer**

Next task: **Testing: Walk the 111 cases in S13 and write a test for each one that still applies.** | **Persona: QA Engineer**

### Subphase 9.2: Live runs against the new pipeline
- [ ] **Testing: Run against a fresh store and prove the machine-setup gate appears and cannot be bypassed.** (Ref: S10.1, S16) | **Persona: QA Engineer**
- [ ] **Testing: Run with the store unreachable and prove the splash shows the diagnosis and nothing is persisted anywhere.** (Ref: S9, S16) | **Persona: QA Engineer**
- [ ] **Testing: Run against a slow store and a stalled share, and prove neither holds the splash past its bound.** (Ref: S12, S16) | **Persona: QA Engineer**
- [ ] **Testing: Compare the closing result with the Phase 1 baseline and account for every difference.** (Ref: S16) | **Persona: QA Engineer**

Next task: **Testing: Run against a fresh store and prove the machine-setup gate appears and cannot be bypassed.** | **Persona: QA Engineer**

**GATE: every applicable edge case has a test, the three live runs pass, and the difference against the baseline is explained line by line.**

---

## Phase 10: Embed instructions and close-out

### Subphase 10.1: The embed guide
- [ ] **Documentation: Write the `.github/instructions` file for the new module, with an `applyTo` covering it.** (Ref: S2) | **Persona: Tech Lead**
- [ ] **Documentation: Cover how to embed the module — the entry point, the host seam, the contracts it needs and the order they must be registered in.** (Ref: S4) *Depends on: the instruction file above* | **Persona: Tech Lead**
- [ ] **Documentation: Cover how to add a step, how to add a log entry and how to add a machine-configuration field.** (Ref: S6, S9, S10) *Depends on: the instruction file above* | **Persona: Tech Lead**
- [ ] **Documentation: State the invariants a later change must not break — no bypass of the machine gate, unconditional logging, and only connection strings kept local.** (Ref: S10.1, S9.2, S11.5) *Depends on: the instruction file above* | **Persona: Tech Lead**

Next task: **Documentation: Write the `.github/instructions` file for the new module, with an `applyTo` covering it.** | **Persona: Tech Lead**

### Subphase 10.2: Living-spec re-adoption
- [ ] **Documentation: Re-adopt a living spec for the new startup surface and register it.** (Ref: S16) | **Persona: Tech Lead**
- [ ] **Documentation: Retire the old capability's leftovers and confirm the registry matches the tree.** (Ref: S3.1) *Depends on: the re-adoption task above* | **Persona: Tech Lead**

Next task: **Documentation: Re-adopt a living spec for the new startup surface and register it.** | **Persona: Tech Lead**

### Subphase 10.3: Documentation and changelog
- [ ] **Documentation: Record the rebuild in the changelog in the repo's end-user format.** (Ref: S1) | **Persona: Tech Lead**
- [ ] **Documentation: Update the README module list and remove every reference to the retired splash and sign-in surfaces.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Documentation: Correct the UI-automation instructions so they describe the new gate and the store-backed log panel.** (Ref: S3.1) | **Persona: Tech Lead**
- [ ] **Testing: Run the retired-symbol audit one final time and confirm nothing came back.** (Ref: S3.1, S16) *Depends on: the three tasks above* | **Persona: QA Engineer**

Next task: **Documentation: Record the rebuild in the changelog in the repo's end-user format.** | **Persona: Tech Lead**

**GATE: the embed guide exists, the registry matches the tree, and the audit is clean. The rebuild is closed.**
