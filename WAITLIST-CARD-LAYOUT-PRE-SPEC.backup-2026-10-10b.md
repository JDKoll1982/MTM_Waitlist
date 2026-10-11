# Pre-Spec — Waitlist Line Card Layout (image-driven redesign)

**Status**: Pre-specification — all decisions settled, spec not yet written.
**Created**: 2026-10-09
**Last revised**: 2026-10-10 (question round complete).
**Surface**: the waitlist list rows — `Module_Waitlist/Controls/WaitlistLineCardView.xaml` — **only**.
**History**: the superseded decisions, the retired open questions and the earlier layout revisions are preserved in
`WAITLIST-CARD-LAYOUT-PRE-SPEC.backup-2026-10-10.md`. **This file contains current decisions only.** Decision
numbers are kept from the original so any reference made against the backup still resolves; the gaps in the
sequence (no D8, D9, D10 or D17) are decisions that were superseded and deliberately removed.

**How to use**: this is the seeding input for one `/speckit.specify` run. Decisions and per-Item cell assignments
are settled and must not be re-litigated; the requirement material is deliberately WHAT/WHY so the spec stays
behavioural.

---

## 1. What is being asked

The waitlist list row is currently a 96×96 picture, a four-row label/value panel and up to four icon buttons with a
status pill beneath. The target is the layout in the current reference image:

```
+-------------------------------------------------------------------------------------------------+
|#|             |                     |                                          |                |
|#| +---------+ |       Waiting       |                  Pickup                  |                |
|#| |         | |                     |                MMC0001000                |+----+----+----+|
|#| | picture | |       ,-----.       |------------------------------------------|| OK | X  | || ||
|#| |         | |      ( 01:02 )      |    Data Cell 1     |     Data Cell 2     |+----+----+----+|
|#| |         | |       `-----'       |     Context 1      |      Context 2      |                |
|#| +---------+ |                     |------------------------------------------|                |
|#|             |                     |    Data Cell 3     |     Data Cell 4     |+----+----+----+|
|#|   100-03    |      John Koll      |     Context 3      |      Context 4      || [] | Q  | pr ||
|#|             |                     |------------------------------------------|+----+----+----+|
|#|             |                     |                                          |                |
|#|             |                     |                                          |                |
|#|             |                     |                                          |                |
+-------------------------------------------------------------------------------------------------+
```

`#` is the accent bar; the picture shown is the placeholder state. The sketch is schematic, not to scale.

Read left to right the card has **four zones**, separated by vertical rules:

1. **Identity zone** — the material picture with its new-message badge, unchanged in meaning, and beneath it the
   requesting work centre as a large unlabelled value (`100-03`).
2. **Status zone** — a centred stack of exactly the rows below (D25).
3. **Identity-and-detail zone** — an inner grid of **3 rows × 2 columns** (D21): row 1 the always-spanning title
   pair, rows 2–3 the detail cells.
4. **Action zone** — up to six icon buttons in a 2×3 grid, standing off from the detail zone behind a vertical rule,
   removing unavailable buttons and **re-centring** what remains (D18, D30).

### The status zone's rows (D25, D27, D28, D29)

| Row | Live request (Waiting / In Progress) | Closed request (Done / Cancelled) |
|---|---|---|
| **1** | the status word, in friendly wording | the status word, in friendly wording |
| **2** | the ring, filling as the allotted time is used, carrying the **time left** inside it | the ring **frozen**, carrying the **time taken** |
| **3** | the **requester** while unclaimed; the **material handler** once claimed | the **material handler** |
| **4** | — | the **requester** |

A row with no value is **removed**, not blanked (D27). When the request is **overdue**, row 1 reads `Overdue`, the
ring shows the **time overrun** (`-00:15`), and **the whole status column changes its colour theme** (D29).

---

## 2. Decisions settled with the requester

