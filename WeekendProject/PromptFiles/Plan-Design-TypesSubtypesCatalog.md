# Current Types & Subtypes — Full Catalog (both modules)

> Parsed from code on 2026-09-07. Authoritative sources:
> `RequestTypeInventory` / `RequestSubtypeInventory` (`MTM_Waitlist.Settings/Models`),
> `Assets/Config/waitlist-request-types.json`, `NewRequestFlowRules.GetDefaultTypes()`,
> `WaitlistViewViewModel.ResolveImagePath` / `AddRequestFields`, `WaitlistRequestTitles`,
> `SetupDataCatalog` / `SetupModels` (`MTM_Waitlist.Setup`).
>
> Use for diagramming the current state before the `Type = umbrella / Subtype = thing` refactor.

## Module 1 — Waitlist / New Request

### Request Types → Subtypes (8 types, 24 subtypes)
1. **Pickup** → `Pickup Other`, `Pickup NCM`, `Pickup WIP`, `Pickup FG`, `Pickup Coil`, `Pickup Flatstock`
2. **Other** → `General Text Entry`
3. **Coil** → `Bring`, `Pickup`, `Wrong Coil @ press`, `Need Riser Table`, `Need Coil Turned around`
4. **Scrap** → `Empty`, `Pickup Hopper, do not return`, `Bring Hopper`
5. **Flatstock** → `Bring`, `Pickup`, `Wrong Flatstock @ Workcenter`
6. **Table Handling** → `Table Place Parts`, `Table Remove Parts`
7. **Die Handling** → `Bring Die`, `Pull Die and Put Away`, `Pull Die and Take to Die Shop`, `Pull Die and Leave @ press`
8. **Forklift Assist** → *(no subtypes)*

### List-card badge families (derived, separate from the above)
Resolved by subtype keyword (`ResolveImagePath`) → template selector; only these have line-card controls:
- **COIL** (`coil.png`)
- **FG** (`pickup_fg.png`)
- **NCM** (`pickup_ncm.png`)
- **O/S** (`pickup_os.png`)
- **WIP** (`pickup_wip.png`)
- **SCRAP** (`scrap.png`)

### Friendly card titles (`WaitlistRequestTitles`)
Key examples: `Coil/Bring → "Deliver: Coil"`, `Coil/Pickup → "Return: Coil"`,
`Scrap/Empty → "Scrap: Empty"`, `Scrap/Bring Hopper → "Deliver: Hopper"`,
`Die Handling/Bring Die → "Deliver: Die"`, `Die Handling/Pull Die and Put Away → "Pull Die: Return to Location"`,
`Table Handling/Table Place Parts → "Assist: Place parts on table"`, `Other/General Text Entry → "General Request"`.

## Module 2 — Module_Setup

> Note: Module_Setup does **not** have its own request Type/Subtype system in code (verified — no
> `RequestTypeInventory` / `RequestSubtypeInventory` / `WaitlistRequestDraft` / `SubmitAsync` usage).
> Its type-like vocabulary:

### Dunnage types (`SetupDunnageType`)
`Coils`, `Flatstock`, `Components`, `Other`

### Subordinate-part categories (`SetupSubordinatePart.Category`)
`Coil`, `Die`, `Component`, `Flatstock` (+ `Other` after normalization)
- Sample instances: Coil `MMC0001000`, Die `FGT-0653`, Component `23-23451-006`, Flatstock / Flat stock `MMF0001154`.

### Setup workflow step enum (`SetupWorkflowStep`, not a type taxonomy)
Work Center → Work Order → Part → Sequence/Operation → Dunnage Type → Review → Result (7 steps)

### Scrap types (Setup side, free-form user-entered — not coded)
`3003 Aluminum`, `5052 Aluminum`, `Galvanized Steel`, `Steel`, `Skeleton`, `Gaylord`

---

## Diagram guidance (the inconsistencies to show)
- Module 1 `Pickup` type's subtypes are really **categories** (`Pickup NCM`, `Pickup Coil`, …).
- Module 1 `Coil` type's subtypes are really **actions** (`Bring`, `Pickup`).
- Module 1 card badges (FG/NCM/WIP/O-S/SCRAP) are a *different* material set than the `Pickup` subtypes.
- Module 2 dunnage categories (`Coils/Flatstock/Components/Other`) don't align with Module 1's material families.
- Target: unify as **Type = umbrella (Pickup/Deliver/Assist)**, **Subtype = thing (Coil/Component/Finished Product…/Other)**.
---

## Clarifications (2026-09-07) — refactor-data source decisions

> These reflect the Request-Config-Template.csv Q&A and supersede the ad-hoc badge families above.

- **Coil / Flatstock pickup is one merged item** `pickup-coil` (Coil or Flatstock), prefix MMC = Coil, MMF = Flatstock; deliver rows stay separate.
- **FG / WIP / Outside Service source = TODO** via Infor Visual product-type; **NCM is a defect-feature refactor** (`waitlist_defect_types` table + CRUD SPs + Module_Settings panel).
- **WIP / Outside `{Sequence}`** comes from `setup_active_jobs.sequence_number`.
- **Dunnage** rows are **user-selected** (image-card style) in both Pickup and Deliver.
- **Riser Table / Hopper** are zero-payload equipment/consumable flag requests.
- **'Other'** is now its own top-level Category (catch-all free text), replacing former `pickup-other` + `assist-forklift`; card is full-width Row 1 / RowSpan = 2.
- Full 18-row target catalog is in `Documents/Request-Config-Template.csv` (Pickup 9 / Deliver 6 / Assist 2 / Other 1).