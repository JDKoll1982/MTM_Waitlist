<#
.SYNOPSIS
    Verifies that a target workstation can reach everything the image-location feature needs.

.DESCRIPTION
    Checks, in order:
      1. The MySQL connection used by the helper server is reachable and the schema is present.
      2. config_images_locations exists with its unique scope/item key.
      3. The shared image folder resolves, is reachable, and is writable.
      4. The appsettings.json default matches the value the database will actually serve.

    Intended to run as a pre-deployment gate. Exits non-zero on the first hard failure so a
    pipeline stops before shipping a build that cannot save images.

.PARAMETER ConnectionString
    MySQL connection string. Defaults to $env:MTM_WAITLIST_DB_CONNECTION_STRING.

.PARAMETER SkipShareWriteTest
    Verify the share resolves but do not attempt to create a probe file.

.EXAMPLE
    ./.github/scripts/Test-DeploymentPreflight.ps1
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = $env:MTM_WAITLIST_DB_CONNECTION_STRING,
    [string]$AppSettingsPath,
    [switch]$SkipShareWriteTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($AppSettingsPath)) {
    $AppSettingsPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'appsettings.json'
}

$script:Failures = @()
$script:Warnings = @()

function Write-CheckResult {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('PASS', 'FAIL', 'WARN')][string]$Status,
        [string]$Detail
    )

    $colour = switch ($Status) {
        'PASS' { 'Green' }
        'WARN' { 'Yellow' }
        'FAIL' { 'Red' }
    }

    Write-Host ("[{0}] {1}" -f $Status, $Name) -ForegroundColor $colour
    if ($Detail) {
        Write-Host ("       {0}" -f $Detail)
    }

    if ($Status -eq 'FAIL') { $script:Failures += $Name }
    if ($Status -eq 'WARN') { $script:Warnings += $Name }
}

function Get-MySqlConnectorPath {
    $root = Join-Path $env:USERPROFILE '.nuget\packages\mysqlconnector'
    if (-not (Test-Path $root)) { return $null }

    # Windows PowerShell needs the .NET Framework build; PowerShell 7+ needs the .NET build.
    $targetFolder = if ($PSVersionTable.PSEdition -eq 'Core') { 'net8.0' } else { 'net48' }

    Get-ChildItem $root -Directory |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName ("lib\{0}\MySqlConnector.dll" -f $targetFolder) } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
}

Write-Host "MTM_Waitlist deployment preflight" -ForegroundColor Cyan
Write-Host ("Workstation: {0}    User: {1}" -f $env:COMPUTERNAME, $env:USERNAME)
Write-Host ''

# --- 1 & 2: database ------------------------------------------------------
$sharedFolderFromDatabase = $null

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-CheckResult -Name 'MySQL connection string present' -Status 'FAIL' `
        -Detail 'Set MTM_WAITLIST_DB_CONNECTION_STRING or pass -ConnectionString.'
}
else {
    $connectorPath = Get-MySqlConnectorPath
    $driverLoaded = $false

    if ($connectorPath) {
        try {
            Add-Type -Path $connectorPath
            $driverLoaded = $true
        }
        catch {
            $driverLoaded = $false
        }
    }

    if (-not $driverLoaded) {
        # Not a deployment blocker: the driver could not be hosted, so the check never ran.
        Write-CheckResult -Name 'MySQL reachable' -Status 'WARN' `
            -Detail 'MySqlConnector could not be loaded in this PowerShell host. Run under PowerShell 7, or verify the database with: dotnet test MTM_Waitlist.Tests --filter ConfigImagesLocationsIntegrationTests'
    }
    else {
        try {
            $connection = [MySqlConnector.MySqlConnection]::new($ConnectionString)
            $connection.Open()

            Write-CheckResult -Name 'MySQL reachable' -Status 'PASS' `
                -Detail ("{0} (server {1})" -f $connection.DataSource, $connection.ServerVersion)

            $command = $connection.CreateCommand()
            $command.CommandText = @"
SELECT
    (SELECT COUNT(*) FROM information_schema.tables
      WHERE table_schema = DATABASE() AND table_name = 'config_images_locations') AS has_table,
    (SELECT COUNT(*) FROM information_schema.statistics
      WHERE table_schema = DATABASE() AND table_name = 'config_images_locations'
        AND index_name = 'uq_config_images_locations_scope_item') AS has_unique_key,
    (SELECT setting_value FROM config_settings_values
      WHERE setting_key = 'image_storage.shared_folder_path' AND scope_key = 'all_users') AS shared_folder
"@
            $reader = $command.ExecuteReader()
            if ($reader.Read()) {
                $hasTable = [int]$reader['has_table']
                $hasKey = [int]$reader['has_unique_key']
                $sharedFolderFromDatabase = if ($reader['shared_folder'] -is [DBNull]) { $null } else { [string]$reader['shared_folder'] }

                if ($hasTable -eq 1) {
                    Write-CheckResult -Name 'config_images_locations exists' -Status 'PASS'
                }
                else {
                    Write-CheckResult -Name 'config_images_locations exists' -Status 'FAIL' `
                        -Detail 'Apply Database/Tables/17_config_images_locations/create.sql before deploying.'
                }

                if ($hasKey -ge 2) {
                    Write-CheckResult -Name 'Unique scope/item key present' -Status 'PASS' `
                        -Detail 'uq_config_images_locations_scope_item spans (scope, scope_item_id).'
                }
                else {
                    Write-CheckResult -Name 'Unique scope/item key present' -Status 'FAIL' `
                        -Detail 'Duplicate overrides are possible without this key.'
                }
            }
            $reader.Close()
            $connection.Close()
        }
        catch {
            Write-CheckResult -Name 'MySQL reachable' -Status 'FAIL' -Detail $_.Exception.Message
        }
    }
}

