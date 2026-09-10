<#
.SYNOPSIS
    Verifies the mtm_mock deployment end to end: drops, deploys from the master lists only,
    then runs the two validation scripts and asserts the expected invariants.

.DESCRIPTION
    This is the executable form of the quickstart.md section 1 gate and covers:
      * the five mirrors + five stage twins deploy from Database/Mock/AllTables.sql
      * the ten sp_visual_* procedures deploy from Database/Mock/AllSPs.sql
      * every shape refreshes and reads back with an identical result shape (FR-004)
      * a refresh never leaves a partial/empty live table and no `_prev` table persists (FR-006/SC-005)
      * the master lists are in sync with their file-per-artifact sources

.PARAMETER Server / Port / User / Password / MysqlExe
    MySQL connection settings. Prefer the MYSQL_PWD environment variable for the password.

.EXAMPLE
    $env:MYSQL_PWD='root'; pwsh -NoProfile -File Database/CopilotScripts/verify_mtm_mock_deploy.ps1
#>
[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [int]$Port = 3306,
    [string]$User = 'root',
    [string]$Password,
    [string]$MysqlExe
)

$ErrorActionPreference = 'Stop'

# This script lives in <repo>/Database/CopilotScripts.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$mockRoot = Join-Path $repoRoot 'Database/Mock'

if (-not $MysqlExe) {
    $cmd = Get-Command mysql -ErrorAction SilentlyContinue
    if ($cmd) { $MysqlExe = $cmd.Source }
    else {
        $candidate = Get-ChildItem 'C:\Program Files\MySQL' -Recurse -Filter 'mysql.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if (-not $candidate) { throw 'mysql.exe not found. Pass -MysqlExe explicitly.' }
        $MysqlExe = $candidate.FullName
    }
}

if ($Password) { $env:MYSQL_PWD = $Password }

$failures = [System.Collections.Generic.List[string]]::new()

