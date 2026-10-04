# Implementation Plan: Identifier-First Sign-In and a Faster Launch

**Feature**: `specs/011-identifier-first-signin/spec.md`
**Branch**: `011-identifier-first-signin`
**Size**: oversized

## Scale Note

This change spans roughly sixty files across four subsystems: the launch module (`MTM_Waitlist.Startup`), the
launch surfaces (`Module_Startup/Views`), the external-read cache (`MTM_Waitlist.Mock` and
`MTM_Waitlist.Mock.Service`), and the database (a new read shape's full artifact set plus one new stored
procedure). Nothing here is a one-file change, and the database and service work is real work rather than a
configuration line. A reader should watch two things in particular: that every wait keeps its own stated maximum
while a new launch-wide budget is added on top, and that the cache is never refreshed from the application.

## Summary

Sign-in becomes two stages: an identifier first (a user name, or exactly four digits for an employee number),
then a credential. An employee number the store does not hold is looked up in the plant system, and a person the
plant system knows is set up on a PIN they choose, with the lowest rank read from the store. The four separate
launch windows become stages of the single main window, which maximises and reveals its navigation panel only
when the launch hands over. The launch gains a 45-second budget on top of each step's existing maximum, runs the
session checks and the local reads concurrently, and starts the picture cache at the beginning instead of near
the end.

The technical approach reuses what the rebuilt launch already provides: the step catalogue stays the sequence,
the runner keeps holding each step to its own maximum, and the pipeline keeps routing. Concurrency is expressed
as explicit parallel groups in the pipeline, not by rewriting the runner. The plant-system lookup is a sixth read
shape handled exactly like the five that exist, so the in-app fallback, the service's refresh engine and the
startup validation all keep working through their existing catalog-driven paths.

## Project Structure

```text
MTM_Waitlist.Startup/
  Services/
    LaunchPipeline.cs                 # sequential walk -> grouped/concurrent walk; launch-wide budget
    LaunchStepCatalog.cs              # step order (picture-cache early); retarget read-remembered-sign-in
    LaunchStepRunner.cs               # unchanged contract: one step, one stated maximum
    LaunchActivityFeed.cs             # feed already append-only; no change needed for several lines
    LaunchBudget.cs                   # NEW: the launch-wide 45-second budget and its stated stop
    SignInSteps.cs                    # unchanged credential/session steps
    ConfigurationStep.cs              # ReadRememberedSignInStep target reconciled
    IdentifierSignInService.cs        # NEW: classifies the identifier, store-first, Visual second
    AccountCreationService.cs         # NEW: create profile + credential + rank via one procedure
    PersonIdentityService.cs          # extend with a by-identifier resolve
    RememberedSignInService.cs        # reconcile the key-file contradiction (behaviour unchanged)
    DependencyInjection/ModuleDependencyInjectionExtensions.cs   # register the new services
  ViewModels/
    SignInViewModel.cs                # two stages + set-up panel state machine
    LaunchSurfaceViewModel.cs         # NEW: several concurrent lines in one place
Module_Startup/Views/
  LaunchWindow.xaml(.cs)              # NEW: the one window that hosts every launch stage
  SplashPage.xaml(.cs)                # becomes a stage view, not a window
  SignInWindow.xaml(.cs)              # becomes the sign-in stage view
  MachineSetupWindow.xaml(.cs)        # becomes the setup stage view
  BlockedStateWindow.xaml(.cs)        # becomes the stopped stage view
App.xaml.cs                           # ShowLaunchSurface -> one window, staged
MainWindow.xaml(.cs)                  # nav hidden during launch, maximise on hand-over, re-centre on resize
MTM_Waitlist.Mock/
  Models/VisualReadShapeModule.cs     # + Employee (the new shape's module)
  Services/VisualReadShapeCatalog.cs  # five -> six entries
  Services/VisualEmployeeByNumberFallback.cs   # NEW: the in-app fallback for the sixth shape
MTM_Waitlist.Mock.Service/            # catalog-driven; no engine change beyond the new catalog entry
Database/
  InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql        # NEW live source script
  InforVisual/Queues/Module_Mock/Populations/employee_by_number_population.sql  # NEW
  Mock/Tables/visual_employee_by_number_result/(create|rollback).sql        # NEW
  Mock/Tables/visual_employee_by_number_result_stage/(create|rollback).sql  # NEW
  Mock/Tables/visual_employee_by_number_result_prev/(create|rollback).sql   # NEW
  Mock/StoredProcedures/sp_visual_employee_by_number_get/(create|rollback).sql     # NEW
  Mock/StoredProcedures/sp_visual_employee_by_number_refresh/(create|rollback).sql # NEW
  Mock/AllTables.sql, Mock/AllSPs.sql, Mock/Bootstrap/update_table_descriptions.sql # regenerate
  StoredProcedures/sp_core_account_setup_create/(create|rollback).sql       # NEW: profile + credential + rank
  Database-Ruleset.md                 # five -> six read shapes
MTM_Waitlist.Tests/                   # see contracts/ and tasks
.github/instructions/startup-rebuild.instructions.md   # reconcile the retired-key-file statement
```

**Structure Decision**: no new projects. Every piece lands in the project that already owns its area, so the
existing composition root, the existing module DI extensions and the existing test projects keep working. The
only new *kind* of thing is a read-shape module value, because the sixth shape belongs to neither the setup nor
the waitlist module.

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-first, verified delivery | PASS. Spec and its checklist precede this plan; each task carries its own verification. |
| II. Live data integrity, internal stores never mocked | PASS. Sign-in and account creation read and write the live store; the plant-system fallback is the external-read path only. No demo or mock mode is introduced. |
| III. Stored-procedure-first database discipline | PASS. Account creation is one stored procedure; the new read shape ships get and refresh procedures; every new artifact ships its rollback in the same change. |
| IV. MCP-first, grounded decisions | PASS. The launch, mock and WinUI APIs are checked against Context7 and Microsoft Learn before they are used; the reconciliation item is grounded by reading the code rather than by memory. |
| V. WinUI 3 platform conformance | PASS. One window with staged content uses the existing `WindowEx`/`AppWindow` surface; centring and maximising use the Windows App SDK `AppWindow` API checked against Microsoft Learn. |
| VI. Evidence-based verification | PASS. Every wait's maximum, the budget, the concurrency overlap and the shape count are asserted, and the build guards (`InlineSqlAuditTests`, `RetiredSymbolAuditTests`) are run. |
| VII. Readable, end-user-facing delivery | PASS. The chat reply follows the repo's response-format rules; the launch surface's lines stay plain language. |

No violations, so no Complexity Tracking table.

## Phase 0 Research

See `research.md`. The load-bearing decisions are: how concurrency is expressed without weakening the per-step
maximum, where the sixth read shape's module comes from, how the set-up path avoids the temporary-credential
state, and what the key-file contradiction actually is.

## Phase 1 Design

See `data-model.md` and `contracts/`.

## Re-check After Design

The Constitution Check is re-assessed after `contracts/` is written. No row changes: the design keeps the
procedure-first rule (account creation is a single procedure), keeps the external-read boundary (the sixth shape
is read-only and cached-fallback only), and adds no local role source.
