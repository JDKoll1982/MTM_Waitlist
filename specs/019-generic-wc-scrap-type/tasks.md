# Tasks: Generic Work Center — Stated Scrap Type and Lugger

**Feature**: `019-generic-wc-scrap-type` · **Plan**: [plan.md](./plan.md) · **Spec**: [spec.md](./spec.md) · **Wording**: [wording.md](./wording.md)
**Size**: `oversized` — nothing trimmed.

## Scale note

The change reaches five assemblies: fifteen production files, two of them new, and eleven test files. Two points carry the risk: the scrap step stops being a stub, so its read, its availability and its recorded answer move together; and the board starts supplying a card fact that until now only a work order supplied. What it must not do is give the generic card a second lugger resolution. Read a phase's `Files:` line before editing anything in it.

**No schema work.** Both reads already exist: `sp_waitlist_lugger_arrangement_list` through `ILuggerArrangementService.GetSnapshotAsync`, and `ScrapTypeCatalog.All` as a compiled constant. The stated type is stored in the request's existing `answers_json` column. No new stored procedure, no `create.sql`, no `rollback.sql`, no table or column change, and nothing here touches `InlineSqlAuditTests` on account of schema.

## Paths

Feature dir is `specs/019-generic-wc-scrap-type`. Run every command from the repo root `C:\Users\jkoll\source\repos\MTM_Waitlist`, with `python`, never `python3`.

## File ownership map

One owner per file. A file listed here appears under exactly one task, so phases fan out without a merge conflict.

| File | Task | Phase |
|---|---|---|
| `specs/019-generic-wc-scrap-type/wording.md` | T001 | Setup |
| `MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs` *(new)* | T002 | Foundational |
| `MTM_Waitlist.Core/Models/Lugger/LuggerArrangementSnapshot.cs`, `MTM_Waitlist.Core/Services/LuggerArrangementService.cs` | T003 | Foundational |
| `MTM_Waitlist.Core/Services/ScrapTypeCatalog.cs` | T004 | Foundational |
| `MTM_Waitlist.Settings/Models/StatedScrapLugger.cs` *(new)* | T005 | Foundational |
| `MTM_Waitlist.Settings/Administration/ViewModels/LuggerArrangementViewModel.cs` | T006 | Foundational |
| `MTM_Waitlist.Settings/Services/IRequestJobPartAvailabilityProvider.cs`, `Services/RequestJobAvailabilityProvider.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs` | T007 | Foundational |
| `MTM_Waitlist.Tests/Module_Core/Models/LuggerArrangementSnapshotTests.cs` *(new)* | T008 | Foundational |
| `MTM_Waitlist.Tests/Module_Core/Services/ScrapTypeCatalogTests.cs` | T009 | Foundational |
| `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapAnswerTests.cs` *(new)* | T010 | Foundational |
| `MTM_Waitlist.Tests/Module_Settings/ViewModels/LuggerArrangementViewModelTests.cs` | T011 | Foundational |
| `MTM_Waitlist.Tests/Module_Settings/Services/StatedScrapLuggerReadTests.cs` *(new)* | T012 | Foundational |
| `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapTypeViewModelTests.cs` *(new)* | T013 | US1 |
| `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapSubmissionTests.cs` *(new)* | T014 | US1 |
| `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs` | T015 | US1 |
| `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs` | T016 | US1 |
| `MTM_Waitlist.GenericRequest/Views/GenericRequestScrapTypePage.xaml` | T017 | US1 |
| `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs` | T018 | US1 |
| `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw` | T019 | US1 |
| `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests.cs` | T020 | US2 |
| `Strings/en-us/Resources.resw` | T021 | US2 |
| `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` | T022 | US2 |
| `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelGenericScrapTests.cs` *(new)* | T023 | US3 |
| `CHANGELOG.md` | T024 | Polish |

T025, T026 and T027 own no file: they run an existing audit or suite and record the result.

---

