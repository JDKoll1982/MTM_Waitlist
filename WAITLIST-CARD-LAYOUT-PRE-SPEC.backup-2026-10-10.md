# Pre-Spec — Waitlist Line Card Layout (image-driven redesign)

**Status**: Pre-specification — decisions captured, spec not yet written.
**Created**: 2026-10-09
**Revised**: 2026-10-10 — the requester replaced the reference image twice; the layout description, D16–D18 and
the open questions were updated to match the current one. Per-Item cell proposals in §5 are still awaiting
sign-off.
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

The target is the layout in the supplied reference image (`NewWaitlistCardLayout.jpeg`, replaced by the requester
on **2026-10-10**):

```
+-------------------------------------------------------------------------------------------------+
|#| +---------+ |       Waiting       |                  Pickup                  |                |
|#| |         | |                     |                MMC0001000                |+----+----+----+|
|#| | picture | |       ,-----.       |------------------------------------------|| OK | X  | || ||
|#| |         | |      ( 01:02 )      |    Data Cell 1     |     Data Cell 2     |+----+----+----+|
|#| |         | |       `-----'       |     Context 1      |      Context 2      |                |
|#| +---------+ |                     |------------------------------------------|                |
|#|             |                     |    Data Cell 3     |     Data Cell 4     |+----+----+----+|
|#|   100-03    |      John Koll      |     Context 3      |      Context 4      || [] | Q  | pr ||
|#|             |                     |------------------------------------------|+----+----+----+|
+-------------------------------------------------------------------------------------------------+
```

Read left to right the card has **four zones**, separated by vertical rules. In the sketch `#` is the accent bar,
and the picture shown is the placeholder state:

1. **Identity zone** — the material picture with its new-message badge, unchanged in meaning.
2. **Status zone** — a **centred** stack of **four** rows (**D25**): the status word, the ring — which fills as the
   allotted time is used and carries the **time left** inside it — that time left again as `hh:mm`, and the
   requester. The earlier revision's `Status:` prefix and `Elapsed:` line are both gone.
3. **Identity-and-detail zone** — the **centred** large title, the request identifier centred beneath it, and then
   a bordered panel of label/value cells split by internal rules.
4. **Action zone** — a 2×3 grid of **square** icon buttons, standing off from the detail zone behind its own
   vertical rule.

### What the replacement reference image (2026-10-10) shows, and where it disagrees with this document

The requester replaced the reference image, so these are recorded from it directly rather than inferred:

| Observation | Status |
| --- | --- |
| The detail panel is **four cells in a 2×2 block**. | Matches D13's "four cells" and **resolves O3**. |
| Each panel cell is a **bold label with its value on the line beneath**, not a label and value side by side. | Adopt — new. |
| The four cells are drawn as neutral placeholders (`Data Cell 1`–`Data Cell 4` over `Context 1`–`Context 4`). | The image is a layout mock; what each cell *contains* is the per-Item decision §5 is collecting now. |
| The earlier revision's full-width `Press Location:` row is **gone**. A large **unlabelled value** (`100-03`) now sits under the picture instead. | New — what that value is, and whether it is labelled anywhere, is **O9**. |
| The status zone is a **centred** stack of `Status: <value>`, ring, `Elapsed: <value>`. | Adopt in shape — see D17, refined to **three** rows by **D25**. |
| The ring carries a **percentage** (`90%`) and a separate `Elapsed: 2h 58m` line sits beneath it. | **Superseded by D25** — the ring carries the **time left** inside it, the `hh:mm` row beneath was **removed**, and the person row takes the elapsed line's place. |
| The ring is **blue** at 90 % elapsed. | **Resolved by D29** — blue is the normal status-column theme, so §3's green → amber → red scale is withdrawn. Under D25 the `90%` figure is replaced by the **time left**. |
| The status **value** is drawn in dark red while the `Status:` label stays neutral. | Becomes **D29**: the whole status column re-themes when overdue. D25 already drops the `Status:` prefix, so the colour is carried by the word itself. |
| The picture is the grey **placeholder** and carries **no** new-message badge. | Expected state for a request with no picture and no new messages; D7 is unchanged. |
| The six action buttons are **square**, in two rows of three, with wider gaps than the current pills. | Adopt — see D18. Minimum 44×44 hit target still governs. |
| No `Location` cell appears anywhere in this revision. | **O8 is retired** — the earlier revision's `Location` cell is not part of this layout, so it no longer contradicts D16, which stands. |

---

## 2. Decisions locked with the requester

