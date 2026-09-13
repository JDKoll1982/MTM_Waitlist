# Contract — The card, the identifier and the sort control

What a consumer or a test codes against on the list. Every stored value and identifier here is copied verbatim from
the spec's Verbatim Constraints.

## 1. One card shape for every Item

**One layout.** The card is the same shape for every request, and **no layout variant is selected by Item**
(FR-006). The card's own grid keeps the existing `Auto` / `*` row and column sizing with `MinHeight` and star
widths; no layout-critical container gains a hardcoded pixel boundary (constitution V).

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

## 2. Line 1 — the umbrella phrase

The Category's own word, or an Item-specific phrase where the Item defines one (FR-005).

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

## 3. Line 2 — the identifier

A fixed value, a value read from the job, or a value that depends on an answer captured during the flow (FR-005).

| Kind | Items |
|---|---|
| **fixed value** | `pickup-riser-table`, `deliver-riser-table` → `Riser Table`; `pickup-hopper`, `deliver-hopper` → `Hopper` |
| **read from the job** | `pickup-coil`, `pickup-component`, `pickup-dunnage`, `pickup-scrap`, `deliver-coil`, `deliver-flatstock`, `deliver-die`, `deliver-dunnage`, `assist-coil-turn`, `assist-table-place`, `assist-table-remove`, and the two wrong-material Items (which name the **correct** material) |
| **depends on a captured answer** | `pickup-die` — the die's location when the captured destination is `Home Location`, otherwise the die's number. `other` — the message the person typed. |

**The die's rule, verbatim.** `Home Location` is the value that switches the second line from the die's number to
the die's location. The three destination options are `Die Shop`, `Home Location`, `Other`.

**Token set.** Line 2 is written as a `{token}` template on the Item's catalog row. Job-derived tokens:
`{part_number}`, `{part_description}`, `{die_number}`, `{die_location}`, `{dunnage_part}`, `{sequence_number}`,
`{scrap_type}`. Captured tokens: `{answer}`, `{destination}`, `{component}`, `{defect}`. An unresolvable token
renders the Item's own display name and reports the configuration problem — never a blank, never a fabricated
value.

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

| Row | Value |
|---|---|
| **Requested by** | the stored requester |
| **Press** | the stored work centre |
| **Remaining time** | `target_time_utc`, or `requested_utc + the Item's allotted minutes`; **bold red** when overdue |
| **Waiting** | `requested_utc` → now |

## 6. The status vocabulary

**Stored statuses, unchanged and not to be extended:** `Pending`, `Accepted`, `Completed`, `Canceled`.

**Friendly labels the card shows, display text only:** `Waiting`, `In Progress`, `Done`, `Cancelled`. A friendly
label must never be written to the store and must never be compared as though it were a stored status.

## 7. The sort control

A flyout on the shell, beside the existing My Requests and building controls (FR-011). Implemented as a
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

## 9. What the list must not do

- **Not** choose a card layout by Item (FR-006).
- **Not** draw an Item's own fields on the card (FR-007).
- **Not** let a placeholder replace a picture the request already resolves (FR-009).
- **Not** reorder a row's overdue marking out of view because the sort changed (FR-012).
- **Not** report an unreachable store as an empty list — the per-screen unavailable state with a manual retry is
  the only correct presentation (FR-026).
