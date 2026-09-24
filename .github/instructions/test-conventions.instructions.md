---
applyTo: "MTM_Waitlist.Tests/**"
---

# Test Conventions — `MTM_Waitlist.Tests`

The suite is the evidence gate for every change in this repository: a change is complete only when the solution
builds with `0 Warning(s) 0 Error(s)` **and** this suite reports `Failed: 0`. The repository's checklist rules
forbid marking a task done without that proof. The suite's work is threefold — pin the behaviour of view models
and services, police the architectural rules a compiler cannot see (retired symbols and wording, inline SQL,
placeholder resources, XAML policy), and keep the data-access boundary honest (internal stores always live; only
external reads are cached).

## Tech stack

- C# / .NET 10 (`net10.0-windows10.0.19041.0`), **x64 only** (`Platforms=x64`, `PlatformTarget=x64`,
  `RuntimeIdentifier=win-x64`)
- **MSTest 3.7.0** (`MSTest.TestAdapter` + `MSTest.TestFramework`) on `Microsoft.NET.Test.Sdk` 17.13.0
- `coverlet.collector` 6.0.2 for coverage collection — opt-in, not part of the gate
- **No mocking library.** No Moq, NSubstitute, FakeItEasy or AutoFixture anywhere in the suite. Fakes are
  hand-written: purpose-built no-op services (`NoOpAppLifecycleService.cs`, `NoOpSetupDialogService.cs`) and small
  recording stubs declared in the test class that needs them
- The app project declares `InternalsVisibleTo("MTM_Waitlist.Tests")`, so tests reach `internal` seams directly
- The project is **deliberately nested inside the repository** so it stays under version control and on the CI
  path: `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`, `UseWinUI=false` with `EnableWindowsTargeting=true`
- It copies real application content into its own output — every `Database/**/Queues/**/*.sql` script — so
  integration tests load the same artifacts the app ships. (The retired `Assets/Config/waitlist-request-types.json`
  catalog is no longer part of that set: FR-023 retired it with the type/subtype vocabulary.)
- There is no Node, npm, TypeScript or browser-automation layer in this repository

## Test code style

- File-scoped namespaces; `Nullable` and `ImplicitUsings` enabled
- `[TestClass] public sealed class <Subject>Tests`; each case is `[TestMethod] public void` or `public async Task`
- **The name is the specification**: `<Method>_<Scenario>_<Expectation>` — for example
  `Constructor_SelectsFirstBuildingByDefault`, `AcceptAsync_NotAvailable_ReturnsNull`,
  `SelectSequenceAsync_KeepsTheJobsCoilAtItsWorkCentreLocation`,
  `WaitlistViewViewModel_RefreshesActiveList_WhenRequestIsSubmitted`. A test whose scenario needs a comment to
  explain it is misnamed
- Arrange / Act / Assert with one behavioural claim per test. No assertion-free tests, no tests that merely
  construct an object
- Comments explain a non-obvious constraint (thread affinity, a machine-specific quirk, why a guard exists) —
  never what the next line does
- Files mirror the production project: `MTM_Waitlist.Tests/Module_<Area>/…` (`Module_Waitlist`, `Module_Settings`,
  `Module_Mock`, `Module_Mock_Service`, `Module_Setup`, `Module_Core`, `Module_Startup`, `Module_Shared`). The
  older `Core/`, `Models/`, `Services/` and `ViewModels/` folders stay as written; **new tests follow
  `Module_<Area>`**. The namespace mirrors the production namespace under test

## Architecture patterns

- **Ports and fakes, not mocks.** Constructor injection is the only seam; a test supplies a hand-written fake
  that records calls or returns canned values. "The service was called with X" is asserted with a recording
  stub, never a verification API
- **`[TestInitialize]` for fixtures** — 21 classes use it, some with `Task TestInitializeAsync()` for async
  setup. Fixtures create their own temp files/folders and clean up; nothing writes into the repository tree
- **Audit tests are first-class.** `Module_Mock/RetiredSymbolAuditTests.cs` scans `.cs/.xaml/.resw/.sql/.csproj/
  .json` for retired symbols, retired wording and placeholder resource values; its pattern set is the extension
  point — extend it rather than inventing a second mechanism. Inline SQL is policed the same way
- **A regression test must fail first.** When a check accompanies a defect fix, run it against the pre-fix code
  and record that failure; a check that already passed proves nothing
- Markup and policy assertions read XAML and `.resw` files as text (unique `x:Uid`, no `Button` without a
  `Command`, no placeholder URI), because the XAML compiler cannot see those rules

## Running the suite

