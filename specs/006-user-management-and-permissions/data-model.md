# Phase 1 Data Model: User Management and Permissions

Everything below is either an existing artifact this feature reshapes or a new one it introduces. Column names
follow `database-schema-rules.instructions.md`: lower-case snake case, `public_id` for the external identifier,
`is_` for booleans, `_utc` for UTC timestamps, and a constraint or index name that stays inside 64 characters.

## Role

The job-title catalogue. The code is the identity; the name is presentation and may change.

| Field | Type | Rule |
| --- | --- | --- |
| `id` | `BIGINT` | Surrogate key, unchanged. |
| `public_id` | `CHAR(36)` | Unchanged. |
| `role_code` | `VARCHAR(64)` | The identity. Lower-case snake case. Unique. |
| `role_name` | `VARCHAR(128)` | Display name only. Never compared for access (FR-054). |
| `role_rank` | `INT` | **New.** The rung. Not unique: the three worker roles share rung 10 on purpose. |
| `created_utc`, `updated_utc` | `DATETIME` | Unchanged. |

The shipped catalogue holds nine roles, replacing the eight it holds today:

| `role_code` | `role_name` | `role_rank` |
| --- | --- | --- |
| `developer` | Developer | 100 |
| `it_department` | IT Department | 90 |
| `plant_manager` | Plant Manager | 80 |
| `production_lead` | Production Lead | 70 |
| `setup_lead` | Setup Lead | 60 |
| `material_handler_lead` | Material Handler Lead | 50 |
| `material_handler` | Material Handler | 10 |
| `production` | Production | 10 |
| `setup` | Setup | 10 |

`admin` is removed. Its display name `Admin` goes with it. The rename seed writes the renamed row **with the rung
above, `role_rank` 90**, because the column is `NOT NULL DEFAULT 0` and an insert that left the rung out would rank
the renamed top role below the worker roles; its paired reversal removes that row and re-inserts `admin` carrying the
rung the removed row held. Because the reversal therefore leaves the retired role in the catalogue, the forward
rename is re-applied once the reversal has been proved, before any baseline keyed to `it_department` is seeded. The
retired vocabulary that must not survive anywhere in the
the application is `administrator`, `supervisor`, `manager`, `quality`, `quality inspector` and `Setup Tech`.

**Relationship**: one role is held by many assignments; a role's baseline is one `role`-scoped settings row per
permission.

## Person

One person's account, `core_users_profiles`, reshaped.

| Field | Type | Rule |
| --- | --- | --- |
| `id` | `BIGINT` | Unchanged. The durable identity the audit and the settings rows point at. |
| `public_id` | `CHAR(36)` | Unchanged. |
| `username_normalized` | `VARCHAR(128)` | **Now upper case.** Between 1 and 128 characters. Unique. Both the stored value and the comparison use the upper-case form, so a sign-in matches whatever case is typed (FR-002). |
| `first_name` | `VARCHAR(128)` | **New.** Between 1 and 128 characters. |
| `last_name` | `VARCHAR(128)` | **New.** Between 1 and 128 characters. |
| `display_name` | `VARCHAR(256)` | Derived from the two names. Must not exceed 256 characters. |
| `employee_identifier` | `VARCHAR(128)` | Exactly four digits. **Deliberately not unique**: two people may share one. |
| `password_hash` | `VARCHAR(128)` | Unchanged. A temporary credential is a marker here, never a stored PIN. |
| `password_salt` | `VARBINARY(32)` | Unchanged. |
| `require_password_change` | `TINYINT(1)` | Unchanged meaning: a real password must be set at the next sign-in. |
| `temporary_credential_failed_attempts` | `INT NOT NULL DEFAULT 0` | **New.** Wrong attempts against a temporary credential. Held with the account so it survives a restart (FR-037) and does not expire with time (FR-038). |
| `is_active` | `TINYINT(1)` | Unchanged. A new account is created active and the create page offers no control for it (FR-007). |
| `created_utc`, `updated_utc` | `DATETIME` | Unchanged. |

**State, and the transitions the feature must honour**

```text
                    create (always lands here)          deactivate
   (nothing) --------------------------------> ACTIVE --------------> INACTIVE
                                                 ^                      |
                                                 +----------------------+
                                                        reactivate

   credential state, independent of the account's active flag
   TEMPORARY (require_password_change = 1, or the legacy marker)  --sign-in--> REAL PASSWORD
        |                                                                          |
        |  5 failed attempts                                                       |
        +-------------------------> STOPPED (a fresh reset is the only way back)   |
        |                                                                          |
        +<---------------------------- a fresh reset ------------------------------+
```

