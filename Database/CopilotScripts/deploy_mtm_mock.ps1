<#
.SYNOPSIS
    Deploys the mtm_mock cache database artifacts to a MySQL server.

.DESCRIPTION
    Deploys, in dependency order, every artifact under Database/Mock/:
      1. Bootstrap/create_database.sql
      2. Tables/<name>/create.sql            (5 mirrors + 5 stage twins)
      3. StoredProcedures/<name>/create.sql  (sp_visual_*)
      4. Bootstrap/update_table_descriptions.sql
      5. Seeds/<name>/create.sql

    Used by quickstart.md section 1 and by the feature's verification gates.
    Idempotent: every artifact is re-runnable (CREATE ... IF NOT EXISTS / DROP ... IF EXISTS).

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

.EXAMPLE
    $env:MYSQL_PWD='root'; ./Database/CopilotScripts/deploy_mtm_mock.ps1
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

Write-Host '1/5 Bootstrap create_database.sql'
Invoke-SqlFile -Path (Join-Path $mockRoot 'Bootstrap/create_database.sql')

Write-Host '2/5 Tables'
Get-ChildItem (Join-Path $mockRoot 'Tables') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host '3/5 StoredProcedures'
Get-ChildItem (Join-Path $mockRoot 'StoredProcedures') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host '4/5 Bootstrap update_table_descriptions.sql'
$descriptions = Join-Path $mockRoot 'Bootstrap/update_table_descriptions.sql'
if (Test-Path $descriptions) { Invoke-SqlFile -Path $descriptions -Database 'mtm_mock' }

Write-Host '5/5 Seeds'
Get-ChildItem (Join-Path $mockRoot 'Seeds') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        $create = Join-Path $_.FullName 'create.sql'
        if (Test-Path $create) { Invoke-SqlFile -Path $create -Database 'mtm_mock' }
    }

Write-Host 'mtm_mock deployed.' -ForegroundColor Green
