# Contract — The card, the identifier and the sort control

What a consumer or a test codes against on the list. Every stored value and identifier here is copied verbatim from
the spec's Verbatim Constraints.

> **Superseded in part, 2026-10-10 — `specs/012-waitlist-card-layout` (FR-096 to FR-102).** This file was the card's
> anatomy contract, and the waitlist card layout work replaced that anatomy. **The freeze this contract carried is
> lifted in writing**, and the live card contract is now
> `specs/012-waitlist-card-layout/contracts/card-and-identifier.md`. Seven rules below are superseded; each is
> annotated where it stands and collected with its replacement in **§10** at the end of this file, so no later
> reader can take an old rule as licence. The identifier rules — §2 (Line 1), §3 (Line 2), §4 (the picture) and §7
> (the sort control) — are unchanged and still bind.

## 1. One card shape for every Item

**One layout.** The card is the same shape for every request, and **no layout variant is selected by Item**
(FR-006). The card's own grid keeps the existing `Auto` / `*` row and column sizing with `MinHeight` and star
widths; no layout-critical container gains a hardcoded pixel boundary (constitution V).

> **Amended 2026-10-10 (FR-097, FR-103).** "One layout" still holds in the sense that matters here — one shape for
> every request, and no Item picks the card's *layout* — but **the card's anatomy is no longer frozen to what this
> file describes**, and that lift is recorded here rather than only in the newer file, because this is the file the
> freeze lived in. What replaces the freeze is a card drawn as **four zones** (identity, status,
> identity-and-detail, action) whose third zone selects **one detail template per Item**, so a card's **height may
> differ between Items and is constant for any one Item** and the Item decides which cells are drawn. Read
> `specs/012-waitlist-card-layout/contracts/card-and-identifier.md` for the card as it is; the rows below are
> history wherever §10 lists them as superseded.

The card retains, unchanged from the verified `specs/003` anatomy:

| Element | Note |
|---|---|
| the Item's image, as a fixed centred square | not stretched to card height |
| Line 1 on its own top row | the umbrella phrase |
| Line 2 beneath it | the Item's identifier |
| the four metadata rows | requested by · press · remaining time · waiting |
| the compact status pill | below the action area |
| the new-message marker | on the Item's image |
| the action area | primary Accept → Complete, plus Cancel |

**What the card loses:** the per-type detail grids. An Item's own fields move to the request's page and **never**
appear on the card (FR-007).

> **Superseded 2026-10-10 (FR-096, FR-098).** Both halves of that sentence are withdrawn, and the replacement is
> recorded here so the old text cannot be read as a rule:
>
> - **An Item's declared fields appear on the card again.** The detail-field grid returns to the card as the third
>   zone's inner grid of three rows and two columns — row 1 spanning both columns with the category word and the
>   request's own identifier, rows 2 and 3 holding the Item's own cells
>   (`specs/012-waitlist-card-layout/contracts/card-and-identifier.md` §4). The per-Item detail grids are
>   therefore **not** lost: what replaces them is one detail template per Item, chosen by the row's Item code, so
>   an Item's fields are drawn on the card rather than only on its request page.
> - **Nothing is excluded from the card on the strength of FR-007.** The fields the card does *not* draw are a
>   card rule with its own reasons (the work-centre fields, the four collapsing Items, the fields FR-056 names),
>   never this sentence.

## 2. Line 1 — the umbrella phrase

The Category's own word, or an Item-specific phrase where the Item defines one (FR-005). An Item may declare a
**Line 1 template** instead of a bare phrase, so its first line can name the job value the request is about; the
phrase is then the template's `{umbrella}` token and its fallback.

| Category | Default Line 1 |
|---|---|
| `Pickup` | `Pickup` |
| `Deliver` | `Deliver` |
| `Assist` | `Assist` |
| `Other` | `Other` |

**Item-specific overrides, verbatim:**