| # | Decision | Value |
| --- | --- | --- |
| D1 | Surface | The waitlist list rows only. The detail page is **not** changed by this work. |
| D2 | Action buttons | Only actions the application can really perform are wired. No new business actions. |
| D3 | Action grid shape | Exactly six slots in a 2×3 grid — see §4. |
| D4 | Print | Ships **disabled**, with a "not available yet" tooltip and a `TODO` in the code-behind naming label printing as the future action. It is a placeholder, not a working feature. |
| D5 | Card click | The card body **stops** opening the request. A dedicated button opens it. |
| D6 | Open-details button | Magnifier glyph, tooltip "Open details". |
| D7 | Picture behaviour | Shift+click enlarge and the new-message badge both stay. |
| D8 | Ring value | ~~Percentage of **allotted time used** = `elapsed ÷ allotted minutes`.~~ **SUPERSEDED 2026-10-10 by D25** — the ring fills as time is used, but carries the time **left** as `hh:mm` inside it, not a percentage. |
| D9 | Elapsed | ~~The `Elapsed:` line is today's **Waiting** value (requested → now).~~ **SUPERSEDED 2026-10-10 by D25** — there is no `Elapsed:` line; row 3 is the time **left**. |
| D10 | Remaining time | ~~No longer a text row. It is conveyed by the ring.~~ **SUPERSEDED 2026-10-10 by D25** — remaining time **is** a text row again (row 3, `hh:mm`), in addition to being inside the ring. |
| D11 | Status presentation | The coloured status **pill is replaced** by plain status text, coloured per state. ~~Styled as inline `Status: <value>` text~~ — **the `Status:` prefix was dropped by D25**, so the word stands alone on the column's first row. |
| D12 | Title and identifier | Top pair of the detail zone: the request's **Category** word, and the request's own identifier (part number / dunnage / die / request description). |
| D13 | Detail cells | The four cells beneath the top pair are **determined by the request's Item**, not hand-chosen per row. Proposals in §5 are **for review**. |
| D14 | Sizing | A full-width row that stretches; tuned to stay readable at the app's default window size. |
| D15 | Pre-spec | This document. It is the input to a new `/speckit.specify` run — no code is written yet. |
| D16 | `Die Location` cell | **Dies only.** The cell the reference image labels `Location` is shown **only** on a Die request (`pickup-die`, `deliver-die`), and is labelled **`Die Location`**, not `Location`. No other Item ever shows it. Stated by the requester 2026-10-10. |
| D17 | Status zone shape | A centred stack. ~~`Status: <value>`, then the ring with its percentage, then `Elapsed: <value>`~~ — **REFINED 2026-10-10 by D25** to three rows: the status word, the ring carrying the time left inside it, and the person. |
| D18 | Action buttons | **Square**, six slots in the same 2×3 grid, same colours and glyphs, same gating. Shape changed by the replacement reference image; the ≥44×44 hit target is unchanged. |
| D19 | Panel never shows | The **requesting work centre** and the **requesting user** are **never** detail cells. The requesting work centre appears as the large value in the left (identity) column; the requesting user appears only on the request's own detail page. Stated by the requester 2026-10-10. **Both universal fallbacks this document previously proposed — `Requested by` and `Press Location` — are withdrawn**; there are no universal fallbacks any more. |
| D20 | Panel spanning by cell count | The panel's geometry follows how many cells the Item actually fills: **4** → 2×2; **3** → cells 1–2 on the top row and **cell 3 spanning the full bottom row**; **2** → cells 1–2 on the top row with the **bottom row collapsed**; **1** → **cell 1 spanning the full width** with the bottom row collapsed. Stated by the requester 2026-10-10. A card's height therefore differs **between** Items but stays constant for any one Item, so §7.3 still holds. |
| D21 | Detail-zone grid | The card's third column is an inner grid of **3 rows × 2 columns**, which is what makes D20's spans expressible. **Row 1 is the title pair** — the Category word and the request's own identifier — and **always spans both columns**. Rows 2 and 3 hold the four detail cells, two per row. So D20's rules are row merges of this grid: three cells → row 3 merged for cell 3; two cells → row 3 removed; one cell → row 2 merged and row 3 removed; **no cells → rows 2 and 3 both removed, leaving the always-spanning title row**, which then occupies the vacated space as a single full-width row. |
| D22 | Hopper Size | Both Hopper Items (`pickup-hopper`, `deliver-hopper`) show a single panel cell, **Size**. The Hopper request itself gains a size answer: **three radio buttons — Small, Medium, Large — with a reference image of the selected hopper beneath them**. On the card, the Size cell is a **link that opens that same hopper image**, behaving like the Ctrl+click enlarge the other pictures already use. Stated by the requester 2026-10-10. **This is more than a presentation change** — see the scope note below; it adds an answer field to two Items and changes the New Request wizard, which §10 currently excludes. |
| D23 | Die shop direction | Both Die Items (`pickup-die`, `deliver-die`) gain a **checkbox in the request workflow** stating whether the die is **currently at the die shop** (the deliver case) or **should be sent to the die shop** (the pickup case). On the card it is a **third panel cell, shown only when that checkbox was ticked during the request**; unticked, the cell is drawn but **empty**. Stated by the requester 2026-10-10. Like D22 this reaches into the New Request wizard — see the scope note below. **Two details remain open:** the cell's label, and whether an unticked checkbox leaves the third row present-but-empty or collapses it (D20 says a third cell spans row 3; D23 says an unticked one is empty). |
| D24 | Fuzzy part-number entry | Where an operator must name a part they cannot pick from the job's own list — the **incorrect part** on Wrong Coil and Wrong Flatstock — entry is a **fuzzy search**, not free text. The typed value is upper-cased and, when it carries a known prefix, **zero-padded to 10 characters**: `MMC1000` → `MMC0001000`, `MMF500` → `MMF0000500`. An **exact** part match resolves immediately; otherwise a modal lists every part whose number **contains** the term, so `MMC0001` lists them all. Prefixes in this application: **`MMC`** coil, **`MMF`** flatstock, **`FGT`** die. The correct part is unaffected — it still comes from the job. This mirrors behaviour that already ships in the receiving application (`Strategy_SharedPartNumberLookup`, `Dialog_FuzzySearchPicker`, `06_FuzzySearchPartsByID.sql` reading Infor Visual `dbo.PART`), which the waitlist app does **not** have today. **Live only, and it degrades rather than blocks:** the search is a live Infor Visual read and is never cached, because a contains-search over every part is not a shape the mirror can hold. When the source is unreachable the operator is **told the search is unavailable and the value they typed is used as entered**. Stated by the requester 2026-10-10. |
| D25 | Status zone has **three** rows | The status column is a stack of three rows (**refined 2026-10-10, second pass**): **Row 1** the status word, in **friendly wording throughout** — Waiting, In Progress, Done, Cancelled (see D26 for the handler-set additions); **Row 2** the ring, which fills as the allotted time is used and carries the **time left** inside it; **Row 3** the person — the **requester** while the request is unclaimed, the **material handler** once it is claimed. The separate `hh:mm` line beneath the ring was **removed at the requester's instruction** because the graphic already carries that value. **This supersedes D8, D9 and D10:** D8 made the ring the percentage of time **used** and D9 made the third line today's `Elapsed:` value; there is now no percentage and no elapsed line. The reference image's `90%` and `Elapsed: 2h 58m` no longer describe the card. |
| D26 | Handler-set sub-statuses | Beyond the four lifecycle states, a material handler can set **additional event statuses** during a request's life: **`Coil Buried`**, **`Flatstock Buried`**, **`Searching For Part`**, and **others still to be enumerated**. Stated by the requester 2026-10-10 and marked **implement later** — the spec must accommodate them without shipping them, and this document does **not** yet hold the full list. Three consequences the spec must handle: the status vocabulary **stops being closed at four**; `RequestStatusPalette.RoleFor` must gain a role **and a colour** for each new status, because it currently returns `None` for anything outside the four and that draws **no status at all**; and something must let the handler **set** them, which is a workflow surface rather than a card change. Row 1's wording stays friendly (D25). |
| D27 | Status-zone rows collapse like the data column | The status zone's lower rows follow the **same rule as the detail panel (D20)**: a row with no value is **removed**, not left blank, so the stack is only as tall as it has content. Applied to **R4 (the requester)** and to **R3 (the material handler)** whenever a request is unclaimed and there is no handler yet. Stated by the requester 2026-10-10. |
| D28 | Done stops the clock | For a **Done** request the timer stops: **R1** `Done`; **R2** the ring **frozen at the moment of completion**, carrying the **total elapsed time** instead of the time left; **R3** the **material handler**; **R4** the **requester**. Same shape for **Cancelled**, frozen at the moment it was cancelled, with R3 the person who cancelled it. Stated by the requester 2026-10-10. Note this means row 2 means *time left* while a request is live and *time taken* once it is closed, so the control must state which one it is drawing, and its accessible name must match. |
| D29 | Overdue | Overdue **replaces row 1**: it reads **`Overdue`**, and the ring shows the **time overrun** rather than the time left (e.g. `-00:15`). In addition **the entire status column changes its colour theme** to signal the condition — not merely the word. Stated by the requester 2026-10-10. This is how the card honours spec 004 **FR-012** (an overdue row stays visibly marked under every sort) now that D25 removed the bold-red remaining-time text. It also **resolves O1**, including its ring-colour half: the ring is part of the column **theme**, not an independent green → amber → red scale. |

