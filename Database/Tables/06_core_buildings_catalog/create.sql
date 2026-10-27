-- Create table: core_buildings_catalog
-- Engine: MySQL 5.7
--
-- The buildings the plant is split into. A work centre names its building in
-- `setup_work_centers_catalog.building`, and a computer names the building it stands in through
-- `core_computer_buildings`, so this table is the one place those two spellings are kept.
--
-- Why it is back after being retired by 010-startup-rebuild (T061)
--   The table was retired as dead weight: nothing read it, because the two screens that offer a building
--   choice carried their list in code. The building is now chosen when a computer is set up and is edited from
--   the Operations settings, so the list is data again and this table is what holds it.
--
-- The default is `Expo Drive`
--   The plant's first building is Expo Drive and every existing work centre but the Vits Drive ones is there,
--   so a row inserted without a name lands where the plant actually runs rather than nowhere.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS core_buildings_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building_name VARCHAR(64) NOT NULL DEFAULT 'Expo Drive' COMMENT 'The building as people name it, and exactly the value a work centre or a computer records.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT '0 withdraws the building from every chooser without deleting the rows that name it.',
    sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Display order inside the choosers.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp of the last write.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_buildings_catalog_public_id (public_id),
    UNIQUE KEY uq_core_buildings_catalog_building_name (building_name),
    KEY idx_core_buildings_catalog_is_active (is_active),
    KEY idx_core_buildings_catalog_sort_rank (sort_rank)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'The buildings the plant is split into, as the one list every building chooser reads.';

SET FOREIGN_KEY_CHECKS = 1;