| # | Decision | Value |
|---|---|---|
| D1 | Surface | The waitlist list rows only. The detail page is **not** changed by this work. |
| D2 | Action buttons | Only actions the application can really perform are wired. No new business actions. |
| D3 | Action grid shape | Up to six slots in a 2×3 grid — see §4. |
| D4 | Print | Ships **disabled**, with a "not available yet" tooltip and a `TODO` in the code-behind naming label printing as the future action. A placeholder, not a working feature. |
| D5 | Card click | The card body **stops** opening the request. A dedicated button opens it. |
| D6 | Open-details button | Magnifier glyph, tooltip "Open details". |
| D7 | Picture behaviour | Shift+click enlarge and the new-message badge both stay. |
| D11 | Status presentation | The coloured status **pill is replaced** by plain status text, coloured per state. The word stands alone — there is no `Status:` prefix. |
| D12 | Title and identifier | The detail grid's always-spanning top row: the request's **Category** word and the request's own identifier (part number / dunnage / die / request description). |
| D13 | Detail cells | The cells beneath the top row are **determined by the request's Item**, not hand-chosen per row. Assignments are in §5. |
| D14 | Sizing | A full-width row that stretches; tuned to stay readable at the app's default window size. |
| D15 | Pre-spec | This document. It is the input to a new `/speckit.specify` run — no code is written yet. |
| D16 | `Die Location` cell | **Dies only.** Shown on `pickup-die` and `deliver-die` and on no other Item, and labelled `Die Location`. |
| D18 | Action buttons | **Square**, same colours, glyphs and gating as today. The ≥44×44 hit target is unchanged. |
| D19 | The panel never shows | The **requesting work centre** and the **requesting user** are **never** detail cells. The work centre is the large left-column value; the requesting user appears only on the request's own detail page. There are **no universal fallbacks** — nothing is padded to fill a short panel. |
| D20 | Panel spanning by cell count | The panel's geometry follows how many cells the Item fills: **4** → 2×2; **3** → cells 1–2 on the top row with **cell 3 spanning the full bottom row**; **2** → cells 1–2 with the **bottom row collapsed**; **1** → **cell 1 spanning the full width**; **0** → both lower rows removed. A card's height differs **between** Items but is constant for any one Item. |
| D21 | Detail-zone grid | The card's third column is an inner grid of **3 rows × 2 columns**. **Row 1 is the title pair** and **always spans both columns**; rows 2 and 3 hold the detail cells. D20's rules are row merges of this grid. With no cells, the title row occupies the vacated space as a single full-width row. |
| D22 | Hopper Size | Both Hopper Items show a single `Size` cell. The Hopper request gains a size answer: **three radio buttons — Small, Medium, Large — with a reference image of the selected hopper beneath them**. On the card the Size cell is a **link that opens that image**, like the Ctrl+click enlarge the other pictures use. |
| D23 | Die shop direction | Both Die Items gain a **checkbox in the request workflow** stating whether the die is **currently at the die shop** (deliver) or **should be sent there** (pickup). On the card it is a **third cell, shown only when that checkbox was ticked** — see **D40**. |
| D24 | Fuzzy part-number entry | The **incorrect part** on Wrong Coil and Wrong Flatstock is **typed through a fuzzy search**, not free text. Input is upper-cased and, with a known prefix, **zero-padded to 10 characters** (`MMC1000` → `MMC0001000`, `MMF500` → `MMF0000500`). An **exact** match resolves at once; otherwise a modal lists every part whose number **contains** the term. Prefixes: **`MMC`** coil, **`MMF`** flatstock, **`FGT`** die. The correct part still comes from the job. **Live only, and it degrades rather than blocks:** the search is a live Infor Visual read and is never cached, because a contains-search over every part is not a mirrorable shape. Unreachable ⇒ the operator is **told the search is unavailable and the value they typed is used as entered**. |
| D25 | Status zone has three rows | Row 1 the status word in **friendly wording throughout** (Waiting, In Progress, Done, Cancelled); row 2 the ring, filling as time is used and carrying the **time left** inside it; row 3 the person — **requester** while unclaimed, **material handler** once claimed. There is **no percentage and no `Elapsed:` line**. |
| D26 | Handler-set sub-statuses | A material handler can set **four** additional event statuses, and the list is **closed**: **`Product Buried`**, **`Cant Find Product`**, **`Changing Forklift`**, **`Changing Propane`**. **The three named in an earlier round — `Coil Buried`, `Flatstock Buried` and `Searching For Part` — are REMOVED and must not be built.** They are set by a **material-handler pull from the database**, through the flyout of **D42**. **The statuses themselves belong to a future spec** (see §10), but the card must be built to accommodate them: the vocabulary **stops being closed at four**, and `RequestStatusPalette` must gain a role **and a colour** for each of the four, because it currently returns `None` for anything outside the four and draws **no status at all**. |
| D27 | Status rows collapse | The status zone's lower rows follow the **same rule as the detail panel (D20)**: a row with no value is **removed**, not left blank. Applies to row 3 while unclaimed and row 4 wherever it has no value. |
| D28 | Closed requests stop the clock | For **Done** and **Cancelled** the timer stops: the ring **freezes at the moment it closed** and carries the **time taken**; row 3 is the material handler (or the person who cancelled); row 4 is the requester. Row 2 therefore means *time left* while live and *time taken* once closed, and the control must state which it is drawing, with a matching accessible name. |
| D29 | Overdue | Overdue **replaces row 1** — it reads `Overdue` and the ring shows the **time overrun** — and **the entire status column changes its colour theme**. This is how the card keeps spec 004 **FR-012** (an overdue row stays visible under every sort) now that D25 removed the bold-red remaining-time text. The ring is part of the column **theme**, not an independent green → amber → red scale. |
| D30 | Action buttons collapse too | The same hide/collapse rule as the panel (**D20**, **D27**) applies to the buttons: an unavailable action is **removed**, not blanked. The remaining buttons **do not stretch**; the button grid **re-centres** in the action zone. |
| D31 | One detail control per card, all in a single file | The card's **detail section (column 3)** is rendered by its **own custom control, one per card**, because the cells differ per Item. **All of those controls live in the same file**, so the arrangement does not add a file per card and bloat the project. The card pulls the right one for the row it is drawing. Sections of the card that are **not** the detail column (identity, status, actions) may still move into controls of their own where they grow complex — but the detail controls specifically are **grouped into one file**. |
| D32 | Where cell values come from | The **job snapshot and its resolver are extended** so the Item's declared fields resolve from the job. Anything the job cannot carry is **read from Infor Visual**. See §5.2 for what is missing today. |
| D33 | Location labels | The configuration labels are corrected to **`Die Location`** for the die Items and **`Home Location`** for the dunnage Items, shipped as a **seed and a migration**, so the card renders the label it reads rather than overriding it in code. The UI's only "Location" labels are **Die Location** and **Home Location**. The code-behind may name the field freely; the UI shows the proper label and value. |
| D34 | The four hidden Items are in scope | `pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service` are **unhidden and implemented with this work**, using D32's sourcing approach. Their cells are in §5. |
| D35 | Everything discussed is in this spec | The hopper size step, the die-shop checkbox, the fuzzy part picker, Weight of Coil and the skid count all **belong to this spec**, notwithstanding the New Request wizard exclusion in earlier revisions. Only D26's handler-set statuses are deferred to a future spec. |
| D36 | `Part Type` is the derived item type | On the four previously-hidden pickups, `Part Type` **is the derived item type of §5.3** — **Finished Goods**, **Outside Service** or **WIP** — and for **NCM** it is the **location-derived** NCM state, because NCM is a location and never a production type. No new field is invented for it. |
| D37 | No location fields on the four pickups | `pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service` show **no location field at all**. Their declared `Pickup location` and `Destination` are therefore **not cells**, and no third "Location" label arises from them (which is what keeps D33's rule true). Their cells are `Part Type` and `# of Skids` only (D34). |
| D38 | `Total Qty In House` | The **full sum of the part number** — read from **Infor Visual** or from the **WIP application**, **depending on the item type** (D36). It is not a per-location figure and it is not invented. |
| D39 | Cell anatomy | Every cell is a **Label on top with its Value beneath**, with no exceptions other than the label-less die-shop cell of **D40** — including the four previously-hidden pickups. Restated as a requirement by the requester 2026-10-10. |
| D40 | The die-shop cell carries no label | The die-shop cell has **no label**. Its **value is the phrase itself**: **`Take To Die Shop`** on a **pickup** (the die should be sent there) and **`Located In Die Shop`** on a **deliver** (the die is there already). It is drawn **only when the D23 checkbox was ticked**; unticked, the cell is **hidden**, not left empty — this replaces D23's original "ticked = false means an empty cell". Both phrases are resource keys, never literals (§7.5). |
| D41 | A cell that cannot be resolved is hidden, and logged where it should have been readable | **Both** cases hide the cell — a source that **exists but cannot be read**, and a cell the Item **never declares**. In addition the first case **writes a warning to the database log** so it can be chased up later; it is never drawn as a reason on the card and never invented. This **supersedes spec 004 FR-026**, which required an unresolvable value to say *why* rather than render blank. |
| D42 | The status flyout | **When the signed-in user is the material handler working on that job**, the **status text on row 1 becomes clickable**. Clicking it opens a **flyout window offering the four statuses of D26** — `Product Buried`, `Cant Find Product`, `Changing Forklift`, `Changing Propane`. Choosing one **re-themes the whole status column to a lighter red than the overdue treatment**, so a handler-reported condition on a live job is distinguishable at a glance from an overdue one. The text is **not clickable for anyone else** — not for other users, and not on a job the signed-in handler is not working. This is the **first interactive element inside the card body**, and it does not reopen D5: the card body still does not open the request. **Three column themes** therefore exist and must be distinct: the **normal** theme, the **handler-condition** theme (lighter red), and the **overdue** theme (D29). |

### Open questions

**None outstanding.** Every question raised during this pre-spec's review is settled, and the record of what was
resolved — by which decision — is kept below so the reasoning is not lost.

**Resolved 2026-10-10 (second round):** **O10** — `Part Type` is the derived item type of §5.3 (D36). **O11** —
the assignment applies to all four pickups; D37 says so explicitly. **O12** — no location fields on any of the four
pickups (D37). **O13** — the hopper images obey the application's existing picture rules (see §5.1). **O15** —
`Total Qty In House` is the full sum of the part number, from Infor Visual or the WIP application according to the
item type (D38).

**Resolved 2026-10-10 (third round):** **O14** — the die-shop cell is **label-less**, carries
`Take To Die Shop` / `Located In Die Shop` as its value, and is **hidden** when unticked (D40). **O16** — **both**
unresolvable cases **hide the cell**; the "source exists but cannot be read" case also **writes a warning to the
database log**. FR-026's "say why" is superseded (D41). **O17** — the handler-set statuses are **closed at four**
(D26), and the three named in the round before that are **removed**.

---

## 3. Field mapping — what each part of the target card is fed by

| Target element | Today's source | Change |
|---|---|---|
| Accent bar | `SampleOrder` → accent brush | unchanged |
| Picture + new-message badge | `Order.EffectiveImagePath`, `Order.HasNewMessages` | unchanged |
| Large value under the picture | requesting work centre | **new placement**, unlabelled |
| Row 1 — the status word | `Order.Status` → friendly wording | the pill becomes a plain word (D11, D25); reads `Overdue` when late (D29) |
| Row 2 — the ring | *new* — `elapsed`, `allotted_minutes` | fills as time is **used**, carries the **time left**; **freezes** and carries the **time taken** once closed (D25, D28) |
| The column's theme | `RequestStatusPalette`, `Order.IsOverdue` | **three distinct themes**: normal, the **handler-condition** theme set by the D42 flyout (lighter red), and the **overdue** theme (D29) |
| Row 1 is interactive for the handler | *new* — the signed-in user against the row's material handler | when the signed-in user **is the material handler on that job**, clicking the status word opens the D42 flyout; for everyone else it is plain text |
| Row 3 — the person | `Order.RequestedBy` while unclaimed; the material handler once claimed | *new on the card*; removed when empty (D25, D27) |
| Row 4 — the requester | `Order.RequestedBy` | *new on the card* for closed requests; removed when empty (D27, D28) |
| Title and identifier | `Order.Title`, `Order.Subtitle` | promoted into the spanning top row of the grid (D12, D21) |
| Detail cells | *new on the card* — the Item's declared fields | new resolution path (§5); Die-only cell is D16, placement is D20/D21 |
| Row 1: Accept / Cancel / Give back | `Order.AcceptCommand` / `CancelCommand` / `ReleaseCommand` | restyled into a grid |
| Row 2: Complete / Details / Print | `Order.CompleteCommand` / **new** / **new, disabled** | one new navigation action, one placeholder |
| Status pill | `Order.StatusBadgeText` | removed (D11) |
| Card-body click → open request | `ItemClick` on the list | removed; the Details button owns it (D5/D6) |

### Ring arithmetic

```
elapsed  = now − requested_utc                  (already computed for the Waiting/Elapsed text)
allotted = the Item's allotted_minutes          (null ⇒ the labelled 15-minute default, per spec 004 §5)
arc      = clamp( elapsed ÷ allotted , 0 , 1 )  (overdue clamps to a full ring)
label    = time left = allotted − elapsed       (negative once overdue ⇒ show the overrun, e.g. -00:15)
           once closed, the ring freezes and the label is the time taken
```

The ring must re-evaluate on the same one-minute tick that already refreshes `WaitingForText` and
`RemainingTimeText`, so the three never disagree.

---

## 4. The action grid (2 columns × 3 rows, row-major)

| | Column 1 | Column 2 | Column 3 |
|---|---|---|---|
| **Row 1** | **Accept** — green check | **Cancel** — red X | **Give back** — amber pause |
| **Row 2** | **Complete** — blue box-with-check | **Open details** — magnifier | **Print** — printer, **disabled** |

Rules that carry over unchanged and must not be weakened:

* Each button is drawn only while the row's own gate says the action is available (`CanAccept`,
  `CanCompleteOrRelease`, `CanCancelRequest`, plus the requester's own cancel gate).
* An unavailable button is **removed**, and what remains **re-centres without stretching** (D30).
* Every button keeps a `ToolTipService.ToolTip` and an `AutomationProperties.Name` from the row's localized text.
* Minimum 44×44 hit target.
* Accept / Complete / Give back keep their three distinct colours and glyphs so they are never mistaken for one
  another.
* **Print** is `IsEnabled="False"` with a "not available yet" tooltip and a code-behind `TODO` naming label printing.
  Visible so the layout is honest about what is coming; inert so it cannot mislead.
* **Open details** replaces the card-body click. The picture keeps Shift+click enlarge; the badge keeps its meaning.

---

## 5. The detail cells — settled per Item

Every cell is the Item's own declared field (`waitlist_request_item_configs.detail_fields_json`) in declared order,
**minus** whatever D19 excludes. **Every cell is a label on top with its value beneath (D39)** — no cell departs
from that shape — and the cells are laid into D21's grid. There are **no fallbacks**: nothing is padded, and an
Item with fewer cells simply has a shorter panel.

`cells` is how many the panel fills, which drives D20's geometry. ⚠ marks a cell that is **not** a declared field
of that Item or has no source today — all of them are listed in §5.2, and none may be invented (§7.2). `—` means
the panel collapses entirely.

### Pickup (11 Items)

| Item | Cells, in order | cells |
|---|---|---|
| `pickup-coil` | Coil number · Quantity in house · Coil description · Average coil weight | 4 |
| `pickup-die` | Die · **Die Location** *(plus, when the D23 checkbox is ticked, the label-less **`Take To Die Shop`** cell)* | 2–3 |
| `pickup-component` | Component · Total Qty In House ⚠ · Item Type (WIP or Finished Goods) ⚠ | 3 |
| `pickup-dunnage` | Dunnage part · **Home Location** | 2 |
| `pickup-scrap` | Scrap Type · Lugger Location ⚠ *(placeholder until a future patch)* | 2 |
| `pickup-hopper` | Size ⚠ *(D22)* | 1 |
| `pickup-fg` | Part Type *(the derived item type, D36)* · # of Skids *(D34)* | 2 |
| `pickup-ncm` | Part Type *(location-derived, D36)* · # of Skids *(D34)* | 2 |
| `pickup-wip` | Part Type *(the derived item type, D36)* · # of Skids *(D34)* | 2 |
| `pickup-outside-service` | Part Type *(the derived item type, D36)* · # of Skids *(D34)* | 2 |
| `pickup-riser-table` | — panel collapses | 0 |

### Deliver (9 Items)

| Item | Cells, in order | cells |
|---|---|---|
| `deliver-coil` | Coil number · Quantity in house · Coil description · Average coil weight | 4 |
| `deliver-flatstock` | Part · Quantity ⚠ · Destination ⚠ | 3 |
| `deliver-component` | Component · Total Qty In House ⚠ · Item Type (WIP or Finished Goods) ⚠ | 3 |
| `deliver-die` | Die · **Die Location** *(plus, when the D23 checkbox is ticked, the label-less **`Located In Die Shop`** cell)* | 2–3 |
| `deliver-dunnage` | Dunnage part · **Home Location** | 2 |
| `deliver-wrong-coil` | Coil number ⚠ *(the incorrect coil, typed via D24)* · Correct coil ⚠ | 2 |
| `deliver-wrong-flatstock` | Part ⚠ *(the incorrect flatstock, typed via D24)* · Correct flatstock ⚠ | 2 |
| `deliver-riser-table` | — panel collapses | 0 |
| `deliver-hopper` | Size ⚠ *(D22)* | 1 |

### Assist (3 Items)

| Item | Cells, in order | cells |
|---|---|---|
| `assist-coil-turn` | Coil Number ⚠ · Weight of Coil ⚠ *(a **required** operator entry, D35)* | 2 |
| `assist-table-place` | — panel collapses | 0 |
| `assist-table-remove` | — panel collapses | 0 |

### Other (1 Item)

| Item | Cells, in order | cells |
|---|---|---|
| `other` | Request description — drawn with **`AutoScrollTextView`** | 1 |

⚠ **Weight of Coil is not Pickup Coil's `Average coil weight`.** One is a job-derived average; the other is the
actual weight the operator must supply on an Assist Coil Turn so a material handler can judge whether a larger
forklift is needed. They must not be conflated.

### 5.1 New request-workflow requirements introduced by this work (D35)

Each of these adds an answer or a step to the New Request workflow. They are in this spec.

| Requirement | Item(s) | Shape |
|---|---|---|
| Hopper Size (D22) | `pickup-hopper`, `deliver-hopper` | three radios + a reference image of the selected hopper; the card's Size cell links to that image |
| Die shop direction (D23) | `pickup-die`, `deliver-die` | a checkbox — at the die shop, or send it there — feeding a conditional third cell |
| Fuzzy part entry (D24) | `deliver-wrong-coil`, `deliver-wrong-flatstock` | the incorrect part is typed through a fuzzy search modal, backed by a **live** Infor Visual read |
| Weight of Coil (D35) | `assist-coil-turn` | a **required** numeric entry |
| # of Skids (D34, D35) | the four previously-hidden Items | a numeric entry, **defaulting to 1**, for how many skids must be picked up |

**The hopper images are held to the application's existing picture rules (D22).** "The same sizing restrictions as
the other waitlist images" is not a new rule — it is the shared policy already used for press and part pictures:

| Rule | Value | Where it is defined |
|---|---|---|
| Shape | **square** — target aspect ratio `1.0`, tolerance ±2 % | `MTM_Waitlist.Shared/Helpers/ImagePicturePolicy.cs` |
| Minimum size | **48 px** a side (the app's own preview floor) | `ImagePicturePolicy.MinimumPixels` |
| Maximum size | **2048 px** a side | `ImageValidationRules.MaxDimensionPixels` |
| File types | `png`, `jpg`, `jpeg` | `ImageValidationRules.AllowedExtensions` |
| File size | between **1 KB** and **10 MB** | `ImageValidationRules.MinFileSizeBytes` / `MaxFileSizeBytes` |
| When nothing usable | draws `Assets/Placeholders/default-no-image.png` | `ImagePicturePolicy.NoImagePath` |

The hopper reference images must satisfy these same rules, and a size whose image is missing or unusable draws the
shared placeholder rather than a blank — matching how every other picture in the application behaves.

### 5.2 What the values resolve to today, and what is missing

This is the **largest gap in the change**. Two different problems hide behind "the card shows the Item's declared
fields":

**(A) The resolver is missing labels the snapshot already holds.** `RequestJobFieldValues.Resolve`
(`MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs:36-57`) maps exactly four labels — `die`,
`pickup location`, and `part number` / `job part number` / `part` — and returns `null` for **everything else**.
But the job snapshot `RequestJobPartAvailability` already carries `CoilPartNumber`, `FlatstockPartNumber`,
`ScrapType`, `ComponentPartNumbers` and `DunnageParts`. Values exist and cannot be reached by the declared label.

**(B) The snapshot does not hold the value at all.** `RequestJobPartAvailability` has no quantity, description,
weight, destination or home location. `RequestDunnagePart` carries `PartNumber`, `DisplayName` and `ImagePath` but
**no location**, and `RequestDiePart` is the only part model that has a `Location`.

| Cell | Snapshot has it? | Resolver maps the label? | Therefore |
|---|---|---|---|
| `Die`, `Die Location` | yes | yes | **resolves** |
| `Part` / `Part number` | yes | yes | **resolves** |
| `Component`, `Dunnage part`, `Request description` | n/a — captured answers | n/a | **resolves** |
| `Coil number`, `Coil Number`, `Correct coil` | yes | **no** | resolver gap (D32) |
| `Correct flatstock` | yes | **no** | resolver gap (D32) |
| `Scrap Type` | yes | **no** | resolver gap (D32) |
| `Quantity in house`, `Quantity`, `Total Qty In House` | **no** | n/a | data gap — see O15 |
| `Coil description`, `Average coil weight`, `Item Type`, `Weight of Coil` | **no** | n/a | data gap — see O15 |
| `Home Location` | **no** | n/a | data gap |
| `Destination` | declared `fixed` | **no fixed resolver** | gap |
| `Part Type`, `Lugger Location`, `Size`, the die-shop cell, `# of Skids` | **no** | n/a | new answers (D22–D24, D34) — see O10, O13 |

**D32 is the answer to (A):** extend the resolver so the labels the snapshot already holds resolve. **(B) needs a
decision per cell** — add the value to the snapshot, read it from Infor Visual, or drop the cell. Nothing may be
invented.

**Declared fields the cards must no longer show.** These exist in configuration but are excluded by D19 or have no
source, so the card change and the configuration now disagree. The spec must say what happens to each:

* `Requesting work center` / `Pickup work center` / `Work center` — declared by many Items, excluded by **D19**
  because the work centre is the left-column value.
* `Description` — declared by `pickup-dunnage` and `deliver-dunnage`, and **verified unfillable** on 2026-10-10:
  no dunnage table holds one (`dunnage_parts` carries `part_id`, `type_id`, `spec_values`, `home_location`;
  `dunnage_types` carries only `type_name` and `icon`). This is **broken configuration**, not a missing source.
* `Quantity` on the die Items, `Reason` on both wrong-part Items, and the surplus fields on the four
  previously-hidden Items — declared but not part of their cards.
* `Destination` on `deliver-flatstock`, `pickup-ncm` and `pickup-outside-service` — kept, but `research.md` records
  that `destination` left the token set on 2026-09-20, so availability needs confirming.

### 5.3 Deriving the label type — FG, Outside Service, WIP — and why NCM is not one of them

Investigated 2026-10-10 against the Infor Visual schema export
(`Documents/Development/InforVisual/DatabaseCSVFiles`, 968 tables) and the existing repo code. **The short answer:
Outside Service and WIP are derivable from the job's routing; Finished Goods needs a location; NCM cannot come from
the routing at all.**

**The Infor Visual schema supports it.** `OPERATION` (257,341 rows) carries `SEQUENCE_NO`, `RESOURCE_ID`,
`VENDOR_ID`, `VENDOR_SERVICE_ID`, `SERVICE_PART_ID`, `SERVICE_ID`, `STATUS` and `PRED_SEQ_NO` — so the job's
*current sequence* is addressable, and whether **that** step is subcontracted is a direct read of its vendor
fields. `PART_LOCATION` carries `PART_ID`, `WAREHOUSE_ID`, `LOCATION_ID`, `QTY`, `STATUS`, `HOLD_REASON_ID`,
`LOCKED` and `TRANSIT`, and `LOCATION` carries `ID`, `TYPE`, `VENDOR_ID` and `CUSTOMER_ID` — so on-hand **by
location**, and a location's own nature, are both available. `PART` carries only a single aggregate `QTY_ON_HAND`.

| Label | Derivable from the routing? | The actual signal |
|---|---|---|
| **Outside Service** | **Yes — from the sequence.** | the `OPERATION` row at that `SEQUENCE_NO` has `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID` populated (or `RESOURCE_ID = 'OUTSIDE_SERVICE'`). |
| **WIP** | **Yes, by position.** | a step earlier than the routing's last one, with open quantity on the order (`DESIRED_QTY − RECEIVED_QTY > 0`) or an open status (`R`, `U`, `F`). |
| **Finished Goods** | **No — routing position is necessary but not sufficient.** | the part must actually be **sitting at a finished-goods location**. `LOCATION.TYPE` / `LOCATION.VENDOR_ID` on the Infor side, or the WIP app's `md_locations` taxonomy, is the signal. |
| **NCM** | **No — never.** | it is a **state of the stock's location, not a production type.** |

**NCM, specifically — confirmed as you described it.** NCM is not a Part Type and no routing field carries it.
The only thing that says a part is NCM is **where it is**:

* In the **WIP application** the signal is purely the location: `md_locations.Location LIKE 'NCM%'`
  (`NCM`, `NCM - VITS`, `NCM-VITS`, `NCM/NA`). Critically, **`inv_inventory.ItemType` still reads `'WIP'` while the
  part sits at an NCM location** — so `ItemType` must never be used for this, only the location.
* In **Infor Visual** the same idea lives in `PART_LOCATION.STATUS` / `HOLD_REASON_ID` against the `LOCATION` the
  stock occupies.

**What already exists in this repository** — this is not new ground:

* `MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs` derives `FG / WIP / OutsideService` with the
  documented priority *Outside Service → Finished Goods → WIP*, from a pure `DispositionInput`. Its own doc states
  the rule as "on-hand quantity > 0 **at a finished-goods / shipping location** and the work order is closed".
* `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` (live-validated) supplies that
  input, and `Database/MTMWipApp/.../GetWipFloorQuantities.sql` supplies the floor cross-check that buckets
  `inv_inventory` by the location taxonomy above.
* `RequestDispositionResolver` is registered in DI at `ServiceRegistrationExtensions.cs:127` and **has no consumer
  outside its own tests** — the derivation exists but is not wired to anything.

**A defect found while checking this, which matters for the `Item Type` cell.** `GetDispositionInput.sql` passes
**`PART.QTY_ON_HAND`** as `FinishedGoodsQuantity`, but `PART.QTY_ON_HAND` is the part's **total on-hand across every
location**, not stock at a finished-goods location — the query applies **no location filter at all**. So the
implementation is looser than its own documentation: any stock anywhere, plus a closed order with nothing open,
reads as **Finished Goods** today. The location-filtered source it should use is `PART_LOCATION` joined to
`LOCATION` (or the WIP floor buckets). **This is worth correcting independently of the card work**, because the
`Item Type (WIP or Finished Goods)` cell on the component cards would otherwise report the wrong answer — and
`pickup-ncm`'s whole premise depends on location being the signal.

**One limitation, stated plainly.** The CSV export is **schema only** — it holds `ColumnDetails`, `Tables`,
`CheckConstraints`, `Indexes` and `TableRowCounts`, with **no data rows and no enum values**. It therefore confirms
that `OPERATION.SEQUENCE_NO` and `PART_LOCATION.QTY`/`STATUS` exist and are usable, but it **cannot** confirm which
location codes this installation actually uses for FG or NCM. Those codes come from live queries and are recorded
in the two SQL scripts above and in `.github/memories/repo/infor-visual-disposition.md`. Any new derivation must be
validated against the live database, not against the export.



### 5.4 The four previously-hidden Items — audit result (2026-10-10)

D34 brings these into scope, so the review that was asked for matters. **Nothing is half built:**

* **The hide is a hard-coded list.** `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs:24-31` holds
  `s_outOfScopeItemIds = ["pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service"]`; `IsVisible` returns
  `false` for them at lines 95-98. There is no `is_in_scope` column, so unhiding is **code plus real job gates**,
  not a data change.
* **They are deferred in writing.** `specs/004-unified-card-item-picker/spec.md:482` — *"The four Items with no
  value source are out of scope and are hidden until their own work lands"*; `data-model.md:55-56` — *"Out of
  scope, catalogued but never offered … excluded by the visibility rules, not by deletion (FR-028)"*. T029 and
  SC-014 say the same. **No spec describes an implementation of them.**
* **What exists:** catalogue rows, display names (`Strings/en-us/Resources.resw:852-862`), the hide rule and the
  test pinning it (`RequestItemPickerRulesTests.cs:110-143`), seed rows and demo requests. **No** view, view model,
  wizard step, stored procedure or contract constant is dedicated to any of the four.
* **A real but unconnected stack exists** for their subject matter — `RequestDisposition`,
  `RequestDispositionResolver`, `WipFloorInventoryService` and friends, holding FG / NCM / WIP / outside-service
  floor quantities. It is registered at `ServiceRegistrationExtensions.cs:127` and **has no consumer outside its own
  tests**. That is the nearest thing to "half done" and it is not wired to the picker at all.

---

## 6. What this reverses, and therefore must be stated in the spec

| Earlier decision | Where | What this change does to it |
|---|---|---|
| "Declared fields appear on the request page and never on the card" | spec 004 **FR-007** | **Formally superseded.** Item fields appear on the card. |
| The card's anatomy is frozen | spec 004 `contracts/card-and-identifier.md`, and the freeze spec 004 itself applied to spec 003 | **The freeze is lifted in writing**, following the same precedent spec 004 used when it unfroze spec 003's per-type card anatomy. |
| "The detail-field grid is **not** on the card" | `specs/004-unified-card-item-picker/data-model.md` §7 | **Reversed.** The Item's declared fields return to the card. |
| "The card's four metadata rows are requested by · press · remaining time · waiting" | `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` | **Superseded** by the new zone anatomy. |
| "The card body is the click target that opens the request" | spec 004 / 009 A3, and the closed defect `Closed-Medium-Waitlist-CardAndFlyoutPictureTookTheCardsClick.md` | **Reversed.** A dedicated button opens the request; the body no longer does. Shift+click enlarge survives. |
| `Press Location` and `Requested by` as card fallbacks | this document's own earlier revisions | **Withdrawn** (D19). |
| The status vocabulary is closed at four | `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` | **Opened** (D26), for a future spec, but the palette must be ready to gain roles and colours. |
| An unresolvable value must **say why** rather than render blank | spec 004 **FR-026** | **Superseded** (D41). Both unresolvable cases **hide the cell**; the case where the source exists but cannot be read also **writes a warning to the database log** so it can be chased up later, rather than telling the operator on the card. |

The spec must call these out explicitly. A silent contradiction of a ratified contract is not reviewable, and these
are cargo-cult risks the moment a later reader finds the old text.

---

## 7. Invariants the change must not break

1. **Overdue stays visible under every sort.** Spec 004 FR-012 requires it. D25 removed the text that carried the
   marking; **D29 replaces it** — row 1 reads `Overdue` and the whole status column re-themes, so the signal does
   not depend on the row's position.
2. **Nothing is invented.** Every value in every cell must come from the request, the job it names, the Item's
   configuration, or a value the application computed or read from Infor Visual. A cell with no source is
   **hidden**, never blanked, never guessed. A cell whose source **exists but cannot be read** is hidden too, and
   additionally **writes a warning to the database log** so the gap can be addressed later (D41) — it is never
   explained on the card and never filled with a substitute.
3. **One card shape per Item, not per request.** Two requests of the same Item must render identically in shape —
   spec 004 FR-006, which survives. D20 refines it: the panel's geometry follows how many cells the **Item** fills,
   and cards of different Items may differ in height.
4. **Action availability is unchanged.** A button's visibility still comes from the row's own gate; no action
   becomes available or unavailable because the layout changed.
5. **Localization.** Every new label — the ring's accessible name, `Open details`, the Print tooltip, the new cell
   labels and status words — is a resource key in `Strings/en-us/Resources.resw`, never a literal.
6. **Accessibility.** The ring must expose a text equivalent saying what it measures **and which of the two things
   it is measuring** (time left while live, time taken once closed), since D28 makes it mean different things.
7. **Animations are optional.** Any animation the new controls add must respect the system's animations-off
   setting, as `AutoScrollTextView` already does.
8. **The card must remain readable at the app's default window size** (D14).

---

## 8. Artifacts the implementation will touch (for the plan, not the spec)

* `Module_Waitlist/Controls/WaitlistLineCardView.xaml` and `.xaml.cs` — the card, now **pulling a detail control
per card** (D31), plus the Print `TODO`.
* **The detail controls, one per card, all in a single file** (D31) — the arrangement that keeps column 3 out of
  the card's own XAML without adding a file per card. Any other section that grows complex may also move into a
  control of its own.
* `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs` — **the single most important file in this change.**
  It maps four labels and returns `null` for every other one, so most of §5's cells resolve to nothing (D32, §5.2).
* `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` (+ `RequestDunnagePart`, `RequestDiePart`) — the
  snapshot the resolver reads. Every **data gap** in §5.2 must be added here, read from Infor Visual, or dropped.
* `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` — the hard-coded out-of-scope list is removed and real
  job gates added (D34, §5.3).
* `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` — gains roles and colours for the four handler-set
  statuses (D26) and **three column themes** — normal, handler-condition (lighter red) and overdue (D29, D42).
* The **status flyout** (D42) — the first interactive element inside the card body, gated on the signed-in user
  being that row's material handler.
* `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` and `ViewModels/WaitlistViewViewModel.cs` — the ring inputs,
  the status rows, and resolving the Item's declared fields for each row.
* `Module_Shared/Controls/AutoScrollTextView.xaml` — reused for the `other` card's description.
* A **new WinUI fuzzy part picker** with a live Infor Visual read behind it (D24) — the receiving application's
  equivalent is WinForms and cannot be reused verbatim. The natural home for the query is beside
  `Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql`.
* The **New Request wizard** pages for §5.1's five new requirements.
* `Database/Seeds/seed_waitlist_request_item_configs/` **and a migration** — the `Die Location` / `Home Location`
  label correction (D33).
* `Strings/en-us/Resources.resw` — the new labels and status words.
* `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` — currently asserts the labels
  `Requested by`, `Press`, `Remaining time`, `Waiting`, and two tests that pinned the old card were already removed
  on 2026-10-10. **The new spec owns rewriting this file** to the new anatomy.
* `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` — superseded; the new spec's contract
  becomes the live one.

**Note for whoever writes the spec**: the reference image is the only visual description, and it is **out of date in
three places** — the ring's contents (D25), the status column's rows (D25/D28) and the panel's earlier
`Press Location` row. The spec must make the *arrangement* verifiable — zone order, which value sits where, which
cells are conditional — from **this document's decisions plus the image**, not from pixel sizes and not from the
image alone.

---

## 9. Acceptance criteria for the spec

* A row can be read at a glance without opening it: what is wanted, what it is for, how long it has waited, and how
  much of its window is left.
* Every card of the same Item presents its cells in the same places; two different Items never swap cell meanings.
* A card with fewer cells is shorter, not padded, and the title row spans correctly in every case (D20, D21).
* `Die Location` appears on Die requests and on no other request.
* The status column reads correctly in every state: the word, the ring carrying time left (or time taken once
  closed), and the person row — requester while unclaimed, material handler once claimed — with empty rows removed
  rather than blank (D25–D28).
* An overdue request is unmistakable after the change, under every sort (D29).
* Unavailable action buttons are absent, and the remaining grid is centred rather than stretched (D30).
* The card remains usable with the system's animations switched off.
* No card shows a value the request does not carry.
* Printing is visibly unavailable and says so when pointed at, rather than appearing broken or appearing to work.
* Opening a request is a deliberate, separate action from clicking anywhere on its row.

---

## 10. Out of scope

* The waitlist **detail page** layout (D1).
* Any change to the request **lifecycle** vocabulary itself. The four handler-set sub-statuses of **D26** and the
  **flyout that sets them (D42)** are a **future spec** — a flyout whose only purpose is to set statuses that do not
  exist yet cannot ship before them. **This spec must still leave the hook:** row 1's status text is built to be
  clickable for the row's own material handler, and the palette is built to carry three themes, so the future work
  is a fill-in rather than a re-cut of the card.
* Any **new real action** beyond Open details. Print is a disabled placeholder (D4).
* **Setup, Settings, and the Mock/visual-fallback surfaces.**
* Any change to the **mirror/fallback** model. The fuzzy part search is deliberately **live-only** and must not be
  given a cached read shape (D24).

**Scope note.** An earlier revision of this document excluded the New Request wizard. **That exclusion is lifted:**
the hopper size step, the die-shop checkbox, the fuzzy part picker, Weight of Coil and the skid count all belong to
this spec (D35). The only deferral is D26's handler-set statuses.
