# Pre-Spec — Waitlist Line Card Layout (image-driven redesign)

**Status**: Pre-specification — decisions captured, spec not yet written.
**Created**: 2026-10-09
**Surface**: `Module_Waitlist/Controls/WaitlistLineCardView.xaml` (the waitlist list rows) **only**.
**How to use**: this file is the seeding input for one `/speckit.specify` run. It is deliberately technology-light in
its "Requirements" section so the spec stays WHAT/WHY, and deliberately specific in its "Decisions" and
"Deterministic cells" sections so the spec does not have to re-litigate them.

---

## 1. What is being asked

The waitlist list row is currently laid out as:

```
| accent | 96x96 picture | bordered 4-row label/value panel        | up to 4 icon buttons |
|  bar   |  + badge      | Requested by / Press / Remaining / Wait | status pill below    |
```

The target is the layout in the supplied reference image:

```
+-- accent bar
|  +--------+  |  Status: <value>          Pickup                    +----+----+----+
|  |        |  |                           MMC0001000                | OK | X  | || |
|  | picture|  |      ( (ring) )           +-------------+-----------+ +----+----+----+
|  | +badge |  |        90%                | Requested by| Location  | | [] | Q  | pr |
|  |        |  |    Elapsed: 2h 58m        | John Koll   | V-A0-00   | +----+----+----+
|  +--------+  |                           +-------------+-----------+               |
|              |                           | Press Location: 100-03  |               |
+-----------------------------------------------------------------------------------------+
```

Read left to right the card has **four zones**, separated by vertical rules:

1. **Identity zone** — the material picture with its new-message badge, unchanged in meaning.
2. **Status zone** — an inline `Status: <value>` line, a circular progress ring with a percentage inside it, and an
   `Elapsed:` line beneath.
3. **Identity-and-detail zone** — the large title, the request identifier beneath it, and a bordered panel of
   label/value cells split by internal rules.
4. **Action zone** — a 2×3 grid of icon buttons.

---

## 2. Decisions locked with the requester

| # | Decision | Value |
|---|---|---|
| D1 | Surface | The waitlist list rows only. The detail page is **not** changed by this work. |
| D2 | Action buttons | Only actions the application can really perform are wired. No new business actions. |
| D3 | Action grid shape | Exactly six slots in a 2×3 grid — see §4. |
| D4 | Print | Ships **disabled**, with a "not available yet" tooltip and a `TODO` in the code-behind naming label printing as the future action. It is a placeholder, not a working feature. |
| D5 | Card click | The card body **stops** opening the request. A dedicated button opens it. |
| D6 | Open-details button | Magnifier glyph, tooltip "Open details". |
| D7 | Picture behaviour | Shift+click enlarge and the new-message badge both stay. |
| D8 | Ring value | Percentage of **allotted time used** = `elapsed ÷ allotted minutes`. |
| D9 | Elapsed | The `Elapsed:` line is today's **Waiting** value (requested → now). |
| D10 | Remaining time | No longer a text row. It is conveyed by the ring (and by the overdue marking — see §7). |
| D11 | Status presentation | The coloured status **pill is replaced** by inline `Status: <value>` text, coloured per state. |
| D12 | Title and identifier | Top pair of the detail zone: the request's **Category** word, and the request's own identifier (part number / dunnage / die / request description). |
| D13 | Detail cells | The four cells beneath the top pair are **determined by the request's Item**, not hand-chosen per row. Proposals in §5 are **for review**. |
| D14 | Sizing | A full-width row that stretches; tuned to stay readable at the app's default window size. |
| D15 | Pre-spec | This document. It is the input to a new `/speckit.specify` run — no code is written yet. |

### Decisions still open (must be answered before or during `/speckit.clarify`)