## Phase 1: Setup

**Files:** `specs/019-generic-wc-scrap-type/wording.md`

**Wave 1 — independent:**

- [x] **T001** Record the page and card wording the spec's 2026-11-03 design note requires, adopting the unattended default and marking it `STATUS: default, awaiting review`. Write: intro `Pick the scrap type this request is carrying. The card will name the lugger it goes to.`; delete `GenericRequest_Scrap_Luggers.Text`; gap `Not assigned`; outage `Temporarily unavailable`. Name the `.resw` file each key lives in, the state each belongs to, and the keys deliberately left alone (`GenericRequest_Scrap_Title.Text`, `GenericRequest_Scrap_Empty.Text`, and the frame's own unavailable keys). Every wording task below reads this file; none invents its own words. · `specs/019-generic-wc-scrap-type/wording.md` · **Check**: the file exists, names four keys, states the review flag, and says which file each key is written to.

**⟶ Wait for Wave 1 to finish, then Phase 2.**

---

## Phase 2: Foundational

**Files:** `MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs`, `MTM_Waitlist.Core/Models/Lugger/LuggerArrangementSnapshot.cs`, `MTM_Waitlist.Core/Services/LuggerArrangementService.cs`, `MTM_Waitlist.Core/Services/ScrapTypeCatalog.cs`, `MTM_Waitlist.Settings/Models/StatedScrapLugger.cs`, `MTM_Waitlist.Settings/Administration/ViewModels/LuggerArrangementViewModel.cs`, `MTM_Waitlist.Settings/Services/IRequestJobPartAvailabilityProvider.cs`, `Services/RequestJobAvailabilityProvider.cs`, and the five test files T008–T012 own plus `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs`.

Blocks every story. Nothing in Phase 3 onward starts until Phase 2's last wave is green.

### Wave 1 — independent (different files)

- [x] **T002** [P] Add the recorded answer for a raised request: sealed record `GenericRequestScrapAnswer(string ScrapType)`, `const string ScrapTypeKey = "scrap.type"`, `ToAnswers()` returning a `Dictionary<string, string>` built with `StringComparer.OrdinalIgnoreCase`, and a static `TryRead(IReadOnlyDictionary<string, string>? answers)` that answers null when the key is missing and null when the stored value is empty or whitespace. Mirror `GenericRequestDunnageAnswer` in the same folder; add no second key. · `MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs` · **Check**: `dotnet build MTM_Waitlist.Core` clean.

- [x] **T003** [P] Add `LuggerArrangementSnapshot.WasRead`: `Create` sets it true, `Empty` — also the type's initialiser — leaves it false, so an outage stops looking like an arrangement with no rows. Make no behavioural change in `LuggerArrangementService`: `GetSnapshotAsync` already returns `Empty` when the read answers null, which is now exactly the unread snapshot; correct its remark only if the remark contradicts the type. · `MTM_Waitlist.Core/Models/Lugger/LuggerArrangementSnapshot.cs`, `MTM_Waitlist.Core/Services/LuggerArrangementService.cs` · **Check**: `dotnet build MTM_Waitlist.Core` clean; T008 green.

- [x] **T004** [P] Promote the composition rule into Core as `ScrapTypeCatalog.Compose(IEnumerable<string>? assignedTypes)`: the catalogue first in `All`'s own order, then every assigned name the catalogue does not already hold, de-duplicated on the trimmed name case-insensitively, dropping `ScrapDecisionRules.RequiredPlaceholder` and `ScrapDecisionRules.NoScrap`. A null or empty argument yields the catalogue alone. This becomes the one home of the FR-002 rule. · `MTM_Waitlist.Core/Services/ScrapTypeCatalog.cs` · **Check**: `dotnet build MTM_Waitlist.Core` clean; T009 green.

