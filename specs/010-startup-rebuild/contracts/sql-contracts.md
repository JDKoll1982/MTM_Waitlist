# Contract: SQL artifacts and procedure signatures

Source: brief S8, S9, S11.1 to S11.6, S14. Requirements: FR-010, FR-011, FR-014, FR-021, FR-024, FR-025,
FR-029, FR-030.

Constitution Principle III governs every artifact here: canonical naming, one artifact per folder with paired
`create.sql` and `rollback.sql`, every data operation behind a stored procedure, and the hand-maintained
aggregates kept in sync. `InlineSqlAuditTests` fails the build if statement text appears in application code.

The two identifiers pinned verbatim by the spec are `user_active_sessions` and `ops_startup_logs`. Neither is
renamed, recased or pluralised anywhere.

---

## 1. New table: `user_active_sessions`

Folder: `Database/Tables/<next-free-ordinal>_user_active_sessions/`, with `create.sql` and `rollback.sql`.
Registered in `AllTables.sql`.

```sql
CREATE TABLE IF NOT EXISTS user_active_sessions (
    id             BIGINT       NOT NULL AUTO_INCREMENT,
    public_id      CHAR(36)     NOT NULL,
    user_id        BIGINT       NOT NULL,
    computer_id    BIGINT       NOT NULL,
    token_hash     CHAR(64)     NOT NULL,
    token_salt     VARBINARY(32) NOT NULL,
    issued_utc     DATETIME     NOT NULL,
    expires_utc    DATETIME     NOT NULL,
    revoked_utc    DATETIME     NULL,
    is_active      TINYINT(1)   NOT NULL DEFAULT 1,
    source_label   VARCHAR(32)  NOT NULL,
    created_utc    DATETIME     NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_user_active_sessions_public_id (public_id),
    UNIQUE KEY uq_user_active_sessions_user_computer (user_id, computer_id),
    KEY idx_user_active_sessions_is_active_expires_utc (is_active, expires_utc),
    KEY idx_user_active_sessions_computer_id (computer_id),
    CONSTRAINT fk_user_active_sessions_core_users_profiles_user_id
        FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_user_active_sessions_core_computers_registry_computer_id
        FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
```

`uq_user_active_sessions_user_computer` is what enforces one active row per person per machine (S8.1).

### Procedures

```sql
-- Write or replace the one active session for a person on this machine. Computes expires_utc from the
-- session-length setting read at sign-in time, using fn_server_utc_now() as "now".
-- IN  p_user_id, p_computer_id, p_token_hash, p_token_salt, p_session_length_minutes, p_source_label
-- OUT p_public_id, p_expires_utc
sp_auth_user_active_sessions_upsert

-- Validate. A row is valid only while is_active = 1 AND revoked_utc IS NULL AND
-- fn_server_utc_now() < expires_utc. The store's clock decides; the workstation's is never consulted.
-- IN  p_user_id, p_computer_id, p_token_hash
-- OUT one row: public_id, issued_utc, expires_utc, source_label, is_valid
sp_auth_user_active_sessions_get

-- Clear on sign-out. Sets revoked_utc and is_active = 0 before the application restarts.
-- IN  p_user_id, p_computer_id
-- OUT p_rows_affected
sp_auth_user_active_sessions_clear

-- Support read: live sessions, optionally filtered by person or machine, newest first.
-- IN  p_user_id (nullable), p_computer_id (nullable)
-- OUT one row per live session
sp_auth_user_active_sessions_list
```

No procedure clears sessions by user for a password reset, because a reset must not end an existing session
(S14).

### Validation script

`Database/Validation/<name>/validate.sql`, wired into the schema validator: the table exists, the two unique
keys exist, both foreign keys resolve, and no row stores a plaintext token (asserted by shape: `token_hash` is
64 characters and `token_salt` is present).

---

## 2. New table: `auth_remembered_sign_ins`

Folder: `Database/Tables/<next-free-ordinal>_auth_remembered_sign_ins/`, with `create.sql` and `rollback.sql`.
Registered in `AllTables.sql`. The brief does not name a table for the remembered sign-in; this is the plan's
decision D12, and the reasoning is recorded in `research.md`.

```sql
CREATE TABLE IF NOT EXISTS auth_remembered_sign_ins (
    id                 BIGINT       NOT NULL AUTO_INCREMENT,
    public_id          CHAR(36)     NOT NULL,
    user_id            BIGINT       NOT NULL,
    computer_id        BIGINT       NOT NULL,
    payload_ciphertext VARBINARY(512) NOT NULL,
    payload_iv         VARBINARY(16)  NOT NULL,
    key_fingerprint    CHAR(64)     NOT NULL,
    is_active          TINYINT(1)   NOT NULL DEFAULT 1,
    created_utc        DATETIME     NOT NULL,
    updated_utc        DATETIME     NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_remembered_sign_ins_public_id (public_id),
    UNIQUE KEY uq_auth_remembered_sign_ins_user_computer (user_id, computer_id),
    CONSTRAINT fk_auth_remembered_sign_ins_core_users_profiles_user_id
        FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_remembered_sign_ins_core_computers_registry_computer_id
        FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
```

`key_fingerprint` is what lets a rotated key be detected and fall back to the ordinary sign-in form rather than
failing (FR-014).

### Procedures

