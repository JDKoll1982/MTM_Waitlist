# Data Contract: the job fill seam and request provenance

What feature 021 reads, seeds and stores. Names below are the interface; renaming one is a change to the contract.

The pinned copy is listed once, in `job-fill-offer.contract.md`. This file repeats only the one string it needs
(`The running job could not be read`) and otherwise refers to that file.

## 1. The seam

`MTM_Waitlist.GenericRequest.Contracts.IGenericRequestJobFillData` — declared in the consuming module, implemented at
the composition root, for the same reason `IRequestJobPartAvailabilityProvider` is: no library in this solution
references `MTM_Waitlist.Setup`.

```csharp
Task<GenericRequestStepResult<GenericRequestJobFillCandidate>> ProbeAsync(
    string workCenter,
    CancellationToken cancellationToken = default);

Task<GenericRequestStepResult<GenericRequestJobFill>> ReadAsync(
    string workCenter,
    CancellationToken cancellationToken = default);
```

**`ProbeAsync` names the job and reads no values** (FR-002, research R5). It is called when the Category step loads.

| Presentation | Meaning for the offer |
|---|---|
| `Populated` | a job is running; `Items` holds exactly one `GenericRequestJobFillCandidate` |
| `Empty` | no job is running; the offer is never drawn (FR-004) |
| `Unavailable` | the probe could not answer; the offer is not drawn, and the step is what it is today |
| `Loading` | never returned by this method |

**`ReadAsync` reads the values and is called only after the requester accepts** (FR-002). `AttemptedAtUtc` is the
time of this attempt and is what the unavailable panel shows (FR-016).

| Presentation | Meaning for the offer |
|---|---|
| `Populated` | `Items` holds exactly one `GenericRequestJobFill`; it is seeded onto the draft |
| `Empty` | the job went away between the probe and the read; nothing is seeded, and the step reads `category-plain` |
| `Unavailable` | the read failed; nothing is seeded, the panel reports `The running job could not be read` with the attempt time and a retry |
| `Loading` | never returned by this method |

`GenericRequestJobFillCandidate` and `GenericRequestJobFill` are declared in
`MTM_Waitlist.GenericRequest/Models/GenericRequestJobFill.cs`, together with `GenericRequestJobFillOfferState`.

| Type | Member | Meaning |
|---|---|---|
| `GenericRequestJobFillCandidate` | `string WorkOrder` | the running job's work order, which is what the offer names |
| `GenericRequestJobFillOfferState` | `NotChecked`, `Offered`, `Answered`, `Unavailable` | the four states in `job-fill-offer.contract.md` |

## 2. The seeded candidates and their flags

`GenericRequestJobFill` is the only place that knows an answer came from the job, and it answers one question per
answer: whether that answer is still the job's (research R8).

| Member | Meaning |
|---|---|
| `string WorkOrder` | the job the values were taken from; never empty on a populated read |
| `string PartNumber` | the job's part, or empty when the job carries none |
| `string Sequence` | the job's sequence, or empty |
| `string DieNumber` | the job's real die, or empty |
| `string DunnageType` | the job's dunnage type, or empty |
| `string DunnagePart` | the job's dunnage part, or empty |
| `bool IsPartFromJob` | `PartNumber` is still the job's |
| `bool IsSequenceFromJob` | `Sequence` is still the job's |
| `bool IsDieFromJob` | `DieNumber` is still the job's |
| `bool IsDunnageFromJob` | `DunnageType` / `DunnagePart` are still the job's — one flag, because the dunnage answer is a pair |
| `bool HasAnySeededValue` | at least one of the five values above is non-empty |
| `void ClearPart()` | the requester changed the part: `IsPartFromJob = false` |
| `void ClearSequence()` | the requester changed the sequence: `IsSequenceFromJob = false` |
| `void ClearDie()` | the requester changed the die: `IsDieFromJob = false` |
| `void ClearDunnage()` | the requester changed the dunnage: `IsDunnageFromJob = false` |
| `void ClearAll()` | every flag cleared, as one clearing rule |

Every `Clear…` method only clears its flag. It never edits `GenericRequestDraft`; the step that changed the answer
writes the new value, exactly as it does today.

## 3. What the fill seeds on the draft

`MTM_Waitlist.GenericRequest.Models.GenericRequestDraft` gains one member:

```csharp
public GenericRequestJobFill? JobFill { get; set; }
```

Accepting seeds, and only seeds, these draft members from `JobFill`:

`PartNumber`, `Sequence`, `DieNumber`, `DunnageType`, `DunnagePart`.

The fill never writes: `WorkCenter`, `Building`, `Category`, `ItemCode`, `ItemName`, `Answer`, `Quantity`,
`EstimatedMinutes`, `ScrapType`, `Description`, `AcceptedWarnings`, `RequiresPart`, `RequiresDunnage`,
`RequiresScrap`, `RequiresDie`. The job carries no generic Item code, so the requester picks the category and the
item as they do today (research R2, FR-011).

**Clearing.** `JobFill` is cleared together with the answers it describes:

- a different work center clears it with everything below the work center;
- a different category clears it with everything below the category;
- a different item clears it with everything below the item, and `JobFill.ClearAll()` runs for any answer the item
  change leaves behind (FR-009);
- a decline leaves it `null` and writes nothing.

