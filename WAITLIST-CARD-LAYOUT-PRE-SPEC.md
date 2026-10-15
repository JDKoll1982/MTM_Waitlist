# Pre-Spec — Waitlist Line Card Layout (image-driven redesign)

**Status**: Pre-specification — all decisions settled, spec not yet written.
**Created**: 2026-10-09
**Last revised**: 2026-10-10 (review complete; every question settled).
**Surface**: the waitlist list rows — `Module_Waitlist/Controls/WaitlistLineCardView.xaml` — **only**.
**History**: earlier revisions, including decisions that were later superseded or replaced, are preserved in
`WAITLIST-CARD-LAYOUT-PRE-SPEC.backup-2026-10-10.md` and
`WAITLIST-CARD-LAYOUT-PRE-SPEC.backup-2026-10-10b.md`. **This file contains current decisions only.** Decision
numbers are kept from the original so a reference made against a backup still resolves; the gaps in the sequence
(there is no D8, D9, D10 or D17) are decisions that were superseded and deliberately removed.

**How to use**: this is the seeding input for one `/speckit.specify` run. The decisions and the per-Item cell
assignments are settled and must not be re-litigated; the requirement material is deliberately WHAT/WHY so the spec
stays behavioural.

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
2. **Status zone** — a centred stack of the rows below (D25).
3. **Identity-and-detail zone** — an inner grid of **3 rows × 2 columns** (D21): row 1 the always-spanning title
   pair, rows 2–3 the detail cells.
4. **Action zone** — up to six square icon buttons in a 3×2 grid, standing off behind a vertical rule, removing
   unavailable buttons and **re-centring** what remains (D18, D30).

### The status zone's rows (D25, D27, D28, D42)

| Row | Live request | Closed request (Done / Cancelled) |
|---|---|---|
| **1** | the status word — see the precedence below | the status word |
| **2** | the ring, filling as the allotted time is used, carrying the **time left** inside it | the ring **frozen**, carrying the **time taken** |
| **3** | the **requester** while unclaimed; the **material handler** once claimed | the **material handler** |
| **4** | — | the **requester** |

A row with no value is **removed**, not blanked (D27).

**What row 1 reads, in precedence order (D43):**

| Condition | Row 1 reads | Column theme |
|---|---|---|
| the row's material handler has **set a status** (D26) | that status, e.g. `Product Buried` | **handler-condition** — a lighter red (D42) |
| the request is **overdue** and no handler status is set | `Overdue`, with the ring showing the time overrun (`-00:15`) | **overdue** — red (D29) |
| neither | the lifecycle word in friendly wording — Waiting, In Progress, Done, Cancelled (D25) | **normal** |

---

## 2. Decisions settled with the requester