```sql
-- IN  p_user_id, p_computer_id
-- OUT one row: payload_ciphertext, payload_iv, key_fingerprint, updated_utc
sp_auth_remembered_sign_ins_get

-- IN  p_user_id, p_computer_id, p_payload_ciphertext, p_payload_iv, p_key_fingerprint
-- OUT p_public_id
sp_auth_remembered_sign_ins_upsert

-- IN  p_user_id, p_computer_id
-- OUT p_rows_affected
sp_auth_remembered_sign_ins_clear
```

### Validation script

Same shape as section 1, plus: `payload_iv` is present and `payload_ciphertext` is not equal to any plaintext
form (asserted by requiring both columns non-null, since a row without an IV cannot be decrypted and would
never have been written by the upsert).

---

## 3. Repurposed table: `ops_startup_logs`

Existing folder `Database/Tables/10_ops_startup_logs/`. `create.sql` is amended, not replaced, and
`rollback.sql` is extended for the new columns and indexes. Registered in `AllTables.sql`.

Added columns: `module VARCHAR(64) NULL`, `error_type VARCHAR(128) NULL`, `exception_detail MEDIUMTEXT NULL`.

Added indexes for the panel's filter set: `(level, created_utc)`, `(host_id, created_utc)`,
`(module, created_utc)`, `(actor_id, created_utc)`, `(error_type, created_utc)`.

Existing columns reused unchanged: `public_id`, `correlation_id`, `created_utc`, `level` (severity),
`event_action`, `outcome`, `actor_kind`, `actor_id` (person), `host_id` (machine), `mac_address`, `message`,
`payload_json`, `previous_hash`, `entry_hash`.

### Procedures

```sql
-- The single writer. Reads previous_hash, computes entry_hash over the entry plus that link, inserts, and
-- commits, inside one transaction, so concurrent writers cannot fork the chain.
-- IN  p_public_id, p_correlation_id, p_level, p_event_action, p_outcome, p_actor_kind, p_actor_id,
--     p_host_id, p_mac_address, p_module, p_error_type, p_message, p_exception_detail, p_payload_json
-- OUT p_id, p_entry_hash
sp_ops_startup_logs_insert

-- The panel's reader. Every query is bounded by a time window and a page size, so a large store cannot
-- freeze the screen.
-- IN  p_level, p_host_id, p_actor_id, p_module, p_error_type, p_from_utc, p_to_utc, p_page_size, p_offset
-- OUT one row per entry, newest first, including message, exception_detail and the chain link
sp_ops_startup_logs_filter

-- Retention, bounded by age and by size, removing the oldest first.
-- IN  p_max_age_days, p_max_rows
-- OUT p_rows_deleted
sp_ops_startup_logs_purge
```

### Validation script

Existing log-store validation extended: the three new columns exist, all five new indexes exist, and every row
has `entry_hash` set.

---

## 4. Deleted artifacts

Folder deleted, entry removed from the matching aggregate, and any document naming it updated. The list and the
reason for each are in `data-model.md` section E10.

| Deleted | Kind |
|---|---|
| `auth_sessions_tokens` | table, superseded by `user_active_sessions` |
| `core_buildings_catalog`, `core_buildings_history` | tables, no consumer |
| `sp_core_buildings_upsert` | procedure, no consumer |
| `sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`, `sp_auth_computer_registered_get`, `sp_auth_session_expiry_get`, `sp_auth_password_reset_required_get`, `sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`, `sp_core_computers_registry_update_by_mac` | procedures, startup-only |

`Database/Validation/startup_schema/` is reworked rather than deleted: it keeps the checks that cover shared
tables and drops the rest.

## 5. Artifacts that must not be deleted

`fn_server_utc_now` is **retained**, because section 1's validation rule and FR-010 depend on it. It is the
only retained member of the otherwise-removable startup-only set, so the removal phase must be told explicitly
or the audit would take it.

Shared artifacts that three foreign keys and several live consumers depend on: `core_users_profiles`,
`core_computers_registry`, `auth_roles_catalog`, `auth_roles_assignments`, `config_settings_values`,
`sp_auth_user_row_get`, `sp_auth_temporary_credential_attempt_record`, the six
`sp_core_computers_registry_*` procedures, `sp_auth_roles_list`, `sp_config_settings_values_get`,
`sp_config_settings_values_upsert`, `fn_server_utc_now`, `seed_dev_masked_baseline`,
`seed_username_upper_normalization`, `seed_role_admin_to_it_department`, `AllSeeds.sql`,
`update_table_descriptions.sql`, `AllTables.sql`, `AllSPs.sql`, `AllFunct.sql`.

## 6. Seeded changes

```sql
-- seed_permission_role_baselines: four new keys, one baseline row per role including IT Department.
permission.settings.machine_configuration     -> IT Department 1, Developer 1, every other role 0
permission.settings.ignored_locations_edit    -> IT Department 1, Developer 1, every other role 0
permission.settings.log_panel                 -> Developer 1, every other role 0
permission.settings.session_length            -> IT Department 1, Developer 1, every other role 0

-- seed_dev_masked_baseline: a developer on each of the two seeded machines.
Developer granted to JKoll and JohnK
```

The existing `permission.settings.ignored_locations` rows are left exactly as they are. That is deliberate: it
is what keeps FR-030 true (see `machine-configuration-contract.md` section 5).

A fresh store must grant `IT Department` and `Developer` the four keys, and a role without them must not be able
to use the feature. Both are asserted by tests in Phase 7.4.
