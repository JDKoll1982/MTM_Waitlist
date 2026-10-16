<#
.SYNOPSIS
    Re-captures the mtm_mock cold-cache baseline seed from a live mirror.

.DESCRIPTION
    Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql is a CAPTURE, not hand-written
    content: a fresh install has never been refreshed, so without baseline rows the very first
    fallback read would serve an empty set for every journey (FR-017). A capture goes stale
    whenever the mirror's own refreshes change shape. The committed 2026-09-12 capture is a
    worked example: it still carries 79,218 shape-4 rows that are overwhelmingly zero-stock,
    because the population read was afterwards narrowed to locations holding stock (T150/T151),
    so the file no longer describes what the mirror actually produces.

    This script regenerates that file from the mirror as it stands now.

    READ-ONLY against the database. It never writes to mtm_mock, and it never touches
    mtm_waitlist, mtm_wip_application_winforms or mtm_receiving_application. The only file it
    writes is the seed create.sql (plus AllSeeds.sql, which is generated from it).

    Per shape it:

      1. reads the deployed column list from information_schema, so the capture follows the
         schema rather than a hard-coded list;
      2. lets MySQL quote every value, so an apostrophe, a tab or a newline inside a description
         round-trips into the file instead of breaking the statement;
      3. stamps `is_seed_content = 1` on every row. A copied live row is NOT a refresh, and that
         flag is what lets the read-status surface say "seeded content, not yet refreshed"
         instead of reporting a fabricated cache age for the machine (FR-022);
      4. verifies that the row count it read equals the live COUNT(*), and refuses to write if
         they disagree. A truncated read must never be allowed to overwrite a good seed.

    Not regenerated: `rollback.sql`. It deletes by `is_seed_content = 1` for every shape, so it
    already removes whatever this script writes.

.PARAMETER Server
    MySQL host holding the mirror. Default: the server named by
    MTM_WAITLIST_DB_CONNECTION_STRING when that is set, otherwise localhost.

.PARAMETER Port
    MySQL port. Default: 3306

.PARAMETER User
    MySQL user. Default: the user named by MTM_WAITLIST_DB_CONNECTION_STRING when set, otherwise root.

.PARAMETER Password
    MySQL password. Prefer the MYSQL_PWD environment variable over this parameter so the
    credential never appears in a process command line.

.PARAMETER Database
    Mirror database name. Default: mtm_mock

.PARAMETER MysqlExe
    Path to mysql.exe. Default: resolved from PATH, else from the MySQL install.

.PARAMETER RowsPerStatement
    Tuples per INSERT statement. Default: 253, which is what the committed file uses.

.PARAMETER OutPath
    Output file. Default: Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql

.PARAMETER IncludeEmployeeShape
    Capture shape 6 (employee_by_number) as well. Off by default: that shape carries real
    employee numbers and names, and the locked schema ruleset says raw production user data must
    not move into non-production seed content.

.PARAMETER AllowEmptyShape
    Permit a shape the mirror holds zero rows for. Off by default, because an empty shape almost
    always means the wrong database or a mirror that has never refreshed, and writing it would
    delete good baseline rows from the seed.

.PARAMETER DryRun
    Report what would be captured, then exit without writing anything.

.PARAMETER SkipMasterRebuild
    Do not regenerate Database/Mock/AllSeeds.sql afterwards. AllSeeds.sql concatenates this file
    verbatim, so it is stale the moment this script writes; only skip when regenerating by hand.

.EXAMPLE
    pwsh -NoProfile -File Database/CopilotScripts/capture_mock_seed_baseline.ps1 -DryRun

.EXAMPLE
    $env:MYSQL_PWD='root'; pwsh -NoProfile -File Database/CopilotScripts/capture_mock_seed_baseline.ps1
#>
[CmdletBinding()]
param(
    [string]$Server,
    [int]$Port = 3306,
    [string]$User,
    [string]$Password,
    [string]$Database = 'mtm_mock',
    [string]$MysqlExe,
    [int]$RowsPerStatement = 253,
    [string]$OutPath,
    [switch]$IncludeEmployeeShape,
    [switch]$AllowEmptyShape,
    [switch]$DryRun,
    [switch]$SkipMasterRebuild
)

$ErrorActionPreference = 'Stop'

# This script lives in <repo>/Database/CopilotScripts.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$mockRoot = Join-Path $repoRoot 'Database/Mock'

