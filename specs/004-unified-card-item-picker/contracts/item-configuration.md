# Contract — Item configuration

The interface between the store and the application for everything an Item does. Every identifier below is copied
verbatim from the spec's Verbatim Constraints and from the spec's Key Entities; none may be renamed, recased or
pluralized.

## 1. Stored artifacts

| Artifact | Kind | Path |
|---|---|---|
| `waitlist_request_item_configs` | table | `Database/Tables/31_waitlist_request_item_configs/{create,rollback}.sql` |
| `sp_waitlist_request_item_configs_get` | procedure | `Database/StoredProcedures/sp_waitlist_request_item_configs_get/{create,rollback}.sql` |
| `sp_waitlist_request_item_allotted_minutes_update` | procedure | `Database/StoredProcedures/sp_waitlist_request_item_allotted_minutes_update/{create,rollback}.sql` |
| `sp_waitlist_request_item_observed_average_get` | procedure | `Database/StoredProcedures/sp_waitlist_request_item_observed_average_get/{create,rollback}.sql` |
| `seed_waitlist_request_item_configs` | seed | `Database/Seeds/seed_waitlist_request_item_configs/{create,rollback}.sql` |

Every one of them ships a paired `create.sql` / `rollback.sql`, and the same change updates
`Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql` and
`Database/Bootstrap/update_table_descriptions.sql` (FR-025).

## 2. `sp_waitlist_request_item_configs_get`

**Parameters.** None.

**Returns.** One row per configured Item, all columns of `waitlist_request_item_configs` except `id`:
`public_id`, `item`, `category`, `control_flow`, `requires_answer`, `answer_value_type`, `prompt_text`, `min_length`,
`max_length`, `options_json`, `detail_fields_json`, `allotted_minutes`.

**Consumer contract.**

- The result is read **once**, as the Item step is entered — never per keystroke and never per row render.
- `detail_fields_json` and `options_json` are JSON strings parsed by the application, not by SQL. The schema targets
  MySQL 5.7, where `JSON_TABLE` is unavailable, and parsing in the application is the pattern
  `RequestTypeCatalogService` already uses for `center_data_grid_fields_json`.
- A malformed JSON payload is a reportable configuration problem, not an empty list and not an exception that
  escapes to the screen (FR-026).
- **A catalogued Item with no returned row** is reported as **unavailable** in plain language — never
  half-configured and never a crash (FR-014).
- **A returned row whose `item` is not in the catalog** is ignored and never offered (spec Edge Cases).

## 3. `sp_waitlist_request_item_allotted_minutes_update`

**Parameters.** `p_item VARCHAR(64)`, `p_allotted_minutes INT`, plus the auditing columns the repo's other update
procedures carry.

**Consumer contract.**

- Writes the configured allotment for one Item. Values are clamped by the caller to a positive minimum and a
  twenty-four-hour maximum, matching `UrgencySettingsService.SetMaxAllottedAsync`'s existing clamp.
- Called only from the minutes editor, and only for a person who passes that screen's **existing** role gate
  (FR-020). **No new role gate is introduced**, and the two configuration screens do **not** have their gates
  collapsed into one: today the minutes editor is governed by `CanManageUrgencySettings`
  (`UrgencyAllotmentEditorViewModel`) and the picture screen by `CanManageImageLocationSettings`
  (`SettingsViewModel`), and each keeps its own. Both are only re-keyed, from subtype to Item, which is what
  "reuse the role gate that already governs them, and introduce no new one" requires.
- Must never write back an observed average as if it were a configured value (FR-018). The observed average is
  read-only display data and never reaches this procedure.

## 4. `sp_waitlist_request_item_observed_average_get`

**Parameters.** None.

**Returns.** Per Item: `item`, request count, and the average of `completed_utc - accepted_utc`.

**Consumer contract (FR-019).**

- Counts **completed** requests only, measured from `accepted_utc` to `completed_utc`.
- A request that was released and later completed contributes **exactly once**, through its final accepted →
  completed pair.
- A request missing either endpoint is excluded.
- **No time window** is applied; the average covers all of an Item's completed requests.
- An Item with no completed request yields no value — the screen shows the configured minutes alone rather than a
  fabricated zero (FR-026).
- The value is **derived and never stored**. It is displayed beside the configured value and is never written back
  as the configured value.

## 5. The configuration the table carries

**From the store** (FR-013): how far the flow goes before confirmation (`control_flow`), whether an answer is
required (`requires_answer`), the answer's type (`answer_value_type`), the prompt (`prompt_text`), the length limits
(`min_length`, `max_length`), the options offered (`options_json`), the fields the Item's page shows
(`detail_fields_json`), and the Item's allotted minutes (`allotted_minutes`).

**From the code catalog** (§16.3): existence, Category, order, umbrella verb (card Line 1) and the identifier
template (card Line 2).

**From the visibility rules:** whether the Item is offered at all for the requesting job (FR-002).

**Verbatim values.** `control_flow` is `direct-to-confirmation` or `collect-input-then-confirm`. `answer_value_type`
is `enum` or `text`. `category` is one of `Pickup`, `Deliver`, `Assist`, `Other`. `item` is one of the twenty-three
Item codes.

**The `list` key — where an enumerated answer's choices come from (FR-035, FR-050).** A field inside
`detail_fields_json` may carry one further key, `list`, naming the job-derived list its choices come from. It is a
**name**, never the Item's identity, so two Items configured the same way behave the same way:

| `list` value | the choices are |
|---|---|
| `component` | the requesting job's component part numbers |
| `dunnage` | the dunnage parts assigned to the requesting job |
| absent | the job's component list — the path the existing rows already rely on |
| any other name | **nothing**: the screen reports that rather than inventing a list |

`list` is **data**: adding or changing it is a one-row edit and needs no rebuild (FR-015). It is what keeps the
dunnage step from being selected by an Item code in code — the step is chosen because the row names the `dunnage`
list.

## 6. The default allotment

`15` minutes, when `allotted_minutes` is null. The value is a constant in the application, and the surface that
shows a deadline derived from it **labels it as a default rather than as a configured value** (FR-017). The Item
keeps its place in the urgency order (SC-007).

## 7. What the configuration must never become

- **Never a shipped input from the spreadsheet.** FR-027: the per-Item field definitions come solely from
  `WeekendProject/Documents/Request-Config-Template.csv`, used once when the configuration is authored. The
  application must not read, embed, parse or ship that file, and no build step may depend on it. The same applies
  to `unified-item-picker-workflows.md`. This is enforced by an audit test, not by intent (§D19).
- **Never a frozen field list.** The field set is mutable by design: adding or changing an Item's fields is a data
  change and must not require a rebuild (FR-015). Tests assert the mechanism — fields are read from configuration in
  the declared order with the right value types and labels — and never today's contents.
- **Never a legacy fallback.** The retiring `center_data_grid_fields_json` is not a second source, and there is no
  precedence rule to arbitrate because there is only one source (§16.15).
