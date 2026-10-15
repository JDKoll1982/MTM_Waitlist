<#
.SYNOPSIS
    Reports - and optionally repairs - a store whose deployed stored procedures have fallen behind the
    repository's own create.sql artifacts.

.DESCRIPTION
    The repository is the source of truth for every stored procedure: Database/StoredProcedures/<name>/create.sql
    is the artifact, and the create.sql files are self-dropping (DROP PROCEDURE IF EXISTS then CREATE), so
    re-running one is safe and idempotent.

    A deployed routine that has fallen behind is invisible at run time, and that is the reason this script
    exists. MySQL accepts a call that passes MORE parameters than the routine declares, silently ignores the
    surplus, and reports success - so a call site that passes a parameter the deployed routine does not have
    writes its other columns, reports a healthy result, and loses that one value with nothing logged, nothing
    thrown, and nothing on screen to suggest anything is missing.

    The report is by parameter list, which is the shape that class of drift changes. Body-only edits are not
    detected by comparison; pass -Apply to bring every routine in the schema up to the repository's version
    regardless, which is the only way to be sure (MySQL resolves table and column references when a routine
    runs, not when it is created, so applying a create.sql is safe even where a table has not moved yet).

.PARAMETER Database
    The schema to inspect and repair. Default: mtm_waitlist.

.PARAMETER ConnectionString
    A MySQL connection string for the target server. Default: $env:MTM_WAITLIST_DB_CONNECTION_STRING, which is
    what the application itself resolves.

.PARAMETER MysqlExe
    Full path to mysql.exe. Discovered from PATH, then the usual MySQL Server install locations, then MAMP.

.PARAMETER Apply
    Apply every create.sql in the schema, rather than only reporting. Without it the script changes nothing.

.PARAMETER Procedure
    Restrict an -Apply run to these routine names. Without it, every create.sql in the schema is applied.

.PARAMETER Seed
    Optional name (or names) of a Database/Seeds/<name>/create.sql to apply after the procedures. Seeds are
    data changes and are never applied implicitly, so each one has to be named.

.PARAMETER ReportPath
    Where to write the plain-text report. Default: <temp>\mtm-schema-drift-<database>.txt.

.EXAMPLE
    pwsh -File Database\CopilotScripts\refresh_schema_drift.ps1
    Reports every mtm_waitlist routine whose parameter list differs from the repository.

.EXAMPLE
    pwsh -File Database\CopilotScripts\refresh_schema_drift.ps1 -Apply -Seed seed_waitlist_request_item_configs
    Repairs the schema and then re-seeds the per-Item configuration table.
#>
[CmdletBinding()]
param(
    [string] $Database = 'mtm_waitlist',
    [string] $ConnectionString = $env:MTM_WAITLIST_DB_CONNECTION_STRING,
    [string] $MysqlExe = '',
    [switch] $Apply,
    [string[]] $Procedure = @(),
    [string[]] $Seed = @(),
    [string] $ReportPath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$proceduresRoot = Join-Path $repoRoot 'Database\StoredProcedures'
$seedsRoot = Join-Path $repoRoot 'Database\Seeds'

# A comma-separated list reaches this script as ONE argument under some invocation styles (pwsh -File), and as
# separate arguments under others, so both spellings have to mean the same thing.
function Expand-List {
    param([string[]] $Values)
    return @($Values | Where-Object { $_ } | ForEach-Object { $_ -split ',' } | Where-Object { $_.Trim() } | ForEach-Object { $_.Trim() })
}

$Procedure = Expand-List -Values $Procedure
$Seed = Expand-List -Values $Seed

if (-not $ReportPath) {
    $ReportPath = Join-Path $env:TEMP "mtm-schema-drift-$Database.txt"
}

function Write-Report {
    param([string] $Line = '')
    Write-Output $Line
    Add-Content -Path $ReportPath -Value $Line -Encoding utf8
}

function Resolve-MysqlExe {
    param([string] $Given)

    if ($Given) {
        if (-not (Test-Path $Given)) { throw "mysql.exe not found at '$Given'." }
        return $Given
    }

    $fromPath = Get-Command mysql.exe -ErrorAction SilentlyContinue
    if ($fromPath) { return $fromPath.Source }

    $candidates = @(
        'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe',
        'C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql.exe',
        'C:\Program Files (x86)\MySQL\MySQL Server 8.0\bin\mysql.exe',
        'C:\Program Files (x86)\MySQL\MySQL Server 5.7\bin\mysql.exe',
        'C:\MAMP\bin\mysql\bin\mysql.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    foreach ($root in @('C:\MAMP\bin\mysql', 'C:\wamp64\bin\mysql', 'C:\xampp\mysql')) {
        if (-not (Test-Path $root)) { continue }
        $found = Get-ChildItem $root -Recurse -Filter mysql.exe -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { return $found.FullName }
    }

    throw 'mysql.exe was not found. Pass -MysqlExe with its full path.'
}

function Split-ConnectionString {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw 'No connection string. Set MTM_WAITLIST_DB_CONNECTION_STRING or pass -ConnectionString.'
    }

    $settings = @{}
    foreach ($part in ($Value -split ';')) {
        $pair = $part.Split('=', 2)
        if ($pair.Count -ne 2) { continue }
        $settings[$pair[0].Trim().ToLowerInvariant()] = $pair[1].Trim()
    }

    foreach ($required in @('server', 'user id')) {
        if (-not $settings.ContainsKey($required)) { throw "The connection string has no '$required' entry." }
    }

    $port = if ($settings.ContainsKey('port')) { $settings['port'] } else { '3306' }
    $password = ''
    foreach ($key in @('password', 'pwd')) {
        if ($settings.ContainsKey($key)) { $password = $settings[$key]; break }
    }

    return @{
        Host = $settings['server']
        Port = $port
        User = $settings['user id']
        Password = $password
    }
}

