# Unified Waitlist Card — Line 1 / Line 2 Proposal (by Request Type & Subtype)

> Goal: replace the current per-type 3–6 line card fields with a **single uniform 2-line card**,
> and move type-specific detail data to the detail page. The **Coil card is the format source of
> truth** (richest, most current). Data model intent (user-confirmed 2026-09-07):
>
> - **Line 1 = the ITEM being requested** — the concrete material identifier (e.g. the coil number
>   `MMC0001000`, the part, the die, the scrap type/alloy).
> - **Line 2 = the ACTION** — what is being done with the item (Deliver / Return / Pickup / Empty /
>   Place / Remove / Pull / Assist).
>
> Enumerated from the canonical title map (`WaitlistRequestTitles`) + `WaitlistViewViewModel.AddRequestFields`.

> **SOURCE OF TRUTH (2026-09-07):** the authoritative row spec (card Line 1 umbrella / Line 2 item per
> row) is `Documents/Request-Config-Template.csv`. This doc is the **card-grid + Line1/Line2 layout
> spec** (the grid, ordering, and the full-width 'Other' card on Row 1 / RowSpan = 2 live here).
> Implementation is driven by `14-0%-Phase4-UnifiedWaitlistCard.md`.

## Verified against the codebase (2026-09-07)

Reading the real sources, the request model does **not persist** the item, but that is not the whole
story — item data is partially **live-resolvable** at render time, the same way the detail page
already fetches inventory:

- `WaitlistRequest` / `WaitlistRequestDraft` / DB row store only: `RequestType`, `Subtype`, `InputValue`
  (free text), `WorkCenter`, `WorkCenterName`, `ActiveSetupJobId`, requester, status, timestamps,
  assignee, note. **No part / coil / quantity / location / destination field.**
- **Coil item IS resolvable:** `CoilAvailabilityService` (+ `SampleJobCoilCatalog`) returns the coil on
  the active job for a `WorkCenter` (`CoilNumber`, description, qty-on-hand, avg weight). The New Request
  flow already shows "Current coil COIL-204 …" per work center, and `WaitlistViewDetailViewModel`
  already re-resolves part inventory at render. So a Coil card can re-resolve its item from
  `WorkCenter`/`ActiveSetupJobId` — mock `COIL-204` today, real coil once the file-02/06 Infor wiring is live.
- **WIP has a real anchor:** `ActiveSetupJobId` (the actual active job) is stored and is the detail page's
  fallback for the "Work order" context.
- **FG / NCM / O-S / Scrap / Die / Flatstock / Table:** no stored item and **no resolver keyed off the
  request** returns a real part/die/scrap-alloy today — only hard-coded mock tokens in `AddRequestFields`.
- **Other / Forklift Assist:** the item is the requester's typed `InputValue`, which IS stored → real `✓`.

Readiness legend (updated):

- **Item data ready?** — `✓` real data already used; `~` **live-resolvable** from the request's work
  center/job (mock today, real once Infor is wired); `X` no stored **and** no resolvable identifier yet.
- **Action data ready?** — does the codebase currently carry the data to render Line 2 (the action)?
  The **subtype already encodes the action** on every request, so this is `✓` everywhere; only the
  presentation is missing.

---

## Coil — badge `COIL`
>
> Item = the coil number on the active job. The coil number is treated as the source of truth. The
> request does **not** store it, but it is **live-resolvable** via `CoilAvailabilityService` keyed by
> `WorkCenter` (mock `COIL-204`; real coil once the file-02/06 Infor coil lookup is wired) — the same
> re-resolve-at-render pattern the detail page already uses for inventory.

| Subtype | Current title (action) | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Bring | Deliver: Coil | Coil number (e.g. `MMC0001000`) | Deliver | ~ | ✓ |
| Pickup | Return: Coil | Coil number | Return | ~ | ✓ |
| Wrong Coil @ Press | Deliver & Pickup: Wrong Coil at Work Center | Coil number (the wrong coil on job) | Deliver & Pickup | ~ | ✓ |
| Need Riser Table | Deliver: Riser Table | Riser table | Deliver | X | ✓ |
| Need Coil Turned Around | Assist: Coil Facing Wrong Way | Coil number | Rotate / Turn | ~ | ✓ |

## Scrap — badge `SCRAP`
>
> Item = scrap type / alloy (Galvanized, 3003/5052 Aluminum, Steel, Skeleton, Gaylord) or the hopper.
> Today the alloy only appears as the destination "scrap lugger"; the part field is `"Not provided"`.

| Subtype | Current title (action) | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Empty | Scrap: Empty | Scrap type (alloy) | Empty | X | ✓ |
| Pickup Hopper, Do Not Return | Pickup: Hopper (do not return) | Hopper / scrap | Pickup | X | ✓ |
| Bring Hopper | Deliver: Hopper | Hopper | Deliver | X | ✓ |

## Pickup family — badges `FG` / `NCM` / `WIP` / `O/S` / `COIL` / `FLATSTOCK` / `OTHER`
>
> Top-level request type **Pickup**; the subtype names the material class. (These become the distinct
> card badges via the subtype keyword.)