- [x] **T005** [P] Add the record a stated scrap type resolves to: `StatedScrapLugger(string? LuggerLocation, string LuggerImagePath, bool ArrangementRead)`. Three outcomes it must be able to say: a named lugger (`true`, the name), a configuration gap (`true`, null) and an unreadable arrangement (`false`, null). It carries no state of its own and reads nothing. · `MTM_Waitlist.Settings/Models/StatedScrapLugger.cs` · **Check**: `dotnet build MTM_Waitlist.Settings` clean.

**⟶ Wait for Wave 1 to finish, then:**

### Wave 2 — independent (different files)

- [x] **T006** [P] Replace `LuggerArrangementViewModel`'s private `BuildScrapTypeOptions` with a call to `ScrapTypeCatalog.Compose(snapshot.ScrapTypes)` and delete the private copy, so the editor and the generic step cannot drift on the ordering or the de-duplication rule. The picker's visible choices do not change. · `MTM_Waitlist.Settings/Administration/ViewModels/LuggerArrangementViewModel.cs` · **Check**: `dotnet build MTM_Waitlist.Settings` clean; T011 green; no `BuildScrapTypeOptions` remains in the file.

- [x] **T007** [P] Split the lugger resolution so one routine serves both entry points. Add `Task<StatedScrapLugger> GetStatedScrapLuggerAsync(string scrapType, CancellationToken cancellationToken = default)` to the interface, and in the composition root turn `ApplyLuggerLocationAsync`'s body into one private routine that takes the scrap type and answers a `StatedScrapLugger` — the arrangement read, `Resolve`, the gap warning when the arrangement was read, and the lugger picture. `ApplyLuggerLocationAsync` calls it and copies the result onto the record exactly as today; `GetStatedScrapLuggerAsync` returns it unchanged. The stated path reads no job and knows no work center. An unread arrangement answers `ArrangementRead: false` and raises no configuration warning. Update the `StubJobAvailabilityProvider` in the detail view model's tests with the new member, because the interface change breaks it. · `MTM_Waitlist.Settings/Services/IRequestJobPartAvailabilityProvider.cs`, `Services/RequestJobAvailabilityProvider.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean, then `dotnet build MTM_Waitlist.Tests` clean, because the interface grows a member and both the app's implementation and the tests' stub must satisfy it; the work-order card's behaviour is unchanged.

**⟶ Wait for Wave 2 to finish, then:**

### Wave 3 — independent (different files)

- [x] **T008** [P] Test the snapshot's read state: `Create` over rows answers `WasRead` true; `Create` over zero rows answers true, because an arrangement with no rows was read; `Empty` answers false; a default-initialised value answers false; a snapshot reports `WasRead` false and empty `Rows` together for the outage case. Fail-first against T003. · `MTM_Waitlist.Tests/Module_Core/Models/LuggerArrangementSnapshotTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~LuggerArrangementSnapshotTests` green after T003.

- [x] **T009** [P] Extend the catalogue's tests with `Compose`: the catalogue's own order is kept; an assigned outside type is appended after it; `5052 ALUMINUM ` and `5052 aluminum` produce one choice; the placeholder and no-scrap spellings are never offered even when assigned; null and empty arguments yield the catalogue alone; a type differing inside its spelling is a second choice. Fail-first against T004. · `MTM_Waitlist.Tests/Module_Core/Services/ScrapTypeCatalogTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~ScrapTypeCatalogTests` green after T004.

- [x] **T010** [P] Test the recorded answer: `scrap.type` round-trips through `ToAnswers` and `TryRead` in the requester's own wording and spacing; key lookup ignores case; a missing key, an empty value and a whitespace value each answer null rather than throwing; a null dictionary answers null. Fail-first against T002. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapAnswerTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~GenericRequestScrapAnswerTests` green after T002.

- [x] **T011** [P] Prove the editor uses the promoted rule: its scrap-type choices are the catalogue in order followed by the arrangement's outside types, a case variant is not offered twice, and neither decision sentinel appears — the same assertions T009 makes, driven through the view model so a reintroduced private copy fails here. · `MTM_Waitlist.Tests/Module_Settings/ViewModels/LuggerArrangementViewModelTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~LuggerArrangementViewModelTests` green after T006, and green against the pre-existing cases in that file.

