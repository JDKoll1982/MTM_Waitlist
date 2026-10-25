# Contract: persistence and permission

The stored-procedure seam and the permission this feature adds. The eleven procedures with their
parameters, result columns and guarantees, and the permission with its holders, are pinned here so a
caller, a test and the schema are written against the same names. Every string comes from `spec.md`
(Requirements and Verbatim Constraints), `plan.md` (Project Structure and Call Paths) or
`research.md` (D2 to D8).

Engine is MySQL 5.7. Every artifact ships `create.sql` beside `rollback.sql`, and every artifact is
listed in `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql` and
`Database/Seeds/AllSeeds.sql`, with `Database/Bootstrap/update_table_descriptions.sql` updated in the
same change.

The column names below are the ones `data-model.md` defines. Where the two files could drift, the
data model is the source and this file restates it.

## 1. The three tables

Paths follow the plan: `Database/Tables/36_waitlist_lugger_locations/`,
`Database/Tables/37_waitlist_lugger_scrap_assignments/` and
`Database/Tables/38_waitlist_lugger_arrangement_changes/`.

### `waitlist_lugger_locations`

| Column | Type | Holds |
|---|---|---|
| `id` | `BIGINT` auto increment, primary key | the row |
| `public_id` | `CHAR(36)` not null, unique | the identifier a caller and a test use |
| `lugger_name` | `VARCHAR(64)` not null, unique | the name a card shows, for example `Door 6 Lugger` |
| `status` | `VARCHAR(16)` not null, default `Backup` | exactly one of `Available`, `Backup`, `Full`, `Do Not Use` |
| `status_reason` | `VARCHAR(500)` null | why the lugger is `Do Not Use`, required for that status and null for the others |
| `display_position` | `INT` not null | the lugger's place in the set's display order |
| `created_utc` | `DATETIME` not null | when it was added |
| `updated_utc` | `DATETIME` not null | when it last changed |

Name uniqueness is compared by the table's `utf8mb4_unicode_ci` collation, so `Door 6 Lugger` and
`door 6 lugger` are the same name and the second insert is refused (FR-004). Removal deletes the row
(research D3) and the foreign key below takes its assignments with it (FR-005). A newly added lugger is
written as `Backup`, because it holds no dump point until a scrap type's order makes it one.

### `waitlist_lugger_scrap_assignments`

| Column | Type | Holds |
|---|---|---|
| `id` | `BIGINT` auto increment, primary key | the row |
| `public_id` | `CHAR(36)` not null, unique | the identifier a caller and a test use |
| `lugger_id` | `BIGINT` not null, foreign key to `waitlist_lugger_locations(id)` `ON DELETE CASCADE` | the lugger the type dumps through |
| `scrap_type` | `VARCHAR(128)` not null | the spelling the lead entered, for example `5052 Aluminum` |
| `scrap_type_key` | `VARCHAR(128)` not null | `LOWER(TRIM(scrap_type))`, the key every lookup matches on |
| `is_dump_point` | `TINYINT(1)` not null, default 0 | whether this pairing holds the dump point for its scrap type |
| `sort_position` | `INT` not null | where the lugger sits in that scrap type's try order |
| `created_utc` | `DATETIME` not null | when it was assigned |
| `updated_utc` | `DATETIME` not null | when it last changed |

Keys: `uq_waitlist_lugger_scrap_assignments_public_id` and
`uq_waitlist_lugger_scrap_assignments_type_lugger (scrap_type_key, lugger_id)`, so one pairing exists
once and the same lugger cannot appear twice for one scrap type. At most one `is_dump_point = 1` row
per `scrap_type_key` is enforced by the procedures, in one transaction, not by a key. The status lives
on the lugger and the role lives on the pairing, which is research D2.

### `waitlist_lugger_arrangement_changes`

