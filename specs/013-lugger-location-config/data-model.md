# Phase 1 Data Model: Scrap Lugger Location

This feature adds three tables and changes no existing table.

The arrangement the `Lugger Location` cell reads is held in three tables. One holds the luggers, one holds the
scrap types each lugger takes, and one holds the record of every edit to the other two. All three are InnoDB on
MySQL 5.7, with `utf8mb4` and `utf8mb4_unicode_ci`, matching the tables already in `mtm_waitlist`. Each table
ships `create.sql` and `rollback.sql` in the same change, and the aggregate lists move with it.

Nothing about a lugger is written to a request. The request rows this feature reads stay exactly as they are.

## 1. `waitlist_lugger_locations`

Folder `Database/Tables/36_waitlist_lugger_locations/`. One row is one named lugger.

| Column | Type | Null | Meaning |
|---|---|---|---|
| `id` | `BIGINT` | no | Auto-increment primary key. |
| `public_id` | `CHAR(36)` | no | The row's public identifier. |
| `lugger_name` | `VARCHAR(64)` | no | The name the lead maintains and the handler reads on the card. Holds one of the seven seeded names until a lead renames it. |
| `status` | `VARCHAR(16)` | no | Exactly one of `Available`, `Backup`, `Full`, `Do Not Use`. Defaults to `Backup`, because a newly added lugger holds no dump point until a scrap type's order makes it one. |
| `status_reason` | `VARCHAR(500)` | yes | The reason recorded when the status is `Do Not Use`, readable by a handler asking about the lugger (FR-019). Null for the other three statuses. |
| `display_position` | `INT` | no | The lugger's place in the display order (FR-001). |
| `created_utc` | `DATETIME` | no | When the row was created. |
| `updated_utc` | `DATETIME` | no | When the row last changed. |

Keys and indexes:

- `PRIMARY KEY (id)`
- `UNIQUE KEY uq_waitlist_lugger_locations_public_id (public_id)`
- `UNIQUE KEY uq_waitlist_lugger_locations_lugger_name (lugger_name)`, which is FR-004's uniqueness rule and is
  compared without regard to case because of the table collation.
- `KEY idx_waitlist_lugger_locations_display_position (display_position)`
- `KEY idx_waitlist_lugger_locations_status (status)`

Rules the columns follow:

- There is no "still in use" column. The spec's Key Entities sketch mentions one, and D3 replaces it: removal
  deletes the row, because FR-003 requires a removed name to be re-creatable and MySQL 5.7 has no partial unique
  index to let a retired row keep the name out of the way.
- There is no reserved-name column and no fixed-row flag. Every one of the seven rows may be renamed, removed and
  re-created, which is FR-003 in full.
- The seven seeded names are `Door 6 Lugger`, `Door 7 Lugger`, `Door 8 Lugger`, `Door 9 Lugger`,
  `Extra Lugger (Outside)`, `Skeleton Lugger`, `Scrap Trailer` (FR-002). They are ordinary rows from the moment
  the seed writes them.
- `status` is the lugger's own operating state, not a per scrap type value. A lugger that takes two scrap types
  is blocked for both at once (FR-012, FR-021, D2).
- `status_reason` exists on the lugger rather than only in the change table because FR-019 makes it something a
  handler reads about the lugger as it stands now.
- `display_position` is distinct and contiguous by procedure convention, not by a unique key. A reorder rewrites
  every position of one scrap type or of the whole set in a single transaction, and a unique key would reject the
  intermediate state.

## 2. `waitlist_lugger_scrap_assignments`

Folder `Database/Tables/37_waitlist_lugger_scrap_assignments/`. One row is one statement that a scrap type dumps
through a given lugger.

| Column | Type | Null | Meaning |
|---|---|---|---|
| `id` | `BIGINT` | no | Auto-increment primary key. |
| `public_id` | `CHAR(36)` | no | The row's public identifier. |
| `lugger_id` | `BIGINT` | no | The lugger this scrap type dumps through. |
| `scrap_type` | `VARCHAR(128)` | no | The scrap type as the lead spelled it, or as the seed wrote it. |
| `scrap_type_key` | `VARCHAR(128)` | no | `LOWER(TRIM(scrap_type))`. The value the card's lookup is matched on (FR-009, D5). |
| `is_dump_point` | `TINYINT(1)` | no | 1 when this lugger is the one a handler is sent to for this scrap type (FR-013). Defaults to 0. |
| `sort_position` | `INT` | no | The order the system tries the luggers for this scrap type (FR-006). |
| `created_utc` | `DATETIME` | no | When the assignment was made. |
| `updated_utc` | `DATETIME` | no | When the assignment or its position last changed. |

