# Research — Optional Job Fill

Phase 0 findings behind the plan. Each entry is a decision, why it was taken, and what it rules out.

## R1 — The offer is asked on the category step, not on a new step

**Decision**: the offer is a panel on the Category step. It is probed and drawn when that step loads, and answered
there.

**Rationale**: the work center step is not always seen. `GenericRequestNavigator.Launch` (the work-center tile)
writes the work center and building and goes straight to Category; the WorkCenter page is only reached through
`StartAnother`. So "when the work center step completes" means "when the first step after it opens", and Category is
that step on both entry paths. A panel is testable through the view model, where a ContentDialog would need a dialog
seam that does not exist in this module.

**Alternatives considered**: a new wizard step (rejected — it changes the flow's shape and every chunk's step
order); a ContentDialog raised from the work center view model (rejected — no prompt seam in the generic module, and
it would still miss the `Launch` path).

## R2 — The fill supplies candidates, never the identity

**Decision**: the fill carries the running job's part number and sequence, its real die, and its dunnage part. It
does not choose the category or the item.

**Rationale**: the job carries no generic Item code, and the generic flow's identity is the stated item. Inventing a
job-to-item mapping would make the job decide what the request is, which FR-011 forbids. The requester picks the
category and item as they do today; the answers those items ask for arrive already chosen.

**Alternatives considered**: mapping the job's part to a generic item (rejected — it is identity, and the mapping
does not exist); prefilling only the part step (rejected — the die and dunnage steps ask the same kind of question
and the job answers them).

## R3 — Provenance is a new column, not the existing job id

**Decision**: `waitlist_requests_queue` gains a nullable `provenance_work_order`. `active_setup_job_id` stays null on
a filled generic request.

**Rationale**: `active_setup_job_id` means *identity*. Both request reads return it unconditionally, and the detail
page decides whether to draw a Work order row from `is_generic`. Writing the job there would make a filled request
look like a work-order request to every consumer that reads the column without also reading the flag. A separate
nullable column says what the job was without saying what the request is, and it defaults honestly for existing rows.

**Alternatives considered**: reusing `active_setup_job_id` (rejected — collapses provenance into identity); storing
the job inside `answers_json` (rejected — the answers map is the item's declared labels, and a job is not one).

## R4 — The job read is an app-root adapter over the existing Setup read

**Decision**: `MTM_Waitlist.GenericRequest` declares `IGenericRequestJobFillData`; the application project implements
it over `IActiveJobItemResolverService.ResolveAsync`, and registers it beside the other generic-request services.

**Rationale**: no library in this repository references `MTM_Waitlist.Setup`, and the existing
`RequestJobAvailabilityProvider` already shows the sanctioned shape: the interface lives with the consumer, the
mapping lives at the composition root because that is the only place that can see both sides.

**Alternatives considered**: referencing `MTM_Waitlist.Setup` from `MTM_Waitlist.GenericRequest.Data` (rejected —
breaks the layering the rest of the solution keeps); copying `sp_setup_active_jobs_latest_by_work_center_get` into the
generic data project (rejected — a second read of the same procedure, drifting from the first).

## R5 — The probe answers the job's name, not its values

**Decision**: the probe returns the running job's work order (enough to name it in the offer). The part, sequence,
die and dunnage are read only after the requester accepts.

**Rationale**: FR-002 forbids reading the job before the choice is made, while FR-001 requires offering it, which
cannot be done without knowing a job is there.

**Alternatives considered**: one read that returns everything and is simply not shown until accepted (rejected — the
values would be in memory before the choice, which is what FR-002 rules out).

## R6 — A failed value read is reported in the panel and seeds nothing

**Decision**: when the accepted read fails, the panel shows the unavailable presentation with the attempt time and a
retry, and no draft value is written.

**Rationale**: FR-016 asks for an unavailable report with the attempt time and a retry, and forbids a partly filled
request presented as complete. Seeding nothing keeps the request generic and complete-on-its-own.

**Alternatives considered**: failing the whole step (rejected — the category list is unaffected, and the requester can
still raise a generic request); silently continuing (rejected — it hides the failure the spec asks to report).

## R7 — Permission: no job-visibility permission exists

**Decision**: the offer is not gated on a permission. The adapter carries a `simplified:` note naming the point where
a gate would go, and the concern is recorded.

**Rationale**: the application declares twenty-one permissions and none of them governs seeing a job's data. Every
signed-in person who can open the request flow can already see the job data the fill would show. Minting a
twenty-second key would invent a product policy (who holds it) that nobody has set.

**Alternatives considered**: `permission.setup.work_centers` (rejected — it gates work-center setup, and requesters do
not hold it, so the offer would never be made to the people the chunk is for); a new `permission.requests.job_fill`
key (rejected — it invents the role baselines that decide who may raise a filled request).

## R8 — "From job" is a flag per answer on the draft

**Decision**: the draft holds the job fill and one flag per answer it seeded. Each flag clears when the requester
changes that answer, and all of them clear with the answers they describe.

**Rationale**: the draft already owns what each step records, and the existing clears (work center, category, item)
already name every downstream answer. Adding the job state beside them keeps one clearing rule rather than two.

**Alternatives considered**: a second "changed" set (rejected — two sources of truth for the same fact); marking at
render time by comparing against the job (rejected — a value that happens to equal the job's is not the same as one
taken from it).

## R9 — The detail page is the only surface that renders provenance

**Decision**: the provenance line is drawn on the request detail page. The card draws nothing new.

**Rationale**: FR-012 forbids a job cell on the card, and no request export exists in this application, so there is no
second surface to carry it. The stored column means any later export can carry it.

**Alternatives considered**: adding a card cell (rejected — FR-012); adding an export (rejected — inventing a surface
to satisfy "and in any export").

## R10 — A die request writes provenance on every row it raises

**Decision**: the job is recorded on each request a filled die request raises.

**Rationale**: one die request raises one request per chosen die, and each of those requests was filled from the same
job. A provenance that landed on only the first would be wrong on the others.

**Alternatives considered**: recording it once against the batch (rejected — there is no batch row).