| # | Decision | Value |
|---|---|---|
| D1 | Surface | The waitlist list rows only. The detail page is **not** changed by this work. |
| D2 | Action buttons | Only actions the application can really perform are wired. No new business actions. |
| D3 | Action grid shape | Up to six slots in a 2×3 grid — see §4. |
| D4 | Print | **Superseded by D59 (2026-10-11): there is no print action on the card.** Ships **disabled**, with a "not available yet" tooltip and a `TODO` in the code-behind naming label printing as the future action. A placeholder, not a working feature. |
| D5 | Card click | The card body **stops** opening the request. A dedicated button opens it. The status flyout (D42) is the one interactive element in the body and does not change this. |
| D6 | Open-details button | Magnifier glyph, tooltip "Open details". |
| D7 | Picture behaviour | Shift+click enlarge and the new-message badge both stay. |
| D11 | Status presentation | The coloured status **pill is replaced** by plain status text, coloured per state. The word stands alone — there is no `Status:` prefix. |
| D12 | Title and identifier | The detail grid's always-spanning top row: the request's **Category** word and the request's own identifier (part number / dunnage / die / request description). |
| D13 | Detail cells | The cells beneath the top row are **determined by the request's Item**, not hand-chosen per row. Assignments are in §5. |
| D14 | Sizing | A full-width row that stretches; tuned to stay readable at the app's default window size. |
| D15 | Pre-spec | This document. It is the input to a new `/speckit.specify` run — no code is written yet. |
| D16 | `Die Location` cell | **Dies only.** Shown on `pickup-die` and `deliver-die` and on no other Item, and labelled `Die Location`. |
| D18 | Action buttons | **Square**, same colours, glyphs and gating as today. The ≥44×44 hit target is unchanged. **Give back's colour is the one open disagreement between the two settled records**: §4's table below names it **amber**, but spec 003's ratified action contract pins it to `SystemFillColorSolidNeutralBrush` and carries a rationale for that brush. This work left it on the solid neutral, because changing it means superseding another feature's contract; see **D44**. |
| D19 | The panel never shows | The **requesting work centre** and the **requesting user** are **never** detail cells. The work centre is the large left-column value; the requesting user appears only on the request's own detail page. There are **no universal fallbacks** — nothing is padded to fill a short panel. |
| D20 | Panel spanning by cell count | The panel's geometry follows how many cells the Item fills: **4** → 2×2; **3** → cells 1–2 on the top row with **cell 3 spanning the full bottom row**; **2** → cells 1–2 with the **bottom row collapsed**; **1** → **cell 1 spanning the full width**; **0** → both lower rows removed. A card's height differs **between** Items but is constant for any one Item. |
| D21 | Detail-zone grid | The card's third column is an inner grid of **3 rows × 2 columns**. **Row 1 is the title pair** and **always spans both columns**; rows 2 and 3 hold the detail cells. D20's rules are row merges of this grid. With no cells, the title row occupies the vacated space as a single full-width row. |
| D22 | Hopper Size | Both Hopper Items show a single `Size` cell. The Hopper request gains a size answer: **three radio buttons — Small, Medium, Large — with a reference image of the selected hopper beneath them**. On the card the Size cell is a **link that opens that image**, like the Ctrl+click enlarge the other pictures use. The images obey the application's existing picture rules (§5.1). |
| D23 | Die shop direction | Both Die Items gain a **checkbox in the request workflow** stating whether the die is **currently at the die shop** (deliver) or **should be sent there** (pickup). On the card it is a **third cell, shown only when that checkbox was ticked** — see **D40**. |
| D24 | Fuzzy part-number entry | The **incorrect part** on Wrong Coil and Wrong Flatstock is **typed through a fuzzy search**, not free text. Input is upper-cased and, with a known prefix, **zero-padded to 10 characters** (`MMC1000` → `MMC0001000`, `MMF500` → `MMF0000500`). An **exact** match resolves at once; otherwise a modal lists every part whose number **contains** the term. Prefixes: **`MMC`** coil, **`MMF`** flatstock, **`FGT`** die. The correct part still comes from the job. **Live only, and it degrades rather than blocks:** the search is a live Infor Visual read and is never cached, because a contains-search over every part is not a mirrorable shape. Unreachable ⇒ the operator is **told the search is unavailable and the value they typed is used as entered**. |
| D25 | Status zone has three rows | Row 1 the status word in **friendly wording** (Waiting, In Progress, Done, Cancelled); row 2 the ring, filling as time is used and carrying the **time left** inside it; row 3 the person — **requester** while unclaimed, **material handler** once claimed. There is **no percentage and no `Elapsed:` line**. |
| D26 | Handler-set sub-statuses | A material handler can set **four** additional statuses: **`Product Buried`**, **`Cant Find Product`**, **`Changing Forklift`**, **`Changing Propane`**. They are **set through the flyout of D42** and persist by a **material-handler pull from the database**. `RequestStatusPalette` must gain a role **and a colour** for each, because it currently returns `None` for anything outside the four lifecycle words and would draw **no status at all**. These ship **with this work** (D35). |
| D27 | Status rows collapse | The status zone's lower rows follow the **same rule as the detail panel (D20)**: a row with no value is **removed**, not left blank. Applies to row 3 while unclaimed and row 4 wherever it has no value. |
| D28 | Closed requests stop the clock | For **Done** and **Cancelled** the timer stops: the ring **freezes at the moment it closed** and carries the **time taken**; row 3 is the material handler (or the person who cancelled); row 4 is the requester. Row 2 therefore means *time left* while live and *time taken* once closed, and the control must state which it is drawing, with a matching accessible name. |
| D29 | Overdue | Overdue **replaces row 1** — it reads `Overdue` and the ring shows the **time overrun** — and **the entire status column changes its colour theme**. This is how the card keeps spec 004 **FR-012** (an overdue row stays visible under every sort) now that D25 removed the bold-red remaining-time text. The ring is part of the column **theme**, not an independent green → amber → red scale. |
| D30 | Action buttons collapse too | The same hide/collapse rule as the panel (**D20**, **D27**) applies to the buttons: an unavailable action is **removed**, not blanked. The remaining buttons **do not stretch**; the button grid **re-centres** in the action zone. |
| D31 | One detail control per card, all in a single file | The card's **detail section (column 3)** is rendered by its **own custom control, one per card**, because the cells differ per Item. **All of those controls live in the same file**, so the arrangement does not add a file per card and bloat the project. The card pulls the right one for the row it is drawing. Sections that are **not** the detail column (identity, status, actions) may still move into controls of their own where they grow complex — but the detail controls specifically are **grouped into one file**. |
| D32 | Where cell values come from | The **job snapshot and its resolver are extended** so the Item's declared fields resolve from the job. Anything the job cannot carry is **read from Infor Visual**. See §5.2. |
| D33 | Location labels | The configuration labels are corrected to **`Die Location`** for the die Items and **`Home Location`** for the dunnage Items, shipped as a **seed and a migration**, so the card renders the label it reads rather than overriding it in code. The UI's only "Location" labels are **Die Location** and **Home Location**. The code-behind may name the field freely; the UI shows the proper label and value. |
| D34 | The four hidden Items are in scope | `pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service` are **unhidden and implemented with this work**, using D32's sourcing approach. Their cells are in §5. |
| D35 | Everything discussed is in this spec | Nothing raised during this review is deferred. The four handler-set statuses and the flyout that sets them, the hopper size step, the die-shop checkbox, the fuzzy part picker, Weight of Coil and the skid count all belong to this spec. |
| D36 | `Part Type` is the derived item type | On the four previously-hidden pickups, `Part Type` **is the derived item type of §5.3** — **Finished Goods**, **Outside Service** or **WIP** — and for **NCM** it is the **location-derived** NCM state, because NCM is a location and never a production type. No new field is invented for it. |
| D37 | No location fields on the four pickups | `pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service` show **no location field at all**. Their declared `Pickup location` and `Destination` are therefore **not cells**, and no third "Location" label arises from them. Their cells are `Part Type` and `# of Skids` only (D34). |
| D38 | `Total Qty In House` | The **full sum of the part number** — read from **Infor Visual** or from the **WIP application**, **depending on the item type** (D36). It is not a per-location figure and it is not invented. |
| D39 | Cell anatomy | Every cell is a **Label on top with its Value beneath**, with no exceptions other than the label-less die-shop cell of **D40**. |
| D40 | The die-shop cell carries no label | The die-shop cell has **no label**. Its **value is the phrase itself**: **`Take To Die Shop`** on a **pickup** and **`Located In Die Shop`** on a **deliver**. It is drawn **only when the D23 checkbox was ticked**; unticked, the cell is **hidden**, not left empty. Both phrases are resource keys, never literals (§7.5). |
| D41 | A cell that cannot be resolved is hidden, and logged where it should have been readable | **Both** cases hide the cell — a source that **exists but cannot be read**, and a cell the Item **never declares**. In addition the first case **writes a warning to the database log** so it can be chased up later; it is never drawn as a reason on the card and never invented. This **supersedes spec 004 FR-026**, which required an unresolvable value to say *why* rather than render blank. |
| D42 | The status flyout | **When the signed-in user is the material handler working on that job**, the **status text on row 1 becomes clickable**. Clicking it opens a **flyout window offering the four statuses of D26**. Choosing one **re-themes the whole status column to a lighter red than the overdue treatment**, so a handler-reported condition on a live job is distinguishable at a glance from an overdue one. The text is **not clickable for anyone else** — not for other users, and not on a job the signed-in handler is not working. This is the **first interactive element inside the card body**; it does not reopen D5. |
| D43 | Row 1 precedence | A **handler status takes over row 1** and its theme while it is set. **Overdue** applies when no handler status is set. With neither, row 1 reads the lifecycle word in the normal theme. A handler status therefore hides the lifecycle state on the card for as long as it is set — the same trade D29 makes for overdue, and the reason both are deliberately visible states rather than silent ones. |
| D44 | The action grid is 3 columns × 2 rows | **Corrected 2026-10-11 (owner, on review of the built card).** The grid is **three columns by two rows**, not the two-by-three that §1 bullet 4, **D18** and spec **FR-061** still said: **Accept · Cancel · Give back** across the top, **Complete · Open details · Print** beneath. This is what §4's own table has always shown, so the table was right and the prose around it was stale — the built card followed the prose and drew three rows. Six actions in three rows made the action zone the tallest thing on the card, which the two-row grid removes. **Not changed: Give back's colour.** §4's table names it amber while spec 003's ratified action contract pins it to `SystemFillColorSolidNeutralBrush`; the two records disagree, and settling it means superseding spec 003's contract, so it is left on the neutral and raised rather than quietly changed. |