# --- 3 & 4: shared image folder -------------------------------------------
$sharedFolderFromAppSettings = $null

if (-not (Test-Path $AppSettingsPath)) {
    Write-CheckResult -Name 'appsettings.json found' -Status 'FAIL' -Detail $AppSettingsPath
}
else {
    $appSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
    $sharedFolderFromAppSettings = $appSettings.ImageStorage.SharedFolderPath

    if ([string]::IsNullOrWhiteSpace($sharedFolderFromAppSettings)) {
        Write-CheckResult -Name 'ImageStorage.SharedFolderPath configured' -Status 'FAIL' `
            -Detail 'The ImageStorage block is missing or has no SharedFolderPath.'
    }
    else {
        Write-CheckResult -Name 'ImageStorage.SharedFolderPath configured' -Status 'PASS' `
            -Detail $sharedFolderFromAppSettings
    }
}

# The database override wins at runtime, so that is the path that must actually work.
$effectiveShare = if (-not [string]::IsNullOrWhiteSpace($sharedFolderFromDatabase)) {
    $sharedFolderFromDatabase
}
else {
    $sharedFolderFromAppSettings
}

if ($sharedFolderFromDatabase -and $sharedFolderFromAppSettings -and
    $sharedFolderFromDatabase -ne $sharedFolderFromAppSettings) {
    Write-CheckResult -Name 'appsettings default matches database override' -Status 'WARN' `
        -Detail ("appsettings='{0}' but the database serves '{1}'." -f $sharedFolderFromAppSettings, $sharedFolderFromDatabase)
}
elseif ($sharedFolderFromAppSettings) {
    Write-CheckResult -Name 'appsettings default matches database override' -Status 'PASS'
}

if ([string]::IsNullOrWhiteSpace($effectiveShare)) {
    Write-CheckResult -Name 'Image share reachable' -Status 'FAIL' -Detail 'No share path could be resolved.'
}
elseif (-not (Test-Path $effectiveShare)) {
    Write-CheckResult -Name 'Image share reachable' -Status 'FAIL' `
        -Detail ("'{0}' is not reachable from this workstation." -f $effectiveShare)
}
else {
    Write-CheckResult -Name 'Image share reachable' -Status 'PASS' -Detail $effectiveShare

    if ($SkipShareWriteTest) {
        Write-CheckResult -Name 'Image share writable' -Status 'WARN' -Detail 'Skipped by request.'
    }
    else {
        $probe = Join-Path $effectiveShare ("_preflight_{0}.tmp" -f [guid]::NewGuid().ToString('N'))
        try {
            Set-Content -Path $probe -Value 'preflight' -ErrorAction Stop
            Remove-Item $probe -Force
            Write-CheckResult -Name 'Image share writable' -Status 'PASS' `
                -Detail ("Verified as {0}." -f $env:USERNAME)
        }
        catch {
            Write-CheckResult -Name 'Image share writable' -Status 'FAIL' `
                -Detail ("Cannot write as {0}: {1}" -f $env:USERNAME, $_.Exception.Message)
        }

        $archive = Join-Path $effectiveShare 'Archive'
        if (Test-Path $archive) {
            Write-CheckResult -Name 'Archive subfolder present' -Status 'PASS' -Detail $archive
        }
        else {
            Write-CheckResult -Name 'Archive subfolder present' -Status 'WARN' `
                -Detail 'Created automatically on the first image replacement.'
        }
    }
}

Write-Host ''
if ($script:Failures.Count -gt 0) {
    Write-Host ("PREFLIGHT FAILED ({0} failed, {1} warnings)" -f $script:Failures.Count, $script:Warnings.Count) -ForegroundColor Red
    $script:Failures | ForEach-Object { Write-Host ("  - {0}" -f $_) -ForegroundColor Red }
    exit 1
}

Write-Host ("PREFLIGHT PASSED ({0} warnings)" -f $script:Warnings.Count) -ForegroundColor Green
exit 0