Keys and indexes:

- `PRIMARY KEY (id)`
- `UNIQUE KEY uq_waitlist_lugger_scrap_assignments_public_id (public_id)`
- `UNIQUE KEY uq_waitlist_lugger_scrap_assignments_type_lugger (scrap_type_key, lugger_id)`
- `CONSTRAINT fk_waitlist_lugger_assignments_lugger_locations_lugger_id` on `lugger_id`, referencing
  `waitlist_lugger_locations (id)`, with `ON DELETE CASCADE`. The from-table segment is shortened to
  `waitlist_lugger_assignments` to stay inside the 64-character limit, following the shortening the repository
  already uses on other long foreign keys.
- `KEY idx_waitlist_lugger_scrap_assignments_scrap_type_key (scrap_type_key)`
- `KEY idx_waitlist_lugger_scrap_assignments_lugger_id (lugger_id)`

Rules the columns follow:

- The dump point role is stored here and not on the lugger. A lugger that takes two scrap types can hold the role
  for one and be a backup for the other, and one status column cannot say that (D2).
- The role cannot be derived from the order. FR-016 requires an emptied lugger to come back as a backup, and
  "the first usable lugger in order" would hand the role straight back to it.
- `sort_position` is distinct within a scrap type by procedure convention, not by a unique key, for the same
  intermediate-state reason as `display_position`.
- The seven seeded assignments are `3003 Aluminum` to `Door 6 Lugger`, `Galvanized Steel` to `Door 7 Lugger`,
  `5052 Aluminum` to `Door 8 Lugger`, `Steel` to `Door 9 Lugger`, `3003 Aluminum` to `Extra Lugger (Outside)` as
  the backup to `Door 6 Lugger`, `Skeleton` to `Skeleton Lugger`, and `Gaylord` to `Scrap Trailer` (FR-008). Each
  primary is seeded with `is_dump_point = 1` at `sort_position` 1, and the `Extra Lugger (Outside)` row is seeded
  at 0 and 2.
- The seed writes the scrap type spelling the spec pins, so `5052 Aluminum` stores the key `5052 aluminum`, which
  is how the setup workflow's own list spells it (D5, FR-009).
- `scrap_type` is free text. There is no foreign key to a scrap type catalogue because FR-011 lets a scrap type
  the shipped list does not name be recorded.
- The composite unique key allows a scrap type to name several luggers and a lugger to take several scrap types,
  and refuses the same lugger twice for one scrap type (FR-006, FR-007).
- `scrap_type_key` is written in the same statement as `scrap_type`, by the procedure that owns the row. A write
  outside the procedures is the only way the two could disagree.

## 3. `waitlist_lugger_arrangement_changes`

Folder `Database/Tables/38_waitlist_lugger_arrangement_changes/`. One row is one recorded change to the
arrangement. It is both the audit trail FR-032 asks for and the source of the notice FR-033 delivers (D6).

| Column | Type | Null | Meaning |
|---|---|---|---|
| `id` | `BIGINT` | no | Auto-increment primary key. The notice poll reads changes above a watermark held on this column. |
| `public_id` | `CHAR(36)` | no | The row's public identifier. |
| `change_kind` | `VARCHAR(32)` | no | What kind of edit was made. One of `lugger_added`, `lugger_renamed`, `lugger_removed`, `status_changed`, `dump_point_promoted`, `assignment_added`, `assignment_removed`, `assignment_reordered`, `lugger_reordered`. |
| `lugger_id` | `BIGINT` | yes | Cross-reference to the lugger as it stands. Null once the lugger has been removed. |
| `lugger_name` | `VARCHAR(64)` | no | The name the lugger holds after the change, or the name it held when it was removed. Text on purpose, so the audit outlives the row (D3). |
| `scrap_type` | `VARCHAR(128)` | yes | The affected scrap type as it was spelled at the time. Null when the change touched no assignment. |
| `scrap_type_key` | `VARCHAR(128)` | yes | The normalised key, so a row is found by the same match the card uses. |
| `previous_value` | `VARCHAR(255)` | yes | The value before the change, read according to `change_kind`. |
| `new_value` | `VARCHAR(255)` | yes | The value after the change, read according to `change_kind`. |
| `change_reason` | `VARCHAR(500)` | yes | The reason recorded with a `Do Not Use` status, kept after the lugger is unblocked and the lugger row clears it. |
| `changed_by_employee_number` | `VARCHAR(32)` | no | Who made the change (FR-032). |
| `changed_by_employee_name` | `VARCHAR(128)` | no | The same person's name, so the notice and the audit read without a join. |
| `changed_utc` | `DATETIME` | no | When the change was made (FR-032). |

