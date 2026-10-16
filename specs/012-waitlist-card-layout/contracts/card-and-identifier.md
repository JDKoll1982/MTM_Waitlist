# Contract - The waitlist line card and its identifiers

The live card contract for `012-waitlist-card-layout`. This file **supersedes** the anatomy contract at
`specs/004-unified-card-item-picker/contracts/card-and-identifier.md`. Every stored value, identifier, label, phrase
and number below is copied verbatim from `spec.md` (Functional Requirements and Verbatim Constraints). A consumer or
a test codes against the names here, not against the superseded file.

## 1. The four zones, in order

A row is one card drawn as four zones, left to right, separated by vertical rules (FR-001). The zone names are the
spec's own; the third zone is the detail zone that holds the detail grid.

| Order | Zone | Carries |
|---|---|---|
| 1 | `identity` | the material picture, the new-message badge, the requesting work centre |
| 2 | `status` | the status word, the time ring, the person, and for a closed request the requester |
| 3 | `identity-and-detail` | the category word, the request's own identifier, and the Item-driven detail grid |
| 4 | `action` | up to six square action buttons |

Rules that bind the zones: nothing is padded to fill a short panel and no uniform card height is imposed (FR-006). A
card's height may differ between Items but must be constant for any one Item (FR-009). A row stretches the full
width and remains readable at the application's default window size (FR-010).

## 2. The identity zone

- The accent bar, the material picture and the new-message badge keep their present meaning (FR-002).
- Shift+click enlarging of the picture and the badge's meaning stay (FR-003).
- The requesting work centre is drawn as a **large unlabelled value beneath the picture** (FR-004).
- The requesting work centre and the requesting user are **never** detail cells, and the requesting user appears only
  on the request's own detail page (FR-005).
- A request with no picture and no new-message badge draws the placeholder state and omits the badge.

## 3. The status zone

A centred stack of rows in this order, with a row that has no value removed rather than left blank (FR-011, FR-018):

| Row | Contents |
|---|---|
| 1 | the status word in plain coloured text, with no label in front of it and no coloured pill (FR-008, FR-012) |
| 2 | the time ring, carrying the time left while the request is live (FR-013) |
| 3 | the person: the requester while unclaimed, the material handler once claimed (FR-022) |
| 4 | for a closed request only, the requester (FR-011, FR-022) |

A closed request freezes the ring at the moment it closed and carries the time taken instead of the time left
(FR-015). The ring must state which of the two things it measures, and its accessible name must say the same
(FR-016). The ring re-evaluates on the same one-minute tick that refreshes the row's other time text (FR-017). The
status zone shows no percentage and no elapsed-time line (FR-021).

The zone is drawn as an inset panel — a themed border and fill around the stack — so the column reads as its own
surface against the card rather than a band of bare text. Both come from theme resources, so the panel follows the
theme. The fill is deliberately neutral and never carries the state: the word and the ring are still the only things
that say which state the row is in (FR-019), and the word still draws no pill of its own (FR-008).

A cancelled request with no material handler reads the person who cancelled in row 3, with the requester in row 4.

The status word, its precedence and the three column themes are pinned in
`contracts/status-and-conditions.md`.

## 4. The detail zone

An inner grid of three rows and two columns (FR-024). Row 1 always spans both columns and holds **the category word
and the request's own identifier** (FR-007). The detail cells occupy rows 2 and 3.

The cells a card shows are determined by the request's Item and are never chosen per row (FR-025). Every cell is a
label on top with its value beneath, apart from the label-less die-shop cell (FR-028).

**Geometry by the number of cells the Item declares** (FR-026, FR-027):

| Cells declared | Geometry |
|---|---|
| 4 | two rows of two |
| 3 | cells 1 and 2 share the top row, cell 3 spans the bottom row |
| 2 | the bottom row is collapsed |
| 1 | the single cell spans the full width |
| 0 | both lower rows are removed and the title row takes the vacated space as one full-width row |