function Invoke-Sql {
    param([string]$Sql, [string]$Database)

    # -N (--skip-column-names) keeps scalar reads clean; the validation scripts' own
    # `SELECT 'step' AS shape` banners still come through and are discarded by callers.
    $arguments = @('-h', $Server, '-P', $Port, '-u', $User, '--default-character-set=utf8mb4', '-N')
    if ($Database) { $arguments += $Database }

    $output = $Sql | & $MysqlExe @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SQL failed (exit $LASTEXITCODE):`n$output`n--- SQL ---`n$Sql"
    }
    return $output
}

function Get-Scalar {
    param([string]$Sql, [string]$Database)
    # Single scalar query: the first non-empty line is the value.
    return ((Invoke-Sql -Sql $Sql -Database $Database) | Where-Object { "$_".Trim() -ne '' } | Select-Object -First 1)
}

function Invoke-SqlFile {
    param([string]$Path, [string]$Database)
    Invoke-Sql -Sql (Get-Content -LiteralPath $Path -Raw) -Database $Database | Out-Null
}

function Assert-Equal {
    param($Actual, $Expected, [string]$What)
    if ("$Actual" -ne "$Expected") {
        $failures.Add("$What — expected [$Expected], got [$Actual]")
        Write-Host "  FAIL $What (expected $Expected, got $Actual)" -ForegroundColor Red
    }
    else {
        Write-Host "  ok   $What = $Actual" -ForegroundColor DarkGray
    }
}

Write-Host "1. Master lists in sync with artifacts" -ForegroundColor Cyan
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'build_mtm_mock_masters.ps1') -Check
if ($LASTEXITCODE -ne 0) { $failures.Add('Master lists are out of date.') }

Write-Host "2. Drop and redeploy mtm_mock from the master lists only" -ForegroundColor Cyan
Invoke-SqlFile -Path (Join-Path $mockRoot 'Bootstrap/rollback.sql')
Invoke-SqlFile -Path (Join-Path $mockRoot 'Bootstrap/create_database.sql')
Invoke-SqlFile -Path (Join-Path $mockRoot 'AllTables.sql') -Database 'mtm_mock'
Invoke-SqlFile -Path (Join-Path $mockRoot 'AllSPs.sql') -Database 'mtm_mock'

$tableCount = Get-Scalar -Sql 'SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = ''mtm_mock'';' -Database 'mtm_mock'
$procCount = Get-Scalar -Sql 'SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = ''mtm_mock'' AND routine_type = ''PROCEDURE'';' -Database 'mtm_mock'
Assert-Equal $tableCount 10 'tables deployed (5 mirrors + 5 stage twins)'
Assert-Equal $procCount 10 'procedures deployed (sp_visual_<shape>_{get,refresh})'

Write-Host "3. Cold cache serves baseline seed content (FR-017)" -ForegroundColor Cyan
Invoke-SqlFile -Path (Join-Path $mockRoot 'AllSeeds.sql') -Database 'mtm_mock' | Out-Null

$seedHit = (Invoke-Sql -Sql "CALL sp_visual_work_order_lookup_get('SEED-WO-100');" -Database 'mtm_mock') -join "`n"
if ($seedHit -notmatch 'SEED-PART-100') {
    $failures.Add('FR-017: a never-refreshed cache did not serve the baseline seed content.')
}
else { Write-Host '  ok   never-refreshed cache serves baseline seed content' -ForegroundColor DarkGray }

$seedFlags = Get-Scalar -Sql "SELECT COUNT(*) FROM visual_work_order_lookup_result WHERE is_seed_content = 1;" -Database 'mtm_mock'
Assert-Equal $seedFlags 1 'seeded rows are marked is_seed_content = 1'

Write-Host "4. All-shapes round trip (FR-004)" -ForegroundColor Cyan
Invoke-SqlFile -Path (Join-Path $mockRoot 'Validation/all_shapes_roundtrip/validate.sql') -Database 'mtm_mock' | Out-Null

$sentinel = (Invoke-Sql -Sql "CALL sp_visual_work_order_lookup_get('VAL-WO');" -Database 'mtm_mock') -join "`n"
if ($sentinel -notmatch 'VAL-P1') { $failures.Add('work_order_lookup get did not return the refreshed sentinel row.') }
else { Write-Host '  ok   work_order_lookup get returns the refreshed row' -ForegroundColor DarkGray }

Write-Host "5. Refresh invariants (FR-006 / SC-005)" -ForegroundColor Cyan
$prevTables = Get-Scalar -Sql "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='mtm_mock' AND table_name LIKE '%\_prev';" -Database 'mtm_mock'
Assert-Equal $prevTables 0 'no transient _prev table persists'

$seedRows = Get-Scalar -Sql "SELECT (SELECT COUNT(*) FROM visual_work_order_lookup_result WHERE is_seed_content = 1) + (SELECT COUNT(*) FROM visual_operation_sequences_result WHERE is_seed_content = 1) + (SELECT COUNT(*) FROM visual_subordinate_parts_result WHERE is_seed_content = 1) + (SELECT COUNT(*) FROM visual_inventory_locations_result WHERE is_seed_content = 1) + (SELECT COUNT(*) FROM visual_disposition_input_result WHERE is_seed_content = 1);" -Database 'mtm_mock'
Assert-Equal $seedRows 0 'no seed rows remain after refresh (all mirrors carry refreshed content)'

Write-Host "6. Refresh swap smoke (FR-006)" -ForegroundColor Cyan
Invoke-SqlFile -Path (Join-Path $mockRoot 'Validation/refresh_swap_smoke/validate.sql') -Database 'mtm_mock' | Out-Null
$smokeRows = Get-Scalar -Sql 'SELECT COUNT(*) FROM visual_work_order_lookup_result;' -Database 'mtm_mock'
Assert-Equal $smokeRows 1 'live mirror holds exactly the complete sentinel snapshot'

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host "FAILED ($($failures.Count))" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'mtm_mock deployment verified: all checks passed.' -ForegroundColor Green
