<#
.SYNOPSIS
    Rewrites the static logging call sites in this repository from one log type to another.

.DESCRIPTION
    The application logs through more than one surface. Only one of them is written as a static type at every
    call site, and that is the one this script migrates:

      * StartupDebugLog   - static, [Conditional("DEBUG")], 489 references in 83 files. THIS is script-shaped:
                            renaming or re-homing the type means editing every call site.
      * ILogger<T>        - 226 calls in 26 files. Needs a logging PROVIDER that writes to the store, not a
                            call-site rewrite. This script deliberately does not touch it.
      * ServiceLog        - the on-host helper service's own logger, 25 calls in 6 files. Separate host, separate
                            decision; pass -OldType ServiceLog to migrate it once that decision is made.
      * Debug.WriteLine   - ad hoc, Debug-only, left alone.

    The rewrite is a pure token replacement of the type name at call sites. It never touches the arguments, the
    line structure or the multi-line call formatting, so a 489-line mechanical edit carries no semantic risk.

    Two details make it correct:

      1. The fully-qualified form is handled first. One production file calls
         MTM_Waitlist.Module_Core.Helpers.StartupDebugLog.Info(...). Replacing the short name there would leave
         MTM_Waitlist.Module_Core.Helpers.AppLog, so the qualifier is consumed instead.
      2. Using directives are NOT rewritten. MTM_Waitlist.Module_Core.Helpers contains ten other types and is
         imported by 134 files, so touching that import would break files that never logged. Instead the new
         type is made reachable with a single global using, which is what -GlobalUsing adds.

    Dry run is the default. Nothing is written without -Apply.

.PARAMETER Root
    Repository root. Defaults to the parent of this script's folder.

.PARAMETER OldType
    The log type being retired. Default: StartupDebugLog.

.PARAMETER NewType
    The log type taking its place. Default: AppLog. Confirm the real name before applying.

.PARAMETER Apply
    Write the changes. Without it the script only reports what it would do.

.PARAMETER GlobalUsing
    The global using line added so the new type resolves without per-file using edits.

.PARAMETER AddGlobalUsing
    Add -GlobalUsing to Usings.cs when applying. On by default; pass -AddGlobalUsing:$false to skip it when the
    new type already resolves in every file.

.EXAMPLE
    .\tools\migrate-logging-callsites.ps1
    Reports every call site that would change. Writes nothing.

.EXAMPLE
    .\tools\migrate-logging-callsites.ps1 -NewType AppLog -Apply
    Applies the rename and adds the global using.

.NOTES
    Refs: STARTUP-REBUILD-BRIEF.md S9 (logging), S3.1 (removal inventory).
    Run this BEFORE deleting StartupDebugLog, so the tree is never left with a type that does not exist.
#>
[CmdletBinding()]
param(
    [string] $Root,
    [string] $OldType = 'StartupDebugLog',
    [string] $NewType = 'AppLog',
    [string] $QualifiedPrefix = 'MTM_Waitlist.Module_Core.Helpers',
    [string] $GlobalUsing = 'global using MTM_Waitlist.Logging;',
    [string] $GlobalUsingFile = 'Usings.cs',
    [switch] $AddGlobalUsing = $true,
    [string[]] $ExcludeDirectoryNames = @('bin', 'obj'),
    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolved here rather than in the parameter default: $PSScriptRoot is not yet populated while the parameter
# block of a script invoked with -File is being bound, so a default there binds empty and the script dies
# before it does anything. Observed on 2026-09-25 with Windows PowerShell 5.1.
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($OldType) -or [string]::IsNullOrWhiteSpace($NewType)) {
    throw 'Both -OldType and -NewType must be given.'
}

if ($OldType -eq $NewType) {
    throw "-OldType and -NewType are both '$OldType'. There is nothing to migrate."
}

if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
    throw "The repository root '$Root' does not exist."
}

$shortPattern = [regex]::Escape("$OldType.")
$qualifiedPattern = [regex]::Escape("$QualifiedPrefix.$OldType.")
$configurePattern = [regex]::Escape("$OldType.Configure(")
$isTestPath = [regex]'(?i)(\\|^)MTM_Waitlist\.Tests(\\|$)'

function Get-MigrationFiles {
    param([string]$Root, [string[]]$ExcludeDirectoryNames)

    Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.cs' -File | Where-Object {
        $relative = $_.FullName.Substring($Root.Length).TrimStart('\')
        $segments = $relative -split '\\'
        -not ($segments | Where-Object { $ExcludeDirectoryNames -contains $_ })
    }
}

function Read-TextWithEncoding {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $encoding = New-Object System.Text.UTF8Encoding($hasBom)

    return [pscustomobject]@{
        Text    = [System.IO.File]::ReadAllText($Path)
        Encoding = $encoding
    }
}

Write-Host ''
Write-Host "Migrating static log call sites: $OldType -> $NewType" -ForegroundColor Cyan
Write-Host "Root: $Root"
Write-Host "Mode: $(if ($Apply) { 'APPLY' } else { 'DRY RUN (nothing is written)' })"
Write-Host ''