### Decisions still open (must be answered before or during `/speckit.clarify`)

| # | Open question | Why it blocks |
| --- | --- | --- |
| O2 | **How is each panel cell sourced, and where does `Die Location` sit?** The current image draws four neutral cells (`Data Cell 1`–`4`), so it names none of them. Confirm that every cell's value comes only from the Item's declared fields, and specifically **which of the four cells carries `Die Location` on a Die request** — D16 adds a cell that is not simply "the next declared field". | The constitution forbids inventing a value the request does not carry, and the Die cell has no declared position in the 2×2 block. |
| O5 | **Spec 004 FR-007 is reversed by D13.** That requirement says declared Item fields appear on the request page and *never* on the card. Confirm the reversal is intended, since it changes a ratified contract. | A spec that silently contradicts a shipped contract is not reviewable. |
| O6 | **`Accept` and `Complete` are currently the same slot in two states.** With a fixed 2×3 grid, one of the two is always blank. Accept the gap, or place them so the gap is predictable? | Visual stability of the grid. |
| O7 | **The Die cell's label conflicts with its configured label.** D16 requires the card cell to read **`Die Location`**, but the Item's declared field for both die Items is configured as **`Pickup location`** (seed `detail_fields_json`). Does the card override the label for this one cell, or does the configured label change to match? | The label is either a presentation override or a data change; §10 says a data change is a separate decision and possibly a separate spec. |
| O9 | **What is the large unlabelled value under the picture (`100-03`)?** The previous revision carried `Press Location: 100-03` as a labelled full-width panel row; this revision has moved the value under the picture and dropped its label. Is it the press location, the requesting work centre, something else — and should it carry a label? | A prominent value with no label cannot be understood by a first-time user, and §7.2 forbids inventing one. |