| D45 | The detail title is two centred lines, and the cells are ruled off from each other | **Corrected 2026-10-11 (owner, on review of the built card against `NewWaitlistCardLayout.jpg`).** Row 1 of the detail zone is the title **pair** as D21 settled, but **stacked and centred** — the Item's word large on the first line, the request's identifier smaller beneath it — rather than side by side, where the two read as one sentence and the identifier, which is the part a person is looking for, gets lost in it. The cells beneath are each closed off by a **hairline along their top and left edge**, which is what draws one continuous line between two cells and one under the title; the per-Item grids therefore carry **no spacing of their own** (the gap moved inside each cell as padding) and each cell **fills its row**, so a rule cannot stop short at the shorter of two cells. Cell copy is **centred**, its label semi-bold. |
| D46 | The action zone keeps three columns, and a missing button lets the rest shift left | **Corrected 2026-10-11 (owner, on review of the built card).** D44 fixed the count of columns; this fixes what happens when one of them is empty. A button whose gate says the action is unavailable is **removed** (FR-062), and in a grid with a row and a column written per button that left a **hole** — the row read as if a button had failed to load. The zone is now a **reflow panel** that keeps **three columns regardless** and fills them left to right with the buttons that are actually drawn: with the fourth of six unavailable the five that remain read **1 2 3 / 5 6**, and only the **number of rows** shrinks. The width stays three columns even with one or two buttons, so the cards in one list are all the same shape. The rule itself lives in `WaitlistActionZoneLayout`, where it is checked by its own tests; the panel applies it. |
| D47 | The cached-data notice sits in its own row at the foot of the shell | **Decided 2026-10-11 (owner).** The "Cached data in use" notice was drawn in the row **above** the page frame, so it pushed every page down the moment Infor Visual went unreachable and pulled it back up when it recovered — the screen moved under the person's hands for a reason that had nothing to do with their work. It now has **its own row after the frame**, and being an `Auto` row that is empty while the notice is away, it costs no height when it has nothing to say. |
| D48 | The PIN box takes the keyboard when the credential stage appears | **Decided 2026-10-11 (owner).** Choosing one of the people offered beside the form fills in the user name and shows the PIN box, and the person then had to click into the box before typing. The PIN box is the only thing that stage asks for, so it takes the keyboard **when the stage appears** — once per appearance rather than on every redraw, so it cannot fight a person who has moved on to something else on the page. Both routes into the stage (a typed user name, a remembered person) are covered by the same rule. |
| D49 | The enlarging gesture claims the whole gesture, and reads Shift from both sources | **Corrected 2026-10-11 (owner: "shift click on request image is not working").** A picture whose plain click belongs to its host enlarges on **Shift+click**, caught on the press so the host never sees it. The press alone was not enough: the host around the picture acts on a **tap** (it selects the row or raises `ItemClick`), so the enlarged view opened and was taken down with the host's own action before it could be seen — the click looked as if it had done nothing. The **release and the tap are now claimed** for the control as well. The Shift state itself is read from **both** the pointer event and the keyboard: a mouse pointer event does not always carry the modifiers that were down when it was created, and asking only the event is how the gesture came to do nothing in the running app. |
| D50 | A reload raised by a change in the store reuses the job data the load before it read | **Decided 2026-10-11 (owner: a 5–10 second pause after a status change or a cancel).** Every action raises a change, and the list rebuilds itself from the store, which is right — but the rebuild also **re-read** each work centre's job, its allotted window and its per-row pictures, at one live read per work centre per row. Acting on a card changes a request's status and its handler and touches **none** of those, so the reload now **reuses** what the load before it read, bounded by a **two-minute** window (a screen left open still re-reads). A load from a building change, or the person's own refresh, still reads everything. |
| D51 | The die-shop question follows the Item's direction | **Decided 2026-10-11 (owner).** The checkbox asked one question — *"Does this die go to the die shop?"* — on both die Items, while the card draws a different phrase for each: `Deliver to die shop` for a pickup and `Located at die shop` for a deliver. The question is now chosen by the **direction**, as the phrase already was, and both are read from the same rule so they cannot drift: a pickup asks whether the die has to be **taken** there, a deliver asks whether it **is** there. A wording that cannot be resolved leaves the step with its own default rather than showing a resource key. |
| D52 | The store the app runs against must match the repository, and a drift check now says so | **Found 2026-10-11, while chasing the missing die-shop message.** The message never appeared for a reason that had nothing to do with the card: the local store the app was talking to was a **pre-012 snapshot**. Its `sp_waitlist_request_insert` had **no `p_answers_json` parameter**, and MySQL accepts a call that passes **more** parameters than the routine declares, **ignores the surplus and reports success** — so every request saved normally and every captured answer was silently discarded, with nothing logged and nothing on screen. Its per-Item configuration seed was stale as well, so the die Items did not even **declare** the die-shop cell. Both are now brought up to the repository, and `Database/CopilotScripts/refresh_schema_drift.ps1` reports — and with `-Apply` repairs — a store whose routines have fallen behind their `create.sql` artifacts, because this class of drift is invisible at run time by construction. |

