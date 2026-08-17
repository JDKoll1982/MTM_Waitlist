# SQL Naming Compliance Spec

## Objective
Enforce hard-fail naming and schema conventions for all SQL files under Database/.

## CI Entry Point
- Workflow file: .github/workflows/sql-naming-compliance.yml
- Validator script: .github/scripts/validate-sql-naming.ps1

## Trigger Conditions
- Pull requests that change:
- Database/**/*.sql
- Database/Bootstrap/update_table_descriptions.sql
- .github/scripts/validate-sql-naming.ps1
- .github/workflows/sql-naming-compliance.yml
- .github/instructions/database-schema-rules.instructions.md
- Manual trigger via workflow_dispatch.

## Validation Rules
1. File naming:
- Bootstrap file must be `Database/Bootstrap/create_database.sql`.
- Table/column description update file must be `Database/Bootstrap/update_table_descriptions.sql`.
- Table/procedure/view files must use per-artifact folders with `create.sql` and `rollback.sql`.
- Seed files must use per-artifact folders with `create.sql` and `rollback.sql`.
- Validation files must use per-artifact folders with `validate.sql`.

8. Description update policy:
- Any SQL add/edit/remove under `Database/` must include a synchronized update to `Database/Bootstrap/update_table_descriptions.sql`.
- New tables must include a table comment update and per-column comment updates in that script.
- Removed tables/columns must be removed from that script in the same change.

2. Identifiers:
- Lowercase snake_case only.
- No quoted identifiers.
- Banned abbreviations rejected: cfg, usr, ws, sess, rpt.
- Banned words rejected: class, delete, order, type.
- Exception: value_type is allowed.

3. Table naming:
- Must follow module structure with at least three segments:
- {category}_{table}_{action_or_purpose}

4. Required table columns:
- id BIGINT NOT NULL AUTO_INCREMENT
- public_id CHAR(36) NOT NULL

5. Column naming:
- DATETIME/TIMESTAMP columns must end with _utc.
- TINYINT(1) columns must start with is_/has_.
- Typed settings boolean columns may end in _bool.

6. Constraints and indexes:
- Foreign key constraints must match fk_<from_table>_<to_table>_<column>.
- Unique keys must start with uq_.
- Non-unique indexes must start with idx_.

7. Identifier length guardrail (MySQL 5.7):
- All identifiers (table, column, constraint, key, index) must be 64 characters or fewer.
- Prefer concise middle segments for composite keys and constraints to keep names deterministic and under the limit.
- Keep required prefixes (`fk_`, `uq_`, `idx_`) while shortening descriptive suffixes when needed.

## Failure Behavior
- Any violation exits with status code 1 and fails the CI job.
- Violations are printed with file and line context.

## Exception Process
- Naming exceptions require explicit written approval in PR notes.
