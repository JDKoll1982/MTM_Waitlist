-- Create function: fn_config_settings_scope_rank
-- Engine: MySQL 5.7
-- Purpose: the precedence rung of a settings scope. The reader takes the highest-ranked applicable row, so a
--          higher number means the row wins.
-- Corrected order: a person's own value now beats every wider scope. `user` moved from 3 to 6, above `role`,
--          `admin` and `developer`, because a value stored for an individual was previously overridden by an
--          inherited one (FR-053). `role` is new and sits between the two legacy broad scopes and `admin`.
-- Safe to ship now: every row in config_settings_values today is scoped `all_users`, so the re-rank moves no
--          row's effective answer. The census is pinned by
--          MTM_Waitlist.Tests/Module_Settings/Fixtures/PermissionMigrationBaseline.cs.
-- Ordering rule: this function must be in place before any per-person permission row is ever written, or that
--          row resolves under the old order and is silently overridden.

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_config_settings_scope_rank;

CREATE FUNCTION fn_config_settings_scope_rank(p_scope_type VARCHAR(16))
RETURNS TINYINT
DETERMINISTIC
RETURN CASE LOWER(TRIM(p_scope_type))
    WHEN 'computer' THEN 1
    WHEN 'all_users' THEN 2
    WHEN 'role' THEN 3
    WHEN 'admin' THEN 4
    WHEN 'developer' THEN 5
    WHEN 'user' THEN 6
    ELSE 0
END;