| D53 | A request that resolved no data centres its title pair, and its cell rows collapse to nothing | **Decided 2026-10-11 (owner, on reviewing a `deliver-riser-table` card).** The values a card draws come from the requesting job and from the request's own captured answers; when nothing resolves, `CellsToDraw` drops every cell, so **no cells is the same statement as no data**. That row drew its **title pair at the top** of a full-height zone with an empty area beneath it, which reads as a card whose contents failed to load. The zone's alignment now follows the row's **own cell count**: **centred** when it filled none, at the top over its cells otherwise. The two lower rows also collapse to **nothing** rather than to a gap: the zone carries **no spacing of its own**, the gap between the title and the cells is owned by the **cell area**, and that area is **collapsed** on the same count — so an empty row leaves no gap, no separator and no vacant row behind. This refines D24/FR-026, which said the title row "keeps the space": it keeps the space and is centred in it. |
| D54 | **Superseded by D59.** The action zone's disabled `Print` button is washed out to an empty box, and is left alone pending a decision | **Reported 2026-10-11 (found while capturing the card for D53), NOT changed.** `Print` is drawn disabled (FR-067), and the platform's **disabled visual state** fades the whole control — so its neutral fill becomes near-white **and its white glyph fades with it**, leaving a faint outline that reads as an empty box on every card rather than as a placeholder that is coming. The owner's own screenshot of the card shows it. It is **pre-existing** and unrelated to D45–D53, and it is a question about FR-067's presentation rather than a defect in the layout, so it is recorded here with its cause rather than changed uninvited: the fix is to keep the glyph legible at the disabled opacity (an explicit disabled foreground/background on this one button), and it needs a decision on how muted the placeholder should look. |

| D55 | The cached-data notice carries its own bottom gap, because nothing else insets that row | **Decided 2026-10-11 (owner: "add proper padding so it does not sit at the very bottom of the window").** The notice has a row of its own at the foot of the shell, and that row reaches the bottom of the window: the shell's content margin is **`56,24,56,0`**, so its bottom is **zero**, and nothing else insets the row. The notice therefore sat flush against the window's bottom edge and read as a strip of chrome rather than as a notice inside the page. It now carries **`0,8,0,24`** — 8 above, clearing the page it annotates, and 24 below, the shell's own inset, so the two gaps are the ones the shell already uses and not two new numbers. Horizontal inset was already right (the 56 each side of the same margin) and is unchanged. |

| D56 | A status change re-reads nothing that a status change cannot have moved, and the picture share is probed once per window rather than once per picture | **Decided 2026-10-11 (owner: "the application still hangs some times when I click the buttons that cause the status of the request to change, normally hangs for about 5 seconds"), measured rather than assumed.** The action's write was never the problem — it lands in the same second as the click — the **rebuild that follows it** was. An instrumented cold rebuild spent **2698 ms of its three-row loop resolving pictures** and **1020 ms reading jobs**, and the action path paid that whenever the two-minute reuse window had lapsed; a file probe against the plant picture share costs a **failed name resolution of 2.3–2.7 s** off the plant network (measured directly, where the same probe answers in 0.01 s once the operating system has cached the failure). Three changes: (1) `PictureShareReachability` remembers whether a share is answering, per share, for a short window — the same shape as `MySqlHostFallback` — so a silent share is probed once instead of once per part per extension, and while the share answers nothing changes at all; (2) a reload raised by a **change in the store** reuses the resolved pictures as well as the job data, because a status change cannot change a picture; (3) the **two-minute window is gone** — reuse is bounded by the **cold load** (the page opening, the building changing, the person's own refresh), which the person controls and which is the only thing that pays for a live read. Measured from the click to the card changing: **0.05 s**, **0.11 s** back-to-back, **1.17 s** after 170 s of quiet (an idle connection being re-established). **Disproved and reverted:** resolving the per-work-centre and per-Item data concurrently up front measured **2027 ms against the serial 1020 ms** — the store seam serialises those reads, and the warm-up read data the row loop only needs conditionally. |
| D57 | A hang is not a frozen window: the UI thread never blocks, so the measurement has to be of the card | **Established 2026-10-11, and it corrects how the symptom was described.** `tools/measure-ui-responsiveness.ps1` asks the window to acknowledge a null message every 25 ms and reports the longest stretch it could not; across every rebuild measured — 3718 ms of rebuild work included — the window answered **every** time (`0 s` unresponsive). The rebuild runs on pool threads, so the UI thread keeps pumping input throughout. What a person experiences as "it hangs for five seconds" is therefore **the card not changing for five seconds** while the window is in fact responsive, and a fix has to remove the *delay before the row updates*, not a block on the UI thread. That distinction is what pointed at the rebuild's per-row I/O rather than at anything on the UI thread. |
| D58 | The picture share is unreachable from this workstation, and part pictures are therefore placeholders | **Recorded 2026-10-11 while measuring D56, as a fact about the machine rather than a defect.** `Database/Seeds` and the shipped defaults point the picture root at `\\mtmanu-fs01\Expo Drive\…`, which this workstation cannot resolve off the plant network (`GetHostAddresses` fails after 2.71 s). The launch step already says so in its own words — *"the picture share could not be read, so the copies held on this computer are the ones being used"* — and every card draws the shared no-image placeholder as a result. **Not changed here:** the resolver's own acceptance rule answers "no picture" for a part whose file cannot be read, and whether a machine holding a local copy should draw it while the share is down is a question about FR-030 and FR-080, not about this stall. Flagged rather than settled. |

