# Phase 1 Data Model — Unified Card and Category/Item Request Picker

The entities this feature **introduces** or **reshapes**. Everything the feature does not touch is left out. Field
names in `code spans` are the real identifiers; the stored column names and the four Category codes, the
twenty-four Item codes, the status vocabulary and the two bring phrases are copied verbatim from the spec's
Verbatim Constraints and must not be renamed, recased or pluralized.

---

## 1. Category — *existing, unchanged in shape*

A `RequestCategory` enumeration, not a table. Its four members are the stored values of the request's `category`
column and the Codes in the picker's first step.

| Member | Stored / stored-string value | Umbrella phrase on the card (Line 1) | Image family |
|---|---|---|---|
| `RequestCategory.Pickup` | `Pickup` | `Pickup` (an Item may override it) | the Pickup family |
| `RequestCategory.Deliver` | `Deliver` | `Deliver` (an Item may override it) | the Deliver family |
| `RequestCategory.Assist` | `Assist` | `Assist` | the Assist family |
| `RequestCategory.Other` | `Other` | `Other` | the Other family |

**Validation.** Exactly these four strings are accepted on read; the parser is case-insensitive on input and
normalises to the stored casing. Anything else is not a Category and the row is treated as an unknown Item (see
§3, stray-row rule).

**Relationship.** One Category has many Items. An Item belongs to exactly one Category.

---

## 2. Item — *existing catalog, reshaped*

`RequestItemDefinition` in `MTM_Waitlist.Settings/Models`, produced by `RequestItemCatalog.Items`. Twenty-four
rows. This feature keeps the identity and display columns and **moves the behaviour columns off it** (§3).

| Property | Type | Source | Notes |
|---|---|---|---|
| `Id` | `string` | CSV col 3 | the stable Item code, e.g. `pickup-coil`, `other` |
| `Category` | `RequestCategory` | CSV col 1 | exactly one |
| `Order` | `int` | CSV col 2 | position within the Category |
| `DisplayNameResourceKey` | `string` | **new** | resource key for the display name; replaces the literal |
| `NormalizedName` | `string` | CSV col 5 | kept |
| `UmbrellaVerb` | `string` | CSV col 6 | card Line 1; `Wrong Coil Bring:` / `Wrong Flatstock Bring:` for the two wrong-material Items, `Pickup Die:` / `Deliver Die:` for the die Items |
| `CardLine1Template` | `string` | CSV col 6 | **new, optional** — the first line as a template, whose `{umbrella}` token is `UmbrellaVerb`; the die Items read `{umbrella} {job_part_number}`. Empty means the phrase alone |
| `CardLine2Template` | `string` | CSV col 11 | **new** — the identifier template (§7) |
| `ProducedValue` | `string` | CSV col 18 | kept — the value the request stores, distinct from what the card displays |
| ~~`ValueType`~~ | — | CSV col 10 | **moved** to `RequestItemConfiguration.AnswerValueType` |
| ~~`NeedsUserEntry`~~ | — | CSV col 12 | **moved** to `RequestItemConfiguration.RequiresAnswer` |

**The twenty-four codes, verbatim.** Pickup — `pickup-coil`, `pickup-die`, `pickup-component`, `pickup-fg`,
`pickup-ncm`, `pickup-wip`, `pickup-outside-service`, `pickup-riser-table`, `pickup-dunnage`, `pickup-scrap`,
`pickup-hopper`. Deliver — `deliver-coil`, `deliver-riser-table`, `deliver-hopper`, `deliver-flatstock`,
`deliver-component`, `deliver-die`, `deliver-dunnage`, `deliver-wrong-coil`, `deliver-wrong-flatstock`. Assist —
`assist-coil-turn`, `assist-table-place`, `assist-table-remove`. Other — `other`.

**In scope: twenty.** **Out of scope, catalogued but never offered: `pickup-fg`, `pickup-ncm`, `pickup-wip`,
`pickup-outside-service`** — excluded by the visibility rules, not by deletion (FR-028).

**Validation.** The catalog is asserted as it is today: twenty-four rows, the Category split (11 / 9 / 3 / 1), the
per-Category order, the umbrella verb, and the Line 2 template. These are identity assertions and stay. No test may
assert an Item's *field list* or *flow* — those are configuration (§3) and deliberately mutable (FR-015).

**Relationship.** Every Item must have exactly one `RequestItemConfiguration` row (FR-014).

---

## 3. Item configuration — **new entity, new table**

