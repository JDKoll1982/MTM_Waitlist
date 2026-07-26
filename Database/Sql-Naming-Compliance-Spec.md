# SQL Naming Compliance Spec

## Objective
Enforce hard-fail naming and schema conventions for all SQL files under Database/.

## CI Entry Point
- Workflow file: .github/workflows/sql-naming-compliance.yml
- Validator script: .github/scripts/validate-sql-naming.ps1

## Trigger Conditions
- Pull requests that change:
- Database/**/*.sql
- .github/scripts/validate-sql-naming.ps1
- .github/workflows/sql-naming-compliance.yml
- .github/instructions/database-schema-rules.instructions.md
- Manual trigger via workflow_dispatch.

## Validation Rules
1. File naming:
- Migration and rollback files must match:
- 0001__description.sql
- 0001__description_rollback.sql
- Seed files must match seed_<description>.sql.

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

## Failure Behavior
- Any violation exits with status code 1 and fails the CI job.
- Violations are printed with file and line context.

## Exception Process
- Naming exceptions require explicit written approval in migration PR notes.