Two requests of the same Item render identically in shape and height (FR-103). `Die Location` appears on the two die
Items and on no other Item (FR-029), and is read from configuration together with `Home Location` rather than being
overridden (FR-059).

A cell the Item never declares is hidden; a cell whose source exists but cannot be read is hidden **and** a warning is
written to the database log (FR-058). Neither case is explained on the card and neither is filled with a substitute
(FR-060).

## 5. The action zone

A grid of up to six square buttons in **two columns by three rows, in row-major order** (FR-061):

| Position | Button label |
|---|---|
| row 1, column 1 | `Accept` |
| row 1, column 2 | `Cancel` |
| row 2, column 1 | `Give back` |
| row 2, column 2 | `Complete` |
| row 3, column 1 | `Open details` |
| row 3, column 2 | `Print` |

- A button is drawn only while the row's own gate says the action is available, and no action becomes available or
  unavailable because the layout changed (FR-062).
- An unavailable button is **removed** rather than blanked, and the remaining buttons re-centre in the action zone
  without stretching (FR-063).
- Every button keeps a tooltip and an accessible name, both taken from the row's localized text (FR-064).
- Every button keeps a hit target of at least **44 by 44** (FR-065).
- `Accept`, `Complete` and `Give back` keep distinct colours and glyphs so they are never mistaken for one another
  (FR-066).
- `Print` ships disabled, says **"not available yet"** when pointed at, and performs no action (FR-067).
- `Open details` is a button carrying a magnifier glyph and the tooltip `Open details`, and it is the only way the
  card opens the request (FR-068). The card body no longer opens the request (FR-069).

**The button names and the button text are not the same thing.** `Accept`, `Cancel`, `Give back`, `Complete`,
`Open details` and `Print` are the names the specification gives these six buttons (FR-061). The text a button
actually draws, its tooltip and its accessible name all come from the row's own localized action text, which is
`Accept request`, `Cancel request`, `Give back`, `Complete request`, `Open details` and `Print`
(`Waitlist_Action.AcceptRequest`, `Waitlist_Action.CancelRequest`, `Waitlist_Action.ReleaseRequest`,
`Waitlist_Action.CompleteRequest`, `Waitlist_Card_OpenDetails.Text`, `Waitlist_Card_Print.Text`). Only
`Give back`, `Open details` and `Print` are literal strings; the other three differ from the button's name. A test
asserting a button's text must assert the resource value, not the name in the table above.

## 6. The answers the card exposes

- The label-less die-shop cell carries no label and its value is the phrase itself: **`Take To Die Shop`** on a
  pickup and **`Located In Die Shop`** on a deliver, drawn only when the die-shop answer was given (FR-055). When the
  checkbox was not ticked the cell is absent rather than empty.
- The hopper Items show a `Size` cell chosen from **`Small`**, **`Medium`**, **`Large`**, and the Size cell opens the
  chosen size's reference image in the same way the card's other pictures enlarge (FR-036 from the hopper Items,
  FR-077, FR-078).
- A hopper image is held to the application's existing picture rules: square within a tolerance of **two per cent**,
  at least **48** and at most **2048** pixels a side, a permitted file type (**`png`**, **`jpg`**, **`jpeg`**), and a
  size between **1 KB and 10 MB**; a missing or unusable image draws the shared placeholder (FR-079, FR-080).
- The four returning Items ask how many skids must be picked up, a numeric answer **defaulting to 1** (FR-087).
- `Weight of Coil` is the actual weight the operator supplies on an assist coil turn and is never conflated with the
  job's average coil weight (FR-054), which is read live from the receiving store's `receiving_history` through
  `sp_receiving_history_average_coil_weight` via `IAverageCoilWeightService` and never from Infor Visual's per-part
  `PART.WEIGHT`.