Stores `waitlist_request_item_configs`. One row per Item, twenty-four rows. This is where "how far the flow goes,
whether an answer is required, the prompt, the length limits, the options and the fields the Item's page shows" come
from (FR-013), and it is what makes each of those changeable as data (FR-015).

| Column | Type | Null | Meaning / validation |
|---|---|---|---|
| `id` | `BIGINT` | no | `PRIMARY KEY AUTO_INCREMENT` |
| `public_id` | `CHAR(36)` | no | `uq_waitlist_request_item_configs_public_id` |
| `item` | `VARCHAR(64)` | no | the Item code. `uq_waitlist_request_item_configs_item` — one row per Item |
| `category` | `VARCHAR(16)` | no | the Category the Item belongs to — `Pickup`, `Deliver`, `Assist` or `Other` |
| `control_flow` | `VARCHAR(64)` | no | `direct-to-confirmation` or `collect-input-then-confirm`. Default `direct-to-confirmation` |
| `requires_answer` | `TINYINT(1)` | no | default `0` |
| `answer_value_type` | `VARCHAR(16)` | yes | `enum` or `text` where an answer is required; null otherwise |
| `prompt_text` | `VARCHAR(500)` | yes | the question shown |
| `min_length` | `INT` | no | default `0`; applies to a text answer |
| `max_length` | `INT` | no | default `200`; applies to a text answer |
| `options_json` | `JSON` | yes | the ordered options for an enumerated answer |
| `detail_fields_json` | `JSON` | yes | the ordered fields the Item's page shows (§4) |
| `allotted_minutes` | `INT` | yes | the Item's configured allotment. **Null means not configured** → the 15-minute default, labelled as a default (FR-017) |
| `created_utc` | `DATETIME` | no | |
| `updated_utc` | `DATETIME` | no | |

**Both directions of disagreement are defined** (§16.3):

- **An Item in the catalog with no configuration row** → the Item is reported **unavailable** in plain language,
  never half-configured and never a crash (FR-014, FR-026).
- **A configuration row whose `item` is not in the catalog** → the row is **ignored** and never offered; nothing
  breaks (spec Edge Cases).

**Read once.** The whole table is read once as the wizard's Item step is entered, not per keystroke and not per row
render. The read is `sp_waitlist_request_item_configs_get`; the minutes editor writes through
`sp_waitlist_request_item_allotted_minutes_update`. Both are accompanied by paired `create.sql` / `rollback.sql`, and
`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql` and `Bootstrap/update_table_descriptions.sql` move in the same change
(FR-024, FR-025).

**Seed.** One row for every one of the twenty-four Items, so a missing row is the exception rather than the normal
case, and so `FR-014`'s "every Item MUST have a configuration row" is true of the shipped data. The four
out-of-scope Items are seeded too and hidden by rule — a missing row and a hidden Item are different states and must
not be conflated.

---

## 4. Item field definition — new *embedded* shape

An element of `detail_fields_json`. These are the fields the Item's page shows (FR-007 — they appear on the request
page and never on the card), and they are the per-Item content FR-027 sources solely from the configuration
spreadsheet.

| Property | Meaning |
|---|---|
| `label` | the resource key or literal shown beside the value |
| `value_type` | the approved `value_type` name — `string`, `text`, `enum` |
| `source` | where the value comes from: a job field, the captured answer, or a fixed value |
| `list` | **new, optional** — the job-derived list this field's choices come from, by name (`component`, `dunnage`). Absent means the job's component list, which is what the existing rows rely on; a name nothing supplies yields no choices, which the screen reports rather than inventing a list (FR-035, FR-050) |
| `order` | position in the declared order; the page renders fields in this order |
| `is_required` | whether the page blocks confirmation without it |

**Validation and the shape of the guarantee.** The field set is **mutable by design** (FR-015, §16.15). Tests
assert the *mechanism* — that fields are read from the configuration, laid out in the declared order, with the right
value types and labels — and **never the current contents**. A test that hard-coded today's fields would turn the
next spreadsheet change into a build failure.

---

## 5. Item allotted minutes and Item observed time

Two numbers with different meanings that a screen shows side by side and must never conflate.

| | Configured | Observed |
|---|---|---|
| Entity | `allotted_minutes` on the configuration row (§3) | `RequestItemObservedTime` — **derived, never stored** |
| Source | seeded, then edited on the minutes screen | `sp_waitlist_request_item_observed_average_get` |
| Meaning | how long the Item is *allowed* | how long its completed requests *actually took* |
| Fallback | 15 minutes when null, labelled a default | no value when no request has completed |
| Written back? | yes, by the minutes editor | **never** — it is a display of observed data |

