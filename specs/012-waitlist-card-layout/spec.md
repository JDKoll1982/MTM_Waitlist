# Feature Specification: Waitlist Line Card Layout

**Feature Branch**: `012-waitlist-card-layout`
**Created**: 2026-10-10
**Status**: Draft
**Input**: Redesign the waitlist list row into four zones fed by the request's own Item: an identity zone (picture, badge, requesting work centre), a status zone (status word, time ring, person), an Item-driven detail grid, and an action grid that re-centres as buttons become unavailable. Requirements source: the settled pre-specification `docs/research/research-waitlist-card-layout-pre-spec.md`, whose 39 decisions (D1 to D43, with deliberate gaps at D8, D9, D10 and D17) carry no open questions. Section numbers below refer to that document.

## User Scenarios & Testing

### User Story 1 - A row that can be read at a glance (Priority: P1)

A material handler scans the waitlist. Each row now reads left to right as four zones: who and what (the material picture with its new-message badge, the requesting work centre as a large unlabelled value beneath it), where it is (the status word, the time ring, the person), what it wants (the request's category word and its own identifier across the top of an inner grid, with the Item's own values beneath), and what can be done about it (up to six square action buttons that re-centre as buttons become unavailable).

**Why this priority**: every other slice hangs off this arrangement, and it is the piece that makes the list readable without opening anything.

**Independent Test**: with one live request in the list, confirm the four zones appear in order and are separated by vertical rules, that the work centre reads as a large unlabelled value under the picture, that the category word and identifier span the top of the detail grid, and that clicking the card body no longer opens the request while the details button does.

**Acceptance Scenarios**:

1. **Given** a row in the waitlist, **When** it is drawn, **Then** the identity, status, detail and action zones appear left to right, separated by vertical rules.
2. **Given** a row, **When** it is drawn, **Then** the requesting work centre appears as a large unlabelled value beneath the picture.
3. **Given** a request, **When** its card is drawn, **Then** the category word and the request's own identifier occupy the full-width top row of the detail grid.
4. **Given** a row, **When** the card body is clicked, **Then** no request opens; **When** the details button is clicked, **Then** the request opens.
5. **Given** a handler who holds shift and clicks the picture, **When** the click lands, **Then** the picture enlarges, exactly as before.
6. **Given** a row awaiting a claim, **When** it is drawn, **Then** the Accept, Cancel and Give back buttons appear in the first grid row and Complete and Open details in the second. [Amended 2026-10-11, owner direction (D59): Print was the second row's third button and is no longer an action of the card.]

### User Story 2 - The status column tells the truth about a request (Priority: P1)

The status zone is a centred stack. Row 1 is the status word in plain coloured text, with the old coloured pill gone. Row 2 is a ring that fills as the allotted time is used and carries the time left inside it. Row 3 is the requester while the request is unclaimed and the material handler once it is claimed. A closed request freezes the ring and carries the time taken, moving the requester to a fourth row. Row 1 reads a handler-reported condition when one is set, otherwise an overdue marking, otherwise the lifecycle word, and the whole column changes colour theme with that choice.

**Why this priority**: this is how a handler knows whether to walk over now or later, and it is where the previous card's remaining-time text was removed, so leaving it out would lose the overdue signal the list already guarantees.

**Independent Test**: put one request live, one overdue and one completed in the list and confirm the word, the ring's contents, the person row and the column theme for each, then set a handler condition on the live one and confirm row 1 and the theme change.

**Acceptance Scenarios**:

1. **Given** a live request nobody has claimed, **When** it is drawn, **Then** row 1 reads `Waiting`, row 2 shows the ring filling with the time left inside it, and row 3 reads the requester.
2. **Given** a live request a handler has claimed, **When** it is drawn, **Then** row 3 reads the material handler.
3. **Given** an overdue request with no handler condition, **When** it is drawn, **Then** row 1 reads `Overdue` and the ring shows the time overrun.
4. **Given** a completed request, **When** it is drawn, **Then** the ring is frozen at the moment it closed and carries the time taken, row 3 reads the material handler, and row 4 reads the requester.
5. **Given** a cancelled request, **When** it is drawn, **Then** the person row reads the material handler or the person who cancelled, and the requester takes the fourth row.
6. **Given** a request whose handler has set a condition, **When** it is drawn, **Then** row 1 reads that condition and the column takes the lighter red theme.
7. **Given** a request that is both overdue and carrying a handler condition, **When** it is drawn, **Then** only the condition is shown and the column is not themed as overdue.
8. **Given** an unclaimed request with no handler and no condition, **When** it is drawn, **Then** the empty person row is absent rather than blank.
9. **Given** the ring, **When** a screen reader reads it, **Then** it announces what is measured and whether that is the time left or the time taken.

### User Story 3 - Every Item shows its own cells, in the same places (Priority: P1)

The detail grid is determined by the request's Item, never chosen per row. An Item with four cells fills a two-by-two grid, one with three lets the third span the bottom row, one with two collapses the bottom row, one with one spans the full width, and one with none removes both lower rows so the title row takes the space. Two requests of the same Item always look identical; two different Items never swap a cell's meaning.

**Why this priority**: it is what makes the card useful rather than decorative, and it is the slice that lets a handler read the values the request is about without opening it.

**Independent Test**: draw one request of each cell count (four, three, two, one and none) and confirm the geometry, the title row's span, the label-above-value anatomy, and that nothing is padded to fill a short panel.

**Acceptance Scenarios**:

1. **Given** an Item declaring four cells, **When** its card is drawn, **Then** the cells fill two rows of two.
2. **Given** an Item declaring three cells, **When** its card is drawn, **Then** cells one and two share the top row and cell three spans the bottom row.
3. **Given** an Item declaring two cells, **When** its card is drawn, **Then** the bottom row is collapsed.
4. **Given** an Item declaring one cell, **When** its card is drawn, **Then** that cell spans the full width.
5. **Given** an Item declaring no cells, **When** its card is drawn, **Then** both lower rows are absent and the title row occupies the vacated space as one full-width row.
6. **Given** any cell, **When** it is drawn, **Then** its label and its value read on one line, the label small and secondary, the value larger and semibold, with a real gap between them.
7. **Given** two requests of the same Item, **When** both are drawn, **Then** their panels have the same shape and the same height.
8. **Given** a die request and a coil request, **When** both are drawn, **Then** `Die Location` appears only on the die request.
9. **Given** a cell whose source cannot be read, **When** the card is drawn, **Then** the cell is absent and a warning is written to the database log.

### User Story 4 - A handler reports a condition from the card (Priority: P2)

When the signed-in person is the material handler working on that request, the status word becomes clickable. Clicking it opens a flyout offering four conditions: `Product Buried`, `Cant Find Product`, `Changing Forklift` and `Changing Propane`. Choosing one re-themes the whole status column to a lighter red than the overdue treatment and is stored against the request, so it comes back when the list is next read. For everyone else the word stays inert text.

**Why this priority**: without it a handler stuck behind a buried product has no way to say so on the card, and the four words are an addition to the vocabulary rather than a change to the lifecycle.

**Independent Test**: sign in as the handler working a specific request, open the flyout, choose a condition, confirm the column re-themes and the condition survives a list reload, then sign in as another person and confirm the word is inert.

**Acceptance Scenarios**:

1. **Given** the signed-in person is the material handler on that request, **When** they click the status word, **Then** a flyout offering the four conditions opens.
2. **Given** the flyout, **When** a condition is chosen, **Then** the whole status column takes the lighter red handler-condition theme.
3. **Given** a chosen condition, **When** the list is read again, **Then** the condition is restored on that request's card.
4. **Given** the signed-in person is not that request's material handler, **When** they point at the status word, **Then** it is inert and no flyout opens.
5. **Given** a request the signed-in handler is not working, **When** it is drawn, **Then** its status word is inert for them.
6. **Given** a keyboard-only handler, **When** they reach the status word, **Then** they can open the flyout and choose a condition without a pointer.
7. **Given** the flyout's gate, **When** the status word is drawn, **Then** whether it is operable is announced rather than left to be discovered.

### User Story 5 - The four previously hidden Items come back (Priority: P2)

Four catalogued Items (finished goods, NCM, work in progress, and outside service) were hidden by a fixed rule because they had no value source. Their values now come from the requesting job and, where the job cannot carry them, from the plant system. Each shows a Part Type and a count of skids, no location field at all, and each is offered only when the requesting job has the work it names.

**Why this priority**: they are catalogued and named in the interface already, so they are visible work waiting to land rather than new scope, and their absence silently withholds four supported Items.

**Independent Test**: for a job with each kind of work, confirm the matching Item is offered, that its card shows Part Type and the skid count, and that no location cell appears.

**Acceptance Scenarios**:

1. **Given** a requesting job with finished-goods work, **When** the picker is shown, **Then** the finished-goods pickup is offered.
2. **Given** one of the four Items, **When** its card is drawn, **Then** it shows Part Type and # of Skids and no location cell.
3. **Given** a stock whose work is subcontracted, **When** Part Type is derived, **Then** it reads `Outside Service`.
4. **Given** a part sitting at an NCM location, **When** Part Type is derived, **Then** it reads the location-derived NCM state rather than the part's recorded item type.
5. **Given** a requesting job with none of that work, **When** the picker is shown, **Then** the matching Item is not offered.

### User Story 6 - The New Request workflow asks the new questions (Priority: P2)

Five answers join the New Request workflow, and each appears on the matching card: a hopper size chosen from three options with a reference image the card can open, a die-shop direction on the die Items that decides whether a third cell appears, a fuzzy part search for the incorrect part on the two wrong-part Items, a required weight of coil on an assist coil turn, and a skid count on the four returning Items.

**Why this priority**: the cards cannot show what was never asked, and the pre-spec settles that nothing raised in review is deferred.

**Independent Test**: raise one request per new answer, then confirm each value appears in the right cell of the matching card, including the conditional die-shop cell and the hopper image link.

**Acceptance Scenarios**:

1. **Given** a hopper request, **When** it is raised, **Then** the size is chosen from Small, Medium and Large and the chosen size's reference image is shown beneath the choices.
2. **Given** a hopper card, **When** its Size cell is opened, **Then** that reference image opens in the same way the card's other pictures enlarge.
3. **Given** a hopper size whose image is missing or unusable, **When** the card is drawn, **Then** the shared placeholder is drawn rather than a blank.
4. **Given** a die request, **When** it is raised, **Then** a checkbox states whether the die is at the die shop or must be sent there.
5. **Given** that checkbox was ticked, **When** the card is drawn, **Then** a third, label-less cell reads `Take To Die Shop` on a pickup and `Located In Die Shop` on a deliver.
6. **Given** that checkbox was not ticked, **When** the card is drawn, **Then** the third cell is absent rather than empty.
7. **Given** a wrong-coil or wrong-flatstock request, **When** the incorrect part is typed, **Then** the entry runs through a fuzzy search that upper-cases the term and zero-pads it to ten characters when it starts with a known prefix.
8. **Given** a typed term that matches a part exactly, **When** the search runs, **Then** the part resolves at once; **Given** a term that matches none exactly, **Then** a list of every part whose number contains the term is offered.
9. **Given** the plant system cannot be reached, **When** the fuzzy search runs, **Then** the operator is told the search is unavailable and the value they typed is used as entered.
10. **Given** an assist coil turn, **When** it is raised, **Then** the weight of the coil is a required numeric answer.
11. **Given** one of the four returning Items, **When** it is raised, **Then** the skid count defaults to one and can be changed.

### Edge Cases

- A request with no picture and no new-message badge: the identity zone draws its placeholder state and the badge is absent.
- A cancelled request with no material handler: the person row reads the person who cancelled and the requester takes the fourth row.
- A request that is overdue while a handler condition is set: the condition wins, and the two are never shown together.
- An Item with no configured allotment: the ring uses the labelled fifteen-minute default rather than showing no measure.
- An Item whose declared cell has no source at all: the cell is hidden. A cell whose source exists but cannot be read: the cell is hidden and a warning is written to the database log. Neither is explained on the card.
- A long request description: it is shown in full and scrolls rather than being clipped.
- A hopper size chosen before its image was ever supplied: the shared placeholder is drawn.
- A fuzzy term carrying a known prefix and a short number: it is upper-cased and zero-padded before the search; a term with no known prefix is searched as typed.
- A fuzzy term that matches no part at all: the operator is told nothing matched and the value stands as entered.
- A weight of coil left blank: the answer is required, so the request cannot be confirmed.
- An Item's configuration declaring a field the card must not show: the field is not drawn, and the configuration is left alone.
- Two requests of the same Item raised minutes apart: their cards are the same shape and the same height.
- The system's animations switched off: any animation the new card adds is skipped and the card remains usable.
- A handler who can only use a keyboard: the flyout and every action button are reachable and operable.
- The application at its default window size: every zone's values remain readable and nothing is clipped away.

## Requirements

### Functional Requirements

**Card anatomy and the four zones**

- FR-001: A row MUST be drawn as four zones, left to right: identity, status, identity-and-detail, and action, separated by vertical rules.
- FR-002: The accent bar, the material picture and the new-message badge MUST keep their present meaning.
- FR-003: Shift+click enlarging of the picture and the badge's meaning MUST stay.
- FR-004: The requesting work centre MUST be drawn as a large unlabelled value beneath the picture.
- FR-005: The requesting work centre and the requesting user MUST never be detail cells, and the requesting user MUST appear only on the request's own detail page.
- FR-006: Nothing MUST be padded to fill a short panel and no universal fallback value MUST be invented; where a value is absent the space is removed.
- FR-007: The detail grid's top row MUST always span both columns and MUST hold the request's category word and the request's own identifier.
- FR-008: The coloured status pill MUST be removed, and the status MUST read as plain coloured text standing alone with no label in front of it.
- FR-009: A card's height MAY differ between Items but MUST be constant for any one Item.
- FR-010: A row MUST stretch the full width and MUST remain readable at the application's default window size.

**The status zone**

- FR-011: The status zone MUST be a centred stack of rows in this order: the status word; the time ring; the person; and, for a closed request, the requester.
- FR-012: The status word MUST read, in precedence order, the handler-set condition when one is set, otherwise `Overdue` when the request is overdue, otherwise the lifecycle word in friendly wording (`Waiting`, `In Progress`, `Done`, `Cancelled`).
- FR-013: The ring MUST fill as the allotted time is used and MUST carry the time left inside it.
- FR-014: An overdue request MUST read `Overdue`, MUST show the time overrun in its ring, and MUST theme the whole status column as overdue.
- FR-015: A closed request MUST freeze the ring at the moment it closed and MUST carry the time taken instead of the time left.
- FR-016: The ring MUST state which of the two things it is measuring, and its accessible name MUST say the same.
- FR-017: The ring MUST re-evaluate on the same one-minute tick that refreshes the row's other time text, so the ring and that text never disagree.
- FR-018: A status row with no value MUST be removed rather than left blank.
- FR-019: The status column MUST carry three distinct themes: normal, handler-condition as a lighter red, and overdue.
- FR-020: An overdue request MUST remain distinguishable as its own state under every sort, and MUST NOT depend on the row's position.
- FR-021: The status zone MUST show no percentage and no elapsed-time line.
- FR-022: The person row MUST read the requester while the request is unclaimed and the material handler once it is claimed, and a closed request MUST show the material handler, or the person who cancelled, in that row with the requester in the fourth row. Both rows MUST name the person by their name: the material handler is recorded against a request by employee number, because that number is what the handler authorizations compare, and that number MUST NOT be printed where a person's name belongs. A handler whose name no source records leaves the row empty rather than falling back to the number.
- FR-023: A handler-set condition and an overdue marking MUST never both be shown, and the condition MUST take precedence for as long as it is set.

**The detail grid and its cells**

- FR-024: The detail zone MUST be an inner grid of three rows and two columns, with the title pair in row 1 across both columns and the detail cells in rows 2 and 3.
- FR-025: The cells a card shows MUST be determined by the request's Item and MUST NOT be chosen per row.
- FR-026: The grid's geometry MUST follow the number of cells the Item fills: four cells as two rows of two; three cells with the third spanning the bottom row; two cells with the bottom row collapsed; one cell spanning the full width; no cells with both lower rows removed.
- FR-027: The detail grid MUST be centred in its area whatever it is holding, so a panel whose lower row is empty stays in the middle of the section instead of sitting against the top of it; with no cells, the title row MUST take the vacated space as one full-width row. [Amended 2026-10-22, owner direction. The grid used to be centred only when the row resolved no data and pinned to the top otherwise, which left a two-cell card's grid at the top of a taller section with an empty stretch beneath it.]
- FR-028: Every cell MUST read `Label: value` on one line, apart from the label-less die-shop cell: the label small, secondary and semibold, the value a step larger, semibold and in the primary text colour, with a real gap between the two that does not depend on the separator's trailing space, and the pair starting from the cell's left edge. [Amended 2026-10-22, owner direction, replacing the earlier "label on top with its value beneath": the shipped cards have drawn the pair on one line since the cell area returned to the card, and with only a trailing space between them the pair read `Part Type:WIP`. Centring each pair inside its cell was tried the same day and rejected — it put the six cells' contents on six different axes — so what the card centres is the grid, not the cell (FR-027).]
- FR-029: `Die Location` MUST be shown on the two die Items and on no other Item.

**Detail cells by Item**

- FR-030: `pickup-coil` MUST show Coil number, Quantity in house, Coil description and Average coil weight.
- FR-031: `pickup-die` MUST show Die and `Die Location`, plus the label-less die-shop cell when the die-shop answer calls for it.
- FR-032: `pickup-component` MUST show Component, Total Qty In House and Item Type (WIP or Finished Goods).
- FR-033: `pickup-dunnage` MUST show Dunnage part and `Home Location`.
- FR-034: `pickup-scrap` MUST show Scrap Type and Lugger Location.
- FR-035: `pickup-hopper` MUST show Size.
- FR-036: `pickup-fg` MUST show Part Type and # of Skids.
- FR-037: `pickup-ncm` MUST show Part Type and # of Skids.
- FR-038: `pickup-wip` MUST show Part Type and # of Skids.
- FR-039: `pickup-outside-service` MUST show Part Type and # of Skids.
- FR-040: `pickup-riser-table` MUST show no detail cells, so its panel collapses.
- FR-041: `deliver-coil` MUST show Coil number, Quantity in house, Coil description and Average coil weight.
- FR-042: `deliver-flatstock` MUST show Part, Quantity and Destination.
- FR-043: `deliver-component` MUST show Component, Total Qty In House and Item Type (WIP or Finished Goods).
- FR-044: `deliver-die` MUST show Die and `Die Location`, plus the label-less die-shop cell when the die-shop answer calls for it.
- FR-045: `deliver-dunnage` MUST show Dunnage part and `Home Location`.
- FR-046: `deliver-wrong-coil` MUST show Coil number as the incorrect coil and Correct coil.
- FR-047: `deliver-wrong-flatstock` MUST show Part as the incorrect flatstock and Correct flatstock.
- FR-048: `deliver-riser-table` MUST show no detail cells, so its panel collapses.
- FR-049: `deliver-hopper` MUST show Size.
- FR-050: `assist-coil-turn` MUST show Coil Number and Weight of Coil.
- FR-051: `assist-table-place` MUST show no detail cells, so its panel collapses.
- FR-052: `assist-table-remove` MUST show no detail cells, so its panel collapses.
- FR-053: `other` MUST show Request description, drawn in full and scrolling rather than clipped when it is long.
- FR-054: Weight of Coil MUST be the actual weight the operator supplies on an assist coil turn and MUST NOT be conflated with the job's average coil weight.
- FR-055: The die-shop cell MUST carry no label and its value MUST be the phrase itself, `Take To Die Shop` on a pickup and `Located In Die Shop` on a deliver, drawn only when the die-shop answer was given.
- FR-056: A declared field that is not one of an Item's cells MUST NOT be shown: the requesting, pickup and work-centre fields; `Description` on the dunnage Items; `Quantity` on the die Items; `Reason` on both wrong-part Items; and the declared fields of the four returning Items other than Part Type and # of Skids.
- FR-057: `Destination` MUST be shown only on `deliver-flatstock` and MUST NOT be shown on `pickup-ncm` or `pickup-outside-service`.
- FR-058: A cell the Item never declares MUST be hidden, and a cell whose source exists but cannot be read MUST be hidden and MUST additionally write a warning to the database log so the gap can be chased up later.
- FR-059: The card MUST show `Die Location` on the die Items and `Home Location` on the dunnage Items, reading those labels from configuration rather than overriding them.
- FR-060: Neither unresolvable case MUST be explained on the card and neither MUST be filled with a substitute.

**The action zone**

- FR-061: The action zone MUST be a grid of up to five square buttons in three columns and two rows, in row-major order: Accept, Cancel, Give back across the top; then Complete and Open details. [Corrected 2026-10-11, owner review (D44): this said two columns and three rows; three columns by two is what the settled action-grid table has always shown.] [Amended 2026-10-11, owner direction (D59): the sixth button was Print, and Print is no longer an action of the card at all.]
- FR-062: A button MUST be drawn only while the row's own gate says the action is available, and no action MUST become available or unavailable because the layout changed.
- FR-063: An unavailable button MUST be removed rather than blanked, and the remaining buttons MUST re-centre in the action zone without stretching.
- FR-064: Every button MUST keep a tooltip and an accessible name, both taken from the row's localized text.
- FR-065: Every button MUST keep a hit target of at least 44 by 44.
- FR-066: Accept, Complete and Give back MUST keep distinct colours and glyphs so they are never mistaken for one another.
- FR-067: The card MUST NOT carry a print action. Printing a label is a future action of the request's **detail page**, where the detail a label needs is, and it MUST NOT be drawn on the card as a placeholder in the meantime. [Superseded 2026-10-11, owner direction (D59). It read: *Print MUST ship disabled, MUST say it is not available yet when pointed at, and MUST perform no action.* The placeholder was removed because it performed nothing and the platform's disabled state faded it until its glyph was invisible, so it read as an empty box on every card rather than as something coming.]
- FR-068: Open details MUST be a button carrying a magnifier glyph and the tooltip `Open details`, and it MUST be the only way the card opens the request.
- FR-069: The card body MUST no longer open the request.

**The handler condition and its flyout**

- FR-070: When the signed-in person is the material handler working on that request, the status word MUST become clickable.
- FR-071: Clicking the status word MUST open a flyout offering four conditions: `Product Buried`, `Cant Find Product`, `Changing Forklift`, `Changing Propane`.
- FR-072: Choosing a condition MUST re-theme the whole status column to the handler-condition theme, a lighter red than the overdue theme.
- FR-073: A chosen condition MUST be stored against the request and MUST be restored onto that row when the list is next read.
- FR-074: The status word MUST be inert for everyone else, both for another person and on a request the signed-in person is not working.
- FR-075: The flyout MUST be reachable and operable by keyboard alone, and whether the word is operable MUST be announced rather than left to be discovered.
- FR-076: Only the row's own material handler MUST be able to set that row's condition.

**New answers in the New Request workflow**

- FR-077: Both hopper Items MUST ask for the hopper size as three choices, Small, Medium and Large, with a reference image of the chosen size shown beneath them.
- FR-078: The hopper's Size cell on the card MUST open that reference image, in the same way the card's other pictures enlarge.
- FR-079: A hopper image MUST be held to the application's existing picture rules: square within a small tolerance, at least 48 and at most 2048 pixels a side, a permitted file type, and a size between 1 KB and 10 MB.
- FR-080: A hopper size whose image is missing or unusable MUST draw the shared placeholder rather than a blank.
- FR-081: Both die Items MUST ask a checkbox question stating whether the die is at the die shop or must be sent there, and that answer MUST decide whether the card draws its third, label-less cell.
- FR-082: On the two wrong-part Items the incorrect part MUST be typed through a fuzzy search rather than free text, with the term upper-cased and zero-padded to ten characters when it starts with a known prefix.
- FR-083: A term matching a part exactly MUST resolve at once, and a term that matches none exactly MUST offer a list of every part whose number contains the term.
- FR-084: The fuzzy search MUST read the plant system live and MUST NOT be cached.
- FR-085: When the plant system cannot be reached the operator MUST be told the search is unavailable and the value they typed MUST be used as entered.
- FR-086: An assist coil turn MUST ask for the weight of the coil as a required numeric answer.
- FR-087: The four returning Items MUST ask how many skids must be picked up, as a numeric answer defaulting to one.

**Where the card's values come from**

- FR-088: The declared job-sourced labels MUST resolve from the requesting job, including the ones the job already carries and the resolver currently ignores: the coil number, the correct coil, the correct flatstock and the scrap type.
- FR-089: Anything the job cannot carry MUST be read from a source that holds it, and MUST NOT be invented and MUST NOT be left blank.
- FR-090: `Total Qty In House` MUST be the full sum for the part, not a per-location figure, read from the source that holds it for the item's type. [NEEDS CLARIFICATION: the finished-goods rule the item-type derivation leans on currently counts stock at any location rather than stock at a finished-goods location, so a part with stock anywhere and nothing open reads as Finished Goods; whether that rule is corrected within this work or tracked independently is not settled.]
- FR-091: Part Type on the four returning pickups MUST be the derived item type, `Finished Goods`, `Outside Service` or `WIP`, and for NCM it MUST be the location-derived state.
- FR-092: The item type MUST be derived with the priority Outside Service, then Finished Goods, then WIP: Outside Service from the subcontracted operation, WIP from an open step earlier than the routing's last, and Finished Goods only when the part is actually at a finished-goods location. [NEEDS CLARIFICATION: where NCM sits in this order is not stated anywhere. A part that is both at an NCM location and has outside-service work is the case with no answer. This work places NCM after Outside Service and before Finished Goods/WIP — NCM is a statement about where the stock is, and the two production types state what work remains — but that placement is an assumption recorded here rather than a settled rule.]
- FR-093: NCM MUST never be treated as a production type and MUST be derived from the location the stock occupies, never from the part's recorded item type.
- FR-094: The four returning Items MUST be offered when the requesting job has the work they name, and MUST no longer be hidden by a fixed list.
- FR-095: `Description` on the dunnage Items is declared in configuration but can never hold a value, since no dunnage record carries one. [NEEDS CLARIFICATION: whether that declaration is removed or left in place with the cell hidden is not settled, so neither is encoded here.]

**Contracts this work reverses**

- FR-096: An Item's declared fields MUST appear on the card. This formally supersedes the earlier rule that they appear on the request page and never on the card.
- FR-097: The card's anatomy MUST no longer be treated as frozen, and the lift MUST be recorded in writing so a later reader cannot take the old freeze as licence.
- FR-098: The detail-field grid MUST return to the card, reversing the earlier decision that it is not on the card.
- FR-099: The four metadata rows of the previous card, requested by, press, remaining time and waiting, MUST be superseded by the four-zone anatomy.
- FR-100: The card body MUST no longer be the click target that opens the request, reversing the earlier rule and the defect recorded against it.
- FR-101: An unresolvable value MUST be hidden, and where its source exists but cannot be read it MUST also be logged, superseding the earlier rule that it must say why rather than render blank.
- FR-102: The status vocabulary MUST be opened from four states to eight, the four lifecycle words plus the four handler-set conditions, and the status colouring MUST carry three themes.

**Invariants that must survive**

- FR-103: Two requests of the same Item MUST render identically in shape and height, and the panel's geometry MUST follow the Item.
- FR-104: Every new label MUST be a localized resource rather than a literal: the ring's accessible name, `Open details`, the new cell labels, the die-shop phrases, the four conditions and the new status words.
- FR-105: Any animation the new card adds MUST respect the system's animations-off setting.

**Added by the owner's review of the running card**

- FR-106: The condition a material handler reports MUST be cleared when the request is returned to the open pool, so the card reads the `Waiting` lifecycle word again rather than the condition the previous handler left on a job nobody holds. A condition describes the assigned handler's own work on that request and MUST NOT outlive the assignment it was reported against. [Added 2026-10-16, owner direction, after reviewing a given-back job still reading its previous handler's condition.]

**Added by the owner's review of the request detail page**

- FR-107: The request detail page MUST render **one** card for the request itself. The work centre, the person who asked and the remaining time MUST be stated once, on the page's hero card, and MUST NOT be repeated in a second card below it. [Added 2026-10-16, owner direction.]
- FR-108: The detail page's `Work order` row MUST state the work order the request's job is for, in the `WO-######` form the work-order reads use. It MUST NOT state the work centre, and where the job cannot be resolved the row MUST be left out rather than filled with anything else (FR-002, FR-101). [Added 2026-10-16, owner direction, after the row was found printing the press.]
- FR-109: Each picture on the hero card MUST carry a small caption beneath it saying what it is, because a picture standing alone beside a title names nothing. [Added 2026-10-16, owner direction.]
- FR-110: The hero card's `Remaining Time` value MUST state the time left **against the time the Item allotted**, so the countdown is readable as a proportion rather than as a number on its own. [Added 2026-10-16, owner direction.]

**Added by the owner's review of the card's description cells**

- FR-111: Every card cell whose value is a **description** MUST be drawn in full by the one-line scrolling control rather than clipped with an ellipsis, and the rule MUST follow the value rather than the Item: the request description on `other`, the part description on `deliver-component`, and the coil description on both coil Items. It MUST NOT be pinned per panel slot, because a description sits in the third slot on a coil card and the first on a component card, and it moves as soon as a value ahead of it is missing. This broadens FR-053 from `other` alone. [Added 2026-10-22, owner direction, after a component card clipped its part description.]

### Key Entities

- **Request row**: one waiting request as it appears in the list. Holds its Item, its lifecycle state, its requesting work centre, the requester, the material handler, and when it was raised and closed.
- **Item**: the canonical kind of thing a request is for, one of twenty-four. Determines the request's category word, the cells its card shows, the wording of its labels, and its allotted time.
- **Request item configuration**: the declared per-Item detail fields, in declared order, together with the label each cell shows.
- **Detail cell**: one label-and-value pair on a card, put there by the Item's declaration. A cell with no source is absent rather than empty.
- **Card zone**: one of the four regions a row is divided into: identity, status, identity-and-detail, action.
- **Status state**: what the status word reads. One of the four lifecycle words, an overdue marking, or one of the four handler-set conditions.
- **Handler condition**: a condition a material handler reports against a request they are working. Four exist, and only that row's handler may set one.
- **Time ring**: the card's measure of a request's window. Time left while live, time taken once closed.
- **Allotted time**: the Item's configured window, or the labelled fifteen-minute default when the Item configures none.
- **Job snapshot**: the requesting job's material, from which the card's job-sourced values resolve.
- **Item type**: the derived production state of a part, Finished Goods, Outside Service or WIP, or for NCM a state derived from the location the stock occupies.
- **Request answer**: a value the requester supplied when the request was raised, such as the hopper size, the die-shop direction or the weight of a coil.

## Success Criteria

### Measurable Outcomes

- SC-001: A row can be read without opening it: what is wanted, what it is for, how long it has waited, and how much of its window is left.
- SC-002: Every row shows all four zones in order, with the action grid showing up to six buttons.
- SC-003: Two requests of the same Item render identically in shape and height, and two different Items never swap a cell's meaning.
- SC-004: A card with fewer cells is shorter rather than padded, verified for the no-cell, one-cell, two-cell, three-cell and four-cell cases.
- SC-005: `Die Location` appears on die requests and on no other request.
- SC-006: The status column reads correctly in every state: the word, the ring carrying time left or time taken, and the person row, with empty rows removed rather than blank.
- SC-007: A handler condition shows on row 1 in the lighter red theme, an overdue request is unmistakable under every sort, and the two are never both shown.
- SC-008: Only the row's own material handler can open the status flyout; for everyone else the status word is inert.
- SC-009: Unavailable action buttons are absent, and the remaining grid is centred rather than stretched.
- SC-010: All twenty-four Items render their own cells, and the four returning Items are offered whenever the requesting job has the work they name.
- SC-011: No card shows a value the request does not carry, and a cell whose source cannot be read is hidden with a warning recorded rather than explained on the card.
- SC-012: The card offers no action it cannot perform, so nothing on it reads as broken or as working when it is not. [Amended 2026-10-11, owner direction (D59): this read *Printing is visibly unavailable and says so when pointed at*; the card now carries no print action at all.]
- SC-013: Opening a request is a deliberate, separate action from clicking its row.
- SC-014: The card is usable with the system's animations switched off, and the flyout is operable by keyboard alone.
- SC-015: The New Request workflow collects all five new answers, and each one appears on the matching card.
- SC-016: The four handler conditions survive a restart and are restored on the row they were set against.
- SC-017: A card remains readable at the application's default window size.

## Assumptions

- **Informed defaults.** A status row with nothing to say is removed rather than blanked, and the detail panel behaves the same way. A handler condition outranks an overdue marking, and an overdue marking outranks the lifecycle word. A closed request's ring measures the time taken and a live request's measures the time left. An Item with no configured allotment uses the labelled fifteen-minute default. A skid count left alone is one. A hopper image that is missing or unusable draws the shared placeholder.
- **Cells with no source yet.** `Destination` on `deliver-flatstock` is a planned cell that no source currently supplies, so it is hidden until a source exists rather than being filled. `Lugger Location` on `pickup-scrap` is no longer in this state: 013-lugger-location-config gives it a source in configuration, so the cell is drawn with the lugger the arrangement assigns to the job's scrap type. Every other cell named in FR-030 to FR-053 resolves from the request, the requesting job, the Item's configuration, or the plant system.
- **Accepted risks (recorded, not fixed).** A handler condition hides the lifecycle state on the card for as long as it is set, and an overdue marking hides it too; both are deliberate, visible trade-offs rather than silent ones. The fuzzy part search is deliberately live-only, so it degrades to what the operator typed when the plant system is unreachable instead of serving a stale answer. Adding four conditions to a vocabulary that held four opens the status palette to eight states, which every reader of the palette must now handle.
- **Scope.** Only the waitlist list rows change. The request detail page is untouched. The four lifecycle words themselves are unchanged; the four conditions are an addition to the vocabulary. No new real action is added beyond opening a request and setting a condition; printing is not added to the card at all, and belongs to the request's detail page later (D59). Setup, Settings and the mock and visual-fallback surfaces are untouched. The mirror and fallback model is unchanged, and the fuzzy search is not given a cached read shape.
- **Dependencies.** The work depends on the requesting job's own data being readable, on the plant system being reachable for the live reads, and on the Item configurations being brought into line with the card so that a label the card shows is the label configuration declares.

## Verbatim Constraints

Values the request pinned exactly; downstream steps and the implementation must use these strings and numbers verbatim.

- The four handler-set conditions: `Product Buried`, `Cant Find Product`, `Changing Forklift`, `Changing Propane`.
- The label-less die-shop cell's value on a pickup: `Take To Die Shop`. On a deliver: `Located In Die Shop`.
- The status words: `Waiting`, `In Progress`, `Done`, `Cancelled`, and the overdue marking `Overdue`.
- The cell labels: `Die Location` on the two die Items, `Home Location` on the two dunnage Items.
- The button tooltip for opening a request: `Open details`.
- The fuzzy-search prefixes: `MMC` for coil, `MMF` for flatstock, `FGT` for die. A zero-padded part number is ten characters long (`MMC1000` becomes `MMC0001000`, `MMF500` becomes `MMF0000500`).
- The four returning Items: `pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service`.
- The hopper size choices: `Small`, `Medium`, `Large`.
- Numbers: a hit target of at least 44 by 44; a hopper image square within a tolerance of two per cent and at least 48 and at most 2048 pixels a side; a hopper image file between 1 KB and 10 MB; the permitted image file types `png`, `jpg`, `jpeg`; a skid count defaulting to 1; the default allotment of 15 minutes.
