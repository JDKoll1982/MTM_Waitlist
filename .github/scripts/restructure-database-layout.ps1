param(
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

function Initialize-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        if ($WhatIf) {
            Write-Host "Create $Path"
        }
        else {
            New-Item -ItemType Directory -Path $Path | Out-Null
        }
    }
}

function Write-TextFile {
    param(
        [string]$Path,
        [string]$Content
    )

    Initialize-Directory (Split-Path -Parent $Path)

    if ($WhatIf) {
        Write-Host "Write $Path"
        return
    }

    Set-Content -Path $Path -Value $Content -NoNewline
}

function Remove-PathIfExists {
    param([string]$Path)
    if (Test-Path $Path) {
        if ($WhatIf) {
            Write-Host "Remove $Path"
        }
        else {
            Remove-Item -Path $Path -Recurse -Force
        }
    }
}

function Get-TableBlock {
    param(
        [string]$Source,
        [string]$TableName
    )

    $pattern = '(?ms)CREATE TABLE IF NOT EXISTS ' + [regex]::Escape($TableName) + '\s*\(.*?\) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;'
    $match = [regex]::Match($Source, $pattern)
    if (-not $match.Success) {
        throw "Unable to find CREATE TABLE block for $TableName"
    }

    return $match.Value
}

$bootstrapCreate = @'
-- Create MTM_Waitlist application database
-- Engine: MySQL 5.7
-- Run once per target environment before executing any other migrations.

CREATE DATABASE IF NOT EXISTS mtm_waitlist CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE mtm_waitlist;
'@

$bootstrapRollback = @'
-- Rollback for create database

DROP DATABASE IF EXISTS mtm_waitlist;
'@

$baselineSchema = Get-Content -Path (Join-Path $repoRoot 'Database\Migrations\0001__baseline_startup_schema.sql') -Raw
$validationScript = Get-Content -Path (Join-Path $repoRoot 'Database\Validation\0003__validate_startup_schema.sql') -Raw
$functionCreate = Get-Content -Path (Join-Path $repoRoot 'Database\Migrations\0002__fn_server_utc_now.sql') -Raw
$functionRollback = Get-Content -Path (Join-Path $repoRoot 'Database\Rollbacks\0002__fn_server_utc_now_rollback.sql') -Raw

Write-TextFile -Path (Join-Path $repoRoot 'Database\Bootstrap\create_database.sql') -Content $bootstrapCreate
Write-TextFile -Path (Join-Path $repoRoot 'Database\Bootstrap\rollback.sql') -Content $bootstrapRollback

$tableNames = @(
    'auth_roles_catalog',
    'core_users_profiles',
    'auth_roles_assignments',
    'core_workstations_registry',
    'auth_sessions_tokens',
    'config_settings_values',
    'config_settings_history',
    'ops_startup_logs'
)

foreach ($tableName in $tableNames) {
    $tableBlock = Get-TableBlock -Source $baselineSchema -TableName $tableName
    $createContent = @"
-- Create table: $tableName
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

$tableBlock

SET FOREIGN_KEY_CHECKS = 1;
"@

    $rollbackContent = @"
-- Rollback for table: $tableName

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS $tableName;

SET FOREIGN_KEY_CHECKS = 1;
"@

    $artifactFolder = Join-Path $repoRoot ("Database\Tables\$tableName")
    Write-TextFile -Path (Join-Path $artifactFolder 'create.sql') -Content $createContent
    Write-TextFile -Path (Join-Path $artifactFolder 'rollback.sql') -Content $rollbackContent
}

$functionFolder = Join-Path $repoRoot 'Database\StoredProcedures\fn_server_utc_now'
Write-TextFile -Path (Join-Path $functionFolder 'create.sql') -Content $functionCreate
Write-TextFile -Path (Join-Path $functionFolder 'rollback.sql') -Content $functionRollback

$seedFolder = Join-Path $repoRoot 'Database\Seeds\seed_dev_masked_baseline'
$seedCreate = @'
-- Dev/test masked baseline seed
-- Do not use real production identity/session data in non-production.

USE mtm_waitlist;

INSERT INTO
    auth_roles_catalog (
        public_id,
        role_code,
        role_name,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'material_handler',
        'Material Handler',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production',
        'Production',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production_lead',
        'Production Lead',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup',
        'Setup',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup_lead',
        'Setup Lead',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'plant_manager',
        'Plant Manager',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'developer',
        'Developer',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    role_name = VALUES(role_name),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        setting_value_int,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES (
        UUID(),
        'sessions.retention_inactive_days',
        30,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'waitlist.resolved_retention_days',
        90,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'settings.history_retention_days',
        30,
        'int',
        NULL,
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    setting_value_int = VALUES(setting_value_int),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    core_users_profiles (
        public_id,
        username_normalized,
        display_name,
        employee_identifier,
        is_active,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'johnk',
        'John Koll',
        'emp-dev-001',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    display_name = VALUES(display_name),
    employee_identifier = VALUES(employee_identifier),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    auth_roles_assignments (
        public_id,
        user_id,
        role_id,
        assigned_utc,
        assigned_by_user_id
    )
SELECT UUID(), users.id, roles.id, UTC_TIMESTAMP(), users.id
FROM
    core_users_profiles users
    INNER JOIN auth_roles_catalog roles ON roles.role_code = 'developer'
WHERE
    users.username_normalized = 'johnk'
ON DUPLICATE KEY UPDATE
    assigned_utc = VALUES(assigned_utc),
    assigned_by_user_id = VALUES(assigned_by_user_id);

INSERT INTO
    core_workstations_registry (
        public_id,
        workstation_name,
        hostname_normalized,
        mac_address_normalized,
        is_registered,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'johnspc',
        'johnspc',
        'd8-43-ae-47-d0-d6',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    workstation_name = VALUES(workstation_name),
    updated_utc = VALUES(updated_utc);
'@

$seedRollback = @'
-- Rollback for dev/test masked baseline seed
-- Remove the synthetic startup seed records if they exist.

USE mtm_waitlist;

DELETE FROM auth_roles_assignments WHERE assigned_by_user_id IS NOT NULL;
DELETE FROM core_workstations_registry WHERE workstation_name = 'johnspc';
DELETE FROM core_users_profiles WHERE username_normalized = 'johnk';
DELETE FROM config_settings_values WHERE setting_key IN (
    'sessions.retention_inactive_days',
    'waitlist.resolved_retention_days',
    'settings.history_retention_days'
);
DELETE FROM auth_roles_catalog WHERE role_code IN (
    'material_handler',
    'production',
    'production_lead',
    'setup',
    'setup_lead',
    'plant_manager',
    'developer'
);
'@

Write-TextFile -Path (Join-Path $seedFolder 'create.sql') -Content $seedCreate
Write-TextFile -Path (Join-Path $seedFolder 'rollback.sql') -Content $seedRollback

$validationFolder = Join-Path $repoRoot 'Database\Validation\startup_schema'
Write-TextFile -Path (Join-Path $validationFolder 'validate.sql') -Content $validationScript

if (-not $WhatIf) {
    Remove-PathIfExists -Path (Join-Path $repoRoot 'Database\Migrations')
    Remove-PathIfExists -Path (Join-Path $repoRoot 'Database\Rollbacks')
    Remove-PathIfExists -Path (Join-Path $repoRoot 'Database\Seeds\seed_dev_masked_baseline.sql')
    Remove-PathIfExists -Path (Join-Path $repoRoot 'Database\Validation\0003__validate_startup_schema.sql')
}

Write-Host 'Database layout restructure complete.'