**The observed average (FR-019).** Per Item, the mean of `completed_utc - accepted_utc` over requests whose status
is `Completed`. Only completed requests count; a request with either endpoint missing is excluded; a request that was
released and later completed contributes **once**, through its final accepted → completed pair. No time window is
applied — the average covers all of an Item's completed requests.

---

## 6. Request — *reshaped*

`waitlist_requests_queue`, mirrored by `WaitlistRequest` and `WaitlistRequestDraft`.

| Column | Change | Notes |
|---|---|---|
| `request_type` | **removed** | FR-004, FR-023 |
| `subtype` | **removed** | FR-004, FR-023 |
| `category` | **added** `VARCHAR(16) NOT NULL` | one of `Pickup`, `Deliver`, `Assist`, `Other` |
| `item` | **added** `VARCHAR(64) NOT NULL` | one of the twenty-four Item codes |
| `input_value` | kept | the one value the flow captured, where an Item captures one. For a die Item it carries **which die the request is for** — the destination question retired 2026-09-20, which freed this column (D22). No column is added |
| `status` | kept | `Pending`, `Accepted`, `Completed`, `Canceled` — **unchanged, not extended** |
| `requested_utc` | kept | the "Waiting" row's anchor |
| `target_time_utc` | kept | the due value; when null, derived as `requested_utc + the Item's allotted minutes` |
| `accepted_utc`, `completed_utc`, `released_utc` | kept | the observed average reads the first two |
| the requester, work centre, assignment and cancellation columns | kept | the four metadata rows the card shows (FR-008) |

**Procedures.** `sp_waitlist_request_get`, `sp_waitlist_request_insert` and `sp_waitlist_request_list` carry the new
pair and stop carrying the old two. `sp_waitlist_request_types_get` and `sp_waitlist_request_subtypes_get` are
removed. `sp_waitlist_request_status_update` is untouched — which is a deliberate signal for FR-032.

**Validation.** `category` and `item` are non-null on every stored request. There is no back-fill and no migration
path: the database is reinstalled from seed, and the specification does not invent a path for rows that do not
exist.

---

## 7. The card — *reshaped from seven shapes to one*

The list row model (`SampleOrder`, populated by `WaitlistViewViewModel`) becomes the same shape for every Item. No
property of the row selects a layout; FR-006 admits no Item-based variant.

| Element | Source | Notes |
|---|---|---|
| Line 1 — umbrella phrase | `RequestItemDefinition.UmbrellaVerb` | the Category's word, or the Item's own phrase where it defines one |
| Line 2 — identifier | `CardLine2Template` resolved against the job snapshot and the captured answer | a fixed value, a value read from the job, or a value that depends on the captured answer |
| Picture | Item override → Category family → the existing placeholder | a placeholder never replaces a picture the request already resolves (FR-009, FR-021) |
| Requested by | the stored requester | unchanged |
| Press (work centre) | the stored work centre | unchanged |
| Remaining time | `target_time_utc`, else `requested_utc + allotted minutes` | bold red when overdue — this is what keeps overdue work visible under every sort (FR-012) |
| Waiting | `requested_utc` → now | unchanged |
| Status pill, new-message marker, action area | unchanged from `specs/003` | the lifecycle does not change with the Item (FR-032) |

**Line 2 resolution.** The template's tokens come from a closed set — job-derived (`part_number`, `part_description`,
`die_number`, `die_location`, `dunnage_part`, `sequence_number`, `scrap_type`) and captured (`answer`, `component`,
`defect`). A die Item's line is composed once from the die's own number and its **home location** by
`RequestDiePart.ComposeLabel`, with no conditional on any answer: the destination question its old switch turned on
retired 2026-09-20 (D22), and `destination` left the token set with it. An unresolvable token renders the Item's own
display name and reports the configuration problem rather than rendering a blank (FR-026).

