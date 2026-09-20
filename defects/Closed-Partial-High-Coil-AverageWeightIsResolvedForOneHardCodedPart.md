# High — Coil — "Average coil weight" is looked up for one hard-coded part

> **Naming note (2026-09-20).** The **Planned fix** row below names specs by their **template** number, from
> `WeekendProject/SpecTemplates/` -- the seeds written before the specs existed -- so `Spec 01-...` is *not*
> `specs/001-...`. Those templates have since shipped under different numbers: `01-truthful-data-and-controls`
> is now **`specs/002-truthful-data-and-controls`** (shipped), `02-handler-fulfilment-and-urgency` is
> **`specs/003-waitlist-handler-fulfilment`** (shipped), and `03-unified-card-and-taxonomy` is
> **`specs/004-unified-card-item-picker`** (in flight). Templates **04-08** have no spec yet. The single copy
> of this map is `WeekendProject/SpecTemplates/00-INDEX.md`. The **Status** row above already uses the real
> `specs/00N` names -- read that one for what actually happened.


| Field | Value |
| --- | --- |
| **Criticality** | High |
| **Feature area** | Waitlist — coil cards and coil detail (Average coil weight) |
| **Type** | Real data fetched with the wrong key |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`**. **Closure:** the fixed-part lookup (`MMC0001000`) and the `5,000 lb` fallback were removed, and the field is left unset when nothing resolves — never wrong, never defaulted. **Remainder owned by Spec `03-unified-card-and-taxonomy`**, which resolves the coil per request and queries the average for that part. **Proof:** `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestSummaryHonestyTests` (the call is made with the resolved part, and nothing resolvable yields no value) plus the fabricated-literal scan. |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — removes the fixed-part lookup (`MMC0001000`) and the `5,000 lb` fallback so the field is never wrong. **Spec `03-unified-card-and-taxonomy`** re-implements it per request: the coil is resolved from the job (§5.6 of the seed uses the active-job/work-order reads), and the average is then queried for that part. Seeds: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`, `…/03-unified-card-and-taxonomy.md` |
| **Files** | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (line 126), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` (lines 196–226), `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` |
| **Related** | `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` (the coil number shown beside this value is itself fabricated) |

---

## 1. Summary

The "Average coil weight" value is produced by a **real** query against the live receiving store —
but it is always asked for the same part, `MMC0001000`, regardless of which coil or work order the
request is about:

```csharp
// WaitlistViewViewModel.cs:126 — list cards
var resolved = await _averageCoilWeightService.ResolveAverageCoilWeightTextAsync("MMC0001000").ConfigureAwait(false);

