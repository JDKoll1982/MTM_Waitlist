<#
.SYNOPSIS
    Deploys the mtm_mock cache database artifacts to a MySQL server.

.DESCRIPTION
    Deploys, in dependency order, every artifact under Database/Mock/:

      0. capture_mock_seed_baseline.ps1      (re-records the baseline seed from THIS host's live mirror)
      1. Bootstrap/create_database.sql
      2. Tables/<name>/create.sql            (one mirror + one stage twin per shape)
      3. StoredProcedures/<name>/create.sql  (sp_visual_*)
      4. Bootstrap/update_table_descriptions.sql
      5. Seeds/<name>/create.sql

    Step 0 is first because the baseline seed is what a machine with a cold cache serves, and a capture taken
    months ago describes a mirror that no longer exists: it once carried 79,218 shape-4 rows that were
    overwhelmingly zero-stock, and the population read had since been narrowed to locations holding stock. So
    the seed is re-recorded from the host being deployed to, and step 6 then applies it.

    Step 0 is READ-ONLY against the database and never fails the deploy. The capture refuses to write unless the
    rows it read equal the live COUNT(*), so a failed capture leaves the committed seed exactly as it was, and
    this script deploys that instead of stopping. A host with nothing to capture — no mirror yet, or a mirror
    holding only seed content because it has never been refreshed — is an ordinary outcome that reports itself
    and is skipped (`capture_mock_seed_baseline.ps1` exits 3; see its own help).

    Used by quickstart.md section 1 and by the feature's verification gates.
    Idempotent: every artifact is re-runnable (CREATE ... IF NOT EXISTS / DROP ... IF EXISTS).

    Do not point this at a host whose cache is warm and in use if the intent is only to check the artifacts: it
    drops and recreates every procedure (`DROP PROCEDURE IF EXISTS`), including the refresh procedures the cache
    service is running. `verify_mtm_mock_deploy.ps1` is also destructive; the read-only checks are
    `build_mtm_mock_masters.ps1 -Check` and `Database/Mock/Validation/read_shape_metadata/validate.sql`.

.PARAMETER Server
    MySQL host. Default: localhost

.PARAMETER Port
    MySQL port. Default: 3306

.PARAMETER User
    MySQL user. Default: root

.PARAMETER Password
    MySQL password. Prefer the MYSQL_PWD environment variable over this parameter.

.PARAMETER MysqlExe
    Path to mysql.exe. Default: resolved from PATH, else the MySQL 9.6 install.

.PARAMETER SkipSeedCapture
    Deploy the committed baseline seed without re-recording it from this host. Use it when the host's mirror is
    the wrong source — a development store whose cache was filled from the seed in the first place — or when the
    seed is deliberately being carried between machines.

.EXAMPLE
    $env:MYSQL_PWD='root'; ./Database/CopilotScripts/deploy_mtm_mock.ps1
#>
[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [int]$Port = 3306,
    [string]$User = 'root',
    [string]$Password,
    [string]$MysqlExe,
    [switch]$SkipSeedCapture
)

$ErrorActionPreference = 'Stop'

# This script lives in <repo>/Database/CopilotScripts.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$mockRoot = Join-Path $repoRoot 'Database/Mock'

if (-not $MysqlExe) {
    $cmd = Get-Command mysql -ErrorAction SilentlyContinue
    if ($cmd) {
        $MysqlExe = $cmd.Source
    }
    else {
        $candidate = Get-ChildItem 'C:\Program Files\MySQL' -Recurse -Filter 'mysql.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if (-not $candidate) { throw 'mysql.exe not found. Pass -MysqlExe explicitly.' }
        $MysqlExe = $candidate.FullName
    }
}

if ($Password) { $env:MYSQL_PWD = $Password }
if (-not $env:MYSQL_PWD) {
    Write-Warning 'MYSQL_PWD is not set; the client may prompt for a password.'
}

function Invoke-SqlFile {
    param([string]$Path, [string]$Database)

    if (-not (Test-Path $Path)) { throw "Artifact not found: $Path" }

    $arguments = @('-h', $Server, '-P', $Port, '-u', $User, '--default-character-set=utf8mb4')
    if ($Database) { $arguments += $Database }

    Write-Host "  -> $(Resolve-Path -Relative $Path)" -ForegroundColor DarkGray

    # Pipe the artifact through stdin: `SOURCE` is not accepted via -e, and stdin lets the
    # client honour DELIMITER directives in multi-statement procedure files.
    Get-Content -LiteralPath $Path -Raw | & $MysqlExe @arguments
    if ($LASTEXITCODE -ne 0) { throw "Failed deploying $Path (exit $LASTEXITCODE)" }
}

Write-Host "Deploying mtm_mock to ${Server}:${Port} as $User" -ForegroundColor Cyan

# Step 0. The baseline seed is re-recorded from the host being deployed to, because a capture describes the mirror
# it was taken from and mirrors change shape. It must run before the seeds are applied, and it is the only step
# here that reads rather than writes a database.
if ($SkipSeedCapture) {
    Write-Host '0/6 Baseline seed capture skipped (-SkipSeedCapture): the committed seed is deployed as it is.' -ForegroundColor Yellow
}
else {
    Write-Host '0/6 Re-record the baseline seed from this host''s live mirror'
    $captureScript = Join-Path $PSScriptRoot 'capture_mock_seed_baseline.ps1'
    $pwshExe = (Get-Command pwsh -ErrorAction SilentlyContinue).Source

    if (-not $captureScript -or -not (Test-Path $captureScript)) {
        Write-Warning "capture_mock_seed_baseline.ps1 is missing beside this script, so the committed seed is deployed as it is."
    }
    elseif (-not $pwshExe) {
        Write-Warning 'PowerShell 7 (pwsh) was not found, so the baseline seed could not be re-recorded. Deploying the committed seed, which is unchanged.'
    }
    elseif (-not $env:MYSQL_PWD) {
        # The capture would have to take its credential from the application's connection string, which can name a
        # different host from the one being deployed to. Refusing is better than capturing the wrong mirror.
        Write-Warning 'No MySQL password is in play, so the baseline seed was not re-recorded: the capture would have had to take its target''s credential from another host''s connection string. Set MYSQL_PWD (or pass -Password) and re-run, or pass -SkipSeedCapture to say so deliberately.'
    }
    else {
        # The credential reaches the child through MYSQL_PWD, which it inherits. It is never passed as an argument,
        # because that would put it in the process list.
        & $pwshExe -NoProfile -ExecutionPolicy Bypass -File $captureScript `
            -Server $Server -Port $Port -User $User

        switch ($LASTEXITCODE) {
            0 { Write-Host '      seed re-recorded from this host.' -ForegroundColor DarkGray }
            3 { Write-Host '      nothing to capture on this host; the committed seed is deployed.' -ForegroundColor DarkGray }
            default {
                Write-Warning "Re-recording the baseline seed failed (exit $LASTEXITCODE). The seed on disk is untouched, so the committed one is deployed."
            }
        }
    }
}

Write-Host '1/6 Bootstrap create_database.sql'
Invoke-SqlFile -Path (Join-Path $mockRoot 'Bootstrap/create_database.sql')

Write-Host '2/6 Tables'
Get-ChildItem (Join-Path $mockRoot 'Tables') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host '3/6 StoredProcedures'
Get-ChildItem (Join-Path $mockRoot 'StoredProcedures') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host '4/6 Bootstrap update_table_descriptions.sql'
$descriptions = Join-Path $mockRoot 'Bootstrap/update_table_descriptions.sql'
if (Test-Path $descriptions) { Invoke-SqlFile -Path $descriptions -Database 'mtm_mock' }

Write-Host '6/6 Seeds'
Get-ChildItem (Join-Path $mockRoot 'Seeds') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host 'mtm_mock deployed.' -ForegroundColor Green
