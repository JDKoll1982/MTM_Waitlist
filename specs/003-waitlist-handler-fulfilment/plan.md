# Implementation Plan: Waitlist Handler Fulfilment and Urgency Ordering

**Branch**: `003-waitlist-handler-fulfilment` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-waitlist-handler-fulfilment/spec.md`
**Research**: [research.md](./research.md) | **Model**: [data-model.md](./data-model.md) | **Contracts**: [contracts/](./contracts/)

## Summary

The waitlist lifecycle already exists and is tested end to end — `IWaitlistRequestService` accepts,
completes, releases, cancels and notes a request through `sp_waitlist_request_status_update`, writes
an audit entry for each, and `RequestActionPolicy` already decides who may do what. Nothing on a
screen calls any of it, and the two action buttons the card draws have no command. This feature is
wiring, not construction: it puts the actions on the card's existing action area — the card in the list is
the one surface that offers them, and the request detail page offers none (FR-025) — gates each one through
the existing policy, orders the list with the existing `UrgencyCalculator.OrderMostUrgentFirst`, and reports
a refusal in plain language. The card's action area, its 96×96 image, its four metadata rows, its per-type
detail grid and its compact status pill are unchanged.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0-windows10.0.19041.0`), x64 only  
**Primary Dependencies**: WinUI 3 on Windows App SDK 2.3.x (self-contained), CommunityToolkit.Mvvm 8.4.2 (source generators), Microsoft.Extensions.DependencyInjection, MSTest 3.7.0  
**Storage**: MySQL `mtm_waitlist` (live). No table and no seed is added. As planned, every action reuses
`sp_waitlist_request_status_update` and the audit path already behind it. **Revised after implementation:** two
procedures did change, both recorded in the tasks that made them — `sp_waitlist_request_audit_list` was added
because `sp_waitlist_request_audit_insert` had no read half, so the history could only ever show the current
session (T037); and `sp_waitlist_request_list` gained `last_message_utc` because the new-message indicator was
comparing against `updated_utc`, which moves for lifecycle changes as well as messages (T044). Each ships with
its `create.sql` / `rollback.sql` pair and its `AllSPs.sql` block, per constitution III.  
**Testing**: MSTest 3.7 in `MTM_Waitlist.Tests`, folders mirroring the production modules; the view models are constructed directly with a fake service and a no-op prompt double, so no signed-in shell is needed for the unit gates  
**Target Platform**: Windows 10 19041+ / Windows 11, x64, unpackaged and self-contained (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`)  
**Project Type**: desktop application — the WinUI 3 app is the composition root (Views, XAML, DI, navigation) with testable non-view code in per-module class libraries  
**Performance Goals**: none added. The list gains one ordering pass over rows it already holds and one role check per load, not per binding. The per-sub-type max-allotted lookup used by ordering is memoised per load so a list of N rows costs one lookup per distinct sub-type, not N.  
**Constraints**: the frozen card anatomy (spec Verbatim Constraints) must not move a single dimension; every new user-visible string is resource-backed and localized; no control is drawn whose activation has no effect; the solution build and test run single-threaded (`/m:1 /nodeReuse:false`) because the build otherwise races on the XAML compiler; `WMC9999` is a masked XAML error, never an environment problem  
**Scale/Scope**: 2 view models, 1 row model, 1 policy class, 1 new prompt seam plus its app-side implementation and DI line, 1 card XAML file, 1 detail page, the resource file, and the tests that prove the gates. **Revised after implementation:** the deliverable also grew a status palette and its converter (`MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs`, `Converters/RequestStatusToBrushConverter.cs`), an image-resolution policy (`Helpers/RequestImagePathPolicy.cs`), two database artifacts (T037, T044), and the second-half fixes recorded at the end of this plan. The documentation corrected grew from two files to five: the defect file, `CHANGELOG.md`, and the defects and changelog entries the later passes required.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
| --- | --- | --- |
| I. Spec-First, Verified Delivery | Spec written before code; work driven by `tasks.md`; nothing ticked until verified | **PASS** — spec at `specs/003-waitlist-handler-fulfilment/spec.md`, this plan, and a task list to follow; each task is ticked only against the gates in `contracts/verification-gates.md` |
| II. Live Data Integrity | Internal stores always read and written live; no demo/mock mode; no fabricated value in place of absent data | **PASS** — every action writes `mtm_waitlist` live through the existing stored procedure; FR-014 forbids substituting a value the request does not carry, and `FabricatedValueGuard` / `RetiredSymbolAuditTests` already fail the build on a substitute |
| III. Stored-Procedure-First | Every data operation through a stored procedure; paired `create.sql`/`rollback.sql` for any schema artifact | **PASS** — the actions call the existing `sp_waitlist_request_status_update` through `ExecuteStoredProcedureNonQueryAsync`, and no inline statement text is introduced anywhere. **Revised after implementation:** the planned "no schema artifact" claim did not survive contact with the feature — `sp_waitlist_request_audit_list` was added (T037) and `sp_waitlist_request_list` was changed (T044), each with its paired `create.sql`/`rollback.sql` and its `AllSPs.sql` block, so the principle is still met but was not untouched. `InlineSqlAuditTests` remains the enforcement. |
| IV. MCP-First, Grounded Decisions | Decisions grounded in Serena/Context7/Microsoft Learn rather than recall | **PASS** — research R1 is settled by the WinUI binding documentation retrieved through Context7 (only `TemplatedParent` is documented for `RelativeSource`; no `FindAncestor` example) together with a repository-wide search finding zero `RelativeSource`/`FindAncestor` usage. Microsoft Learn MCP was reported disabled by the host during this run; the shortfall and the substitute source are recorded in `research.md` R1. |
| V. WinUI 3 Platform Conformance | `Microsoft.UI.Xaml` only; MVVM Toolkit generators; localized resource-backed text; converters registered in `App.xaml`; fluid layout | **PASS** — `[RelayCommand]`/`[ObservableProperty]` generators, `ContentDialog` with an explicit `XamlRoot`, new `.resw` keys with `GetLocalized`, no new converter, no layout change outside the frozen action area |
| VI. Evidence-Based Verification Gates | Clean solution build and green suite; `WMC9999` treated as a real error | **PASS** — `contracts/verification-gates.md` G1–G8, including the gate combinations that must fail before the wiring exists |
| Security & Secrets | No hardcoded credentials; nothing credential-shaped logged or displayed | **PASS** — the feature reads no credential; it uses the signed-in employee number and role already resolved at startup |
| External Integration & Cache Boundaries | Infor Visual never written; cached data is fallback only | **PASS** — no Infor Visual path is touched; the inventory-location read on the detail page is unchanged |
| Documentation & Extensibility | Affected documentation updated in the same change, stale references removed | **PASS** — `defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` moves to `FIXED` with its proof, and `CHANGELOG.md` gains the two entries the defect file already claims exist |

The post-Phase-1 re-check reaches the same result: the design adds no principle-affecting mechanism,
no database artifact, and no new cross-cutting abstraction.

## Project Structure

### Documentation (this feature)

```text
specs/003-waitlist-handler-fulfilment/
├── spec.md                     # Feature specification
├── plan.md                     # This file (/speckit.plan command output)
├── research.md                 # Phase 0 output — resolved decisions
├── data-model.md               # Phase 1 output — row action state, urgency, audit view
├── contracts/                  # Phase 1 output — action, gate and verification contracts
│   ├── action-contracts.md
│   └── verification-gates.md
├── checklists/requirements.md  # Spec quality checklist
└── tasks.md                    # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
MTM_Waitlist.Core/
└── Services/
    └── RequestActionPolicy.cs              # + CanViewerHandleRequests(role) and the handler role set

MTM_Waitlist.Waitlist.View/
├── Models/
│   └── SampleOrder.cs                      # + action state, gate flags, action commands, urgency
├── Services/
│   └── IWaitlistRequestActionPrompt.cs     # NEW — confirm / cancel-reason seam (no WinUI in the VM)
└── ViewModels/
    ├── WaitlistViewViewModel.cs            # handler commands, role gate, most-urgent-first ordering
    └── WaitlistViewDetailViewModel.cs      # note draft + save, audit history rows (no action commands — FR-025)

Module_Waitlist/
├── Controls/
│   └── WaitlistLineCardView.xaml           # the frozen action area gains gated icon buttons
└── Views/
    └── WaitlistViewDetailPage.xaml         # message editor + request history (no action bar — FR-025)

Services/
├── WaitlistRequestActionPrompt.cs          # NEW — app-side ContentDialog implementation
└── DependencyInjection/
    └── ServiceRegistrationExtensions.cs    # + one registration line

Strings/en-us/
└── Resources.resw                          # + the new visible strings

MTM_Waitlist.Tests/
├── NoOpWaitlistRequestActionPrompt.cs      # NEW — test double for the prompt seam
├── Core/Services/RequestActionPolicyTests.cs
└── Module_Waitlist/
    ├── Controls/WaitlistLineCardMarkupTests.cs     # revised: gated, not absent
    ├── ViewModels/WaitlistViewViewModelActionTests.cs   # NEW
    └── ViewModels/WaitlistViewDetailActionTests.cs     # NEW
```

---

## Post-implementation revision

The plan above is the plan that was built. The shipped feature then went through four further passes, all of
them prompted by using the build rather than by reading it, and all four changed either a requirement or a
database artifact — so they are recorded here rather than left to the diff. The tasks for each are in
`tasks.md` (T031; T032–T036; T037–T041; T042–T049) and the gates that verified them are in
`contracts/verification-gates.md` (G5, G11).

| Pass | What it changed | Requirement |
| --- | --- | --- |
| Concurrent claims | The store arbitrates first-claim-wins; the losing handler is shown an acknowledgement-required warning naming the request as taken, instead of a silent no-op | FR-024 |
| First fix pass | Actions moved off the request page onto the card; the page reloads itself on a 30-second cadence; the new-message indicator; the missing audit read half | FR-025, FR-026, FR-027 |
| Second pass | The message box sends on the first click and survives a refresh; one history entry per note; the indicator fires for messages only; anyone signed in may write; the history names the sender and hides the event type; Accept and Complete differ in colour and icon; one badge colour per status | FR-012, FR-013, FR-028, FR-029, FR-030 |
| Third pass | The card keeps its own image instead of taking the resolver's "no image available" placeholder, and the substitution is logged | FR-031 |

**One plan-level claim did not survive.** The plan said no stored procedure would be added or changed. Two
were: `sp_waitlist_request_audit_list` (T037) — without it the history could only ever show the current session,
which is absence rendered as nothing — and `sp_waitlist_request_list`, which gained `last_message_utc` (T044)
because `updated_utc` moves for lifecycle changes, so every accepted or completed job raised the new-message
marker as though somebody had spoken. Both ship with paired `create.sql` / `rollback.sql` and an `AllSPs.sql`
block.

**The four passes were journaled late, and the gap is worth knowing about.** The spec had already reached
`status: completed`, and the companion writer refuses to regress or append to a shipped spec, so the first
capture after the passes was refused (`already at status=completed; not regressing to implement`) and their
task boxes were marked in `tasks.md` directly. On 2026-09-13 the spec was **reopened**; the capture then synced
**8 new task events (49/49 complete)** and closed at `status: implemented`, and the companion record also
carries the requirement coverage (FR-001 … FR-031), the decisions, the verification evidence and the open
concerns for this work. The lesson is narrow but real: a shipped spec is a closed book to its own journal, so
touches made after the terminal status have nowhere to land unless a human reopens it.

**Final gates (2026-09-13):** `0 Warning(s) 0 Error(s)` and `Failed: 0, Passed: 888, Skipped: 19, Total: 907`.

defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md   # status + proof
CHANGELOG.md                                                  # the entries the defect file already claims
```

**Structure Decision**: every production edit lands in the module that already owns the code — the
policy in `MTM_Waitlist.Core`, the row and view models in `MTM_Waitlist.Waitlist.View`, the markup in
`Module_Waitlist`, and the one dialog implementation in the app's composition root beside
`SetupDialogService`, which is the existing precedent for a module-interface/app-implementation pair.
The test double mirrors `NoOpSetupDialogService`.
