# Contract: In-App Visual Read Fallback (`MTM_Waitlist.Mock`)

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Consumers**: `MTM_Waitlist` app; `MTM_Waitlist.Setup`, `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.Settings`
**Implements**: FR-002, FR-003, FR-004, FR-005, FR-017, FR-020, FR-022, FR-024, FR-025
**Verified by**: SC-003, SC-004, SC-006, SC-011, SC-012

This is the contract between the routed readers and the Visual fallback. It is *internal* to the product (a C#
interface contract) — there is no network surface here.

---

## 1. Reachability contract

```csharp
public interface IVisualReachabilityDetector
{
    VisualReachabilityState Current { get; }        // Unknown | Live | Cached
    event EventHandler<VisualReachabilityState> StateChanged;
    Task ProbeAsync(CancellationToken cancellationToken);
}
```

| Rule | Requirement |
|---|---|
| Independence | Must not reference the retired `MockRoutingService` / `MockModeChangeDetector` / `MockConfigurationService` stack (FR-014) |
| Hysteresis | `Live → Cached` requires **2 consecutive** probe failures; `Cached → Live` requires **1** success (flapping edge case) |
| Non-manual | No public API sets the state; it is derived only from probe results (FR-003) |
| Cost | Probing is a cheap connectivity check; it never executes a read shape |
| Scheduling | Probing is app-owned. The application **never** performs a scheduled *refresh* (FR-025) |

## 2. Fallback read contract (one seam per read shape)

```csharp
public interface IVisualReadFallback<TRequest, TRow>
{
    Task<IReadOnlyList<TRow>> ReadAsync(TRequest request, CancellationToken cancellationToken);
    Task<CachedReadResult<IReadOnlyList<TRow>>> ReadWithProvenanceAsync(TRequest request, CancellationToken cancellationToken);
}
```

**Algorithm — identical for all five shapes (FR-024):**

1. Attempt the **live** Visual read through the shape's existing executor and script.
2. On **unreachability** (connectivity failure, timeout, login failure) → read the mirror via
   `sp_visual_<shape>_get` and return those rows.
3. On a **successful** live read → return exactly those rows, **including when the result set is empty**. An empty
   live result is a valid answer and **must not** be replaced by cached rows (FR-024).
4. On any other live error (for example a genuine query error against a reachable server) → **surface the error**;
   do not silently substitute cache. Only *unreachability* triggers fallback (FR-002).
5. Never write to Visual (spec non-goal NG2).

**Structural identity (FR-004, SC-004):** the caller receives the same row type, the same column set, and the same
ordering rules whichever source answered. `ReadWithProvenanceAsync` is available *only* to the status surface; it
returns the same rows plus `ServedFromLive | ServedFromCache` and the snapshot's `refreshed_utc`. No other caller
branches on provenance — that is how "callers cannot distinguish the source" is enforced.

**Seed behaviour (FR-017):** with a never-populated mirror, `sp_visual_<shape>_get` returns the baseline seed rows, so
a fresh install returns a usable result rather than an error. The status surface marks this
`IsSeedContentOnly = true` rather than reporting a refresh age.

## 3. The five shapes

Column names below are the **result** column names (PascalCase in C#, per repo naming rules). The mirror's physical
column names are in `data-model.md` §3.

| # | Shape key | Channel / caller to be routed | Inputs | Result rows |
|---|---|---|---|---|
| 1 | `work_order_lookup` | `SetupLookupService.LookupWorkOrderFromBackendAsync` (`MTM_Waitlist.Setup`) | `NormalizedWorkOrder` | `PartNumber`, `Description`, `WorkCenter` |
| 2 | `operation_sequences` | `SetupLookupService.GetSequencesFromBackendAsync` | `NormalizedWorkOrder`, `PartNumber` | `SequenceNumber`, `Description` |
| 3 | `subordinate_parts` | `SetupLookupService.GetSubordinatePartsFromBackendAsync` | `NormalizedWorkOrder`, `PartNumber`, `SequenceNumber` | `Category`, `PartNumber`, `Description`, `Location`, `User8`, `OnHandQuantity` |
| 4 | `inventory_locations` | `WaitlistInventoryService.GetInventoryLocationsFromBackendAsync` (`MTM_Waitlist.Waitlist.View`) | `PartNumber` | `PartNumber`, `Location`, `OnHandQuantity` |
| 5 | `disposition_input` | `RequestDispositionResolver.GetDispositionInputAsync` (`MTM_Waitlist.Settings`) | `WorkOrder`, `PartNumber` | `WorkOrderStatus`, `OpenWorkOrderQuantity`, `FinishedGoodsQuantity`, `HasOutsideVendorOperation` |

**Preserved behaviours:** shape 4 keeps the caller-side on-hand ≥ 1 and ignored-location filtering unchanged; shape 5
keeps `RequestDispositionStatusCodes` (Open = {R,U,F}, Closed = {C}, X never FG) as the only status-code authority;
shape 5's floor/WIP half continues to read the live WIP store (FR-018).

**Not in this contract:** floor/WIP stock, receiving data, request disposition's MySQL floor snapshot, and every
`mtm_waitlist` read. Those are always live and have no cached counterpart (FR-001, FR-018).

## 4. Read status / indicator contract

```csharp
public interface IReadStatusProvider
{
    ReadStatusSnapshot Current { get; }
    event EventHandler<ReadStatusSnapshot> Changed;
}
```

| Field | Requirement |
|---|---|
| `IsCachedDataInUse` | `true` **iff** `VisualReachabilityState == Cached`; drives indicator visibility (FR-005) |
| `CachedDataAgeUtc` | The last successful refresh time across shapes; `null` when `IsSeedContentOnly` (FR-022) |
| `IsSeedContentOnly` | `true` before any successful refresh (FR-017) |
| `PerShapeLastRefreshUtc` | Feeds operator visibility (FR-013) |

**UI rules:** the indicator is non-interactive (`InfoBar` with `IsClosable="False"`, no buttons, no command binding) and
must not offer any state change; it becomes visible on entering `Cached` and disappears on returning to `Live`
(FR-003, FR-005, US4). Data is **never** refused based on age (FR-022).

## 5. Force-refresh client contract

```csharp
public interface IMockServiceRefreshClient
{
    Task<RefreshRequestResult> RequestRefreshAsync(IReadOnlyCollection<string>? shapeKeys, CancellationToken cancellationToken);
}
```

| Rule | Requirement |
|---|---|
| Transport | `POST /api/refresh` on the service API, authenticated with the shared credential (see `mock-service-http-api.md`) |
| Failure | If the service is absent or returns an error, the request fails gracefully and the app continues on cached content — the service is never a hard dependency (FR-025, SC-011) |
| Side effects | Requesting a refresh must not change the app's own read state machine directly; the detector observes reality (prevents duplicate/looped refreshes) |

## 6. Extensibility contract (FR-016, FR-020, SC-012)

Adding a sixth shape **must not** change this contract:

- A new shape adds one `IVisualReadFallback<TRequest,TRow>` implementation, one mirror table (+ stage twin), one
  `sp_visual_<shape>_refresh`, one `sp_visual_<shape>_get`, one shape-catalog entry, and its tests.
- No existing shape's interface, result type, or procedure signature changes.
- Clients already deployed keep serving the existing shapes while the sixth is added — the mirror tables are
  independent, so the change is additive and needs no client downtime.
- The ordered procedure is published as the playbook (FR-028) and is reproduced in `quickstart.md` §7.