if (-not $OutPath) {
    $OutPath = Join-Path $mockRoot 'Seeds/seed_visual_mirror_baseline/create.sql'
}

# The MySQL client escapes special characters in batch output, but QUOTE() inside the query has
# already produced the literal -- escaping it a second time would corrupt the value. --raw turns
# the client's escaping off, and the record separator below is what splits rows instead of a
# newline, so a newline inside a description cannot split a row in half.
$rs = [char]30
$bt = [char]96
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$numericTypes = @(
    'tinyint', 'smallint', 'mediumint', 'int', 'integer', 'bigint',
    'decimal', 'numeric', 'float', 'double', 'real', 'bit', 'year'
)

# Shape key -> mirror table. The keys are the seven shape keys the freshness report and
# read_shape_metadata derive from the table names, so they are not free text.
$shapeDefs = @(
    [pscustomobject]@{ Number = 1; Key = 'work_order_lookup';         Table = 'visual_work_order_lookup_result' }
    [pscustomobject]@{ Number = 2; Key = 'operation_sequences';       Table = 'visual_operation_sequences_result' }
    [pscustomobject]@{ Number = 3; Key = 'subordinate_parts';         Table = 'visual_subordinate_parts_result' }
    [pscustomobject]@{ Number = 4; Key = 'inventory_locations';       Table = 'visual_inventory_locations_result' }
    [pscustomobject]@{ Number = 5; Key = 'disposition_input';         Table = 'visual_disposition_input_result' }
    [pscustomobject]@{ Number = 6; Key = 'employee_by_number';        Table = 'visual_employee_by_number_result' }
    [pscustomobject]@{ Number = 7; Key = 'part_warehouse_quantities'; Table = 'visual_part_warehouse_quantities_result' }
)

# ---------------------------------------------------------------- connection

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

# The application's own configuration is the source of truth for where the mirror is, so follow it
# rather than assuming localhost: a workstation that reads a remote mirror must capture that mirror.
if (-not $Server -or -not $User -or -not $Password) {
    $cs = $env:MTM_WAITLIST_DB_CONNECTION_STRING
    if ($cs) {
        foreach ($token in ($cs -split ';')) {
            $kv = $token -split '=', 2
            if ($kv.Count -ne 2) { continue }
            switch ($kv[0].Trim().ToLowerInvariant()) {
                'server'   { if (-not $Server) { $Server = $kv[1].Trim() } }
                'data source' { if (-not $Server) { $Server = $kv[1].Trim() } }
                'uid'      { if (-not $User) { $User = $kv[1].Trim() } }
                'user'     { if (-not $User) { $User = $kv[1].Trim() } }
                'user id'  { if (-not $User) { $User = $kv[1].Trim() } }
                'password' { if (-not $Password) { $Password = $kv[1].Trim() } }
                'pwd'      { if (-not $Password) { $Password = $kv[1].Trim() } }
            }
        }
    }
}

if (-not $Server) { $Server = 'localhost' }
if (-not $User) { $User = 'root' }
if ($Password) { $env:MYSQL_PWD = $Password }
if (-not $env:MYSQL_PWD) {
    Write-Warning 'MYSQL_PWD is not set; the client may prompt for a password.'
}

$baseArgs = @(
    '-h', $Server
    '-P', "$Port"
    '-u', $User
    '--default-character-set=utf8mb4'
)

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("mtm-mock-capture-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

# ---------------------------------------------------------------- helpers

function Invoke-MySqlScalar {
    param([string]$Sql)

    $out = & $MysqlExe @baseArgs '--batch' '--skip-column-names' '--raw' '-e' $Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "mysql failed (exit $LASTEXITCODE):`n$($out -join [Environment]::NewLine)"
    }
    # The leading comma keeps a single-line answer an array, so the caller's [0] is the value and
    # not the value's first character.
    return , @($out | ForEach-Object { "$_" })
}

