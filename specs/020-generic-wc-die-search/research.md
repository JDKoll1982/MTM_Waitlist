# Research: Generic Work Center — Die Search

The spec's design note asked for the screen's design to be settled before it was built. These are those decisions, plus the ones the codebase did not settle on its own. Each is grounded in a file that already exists in this repository, because the plant SQL Server (`VISUAL`/`MTMFG`) is not reachable from this workstation and no live data could be consulted.

## R1 — How a part number finds every die it has

**Decision**: the read answers from two relations and prefers the explicit one. `PART_ALTERNATE` rows whose `ALTERNATE_PART_ID` is an `FGT` part are the primary association. The part's work-order history completes the list: `WORK_ORDER` rows whose `PART_ID` is the searched part, joined to `REQUIREMENT` on the five work-order keys, where the requirement's `PART_ID` is an `FGT` part. A die found by both is listed once, and the explicit association wins when the two disagree about its description or location.

**Rationale**: FR-003 asks for exactly this precedence and the parent spec's A10 permits it. `PART_ALTERNATE` is the only table in the exported schema that records a part-to-part association an engineer maintains deliberately; the requirement history is what the work-order flow already treats as "the dies this part runs with" (`GetSubordinateParts.sql`, `req.PART_ID LIKE 'FGT%'`).

**Alternatives considered**: `PART_SUBSTITUTE` (substitutes are like-for-like material, not tooling, so a die would appear only by accident); searching every `FGT` part's description for the part number (a description match is not an association and would invent ties); storing the association in the internal store (forbidden — dies belong to Infor Visual, and a second copy would drift).

## R2 — What the typed term is

**Decision**: the term is normalised by `PartMatchSearchInput.Normalize`, the rule the part search already applies: upper-case, and zero-pad the digits to ten characters when the term starts with a known prefix (`FGT`, `MMC`, `MMF`). A normalised term that starts with `FGT` is a **die number** and is matched exactly; if Infor Visual does not record that die the search answers nothing and the flow refuses, which is FR-002. Any other term is a **part number**, matched exactly against the explicit association and the work-order history. A term with no known prefix is also tried once as a die number, prefixed `FGT` and zero-padded when its remainder is all digits, so `602` finds `FGT0000602`.

**Rationale**: FR-004 asks for the existing normalisation and nothing else. The die-number classification is what makes FR-002 possible: a typed die number that exists nowhere has to be told apart from a part number that owns no dies, and the `FGT` family is how this plant names a die (`GetPartMatches.sql`, `GetSubordinateParts.sql`, `PartPictureLayout.KnownPartNumberPrefixes`).