| Item | Line 1 |
|---|---|
| `deliver-wrong-coil` | `Wrong Coil Bring:` |
| `deliver-wrong-flatstock` | `Wrong Flatstock Bring:` |

These are the Item's own first line, not the plain Category word.

**Two Items add a job value to that phrase (FR-053).** `pickup-die` and `deliver-die` declare the Line 1 template
`{umbrella} {job_part_number}`, so their cards read `Pickup Die: PART-9003` / `Deliver Die: PART-9003` — the part
the die is assigned to. The phrase itself stays `Pickup Die:` / `Deliver Die:` and is what the line degrades to when
the job value cannot be resolved, so a first line always says what kind of request it is.

**The Line 1 token set** is the identifier's token set plus `{umbrella}` — the Item's own phrase. Nothing else is
added, and an Item that declares no Line 1 template resolves to its phrase exactly as before.

## 3. Line 2 — the identifier

A fixed value, a value read from the job, or a value that depends on an answer captured during the flow (FR-005).

| Kind | Items |
|---|---|
| **fixed value** | `pickup-riser-table`, `deliver-riser-table` → `Riser Table`; `pickup-hopper`, `deliver-hopper` → `Hopper` |
| **read from the job** | `pickup-coil`, `pickup-scrap`, `deliver-coil`, `deliver-flatstock`, `deliver-die`, `assist-coil-turn`, `assist-table-place`, `assist-table-remove`, the four out-of-scope Items, and the two wrong-material Items (which name the **correct** material) |
| **depends on a captured answer** | `pickup-component`, `deliver-component` — the component the operator chose; `pickup-dunnage`, `deliver-dunnage` — the dunnage part they chose; `other` — the message the person typed. (`pickup-die` was also a row here: its identifier switched on the captured destination. FR-053 replaced that switch with one composition, and the question itself retired 2026-09-20 — D22 — so the die's line is read from the job like every other die value.) |

**Where a job value comes from — the `{part_number}` and `{scrap_type}` tokens (2026-09-22).** A job-derived
token is supplied only where a template names it, and only from a value the requesting job actually holds:

- `{part_number}` is **the part the request is about**. Where the Item is about a material the job holds, that is
the material's own number — the coil (`MMC`) for `pickup-coil`, the coil Items, the wrong-coil Item and the
coil-turn assist; the flatstock (`MMF`) for the flatstock Items; and the **coil** for the merged `pickup-coil`,
falling back to the job's flatstock on a flatstock-only job (D21). Where the Item is about no particular material —
the two table assists, and the four out-of-scope Items — it is the **job's own part number**, which is the same
value the request page shows for a declared `Part` row, so the card and the page cannot disagree about one request.
Which material an Item is about comes from `RequestItemPickerRules.RequiredJobPart` — the very rule that decides
whether the Item is offered at all — so the picker and the card cannot disagree either (FR-002, FR-013).
- `{scrap_type}` is the scrap type the job's **real** decision names (FR-030), read through the same rule the Scrap
Item's visibility gates on (FR-031): the placeholder and `No Scrap` are not decisions, so neither is carried.
- A job that holds nothing for the token leaves it unresolved, and the card then shows the Item's display name and
reports the configuration problem rather than inventing a part (FR-026).

**These sources were missing until 2026-09-22, and the cards said the wrong thing.** Nothing supplied
`{part_number}`, `{scrap_type}` or `{component}`, so every card whose identifier named one of them fell back to the
Item's own display name: a `deliver-component` request read `Deliver` / `Component` rather than
`Deliver` / `V-EMB-2`, a coil request read `Coil`, a work-in-process request read `Work In Process (WIP)`. The
second line named the kind of request instead of the part involved.

**The die's rule.** The second line is the die's own `FGT` number and nothing else — `FGT0002000` (FR-056). Where the
die lives is a separate fact that the request page lists for itself (FR-057), so it is not part of the name.

> **The spelling changed twice on 2026-09-20, and two records keep the earlier ones deliberately.** The identifier
> was `FGT0002000-DIE SHOP`, then briefly `FGT0002000 - (DIE SHOP)`, and is now the number alone. The dated evidence
> in `tasks.md` and the run journal in `progress.md` still show the earlier spellings, because they record what the
> running app displayed on the day: they are history, not a rule, and rewriting them would falsify an observation.
> A request raised under any earlier spelling still names its own die — `WaitlistRequestTitles` accepts all three
> when it reads the stored `input_value`, so a format change cannot silently fall an existing request back to the
> job's primary die.

> **Superseded 2026-09-14 (FR-053), then retired 2026-09-20 (D22).** The line used to switch on the captured
destination (`{die_location}` when it was `Home Location`, the number otherwise). FR-053 replaced that with a single
composition, so no shipped row used the conditional any more; and the destination question has now retired in full —
a die always goes to its home location and the person is never asked where it goes. The conditional syntax is gone
from the resolver's contract as well as from the rows, and **no row may name `destination`**.

**Token set.** Line 2 is written as a `{token}` template on the Item's catalog row. Job-derived tokens:
`{part_number}`, `{part_description}`, `{die_number}`, `{die_location}`, `{dunnage_part}`, `{sequence_number}`,
`{scrap_type}`, `{job_part_number}`. Captured tokens: `{answer}`, `{component}`, `{defect}`. An
unresolvable token renders the Item's own display name and reports the configuration problem — never a blank, never
a fabricated value.

> **Superseded in part 2026-10-10 (FR-101, D41).** The sentence above is the **identifier's** rule and it still
> holds: a Line 2 token with no source reads the Item's own display name, and the configuration problem is stated
> rather than hidden (`WaitlistRequestTitles` is unchanged, and its tests still assert it). What is superseded is
> the same "say why rather than render blank" rule applied to a **card detail cell**. There, both unresolvable
> cases are now **hidden**: a cell whose source exists but cannot be read is hidden **and** writes a warning to the
> database log, and neither case is explained on the card and neither is filled with a substitute
> (`specs/012-waitlist-card-layout/contracts/card-and-identifier.md` §4, FR-058, FR-060). A later reader must not
> extend this paragraph to the card's cells.

**`{job_part_number}` is the job's own part number**, not a subordinate's. It exists because `{part_number}` means
the *subordinate's* number for the coil and flatstock Items, so a die's "which part is this die for" value must not
borrow it (FR-053).

**`{dunnage_part}` is the captured answer.** The dunnage Items ask which dunnage the operator needs (FR-048), so
their identifier is the part that answer carries: one the job had assigned, or the substitute the operator picked.
The request stores that part as its one answer like any other, and the card reads it back through the template —
the stored request cannot tell an assigned part from a substitute, and does not need to (FR-049, FR-051).

**The real material identifiers stay deferred.** The coil or part number, quantity and stock location remain the
property of the later item-resolution work, so the card shows only what a request truthfully carries.

## 4. The picture

Resolution order, and the rule that governs it (FR-009, FR-021):

```text
the Item's picture  ──▶  the Category's image family  ──▶  the existing default placeholder
```

A `"nothing configured"` answer for an Item **must not** replace an image the request already resolves. The Item is
keyed in `config_images_locations` under the `request_item` scope; the Category family under `request_category`.

## 5. The four metadata rows

They stay on the card, for every Item — they are the same for every Item, so they do not break uniformity
(FR-008).

> **Superseded 2026-10-10 (FR-099).** The four metadata rows are gone from the card and this section no longer
> describes it. What replaces them is the card's **four zones**: requested by and the work centre read inside the
> identity and status zones rather than as labelled rows (the work centre as the identity zone's large unlabelled
> value beneath the picture, the person in the status zone's third row and the requester in its fourth), remaining
> time becomes the **time ring** carrying the time left while live and the time taken once closed, and waiting is
> no longer a row at all. See `specs/012-waitlist-card-layout/contracts/card-and-identifier.md` §§1–3 and
> `contracts/status-and-conditions.md`.
>
> The four rows, kept here as the record of what was replaced:
>
> | Superseded row | Was | Now |
> |---|---|---|
> | **Requested by** | the stored requester | the status zone's person row (row 3), and row 4 on a closed request |
> | **Press** | the stored work centre | the identity zone's large unlabelled value beneath the picture |
> | **Remaining time** | `target_time_utc`, or `requested_utc + the Item's allotted minutes`; **bold red** when overdue | the time ring: the time left while live, the time taken once closed, the overrun when overdue |
> | **Waiting** | `requested_utc` → now | no row — the ring is the card's measure of the window |
>
> The sort control's dependence on these rows is **not** superseded: §7's *most urgent* and *longest waiting*
> options still derive from the same due and requested values, which the ring and the row's own time text now
> carry.

## 6. The status vocabulary

**Stored statuses, unchanged and not to be extended:** `Pending`, `Accepted`, `Completed`, `Canceled`.

**Friendly labels the card shows, display text only:** `Waiting`, `In Progress`, `Done`, `Cancelled`. A friendly
label must never be written to the store and must never be compared as though it were a stored status.

> **Superseded in part 2026-10-10 (FR-102).** The **stored lifecycle statuses are unchanged** — `Pending`,
> `Accepted`, `Completed` and `Canceled` are still the four the `status` column holds, and a handler's condition is
> never written over one of them. But the card's **display vocabulary is opened from four states to eight**: the
> four lifecycle words above, the `Overdue` marking, and the **four handler-set conditions** a material handler can
> report (`Product Buried`, `Cant Find Product`, `Changing Forklift`, `Changing Propane`). Conditions are stored in
> their own nullable `handler_status` column, not as new lifecycle statuses, which is why "not to be extended" still
> describes the `status` column and no longer describes what the card can read.
>
> The status colouring carries **three themes** — normal, handler-condition (a lighter red) and overdue — rather
> than the pill's single appearance. The superseded pill itself is gone: the status word is plain coloured text
> with no label in front of it. See `specs/012-waitlist-card-layout/contracts/status-and-conditions.md` §§1–3.

## 7. The sort control

A flyout on the shell, beside the existing Local Work Centers and building controls (FR-011). Implemented as a
`MenuFlyout` of `RadioMenuFlyoutItem` elements sharing a `GroupName` with `IsChecked` on the selected one — the
documented WinUI 3 mechanism for mutually-exclusive menu options (Microsoft Learn, *Menu flyout and menu bar*).

**The five options, in this order, verbatim:** most urgent (the default), longest waiting, press, requested by,
status.

| Contract point | Rule |
|---|---|
| Default | **most urgent**, on first run and for anyone who never touches the control (FR-010) |
| Remembered | **per person, always** — the choice comes back next time and survives a restart (SC-009) |
| Store | the existing local-settings mechanism, **not** a new table and not the database |
| Overdue | an overdue row stays **visibly marked as overdue whatever the sort**, and the most overdue stay findable (FR-012) |
| Consistency | the order shown never contradicts the card's own **Remaining time** or **Waiting** rows |

**Why most urgent and longest waiting are both offered.** They are not synonyms. *Waiting* is `requested_utc` →
now — the card's **Waiting** row. *Urgency* derives from the due value — the card's **Remaining time** row. The two
diverge by exactly the Item's allotment, and this feature is the change that creates that divergence, because it
gives every Item its own minutes.

## 8. The card's row contract for tests

A test may assert, for any Item: the Line 1 phrase, the Line 2 value or its resolution, the four metadata values,
the picture's resolution order, and that the row exposes the same action affordances for the same stored status as
before. A test may **not** assert an Item's field list, and may not assert the presence of a per-Item layout variant
— there is none.

> **Superseded in part 2026-10-10 (FR-096 to FR-100, FR-103).** The prohibition on asserting an Item's field list is
> withdrawn, and so is "there is none". An Item's cells are now drawn through **one detail template per Item**
> chosen by the row's Item code, so a test **may** assert an Item's field list — and the delivered suite does,
> through its own per-Item tables reading the shipped seed. Two further changes to the contract above: the four
> metadata values are no longer a thing to assert (the four zones replaced those rows, §5), and the action
> affordances are now a grid of six buttons rather than a primary-plus-cancel set, in the shape
> `specs/012-waitlist-card-layout/contracts/card-and-identifier.md` §5 pins. The Line 1, Line 2 and picture
> resolution rules above are unchanged and still assertable.

## 9. What the list must not do

- **Not** choose a card layout by Item (FR-006).
- **Not** draw an Item's own fields on the card (FR-007) — **superseded 2026-10-10 (FR-096, FR-098):** an Item's
  own fields **are** the card's detail cells, drawn through the Item's own detail template. What the list must not
  do is choose them per row: the cells come from the Item, never from the individual request (FR-025, FR-103).
- **Not** let a placeholder replace a picture the request already resolves (FR-009).
- **Not** reorder a row's overdue marking out of view because the sort changed (FR-012).
- **Not** report an unreachable store as an empty list — the per-screen unavailable state with a manual retry is
  the only correct presentation (FR-026).

## 10. Superseded by `012-waitlist-card-layout` — the seven rules, each with its replacement

Recorded 2026-10-10 (FR-096 to FR-102). This is the list a later reader should check before treating any rule in
this file as current. The authority for it is `docs/research/research-waitlist-card-layout-pre-spec.md` §6, "What this reverses", and the
live contract is `specs/012-waitlist-card-layout/contracts/card-and-identifier.md`.

| # | The rule as this file had it | Superseded by |
|---|---|---|
| 1 | An Item's declared fields appear on the request's page and **never** on the card (§1, FR-007) | **An Item's declared fields appear on the card**, as the third zone's detail cells (FR-096) |
| 2 | The card's anatomy is frozen — one layout, and `specs/004` had already frozen `specs/003`'s per-type anatomy (§1) | **The freeze is lifted in writing** (FR-097). The card is drawn as four zones and its third zone selects one detail template per Item; a card's height may differ between Items and is constant for any one Item (FR-103) |
| 3 | The detail-field grid is **not** on the card; the per-type grids are lost (§1) | **The detail-field grid returns to the card** — an inner grid of three rows and two columns, row 1 spanning both columns with the category word and the identifier, rows 2 and 3 holding the Item's cells (FR-098) |
| 4 | Four metadata rows on the card for every Item: requested by · press · remaining time · waiting (§5) | **The four zones supersede them** — the person rows move into the status zone, the work centre becomes the identity zone's large unlabelled value, and remaining time and waiting become the time ring (FR-099) |
| 5 | The card body is the click target that opens the request (with the defect recorded against it) | **A dedicated `Open details` button opens the request and the body no longer does** (FR-100). The picture's Shift+click enlarge survives, and the status word is the body's one interactive element |
| 6 | An unresolvable value must **say why** rather than render blank (FR-026) | For a **detail cell**, an unresolvable value is **hidden**, and where its source exists but cannot be read it is **also logged** to the database log; neither case is explained on the card and neither is filled with a substitute (FR-101). The identifier's own FR-026 fallback is unchanged — see §3 |
| 7 | The status vocabulary is **closed at four** (§6), with a single compact status pill | The vocabulary is **opened to eight** — the four lifecycle words plus the `Overdue` marking and the four handler-set conditions — and the colouring carries **three themes**: normal, handler-condition (a lighter red) and overdue (FR-102). The pill is replaced by plain coloured text with no label in front of it |

Rules 1, 3 and 6 are the ones most likely to be misread, because each of them **forbids** something this feature
deliberately did. A reader who finds only the old sentence and no replacement will undo the card. That is why the
replacement is carried in this file and not only in the newer one.