**Resolved during the 2026-10-10 revision:** **O1** — overdue **replaces** row 1 and re-themes the whole status
column (D29), so there is no separate overdue line and the ring is not an independent colour scale. **O3** — the
replacement image shows four cells in a 2×2 block, so D13's "four cells" stands and §5's method of taking the
Item's leading declared fields is unchanged. **O4** — answered by **D20**: an Item that declares fewer cells does
not have them padded with fallbacks; the panel spans or collapses instead, and **D19** withdraws both fallbacks
that O4 proposed. **O8** — retired; the replacement image shows no `Location` cell at all, so nothing contradicts
D16.

---

## 3. Field mapping — what each part of the target card is fed by

| Target element | Today's source | Change |
| --- | --- | --- |
| Accent bar | `SampleOrder` → accent brush | unchanged |
| Picture + new-message badge | `Order.EffectiveImagePath`, `Order.HasNewMessages` | unchanged |
| Large value under the picture (`100-03`) | *new placement* — previously the labelled `Press Location` panel row | **O9** — what it is, and whether it is labelled |
| Row 1 — the status word | `Order.Status` → **friendly wording** | the pill becomes a plain word (D11, D25); it reads **`Overdue`** when the row is late (D29) |
| Row 2 — the ring | *new* — needs `elapsed` and `allotted_minutes` | fills as time is **used**, and carries the **time left** inside it; once the request is closed it **freezes** and carries the **time taken** (D25, D28) |
| The column's theme | `RequestStatusPalette` + `Order.IsOverdue` | **the whole status column re-themes when overdue** (D29) |
| Row 3 — the person | `Order.RequestedBy` while unclaimed; the material handler once claimed | *new on the card*; the row is **removed** when it has no value (D25, D27) |
| Row 4 — the requester | `Order.RequestedBy` | *new on the card*, added for closed requests; **removed** when empty (D27, D28) |
| Large title | `Order.Title` (the Item's umbrella phrase) | promoted to the headline, centred (D12, D17) |
| Identifier | `Order.Subtitle` (resolved from the Item's template) | promoted, centred (D12, D17) |
| Four detail cells (2×2) | *new on the card* — the Item's declared fields | new resolution path (§5); each cell is a label over its value; contents are per-Item (**D13**, **D16**, **O4**) |
| Row 1: Accept / Cancel / Give back | `Order.AcceptCommand` / `CancelCommand` / `ReleaseCommand` | restyled into a grid |
| Row 2: Complete / Details / Print | `Order.CompleteCommand` / **new** / **new, disabled** | one new navigation action, one placeholder |
| Status pill | `Order.StatusBadgeText` | removed (D11) |
| Card-body click → open request | `ItemClick` on the list | removed; replaced by the Details button (D5/D6) |

### Ring arithmetic

```
elapsed  = now − requested_utc                 (already computed for the Waiting/Elapsed text)
allotted = the Item's allotted_minutes          (null ⇒ the labelled 15-minute default, per spec 004 §5)
arc      = clamp( elapsed ÷ allotted , 0 , 1 )  (overdue clamps to a full ring)
label    = time left  = allotted − elapsed      (negative once overdue ⇒ show the overrun, e.g. -00:15)
            when the request is closed (Done / Cancelled) the ring freezes and the label is the time taken
```

The ring must re-evaluate on the same one-minute tick that already refreshes `WaitingForText` and
`RemainingTimeText`, so the three never disagree. **The ring's colour is not an independent scale:** it is drawn by
the status column's **theme**, which switches as a whole when the row is overdue (D29). The reference image's blue
ring at 90 % is therefore consistent — blue is the normal theme, and blue at 90 % simply means the row was not
late; §3's earlier "green → amber → red" suggestion is withdrawn.

---

## 4. The action grid (2 columns × 3 rows, row-major)

| | Column 1 | Column 2 | Column 3 |
| --- | --- | --- | --- |
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
* The six buttons are **square** in the replacement reference image rather than the current tall pills (D18), and
  the whole grid stands off from the detail zone behind its own vertical rule.

---

## 5. The detail cells — settled per Item (D13, D16, D19–D24)

**Status: settled 2026-10-10 for all twenty visible Items.** The four hidden Pickup Items are deferred — see §5.1.

**The rule.** A cell is the Item's own declared field (`waitlist_request_item_configs.detail_fields_json`), in
declared order, minus anything **D19** excludes. Cells are drawn as a **bold label with its value on the line
beneath**, and laid into the 3×2 grid of **D21**; a short list merges or drops rows per **D20**. There are **no
fallbacks**: nothing is padded, and an Item that declares fewer cells simply has a shorter panel.

**Reading the tables.** `cells` is how many the panel actually fills, which drives D20's geometry. ⚠ marks a cell
that is **not** a declared field of that Item or has no source today — each one is listed in the sourcing notes
below and must be resolved before it can be built. `—` means the panel collapses entirely.

* **`Die Location` is Die-only (D16)** and is never a filler. It appears on `pickup-die` and `deliver-die` only,
  and its configured label reads `Pickup location` — see **O7**.

### Pickup (11 Items — 4 are catalogued but hidden)

| Item | Title | Identifier | Cells, in order | cells |
| --- | --- | --- | --- | --- |
| `pickup-coil` | Pickup | coil number | Coil number · Quantity in house · Coil description · Average coil weight | 4 |
| `pickup-die` | Pickup | die number | Die · **Die Location** *(+ the D23 die-shop cell when ticked)* | 2–3 |
| `pickup-component` | Pickup | component | Component · Total Qty In House ⚠ · Item Type (WIP or Finished Goods) ⚠ | 3 |
| `pickup-riser-table` | Pickup | part number | — panel collapses | 0 |
| `pickup-dunnage` | Pickup | dunnage part | Dunnage part · Home location | 2 |
| `pickup-scrap` | Pickup | scrap type | Scrap Type · Lugger Location ⚠ *(placeholder until a future patch)* | 2 |
| `pickup-hopper` | Pickup | part number | Size ⚠ *(D22 — needs a new answer)* | 1 |
| `pickup-fg` *(hidden)* | Pickup | part number | **Deferred — §5.1** | — |
| `pickup-ncm` *(hidden)* | Pickup | part number | **Deferred — §5.1** | — |
| `pickup-wip` *(hidden)* | Pickup | part number | **Deferred — §5.1** | — |
| `pickup-outside-service` *(hidden)* | Pickup | part number | **Deferred — §5.1** | — |

### Deliver (9 Items)

| Item | Title | Identifier | Cells, in order | cells |
| --- | --- | --- | --- | --- |
| `deliver-coil` | Deliver | coil number | Coil number · Quantity in house · Coil description · Average coil weight | 4 |
| `deliver-flatstock` | Deliver | part number | Part · Quantity · Destination | 3 |
| `deliver-component` | Deliver | component | Component · Total Qty In House ⚠ · Item Type (WIP or Finished Goods) ⚠ | 3 |
| `deliver-die` | Deliver | die number | Die · **Die Location** *(+ the D23 die-shop cell when ticked)* | 2–3 |
| `deliver-dunnage` | Deliver | dunnage part | Dunnage part · Home location | 2 |
| `deliver-wrong-coil` | Deliver | coil number | Coil number ⚠ *(the *incorrect* coil, typed via the D24 fuzzy search)* · Correct coil | 2 |
| `deliver-wrong-flatstock` | Deliver | part number | Part ⚠ *(the *incorrect* flatstock, typed via the D24 fuzzy search)* · Correct flatstock | 2 |
| `deliver-riser-table` | Deliver | part number | — panel collapses | 0 |
| `deliver-hopper` | Deliver | part number | Size ⚠ *(D22 — needs a new answer)* | 1 |

### Assist (3 Items)

| Item | Title | Identifier | Cells, in order | cells |
| --- | --- | --- | --- | --- |
| `assist-coil-turn` | Assist | coil number | Coil Number · Weight of Coil ⚠ *(a new **required** operator entry)* | 2 |
| `assist-table-place` | Assist | part number | — panel collapses | 0 |
| `assist-table-remove` | Assist | part number | — panel collapses | 0 |

### Other (1 Item)

| Item | Title | Identifier | Cells, in order | cells |
|---|---|---|---|---|
| `other` | Other | request description | Request description — drawn with **`AutoScrollTextView`** | 1 |

⚠ **Weight of Coil is not the same value as Pickup Coil's `Average coil weight`.** The latter is a job-derived
average; the former is the actual weight the operator must supply on an Assist Coil Turn, so a material handler can
judge whether a larger forklift is needed. They must not be conflated when the field is added.

### 5.1 The four hidden Pickup Items — audit result (2026-10-10)

`pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service` are catalogued but hidden, so no card of
theirs is drawn today. The requester asked for a full codebase review, believing the work was half done in an
earlier spec. **The review says that belief is not borne out: these four are consistently *deferred*, not partly
built.** The evidence:

* **The hide is a hard-coded list, not data.** `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs:24-31`
  holds `s_outOfScopeItemIds = ["pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service"]`, and
  `IsVisible` returns `false` for them at lines 95-98. There is no `is_in_scope` column; turning them on needs
  **code plus real job gates**, not a data change.
* **They are deferred in writing.** `specs/004-unified-card-item-picker/spec.md:482` — *"The four Items with no
  value source are out of scope and are hidden until their own work lands"*; `data-model.md:55-56` — *"Out of
  scope, catalogued but never offered … excluded by the visibility rules, not by deletion (FR-028)"*. T029 and
  SC-014 record the same thing. **No spec describes an implementation of them.**
* **What does exist** is catalogue rows, their display names in `Strings/en-us/Resources.resw:852-862`, the hide
  rule and the test that pins it (`RequestItemPickerRulesTests.cs:110-143`), seed rows, and demo requests. There
  is **no** view, view model, wizard step, stored procedure or contract constant dedicated to any of the four.
* **A real, unconnected stack exists for their subject matter** — `RequestDisposition`, `RequestDispositionResolver`,
  `WipFloorInventoryService` and friends, holding the FG / NCM / WIP / outside-service floor quantities. It is
  registered at `ServiceRegistrationExtensions.cs:127` and **has no consumer outside its own tests**. That is the
  nearest thing to "half done", and it is not wired to the picker at all.

**Their declared fields** (all four are `direct-to-confirmation` with no answer and no options; `pickup-fg`,
`pickup-ncm` and `pickup-outside-service` also carry `allotted_minutes = NULL`):

| Item | Declared fields, in order (source) |
|---|---|
| `pickup-fg` | Part number (job) · Quantity remaining (job) · Part description (job) · Customer (job) · Packlist (job) |
| `pickup-ncm` | Part (job) · Quantity to move (job) · Pickup location (job) · Destination (**fixed**) · Traceability ID (job) |
| `pickup-wip` | Part and quantity (job) · Pickup work center (**fixed**) · WIP destination (job) · Operation sequence (job) |
| `pickup-outside-service` | Part (job) · Quantity to move (job) · Pickup location (job) · Destination (**fixed**) · Traceability ID (job) |

**Do not reach for the old proposals for these four.** Their cells are undecided, and §5.2 shows that almost
nothing they declare can be resolved today anyway.

### 5.2 The job snapshot does not carry most of the cells this document has just settled

This is the **largest gap in the whole change**, and it is not about the four hidden Items — it applies to the
twenty visible ones. Two different problems are hiding behind "the card shows the Item's declared fields":

**(A) The resolver is missing labels the snapshot already holds.** `RequestJobFieldValues.Resolve`
(`MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs:36-57`) maps exactly four labels — `die`,
`pickup location`, and `part number` / `job part number` / `part` — and returns `null` for **everything else**
(the `_ => null` arm at line 57). But the job snapshot `RequestJobPartAvailability` already carries
`CoilPartNumber`, `FlatstockPartNumber`, `ScrapType`, `ComponentPartNumbers` and `DunnageParts`. So values exist
and simply cannot be reached by the label the configuration declares.

**(B) The snapshot does not hold the value at all.** `RequestJobPartAvailability` has no quantity, no
description, no weight, no destination and no home location. `RequestDunnagePart` carries `PartNumber`,
`DisplayName` and `ImagePath` but **no location**, and `RequestDiePart` is the only part model with a `Location`.

Taking the cells settled in §5, here is what can actually be drawn today:

| Cell | Snapshot has it? | Resolver maps the label? | Therefore |
|---|---|---|---|
| `Die`, `Die Location` | yes | yes | **resolves** |
| `Part` / `Part number` | yes | yes | **resolves** |
| `Component`, `Dunnage part`, `Request description` | n/a — captured answers | n/a | **resolves** |
| `Coil number`, `Coil Number`, `Correct coil` | yes | **no** | resolver gap |
| `Correct flatstock` | yes | **no** | resolver gap |
| `Scrap Type` | yes | **no** | resolver gap |
| `Quantity in house`, `Quantity`, `Total Qty In House` | **no** | n/a | data gap |
| `Coil description`, `Average coil weight`, `Item Type`, `Weight of Coil` | **no** | n/a | data gap |
| `Home location` | **no** | n/a | data gap |
| `Destination` | yes (as a fixed value) | **no fixed resolver** | gap |
| `Lugger Location`, `Size`, the die-shop cell | **no** | n/a | new answers (D22–D24) |

**So most of the settled cells cannot be filled from the job snapshot as it stands.** Every one needs a decision
the spec must carry: extend the resolver for the labels the snapshot already holds, add the missing values to the
snapshot (or read them from Infor Visual), or drop the cell. §7.2 forbids inventing any of it. **This is the first
thing the spec must address, ahead of the four hidden Items.**

**Consequences of the `source` of each declared field** (from the seed) that the spec must respect:

* `source: job` — the value must be read from the requesting job's snapshot. A job that cannot be read leaves the
  cell out rather than inventing a value.
* `source: answer` — the value is what the requester captured, and is the only field marked required for its Item.
* `source: fixed` — a value computed by the application (typically the requesting work centre).
* The existing rule that an unresolvable value **says why** rather than rendering blank (spec 004 FR-026) applies
  to these cells too.

**⚠ Complete list of cells that need a source or a decision before they can be built (2026-10-10).** Every one of
these is either not a declared field of its Item, or has no source to read from. Each needs a stated source — the
job snapshot, a value the application computes, or a configuration change — and §7.2 forbids inventing it, while
§10 makes a configuration change its own decision.

| # | Cell | Items | The gap |
| --- | --- | --- | --- |
| 1 | `Total Qty In House` | `pickup-component`, `deliver-component` | Not declared. Both Items declare `Quantity`, which may or may not be the same number — confirm. |
| 2 | `Item Type (WIP or Finished Goods)` | `pickup-component`, `deliver-component` | Not declared. Needs a derivation rule, not a guess. |
| 3 | `Lugger Location` | `pickup-scrap` | The Item declares `Scrap lugger`, a different field. Ships as a **placeholder** pending a future patch. |
| 4 | `Size` | `pickup-hopper`, `deliver-hopper` | **D22** — needs a new answer field, three radios and per-size hopper images. |
| 5 | the die-shop cell | `pickup-die`, `deliver-die` | **D23** — needs a new checkbox answer and a label; also unresolved whether unticked leaves the row empty or collapses it. |
| 6 | the *incorrect* `Coil number` / `Part` | `deliver-wrong-coil`, `deliver-wrong-flatstock` | Currently `source: job`. **D24** moves it to operator entry through a fuzzy picker, which changes its source to `answer`. |
| 7 | `Weight of Coil` | `assist-coil-turn` | A new **required** operator entry. Do not confuse it with Pickup Coil's job-derived `Average coil weight`. |
| 8 | `Request description` | `other` | Declared and readable, but the card must draw it with `AutoScrollTextView`, not a plain `TextBlock`. |

**Declared fields the cards must no longer show.** These exist in configuration but are excluded by D19 or have no
source, so the card change and the configuration now disagree. Decide whether the field is withdrawn or kept only
for the request page:

* `Requesting work center` / `Pickup work center` / `Work center` — declared by 11 Items, excluded from the panel
  by **D19** because the work centre is the left-column value.
* `Description` — declared by `pickup-dunnage` and `deliver-dunnage`, and **verified unfillable** on 2026-10-10:
  no dunnage table holds one (`dunnage_parts` carries `part_id`, `type_id`, `spec_values`, `home_location`;
  `dunnage_types` carries only `type_name` and `icon`). This is **broken configuration**, not a missing source.
* `Quantity` on the die Items, `Reason` on both wrong-part Items — declared but dropped by the requester's choices.
* `Destination` on `deliver-flatstock` — kept, but flagged: `research.md` records that `destination` left the
  token set on 2026-09-20, so its availability needs confirming.

**The dunnage case is the reverse of the others.** There the field *is* declared but nothing can fill it — see
entry `Description` in the list above. Verified against `mtm_receiving_application` on 2026-10-10: every dunnage
table was checked and none holds a description, so this is **broken configuration** rather than a source the spec
can go and find. The spec must say what the card does with it, and the field should be withdrawn or given a real
source.

**Conflict with spec 004 FR-026 to resolve.** That rule requires an unresolvable value to **say why** rather than
render blank. D19/D20 require an absent cell to be **collapsed or blank**. The spec must separate the two cases
explicitly: a cell whose *source exists but whose value cannot be read* (FR-026's case, which still says why) from
one the Item *never declares at all* (D19/D20's case, which collapses).

---

## 6. What this reverses, and therefore must be stated in the spec

| Earlier decision | Where | What this change does to it |
| --- | --- | --- |
| "The detail-field grid is **not** on the card" | `specs/004-unified-card-item-picker/data-model.md` §7 | **Reversed.** Four Item-declared fields return to the card. |
| "Declared fields appear on the request page and never on the card" | spec 004 **FR-007** | **Reversed** for the four cells this layout shows. |
| "The card's four metadata rows are requested by · press · remaining time · waiting" | `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` | **Superseded** by the new zone anatomy. |
| "The card body is the click target that opens the request" | spec 004 / 009 A3, and the closed defect `Closed-Medium-Waitlist-CardAndFlyoutPictureTookTheCardsClick.md` | **Reversed.** A dedicated button opens the request; the body no longer does. The picture's Shift+click enlarge survives. |

The spec must call these out explicitly. A silent contradiction of a ratified contract is not reviewable, and
these are all cargo-cult risks the moment a later reader finds the old text.

---

## 7. Invariants the change must not break

1. **Overdue stays visible under every sort.** Spec 004 FR-012 requires an overdue row to remain visibly marked
   whatever the sort. D25 removed the bold-red remaining-time text that used to carry it, and **D29 replaces it**:
   row 1 reads `Overdue` and **the whole status column re-themes**. That is the named replacement the spec must
   state, and the ring must not be the only carrier — the theme covers the column, so the signal survives whatever
   the sort does to the row's position.
2. **Nothing is invented.** Every value in every cell must come from the request, the job it names, the Item's
   configuration, or a value the application computed. A cell with no source is omitted, never blanked, never
   guessed.
3. **One card shape per Item, not per request.** The card may not vary its *layout* for **two requests of the same
   Item** — only the values differ. This is spec 004 FR-006 and it survives. **D20 refines it:** the panel's
   geometry varies with how many cells the Item fills (4 → 2×2, 3 → third spans, 2 or 1 → bottom row collapsed),
   which is a property of the **Item**, not of the request, so two cards of the same Item are always identical in
   shape. Cards of different Items may therefore differ in height.
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

Added by the 2026-10-10 question round:

* `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs` — **the resolver, and the single most important file in
  this change.** Today it maps four labels and returns `null` for every other one, so most of §5's cells resolve to
  nothing (§5.2). Extending it, or replacing it with one that can reach the snapshot's real values, is prerequisite
  work.
* `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` (+ `RequestDunnagePart`, `RequestDiePart`) — the
  snapshot the resolver reads. Anything §5.2 lists as a **data gap** has to be added here, or read from Infor
  Visual, or dropped.
* `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` — gains a role **and a colour** for each
  handler-set sub-status (D26), and a **column theme** for the overdue state (D29). It currently returns `None`
  for anything outside the four lifecycle words, which draws no status at all.
* `Module_Shared/Controls/AutoScrollTextView.xaml` — reused for the `other` card's description cell.
* A **new WinUI fuzzy part picker** and the live Infor Visual read behind it (D24), plus the New Request wizard
  steps for hopper Size (D22), the die-shop checkbox (D23) and Weight of Coil.
* `Module_Waitlist/Views/WaitlistViewPage.xaml` and the New Request wizard pages — for the scope exceptions in
  §10; decide whether they belong to this spec or their own.

**Note for whoever writes the spec**: the reference image is the only description of the intended visual result,
and **it is now out of date in three places** — the ring's contents (D25), the status column's rows (D25/D28), and
the detail panel's earlier `Press Location` row. The spec must therefore be written so the *arrangement* it
requires is verifiable — zone order, which value sits where, which cells are conditional — from **this document's
decisions plus the current image**, not from pixel sizes and not from the image alone.

---

## 9. Suggested acceptance criteria for the spec

* A row can be read at a glance without opening it: what is wanted, what it is for, who asked, how long it has
  waited, and how much of its window has gone.
* Every card of the same Item presents its cells in the same places; two different Items never swap cell meanings.
* A `Die Location` cell appears on Die requests and on no other request (D16).
* The status column reads correctly at every state: the status word on row 1, the ring carrying the time left (or
  the time taken once closed), and the person row naming the requester while unclaimed and the material handler
  once claimed — rows disappearing rather than showing blanks (D25–D28).
* An overdue request is unmistakable after the change, under every sort.
* No card shows a value the request does not carry.
* Printing is visibly not available, and says so when pointed at, rather than appearing broken or appearing to work.
* Opening a request is a deliberate, separate action from clicking anywhere on its row.

---

## 10. Out of scope

* The waitlist **detail page** layout (D1).
* Any change to the request **lifecycle** or status vocabulary.
* Any **new** real action beyond Open details. Print is a disabled placeholder (D4).
* The **New Request** wizard, Setup, Settings, and the Mock/visual-fallback surfaces.

  **Exception opened 2026-10-10 — Hopper size (D22).** The requester asked for the Hopper Items to show a `Size`
  cell fed by a new answer captured in the New Request wizard (three radio buttons plus a reference image of the
  selected hopper), with the card's Size cell linking to that image. That **reaches into the wizard this section
  otherwise excludes**, and it needs two things this document cannot supply: a **source for the per-size hopper
  images**, and a **new answer field** on `pickup-hopper` and `deliver-hopper` — neither of which declares a size
  today (both declare only the requesting work centre). Decide whether that work joins this spec or becomes its
  own; the card change alone can ship against a placeholder, as the Scrap lugger cell does.

  **Exception opened 2026-10-10 — Die shop direction (D23).** The same shape of problem: the die Items gain a
  **checkbox** in the wizard ("is the die at the die shop, or should it be sent there?") feeding a third,
  conditional panel cell. That is another **answer field on two Items plus a wizard step**, and it also needs a
  label for the cell and a decision on whether an unticked checkbox leaves the row present-but-empty or collapses
  it. Group this with D22 when deciding what joins this spec.

  **Exception opened 2026-10-10 — fuzzy part-number entry (D24).** Larger than the other two, because the
  capability does not exist in this application at all. It needs a **WinUI picker control** (the receiving app's
  equivalent is WinForms, so nothing can be reused verbatim), **prefix-and-pad normalisation** for `MMC` / `MMF` /
  `FGT`, a **new live Infor Visual read** behind the existing stored-procedure seam — the natural home being
  another query under `Database/InforVisual/Queues/Module_Startup/Queries/` alongside `GetEmployeeByNumber.sql` —
  and a **wizard step** that captures the incorrect part. It is deliberately **outside** the mirror/fallback
  model, and its unreachable behaviour is to say so and accept what was typed (D24), so it must not be given a
  cached read shape by mistake.
* Any **database** change — this is a presentation change over data the store already holds. (If O2 resolves to a
  value the store does not hold, that becomes a separate decision and a separate spec.)