function Invoke-Query {
    param([string] $Sql)

    $output = & $script:mysqlExe @script:clientArguments --batch --skip-column-names --execute=$Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Query failed (exit $LASTEXITCODE): $($output -join ' ')"
    }

    return @($output)
}

function Invoke-ScriptFile {
    param([string] $Path)

    $command = '"{0}" --defaults-extra-file="{1}" {2} < "{3}"' -f `
        $script:mysqlExe, $script:configPath, $script:databaseArguments, $Path
    $output = & $env:ComSpec /c $command 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Applying '$Path' failed (exit $LASTEXITCODE): $($output -join ' ')"
    }

    return @($output)
}

function Get-DeclaredSchema {
    param([string] $Path)

    $use = Select-String -Path $Path -Pattern '^\s*USE\s+([A-Za-z0-9_]+)\s*;' | Select-Object -First 1
    if (-not $use) { return '' }
    return $use.Matches[0].Groups[1].Value
}

function Get-DeclaredRoutine {
    param([string] $Path)

    $create = Select-String -Path $Path -Pattern '^\s*CREATE\s+PROCEDURE\s+([A-Za-z0-9_]+)\s*\(' | Select-Object -First 1
    if (-not $create) { return '' }
    return $create.Matches[0].Groups[1].Value
}

function Get-DeclaredParameters {
    param([string] $Path, [string] $Routine)

    $text = Get-Content $Path -Raw
    $start = $text.IndexOf("CREATE PROCEDURE $Routine", [StringComparison]::OrdinalIgnoreCase)
    if ($start -lt 0) { return @() }

    $signature = Get-SignatureText -Text $text -Start $start
    if ($signature -eq '') { return @() }

    $names = @()
    foreach ($match in [regex]::Matches($signature, '(?im)^\s*(?:IN|OUT|INOUT)?\s*`?([A-Za-z0-9_]+)`?\s+(?:CHAR|VARCHAR|VARBINARY|TINYINT|SMALLINT|MEDIUMINT|INT|BIGINT|DECIMAL|NUMERIC|FLOAT|DOUBLE|DATE|DATETIME|TIMESTAMP|TIME|JSON|TEXT|BLOB|ENUM|SET|BINARY|BIT|BOOL|BOOLEAN|LONGTEXT|MEDIUMTEXT|TINYTEXT)')) {
        $names += $match.Groups[1].Value
    }

    return $names
}

# The signature's closing parenthesis is the one that balances the opening one, not the first ')' after it: a
# parameter declared CHAR(36) or DECIMAL(14,4) closes a parenthesis of its own first.
function Get-SignatureBounds {
    param([string] $Text, [int] $Start)

    $open = $Text.IndexOf('(', $Start)
    if ($open -lt 0) { return $null }

    $depth = 0
    for ($index = $open; $index -lt $Text.Length; $index++) {
        if ($Text[$index] -eq '(') { $depth++ }
        elseif ($Text[$index] -eq ')') {
            $depth--
            if ($depth -eq 0) { return @{ Open = $open; Close = $index } }
        }
    }

    return $null
}

function Get-SignatureText {
    param([string] $Text, [int] $Start)

    $bounds = Get-SignatureBounds -Text $Text -Start $Start
    if ($null -eq $bounds) { return '' }

    return $Text.Substring($bounds.Open + 1, $bounds.Close - $bounds.Open - 1)
}


$settings = Split-ConnectionString -Value $ConnectionString

# The password never reaches a command line: mysql.exe reads it from a client option file, which is written
# outside the repository and removed in the finally block.
$configPath = Join-Path $env:TEMP ("mtm-schema-drift-{0}.cnf" -f ([guid]::NewGuid().ToString('n').Substring(0, 8)))
@(
    '[client]'
    "host=$($settings.Host)"
    "port=$($settings.Port)"
    "user=$($settings.User)"
    "password=$($settings.Password)"
    'default-character-set=utf8mb4'
) | Set-Content -Path $configPath -Encoding ascii

$mysqlExe = Resolve-MysqlExe -Given $MysqlExe

# The schema goes on the client too, not only in the SQL: an unqualified statement - SHOW CREATE PROCEDURE, for
# instance - fails with "No database selected" without it, and a qualified one cannot then be run against the
# wrong schema by accident.
$clientArguments = @("--defaults-extra-file=$configPath", "--database=$Database", "--raw")
$databaseArguments = $Database

Set-Content -Path $ReportPath -Value "Schema drift report for '$Database' on $($settings.Host):$($settings.Port)" -Encoding utf8
Write-Report "Repository: $repoRoot"
Write-Report "mysql.exe:  $mysqlExe"
Write-Report "Mode:       $(if ($Apply) { 'APPLY' } else { 'report only' })"
Write-Report ''

try {
    Write-Report '--- stored procedures ---'

    $artifacts = @()
    foreach ($folder in Get-ChildItem $proceduresRoot -Directory | Sort-Object Name) {
        $createPath = Join-Path $folder.FullName 'create.sql'
        if (-not (Test-Path $createPath)) { continue }

        $schema = Get-DeclaredSchema -Path $createPath
        if ($schema -ne $Database) { continue }

        $routine = Get-DeclaredRoutine -Path $createPath
        if (-not $routine) { continue }

        $artifacts += [pscustomobject]@{
            Name       = $routine
            Path       = $createPath
            Parameters = @(Get-DeclaredParameters -Path $createPath -Routine $routine)
        }
    }
    $missing = @()
    $drifted = @()

    foreach ($artifact in $artifacts) {
        $exists = (Invoke-Query -Sql "SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA='$Database' AND ROUTINE_NAME='$($artifact.Name)'")[0]
        if ($exists -eq '0') {
            Write-Report ("MISSING  {0}" -f $artifact.Name)
            $missing += $artifact
            continue
        }

        $deployed = Invoke-Query -Sql "SELECT PARAMETER_NAME FROM information_schema.PARAMETERS WHERE SPECIFIC_SCHEMA='$Database' AND SPECIFIC_NAME='$($artifact.Name)' AND PARAMETER_MODE IS NOT NULL ORDER BY ORDINAL_POSITION"

        $absent = @($artifact.Parameters | Where-Object { $deployed -notcontains $_ })
        $extra = @($deployed | Where-Object { $artifact.Parameters -notcontains $_ })

        if ($absent.Count -gt 0) {
            Write-Report ("DRIFT    {0}: deployed routine is missing {1} parameter(s): {2}" -f $artifact.Name, $absent.Count, ($absent -join ', '))
            $drifted += $artifact
            continue
        }

        if ($extra.Count -gt 0) {
            Write-Report ("AHEAD    {0}: deployed routine declares {1} parameter(s) the repository does not: {2}" -f $artifact.Name, $extra.Count, ($extra -join ', '))
        }
    }

    Write-Report ''
    Write-Report ("Procedures in schema: {0}. Missing: {1}. Parameter drift: {2}." -f $artifacts.Count, $missing.Count, $drifted.Count)
    Write-Report ''
    Write-Report 'A body that was changed without its parameters changing is NOT reported by the list above: it cannot'
    Write-Report 'be told from formatting, so it is not claimed. `-Apply` is the remedy - it rewrites every routine in'
    Write-Report 'the schema from the repository, which is the only way to be sure a body matches its artifact.'

    if ($Apply) {
        $toApply = if ($Procedure.Count -gt 0) {
            @($artifacts | Where-Object { $Procedure -contains $_.Name })
        }
        else {
            $artifacts
        }

        if ($Procedure.Count -gt 0 -and $toApply.Count -ne $Procedure.Count) {
            $known = @($toApply | ForEach-Object { $_.Name })
            $unknown = @($Procedure | Where-Object { $known -notcontains $_ })
            throw ("No create.sql artifact in '$Database' for: {0}." -f ($unknown -join ', '))
        }

        Write-Report ''
        Write-Report '--- applying ---'
        foreach ($artifact in $toApply) {
            Invoke-ScriptFile -Path $artifact.Path | Out-Null
            Write-Report ("APPLIED  {0}" -f $artifact.Name)
        }

        Write-Report ("Applied {0} create.sql artifact(s)." -f $toApply.Count)
    }

    foreach ($seedName in $Seed) {
        $seedPath = Join-Path (Join-Path $seedsRoot $seedName) 'create.sql'
        if (-not (Test-Path $seedPath)) { throw "Seed '$seedName' has no create.sql at '$seedPath'." }

        $schema = Get-DeclaredSchema -Path $seedPath
        if ($schema -ne $Database) { throw "Seed '$seedName' declares schema '$schema', not '$Database'." }

        if (-not $Apply) {
            Write-Report "SEED     $seedName would be applied (pass -Apply to run it)."
            continue
        }

        Write-Report "SEED     applying $seedName ..."
        Invoke-ScriptFile -Path $seedPath | Out-Null
        Write-Report "SEED     $seedName applied."
    }
}
finally {
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
}

Write-Report ''
Write-Report "Report: $ReportPath"
