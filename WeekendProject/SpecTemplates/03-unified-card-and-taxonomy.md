# Spec seed 03 — Unified 2-line request card and Category/Item taxonomy

| Field | Value |
| --- | --- |
| **Suggested feature name** | `unified-request-card-taxonomy` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §5 — from `PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` (**20 carry-forward**, the live revision) + `Documents/Request-Config-Template.csv` + the three `Plan-Design-*` reference docs |
| **Carry-forward boxes** | **20** (plus the **27** `App-Validation-Checklist.md` §4–§8 checks, which are this workstream's acceptance suite) |
| **Depends on** | template 01 (which removes the fabricated values this spec replaces with real ones). Independent of template 02, but the two touch the same card — coordinate the button slot. |
| **Defect files it completes** | `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` (real values), `defects/High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` (per-coil lookup) |
| **Validation** | `App-Validation-Checklist.md` **§4–§8 (27 checks)** — §0–§3 are `[NOW]` gates already executed by `specs/001` T104/T105 and must not be re-listed |

---

## 1. Why this spec exists

This is the largest UI surface in the backlog and the prerequisite for the analytics workstream's
card-level metrics. It does three things the application was designed around but never finished:
replace the ad-hoc subtype keyword matching with the actual **Category → Item** model, make the card a
uniform two-line shape (verb + item), and — the part that matters most to the floor — **resolve the
item that is really on the job** instead of showing built-in placeholder part numbers.

The source file's own status: *"27/47 = 57%. The open boxes are now the interactive in-app UI phases …
those require running the WinUI app (+ XamlMcp) to build and visually verify."* Its ordering is
Phase 3 (picker) → Phase 4 (card) → Phase 5 (NCM frontend) → Phase 6.2 → Phase 7.

## 2. Input to paste into `/speckit.specify`

```text
Rebuild the waitlist request card and the New Request picker around the Category/Item model the
application was designed for, and make every item identifier shown on a request resolve to what is
actually on the job. Today the card's badge and template are chosen by keyword-matching the stored
sub-type text, the New Request picker presents types rather than categories and items, and the
material identifiers shown on the card (part numbers, coil numbers, work orders, sequences, defect
text, destinations) are built-in placeholders rather than resolved values. This feature introduces the
uniform two-line request card — line one the umbrella verb, line two the item identifier — driven by a
Category (Pickup, Deliver, Assist, Other) and Item taxonomy taken from the request configuration
template, which is the source of truth where documents disagree. The New Request flow becomes
Category then Item, with the item cards the user already knows from Work Center Setup, and items that
are auto-populated from the job appear only when that job actually carries that part. Items that need
the user to supply information (a die destination, a component, a defect, free text for Other) are
validated against the source of truth where the template says so, and Destinations for Deliver rows
stay the requesting work center without user entry. Item resolution becomes real: the finished-goods,
work-in-progress and outside-service rows resolve their part, sequence and disposition from the same
queries the app already uses for inventory locations, work-order lookup and subordinate parts, so a
card can never show a part number, work order or sequence that the request does not actually have —
the placeholder identifiers that are currently hard-coded must be gone, including any fixed part
number, work order or sequence. The request type's image family follows the Category, the
type-specific detail fields move off the card and onto the request detail page, the full-width Other
card spans the first two rows, and every one of the template's canonical rows must render correctly.
A settings panel manages the defect type list used by nonconforming-material requests, and that list
populates the item line for those requests. The existing role gating is preserved and extended to any
item that accepts user entry, and the whole delivery is proven by the validation checklist's section
covering this work.
```

## 3. Carried-forward requirements (from the source; do not weaken)

**The row spec is authoritative** — `Documents/Request-Config-Template.csv` is the 23-row spec
(Pickup 11 / Deliver 8 / Assist 3 / Other 1) and documents defer to it ("CSV wins" where docs
diverge). Its **`Notes / to-build` column carries requirements found nowhere else** and must be
transcribed into the spec, notably:

- `pickup-fg` / `pickup-wip` / `pickup-outside-service` — *"No persisted source yet; must build Infor
  Visual order lookup (or capture in Module_Setup refactor)"*.
- `pickup-ncm` — *"needs new mysql table + stored procedures + settings panel"*.
- `deliver-wrong-coil` / `deliver-wrong-flatstock` — user-entered explanation.
- `other` — card spans Row 1 with `RowSpan = 2`.

**Category/Item → request model** — replace `ResolveImagePath`'s ad-hoc subtype keyword matching with
the explicit Category/Item model (it still drives live badge→template selection), and order
`NewRequestFlowRules.GetDefaultTypes()` by Category then Item.

**New Request picker (Category → Item)** — re-lay selection to match the CSV `Order`; conditional
visibility for auto-populated items (coil, flatstock, die, component, dunnage) driven by whether the
requesting job carries that part *(this needs the composition-root bridge to
`IActiveJobItemResolverService` — the New Request library references Settings but not Setup)*; dunnage
image-card selection in the Module_Setup style, with the user always choosing the dunnage part;
user-entry item paths (die destination, component pick, NCM defect, Other free text) validated against
Infor Visual where the CSV requires it; keep destination = requesting work center for all Deliver
rows; zero-payload items (Riser Table, Hopper) are pure flag selections.

**Uniform 2-line card** — one control, Line 1 = umbrella verb, Line 2 = item identifier; type-specific
detail fields move **off** the card onto the detail page; the Other card renders full-width on Row 1
with `RowSpan = 2`; item data is re-resolved at render for coil/flatstock/die/dunnage rows from the
work center and job; the badge/selector maps Category to the correct image family.

**NCM defect feature** — the backend (`waitlist_defect_types` table, its CRUD procedures and
`DefectTypeCatalogService`) is already done; add the defect-types editor to the Settings module
(searchable list, add/remove), wire the NCM item's line 2 to `{PartNumber} / {Defect (user entry)}`
from the managed list, and cover the table, procedures, picker and card with tests.

**Item resolution (the gate that matters most)**

- Implement FG / WIP / Outside item resolution from the confirmed derivation, reusing the
  `GetInventoryLocations.sql` / `LookupWorkOrder.sql` / `GetSubordinateParts.sql` join patterns
  (VISUAL / MTMFG), feeding the classifier's disposition input. Wire `{PartNumber} / {Sequence}` from
  the active setup job's sequence number and the derived type.
- **Gate:** FG/WIP/Outside items resolve a real part/sequence/disposition — **no hard-coded
  `FG-10042` / `WO-073112 / RM-48190`**.

**Validation & cleanup** — tests covering catalog load, resolvers, the New Request picker and card
render across **all 23 canonical CSV rows**; remove the obsolete field hard-coding in
`WaitlistViewViewModel.AddRequestFields` (replaced by the resolvers); security review of role gating
and Infor Visual validation for user-entry items; then run `App-Validation-Checklist.md` §4–§8 (27
checks) as the acceptance suite.

## 4. Explicitly out of scope — do not resurrect

- **The request-type/subtype editor** — the Developer Settings editor page, the type edit view, the
  4-step guided wizard modal and the per-control card were **cancelled by owner decision
  (2026-09-11)**, and its backend is already deleted. The catalog's *data* half is done; do not build
  the editor UI.
- **The retired snapshot files** `14-30%`, `14-45%`, `14-47%`, `14-53%`, `14-55%` — earlier revisions
  of the same list; never merge their boxes.
- **Mock-parity and mock-mode flow boxes** in the source — obsolete (`OPEN-WORK-NEXT-SPEC.md` §3).
- **Analytics metrics on the card** — template 04 consumes this card, it does not extend it here.
- **Any re-introduction of sample/demo data** — the resolvers must fail to *nothing*, never to a
  placeholder.

## 5. Defects this spec completes

| Defect file | What this spec delivers |
| --- | --- |
| `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` | the real part/quantity/customer/vendor values, resolved from the request and its job, replacing the values template 01 removed |
| `defects/High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` | the coil's own part resolved from the job, so the average-weight lookup is requested for the right part |

Both files already record template 01 as the interim fix and **this spec as the completion**; update
each one's status line when this spec lands.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. `App-Validation-Checklist.md` **§4–§8 (27 checks)** executed as the acceptance suite.
3. Card-render tests across all 23 CSV rows, asserting the resolved item values (never a literal).
4. UI automation pass with XamlMcp where available, and the PowerShell UIA recipe otherwise
   (`.github/instructions/winui3-ui-automation.instructions.md`) — the picker and card are visual by
   nature and the source file says so.
5. Remember the build traps: `[RelayCommand]` strips a trailing `Async` from generated command names,
   `x:Name` is required on any element using `x:Load`, and a real XAML error surfaces only as the
   masked `WMC9999`.

## 7. Open decisions to resolve before `/speckit.specify`

1. **Category/Item storage** — new columns/tables for the taxonomy, or derived from the existing
   type/subtype strings? (Affects migration and every guard.)
2. **Coil item resolution source** — the active setup job (`ICoilAvailabilityService`) or the Infor
   Visual work-order read; which wins when they disagree?
3. **NCM defect table** — the source says it "needs new mysql table + stored procedures + settings
   panel", but the backend is reported done; confirm what actually remains.
4. **`IActiveJobItemResolverService` bridge** — where it is registered, given the New Request library
   does not reference the Setup module.
5. **How the 23 CSV rows map to the shipped request types** — confirm before writing requirements,
   because the CSV is the source of truth.