- [x] **T012** [P] Audit the split at the source, since the test project does not reference the app project and the provider cannot be instantiated from a test: assert `Services/RequestJobAvailabilityProvider.cs` declares `GetStatedScrapLuggerAsync`, that both entry points call one and the same private routine, that the routine contains the only `GetSnapshotAsync` call in the file, and that the stated entry point takes no work center and no job snapshot. · `MTM_Waitlist.Tests/Module_Settings/Services/StatedScrapLuggerReadTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~StatedScrapLuggerReadTests` green after T007; the audit names the offending path when it fails.

**⟶ Wait for Wave 3 to finish, then Phase 3.**

---

## Phase 3: User Story 1 — the requester states the scrap type (P1)

**Files:** `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs`, `MTM_Waitlist.GenericRequest/Views/GenericRequestScrapTypePage.xaml`, `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`, and the three test files T013 and T014 own.

**Goal**: raise a generic scrap request and choose its type from the plant's one list, and advance no further without one.
**Independent Test**: raise a generic scrap request, confirm the choices match the shared list in its own order, then try to continue without choosing and confirm the flow does not advance.

Two things this phase does not author, because they already exist and are verified in Polish: `GenericRequestDraft.ScrapType` is unchanged and is what this phase fills, and `GenericRequestSummaryViewModel` already adds the confirmation row from it under `GenericRequest_Summary_Scrap.Label`. Add no summary work and no draft field.

### Tests

**Wave 1 — independent (different files):** write these first and let them fail until the implementation waves land.

- [x] **T013** [P] [US1] Test the step: the read's `Populated` maps to the step's `Populated` and `Unavailable` to `Unavailable` carrying the attempt time; `CanAnswer` is false with nothing chosen and the Continue command cannot execute; choosing a type enables it; choosing writes `Draft.ScrapType`; revisiting the step shows the stored type as `Selected`, matched on the trimmed, case-insensitive name; nothing pre-selects when the draft holds none. Fail-first. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapTypeViewModelTests.cs` · **Check**: fails before T015–T018, green after.

- [x] **T014** [P] [US1] Extend the generic-request doubles with a fake `ILuggerArrangementService` (a snapshot in hand, an unread `Empty`, a throwing read) and test the raise: a draft asking for scrap with a stated type records `scrap.type` in the answers dictionary in the requester's wording; a scrap draft with no stated type records no scrap key; an item that does not ask for scrap records none even when the draft carries a leftover type; `input_value` still carries what it carried before, so the type is not stored there. Add every double the later scrap tests need, so no later phase edits this file. Fail-first. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapSubmissionTests.cs` · **Check**: fails before T016, green after.

### Implementation

**Wave 2 — the option's shape, which the step and the page both read:**

- [x] **T015** [US1] Reduce `GenericRequestScrapOption` to the scrap type: delete `AvailableLuggerCount` and `AvailableLuggerDisplay`, and drop the `System.Globalization` import if nothing else in the file uses it. Nothing populated the count; it is the deleted preview's leftover. · `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs` · **Check**: `dotnet build MTM_Waitlist.GenericRequest` clean; `AvailableLuggerCount` appears nowhere in the tree.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — independent (different files):**