| Column | Type | Holds |
|---|---|---|
| `id` | `BIGINT` auto increment, primary key | the notice watermark and the poll cursor |
| `public_id` | `CHAR(36)` not null, unique | the identifier a test uses |
| `change_kind` | `VARCHAR(32)` not null | one of the nine kinds below |
| `lugger_id` | `BIGINT` null | a cross reference to the lugger as it stands, null after a removal |
| `lugger_name` | `VARCHAR(64)` not null | the lugger's name as text, so the record outlives the row |
| `scrap_type` | `VARCHAR(128)` null | the affected scrap type as spelled at the time, null when none |
| `scrap_type_key` | `VARCHAR(128)` null | the normalised key, null when none |
| `previous_value` | `VARCHAR(255)` null | the value before the change |
| `new_value` | `VARCHAR(255)` null | the value after the change |
| `change_reason` | `VARCHAR(500)` null | the `Do Not Use` reason, kept after the lugger row clears it |
| `changed_by_employee_number` | `VARCHAR(32)` not null | who made the change (FR-032) |
| `changed_by_employee_name` | `VARCHAR(128)` not null | the same person's name, so the notice reads without a join |
| `changed_utc` | `DATETIME` not null | when the change was made (FR-032) |

The nine `change_kind` values, and what `previous_value` and `new_value` carry for each:

| `change_kind` | `previous_value` | `new_value` |
|---|---|---|
| `lugger_added` | null | the new name |
| `lugger_renamed` | the old name | the new name |
| `lugger_removed` | the name it had | null |
| `status_changed` | the status before | the status after |
| `dump_point_promoted` | the name of the lugger that held the role | the name of the lugger that holds it now |
| `assignment_added` | null | the lugger's name |
| `assignment_removed` | the lugger's name | null |
| `assignment_reordered` | the previous `sort_position` | the new `sort_position` |
| `lugger_reordered` | the previous `display_position` | the new `display_position` |

There is no foreign key on `lugger_id`: a cascade would delete the audit with the lugger, and a
`SET NULL` would rewrite history. `lugger_name` is the durable identifier and `lugger_id` a
convenience. The table is append only; no procedure updates or deletes a row.

## 2. The twelve procedures

All eleven live under `Database/StoredProcedures/<name>/` with `create.sql` and `rollback.sql`.
Parameters keep the repository's `p_` prefix.

Conventions shared by the writes:

- A write returns one result set with one column, `affected_rows`, which is 0 when nothing matched, so
  a caller can tell a lost race from a success.
- A refusal raises `SIGNAL SQLSTATE '45000'` with a plain message, and the application reports that
  message as the refusal rather than as a success.
- Every write runs in one transaction and inserts its own change rows inside it (research D6).
- `p_actor_employee_number` and `p_actor_employee_name` are passed by every write and land in the
  change row.

### `sp_waitlist_lugger_locations_list`

Parameters: none.

Result columns: `id`, `public_id`, `lugger_name`, `status`, `status_reason`, `display_position`,
`created_utc`, `updated_utc`.

Order: `display_position` ascending, then `lugger_name` ascending.

Guarantee: one row per lugger that exists. A removed lugger has no row. `status` is always one of the
four pinned strings.

### `sp_waitlist_lugger_locations_insert`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_name` | `VARCHAR(64)` | the name the lead typed |
| `p_display_position` | `INT` | where it sits in the display order |
| `p_actor_employee_number` | `VARCHAR(32)` | who added it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: trims the name and refuses a blank one. Refuses a name already held, under the
case-insensitive unique key. Inserts with status `Backup` and a null reason. Writes one `lugger_added`
change row with both `scrap_type` columns null.

