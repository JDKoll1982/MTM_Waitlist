# Tasks: Startup Rebuild

**Scale note**: this is the largest change in the repository so far. It spans eight areas — the rebuilt
`MTM_Waitlist.Startup` project, the new `MTM_Waitlist.Logging` library, the `Module_Startup/Views` surface, the
app host and its DI registration, `MTM_Waitlist.Core` contracts and models, `MTM_Waitlist.Settings`, the
`Database/` tree (two new tables, one repurposed table, four new permission keys, nine procedures and three
tables deleted) and `MTM_Waitlist.Tests`. A reader should watch three things: **the ordering** (the old surface
is deleted and replaced by a placeholder before anything new is built), **the deliberate deferrals**
(`StartupDebugLog` to the logging phase, `StartupState` to the identity phase, and the old file-based log path
to this feature's logging phase, because deleting any of them earlier leaves the tree uncompilable), and
**the four shared database artifacts** that must never be deleted by the dead-weight audit.

Phase order follows the user-story priorities in `spec.md`. The owner's `STARTUP-REBUILD-CHECKLIST.md`
(179 tasks, 11 phases, 11 gates) is mirrored inside these phases: each task names its checklist reference
(`checklist 2.4a`), and identifiers, file paths and procedure names are quoted as the checklist and
`contracts/` quote them. Where the two orderings cannot agree, the divergence is stated in
[Dependencies & Execution Order](#dependencies--execution-order).

**Integration points.** Four files are edited in more than one phase on purpose, because they are the seams
the feature is built through rather than per-story code: `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
(Phase 2 owns it; each startup phase appends its own registrations in phase order), `App.xaml.cs`, the `.resw`
string files, and the solution and project files. They are listed under Phase 2 only. Two repo-wide mechanical
migrations are also units of work rather than per-file ownership: brief S7's `StartupState` consumer migration
(T040) and the 489-site logging call-site migration (T063).

---

## Phase 1: Setup — the guard and the baseline

Nothing is deleted until the guard exists and the baseline is recorded. The guard is what stops the old symbols
creeping back during the rebuild.

**Wave 1 — independent (different files):**
- [ ] **T001** [P] Add retired-symbol patterns for the entire old startup surface to `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` (checklist 1.1a). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`
- [ ] **T002** [P] Record the pre-removal build and suite result plus the skipped count, for comparison after the removal gate (checklist 1.2a). · `specs/010-startup-rebuild/baseline.md`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T003** [P] Capture the current splash and sign-in window geometry, and their automation names, for the post-removal comparison (checklist 1.2b). · `specs/010-startup-rebuild/baseline.md`
- [ ] **T004** Add a self-test proving each new retired-symbol pattern bites when its symbol is reintroduced (checklist 1.1b). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T005** Assert no file remains under `Module_Startup/Views`, no type remains in namespace `MTM_Waitlist.Module_Startup`, and no member of the removed `MTM_Waitlist.Core` startup contracts, models or options survives (checklist 1.1c, 1.1d). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**Checkpoint**: the guard fails the build when a removed symbol is reintroduced, and the baseline is recorded.
No deletion before this holds.

---

## Phase 2: Foundational — removal proven, contracts, store foundation, pipeline

**Files**: `App.xaml.cs`, `Services/AppLifecycleService.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`,
`Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, `MTM_Waitlist.sln`, `MTM_Waitlist.csproj`,
`MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`, `MTM_Waitlist.Logging/MTM_Waitlist.Logging.csproj`,
`MTM_Waitlist.Startup/MTM_Waitlist.Startup.csproj`, `MTM_Waitlist.Startup/Options/`,
`MTM_Waitlist.Startup/Models/LaunchStep.cs`, `MTM_Waitlist.Startup/Services/LaunchActivityFeed.cs`,
`MTM_Waitlist.Startup/Services/LaunchStepRunner.cs`, `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs`,
`MTM_Waitlist.Startup/Services/MachineConfigurationService.cs`, `MTM_Waitlist.Startup/Services/LaunchPipeline.cs`,
`MTM_Waitlist.Startup/Services/ConfigurationStep.cs`, `MTM_Waitlist.Startup/Services/StoreReachabilityStep.cs`,
`MTM_Waitlist.Startup/Services/MachineReadinessStep.cs`,
`MTM_Waitlist.Startup/Services/PersonIdentityService.cs`, `MTM_Waitlist.Startup/Services/MachineFactsService.cs`,
`MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`MTM_Waitlist.Logging/ILogService.cs`, `MTM_Waitlist.Logging/LogEntry.cs`,
`MTM_Waitlist.Logging/StoreLogWriter.cs`, `MTM_Waitlist.Logging/StoreLoggerProvider.cs`,
`MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`MTM_Waitlist.Core/Contracts/Services/IPersonIdentity.cs`, `MTM_Waitlist.Core/Contracts/Services/IMachineFacts.cs`,
`MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs`,
`MTM_Waitlist.Core/Helpers/StartupDebugLog.cs`, `MTM_Waitlist.Core/Models/StartupState.cs`,
the `MTM_waitlist.Core` activation folder, the contracts and models deleted by T010 to T014, and the contracts,
models and options deleted in T012, T013 and T014 · `MTM_Waitlist.Core/`, `Module_Core/Views/ShellPage.xaml`,
`Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`,
`Strings/en-us/Resources.resw`, `Strings/en-us/TooltipResources.resw`, `Strings/en-us/TooltipResources.developer.resw`,
`Database/**`, `capabilities/startup/**`, `STARTUP-FLOW.md`, `STARTUP-FLOW.html`, `STARTUP-FLOW.pdf`,
`.github/instructions/test-conventions.instructions.md`,
`MTM_Waitlist.Tests/Module_Waitlist/Views/PageActivationAuditTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchStepRunnerTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchActivityFeedTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineRetryTests.cs`,
`specs/010-startup-rebuild/removal-proof.md`, `specs/010-startup-rebuild/security-review-logging.md`

**Wave 1 — remove the old host surface, independent (different files):**
- [ ] **T006** [P] Remove the splash and sign-in window statics, the five window-handoff methods, the three window-closed handlers and the splash/login exit safety nets (checklist 2.1a, 2.1b). · `App.xaml.cs`
- [ ] **T007** [P] Delete the app-lifecycle service and retire the `IAppLifecycleService` seam (checklist 2.1c). · `Services/AppLifecycleService.cs`, `MTM_Waitlist.Core/Contracts/Services/IAppLifecycleService.cs`
- [ ] **T008** [P] Remove every startup registration, including the hosted log service (checklist 2.1d). · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T009** Remove the startup module DI call (checklist 2.1e). · `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
- [ ] **T010** Delete the removed startup contracts: `IActivationService`, `IComputerGateService`, `IComputerRegistryService`, `ISignOutService`, `SignOutResult`, `IAppProcessRestarter` (checklist 2.2b). · `MTM_Waitlist.Core/Contracts/Services/`
- [ ] **T011** Delete the activation service and the whole `MTM_Waitlist.Core/Activation/` folder (checklist 2.2a). · `MTM_Waitlist.Core/Services/ActivationService.cs`, `MTM_Waitlist.Core/Activation/`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T012** [P] Delete the `IStartup*` contracts, keeping `IStartupLogService` and `IStartupLogForwarder` standing until this feature's logging phase deletes their implementations (checklist 2.2b, deliberate deferral). · `MTM_Waitlist.Core/Contracts/Services/`
- [ ] **T013** [P] Delete `StartupResult`, `StartupSessionSnapshot`, `StartupCredentialCheckResult`, `StartupPasswordResetRequirement` and `StartupRegistrationRequest`, leaving `StartupState` standing until Wave 11 deletes it (checklist 2.2c, plan D4). · `MTM_Waitlist.Core/Models/`
- [ ] **T014** [P] Delete `StartupDevelopmentOptions`, `StartupWindowOptions` and their `Configure<>` bindings, and replace `StartupDatabaseOptions` with the rebuilt module's own options so the connection string is kept; `StartupLoggingOptions` stays standing until the logging phase (checklist 2.2d, deliberate deferral). · `MTM_Waitlist.Core/Models/`, `MTM_Waitlist.Startup/Options/`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 3 to finish, then:**
- [ ] **T015** Empty `MTM_Waitlist.Startup` of the old startup types, keeping the project, its solution entry and its two project references so all three are re-pointed rather than re-created; `StartupLogService` and `StartupLogForwarder` stay standing until the logging phase (plan D1; checklist 2.3a–2.3c). · `MTM_Waitlist.Startup/**`
- [ ] **T016** [P] Delete the whole `Module_Startup/Views` folder, including the dead `SplashPage` (checklist 2.3d). · `Module_Startup/Views/`
- [ ] **T017** [P] Create `MTM_Waitlist.Logging` as a project with its solution entry, re-point the startup references in the app and test projects, and drop the `Compile Remove` entry (checklist 2.3b, 2.3c). · `MTM_Waitlist.sln`, `MTM_Waitlist.Logging/MTM_Waitlist.Logging.csproj`, `MTM_Waitlist.csproj`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`, `MTM_Waitlist.Startup/MTM_Waitlist.Startup.csproj`
- [ ] **T018** [P] Remove the eight sign-out/sign-in resource keys and the fourteen `Startup_*` tooltip keys, and add in the same pass every new localised string the rebuilt surfaces need: the feed lines, machine setup, the blocked state and the sign-in hint (checklist 2.5g, S15). · `Strings/en-us/Resources.resw`, `Strings/en-us/TooltipResources.resw`, `Strings/en-us/TooltipResources.developer.resw`
- [ ] **T019** [P] Remove the deleted-file names from the two `TooltipBehavior.AssociatedFiles` strings and correct the mis-pathed `StartupState.cs` entry (checklist 2.5h). · `Module_Core/Views/ShellPage.xaml`

**⟶ Wait for Wave 4 to finish, then:**
- [ ] **T020** [P] Delete the nine startup-only stored procedures listed in the brief: `sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`, `sp_auth_computer_registered_get`, `sp_auth_session_expiry_get`, `sp_auth_password_reset_required_get`, `sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`, `sp_core_computers_registry_update_by_mac` (checklist 2.4a). · `Database/StoredProcedures/`
- [ ] **T021** [P] Rework the startup schema validation, keeping only the checks that cover shared tables (checklist 2.4c). · `Database/Validation/startup_schema/validate.sql`

**⟶ Wait for Wave 5 to finish, then:**
- [ ] **T022** Confirm `fn_server_utc_now` is retained and that nothing in the deleted set is referenced by a surviving artifact (checklist 2.4d). · `Database/`
- [ ] **T023** Update the procedure and function aggregates for the nine deletions (checklist 2.4b). · `Database/StoredProcedures/AllSPs.sql`, `Database/AllFunct.sql`

**⟶ Wait for Wave 6 to finish, then:**
- [ ] **T024** Confirm no shared artifact from the brief's protected list was touched: twelve shared procedures, five shared tables and four shared seeds (checklist 2.4e). · `Database/`

**⟶ Wait for Wave 7 to finish, then — tests, registry and documents (checklist 2.5):**
- [ ] **T025** [P] Delete the startup test files and every recording fake that exists only to serve them: the computer-gate, computer-registry, startup-coordinator and startup-recovery tests, the login and splash view-model tests, and the startup service tests (checklist 2.5a). · `MTM_Waitlist.Tests/Services/ComputerGateServiceTests.cs`, `MTM_Waitlist.Tests/Services/ComputerRegistryServiceTests.cs`, `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`, `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs`, `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs`, `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/StartupArchiveCleanupTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs`
- [ ] **T026** [P] Delete the retired `startup` capability folder; its `living-specs.yml` entry and its `capabilities/DRIFT.md` row are retired in the close-out phase, so the registry is edited once, at re-adoption (checklist 2.5c, deliberate deferral). · `capabilities/startup/`
- [ ] **T027** [P] Delete the retired flow documents (checklist 2.5d). · `STARTUP-FLOW.md`, `STARTUP-FLOW.html`, `STARTUP-FLOW.pdf`
- [ ] **T028** [P] Update the startup module mentions in the test-conventions instructions (checklist 2.5f). · `.github/instructions/test-conventions.instructions.md`

**⟶ Wait for Wave 8 to finish, then — placeholder and proof (checklist Phase 3):**
- [ ] **T029** Create a minimal placeholder window under `Module_Startup/Views` stating that startup was removed, with one Close button, a stable automation id and a plain title (checklist 3.1a, 3.1b). · `Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`
- [ ] **T030** Wire the placeholder as the only window the host shows at launch, with no shell navigation and no store reads (checklist 3.1c). · `App.xaml.cs`

**⟶ Wait for Wave 9 to finish, then:**
- [ ] **T031** [P] Re-pin the page-activation exemption list to the placeholder's page set (checklist 3.2b). · `MTM_Waitlist.Tests/Module_Waitlist/Views/PageActivationAuditTests.cs`
- [ ] **T032** Make the Close button end the process cleanly, leaving no orphan holding store connections (checklist 3.1d). · `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`

**⟶ Wait for Wave 10 to finish, then:**
- [ ] **T033** Prove by automation that no launch path reaches the shell, the sign-in form or a store read, and record the run (checklist 3.2a). · `specs/010-startup-rebuild/removal-proof.md`

**⟶ Wait for Wave 11 to finish, then:**
- [ ] **T034** Run the full suite and the schema validator against a live store, record the difference against the Phase 1 baseline, and account for every deleted test (checklist 3.2c, 3.2d). · `specs/010-startup-rebuild/removal-proof.md`

**⟶ Wait for Wave 12 to finish, then — the two identity contracts (checklist Phase 4):**
- [ ] **T035** [P] Define the person identity contract: read-only, no writer but the launch pipeline, roles read from the store only (checklist 4.1a, plan D14). · `MTM_Waitlist.Core/Contracts/Services/IPersonIdentity.cs`
- [ ] **T036** [P] Define the machine facts contract: hostname, MAC, the registry row, the registered verdict and whether the hardware identity was readable (checklist 4.1b, plan D14). · `MTM_Waitlist.Core/Contracts/Services/IMachineFacts.cs`
- [ ] **T037** [P] Implement the person identity contract against the store (checklist 4.2). · `MTM_Waitlist.Startup/Services/PersonIdentityService.cs`
- [ ] **T038** [P] Implement the machine facts contract against the store, reusing `ComputerRecord` (checklist 4.2). · `MTM_Waitlist.Startup/Services/MachineFactsService.cs`
- [ ] **T039** Register both contracts in the host (checklist 4.2). · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 13 to finish, then:**
- [ ] **T040** Migrate the roughly twenty `StartupState` consumers off it, in the order brief S7 lists — permissions, user management, waitlist attribution, tooltips and the control inspector, Settings computer screens, picture preferences and the shell user badge (checklist 4.3, plan D15). · repo-wide mechanical migration
- [ ] **T041** [P] Rewrite the permission-gate audit so the developer gate it asserts points at the new contract rather than the deleted object (checklist 4.4a, plan D23). · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`

**⟶ Wait for Wave 14 to finish, then:**
- [ ] **T042** Delete `StartupState` once no consumer references it (checklist 4.3, plan D15). · `MTM_Waitlist.Core/Models/StartupState.cs`
- [ ] **T043** Confirm no consumer still reads the deleted object and that the build is clean (checklist 4.4b). · repo-wide build

**⟶ Wait for Wave 15 to finish, then — the store foundation (checklist 5.5, 5.6, 6.1, 7.3, 7.4):**
- [ ] **T044** [P] Create `user_active_sessions` — one active row per person per machine, the token held as a salted hash, with expiry, issued time and the machine key — shipping `create.sql` and `rollback.sql` (checklist 5.5a, 5.5b, FR-010, FR-011). · `Database/Tables/34_user_active_sessions/`
- [ ] **T045** [P] Add the write, validate, clear and list procedures for `user_active_sessions`, with their rollback, and a validation script wired into the schema validator (checklist 5.5c, 5.5d). · `Database/StoredProcedures/`, `Database/Validation/`
- [ ] **T046** [P] Create `auth_remembered_sign_ins` — the encrypted payload, its initialisation vector and a key fingerprint, keyed by person and machine — with `create.sql`, `rollback.sql`, the read, upsert and clear procedures, and a validation script (checklist 8.4, plan D12). · `Database/Tables/35_auth_remembered_sign_ins/`, `Database/StoredProcedures/`, `Database/Validation/`
- [ ] **T047** [P] Extend the log store on `ops_startup_logs` with `module`, `error_type` and `exception_detail`, and add the five indexes the panel filters on, shipping the amended `create.sql` and the extended `rollback.sql` (checklist 6.1a, 6.1b, plan D19). · `Database/Tables/10_ops_startup_logs/`
- [ ] **T048** [P] Add `sp_ops_startup_logs_insert` as the single writer, keeping the hash chain intact under concurrent writers inside one transaction (checklist 6.1c). · `Database/StoredProcedures/`
- [ ] **T049** [P] Add the log reader procedures for the panel's filter set and the retention routine bounded by age and by size, oldest first (checklist 6.1d, 6.1e). · `Database/StoredProcedures/`
- [ ] **T050** [P] Extend the log-store validation script with the three columns, the five indexes and a non-null `entry_hash` (checklist 6.1f). · `Database/Validation/`
- [ ] **T051** [P] Confirm the computer scope of `config_images_locations` can express the picture-source shape the machine configuration needs, and add whatever is missing as `create.sql` plus `rollback.sql` rather than columns on the computer registry (research.md, deferred to implementation). · `Database/StoredProcedures/`, `Database/Tables/17_config_images_locations/`
- [ ] **T052** [P] Add the four permission keys to the catalogue constants and registry, leaving `permission.settings.ignored_locations` exactly as it is (checklist 7.3a, plan D10, D11). · `MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs`
- [ ] **T053** [P] Baseline the four keys per role, including `IT Department`, and add the plant-scope rows and the seed rows the seven preferences need, confirming the settings upsert procedure and the scope rank function cover them (checklist 7.4a, 5.1h). · `Database/Seeds/seed_permission_role_baselines/create.sql`, `Database/Seeds/seed_permission_role_baselines/rollback.sql`, `Database/Validation/settings_schema/validate.sql`
- [ ] **T054** [P] Extend the development seed so `JKoll` and `JohnK` hold `Developer` on both seeded machines, and a fresh store grants `IT Department` and `Developer` the four keys (checklist 7.4b, plan D16). · `Database/Seeds/seed_dev_masked_baseline/create.sql`, `Database/Seeds/seed_dev_masked_baseline/rollback.sql`
- [ ] **T055** [P] Prove no credential, key material or remembered secret can reach the log store, and that the key file's path is the only secret-adjacent value ever written (checklist 6.1g). · `specs/010-startup-rebuild/security-review-logging.md`

**⟶ Wait for Wave 16 to finish, then:**
- [ ] **T056** [P] Enumerate every deployed table, procedure, function and view and record which have no consumer (checklist 5.6a). · `specs/010-startup-rebuild/removal-proof.md`
- [ ] **T057** Register tables 34 and 35 and the extended table 10 in the table aggregate (checklist 5.5b, 6.1b). · `Database/AllTables.sql`
- [ ] **T058** Register the new session, remembered-sign-in and log procedures in the procedure aggregate (checklist 5.5c, 6.1c–6.1e). · `Database/StoredProcedures/AllSPs.sql`
- [ ] **T059** Register the amended seeds in the seed aggregate (checklist 7.4a, 7.4b). · `Database/Seeds/AllSeeds.sql`

**⟶ Wait for Wave 17 to finish, then:**
- [ ] **T060** Delete `auth_sessions_tokens`, superseded by `user_active_sessions`, with its aggregate entries (checklist 5.6b). · `Database/Tables/05_auth_sessions_tokens/`, `Database/AllTables.sql`
- [ ] **T061** Delete the orphaned `core_buildings_catalog`, `core_buildings_history` and `sp_core_buildings_upsert` (checklist 5.6c). · `Database/Tables/06_core_buildings_catalog/`, `Database/Tables/07_core_buildings_history/`, `Database/StoredProcedures/`
- [ ] **T062** Update the table, procedure, function, view and seed aggregates for every deletion (checklist 5.6d). · `Database/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/AllFunct.sql`, `Database/AllViews.sql`, `Database/Seeds/AllSeeds.sql`

**⟶ Wait for Wave 18 to finish, then:**
- [ ] **T063** Run the schema validator against a live store and confirm it passes after the deletions (checklist 5.6e, 7.4c). · `Database/Validation/`

**⟶ Wait for Wave 19 to finish, then — the unconditional logging seam (checklist 6.2, 6.3, 6.4):**
- [ ] **T064** [P] Create the logging seam and its entry type, unconditional — no `[Conditional]`, no `#if DEBUG`, severity, module, machine, person, error type and exception detail captured by the seam rather than supplied by the call site (checklist 6.2a, 6.2b, 6.2d, plan D6, FR-021). · `MTM_Waitlist.Logging/ILogService.cs`, `MTM_Waitlist.Logging/LogEntry.cs`
- [ ] **T065** [P] Queue the writes so a slow or unavailable store never delays a caller, dropping the oldest when the queue is full, with a bounded shutdown flush (checklist 6.2c, 6.2e, plan D20). · `MTM_Waitlist.Logging/StoreLogWriter.cs`
- [ ] **T066** [P] Add the logging provider that writes `ILogger` output to the same store, so the 226 existing calls need no edits (checklist 6.4a, plan D5). · `MTM_Waitlist.Logging/StoreLoggerProvider.cs`
- [ ] **T067** [P] Delete the file-based provider registration and register the seam and the provider in the host (checklist 6.4b). · `MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 20 to finish, then:**
- [ ] **T068** Run the call-site migration in dry run and reconcile its counts against 489 references in 83 files, apply it, fix the seven `Configure` call sites by hand, and add the one global using (checklist 6.3a–6.3c, plan D7, D8). · `tools/migrate-logging-callsites.ps1`

**⟶ Wait for Wave 21 to finish, then:**
- [ ] **T069** Delete the retired static logging type once the script reports zero remaining references, and build to prove no call site was left behind (checklist 6.3d, 6.3e). · `MTM_Waitlist.Core/Helpers/StartupDebugLog.cs`
- [ ] **T070** [P] Confirm with a count that no `ILogger` call site had to change (checklist 6.4c). · repo-wide count

**⟶ Wait for Wave 22 to finish, then — steps as data, the feed and the pipeline (checklist 8.1, 8.3):**
- [ ] **T071** [P] Define the step model — id, name, description, category, stated maximum wait and the best-effort marker — so the displayed count is derived rather than hard-coded (checklist 8.1a, FR-002, FR-003). · `MTM_Waitlist.Startup/Models/LaunchStep.cs`
- [ ] **T072** [P] Build the append-only activity feed with a timestamp, the step, the entry kind, the text, the target and the outcome (checklist 8.1d, FR-002). · `MTM_Waitlist.Startup/Services/LaunchActivityFeed.cs`
- [ ] **T073** [P] Build the runner that announces each step before it runs and reports started, completed and failed, and that enforces each step's stated maximum (checklist 8.1b, FR-002, FR-003). · `MTM_Waitlist.Startup/Services/LaunchStepRunner.cs`
- [ ] **T074** [P] Build the step catalog as data, with every step's stated maximum and no unbounded step, and the two best-effort steps bounded more tightly than the thirty-second ceiling (checklist 8.1a, plan D21, FR-003, SC-002). · `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs`
- [ ] **T075** [P] Implement the machine configuration service: the configuration state with its four unconfigured reasons, the save that refuses a display name already in use, and the reset that touches this machine's configuration only (plan FR-006, FR-009, FR-018). · `MTM_Waitlist.Startup/Services/MachineConfigurationService.cs`
- [ ] **T076** [P] Build the launch steps that run before sign-in — configuration, store reachability and the machine readiness check — each announcing itself first (checklist 8.1b, FR-002). · `MTM_Waitlist.Startup/Services/ConfigurationStep.cs`, `MTM_Waitlist.Startup/Services/StoreReachabilityStep.cs`, `MTM_Waitlist.Startup/Services/MachineReadinessStep.cs`

**⟶ Wait for Wave 23 to finish, then:**
- [ ] **T077** Build the launch pipeline behind a single entry point: `RunAsync` ending at exactly one of the main screens, sign-in, machine setup, a stated stop or an ended process; `RetryFromAsync` repeating the named step and what follows it only; a re-entrant entry refused; and the pipeline, not the screen, refusing to continue while the machine is unconfigured (checklist 8.3a, plan D3, FR-001, FR-006, FR-020). · `MTM_Waitlist.Startup/Services/LaunchPipeline.cs`

**⟶ Wait for Wave 24 to finish, then:**
- [ ] **T078** Replace the app-lifecycle seam so the host owns one call and two events, states the reason before the process ends, keeps the shell hidden until the pipeline routes to it, and no longer owns window handoff (checklist 8.3b–8.3d, FR-008). · `App.xaml.cs`
- [ ] **T079** [P] Register the pipeline, the runner, the catalog and the pre-sign-in steps in the host (checklist 8.1, 8.3). · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 25 to finish, then:**
- [ ] **T080** [P] Test the runner: each step is named before it runs, a stalled step reports at its stated maximum, and a sub-operation names its operation, its target and its outcome (checklist 8.1b, FR-002, FR-003). · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchStepRunnerTests.cs`
- [ ] **T081** [P] Test the feed: append-only, timestamped, never cleared while a launch runs, and a best-effort failure still gets a line (checklist 8.2, FR-026). · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchActivityFeedTests.cs`
- [ ] **T082** [P] Test retry: it repeats the failed step and the steps after it, never the whole pipeline and never step 1 for a store-only retry (checklist 8.1c, FR-020). · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineRetryTests.cs`

**Checkpoint**: the old surface is gone with the guard passing, the app launches only to the placeholder, the
two identity contracts are the only identity source, the store holds sessions, remembered sign-ins and
diagnostics behind procedures, and the launch pipeline exists behind one host call with a stated maximum on
every step. No user-story work begins before this checkpoint.

---

## Phase 3: User Story 1 (P1) — a launch that can be watched and diagnosed

**Files**: `Module_Startup/Views/SplashWindow.xaml`, `Module_Startup/Views/SplashWindow.xaml.cs`,
`Module_Startup/Views/SplashPage.xaml`, `Module_Startup/Views/SplashPage.xaml.cs`,
`MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, `MTM_Waitlist.Startup/Services/PictureCacheStep.cs`,
`MTM_Waitlist.Startup/Services/VisualVerdictPrimingStep.cs`, `MTM_Waitlist.Tests/Module_Startup/ViewModels/SplashViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/PictureCacheStepTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs`.
Retires `Module_Startup/Views/StartupPlaceholderWindow.xaml` and `.xaml.cs` (created in Phase 2).
Registrations for this phase append to the Phase 2-owned startup DI extension.

### Tests
- [ ] **T083** [P] [US1] Test the picture refresh: it cannot stop the launch, and an unreachable source is recorded with existing copies left in place (checklist 8.5c, FR-026). · `MTM_Waitlist.Tests/Module_Startup/Services/PictureCacheStepTests.cs`
- [ ] **T084** [P] [US1] Test the verdict priming: the verdict is settled before a screen opens, neither best-effort step can hold the splash past its bound, and an awaiting run waits only on the step that opens one (checklist 8.5c, FR-003, FR-027). · `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs`
- [ ] **T085** [P] [US1] Test the splash: a line appears before the work it describes, and a stop states its cause rather than leaving a step number on screen (checklist 8.2, FR-002, FR-004). · `MTM_Waitlist.Tests/Module_Startup/ViewModels/SplashViewModelTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T086** [P] [US1] Build the splash window with the live, append-only activity feed bound to the launch activity feed, replacing the placeholder (checklist 8.2a). · `Module_Startup/Views/SplashWindow.xaml`, `Module_Startup/Views/SplashWindow.xaml.cs`
- [ ] **T087** [P] [US1] Build the splash view model: the feed lines, the derived count and the diagnosis carried on the line that caused it (checklist 8.2e, FR-002, FR-004). · `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`
- [ ] **T088** [P] [US1] Add the picture-cache step: kept in the sequence, best effort, its own feed line, bounded more tightly than the thirty-second ceiling (checklist 8.5a, plan D21, FR-026). · `MTM_Waitlist.Startup/Services/PictureCacheStep.cs`
- [ ] **T089** [P] [US1] Add the verdict-priming step: settled before a screen opens, awaited only on the run that opens one, never able to hold the launch (checklist 8.5b, FR-027). · `MTM_Waitlist.Startup/Services/VisualVerdictPrimingStep.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T090** [US1] Add the error toast anchored to the bottom of the splash, never covering the feed, and show each diagnosis on the feed line that caused it and in the toast (checklist 8.2b, 8.2e, FR-005). · `Module_Startup/Views/SplashWindow.xaml`
- [ ] **T091** [US1] Keep the launch window size configurable and its sizing failures non-fatal (checklist 8.2c). · `Module_Startup/Views/SplashWindow.xaml`, `Module_Startup/Views/SplashWindow.xaml.cs`
- [ ] **T092** [US1] Build the splash page that hosts the feed for the single-window path (plan, `SplashPage` rebuilt). · `Module_Startup/Views/SplashPage.xaml`, `Module_Startup/Views/SplashPage.xaml.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T093** [US1] Register the splash surface, its view model and the two best-effort steps in the host. · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
- [ ] **T094** [US1] Retire the placeholder window and its code-behind once the splash is the launch surface (the Phase 2 hand-off). · `Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`

**Checkpoint**: the launch window renders the feed, names each piece of work before it runs, reports every stop
with a cause specific to it in a strip at the bottom, and no wait on the launch path is unbounded; the picture
refresh and the verdict priming can neither stop nor hold the launch. The shell stays unreachable until
Phase 5 lands, which is the story's own boundary rather than a gap.

---

## Phase 4: User Story 2 (P1) — a new machine is configured deliberately and cannot be skipped

**Files**: `Module_Startup/Views/MachineSetupWindow.xaml`, `Module_Startup/Views/MachineSetupWindow.xaml.cs`,
`MTM_Waitlist.Startup/ViewModels/MachineSetupViewModel.cs`, `MTM_Waitlist.Startup/Services/MachineSetupGate.cs`,
`MTM_Waitlist.Startup/Services/MachineSetupStep.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupGateTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupNoBypassTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs`.
Registrations for this phase append to the Phase 2-owned startup DI extension.

### Tests
- [ ] **T095** [P] [US2] Test the gate: an `IT Department` or `Developer` sign-in authorises configuration only and never opens the main screens, and a refusal is stated (FR-007). · `MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupGateTests.cs`
- [ ] **T096** [P] [US2] Test the closed gate: no input sequence reaches the shell from an unconfigured machine, and every abort route ends the process with its reason stated first (checklist 7.2d, 7.2e, FR-006, FR-008, SC-004, SC-006). · `MTM_Waitlist.Tests/Module_Startup/Services/MachineSetupNoBypassTests.cs`
- [ ] **T097** [P] [US2] Test the setup screen: a display name already in use is refused and a different one is asked for, and the save writes this machine's identity and its picture sources (FR-009). · `MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T098** [P] [US2] Build the machine-setup screen, shown when the machine is unconfigured and before any operator signs in, with a stable automation id (checklist 7.1a). · `Module_Startup/Views/MachineSetupWindow.xaml`, `Module_Startup/Views/MachineSetupWindow.xaml.cs`
- [ ] **T099** [P] [US2] Build the gate that unlocks setup: an `IT Department` or `Developer` sign-in that authorises configuration only and never the shell, authenticating the person itself (checklist 7.1b, plan D11, FR-007). · `MTM_Waitlist.Startup/Services/MachineSetupGate.cs`
- [ ] **T100** [P] [US2] Build the setup view model: capture the display name, the description and the shared picture sources, and enforce the machine-configuration permission where the save happens rather than only where the control is drawn (checklist 7.1c, 7.3c, FR-007). · `MTM_Waitlist.Startup/ViewModels/MachineSetupViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T101** [US2] Add the setup step: it returns setup whenever the machine is unconfigured, or its configuration is removed, revoked or unreadable, and it writes the captured configuration to the store against the machine (checklist 7.1d, 7.1e, FR-009). · `MTM_Waitlist.Startup/Services/MachineSetupStep.cs`
- [ ] **T102** [US2] End the process on every abort route — close box, Escape, Alt+F4, a cancel control, declining the setup sign-in — stating the reason first and localised, so the gate is never reported as a crash (checklist 7.2b, 7.2c, FR-008). · `Module_Startup/Views/MachineSetupWindow.xaml`, `Module_Startup/Views/MachineSetupWindow.xaml.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T103** [US2] Confirm the pipeline, not the screen, is what refuses to continue while the machine is unconfigured, so a dismissal route the screen misses still cannot reach the shell (checklist 7.2a, FR-006). · `MTM_Waitlist.Startup/Services/LaunchPipeline.cs`
- [ ] **T104** [US2] Register the setup screen, its view model, the gate and the step in the host. · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
- [ ] **T105** [US2] Add all four permission keys to the Settings privileges page so each has a place to be granted (checklist 7.3b). · `Module_Settings/Views/PermissionsPage.xaml`, `MTM_Waitlist.Settings/ViewModels/PermissionsViewModel.cs`

**Checkpoint**: an unconfigured machine stops at setup before any operator signs in, no input or code path
reaches the shell from it, every abort route ends the process with its reason stated, and the four keys exist,
are baselined per role and are listed on the privileges page.

---

## Phase 5: User Story 3 (P2) — signing in identifies both the person and the machine

**Files**: `MTM_Waitlist.Startup/Services/CredentialCheckService.cs`,
`MTM_Waitlist.Startup/Services/LaunchSessionService.cs`, `MTM_Waitlist.Startup/Services/MachineGateService.cs`,
`MTM_Waitlist.Startup/Services/RememberedSignInService.cs`, `MTM_Waitlist.Startup/Services/SignInSteps.cs`,
`MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs`, `MTM_Waitlist.Startup/ViewModels/PasswordChangeViewModel.cs`,
`Module_Startup/Views/SignInWindow.xaml`, `Module_Startup/Views/SignInWindow.xaml.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialAttemptLimitTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchSessionServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/RememberedSignInServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs`.
Registrations for this phase append to the Phase 2-owned startup DI extension.

### Tests
- [ ] **T106** [P] [US3] Test the attempt limit: the sixth attempt on a temporary credential is refused even with the correct value, and it stays refused after a restart (checklist 8.4h, FR-012, SC-007). · `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialAttemptLimitTests.cs`
- [ ] **T107** [P] [US3] Test the session: validity is judged on the store's clock and never the workstation's, and a sign-out clears the session before the restart (FR-010, FR-011). · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchSessionServiceTests.cs`
- [ ] **T108** [P] [US3] Test the remembered sign-in: an unreadable key falls back to the ordinary form, the fault is recorded, and nothing is kept on the machine in its place (FR-014). · `MTM_Waitlist.Tests/Module_Startup/Services/RememberedSignInServiceTests.cs`
- [ ] **T109** [P] [US3] Test the sign-in surface: the hint states what is missing rather than leaving the person guessing (checklist 8.4a). · `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T110** [P] [US3] Implement the credential check with the five-attempt limit on temporary credentials only, using the store's existing attempt counter so the limit survives a restart (checklist 8.4b, FR-012). · `MTM_Waitlist.Startup/Services/CredentialCheckService.cs`
- [ ] **T111** [P] [US3] Implement the session against `user_active_sessions`, judged on the store's clock, with the expiry resolved from the session-length setting at issue time so a change takes effect at the next sign-in (checklist 8.4e, plan D17, FR-010, FR-011, SC-011). · `MTM_Waitlist.Startup/Services/LaunchSessionService.cs`
- [ ] **T112** [P] [US3] Implement the machine gate with its four outcomes, admitting a machine whose hardware identity cannot be read because a fact that could not be read is not a failed check (checklist 8.4d, FR-015). · `MTM_Waitlist.Startup/Services/MachineGateService.cs`
- [ ] **T113** [P] [US3] Implement the remembered sign-in against the store, encrypted with the shared key file read at its UNC path, read only, with a key fingerprint so a rotated key is detected (checklist 8.4f, plan D13, FR-014). · `MTM_Waitlist.Startup/Services/RememberedSignInService.cs`
- [ ] **T114** [P] [US3] Build the sign-in surface, carrying the hint that states what is missing (checklist 8.4a). · `Module_Startup/Views/SignInWindow.xaml`, `Module_Startup/Views/SignInWindow.xaml.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T115** [US3] Build the sign-in view model and the forced password change, opened only once the temporary credential has been accepted (checklist 8.4c, FR-013). · `MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs`, `MTM_Waitlist.Startup/ViewModels/PasswordChangeViewModel.cs`
- [ ] **T116** [US3] Add the sign-in steps — sign-in, the machine gate, the forced password change and the session — each announcing itself before it runs (checklist 8.4, FR-002). · `MTM_Waitlist.Startup/Services/SignInSteps.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T117** [US3] Fall back to the ordinary sign-in form when the key file cannot be read, recording the fault and storing nothing locally instead (checklist 8.4g, FR-014). · `MTM_Waitlist.Startup/Services/RememberedSignInService.cs`
- [ ] **T118** [US3] Register the sign-in services, its view models and its steps in the host. · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**Checkpoint**: signing in decides whether the store recognises the person, whether the session is still good and
whether the machine is known; the store's clock decides validity; the five-attempt limit holds across a restart;
a temporary credential forces a new password before anything else; and an unreadable key file degrades to the
ordinary form.

---

## Phase 6: User Story 4 (P2) — a stop offers only the remedies that can help

**Files**: `MTM_Waitlist.Startup/Services/BlockedStateRemedies.cs`,
`MTM_Waitlist.Startup/Services/StartupRecoveryService.cs`, `MTM_Waitlist.Startup/ViewModels/BlockedStateViewModel.cs`,
`Module_Startup/Views/BlockedStateWindow.xaml`, `Module_Startup/Views/BlockedStateWindow.xaml.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/BlockedStateRemediesTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/StartupRecoveryServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/ViewModels/BlockedStateViewModelTests.cs`.
Registrations for this phase append to the Phase 2-owned startup DI extension.

### Tests
- [ ] **T119** [P] [US4] Test the remedy table: a store outage offers no reset, and every stopping condition offers only actions that could remove its cause (FR-017, SC-010). · `MTM_Waitlist.Tests/Module_Startup/Services/BlockedStateRemediesTests.cs`
- [ ] **T120** [P] [US4] Test the reset: it previews exactly what it will reset, affects only this machine's configuration, and never touches a person, a role or a permission (FR-018). · `MTM_Waitlist.Tests/Module_Startup/Services/StartupRecoveryServiceTests.cs`
- [ ] **T121** [P] [US4] Test retry from the surface: only the failed piece and what follows it are repeated, and a silent repair produces no prompt (FR-016, FR-019, FR-020). · `MTM_Waitlist.Tests/Module_Startup/ViewModels/BlockedStateViewModelTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T122** [P] [US4] Build the cause-to-remedy table, with `CanRestoreDefaults` false wherever a reset could not remove the cause and the preview naming exactly what a reset would touch (checklist 8.6b, 8.6c, FR-017, FR-018). · `MTM_Waitlist.Startup/Services/BlockedStateRemedies.cs`
- [ ] **T123** [P] [US4] Implement the targeted reset and the silent repair: reset only this machine's store-side configuration rows, and repair a fault without asking wherever it can be repaired without hurting the operator (checklist 8.6d, 8.6e, FR-018, FR-019). · `MTM_Waitlist.Startup/Services/StartupRecoveryService.cs`
- [ ] **T124** [P] [US4] Build the single blocked-state surface with a diagnosis specific to the cause, and Hide Reset when the store is unreachable (checklist 8.6a, 8.6b, FR-004, FR-017). · `Module_Startup/Views/BlockedStateWindow.xaml`, `Module_Startup/Views/BlockedStateWindow.xaml.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T125** [US4] Build the blocked-state view model, with Retry going through the pipeline's retry-from step so only the failed piece and what follows it are repeated (checklist 8.6f, FR-016, FR-020). · `MTM_Waitlist.Startup/ViewModels/BlockedStateViewModel.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T126** [US4] Show the popup that lists exactly what will be reset before anything is reset (checklist 8.6c, FR-018). · `Module_Startup/Views/BlockedStateWindow.xaml`
- [ ] **T127** [US4] Make Close end the process with its reason stated, so the stop is never reported as a crash (FR-008). · `Module_Startup/Views/BlockedStateWindow.xaml`, `Module_Startup/Views/BlockedStateWindow.xaml.cs`

**⟶ Wait for Wave 3 to finish, then:**
- [ ] **T128** [US4] Register the blocked-state surface, its view model and the recovery service in the host. · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**Checkpoint**: a stopped launch names a cause specific to it, offers only remedies that could remove that
cause, previews a reset before it acts and confines it to this machine's configuration, and repairs a
repairable fault without asking.

---

## Phase 7: User Story 5 (P2) — support can see what happened on any machine

**Files**: `Module_Settings/Views/DeveloperLogPanelView.xaml`, `Module_Settings/Views/DeveloperLogPanelView.xaml.cs`,
`MTM_Waitlist.Settings/ViewModels/DeveloperLogPanelViewModel.cs`,
`MTM_Waitlist.Startup/Services/StartupLogService.cs`, `MTM_Waitlist.Startup/Services/StartupLogForwarder.cs`,
`MTM_Waitlist.Core/Contracts/Services/IStartupLogService.cs`, `MTM_Waitlist.Core/Contracts/Services/IStartupLogForwarder.cs`,
`MTM_Waitlist.Core/Models/StartupLoggingOptions.cs`, `MTM_Waitlist.Tests/Services/StartupLogServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/DeveloperLogPanelViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/StoreLogCoverageTests.cs`.
The panel is hosted inside Settings in Phase 8, where the Settings page is owned.

### Tests
- [ ] **T129** [P] [US5] Test the panel: filtering by machine and by error kind together lists only matching entries, and every query is bounded by a time window and a page size (US5 acceptance scenario 2, SC-005). · `MTM_Waitlist.Tests/Module_Settings/ViewModels/DeveloperLogPanelViewModelTests.cs`
- [ ] **T130** [P] [US5] Test coverage: a release build records at least one entry per completed launch where it records none today, and no local log file is produced (SC-003). · `MTM_Waitlist.Tests/Module_Startup/Services/StoreLogCoverageTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T131** [P] [US5] Build the developer log panel: newest first, the filter set the store indexes, the entry card showing the full message, the exception detail and the chain link, and every query bounded (checklist 6.5a, 6.5c, 6.5d). · `Module_Settings/Views/DeveloperLogPanelView.xaml`, `Module_Settings/Views/DeveloperLogPanelView.xaml.cs`
- [ ] **T132** [P] [US5] Build the panel view model and gate it on `permission.settings.log_panel`, enforced in the view model rather than only hidden (checklist 6.5b, plan D11). · `MTM_Waitlist.Settings/ViewModels/DeveloperLogPanelViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T133** [US5] Delete the deferred file-based logging path: the log service, the log forwarder, their two contracts and the logging options, and remove the file-based provider registration (checklist 6.6a, deliberate deferral — their implementations had to stay standing through Phase 2 for the tree to keep compiling). · `MTM_Waitlist.Startup/Services/StartupLogService.cs`, `MTM_Waitlist.Startup/Services/StartupLogForwarder.cs`, `MTM_Waitlist.Core/Contracts/Services/IStartupLogService.cs`, `MTM_Waitlist.Core/Contracts/Services/IStartupLogForwarder.cs`, `MTM_Waitlist.Core/Models/StartupLoggingOptions.cs`
- [ ] **T134** [P] [US5] Delete the test that exists only for the deleted log path (checklist 6.6b). · `MTM_Waitlist.Tests/Services/StartupLogServiceTests.cs`
- [ ] **T135** [P] [US5] Rewrite the retired static-log and activation tests so they no longer require `IStartupLogService`, recording what replaces each assertion (checklist 2.5b, deferred to here because the contracts they name survive until this phase). · `MTM_Waitlist.Tests/**`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T136** [US5] Register the panel view model in the host. · `MTM_Waitlist.Settings/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**Checkpoint**: the seam and the provider write every diagnostic to the store with its severity, module, machine,
person, error kind and message; the panel reads and filters them newest first in bounded queries; and no local
log file is produced. The panel becomes reachable inside Settings in Phase 8.

---

## Phase 8: User Story 6 (P3) — preferences follow the person, not the computer

**Files**: `MTM_Waitlist.Core/Services/ThemeSelectorService.cs`,
`MTM_Waitlist.Core/Services/WaitlistSortPreferenceService.cs`, `MTM_Waitlist.Core/Services/IgnoredLocationsService.cs`,
`MTM_Waitlist.Core/Services/NewRequestAlertService.cs`,
`MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs`,
`MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`,
`MTM_Waitlist.Settings/Services/LocalSettingsService.cs`, `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs`,
`MTM_Waitlist.Core/Models/LocalSettingsOptions.cs`, `MTM_Waitlist.Core/Contracts/Services/IFileService.cs`,
`MTM_Waitlist.Core/Helpers/SettingsStorageExtensions.cs`, `MTM_Waitlist.Settings/Models/ImageStorageOptions.cs`,
`MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs`, `appsettings.json`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`,
`MTM_Waitlist.Tests/Services/ScopedPreferenceTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`.

### Tests
- [ ] **T137** [P] [US6] Test the ignored-locations editor: it is read-only for every role but `IT Department` and `Developer`, the write is refused where the action happens, and the session-length setting refuses an unauthorised change with a stated reason (checklist 7.3d, 7.3e, FR-024, FR-029, SC-012). · `MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`
- [ ] **T138** [P] [US6] Test that theme, waitlist order, alerts and seen-requests are held against the person in the store and are already there on another computer (FR-023, SC-008). · `MTM_Waitlist.Tests/Services/ScopedPreferenceTests.cs`
- [ ] **T139** [P] [US6] Test the role-restriction inventory: every setting restricted to particular roles today is still restricted to the same roles, one setting at a time (FR-030, SC-012). · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T140** [P] [US6] Replace the settings read and write in the theme selector with the scoped store setting (checklist 5.1a, plan D9, FR-023). · `MTM_Waitlist.Core/Services/ThemeSelectorService.cs`
- [ ] **T141** [P] [US6] Replace the settings read and write in the waitlist sort preference with the scoped store setting (checklist 5.1b, FR-023). · `MTM_Waitlist.Core/Services/WaitlistSortPreferenceService.cs`
- [ ] **T142** [P] [US6] Replace the settings read in the ignored-locations service with the plant-scope store setting (checklist 5.1c, FR-024). · `MTM_Waitlist.Core/Services/IgnoredLocationsService.cs`
- [ ] **T143** [P] [US6] Replace the settings read and write in the new-request alert service with the scoped store setting (checklist 5.1d, FR-023). · `MTM_Waitlist.Core/Services/NewRequestAlertService.cs`
- [ ] **T144** [P] [US6] Replace the settings read and write in the message-seen store with the scoped store setting (checklist 5.1e, FR-023). · `MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs`
- [ ] **T145** [P] [US6] Replace the settings read and write in the dunnage image-search view model with the scoped store setting (checklist 5.1g, FR-023). · `MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T146** [US6] Delete the duplicate ignored-locations read and write in the settings view model, call the service instead, and make the editor read-only for every role but `IT Department` and `Developer`, enforcing the edit key where the write happens (checklist 5.1f, 7.3c, 7.3d, FR-024). · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T147** [US6] Add the session-length setting to the settings panel, changeable only by `IT Department` or `Developer`, enforced where the change happens (checklist 7.3e, plan D17, FR-029). · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`

**⟶ Wait for Wave 3 to finish, then:**
- [ ] **T148** [US6] Host the developer log panel from Phase 7 inside Settings (the Phase 7 hand-off). · `Module_Settings/Views/SettingsPage.xaml`, `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`
- [ ] **T149** [P] [US6] Inventory every setting that is role-restricted today and confirm each keeps the same restriction (checklist 7.3f, FR-030). · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`

**⟶ Wait for Wave 4 to finish, then — delete the local mechanism (checklist 5.2):**
- [ ] **T150** Delete the local settings service and its contract, including the test-only corruption helper (checklist 5.2a). · `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs`, `MTM_Waitlist.Settings/Services/LocalSettingsService.cs`
- [ ] **T151** [P] [US6] Delete the file service and its DI registration, since the local settings service was its only consumer (checklist 5.2b). · `MTM_Waitlist.Core/Contracts/Services/IFileService.cs`
- [ ] **T152** [P] [US6] Delete the local settings options model and its configuration binding (checklist 5.2c). · `MTM_Waitlist.Core/Models/LocalSettingsOptions.cs`
- [ ] **T153** [P] [US6] Delete the already-dead settings storage extensions (checklist 5.2d). · `MTM_Waitlist.Core/Helpers/SettingsStorageExtensions.cs`
- [ ] **T154** [P] [US6] Confirm the packaging-mode helper call sites and the image cache paths are untouched, since packaging mode and the image cache both stay (checklist 5.2e). · `MTM_Waitlist.Shared/`

**⟶ Wait for Wave 5 to finish, then:**
- [ ] **T155** [P] [US6] Move the shared folder and keys folder paths, and the dunnage root folder, into the machine configuration captured by machine setup, and re-point the resolver that reads them (checklist 5.3b, 5.3c, FR-025). · `MTM_Waitlist.Settings/Models/ImageStorageOptions.cs`, `MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs`
- [ ] **T156** [P] [US6] Reduce `appsettings.json` to the connection strings plus the reviewed `MockServiceClient` exception, deleting the startup, local-settings and logging sections and confirming `ModuleSharedOptions` and `ModuleCoreSettingsOptions` before either is deleted (checklist 2.2e, 5.3a, 5.3d, 5.3e, 6.6c, plan D24, FR-025). · `appsettings.json`
- [ ] **T157** [P] [US6] Delete the test that exists only for the local-file mechanism, and replace the fake local settings service in the twelve behaviour-test files listed in S11.5.5 with the store seam, keeping every existing assertion (checklist 5.4a, 5.4e). · `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs`, `MTM_Waitlist.Tests/**`

**⟶ Wait for Wave 6 to finish, then:**
- [ ] **T158** [US6] Confirm no test in the suite still references a local settings type or key, and that the solution builds with no reference left to a deleted settings type (checklist 5.4f, 5.2f). · `MTM_Waitlist.Tests/**`

**Checkpoint**: a person's preferences follow them to another computer, the plant-wide locations list is readable
by everyone and changeable only by `IT Department` and `Developer`, the session length is theirs to change and
nobody else's, only what is needed to reach the store stays on the machine, and the suite holds no reference to a
local settings type.

---

## Phase 9: Polish — acceptance surface, embed guide and close-out

**Files**: `MTM_Waitlist.Tests/Module_Startup/EdgeCases/**`, `specs/010-startup-rebuild/acceptance.md`,
`STARTUP-REBUILD-BRIEF.md`, `living-specs.yml`, `capabilities/DRIFT.md`, `capabilities/startup-launch/spec.md`,
`.github/instructions/startup-rebuild.instructions.md`, `.github/instructions/winui3-ui-automation.instructions.md`,
`README.md`, `CHANGELOG.md`.

### Tests
- [ ] **T159** [P] Expand S13 from its group summary into the plain list of the cases the three read-only sweeps found, then walk that list and write a test for each case that still applies, as one test per case. The list does not exist yet, so it is recorded here before anything is walked (checklist 9.1a, FR-031, SC-013). · `STARTUP-REBUILD-BRIEF.md`, `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`
- [ ] **T160** [P] Confirm the suite holds no disabled, skipped or commented-out startup test (checklist 9.1c, SC-009). · `MTM_Waitlist.Tests/**`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T161** [P] Run against a fresh store and prove the machine-setup gate appears and cannot be bypassed (checklist 9.2a, SC-004). · `specs/010-startup-rebuild/acceptance.md`
- [ ] **T162** [P] Write the embed guide for the new module with an `applyTo`, covering the entry point, the host seam, the contracts it needs and their registration order, how to add a step, how to add a log entry and how to add a machine-configuration field, plus the invariants a later change must not break (checklist 10.1a–10.1d). · `.github/instructions/startup-rebuild.instructions.md`
- [ ] **T163** [P] Re-adopt a living spec for the new startup surface, retire the old capability's leftovers and confirm the registry matches the tree (checklist 10.2a, 10.2b; the deferred Phase 2 registry edit). · `capabilities/startup-launch/spec.md`, `living-specs.yml`, `capabilities/DRIFT.md`
- [ ] **T164** [P] Record the rebuild in the changelog in the repo's end-user format (checklist 10.3a). · `CHANGELOG.md`
- [ ] **T165** [P] Update the README module list and remove every reference to the retired splash and sign-in surfaces (checklist 2.5f, 10.3b). · `README.md`
- [ ] **T166** [P] Correct the UI-automation instructions so they describe the new gate and the store-backed log panel instead of the retired splash, sign-in and log-directory path (checklist 2.5e, 6.6c, 10.3c). · `.github/instructions/winui3-ui-automation.instructions.md`

**⟶ Wait for Wave 1 to finish, then:**
- [ ] **T167** Record in S13 the reason for every case that no longer applies, beside the case itself, rather than leaving it untested and unexplained (checklist 9.1b, FR-031, SC-013). · `STARTUP-REBUILD-BRIEF.md`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T168** Run with the store unreachable and prove the diagnosis is shown and nothing is persisted anywhere; then run against a slow store and a stalled share and prove neither holds the splash past its bound; compare the closing result with the Phase 1 baseline and account for every difference, including the exclusions recorded in T167 (checklist 9.2b–9.2d, SC-001, SC-002). · `specs/010-startup-rebuild/acceptance.md`

**⟶ Wait for Wave 3 to finish, then:**
- [ ] **T169** Run the retired-symbol audit one final time and confirm nothing came back (checklist 10.3d, FR-028, SC-009). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 4 to finish, then:**
- [ ] **T170** Validate against the Success Criteria with the single suite run: stop any stale `MTM_Waitlist`, XAML-compiler or MSBuild process, set `MSBUILDDISABLENODEREUSE=1`, build `MTM_Waitlist.sln` for x64 Debug with `/m:1 /nodeReuse:false` at zero warnings and zero errors, then run the suite (checklist 3.2c, FR-001–FR-030, SC-001–SC-012). · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

**Checkpoint**: every applicable edge case has a test, the live runs pass, the difference against the baseline is
explained line by line, the embed guide exists, the registry matches the tree and the audit is clean. The rebuild
is closed.

---

## Dependencies & Execution Order

**Phase order.** Setup → Foundational → US1 → US2 → US3 → US4 → US5 → US6 → Polish. Nothing in Foundational may
begin before the guard bites and the baseline exists; no user-story phase may begin before Foundational's
checkpoint; Polish waits on every story.

**Waves, one line each.**

- Setup: W1 guard patterns and the baseline result → W2 window geometry and the guard self-test → W3 the guard's
  own assertions.
- Foundational: W1 host surface removed → W2 startup registrations and the removed contracts → W3 contracts and
  models → W4 the module, its views, the project files and the strings → W5 the startup-only procedures and the
  validation rework → W6 the retained function and the aggregates → W7 the protected list confirmed → W8 test,
  registry, document and resource cleanup → W9 the placeholder → W10 the page-activation list and the clean exit
  → W11 the automation proof → W12 the suite and schema diff → W13 the two identity contracts → W14 the consumer
  migration and the audit rewrite → W15 `StartupState` deleted and confirmed → W16 the store foundation (two
  tables, the log store, the picture scope, the four keys, the seeds, the security review) → W17 the dead-weight
  enumeration and the aggregate registrations → W18 the three dead-weight deletions and the aggregates → W19 the
  live schema validator → W20 the logging seam, its writer, its provider and its registration → W21 the 489-site
  migration → W22 the old static log type deleted → W23 the step model, the feed, the runner, the catalog, the
  machine configuration service and the pre-sign-in steps → W24 the pipeline → W25 the host seam and its
  registration → W26 the runner, feed and retry tests.
- US1: W1 the splash surface, its view model and the two best-effort steps → W2 the toast, the sizing failure path
  and the splash page → W3 the registration and the placeholder retired.
- US2: W1 the setup screen, the gate and the view model → W2 the setup step and the abort routes → W3 the pipeline
  refusal confirmed, the registration and the privileges page.
- US3: W1 the credential check, the session, the machine gate, the remembered sign-in and the sign-in surface →
  W2 the view models and the steps → W3 the key-file fallback and the registration.
- US4: W1 the remedy table, the recovery service and the blocked-state surface → W2 the view model → W3 the reset
  preview and the close route → W4 the registration.
- US5: W1 the panel and its view model → W2 the deferred log path deleted → W3 the registration.
- US6: W1 the six scoped-preference substitutions → W2 the duplicate read removed and the editor locked → W3 the
  session-length setting → W4 the panel hosted and the restriction inventory → W5 the local mechanism deleted →
  W6 the picture paths moved, `appsettings.json` reduced and the local-file tests replaced → W7 the confirmation.
- Polish: W1 the edge-case tests, the acceptance runs, the embed guide, the living spec, the changelog, the README
  and the UI-automation instructions → W2 the brief's exclusions recorded → W3 the live runs and the baseline
  comparison → W4 the final audit → W5 the single validation run.

**Deliberate deferrals, and why.** `StartupState` is deleted in Foundational's W15 rather than in W3, because
roughly twenty consumers compile against it. `StartupLogService`, `StartupLogForwarder`, their two contracts and
`StartupLoggingOptions` are deleted in US5 rather than in Foundational's W3, because the old file-based path
compiles against them. `StartupDebugLog` is deleted in Foundational's W22, after its 489 call sites are migrated.
Deleting any of them earlier makes the removal phase's own gate unreachable. The `living-specs.yml` row for the
retired `startup` capability is retired in Polish with the re-adoption, so the registry is edited once.

**Where this order diverges from the owner's checklist.** The checklist is numbered by *workstream* (removal →
placeholder → contracts → local state → logging → machine privileges → pipeline → acceptance → close-out); this
list is ordered by *user-story priority* as the specification requires, so the same work appears in a different
place rather than a different shape. Three divergences are worth naming:

1. **Local state (checklist Phase 5) is US6's work, and US6 is P3 in the spec, so it runs last of the stories
   here.** Its store-side half (the two new tables, the permission keys, the seeds, the dead-weight audit) is
   Foundational instead, because the session table blocks US3 and the keys block US2, US3, US5 and US6.
2. **Validation and the acceptance surface (checklist Phase 9) and the close-out (Phase 10) are one final Polish
   phase here.** The checklist's own gates are unchanged and each is reproduced as a Checkpoint.
3. **US1's and US3's pipeline steps (checklist Phase 8.1, 8.3) are Foundational rather than story work**, because
   the step model, the runner, the feed, the pipeline and the host seam are the infrastructure all four startup
   stories are written against. US1 therefore delivers the visible launch surface, and US3 delivers identity.

**Single-owner resolution.** Four files are seams rather than story code and are owned by Foundational, with the
later startup phases appending to them in phase order: `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`App.xaml.cs`, the three `.resw` files, and `MTM_Waitlist.sln` with the five project files. Two paths are
hand-offs between sequential phases and named as such: `Module_Startup/Views/StartupPlaceholderWindow.xaml`
(created in Foundational, retired in US1) and the Settings page and view model (owned by US6, hosting US5's
panel). Two repo-wide mechanical migrations are units of work rather than per-file ownership: T040 and T068.