- [x] **T016** [P] [US1] Make the read real. Take `ILuggerArrangementService` as a constructor dependency, have `ReadScrapTypesAsync` read one snapshot and answer `GenericRequestStepResult<GenericRequestScrapOption>.Populated(ScrapTypeCatalog.Compose(snapshot.ScrapTypes).Select(type => new GenericRequestScrapOption(type)), attemptedAt)` in the composed order, and answer `Unavailable` — with no list at all — when `snapshot.WasRead` is false. Stop `ReadStepAvailabilityAsync` marking `GenericRequestStep.Scrap` unavailable, leaving `Die` as the only unimplemented source. In `AnswersForAsync`, merge `GenericRequestScrapAnswer(draft.ScrapType).ToAnswers()` when `draft.RequiresScrap` and `draft.ScrapType` is not empty. Correct the class remark that still calls the scrap read unimplemented. · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs` · **Check**: T014 green; `dotnet build MTM_Waitlist.GenericRequest` clean; the read reports an outage as `Unavailable` and never as `Empty`.

- [x] **T017** [P] [US1] Take the count out of the page: `GenericRequestScrapTypePage.xaml`'s item template becomes the scrap type alone, dropping the `GenericRequest_Scrap_Luggers` label and the `AvailableLuggerDisplay` column and their column definitions. Change nothing else on the page: the list, the selection binding, and the step frame stay as they are. · `MTM_Waitlist.GenericRequest/Views/GenericRequestScrapTypePage.xaml` · **Check**: `dotnet build MTM_Waitlist.csproj` clean with no masked `WMC9999`; `AvailableLuggerDisplay` and `GenericRequest_Scrap_Luggers` no longer appear in the file.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — independent (different files):**

- [x] **T018** [P] [US1] Gate the step on its own answer: override `CanAnswer` as `Selected is not null`, and notify the Continue command whenever `Selected` changes. In `LoadCoreAsync`, keep the list in the seam's order and re-select the draft's stored type by trimmed, case-insensitive comparison, leaving `Selected` null when the draft holds none. `CommitToDraft` keeps writing the chosen type. It asks no second question and adds no state. · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs` · **Check**: T013 green; `dotnet build MTM_Waitlist.GenericRequest` clean.

- [x] **T019** [P] [US1] Apply the wording: rewrite `GenericRequest_Scrap_Intro.Text` to the sentence in `wording.md` and delete `GenericRequest_Scrap_Luggers.Text` with the column it labelled. Leave `GenericRequest_Scrap_Title.Text` and `GenericRequest_Scrap_Empty.Text` exactly as they are, and change no other key's value. · `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw` · **Check**: the intro resolves to the sentence in `wording.md`; `GenericRequest_Scrap_Luggers` appears in no `.resw` and no XAML.

**Checkpoint**: a generic scrap request states its type, cannot advance without one, goes to the confirmation rather than the details step, and records the type on the raise. US2 and US3 build on this.

---

## Phase 4: User Story 2 — the card says where the scrap goes (P1)

**Files:** `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Strings/en-us/Resources.resw`, and the test file T020 owns.

**Goal**: a generic scrap card names the stated type and the lugger the arrangement assigns now, through the same resolution the work-order scrap card uses.
**Independent Test**: raise a generic scrap request, confirm the card names the assigned lugger, mark that lugger full, and confirm the card follows the arrangement to its backup.

### Tests

**Wave 1 — independent (different files):** write these first and let them fail until T022 lands.