Keys and indexes:

- `PRIMARY KEY (id)`
- `UNIQUE KEY uq_waitlist_lugger_arrangement_changes_public_id (public_id)`
- `KEY idx_waitlist_lugger_arrangement_changes_changed_utc (changed_utc)`
- `KEY idx_waitlist_lugger_arrangement_changes_lugger_id (lugger_id)`

What `previous_value` and `new_value` mean for each kind:

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

Rules the columns follow:

- There is no foreign key on `lugger_id`. FR-005 and D3 delete the lugger and its assignments, and a cascading
  foreign key here would delete the audit along with them. A `SET NULL` foreign key would also rewrite history,
  so the durable identifier is `lugger_name` and `lugger_id` is a convenience.
- `lugger_name` and `scrap_type` are copies taken at the moment of the edit. A later rename does not reach back
  into an earlier row.
- A row is appended by the procedure that makes the change and is never written by the application (D6).
- `changed_by_employee_number` comes from the signed-in person whose permission was checked before the call. The
  arrangement permission is held by `Material Handler Lead` and above and withheld from everybody else (FR-028).
- A change that touches no scrap type, such as adding a lugger or renaming one that takes none, writes one row
  with both `scrap_type` columns null.

## 4. Relationships

```text
waitlist_lugger_locations  1 ----< many  waitlist_lugger_scrap_assignments
        (lugger_id, ON DELETE CASCADE)

waitlist_lugger_arrangement_changes    no foreign key
        lugger_id     a cross-reference, null after a removal
        lugger_name   the durable name
        scrap_type    the affected scrap type, null when none
        scrap_type_key  the normalised key
```

Three things are deliberately not modelled:

- **No lugger column on a request.** FR-023 reads the value when the card is drawn, so a request stores nothing
  about the lugger and there is no relationship to draw. This is also why a removal cannot rewrite an open card's
  meaning: the next load simply reads the new arrangement.
- **No dismissal table and no per-person read cursor.** D7 keeps the notice watermark and the dismissals in the
  client session, so FR-036 and FR-037 are answered by where the session starts rather than by stored rows.
- **No gap table.** A scrap type with no assignment is a configuration gap FR-026 says to record, and it is
  recorded as a log line through `AppLog.Warn` when the list is built, not as a fifth entity. A row there would
  become a value a reader could mistake for an answer.

## 5. Validation rules

MySQL 5.7 parses and ignores `CHECK` constraints, so no rule below is enforced by the storage engine unless the
table says so. The procedures enforce the rest, and each refuses the write rather than repairing it.

