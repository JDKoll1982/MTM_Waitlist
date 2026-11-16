# Data Model: Generic Work Center — Optional Job Fill

Four things are new or reshaped: the seam that asks whether a job is running, the fill it hands back once the
requester accepts, the draft that holds it, and the one column the queue gains so a raised request can say what it
was filled from. Everything else the chunk touches already exists and keeps its shape.

## Job fill offer

The choice presented on the Category step. It is one request's decision, held in
`GenericRequestCategoryViewModel`, and it is never stored (A3, A7).

| Field | Type | Rule |
|---|---|---|
| `State` | `GenericRequestJobFillOfferState` | Which of the four states below the offer is in. Starts at `NotChecked` and never returns to `Offered` (FR-003). |
| `WorkOrder` | text | The running job's work order, taken from the probe. Empty unless the state is `Offered` or `Answered` with a job. This is what the copy names in `Fill this request from job {0} at {1}?` (FR-001, R5). |
| `WorkCenter` | text | The work center the offer names, which is the draft's own work center, never a second reading of it. |
| `AttemptedAtUtc` | `DateTimeOffset?` | When the last job read was attempted. Set only by the accepted read, and drawn with the retry when the state is `Unavailable` (FR-016). |

The probe answers a name, never the values: `WorkOrder` and `WorkCenter` are populated, and no part, sequence, die or
dunnage is read until the requester accepts (FR-002, R5).

## Request provenance

The job a request's values were taken from, carried end to end. It is **not** identity (FR-011, R3).

| Field | Where | Rule |
|---|---|---|
| `ProvenanceWorkOrder` | `WaitlistRequestDraft` (`string?`) | The accepted fill's work order, or null for a request that was not filled. Set on the raise from `Draft.JobFill.WorkOrder`; null and empty are the same fact and neither is written. |
| `ProvenanceWorkOrder` | `WaitlistRequest` (`string?`) | What the row holds, read back on every read of the request. Null for a generic request raised without a fill and for every work-order request. |
| `provenance_work_order` | `waitlist_requests_queue`, `VARCHAR(64) NULL` | `NULL` when the request was not filled. Width matches `active_setup_job_id`, which it deliberately does not replace. |
| `p_provenance_work_order` | `sp_waitlist_request_insert` | Appended after `p_accepted_warnings_json`, written through the same `NULLIF(TRIM(...), '')` optional-value idiom as its neighbours, so a caller that passes nothing writes `NULL` (constitution III). |

`active_setup_job_id` stays null on a filled generic request and `is_generic` stays true. Writing the job into
`active_setup_job_id` would make a filled request answer "work-order request" to every consumer that reads the column
without also reading the flag, which FR-011 and FR-012 forbid (R3).

## Stored row and its reads

The queue table gains exactly one column, and three procedures gain exactly one parameter or one projected column.

| Artifact | Change | Rule |
|---|---|---|
| `Tables/18_waitlist_requests_queue/create.sql` | `provenance_work_order VARCHAR(64) NULL` with a comment naming it as provenance, not identity | Nullable with no default, so a row written before this change reads as "not filled" rather than a special case. `rollback.sql` drops it. |
| `sp_waitlist_request_insert` | appended `p_provenance_work_order` | Blank trims to `NULL`. The column is never inferred from anything else the procedure receives. |
| `sp_waitlist_request_get` | projects `provenance_work_order` | Read back so the detail page can draw the provenance line (FR-013). |
| `sp_waitlist_request_list` | projects `provenance_work_order` | The card draws nothing new from it (FR-012, R9); it is projected so both reads answer the same row shape. |

`AllTables.sql`, `AllSPs.sql` and `Bootstrap/update_table_descriptions.sql` move in the same change, as principle III
requires.

## Job fill seam

`IGenericRequestJobFillData`, declared in `MTM_Waitlist.GenericRequest.Contracts` and implemented at the composition
root over the Setup module's existing active-job read. Two operations, deliberately separate so no value is read before
the choice is made (FR-002, R4, R5).

