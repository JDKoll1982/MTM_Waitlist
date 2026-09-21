-- Rollback for function: fn_config_settings_scope_rank
-- Restores the previous precedence order rather than dropping the function: readers call it, so an absent
-- function breaks every settings read. `user` goes back to 3, below `admin` and `developer`, and `role` falls
-- through to the ELSE branch's 0 because the scope did not exist before this feature.
--
-- Reverting the order is only safe while no per-person row is stored, because a stored `user`-scoped row
-- would lose to an inherited one under the old order. That is the same precondition the forward change
-- carries, stated from the other side.

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_config_settings_scope_rank;

CREATE FUNCTION fn_config_settings_scope_rank(p_scope_type VARCHAR(16))
RETURNS TINYINT
DETERMINISTIC
RETURN CASE LOWER(TRIM(p_scope_type))
    WHEN 'computer' THEN 1
    WHEN 'all_users' THEN 2
    WHEN 'user' THEN 3
    WHEN 'admin' THEN 4
    WHEN 'developer' THEN 5
    ELSE 0
END;