| Rule | Enforced by | Traces |
|---|---|---|
| A lugger name is required and unique across the set, compared without regard to case. | `uq_waitlist_lugger_locations_lugger_name` on the case-insensitive collation; the procedure reports the clash in plain words. | FR-004 |
| No name is reserved. Every row may be renamed, removed and re-created, including the seven seeded ones. | No column and no flag can mark a row fixed; removal deletes the row. | FR-003, D3 |
| A lugger carries exactly one status, one of `Available`, `Backup`, `Full`, `Do Not Use`. | `sp_waitlist_lugger_locations_insert` and `sp_waitlist_lugger_locations_status_set` reject any other value. | FR-012 |
| `status_reason` is required and must not be blank after trimming when the status becomes `Do Not Use`. | `sp_waitlist_lugger_locations_status_set` rejects the write. | FR-019 |
| `status_reason` is null for the other three statuses, and the change row keeps the reason it had. | The same procedure clears the column on the way out of `Do Not Use`, and writes `change_reason` first. | FR-019 |
| A lugger's `display_position` is distinct and its order is what the list shows. | Both the insert and rename procedures place the row; the arrangement list orders on the column. | FR-001 |
| Removing a lugger removes its assignments and leaves none pointing at it. | `ON DELETE CASCADE` on the assignments foreign key. | FR-005 |
| A scrap type may name more than one lugger, a lugger may take more than one scrap type, and one lugger may not appear twice for one scrap type. | `uq_waitlist_lugger_scrap_assignments_type_lugger`. | FR-006, FR-007 |
| A job's scrap type matches its assignment without regard to letter case or surrounding spaces. | `scrap_type_key` is written as `LOWER(TRIM(scrap_type))`, and the card normalises the job's value the same way before matching. | FR-009, D5 |
| The inner spelling is not normalised, so `5052 aluminum` and `5052  aluminum` stay two different scrap types. | The key trims and lowercases only, and never collapses inner whitespace. | FR-009, D5 |
| `Scrap Type Required` and `No Scrap` are not assignable and never produce a lugger on a card. | The assignment procedures reject both after normalising, and the seed writes neither. | FR-010 |
| A scrap type the shipped list does not name may be recorded. | No catalogue foreign key, no membership check, and free text accepted. | FR-011 |
| A scrap type with at least one lugger that can receive material has exactly one row with `is_dump_point = 1`. | The procedures hold that scrap type's assignment rows with `SELECT ... FOR UPDATE` for the whole transaction, then set exactly one flag. | FR-013, FR-020 |
| A scrap type whose luggers are all blocked has no row with `is_dump_point = 1`, and the card falls back to the first row in order with its status beside the name. | The status procedure clears the flag when no usable lugger remains; the reader names the first row in `sort_position` order. | FR-013, FR-024 |
| A blocked lugger never holds the flag and never sorts above a lugger that does. | The status procedure re-derives the role and rewrites the positions of the affected scrap types in the same transaction. | FR-017, FR-020 |
| An emptied lugger does not take the role back from a lugger that holds it. | The status procedure leaves the flag where it is when a usable lugger already holds it. | FR-016 |
| Promoting a lugger returns the previous dump point to `Backup`. | `sp_waitlist_lugger_locations_promote` writes both rows in one transaction. | FR-018 |
| A status change applies to every scrap type the lugger takes, and every one of them is left with one dump point and no blocked lugger above it. | The status procedure loops the affected scrap types inside one transaction. | FR-021, FR-020 |
| Every change records who made it and when, and no change goes unrecorded. | Each mutating procedure inserts its own change rows; no separate audit procedure exists and the application never writes the table. | FR-032, D6 |
| The change table is append only. Nothing updates or deletes a row. | The procedures only insert, and no procedure takes a change row as a parameter. | FR-032, D6 |
| No lugger identifier, name, status or role is stored on a request. | There is no such column, and the feature adds none. | FR-023 |

## 6. State transitions

### Lugger status

```text
Available  usable, and holds the dump point for at least one scrap type it takes
Backup     usable, and holds no dump point for any scrap type it takes
Full       filled, cannot receive material
Do Not Use blocked with a required reason, cannot receive material
```

The lead chooses `Full`, `Do Not Use` or `Available` from `Full` or `Do Not Use`, and promotes a lugger. The
procedures choose between `Available` and `Backup`, because that difference is the role and not a preference.

| From | Trigger | To | What the procedure does besides the status |
|---|---|---|---|
| `Available` | lead sets `Full` | `Full` | Promotes the next usable lugger in each affected scrap type's order, and sorts the blocked lugger after every usable one (FR-015, FR-017). |
| `Available` | lead sets `Do Not Use` with a reason | `Do Not Use` | The same promotion, and the reason is stored on the lugger (FR-015, FR-019). |
| `Backup` | lead sets `Full` | `Full` | Re-derives the role of each affected scrap type, which changes nothing if it held none (FR-021). |
| `Full` | lead empties it | `Backup` | Leaves the current dump point holding the role (FR-016). |
| `Do Not Use` | lead sets `Available` | `Backup` | Clears the reason and keeps it on the change row, and leaves the current dump point holding the role (FR-016, FR-019). |
| `Backup` | lead promotes it | `Available` | Returns the previous dump point to `Backup` (FR-018). |
| `Available` | lead promotes a different lugger | `Backup` | The chosen lugger becomes the dump point (FR-018). |