# Streams the generated tuples straight to a file: the client's stdout handle is redirected, so no
# PowerShell string decoding sits between MySQL and the capture, and the bytes are exactly what the
# server sent.
function Invoke-MySqlToFile {
    param([string]$Sql, [string]$Tag)

    $sqlFile = Join-Path $tempRoot "$Tag.sql"
    $outFile = Join-Path $tempRoot "$Tag.out"
    $errFile = Join-Path $tempRoot "$Tag.err"

    [System.IO.File]::WriteAllText($sqlFile, $Sql, $utf8NoBom)

    $proc = Start-Process -FilePath $MysqlExe -ArgumentList ($baseArgs + @('--batch', '--skip-column-names', '--raw')) `
        -RedirectStandardInput $sqlFile -RedirectStandardOutput $outFile -RedirectStandardError $errFile `
        -NoNewWindow -Wait -PassThru

    $err = if (Test-Path $errFile) { [System.IO.File]::ReadAllText($errFile).Trim() } else { '' }
    if ($proc.ExitCode -ne 0) {
        throw "mysql failed while reading '$Tag' (exit $($proc.ExitCode)):`n$err"
    }
    if ($err) { Write-Warning "mysql wrote to stderr while reading '$Tag': $err" }

    return $outFile
}

# What the committed file currently holds per table, so the report can say what changed. A section
# that names the table but carries no tuple lines is an `INSERT ... SELECT` derivation, not an empty
# section, and the report has to tell those two apart.
function Get-CommittedSections {
    param([string]$Path)

    $sections = @{}
    if (-not (Test-Path $Path)) { return $sections }

    $current = $null
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        if ($line -match ('^INSERT\s+IGNORE\s+INTO\s+' + $bt + '([^' + $bt + ']+)' + $bt)) {
            $current = $Matches[1]
            if (-not $sections.ContainsKey($current)) {
                $sections[$current] = [pscustomobject]@{ Rows = 0; Derived = $true }
            }
            continue
        }
        # Tuple lines start with the numeric id column. The explicit column list on the line above
        # `VALUES` also starts with a bracket, but with an identifier, so it is not counted.
        if ($current -and $line -match '^\s+\([0-9]') {
            $sections[$current].Rows++
            $sections[$current].Derived = $false
        }
    }
    return $sections
}

function Get-ColumnsOf {
    param([string]$Table)

    $sql = "SELECT column_name, data_type FROM information_schema.columns " +
           "WHERE table_schema = '$Database' AND table_name = '$Table' ORDER BY ordinal_position;"
    $rows = Invoke-MySqlScalar -Sql $sql
    if (-not $rows -or $rows.Count -eq 0) {
        throw "Table `$Database.$Table` does not exist on $Server. Wrong server, or the mirror has not been deployed there."
    }

    $cols = foreach ($row in $rows) {
        $parts = "$row" -split "`t"
        if ($parts.Count -ge 2) {
            [pscustomobject]@{ Name = $parts[0]; Type = $parts[1].ToLowerInvariant() }
        }
    }
    return @($cols)
}

function Get-TupleExpression {
    param([pscustomobject]$Column, [string]$Stamp)

    $ident = "$bt$($Column.Name)$bt"

    # `is_seed_content` and `refreshed_utc` are not copied from the mirror on purpose: the rows are
    # being written as seed content, so the flag says 1 and the stamp is the capture time.
    #
    # The stamp goes through QUOTE() rather than a bare SQL literal, because a literal's quotes are
    # consumed by the parser and the output would carry a bare `2026-10-16 12:43:32` that the client
    # then rejects. QUOTE() puts the quotes in the emitted text, which is what the tuple needs.
    if ($Column.Name -eq 'is_seed_content') { return '1' }
    if ($Column.Name -eq 'refreshed_utc') { return "QUOTE('$Stamp')" }

    if ($numericTypes -contains $Column.Type) {
        # CONCAT returns NULL if ANY argument is NULL, which would silently drop the whole row, so
        # every column is wrapped.
        return "IFNULL(CAST($ident AS CHAR),'NULL')"
    }
    return "IFNULL(QUOTE($ident),'NULL')"
}

# ---------------------------------------------------------------- capture

$captureTime = Get-Date
# The column is `refreshed_utc`, and the service's own refreshes write UTC into it, so the capture
# stamps UTC too. A local-time stamp here would sit five hours off the values the mirror itself holds.
$stamp = $captureTime.ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss')
$captureDate = $captureTime.ToString('yyyy-MM-dd')

Write-Host "Capturing the mtm_mock baseline from ${Server}:${Port} as $User" -ForegroundColor Cyan
Write-Host "  output : $OutPath"
Write-Host ''

