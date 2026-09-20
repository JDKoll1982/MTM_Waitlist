# Critical — Waitlist — Cards and detail pages show fabricated material data

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
| **Criticality** | Critical |
| **Feature area** | Waitlist — request cards and request detail |
| **Type** | Fabricated data presented as real (spec violation) |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`** (the seed was `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`). **Closure:** every literal in §3 was removed from the card and the detail page, and a value with no source is not rendered — no substitute, no placeholder. **Remainder owned by Spec `03-unified-card-and-taxonomy`**, which restores the real attributes. **Proof:** `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests`, `…/WaitlistViewDetailFieldTests`, `…/WaitlistViewDetailBlockStateTests`, `…/Controls/WaitlistLineCardMarkupTests`, plus the fabricated-literal scan (`MTM_Waitlist.Tests/Module_Mock/FabricatedValueGuard.cs`). |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — removes every fabricated value listed in §3 so nothing false is rendered, and adds the test that fails against today's code. The *real* values are restored by **Spec `03-unified-card-and-taxonomy`**, whose item-resolution gate already forbids the `FG-10042` / `WO-073112 / RM-48190` literals. Seeds: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`, `…/03-unified-card-and-taxonomy.md` |
| **Files** | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (lines 560–701), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` (lines 316–520) |
| **Blocks** | `specs/001-module-mock-visual-fallback` FR-001 / FR-003 / FR-014, SC-013 — and the whole point of that release |

---

## 1. Summary

Every waitlist card and every request detail page is populated with **hard-coded text** for the
material values an operator reads: the coil number, part numbers, on-hand quantity, customer,
packlist, traceability ID, WIP destination, outside-service vendor, die number, and quantity.
These literals live in `WaitlistViewViewModel.AddRequestFields(...)` and are rendered unchanged for
every real production request — the detail page then reads them back by label
(`WaitlistViewDetailViewModel.FieldValue(item, "Customer")`, etc.).

The code comments admit it:

```csharp
// ...otherwise always show the actual coil on the job
// (sample/mock value until the live Infor Visual coil lookup is wired)
var coilNumber = wrongCoil ? "Wrong coil" : "COIL-204";
```

A user raising a Coil request sees `COIL-204 / 46,000 lb / 0.060 x 48 in galvanized coil`. A user
raising a Finished-Goods pickup sees part `FG-10042`, customer `Northstar Manufacturing`, packlist
`PL-80421`. A Coil-Delivery request for scrap sees "Not provided". None of it is read from the
request, the job, or Infor Visual.

**This is the exact failure the Module_Mock release was written to eliminate** (no substituted
sample data, live data only), and it is materially worse than the old demo mode because the old
mode was opt-in behind a visible setting, while this is unconditional and indistinguishable from
real data on screen.

## 2. Impact

- An operator or material handler can act on a **wrong coil, part, quantity, customer or vendor**.
- The values are consistent across all users and all requests, so they look plausible and are not
  self-evidently stale — nothing on screen says they are placeholders.
- The defect is on the primary screen of the product, in the fields the work is actually performed
  against, and on the detail page under headings whose own text promises the opposite:
  "Material and inventory information needed to select and stage the requested coil."
- It also silently defeats `defects/Closed-Partial-High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md`: the
  "Requested coil" the average is shown beside is itself fabricated.

## 3. Evidence — the exact literals

`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, method
`private static void AddRequestFields(SampleOrder item, WaitlistRequest request)` starting at
**line 560**, called from `CreateSessionOrder(...)` (line 384) for every real request.

| Line | Field shown to the user | Hard-coded value |
| --- | --- | --- |
| 572 | Scrap lugger | `"Not selected"` (falls back on subtype text) |
| 573 | Scrap reason | request detail text (real) |
| 585 | Coil → Requested coil | `"COIL-204"` (or `"Wrong coil"` for the wrong-coil subtype) |
| 586 | Coil → Quantity in house | `"46,000 lb"` |
| 588 | Coil → Coil description | `"0.060 x 48 in galvanized coil"` |
| 589 | Coil → Average coil weight | `"5,000 lb"` (later overwritten by a real, but wrong-part, lookup) |
| 599 | Pickup FG → Part number | `"FG-10042"` |
| 600 | Pickup FG → Part description | `"Finished bracket assembly"` |
| 601 | Pickup FG → Quantity remaining | `"24 each"` |
| 602 | Pickup FG → Customer | `"Northstar Manufacturing"` |
| 603 | Pickup FG → Packlist | `"PL-80421"` |
| 614 | Pickup NCM → Part | `"RM-50218 / Customer RM-77"` |
| 615 | Pickup NCM → Traceability ID | `"NCM-260803-014"` |
| 616 | Pickup NCM → Quantity to move | `"2 containers"` |
| 617 | Pickup NCM → Destination | `"NCM Area"` |
| 623 | Pickup WIP → Work order | `"WO-072368"` |
| 624 | Pickup WIP → Part and quantity | `"WIP-218 / 12 pieces"` |
| 626 | Pickup WIP → WIP destination | `"WIP Area / Rack B-14"` |
| 627 | Pickup WIP → Operation sequence | `"30"` |
| 634–637 | Pickup Coil → coil/quantity/description/weight | `"COIL-204"`, `"46,000 lb"`, `"0.060 x 48 in galvanized coil"`, `"5,000 lb"` |
| 647 | Pickup O/S → Part or work order | `"WO-073112 / RM-48190"` |
| 648 | Pickup O/S → Quantity to move | `"6 pieces"` |
| 649 | Pickup O/S → Outside-service destination | `"Heat Treat Section"` |
| 650 | Pickup O/S → Vendor or service | `"Midwest Heat Treat"` |
| 662 | Pickup/Other → Request description | request detail text (real) |
| 672–675 | Flatstock → Part / Quantity / Destination | `"RM-8201"`, `"12 sheets"`, `"Flatstock staging"` |
| 681–682 | Table handling / Die handling → Part or Die | `"Part A-12"` / `"Die 4402"` |
| 683 | Table/Die → Quantity | `"1"` |
| 684–685 | Table/Die → Destination | `"Table staging"` / `"Die shop"` |
| 570 | Scrap → Part number | `"Not provided"` (shows as a value, not as absence) |

