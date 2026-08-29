<#
.SYNOPSIS
Scans the entire MTM_Waitlist repo for any remaining "workstation" / "Work Station"
occurrences and produces an edit-map table (file, line, category, matched text).

.DESCRIPTION
Drives the Workstation -> Computer / Work Center rename. For each match it classifies
the occurrence as "Computer", "Work Center", or "Review" (ambiguous), and writes a
Markdown table to a results file and the console.

Out of scope / excluded (generated, binary, SCM, or log):
  - Directories: bin, obj, .git, .vs, node_modules, TestResults, packages, .serena, pri_dump
  - Binary extensions: png, jpg, jpeg, gif, webp, ico, dll, exe, pdb, pri, dgspec,
    snupkg, nupkg, db, bdb, dat, ttf, otf
  - Log files: *.log, Log.md, testout.txt, testerr.txt
  - PRI dump: pri_dump.xml (and the pri_dump/ directory is already excluded above)
  - Tool self: scan_workstation_rename.ps1 and Rename_Scan_Results.md (avoid feedback loop)

.EXAMPLE
.\tools\scan_workstation_rename.ps1
.\tools\scan_workstation_rename.ps1 -OutFile .\Documents\Rename_Scan_Results.md
#>

param(
    [string]$Root = '',
    [string]$OutFile = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = if ($Root) { $Root } else { (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
if (-not (Test-Path $repoRoot)) { throw "Repo root not found: $repoRoot" }

if (-not $OutFile) { $OutFile = Join-Path $repoRoot 'Documents\Rename_Scan_Results.md' }

$excludeDirs   = @('bin', 'obj', '.git', '.vs', 'node_modules', 'TestResults', 'packages', '.serena', 'pri_dump')
$excludePaths  = @('Documents\Development\InforVisual')
$binaryExts    = @('.png', '.jpg', '.jpeg', '.gif', '.webp', '.ico', '.dll', '.exe', '.pdb', '.pri', '.dgspec', '.snupkg', '.nupkg', '.db', '.bdb', '.dat', '.ttf', '.otf')
$logExts       = @('.log')
$excludeFileNames = @('Log.md', 'testout.txt', 'testerr.txt', 'pri_dump.xml', 'scan_workstation_rename.ps1', 'Rename_Scan_Results.md')
$pattern       = 'workstation|work station'   # -match is case-insensitive
$lineTruncate  = 90

function Test-Excluded([string]$fullPath) {
    $parts = $fullPath.Split([IO.Path]::DirectorySeparatorChar)
    foreach ($p in $parts) { if ($excludeDirs -contains $p) { return $true } }
    $rel = $fullPath.Substring($repoRoot.Length).TrimStart('\', '/')
    foreach ($xp in $excludePaths) {
        if ($rel -eq $xp -or $rel.StartsWith($xp + '\') -or $rel.StartsWith($xp + '/')) { return $true }
    }
    return $false
}

function Get-Category([string]$line, [string]$path) {
    $l = $line.ToLowerInvariant()
    $p = $path.ToLowerInvariant()

    # Strong path-level work-center context (these files are the work-center catalog / module).
    if ($p -match 'sp_setup_workstations|setup_workstations_catalog|setupworkstationpage|setupworkstationviewmodel|setupworkstationservice|setupworkflow|vw_setup_workstations|fn_setup_workstation|seed_setup_workstations|hot_workcenters') {
        return 'Work Center'
    }
    # Strong path-level computer context (registry / startup / session / settings scoped to a machine).
    if ($p -match 'core_workstations_registry|auth_sessions_tokens|config_settings_values|config_settings_history|startupsession|startupcoordinator|startupstate') {
        return 'Computer'
    }

    # Line-level hints as a fallback.
    $workCenterHints = @(
        'setup_workstations_catalog', 'sp_setup_workstations_', 'fn_setup_workstation_name_normalized',
        'vw_setup_workstations_', 'setupworkstation', 'setup_workstation_id', 'setup_workstations',
        'hot_workcenters', 'work_center', 'setup workstation', 'waitlist_requests_queue'
    )
    $computerHints = @(
        'core_workstations_registry', 'core_workstation_id', 'auth_sessions_tokens',
        'config_settings_values', 'config_settings_history', 'startupsession', 'startup',
        'computer'
    )
    foreach ($h in $workCenterHints) { if ($l -like "*$h*") { return 'Work Center' } }
    foreach ($h in $computerHints)   { if ($l -like "*$h*") { return 'Computer' } }

    return 'Review'
}

function Get-RenameSuggestion([string]$name, [string]$category) {
    # PascalCase names (e.g. SetupWorkstationService) -> PascalCase replacement (WorkCenter / Computer).
    # snake_case / kebab-case names -> snake_case replacement (work_center / computer).
    $replacement = if ($category -eq 'Computer') { 'computer' } else { 'work_center' }
    if ($name -match '[A-Z]' -and $name -notmatch '_' -and $name -notmatch '-') {
        $replacement = if ($category -eq 'Computer') { 'Computer' } else { 'WorkCenter' }
    }
    return [regex]::Replace($name, 'workstation|work station', $replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

$files = Get-ChildItem -Path $repoRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        -not (Test-Excluded $_.FullName) -and
        -not ($binaryExts -contains $_.Extension.ToLowerInvariant()) -and
        -not ($logExts -contains $_.Extension.ToLowerInvariant()) -and
        -not ($excludeFileNames -contains $_.Name)
    }

# Files and folders whose leaf name contains the pattern (these need a filename/folder rename).
$renameItems = New-Object System.Collections.Generic.List[object]
$dirsToCheck = Get-ChildItem -Path $repoRoot -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) -and ($_.Name -match $pattern) }
foreach ($d in $dirsToCheck) {
    $cat = Get-Category '' $d.FullName
    $renameItems.Add([pscustomobject]@{
        Type = 'Folder'; Path = $d.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
        Category = $cat; Suggested = (Get-RenameSuggestion $d.Name $cat)
    })
}
foreach ($f in $files) {
    if ($f.Name -match $pattern) {
        $cat = Get-Category '' $f.FullName
        $renameItems.Add([pscustomobject]@{
            Type = 'File'; Path = $f.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
            Category = $cat; Suggested = (Get-RenameSuggestion $f.Name $cat)
        })
    }
}
$renameItems = $renameItems | Sort-Object Path

$results = New-Object System.Collections.Generic.List[object]
$totalFiles = $files.Count
$fileIdx = 0
foreach ($f in $files) {
    $fileIdx++
    $pct = [int](($fileIdx / [Math]::Max(1, $totalFiles)) * 100)
    Write-Progress -Activity "Scanning for workstation occurrences" -Status "File $fileIdx of $totalFiles : $($f.Name)" -PercentComplete $pct
    $rel = $f.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
    $lines = Get-Content -LiteralPath $f.FullName -ErrorAction SilentlyContinue
    if ($null -eq $lines) { continue }
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $raw = $lines[$i]
        if ($null -eq $raw) { continue }
        if ($raw -match $pattern) {
            $trimmed = $raw.Trim()
            if ($trimmed.Length -gt $lineTruncate) { $trimmed = $trimmed.Substring(0, $lineTruncate) }
            $results.Add([pscustomobject]@{
                File     = $rel
                Line     = $i + 1
                Category = (Get-Category $raw $f.FullName)
                Match    = $trimmed
            })
        }
    }
}
Write-Progress -Activity "Scanning for workstation occurrences" -Completed

# --- Summary by file ---
$summary = $results | Group-Object File | ForEach-Object {
    $cats = ($_.Group.Category | Sort-Object -Unique) -join ', '
    [pscustomobject]@{ File = $_.Name; Count = $_.Count; Categories = $cats }
} | Sort-Object File

# --- Build Markdown ---
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# Rename Scan Results - workstation -> Computer / Work Center")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
[void]$sb.AppendLine("Repo: $repoRoot")
[void]$sb.AppendLine("Pattern: ``workstation`` / ``Work Station`` (case-insensitive)")
[void]$sb.AppendLine("Total matches: $($results.Count) in $($summary.Count) files")
[void]$sb.AppendLine("Files/folders to rename: $($renameItems.Count)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Excluded (generated/binary/SCM): $($excludeDirs -join ', '); binary exts: $($binaryExts -join ', '); log/PRI/tool files: $($logExts -join ', ') + $($excludeFileNames -join ', ')")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("> Category is a heuristic. Items marked **Review** need a human decision (computer vs work center).")
[void]$sb.AppendLine("")

[void]$sb.AppendLine("## Summary by file")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| File | Count | Categories |")
[void]$sb.AppendLine("|---|---|---|")
foreach ($s in $summary) {
    [void]$sb.AppendLine("| ``$($s.File)`` | $($s.Count) | $($s.Categories) |")
}
[void]$sb.AppendLine("")

[void]$sb.AppendLine("## Files / folders to rename")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Type | Path | Category | Suggested name |")
[void]$sb.AppendLine("|---|---|---|---|")
foreach ($r in $renameItems) {
    [void]$sb.AppendLine("| $($r.Type) | ``$($r.Path)`` | $($r.Category) | ``$($r.Suggested)`` |")
}
[void]$sb.AppendLine("")

[void]$sb.AppendLine("## Detailed edit map")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| File | Line | Category | Matched text |")
[void]$sb.AppendLine("|---|---|---|---|")
foreach ($r in $results) {
    $esc = $r.Match.Replace('|', '\|')
    [void]$sb.AppendLine("| ``$($r.File)`` | $($r.Line) | $($r.Category) | $esc |")
}

$outDir = Split-Path -Parent $OutFile
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
Set-Content -LiteralPath $OutFile -Value $sb.ToString() -Encoding utf8

# --- Console output ---
Write-Host "Matches: $($results.Count) across $($summary.Count) files" -ForegroundColor Cyan
Write-Host "Files/folders to rename: $($renameItems.Count)" -ForegroundColor Cyan
Write-Host "Results written to: $OutFile" -ForegroundColor Green
Write-Host ""
Write-Host "## Summary by file" -ForegroundColor Cyan
$summary | Format-Table -AutoSize
Write-Host "## Files / folders to rename" -ForegroundColor Cyan
$renameItems | Format-Table -AutoSize
Write-Host "## Detailed edit map" -ForegroundColor Cyan
$results | Format-Table -AutoSize