$counts = Invoke-MySqlScalar -Sql "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$Database' AND table_name LIKE 'visual_%';"
if ([int]$counts[0] -eq 0) {
    throw "No visual_* tables in `$Database` on $Server -- this does not look like the mtm_mock mirror."
}

$committed = Get-CommittedSections -Path $OutPath

$captured = @()
$liveFreshness = ''

foreach ($shape in $shapeDefs) {
    $isEmployee = $shape.Key -eq 'employee_by_number'

    if ($isEmployee -and -not $IncludeEmployeeShape) {
        $captured += [pscustomobject]@{
            Shape = $shape; Rows = 0; Skipped = $true; Freshness = ''
            Body = $null; Columns = $null
        }
        continue
    }

    $liveCount = [int](Invoke-MySqlScalar -Sql "SELECT COUNT(*) FROM $bt$Database$bt.$bt$($shape.Table)$bt;")[0]
    if ($liveCount -eq 0 -and -not $AllowEmptyShape) {
        throw ("Shape $($shape.Number) ($($shape.Key)) holds 0 rows in `$Database` on $Server. " +
               "An empty shape nearly always means the wrong database or a mirror that has never refreshed, and " +
               "writing it would strip the good baseline rows from the seed. Pass -AllowEmptyShape if the emptiness " +
               "is genuinely correct.")
    }

    $freshnessRows = Invoke-MySqlScalar -Sql "SELECT IFNULL(MAX(refreshed_utc),'-') FROM $bt$Database$bt.$bt$($shape.Table)$bt;"
    $freshness = if ($freshnessRows -and $freshnessRows.Count -gt 0) { "$($freshnessRows[0])" } else { '-' }
    if ($freshness -ne '-' -and ($liveFreshness -eq '' -or $freshness -gt $liveFreshness)) { $liveFreshness = $freshness }

    $columns = Get-ColumnsOf -Table $shape.Table

    # The identifier on the column-list line must match, so a column that no longer exists fails
    # loudly here rather than producing a file the client rejects later.
    $expressions = @("'('")
    for ($i = 0; $i -lt $columns.Count; $i++) {
        if ($i -gt 0) { $expressions += "','" }
        $expressions += (Get-TupleExpression -Column $columns[$i] -Stamp $stamp)
    }
    $expressions += "')'"
    $expressions += "CHAR($([int]$rs))"

    $orderBy = if ($columns.Name -contains 'id') { " ORDER BY ${bt}id${bt}" } else { '' }
    $selectSql = "SELECT CONCAT(" + ($expressions -join ', ') + ") FROM $bt$Database$bt.$bt$($shape.Table)$bt$orderBy;"

    Write-Host ("  shape {0} {1,-26} {2,8} rows" -f $shape.Number, $shape.Key, $liveCount) -ForegroundColor DarkGray

    $outFile = Invoke-MySqlToFile -Sql $selectSql -Tag $shape.Key
    $text = [System.IO.File]::ReadAllText($outFile, [System.Text.Encoding]::UTF8)

    # The client writes the row terminator AFTER each record, so the newline is the leading character
    # of the next chunk, not the trailing one of this one: trim both ends or every tuple but the first
    # starts at column 0. A tuple always runs from its opening bracket to its closing one, so trimming
    # outside those cannot touch a value.
    $tuples = foreach ($chunk in ($text -split $rs)) {
        $trimmed = $chunk.Trim()
        if (-not $trimmed) { continue }
        if (-not $trimmed.StartsWith('(')) {
            throw ("Shape $($shape.Number) ($($shape.Key)): a captured record does not begin with a bracket, " +
                   "so the stream is not the shape this script expects. Refusing to write a malformed seed.")
        }
        $trimmed
    }
    $tuples = @($tuples)

    if ($tuples.Count -ne $liveCount) {
        throw ("Shape $($shape.Number) ($($shape.Key)): read $($tuples.Count) rows but the mirror holds $liveCount. " +
               "Refusing to write a truncated capture over a good seed.")
    }

    $captured += [pscustomobject]@{
        Shape = $shape; Rows = $tuples.Count; Skipped = $false; Freshness = $freshness
        Body = $tuples; Columns = $columns
    }
}

# ---------------------------------------------------------------- report