// WaitlistViewDetailViewModel.cs:215 + 226 — detail page
var resolved = await _averageCoilWeightService.ResolveAverageCoilWeightTextAsync(CoilReceivingPartId).ConfigureAwait(true);
...
/// <summary>The coil part keyed in the receiving_history seed for the sample coil (MMC0001000).</summary>
private const string CoilReceivingPartId = "MMC0001000";
```

So every coil request in the plant displays the average skid weight of one sample part. If that part
has no receiving history the field is left showing the fabricated `"5,000 lb"`
(line 589 / 637 of `WaitlistViewViewModel.cs`), so the user cannot tell a real answer from a
placeholder.

## 2. Impact

- Operators sizing a lift (the stated purpose of the field: whether a larger forklift is needed) are
  given a number that does not describe the coil in front of them.
- The value is *live-sourced*, which makes it more convincing than a literal — it will change when
  receiving history for `MMC0001000` changes, so it looks like it is tracking reality.
- Because the query is keyed on a constant, the field cannot be correct for any coil except that one,
  and no test would notice a wrong-part regression while the constant remains.

## 3. Evidence

- `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs:126` — literal `"MMC0001000"` in the
  list enrichment path (`EnrichCoilAverageWeightAsync`, called for every row from
  `LoadOrdersAsync`, line 291).
- `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs:226` — `CoilReceivingPartId`
  constant; used at line 215; the enrichment helper is invoked from `OnNavigatedTo` (line 178).
- The query itself is correct and live:
  `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` →
  `IMySqlHelperServer.ExecuteStoredProcedureQueryAsync("sp_receiving_history_average_coil_weight",
  ["@p_part_id"], MySqlDatabaseTarget.MtmReceivingApplication, …)`, rounding the returned
  `AverageWeight` to a whole number and formatting it as `"{n:N0} lb"`. The procedure's artifact is
  `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight/create.sql`.
- A stale comment in the same method still describes the retired demo toggle:
  `WaitlistViewDetailViewModel.cs:198` — "(RecvMockData ON = sample, OFF = real query)". That key was
  deleted by `specs/001`; `RetiredSymbolAuditTests` only matches `Feature.RecvMockData`, so a bare
  `RecvMockData` in a comment passes the audit.
- The coil identity that *should* be used is already available in the app:
  `MTM_Waitlist.Waitlist.NewRequest/Services/CoilAvailabilityService.cs`
  (`GetCoilForJobAsync(workCenter)` → coil number / part number, with a coil part-number prefix rule),
  and the work-order read shapes (`Database/InforVisual/Queues/Module_Waitlist/Queries/GetWorkOrderLookup.sql`
  and its cached counterpart `sp_visual_work_order_lookup_get`).

## 4. Root cause

The enrichment was introduced while the coil lookup was still sample-backed: the only part id known to
receiving history in the development data was the seeded sample coil, so it was written in as a
literal, documented as a "sample coil" constant, and never replaced when the live work-order and coil
reads became available.

## 5. Fix

1. **Resolve the part from the request, not from a constant.** In both call sites, derive the part id
   from the request's own identity, in this order:
   1. the coil/part carried by the active work-center setup job for the request's work center
      (`ICoilAvailabilityService.GetCoilForJobAsync` already returns it, including the
      `CoilPartNumberPrefix` handling);
   2. otherwise the part on the work order from the live-or-cached work-order read shape (live first,
      cached fallback — never a cache read for a live-only store);
   3. otherwise **no value at all** — clear the field rather than leaving `"5,000 lb"`.
2. **Delete the fallback literal.** Replace `"5,000 lb"` in `AddRequestFields` (lines 589, 637) with an
   empty or resource-backed "not available" value so a failed lookup is visible as absence.
3. **Delete `CoilReceivingPartId`** and pass the resolved part id in; keep the formatting/rounding in
   `AverageCoilWeightService` as it is (it is already SP-first and correct).
4. **Fix the stale comment** at `WaitlistViewDetailViewModel.cs:196–199` that describes
   `RecvMockData ON/OFF`; the toggle no longer exists.
5. If resolving the coil for an arbitrary coil request needs a new read, add it as a queue query or
   stored procedure with a matching rollback in the same change (constitution III) and register the
   shape for refresh if it is an Infor Visual read (see the sixth-shape playbook in
   `specs/001-module-mock-visual-fallback/`).

## 6. Verification

1. Unit test on `WaitlistViewViewModel` with a stub `IAverageCoilWeightService` that records its
   argument: build an order for a request whose work order resolves to part *X* and assert the
   service was called with *X* — fails today (it is called with `MMC0001000`) and cannot pass while
   the constant remains.
2. Unit test for the empty path: when the service returns empty and no part resolves, assert the
   field is empty/"not available" and **not** `"5,000 lb"`.
3. Same two assertions for `WaitlistViewDetailViewModel` (it has an existing test class:
   `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailInventoryTests.cs`, and
   `AverageCoilWeightServiceTests.cs` covers the service).
4. Manual: raise two coil requests against two different work orders with different coils and confirm
   the two cards show different average weights, each traceable to that part in receiving history.
5. Suite `Failed: 0`, build `0 Warning(s) 0 Error(s)`; `RetiredSymbolAuditTests` and
   `InlineSqlAuditTests` must stay green.

## 7. Related

- `MTM_Waitlist.Waitlist.NewRequest/Services/CoilAvailabilityService.cs` — existing coil-on-the-job
  resolution.
- `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight/create.sql` —
  the procedure and its documented semantics.
- `FEATURES.md` §7, §14.