One case needs saying plainly. Emptying a lugger makes it usable, and FR-016 keeps it out of a role that another
lugger already holds. If the scrap type it takes has no usable lugger holding the role and this lugger is the only
candidate, FR-013 still requires a dump point, so the role goes to the only lugger there is and it reads
`Available` again. FR-016 is about not displacing a holder, not about leaving a scrap type without one.

### Dump point role

Exactly one assignment row per scrap type carries the flag, and only while a lugger that can receive holds it.

```text
assignment added      the first usable lugger in the new order takes the role (D8, FR-006)
assignment removed    the first usable lugger of the remaining order takes it; if none can receive, no row holds it
order rewritten       the first usable lugger in the new order takes it (D8)
promotion             the chosen lugger takes it and the previous holder becomes Backup (FR-018)
status blocks the holder   the next usable lugger in order takes it; if none can receive, no row holds it
status unblocks a lugger   the current holder keeps the role (FR-016)
```

The ordering rule keeps the order and the role saying the same thing:

- Position 1 within a scrap type is the lugger that holds the role, whenever a lugger that can receive holds it.
- Every blocked lugger sorts after every lugger that can receive, for that scrap type (FR-017).
- Luggers that can receive keep their relative order among themselves.
- A reorder rewrites every position of one scrap type in one transaction.

A lugger whose only assignment has no role reads `Backup`, and so does a lugger that is assigned nowhere yet. A
lugger that holds the role for one scrap type and is a backup for another reads `Available`, because the single
status column records that it holds the role somewhere. The card never reads the status to choose a lugger. It
reads the flag for the role, and the status only when every lugger is blocked (FR-024).

### Notice lifetime

```text
list opened      the session watermark is set to the highest change id (D7)
change recorded  the next minute tick reads the rows above the watermark and shows each once
dismissed        the bar closes and the id is held in the session only
```

No row records a dismissal. D7 places the watermark at the moment the list opens, which is what FR-037 asks for:
a change made while nobody was using the application is never replayed. It also means a later session starts
above every older change, so a per-person dismissal table would answer a question already answered.

## 7. Who writes what

| Procedure | Rows written |
|---|---|
| `sp_waitlist_lugger_locations_insert` | One `waitlist_lugger_locations` row, then one `lugger_added` change row with both `scrap_type` columns null. |
| `sp_waitlist_lugger_locations_rename` | The lugger row, then one `lugger_renamed` change row per affected scrap type, carrying the old name in `previous_value`. One row with null `scrap_type` when the lugger takes none. |
| `sp_waitlist_lugger_locations_delete` | The lugger row, with its assignments removed by cascade, then one `lugger_removed` change row per affected scrap type, taken before the delete so the names survive. |
| `sp_waitlist_lugger_locations_status_set` | The lugger row, any other lugger row whose `Available` or `Backup` status moves with the role, then one `status_changed` change row per affected scrap type and one `dump_point_promoted` row for each scrap type whose role moved. |
| `sp_waitlist_lugger_locations_promote` | Two lugger rows, the promoted one to `Available` and the previous holder to `Backup`, then one `dump_point_promoted` change row. |
| `sp_waitlist_lugger_scrap_assignments_set` | One assignment row inserted or updated with its `sort_position`, the lugger rows whose status the new role changes, then `assignment_added` for a new row or `assignment_reordered` when only the position changed, plus `dump_point_promoted` when the role moved. |
| `sp_waitlist_lugger_scrap_assignments_delete` | The assignment row, the lugger rows whose status the lost role changes, then `assignment_removed` and `dump_point_promoted` when the role moved. |
| `sp_waitlist_lugger_locations_position_set` | Every lugger row whose place moved, renumbered contiguously from 1 in one transaction, then one `lugger_reordered` change row. |
| `sp_waitlist_lugger_locations_list` | Nothing. Reads the luggers in `display_position` order. |
| `sp_waitlist_lugger_scrap_assignments_list` | Nothing. Reads the assignments in `sort_position` order. |
| `sp_waitlist_lugger_arrangement_list` | Nothing. Reads the arrangement in one result set for the summary and the editor. |
| `sp_waitlist_lugger_arrangement_changes_list` | Nothing. Reads the change rows above the caller's watermark. |

A status change therefore writes two change rows for one scrap type when the role moves, one for the status and
one for the promotion. That is the smallest set that lets FR-035 name both what changed and what the handler
should walk to, and each row is still one recorded change against one affected scrap type.