| D59 | The card carries **no** print action, and its placeholder is gone with its TODO | **Decided 2026-10-11 (owner: "remove the print button from all request types and remove its todo logic as well — this will not be a feature for the card itself, but for the details page later").** Printing a label needs the detail a label is made of, which lives on the request's own page, so the card is not where it belongs. **Supersedes D4** (the disabled "not available yet" placeholder with the code-behind TODO), **D54** (the decision to leave that placeholder alone pending a call on how muted it should look — D54's own evidence is why the answer turned out to be "remove it": the platform's disabled state faded the control until its 18px glyph was invisible, so it read as an empty box on every card), the **§4 table's** `Print` cells, the second row's `Print` entry in §2's grid diagram, and this document's **§5 localization**, **§8 artifacts** and **§10 out-of-scope** lines that name a Print tooltip, TODO or placeholder. **Also supersedes the spec's FR-067 and amends FR-061** (six buttons become five), **SC-012**, acceptance scenario 6 and FR-104's label list — all updated in place with the direction recorded. Nothing about print is left on the card: no control, no glyph, no resource key, no TODO. The action zone still lays out **three columns by two rows** (D44), which is what lets five actions take two rows rather than three. |

| D60 | The card names the material handler, and never prints their employee number | **Decided 2026-10-11 (owner, on reviewing a claimed card: "do not have the app show the user's employee number, instead show the user's full name").** The card's person row read **`6229`** under the ring where the name of the person holding the work belongs: the queue records the assignee in `assigned_material_handler` as an **employee number**, because that number is what every handler authorization compares, and the row had nothing else to print. The number stays exactly where it is needed — the assignment column and the authorization comparisons are untouched — and the **name** is resolved beside it from the entry the handler themselves wrote, matched to that same handler's number (`actor_employee_number = assigned_material_handler`, newest first). Both request reads return it (`sp_waitlist_request_list`, `sp_waitlist_request_get` → `assigned_material_handler_name`), the request and the card row carry it (`AssignedMaterialHandlerName`), and `PersonText` reads the name and nothing else. A handler whose name no source records leaves the row **empty** rather than falling back to the number: an employee number is an identifier, not a name. **Also removes the request detail page's "Employee number" row**, which printed the requester's number directly beneath the row that already names them; a person is named by name wherever the app names one. **Left alone:** the sign-in page still offers each remembered person by their sign-in name (the account key a person types is the number), and the machine set-up panel still echoes the employee number it is configuring — both are identity surfaces, not places where the app names a person. | **Amends FR-022** (both person rows are named, with the no-number rule stated) and `data-model.md` §5. |

### Open questions

**None outstanding.** Every question raised during this pre-spec's review is settled and recorded as a decision above.



---

## 3. Field mapping — what each part of the target card is fed by

| Target element | Today's source | Change |
|---|---|---|
| Accent bar | `SampleOrder` → accent brush | unchanged |
| Picture + new-message badge | `Order.EffectiveImagePath`, `Order.HasNewMessages` | unchanged |
| Large value under the picture | requesting work centre | **new placement**, unlabelled |
| Row 1 — the status word | `Order.Status`, the handler status, `Order.IsOverdue` | plain word (D11, D25); precedence per D43 |
| Row 2 — the ring | *new* — `elapsed`, `allotted_minutes` | fills as time is **used**, carries the **time left**; **freezes** and carries the **time taken** once closed (D25, D28) |
| The column's theme | `RequestStatusPalette`, `Order.IsOverdue` | **three distinct themes**: normal, **handler-condition** (lighter red, D42) and **overdue** (D29) |
| Row 1 is interactive for the handler | *new* — the signed-in user against the row's material handler | clicking the status word opens the D42 flyout; plain text for everyone else |
| Row 3 — the person | `Order.RequestedBy` while unclaimed; the material handler once claimed | *new on the card*; removed when empty (D25, D27) |
| Row 4 — the requester | `Order.RequestedBy` | *new on the card* for closed requests; removed when empty (D27, D28) |
| Title and identifier | `Order.Title`, `Order.Subtitle` | promoted into the spanning top row of the grid (D12, D21) |
| Detail cells | *new on the card* — the Item's declared fields | new resolution path (§5); Die-only cell is D16, placement is D20/D21 |
| Row 1: Accept / Cancel / Give back | `Order.AcceptCommand` / `CancelCommand` / `ReleaseCommand` | restyled into a grid |
| Row 2: Complete / Details / ~~Print~~ | `Order.CompleteCommand` / **new** / *(no third cell — D59)* | one new navigation action, and no placeholder |
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

## 4. The action grid (3 columns × 2 rows, row-major)

| | Column 1 | Column 2 | Column 3 |
|---|---|---|---|
| **Row 1** | **Accept** — green check | **Cancel** — red X | **Give back** — amber pause |
| **Row 2** | **Complete** — blue box-with-check | **Open details** — magnifier | *(~~Print~~ — removed, D59)* |

Rules that carry over unchanged and must not be weakened:

* Each button is drawn only while the row's own gate says the action is available (`CanAccept`,
  `CanCompleteOrRelease`, `CanCancelRequest`, plus the requester's own cancel gate).
* An unavailable button is **removed**, and what remains **re-centres without stretching** (D30).
* Every button keeps a `ToolTipService.ToolTip` and an `AutomationProperties.Name` from the row's localized text.
* Minimum 44×44 hit target.
* Accept / Complete / Give back keep their three distinct colours and glyphs so they are never mistaken for one
  another.
* ~~**Print** is `IsEnabled="False"` with a "not available yet" tooltip and a code-behind `TODO` naming label printing.~~ **Superseded by D59:** the card draws no print control and carries no print TODO.
  Visible so the layout is honest about what is coming; inert so it cannot mislead.
* **Open details** replaces the card-body click. The picture keeps Shift+click enlarge; the badge keeps its meaning.

---

## 5. The detail cells — settled per Item