| # | Open question | Why it blocks |
|---|---|---|
| O1 | **Does `Status:` ever read "Overdue"?** The reference image shows `Status: Overdue` in red, but *overdue* is not a lifecycle status — it is derived from the due time. Does the line show the lifecycle status (Waiting / In Progress / Done / Cancelled), or does overdue **replace** it (and, if so, is the lifecycle status then invisible)? | Decides whether the zone is one line or two, and whether the existing overdue invariant (spec 004 FR-012) is still honoured. |
| O2 | **What fills `Location:`?** The store has `building`, `work_center`, `work_center_name` and no `location` column. The image's two values (`V-A0-00`, `100-03`) are both work-centre-shaped. Is `Location` the building, the requesting work centre, or a job-supplied location? | Without it the second cell cannot be filled honestly (constitution: never invent a value the request does not carry). |
| O3 | **The image shows three detail cells, the decision says four.** Confirm the grid is 2 + 4, and confirm how the four are arranged (2×2, or 1 pill row plus 3, etc.). | Layout geometry. |
| O4 | **Items that declare fewer than four fields.** Many Items declare only two or three (see §5). Should the surplus cells hide, or fall back to universal values? | Determines whether the card's height varies per row. A card whose height changes per Item is usually unwanted. |
| O5 | **Spec 004 FR-007 is reversed by D13.** That requirement says declared Item fields appear on the request page and *never* on the card. Confirm the reversal is intended, since it changes a ratified contract. | A spec that silently contradicts a shipped contract is not reviewable. |
| O6 | **`Accept` and `Complete` are currently the same slot in two states.** With a fixed 2×3 grid, one of the two is always blank. Accept the gap, or place them so the gap is predictable? | Visual stability of the grid. |

---

## 3. Field mapping — what each part of the target card is fed by

