# Medium — Waitlist — the Part stock window said "No stock is recorded" beside a card that read 2,885 in house

| Field | Value |
| --- | --- |
| **Criticality** | Medium — no data was lost and nothing was written, but the window flatly contradicted the card that opened it and denied stock the plant holds. A handler reading it would conclude the component was out of stock |
| **Feature area** | Waitlist → Deliver Component card → `Part stock` window (`PartStockDialog`) |
| **Type** | Truthfulness bug: the part-wide total was computed, then not drawn, and the empty-location state made an unconditional claim |
| **Found** | 2026-10-22, by the owner using the built app on the live store |
| **Status** | **FIXED** — the window now lists **every** location that holds the part, including the ones the detail page's grid hides, and draws the total whether or not a location row came back |
| **Fixed by** | `PartStockService` no longer consults the ignored-locations set (`InventoryLocationFiltering.HoldingLocations`), plus `PartStockDialogViewModel.HasTotal` / `ShowsStockRows` and a total-aware empty state |
| **Files** | `MTM_Waitlist.Waitlist.View/Services/PartStockService.cs`, `MTM_Waitlist.Waitlist.View/Services/InventoryLocationFiltering.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/PartStockDialogViewModel.cs`, `Module_Waitlist/Views/PartStockDialog.xaml`, `Strings/en-us/Resources.resw`, `MTM_Waitlist.Tests/Module_Waitlist/Services/PartStockServiceTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Services/InventoryLocationFilteringTests.cs` |

---

## 1. Reported symptom

> "analyze and fix the issue (card says 2885 in house, and modal says none)"

The Deliver Component card for component `A0006376159-Blank` read **Total Qty In House 2,885**, and the window
opened from that same card's identifier answered **"No stock is recorded for this part."** with the
**Infor Visual** badge beside it — so the lookup had succeeded and both numbered surfaces disagreed.

## 2. Root cause

Two behaviours, each defensible alone, that cannot both be shown at once.

1. **The card's total and the window's rows are filtered differently, on purpose.** The card's
   `Total Qty In House` is `PartStockService`'s `TotalQuantity`, which is Infor Visual's part-wide sum
   (`GetPartWarehouseQuantities.sql`: `SUM(PART_LOCATION.QTY)` over **every** location). The window's rows are the
   same service's `Locations`, which pass through `InventoryLocationFiltering.Apply` — on-hand ≥ 1 **and** not in the
   shared ignored-locations set (defaults `WC, NCM, V-WC, NCM-VITS, SHIP`).
2. **The window then discarded the total it had.** `TotalText` was set from `TotalQuantity`, but the total row lived
   inside a `ScrollViewer` whose visibility was `HasLocations`, so with no surviving row the total was never drawn —
   and `EmptyText` was chosen as `Locations.Count == 0 ? "NoStock" : ""`, so it claimed "no stock" without
   consulting the total at all.

The live data makes the combination concrete: `A0006376159-Blank` has twelve `PART_LOCATION` rows summing exactly
**2,885.00**, and every unit of it sits in `WC` — one of the shipped ignored locations. Every other row is zero, so
after the filter the list was empty while the part-wide total was 2,885.

## 3. Evidence

| Claim | Evidence |
| --- | --- |
| The card's 2,885 is the real part-wide sum | `SELECT p.ID, SUM(pl.QTY) …` against live `VISUAL`/`MTMFG` returned `A0006376159-Blank \| 12 rows \| 2885.00` |
| All of it is in an ignored location | The same part's rows are `FG .00, NCM .00, QUALITY .00, R-O3-04 .00, SHIP .00, T-00 .00, U-04 .00, U-E0-00 .00, W-00 .00, W-04 .00, WC 2885.00, X-00 .00`; `WC` is in `IgnoredLocationDefaults.Locations` |
| The filter, not the read, emptied the list | `InventoryLocationFilteringTests` and `PartStockServiceTests` both assert ignored and sub-one rows are dropped while `TotalQuantity` keeps them |

## 4. Fix

**The window's location list is no longer filtered.** The window asks *where is this material held*, so
`PartStockService` now reads the Visual rows through `InventoryLocationFiltering.HoldingLocations` — every location
holding at least one unit, nothing hidden — and no longer depends on `IIgnoredLocationsService` at all. The plant's
ignored-locations set keeps its own job, which is the request detail page's inventory grid (`WaitlistInventoryService`
→ `InventoryLocationFiltering.Apply`), and stops being applied to a surface whose whole point is the complete answer.
`A0006376159-Blank` therefore lists `WC 2,885` instead of nothing.

The two supporting rules stay, because they keep the window honest in the cases the list cannot cover:

- `PartStockDialogViewModel.HasTotal` (`!IsLoading && TotalText` non-empty) and `ShowsStockRows`
  (`HasLocations || HasTotal`); the `ScrollViewer` binds to `ShowsStockRows` and the total row to `HasTotal`, so the
  total is drawn even when the location read returns no row at all.
- The empty state is decided by the total rather than by the row count: a total above zero with no rows reads
  **"This part's stock is not held in any location this lookup can show."** (`PartStock_Dialog.NoLocations`); only a
  zero total reads `PartStock_Dialog.NoStock`.

## 5. Verification

- `InventoryLocationFilteringTests` gains `HoldingLocations` cases: every plant code the grid hides is still shown,
  the zero-quantity row is dropped, ordering holds, and a null collection answers nothing.
- `PartStockServiceTests` gains the live case — `A0006376159-Blank` in `WC` draws `WC 2,885` and no empty-state text —
  plus the total-with-no-rows fallback and the all-zero part that still reads `PartStock_Dialog.NoStock`.
- Confirmed against the live plant system: `SELECT p.ID, SUM(pl.QTY)` returns `A0006376159-Blank | 12 rows | 2885.00`,
  all of it in `WC`.
- Full suite: 2,314 passed, 52 skipped, 0 failed; the app builds and starts with 0 warnings.
