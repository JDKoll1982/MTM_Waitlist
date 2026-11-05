# Tasks: Generic Work Center — Stated Part, Sequences and Quantity

**Feature**: 017 Generic Work Center — Stated Part, Sequences and Quantity
**Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md) | **Contracts**: [contracts/](./contracts/)

## Scale note

An oversized change: seven checked-in query scripts against two external systems, two new contracts,
one new data library, one rewritten screen, an extended card, and three test areas. A reader should
watch two lines. The **live line**: every part, sequence and quantity is read live at the moment it is
shown, and no `mtm_mock` shape is added for any of them (FR-017). The **derived line**: the requester
no longer says whether the part is work in progress or a finished good; the chosen sequence's
operation state decides, and the screen shows that decision rather than asking for it (FR-012).

---

## Phase 1: Setup

**Wave 1 — independent:**

- [x] **T001** Create the data library, add it to the solution, and reference it from the app, so the live part reads have a home that is not the WinUI screens library · `MTM_Waitlist.GenericRequest.Data/MTM_Waitlist.GenericRequest.Data.csproj`, `MTM_Waitlist.sln`, `MTM_Waitlist.csproj`

---

## Phase 2: Foundational

Blocks every story: the screen, the seam and the scripts are one collaboration.

**Wave 1 — independent (different files):**

- [x] **T002** [P] Return the part's description beside its number from the live fuzzy part search (FR-001) · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql`
- [x] **T003** [P] Add the Visual sequence-list script: the part's own-part sequences and its requirement-at-an-operation sequences in one read, each with its status, its outside-service signal and which relation named it (FR-004, FR-005, FR-009) · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequences.sql`
- [x] **T004** [P] Add the Visual sequence-state script, re-read at quantity time so the owner is decided from the state now and not from the list's status (FR-009, R4) · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequenceState.sql`
- [x] **T005** [P] Add the floor part-match script, matching `inv_inventory.PartID` and excluding dunnage rows (FR-001) · `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipInventoryPartMatches.sql`
- [x] **T006** [P] Add the floor operation-list script, keeping only distinct numeric operations and dropping `N/A`, blank and non-numeric values (FR-004, FR-006) · `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipPartOperations.sql`
- [x] **T007** [P] Add the floor operation-quantity script for one part at one operation (FR-009, FR-013) · `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipOperationQuantity.sql`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T008** Add the three floor reads, each reporting whether it succeeded so "the floor holds none" and "the floor could not be asked" stay distinct (FR-011, FR-017) · `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs`
- [x] **T009** Reshape the part option around its source, and the sequence option around every source that supplied it, and add `SequenceSource` (FR-001, FR-004) · `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`
- [x] **T010** Add the quantity reading with its kind, its figure and the system that answered (FR-013, FR-014, FR-017) · `MTM_Waitlist.GenericRequest/Models/GenericRequestQuantityReading.cs`
- [x] **T011** Add the outside-service and unrecorded cases to the part stage, and derive it rather than ask for it (FR-009, FR-010, FR-011, FR-012) · `MTM_Waitlist.GenericRequest/Contracts/GenericRequestStep.cs`
- [x] **T012** Add the part-shaped seam: completions, sequences and quantity, each reporting its presentation and its attempt time (FR-017) · `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestPartData.cs`
- [x] **T013** Record the chosen sequence's sources, the derived stage and the reading on the draft, and clear them when the part changes (FR-008, FR-016) · `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`
- [x] **T014** Add the part step's resource keys: refusals, per-region retries, quantity, derived stage and the three source names (FR-002, FR-003, FR-013, FR-014, FR-017) · `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`
- [x] **T015** Add the card's sequence, on-hand and not-inventoried labels and the two system names (FR-018, FR-019) · `MTM_Waitlist.GenericRequest.Controls/Strings/en-us/Resources.resw`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T016** Implement the seam: completions merged from both systems, sequences merged from three sources with a number counted once, and the quantity owned by the chosen sequence's operation state (FR-001, FR-004, FR-009, FR-011) · `MTM_Waitlist.GenericRequest.Data/Services/GenericRequestPartData.cs`
- [x] **T017** Show the derived stage for all four cases on the review, since the two-way choice it replaced is gone (FR-012) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T018** Register the part seam and its live implementation beside the rest of the flow's services · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`
- [x] **T019** Forward the wide seam's part and sequence reads to the part seam, so a caller holding either interface reaches the same live implementation (R7) · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`

**Checkpoint**: the solution builds and every read reports its own presentation. No story work begins
until this phase is green.

---

## Phase 3: User Story 1 — a stated part is proved, and its sequences are listed (P1)

**Goal**: the requester types a part, completions come from Infor Visual and the floor, and the
accepted part's sequences are listed once each with their sources.

**Independent test**: type a part carrying several sequences and confirm all are listed once with
sources; type a part only the floor holds and confirm it is accepted with the floor named; type a
number neither knows and confirm the step refuses and names both.