### `sp_waitlist_lugger_locations_rename`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_public_id` | `CHAR(36)` | the lugger to rename |
| `p_lugger_name` | `VARCHAR(64)` | the new name |
| `p_actor_employee_number` | `VARCHAR(32)` | who renamed it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`, which is 0 when the lugger does not exist.

Guarantee: writes the new name, refusing a blank one and a name another lugger already holds. Writes
one `lugger_renamed` row per affected scrap type, each carrying the old name in `previous_value` and
the new name in `new_value`, so the notice names both (FR-035). A lugger that takes no scrap type
writes one row with both `scrap_type` columns null. Every card that names the lugger reads the new
name on its next load (FR-023).

### `sp_waitlist_lugger_locations_position_set`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_public_id` | `CHAR(36)` | the lugger to move |
| `p_display_position` | `INT` | the 1-based place it should hold |
| `p_actor_employee_number` | `VARCHAR(32)` | who moved it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`, which is 0 when the lugger does not exist.

Guarantee: the requested place is clamped to the arrangement that exists, and the whole order is
renumbered contiguously from 1 in one transaction, so two luggers can never share a place and no hole
is left behind a move. A move that asks for the place the lugger already holds changes nothing and
records nothing; a real move writes one `lugger_reordered` row naming the old and the new position.
That kind is **not** announced to handlers: a display position sends nobody anywhere differently, and
FR-033 does not list it among the changes a handler is told.

### `sp_waitlist_lugger_locations_delete`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_public_id` | `CHAR(36)` | the lugger to remove |
| `p_actor_employee_number` | `VARCHAR(32)` | who removed it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: records the affected scrap types before deleting, then deletes the lugger, whose
assignments go with it through the cascade, so no assignment points at a lugger that no longer exists
(FR-005). Writes one `lugger_removed` row per affected scrap type and one `dump_point_promoted` row
where another lugger took the role. A scrap type that loses its last lugger is left with no
assignment, and its cards draw no lugger cell until the lead assigns one (FR-026).

### `sp_waitlist_lugger_locations_status_set`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_public_id` | `CHAR(36)` | the lugger |
| `p_status` | `VARCHAR(16)` | one of `Available`, `Backup`, `Full`, `Do Not Use` |
| `p_reason` | `VARCHAR(500)` | the reason, required for `Do Not Use` |
| `p_actor_employee_number` | `VARCHAR(32)` | who set it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: one transaction. It refuses any status outside the four pinned strings. It refuses
`Do Not Use` with a blank reason and clears the reason for the other three (FR-019). It then
re-derives every scrap type the lugger is assigned to and leaves each one complete: exactly one dump
point whenever a lugger can receive material, no blocked lugger ordered above the dump point, and never
two dump points (FR-013, FR-017, FR-020, FR-021). It holds each affected scrap type's assignment rows
with `SELECT ... FOR UPDATE` for the whole transaction, which is what makes two leads changing at the
same moment produce one outcome rather than a half-moved arrangement.

The transitions, matching `plan.md`:

| From | To | Effect |
|---|---|---|
| `Available` | `Full` | the next usable lugger in that scrap type's order becomes the dump point, and the blocked lugger sorts after every usable one |
| `Available` | `Do Not Use` | the same promotion, with the reason recorded |
| `Backup` | `Full` | the affected scrap types re-derive, which changes nothing where the lugger held no role |
| `Full` | `Backup` | the current dump point keeps the role and the emptied lugger does not reclaim it |
| `Do Not Use` | `Available` | the reason is cleared and kept on the change row, and the current dump point keeps the role |
| `Backup` | `Available` | reachable only through `sp_waitlist_lugger_locations_promote` |

Writes one `status_changed` row per affected scrap type and one `dump_point_promoted` row for each
scrap type whose role moved.

### `sp_waitlist_lugger_locations_promote`

| Parameter | Type | Meaning |
|---|---|---|
| `p_lugger_public_id` | `CHAR(36)` | the lugger the lead chose |
| `p_scrap_type_key` | `VARCHAR(128)` | the scrap type it is promoted for |
| `p_actor_employee_number` | `VARCHAR(32)` | who promoted it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: makes the chosen lugger the dump point for that scrap type, sets it to `Available`, and
returns the previous dump point to `Backup` (FR-018). Refuses when the lugger is not assigned to that
scrap type, and refuses a lugger that is `Full` or `Do Not Use`, because a blocked lugger cannot
receive material. Writes one `dump_point_promoted` row naming both luggers.

### `sp_waitlist_lugger_scrap_assignments_list`

| Parameter | Type | Meaning |
|---|---|---|
| `p_scrap_type_key` | `VARCHAR(128)` | the normalised scrap type to read |

Result columns: `id`, `public_id`, `scrap_type`, `scrap_type_key`, `lugger_public_id`, `lugger_name`,
`status`, `status_reason`, `is_dump_point`, `sort_position`.

