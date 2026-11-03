# Research: Generic Work Center — Designation and Request Foundation

Every entry answers one question the plan could not settle by copying an existing pattern. Each records what was
chosen, why, and what was rejected.

## R1. Where a generic request is written

**Decision**: a generic request is raised through the existing `IWaitlistRequestService.SubmitAsync` seam, extended
with a generic flag, a description and the accepted warnings.

**Rationale**: FR-009 requires the same queue and the same stored-procedure path as every other request. `SubmitAsync`
already holds the write, the affected-rows check, the audit entry, the in-memory collection, the change notification
and the board refresh. A second seam would duplicate all of that and let the two drift, and the drift would be
invisible until a generic request failed to appear on the board.

**Alternatives considered**: a `GenericRequestSubmissionService` writing `sp_waitlist_request_insert` itself. Rejected:
two writers of one row shape, two places to change when the procedure changes, and the board refresh would have to be
re-implemented.

## R2. How a generic request is told apart from a work-order request

**Decision**: `active_setup_job_id` becomes nullable and the queue gains an explicit `is_generic` column.

**Rationale**: FR-008 forbids storing a work order number, so the column must accept NULL. But a null alone is a weak
signal: it is also what a missing value looks like, and the card, the list read and the detail page all have to answer
"which kind is this". An explicit flag makes the answer a stored fact rather than an inference, and it cannot be
confused by a future column that happens to be empty.

**Alternatives considered**: infer from `active_setup_job_id IS NULL`. Rejected: the same expression has to be repeated
in every read, migration and query, and one caller using `COALESCE` would silently reclassify the row.

## R3. Where the description and the accepted warnings live

**Decision**: two new queue columns, `description` and `accepted_warnings_json`.

**Rationale**: FR-016 requires the description to travel wherever the needed-by answer travels, and FR-019 requires
every accepted warning to be recorded. `note` is already the handler's own short note through `UpdateNoteAsync`, so
reusing it would let two different authors overwrite one field. `answers_json` holds the item's declared answers keyed
by label, so the description would arrive as if it were an answer to a declared question. Neither fits.

**Alternatives considered**: reuse `note` for the description; add the description to `answers_json` under a reserved
label. Both rejected on the grounds above.

## R4. Where the item's configured answer is asked

**Decision**: on the item step, beside the item list.

**Rationale**: the work-order flow asks the item's configured question at its own item step, so the same question is
answered in the same place in both flows (FR-006, A4). Asking it on the details step would put one item's question
next to another item's needed-by, and a requester going back to change the item would carry a stale answer.

**Alternatives considered**: a separate answer step. Rejected: it adds a screen to every item, most of which ask
nothing.

## R5. What the four externally proved answers do in this chunk

**Decision**: the part, dunnage, scrap and die reads return the unavailable presentation with the attempt time, and the
corresponding screens offer a retry.

**Rationale**: FR-012 requires the source to be reported as unavailable and forbids a text box, an offline list or any
substitute. The seam already models exactly that: `GenericRequestStepResult.Unavailable` carries the attempt instant
and produces the retry control.

**Alternatives considered**: hiding the four steps entirely for items that need them. Rejected: a request whose item
needs a part would then reach confirmation without the part, which understates the submission (FR-017) and would
change the flow's shape when chunk 017 lands.

## R6. What the generic card calls a request's stated value

**Decision**: add a fifth card variant, `Answer`, whose cell is the item's configured answer.

**Rationale**: the card delivered by spec 015 has a variant per answer kind (part, dunnage, scrap, die), and this chunk's
answer is none of those: it is the configured text or choice the item declares. FR-023 makes the stated value part of
the card's identity, so it needs a shape of its own rather than being forced into the part variant and printing a part
number that no request has.

**Alternatives considered**: reuse the `Part` variant and leave its cell empty. Rejected: the card would then draw no
identity at all, and the empty cell is the failure FR-025 forbids.

## R7. How the flow is hosted and reached

**Decision**: a `GenericRequestNavigator` in the application implements `IGenericRequestNavigator` over
`INavigationService`, mapping each step to its view model's full name; the work centre step of the new request wizard
navigates into the generic flow when the chosen work centre is designated generic.

**Rationale**: spec 015 left the navigator seam explicitly for this ("a later chunk wires it to `INavigationService`").
The generic pages already carry a parameterless constructor that resolves its view model from
`GenericRequestPageServices`, which is what `Frame.Navigate` needs, so registering the view models and calling
`Configure` once at start-up is the whole host.

**Alternatives considered**: host all steps inside one page and swap a `ContentControl`. Rejected: it duplicates the
shell's navigation, back stack and transitions, and the pages are already `Page`s.

## R8. How the work centre step knows a work centre is generic

**Decision**: add `is_generic` to `sp_setup_work_centers_catalog_get`'s result and carry it through `WorkCenterDetail`
and `WorkCenterSelectionItem`.

**Rationale**: the view behind that procedure already exposes the column; the read simply does not project it. The
designation is read once with the catalog the step already loads, so no second read is added.

**Alternatives considered**: a dedicated `sp_setup_work_centers_generic_get` called per selection. Rejected: a second
round trip per click for a column already in hand.

## R9. How the board draws two card shapes in one list

**Decision**: a `DataTemplateSelector` on the board's list, keyed on a new `SampleOrder.IsGeneric` flag, choosing
between the existing work-order card template and a generic card template.

**Rationale**: WinUI picks a template per item through a `DataTemplateSelector`, and this repository already uses one
for a harder version of the same problem (`WaitlistLineCardDetailTemplateSelector`). Overriding both
`SelectTemplateCore` overloads is the documented requirement and the one this repository has already been bitten by.

**Alternatives considered**: two lists filtered by kind. Rejected: FR-022 and the spec's user story require both kinds
in one list, told apart at a glance.

## R10. What a generic row carries into the board

**Decision**: the board row model gains `IsGeneric`, `Description` and `GenericAnswer`, mapped from the queue columns
the list read now returns, and the generic card is built from the row rather than from a second read.

**Rationale**: the board already reads every row once; a per-row second read is the pattern the card layout work
deliberately avoided, and the fields are already in the row.

**Alternatives considered**: resolve the generic card model from the Item catalog at draw time. Rejected: the card's
identity is the answer the requester gave, which only the stored row holds.