```powershell
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

- Kill stale processes first (`MTM_Waitlist`, `Microsoft.UI.Xaml.Markup.Compiler`, `VBCSCompiler`, `MSBuild`) and
  set `MSBUILDDISABLENODEREUSE=1`
- **Never build the solution in parallel** — the XAML compiler races on its own intermediate files, so solution
  builds use `/m:1 /nodeReuse:false`
- `WMC9999` is a masked XAML error to be surfaced, not ignored. `PRI175`/`PRI224` mean a running app or stale
  `*.pri` files, not a code error
- Baseline at the last recorded run: **787 total — 768 passed, 19 skipped, 0 failed**, against 770
  `[TestMethod]` declarations in 111 `[TestClass]` files (the difference is data-driven cases)

## Environment-gated tests

- Integration tests that need a live environment are opt-in and use `Assert.Inconclusive` when it is absent —
  10 guards today, for example `INFOR_VISUAL_SQL_*` credentials for live Infor Visual checks and a live MySQL for
  the stored-procedure integration tests
- A skipped test is recorded as **environment-gated**: never reported as passing, and never "fixed" by weakening
  the guard
- **Never weaken a gate to go green**: no deleting a failing test, no adding `[Ignore]` (the suite has none
  today), no new skip to dodge a real failure, and no `Assert.Inconclusive` where a real assertion is possible
- Tests must not depend on wall-clock time, machine locale or the developer's profile path; resolve paths from
  `AppContext.BaseDirectory` or a temp folder
- Never require elevation, and never leave a process running — a stray `MTM_Waitlist.exe` locks the build output

## Coverage priority

- Cover the seams that carry rules — view-model state and commands, service transitions, stored-procedure
  shapes, and the audit/policy scans — over getters and trivial constructors
- Add to the existing test class for a subject instead of creating a parallel one
- **UI-only behaviour is not covered by this suite.** What renders, window geometry and navigation are verified
  with the PowerShell UI-Automation recipe in `.github/instructions/winui3-ui-automation.instructions.md`, which
  needs a signed-in session. The in-app XAML inspector (`xamlmcp`) is not wired in this repository, so do not
  plan around it

## Domain context that shapes expectations

- **The application**: WinUI 3 on the Windows App SDK, self-contained and **unpackaged** (no package identity).
  Views and XAML live in the app project; testable non-view code lives in per-module class libraries
  (`MTM_Waitlist.Core`, `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.Waitlist.NewRequest`, `MTM_Waitlist.Settings`,
  `MTM_Waitlist.Setup`, `MTM_Waitlist.Mock`, `MTM_Waitlist.Startup`, …). MVVM uses the CommunityToolkit.Mvvm
  source generators (`[ObservableProperty]`, `[RelayCommand]`); view models derive from `ObservableRecipient` and
  implement `INavigationAware`, loading data in `OnNavigatedTo`
- **Stores**: four MySQL databases — `mtm_waitlist` (the requests), `mtm_wip_application_winforms`,
  `mtm_receiving_application` and `mtm_mock` (the external-read cache) — plus Infor Visual, read-only, over SQL
  Server
- **The rule that shapes most expectations**: the three internal stores are always read and written **live**, and
  a failure is reported as a per-screen unavailable state carrying
  `Store`/`LastAttemptUtc`/`RetryCount`/`NextRetryUtc` plus a manual retry — never substituted with sample rows
  and never turned into a persistent banner. Only Infor Visual reads fall back, only to the `mtm_mock` mirror,
  only automatically on unreachability, and a reachable-but-empty live result is a real answer that is never
  replaced by cached data. There is no manual demo/mock mode; the retired-symbol audit fails the build if one
  returns
- **Data access**: every operation goes through a stored procedure — no inline or hard-coded SQL in application
  code — and every schema artifact ships `create.sql` + `rollback.sql` in the same change
- **Terminology**: request (never ticket), work centre, building (Expo Drive / Vits Drive), coil, dunnage,
  handler, requester, remaining time / overdue, request type and subtype, waitlist

## Systems the tests stand in for

- **Infor Visual** — the read-only external ERP over SQL Server. `Database/InforVisual/Queues/**` holds the query
  scripts; `sp_visual_<shape>_get` reads the cached mirror and `sp_visual_<shape>_refresh` refreshes it.
  Unreachable on most developer machines, so tests must skip cleanly
- **MySQL** — the four databases above; `Database/StoredProcedures/**` and `Database/MTMReceivingApp/**` hold the
  procedures the tests exercise. The app resolves the host through `MySqlHostFallback` (keep the configured host
  if it answers, else `localhost` if that answers, else leave the string unchanged so the failure is reported) —
  so tests must never assert a literal `Server=` value; derive the expectation the same way the production code
  does
- **`MTM_Waitlist.Mock.Service`** — the on-host helper that refreshes the mirror and serves it over HTTP. Its own
  integration tests under `Module_Mock_Service/` cover the API, the refresh/backup engines and role-based
  authorization, and must not require the service to be running
- **Test doubles** for all of the above are hand-written fakes in the test project. A fake that silently reports
  success for an unreachable store is a defect: the unavailable-state behaviour is part of the specification
- The source of truth for expectations is the feature's `spec.md`, `plan.md`, `tasks.md` and the `defects/`
  records — not the current implementation. When they disagree, the specification wins and the test should fail