Every cell is the Item's own declared field (`waitlist_request_item_configs.detail_fields_json`) in declared order,
**minus** whatever D19 excludes. **Every cell is a label on top with its value beneath (D39)**, apart from the
label-less die-shop cell (D40), and the cells are laid into D21's grid. There are **no fallbacks**: nothing is
padded, and an Item with fewer cells simply has a shorter panel.

`cells` is how many the panel fills, which drives D20's geometry. ⚠ marks a cell that is **not** a declared field
of that Item or has no source today — all are listed in §5.2, and none may be invented (§7.2). `—` means the panel
collapses entirely.

### Pickup (11 Items)

| Item | Cells, in order | cells |
|---|---|---|
| `pickup-coil` | Coil number · Quantity in house · Coil description · Average coil weight | 4 |
| `pickup-die` | Die · **Die Location** *(plus, when the D23 checkbox is ticked, the label-less **`Take To Die Shop`** cell)* | 2–3 |
| `pickup-component` | Component · Total Qty In House ⚠ · Item Type (WIP or Finished Goods) ⚠ | 3 |
| `pickup-dunnage` | Dunnage part · **Home Location** | 2 |
| `pickup-scrap` | Scrap Type · Lugger Location ⚠ *(placeholder until a future patch)* | 2 |
| `pickup-hopper` | Size ⚠ *(D22)* | 1 |
| `pickup-fg` | Part Type *(the derived item type, D36)* · # of Skids | 2 |
| `pickup-ncm` | Part Type *(location-derived, D36)* · # of Skids | 2 |
| `pickup-wip` | Part Type *(the derived item type, D36)* · # of Skids | 2 |
| `pickup-outside-service` | Part Type *(the derived item type, D36)* · # of Skids | 2 |
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
| `assist-coil-turn` | Coil Number ⚠ · Weight of Coil ⚠ *(a **required** operator entry)* | 2 |
| `assist-table-place` | — panel collapses | 0 |
| `assist-table-remove` | — panel collapses | 0 |

### Other (1 Item)

| Item | Cells, in order | cells |
|---|---|---|
| `other` | Request description — drawn with **`AutoScrollTextView`** | 1 |

⚠ **Weight of Coil is not Pickup Coil's `Average coil weight`.** One is the average the receiving store's own
weighing history holds for the coil part (`mtm_receiving_application.receiving_history`, read through
`sp_receiving_history_average_coil_weight`); the other is the actual weight the operator must supply on an Assist
Coil Turn so a material handler can judge whether a larger forklift is needed. They must not be conflated.

### 5.1 New request-workflow requirements

Each of these adds an answer or a step to the New Request workflow, and each is in this spec (D35).

| Requirement | Item(s) | Shape |
|---|---|---|
| Hopper Size (D22) | `pickup-hopper`, `deliver-hopper` | three radios + a reference image of the selected hopper; the card's Size cell links to that image |
| Die shop direction (D23) | `pickup-die`, `deliver-die` | a checkbox — at the die shop, or send it there — feeding a conditional third cell |
| Fuzzy part entry (D24) | `deliver-wrong-coil`, `deliver-wrong-flatstock` | the incorrect part is typed through a fuzzy search modal, backed by a **live** Infor Visual read |
| Weight of Coil | `assist-coil-turn` | a **required** numeric entry |
| # of Skids (D34) | the four previously-hidden Items | a numeric entry, **defaulting to 1**, for how many skids must be picked up |

**The hopper images are held to the application's existing picture rules**, which is not a new rule but the shared
policy already used for press and part pictures:

