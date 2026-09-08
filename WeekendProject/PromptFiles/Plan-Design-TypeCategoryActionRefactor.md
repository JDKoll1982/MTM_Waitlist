# Type / Subtype Refactor — Type = Category, Subtype = Action

> Design doc (2026-09-07). User direction: the current Type/Subtype system is convoluted.
> **Type should be a category** (the class of material/asset a request concerns) and **Subtype
> should be the action** (what is being done with it: Deliver / Return / Pickup / Empty / Place /
> Remove / Pull / Assist / General).
>
> This restructure is meant to be designed/executed **alongside** the uniform 2-line card refactor
> (see `Unified-Waitlist-Card-Proposal.md`), which consumes `Type` (badge = category) and `Subtype`
> (action = Line 2).

> **SOURCE OF TRUTH (2026-09-07):** the authoritative, implementation-ready row spec is
> `Documents/Request-Config-Template.csv` (18 rows: Pickup 9 / Deliver 6 / Assist 2 / Other 1).
> This design doc records the reasoning and the confirmed taxonomy; where it diverges from the CSV,
> the **CSV wins**. Implementation is driven by `14-0%-Phase4-UnifiedWaitlistCard.md`.

## The problem (two inconsistent vocabularies)

1. **New-Request / DB types** (`request_types`): `Pickup, Other, Coil, Scrap, Flatstock,
   Table Handling, Die Handling, Forklift Assist`. `Pickup` is really an *action*, not a category;
   its subtypes (`Pickup NCM / Pickup WIP / Pickup FG / Pickup Coil / Pickup Flatstock / Pickup Other`)
   embed the true *category* inside the name. `Coil` is already a category whose subtypes are
   actions (`Bring / Pickup / Wrong Coil @ Press / …`).
2. **Card-badge families** (COIL / FG / NCM / WIP / O/S / SCRAP) are a *different* material set,
   derived ad hoc from subtype keywords in `WaitlistViewViewModel.ResolveImagePath`.

So the same concept ("Coil") is a type here, a subtype there, and a badge keyword elsewhere — and
"Pickup" is overloaded as both an action and a pseudo-category.

## Target rule

- **Type = Category** — the material/asset class. One canonical set.
- **Subtype = Action** — what a handler does with the item. Small, shared action vocabulary.
- The **stable GUIDs** already in `RequestTypeInventory` / `RequestSubtypeInventory` are preserved
  (they back image overrides), so this is a **display-name + model re-map**, not new identities.

## Confirmed taxonomy (2026-09-07) — SUPERSEDES the draft below

User provided an authoritative hierarchy sketch and confirmed: **Category (Type) = the umbrella
(`Pickup` / `Deliver` / `Assist`); Subtype = the THING being handled** (not the verb). The earlier
draft that listed materials (Coil/FG/NCM/…) as top-level categories is superseded.

Confirmed umbrella (Category) tree:

```
Pickup
├── Coil
├── Component
├── Finished Product
│   ├── FG
│   ├── NCM
│   ├── WIP
│   └── Outside Service
└── Other  → "User Enters Part number, Infor Visual validated"

Deliver   (same Thing subtree as Pickup — ASSUMED, confirm)
Assist    (general/assist)
```

Notes:
- **Type = umbrella Category**: `Pickup`, `Deliver`, `Assist` (user-confirmed roots).
- **Subtype = the Thing** hanging under the umbrella (e.g. `Pickup-Coil`, `Deliver-Coil`).
- `Finished Product` is a grouping node whose leaves are the disposition paths FG / NCM / WIP /
  Outside Service (which sub-kind of finished product).
- `Other` = handler types a free-form **Part number**, validated against Infor Visual.
- **Uniform card (user-confirmed)**: **Line 1 = the umbrella** ("Deliver") · **Line 2 = the concrete
  item identifier** (e.g. `MMC0001000`).

> Remaining assumption to confirm: `Deliver` reuses the same Thing subtree as `Pickup` (Coil /
> Component / Finished Product… / Other). If it has its own set, that changes the catalog.

## Proposed catalog (DRAFT — superseded for Pickup by the confirmed tree above)

### Canonical Categories (Type) (draft)

| Category | Scope | Card badge |
| --- | --- | --- |
| Coil | coil inventory | COIL |
| Finished Goods | FG to ship | FG |
| Non-Conforming | rejected material | NCM |
| Work In Process | WIP between ops | WIP |
| Outside Service | material to a vendor | O/S |
| Scrap | scrap / offal removal | SCRAP |
| Flatstock | flat sheet material | FLATSTOCK |
| Die | tooling / dies | DIE |
| Table | parts on/off a table work center | TABLE |
| Other | general / forklift assist / misc | OTHER |

### Canonical Actions (Subtype)

`Deliver` · `Return` · `Pickup` · `Empty` · `Place` · `Remove` · `Pull` · `Assist` · `General`

### Current → target mapping (draft)