| Target element | Today's source | Change |
|---|---|---|
| Accent bar | `SampleOrder` → accent brush | unchanged |
| Picture + new-message badge | `Order.EffectiveImagePath`, `Order.HasNewMessages` | unchanged |
| `Status: <value>` | `Order.Status` / `Order.StatusBadgeText` + `RequestStatusPalette` | pill → inline text (D11) |
| Ring percentage | *new* — needs `elapsed`, `allotted_minutes` | new calculation (D8) |
| `Elapsed: <value>` | `Order.WaitingForText` (today's **Waiting** row) | re-labelled and moved (D9) |
| Large title | `Order.Title` (the Item's umbrella phrase) | promoted to the headline (D12) |
| Identifier | `Order.Subtitle` (resolved from the Item's template) | promoted (D12) |
| Detail cells | *new on the card* — the Item's declared fields | new resolution path (§5) |
| Row 1: Accept / Cancel / Give back | `Order.AcceptCommand` / `CancelCommand` / `ReleaseCommand` | restyled into a grid |
| Row 2: Complete / Details / Print | `Order.CompleteCommand` / **new** / **new, disabled** | one new navigation action, one placeholder |
| Status pill | `Order.StatusBadgeText` | removed (D11) |
| Card-body click → open request | `ItemClick` on the list | removed; replaced by the Details button (D5/D6) |

### Ring arithmetic

```
elapsed  = now − requested_utc                 (already computed for the Waiting/Elapsed text)
allotted = the Item's allotted_minutes          (null ⇒ the labelled 15-minute default, per spec 004 §5)
percent  = clamp( elapsed ÷ allotted , 0 , 1 )  (overdue clamps to 100 %)
```

The ring must re-evaluate on the same one-minute tick that already refreshes `WaitingForText` and
`RemainingTimeText`, so the three never disagree. Ring colour should follow the existing remaining-time palette
(green → amber → red), which already carries the overdue signal that D10 removes from the text.

---

## 4. The action grid (2 columns × 3 rows, row-major)

| | Column 1 | Column 2 | Column 3 |
|---|---|---|---|
| **Row 1** | **Accept** — green check | **Cancel** — red X | **Give back** — amber pause |
| **Row 2** | **Complete** — blue box-with-check | **Open details** — magnifier | **Print** — printer, **disabled** |

Rules that carry over unchanged and must not be weakened:

* Each button is drawn only while the row's own gate says the action is available (`CanAccept`,
  `CanCompleteOrRelease`, `CanCancelRequest`, plus the requester's own cancel gate).
* Every button keeps a `ToolTipService.ToolTip` and an `AutomationProperties.Name` from the row's localized text.
* Minimum 44×44 hit target.
* Accept / Complete / Give back keep their three distinct colours and glyphs so they are never mistaken for one
  another.
* **Print** is `IsEnabled="False"` with a "not available yet" tooltip and a code-behind `TODO` naming label
  printing. It is visible so the layout is honest about what is coming, and inert so it cannot mislead.
* **Open details** replaces the card-body click. The picture keeps Shift+click enlarge; the badge keeps its
  meaning.

---

## 5. The four detail cells — proposals for review (D13, O3–O4)

**Method.** Each proposal takes the Item's own declared fields, in their declared order
(`waitlist_request_item_configs.detail_fields_json`), and keeps the first four. Where an Item declares fewer than
four, the table says so and proposes the fallback named in O4. **Nothing here is a decision until the requester
signs it off.**

Universal fallbacks used to pad short lists, in this order: `Requested by`, then `Press Location`.

### Pickup (11 Items — 4 are catalogued but hidden)

| Item | Title (top pair) | Identifier (top pair) | Proposed cells 1–4 |
|---|---|---|---|
| `pickup-coil` | Pickup | coil number | Coil number · Quantity in house · Coil description · Average coil weight |
| `pickup-die` | Pickup | die number | Die · Pickup location · Quantity · *(Requested by)* |
| `pickup-component` | Pickup | component | Component · Part description · Quantity · Requesting work center |
| `pickup-riser-table` | Pickup | part number | Requesting work center · *(Requested by)* · *(Press Location)* · *(empty)* |
| `pickup-dunnage` | Pickup | dunnage part | Dunnage part · Description · Home location · Quantity on hand |
| `pickup-scrap` | Pickup | scrap type | Scrap type · Quantity involved · Scrap lugger · Pickup work center |
| `pickup-hopper` | Pickup | part number | Pickup work center · *(Requested by)* · *(Press Location)* · *(empty)* |
| `pickup-fg` *(hidden)* | Pickup | part number | Part number · Quantity remaining · Part description · Customer |
| `pickup-ncm` *(hidden)* | Pickup | part number | Part · Quantity to move · Pickup location · Destination |
| `pickup-wip` *(hidden)* | Pickup | part number | Part and quantity · Pickup work center · WIP destination · Operation sequence |
| `pickup-outside-service` *(hidden)* | Pickup | part number | Part · Quantity to move · Pickup location · Destination |

### Deliver (9 Items)

| Item | Title (top pair) | Identifier (top pair) | Proposed cells 1–4 |
|---|---|---|---|
| `deliver-coil` | Deliver | coil number | Coil number · Quantity in house · Coil description · Average coil weight |
| `deliver-flatstock` | Deliver | part number | Part · Quantity · Work center · Destination |
| `deliver-component` | Deliver | component | Component · Part description · Quantity · Requesting work center |
| `deliver-die` | Deliver | die number | Die · Pickup location · Quantity · *(Requested by)* |
| `deliver-dunnage` | Deliver | dunnage part | Dunnage part · Description · Home location · Quantity on hand |
| `deliver-wrong-coil` | Deliver | coil number | Coil number · Reason · Correct coil · *(Requested by)* |
| `deliver-wrong-flatstock` | Deliver | part number | Part · Reason · Correct flatstock · *(Requested by)* |
| `deliver-riser-table` | Deliver | part number | Requesting work center · *(Requested by)* · *(Press Location)* · *(empty)* |
| `deliver-hopper` | Deliver | part number | Requesting work center · *(Requested by)* · *(Press Location)* · *(empty)* |

### Assist (3 Items)

| Item | Title (top pair) | Identifier (top pair) | Proposed cells 1–4 |
|---|---|---|---|
| `assist-coil-turn` | Assist | coil number | Coil number · Quantity in house · *(Requested by)* · *(Press Location)* |
| `assist-table-place` | Assist | part number | Part · Quantity · Work center · Destination |
| `assist-table-remove` | Assist | part number | Part · Quantity · Work center · Destination |

### Other (1 Item)

| Item | Title (top pair) | Identifier (top pair) | Proposed cells 1–4 |
|---|---|---|---|
| `other` | Other | request description | Request description · Requested work center · *(Requested by)* · *(Press Location)* |

*Italic parentheses mark a fallback cell, not an Item-declared field.*

**Consequences of the `source` of each declared field** (from the seed) that the spec must respect:

* `source: job` — the value must be read from the requesting job's snapshot. A job that cannot be read leaves the
  cell out rather than inventing a value.
* `source: answer` — the value is what the requester captured, and is the only field marked required for its Item.
* `source: fixed` — a value computed by the application (typically the requesting work centre).
* The existing rule that an unresolvable value **says why** rather than rendering blank (spec 004 FR-026) applies
  to these cells too.

---

## 6. What this reverses, and therefore must be stated in the spec

| Earlier decision | Where | What this change does to it |
|---|---|---|
| "The detail-field grid is **not** on the card" | `specs/004-unified-card-item-picker/data-model.md` §7 | **Reversed.** Four Item-declared fields return to the card. |
| "Declared fields appear on the request page and never on the card" | spec 004 **FR-007** | **Reversed** for the four cells this layout shows. |
| "The card's four metadata rows are requested by · press · remaining time · waiting" | `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` | **Superseded** by the new zone anatomy. |
| "The card body is the click target that opens the request" | spec 004 / 009 A3, and the closed defect `Closed-Medium-Waitlist-CardAndFlyoutPictureTookTheCardsClick.md` | **Reversed.** A dedicated button opens the request; the body no longer does. The picture's Shift+click enlarge survives. |

The spec must call these out explicitly. A silent contradiction of a ratified contract is not reviewable, and
these are all cargo-cult risks the moment a later reader finds the old text.

---

## 7. Invariants the change must not break

1. **Overdue stays visible under every sort.** Spec 004 FR-012 requires an overdue row to remain visibly marked
   whatever the sort. D10 removes the "Remaining time" text that currently carries the bold-red marking — the spec
   must name the replacement (ring colour and/or the status line) before this ships.
2. **Nothing is invented.** Every value in every cell must come from the request, the job it names, the Item's
   configuration, or a value the application computed. A cell with no source is omitted, never blanked, never
   guessed.
3. **One card shape per Item, not per request.** The card may not vary its *layout* by Item — only the *contents*
   of the four cells and the values in the top pair vary. This is spec 004 FR-006 and it survives.
4. **Action availability is unchanged.** A button's visibility still comes from the row's own gate; no action
   becomes available or unavailable because the layout changed.
5. **Localization.** Every new label (`Status:`, `Elapsed:`, `Location`, `Press Location`, `Open details`, the
   Print tooltip, the ring's accessible name) is a resource key in `Strings/en-us/Resources.resw`, never a literal.
6. **Accessibility.** The ring must expose a text equivalent (percentage and what it measures) to screen readers,
   since the number is a graphic. Every button keeps a name and a tooltip. Contrast for the inline status text
   must meet the same bar as the pill it replaces.
7. **The 15-minute default stays labelled as a default.** When an Item carries no configured allotment, the ring
   computes from the default — and the card must not present a default as though it were configured (spec 004 §5).

---

## 8. Artifacts the implementation will touch (for the plan, not the spec)

* `Module_Waitlist/Controls/WaitlistLineCardView.xaml` and `.xaml.cs` — the card itself, plus the Print `TODO`.
* `Module_Waitlist/Views/WaitlistViewPage.xaml` / `WaitlistViewPage.xaml.cs` — drop the card-body `ItemClick`
  navigation now that the Details button owns it.
* `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` — the ring's percentage and allotted-minutes inputs; the
  card-visible detail cells.
* `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` — resolve the Item's declared fields for each
  row (today only the generic `Request details` / `Work order` / `Work center` / `Requested by` / `Request ID`
  rows are attached, and the declared fields are resolved on the detail page instead).
* `MTM_Waitlist.Waitlist.View/Helpers/` and `Converters/` — a percentage-to-ring converter, if one is not already
  present.
* `Strings/en-us/Resources.resw` — the new labels.
* `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` — currently asserts the labels
  `Requested by`, `Press`, `Remaining time`, `Waiting`. It will need rewriting to the new anatomy.
* `MTM_Waitlist.Tests/Module_Waitlist/Models/SampleOrderTests.cs` and
  `.../ViewModels/WaitlistViewViewModelActionTests.cs` — the field/percentage assertions.
* `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` — superseded; the new spec's contract
  should be the live one.

**Note for whoever writes the spec**: the reference image is the only description of the intended visual result.
The spec must therefore be written so that the *arrangement* it requires is verifiable from the image — zone
order, which value sits where, and which cells are conditional — rather than by describing pixel sizes.

---

## 9. Suggested acceptance criteria for the spec

* A row can be read at a glance without opening it: what is wanted, what it is for, who asked, how long it has
  waited, and how much of its window has gone.
* Every card of the same Item presents its cells in the same places; two different Items never swap cell meanings.
* An overdue request is still unmistakable after the change, under every sort.
* No card shows a value the request does not carry.
* Printing is visibly not available, and says so when pointed at, rather than appearing broken or appearing to work.
* Opening a request is a deliberate, separate action from clicking anywhere on its row.

---

## 10. Out of scope

* The waitlist **detail page** layout (D1).
* Any change to the request **lifecycle** or status vocabulary.
* Any **new** real action beyond Open details. Print is a disabled placeholder (D4).
* The **New Request** wizard, Setup, Settings, and the Mock/visual-fallback surfaces.
* Any **database** change — this is a presentation change over data the store already holds. (If O2 resolves to a
  value the store does not hold, that becomes a separate decision and a separate spec.)
