# Tasks: Startup Rebuild

**Scale note**: this is the largest change in the repository so far. It spans eight areas — the rebuilt
`MTM_Waitlist.Startup` project, the new `MTM_Waitlist.Logging` library, the `Module_Startup/Views` surface, the
app host and its DI registration, `MTM_Waitlist.Core` contracts and models, `MTM_Waitlist.Settings`, the
`Database/` tree (26 artifacts touched: two tables and ten procedures added, one table repurposed, and three
tables and ten procedures deleted — the ten being the nine startup-only ones plus `sp_core_buildings_upsert` —
with the four permission keys, the aggregates, the descriptions file, the validation scripts and the seeds
moving with them) and `MTM_Waitlist.Tests`. A reader should watch three things: **the ordering** (the old surface
is deleted and replaced by a placeholder before anything new is built), **the deliberate deferrals**
(`StartupDebugLog` to the logging phase, `StartupState` to the identity phase, and the old file-based log path
to this feature's logging phase, because deleting any of them earlier leaves the tree uncompilable), and
**the shared database artifacts** that must never be deleted by the dead-weight audit — the five tables, the eleven
procedures plus the retained `fn_server_utc_now`, the three seeds and the five aggregate and descriptions files
T024 enumerates.

Phase order follows the user-story priorities in `spec.md`. The owner's `STARTUP-REBUILD-CHECKLIST.md`
(181 tasks, 11 phases, 11 gates) is mirrored inside these phases: most tasks name their checklist reference
(`checklist 2.4a`), and the ones added after the checklist was written name the decision or the artifact they come
from instead; identifiers, file paths and procedure names are quoted as the checklist and
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
- [x] **T001** [P] Add retired-symbol patterns for the entire old startup surface to `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` (checklist 1.1a). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`
- [x] **T002** [P] Record the pre-removal build and suite result plus the skipped count, for comparison after the removal gate (checklist 1.2a). · `specs/010-startup-rebuild/baseline.md`

**⟶ Wait for Wave 1 to finish, then:**
- [x] **T003** [P] Capture the current splash and sign-in window geometry, and their automation names, for the post-removal comparison (checklist 1.2b). · `specs/010-startup-rebuild/baseline.md`
- [x] **T004** Add a self-test proving each new retired-symbol pattern bites when its symbol is reintroduced (checklist 1.1b). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [x] **T005** Assert the removed view files are gone and that none of the removed types survives **by name** — keyed on the removed type and member names and the old view file names, not on a whole namespace or an empty folder, because the rebuilt services and view models occupy the module's own `…Services` and `…ViewModels` namespaces and its views occupy `Module_Startup/Views` (S2 decisions 1 and 2) — and that no member of the removed `MTM_Waitlist.Core` startup contracts, models or options survives (checklist 1.1c, 1.1d). Assert the rebuilt view set positively instead of by absence. · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

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
`MTM_Waitlist.Logging/ExceptionDetailSerializer.cs`, `MTM_Waitlist.Logging/DiagnosticContext.cs`,
`MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`MTM_Waitlist.Core/Contracts/Services/IPersonIdentity.cs`, `MTM_Waitlist.Core/Contracts/Services/IMachineFacts.cs`,
`MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs`,
`MTM_Waitlist.Core/Helpers/StartupDebugLog.cs`, `MTM_Waitlist.Core/Models/StartupState.cs`,
the `MTM_Waitlist.Core/Activation/` folder, the contracts and models deleted by T010 to T014, and the contracts,
models and options deleted in T012, T013 and T014 · `MTM_Waitlist.Core/`, `Module_Core/Views/ShellPage.xaml`,
`Module_Core/Views/ShellPage.xaml.cs`, `ViewModels/ShellViewModel.cs`,
`MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`,
`MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/ComputerEditDialogViewModel.cs`,
`Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`,
`Strings/en-us/Resources.resw`, `Strings/en-us/TooltipResources.resw`, `Strings/en-us/TooltipResources.developer.resw`,
`Database/**`, `capabilities/startup/**`, `capabilities/startup-diagnostics/**`, `STARTUP-FLOW.md`, `STARTUP-FLOW.html`, `STARTUP-FLOW.pdf`,
`.github/instructions/test-conventions.instructions.md`,
`MTM_Waitlist.Tests/Module_Waitlist/Views/PageActivationAuditTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchStepRunnerTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchActivityFeedTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineRetryTests.cs`,
`specs/010-startup-rebuild/removal-proof.md`, `specs/010-startup-rebuild/security-review-logging.md`

**Wave 1 — remove the old host surface, independent (different files):**
- [x] **T006** [P] Remove the splash and sign-in window statics, the five window-handoff methods, the three window-closed handlers and the splash/login exit safety nets (checklist 2.1a, 2.1b). · `App.xaml.cs`
- [x] **T007** [P] Delete the app-lifecycle service and retire the `IAppLifecycleService` seam (checklist 2.1c). · `Services/AppLifecycleService.cs`, `MTM_Waitlist.Core/Contracts/Services/IAppLifecycleService.cs`
- [x] **T008** [P] Remove every startup registration, including the hosted log service (checklist 2.1d). · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 1 to finish, then:**
- [x] **T009** Remove the startup module DI call (checklist 2.1e). · `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
- [x] **T010** Delete the removed startup contracts `IActivationService` and `IComputerGateService`. Three listings stand for the same reason `IStartupLogService` stands, a deliberate deferral: `ISignOutService`, `SignOutResult` and `IAppProcessRestarter` until US3 replaces them (T185, T186), because the shell compiles against them, and `IComputerRegistryService` until the Settings module rebuilds the capability that uses it (T187), because the Settings computer screens inject it (checklist 2.2b). · `MTM_Waitlist.Core/Contracts/Services/`
- [x] **T011** Delete the activation service and the whole `MTM_Waitlist.Core/Activation/` folder (checklist 2.2a). · `MTM_Waitlist.Core/Services/ActivationService.cs`, `MTM_Waitlist.Core/Activation/`

**⟶ Wait for Wave 2 to finish, then:**
- [x] **T012** [P] Delete the `IStartup*` contracts, keeping `IStartupLogService` and `IStartupLogForwarder` standing until this feature's logging phase deletes their implementations (checklist 2.2b, deliberate deferral). · `MTM_Waitlist.Core/Contracts/Services/`
- [x] **T013** [P] Delete `StartupResult`, `StartupSessionSnapshot`, `StartupCredentialCheckResult`, `StartupPasswordResetRequirement` and `StartupRegistrationRequest`, leaving `StartupState` standing until Wave 15 deletes it (checklist 2.2c, plan D4). · `MTM_Waitlist.Core/Models/`
- [x] **T014** [P] Delete `StartupDevelopmentOptions`, `StartupWindowOptions` and their `Configure<>` bindings, and replace `StartupDatabaseOptions` with the rebuilt module's own options so the connection string is kept; `StartupLoggingOptions` stays standing until the logging phase (checklist 2.2d, deliberate deferral). · `MTM_Waitlist.Core/Models/`, `MTM_Waitlist.Startup/Options/`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 3 to finish, then:**
- [x] **T015** Empty `MTM_Waitlist.Startup` of the old startup types, keeping the project, its solution entry and its two project references so all three are re-pointed rather than re-created; `StartupLogService` and `StartupLogForwarder` stay standing until the logging phase, and `ComputerRegistryService` leaves the project under T187 rather than being emptied with it (plan D1; checklist 2.3a–2.3c). · `MTM_Waitlist.Startup/**`
- [x] **T016** [P] Delete the whole `Module_Startup/Views` folder, including the dead `SplashPage` (checklist 2.3d). · `Module_Startup/Views/`
- [x] **T017** [P] Create `MTM_Waitlist.Logging` as a project with its solution entry and its dependency-injection extension file, re-point the startup references in the app and test projects, and drop the `Compile Remove` entry (checklist 2.3b, 2.3c). · `MTM_Waitlist.sln`, `MTM_Waitlist.Logging/MTM_Waitlist.Logging.csproj`, `MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, `MTM_Waitlist.csproj`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`, `MTM_Waitlist.Startup/MTM_Waitlist.Startup.csproj`
- [x] **T018** [P] Remove the eight sign-out/sign-in resource keys and the fifteen `Startup_*` tooltip keys, and add in the same pass every new localised string the rebuilt surfaces need: the feed lines, machine setup, the blocked state and the sign-in hint (checklist 2.5g, S15). · `Strings/en-us/Resources.resw`, `Strings/en-us/TooltipResources.resw`, `Strings/en-us/TooltipResources.developer.resw`
- [ ] **T019** [P] Remove the deleted-file names from the two `TooltipBehavior.AssociatedFiles` strings and correct the mis-pathed `StartupState.cs` entry (checklist 2.5h). · `Module_Core/Views/ShellPage.xaml`

**⟶ Wait for Wave 4 to finish, then:**
- [ ] **T020** [P] Delete the nine startup-only stored procedures listed in the brief: `sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`, `sp_auth_computer_registered_get`, `sp_auth_session_expiry_get`, `sp_auth_password_reset_required_get`, `sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`, `sp_core_computers_registry_update_by_mac`, under the recorded approval the Security & Secrets constraint requires for a destructive database operation (checklist 2.4a; plan.md → Complexity Tracking). · `Database/StoredProcedures/`
- [x] **T021** [P] Rework the startup schema validation, keeping only the checks that cover shared tables (checklist 2.4c). · `Database/Validation/startup_schema/validate.sql`

**⟶ Wait for Wave 5 to finish, then:**
- [x] **T022** Confirm `fn_server_utc_now` is retained and that nothing in the deleted set is referenced by a surviving artifact (checklist 2.4d). · `Database/`
- [x] **T023** Update the procedure and function aggregates for the nine deletions, and record the nine in the retired-objects section of the descriptions file, which the ruleset makes mandatory for every SQL artifact removed (checklist 2.4b; constitution III, `Database/Database-Ruleset.md`). · `Database/StoredProcedures/AllSPs.sql`, `Database/Functions/AllFunct.sql`, `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 6 to finish, then:**
- [x] **T024** Confirm no shared artifact from the brief's protected list was touched — the five tables (`core_users_profiles`, `core_computers_registry`, `auth_roles_catalog`, `auth_roles_assignments`, `config_settings_values`), the eleven procedures and the retained `fn_server_utc_now` (twelve data operations in all), and the three seeds with the five aggregate and descriptions files. The two listings this feature deliberately narrows rather than protects — `ILocalSettingsService` and `IFileService` — are deleted with the local-state mechanism, and that narrowing is recorded in the brief's S3.2 (checklist 2.4e; brief S3.2, S11.5.1). · `Database/`, `MTM_Waitlist.Core/Contracts/Services/`

**⟶ Wait for Wave 7 to finish, then — tests, registry and documents (checklist 2.5):**
- [x] **T025** [P] Delete the startup test files and every recording fake that exists only to serve them: the computer-gate, computer-registry, startup-coordinator and startup-recovery tests, the login and splash view-model tests, and the startup service tests (checklist 2.5a; the list includes `ViewModels/LoginViewModelTests.cs`, which S11.5.5 misclassifies as kept — see `specs/010-startup-rebuild/local-state-test-migration.md` §1C). · `MTM_Waitlist.Tests/Services/ComputerGateServiceTests.cs`, `MTM_Waitlist.Tests/Services/ComputerRegistryServiceTests.cs`, `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`, `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs`, `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs`, `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/StartupArchiveCleanupTests.cs`, `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs`
- [ ] **T026** [P] Delete the two retired capability folders — `startup`, and `startup-diagnostics`, whose matched `MTM_Waitlist.Startup/Services/StartupLog*.cs` files T133 deletes; their `living-specs.yml` entries and their `capabilities/DRIFT.md` rows are retired in the close-out phase, so the registry is edited once, at re-adoption (checklist 2.5c, deliberate deferral). · `capabilities/startup/`, `capabilities/startup-diagnostics/`
- [x] **T027** [P] Delete the retired flow documents (checklist 2.5d). · `STARTUP-FLOW.md`, `STARTUP-FLOW.html`, `STARTUP-FLOW.pdf`
- [x] **T028** [P] Update the startup module mentions in the test-conventions instructions (checklist 2.5f). · `.github/instructions/test-conventions.instructions.md`

**⟶ Wait for Wave 8 to finish, then — placeholder and proof (checklist Phase 3):**
- [x] **T029** Create a minimal placeholder window under `Module_Startup/Views` stating that startup was removed, with one Close button, a stable automation id and a plain title (checklist 3.1a, 3.1b). · `Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`
- [x] **T030** Wire the placeholder as the only window the host shows at launch, with no shell navigation and no store reads (checklist 3.1c). · `App.xaml.cs`

**⟶ Wait for Wave 9 to finish, then:**
- [x] **T031** [P] Re-pin the page-activation exemption list to the placeholder's page set, which is all this phase can pin; the list is re-pinned to the rebuilt page set when the placeholder is retired (T094), because the audit fails if a page set changes without the list following it (checklist 3.2b). · `MTM_Waitlist.Tests/Module_Waitlist/Views/PageActivationAuditTests.cs`
- [x] **T032** Make the Close button end the process cleanly, leaving no orphan holding store connections (checklist 3.1d). · `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`

**⟶ Wait for Wave 10 to finish, then:**
- [x] **T033** Prove by automation that no launch path reaches the shell, the sign-in form or a store read, and record the run (checklist 3.2a). · `specs/010-startup-rebuild/removal-proof.md`

**⟶ Wait for Wave 11 to finish, then:**
- [x] **T034** Run the full suite and the schema validator against a live store, record the difference against the Phase 1 baseline, and account for every deleted test (checklist 3.2c, 3.2d). · `specs/010-startup-rebuild/removal-proof.md`

**⟶ Wait for Wave 12 to finish, then — the two identity contracts (checklist Phase 4):**
- [x] **T035** [P] Define the person identity contract: read-only, no writer but the launch pipeline, roles read from the store only (checklist 4.1a, plan D14, FR-022). · `MTM_Waitlist.Core/Contracts/Services/IPersonIdentity.cs`
- [x] **T036** [P] Define the machine facts contract: hostname, MAC, the registry row, the registered verdict and whether the hardware identity was readable (checklist 4.1b, plan D14, FR-022). · `MTM_Waitlist.Core/Contracts/Services/IMachineFacts.cs`
- [x] **T037** [P] Implement the person identity contract against the store (checklist 4.2). · `MTM_Waitlist.Startup/Services/PersonIdentityService.cs`
- [x] **T038** [P] Implement the machine facts contract against the store, reusing `ComputerRecord` (checklist 4.2). · `MTM_Waitlist.Startup/Services/MachineFactsService.cs`
- [x] **T039** Register both contracts in the host (checklist 4.2). · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 13 to finish, then:**
- [x] **T040** Migrate the roughly twenty `StartupState` consumers off it, in the order brief S7 lists — permissions, user management, waitlist attribution, tooltips and the control inspector, Settings computer screens, picture preferences and the shell user badge (checklist 4.3, plan D15). · repo-wide mechanical migration
- [x] **T041** [P] Rewrite the permission-gate audit so the developer gate it asserts points at the new contract rather than the deleted object (checklist 4.4a, plan D23). · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`

**⟶ Wait for Wave 14 to finish, then:**
- [x] **T042** Delete `StartupState` once no consumer references it (checklist 4.3, plan D15). · `MTM_Waitlist.Core/Models/StartupState.cs`
- [x] **T043** Confirm no consumer still reads the deleted object and that the build is clean (checklist 4.4b). · repo-wide build

**⟶ Wait for Wave 15 to finish, then — the store foundation (checklist 5.5, 5.6, 6.1, 7.3, 7.4):**
- [x] **T044** [P] Create `user_active_sessions` — one active row per person per machine, the token held as a salted hash, with expiry, issued time and the machine key — shipping `create.sql` and `rollback.sql` (checklist 5.5a, 5.5b, FR-010, FR-011). · `Database/Tables/34_user_active_sessions/`
- [x] **T045** [P] Add the write, validate, clear and list procedures for `user_active_sessions`, with their rollback, and a validation script wired into the schema validator (checklist 5.5c, 5.5d). · `Database/StoredProcedures/`, `Database/Validation/`
- [x] **T046** [P] Create `auth_remembered_sign_ins` — the encrypted payload, its initialisation vector and a key fingerprint, keyed by person and machine — with `create.sql`, `rollback.sql`, the read, upsert and clear procedures, and a validation script (checklist 8.4, plan D12). · `Database/Tables/35_auth_remembered_sign_ins/`, `Database/StoredProcedures/`, `Database/Validation/`
- [x] **T047** [P] Extend the log store on `ops_startup_logs` with `module`, `error_type`, `exception_detail` and `error_fingerprint`, and add the six indexes the panel filters and groups on, shipping the amended `create.sql` and the extended `rollback.sql` (checklist 6.1a, 6.1b, plan D19, FR-032, FR-033). · `Database/Tables/10_ops_startup_logs/`
- [x] **T048** [P] Add `sp_ops_startup_logs_insert` as the single writer, taking the fingerprint as a parameter and keeping the hash chain intact under concurrent writers inside one transaction (checklist 6.1c, plan D26). · `Database/StoredProcedures/`
- [x] **T049** [P] Add the log reader procedures for the panel's filter set — including grouping by `error_fingerprint` — and the retention routine bounded by age and by size, oldest first (checklist 6.1d, 6.1e, SC-014). · `Database/StoredProcedures/`
- [x] **T050** [P] Extend the log-store validation script with the four columns, the six indexes and a non-null `entry_hash` (checklist 6.1f). · `Database/Validation/`
- [x] **T051** [P] Confirm the computer scope of `config_images_locations` can express the picture-source shape the machine configuration needs, and add whatever is missing as `create.sql` plus `rollback.sql` rather than columns on the computer registry (research.md, deferred to implementation). · `Database/StoredProcedures/`, `Database/Tables/17_config_images_locations/`
- [x] **T052** [P] Add the four permission keys to the catalogue constants and registry, leaving `permission.settings.ignored_locations` exactly as it is (checklist 7.3a, plan D10, D11). · `MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs`
- [ ] **T053** [P] Baseline the four keys per role and role-explicitly: `permission.settings.machine_configuration`, `permission.settings.ignored_locations_edit` and `permission.settings.session_length` at 1 for `IT Department` and `Developer` and 0 for every other role, and `permission.settings.log_panel` at 1 for `Developer` alone, then add the plant-scope rows and the seed rows the seven preferences need, confirming the settings upsert procedure and the scope rank function cover them (checklist 7.4a, 5.1h; contract §5, plan D11). · `Database/Seeds/seed_permission_role_baselines/create.sql`, `Database/Seeds/seed_permission_role_baselines/rollback.sql`, `Database/Validation/settings_schema/validate.sql`
- [x] **T054** [P] Extend the development seed so `JKoll` and `JohnK` hold `Developer` on both seeded machines, and a fresh store grants `permission.settings.machine_configuration`, `permission.settings.ignored_locations_edit` and `permission.settings.session_length` to `IT Department` and `Developer`, and `permission.settings.log_panel` to `Developer` alone (checklist 7.4b, plan D16; contract §5, plan D11). · `Database/Seeds/seed_dev_masked_baseline/create.sql`, `Database/Seeds/seed_dev_masked_baseline/rollback.sql`
- [x] **T055** [P] Prove no credential, key material or remembered secret can reach the log store, and that the key file's path is the only secret-adjacent value ever written (checklist 6.1g). · `specs/010-startup-rebuild/security-review-logging.md`

**⟶ Wait for Wave 16 to finish, then:**
- [x] **T056** [P] Enumerate every deployed table, procedure, function and view, record which have no consumer, and obtain the recorded approval for the dead-weight deletions it enumerates — the second of the two points at which the Security & Secrets constraint requires a destructive database operation to be confirmed, the first being T020's (checklist 5.6a; plan.md → Complexity Tracking). · `specs/010-startup-rebuild/removal-proof.md`
- [x] **T057** Register tables 34 and 35 and the extended table 10 in the table aggregate, and add their descriptions and the four new `ops_startup_logs` columns to the descriptions file, which the ruleset makes mandatory for every SQL artifact added (checklist 5.5b, 6.1b; constitution III). · `Database/Tables/AllTables.sql`, `Database/Bootstrap/update_table_descriptions.sql`
- [x] **T058** Register the new session, remembered-sign-in and log procedures in the procedure aggregate (checklist 5.5c, 6.1c–6.1e). · `Database/StoredProcedures/AllSPs.sql`
- [x] **T059** Register the amended seeds in the seed aggregate (checklist 7.4a, 7.4b). · `Database/Seeds/AllSeeds.sql`

**⟶ Wait for Wave 17 to finish, then:**
- [ ] **T060** Delete `auth_sessions_tokens`, superseded by `user_active_sessions`, with its aggregate entries (checklist 5.6b). · `Database/Tables/05_auth_sessions_tokens/`, `Database/Tables/AllTables.sql`
- [x] **T061** Delete the orphaned `core_buildings_catalog`, `core_buildings_history` and `sp_core_buildings_upsert` (checklist 5.6c). · `Database/Tables/06_core_buildings_catalog/`, `Database/Tables/07_core_buildings_history/`, `Database/StoredProcedures/`
- [x] **T062** Update the table, procedure, function, view and seed aggregates for every deletion, and append the retired tables and procedures to the retired-objects sections of the descriptions file (checklist 5.6d; constitution III, `Database/Database-Ruleset.md`). · `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Functions/AllFunct.sql`, `Database/Views/AllViews.sql`, `Database/Seeds/AllSeeds.sql`, `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 18 to finish, then:**
- [x] **T063** Run the schema validator against a live store and confirm it passes after the deletions (checklist 5.6e, 7.4c). · `Database/Validation/`

**⟶ Wait for Wave 19 to finish, then — the unconditional logging seam (checklist 6.2, 6.3, 6.4):**
- [x] **T064** [P] Create the logging seam and its entry type, unconditional — no `[Conditional]`, no `#if DEBUG`, severity, module, action, outcome, machine, person, error type and exception detail captured by the seam rather than supplied by the call site — see T175 to T179 for the capture itself (checklist 6.2a, 6.2b, 6.2d, plan D6, FR-021, FR-035). · `MTM_Waitlist.Logging/ILogService.cs`, `MTM_Waitlist.Logging/LogEntry.cs`
- [x] **T065** [P] Queue the writes so a slow or unavailable store never delays a caller, dropping the oldest when the queue is full, with a bounded shutdown flush, and absorb the writer's own failures so no failure to record produces a second diagnostic — the second-diagnostic clause is this task's; T179 adds only the no-substitute-record and no-added-wait obligations on the same file (checklist 6.2c, 6.2e, plan D20, D27, FR-037). · `MTM_Waitlist.Logging/StoreLogWriter.cs`
- [x] **T066** [P] Add the logging provider that writes `ILogger` output to the same store, so all 226 tree-wide `ILogger` call sites across 26 files need no edits — 219 `_logger.` plus 7 standalone `logger.`/`Logger.` call sites, re-derived 2026-09-26. The figure's scope matters: 58 of the 219 and 6 of the 7 sit in `MTM_Waitlist.Mock.Service`, a separate host with its own file logger that this provider does not serve, so the application's own surface is 162 call sites (checklist 6.4a, plan D5). · `MTM_Waitlist.Logging/StoreLoggerProvider.cs`
- [x] **T067** [P] Delete the file-based provider registration and register the seam and the provider in the host (checklist 6.4b). · `MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 20 to finish, then:**
- [x] **T068** Run the call-site migration in dry run and reconcile its counts against 489 references in 83 files, apply it, fix the seven `Configure` call sites by hand, and add the one global using (checklist 6.3a–6.3c, plan D7, D8). · `tools/migrate-logging-callsites.ps1`

**⟶ Wait for Wave 21 to finish, then:**
- [x] **T069** Delete the retired static logging type once the script reports zero remaining references — including the two doc-comment mentions a member-call script cannot see, `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs:13` and `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs:15`, which `RetiredSymbolAuditTests` matches as raw text and would otherwise flag — and build to prove no call site was left behind (checklist 6.3d, 6.3e). · `MTM_Waitlist.Core/Helpers/StartupDebugLog.cs`, `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs`, `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs`
- [x] **T070** [P] Confirm with a count that no `ILogger` call site had to change, against the figure the logging contract records and that re-derives exactly: 226 call sites across 26 files tree-wide (219 `_logger.` plus 7 standalone `logger.`/`Logger.`), of which 162 are the application's own — the remaining 64 sit in `MTM_Waitlist.Mock.Service`, which the provider does not serve (checklist 6.4c). · repo-wide count

**⟶ Wait for Wave 22 to finish, then — steps as data, the feed and the pipeline (checklist 8.1, 8.3):**
- [x] **T071** [P] Define the step model — id, name, description, category, stated maximum wait and the best-effort marker — so the displayed count is derived rather than hard-coded (checklist 8.1a, FR-002, FR-003). · `MTM_Waitlist.Startup/Models/LaunchStep.cs`
- [x] **T072** [P] Build the append-only activity feed with a timestamp, the step, the entry kind, the text, the target and the outcome (checklist 8.1d, FR-002). · `MTM_Waitlist.Startup/Services/LaunchActivityFeed.cs`
- [x] **T073** [P] Build the runner that announces each step before it runs and reports started, completed and failed, and that enforces each step's stated maximum (checklist 8.1b, FR-002, FR-003). · `MTM_Waitlist.Startup/Services/LaunchStepRunner.cs`
- [x] **T074** [P] Build the step catalog as data, with every step's stated maximum and no unbounded step, and the two best-effort steps bounded more tightly than the thirty-second ceiling (checklist 8.1a, plan D21, FR-003, SC-002). · `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs`
- [ ] **T075** [P] Implement the machine configuration service: the configuration state with its four unconfigured reasons, the save that refuses a display name already in use, and the reset that `IMachineConfigurationService.ResetToDefaultsAsync` performs on this machine's configuration only — the mechanism, and the only code that writes these rows (plan FR-006, FR-009, FR-018; contract §1, §4). · `MTM_Waitlist.Startup/Services/MachineConfigurationService.cs`
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
- [ ] **T094** [US1] Retire the placeholder window and its code-behind once the splash is the launch surface, and re-pin the page-activation exemption list to the rebuilt page set in the same pass, which is the second re-pin T031 owed (the Phase 2 hand-off). · `Module_Startup/Views/StartupPlaceholderWindow.xaml`, `Module_Startup/Views/StartupPlaceholderWindow.xaml.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Views/PageActivationAuditTests.cs`

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
- [ ] **T123** [P] [US4] Implement the reset policy and the silent repair: decide when a reset is offered and drive it through `IMachineConfigurationService.ResetToDefaultsAsync` rather than writing configuration rows here, so the targeted reset has one implementation, and repair a fault without asking wherever it can be repaired without hurting the operator (checklist 8.6d, 8.6e, FR-018, FR-019; contract §4). · `MTM_Waitlist.Startup/Services/StartupRecoveryService.cs`
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
- [ ] **T129** [P] [US5] Test the panel: filtering by machine and by error kind together lists only matching entries, filtering by severity and machine together does the same, and every query is bounded by a time window and a page size (US5 acceptance scenario 2, SC-005). · `MTM_Waitlist.Tests/Module_Settings/ViewModels/DeveloperLogPanelViewModelTests.cs`
- [ ] **T130** [P] [US5] Test coverage: a release build records at least one entry per completed launch where it records none today, and no local log file is produced (SC-003, SC-016). · `MTM_Waitlist.Tests/Module_Startup/Services/StoreLogCoverageTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T131** [P] [US5] Build the developer log panel: newest first, the filter set the store indexes — including grouping repeated faults by their fingerprint — the entry card showing the full message, the exception detail and the chain link, and the two copy affordances, on the entry card and in the header, whose text T183's formatter produces. Every query is bounded (checklist 6.5a, 6.5c, 6.5d, SC-014, FR-038). · `Module_Settings/Views/DeveloperLogPanelView.xaml`, `Module_Settings/Views/DeveloperLogPanelView.xaml.cs`
- [ ] **T132** [P] [US5] Build the panel view model and gate it on `permission.settings.log_panel`, enforced in the view model rather than only hidden, with the two copy commands behind it and the clipboard call made through the options overload so a refused copy is reported rather than raised (checklist 6.5b, plan D11, D29). · `MTM_Waitlist.Settings/ViewModels/DeveloperLogPanelViewModel.cs`

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
`MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`,
`MTM_Waitlist.Tests/Services/ScopedPreferenceTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`.

### Tests
- [ ] **T137** [P] [US6] Test the ignored-locations editor: it is read-only for every role but `IT Department` and `Developer`, the write is refused where the action happens, and the session-length setting refuses an unauthorised change with a stated reason (checklist 7.3d, 7.3e, FR-024, FR-029, SC-012). · `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`
- [ ] **T138** [P] [US6] Test that theme, waitlist order, alerts, seen-requests and the parts-without-pictures choice are held against the person in the store and are already there on another computer (FR-023, SC-008). · `MTM_Waitlist.Tests/Services/ScopedPreferenceTests.cs`
- [ ] **T139** [P] [US6] Test the role-restriction inventory: every setting restricted to particular roles today is still restricted to the same roles, one setting at a time (FR-030, SC-012). For the ignored-locations list, SC-012 pins the read restriction — `permission.settings.ignored_locations`, whose grants do not change — and the narrowed write to `IT Department` and `Developer` through the new edit key is a named, deliberate exception (research D10). · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`

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
- [ ] **T149** [P] [US6] Inventory every setting that is role-restricted today and confirm each keeps the same restriction (checklist 7.3f, FR-030), recording for the ignored-locations list that its unchanged restriction is the read key and that the narrowed write is the deliberate exception from research D10 (SC-012). · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleRestrictionInventoryTests.cs`

**⟶ Wait for Wave 4 to finish, then — delete the local mechanism (checklist 5.2):**
- [ ] **T150** Delete the local settings service and its contract, including the test-only corruption helper (checklist 5.2a). · `MTM_Waitlist.Core/Contracts/Services/ILocalSettingsService.cs`, `MTM_Waitlist.Settings/Services/LocalSettingsService.cs`
- [ ] **T151** [P] [US6] Delete the file service and its DI registration, since the local settings service was its only consumer (checklist 5.2b). · `MTM_Waitlist.Core/Contracts/Services/IFileService.cs`
- [ ] **T152** [P] [US6] Delete the local settings options model and its configuration binding, and with it the two tests that construct the type — `LocalSettingsOptions_Defaults` and `LocalSettingsOptions_AcceptsConfiguredValues` (checklist 5.2c; `specs/010-startup-rebuild/local-state-test-migration.md` §4 item 5). · `MTM_Waitlist.Core/Models/LocalSettingsOptions.cs`, `MTM_Waitlist.Tests/Core/Models/StartupOptionsModelsTests.cs`, `MTM_Waitlist.Tests/Models/StartupModelsTests.cs`
- [ ] **T153** [P] [US6] Delete the already-dead settings storage extensions (checklist 5.2d). · `MTM_Waitlist.Core/Helpers/SettingsStorageExtensions.cs`
- [ ] **T154** [P] [US6] Confirm the packaging-mode helper call sites and the image cache paths are untouched, since packaging mode and the image cache both stay (checklist 5.2e). · `MTM_Waitlist.Shared/`

**⟶ Wait for Wave 5 to finish, then:**
- [ ] **T155** [P] [US6] Move the shared folder and keys folder paths, and the dunnage root folder, into the machine configuration captured by machine setup, and re-point the resolver that reads them (checklist 5.3b, 5.3c, FR-025). · `MTM_Waitlist.Settings/Models/ImageStorageOptions.cs`, `MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs`
- [ ] **T156** [P] [US6] Reduce `appsettings.json` to the connection strings plus the reviewed `MockServiceClient` exception, deleting the startup, local-settings and logging sections and confirming `ModuleSharedOptions` and `ModuleCoreSettingsOptions` before either is deleted (checklist 2.2e, 5.3a, 5.3d, 5.3e, 6.6c, plan D24, FR-025). · `appsettings.json`
- [ ] **T157** [P] [US6] Delete the test that exists only for the local-file mechanism, and replace the local settings double with the store seam in the files listed in `specs/010-startup-rebuild/local-state-test-migration.md` §2 — ten doubles across eleven kept files, five replaced and five deleted outright, four of them already dead — keeping every existing assertion and removing the one test method that asserts local persistence, `AddAndRemove_PersistToLocalSettings` (checklist 5.4a, 5.4e). · `MTM_Waitlist.Tests/Services/LocalSettingsServiceTests.cs`, `MTM_Waitlist.Tests/**`

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
- [ ] **T159** [P] Expand S13 from its group summary into the plain list of the cases the three read-only sweeps found, and record the twenty-three cases in `spec.md`'s Edge Cases section as part of the same catalogue, then walk that list and write a test for each case that still applies, one test per case — the four spec cases T171 to T174 already own are covered by those tasks and are not written twice (checklist 9.1a, FR-031, SC-013). The list does not exist yet, so it is recorded here before anything is walked (checklist 9.1a, FR-031, SC-013). · `STARTUP-REBUILD-BRIEF.md`, `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`
- [ ] **T160** [P] Confirm the suite holds no disabled, skipped or commented-out startup test — a skipped case is a case dropped silently, which FR-031 forbids (checklist 9.1c, brief S16, FR-031, SC-013). · `MTM_Waitlist.Tests/**`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T161** [P] Run against a fresh store and prove the machine-setup gate appears and cannot be bypassed (checklist 9.2a, SC-004). · `specs/010-startup-rebuild/acceptance.md`
- [ ] **T162** [P] Write the embed guide for the new module with an `applyTo`, covering the entry point, the host seam, the contracts it needs and their registration order, how to add a step, how to add a log entry and how to add a machine-configuration field, plus the invariants a later change must not break (checklist 10.1a–10.1d). · `.github/instructions/startup-rebuild.instructions.md`
- [ ] **T163** [P] Re-adopt a living spec for the new startup surface, retire both old capabilities' leftovers — the `startup` and `startup-diagnostics` rows in `living-specs.yml` and in `capabilities/DRIFT.md`'s two-capability table, whose matched files are gone — and confirm the registry matches the tree (checklist 10.2a, 10.2b; the deferred Phase 2 registry edit). · `capabilities/startup-launch/spec.md`, `living-specs.yml`, `capabilities/DRIFT.md`
- [ ] **T164** [P] Record the rebuild in the changelog in the repo's end-user format (checklist 10.3a). · `CHANGELOG.md`
- [ ] **T165** [P] Update the README module list and remove every reference to the retired splash and sign-in surfaces (checklist 2.5f, 10.3b). · `README.md`
- [ ] **T166** [P] Correct the UI-automation instructions so they describe the new gate and the store-backed log panel instead of the retired splash, sign-in and log-directory path (checklist 2.5e, 6.6c, 10.3c). · `.github/instructions/winui3-ui-automation.instructions.md`

**⟶ Wait for Wave 1 to finish, then — the four spec edge cases no other task covers, sequential (same file):**
- [ ] **T171** [US3] Test the case where a temporary credential is reset while the person is signing in with the old one, so an in-flight sign-in meets the reset rather than it being assumed absent (spec.md Edge Cases; T159's catalogue; FR-031, SC-013). · `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`
- [ ] **T172** [US3] Test that a credential reset issued while the person is signed in elsewhere does not end the session already running, matching the rule that no procedure clears sessions by person for a reset (spec.md Edge Cases; FR-011; contracts/sql-contracts.md §1). · `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`
- [ ] **T173** [US3] Test that an account still holding the legacy temporary value signs in and has that value recognised exactly as it is today (spec.md Edge Cases; FR-012, FR-013). · `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`
- [ ] **T174** [US3] Test that a sign-out whose restart fails still ends the session and tells the person to open the application again, rather than leaving a half-signed-out state (spec.md Edge Cases; FR-008, FR-011, SC-001). · `MTM_Waitlist.Tests/Module_Startup/EdgeCases/LaunchEdgeCaseTests.cs`

**⟶ Wait for Wave 2 to finish, then:**
- [ ] **T167** Record in S13 the reason for every case that no longer applies, beside the case itself, rather than leaving it untested and unexplained (checklist 9.1b, FR-031, SC-013). · `STARTUP-REBUILD-BRIEF.md`

**⟶ Wait for Wave 3 to finish, then:**
- [ ] **T168** Run with the store unreachable and prove the diagnosis is shown and nothing is persisted anywhere; then run against a slow store and a stalled share and prove neither holds the splash past its bound; compare the closing result with the Phase 1 baseline and account for every difference, including the exclusions recorded in T167 (checklist 9.2b–9.2d, SC-001, SC-002). · `specs/010-startup-rebuild/acceptance.md`

**⟶ Wait for Wave 4 to finish, then:**
- [ ] **T169** Run the retired-symbol audit one final time and confirm nothing came back (checklist 10.3d, FR-028, SC-009). · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 5 to finish, then:**
- [ ] **T170** Validate against the Success Criteria with the single suite run: stop any stale `MTM_Waitlist`, XAML-compiler or MSBuild process, set `MSBUILDDISABLENODEREUSE=1`, build `MTM_Waitlist.sln` for x64 Debug with `/m:1 /nodeReuse:false` at zero warnings and zero errors, then run the suite (checklist 3.2c, FR-001–FR-038, SC-001–SC-017). · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

**Checkpoint**: every applicable edge case has a test, the live runs pass, the difference against the baseline is
explained line by line, the embed guide exists, the registry matches the tree and the audit is clean. The rebuild
is closed.

---

## Phase 2 addendum: exception-detail capture

**Added 2026-09-26**, from `MTM_Waitlist_Exception_Logging_Reference.md` after it was reconciled with this
repository. **These tasks execute in Foundational W20, beside T064 and T065**; they are numbered last so that the
174 identifiers already in this file do not move. Their decisions are D25 to D28 in `research.md`; the
requirements they satisfy are FR-032 to FR-037 and SC-014 to SC-016, and the contract they implement is
`contracts/logging-contract.md` §1.1 to §1.4.

**Files**: `MTM_Waitlist.Logging/ExceptionDetailSerializer.cs`, `MTM_Waitlist.Logging/DiagnosticContext.cs`,
`MTM_Waitlist.Logging/StoreLogWriter.cs`, `MTM_Waitlist.Logging/LogEntry.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/ExceptionCaptureTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/ExceptionFingerprintTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/DiagnosticContextTests.cs`.

### Tests
- [ ] **T180** [P] Test the capture: the chain carries every exception with its depth and index, an aggregate contributes all of its independent failures, an over-long chain is truncated with its marker rather than silently, a member that throws on read is replaced instead of losing the entry, and a failing recorder raises no second diagnostic (FR-032, FR-037). · `MTM_Waitlist.Tests/Module_Startup/Services/ExceptionCaptureTests.cs`
- [ ] **T181** [P] Test the fingerprint: the same fault raised twice produces the same value, a different fault or module produces a different one, the message's variable parts are normalized out, and an entry raised without an exception carries none (FR-033, SC-014). · `MTM_Waitlist.Tests/Module_Startup/Services/ExceptionFingerprintTests.cs`
- [ ] **T182** [P] Test the gathered context: the runtime, view and store objects are built by the seam and not by the caller, a store fault carries the provider's code and a statement fingerprint containing no parameter values, and nothing on the never-write list reaches any of them (FR-034 to FR-036, SC-015, SC-016). · `MTM_Waitlist.Tests/Module_Startup/Services/DiagnosticContextTests.cs`

### Implementation

**Wave 1 — independent (different files):**
- [ ] **T175** [P] Serialize the exception chain into `exception_detail`: one node per exception carrying its level, index, type, message, stack, source, HRESULT, help link, target site and allowlisted data, with `ToString()` stored once as `full`, capped at a node boundary with an explicit truncation marker, and defensive against a member that throws on read (plan D25, FR-032). · `MTM_Waitlist.Logging/ExceptionDetailSerializer.cs`
- [ ] **T177** [P] Build the context the seam gathers: the `runtime` object, the `ui` object from whatever the caller could name, and the `database` object read from the provider's own exception, with the statement recorded as a value-free fingerprint (plan D27, FR-034, FR-035, FR-036). · `MTM_Waitlist.Logging/DiagnosticContext.cs`

**⟶ Wait for Wave 1 to finish, then (each extends a file Wave 1 created):**
- [ ] **T176** Derive `error_fingerprint` from the fault's type, module, action and a normalized message shape, and carry it on the entry so the write procedure can store it (plan D26, FR-033). · `MTM_Waitlist.Logging/ExceptionDetailSerializer.cs`
- [ ] **T178** Redact before anything is written: parameter names and their count may be recorded, values and credentials may not, and a provider message is cut at the first `=` that follows a parameter name (FR-036; `contracts/logging-contract.md` §6). · `MTM_Waitlist.Logging/DiagnosticContext.cs`

**⟶ Wait for Wave 2 and T065 to finish, then:**
- [ ] **T179** Absorb the writer's own secondary failures — the two clauses T065 does not carry: no local substitute record, and no added wait for the caller whose fault is being recorded (plan D27, FR-037). · `MTM_Waitlist.Logging/StoreLogWriter.cs`

**Checkpoint**: a diagnostic carries the fault whole, groups with its own repeats, describes the machine and the
action it happened under, and is dropped rather than recorded anywhere else when the store refuses it.

---

## US5 addendum: the panel's copy

**Added 2026-09-26** at the owner's direction, from `MTM_Waitlist_Exception_Logging_Reference.md`. **These tasks
execute in US5 (Phase 7) beside T131 and T132**; they are numbered last so that the identifiers already in this
file do not move. Their decision is D29 in `research.md`, and the requirements they satisfy are FR-038 and
SC-017.

**Files**: `MTM_Waitlist.Logging/LogExportFormatter.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/LogExportFormatterTests.cs`.

- [ ] **T183** [P] Format the panel's copy: the entries' columns in the order the panel shows them, the exception chain and payload exactly as stored, the chain link included, an entry separator, a header naming the filter the copy came from, and a stated ceiling cut with an explicit marker rather than silently — the ceiling's value is recorded in `contracts/logging-contract.md` §7 by this task, because no artifact states it yet (plan D29, FR-038). · `MTM_Waitlist.Logging/LogExportFormatter.cs`
- [ ] **T184** [P] Test the copy: it carries the chain, the runtime context, the store diagnostics and the chain link for the entries it was given, it names the filter it came from, it is cut with a marker at the ceiling, it holds nothing the store is forbidden to hold, and a refused clipboard is reported rather than raised (FR-038, SC-017). · `MTM_Waitlist.Tests/Module_Startup/Services/LogExportFormatterTests.cs`

**Checkpoint**: a developer can copy one entry, or the view they are looking at, as text and paste it to someone
who has no access to the store, and doing so writes nothing to the machine.

---

## Foundational addendum: two dependencies the removal would otherwise strand

**Added 2026-09-26**, after a verification pass found that two consumers of the startup surface had no task.
Neither can be answered inside Foundational, so both follow this file's deliberate-deferral pattern rather than
forcing the Phase 2 gate to fail: **T185, T186 and T188 execute in US3 Wave 3, beside T111**, where the session service and the
restart path they need exist, and **T187 executes in Foundational W4 beside T015**, which is the task that would
otherwise empty the project its implementation lives in. T010 and T015 therefore keep the four affected listings
standing until these tasks land. They are numbered last so that the identifiers already in this file do not move.

**Files**: `ViewModels/ShellViewModel.cs`, `Module_Core/Views/ShellPage.xaml.cs`,
`MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`, `MTM_Waitlist.Startup/Services/`,
`MTM_Waitlist.Core/Contracts/Services/IComputerRegistryService.cs`,
`MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/ComputerEditDialogViewModel.cs`,
`MTM_Waitlist.Settings/Services/ComputerRegistryService.cs`,
`MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs`, `research.md`.

- [ ] **T185** [US3] Re-point the shell's sign-out off `ISignOutService` and `SignOutResult` before US3's registration pass deletes them: `ShellViewModel`
  injects `ISignOutService` and exposes `Task<SignOutResult> SignOutAsync`, and `ShellPage.xaml.cs` uses
  `SignOutResult` and calls `MTM_Waitlist.Module_Startup.Services.SignOutService.ResolveRestartFailedMessage()`
  at line 207 — the production file outside the startup module that names `MTM_Waitlist.Module_Startup.Services`, which
  T005's assertion covers; T188 owns the test-side reference. Move it onto the rebuilt session service, keep the refusal message it produces, and re-point
  `ShellViewModelTests`, which asserts that a sign-out goes through the service rather than merely clearing the
  displayed name (FR-011, SC-001). · `ViewModels/ShellViewModel.cs`, `Module_Core/Views/ShellPage.xaml.cs`, `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`
- [ ] **T186** [US3] Provide the process-restart path `IAppProcessRestarter` carried, so a sign-out still ends the
  session and restarts, and so a restart that fails is reported rather than leaving a half-signed-out state
  (FR-011; spec.md Edge Cases; the case T174 tests). · `MTM_Waitlist.Startup/Services/`, `ViewModels/ShellViewModel.cs`
- [ ] **T187** Rebuild the computer-management capability inside `MTM_Waitlist.Settings` before T015 empties the project its implementation lives in, and delete `IComputerRegistryService` with it (research D30). The Settings "Computers" panel keeps listing, adding, editing and deactivating registry rows: its service is recreated at `MTM_Waitlist.Settings/Services/ComputerRegistryService.cs` over the rebuilt machine contracts — this machine's record comes from `IMachineFacts` (T038), and the fleet's list, upsert, update and delete keep the `sp_core_computers_registry_*` procedures behind that seam — and `ComputerManagementViewModel` and `ComputerEditDialogViewModel` stop injecting the deleted interface. `FakeComputerRegistryService` in the kept `SettingsViewModelTests.cs` is re-pointed at the new seam or deleted with the interface; `ComputerManagementViewModel`'s `StartupState` injection is T040's to remove (FR-022). · `MTM_Waitlist.Settings/Services/ComputerRegistryService.cs`, `MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`, `MTM_Waitlist.Settings/ViewModels/ComputerEditDialogViewModel.cs`, `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, `MTM_Waitlist.Core/Contracts/Services/IComputerRegistryService.cs`
- [ ] **T188** [US3] Re-point or retire `MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs`:
  line 13 imports `MTM_Waitlist.Module_Startup.Services` and line 324 constructs `StartupSessionRepository`, a type
  T015 clears whose six procedures T020 deletes, so the file strands T015's green-build checkpoint. Move its session
  case onto the rebuilt session service, or record in the file why the case is retired (FR-010, FR-011). · `MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs`

**Checkpoint**: the solution builds with the deleted contracts gone and nothing outside the rebuilt startup
surface still naming them.

---

## Dependencies & Execution Order

**Phase order.** Setup → Foundational → US1 → US2 → US3 → US4 → US5 → US6 → Polish. Nothing in Foundational may
begin before the guard bites and the baseline exists; no user-story phase may begin before Foundational's
checkpoint; Polish waits on every story.

**Waves, one line each.**

- Setup: W1 guard patterns and the baseline result → W2 window geometry and the guard self-test → W3 the guard's
  own assertions.
- Foundational: W1 host surface removed → W2 startup registrations and the removed contracts → W3 contracts and
  models → W4 the module, its views, the project files, the strings and the computer-registry relocation (T187) → W5 the startup-only procedures and the
  validation rework → W6 the retained function, the aggregates and the descriptions file → W7 the protected list confirmed → W8 test,
  capability, document and resource cleanup → W9 the placeholder → W10 the page-activation list and the clean exit
  → W11 the automation proof → W12 the suite and schema diff → W13 the two identity contracts → W14 the consumer
  migration and the audit rewrite → W15 `StartupState` deleted and confirmed → W16 the store foundation (two
  tables, the log store, the picture scope, the four keys, the seeds, the security review) → W17 the dead-weight
  enumeration and the aggregate and description registrations → W18 the three dead-weight deletions, the
  aggregates and the retired-objects sections → W19 the
  live schema validator → W20 the logging seam, its writer, its provider and its registration, with the capture
  addendum (T175–T182) executed beside T064 and T065 → W21 the 489-site
  migration → W22 the old static log type deleted → W23 the step model, the feed, the runner, the catalog, the
  machine configuration service and the pre-sign-in steps → W24 the pipeline → W25 the host seam and its
  registration → W26 the runner, feed and retry tests.
- US1: W1 the splash surface, its view model and the two best-effort steps → W2 the toast, the sizing failure path
  and the splash page → W3 the registration and the placeholder retired.
- US2: W1 the setup screen, the gate and the view model → W2 the setup step and the abort routes → W3 the pipeline
  refusal confirmed, the registration and the privileges page.
- US3: W1 the credential check, the session, the machine gate, the remembered sign-in and the sign-in surface →
  W2 the view models and the steps → W3 the key-file fallback and the registration, with the shell's sign-out
  re-point (T185–T186, T188).
- US4: W1 the remedy table, the recovery service and the blocked-state surface → W2 the view model → W3 the reset
  preview and the close route → W4 the registration.
- US5: W1 the panel and its view model, with the copy addendum (T183–T184) → W2 the deferred log path deleted →
  W3 the registration.
- US6: W1 the six scoped-preference substitutions → W2 the duplicate read removed and the editor locked → W3 the
  session-length setting → W4 the panel hosted and the restriction inventory → W5 the local mechanism deleted →
  W6 the picture paths moved, `appsettings.json` reduced and the local-file tests replaced → W7 the confirmation.
- Polish: W1 the edge-case tests, the acceptance runs, the embed guide, the living spec, the changelog, the README
  and the UI-automation instructions → W2 the four spec edge cases with no task of their own → W3 the brief's
  exclusions recorded → W4 the live runs and the baseline comparison → W5 the final audit → W6 the single
  validation run.

**Deliberate deferrals, and why.** `StartupState` is deleted in Foundational's W15 rather than in W3, because
roughly twenty consumers compile against it. `StartupLogService`, `StartupLogForwarder`, their two contracts and
`StartupLoggingOptions` are deleted in US5 rather than in Foundational's W3, because the old file-based path
compiles against them. `StartupDebugLog` is deleted in Foundational's W22, after its 489 call sites are migrated.
Deleting any of them earlier makes the removal phase's own gate unreachable. `ISignOutService`,
`SignOutResult` and `IAppProcessRestarter` are deleted in US3 rather than in Foundational's W2, because the shell
compiles against them until T185 and T186 re-point it, and `IComputerRegistryService` is rebuilt inside
`MTM_Waitlist.Settings` in Foundational's W4 rather than deleted in W2, because the Settings computer screens
inject it and would lose a live function with it (T187). The `living-specs.yml` row for the
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

## Phase 10: Convergence

Appended by `/speckit.converge` on 2026-09-27, after the Phase 2 work landed and the analysis of the same day
was run over the specification, the plan and this file. Every item below traces to the requirement, task or
constraint that produced it, and nothing above this heading was changed.

- [ ] T189 Add the retired static logger to the reintroduction guard, with its re-introduction sample, so that
  reintroducing it fails the build as the guard exists to do per T069, FR-028, SC-009 (partial). The guard is
  currently inconsistent with its own purpose: T069 deleted the type in the same change as two other logging
  types that both carry patterns, so this is an omission rather than a deliberate exemption.
- [ ] T190 Reconcile the launch-step artifacts with the shipped catalog per FR-003,
  `contracts/launch-step-contract.md` (contradicts). The catalog reports seventeen individual operations while
  `plan.md`, the step record in this file, and the step ids retried by T077, T121 and T125 still describe the
  eleven grouped steps those operations replaced.
- [ ] T191 Record the four changes that carry no task — the schema validator repair, the picture-layout seed
  retirement, the three documentation corrections, and the orphaned logging contract's deletion — either as
  designed work in `plan.md` or as tasks, so that every change in the tree traces to something per
  Constitution I (unrequested).
- [ ] T192 Add the splash-visibility requirement to `spec.md` with its acceptance criterion — every individual
  thing the launch processes is shown on the splash — because the catalog implements it as seventeen entries
  and no requirement states it, leaving the code ahead of the specification per the owner's instruction of
  2026-09-27 (missing).
- [ ] T193 Record the destructive approval for the phase 2.4 and 5.6 deletions in the plan's Complexity
  Tracking, which is where the plan states the approval is recorded before each deletion set runs, rather than
  only in the companion ledger per Constitution: Security & Secrets, `plan.md` → Complexity Tracking (partial).
- [ ] T194 Record coverage entries for FR-032 to FR-038 and for all seventeen success criteria, so the
  requirement record traces the work that already exists under the two addenda and so SC-013's demand that
  every acceptance case be covered or retired can be read from the record per SC-013 (partial).