- Deactivating does not end an open session, and neither does a password reset (FR-035, FR-102). Both facts are
  stated on the screen, because they are what a person assumes wrongly.
- A reset on a switched-off account does not switch it back on (FR-029).
- A fresh reset clears the wrong-attempt count and issues a new PIN; the earlier PIN stops working and the screen
  says so (FR-032, FR-039).
- A successful sign-in with a temporary credential clears the count (FR-039).
- The legacy value `0000` stays recognised as temporary, so an older account keeps working until its owner changes
  it, and the limit applies to it exactly as to a new PIN (FR-031, US7 scenario 10).

## Role assignment

`auth_roles_assignments`, unchanged in shape. A person holds exactly one role in practice (FR-009); the table
permitted several before this feature and still does, so every reader takes the highest `role_rank` among a
person's live roles (FR-017). The rename repoints these rows and nothing else, because the catalogue row is the
only other place a role is referenced.

## Permission

One gated action, declared once in `PermissionRegistry`. The declaration is code, and it carries no per-role
baseline: the baselines are seeded rows, and the two are tied by a check that runs in both directions — every
declared key has a baseline row for every role the catalogue holds, and every baseline row names a declared key
(FR-046, FR-047, FR-048). That split is the deliberate resolution of the contradiction inside the seed's
section 3.8, recorded in `research.md`.

| Field | Meaning |
| --- | --- |
| `Key` | One of the fourteen pinned keys. Unique, and spelled exactly as the spec pins it. |
| `Label` | The resource key for the plain-language label shown on the page. |
| `Gates` | The resource key for the sentence saying what it gates. |
| `Area` | The group the page shows it under: requests, cache, settings, setup, administration. |
| `Fallback` | The shipped answer used when neither a person's own row nor a role's baseline exists, and when the store cannot be reached. |
| `GateSites` | The places that read it. This is what the two checks compare against, in both directions. |

The fourteen keys are pinned verbatim in the specification's Verbatim Constraints section and copied exactly once
in `contracts/permission-contract.md`, which is the canonical list the registry test reads. They are not repeated
here, so a change to the set is a change in two places rather than three.

## Role baseline

One row per permission per role in `config_settings_values`, with `scope_type` `role`, `scope_key`
`role:<role_code>`, `value_type` `bool`, and `setting_value_bool` carrying the answer. Shipped as data by
`seed_permission_role_baselines`, so what a role may do is changed without a code change (FR-048). A role added to
the catalogue after this ships answers from its own baseline or from the fallback, never from a failure (FR-060).

## Person permission value

A `user`-scoped row in the same table, `scope_key` `user:<id>`, written only when the person's answer differs from
their role's baseline. Its presence is what the page marks as granted or denied for that person rather than
inherited. A person's own row outranks their baseline and every wider scope (FR-053). The choices survive a change
of role, because they were made for the person.

## Resolution

For a permission and a person:

```text
the person's own row  ->  their role's baseline  ->  the shipped fallback
```

An unreachable store yields the shipped fallback (FR-050), so a settings failure never turns every control in the
application into a refusal. The same lookup answers both whether a control is shown and whether the action it
guards is permitted (FR-056, FR-055), and the answer is cached for the session with explicit invalidation when a
permission is saved or a sign-in resolves a different person.

**The precedence order is corrected before any person's row is written (FR-053).** `fn_config_settings_scope_rank`
becomes `computer` 1, `all_users` 2, `role` 3, `admin` 4, `developer` 5, `user` 6, and the effective read keeps
taking the highest rank that applies. Today the order puts `user` third, below `admin` and `developer`, so a
person's own value loses to an inherited one.

## Permission change record

`config_settings_history`, which already exists and already carries `previous_setting_value_*`,
`changed_setting_value_*`, `changed_by_user_id` and `changed_utc`. One row per changed permission, written in the
same transaction as the value it records (FR-072). A reversal reads this table rather than any new store. A
reversal that finds the value has moved shows what it is now and asks before restoring (FR-070), and a reversal of
a save that changed several rows reverses all of them (FR-071).

## Account change audit record