| Current (Type / Subtype) | → Category (Type) | → Action (Subtype) |
| --- | --- | --- |
| Pickup / Pickup NCM | Non-Conforming | Pickup |
| Pickup / Pickup WIP | Work In Process | Pickup |
| Pickup / Pickup FG | Finished Goods | Pickup |
| Pickup / Pickup Coil | Coil | Return |
| Pickup / Pickup Flatstock | Flatstock | Return |
| Pickup / Pickup Other | Other | Pickup |
| Pickup / Outside Service | Outside Service | Pickup |
| Coil / Bring | Coil | Deliver |
| Coil / Pickup | Coil | Return |
| Coil / Wrong Coil @ Press | Coil | Replace *(Deliver+Pickup)* |
| Coil / Need Coil Turned Around | Coil | Turn *(Assist)* |
| Coil / Need Riser Table | ? *(see open Qs)* | Deliver |
| Scrap / Empty | Scrap | Empty |
| Scrap / Pickup Hopper, Do Not Return | Scrap | Pickup |
| Scrap / Bring Hopper | Scrap | Deliver |
| Flatstock / Bring | Flatstock | Deliver |
| Flatstock / Pickup | Flatstock | Return |
| Flatstock / Wrong Flatstock @ Workcenter | Flatstock | Replace |
| Table Handling / Table Place Parts | Table | Place |
| Table Handling / Table Remove Parts | Table | Remove |
| Die Handling / Bring Die | Die | Deliver |
| Die Handling / Pull Die and Put Away | Die | Store *(Return)* |
| Die Handling / Pull Die and Take to Die Shop | Die | Transport |
| Die Handling / Pull Die and Leave @ Press | Die | Leave |
| Other / General Text Entry | Other | General |
| Forklift Assist | Other | Assist |

## Affected areas (for the eventual implementation)

- **Module_Setup (user-flagged):** the Setup workflow shares the shell/request model and request-type
  imagery with the New Request flow. Its direct service code does **not** reference the waitlist
  `RequestType`/`Subtype` inventory today (verified 2026-09-07: no `RequestSubtypeInventory` /
  `WaitlistRequestDraft` / `SubmitAsync` usage in `MTM_Waitlist.Setup`), but its dunnage "TypeId"
  vocabulary (`Coils / Flatstock / Components / Other`) and its role in driving the request flow mean
  it must be re-mapped in lockstep. The exact Setup coupling point is to be confirmed before coding.
- `Assets/Config/waitlist-request-types.json` + DB seed (`Database/Seeds/seed_waitlist_request_catalog/create.sql`, `AllSeeds.sql`) — display-name/model re-map (keep stable GUIDs).
- `RequestTypeInventory` / `RequestSubtypeInventory` (Settings) — canonical category set + action subtypes.
- `NewRequestFlowRules.GetDefaultTypes()` + `NewRequestFlowService` flow — order/pick by category → action.
- `WaitlistRequestTitles.For(...)` — titles become `Action: Category`.
- `WaitlistViewViewModel.ResolveImagePath` / `AddRequestFields` — badge = category, Line 2 = action, and the ad-hoc keyword matching is replaced by the explicit model.
- Card template selector / badges (COIL/FG/… = category) and the uniform 2-line control.
- `SampleWaitlistRequestCatalog` + related tests.

## Open questions before coding

1. **"Need Riser Table"** — a riser table is an *equipment* delivery, not a coil item. Does it belong to
   category **Other** (action Deliver) rather than Coil? Or is it a legit Coil-area request (riser used
   with coils) that stays under Coil?
2. **Table Handling / Die / Forklift** — keep `Table`, `Die`, and `Other (forklift assist)` as categories,
   or fold Table-handling under a generic? They are legitimate work-center *actions on parts/dies*, so the
   draft keeps them as categories.
3. **Action granularity** — should `Pull …` variants (Store / Transport / Leave) collapse to a single
   `Return`/`Transport`, or stay explicit? This affects how many distinct action options a handler sees.
4. **"Replace / Wrong …"** — treat Wrong Coil / Wrong Flatstock as a distinct action (`Replace`) or as
   `Deliver+Return`? The current titles phrase them as "Deliver & Pickup …".
5. **Scope of the re-map** — full data migration (JSON + DB seeds + inventories) plus UI, or start by
   re-naming only the in-code catalog (`RequestTypeInventory`/`RequestSubtypeInventory`/titles) and defer
   the DB/JSON refresh?

Please answer 1–5 (or tell me to make reasonable calls) so I can lock the catalog and fold this into the
uniform-card refactor plan.

---

## Clarifications (2026-09-07) — from Request-Config-Template.csv Q&A

Resolved the open questions and product-type gaps. Authoritative row spec: `Documents/Request-Config-Template.csv`.

1. **FG / WIP / Outside-Service product type = TODO.** Source of truth is `(none yet) - Refactor Module_Setup`. Must figure out how the **product type (FG / WIP / Outside)** gets set **through Infor Visual**. NCM is **NOT** a product-type refactor — it is a **defect-feature refactor** (see below).
2. **WIP / Outside `{Sequence}` source = existing column `setup_active_jobs.sequence_number`** (no new read-back needed for the sequence value).
3. **Die location (pickup-die when destination = Home Location):** believed to be pulled during the Module_Setup workflow — **where is unknown; research Module_Setup before coding.**
4. **NCM defect feature (real scope, mtm_waitlist):** new table `waitlist_defect_types` + CRUD stored procedures + a **settings panel in Module_Settings** to add/remove defect types. Defect is user-picked from a searchable list.
5. **Dunnage (pickup & deliver):** user **always selects** the dunnage part — using the **same image-card selection style as Module_Setup** (so Dunnage rows are **user-entry**, not auto).
6. **Riser Table / Hopper = pure flag requests (zero payload):** request type + work center + optional note only.
7. **Card-grid layout (Row 1, RowSpan = 2 on the 'Other' card):** the layout spec lives in `16-100%-Design-UnifiedWaitlistCard.md`.
8. **Uniform card (confirmed):** Line 1 = umbrella Category, Line 2 = the item identifier.