Order: `sort_position` ascending, then `lugger_name` ascending.

Guarantee: one row per assignment of that scrap type, each carrying its lugger's current status. An
unassigned scrap type returns no row.

### `sp_waitlist_lugger_scrap_assignments_set`

| Parameter | Type | Meaning |
|---|---|---|
| `p_scrap_type` | `VARCHAR(128)` | the scrap type as the lead entered it |
| `p_lugger_public_id` | `CHAR(36)` | the lugger to assign |
| `p_sort_position` | `INT` | the position in that scrap type's try order |
| `p_actor_employee_number` | `VARCHAR(32)` | who assigned or reordered it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: writes the entered spelling in `scrap_type` and `LOWER(TRIM(p_scrap_type))` in
`scrap_type_key`, in the same statement, so the two cannot disagree. An existing pairing is updated in
place, keyed on `(scrap_type_key, lugger_id)`. Refuses `Scrap Type Required` and `No Scrap` under any
case or surrounding spaces (FR-010). Accepts a type the shipped list does not name (FR-011).
Re-derives the scrap type's dump point: the lugger the lead promoted if there is one, otherwise the
first usable lugger in the new order (research D8). Writes `assignment_added` or
`assignment_reordered`, plus `dump_point_promoted` when the role moved.

### `sp_waitlist_lugger_scrap_assignments_delete`

| Parameter | Type | Meaning |
|---|---|---|
| `p_scrap_type_key` | `VARCHAR(128)` | the normalised scrap type |
| `p_lugger_public_id` | `CHAR(36)` | the lugger to unassign |
| `p_actor_employee_number` | `VARCHAR(32)` | who unassigned it |
| `p_actor_employee_name` | `VARCHAR(128)` | the same person's name |

Result columns: `affected_rows`.

Guarantee: removes the pairing. When it held the dump point, the first usable lugger remaining in that
order takes the role. When it was the last pairing for that scrap type, the scrap type holds no
assignment and its cards draw no lugger cell (FR-026). Writes `assignment_removed`, plus
`dump_point_promoted` when the role moved.

### `sp_waitlist_lugger_arrangement_list`

Parameters: none.

Result columns: `lugger_public_id`, `lugger_name`, `status`, `status_reason`, `lugger_display_position`,
`scrap_type`, `scrap_type_key`, `assignment_public_id`, `is_dump_point`, `assignment_sort_position`.

Order: `lugger_display_position` ascending, then `assignment_sort_position` ascending, then
`lugger_name`.

Guarantee: one row per lugger and assignment pairing. A lugger with no assignment appears once, with
`scrap_type`, `scrap_type_key`, `assignment_public_id`, `is_dump_point` and
`assignment_sort_position` all null. This is the one read behind the summary, the editor and the card
path, so a list load costs one call for the whole arrangement (research D1).

### `sp_waitlist_lugger_arrangement_changes_list`

| Parameter | Type | Meaning |
|---|---|---|
| `p_after_id` | `BIGINT` | the highest change id the caller has already seen |

Result columns: `id`, `public_id`, `change_kind`, `lugger_name`, `scrap_type`, `previous_value`,
`new_value`, `change_reason`, `changed_by_employee_name`, `changed_utc`.

Order: `id` ascending.

Guarantee: returns only rows whose `id` is greater than `p_after_id`. With a null `p_after_id` it
returns exactly one row whose `id` is the current highest change id, or 0 when no change has been
recorded, with every other column null. That call is how the client takes its watermark when the list
opens, and it is why a change made before the list opened is never replayed (FR-037).

## 3. The seeds

### `seed_waitlist_lugger_arrangement_defaults`

Path `Database/Seeds/seed_waitlist_lugger_arrangement_defaults/`, with `create.sql` and
`rollback.sql`. Registered in `Database/Seeds/AllSeeds.sql`.

The seed inserts the seven luggers and the seven assignments only when
`waitlist_lugger_locations` is empty, inside one transaction. It never truncates and never overwrites,
so a second run adds nothing and a lugger the lead renamed is left alone. It therefore lands on an
installed store without a reinstall (research D4).

