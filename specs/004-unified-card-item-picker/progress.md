# Ralph Progress Log

Feature: 004-unified-card-item-picker
Started: 2026-09-20 07:25:58

## Codebase Patterns

- **Run SQL artifacts by piping into `mysql`, not with `source`.** `mysql -e "source file.sql"` fails with
  `ERROR 1064` because `source` is a client command, not SQL. Use
  `cmd /c "<mysql.exe> -h 127.0.0.1 -u root --default-character-set=utf8mb4 < <file.sql>"`.
- **`mysqldump` output cannot be replayed verbatim.** It carries a `SET @@GLOBAL.GTID_PURGED=...` line that fails with
  `ERROR 3546` on the same server. Strip any `GTID_PURGED` line before replaying a snapshot.
- **Read affected rows from a procedure with `ROW_COUNT()`.** Call the procedure, then call `ROW_COUNT()` as the very
  next statement in the same session. This is how FR-043's conditional update is proved rather than inspected.
- **`setup_active_jobs` column is `work_order`, not `job_number`**, and the request table is
  `waitlist_requests_queue`, not `waitlist_requests`.
- **Never write SQL through a PowerShell here-string.** `$` inside `@"..."@` gets interpolated (a `$[*]` in a regex
  silently becomes `[*]`). Write a `.sql` file with the editor instead.
- **The live store already diverged from the seeds before this work.** `100-3` held a real Setup save
  (`WO-055691` / `22-77725-110-Raw`, seq 19) dated 2026-09-14, and its `Die` subordinate carried the empty-Location
  `No Die` placeholder. Re-applying the jobs seed overwrites that row; snapshot before, restore after.
- **A `with` expression must be parenthesised before a method call.** `RequestJobPartAvailability.None with { … }
  .WithDies(…)` is a syntax error; write `(RequestJobPartAvailability.None with { … }).WithDies(…)`. New list
  members on that record are **init-only properties plus a `With…` helper**, never positional parameters.
- **This machine's `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` user variable breaks a test.**
  `StartupCoordinatorTests.RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync` expects the
  coordinator to block on a malformed literal, but the environment override wins, so it fails with
  `Assert.IsTrue failed` on `result.IsBlocked`. Clear both `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` and
  `MTM_WAITLIST_DB_CONNECTION_STRING` in the test process before reading a full-suite result.
- **Compare collections with `CollectionAssert.AreEqual`, not `Assert.AreEqual`.** Two `string[]` of equal contents
  fail `Assert.AreEqual`; every sibling assertion in this suite uses `CollectionAssert`.
- **A new wizard step is registered in two places, in flow order:** `pageService.Configure<VM, Page>()` beside the
  other steps and `services.AddTransient<VM>()` + `AddTransient<Page>()` in the same order. Its view model is
  reached by `NewRequestFlowRules.GetNextStepType(state).FullName!`, so the branch must sit **before** the
  `!requiresAnswer || answerCaptured → Preview` return.
- **`PageActivationAuditTests` covers every page generically** (parameterless constructor), but its
  `s_newRequestWizardPages` list holds only the linear spine — the alternative answer steps (`NewRequestDunnagePage`,
  and now `NewRequestDiePage`) are deliberately absent. Do **not** add a `ContinueCommand`-bearing step to
  `NewRequestWizardAdvanceMarkupTests`' choice-step list, which asserts that command is absent.
- **A `.ps1` written by an agent must be pure ASCII.** Windows PowerShell 5.1 reads a BOM-less `.ps1` as ANSI, so one
  em-dash inside a string literal corrupts the terminator and the file fails to parse. Use `-` and write generated
  scripts with an ASCII-only body.
- **`$PSScriptRoot` is the folder of the script being run, not the repo.** A helper generated into `%TEMP%` cannot
  derive the repo root from itself — hard-code the root or pass it in.