**Files**: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestPartViewModel.cs`,
`MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartDataTests.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartViewModelTests.cs`

**Wave 1:**

- [x] **T020** Rewrite the part step around its three reads: completions as they are typed, the sequence list when a part is accepted, and the quantity region when a sequence is chosen; drop the stage selector and the list filter (FR-001, FR-004, FR-007, FR-012) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestPartViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T021** Replace the part page: a completion box that suggests as the requester types, a sequence list showing each number's sources and its derived stage, and a quantity region with its own unavailable line and retry (FR-001, FR-004, FR-017, plan OD-1, plan OD-4) · `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T022** [P] Prove the completions come from both systems with their sources, the sequence union counts a number once, a cancelled-only sequence is not offered, and `N/A` is not a sequence (FR-001, FR-004, FR-005, FR-006) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartDataTests.cs`
- [x] **T023** [P] Prove the step refuses an unknown number naming both sources, refuses a part with no sequence, clears the sequence and reading when the part changes, and cannot advance without a chosen sequence (FR-002, FR-003, FR-008, FR-017) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartViewModelTests.cs`

**Checkpoint**: a stated part is proved and its sequences are listed, independently of the quantity.

---

## Phase 4: User Story 2 — the chosen sequence decides where the quantity comes from (P1)

**Goal**: the chosen sequence's operation state picks the owning system, the figure is shown as units
on hand with that system named, and outside-service work is reported as not inventoried.

**Independent test**: choose an open sequence and a closed sequence for one part and confirm the two
figures came from different systems; choose an outside-service sequence and confirm the step says not
inventoried and shows no number.

**Files**: `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestQuantityTests.cs`

**Wave 1:**

- [x] **T024** Prove the owner decision: an open operation reads the floor, a closed one reads Visual by part number, outside service reads nothing and reports not inventoried, an unrecorded state tries the floor first, zero is shown as a figure, and an unreachable system reports unavailable without a figure (FR-009, FR-010, FR-011, FR-013, FR-014, FR-017) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestQuantityTests.cs`

**Checkpoint**: the quantity is attributed to the system that owns the sequence and the step can be
left only with an answered reading (FR-015).

---

## Phase 5: User Story 3 — the part answer reaches the card (P2)

**Goal**: a generic part request's card shows the part, the chosen sequence and the quantity with the
system that supplied it, or that the part is not inventoried.

**Independent test**: raise a generic part request, view its card, and confirm the part, sequence,
quantity and system appear, and that an outside-service request says not inventoried.

**Files**: `MTM_Waitlist.GenericRequest.Controls/Models/GenericRequestCardModel.cs`,
`MTM_Waitlist.GenericRequest.Controls/Helpers/GenericRequestCardRules.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestCardRulesTests.cs`

**Wave 1:**

- [x] **T025** Add the chosen sequence, the on-hand figure and the answering system to the card model, beside the requested quantity rather than on top of it (FR-018, R8) · `MTM_Waitlist.GenericRequest.Controls/Models/GenericRequestCardModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T026** Draw the sequence cell, an on-hand cell carrying the answering system, and a not-inventoried cell instead of a number, while drawing no cell at all for a quantity that was never read (FR-018, FR-019, FR-020) · `MTM_Waitlist.GenericRequest.Controls/Helpers/GenericRequestCardRules.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T027** Prove the card draws a zero, omits an unread quantity, and prefers the not-inventoried cell for outside-service work (FR-013, FR-019, FR-020) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestCardRulesTests.cs`

**Checkpoint**: the card and its detail page carry the part, its sequence and its on-hand answer.

---

## Phase 6: Polish

**Wave 1:**

- [x] **T028** Validate against the Success Criteria: build the solution and run the suite · `MTM_Waitlist.sln`
- [x] **T029** [P] Record the confirmed floor operation column and the derived-stage decision in the feature's own notes, so the next reader does not re-derive them · `specs/017-generic-wc-stated-part/research.md`

---

## Phase 7: Carrying the part answer to the card

Added during implementation: FR-016 and FR-018 need the part facts to leave the flow and come back with the
request, which is a mapping task the phases above did not name.

**Files**: `MTM_Waitlist.Core/Models/GenericRequestPartAnswer.cs`,
`MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`,
`MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartAnswerTests.cs`

**Wave 1:**

- [x] **T030** Record the part, the sequence, its sources, the figure and the answering system on the request's own answers, and read them back onto the board row so a part request draws a part card (FR-016, FR-018) · `MTM_Waitlist.Core/Models/GenericRequestPartAnswer.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T031** Prove the part answer survives the round trip and that a request stating no part reads back as none (FR-016) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartAnswerTests.cs`

**Checkpoint**: a raised part request states the same four facts on the board that the requester saw.

---

## Dependencies & Execution Order

Setup → Foundational → US1 → US2 → US3 → Polish.

- **Setup** (T001): wave 1 only.
- **Foundational**: wave 1 is the six scripts, all independent. Wave 2 (the Core floor reads, the
  models, the contracts, the draft, both resource files) waits on wave 1. Wave 3 (the seam
  implementation and the review's derived stage) waits on wave 2. Wave 4 (registration, forwarding)
  waits on wave 3. No story begins before this phase is green.
- **US1**: wave 1 the view model, wave 2 the page, wave 3 the two proofs in parallel.
- **US2**: one task, proving the decision Foundational implemented; it waits on US1 because the step
  it drives is that screen.
- **US3**: wave 1 the model, wave 2 the rules, wave 3 the proof.
- **Polish**: the suite run and the notes, after every story.

Every file above appears in exactly one phase.
