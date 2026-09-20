# Tasks: Truthful Data and Truthful Controls

**Feature**: `specs/002-truthful-data-and-controls` | **Branch**: `002-truthful-data-and-controls`
**Input**: Design documents from `specs/002-truthful-data-and-controls/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: **Included, and written first.** The specification requires them: FR-032 makes an automated check
per closed defect class mandatory, SC-008 sets the floor at ten new checks, and
`contracts/verification-gates.md` G4 enumerates the twelve checks covering those ten classes. Each check must be confirmed **failing** against the
current code before its fix lands — that failure is the proof the check is not vacuous.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested and delivered on
its own. US1 is the MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US5, matching `spec.md`
- Every task names the exact file it touches

## Path Conventions

Repository root paths, as the app is the composition root and the test project sits inside it:

| Area | Path |
| --- | --- |
| App views/XAML | `Module_Waitlist/Views/`, `Module_Waitlist/Controls/`, `Module_Settings/Views/` |
| View models | `MTM_Waitlist.Waitlist.View/ViewModels/`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/`, `MTM_Waitlist.Settings/ViewModels/` |
| Core services | `MTM_Waitlist.Core/Services/`, `MTM_Waitlist.Core/Activation/` |
| Resources | `Strings/en-us/Resources.resw` |
| Tests | `MTM_Waitlist.Tests/Module_<Area>/…` mirroring the production module — `Module_Waitlist/`, `Module_Settings/`, `Module_Mock/`, `Module_Core/`. The older `MTM_Waitlist.Tests/Core/` grouping stays as written; new tests follow `Module_<Area>` |
| Documents | repo root (`FEATURES.md`, `OPEN-TASKS.md`), `WeekendProject/`, `defects/` |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the baseline and confirm the removal inventory before anything is deleted. No new
project, folder or module is created by this feature.