- [x] **T020** [P] [US2] Assert the merge on the card the board builds. Extend this file's `BuildViewModel` with a fake `IRequestJobPartAvailabilityProvider` whose `GetStatedScrapLuggerAsync` answers per stated type and whose `GetAvailabilityAsync` answers `None`, then `RefreshAsync` a generic scrap request carrying a stated type. Assert: its scrap-type cell and its title carry the stated wording; its lugger cell carries the lugger the fake answers for that type; an all-blocked answer renders `Door 9 Lugger (Full)` unchanged as the value; a gap answer renders `WaitlistRequest_Scrap_LuggerNotAssigned.Text` resolved through `GetLocalized()`; an outage answer renders `WaitlistRequest_Scrap_LuggerUnavailable.Text` resolved the same way; each of those two values is asserted to differ from its key, so a missing resource fails rather than passing vacuously; both states leave `DrawsPicture` true with an empty picture path, which is today's behaviour for a lugger with no picture and is pinned rather than fixed; a work-order scrap request for the same job is byte-identical to what it renders today and never calls the stated read; a generic request that is not the scrap item is untouched; a provider whose call throws leaves the card as it is today. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests.cs` · **Check**: fails before T022, green after; the work-order and non-scrap cases pass before T022 as evidence the merger is guarded.

- [x] **T021** [P] [US2] Add the card's two wordings to the application's resources, exactly as `wording.md` records them: `WaitlistRequest_Scrap_LuggerNotAssigned.Text` = `Not assigned`, `WaitlistRequest_Scrap_LuggerUnavailable.Text` = `Temporarily unavailable`. Change no existing key. · `Strings/en-us/Resources.resw` · **Check**: both keys resolve, and neither resolves to its own key name.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T022** [US2] Merge the stated facts into the card's availability. In the load loop, after `ResolveJobAvailabilityAsync`, and only where `WaitlistCardCellRules.IsScrapItem(request.Item)` and `request.IsGeneric` both hold, read the stated type from `GenericRequestScrapAnswer.TryRead(request.Answers)?.ScrapType`, call `_jobAvailabilityProvider.GetStatedScrapLuggerAsync(statedType)` once for that request, and pass `CreateSessionOrder` and `ResolveDeclaredCells` a `RequestJobPartAvailability` carrying that type in `ScrapType`, the resolved lugger in `LuggerLocation` and its picture in `LuggerLocationImagePath`, or — for a gap or an outage — the `wording.md` sentence as `LuggerLocation` with an empty picture. Leave `ResolveJobAvailabilityAsync` and its per-work-center cache alone: the merged record is per request. A request with no stated type, a null provider, or a failed call keeps the availability it had and the card draws as it does today. Teach no second resolution and add no cache. · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · **Check**: T020 green; `dotnet build MTM_Waitlist.Waitlist.View` clean; `CreateSessionOrder` and `ResolveDeclaredCell` themselves are unchanged.

**Checkpoint**: a generic scrap card names its type and the lugger assigned now, follows the arrangement's blocking and backup rules, and reports a gap or an outage in words rather than drawing blank. A work-order scrap card is untouched.

---

## Phase 5: User Story 3 — the stated type survives the arrangement changing (P2)

**Files:** `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelGenericScrapTests.cs`

This story owns no production file, and that is the point: the wording comes from the request's own `answers_json` and the lugger is a live read, so nothing has to be built for it. Its work is the tests that would fail if either half were ever copied onto the other.

### Tests

**Wave 1:**

- [x] **T023** [US3] Pin the surviving change, driving the row the way T020 does: a request carrying a type the arrangement no longer holds still names that type in its stored wording and reports the lugger cell as not assigned; a request carrying an old spelling still shows the old spelling after the arrangement renames the type; a type reassigned to a different lugger draws the lugger the fake read assigns now, not the one it assigned on a previous load; a type outside the shared list, recorded on a request, still draws with its type named. Reuse the fake provider T020 defines. Each case goes through the board's own read, so a change that starts rewriting the stored answer, or starts caching the lugger on the request, fails here. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelGenericScrapTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~WaitlistViewViewModelGenericScrapTests` green after T022.

**Checkpoint**: FR-011, FR-012 and FR-013 hold by test, and the card cannot quietly rewrite what the plant was told.

---

## Phase 6: Polish

**Files:** `CHANGELOG.md`, and `docs/guides/guides-xaml-surface-inventory.md` only if T025's audit demands an entry change.

**Wave 1 — independent (different files):**

