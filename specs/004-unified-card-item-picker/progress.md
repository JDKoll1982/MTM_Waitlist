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
- **`R` is an alias for `Invoke-History`.** Naming a helper function `R` (or `Lines`) silently calls the built-in
  instead, and the failure looks like a logic bug in the caller. Pick a name no alias claims.
- **A trailing `)` imbalance reports a `ParserError` at the wrong column.** Do not trust the reported position -
  dump the script with numbered lines and read the tail of the block before re-running.
- **`Get-ChildItem -Filter` accepts one string, not an array.** `-Filter @('a.cs','b.cs')` fails with *"Cannot convert
  System.Object[]"*; use `-Include` with `-Recurse`, or one `-Filter` per call.
- **`[regex]::Matches(...) | Select-Object -First 1` returns the collection, not the first match.** Use
  `[regex]::Match(...)`. A sweep built on the former reports matches it did not find.
- **`Get-Content` on a BOM-less UTF-8 file decodes as ANSI and destroys non-ASCII on write-back.** Any in-place edit
  must read with `[System.IO.File]::ReadAllLines($p, [System.Text.Encoding]::UTF8)` and write with
  `UTF8Encoding($false)`.
- **A measurement tool must not let "no evidence" read as "perfect".** Walking only the entries a file already
  mentions passes whatever is absent, so a store that never ran looks like a store with nothing wrong. Enumerate the
  expected set independently and report the gap.
- **Retention equal to the period being measured is the same as having no history.** Tie the retention constant to
  the window constant in one place, so the window cannot be raised past the evidence.
- **A reader must never block the writer, and vice versa.** Open an append-only history with `FileMode.Append` and
  `FileShare.Read`, and replace it atomically (`.tmp` + `File.Move(overwrite: true)`) when retention prunes it.
- **The C#/XAML naming guard is diff-scoped by design.** `.github/workflows/csharp-xaml-naming-compliance.yml`
  builds its file list from `git diff --name-only $base $head` and calls the script with `-Files`, so a pull request
  that touches no C#/XAML/RESW file passes **and is not evidence that the tree is clean**. A repo-wide backlog of
  **910** findings sat behind that gate unmeasured until now.
- **Windows PowerShell 5.1 cannot run `.github/scripts/validate-database-schema.ps1`.** Its `Add-Type` of the
  isolated load context fails with *"The type or namespace name 'Loader' does not exist in the namespace
  'System.Runtime'"* — `System.Runtime.Loader` is not in the 5.1 framework — and the script exits **1 before it
  reaches a single check**, which reads as a schema failure and is not one. Always run it as
  `pwsh -NoProfile -ExecutionPolicy Bypass -File .github/scripts/validate-database-schema.ps1`. The CI workflow
  already uses `shell: pwsh`; do not "simplify" that to `powershell`.
- **Never resolve a build dependency with `Get-ChildItem -Recurse | Select-Object -First 1` over the NuGet cache.**
  `validate-database-schema.ps1` did that for `Microsoft.Extensions.Logging.Abstractions` and picked **1.1.1** — the
  first of 22 cached versions, nine majors behind the 10.0.10 the app resolves — so the isolated load context
  force-loaded the wrong assembly and MySqlConnector's logging type initializer threw. The script died before any
  check, with an error that names neither the file nor the real cause. Resolve from **beside the assembly that needs
  it** (the build output) first, and fall back to the **highest** cached version by parsing the version folder name
  in a `try/catch` — never to the first one the file system happens to enumerate. This is version-dependent
  behaviour: the CI runner's cache held only 10.0.10, so the same script worked there and failed only locally.