$files = Get-MigrationFiles -Root $Root -ExcludeDirectoryNames $ExcludeDirectoryNames
$results = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $read = Read-TextWithEncoding -Path $file.FullName
    $original = $read.Text

    if (-not $original.Contains("$OldType.")) {
        continue
    }

    $totalHits = ([regex]::Matches($original, $shortPattern)).Count
    $qualifiedHits = ([regex]::Matches($original, $qualifiedPattern)).Count
    $configureHits = ([regex]::Matches($original, $configurePattern)).Count

    # Qualified first: the qualifier is consumed rather than left dangling in front of the new name.
    $rewritten = [regex]::Replace($original, $qualifiedPattern, "$NewType.")
    $rewritten = [regex]::Replace($rewritten, $shortPattern, "$NewType.")

    $results.Add([pscustomobject]@{
        Path          = $file.FullName
        RelativePath  = $file.FullName.Substring($Root.Length).TrimStart('\')
        IsTest        = $isTestPath.IsMatch($file.FullName)
        Hits          = $totalHits
        Qualified     = $qualifiedHits
        Configure     = $configureHits
        Changed       = ($rewritten -ne $original)
        RewrittenText = $rewritten
        Encoding      = $read.Encoding
    })
}

if ($results.Count -eq 0) {
    Write-Host "No file references '$OldType'. Nothing to do." -ForegroundColor Yellow
    return
}

$production = $results | Where-Object { -not $_.IsTest }
$tests = $results | Where-Object { $_.IsTest }

Write-Host 'Per file:' -ForegroundColor Cyan
$results |
    Sort-Object -Property @{ Expression = 'IsTest' }, @{ Expression = 'Hits'; Descending = $true } |
    ForEach-Object {
        $kind = if ($_.IsTest) { 'test' } else { 'app ' }
        $note = if ($_.Configure -gt 0) { "  <- $($_.Configure) Configure call(s) need a human" } else { '' }
        Write-Host ("  [{0}] {1,4} hit(s)  {2}{3}" -f $kind, $_.Hits, $_.RelativePath, $note)
    }

$summary = [pscustomobject]@{
    Files          = $results.Count
    ProductionFiles = $production.Count
    TestFiles      = $tests.Count
    CallSites      = ($results | Measure-Object -Property Hits -Sum).Sum
    QualifiedSites = ($results | Measure-Object -Property Qualified -Sum).Sum
    ConfigureSites = ($results | Measure-Object -Property Configure -Sum).Sum
}

Write-Host ''
Write-Host 'Totals:' -ForegroundColor Cyan
Write-Host ("  files            : {0}  ({1} production, {2} test)" -f $summary.Files, $summary.ProductionFiles, $summary.TestFiles)
Write-Host ("  call sites       : {0}" -f $summary.CallSites)
Write-Host ("  fully qualified  : {0}  (qualifier consumed, not carried over)" -f $summary.QualifiedSites)
Write-Host ("  Configure calls  : {0}  (signature change - review by hand)" -f $summary.ConfigureSites)

if ($summary.ConfigureSites -gt 0) {
    Write-Host ''
    Write-Host 'Configure call sites needing manual review:' -ForegroundColor Yellow
    $results | Where-Object { $_.Configure -gt 0 } | ForEach-Object { Write-Host "  $($_.RelativePath)" }
}

if (-not $Apply) {
    Write-Host ''
    Write-Host 'Dry run complete. Re-run with -Apply to write these changes.' -ForegroundColor Yellow
    return
}

$written = 0
foreach ($result in $results) {
    if (-not $result.Changed) { continue }

    [System.IO.File]::WriteAllText($result.Path, $result.RewrittenText, $result.Encoding)
    $written++
}

Write-Host ''
Write-Host "Rewrote $written file(s)." -ForegroundColor Green

if ($AddGlobalUsing) {
    $usingPath = Join-Path -Path $Root -ChildPath $GlobalUsingFile

    if (-not (Test-Path -LiteralPath $usingPath -PathType Leaf)) {
        Write-Warning "Global-using file '$GlobalUsingFile' was not found, so '$GlobalUsing' was not added. Add it by hand, or every call site will fail to resolve."
    }
    else {
        $read = Read-TextWithEncoding -Path $usingPath

        if ($read.Text -split "`n" | Where-Object { $_.Trim() -eq $GlobalUsing }) {
            Write-Host "Global using already present in $GlobalUsingFile."
        }
        else {
            $newText = $GlobalUsing + [Environment]::NewLine + $read.Text
            [System.IO.File]::WriteAllText($usingPath, $newText, $read.Encoding)
            Write-Host "Added '$GlobalUsing' to $GlobalUsingFile." -ForegroundColor Green
        }
    }
}

$remaining = Get-MigrationFiles -Root $Root -ExcludeDirectoryNames $ExcludeDirectoryNames |
    Select-String -Pattern ([regex]::Escape($OldType)) -SimpleMatch:$false

Write-Host ''
if ($remaining) {
    Write-Host "Still referencing '$OldType' after the rewrite:" -ForegroundColor Yellow
    $remaining | Group-Object Path | ForEach-Object {
        Write-Host ("  {0,4}  {1}" -f $_.Count, $_.Name.Substring($Root.Length).TrimStart('\'))
    }
    Write-Host 'Expected if the old type still exists. It must read zero before that type is deleted.'
}
else {
    Write-Host "No remaining reference to '$OldType'. It is now safe to delete that type." -ForegroundColor Green
}

Write-Host ''
Write-Host 'Next: build the solution, then run the suite.' -ForegroundColor Cyan