- An Item with no configured allotment uses the default allotment of **15 minutes** (FR-017 context, Assumptions).

**The fuzzy incorrect-part entry** (the two wrong-part Items, FR-082, FR-083). The typed term is upper-cased and
zero-padded to ten characters when it starts with a known prefix:

| Prefix | For | Example |
|---|---|---|
| `MMC` | coil | `MMC1000` becomes `MMC0001000` |
| `MMF` | flatstock | `MMF500` becomes `MMF0000500` |
| `FGT` | die | a ten-character zero-padded number |

A term that matches a part exactly resolves at once; a term that matches none exactly offers a list of every part
whose number contains the term; a term with no known prefix is searched as typed; an unreachable plant system tells
the operator the search is unavailable and the typed value stands as entered.

## 7. Identifiers a consumer or test codes against

**The Item ids (24).** `pickup-coil`, `pickup-die`, `pickup-component`, `pickup-dunnage`, `pickup-scrap`,
`pickup-hopper`, `pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service`, `pickup-riser-table`,
`deliver-coil`, `deliver-flatstock`, `deliver-component`, `deliver-die`, `deliver-dunnage`, `deliver-wrong-coil`,
`deliver-wrong-flatstock`, `deliver-riser-table`, `deliver-hopper`, `assist-coil-turn`, `assist-table-place`,
`assist-table-remove`, `other`.

The four returning Items are `pickup-fg`, `pickup-ncm`, `pickup-wip` and `pickup-outside-service`; each shows
`Part Type` and `# of Skids` and no location cell (FR-036 to FR-039).

**The declared field labels, per Item, in declared order** (FR-030 to FR-053). These are the labels the Item
declares and the cells its card is built from; a cell whose source resolves nothing is absent rather than drawn, and
the four labels in that state today are listed below the table.

| Item | Cells |
|---|---|
| `pickup-coil`, `deliver-coil` | `Coil number`, `Quantity in house`, `Coil description`, `Average coil weight` |
| `pickup-die`, `deliver-die` | `Die`, `Die Location`, plus the label-less die-shop cell when the answer calls for it |
| `pickup-component`, `deliver-component` | `Component`, `Total Qty In House`, `Item Type` (WIP or Finished Goods) |
| `pickup-dunnage`, `deliver-dunnage` | `Dunnage part`, `Home Location` |
| `pickup-scrap` | `Scrap Type`, `Lugger Location` |
| `pickup-hopper`, `deliver-hopper` | `Size` |
| `pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service` | `Part Type`, `# of Skids` |
| `deliver-flatstock` | `Part`, `Quantity`, `Destination` |
| `deliver-wrong-coil` | `Coil number` as the incorrect coil, `Correct coil` |
| `deliver-wrong-flatstock` | `Part` as the incorrect flatstock, `Correct flatstock` |
| `assist-coil-turn` | `Coil Number`, `Weight of Coil` |
| `pickup-riser-table`, `deliver-riser-table`, `assist-table-place`, `assist-table-remove` | no detail cells, the panel collapses |
| `other` | `Request description`, drawn in full and scrolling rather than clipped when long |

`Destination` is shown only on `deliver-flatstock` and never on `pickup-ncm` or `pickup-outside-service` (FR-057).

**Declared fields that must not be shown** (FR-056): the requesting, pickup and work-centre fields; `Description` on
the dunnage Items; `Quantity` on the die Items; `Reason` on both wrong-part Items; and the declared fields of the
four returning Items other than `Part Type` and `# of Skids`.

**Button names and messages:** `Accept`, `Cancel`, `Give back`, `Complete`, `Open details`, `Print` — the
specification's names for the six buttons, whose drawn text is the row's localized action text (§5); the Print
message `not available yet`; the tooltip `Open details`.

**Declared fields whose source does not exist yet** (FR-058, FR-060). Four labels the seed declares resolve to
nothing today, so their cell is **hidden** rather than filled — and each stays declared so the cell appears with no
card change the day a source lands:

| Label | Declared by | Why it is hidden today |
|---|---|---|
| `Quantity in house` | `pickup-coil`, `deliver-coil` | no resolver arm reads a quantity; the `part_warehouse_quantities` read shape is registered and mirrorable but no card cell consumes it |
| `Quantity` | `deliver-flatstock` | the same absent quantity source |
| `Total Qty In House` | `pickup-component`, `deliver-component` | the same absent quantity source (FR-090 records the finished-goods rule as unsettled) |
| `Lugger Location` | `pickup-scrap` | no resolver arm exists for it, and none is added by this work |

A reader must not take any of the four as "declared and therefore drawn": the delivered behaviour is that a cell
with no source is absent, and the exception above is the complete set of the declarations in that state.

**Every other new label is a localized resource rather than a literal** (FR-104): the ring's accessible name
(`Waitlist_Card_Ring.TimeLeft`, `Waitlist_Card_Ring.TimeTaken`), `Open details` (`Waitlist_Card_OpenDetails.Text`),
the Print message (`Waitlist_Card_Print.Unavailable`), the new cell labels (under keys that **are** the declared
labels — `Die Location`, `Home Location`, `Scrap Type`, `Coil Number`, `Size`, `# of Skids`, `Weight of Coil`,
`Die Shop Direction`, `Part Type`, `Item Type`, `Total Qty In House`), the die-shop phrases
(`Waitlist_Card_DieShop.TakeToDieShop`, `Waitlist_Card_DieShop.LocatedInDieShop`), the flyout heading
(`Waitlist_Card_StatusFlyout.Heading`) and the status word's help text (`Waitlist_Card_StatusWord.SetCondition`).

**The eight status words and the four conditions are deliberately NOT localized, and FR-104 is therefore met in
part.** `Waiting`, `In Progress`, `Done`, `Cancelled`, `Overdue` and the four conditions are **the store's own
vocabulary**: they are compared **ordinally and exactly** against a stored value
(`HandlerStatusCatalog.IsCondition`, `SampleOrder.LifecycleWordFor`), written verbatim, and pinned verbatim by the
tests that also reject a differently cased or padded variant. Making them resource keys would make a display string
decide a comparison — the exact inversion FR-012 and FR-102 forbid — so they stay literals by decision, not by
oversight. FR-104's requirement that labels be localized applies to the list above it; it must not be read as
covering the eight words, and this contract must not claim that it does.

**Where a declared field's label becomes a resource key.** Because the card resolves a cell's label at bind time
from the declaration, a label's resource key **is** the declared label. Renaming such a key breaks the
declaration.

## 8. The lift of the anatomy freeze

The card's anatomy is **no longer frozen**, and this lift is recorded here so a later reader cannot take the old
freeze as licence (FR-097). The freeze lived in `specs/004-unified-card-item-picker/contracts/card-and-identifier.md`;
these rows of it are superseded by the four-zone anatomy (FR-096 to FR-102):

| Superseded row | Superseded by |
|---|---|
| the four metadata rows: requested by, press, remaining time, waiting | the four zones, with the person rows in the status zone and the work centre in the identity zone (FR-099) |
| the compact status pill and its four-word vocabulary | plain coloured status text with no pill, and a vocabulary of eight states (FR-008, FR-102) |
| the rule that an Item's declared fields appear on the request page and never on the card | an Item's declared fields appear on the card (FR-096) |
| the removal of the per-type detail grids from the card | the detail-field grid returns to the card (FR-098) |
| the card body as the click target that opens the request | `Open details` is the only way the card opens the request (FR-100) |
| the rule that an unresolvable value must say why rather than render blank | an unresolvable value is hidden, and where its source exists but cannot be read it is also logged (FR-101) |

The four lifecycle words themselves are unchanged by this lift; the four handler conditions are an addition to the
status vocabulary.
