-- Create table: core_computer_buildings
-- Engine: MySQL 5.7
--
-- Which building a computer stands in. Machine setup asks for it while the computer is being configured, and
-- every screen that offers a building choice opens on this answer, so the building a workstation belongs to is
-- captured once and then read rather than asked for again.
--
-- One row per computer
--   `uq_core_computer_buildings_computer_id` is what makes the answer singular: a computer standing in two
--   buildings is not a state any screen could draw, so the store refuses it rather than the application having
--   to pick one.
--
-- The default is `Expo Drive`
--   A computer whose row is written without a building — a row inserted by a seed or by an operator before the
--   screen asks — stands in the plant's first building, which is where a workstation without an answer is.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS core_computer_buildings (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    computer_id BIGINT NOT NULL COMMENT 'The computer, from core_computers_registry.id.',
    building_name VARCHAR(64) NOT NULL DEFAULT 'Expo Drive' COMMENT 'The building this computer stands in, spelled as core_buildings_catalog.building_name spells it.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp of the last write.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_computer_buildings_public_id (public_id),
    UNIQUE KEY uq_core_computer_buildings_computer_id (computer_id),
    KEY idx_core_computer_buildings_building_name (building_name),
    CONSTRAINT fk_core_computer_buildings_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'The building each computer stands in, chosen when that computer is set up.';

SET FOREIGN_KEY_CHECKS = 1;