- **A red CI job is not evidence that a check ran.** `database-schema-validation.yml` has failed on **every** recent
  run (10 of 10, 2026-09-12 → 09-14) — and the reason is environmental: its connection-string secrets hold
  private-LAN hosts that a GitHub-hosted runner cannot reach (*"Unable to connect to any of the specified MySQL
  hosts"*, *"Connect Timeout expired."*), so the job fails honestly but for a reason that says nothing about the
  schema. With no secrets configured the same script skips and exits **0**. The job is therefore either vacuous or
  permanently red; read the log before treating either state as coverage.
- **`TRUNCATE` is refused on any table an FK references, whatever the row counts.** `Database/Seeds/
  seed_config_image_storage_settings/create.sql` opens with `TRUNCATE TABLE config_settings_values` and cannot
  succeed once `config_settings_history` exists with `fk_settings_history_settings_values_config_setting_id`
  (`NO ACTION`) — even though history held **0** rows against 3 values rows, so the block is vacuous and fatal. The
  seed is applied **first** of eight, so this breaks a fresh install and, because `Apply-Seeds` also runs on the
  success path, can turn an otherwise-green validation red at its final step. Use `DELETE FROM` where the FK must be
  honoured, or drop and re-add the FK around the truncate.
- **`git rm` needs `--` before the path list.** A repo path that begins with `-` is parsed as an option and the
  command fails or does nothing, silently. Use `git rm -r --quiet -- <paths>`.
- **A duplicate-create check must cover all four artifact kinds.** Scanning only `Database/Tables/**/create.sql`
  finds the table duplicates and misses the procedure, view and function duplicates created by the same rename —
  this repository had 11 pairs, not 3. Group every `create.sql` under `Database/**` by (kind, created object name)
  and fail on any name claimed twice.
- **A guard that runs after a connection-string gate never runs in CI.** `validate-database-schema.ps1` skipped
  its whole validation phase when the connection-string secrets were empty, so the duplicate creates passed every
  run; when the secrets *are* present the same phase cannot reach the private-LAN hosts and the job fails instead.
  Put static checks **before** the gate; they are the only checks a DB-less runner can perform.
- **`Database/Tables/20`–`29` hold `rollback.sql` only, by design.** They are the retained drops for the retired
  `mock_*` family and the retired request type/subtype catalog. `RetiredSymbolAuditTests.IsExemptFromSqlAudit`
  exempts any file named `rollback.sql` precisely so these can name an object that no longer exists. A create-less
  artifact folder is not corruption and must not be tidied away.
- **A `ContentDialog`'s buttons carry the framework's own AutomationIds — `PrimaryButton`, `SecondaryButton`,
  `CloseButton` — not empty ones.** Matching a dialog button on an empty AutomationId selects the *card's* button
  instead, because the card's "Cancel request" button also has a visible Name of "Cancel request". Match on the Name
  **plus** membership in that three-id set. The reason box is `AutomationId=''` with `Name='Reason (optional)'` —
  match the Name (the shell's search box owns the id `TextBox`).
- **A `ContentDialog` is drawn as a `Window` whose `BoundingRectangle` is the whole app window**, so a naive
  descendant text dump returns the shell behind it. Bound the band from `confirmButton.Y - 200` to `confirmButton.Y`.
- **`WaitlistViewPage_CardList` is virtualized.** Only viewport cards are realized; off-screen groups report
  `Infinity`/`NaN` rectangles, so a driver must skip infinite rectangles and scroll with
  `ScrollPattern.LargeIncrement` until the percentage stops moving. Once the list shrinks below the viewport,
  `SetScrollPercent` and `Scroll` throw *"Operation is not valid due to the current state of the object"* — treat
  that throw as "the whole list is already visible".
- **`MainWindowHandle` first points at the splash window** (title `WinUI Desktop`), which has no UIA children; the
  shell window replaces it. Re-resolve `FromElement(FromHandle($p.MainWindowHandle))` in a poll loop and probe for a
  shell AutomationId (`WaitlistViewPage_AddRequestButton`) rather than sleeping or locating by title.
- **A helper used inside a function that also `return`s a value must write with `Write-Host`, not `Write-Output`.**
  `Write-Output` feeds the function's return value and corrupts it — this bit the `Log` helper and made
  `Find-MtmById` receive an array of strings. Related: run a driver `.ps1` with
  `powershell -NoProfile -ExecutionPolicy Bypass -File <path>`, never dot-source it — dot-sourcing lets the
  script's `switch` value flow into the caller's pipeline.
- **The request table's ids are not contiguous, and were not before this work either.** `waitlist_requests_queue`
  currently holds 37 rows with ids `1–18, 28, 29, 31, 32, 33, 35–43, 47–51`; ids `19–27, 30, 34, 44, 45, 46` are
  absent. T129's nineteen raised rows are exactly the nineteen listed, so nothing is missing from that population —
  but ids `30` and `44`, named by the ticked T132 and T134, **are** absent, and neither of those two tasks carries
  an evidence block in `tasks.md`. Read the id gaps before treating a missing row as a lost one.
- **A ticked task with no evidence block is not evidence, and the criteria walk must say so.** Three tasks in this
  feature (T132, T134, T138) are `[x]` with no run recorded anywhere; T132 and T134 have been ticked since commit
  `45a9984` (2026-09-13), i.e. from the day Phase 9 was written, and T138 the same. `git log -S` on the task id is
  the fastest way to date a tick; if the tick predates the population the task claims to have exercised, the tick
  cannot be corroborated from the repo.
- **The whole nineteen-row batch has a future deadline and none is overdue.** All 19 rows carry a non-null
  `target_time_utc` that the application derived from the Item's allotted minutes, and none has passed it; the only
  `is_overdue = 1` row in `waitlist_requests_queue` is demo row `10` (`pickup-coil`, `Accepted`, seeded
  2026-09-14). So SC-018's negative half is verifiable from the store and its real-expiry half is not — an
  "is anything overdue?" query answers only the first.
- **`waitlist_request_item_configs` holds 23 rows, not 19, and has no `is_in_scope` column.** A query for that
  column fails `ERROR 1054`. The 19 in-scope Items are the set T129 raised; the other four (`pickup-fg`,
  `pickup-ncm`, `pickup-outside-service`, `pickup-wip`) are the FR-028 out-of-scope Items. Compare **sets**, not
  counts, when checking SC-014.
- **A `GridView` card exposes `InvokePattern`, so `ItemClick` can be driven headlessly.** The dunnage and work-centre
  tiles are `ControlType.ListItem` under a `ControlType.List` and answer
  `$e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()` — the same route the user's
  click takes. `SelectionItemPattern.Select()` also works on `NavigationViewItem` and on the facility selector's
  `ComboBoxItem`s.
- **A real mouse click does nothing while another window holds the foreground.** `SetCursorPos` + `mouse_event` was
  silently a no-op with a File Explorer window focused, so a driver that "clicked" reported success and changed
  nothing. Prefer `InvokePattern`; treat synthetic mouse input as the last resort it is.
- **`BoundingRectangle` can be `Infinity`, and casting it to `[int]` throws.** Virtualized or off-screen items
  report it, and the failure is `Cannot convert value "∞" to type "System.Int32"` — guard with
  `[double]::IsInfinity()` before reading a centre point, exactly as the waitlist card list already required.
- **`Get-Process X | Stop-Process -Force` is refused; `Stop-Process -Id $_.Id -Force` is not.** Enumerate the
  instances and stop them by id:
  `Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }`.
- **`\G` does not work through `mysql -e` on this build.** It exits 1 with no rows. Use `-N -B` (tab-separated,
  no header) and read the columns positionally instead.
- **A duplicate-active refusal is the guard, not the step failing.** `WaitlistRequestService.SubmitAsync` matches on
  `(building, work centre, item, answer)` against the queue snapshot, so once a dunnage walk has written its row a
  second identical walk is refused with `Matching request already active`. To exercise the *other* answer path,
  switch facility rather than deleting the row — the walk needs a job carrying dunnage, and only `100-1806`,
  `V100-33` and `100-3` have one.
- **The dunnage step has no Continue button, and that is the requirement.** `NewRequestDunnagePage` carries only
  `NewRequestDunnagePage_BackButton` and `NewRequestDunnagePage_SubstituteButton`; choosing a card *is* the advance.
  A driver waiting for a Continue that never renders will time out on a page that is working correctly.

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
---
## Iteration 4 - 2026-09-20
**User Story**: Phase 14 Wave 3 - the database-artifact duplicate pairs (T213-T221), complete
**Tasks Completed**:
- [x] T213: proved all 11 duplicate-create pairs identical (SHA256, `create.sql` and `rollback.sql`)
- [x] T214: deleted the 11 superseded folders (22 files) with `git rm -r`
- [x] T215: swept for dangling references - none; the surviving folder is the only definition of each object
- [x] T216: reconciled the aggregate, the folder set and the bootstrap; proved a fresh install produces every table
- [x] T217: found and removed six duplicate *procedure* pairs, not just the tables
- [x] T218: recorded the rollback-only convention for `Database/Tables/20`-`29` in the ruleset
- [x] T219: confirmed and recorded that the mirror schema lives only under `Database/Mock/`
- [x] T220: taught `validate-database-schema.ps1` to fail on two artifacts creating one object
- [x] T221: established that the CI workflow is either vacuous or permanently red, and never schema-checked
**Tasks Remaining in Story**: None - the database-artifacts batch is complete. 37 tasks remain elsewhere in `tasks.md`.
**Commit**: the batch commit for this iteration (amended to carry the validator's assembly-resolution fix)
**Files Changed**:
- Database/Tables/02_core_workstations_registry/ (deleted)
- Database/Tables/13_setup_workstations_catalog/ (deleted)
- Database/Tables/14_config_workstation_hot_workcenters/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_delete/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_get_all/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_touch/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_upsert/ (deleted)
- Database/StoredProcedures/sp_config_hot_workcenters_delete_for_workstation/ (deleted)
- Database/StoredProcedures/sp_config_hot_workcenters_get_for_workstation/ (deleted)
- Database/Views/vw_setup_workstations_active/ (deleted)
- Database/Functions/fn_setup_workstation_name_normalized/ (deleted)
- .github/scripts/validate-database-schema.ps1 (T220 duplicate-object guard)
- Database/Database-Ruleset.md (T218, T219 review notes)
- specs/004-unified-card-item-picker/tasks.md (T213-T221 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The duplicate defect was four artifact kinds wide, not one.** T217 asked only about procedures; the same rename left duplicate *views* and *functions* too. Counting the pairs that actually exist gives **11**, not the 3 the wave was scoped around: 3 tables, 6 procedures, 1 view, 1 function. All 11 are byte-identical in both files, and in every pair the legacy-named folder creates the **renamed** object - so the folder name is the only thing wrong, and deleting the stale half cannot change behaviour.
- **Deleting the stale half needed no aggregate edit.** `AllTables.sql` never listed the duplicate folders, so it already held each table exactly once. Verified as a **set equality**, not a spot check: 21 aggregate names and 21 folder names, nothing only-in-aggregate, nothing only-in-folders, no duplicate in the aggregate.
- **The fresh-install proof can be run without touching the live store.** `AllTables.sql` opens with `USE mtm_waitlist;`, so the file cannot be piped into a scratch schema as-is - strip that line, create `mtm_waitlist_probe`, apply, count, drop. The apply exited 0 and produced exactly **21** base tables including all three renamed ones; `mtm_waitlist_probe` was dropped afterwards and the live store was never in the path.
- **The live store now matches the aggregate exactly** - 21 base tables against 21 aggregate creates, same names. That is a stronger statement than the previous iteration's notes carried, and it means the T215 sweep's conclusion ("no dangling references") is corroborated by the running database rather than only by grep.
- **The duplicate creates survived CI because no schema check has ever run there, and the reason is structural.** `validate-database-schema.ps1` applies the table creates in order, so a second create of the same table is not an error to MySQL - it is a no-op overwrite. The check that would have caught it (two artifacts, one object) did not exist, and the phases that *would* have caught it downstream cannot run on a runner at all: with the connection-string secrets configured - and they **are** configured - the hosts they name are private-LAN addresses, so the job dies on *"Unable to connect to any of the specified MySQL hosts"* / *"Connect Timeout expired."* before reaching the schema. With no secrets it skips and exits 0 instead. Either way the tree is never compared against a database. The new guard sits **before** the gate for exactly that reason: it is the only check in the job that a runner can actually perform.
- **A red CI job is not coverage.** All 10 of the most recent runs of `database-schema-validation.yml` failed (2026-09-12 to 2026-09-14), and the failure is environmental - unreachable LAN hosts - so it says nothing about the schema. The job's two reachable states are "vacuous, exit 0" and "permanently red"; neither is a check. T221's real finding is narrower and less comfortable than the task assumed: the workflow never failed for a *schema* reason, and it also never passed for one.
- **A seed can be unable to run at all, and the failure is vacuous.** With a reachable store the validator now gets past the schema check and dies in its seed phase: `Database/Seeds/seed_config_image_storage_settings/create.sql` opens with `TRUNCATE TABLE config_settings_values`, which MySQL refuses once `config_settings_history` exists with `fk_settings_history_settings_values_config_setting_id` (`NO ACTION`) - regardless of row counts. History held **0** rows against 3 values rows, so the constraint blocks nothing real and the statement fails anyway. It is applied **first** of eight, so this breaks a fresh install; and because `Apply-Seeds` also runs on the success path, it can turn an otherwise-green validation red at its final step. Reported, not fixed here - it is a seed defect rather than part of the artifact-deduplication unit.
- **A destructive-sounding run wrote nothing, and that was verified rather than assumed.** The refused `TRUNCATE` aborted the seed loop on its **first** file, so no later seed ran and no partial state was left. Confirmed against the store afterwards: `config_settings_values` still holds its original 3 rows and **0** rows matching `image_storage.%` (the seed would have written 4).
- **The validator could not run locally at all, for a reason that never affected CI.** It resolved `Microsoft.Extensions.Logging.Abstractions` with `Get-ChildItem -Recurse | Select-Object -First 1`, which returns **1.1.1** - the oldest of 22 cached versions, nine majors behind the 10.0.10 the app resolves - and the isolated load context force-loaded it, so MySqlConnector's logging type initializer threw and the script died before any check. The runner's cache held only 10.0.10, which is why CI reported connection errors instead. Fixed by preferring the copy beside `MySqlConnector.dll` and taking the **highest** cached version as the fallback.
- **PowerShell 5.1 is a trap for this script.** The validator cannot run under 5.1 at all (`System.Runtime.Loader` absent), and it exits 1 with a message that looks like a schema failure. Anyone who runs it with `powershell` instead of `pwsh` will read a false red. Recorded in the patterns section above so the next iteration does not have to rediscover it.
- **Pre-existing uncommitted work was left alone.** The working tree carried substantive uncommitted changes from earlier iterations (`MTM_Waitlist.Settings`, `MTM_Waitlist.Waitlist.NewRequest`, `MTM_Waitlist.Waitlist.View`, `Services/DependencyInjection`, several test files, `Database/Seeds/*`, and `.specify/extensions/ralph/scripts/powershell/ralph-loop.ps1`) - 366 insertions across 16 files. They are outside this unit's scope and were not verified by this iteration, so they were deliberately **not** swept into this commit even though the loop recipe suggests `git add -A`. They remain uncommitted and are flagged for the owner below.
- **Verification evidence.** Duplicate scan after deletion: 0 duplicate creates across 21 table and 84 routine/view `create.sql` files. Validator on a clean tree with no connection-string variables, under `pwsh`: *"Duplicate-object check passed: 94 artifact(s) create 94 distinct object(s)."* then *"Skipping validation because no configured connection-string env vars are set."*, exit 0. Validator with a deliberately recreated duplicate: names the table and both paths, exit 1. Live store: for the workstation/work-centre family only the renamed objects exist. No C#, XAML, RESW or `.csproj` file was touched this iteration, so the last full-suite result (`total: 1136, failed: 0, succeeded: 1109, skipped: 27`) still stands and no build or test run was needed.
- **Next iteration's scope.** The remaining 37 tasks are dominated by running-app UI gates (T129-T133, T139, T164, T174, T184, T188), the owner's database reinstall (T165), and the documentation batch (T189-T210). The documentation batch is the only self-contained, verifiable unit left that does not need a signed-in app; the UI gates need a valid account credential and a running Debug build.
---

---
## Iteration 5 - 2026-09-20
**User Story**: Phase 14 (US7, "The codebase is honest about itself") — complete. This iteration landed T230, the last open task in the story.
**Tasks Completed**:
- [x] T189-T210: the documentation batch — every claim that no longer matched the shipped application corrected
- [x] T227: the honesty sweep over the retired-systems boundary
- [x] T228: the 39-box `READINESS-CHECKLIST.md` walk
- [x] T229: the SC-007 / SC-008 diagnosis corrected wherever it was written as "it needs 30 days of wall time"
- [x] T230: a durable, append-only run history for the criteria to be measured against
- [x] T232: four further SC-007 / SC-008 claims corrected in `specs/001-module-mock-visual-fallback/tasks.md`
**Tasks Remaining in Story**: None - US7 is complete. 10 tasks remain elsewhere in `tasks.md`.
**Commit**: `7333147` — "feat(004): US7 make the codebase honest about itself" (39 files, +2249/-154). Committed as one coherent unit: the story's last open task plus the documentation batch it completes. This progress entry lands in the follow-up docs commit, as iteration 3's did.
**Files Changed**:
- MTM_Waitlist.Mock.Service/Models/ReliabilityCriteria.cs (new - the numbers the criteria are measured over, in one place)
- MTM_Waitlist.Mock.Service/Models/RunHistoryEntry.cs (new - the JSONL line shape, and the measurement tool's contract)
- MTM_Waitlist.Mock.Service/Models/RefreshRunTrigger.cs (new - scheduled vs onDemand)
- MTM_Waitlist.Mock.Service/Services/RunHistoryStore.cs (new - the append-only history, with retention)
- MTM_Waitlist.Mock.Service/Models/RefreshRunRecord.cs (CycleUtc + Trigger)
- MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs (cycle identity threaded through every record)
- MTM_Waitlist.Mock.Service/Services/RefreshRunRecordRecorder.cs (writes the history beside the run record)
- MTM_Waitlist.Mock.Service/Services/BackupEngine.cs (same, for every backup outcome)
- MTM_Waitlist.Mock.Service/Services/ServiceLog.cs (RetentionDays 30 -> ReliabilityCriteria.HistoryRetentionDays)
- MTM_Waitlist.Mock.Service/Models/BackupPolicy.cs (RetentionCount 14 -> ReliabilityCriteria.BackupRetentionCount)
- MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs (the duplicated RetentionCount default, same source)
- MTM_Waitlist.Mock.Service/Services/ServiceHostBuilder.cs (RunHistoryStore registered; both factories updated)
- MTM_Waitlist.Tests/Module_Mock_Service/RunHistoryStoreTests.cs (new - 32 cases)
- MTM_Waitlist.Tests/Module_Mock_Service/{RefreshCycleGateTests,BackupRestoreTests,BackupRetentionTests,ServiceApiSecurityTests,ServiceCapabilityEnforcementTests}.cs (constructors)
- tools/measure-reliability-window.ps1 (SC-007 now reads scheduled cycles only; SC-008 now checks each store's own windows and names a store that never ran)
- tools/README.md (the tool documented)
- specs/004-unified-card-item-picker/tasks.md (the story's tasks ticked with recorded evidence)
- specs/001-module-mock-visual-fallback/tasks.md, OPEN-TASKS.md, READINESS-CHECKLIST.md, FEATURES.md
- WeekendProject/Module_Mock/Module_Mock-Planning-Progress.md, WeekendProject/PromptFiles/* (the documentation batch)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The criterion was unmeasurable for two reasons, and only one of them was retention.** `ServiceLog.RetentionDays` (30) equalled the window and `BackupPolicy.RetentionCount` (14) was half of it, so the record of the window was swept as the window closed. But the deeper blocker was that **nothing aggregated a history at all**: `RefreshRunRecordStore` and `BackupArtifactStore` are dictionaries keyed by shape and by store, so each holds one run — the last one — and no amount of wall time turns that into 30 days of evidence. A history had to be created, not merely retained longer.
- **Cycle identity is what makes SC-007 countable, and it has to be recorded rather than reconstructed.** SC-007 is stated over *cycles*, not shapes. Grouping by timestamp alone would be a guess: two shapes refreshed minutes apart by different code paths look identical to a reader. Carrying an explicit `CycleUtc` on every record makes "these five shapes were one cycle" a stored fact, so a cycle that refreshed four of five shapes counts as the partial failure it is instead of as an 80% success.
- **An on-demand refresh would have let the rate be improved by asking for more refreshes.** The API's refresh endpoint calls the same `TryRunShapesAsync` the schedule does, so without a `Trigger` field a caller could raise the success rate by triggering successful runs. The measurement now keeps `scheduled` only, and the tool's filter treats a record with no `trigger` as scheduled so a history written before the field existed still reads.
- **"No evidence" must not read as "perfect".** The first cut of SC-008 walked the four known stores and asked each whether the window had a record. That silently passes a store whose schedule never fired at all, because the check only looked at stores the history already mentioned. It now reports a store with no record at all as a failure — that store is exactly what the criterion exists to catch.
- **Retention equal to the window is the failure mode, so the constants are tied together rather than restated.** `ReliabilityCriteria` holds `MeasuredWindowDays = 30`, `HistoryRetentionDays = 60`, `BackupRetentionCount = 44`; `ServiceLog.RetentionDays` and both copies of the backup default now read from it. Raising the window moves the retention with it, so the criterion cannot be made unmeasurable by editing one number.
- **A reader must never block a writer.** The history is opened `FileMode.Append` / `FileShare.Read`, so the measurement tool — or an operator with the file open — cannot stall the service, and the service cannot stall the measurement. Retention replaces the file atomically through a `.tmp` + `File.Move(overwrite: true)`, so a reader never sees a half-written one.
- **PowerShell aliases bite in this repo's scripts.** `R` is an alias for `Invoke-History` in Windows PowerShell, so a helper named `R` silently calls the wrong command. The same class of problem produced a misleading `ParserError` pointing at the wrong column for a trailing `)` imbalance — dump numbered lines before re-running rather than trusting the reported position.
- **`Get-ChildItem -Filter` takes one string, not an array.** Passing `@('a.cs','b.cs')` fails with *"Cannot convert System.Object[]"*; use `-Include` with `-Recurse`, or one `-Filter` per call.
- **`[regex]::Matches(...) | Select-Object -First 1` returns the collection, not the first match.** Use `[regex]::Match(...)`. A sweep built on the former reports a match it did not find.
- **`Get-Content` on a BOM-less UTF-8 file decodes as ANSI and destroys non-ASCII on write-back.** Every script that edits a file in place here must use `[System.IO.File]::ReadAllLines($p, [System.Text.Encoding]::UTF8)` and write with `UTF8Encoding($false)`.
- **Verification evidence.** Build `0 Warning(s) / 0 Error(s)`. Suite **1141 passed / 0 failed / 27 skipped / 1168 total** with both connection-string variables absent — the 1109 baseline plus the 32 new `RunHistoryStoreTests` cases. The loop was then closed by hand: a fixture wrote **270 lines through the real `RunHistoryStore`** and `tools/measure-reliability-window.ps1 -Root <that folder>` read them and reported SC-007 **30 of 30 scheduled cycles, 100%, PASS** and SC-008 **120 windows, 4 of 4 stores, 0 missing artifacts, PASS**. All four seeded scenarios still discriminate (93% -> rate FAIL, 97% with one unattributed skip -> attribution FAIL, one missing artifact -> SC-008 FAIL). The tool now labels its own output `Evidence: read from <path>` rather than `seeded` when it did not generate the file.
- **One flake, seen once, and it is the documented one.** A full-suite run in this iteration reported 1140 passed / 1 failed before two clean runs at 1141 / 0. The failure is the known cross-test leak: three test classes save-and-clear the connection-string variables and restore them in `[TestCleanup]`, but `VisualUnreachableFastFailTests`, `MockServiceRefreshClientTests` and `BackupRestoreTests` do not, so `StartupCoordinatorTests.RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync` can see a stale value. Pre-existing, not caused by this unit, and not fixed here.
- **A fixture proves the metric, not the service.** The 30/30 and 120/120 above are a fixture's numbers. Reporting them as the measured outcome of SC-007/SC-008 would be exactly the dishonesty this story exists to remove, which is why `READINESS-CHECKLIST.md` Phase 5 stays owed and unticked and the tool says so on its own output.
- **Next iteration's scope.** 10 tasks remain: the Phase 9 running-app UI gates and criteria walk (T129, T130, T131, T133, T139), the Phase 11 gates (T164, T165), Phase 12's T174, and Phase 13's T184 and T188. All but T165 need a signed-in session against a running Debug build.
---
---
## Iteration 6 - 2026-09-21
**User Story**: Phase 9 Wave 7 - T129 (raise the nineteen requests through the application, one per in-scope Item, as the ten existing accounts, across more than the seven prepared-situation work centres, all left unhelped)
**Tasks Completed**: 
- [x] T129: Nineteen requests raised through the running Debug build of `MTM_Waitlist.exe`, one fresh sign-in per account, one request per in-scope Item, and read back from `mtm_waitlist.waitlist_requests_queue` as rows 28-51 (all `Pending`, nineteen distinct `item` values, eight work centres `100-3` / `100-6` / `100-7` / `100-12` / `100-18` / `100-1806` / `V100-33` / `V100-34` across both buildings, all eight roles represented). None accepted, completed or canceled (FR-040). Evidence block recorded under T129 in `tasks.md`.
**Tasks Remaining in Story**: None - story complete for this task
**Commit**: `docs(004): record iteration 6 of the ralph loop - T129 population raised` (this entry's own commit, carrying `tasks.md` and `progress.md`)
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T129 ticked with its evidence block)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The wizard snapshots the building when it is entered, and never re-reads it.** `WaitlistViewPage.OnAddRequestClick` (L57-65) builds the flow state from `ViewModel.SelectedBuilding`, and `NewRequestWorkCenterViewModel.SelectWorkCenterAsync` sets only `WorkCenter`, `RequesterEmployeeNumber` and `RequesterEmployeeName` - it never writes `Building`. Switching the shell's facility selector *after* the wizard has opened therefore raises the request under the old building while the work-centre list filters on the new one, which is how five Vits Drive rows were first stored as `Expo Drive`. The driver now switches the building **before** entering the wizard, and re-enters through the waitlist's own Add Request button (`ReturnToWaitlist`, not `AddAnotherRequest`) so the state is rebuilt from the current selection - `AddAnotherRequest` deliberately carries `_state.Building` forward.
- **The New Request result page's `Continue` button is not a neutral "carry on".** `NewRequestResultViewModel.ContinueDuplicateAsync` calls `ResubmitAsync(allowDuplicate: true)`, so on the duplicate page it re-submits the same request with the guard bypassed. One accidental click created a real row. The driver must never use `Continue` for navigation; it now offers only `ReturnToWaitlist` then `AddAnotherRequest`.
- **`Get-StepName` must take the longest `New Request...` title, not the first.** The shell's breadcrumb reads exactly `New Request` and sits ahead of the page title in the tree, so "first match wins" reported the breadcrumb and every wait for a step title timed out on pages whose title is not first. Fixed to keep the longest candidate.
- **The duplicate guard is (building, work centre, item, answer), and it reads the app's in-memory snapshot.** `WaitlistRequestService.SubmitAsync` (L817-826) matches on those four fields only, so a plan can place the same Item on several work centres safely - but the check runs against the queue as loaded at sign-in, so rows deleted from MySQL after launch still block a request until the app is restarted.
- **Two of the ten accounts are one person.** `johnk` and `jkoll` both carry employee `6229`, and the queue has no user-id column, so the store shows nine distinct requesters for ten sign-ins. The account *is* what was signed in (the `USERNAME` environment variable drives `StartupCoordinator`), but the store cannot distinguish the two.
- **The eighteen demo rows were left in place, and the two populations do not collide.** No task rewrites `seed_waitlist_requests_default`; the plan avoided every (building, work centre, item, answer) the demo rows occupy, and a join across the two populations returns no active collision.
- **`auth_roles_catalog`'s primary key is `id`, not `role_id`.** The assignment table's foreign key is `role_id`, which makes the obvious join look correct and fail with `ERROR 1054`.
- **Next iteration's scope.** 9 tasks remain: the Phase 9 running-app UI gates and criteria walk (T130, T131, T133, T139), the Phase 11 gates (T164, T165), Phase 12's T174, and Phase 13's T184 and T188.
---
## Iteration 7 - 2026-09-21
**User Story**: Phase 9 Wave 8 - T130 + T131 + T133 (five of the seven request-handling outcomes, driven through the running Debug build)
**Tasks Completed**: 
- [x] T130: Outcomes 1 and 2. Outcome 1 on row 38 (`pickup-hopper`, 100-12, raised by 9004) - `Pending` -> accept as `johnk` (6229) -> `Accepted`/`assigned=6229`/`accepted_utc=14:39:52` -> complete -> `Completed`/`completed_utc=14:39:56`, card left the list. Outcome 2 on row 39 (`deliver-wrong-coil`, 100-12, raised by 9004) - accept `14:40:38` -> give back -> exactly one row, back to `Pending`, `assigned_material_handler=NULL`, `released_utc=14:40:42` -> second accept -> complete -> `Completed` `14:40:50` (FR-042).
- [x] T131: Outcome 3. As `johnk` (6229) the raiser on row 47 (`pickup-riser-table`, V100-34, Vits Drive) the card offered `accept`,`cancel`; the "Cancel this request?" dialog showed its message and reason box; confirming left `status=Canceled`, `canceled_utc=14:47:26`, `cancellation_reason='Operator no longer needs the riser table.'`, `canceled_by_employee_number=6229`, and the card left the list. As `test.setup` (9006) row 33 (`deliver-dunnage`, 100-1806, raised by 9001) offered `accept` alone - no cancel affordance - and stayed `Pending`.
- [x] T133: Outcomes 5 and 6. Outcome 5 on row 31 (`deliver-die`, 100-7, raised by 6229) - `johnk` accepted (`14:41:45`, assigned 6229), gave back (`released_utc=14:42:17`, assigned NULL), then `test.setup` (9006) accepted: `assigned=9006`, 6229 gone. Outcome 6 on row 32 (`pickup-coil`, 100-6, raised by 9001) - `test.admin` (9001) self-accepted (`14:44:07`, assigned 9001) and self-completed (`14:44:28`), so FR-041 self-accept is permitted.
**Tasks Remaining in Story**: None - the unit is complete. 6 tasks remain in the feature: T139, T164, T165, T174, T184, T188.
**Commit**: `feat(004-unified-card-item-picker): US2 request-handling outcomes 1, 2, 3, 5 and 6` (this entry's own commit, carrying `tasks.md` and `progress.md`)
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T130, T131 and T133 ticked with their evidence blocks)
- specs/004-unified-card-item-picker/progress.md (this entry, plus eight new `## Codebase Patterns` bullets)
**Learnings**:
- **A `ContentDialog`'s buttons have real AutomationIds - `PrimaryButton`, `SecondaryButton`, `CloseButton`.** The first driver matched on "empty AutomationId", which selected the *card's* own "Cancel request" button (same visible Name), so the confirmation never fired and the row stayed `Pending` while the log said the click succeeded. Match Name **plus** set membership. The reason box is `AutomationId=''` / `Name='Reason (optional)'` - match the Name, because the shell's search box owns the id `TextBox`.
- **The dialog is drawn as a `Window` covering the whole app**, so a descendant text dump returns the shell behind it. Bound the read to `confirmButton.Y - 200` .. `confirmButton.Y`.
- **The card list is virtualized, and `ScrollPattern` throws once the list fits the viewport.** Skip `Infinity`/`NaN` rectangles, scroll with `LargeIncrement` until the percentage stops moving, and treat "Operation is not valid due to the current state of the object" as "everything is already visible".
- **`MainWindowHandle` points at the splash window first** (title `WinUI Desktop`, no UIA children). Poll for a shell AutomationId instead of sleeping, and never locate the window by title - the title is not stable.
- **A helper inside a function that returns a value must use `Write-Host`.** `Log` used `Write-Output`, so `return $root` returned an array and `Find-MtmById` got a string. Run the driver with `-File`, never dot-sourced, or the `switch` value flows into the caller's pipeline.
- **The cancel affordance is drawn from `CanCancelRequest`, which is `(isRequester && CanRequesterCancel(order)) || isAssignee`.** A bystander sees `accept` alone - that is the whole of "a non-raiser cannot cancel" at the screen. But an **assignee** does see `cancel` on a request they did not raise (observed on row 38 after `johnk` claimed a request `9004` raised), and `CancelRequestAsync` routes that through `TransitionStatusAsync`, which performs no ownership check. `TransitionStatusAsync`'s only guard is `p_expected_status`; the ownership rule lives entirely in `CanCancelRequest` plus `CancelOwnRequestAsync`.
- **The give-back does not re-stamp `accepted_utc`, and `released_utc` survives a re-claim.** Row 39's `accepted_utc` still read the first claim's `14:40:38` after the second accept, and `released_utc` stayed set. FR-042 asks for neither, so this is recorded rather than fixed.
- **The request table's ids are not contiguous.** 37 rows: `1-18, 28, 29, 31, 32, 33, 35-43, 47-51`. Ids `19-27, 30, 34, 44, 45, 46` are absent. T129's nineteen rows are exactly the nineteen listed, so that population is intact - but ids `30` and `44`, named by the **already-ticked** T132 and T134, do not exist, and neither of those two tasks carries an evidence block in `tasks.md`. Flagged here rather than smoothed over; a later reader must not treat the gap as a lost row.
- **MySQL session time is UTC+5 on this host** (`14:39` when the wall clock read `09:39 -05:00`), and `NOW()` reports `Central Daylight Time`. The stored `*_utc` values are therefore not comparable to local time. Recorded as a fact to avoid a future false "clock bug" report.
- **Next iteration's scope.** 6 tasks remain: Phase 9's T139 (the criteria walk), Phase 11's T164 and T165, Phase 12's T174, Phase 13's T184 and T188.
---
## Iteration 8 - 2026-09-20
**User Story**: Phase 9 Wave 9 gate - T139, the Success Criteria walk (SC-013 … SC-020)
**Tasks Completed**: 
- [x] T139: Walked all eight Success Criteria against the evidence each one names, and corroborated the four the store can answer directly against the live `mtm_waitlist` database. **Closed from recorded evidence:** SC-014 (the 19 in-scope Items raised once - rows 28-51 hold 19 rows with 19 distinct `item` values, a set equality against the 19 in-scope rows of the 23 in `waitlist_request_item_configs`), SC-015 (10 accounts in `core_users_profiles`, all active; 9 distinct employee numbers because `johnk`/`jkoll` share 6229; all 8 roles in `auth_roles_catalog` reached through `auth_roles_assignments`), SC-016 (0 rows matching `900-%` in either `setup_work_centers_catalog` or `setup_active_jobs`; the 7 prepared situations on 100-3/100-6/100-7/100-18/100-1806 and V100-33/V100-34, plus the live save on 100-12), SC-020 (FR-045, confirmed **last**: the 19 rows and the 10 accounts still present), and the T135/T136 build and test gates (`0 Warning(s) 0 Error(s)`; `total: 1168, failed: 0, succeeded: 1141, skipped: 27`). **Reported as unproven, not asserted:** SC-013 (rests on T138, ticked with no evidence block), SC-018 (rests on T132; the store shows 0 overdue batch rows and 0 batch rows past their derived deadline, so only the negative half is checkable), SC-019 (rests on T134), and with them outcomes 4 and 7 of SC-017 - five of its seven outcomes carry evidence blocks (T130, T131, T133). **No suite was re-run** (T139 forbids it and no code changed) and **no application was started**.
**Tasks Remaining in Story**: None - the unit is complete. 5 tasks remain in the feature: T164, T165, T174, T184, T188.
**Commit**: `docs(004-unified-card-item-picker): record iteration 8 of the ralph loop - T139 success-criteria walk` (this entry's own commit, carrying `tasks.md` and `progress.md`)
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T139 ticked with its per-criterion evidence block; the Phase 9 checkpoint paragraph amended to name the three criteria the walk could not close)
- specs/004-unified-card-item-picker/progress.md (this entry, plus three new `## Codebase Patterns` bullets)
**Learnings**:
- **A criteria walk is mostly a search for absences, and the absence has to be stated.** SC-014 and SC-020 are the only criteria a count can settle; SC-016 is an absence of fixture rows, SC-013 is an absence of a recorded run, and SC-018 is an absence of an overdue row. Reading the store answered "is anything overdue?" with a clean no - which proves nothing about whether the overdue *path* works, because a population with no past-due row can never exercise it. Report that distinction rather than ticking on the negative.
- **The tick's date is the tell.** `git log -S '<task id>'` dates a tick, and dating T132/T134 to commit `45a9984` (2026-09-13) shows they were marked done on the day Phase 9 was written, before T129 created the population they claim to have exercised. A tick that predates its own subject cannot be evidence, and no amount of re-reading `tasks.md` will change that.
- **Amend a checkpoint claim you just falsified; do not leave it standing beside the finding.** The Phase 9 checkpoint asserted SC-013 … SC-020 were "provable from this phase alone". The walk disproved that for three of them, so the paragraph now says which three and why, rather than contradicting the evidence block a few lines above it.
- **Next iteration's scope.** 5 tasks remain, all running-app UI gates or the owner's action: Phase 11's T164 (the dunnage step) and T165 (the owner's live-database reinstall, explicitly reserved to them), Phase 12's T174 (the pickup-die/deliver-die card lines), Phase 13's T188 (die page behaviour) and T184 (a die request from 100-7). SC-013, SC-018 and SC-019 also need running-app gates - a sign-out restart, a real expiry, and two instances acting at once - so a future iteration that can sign in and drive the Debug build closes both those criteria and T164/T174/T184/T188 together.
---
## Iteration 9 - 2026-09-20
**User Story**: Phase 11 Wave 6 gate - T164, the dunnage step driven in the running app
**Tasks Completed**: 
- [x] T164: Walked the dunnage step end to end twice in the running Debug build, once down each answer path, against the local `mtm_waitlist` @ 127.0.0.1. On `100-1806` the step rendered `New Request - Choose Dunnage | Work Center: 100-1806 | Which dunnage do you need from the material handlers?` between Item and Preview, with `NewRequestDunnagePage_DunnageTiles` holding one card for the job's single assigned part (`Rack, 24 x 36 wire` / `DNG0007788`); on `V100-33` the same step offered `Tote, collapsible` / `DNG0007789`. The page has **no** Continue button - only `NewRequestDunnagePage_BackButton` and `NewRequestDunnagePage_SubstituteButton` - so the card click is the advance, and it reached Preview reading `Detail | 12 x 8 x 4 (SW)`. `Use substitute…` opened the existing `SetupDunnageImageSearchDialog` (`Search Dunnage Parts by Image`, with its Refresh, Show-all and Close controls) and reported the step's own substitute button `enabled=False` while it was open; the `12 x 8 x 4 (SW)` tile chosen there carried through Confirm and Submit (`Request completed | Request submitted.`) into row **52** of `waitlist_requests_queue` (`100-1806` / `pickup-dunnage` / `input_value = 12 x 8 x 4 (SW)`). The assigned-card path was then walked on `V100-33` into row **53** (`input_value = DNG0007789`), and both were found back on the Waitlist by their second line - `Pickup > 12 x 8 x 4 (SW)` and `Pickup > DNG0007789` - which is FR-051 reading the captured answer back.
- [ ] T165: **Not ticked, deliberately.** Its two non-reinstall halves were read back and are recorded under the task: both dunnage rows are live in the local store with `list`/`dunnage` intact on the `source: answer` field, and `AllSeeds.sql` (833-841, 898-906) is identical to `seed_waitlist_request_item_configs/create.sql` (121-129, 186-194) for both rows. The reinstall the task names is the owner's action, so the tick stays theirs.
**Tasks Remaining in Story**: 1 - T165, reserved to the owner. 3 tasks remain in the feature after it: T174, T184, T188.
**Commit**: `docs(004-unified-card-item-picker): record iteration 9 of the ralph loop - T164 dunnage UI gate` (this entry's own commit, carrying `tasks.md` and `progress.md`)
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T164 ticked with its per-clause evidence block; T165 given a read-back note that leaves the tick to the owner; the Phase 11 checkpoint paragraph amended now that the UI walk is recorded)
- specs/004-unified-card-item-picker/progress.md (this entry, plus eight new `## Codebase Patterns` bullets)
**Learnings**:
- **A `GridView` card answers `InvokePattern`, and that is the whole of driving `ItemClick` headlessly.** The tiles are `ControlType.ListItem` under a `ControlType.List`, and `GetCurrentPattern([InvokePattern]::Pattern).Invoke()` takes the same route the user's click does. The earlier belief that a templated card needs synthetic mouse input was wrong, and it mattered: the page has no Continue button, so without `Invoke` the step cannot be advanced at all.
- **A synthetic mouse click is silently a no-op while another window holds the foreground.** `SetCursorPos` + `mouse_event` reported no error and changed nothing with a File Explorer window focused, which is the worst kind of failure - the driver's log says the click succeeded. `InvokePattern` has no such dependency.
- **The requirement "Continue is reachable only after a part is chosen" is implemented as "there is no Continue".** `NewRequestDunnagePage` carries only Back and `Use substitute…`, so a driver that waits for a Continue button times out on a page that is behaving exactly as specified. Read the requirement's *intent* against the markup before treating an absent control as a defect.
- **A duplicate-active refusal is evidence about the guard, not a failure of the walk.** The first assigned-card attempt on `100-1806` returned `Matching request already active | An active matching request already exists.` because row 52 already held that `(building, work centre, item, answer)` - FR-054 doing its job. The fix is to change the *scenario* (a different facility carrying dunnage), never to delete the row to make the walk pass.
- **Two answers to the same question can each be the value the card reads back, and both must be walked.** The substitute path and the assigned-card path write the same `input_value` column by different routes - `12 x 8 x 4 (SW)` from the receiving catalogue, `DNG0007789` from the job's own assignment - and the Waitlist card's second line reads the column, not the route. Proving one proves nothing about the other.
- **A store that grows while you test is not a store that drifted.** Driving the real app wrote rows 52 and 53 into the live local `mtm_waitlist`, taking `waitlist_requests_queue` from 37 to 39. These are live-store writes from exercising the application, not seed changes, and a later iteration must not read the shift as seed drift - the same trap the earlier "the live store already diverged from the seeds" note records.
- **`Get-Process X | Stop-Process -Force` is refused; stopping by id is not.** Enumerate and stop individually: `Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }`. Confirmed 0 instances afterwards, so no orphan holds the store's connections.
- **`\G` does not work through `mysql -e` on this build.** It exits 1 and prints nothing, which reads like an empty result rather than a syntax limit. Use `-N -B` and read columns positionally.
- **Next iteration's scope.** 4 tasks remain, every one of them either a running-app UI gate or the owner's action: Phase 11's T165 (the owner's live-database reinstall - its verifiable halves are done and recorded, so only the reinstall is left and the agent must not run it), Phase 12's T174 (the pickup-die/deliver-die card lines, which needs a fresh raise from `100-7` because the existing cards were not raised through the flow being proved), and Phase 13's T188 (the die page behaviour) and T184 (a die request from `100-7`). SC-013, SC-018 and SC-019 also still need running-app gates - a sign-out restart, a real expiry, and two instances acting at once - and the Debug build now drives cleanly to the shell with no credentials, so a future iteration can close those three criteria and T174/T184/T188 in the same way T164 was closed here.