**Where each job-derived token's value comes from (2026-09-22).** `part_number` is the part the request is **about**:
the material the job holds where the Item is about one — its coil (`MMC`) or its flatstock (`MMF`), with the merged
`pickup-coil` taking the coil the job holds and falling back to its flatstock on a flatstock-only job (D21) — and the
**job's own part number** where the Item is about no particular material (the two table assists and the four
out-of-scope Items), which is the value the request page shows for a declared `Part` row. Which material an Item is
about is `RequestItemPickerRules.RequiredJobPart`, the same rule that gates whether the Item is offered at all.
`scrap_type` is the type the job's **real** scrap decision names (FR-030, FR-031 — the same rule the Scrap Item's
visibility gates on), and `component` is the component the operator chose, read from the request's captured answer
exactly as `dunnage_part` is (FR-035). The job's coil number, its flatstock number and its scrap type travel on
`RequestJobPartAvailability` for this purpose. Before this, **no source supplied any of the three tokens**, so every
card whose identifier named one showed the Item's own display name — a `deliver-component` request read `Deliver` /
`Component` where it should read `Deliver` / `V-EMB-2`. A job that holds nothing for the token still leaves it
unresolved, and the card reports that rather than inventing a part.

**The detail-field grid is not on the card.** It moves to the request page, where the declared fields (FR-007) are
laid out in declared order, two per row; a field alone on its row takes that row's full width. The span is derived
from the declared field count, never from the Item's identity — which is the resolution of the design document's
"full-width 'Other' card" note under FR-006.

---

## 8. Sort preference — **new entity**

| Property | Meaning |
|---|---|
| Owner | the viewer — per person, always |
| Store | the existing `ILocalSettingsService` (the mechanism the spec names), through `IWaitlistSortPreferenceService`; **not** a new table and not the database |
| Value | one of five option keys |
| Default on first run | **most urgent** (FR-010) |
| Persistence | survives an application restart (SC-009) |

**The five options, most urgent first, verbatim:** most urgent (the default), longest waiting, press, requested by,
status.

**Distinctness.** *Longest waiting* and *most urgent* are genuinely different orders, not synonyms: waiting is
`requested_utc` → now (the card's **Waiting** row) while urgency derives from the due value (the card's **Remaining
time** row). They diverge by exactly the Item's allotment, and this feature is what creates that divergence, because
it is the change that gives every Item its own minutes.

**Invariant.** Whatever the sort, an overdue row stays visibly marked as overdue (FR-012) and the most overdue
remain findable.

---

## 9. Image location — *re-keyed*

`config_images_locations` already keys an override by (`scope`, `scope_item_id`), and `ImageLocationScope` already
enumerates the scopes. Two values are added and two retire; the column is a `VARCHAR`, so this is a data change,
not DDL.

| Scope value | Change | Key |
|---|---|---|
| `request_item` | **added** | the Item code |
| `request_category` | **added** | the Category code — the family a card falls back to |
| `work_center` | kept | unchanged; work-centre images still use it |
| `request_subtype` | **retired** | with the vocabulary |
| `request_type` | **retired** | with the vocabulary |

---

## 10. The retiring catalog — *the design-time mapping source*

`waitlist_request_types` and `waitlist_request_subtypes` each already carry a populated `category` and `item_id`
column. They are the mapping from every existing request to its Item, and they are **consumed at design time** when
the new seed is authored, then removed — together with `seed_waitlist_request_catalog`,
`RequestSubtypeInventory`, `RequestTypeCatalogService`, `IRequestSubtypeDisplayLabelService` /
`RequestSubtypeDisplayLabelService`, and the request's `request_type` / `subtype` columns. That consumption is the
authoring of a seed, not migration code.

`RequestItemLegacyMapper` and `ImageLocationScope` are **inspected, not assumed dead**: the mapper may still back
the seed rewrite, and work-centre images may still use the scope. Neither is deleted on the strength of its name.

---

## State transitions

**The request lifecycle does not change (FR-032).** The stored statuses `Pending`, `Accepted`, `Completed`,
`Canceled` and their legal transitions are exactly as they are, whichever Item was chosen. The friendly labels the
card shows — `Waiting`, `In Progress`, `Done`, `Cancelled` — remain display text only and must not be written to the
store. No new status, no new transition, and no change to `sp_waitlist_request_status_update`.

**The picker's own progression** is the one state machine this feature reshapes:

```text
Work Centre ──▶ Category ──▶ [availability resolved for the requesting job]
                                    │
                                    ├──▶ Item list built from what the job supports
                                    │
                             ──▶ Item ──▶ Details ──▶ Preview ──▶ Summary ──▶ Result
                                            │
                                            └── renders the Item's configured fields,
                                                prompt and limits; captures the one answer
```

The availability snapshot is resolved **between** the Category and the Item step — never after the Item is chosen —
so an unsupported Item is never offered and then refused (FR-002). An Item with no configuration row stops the
flow at the Item step with a plain-language unavailable report rather than proceeding half-configured (FR-014,
FR-026).