- [x] **T024** [P] Record the change for readers: the generic scrap step now asks for the scrap type from the plant's one list and records it on the request, and a generic scrap card resolves its lugger through the same rule the work-order card uses, with no schema change. · `CHANGELOG.md` · **Check**: one entry naming the feature and the user-visible effect.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T025** Run the surface inventory audit and record the result. The scrap page keeps its path and its inventory row, and no XAML surface is added, moved or renamed, so the expected outcome is no edit. Edit the inventory only if the audit fails, and then only the row it names. Do not restructure the document. · read-only against `docs/guides/guides-xaml-surface-inventory.md` and `MTM_Waitlist.Tests/Module_Core/Views/SurfaceInventoryAuditTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~SurfaceInventoryAuditTests` green.

- [x] **T026** Verify FR-014 rather than re-do it: the `Temp` preview source and the scrap card variant are already gone, removed in `723cec0f` and guarded by the audit that keeps them out. Run that audit and record the result. Invent no deletion or rename work. · read-only against `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~TempArtefactBoundaryAuditTests` green.

- [x] **T027** Verify FR-015 and FR-016 rather than author them: `GenericRequestFlowRules.BuildOrder` already adds `Scrap` when `RequiresScrap` is set and treats it as answered on its own step, so the order is Scrap, Summary, Result and the details step is never visited, and `PreviousStep` already returns from Summary to Scrap. Run the flow-rule tests and record that the scrap cases are the ones that prove it. Author no details stop and add no step. · read-only against `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestFlowRulesTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~GenericRequestFlowRulesTests` green, with the scrap data rows named in the report.

**⟶ Wait for the checks above to finish, then:**

- [x] **T028** Validate against the Success Criteria. No post-implement hook owns validation (`.specify/companion.yml` is absent), so this task runs it. Clean solution build, then the full suite. Treat a masked `WMC9999` as a XAML error, confirm no `NETSDK1022`, and confirm no `[RelayCommand]` method lost its name to trailing-`Async` stripping. Record against SC-001 a type chosen and the request raised in one pass, SC-002 no card without a type and none blank where a lugger is named, SC-003 the generic and work-order cases naming the same lugger, SC-004 the backups followed by the arrangement tests, SC-005 the T023 survival cases, and SC-006 no description asked and no details step visited. · `MTM_Waitlist.sln` · **Check**: `dotnet build MTM_Waitlist.sln` zero errors and `dotnet test` green, both recorded in the report.

---

## Dependencies & Execution Order

**Phase order**: Setup → Foundational → US1 → US2 → US3 → Polish. US1 is the MVP slice. US2 depends on US1's recorded answer (`scrap.type`) and on Foundational's `GetStatedScrapLuggerAsync`. US3 depends on US2's merge and adds tests only.

- **Setup**: one wave. T001 blocks T019 (the intro sentence and the deleted key) and T021 (the card's two sentences).
- **Foundational**: Wave 1 (T002, T003, T004, T005) → Wave 2 (T006, T007) → Wave 3 (T008, T009, T010, T011, T012). Wave 2 holds the two consumers of Wave 1: T006 calls T004's `Compose`, T007 answers T005's record and reads T003's `WasRead`. Wave 3 is the whole phase's proof, and every task in it is a different file.
- **US1**: Wave 1 tests (T013, T014) → Wave 2 option (T015) → Wave 3 (T016 the read, T017 the page) → Wave 4 (T018 the step, T019 the words). T016 needs T015's option shape and Foundational's `Compose` and `WasRead`; T017 needs only T015. T018 needs T016; T019 needs T017 so the deleted key is no longer referenced.
- **US2**: Wave 1 (T020, T021) → T022. T020's assertions need T021's keys to resolve, and both fail until T022 merges. T022 needs Foundational's `GetStatedScrapLuggerAsync` and US1's recorded answer.
- **US3**: one wave (T023), after US2.
- **Polish**: Wave 1 (T024) → the three checks (T025, T026, T027) → T028 last.

**Parallel markers**: T002, T003, T004, T005, T006, T007, T008, T009, T010, T011, T012, T013, T014, T016, T017, T018, T019, T020, T021, T024. Every `[P]` task touches a file no other task in its wave touches, and has no incomplete dependency inside its own wave.