Write-Host ''
Write-Host '  shape                     committed     live' -ForegroundColor Cyan
$totalNew = 0
$wasTotal = 0
foreach ($item in $captured) {
    $table = $item.Shape.Table
    if (-not $committed.ContainsKey($table)) {
        $was = '-'
    }
    elseif ($committed[$table].Derived) {
        $was = 'derived'
    }
    else {
        $was = "$($committed[$table].Rows)"
        $wasTotal += $committed[$table].Rows
    }
    $now = if ($item.Skipped) { 'excluded' } else { "$($item.Rows)" }
    if (-not $item.Skipped) { $totalNew += $item.Rows }
    Write-Host ("  {0,-24}  {1,9}  {2,9}" -f $item.Shape.Key, $was, $now)
}
Write-Host ("  {0,-24}  {1,9}  {2,9}" -f 'TOTAL', $wasTotal, $totalNew) -ForegroundColor Cyan

if ($DryRun) {
    Write-Host ''
    Write-Host '-DryRun: nothing written.' -ForegroundColor Yellow
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    return
}

# ---------------------------------------------------------------- compose

$lines = New-Object System.Collections.Generic.List[string]

$covered = $captured | Where-Object { -not $_.Skipped }
$excluded = $captured | Where-Object { $_.Skipped }

$lines.Add('-- ============================================================')
$lines.Add('-- Seed: seed_visual_mirror_baseline')
$lines.Add("-- Database: $Database")
$lines.Add('-- Engine:   MySQL 5.7 (verified on 9.6)')
$lines.Add('-- Kind:     Seed / create')
$lines.Add('-- Created:  2026-09-09')
$lines.Add("-- Updated:  $captureDate")
$lines.Add('-- Feature:  001-module-mock-visual-fallback (task T026, FR-017)')
$lines.Add('-- ============================================================')
$lines.Add('--')
$lines.Add('-- GENERATED FILE -- do not hand-edit the row sections.')
$lines.Add('-- Regenerate from a mirror with:')
$lines.Add('--   pwsh -NoProfile -File Database/CopilotScripts/capture_mock_seed_baseline.ps1')
$lines.Add('--')
$lines.Add('-- Purpose')
$lines.Add('-- -------')
$lines.Add('-- A fresh `mtm_mock` has never been refreshed. Without baseline rows the very first fallback')
$lines.Add('-- read (before the service''s first scheduled refresh) would return an empty set for every')
$lines.Add('-- journey. These rows make a fresh install serve a usable, coherent result instead (FR-017).')
$lines.Add('--')
$lines.Add('-- Content')
$lines.Add('-- -------')
$lines.Add("-- Captured $stamp UTC from the live ``$Database`` mirror on $Server, so a fresh install")
$lines.Add('-- starts from real Infor Visual reads rather than hand-written samples.')
if ($liveFreshness -ne '') {
    $lines.Add("-- The mirror's own most recent refresh was $liveFreshness UTC, so this is that refresh's content.")
}
$lines.Add('--')
$coveredList = @($covered | Where-Object { $_.Rows -gt 0 })
$coveredTotal = 0
foreach ($item in $coveredList) {
    $coveredTotal += $item.Rows
    $lines.Add('--     ' + $item.Shape.Key.PadRight(26) + ("{0,8}" -f $item.Rows))
}
$lines.Add('--     ' + 'TOTAL'.PadRight(26) + ("{0,8}" -f $coveredTotal))
$lines.Add('--')

if ($excluded.Count -gt 0) {
    $noteIndent = '--' + (' ' * 34)
    $lines.Add('--     ' + $excluded[0].Shape.Key.PadRight(24) + '-    NOT covered. The capture can read this shape, but it')
    $lines.Add($noteIndent + 'carries real employee numbers and names, and the locked schema')
    $lines.Add($noteIndent + 'ruleset keeps raw production user data out of non-production seed')
    $lines.Add($noteIndent + 'content. Pass -IncludeEmployeeShape to capture it anyway; until then')
    $lines.Add($noteIndent + 'a fresh install serves an empty set for an employee-by-number')
    $lines.Add($noteIndent + 'fallback read.')
    $lines.Add('--')
}

