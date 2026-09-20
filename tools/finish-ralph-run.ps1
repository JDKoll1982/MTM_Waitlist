# Finish the Spec Kit Ralph run for a feature whose remaining work is the running-app UI walks.
#
# WHY THIS EXISTS
#   The main run stopped with three tasks left, and only two of them are work an agent can do:
#     * T188 and T184 are running-app UI gates - UI Automation walks against the Debug build.
#     * T165 is the owner's live-database reinstall and is deliberately reserved to a person.
#
#   Because T165 can never be ticked by the agent, the loop can NEVER reach "all tasks complete". It will always
#   run to its full iteration budget instead, so the budget here is deliberately small: enough for the two walks
#   with a retry or two, not enough to spend an hour re-reading a 2,500-line task file between them.
#
#   This is a thin wrapper. It resolves paths, reports what is actually left, distinguishes the owner-reserved
#   task from the agent-doable ones, and then hands off to tools/run-ralph-deepseek.ps1 - which owns the
#   DeepSeek BYOK setup, probes the key once, and starts the loop.
#
# USAGE
#   pwsh -ExecutionPolicy Bypass -File tools/finish-ralph-run.ps1
#   pwsh -ExecutionPolicy Bypass -File tools/finish-ralph-run.ps1 -MaxIterations 4
#   pwsh -ExecutionPolicy Bypass -File tools/finish-ralph-run.ps1 -WhatIfOnly     # report only, launch nothing

[CmdletBinding()]
param(
    # UI-walk iterations took 3-10 minutes each. 8 leaves room for both walks plus retries without wasting an hour.
    [int]$MaxIterations = 8,

    [string]$FeatureDir = 'specs/004-unified-card-item-picker',

    # Tasks no agent may complete, because each one needs a person. Reported, never attempted.
    [string[]]$OwnerReservedTasks = @('T165'),

    # Print what is left and exit without starting the loop.
    [switch]$WhatIfOnly
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# Resolve against the repository root so this works from any working directory.
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not [System.IO.Path]::IsPathRooted($FeatureDir)) { $FeatureDir = Join-Path $repoRoot $FeatureDir }

$tasksPath  = Join-Path $FeatureDir 'tasks.md'
$specDir    = $FeatureDir
$launcher   = Join-Path $PSScriptRoot 'run-ralph-deepseek.ps1'

if (-not (Test-Path $tasksPath)) {
    Write-Host "No tasks.md at $tasksPath - check -FeatureDir." -ForegroundColor Red
    exit 6
}
if (-not (Test-Path $launcher)) {
    Write-Host "The DeepSeek launcher is missing: $launcher" -ForegroundColor Red
    exit 6
}

# Read the open tasks. The pattern tolerates the bold task ids this file uses ("- [ ] **T188** ...").
$openIds = [regex]::Matches((Get-Content $tasksPath -Raw), '- \[ \] \*{0,2}(T\d+)') |
    ForEach-Object { $_.Groups[1].Value }

if ($openIds.Count -eq 0) {
    Write-Host 'All tasks are ticked. There is nothing to finish.' -ForegroundColor Green
    exit 0
}

$ownerIds  = @($openIds | Where-Object { $OwnerReservedTasks -contains $_ })
$agentIds  = @($openIds | Where-Object { $OwnerReservedTasks -notcontains $_ })

Write-Host ''
Write-Host "Feature      : $(Split-Path -Leaf $FeatureDir)" -ForegroundColor Cyan
Write-Host "Open tasks   : $($openIds.Count)  [$($openIds -join ', ')]" -ForegroundColor Cyan
Write-Host "Agent can do : $($agentIds.Count)  [$($agentIds -join ', ')]" -ForegroundColor Green
if ($ownerIds.Count -gt 0) {
    Write-Host "Yours alone  : $($ownerIds.Count)  [$($ownerIds -join ', ')]" -ForegroundColor Yellow
}
Write-Host ''
if ($ownerIds.Count -gt 0) {
    Write-Host 'Read this before you start: because an owner-reserved task is open, the loop will NOT report' -ForegroundColor Yellow
    Write-Host 'completion however many iterations you give it. It runs to the budget and stops. That is expected.' -ForegroundColor Yellow
}
else {
    Write-Host 'Every remaining task is agent-doable, so the loop can reach completion and stop on its own' -ForegroundColor Green
    Write-Host 'rather than running to the budget. The budget is only an upper bound.' -ForegroundColor Green
}
Write-Host ''

if ($WhatIfOnly) {
    Write-Host 'WhatIfOnly - nothing launched.' -ForegroundColor Cyan
    exit 0
}

if ($agentIds.Count -eq 0) {
    Write-Host 'Every remaining task is reserved to a person. Nothing for the loop to do - not starting it.' -ForegroundColor Yellow
    exit 0
}

Write-Host "Starting the loop for $MaxIterations iteration(s) over $($agentIds.Count) agent-doable task(s)..." -ForegroundColor Cyan
Write-Host ''

& $launcher `
    -MaxIterations $MaxIterations `
    -TasksPath $tasksPath `
    -SpecDir $specDir
