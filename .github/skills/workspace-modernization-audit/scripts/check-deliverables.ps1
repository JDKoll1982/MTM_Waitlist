<#
.SYNOPSIS
Validates the two modernization-audit deliverables.

.DESCRIPTION
Deterministic checks only: files exist and are substantial, the specification has its required
sections, the approval register uses known decision tokens, and no proposal ID appears in the
document without a register row. It does not judge prose quality.

.PARAMETER Path
Folder holding the deliverables. Defaults to Dependency_Assessment.

.PARAMETER SkipPdf
Use with the skill's --markdown-only argument: checks REPORT_SUMMARY.md instead of the PDF.

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File .github/skills/workspace-modernization-audit/scripts/check-deliverables.ps1 -Path Dependency_Assessment

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File .github/skills/workspace-modernization-audit/scripts/check-deliverables.ps1 -Path Dependency_Assessment -SkipPdf
#>
[CmdletBinding()]
param(
    [string]$Path = 'Dependency_Assessment',

    [switch]$SkipPdf
)

$ErrorActionPreference = 'Stop'

$allowedDecisions = @(
    'APPROVED'
    'REJECTED'
    'DEFERRED'
    'MORE_RESEARCH_REQUIRED'
    'KEEP_CURRENT'
    'PENDING_USER_DECISION'
)

$requiredSections = @(
    'Overview'
    'Workspace Context'
    'Dependency Inventory'
    'Approved Dependency Updates'
    'File-Level Change'
    'Implementation Plan'
    'Acceptance Criteria'
    'Out of Scope'
    'User Decisions and Approval Register'
    'Research References'
)

$idPattern = '\b(DEP|MOD|SEC|PERF|UX|AI|DEV|DB)-\d{3}\b'

$reportPath = Join-Path $Path 'Dependency_Update_and_Modernization_Report.pdf'
$summaryPath = Join-Path $Path 'REPORT_SUMMARY.md'
$specPath = Join-Path $Path 'DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md'

$results = [System.Collections.Generic.List[object]]::new()

function Add-Result {
    param([string]$Check, [bool]$Pass, [string]$Detail)
    $results.Add([pscustomobject]@{ Check = $Check; Result = $(if ($Pass) { 'PASS' } else { 'FAIL' }); Detail = $Detail })
}

function Get-RegisterRows {
    param([string[]]$Lines)

    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match '^#{1,4}\s.*(User Decisions|Approval Register)') { $start = $i; break }
    }
    if ($start -lt 0) { return @() }

    $block = [System.Collections.Generic.List[string]]::new()
    for ($i = $start + 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match '^#{1,4}\s') { break }
        $block.Add($Lines[$i])
    }

    $decisionIndex = -1
    $rows = [System.Collections.Generic.List[object]]::new()

    foreach ($line in $block) {
        if ($line -notmatch '^\s*\|') { continue }
        $cells = $line.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }

        if ($decisionIndex -lt 0) {
            $headerIndex = 0
            foreach ($cell in $cells) {
                if ($cell -match '^\*{0,2}Decision\*{0,2}$') { $decisionIndex = $headerIndex }
                $headerIndex++
            }
            continue
        }

        if ($line -match '^\s*\|[\s:\-|]+\|\s*$') { continue }
        if ($line -notmatch $idPattern) { continue }

        $id = [regex]::Match($line, $idPattern).Value
        $decision = if ($decisionIndex -lt $cells.Count) { $cells[$decisionIndex] } else { '' }
        $decision = $decision.Trim('`', '*', ' ')

        $rows.Add([pscustomobject]@{ Id = $id; Decision = $decision })
    }

    return $rows.ToArray()
}

# 1. folder and files
Add-Result 'Output folder exists' (Test-Path -LiteralPath $Path -PathType Container) $Path

