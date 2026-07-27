# Database Naming Conventions Questionnaire

Purpose: Capture your explicit naming standards before finalizing the database ruleset and baseline schema.

Please answer each item.

## 1) Table Naming
1. Should table names be plural (for example, users, sessions) or singular (for example, user, session)?
2. Should table names always be snake_case lowercase?
3. Do you want module prefixes for some tables (for example, startup_logs, waitlist_entries), or no prefixes?

## 2) Column Naming
1. Confirm snake_case lowercase for all columns.
2. Primary key column format:
- Option A: <table_singular>_id (for example, user_id)
- Option B: id (same in every table)
3. Foreign key column format:
- Option A: <referenced_table_singular>_id
- Option B: <table>_id (custom per relationship)
4. Boolean column prefix:
- Option A: is_/has_
- Option B: no prefix
5. UTC datetime suffix:
- Option A: _utc
- Option B: no suffix

## 3) Key, Index, and Constraint Names
1. Primary key constraint naming:
- Option A: pk_<table>
- Option B: let MySQL auto-name
2. Foreign key naming:
- Option A: fk_<from_table>_<to_table>_<column>
- Option B: shorter format
3. Unique constraint naming:
- Option A: uq_<table>_<column_or_purpose>
- Option B: custom freeform
4. Non-unique index naming:
- Option A: ix_<table>_<column_or_purpose>
- Option B: idx_<table>_<column_or_purpose>
5. Composite index order style:
- Option A: left-to-right by query filter/selectivity
- Option B: strict business field order

## 4) Public ID and Identity Fields
1. Confirm public UUID column name:
- Option A: public_id
- Option B: <table>_public_id
2. Confirm normalized identity field naming:
- username_normalized, hostname_normalized, mac_address_normalized
- or provide alternatives.

## 5) Audit and History Naming
1. History table suffix:
- Option A: _history
- Option B: _audit
2. Change tracking columns naming:
- changed_utc, changed_by_user_id
- or provide preferred names.
3. Hash chain columns for startup logs:
- previous_hash, entry_hash
- or provide preferred names.

## 6) Value Columns and Typed Settings
1. For key-value settings, confirm:
- setting_key, setting_value, value_type
2. Do you want data-type-specific columns (for example, setting_value_int, setting_value_bool) instead of generic TEXT?

## 7) Abbreviations and Reserved Words
1. Provide an allowed abbreviation list (for example, id, utc, mac, ip, rbac).
2. Confirm whether these abbreviations are allowed in object names:
- cfg, usr, ws, sess, rpt
3. Any banned words or terms to avoid in table/column names?

## 8) Database Artifact File Naming
1. Confirm bootstrap filename:
- `Database/Bootstrap/create_database.sql`
2. Confirm artifact file naming:
- `create.sql` for forward execution inside each artifact folder
- `rollback.sql` for rollback execution inside each artifact folder
- `validate.sql` for validation scripts inside each validation folder

## 9) Case and Cross-Platform Safety
1. Confirm lowercase-only object names to avoid Linux/Windows case sensitivity drift.
2. Confirm no quoted identifiers unless unavoidable.

## 10) Final Decision Lock
1. Should naming conventions be hard-fail rules in PR review for any Database/*.sql change?
2. Should exceptions require explicit written approval in the migration PR notes?

---


## Completed Answers (2026-07-26)

- 1.1: plural
- 1.2: yes
- 1.3: yes, structured module prefixes using pattern `{category}_{table}_{action_or_purpose}`
- 2.1: yes
- 2.2: Option B (`id`)
- 2.3: Option B (custom relationship descriptors such as `employee_id`)
- 2.4: Option A (`is_` / `has_`)
- 2.5: Option A (`_utc`)
- 3.1: Option B (MySQL auto-name for primary key)
- 3.2: Option A (`fk_<from_table>_<to_table>_<column>`)
- 3.3: Option A (`uq_<table>_<column_or_purpose>`)
- 3.4: Option B (`idx_<table>_<column_or_purpose>`)
- 3.5: Option A (left-to-right by filter/selectivity)
- 4.1: Option A (`public_id`)
- 4.2: `username_normalized`, `hostname_normalized`, `mac_address_normalized`
- 5.1: Option A (`_history`)
- 5.2: `changed_utc`, `changed_by_user_id`
- 5.3: `previous_hash`, `entry_hash`
- 6.1: `setting_key`, `setting_value`, `value_type`
- 6.2: yes, include typed setting value columns
- 7.1: allowed abbreviations: `id`, `utc`, `mac`, `ip`, `rbac`, `uuid`
- 7.2: banned abbreviations: `cfg`, `usr`, `ws`, `sess`, `rpt`
- 7.3: banned words: `type`, `class`, `delete`, `order`
- 8.1: `Database/Bootstrap/create_database.sql`
- 8.2: `create.sql` / `rollback.sql` / `validate.sql` inside artifact-specific folders
- 9.1: yes, lowercase only
- 9.2: yes, no quoted identifiers unless unavoidable
- 10.1: yes, hard-fail in PR validation
- 10.2: yes, explicit exception approval required in PR notes
