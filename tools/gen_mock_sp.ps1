# Generates per-table MySQL 5.7 stored-procedure files for the Developer-editable mock master
# tables under Database/StoredProcedures/. Each table gets sp_<table>_get / _insert / _update / _delete.
# Run from the repo root. 'active' flags whether the table has an is_active column.
$ErrorActionPreference = 'Stop'

$tables = @(
    @{ name='mock_master_tables_registry'; active=$true;  cols=@(
        @{ c='table_name'; t='VARCHAR(128)'; k='s' }, @{ c='ui_display_name'; t='VARCHAR(128)'; k='s' },
        @{ c='description_text'; t='VARCHAR(500)'; k='s' }, @{ c='sort_rank'; t='INT'; k='n' } ) },
    @{ name='mock_parts'; active=$true; cols=@(
        @{ c='part_number'; t='VARCHAR(50)'; k='s' }, @{ c='part_category'; t='VARCHAR(32)'; k='s' },
        @{ c='part_description'; t='VARCHAR(500)'; k='s' }, @{ c='unit_of_measure'; t='VARCHAR(20)'; k='s' } ) },
    @{ name='mock_work_orders'; active=$true; cols=@(
        @{ c='work_order_number'; t='VARCHAR(64)'; k='s' }, @{ c='part_number'; t='VARCHAR(50)'; k='s' },
        @{ c='work_center_code'; t='VARCHAR(32)'; k='s' }, @{ c='is_coil_bearing'; t='TINYINT'; k='b' } ) },
    @{ name='mock_work_centers'; active=$true; cols=@(
        @{ c='work_center_code'; t='VARCHAR(32)'; k='s' }, @{ c='work_center_description'; t='VARCHAR(255)'; k='s' },
        @{ c='building'; t='VARCHAR(128)'; k='s' } ) },
    @{ name='mock_locations'; active=$true; cols=@(
        @{ c='location_code'; t='VARCHAR(50)'; k='s' }, @{ c='location_description'; t='VARCHAR(255)'; k='s' },
        @{ c='is_ignored'; t='TINYINT'; k='b' } ) },
    @{ name='mock_requesters'; active=$true; cols=@(
        @{ c='employee_number'; t='VARCHAR(32)'; k='s' }, @{ c='display_name'; t='VARCHAR(128)'; k='s' } ) },
    @{ name='mock_inventory_locations'; active=$false; cols=@(
        @{ c='part_number'; t='VARCHAR(50)'; k='s' }, @{ c='location_code'; t='VARCHAR(50)'; k='s' },
        @{ c='on_hand_quantity'; t='DECIMAL(18,2)'; k='n' } ) },
    @{ name='mock_request_types'; active=$true; cols=@(
        @{ c='request_type'; t='VARCHAR(64)'; k='s' }, @{ c='subtype'; t='VARCHAR(128)'; k='s' } ) }
)

$root = Join-Path (Get-Location) 'Database\StoredProcedures'

function ValExpr($col) {
    if ($col.k -eq 's') { return "NULLIF(TRIM(p_$($col.c)), '')" }
    return "p_$($col.c)"
}

foreach ($t in $tables) {
    $name = $t.name
    $cols = $t.cols
    $extra = @('created_utc','updated_utc')
    if ($t.active) { $extra = @('is_active') + $extra }

    # ---------- get ----------
    $folder = Join-Path $root "sp_${name}_get"; New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $sel = ('id','public_id') + ($cols | ForEach-Object { $_.c }) + $extra
    $body = "SELECT $($sel -join ', ')`nFROM $name`nORDER BY id ASC;"
    $create = "-- Create procedure: sp_${name}_get`n-- Engine: MySQL 5.7`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_get;`n`nCREATE PROCEDURE sp_${name}_get()`n$body`n"
    Set-Content -Path (Join-Path $folder 'create.sql') -Value $create -Encoding UTF8
    Set-Content -Path (Join-Path $folder 'rollback.sql') -Value "-- Rollback Stored Procedure: sp_${name}_get`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_get;`n" -Encoding UTF8

    # ---------- insert ----------
    $folder = Join-Path $root "sp_${name}_insert"; New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $decls = $cols | ForEach-Object { "IN p_$($_.c) $($_.t)" }
    $insCols = @(); $insVals = @()
    foreach ($c in $cols) { $insCols += $c.c; $insVals += (ValExpr $c) }
    $insCols += 'public_id'; $insVals += 'UUID()'
    if ($t.active) { $insCols += 'is_active'; $insVals += '1' }
    $insCols += 'created_utc'; $insVals += 'UTC_TIMESTAMP()'
    $insCols += 'updated_utc'; $insVals += 'UTC_TIMESTAMP()'
    $body = "INSERT INTO $name ($($insCols -join ', '))`nVALUES ($($insVals -join ', '));"
    $create = "-- Create procedure: sp_${name}_insert`n-- Engine: MySQL 5.7`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_insert;`n`nCREATE PROCEDURE sp_${name}_insert(`n    $($decls -join ",`n    ")`n)`n$body`n"
    Set-Content -Path (Join-Path $folder 'create.sql') -Value $create -Encoding UTF8
    Set-Content -Path (Join-Path $folder 'rollback.sql') -Value "-- Rollback Stored Procedure: sp_${name}_insert`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_insert;`n" -Encoding UTF8

    # ---------- update (by id) ----------
    $folder = Join-Path $root "sp_${name}_update"; New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $decls = @('IN p_id BIGINT') + ($cols | ForEach-Object { "IN p_$($_.c) $($_.t)" })
    $assigns = $cols | ForEach-Object { "$($_.c) = $(ValExpr $_)" }
    $body = "UPDATE $name`nSET $($assigns -join ', '), updated_utc = UTC_TIMESTAMP()`nWHERE id = p_id;"
    $create = "-- Create procedure: sp_${name}_update`n-- Engine: MySQL 5.7`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_update;`n`nCREATE PROCEDURE sp_${name}_update(`n    $($decls -join ",`n    ")`n)`n$body`n"
    Set-Content -Path (Join-Path $folder 'create.sql') -Value $create -Encoding UTF8
    Set-Content -Path (Join-Path $folder 'rollback.sql') -Value "-- Rollback Stored Procedure: sp_${name}_update`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_update;`n" -Encoding UTF8

    # ---------- delete ----------
    $folder = Join-Path $root "sp_${name}_delete"; New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $body = "DELETE FROM $name WHERE id = p_id;"
    $create = "-- Create procedure: sp_${name}_delete`n-- Engine: MySQL 5.7`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_delete;`n`nCREATE PROCEDURE sp_${name}_delete(IN p_id BIGINT)`n$body`n"
    Set-Content -Path (Join-Path $folder 'create.sql') -Value $create -Encoding UTF8
    Set-Content -Path (Join-Path $folder 'rollback.sql') -Value "-- Rollback Stored Procedure: sp_${name}_delete`n`nUSE mtm_waitlist;`n`nDROP PROCEDURE IF EXISTS sp_${name}_delete;`n" -Encoding UTF8

    Write-Output "generated sp_${name}_get/insert/update/delete"
}
Write-Output 'done'