**Proof is not bypassed.** A seeded part or sequence is a candidate: the part step still proves it against Infor
Visual and the quantity/operation-state reads still run. A seeded die is the same candidate the die search would
return. The fill supplies the candidate, never a bypass (FR-010, SC-004).

**A job that closes after the offer does not invalidate the request.** The values already shown stay on the draft and
the request still raises; a later read failing is not reported as an error (FR-014).

## 4. The composition root adapter

`MTM_Waitlist.Services.GenericRequestJobFillSource` implements `IGenericRequestJobFillData` over
`IActiveJobItemResolverService.ResolveAsync(string workCenter, CancellationToken)` (`MTM_Waitlist.Module_Setup`),
which is the same read the work-order flow already performs (A2).

- `ProbeAsync` maps a non-null `SetupActiveJobSnapshot` to `Populated` carrying `WorkOrder`; a null snapshot to
  `Empty`; a thrown read to `Unavailable` with the attempt time.
- `ReadAsync` maps the snapshot onto `GenericRequestJobFill` from these members and no others:

| `GenericRequestJobFill` | `SetupActiveJobSnapshot` source |
|---|---|
| `WorkOrder` | `WorkOrder` |
| `PartNumber` | `PartNumber` |
| `Sequence` | `SequenceNumber` |
| `DieNumber` | `PrimaryDie?.PartNumber`, read through `RealDies` so a job whose only die row is the `No Die` placeholder seeds no die |
| `DunnagePart` | the first `DunnageParts` entry's `PartNumber` |
| `DunnageType` | that same entry's `TypeId`, resolved to the dunnage type the dunnage step offers |
- The adapter carries a `simplified:` note at the point a permission gate would go. No permission gates the offer:
  none of the application's permissions governs seeing a job's data, and every signed-in person who can open the
  request flow can already see what the fill would show (FR-015, research R7).

Registration goes in `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, beside the existing
`IGenericRequestStepData` registration:

```csharp
services.AddSingleton<MTM_Waitlist.GenericRequest.Contracts.IGenericRequestJobFillData, GenericRequestJobFillSource>();
```

## 5. Provenance

### Column

`waitlist_requests_queue` gains one nullable column. It is **not** `active_setup_job_id`:

| Column | Type | Meaning |
|---|---|---|
| `provenance_work_order` | `VARCHAR(64) NULL` | the job a generic request's values were taken from; NULL for every request not filled from a job |

`active_setup_job_id` stays NULL and `is_generic` stays `1` on a filled generic request (FR-011, research R3).

Schema artifacts, each with its rollback, in one change:

- `Database/Tables/18_waitlist_requests_queue/create.sql` and `rollback.sql`
- `Database/Tables/AllTables.sql`
- `Database/Bootstrap/update_table_descriptions.sql`

### Stored procedures

| Procedure | Change |
|---|---|
| `sp_waitlist_request_insert` | gains `IN p_provenance_work_order VARCHAR(64)`, appended after `p_accepted_warnings_json`, and writes it through the existing optional-value idiom: `NULLIF(TRIM(COALESCE(p_provenance_work_order, '')), '')` |
| `sp_waitlist_request_get` | returns `provenance_work_order` |
| `sp_waitlist_request_list` | returns `q.provenance_work_order` |

Each ships `create.sql` and `rollback.sql`, and `Database/StoredProcedures/AllSPs.sql` moves in the same change. No
inline statement text is added anywhere (constitution III).

### Application path

```text
GenericRequestLiveStepData.SubmitAsync
  -> WaitlistRequestDraft.ProvenanceWorkOrder <- draft.JobFill?.WorkOrder
  -> IWaitlistRequestService.SubmitAsync
     -> sp_waitlist_request_insert(..., p_provenance_work_order)
  -> sp_waitlist_request_get / sp_waitlist_request_list
     -> WaitlistRequest.ProvenanceWorkOrder <- ReadString(row, "provenance_work_order")
```

| Member | Where |
|---|---|
| `WaitlistRequestDraft.ProvenanceWorkOrder` | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestDraft.cs` |
| `WaitlistRequest.ProvenanceWorkOrder` | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequest.cs` |
| parameter map entry `["p_provenance_work_order"]` | `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` |

**A die request writes provenance on every row it raises.** One die request raises one request per chosen die, and
each of those requests was filled from the same job, so the job lands on all of them (research R10).

### The detail page

`WaitlistViewDetailViewModel` adds one row carrying `Waitlist_Detail_JobFill_Provenance.Text` formatted with the work
order. The row is drawn only when `ProvenanceWorkOrder` is non-empty, and it is drawn as provenance — never as an
identity field, never labelled as a work order, and never on the card (FR-012, FR-013, A4).

No export surface is added (research R9). The stored column is the contract any later export reads.

## 6. What a test may rely on

- The probe reads no values: `ProbeAsync` answers `Populated` with a work order and nothing else.
- The offer is only drawn for `Populated`, so a work center with no job never sees it (FR-004).
- Accepting seeds five draft values and no identity value (FR-005, FR-011).
- Changing one seeded value clears that one flag and leaves the others (FR-007).
- Changing the item clears `JobFill` with the answers that belonged to the previous item (FR-009).
- A failed read seeds nothing, reports `Unavailable` with the attempt time, and the request still raises (FR-016).
- A raised filled request stores `provenance_work_order`, keeps `is_generic = 1`, and keeps `active_setup_job_id`
  NULL (FR-011, FR-013, SC-003).
- The job reads back on both the single-request and the list read.