| Rule | Value | Where it is defined |
|---|---|---|
| Shape | **square** — target aspect ratio `1.0`, tolerance ±2 % | `MTM_Waitlist.Shared/Helpers/ImagePicturePolicy.cs` |
| Minimum size | **48 px** a side (the app's own preview floor) | `ImagePicturePolicy.MinimumPixels` |
| Maximum size | **2048 px** a side | `ImageValidationRules.MaxDimensionPixels` |
| File types | `png`, `jpg`, `jpeg` | `ImageValidationRules.AllowedExtensions` |
| File size | between **1 KB** and **10 MB** | `ImageValidationRules.MinFileSizeBytes` / `MaxFileSizeBytes` |
| When nothing usable | draws `Assets/Placeholders/default-no-image.png` | `ImagePicturePolicy.NoImagePath` |

A size whose image is missing or unusable draws the shared placeholder rather than a blank, matching how every other
picture in the application behaves.

### 5.2 What the values resolve to today, and what is missing

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
| `Quantity in house`, `Quantity`, `Total Qty In House` | **no** | n/a | rules exist (D38) — each still needs its mechanism chosen |
| `Coil description`, `Average coil weight`, `Item Type` | **no** | n/a | data gap — `Item Type` follows §5.3 |
| `Home Location` | **no** | n/a | data gap |
| `Destination` | declared `fixed` | **no fixed resolver** | gap |
| `Part Type`, `Lugger Location`, `Size`, `# of Skids`, `Weight of Coil` | **no** | n/a | new answers (D22–D24, D34) |

**D32 is the answer to (A):** extend the resolver so the labels the snapshot already holds resolve. **(B) needs a
mechanism chosen per cell** — add the value to the snapshot, read it from Infor Visual, or drop the cell. Nothing
may be invented, and a cell whose source exists but cannot be read is hidden and logged (D41).

**Declared fields the cards must no longer show.** These exist in configuration but are excluded by D19 or have no
source, so the card change and the configuration now disagree. The spec must say what happens to each:

* `Requesting work center` / `Pickup work center` / `Work center` — declared by many Items, excluded by **D19**
  because the work centre is the left-column value.
* `Description` — declared by `pickup-dunnage` and `deliver-dunnage`, and **verified unfillable** on 2026-10-10:
  no dunnage table holds one (`dunnage_parts` carries `part_id`, `type_id`, `spec_values`, `home_location`;
  `dunnage_types` carries only `type_name` and `icon`). This is **broken configuration**, not a missing source.
* `Quantity` on the die Items, `Reason` on both wrong-part Items, and the declared fields of the four
  previously-hidden Items — declared but not part of their cards (D37).
* `Destination` on `deliver-flatstock`, `pickup-ncm` and `pickup-outside-service` — kept only for
  `deliver-flatstock`, but `research.md` records that `destination` left the token set on 2026-09-20, so its
  availability needs confirming.

### 5.3 Deriving the label type — FG, Outside Service, WIP — and why NCM is not one of them

Investigated 2026-10-10 against the Infor Visual schema export
(`Documents/Development/InforVisual/DatabaseCSVFiles`, 968 tables) and the existing repo code. **Outside Service
and WIP are derivable from the job's routing; Finished Goods needs a location; NCM cannot come from the routing at
all.**

| Label | Derivable from the routing? | The actual signal |
|---|---|---|
| **Outside Service** | **Yes — from the sequence.** | the `OPERATION` row at that `SEQUENCE_NO` has `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID` populated (or `RESOURCE_ID = 'OUTSIDE_SERVICE'`). |
| **WIP** | **Yes, by position.** | a step earlier than the routing's last one, with open quantity on the order (`DESIRED_QTY − RECEIVED_QTY > 0`) or an open status (`R`, `U`, `F`). |
| **Finished Goods** | **No — routing position is necessary but not sufficient.** | the part must actually be **sitting at a finished-goods location**: `LOCATION.TYPE` / `LOCATION.VENDOR_ID` via `PART_LOCATION`, or the WIP app's `md_locations` taxonomy. |
| **NCM** | **No — never.** | it is a **state of the stock's location, not a production type.** |

**NCM specifically.** NCM is not a Part Type and no routing field carries it. The only thing that says a part is
NCM is **where it is**: in the WIP application, `md_locations.Location LIKE 'NCM%'` (`NCM`, `NCM - VITS`,
`NCM-VITS`, `NCM/NA`) — and **`inv_inventory.ItemType` still reads `'WIP'` while the part sits at an NCM
location**, so `ItemType` must never be used for this. In Infor Visual the same idea lives in
`PART_LOCATION.STATUS` / `HOLD_REASON_ID` against the `LOCATION` occupied.

**What already exists** — this is not new ground. `RequestDispositionClassifier.cs` derives FG / WIP / Outside
Service with the documented priority *Outside Service → Finished Goods → WIP* from a pure `DispositionInput`;
`GetDispositionInput.sql` (live-validated) supplies that input and `GetWipFloorQuantities.sql` supplies the floor
cross-check. `RequestDispositionResolver` is registered in DI at `ServiceRegistrationExtensions.cs:127` and **has
no consumer outside its own tests** — the derivation exists but is not wired to anything.

**A defect found while checking this, which matters for the `Item Type` cell.** `GetDispositionInput.sql` passes
**`PART.QTY_ON_HAND`** as `FinishedGoodsQuantity`, but that is the part's **total on-hand across every location** —
the query applies **no location filter at all**, while the classifier documents the rule as "on-hand > 0 **at a
finished-goods / shipping location**". So any stock anywhere, plus a closed order with nothing open, reads as
**Finished Goods** today. The location-filtered source it should use is `PART_LOCATION` joined to `LOCATION` (or
the WIP floor buckets). **This is worth correcting independently of the card work.**

**One limitation.** The CSV export is **schema only** — `ColumnDetails`, `Tables`, `CheckConstraints`, `Indexes`
and `TableRowCounts`, with **no data rows and no enum values**. It confirms that `OPERATION.SEQUENCE_NO` and
`PART_LOCATION.QTY`/`STATUS` exist and are usable, but it **cannot** confirm which location codes this installation
uses for FG or NCM. Those come from live queries and are recorded in the two SQL scripts and in
`.github/memories/repo/infor-visual-disposition.md`. Validate any new derivation against the live database.

### 5.4 The four previously-hidden Items — audit result (2026-10-10)

D34 brings these into scope, so the review that was asked for matters. **Nothing is half built:**

* **The hide is a hard-coded list.** `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs:24-31` holds
  `s_outOfScopeItemIds = ["pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service"]`; `IsVisible` returns
  `false` for them at lines 95-98. There is no `is_in_scope` column, so unhiding is **code plus real job gates**,
  not a data change.
* **They are deferred in writing.** `specs/004-unified-card-item-picker/spec.md:482` — *"The four Items with no
  value source are out of scope and are hidden until their own work lands"*; `data-model.md:55-56` — *"Out of
  scope, catalogued but never offered … excluded by the visibility rules, not by deletion (FR-028)"*. T029 and
  SC-014 say the same.
* **What exists:** catalogue rows, display names (`Strings/en-us/Resources.resw:852-862`), the hide rule and the
  test pinning it (`RequestItemPickerRulesTests.cs:110-143`), seed rows and demo requests. **No** view, view model,
  wizard step, stored procedure or contract constant is dedicated to any of the four.
* **A real but unconnected stack exists** for their subject matter — `RequestDisposition`,
  `RequestDispositionResolver`, `WipFloorInventoryService` and friends. It is registered at
  `ServiceRegistrationExtensions.cs:127` and **has no consumer outside its own tests**.

---

## 6. What this reverses, and therefore must be stated in the spec

| Earlier decision | Where | What this change does to it |
|---|---|---|
| "Declared fields appear on the request page and never on the card" | spec 004 **FR-007** | **Formally superseded.** Item fields appear on the card. |
| The card's anatomy is frozen | spec 004 `contracts/card-and-identifier.md`, and the freeze spec 004 itself applied to spec 003 | **The freeze is lifted in writing**, following the same precedent spec 004 used when it unfroze spec 003's per-type card anatomy. |
| "The detail-field grid is **not** on the card" | `specs/004-unified-card-item-picker/data-model.md` §7 | **Reversed.** The Item's declared fields return to the card. |
| "The card's four metadata rows are requested by · press · remaining time · waiting" | `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` | **Superseded** by the new zone anatomy. |
| "The card body is the click target that opens the request" | spec 004 / 009 A3, and the closed defect `Closed-Medium-Waitlist-CardAndFlyoutPictureTookTheCardsClick.md` | **Reversed.** A dedicated button opens the request; the body no longer does. Shift+click enlarge survives, and the status flyout (D42) is the one new interactive element in the body. |
| An unresolvable value must **say why** rather than render blank | spec 004 **FR-026** | **Superseded** (D41). Both unresolvable cases **hide the cell**; the case where the source exists but cannot be read also **writes a warning to the database log**. |
| The status vocabulary is closed at four | `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` | **Opened** to **eight** states in total — the four lifecycle words plus the four handler-set statuses of D26 — and the palette must carry **three themes** (D29, D42). |

The spec must call these out explicitly. A silent contradiction of a ratified contract is not reviewable, and these
are cargo-cult risks the moment a later reader finds the old text.

---

## 7. Invariants the change must not break

1. **Overdue stays visible under every sort.** Spec 004 FR-012 requires it. D25 removed the text that carried the
   marking; **D29 replaces it** — row 1 reads `Overdue` and the whole status column re-themes, so the signal does
   not depend on the row's position. Where a handler status is set, that status and its lighter-red theme take
   precedence (D43), and the request still remains visible as a distinct state.
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
5. **Localization.** Every new label — the ring's accessible name, `Open details`, the new cell
   labels, the die-shop phrases, the four handler statuses and the three status words — is a resource key in
   `Strings/en-us/Resources.resw`, never a literal.
6. **Accessibility.** The ring must expose a text equivalent saying what it measures **and which of the two things
   it is measuring** (time left while live, time taken once closed), since D28 makes it mean different things. The
   status flyout must be reachable and operable by keyboard, and its gate must be announced rather than leaving a
   handler to discover that the text is sometimes inert.
7. **Only the row's own handler can set a status.** The flyout gate is the signed-in user against that row's
   material handler, and no other user may change a request's handler status (D42).
8. **Animations are optional.** Any animation the new controls add must respect the system's animations-off
   setting, as `AutoScrollTextView` already does.
9. **The card must remain readable at the app's default window size** (D14).

---

## 8. Artifacts the implementation will touch (for the plan, not the spec)

* `Module_Waitlist/Controls/WaitlistLineCardView.xaml` and `.xaml.cs` — the card, now **pulling a detail control
  per card** (D31). No print `TODO`: the card carries no print action (D59).
* **The detail controls, one per card, all in a single file** (D31) — the arrangement that keeps column 3 out of
  the card's own XAML without adding a file per card. Any other section that grows complex may also move into a
  control of its own.
* `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs` — **the single most important file in this change.**
  It maps four labels and returns `null` for every other one, so most of §5's cells resolve to nothing (D32, §5.2).
* `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` (+ `RequestDunnagePart`, `RequestDiePart`) — the
  snapshot the resolver reads. Every **data gap** in §5.2 must be added here, read from Infor Visual, or dropped.
* `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` — the hard-coded out-of-scope list is removed and real
  job gates added (D34, §5.4).
* `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` — gains roles and colours for the four handler-set
  statuses (D26) and **three column themes** — normal, handler-condition (lighter red) and overdue (D29, D42).
* The **status flyout** (D42) — the first interactive element inside the card body, gated on the signed-in user
  being that row's material handler. It also needs the storage for a handler-set status and the read that restores
  it (D26).
* `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` and `ViewModels/WaitlistViewViewModel.cs` — the ring inputs,
  the status rows, the handler-status state, and resolving the Item's declared fields for each row.
* `Module_Shared/Controls/AutoScrollTextView.xaml` — reused for the `other` card's description.
* A **new WinUI fuzzy part picker** with a live Infor Visual read behind it (D24) — the receiving application's
  equivalent is WinForms and cannot be reused verbatim. The natural home for the query is beside
  `Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql`.
* The **New Request wizard** pages for §5.1's five new requirements.
* `Database/Seeds/seed_waitlist_request_item_configs/` **and a migration** — the `Die Location` / `Home Location`
  label correction (D33).
* `Strings/en-us/Resources.resw` — the new labels, the die-shop phrases and the status words.
* `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` — currently asserts the labels
  `Requested by`, `Press`, `Remaining time`, `Waiting`, and two tests that pinned the old card were already removed
  on 2026-10-10. **The new spec owns rewriting this file** to the new anatomy.
* `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` — superseded; the new spec's contract
  becomes the live one.

**Note for whoever writes the spec**: the reference image is the only visual description, and it is **out of date in
three places** — the ring's contents (D25), the status column's rows (D25, D28, D42) and the panel's earlier
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
* A handler status, when set, shows on row 1 in the lighter-red theme; an overdue request is unmistakable under
  every sort; the two are never confusable and never both shown (D29, D42, D43).
* Only the row's own material handler can open the status flyout; for everyone else the status word is inert text.
* Unavailable action buttons are absent, and the remaining grid is centred rather than stretched (D30).
* The card remains usable with the system's animations switched off, and the flyout is keyboard-operable.
* No card shows a value the request does not carry, and a cell whose source cannot be read is hidden and logged
  rather than explained on the card (D41).
* ~~Printing is visibly unavailable and says so when pointed at~~ **Superseded by D59:** the card offers only actions that do something, so nothing on it reads as broken.
* Opening a request is a deliberate, separate action from clicking anywhere on its row.

---

## 10. Out of scope

* The waitlist **detail page** layout (D1).
* Any change to the four **lifecycle** status words themselves. The four **handler-set** statuses of D26 are an
  **addition** to the vocabulary, not a change to it.
* Any **new real action** beyond Open details and the status flyout. ~~Print is a disabled placeholder (D4)~~ **Superseded by D59:** printing is not on the card at all and belongs to the detail page later.
* **Setup, Settings, and the Mock/visual-fallback surfaces.**
* Any change to the **mirror/fallback** model. The fuzzy part search is deliberately **live-only** and must not be
  given a cached read shape (D24).

**Scope note.** An earlier revision deferred the handler-set statuses and their flyout. **That deferral is
withdrawn: everything raised during this review belongs to this spec** (D35). Nothing discussed here is waiting on
another piece of work.