| Operation | Answers | Rule |
|---|---|---|
| `ProbeAsync(workCenter)` | the running job's work order and work center, or nothing | Names the job only. A work center with no job, or a failed probe, answers nothing and the offer is never drawn (FR-004). |
| `ReadAsync(workCenter)` | one `GenericRequestJobFill`, or an unavailable answer carrying the attempt time | Called only after acceptance. A failure answers unavailable and seeds nothing (FR-016, R6). |

The seam is the only member of this feature that knows the Setup module exists; the fill, the draft and the queue see
plain strings. A work center the signed-in person may not see the job data for is not distinguished, because the
application declares no permission that governs it — the adapter carries a `simplified:` note at the point a gate would
go (FR-015, R7).

## Accepted fill

`GenericRequestJobFill`, in `MTM_Waitlist.GenericRequest.Models`. It is the only place that knows an answer came from
the job, and it answers one question per answer: whether that answer is still the job's (R8).

| Field | Type | Rule |
|---|---|---|
| `WorkOrder` | text | The job the values were taken from. Never empty — a fill without a work order is not a fill and is not kept. |
| `WorkCenter` | text | The work center the job was read at. Must match the draft's work center; a fill for another work center is discarded rather than applied. |
| `PartNumber` | text | The job's part. Seeds the part step; empty when the job states none, in which case the requester is asked (FR-008). |
| `Sequence` | text | The job's sequence number. Seeds the part step beside the part. |
| `DieNumber` | text | The job's first **real** die (the placeholder `No Die` row is never a candidate), or empty. |
| `DieLocation` | text | That die's home location, or empty. Empty is drawn with the die step's existing wording, never as a blank cell. |
| `DunnageType` | text | The job's dunnage type name, or empty. |
| `DunnagePart` | text | The job's dunnage part number, or empty. |
| `IsPartFromJob` | flag | True while the part number is the job's. Cleared by `ClearPart()`. |
| `IsSequenceFromJob` | flag | True while the sequence is the job's. Cleared with the part, because a sequence without its part means nothing. |
| `IsDieFromJob` | flag | True while the chosen die is the job's. Cleared by `ClearDie()`. |
| `IsDunnageFromJob` | flag | True while the dunnage type and part are the job's. Cleared by `ClearDunnage()`. |

```text
IsPartFromJob / IsSequenceFromJob / IsDieFromJob / IsDunnageFromJob
  set only by the accepted fill, cleared individually by ClearPart / ClearDie / ClearDunnage
  clear all by ClearAll, which runs when the item changes (FR-009)
```

Each flag is a fact about the answer, not a guess about it: a value the requester types that happens to equal the
job's is not "from the job", which is why the fill records the provenance rather than comparing on render (R8, FR-007).

## Draft reshape

`GenericRequestDraft` gains one member and keeps everything else it has.

| Field | Type | Rule |
|---|---|---|
| `JobFill` | `GenericRequestJobFill?` | The accepted fill, or null when there is none, the offer was declined, or the item changed. Null is the ordinary state and is never an error (FR-003). |

The draft keeps its identity untouched by the fill: `WorkCenter`, `Building`, `ItemCode`, `ItemName` and `Answer` are
never written by a fill, and `RequiresPart`, `RequiresDunnage`, `RequiresScrap` and `RequiresDie` still come from the
chosen item, so a job cannot add a step the item does not ask for (R2).

## Seeding rules

| Step | Seeded from | Rule |
|---|---|---|
| Part | `PartNumber`, `Sequence` | The candidate is offered as the chosen part and sequence; the step's own quantity read still runs, so a seeded sequence is proved exactly as a typed one is (FR-010). |
| Die | `DieNumber`, `DieLocation` | The candidate is marked as one of the chosen dies. `SelectedDies` keeps its existing shape and list semantics; a seeded die is one entry, and clearing it leaves the list as the requester left it (FR-005, FR-007). |
| Dunnage | `DunnageType`, `DunnagePart` | Both are seeded as the step's current selection and remain changeable. |
| Category, Item | nothing | A fill never chooses the category or the item; those remain the request's identity and the requester's choice (FR-011, R2). |
| Scrap | nothing | The job carries no scrap answer, so the scrap step asks as it does with no job (FR-008). |

