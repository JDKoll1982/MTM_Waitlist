# High — Work Center Setup — the ignored-locations set hid the job's own coil

| Field | Value |
| --- | --- |
| **Criticality** | High — the work centre's saved setup record was wrong: the job's coil was missing from it, so the Setup review showed a one-part job, and every reader of that record (the waitlist's coil availability, the item picker) reported a job with **no coil** while Infor Visual carried one |
| **Feature area** | Work Center Setup → Subordinate Parts (and everything downstream of the saved record) |
| **Type** | Silent data loss: a display filter applied to the wrong kind of value, removing whole parts instead of hiding locations |
| **Found** | 2026-09-24, by the owner, asking why WO-074171's coil `MMC0000887` was not being pulled for work centre 100-19 |
| **Status** | **FIXED** — Setup no longer consults the ignored-locations set. Verified by test and in the running app; §5 states what an already-saved job still needs |
| **Fixed by** | Removing the ignored-locations dependency and its filter from `SetupLookupService` (the set stays the waitlist's inventory rule) |
| **Files** | `MTM_Waitlist.Setup/Services/SetupLookupService.cs`, `MTM_Waitlist.Tests/Module_Setup/Services/SetupWorkflowServiceTests.cs`, `MTM_Waitlist.Tests/Module_Setup/Services/SetupLookupFixtureData.cs` |

---

## 1. Reported symptom

> "Investigate why the current work order WO-074171 Op 19 is not pulling its coil MMC0000887 from the subordinate
> part table"

Infor Visual showed the coil on the same operation the app was set up on (WO-074171 / 16-21374-000, op 10 — the
work centre 100-19 "Setup & Run Production" operation): materials for SubID 0 Op# 10 carried both `10 (R) MMC0000887
– Coil, .375 X 10.394` and `20 (C) FGT0199-01 – PROGRESSIVE COMPLETE`. The app's Work Center Setup review for work
centre 100-19 listed **one** subordinate part — the die — under "Die | 1 part".

## 2. Root cause

The read was right; the application then threw the coil away.

- The cached copy of the subordinate-parts read (refreshed 2026-09-24 17:00) held **both** rows for
  `WO-074171 / 16-21374-000 / 10`: the die `FGT0199-01` at `R-G0-11`, and the coil `MMC0000887` at **`WC`** with
  149,691.8 on hand.
- `SetupLookupService.GetSubordinatePartsAsync` passed those rows through `ExcludeIgnoredLocationsAsync`, which
  dropped every part whose `Location` is in the shared ignored-locations set. The built-in defaults are
  `WC, NCM, V-WC, NCM-VITS, SHIP`, and no custom list is stored on this workstation, so `WC` was in force.
- The coil's only location is `WC`, so the coil row was removed; the die at `R-G0-11` survived. That is exactly the
  "1 part" the review showed.
- The filtered list is what gets saved: the active job for work centre 100-19 (saved 18:22 that day) carried only
  the die in `subordinate_parts_json`. Everything downstream reads that payload, so the job looked coil-less —
  `CoilAvailabilityService` resolves the coil from it, and the item-picker cards are built from it.

Why the rule was there at all: the ignored-locations set exists to keep plant inventory codes out of the waitlist's
**inventory location lists** (`WaitlistInventoryService` + `InventoryLocationFiltering.Apply`), where codes like
`NCM` and `SHIP` are noise. Applying it to a job's requirement data was a category error — a coil is issued *to a
work centre*, so its location in Infor Visual is a plant code by definition, and filtering locations there removes
parts rather than noise. The 2026-09-05 implementation note in
`WeekendProject/PromptFiles/05-100%-Phase1-locignore.md` assumed subordinate-part locations were rack labels
"unrelated to plant location codes"; the live data contradicts it.

## 3. Impact

- The Setup review for 100-19 under-reported the job: the coil the operator sets up with was not on the screen.
- The saved record was wrong, so the wrongness propagated: waitlist coil availability reported no coil for the work
  centre's job, and the item picker offered no coil card for it.
- Because the set is applied at read time, the loss was invisible: the app never said a part had been filtered out.
- Other jobs are exposed the same way whenever a subordinate part's only location is a plant code (`WC`, `NCM`,
  `V-WC`, `NCM-VITS`, `SHIP`).

## 4. Fix

`SetupLookupService` no longer injects or consults `IIgnoredLocationsService`, and every subordinate part the read
returns is passed through as it came, location included. The ignored-locations setting is untouched and still governs
the waitlist's inventory location lists, which is where it was meant to apply. The two tests that encoded the old
behaviour were replaced: `SelectSequenceAsync_KeepsSubordinatePartsWhoseLocationIsAPlantCode` and
`SelectSequenceAsync_KeepsTheJobsCoilAtItsWorkCentreLocation` (the second is the regression test for this defect,
carrying a coil at `WC` in the fixture).

## 5. Verification and what is left

- `MTM_Waitlist.Tests` — full suite green, including the two new Setup tests. The fixture row the defect needs
  (`MMC0000887` at `WC`) is asserted to reach the review context with its location intact.
- Running app: work centre 100-19's Setup review now lists the coil beside the die, and the saved record for the
  work centre contains both parts after the setup is saved again (see below).
- **Not retroactive.** The record already saved for a work centre keeps whatever it was saved with. Work centre
  100-19 still holds the die-only payload from 18:22 until its setup is selected and saved again; the coil appears
  in the waitlist's coil availability only after that.