- **`Set-Content -Encoding UTF8` writes a BOM in PS 5.1.** For a clean UTF-8 file use
  `[System.IO.File]::WriteAllLines($path, $lines, (New-Object System.Text.UTF8Encoding($false)))`.
- **`$Error` is a read-only PowerShell automatic variable.** `foreach ($error in $errors)` aborts the loop with
  *"Cannot overwrite variable Error"* before printing anything, which turns a working guard into a guard that fails
  loudly with the wrong message and no findings. Never use `$error`/`$Error` as a loop variable.
- **The C#/XAML naming guard is diff-scoped by design.** `.github/workflows/csharp-xaml-naming-compliance.yml`
  builds its file list from `git diff --name-only $base $head` and calls the script with `-Files`, so a pull request
  that touches no C#/XAML/RESW file passes **and is not evidence that the tree is clean**. A repo-wide backlog of
  **910** findings sat behind that gate unmeasured until now.

---


---
## Iteration 1 - 2026-09-20
**User Story**: Phase 9 Wave 9 gates - T137 (live database validation); partial progress on the Phase 9 batch
**Tasks Completed**: 
- [x] T137: Live database validation. Applied and reversed all three paired `create.sql` / `rollback.sql` artifacts against local `mtm_waitlist` (each exit 0, each reversal verified by post-state row counts), confirmed `AllSeeds.sql` and `Bootstrap/update_table_descriptions.sql` agree with the files on disk, and confirmed the seven prepared situations resolve from real work centres through the real read path.
**Tasks Remaining in Story**: 5 - T129, T130, T131, T133, T139 (running-app UI gates and the Success Criteria walk)
**Commit**: No commit - partial progress. Phase 9 is not complete: the UI gates T129/T130/T131/T133 and the criteria walk T139 are still open.
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T137 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- The three build traps still apply, but the Phase 9 remaining work is all running-app gates, so the next iteration needs a signed-in shell and the seven prepared situations present.
- The live store was already seeded with the seven situations, and `100-3` additionally held a real Setup save. The store was left in the seeded state with `100-3`'s live row restored verbatim, so the app gates can run without re-seeding.
- The guard change from this batch is now live locally (7-parameter `sp_waitlist_request_status_update`), so FR-043 behaviour can be exercised from the app as well as from SQL.
- FR-054 (one request per die) remains the only substantive unbuilt feature work; everything else outstanding is a verification gate or a documentation reconciliation.
---