A seeded answer is drawn with the qualifier `From job {0}` on the step that shows it, and stops drawing it the moment
the requester changes that answer (FR-006, A6). A job value the item does not ask for is never shown (edge case 2).

## Clear rules

| Trigger | Cleared | Kept |
|---|---|---|
| Requester changes the part | `IsPartFromJob`, `IsSequenceFromJob`, the quantity reading (existing rule) | The fill itself, so the other answers still read as the job's |
| Requester changes the sequence | `IsSequenceFromJob` | `IsPartFromJob` |
| Requester changes the die | `IsDieFromJob` | Every other flag |
| Requester changes the dunnage | `IsDunnageFromJob` | Every other flag |
| Chosen item changes | `JobFill` and every flag with it, plus the existing part/dunnage/scrap/die clears | Identity fields already chosen |

Clearing the item drops the fill outright, provenance included: once the answers that belonged to the previous item are
gone, no value in the request came from the job, so the raise records no provenance rather than naming a job the
request no longer draws anything from (FR-009, edge case 5). The offer is not made again for that request, because the
choice was already given (FR-003).

## State transitions

The offer belongs to one request, so it moves forward only (A3).

```text
NotChecked ──step loads, job running──► Offered ──accept, read ok────► Answered(filled)
                                            │
                                            ├──decline──────────────► Answered(declined)
                                            └──accept, read fails───► Unavailable ──retry──► Answered(filled)
                                                                                        └── read fails again ──► Unavailable
```

| State | Meaning | Screen | Draft effect |
|---|---|---|---|
| `NotChecked` | The step has not loaded, so nothing is known about a job | the step before it loads | none |
| `Offered` | A job is running and the offer is drawn | `category-offer` | none — no value has been read |
| `Answered` | Accepted with values seeded, or declined with nothing seeded | `category-filled` or `category-plain` | `JobFill` set, or null |
| `Unavailable` | The accepted read failed | `category-offer-unavailable` — `The running job could not be read`, the attempt time, a retry | none; nothing is seeded, so no partly filled request can be presented as complete (FR-016, R6) |

Declining is a terminal `Answered(declined)` for that request: the offer is not drawn again within it, the refusal is
not recorded anywhere, and the request that follows is indistinguishable from one raised with no job (FR-003, SC-002,
A7). A retry on `Unavailable` re-reads the job for the choice already made, so it moves to `Answered` when the read
succeeds and stays `Unavailable` with a fresh attempt time when it fails again — the requester is never asked the
question a second time (FR-003).

A job that closes or changes between the offer and the submission does not move the offer out of `Answered`: the fill
is a snapshot of values already shown, not a live reference, so the request still raises and nothing is treated as an
error (FR-014, edge case 1).

## Provenance display

One rendered row, drawn only where the request carries a job.

| Field | Rule |
|---|---|
| `ProvenanceText` | `Filled from job {0}` on the request detail page, drawn only when `ProvenanceWorkOrder` is present. Distinct from the page's existing Work order row, which is identity and is drawn only for a work-order request (FR-013). |
| Card | Nothing new. The generic card draws no job cell, and a filled request draws the generic card (FR-012, SC-003). |
| Export | No export exists in this application; the stored column is what a later export would carry (R9). |

## Relationships

```text
work center ──1:0..1── running job (existing Setup read, via the new adapter)
running job ──0..1── accepted fill ──1── GenericRequestDraft.JobFill ──0..1── provenance on the raised request
raised request ──0..1── provenance_work_order ──0..1── provenance row on the detail page
```

One accepted fill belongs to one request. A die request that raises one request per chosen die records the same
provenance on every one of those requests, because every one of them was filled from the same job (R10).
