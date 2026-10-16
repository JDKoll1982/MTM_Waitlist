# Medium — Mock cache service — two shapes were reported as seed-only content while their mirrors were fully refreshed

| Field | Value |
| --- | --- |
| **Criticality** | Medium — no data was lost and no read was served wrongly, but two status surfaces stated the opposite of the truth: a fully refreshed shape was reported as content that had never been refreshed. The defect also hid the shape it was worst for, because a shape that genuinely has never refreshed gets the identical report |
| **Feature area** | `mtm_mock` freshness report → the Mock cache service's `/api/status` payload and the app's read-status indicator |
| **Type** | Reporting defect: a hand-maintained enumeration fell behind the shape catalog |
| **Found** | 2026-10-14, by the owner comparing the live host against the repository (`LIVE-DB-AND-MOCK-UPDATES.md` §2.3); carried into the hand-off as `NEXT-RUN.md` §2.5. Confirmed and root-caused 2026-10-16 on `V-MTMFG-5` |
| **Status** | **FIXED** and verified on the host the service runs on — see §5 |
| **Fixed by** | Adding the two missing `UNION ALL` branches to `sp_visual_read_shape_freshness_get`, regenerating the mock master list through the builder script, and pinning the enumeration against the shape catalog with a test |
| **Files** | `Database/Mock/StoredProcedures/sp_visual_read_shape_freshness_get/create.sql`, `Database/Mock/AllSPs.sql` (regenerated), `Database/Mock/Validation/read_shape_metadata/validate.sql`, `Database/Mock/Validation/all_shapes_roundtrip/validate.sql`, `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests.cs` |

---

## 1. Reported symptom

`GET /api/status` on the host reported six shapes. Five of them were plainly healthy, and the sixth read:

| Field | Value reported |
| --- | --- |
| `shapeKey` | `employee_by_number` |
| `lastOutcome` | `succeeded` |
| `lastRowCount` | `2084` |
| `refreshedUtc` | `null` |
| `isSeedContentOnly` | `true` |

The mirror was, in fact, holding 2 084 freshly read Infor Visual rows with `is_seed_content = 0` on every one of
them, and the service's own log for that shape ended in `Shape employee_by_number refreshed: 2084 row(s)`.

The owner's note read as a suspicion and asked for it to be chased after the redeploy:

> the refresh for that shape is reporting success without swapping a snapshot.

The suspicion was right that something was reporting wrongly, and wrong about which thing: the refresh was never
broken. The report was.

## 2. Root cause

`sp_visual_read_shape_freshness_get` is the one mock artifact that **must enumerate the shapes by hand**. Freshness is
a per-table read, so unlike `sp_visual_read_shape_metadata_get` it cannot be derived from `information_schema`; it is
a `UNION ALL` chain with one branch per shape. The published playbook says so, at step 3b of
`specs/001-module-mock-visual-fallback/contracts/mock-service-configuration.md` — *"Add one `UNION ALL` branch for the
shape to the freshness report."*

It shipped with **five** branches, for shapes 1–5. Then:

* feature **011** added shape 6, `employee_by_number`, to the catalog, to every artifact the shape needs, to the seed
  note, to the table descriptions and to the roundtrip validation — and not to this procedure;
* feature **012** added shape 7, `part_warehouse_quantities`, the same way — and not to this procedure either.

Nothing failed. The procedure is valid SQL, it simply answered about five shapes.

`ServiceApiOperations` builds the status payload by iterating the **catalog** and looking freshness up by shape key,
so a shape the report omits is not an error there — it is a miss, and a miss falls through to the defaults written
for "no freshness information available":

```csharp
shapeFreshness?.RefreshedUtc,
shapeFreshness?.IsSeedContentOnly ?? true,
```

which is exactly `null` and `true`. The app's read-status indicator reads the same procedure through
`ReadStatusProvider`, so both surfaces produced the same false answer from the same omission.

## 3. Impact

* An operator reading `/api/status`, or a user reading the app's read-status indicator, is told a shape is serving
  seeded placeholder content that has never been refreshed when it is in fact fully refreshed. That is the exact
  inverse of the truth, on the surface the operator uses to judge whether the cache is warm.
* The false answer is **indistinguishable** from the correct one for a shape that genuinely has never refreshed, so
  the report could not be used to find a cold shape either — the omission masked both states.
* Shape 7 would have been invisible in the report from the day it was deployed. That is how it was found: the
  freshly added seventh shape was reported seed-only the moment it landed.
* No read, no write and no backup was affected. The mirror was correct throughout; only the report about it was wrong.

## 4. Fix

1. `Database/Mock/StoredProcedures/sp_visual_read_shape_freshness_get/create.sql` gained the two missing branches,
   `employee_by_number` over `visual_employee_by_number_result` and `part_warehouse_quantities` over
   `visual_part_warehouse_quantities_result`. Its header note now says the branch is mandatory rather than cosmetic,
   names this defect, and points at the test that enforces it.
2. `Database/Mock/AllSPs.sql` was regenerated by `Database/CopilotScripts/build_mtm_mock_masters.ps1` — never edited
   by hand (`NEXT-RUN.md` §7).
3. `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests` gained
   `…_TheFreshnessReport_NamesEveryCatalogShapeExactlyOnce`, which reads the shipped `create.sql` and requires one
   `'<shape key>' AS shape_key` literal for every key in `VisualReadShapeCatalog` — so the next shape added without
   its branch fails the build instead of failing silently in production. Checked against the pre-fix artifact the
   test reports `employee_by_number: 0 branch(es)` and `part_warehouse_quantities: 0 branch(es)`.
4. The live validations now exercise the report. `read_shape_metadata/validate.sql` (read-only) gained steps 7 and 8,
   so the enumeration can be checked on a store that must not be written to; `all_shapes_roundtrip/validate.sql`
   gained step 11, asserting the report covers every shape with no shape still seed-only after the seven refreshes.

## 5. Verification

**Build-time.** `tools/run-tests.ps1 -Modules ShapeAdditionNoDowntimeTests` — 6 passed, 0 failed, including the new
guard. `tools/run-tests.ps1 -Modules Module_Mock` — 292 passed, 0 failed, 2 skipped.

**Live, on the host the service runs on (`V-MTMFG-5`, `172.16.1.104`, 2026-10-16).**

* `build_mtm_mock_masters.ps1 -Check` — exit 0, master lists current.
* `read_shape_metadata/validate.sql` — the report now returns **seven** rows, one per shape, with
  `employee_by_number` at `refreshedUtc 2026-10-16 14:00:02`, `is_seed_content 0`, `row_count 2084`.
* `GET /api/status` after the service was republished to **1.1.0** and redeployed — seven shapes, every one
  `lastOutcome: succeeded`, every one with a non-null `refreshedUtc` and `isSeedContentOnly: false`. Shape 6 reports
  2 091 rows and shape 7 reports 8 746.
* `POST /api/refresh` — one run, seven results, all `succeeded`.

No open remainder. The two shapes this defect misreported are both correct on the host, and the test that would have
caught it is in the build.