$lines.Add('-- Every row carries `is_seed_content = 1`')
$lines.Add('-- --------------------------------------')
$lines.Add('-- The live rows were copied with `is_seed_content = 0`, because they ARE refreshed content.')
$lines.Add('-- They are written here as `1` on purpose: a copied row is not a refresh, and the flag is what')
$lines.Add('-- lets the read-status surface say "seeded content, not yet refreshed" instead of reporting a')
$lines.Add('-- fabricated cache age for this machine (FR-022, data-model.md 3). It is also what makes')
$lines.Add('-- `rollback.sql` able to remove the seed without disturbing a warm cache.')
$lines.Add('--')
$lines.Add('-- Re-runnable')
$lines.Add('-- -----------')
$lines.Add('-- `INSERT IGNORE` respects each mirror''s primary and unique keys, so re-running this seed')
$lines.Add('-- neither duplicates rows nor overwrites real refreshed content. Refreshes always write')
$lines.Add('-- `is_seed_content = 0` and wholesale-replace the live table, so seed rows disappear on the first')
$lines.Add('-- successful refresh.')
$lines.Add('-- ============================================================')
$lines.Add('')
$lines.Add("USE $Database;")
$lines.Add('')
$lines.Add('SET NAMES utf8mb4;')
$lines.Add('')

foreach ($item in $covered) {
    $shape = $item.Shape
    $table = $shape.Table

    $lines.Add("-- Shape $($shape.Number): $($shape.Key)")
    $lines.Add("-- $($item.Rows) rows captured from the live mirror on $captureDate.")
    if ($item.Freshness -ne '-') { $lines.Add("-- Live mirror last refreshed $($item.Freshness) UTC.") }

    if ($shape.Key -eq 'part_warehouse_quantities') {
        $lines.Add('-- This shape is copied from the mirror like the others. It used to be derived from the')
        $lines.Add('-- inventory_locations section above, because the committed capture predated the shape and the')
        $lines.Add('-- derivation was the only way to keep the file internally consistent; that derivation could')
        $lines.Add('-- not compute the finished-goods split and wrote 0 for it. The mirror now holds the shape')
        $lines.Add('-- directly, so the capture carries the real figures instead.')
    }

    if ($item.Rows -eq 0) {
        # `-AllowEmptyShape` was given and the mirror really is empty: emit the statement with no
        # tuples rather than an invalid `VALUES ;`.
        $lines.Add('-- The mirror currently holds no rows for this shape; the statement below is a no-op.')
        $lines.Add("-- SELECT '' FROM $bt$table$bt WHERE 1 = 0;")
        $lines.Add('')
        continue
    }

    $columnList = ($item.Columns | ForEach-Object { "$bt$($_.Name)$bt" }) -join ', '

    $lines.Add("INSERT IGNORE INTO $bt$table$bt")
    $lines.Add("    ($columnList)")
    $lines.Add('VALUES')

    for ($i = 0; $i -lt $item.Body.Count; $i++) {
        $suffix = if ($i -eq $item.Body.Count - 1) { ';' } else { ',' }
        $lines.Add('    ' + $item.Body[$i] + $suffix)
    }
    $lines.Add('')
}

$content = ($lines -join "`r`n") + "`r`n"

# Write through a temporary file beside the target so an interrupted run cannot leave a half-written
# seed where the deploy reads it from.
$targetDir = Split-Path -Parent $OutPath
if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
$staging = Join-Path $targetDir ('.capture-' + [Guid]::NewGuid().ToString('N') + '.tmp')
[System.IO.File]::WriteAllText($staging, $content, $utf8NoBom)
Move-Item -LiteralPath $staging -Destination $OutPath -Force

$lineCount = ($content -split "`r`n").Count
Write-Host ''
Write-Host "Wrote $OutPath ($lineCount lines, $totalNew rows)" -ForegroundColor Green

Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue

# AllSeeds.sql concatenates this file verbatim, so it is stale the moment the seed changes. The
# generator refuses to run under Windows PowerShell 5.1, so it is invoked through pwsh.
if (-not $SkipMasterRebuild) {
    $masters = Join-Path $PSScriptRoot 'build_mtm_mock_masters.ps1'
    $pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
    if (-not $pwsh) {
        Write-Warning "pwsh not found; regenerate the master lists by hand with: pwsh -NoProfile -File $masters"
    }
    else {
        Write-Host 'Regenerating Database/Mock/AllSeeds.sql' -ForegroundColor Cyan
        & $pwsh -NoProfile -File $masters
        if ($LASTEXITCODE -ne 0) {
            throw "build_mtm_mock_masters.ps1 failed (exit $LASTEXITCODE). AllSeeds.sql is now stale."
        }
        Write-Host 'AllSeeds.sql regenerated.' -ForegroundColor Green
    }
}