Rendered on:

- **List cards** — the type-specific templates bind `Fields[0]`..`Fields[4]`
  (`Module_Waitlist/Controls/Coil/CoilWaitlistLineView.xaml` lines 27–42, and the parallel
  `PickupFg/PickupNcm/PickupOs/PickupWip/Scrap` views), inside `WaitlistLineCardView`.
- **Detail page** — `WaitlistViewDetailViewModel.LoadCoilSections` / `LoadFinishedGoodsSections` /
  `LoadNcmSections` / `LoadOutsideServiceSections` / `LoadWipSections` / `LoadScrapSections`
  (lines 316–520) read the same fields by label through
  `FieldValue(SampleOrder item, string label, string fallback = "Not available")`
  (line ~505), and additionally inject static literals of their own
  ("Tipping strategy" = `"Crane below 10 in; otherwise Tipper"` at line 331,
  "Allowed categories" at line ~470, `"Pending"`, `"Pending assignment"`, `"Pending Quality review"`,
  `"Pending pickup"`, and `"Not available"` placeholders).

## 4. Root cause

`AddRequestFields` was written as a card-preview fixture while the real material lookups did not
exist yet, and it was never replaced when the request pipeline went live. The only field that was
later wired to a real service is "Average coil weight" (see the companion defect); every other
material value is still the placeholder. The comment at line 583–585 records the debt explicitly.

## 5. Why the existing gates did not catch it

`MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` (SC-013) matches **symbol names**
(`ISampleDataService`, `SampleDataService`, `Sample*Catalog`, `Feature.InforVisualMockData`,
`MockToggleService`, `MockRouting*`, `MockMode*`, `UseMockData|MockDataExpander`, `sp_mock_`) across
`.cs/.xaml/.resw/.sql/.csproj/.json`. Rendered literals such as `"Northstar Manufacturing"` are not
in that pattern set, so the audit passes while the app shows sample values. The
`WaitlistRequestService` tests assert persistence and transitions against the request model and
never assert what the card renders.

## 6. Fix

**Decision needed first (product):** for each request type, which real source supplies each field?

Recommended shape of the fix:

1. Replace each literal in `AddRequestFields` with either
   (a) the real value from the request / Infor Visual, or
   (b) an explicit absence — the empty string or a resource-backed "Not available" — **never** a
   plausible number.
2. Source the real values through existing SP-first seams:
   - work-order lookup, operation sequences, subordinate parts, inventory locations, disposition
     input → `Database/InforVisual/Queues/Module_Waitlist/Queries/*.sql` (live) or their cached
     counterparts (`mtm_mock`, `sp_visual_*_get`);
   - receiving history / dunnage → `Database/MTMReceivingApp/StoredProcedures/**`;
   - WIP floor quantities → `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs`
     (`GetWipFloorQuantities.sql`) — already implemented and unused by the card.
   Add any missing procedure as `create.sql` + `rollback.sql` in the same change (constitution III).
3. Key the lookups on the request's own identity (`WaitlistRequest.RequestId`, work order, part
   number, coil number) — never on a constant.
4. Move the literals in `WaitlistViewDetailViewModel` (`"Tipping strategy"`, `"Allowed categories"`,
   the `"Pending*"` statuses, `"Not available"`) either to real data or to localized
   "not yet available" strings, and delete the invented content that has no owner (for example
   "Crane below 10 in; otherwise Tipper").
5. If any of these fields genuinely cannot be sourced yet, **omit the row** rather than print a
   placeholder value — a missing row is truthful, a fake value is not.

**Files to change:** `WaitlistViewViewModel.cs` (560–701),
`WaitlistViewDetailViewModel.cs` (316–520 and the `FieldValue` defaults),
plus any new/updated stored procedures and queue query files, and the localization entries in
`Strings/en-us/Resources.resw` with the `Module_Waitlist/Views/*.xaml` bindings that consume them.

## 7. Verification

1. Add a unit test that builds a `SampleOrder` from a `WaitlistRequest` whose part/quantity/customer
   values are *distinctive test values*, then asserts the card fields carry **those** values — this
   fails today and cannot pass against a literal.
2. Add a test asserting no field value in `AddRequestFields` matches a currency/dimension/quantity
   shape that was not derived from the request (for example assert the coil number equals the
   request's coil or is empty).
3. Manual: create one request of each type against a known work order, and confirm every rendered
   material value can be traced to that work order in Infor Visual.
4. Extend `RetiredSymbolAuditTests` (or add a sibling audit) to fail on the specific literal set
   listed in §3, so a regression cannot reintroduce them.
5. Re-run `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`
   (full suite must stay `Failed: 0`) and confirm the build stays `0 Warning(s) 0 Error(s)`.

## 8. Related

- `defects/Closed-Partial-High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` — same method, same root cause,
  for the one field that *is* wired to a real service.
- `defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — the same fabricated-data pattern on the
  confirm screen.
- `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` — the other half of the card's
  unusable state.
- `specs/001-module-mock-visual-fallback/spec.md` FR-001, FR-003, FR-014, SC-013;
  `.specify/memory/constitution.md` II; `FEATURES.md` §3, §4.