| Subtype | Card badge / title | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Pickup NCM | `NCM` · Pickup: NCM | Part number | Pickup (→ NCM Area) | X | ✓ |
| Pickup WIP | `WIP` · Pickup: WIP | Work order + part | Pickup (→ WIP) | X | ✓ |
| Pickup FG | `FG` · Pickup: FG | Part number | Pickup (→ ship / packlist) | X | ✓ |
| Pickup Coil | `COIL` · Return: Coil | Coil number | Return | ~ | ✓ |
| Pickup Flatstock | Flatstock · Return: Flatstock | Flatstock part | Return | X | ✓ |
| Pickup Other | Pickup: Other | Request description (typed) | Pickup | ✓ | ✓ |
| Outside Service | `O/S` · Pickup: Outside Service | Part / work order | Pickup (→ vendor) | X | ✓ |

## Flatstock — badge `FLATSTOCK`

| Subtype | Current title (action) | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Bring | Deliver: Flatstock | Flatstock part | Deliver | X | ✓ |
| Pickup | Return: Flatstock | Flatstock part | Return | X | ✓ |
| Wrong Flatstock @ Workcenter | Return: Wrong flatstock | Flatstock part (wrong) | Return / replace | X | ✓ |

## Table Handling

| Subtype | Current title (action) | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Table Place Parts | Assist: Place parts on table | Part | Place | X | ✓ |
| Table Remove Parts | Assist: Remove parts from table | Part | Remove | X | ✓ |

## Die Handling — badge (Die)

| Subtype | Current title (action) | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Bring Die | Deliver: Die | Die number | Deliver | X | ✓ |
| Pull Die and Put Away | Pull Die: Return to Location | Die number | Pull / Return | X | ✓ |
| Pull Die and Take to Die Shop | Pull Die: Take to Die Shop | Die number | Pull / Transport | X | ✓ |
| Pull Die and Leave @ Press | Pull Die: Leave at Press | Die number | Pull / Leave | X | ✓ |

## Other & Forklift Assist (free-text)
>
> Item = the requester's own typed description (stored in `InputValue`), so it is real data already.

| Request type | Subtype | Suggested Line 1 (Item) | Suggested Line 2 (Action) | Item data ready? | Action data ready? |
| --- | --- | --- | --- | :--: | :--: |
| Other | General Text Entry | Request description (typed) | Request | ✓ | ✓ |
| Forklift Assist | (general) | Assistance description (typed) | Assist | ✓ | ✓ |

---

## Key takeaways

1. **Line 2 (Action) is ready everywhere** — it is already the subtype, and the title map already
   phrases it (Deliver / Return / Pickup / Empty / Place / Remove / Pull / Assist). Only presentation is missing.
2. **Line 1 (Item): the request never stores the item**, but for **Coil it is live-resolvable** via
   `CoilAvailabilityService` by work center (`~`; mock today, real once Infor is wired). The genuinely
   blocked rows are **FG / NCM / O-S / Scrap / Die / Flatstock / Table** (no stored **and** no resolvable
   identifier), plus **Need Riser Table** (no table identity source at all). The code currently shows
   mock tokens (`COIL-204`, `FG-10042`, `RM-50218`, `WO-072368`, `"Not provided"`).
3. **WIP** has a real stored anchor (`ActiveSetupJobId`) it can surface, though that is a job id, not a WO number.
4. **Scrap needs a dedicated item field** — "scrap type/alloy" is not first-class; it is only the
   destination lugger today.
5. **Free-text types (Other / Forklift Assist / Pickup Other)** already render a real item ✓ and need no new wiring.

---

## Clarifications (2026-09-07) — this file is the card-grid + line-1/line-2 layout spec

> Per Request-Config-Template.csv Q&A, the overall **card grid layout** (column count, ordering, the full-width **'Other' card on Row 1 with RowSpan = 2**) is specified here.

- **Card Line 1 = umbrella verb** (Pickup / Deliver / Assist / Other); **Card Line 2 = the item identifier** (e.g. `{CoilNumber}`, `{PartNumber}`, `{DieNumber} - {DieLocation}`, `{DunnagePart}`, or `{UserMessage}` for Other).
- **Coil / Flatstock** pickup is one merged card item; deliver rows stay split.
- **Item identifiers resolve from the active setup job** where available: coil/flatstock/die/component from `setup_active_jobs.subordinate_parts_json`; dunnage from `selected_dunnage_parts_json`; WIP/Outside `{Sequence}` from `sequence_number`. **FG / NCM / WIP / Outside have no persisted item yet (TODO).**
- **Dunnage** line 2 shows the user-selected `{DunnagePart}` via Module_Setup-style image cards.
- **Riser Table / Hopper** show a fixed label (zero payload).
- **Conditional visibility:** auto-populated rows (coil/flatstock/die/component/dunnage) only appear if the requesting job actually has that part.
- **Die location** (shown when destination = Home Location) is pulled during Module_Setup — source field to confirm.
- Uniform card is the format source of truth; detail data moves to the detail page.