**Alternatives considered**: treating every term as both a die number and a part number (a misspelled die number then silently returns another part's dies, which is the misdirection FR-002 exists to prevent); fuzzy `LIKE` matching over die numbers (a wrong die is the expensive error the spec names, so the die path stays exact).

## R3 — Where a die is

**Decision**: the die's location is `PART_SITE.PRIMARY_LOC_ID`, the first non-empty primary location by `SITE_ID`. When Visual records none, it is the `PART_LOCATION` row with the lowest `LOCATION_ID`. When neither exists the read answers no location, and the screen and the card say so in words rather than drawing an empty cell.

**Rationale**: `GetSubordinateParts.sql` already resolves a die's home this way and records why: a die is not inventoried, every `PART_LOCATION` row for one carries quantity zero, so ranking those rows by quantity is meaningless. Ordering the fallback by location id keeps it deterministic. FR-006 and SC-005 forbid a blank location cell.

**Alternatives considered**: the stock row holding the most quantity (meaningless for a die — the reason the subordinate query switched to `PRIMARY_LOC_ID`); no fallback at all (A2 asks for one).

## R4 — What the requester picks, and how

**Decision**: each result row carries its own `IsSelected`, toggled by a tap, and the step continues when at least one row is marked. The existing single-selection `ListView` is replaced by that, and a count line is drawn above the list. Selections survive a new search: a row that comes back is marked if its die number was chosen before.

**Rationale**: this is the shipped New Request die step's own pattern (`NewRequestDieViewModel.SelectDie`, `NewRequestDieOption.IsSelected`), and it is what FR-010 asks for: one or several, marked again when the list is revisited. A `ListView` with `SelectionMode="Multiple"` cannot be two-way bound to a plain collection, so it would need code-behind to read the selection, which the rest of this flow does not do.

**Alternatives considered**: `ListView.SelectedItems` read in code-behind (a second source of truth beside the draft, and not bindable); a single-selection list plus a separate "add another" step (two screens for one question).

## R5 — One request per die, and what the card knows afterwards

**Decision**: the draft keeps the chosen dies in a list and keeps the first chosen in the existing `DieNumber` field for the screens that already read it. Raising the request walks the chosen dies and asks the queue once per die, with that die's number as the request's captured value. Before the walk, each request is given a `GenericRequestDieAnswer` holding the die's number and its location, written into the answers column. A generic die request's card reads that answer for both its die number and its die location. The confirmation step lists every chosen die, and the result step says how many requests were raised.

**Rationale**: `NewRequestFlowState.ToDrafts()` and FR-054 of the work-order flow already established one request per die, and the request's single value column is what keeps two entries apart on the queue. A generic request has no job for the card to look a die up in (`WaitlistRequestTitles.ResolveContext` passes a null job), so the location has to ride the request if the card is to name it (FR-012).

**Alternatives considered**: one request carrying several dies (the queue's duplicate rule would collapse them, and the card has one value column); resolving the location on the board by asking Visual when the card is drawn (a read per card per list load, and the board's die cell is filled from the job today).

## R6 — Where the die read lives

**Decision**: a new seam, `IGenericRequestDieData`, implemented by `GenericRequestDieData` in `MTM_Waitlist.GenericRequest.Data`, registered in the composition root; `GenericRequestLiveStepData.ReadDiesAsync` delegates to it and its step availability stops reporting the Die step unavailable. The failure taxonomy is the one the part read already uses: an unreachable or failed source is `Unavailable`, an empty result is `Empty` with the source named in the wording, and rows are `Populated`.

**Rationale**: the part step's own read (`IGenericRequestPartData`) set this precedent in this solution: a read with its own seam, its own tests, and no page knowing where the data came from. `GenericRequestLiveStepData` is already the delegating seam for every other step.

**Alternatives considered**: implementing the read inside `GenericRequestLiveStepData` (that class is 28 KB and would gain Visual SQL knowledge it does not have today); a static helper on the view model (untestable without a page).

## R7 — A die whose description is the plant's placeholder

**Decision**: the die search shows the die's own description as Visual records it, including `Die still needs location`. It never rewrites that text, and it never treats it as a location. The work-order flow's `DieDecisionRules` transform (`No Die`) is applied only where a job's subordinate rows are read, not here.

**Rationale**: FR-009 says exactly this, and the two readings serve different questions: on a job the placeholder means "this job has no die", while in a search it is a die that exists and has not been given a home yet.

**Alternatives considered**: filtering such dies out of the results (the requester could not then ask for a die the plant records, and the spec explicitly asks that the text be shown).

## R8 — The die search is never answered from the cache

**Decision**: the search has no entry in the visual read-shape catalog and no `mtm_mock` mirror. It is a live-only read, like the incorrect-part search.

**Rationale**: spec A4 says so, and FR-084 of the part-picture work already established the boundary: a contains-or-association search is not a mirrorable shape. The reachability probe still governs the live attempt, and an unreachable source is reported to the requester with a retry rather than being filled from cache.

**Alternatives considered**: adding a sixth read shape (it would have to mirror every die association on three tables, and the app never refreshes the mirror).

## R9 — The dead preview

**Decision**: no new `Temp` artefact is introduced, and the die card variant this chunk replaces is the one now shipping as the generic die card. The boundary audit (`TempArtefactBoundaryAuditTests`) stays green and is the check for FR-014.

**Rationale**: the preview application's source is already gone from the repository (only ignored build output remains on this workstation); the audit is the guard that keeps it gone.