if ($SkipPdf) {
    Add-Result 'Report summary exists' (Test-Path -LiteralPath $summaryPath -PathType Leaf) $summaryPath
    if (Test-Path -LiteralPath $summaryPath -PathType Leaf) {
        $summaryLength = (Get-Content -LiteralPath $summaryPath -Raw).Length
        Add-Result 'Report summary is substantial' ($summaryLength -ge 500) ('{0:N0} characters' -f $summaryLength)
    }
}
else {
    Add-Result 'PDF report exists' (Test-Path -LiteralPath $reportPath -PathType Leaf) $reportPath
    if (Test-Path -LiteralPath $reportPath -PathType Leaf) {
        $pdfSize = (Get-Item -LiteralPath $reportPath).Length
        Add-Result 'PDF is substantial' ($pdfSize -ge 20KB) ('{0:N0} bytes' -f $pdfSize)
    }
}

Add-Result 'Specification exists' (Test-Path -LiteralPath $specPath -PathType Leaf) $specPath

if (-not (Test-Path -LiteralPath $specPath -PathType Leaf)) {
    $results | Format-Table -AutoSize | Out-String | Write-Output
    Write-Output ''
    Write-Output 'FAILED: specification missing; remaining checks skipped.'
    exit 1
}

$specText = Get-Content -LiteralPath $specPath -Raw
$specLines = Get-Content -LiteralPath $specPath
Add-Result 'Specification is substantial' ($specText.Length -ge 2000) ('{0:N0} characters' -f $specText.Length)

# 2. required sections
$missing = @()
foreach ($section in $requiredSections) {
    if ($specText -notmatch [regex]::Escape($section)) { $missing += $section }
}
Add-Result 'Required sections present' ($missing.Count -eq 0) $(if ($missing.Count -eq 0) { "$($requiredSections.Count)/$($requiredSections.Count) found" } else { 'missing: ' + ($missing -join ', ') })

# 3. approval register
$registerRows = Get-RegisterRows -Lines $specLines
Add-Result 'Approval register has rows' ($registerRows.Count -gt 0) ("$($registerRows.Count) row(s)")

$unknown = $registerRows | Where-Object { $_.Decision -and ($allowedDecisions -notcontains $_.Decision) }
Add-Result 'Register decisions are known tokens' ($unknown.Count -eq 0) $(if ($unknown.Count -eq 0) { 'all recognised' } else { 'unrecognised: ' + (($unknown | ForEach-Object { "$($_.Id)=$($_.Decision)" }) -join ', ') })

$blank = $registerRows | Where-Object { -not $_.Decision }
Add-Result 'Register decisions are filled in' ($blank.Count -eq 0) $(if ($blank.Count -eq 0) { 'all filled' } else { 'blank: ' + (($blank | ForEach-Object { $_.Id }) -join ', ') })

# 4. every proposal ID in the document has a register row
$registerIds = @($registerRows | ForEach-Object { $_.Id } | Sort-Object -Unique)
$documentIds = @([regex]::Matches($specText, $idPattern) | ForEach-Object { $_.Value } | Sort-Object -Unique)
$unregistered = @($documentIds | Where-Object { $registerIds -notcontains $_ })
Add-Result 'No unregistered proposal IDs' ($unregistered.Count -eq 0) $(if ($unregistered.Count -eq 0) { "$($registerIds.Count) ID(s) registered" } else { 'missing rows: ' + ($unregistered -join ', ') })

$results | Format-Table -AutoSize | Out-String | Write-Output

if ($registerRows.Count -gt 0) {
    Write-Output 'Decision counts:'
    $registerRows |
        Group-Object Decision |
        Sort-Object Name |
        Select-Object @{n = 'Decision'; e = { $_.Name } }, @{n = 'Count'; e = { $_.Count } } |
        Format-Table -AutoSize | Out-String | Write-Output
}

$failures = @($results | Where-Object { $_.Result -eq 'FAIL' })
if ($failures.Count -gt 0) {
    Write-Output "FAILED: $($failures.Count) check(s) need attention."
    exit 1
}

Write-Output 'PASSED: all deliverable checks succeeded.'
exit 0
