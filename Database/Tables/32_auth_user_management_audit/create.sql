-- Create table: auth_user_management_audit
-- Engine: MySQL 5.7
-- Purpose: One row per account field that actually changed, so who changed what on whose account is
--          answerable after the fact (FR-108, FR-109, FR-110). It records changes and never attempts: a
--          refused write leaves no row here, because nothing changed (FR-026).
-- Shape: one row per changed field. A save that touches four fields writes four rows sharing one
--        change_group_id, so one act reads as one act (FR-110).
--        A password reset writes one row with BOTH value columns null, because the temporary credential is
--        never stored in readable form and there is no before or after value to record (FR-111, FR-030).
-- Actor snapshots: actor_display_name, actor_employee_identifier and actor_role_code are copied in rather
--        than joined, so a later rename or role change cannot re-attribute what somebody did (FR-109).
-- Readers: nothing in the application reads this table. It is written by the user-management procedures and
--        read by support.
-- No foreign keys, deliberately: the actor columns are snapshots and target_user_id must outlive the account
--        it names, so a constraint back to core_users_profiles would both contradict the snapshot rule and
--        block the hard delete of a person the audit talks about.
-- Retention: this feature defines no retention window for this table.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_user_management_audit (
    id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for external references.',
    change_group_id CHAR(36) NOT NULL COMMENT 'Shared by every row one save produced, so one act reads as one act.',
    target_user_id BIGINT NOT NULL COMMENT 'The person whose account changed.',
    field_name VARCHAR(64) NOT NULL COMMENT 'The one field this row is about.',
    previous_value TEXT NULL COMMENT 'The value before the change, or NULL where there is none to record.',
    changed_value TEXT NULL COMMENT 'The value after the change, or NULL where there is none to record.',
    actor_user_id BIGINT NULL COMMENT 'The acting person, joined for durability.',
    actor_display_name VARCHAR(256) NOT NULL COMMENT 'The actor display name as it was when they acted.',
    actor_employee_identifier VARCHAR(128) NOT NULL COMMENT 'The actor employee number as it was when they acted.',
    actor_role_code VARCHAR(64) NOT NULL COMMENT 'The actor role code as it was when they acted.',
    occurred_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the change was recorded.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_user_management_audit_public_id (public_id),
    KEY idx_auth_user_management_audit_change_group_id (change_group_id),
    KEY idx_audit_target_user_id_occurred_utc (target_user_id, occurred_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