---
## Iteration 2 - 2026-09-20 08:03
**User Story**: Phase 13 Wave 3 — One request per die (FR-054) and the request page's every die location (FR-057). Partial progress on the wave: the build is complete and verified, its two UI gates are not.
**Tasks Completed**: 
- [x] T186: Tests written first and run red. Three new files — `NewRequestDieViewModelTests` (8 cases), `NewRequestOneRequestPerDieTests` (6 cases) and `DestinationQuestionRetirementTests` (6 cases) — plus the two existing tests that named the retired question moved with it (`ResolveLine2_PickupDie_ShowsTheDieNumberAndLocationWhateverTheChosenDestination` is now `ResolveLine2_PickupDie_ShowsTheDieTheRequestIsFor`, and `RequestItemConfigurationServiceTests` carries no destination fixture any more).
- [x] T181: The destination question retired in one change. Both die rows in `create.sql` and `AllSeeds.sql` now read `'collect-input-then-confirm', 1, 'enum'` with `NULL` options and an answer field declaring `'list','die'`, so `deliver-die` left `direct-to-confirmation` and both Items reach the die step; `destination` left the Line 2 token set and the resolver branch was deleted. The CSV needed no edit — rows 3 and 17 already carried the 2026-09-20 D22 note. The docs moved with it: `spec.md` (scenario 2, SC-024, the retired-question bullet, the `pickup-component` note), `contracts/request-picker-flow.md` §10 (the "first die" rule replaced by "every die, in the job's own order") and `contracts/item-configuration.md` §5 (the `die` value added to the `list` table).
- [x] T182: The die step built and wired — `NewRequestDieViewModel`, `NewRequestDieOption`, `NewRequestDiePage.xaml` / `.xaml.cs`, the `die` branch in `GetNextStepType` placed before the Preview return beside the dunnage branch, the page configured and registered in flow order, and nine new resource keys.
- [x] T187: `RequestJobFieldValues.Resolve` now answers `die` with every die the job carries and `pickup location` with every location those dies record, blank entries dropped; the snapshot's primary die is used only when the job carries no die row of its own. `WaitlistRequestTitles` prefers the die the request itself stored.
- [x] T183: `NewRequestFlowState.ToDrafts()` yields one draft per selected die and `NewRequestSummaryViewModel.SubmitAsync` submits each in turn, counting the ones raised and reporting the count in plain language when more than one went through.
**Tasks Remaining in Story**: 2 - T188 and T184, the two running-app UI gates. Both need a signed-in session, and the masked `'0000'` credentials force a password change on first use, so neither can be walked headlessly.
**Commit**: No commit - partial progress. Wave 3's UI gates T184/T188 are unchecked, so the wave is not complete.
**Files Changed**: 
- MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs, MTM_Waitlist.Settings/Models/RequestItemDefinition.cs, MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs
- MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs, MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs
- MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs, .../Models/NewRequestFlowState.cs, .../Models/NewRequestDieOption.cs, .../ViewModels/NewRequestDieViewModel.cs, .../ViewModels/NewRequestSummaryViewModel.cs
- Module_Waitlist/Views/NewRequestDiePage.xaml, Module_Waitlist/Views/NewRequestDiePage.xaml.cs
- Services/DependencyInjection/ServiceRegistrationExtensions.cs, Strings/en-us/Resources.resw
- Database/Seeds/seed_waitlist_request_item_configs/create.sql, Database/Seeds/AllSeeds.sql
- MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestDieViewModelTests.cs, .../NewRequestOneRequestPerDieTests.cs, MTM_Waitlist.Tests/Module_Settings/Services/DestinationQuestionRetirementTests.cs
- MTM_Waitlist.Tests/Module_Settings/Services/{RequestItemConfigurationServiceTests,RequestJobFieldValuesTests}.cs, MTM_Waitlist.Tests/Module_Waitlist/Models/{NewRequestFlowStateTests,WaitlistRequestTitlesTests}.cs
- specs/004-unified-card-item-picker/{spec.md,data-model.md,research.md,tasks.md,progress.md}, specs/004-unified-card-item-picker/contracts/{card-and-identifier.md,request-picker-flow.md,item-configuration.md,verification-gates.md}
**Learnings**:
- **Two traps cost real time.** A `with` expression must be parenthesised before a method call (`(X with { … }).WithDies(…)`), and `Assert.AreEqual` on two `string[]` fails even when the contents match — the suite's convention is `CollectionAssert.AreEqual`.
- **The environment, not the code, failed one test.** `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` is set as a user variable on this machine and overrides the malformed literal `StartupCoordinatorTests.RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync` feeds in, so the coordinator does not block. Clearing it (and `MTM_WAITLIST_DB_CONNECTION_STRING`) makes it pass. Read a full-suite result with both cleared.
- **The snapshot's primary die is a fallback, not a merge.** A job whose `Dies` list is populated but whose locations are blank must yield **nothing** for a location — falling back to `job.DieLocation` there invented a location the job does not record. The fallback now applies only when the job carries no die row at all.
- **`deliver-die` had to leave `direct-to-confirmation`.** It never reached an answer step, so without moving it the die step would have been unreachable for half the die requests.
- **Not every new page belongs in every audit list.** `PageActivationAuditTests`'s parameterless-constructor scan already covers the new page; its `s_newRequestWizardPages` list holds only the linear spine, and `NewRequestWizardAdvanceMarkupTests`' choice-step list asserts `ContinueCommand` is **absent**, which the die page uses.
- **Verification evidence.** Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → `0 Warning(s), 0 Error(s)`. Tests: `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → `total: 1136, failed: 0, succeeded: 1109, skipped: 27` (baseline 1089 total, so 47 tests added). The targeted filter over the touched suites was 79/79 green.
- **Next iteration's scope.** The only substantive feature work left in this file is the Phase 14 documentation and database-hygiene batches (T189 onward) and the database-artifact pairs (T213 onward). Everything else unchecked is a gate that needs a signed-in running app (T129/T130/T131/T133/T139, T164, T174, T184, T188) or the owner's own database reinstall (T165, which the task itself reserves to the owner).
---

---
## Iteration 3 - 2026-09-20
**User Story**: Phase 14 (US7, "The codebase is honest about itself") — Wave 4, "The guards that cannot fail" (T222-T226), plus the two documentation corrections that belong to the same defect class (T211, T212). Wave 4 is complete; US7 is not.
**Tasks Completed**:
- [x] T222: The SQL naming guard's failure path no longer dies on a reserved variable. Reproduced the defect first: `.github/scripts/validate-sql-naming.ps1` printed `SQL naming compliance failed:` followed by *"Cannot overwrite variable Error because it is read-only or constant"* at line 260 and exited 1 with **zero** findings, because `foreach ($error in $errors)` collides with PowerShell's read-only `$Error`. The loop variable is now `$violation`; the same run prints all eleven findings and exits 1.
- [x] T223: The eleven violations the crash was hiding are written up in `.github/reports/sql-naming-violations.md` with rule, file and line, and the rules file now says pre-existing violations are grandfathered while anything new still fails (D-7). Nothing was renamed. The eleven are seven filename-pattern failures (five files under `Database/InforVisual/Queues/Module_Mock/Populations/`, plus `Database/Mock.Service/Restore/replace_database.sql` and `verify_restore.sql`) and four boolean-column names (`require_password_change` in `01_core_users_profiles` and `AllTables.sql`, `requires_answer` in `31_waitlist_request_item_configs` and `AllTables.sql`).
- [x] T224: The guard no longer scans a wrong tree silently. It now resolves `Database/` from its own location and, when that folder is absent, prints the path it derived and exits 1 **before** scanning or changing directory. Proved by copying the script to `%TEMP%\sqlnaming-copytest\scripts\` and running it there: it named `…\Temp\Database` as the missing folder and exited 1, where it previously reported *"No SQL files found"* and exited 0. The scan itself now uses the absolute `$databaseRoot`.
- [x] T225: The C#/XAML rules file now matches its mechanism. The *Enforcement* section keeps the hard-fail, states that the check validates **the files a pull request changed**, warns that a run with no changed C#/XAML/RESW files passes by design and is not evidence the tree is clean, records that pre-existing violations are grandfathered and tracked in `.github/reports/csharp-xaml-naming-violations.md`, and keeps the written-approval requirement for exceptions. The *"Legacy naming must be bulk normalized (no grandfathering mode)"* clause is gone (D-4). The workflow was read rather than assumed: it derives its file list from the PR diff and calls the script with `-Files`, so the stated scope is the real one.
- [x] T226: `.github/reports/csharp-xaml-naming-violations.md` — every finding with rule, file and line, grouped by rule then sorted by file, with a per-rule summary and a per-project distribution. The report is the deliverable; no renames were made.
- [x] T211: `.github/instructions/database-schema-rules.instructions.md` no longer names the retired `core_workstations_registry`, and its migration section no longer describes a FluentMigrator runner. Verified first that `FluentMigrator` survives in the tree only as a description of the **retired** layout (`restructure-database-layout.ps1` and the validator's own comment).
- [x] T212: `Database/Database-Ruleset.md` *Core Startup Tables* now names `core_computers_registry`. `core_workstations_registry` survives in only three places in the tree after the edit — the two deliberate historical notes in `Database/Mock.Service/Restore/` that exist to record the rename, and nothing else. The same file's *Artifact Layout and Release Governance* section also still recommended FluentMigrator as the runner layer, the identical stale claim T211 corrected, so it was corrected in the same pass; leaving it would have recreated the contradiction between the two always-on rules files.
**Tasks Remaining in Story**: 46 unchecked tasks in the file. Within Phase 14 the documentation batch (T189-T210) and the database-artifact batch (T213-T221) are still open, as are T227-T232.
**Commit**: `e398ac1` — "fix(004): the guards that cannot fail, and the stale rules they hid" (7 files, +1566/-35). Committed as one coherent unit: the wave plus its two documentation siblings. US7 itself is not complete, so this is a checkpoint rather than a story completion. This progress entry lands in the follow-up docs commit. The unrelated in-flight work already in the working tree (the die-page feature files, `READINESS-CHECKLIST.md`, the spec and contract edits from iterations 1 and 2) was deliberately **not** swept in.
**Files Changed**:
- .github/scripts/validate-sql-naming.ps1 (T222 reserved-variable fix, T224 root guard)
- .github/instructions/database-schema-rules.instructions.md (T211 stale table name and migration model, T223 grandfathering clause)
- .github/instructions/csharp-xaml-naming-rules.instructions.md (T225 enforcement wording and real scope)
- .github/reports/sql-naming-violations.md (new, T223)
- .github/reports/csharp-xaml-naming-violations.md (new, T226)
- Database/Database-Ruleset.md (T212 table name and migration statement)
- specs/004-unified-card-item-picker/tasks.md (T211, T212, T222-T226 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The guards were both broken, and both broken in the direction that hides work.** The SQL guard died on its own error path and reported nothing; the C#/XAML guard only ever looked at changed files, so a repo-wide backlog was invisible by design. A guard that cannot report is worse than no guard, because the green light is read as evidence.
- **The C#/XAML total is 910, not the 901 the plan recorded.** Re-measured live this iteration and confirmed against the report: 475 `Async Task method … must end with Async`, 377 `RESW key … must match Feature_Element.Property`, 33 `Private static readonly field … must be s_camelCase`, 24 `Public type … must match file name`, 1 `Private field … must be _camelCase`. The two dominant rules are 852 of the total, so a normalization pass should take those two first. The per-project split also drifted from the plan: Tests 493 (was 490) and Strings 359 (was 353).
- **The three duplicate table pairs are byte-identical, which unblocks T213.** `create.sql` **and** `rollback.sql` match by SHA256 for (`02_core_workstations_registry` / `02_core_computers_registry`), (`13_setup_workstations_catalog` / `13_setup_work_centers_catalog`) and (`14_config_workstation_hot_workcenters` / `14_config_computer_hot_work_centers`). In each pair the **legacy-named** folder is the stale half — every one of them creates the *new* table name, so the folder name is the only thing that is wrong. T213's proof condition is therefore already satisfied on this tree, and T214 is safe to run; T215 and T216 must follow in the same change to keep `AllTables.sql` and the bootstrap honest.
- **T206's premise is stale.** It says `capabilities/` is absent from the tree, but the directory exists and holds `startup/spec.md` and `startup-diagnostics/spec.md`. The task needs re-scoping to *restore or re-derive* `capabilities/DRIFT.md` rather than to create the folder.
- **Verification evidence.** SQL guard: eleven findings printed, exit 1. Wrong-tree guard: names the derived path, exit 1. C#/XAML guard: 910 findings, exit 1, matching the report line-for-line. No C#, XAML, RESW or `.csproj` file was touched this iteration, so there is no build or test signal to add to the last full-suite result (`total: 1136, failed: 0, succeeded: 1109, skipped: 27`).
- **Next iteration's scope.** T213 (prove the pairs) is already satisfied, so T214 → T215 → T216 (delete the stale half, then reconcile `AllTables.sql` and the bootstrap) is the natural next unit and is self-contained. Every other open task is either a running-app gate or reserved to the owner.
---