`auth_user_management_audit`, new, at `Database/Tables/32_auth_user_management_audit/`. Its own shape, because it
records neither an event on a request nor a settings cell.

| Field | Type | Rule |
| --- | --- | --- |
| `id` | `BIGINT` | Surrogate key. |
| `public_id` | `CHAR(36)` | External reference. |
| `change_group_id` | `CHAR(36)` | Shared by every row one save produced, so one act reads as one act (FR-110). |
| `target_user_id` | `BIGINT` | The person whose account changed. |
| `field_name` | `VARCHAR(64)` | The one field this row is about. |
| `previous_value` | `TEXT NULL` | Before. |
| `changed_value` | `TEXT NULL` | After. |
| `actor_user_id` | `BIGINT NULL` | The acting person, joined for durability. |
| `actor_display_name` | `VARCHAR(256) NOT NULL` | Snapshot, so a later rename cannot re-attribute what they did (FR-109). |
| `actor_employee_identifier` | `VARCHAR(128) NOT NULL` | Snapshot. |
| `actor_role_code` | `VARCHAR(64) NOT NULL` | Snapshot. A later role change must not rewrite history. |
| `occurred_utc` | `DATETIME` | The server clock. |

One row per changed field (FR-108), so one save that touches four fields writes four rows sharing one
`change_group_id`. A password reset writes one row that records the act with **both value columns null**, because
the PIN is never stored (FR-111). A change this feature refuses writes nothing at all (FR-026), so the table
records changes and never attempts. There is no screen that reads it: it is written by the application and read by
support.

## Temporary credential

Not an entity of its own, and never stored as the credential. A random four-digit PIN, shown once to the person who
performed the reset or the create. Only a salted hash of it reaches the store; the PIN itself is written to the
store in no readable form, and never to the logs or the audit row. The four-digit space is small enough that a
stored hash must be treated as recoverable, so nothing treats it as a place the PIN can be read from and the
five-attempt limit is what gives the credential any strength (FR-030, FR-036). It travels as far as the window that
reveals it, the view model that holds it while it is open, and the printed handover slip, and no further.

## Saved list preference

One `user`-scoped row in `config_settings_values` under `admin.users.list_filter`, `value_type` `text`, holding
the search text and the chosen role code as one small JSON object. Remembered for the person rather than the
workstation (decision 25), so two people sharing an application account share the filter. A filter naming a role
that no longer exists, or a filter that cannot be read, falls back to showing everyone and says so, rather than
showing an empty list (FR-091, FR-092).

## Session

`auth_sessions_tokens`, unchanged. It is named here only because three requirements depend on what this feature
does **not** do to it: deactivating does not revoke an open session (FR-102), a password reset does not end one
(FR-035), and neither is a change to the sign-in, session or token model.

## Validation rules, gathered from the requirements

| Rule | Requirement |
| --- | --- |
| A sign-in name, a first name and a last name are each 1 to 128 characters | FR-005 |
| A display name derived from the two names does not exceed 256 characters | FR-005 |
| An employee number is exactly four digits and is not unique | FR-004 |
| A sign-in name is stored and matched in upper case | FR-002, FR-003 |
| A duplicate sign-in name is answered as the duplicate-key `1062` result, naming the name, keeping what was typed, and never retried blindly | FR-008, and the pinned value `1062` |
| A person is created with exactly one role, and nothing in this feature gives them a second; the stored assignments still permit more than one, so readers take the highest-ranked live role | FR-009, FR-017 |
| A person cannot change an account whose role ranks strictly above their own: role, active state, sign-in name or password | FR-019 |
| Peers are editable, and a person's own account does not rank above them | FR-020, FR-021 |
| A person cannot assign a role above their own rank, and the picker offers only roles at or below it | FR-022 |
| A person cannot deactivate themselves | FR-023 |
| A person cannot change the sign-in name of the account they are signed in as | FR-024 |
| The rank, self-lockout and sign-in-name rules are enforced where the change is written | FR-025 |
| The PIN is exactly four digits | Verbatim Constraints |
| Five failed attempts stop a temporary credential being accepted | FR-036, and the pinned value five |
| The permission that opens the permissions page cannot be changed from that page | FR-059 |
| Nothing on the permissions page edits a role baseline | FR-063 |
| A permission cannot exist without appearing on the page, nor appear without a baseline for every role | FR-047 |
| Whatever the Settings cache-refresh control admits is also admitted by the service's cache-refresh permission | FR-057 |