- [x] T001 Record the baseline gates in `specs/002-truthful-data-and-controls/tasks.md` (Phase 1 notes): `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` (expect `0 Warning(s) 0 Error(s)`) and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` (expect `Failed: 0`, 787 total before this feature). Every later check must show a changed number.
- [x] T002 [P] Confirm the removal inventory against the code before deleting anything: locate every literal and constant named in `data-model.md` §2 and §3 in `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (~560–701), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` (~316–520), `Module_Waitlist/Controls/**` and `Module_Waitlist/Views/NewRequestSummaryPage.xaml`; confirm by repository search that `AppNotificationSamplePayload` in `Strings/en-us/Resources.resw` (~495) has no consumer; record both results and any drift from the document in the Phase 1 notes of `specs/002-truthful-data-and-controls/tasks.md`.

**Checkpoint**: the baseline is recorded and the inventory matches the plan.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two shared mechanisms every story's scan check reuses. Building them once prevents five
one-off scan implementations.

**⚠️ CRITICAL**: complete before Phase 3.

- [x] T003 [P] Extend `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` with a pattern-set scan: a helper that accepts a set of regexes and scans `.cs/.xaml/.resw/.sql/.csproj/.json`, excluding `specs/` and `defects/` by default (the exclusion is deliberate — those files quote removed values as evidence). Reused by T015, T023, T026, T036, T046.
- [x] T004 [P] Add the plausible-value guard to `MTM_Waitlist.Tests/Module_Mock/`: a regex set for currency, dimension and quantity shapes plus the literal `Not provided`, used by T005 and T016 to assert no rendered field value is fabricated-looking.

**Checkpoint**: the scan and guard helpers exist and are green with an empty pattern set.

---

## Phase 3: User Story 1 — the waitlist tells the truth about a request (Priority: P1) 🎯 MVP

**Goal**: the card and the request detail page show only values traced to the request, hide a block whose
read legitimately returned nothing, surface a failed read as an error with a retry, and offer no control
that cannot act.

**Independent Test**: open the waitlist and a request detail page for a request whose material attributes are
unresolvable; confirm nothing invented renders, that a section with nothing to show is absent, and that no
control is inert (`quickstart.md` §3).

### Tests for User Story 1 ⚠️ write first, confirm each fails

- [x] T005 [P] [US1] `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests.cs` (new): build an order from a `WaitlistRequest` carrying distinctive part, quantity and customer values and assert the card fields carry **those** values; assert no field value matches the T004 guard. Fails today.
- [x] T006 [P] [US1] `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailFieldTests.cs` (new): assert no detail section renders an attribute no source produces (`Tipping strategy`, `Allowed categories`, the `Pending*` statuses) and that the label/value helper exposes no fallback default. Fails today.
- [x] T007 [P] [US1] `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` (new): assert `Module_Waitlist/Controls/WaitlistLineCardView.xaml` declares no `Button` without a `Command`, and that the Cancel and Accept elements are gone. Fails today.
- [x] T008 [P] [US1] `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailBlockStateTests.cs` (new): a section whose read throws reports the failed state with a retry; a read that succeeds with no rows reports the hidden state; the page-level store-outage state is not entered for either (`data-model.md` §1 invariants 1–3). Fails today.

### Implementation for User Story 1

- [x] T009 [US1] Remove every fabricated literal and constant from the field-population path in `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (~560–701), including the constant part id behind the average-coil-weight lookup and its `5,000 lb` fallback; a field with no source is left unset rather than defaulted (FR-001/FR-002/FR-004, `data-model.md` §2).
- [x] T010 [P] [US1] Remove the fabricated field bindings from the six per-type card views under `Module_Waitlist/Controls/{Coil,PickupFg,PickupNcm,PickupOs,PickupWip,Scrap}/*WaitlistLineView.xaml` so a row renders only when its value is present.
- [x] T011 [US1] Remove the inert Cancel and Accept buttons from `Module_Waitlist/Controls/WaitlistLineCardView.xaml` (~137–181) and show the request's real lifecycle status in the action area instead (FR-007/FR-008, `contracts/surface-contracts.md` C1).
- [x] T012 [US1] Strip the invented attributes and the `"Not available"` fallback from `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` — the `FieldValue(...)` default parameter and `LoadCoilSections` / `LoadFinishedGoodsSections` / `LoadNcmSections` / `LoadOutsideServiceSections` / `LoadWipSections` / `LoadScrapSections` (FR-002/FR-005).
- [x] T013 [US1] Implement the three-state block in `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`: rows when sourced, `Hidden` on a successful empty read, `Failed` (message plus `RetryLoadCommand`) when the read throws or reports an error; marshal any service-event-driven update through `DispatcherQueue` as `Module_Mock/Views/ReadStatusIndicator.xaml.cs` does (`data-model.md` §1, contract C2).
- [x] T014 [US1] Add the block failure and retry strings to `Strings/en-us/Resources.resw` following the existing key style and the fallback form of `GetLocalized()` (research R6), and wire `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`: `x:Load` with `x:Name` on each section container, the error view as a sibling inside that container, card attribute rows using `Visibility` through the registered `BoolToVisibilityConverter` (research R2, contract C2).
- [x] T015 [US1] Verification: rebuild, run the full suite with T005–T008 passing, run the fabricated-literal scan from T003 over `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` and `Module_Waitlist/Controls/**`, and record the evidence in the Phase 3 notes of `specs/002-truthful-data-and-controls/tasks.md` (FR-032, SC-001).

**Checkpoint**: User Story 1 is independently demonstrable — the waitlist and detail surfaces tell the truth.

---

## Phase 4: User Story 2 — New Request feedback tells the truth (Priority: P2)

**Goal**: the confirm step carries no invented figure, the coil weight is never resolved from a constant, and
the alerts control is honest about what this installation can do.

**Independent Test**: run New Request to the confirm step for two work centers and two request types, then
open Settings → Operations → New Request Alerts (`quickstart.md` §4).

### Tests for User Story 2 ⚠️ write first, confirm each fails

- [x] T016 [P] [US2] `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestSummaryHonestyTests.cs` (new): the view model exposes no queue-count or wait-estimate member, and `Module_Waitlist/Views/NewRequestSummaryPage.xaml` contains no such literal. Fails today.
- [x] T017 [P] [US2] `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestSummaryHonestyTests.cs`: with a recording stub for the average-coil-weight service, an order whose work order resolves to part *X* must cause the call with *X* (not `MMC0001000`), and with nothing resolvable the field is empty — never `5,000 lb`, in any state. Fails today.
- [x] T018 [P] [US2] extend `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`: the view model reports `IsNewRequestAlertsAvailable` false when this installation cannot deliver a notification, and no preference is written while it is false. Fails today.

### Implementation for User Story 2

- [x] T019 [US2] Delete the queue/wait card from `Module_Waitlist/Views/NewRequestSummaryPage.xaml` (~161–167) and correct the summary comment in `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs` so it describes what the screen does (defect §5.4, FR-010).
- [x] T020 [US2] Remove the constant-keyed average-coil-weight lookup and its fixed fallback from `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs`, leaving the field unset when nothing resolves (FR-011/FR-012/FR-013). Per-coil resolution belongs to the follow-on specification.
- [x] T021 [US2] Expose `IsNewRequestAlertsAvailable` on `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` — false when this installation cannot deliver a notification (research R3) — and bind the toggle's `IsEnabled` to it in `Module_Settings/Views/SettingsPage.xaml` (~954–990), replacing the static "installed as a packaged app" sentence (FR-014/FR-015, contract C3).
- [x] T022 [US2] Add the localized "this installation cannot deliver notifications" string to `Strings/en-us/Resources.resw`; confirm no preference value is written while the capability is false.
- [x] T023 [US2] Verification: rebuild, run the suite with T016–T018 passing, and record the evidence in the Phase 4 notes of `specs/002-truthful-data-and-controls/tasks.md`.

**Checkpoint**: User Stories 1 and 2 both hold independently.

---

## Phase 5: User Story 3 — Settings tells the truth and searches completely (Priority: P3)

**Goal**: each About label shows its own text, no placeholder privacy entry ships, and every search-aware
panel responds to the search term — with a check that fails when the next panel is added unregistered.

**Independent Test**: read the three About labels, look for the privacy entry, then search `alert`,
`urgency` and `image` in turn (`quickstart.md` §5).

### Tests for User Story 3 ⚠️ write first, confirm each fails

- [x] T024 [P] [US3] `MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests.cs` (new): every `x:Uid` in `Module_Settings/Views/SettingsPage.xaml` is unique and resolves to its own `<Uid>.Text` entry. Fails today (`x:Uid="Settings_About"` appears three times).
- [x] T025 [P] [US3] extend `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`: changing `SearchQuery` raises `PropertyChanged` for `IsNewRequestAlertsPanelVisible`, `IsUrgencyAllotmentsPanelVisible` and `IsImageLocationSettingsPanelVisible`; plus the reflection coverage test that every property whose getter calls `MatchesSearch(...)` is a member of the declared list. Fails today.
- [x] T026 [P] [US3] extend the T003 scan with the placeholder-resource patterns (`YourPrivacyUrlGoesHere`, `example.com`, `TODO`, `Contoso`, `lorem`) over `.resw` values. Fails today.

### Implementation for User Story 3

- [x] T027 [US3] Give the three About elements their own keys in `Module_Settings/Views/SettingsPage.xaml` — section header unchanged, version card `Settings_AppVersion_Title`, about card `Settings_AboutApp_Title` — add both entries to `Strings/en-us/Resources.resw`, reconcile the resource and XAML descriptions of the About card, and confirm the version value is not empty on the unpackaged build (defect §5.1/§5.3, FR-016/FR-017).
- [x] T028 [US3] Remove the privacy hyperlink from `Module_Settings/Views/SettingsPage.xaml` (~1249) and delete `SettingsPage_PrivacyTermsLink.Content` and `.NavigateUri` from `Strings/en-us/Resources.resw` (~487–492); delete `AppNotificationSamplePayload` (~495) when T002's repository search confirmed it has no consumer (FR-018, research R4).
- [x] T029 [US3] Declare `static readonly string[] s_searchAwareProperties` in `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` holding all ten search-consuming members and make `RefreshSearchVisibility()` (~868–879) iterate it, adding the three missing panels (FR-019/FR-020/FR-021).
- [x] T030 [US3] Correct the existing value-only search assertion in `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` (~118) to assert the change notification was raised (FR-021).
- [x] T031 [US3] Verification: rebuild, run the suite with T024–T026 passing, and record the evidence in the Phase 5 notes of `specs/002-truthful-data-and-controls/tasks.md`.

**Checkpoint**: User Stories 1–3 hold independently.

---

## Phase 6: User Story 4 — opening a notification while running behaves like a cold start (Priority: P4)

**Goal**: a recognised activation opens the request; an unrecognised one does nothing visible; neither ever
shows an internal placeholder.

**Independent Test**: while the app is running, deliver a mapped and an unmapped activation and observe the
two outcomes (`quickstart.md` §6).

### Tests for User Story 4 ⚠️ write first, confirm each fails

- [x] T032 [P] [US4] `MTM_Waitlist.Tests/Module_Core/Activation/RequestDeepLinkHandlerTests.cs` (new): a mapped argument navigates to the waitlist detail for that request and shows no dialog; an unmapped argument records a diagnostic, displays nothing and brings the window forward; the cold-start path is unchanged. Fails today.

### Implementation for User Story 4

- [x] T033 [US4] Add the shared helper `TryHandleRequestDeepLink` in `MTM_Waitlist.Core/Activation/RequestDeepLinkHandler.cs`, parsing through `MTM_Waitlist.Core/Services/WaitlistRequestLink.cs`, navigating to the detail page and bringing the window forward (`contracts/notification-activation-contract.md` C1).
- [x] T034 [US4] Delete both placeholder dialogs and route through the helper: `MTM_Waitlist.Core/Services/AppNotificationService.cs` `OnNotificationInvoked` (~35–42, which currently discards the argument it is handed) and `MTM_Waitlist.Core/Activation/AppNotificationActivationHandler.cs` (~57–61) (FR-022/FR-023/FR-024, contract C2/C3).
- [x] T035 [US4] Record the unmapped case through the existing diagnostic seam (`StartupDebugLog` in `MTM_Waitlist.Core/Helpers/`) with the raw arguments at debug level, then verify T032 passes and record the evidence in the Phase 6 notes of `specs/002-truthful-data-and-controls/tasks.md`; confirm notification registration and delivery remain out of scope (contract C3).

**Checkpoint**: User Stories 1–4 hold independently.

---

## Phase 7: User Story 5 — the repository describes only what still exists (Priority: P5)

**Goal**: no shipped text, comment or document presents the retired sample/mock mechanism or the removed
manual toggle as current, and the counting traps are labelled retired.

**Independent Test**: read the changelog, the backlog index, the module task file and the shipped resources
and confirm none presents removed behaviour as current (`quickstart.md` §7).

### Tests for User Story 5 ⚠️ write first, confirm it fails

- [x] T036 [P] [US5] Extend the T003 scan with the retired-wording patterns: `\bMockSaved\b`, a bare `\bRecvMockData\b`, `\bInforVisualMockData\b`, and "sample data" / "sample rows" inside `.resw` values (defect §4.4, FR-025/FR-029); this scan is also the standing check for FR-030, which forbids any demo, sample or mock mode returning. Fails today.

### Implementation for User Story 5

- [x] T037 [P] [US5] Delete the `Setup_Review.Status.MockSaved` entry from `Strings/en-us/Resources.resw` (~211–213).
- [x] T038 [P] [US5] Rewrite the six stale comments listed in `defects/Closed-Low-Localization-RetiredMockWordingStillShips.md` §1: `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` ~196–199, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` 582–585 / 705 / 733, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs:18`, `MTM_Waitlist.Core/Contracts/Services/IIgnoredLocationsService.cs:7`, `MTM_Waitlist.Setup/Services/SetupPersistenceService.cs:303` — describing live behaviour, or stating the gap explicitly.
- [x] T039 [US5] Correct `WeekendProject/ChangeLog.Simple.md`: feature 19 no longer presents the removed manual toggle as current, and feature 18 records the packaged-only caveat (FR-026).
- [x] T040 [US5] Correct `WeekendProject/ChangeLog.md` the same way in its prose form.
- [x] T041 [US5] Add the "retired sources" note to `OPEN-TASKS.md` §3.1 — the six sources with the reason each must not be reopened — and point at it from `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2 without reopening §10.1 (FR-027).
- [x] T042 [US5] Reconcile `WeekendProject/Module_Mock/Tasks.md` so its completed items match what is finished (72 boxes behind reality) (FR-028).
- [x] T043 [US5] Update `FEATURES.md` rows for the defects this feature closes, keeping a ⚠ marker only where a follow-on specification owns the remainder (FR-026).
- [x] T044 [US5] Update each of the ten `defects/*.md` status lines to name this specification, the closure, the owning follow-on specification and the checks that prove it — leaving every quoted literal as evidence (data-model §6).
- [x] T045 [US5] Verification: T036 passes over `Strings/en-us/Resources.resw`, `MTM_Waitlist.Waitlist.View/ViewModels/` and `WeekendProject/`, and the wording scan gate returns empty; then read back `FEATURES.md`, `OPEN-TASKS.md` §3.1 and both changelogs and confirm a reader unfamiliar with the codebase can state today's behaviour from them (SC-010); record the evidence in the Phase 7 notes of `specs/002-truthful-data-and-controls/tasks.md`.

**Checkpoint**: all five stories hold; the repository no longer describes deleted behaviour.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: the gates that span stories, and the evidence of record.

- [x] T046 [P] Run and record the six scan gates from `contracts/verification-gates.md` G3 on a release-representative build, capturing the commands and their empty output in the Phase 8 notes of `specs/002-truthful-data-and-controls/tasks.md` (SC-009).
- [x] T047 [P] Confirm the preserved behaviour: with the external source unreachable, walk the cached-fallback path and verify the values, the read-only indicator, its cached age and the manual retry are unchanged (FR-006/FR-031, SC-013).
- [x] T048 Run the full gate from `contracts/verification-gates.md` G1/G2: `MTM_Waitlist.sln` build `0 Warning(s) 0 Error(s)` and full suite `Failed: 0`, with at least ten new checks present and each shown to have failed before its fix; record the counts in the Phase 8 notes of `specs/002-truthful-data-and-controls/tasks.md` (SC-008/SC-011).
- [x] T049 Walk `quickstart.md` §3–§7 on a signed-in build, recording the outcome per surface in the Phase 8 notes of `specs/002-truthful-data-and-controls/tasks.md`; where no credential is available, record the shell-dependent steps as environment-gated and do not report them as passed (SC-012, G5).
- [x] T050 [P] Cross-check the delivered state against `data-model.md` §1–§5 and `contracts/surface-contracts.md` C1–C5, correcting any clause the implementation did not satisfy or recording why (analysis pass before `/speckit.analyze`).
- [x] T051 Confirm no `Database/**` artifact was introduced, no stored-procedure call changed, and no inline SQL appeared (FR-031, constitution III); record the confirmation in the Phase 8 notes of `specs/002-truthful-data-and-controls/tasks.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: after Setup; **blocks every story** because all four scan checks reuse T003/T004.
- **User Stories (Phase 3+)**: after Foundational. US1 → US2 → US3 → US4 → US5 in priority order, or in parallel if staffed.
- **Polish (Phase 8)**: after the stories it measures.

### User Story Dependencies

- **US1 (P1)**: independent. MVP.
- **US2 (P2)**: independent; touches `SettingsViewModel.cs` and `SettingsPage.xaml`, which **US3 also edits** (T027/T029) — sequence those files, not the stories (T021/T022 before or after T027/T029, never concurrently).
- **US3 (P3)**: independent apart from the shared-file note above.
- **US4 (P4)**: independent of every other story.
- **US5 (P5)**: its comment rewrite (T038) touches the same two view-model files as US1's T009/T012 — run T038 **after** T009/T012. Its remaining tasks are documents only.

### Within Each Story

- Checks first and failing; then the removal or fix; then that story's verification task; then the checkpoint.
- In US1: T009 before T010/T011 (the view model decides what the card binds), T012 before T013, T013 before T014.

---

## Parallel Opportunities

- **Phase 1**: T002 alongside T001.
- **Phase 2**: T003 and T004 are separate files — run together.
- **US1**: T005–T008 are four new test files — run together. T010 and T011 touch different XAML files.
- **US2**: T016–T018 are three test additions in two files; T019 and T020 are the same file (sequential).
- **US3**: T024–T026 run together; T027 and T028 touch the same XAML page (sequential).
- **US5**: T037, T038 and T039–T042 touch different files — T037/T038 together, then the documents in any order.

```text
# Phase 2 together:
Task: "T003 pattern-set scan helper in MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs"
Task: "T004 plausible-value guard in MTM_Waitlist.Tests/Module_Mock/"

# US1 checks together:
Task: "T005 card field values in …/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests.cs"
Task: "T006 detail invented content in …/ViewModels/WaitlistViewDetailFieldTests.cs"
Task: "T007 card markup in …/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs"
Task: "T008 block state in …/ViewModels/WaitlistViewDetailBlockStateTests.cs"
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Phase 1 → Phase 2 (the two helpers).
2. Phase 3: US1 — the safety-critical surface, card and detail.
3. **Stop and validate**: `quickstart.md` §3 on the running app, plus T015's evidence.
4. Ship or demo; the remaining stories are independent increments.

### Incremental delivery

1. US1 (safety) → validate → US2 (confirm step and alerts) → validate → US3 (Settings) → validate → US4 (notification) → validate → US5 (wording and documents) → validate.
2. Each story closes its own defect rows and its own checks; the documents in US5 name what remains with the follow-on specifications.

### Notes

- Never mark a task complete without the evidence its verification task names — the repository's
  checklist-execution rule forbids speculative ticks, and FR-032 makes the failing-before/failing-after check
  the evidence of record.
- `WMC9999` during a build is a masked XAML error, not noise; `PRI175`/`PRI224` means a running app or stale
  `*.pri` files.
- The ten `defects/*.md` files stay as the historical record: only their status lines change.

---

## Phase Notes (evidence of record)

### Phase 1 — Setup baseline and inventory

**T001 — baseline gates (recorded 2026-09-12, before any change of this feature)**

```text
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
  -> Build succeeded.  0 Warning(s)  0 Error(s)

dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
  -> Passed!  - Failed: 0, Passed: 768, Skipped: 19, Total: 787, Duration: 23 s
```

Stale `MTM_Waitlist` / `Microsoft.UI.Xaml.Markup.Compiler` / `VBCSCompiler` / `MSBuild` processes were
stopped and `MSBUILDDISABLENODEREUSE=1` set before the run. This is the number every later gate is
compared against.

**T002 — removal inventory confirmed, with drift**

Confirmed as `data-model.md` §2/§3 describes:

| Target | Confirmed |
| --- | --- |
| `WaitlistViewViewModel.AddRequestFields` | every fabricated literal of §2 is present, across the coil / pickup-FG / pickup-NCM / pickup-WIP / pickup-coil / pickup-OS / pickup-other / other / flatstock / table+die handling / scrap branches, including the `"Not provided"` → value substitutions |
| `WaitlistViewDetailViewModel` | `FieldValue(..., string fallback = "Not available")`, the `"Not available"` / `Pending*` / `Tipping strategy` / `Allowed categories` literals, all six section loaders, and `CoilReceivingPartId = "MMC0001000"` in `EnrichCoilAverageWeightAsync` |
| `Module_Waitlist/Controls/WaitlistLineCardView.xaml` | the two inert `Button` elements (Cancel, Accept) with no `Command` |
| `Module_Waitlist/Views/NewRequestSummaryPage.xaml` | lines 161–167 are the queue/wait card: `Queue & wait time`, `0 active request(s) for this work center.`, `Estimated wait time: approximately 15 minutes.` |
| `Strings/en-us/Resources.resw` | `Setup_Review.Status.MockSaved` at line 211 |
| G3 scan patterns | `YourPrivacyUrlGoesHere` (`Resources.resw:491`), `SettingsPage_PrivacyTermsLink.{Content,NavigateUri}` (`Resources.resw:487–492`, consumed at `SettingsPage.xaml:1249`), `TODO: Handle notification…` (`AppNotificationActivationHandler.cs:57`, `AppNotificationService.cs:39`) |

**Drift recorded against the task text**

1. **`AppNotificationSamplePayload` has a consumer — T028's delete condition does NOT fire.** The entry
   sits at `Resources.resw:520` (not ~495) and is read at `App.xaml.cs:323`, where `OnLaunched` shows it on
   every launch. It is therefore **kept**; the launch-time demo toast is not one of the ten defects in this
   specification and removing its payload would break the build. Recorded as a follow-on candidate rather
   than silently deleted.
2. **The privacy `TODO` lives in the code-behind, not the XAML.** T028 names `SettingsPage.xaml:1249` for
   the hyperlink (correct); the `TODO: Set the URL …` comment is at `Module_Settings/Views/SettingsPage.xaml.cs:10`.
   Both go, or the G3 `TODO` gate cannot come back empty.
3. **Line-number drift is minor but real.** `WaitlistViewViewModel.cs` is 704 lines (task: ~560–701),
   `WaitlistViewDetailViewModel.cs` is 488 (task: ~316–520), `SettingsPage.xaml` is 1188 (task cites ~954–990
   and ~1249), `Resources.resw` is 662. Nothing moved out of the named files; the cited ranges are used as a
   starting point and the enclosing member is the address of record.
4. **`Module_Waitlist/Controls/**` carries no fabricated *values*.** The literal hits there are descriptive
   tooltips (`Coil tipping strategy - Use Crane when the coil height is less than 10 inches…`,
   `NCM destination - …`, `WIP destination - …`) — they state policy, not request data, and are not in
   `data-model.md` §2. Left alone.
5. **`IsNewRequestAlertsAvailable` does not exist yet anywhere in the repository**, so T021 introduces it
   without a name collision.

### Phase 2 — Foundational helpers

**T003 — `RepositoryPatternScan` (in `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`)**

One helper, `Scan(patterns, scope)`, that takes a description/pattern set and reports every matching line as
`RepositoryScanHit(relativePath, lineNumber, description, pattern, text)`. `RepositoryScanScope.Default` is
repository-wide over `.cs/.xaml/.resw/.sql/.csproj/.json`, excluding `bin`, `obj`, `.git`, `TestResults`,
and — deliberately — `specs/` and `defects/`, which quote the removed values as evidence. It also skips the
two files that must declare the patterns they forbid (`RetiredSymbolAuditTests.cs`, `FabricatedValueGuard.cs`)
and supports narrowing by `IncludeUnder`, `ExcludedDirectories`, `Extensions` and `ExcludingFiles(...)`.

**T004 — `FabricatedValueGuard` (new, `MTM_Waitlist.Tests/Module_Mock/FabricatedValueGuard.cs`)**

`IsFabricatedLooking(value)` / `FindFabricatedLooking(values)` over six shapes: currency amount, measured
weight or mass, counted quantity, dimension, bare measurement, and absence rendered as a value
(`Not provided` / `Not available`). Shapes rather than literals, so a substitute re-spelled tomorrow still
fails. Bare numbers are deliberately *not* flagged — an operation sequence or a quantity the requester typed
is sourced data.

**Evidence — 7 new checks, green (2026-09-12)**

```text
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 --filter "FullyQualifiedName~PatternScan|FullyQualifiedName~FabricatedValueGuard"
  Passed FindFabricatedLooking_WithAnEmptyValueSet_ReturnsNothing
  Passed IsFabricatedLooking_WithAnAbsentValue_IsFalse
  Passed IsFabricatedLooking_WithASyntheticValueOfEachShape_IsTrue
  Passed IsFabricatedLooking_WithASourcedValue_IsFalse
  Passed PatternScan_WithAnEmptyPatternSet_ReturnsNoHits
  Passed PatternScan_WithTheDefaultScope_EnumeratesProductionSource
  Passed PatternScan_WithAKnownPresentLiteral_FindsIt
  Passed: 7   Failed: 0
```

Three of those seven exist only to stop the helpers being vacuous: the default scope must reach >100
production files including `WaitlistViewViewModel.cs` and `SettingsPage.xaml` (and must *not* reach `specs/`
or `defects/`), a known-present literal must be found, and a synthetic value of each guard shape must be
flagged. Without them a clean scan would be indistinguishable from a broken enumeration — the failure mode
`contracts/verification-gates.md` G4 warns about.

**Design consequence recorded now, because it constrains every later check**: the G3 scan excludes only
`specs/` and `defects/`, so no test file may contain a fabricated literal. Checks therefore assert *shapes*
(through the guard) or *absence of a member/literal constructed at runtime* — never a pasted literal from
`data-model.md` §2.

### Phase 3 — User Story 1 (card and request detail)

**T005–T008 — written first, confirmed failing (RED evidence, 2026-09-12)**

```text
dotnet test ... --filter "WaitlistViewViewModelFieldTests|WaitlistViewDetailFieldTests|WaitlistLineCardMarkupTests"
  Failed FieldValue_ExposesNoFallbackDefault
  Failed LoadedSections_RenderNoInventedAttributeAndNoAbsencePlaceholder
  Failed CreateSessionOrder_ForEveryRequestShape_CarriesOnlyValuesTheRequestCarries
  Failed CreateSessionOrder_ForEveryRequestShape_RendersNoFabricatedLookingValue
  Failed Card_DrawsNoButtonThatCannotAct
  Failed Card_OffersNoCancelOrAcceptControl
  Failed!  - Failed: 6, Passed: 4, Total: 10

T008 could not even be expressed against the old code — the compiler named the absent API:
  error CS1061: 'WaitlistViewDetailViewModel' does not contain a definition for 'IsSectionLoadFailed'
  error CS1061: 'WaitlistViewDetailViewModel' does not contain a definition for 'SectionFailureMessage'
  error CS1061: 'WaitlistViewDetailViewModel' does not contain a definition for 'RetryLoadCommand'
```

The four checks that passed before the change are the non-vacuity guards (empty slot stays empty, five-slot
padding, no unlabelled row, the status pill still rendering) — they exist so the six above cannot pass by
accident.

**T009/T010 — the card**

`WaitlistViewViewModel.AddRequestFields` was rewritten around a local `Add(label, value)` that skips a blank
value, so a row exists only when the request carries it. Every branch lost its literals: the coil number,
on-hand weight, description and average weight; the finished-goods part/description/quantity/customer/
packlist; the NCM part/quantity/destination/traceability; the WIP work order, part-quantity, destination and
operation sequence; the outside-service part, quantity, destination and vendor; the flatstock, table and die
handling part/quantity/destination; the scrap part number and quantity; the `"Not provided"` / `"Not
selected"` / `"General Text Entry"` substitutes; and the constant-keyed average-coil-weight lookup with its
fixed fallback. The `IAverageCoilWeightService` dependency was removed from the view model entirely, not left
unused (the build gate is 0 warnings).

`AddRequestContextSection`'s `Handler status` row and the `FieldValue(..., string fallback = "Not available")`
default are gone from the detail view model; `FieldValue` now returns `string?` and a label with no source
returns null, so no default literal can re-enter through the helper (contract C2).

**T010 — the six per-type templates**

The templates bound hard-coded labels to positional slots (`Fields[0]`..`Fields[4]`). With the fabricated
rows gone those labels named attributes the neighbours did not carry — e.g. a Pickup FG card would have
labelled the `Subtype` row "Part number". Each of the ten label/value pairs in each of the six views now
binds the field's own `Label` and hides both label and value through the globally registered
`BoolToVisibilityConverter` on a new `WaitlistField.HasValue`. `WaitlistField` gained change notification so a
slot that gains or loses its source re-renders. Per contract C1 nothing inside a list template is deferred
with `x:Load`.

**T011 — the action area**

Both `Button` elements were removed from `WaitlistLineCardView.xaml` and the lifecycle-status pill moved into
their place, still gated on `HasStatusBadge`. The action area now offers only what the application can do.

**T013 — the block states**

`LoadItemAndSections` runs the request resolution and the section build as one unit: a throw clears the
sections, sets `IsSectionLoadFailed`, and explicitly clears `IsItemPresent` and `IsEmptyStateVisible` so a
failure can never render as absence. `RetryLoadCommand` re-runs that unit. `AddSection` drops every row whose
value has no source and drops the section itself when no row survives — the `Hidden` state is expressed as
absence from `TemplateSections`, and the failure block is `x:Load`-gated in the page.

**Deviation from contract C2, recorded rather than silently adapted**: C2 asks for `x:Load` **per section**
with `x:Name` on each container. The detail page renders sections through a data-bound `ItemsControl`, so a
per-item `x:Name`/`x:Load` pair cannot exist. The equivalent guarantee is delivered differently: a section
with nothing to show is never added to the collection (so it is absent from the visual tree and no element is
materialised for it), and the **failure** block — which does need to be a single, addressable element — is a
named `x:Load`-gated sibling carrying `x:Name="SectionFailureBlock"` with the retry bound to
`RetryLoadCommand`. Same observable behaviour, one mechanism instead of two.

**T015 — evidence**

```text
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
  -> Build succeeded.  0 Warning(s)  0 Error(s)

dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
  -> Passed!  - Failed: 0, Passed: 789, Skipped: 19, Total: 808   (baseline 768/19/787; +21 checks)

fabricated-literal scan over MTM_Waitlist.Waitlist.View/ViewModels, Module_Waitlist/Controls,
Module_Waitlist/Views (case-sensitive, the §3 set plus the Not-available/Not-provided wording)
  -> no output
```

The four pre-existing checks that failed after the change were the ones asserting the fabricated values
(`Requested coil` = a stand-in coil, the five finished-goods literals, `Wrong coil`, `"Not selected"`, and the
`>= 3` section counts). They were rewritten to pin the truthful behaviour — absence of the unsourced row,
and the subtype plus the typed detail in its place — because they encoded the defect rather than the intent.
A fifth shipped-text occurrence was corrected: the XML doc example on
`IAverageCoilWeightService.ResolveAverageCoilWeightTextAsync`.

**Deferred to Phase 7**: the six card views' trailing `Notes` blocks still describe the hard-coded labels
(the card's contents are now the field's own label/value pairs). They are shipped text describing removed
behaviour and are corrected with the rest of the wording sweep rather than left as a Phase 3 loose end.

### Phase 4 — User Story 2 (New Request confirm and the alerts capability)

**T016/T018 — written first, confirmed failing**

`ConfirmStepPage_CarriesNoQueueOrWaitFigure` failed against the shipped card, and both settings checks could
not be expressed at all — the compiler named the absent capability:

```text
error CS1061: 'SettingsViewModel' does not contain a definition for 'IsNewRequestAlertsAvailable'
```

**T017 — drift against the task text, recorded**

The task places a "constant-keyed average-coil-weight lookup and its fixed fallback" in
`NewRequestSummaryViewModel.cs`. T002's inventory found neither: the hard-coded `MMC0001000` key and the
`"5,000 lb"` fallback lived in the two **Waitlist** view models and were removed by T009/T012, and
`CoilAvailabilityService` already resolves the weight from the coil the active job actually carries (spec 001).
The check was therefore implemented where the behaviour is, and it is not vacuous: it asserts the resolved
part is a **distinctive** one (`MMC778812`), which a fixed key cannot satisfy, and that an unresolvable weight
leaves the field empty rather than standing in a value.
The one real gap found here was the confirm step's **labelled shell** — a `Average coil weight` label with an
empty value — which is now gated on a new `HasCoilAverageWeight`.

**T021/T022 — the capability is the delivery path's own condition**

`IsNewRequestAlertsAvailable` reports `RuntimeHelper.IsMSIX`, which is the condition
`AppNotificationService.Initialize` and `Show` both evaluate; the panel therefore cannot offer a control the
delivery path would silently ignore, and it is a per-installation answer rather than a claim about the
platform (research R3). The toggle's `IsEnabled` binds to it, the static "installed as a packaged app"
sentence is replaced by a localized message resolved through `NewRequestAlertsDescription`,
`OnNewRequestAlertsEnabledChanged` returns before persisting while the capability is false, and the new
strings live in `Strings/en-us/Resources.resw`.

**Evidence**

```text
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
  -> Build succeeded.  0 Warning(s)  0 Error(s)

dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
  -> Passed!  - Failed: 0, Passed: 798, Skipped: 19, Total: 817   (+9 checks this phase)

scan: NewRequestSummaryPage.xaml for queue/active-request/estimated-wait/minute literals  -> no output
scan: Module_Settings + MTM_Waitlist.Settings for the static "packaged app" claim         -> no output
```

One pre-existing check encoded the defect and was rewritten: `NewRequestAlerts_TogglePersistsUnderKey`
asserted that toggling always persists, which is exactly the silent-preference behaviour the defect names.
It is now `NewRequestAlerts_TogglePersistsOnlyWhenTheInstallationCanDeliver`, asserting the branch this
installation is actually in.

### Phase 5 — User Story 3 (Settings labels, privacy entry, search completeness)

**RED evidence — captured by temporarily restoring the pre-change state, then restoring the fix**

```text
dotnet test ... --filter "SettingsPageMarkupTests|NoPlaceholderResourceValueShips|SearchQuery_ChangeAnnounces…|SearchRegistration_Covers…|PanelAndCategoryMatchSearch"
  Failed SettingsPage_DeclaresEachXUidAtMostOnce                          (x:Uid="Settings_About" three times)
  Failed NewRequestAlerts_PanelAndCategoryMatchSearch_AndTheChangeIsAnnounced
  Failed SearchQuery_ChangeAnnouncesEveryPanelThatConsumesTheTerm
  Failed NoPlaceholderResourceValueShips                                  (the placeholder address value)
  Failed!  - Failed: 4, Passed: 2, Total: 6

restored -> Passed!  - Failed: 0, Passed: 804, Skipped: 19, Total: 823
```

`SearchRegistration_CoversEveryPropertyThatConsumesTheTerm` passed in the reverted state, correctly: it
tests the *declared list*, and only the iteration had been reverted. It fails when a panel that consumes the
term is missing from the list — the case it exists for.

**T028 — one delete did not fire, as T002 predicted**

The privacy hyperlink went, with `SettingsPage_PrivacyTermsLink.Content` and `.NavigateUri`; the
`TODO` marker that told a developer to set the URL lives in the **code-behind** (`SettingsPage.xaml.cs`) and
went with it, since leaving it would keep the G3 `TODO` gate non-empty. `AppNotificationSamplePayload` was
**kept**: T002's repository search found it is read at `App.xaml.cs:323`, so T028's conditional does not
apply. See the Phase 1 drift note.

**T027 — the About description is reconciled in both places**

The About card's XAML fallback said "Manage waitlists and local consumer records securely." while the
resource said something else entirely; the XAML now carries the resource text, so the two cannot disagree.
`Settings_About` stays on the section header, and the version card and About card take
`Settings_AppVersion_Title` and `Settings_AboutApp_Title`. A check was added for the contract clause this
leaves behind: the version value must not be empty on the unpackaged build.

**T029 — the list is the registration, and the check reads the list**

`s_searchAwareProperties` names the ten properties whose getter consumes `MatchesSearch`, and
`RefreshSearchVisibility()` iterates it before announcing the three category aggregates. The coverage check
finds the consumers by locating the call site of the private predicate in each getter's IL, so it is
discovery-driven: the next panel added without registering is caught rather than assumed.

### Phase 6 — User Story 4 (notification activation)

**T033 — one helper, and a seam that makes it checkable**

`RequestDeepLinkHandler.TryHandleRequestDeepLink` is the single place an activation becomes navigation. Both
entry points call it: `AppNotificationActivationHandler.HandleInternalAsync` (cold start) and
`AppNotificationService.OnNotificationInvoked` (while running) — the latter previously discarded the argument
it was handed. Neither file contains its own parse or its own fallback UI.

One small abstraction was added so the routing is testable without a UI thread: `IDeepLinkWindow` (two
members — queue work on the UI thread, bring the window forward) with `AppWindowDeepLinkWindow` as its
production implementation. Without it the handler could only be constructed against a real `WindowEx`, and
FR-032's check could not run in the suite at all. Both are registered in DI alongside the handler.

**T035 — the diagnostic level, recorded**

The unmapped case is recorded through `StartupDebugLog`. The seam exposes `Info` and `Error` only — there is
no `Debug` level in `StartupDebugLog` or in `IStartupLogService` — so the raw argument is recorded at `Info`,
which is the finest level the seam has. Adding a level to the seam would have been a larger change than the
requirement warrants; the requirement is that the argument is *recorded for diagnosis*, and it is.

**T032 — RED evidence captured by temporarily restoring the placeholder**

```text
restored the placeholder dialog in the while-running path, ran the check:
  Failed NeitherActivationPathCanPresentADialogOrADeveloperMarker
   Assert.IsFalse failed. the while-running path (MTM_Waitlist.Core/Services/AppNotificationService.cs)
   still reaches a modal dialog; an activation is not a place to show internal state.
  Failed!  - Failed: 1, Passed: 0, Total: 1

restored -> Passed!  - Failed: 0, Passed: 808, Skipped: 19, Total: 827
```

The dialog half of the check is asserted against the source, deliberately: a dialog needs a UI thread to
appear, and a source scan is what can see one *re-added* to any of the three files. The routing half is
exercised behaviourally — a mapped argument must queue exactly one navigation to the detail page carrying
`request.Id.GetHashCode()`, an unmapped one must navigate nowhere while still coming forward, and repeated
activations must reach the same outcome without accumulating.

### Phase 7 — User Story 5 (wording and documents)

**T036 — the standing check for FR-030**

The retired-wording gate scans the code extensions for `MockSaved`, bare `RecvMockData` and
`InforVisualMockData`, and scans **resource values only** for "sample data" / "sample rows". Two narrowings
were deliberate and are written into the check: key names are not scanned (the WinUI convention names many
legitimate keys with the word "Placeholder"), and code comments that *state the absence* ("never replaced by
sample rows") are the record of the removal rather than a stale claim — so the phrase is checked where it
would be a product statement, which is a shipped resource value. The `MockSaved` entry went in T037.

**T038–T044 — the sweep, and what it could not settle**

The stale comments and the six card-view `Notes` blocks were rewritten; both changelogs, `OPEN-TASKS.md`
§3.1, `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2, `FEATURES.md` and the ten `defects/*.md` status lines
were corrected. Three points need the reader's attention:

1. **T038's targets: one did not exist and three were already done.** `SetupPersistenceService.cs:303` carries
   no stale comment — that line is the live `Setup_Review.Status.Saved` call. The comments at
   `WaitlistViewDetailViewModel.cs` ~196–199 and `WaitlistViewViewModel.cs` 582–585 / 705 / 733 were already
   rewritten by T009/T012, in the order `tasks.md` itself prescribes (T038 after T009/T012).
2. **`WeekendProject/Module_Mock/Tasks.md`: 67 boxes ticked, 6 left unticked**, each with its reason written
   into the file — an unfinished request-type source reconciliation, the coverage tied to it, the
   `PromptFiles/*` sweep (those files are retired rather than corrected), a planning document still headed
   "Not implemented", the live DB-integration half of the Phase 8 gate (environment-gated here), and the
   in-app XamlMcp verification (that agent is not wired into this repo). Ticking those would have been the
   speculative completion the checklist rule forbids.
3. **`OPEN-WORK-NEXT-SPEC.md` §2 names seven files; six are retired sources.** The seventh,
   `Module_Mock/Tasks.md`, is T042's subject rather than a retired source, and §3.1 says so.

**T045 — evidence**

```text
T036 gate over the code extensions, excluding specs/ and defects/:
  MockSaved | RecvMockData | InforVisualMockData -> no output outside the audit file itself
  "sample data" / "sample rows" in resource values -> no output
read-back of FEATURES.md, OPEN-TASKS.md §3.1 and both changelogs: each now states current behaviour, names
  what a follow-on specification owns, and leaves the retired names only in the record of their retirement.
```

### Phase 8 — Polish and cross-cutting gates

**T046 — the six scan gates (each returns empty)**

```text
YourPrivacyUrlGoesHere                       -> no output outside the audit file
MockSaved                                    -> no output outside the audit file
TODO: Handle notification                    -> no output
RecvMockData / InforVisualMockData           -> no output (exempt: the retained rollback.sql drop artifacts,
                                                update_table_descriptions.sql's retired-objects section,
                                                and the audit file naming what it forbids)
fabricated-literal set over the surfaces      -> no output
placeholder-resource values (example.com,
  Contoso, lorem, TODO)                      -> no output
```

**Scope note recorded as a deviation (constitution IV).** `contracts/verification-gates.md` G3 lists the
fabricated-literal gate with `specs/` and `defects/` as its only exclusions, which read as "repository-wide".
Applied literally to `.sql` it is unsatisfiable and wrong: `Database/Mock/Seeds/**` holds the **cached Infor
Visual mirror**, where real material values legitimately appear, and `Database/Seeds/seed_waitlist_request_catalog`
holds catalogue rows. The defect is about what the application **renders**, not what it stores, so the gate is
scoped to the rendering surfaces (`MTM_Waitlist.Waitlist.View/ViewModels`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels`,
`Module_Waitlist/**`, `Module_Settings/**`, `MTM_Waitlist.Settings/ViewModels`) plus the `.resw` values, and the
non-`.md` code extensions are scanned by the audit for the wording patterns. The narrowing is recorded here
rather than applied silently. Test fixtures that use a removed literal as a *requester's typed input* are
therefore out of scope by construction — they are inputs, not rendered values.

**T047 — the preserved cached-fallback behaviour**

FR-006/FR-031 keep the external-read fallback, its read-only indicator, its age and its manual retry exactly
as delivered. That behaviour is covered by spec 001's checks, all still green in this run, including
`Probe_TwoConsecutiveFailures_GoesCached`, `Probe_OneSuccessWhileCached_ReturnsLiveImmediately`,
`WorkOrderLookup_Unreachable_ServesMirrorThroughShapeProcedure`,
`WorkOrderLookup_EmptyLiveAnswer_IsNotReplacedByCache`,
`OperationSequences_Unreachable_ServesMirrorThroughShapeProcedure`,
`OperationSequences_EmptyLiveAnswer_IsNotReplacedByCache`,
`RefreshAsync_ReportsCachedDataAge_FromTheMostRecentShapeRefresh` and
`EveryShippedShape_HasAnInAppFallbackRegistered`. No file on the fallback path was edited by this feature.

**T048 — the full gate**

```text
G1  dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
      -> Build succeeded.  0 Warning(s)  0 Error(s)

G2  dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
      -> Passed!  - Failed: 0, Passed: 809, Skipped: 19, Total: 828
         (baseline: 0 warnings / 0 errors, Failed: 0, Passed: 768, Skipped: 19, Total: 787)
         RetiredSymbolAuditTests and InlineSqlAuditTests stay green.

New checks: 828 - 787 = 41, against SC-008's floor of ten.
```

Checks **shown failing before their fix landed**, by phase:

| Phase | Checks shown failing | How |
| --- | --- | --- |
| 2 | 7 helpers | Not vacuous by construction: each carries a positive control (a known-present literal, a synthetic value of each guard shape) and a scope-size floor. |
| 3 (US1) | 6 | Captured run: `Failed: 6, Passed: 4, Total: 10`; T008's API was absent (`CS1061` ×3). |
| 4 (US2) | 3 | The confirm page carried the fabricated card; T018's capability was absent (`CS1061`). |
| 5 (US3) | 4 | Captured run `Failed: 4, Passed: 2` with the pre-change state temporarily restored, then restored. |
| 6 (US4) | 1 | Captured run `Failed: 1, Passed: 0` with the placeholder dialog temporarily restored, then restored. |
| 7 (US5) | 1 | The `MockSaved` entry and the retired wording were present when the gate was written. |

**T049 — the running-application walkthrough: environment-gated, NOT passed**

`quickstart.md` §3–§7 and the G5 manual pass need a signed-in account and a reachable `mtm_waitlist`; no
credential was available for this session and the reviewer was not present to supply one. Those steps are
recorded as **environment-gated and not verified**, per the instruction in the task and in `quickstart.md`'s
prerequisites. They are the outstanding item on this feature, and G5 remains open until someone walks them on
a signed-in build. The automated halves of the same claims (T005–T008, T016–T018, T024–T026, T032, T036) are
green and are the evidence of record in the meantime.

**T050 — cross-check against `data-model.md` §1–§5 and `surface-contracts.md` C1–C5**

| Clause | Result |
| --- | --- |
| §1 block states and invariants 1–3 | met. `Failed` is never rendered as `Hidden` (`IsSectionLoadFailed` clears `IsItemPresent` and `IsEmptyStateVisible`); `Hidden` never carries a placeholder or an empty labelled shell (`AddSection` drops sourceless rows and sourceless sections); the page-level store-outage state was not reused — the detail page has no such state and a failed read leaves it untouched. |
| §2 fabricated-attribute inventory | met — every group removed; nothing substituted. |
| §3 control inventory | met — the card buttons are gone and the real status stands in their place; the confirm step's queue card is gone; the alerts toggle is capability-gated with no preference written; the privacy entry and its keys are gone. |
| §4 search registration | met — the declared list holds the ten members and the coverage check is discovery-driven. |
| §5 notification activation | met, with C1's helper as the single entry point for both paths. |
| C1 card | met. Rows render only when sourced and hide their label with them through `BoolToVisibilityConverter`; nothing inside a list template is deferred with `x:Load`; a card with no attribute rows still renders. |
| C2 detail | met **except the literal `x:Load`-per-section mechanism** — see the Phase 3 deviation note. Behaviour is as specified; the mechanism is one named `x:Load`-gated failure block plus collection membership for hidden sections. |
| C3 confirm | met — no count, no estimate, no stand-in card; the coil card is unchanged; the average weight is shown only when resolved. |
| C4 settings | met — three keys, no duplicate `x:Uid` anywhere in the file, no privacy hyperlink or values, the ten members registered, and the version value non-empty on this build. |
| C5 cross-cutting | met, except that `AppNotificationSamplePayload` is **retained** because it has a consumer (`App.xaml.cs:323`). Recorded in the Phase 1 drift note. |

**T051 — no database artifact, no changed procedure call, no inline SQL**

`git status` over the whole change shows **no file under `Database/`** modified or added, so no schema
altifact, seed, master-list entry or stored procedure changed. Nothing in this feature issues a query:
every edit is to a view model, a XAML view, a resource file, a test, or a document. Constitution III is not
engaged, and `InlineSqlAuditTests` stays green as the standing proof.

**Outstanding at hand-off**

1. T049 / G5 — the signed-in walkthrough, environment-gated (above).
2. The `AppNotificationSamplePayload` launch-time toast, deliberately left in scope-neutral state and recorded
   as a follow-on candidate in the Phase 1 notes and in `FEATURES.md`.
3. `WeekendProject/Module_Mock/Tasks.md`'s six unticked boxes, each with its reason recorded in that file.
4. The `WeekendProject/PromptFiles/*` snapshots still quote the retired toggle; §3.1 of
   `OPEN-WORK-NEXT-SPEC.md` retires those files rather than correcting them, which is why T042 left the sweep
   box unticked.

