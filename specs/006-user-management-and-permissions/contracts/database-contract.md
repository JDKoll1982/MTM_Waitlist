# Contract: Schema and Stored Procedures

The database interface this feature exposes. Every artifact here ships `create.sql` and `rollback.sql` in the same
change, and the hand-maintained aggregates (`AllTables.sql`, `AllSPs.sql`, `AllFunct.sql`, `AllSeeds.sql`) plus
`Database/Bootstrap/update_table_descriptions.sql` move with them, per constitution III and
`database-schema-rules.instructions.md`.

**The target server is MySQL 5.7.** Every artifact header names 5.7 deliberately: that is the version this goes on
when it goes live. A local development server running a newer 9.x build is expected, and is **not** a reason to
change a header to match it. (Confirmed by the owner, 2026-09-21: the headers are right and the dev server differs.)

## Tables

| Artifact | Change |
| --- | --- |
| `Tables/01_core_users_profiles/` | Add `first_name VARCHAR(128) NOT NULL`, `last_name VARCHAR(128) NOT NULL`, `temporary_credential_failed_attempts INT NOT NULL DEFAULT 0`. |
| `Tables/03_auth_roles_catalog/` | Add `role_rank INT NOT NULL DEFAULT 0`. |
| `Tables/32_auth_user_management_audit/` | New. One row per changed field. |

The folder number 32 follows 31, which is the highest in use.

The four new columns — three on the person table and `role_rank` on the role catalogue — are also applied to a
store that already exists, by a guarded `information_schema` check and
a prepared `ALTER TABLE` in `Bootstrap/update_table_descriptions.sql`, following the block that already adds
`setup_work_centers_catalog.building`. `CREATE TABLE IF NOT EXISTS` alone would not add a column to a live store,
which is why the guard is required rather than optional.

## Functions

`Functions/fn_config_settings_scope_rank/` changes its answer, and only its answer:

```text
computer   1
all_users  2
role       3
admin      4
developer  5
user       6
```

The reader keeps taking the highest-ranked applicable row, so a person's own row now wins. `role` is new and the
other numbers move around it. Nothing observable changes today, because all three rows in the table are
`all_users`.

## Seeds

| Artifact | What it does | Its rollback |
| --- | --- | --- |
| `Seeds/seed_dev_masked_baseline/` | Adds `material_handler_lead` and gives the roles it already holds their rungs. It does **not** remove `admin`, insert `it_department` or repoint the masked `test.admin` account — those belong entirely to the row below, so two seeds never own the same rows and the rename's reversal stays exact for the stated reason. | Removes the rows it added. |
| `Seeds/seed_role_admin_to_it_department/` | Removes `admin`, inserts `it_department` **with `role_rank` 90**, repoints every assignment, in one transaction with an exit handler that rolls back and resignals. The rung is set explicitly because `role_rank` is `NOT NULL DEFAULT 0`: an insert that left it out would rank the renamed top role below the worker roles at 10, and the rank rule would then refuse it the accounts it exists to own. | Inserts `admin` again carrying the rung the row held when the migration removed it — the column's default, because the shipped seed deliberately leaves the retired role unranked — so the reversal restores the rung it replaced rather than inventing one; repoints every assignment currently on `it_department` back to it; removes `it_department`. Exact while no account has been given IT Department since the migration. |
| `Seeds/seed_permission_role_baselines/` | One `role`-scoped row per permission per role, `scope_key` `role:<role_code>`. | Removes the rows it wrote. |
| `Seeds/seed_username_upper_normalization/` | Upper-cases `core_users_profiles.username_normalized`, and reports rather than failing when two rows would collide on the unique key. | Restores the previous values it recorded. |

The forward seed is re-applied once its reversal has been proved, because the reversal puts the retired role back and
the permission baselines are keyed to `it_department`: the baselines are seeded against a catalogue that holds
`it_department` at rung 90 and does not hold `admin`.

A seeded role baseline is `scope_type` `role`, `scope_key` `role:<role_code>`, `value_type` `bool`, and
`setting_value_bool` carrying the answer. No per-person permission row is seeded, which is what keeps ship day
identical.

## The changed reads

Three existing reads gain columns. New columns are appended, and the ordinals in the calling code move in the same
change, because the callers read by index.

| Procedure | Contract |
| --- | --- |
| `sp_auth_credentials_check(p_username)` | `id`, `role_code`, `role_name`, `password_hash`, `password_salt`, `require_password_change`, `display_name`, `employee_identifier`, `temporary_credential_failed_attempts`. The caller normalises the user name to upper case before calling. |
| `sp_auth_user_row_get(p_username)` | `id`, `role_code`, `role_name`, `display_name`, `employee_identifier`. |
| `sp_auth_password_reset_required_get(p_username)` | `user_id`, `role_code`, `role_name`, `display_name`, `employee_identifier`, `password_reset_required`. The verdict keeps its two existing signals and is not widened. |

"An account holds a temporary value" means exactly what `sp_auth_password_reset_required_get` already computes:
`require_password_change = 1`, or a hash of `''` or the legacy marker `0000`.

## The new procedures

Each is a `BEGIN … END` body. A write declares

```text
DECLARE EXIT HANDLER FOR SQLEXCEPTION
BEGIN
  ROLLBACK;
  RESIGNAL;
END;
```

so the transaction is rolled back and the original error is preserved. **A duplicate sign-in name is deliberately
not translated**: MySQL's own duplicate-key error passes through the resignal, and the caller reads `1062` from the
provider and returns the typed "already taken" result. That keeps the pinned `1062` answer as one behaviour with
one source, and it is why no blind retry exists anywhere in the path.

### Refusals

A rule the store enforces raises a refusal with a stable token so the caller can report it in the reader's words
rather than by parsing prose:

```text
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_rank_denied'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_deactivate_denied'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_rename_denied'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_gate_fixed'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_value_moved'
SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_role_unknown'
```

The store, not the screen, decides: it reads the actor's own rung from `auth_roles_assignments` joined to
`auth_roles_catalog` and never takes a rank, a role code or an actor name from its caller. A refused write leaves
no audit row, because nothing changed.

### `sp_auth_roles_list()`

Returns `role_id`, `role_code`, `role_name`, `role_rank`, ordered by `role_rank` descending then `role_code`, and
returning only live roles, so an unranked or retired catalogue row is excluded. This
is the one read behind the picker, the ladder and the rank comparison.

### `sp_auth_temporary_credential_attempt_record(p_user_id, p_was_successful)`

One statement per branch: a successful attempt sets `temporary_credential_failed_attempts = 0`; a failed one adds
one. Nothing else clears it and nothing expires it. Non-query, so the affected-row count reaches the caller.

### `sp_user_management_list(p_search, p_role_code)`

Returns `user_id`, `public_id`, `username_normalized`, `display_name`, `employee_identifier`, `role_code`,
`role_name`, `role_rank`, `is_active`. A null or empty `p_search` matches everyone; a null or empty `p_role_code`
filters nothing. The search matches the sign-in name, the display name, the employee number and the role name
(FR-086). Everyone is returned by default, switched-off people included (FR-088).

### `sp_user_management_get(p_user_id)`

Returns one person: `user_id`, `public_id`, `username_normalized`, `first_name`, `last_name`, `display_name`,
`employee_identifier`, `role_code`, `role_name`, `role_rank`, `is_active`,
`temporary_credential_failed_attempts`. Reading is never refused: an account the reader cannot change is still
readable (FR-027).

### `sp_user_management_create(p_username, p_first_name, p_last_name, p_employee_identifier, p_role_code, p_actor_user_id, p_password_hash, p_password_salt, p_change_group_id)`

In one transaction: refuses a role above the actor's own rung; inserts the profile with the upper-cased sign-in
name, the derived display name, `is_active = 1` and the temporary credential; inserts the role assignment; writes
one audit row per written field sharing `p_change_group_id`. A person with no role cannot come into existence,
because the profile and the assignment are one act.

### `sp_user_management_update(p_user_id, p_username, p_first_name, p_last_name, p_employee_identifier, p_role_code, p_is_active, p_actor_user_id, p_change_group_id)`

In one transaction: refuses a target whose rung is strictly above the actor's; refuses a role above the actor's own
rung; refuses deactivating the actor's own account; refuses changing the sign-in name of the account the actor is
signed in as; writes one audit row per field that actually changed, sharing `p_change_group_id`, and writes no
audit row at all when nothing changed.

### `sp_user_management_reset_password(p_user_id, p_actor_user_id, p_password_hash, p_password_salt, p_change_group_id)`

In one transaction: refuses a target that outranks the actor; stores the hash and salt of the freshly generated
PIN; sets `require_password_change = 1`; clears the wrong-attempt count; leaves `is_active` exactly as it was; and
writes one audit row recording the reset with **both value columns null**. The PIN itself is never sent to the store
and never appears in any readable row: only its salted hash is stored, which is what FR-030 means when it says the
credential cannot be read back — plaintext — and the four-digit space is why the attempt limit exists at all. The
procedure ends no session for that person (FR-035).

### `sp_config_permissions_user_get(p_user_id)`

Returns the permission rows that are stored for this person and for their role: `setting_key`, `scope_type`,
`scope_key`, `setting_value_bool`, `updated_utc`. It returns what is stored; the caller composes what is declared,
filling a key with no stored row from the shipped fallback. The role is resolved inside the procedure from the
person's assignment, so the caller cannot ask about a role that is not theirs.

### `sp_config_permissions_user_set(p_user_id, p_actor_user_id, p_changes_json)`

Takes the whole change set in one call, as a JSON array of `{ "key": …, "from": …, "to": … }`, and in one
transaction:

- refuses a target that outranks the actor, and refuses a reversal by somebody who could not have made the change;
- refuses any entry naming `permission.admin.permissions`, because that row is fixed;
- refuses an entry naming a key outside the `permission.` namespace;
- for each entry, refuses when the stored value is not the `from` the caller last saw, so a value that has moved is
  never overwritten silently, and reports which key moved;
- writes one row per changed key and one `config_settings_history` row per changed key carrying the actor, the key,
  the scope it was written for, both values and the time;
- writes nothing at all when the set is empty.

A save of several permissions and a reversal of that save are therefore the same call with the same atomicity, and
one save writes exactly its own number of history rows.

### `sp_config_permissions_feature_holders_get(p_setting_key)`

Returns every person whose stored value for this key differs from their role's baseline: `user_id`, `display_name`,
`employee_identifier`, `role_code`, `is_active`, `setting_value_bool`. Read-only, so the who-holds-this view has
nothing to write. The roles whose baseline gives the key are read from the seeded baselines rather than returned
here, with the declaration's two-direction check tying the two together (FR-046, FR-047, FR-048).

## The saved list preference

No new artifact. `sp_config_settings_values_get` reads it and `sp_config_settings_upsert` writes it, under the key
`admin.users.list_filter` with `scope_type` `user`, `scope_key` `user:<id>` and `value_type` `text`. No history row
is written for a preference, which is what the existing upsert already does.

## Not changed, and deliberately

`sp_config_settings_upsert` keeps its current shape, including the `ELSE 'developer'` branch in its `scope_key`
expression. Nothing in this feature writes a `role` scope through it: baselines are seeded SQL and a person's rows
go through `sp_config_permissions_user_set`. The procedure's header gains a comment saying so, so the branch is
found by reading rather than by a surprise.