The seven luggers, in `display_position` 1 to 7, named exactly as the Verbatim Constraints pin them:

| `display_position` | `lugger_name` | `status` |
|---|---|---|
| 1 | `Door 6 Lugger` | `Available` |
| 2 | `Door 7 Lugger` | `Available` |
| 3 | `Door 8 Lugger` | `Available` |
| 4 | `Door 9 Lugger` | `Available` |
| 5 | `Extra Lugger (Outside)` | `Backup` |
| 6 | `Skeleton Lugger` | `Available` |
| 7 | `Scrap Trailer` | `Available` |

`Available` is given to each lugger that holds a dump point in the defaults, and `Backup` to
`Extra Lugger (Outside)`, which holds none (FR-013).

The seven default assignments, exactly as the Verbatim Constraints pin them:

| `scrap_type` | `scrap_type_key` | `lugger_name` | `is_dump_point` | `sort_position` |
|---|---|---|---|---|
| `3003 Aluminum` | `3003 aluminum` | `Door 6 Lugger` | 1 | 1 |
| `3003 Aluminum` | `3003 aluminum` | `Extra Lugger (Outside)` | 0 | 2 |
| `Galvanized Steel` | `galvanized steel` | `Door 7 Lugger` | 1 | 1 |
| `5052 Aluminum` | `5052 aluminum` | `Door 8 Lugger` | 1 | 1 |
| `Steel` | `steel` | `Door 9 Lugger` | 1 | 1 |
| `Skeleton` | `skeleton` | `Skeleton Lugger` | 1 | 1 |
| `Gaylord` | `gaylord` | `Scrap Trailer` | 1 | 1 |

The setup workflow spells one type `5052 aluminum`, and the assignment is pinned as `5052 Aluminum`.
The two resolve to the same assignment because matching ignores letter case. Matching also ignores
surrounding spaces and nothing else: inner whitespace is not collapsed, so a genuinely different
spelling stays a different type (FR-009, research D5).

### `seed_permission_role_baselines`

Amended in place, not re-created. The file gains nine rows, one per role the catalogue holds, for the
new key, and its rollback removes those nine rows.

## 4. The permission

### The key

| Item | Value |
|---|---|
| Constant | `PermissionKeys.SettingsLuggerArrangement` |
| Key | `permission.settings.lugger_arrangement` |
| Registry entry | `PermissionRegistry.Entry` with `Area.Settings` |
| Label resource key | `Permissions_Settings_Lugger_Arrangement.Label` |
| Gates resource key | `Permissions_Settings_Lugger_Arrangement.Gates` |
| Fallback | `false` |

The key is added to `MTM_Waitlist.Core/Permissions/PermissionKeys.cs` and to its `All` list, which
takes the declaration from nineteen keys to twenty. The class comment and any test that counts the
keys move with it. The two resource keys follow the registry's own derivation rule for a key's suffix,
so the permission cannot be declared without its wording.

### The roles that hold it

Baseline rows are written under `scope_type = role` and `scope_key = role:<role_code>`, with
`value_type = bool`, as every other permission is.

| `role_code` | Rank | Holds `permission.settings.lugger_arrangement` |
|---|---|---|
| `developer` | 100 | yes |
| `it_department` | 90 | yes |
| `plant_manager` | 80 | yes |
| `production_lead` | 70 | yes |
| `setup_lead` | 60 | yes |
| `material_handler_lead` | 50 | yes |
| `material_handler` | 10 | no |
| `production` | 10 | no |
| `setup` | 10 | no |

`Material Handler Lead` is the lowest role that holds it, and every role above that rung holds it too
(FR-028). Every role below it is refused, and so is a person with no role.

### The fallback when the store is unreachable

The answer is `false`, the registry's own fallback for this key. An outage therefore grants nobody the
arrangement. The Settings entry is not drawn and the route is refused. The page shows its own
unavailable state with a manual retry when it is reached while the store is down, and it never shows
sample luggers in its place.

The notice poll reads the same store, so an outage means no notices rather than a message nobody can
confirm. `permission.requests.handle` is the only key that falls back to `true`, and this feature does
not change that.
