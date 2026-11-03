# Contract: Queue Payload

What a generic request writes and what the queue now reads back. There is one write procedure for both kinds of
request, so a generic request is not a second persistence path (FR-009, constitution III).

Sources: [spec.md](../spec.md) FR-008, FR-009, FR-013 to FR-019, [plan.md](../plan.md) "Interaction path",
[research.md](../research.md) R1, R2 and R3, [data-model.md](../data-model.md) `waitlist_requests_queue`.

## `sp_waitlist_request_insert`

The parameter list after this change, in order. The four last entries are new; every earlier entry keeps its
position and type so an existing caller's positional bind does not shift.

| # | Parameter | Type | Supplies |
| --- | --- | --- | --- |
| 1 | `p_public_id` | `CHAR(36)` | the request's public identifier |
| 2 | `p_building` | `VARCHAR(64)` | the requester's building |
| 3 | `p_work_center` | `VARCHAR(64)` | the work center code |
| 4 | `p_category` | `VARCHAR(16)` | the chosen category |
| 5 | `p_item` | `VARCHAR(64)` | the chosen item code |
| 6 | `p_input_value` | `VARCHAR(255)` | the configured answer — the request's stated value |
| 7 | `p_active_setup_job_id` | `VARCHAR(64)` | the active setup job; `NULL` for a generic request |
| 8 | `p_work_center_name` | `VARCHAR(64)` | the work center's name |
| 9 | `p_requester_employee_number` | `VARCHAR(32)` | the verified requester |
| 10 | `p_requester_employee_name` | `VARCHAR(128)` | the verified requester |
| 11 | `p_status` | `VARCHAR(32)` | `Pending` for a new request |
| 12 | `p_requested_utc` | `DATETIME` | when the request was raised |
| 13 | `p_target_time_utc` | `DATETIME` | the needed-by answer (FR-015) |
| 14 | `p_is_overdue` | `TINYINT` | `0` for a new request |
| 15 | `p_assigned_material_handler` | `VARCHAR(128)` | no handler yet |
| 16 | `p_cancellation_reason` | `VARCHAR(255)` | none |
| 17 | `p_note` | `VARCHAR(500)` | the handler's own note, never the requester's description |
| 18 | `p_answers_json` | `JSON` | the answers beyond `p_input_value`, keyed by the label the item declared |
| 19 | `p_is_generic` | `TINYINT` | `1` for a generic request, `0` otherwise |
| 20 | `p_description` | `VARCHAR(500)` | the requester's optional description (FR-016) |
| 21 | `p_accepted_warnings_json` | `JSON` | the warnings the requester accepted, as a JSON array (FR-019) |

The procedure writes `created_utc` and `updated_utc` itself. A blank text argument is stored as `NULL` rather than as
an empty string, except `status`, which falls back to `Pending`. `p_description` is written separately from
`p_note` and from `p_answers_json`: `note` is already the handler's own note through `UpdateNoteAsync`, and
`answers_json` is keyed by the label an item declared, so a description stored in either would arrive as something it
is not (R3).

## Columns a generic request writes

| Column | Value for a generic request | Supplied by |
| --- | --- | --- |
| `public_id` | a new identifier | the request service, as for every request |
| `building` | the work center's building | the work center step, carried on the draft |
| `work_center` | the work center code | the work center step |
| `work_center_name` | the work center's name | the work center step |
| `category` | the chosen category | the category step |
| `item` | the chosen item code | the item step |
| `input_value` | **the configured answer** the requester gave | the item step (FR-007) |
| `answers_json` | the answers beyond `input_value`, keyed by declared label, or `NULL` | the steps that asked them |
| `is_generic` | `1` | the flow itself — the request was raised through the generic flow |
| `description` | the optional description, or `NULL` | the details step (FR-016) |
| `accepted_warnings_json` | the accepted warnings as a JSON array, or `NULL` | the summary step (FR-019) |
| `target_time_utc` | the needed-by answer | the details step (FR-015) |
| `requester_employee_number` | the signed-in employee number | `IPersonIdentity`, confirmed by `IEmployeeDirectoryService` (FR-020, A2) |
| `requester_employee_name` | the verified employee name | `IPersonIdentity` plus `IEmployeeDirectoryService` (FR-020) |
| `status` | `Pending` | the flow; the initial lifecycle state is unchanged (FR-027) |
| `active_setup_job_id` | `NULL` | the flow — no job is read and no work order is stored (FR-008, A5) |
| `requested_utc` | when the request was raised | the submission |
| `is_overdue` | `0` | the flow |
| `assigned_material_handler` | `NULL` | the flow |
| `cancellation_reason` | `NULL` | the flow |
| `note` | `NULL` | the flow — the description is not a note (R3) |

`active_setup_job_id` accepts `NULL` and `is_generic` carries the kind as a stored fact rather than an inference from
a null (R2). A work-order request is written exactly as it is today: `is_generic = 0`, a job id present, and the two
new text columns `NULL`.

## Reads that now return the new columns

| Procedure | Returns | Why |
| --- | --- | --- |
| `sp_waitlist_request_list` | `is_generic`, `description`, `accepted_warnings_json`, plus every column it returns today | the board tells the two kinds apart and draws the generic card from the row it already read (FR-022, FR-024, R10) |
| `sp_waitlist_request_get` | `is_generic`, `description`, `accepted_warnings_json`, plus every column it returns today | the detail page adds the description and the accepted warnings to the card's facts (FR-026) |
| `sp_setup_work_centers_catalog_get` | `is_generic`, plus `id`, `work_center_name`, `building`, `sort_rank`, `is_active` | the work center step reads the designation with the catalogue it already loads, so no second read is added (FR-001, R8) |

Each of the three adds columns to its result rather than changing an existing one, so a caller that maps by name
keeps working and a caller that ignores the new columns reads what it read before. `sp_setup_work_centers_catalog_get`
projects the column its view already exposes; the read simply did not select it.

## Artifacts, in one change

Every changed artifact ships its pair in the same folder:

```text
Database/Tables/18_waitlist_requests_queue/create.sql
Database/Tables/18_waitlist_requests_queue/rollback.sql
Database/StoredProcedures/sp_waitlist_request_insert/create.sql
Database/StoredProcedures/sp_waitlist_request_insert/rollback.sql
Database/StoredProcedures/sp_waitlist_request_list/create.sql
Database/StoredProcedures/sp_waitlist_request_list/rollback.sql
Database/StoredProcedures/sp_waitlist_request_get/create.sql
Database/StoredProcedures/sp_waitlist_request_get/rollback.sql
Database/StoredProcedures/sp_setup_work_centers_catalog_get/create.sql
Database/StoredProcedures/sp_setup_work_centers_catalog_get/rollback.sql
```

`rollback.sql` restores the artifact as it was before this change: the table's pair drops the three new columns and
restores `active_setup_job_id` to `NOT NULL`, and each procedure's pair restores the parameter list or the result it
had. The aggregate scripts are regenerated in the same change so nothing is left describing the previous shape:

- `Database/Tables/AllTables.sql`
- `Database/StoredProcedures/AllSPs.sql`
- `Database/Bootstrap/update_table_descriptions.sql`

`Database/Database-Ruleset.md` records the conventions the pair follows. No `Database/**` script is run against a
store to produce this contract; the artifacts are the deliverable.
