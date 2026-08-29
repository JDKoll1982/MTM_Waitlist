<#
.SYNOPSIS
Splits Documents/Development/InforVisual/InforVisualGuide.md into per-chapter
Markdown files inside Documents/Development/InforVisual/Infor Visual Guide/.

.DESCRIPTION
The Infor VISUAL System-wide User's Guide is a single ~10,000-line Markdown export
with a flat heading structure (chapter and section titles are all "# " level-1
headings). This script locates every "# Chapter N: Title" heading and writes one
file per chapter.

Output naming:
  NN-Title.md        one file per chapter, using a zero-padded chapter number and a
                     hyphenated, filesystem-safe title
                     (e.g. 02-Application-Global-Maintenance.md)
  Index.md           the trailing "# Index" section, when present
  00-Front-Matter.md cover image, copyright notices, and the Contents table of
                     contents -- written only when -IncludeFrontMatter is used

The split is line-based and lossless: each chapter body is copied verbatim
(including its original "# Chapter N: Title" heading line); headings are not
renumbered or demoted.

.EXAMPLE
.\tools\Split-InforVisualGuide.ps1

.EXAMPLE
.\tools\Split-InforVisualGuide.ps1 -IncludeFrontMatter -Verbose
#>

[CmdletBinding()]
param(
    [string]$Source    = (Join-Path $PSScriptRoot '..\Documents\Development\InforVisual\InforVisualGuide.md'),
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\Documents\Development\InforVisual\Infor Visual Guide'),
    [switch]$IncludeFrontMatter
)

$ErrorActionPreference = 'Stop'

$Source    = [System.IO.Path]::GetFullPath($Source)
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
    throw "Source file not found: $Source"
}

# --- Read the file verbatim (UTF-8) and split into lines -------------------
$text    = [System.IO.File]::ReadAllText($Source, [System.Text.Encoding]::UTF8)
$newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$lines   = $text -split "`r`n|`r|`n"
# Drop the single trailing empty element produced when the file ends with a newline.
if ($lines.Count -gt 0 -and $lines[-1] -eq '') {
    $lines = $lines[0..($lines.Count - 2)]
}

# --- Locate chapter boundaries ---------------------------------------------
$chapterRegex  = '^# Chapter\s+(\d+):\s*(.+)$'
$chapterStarts = New-Object System.Collections.Generic.List[object]
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $chapterRegex) {
        $chapterStarts.Add([pscustomobject]@{
            Index  = $i
            Number = [int]$Matches[1]
            Name   = $Matches[2].Trim()
        })
    }
}
if ($chapterStarts.Count -eq 0) {
    throw 'No "# Chapter N:" headings found in the source file.'
}

# --- Detect a trailing "# Index" section inside the last chapter ------------
$indexStart = -1
$lastStart  = $chapterStarts[$chapterStarts.Count - 1].Index
for ($i = $lastStart; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^#\s*Index\s*$') { $indexStart = $i; break }
}

# --- Helpers ----------------------------------------------------------------
function Get-SafeName([string]$name) {
    $s = [regex]::Replace($name.Trim(), '[^A-Za-z0-9]+', '-')
    $s = $s.Trim('-')
    if ([string]::IsNullOrEmpty($s)) { $s = 'Chapter' }
    return $s
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[void][System.IO.Directory]::CreateDirectory($OutputDir)

$written = New-Object System.Collections.Generic.List[object]

function Write-Segment([string[]]$segment, [string]$fileName) {
    $outPath = Join-Path $script:OutputDir $fileName
    $content = $segment -join $script:newline
    if ($content.Length -gt 0) { $content += $script:newline }
    [System.IO.File]::WriteAllText($outPath, $content, $script:utf8NoBom)
    $script:written.Add([pscustomobject]@{ File = $fileName; Lines = $segment.Count; Path = $outPath })
    Write-Verbose ("Wrote {0} ({1} lines)" -f $outPath, $segment.Count)
}

# --- Front matter (optional) ------------------------------------------------
if ($IncludeFrontMatter -and $chapterStarts[0].Index -gt 0) {
    Write-Segment $lines[0..($chapterStarts[0].Index - 1)] '00-Front-Matter.md'
}

# --- Chapters ---------------------------------------------------------------
for ($k = 0; $k -lt $chapterStarts.Count; $k++) {
    $start = $chapterStarts[$k].Index
    if ($k + 1 -lt $chapterStarts.Count) {
        $end = $chapterStarts[$k + 1].Index - 1
    } else {
        $end = if ($indexStart -gt $start) { $indexStart - 1 } else { $lines.Count - 1 }
    }

    $segment  = $lines[$start..$end]
    $fileName = '{0:D2}-{1}.md' -f $chapterStarts[$k].Number, (Get-SafeName $chapterStarts[$k].Name)
    Write-Segment $segment $fileName
}

# --- Index (optional) -------------------------------------------------------
if ($indexStart -ge 0) {
    Write-Segment $lines[$indexStart..($lines.Count - 1)] 'Index.md'
}

# --- Summary -----------------------------------------------------------------
Write-Host ''
Write-Host ("Source : {0} ({1} lines)" -f $Source, $lines.Count)
Write-Host ("Output : {0}" -f $OutputDir)
Write-Host ("Wrote  : {0} file(s)" -f $written.Count)
foreach ($w in $written) {
    Write-Host ("  {0,-42} {1,6} lines" -f $w.File, $w.Lines)
}
if (-not $IncludeFrontMatter) {
    Write-Host ''
    Write-Host 'Note: front matter (cover, copyright, Contents) was skipped. Re-run with -IncludeFrontMatter to keep it.'
}
Write-Host ''
