<#
.SYNOPSIS
    Measures how long the running MTM_Waitlist window's user-interface thread is unresponsive.

.DESCRIPTION
    A person reports "the application hangs for a few seconds when I do X". That is a statement about the UI
    thread, and this measures exactly it rather than the wall-clock time of the action: the window is asked to
    acknowledge a null message every few milliseconds, and an acknowledgement that does not come back inside the
    timeout means the thread was busy and could not process input.

    Why this rather than a stopwatch around the click. An action that writes to the store and then rebuilds a
    list has two parts, and only one of them is a hang. Timing the whole action conflates a fast write with a slow
    rebuild, and it cannot tell a busy thread from one that is merely awaiting a network call. This can: input
    that is not answered is a frozen window, whatever is holding the thread.

    The measurement is only meaningful while the window is idle at the start. It reports the longest single
    unresponsive stretch, the total, and a timeline, so a fix can be shown to have removed the stall rather than
    moved it.

.PARAMETER Seconds
    How long to sample. Default 20.

.PARAMETER TimeoutMilliseconds
    How long the window is given to answer. Default 80. A thread doing ordinary work answers in well under this,
    so a value in the tens of milliseconds is well clear of normal input handling.

.PARAMETER IntervalMilliseconds
    How often the window is asked, while it is answering. Default 25. Sampling continues immediately after a
    timeout, so a stretch is not missed by the interval.

.PARAMETER ProcessName
    The process whose main window is measured. Default MTM_Waitlist.

.PARAMETER Trigger
    Optional script block run once, immediately after sampling starts, to make the application do the thing that
    is reported to hang. It runs in this process, so it should only drive the window - not wait for anything.

.EXAMPLE
    pwsh -File tools/measure-ui-responsiveness.ps1 -Seconds 15
    Measures an idle window, which is the baseline: a healthy window reports no unresponsive stretch at all.

.EXAMPLE
    pwsh -File tools/measure-ui-responsiveness.ps1 -Seconds 25 -Trigger { Start-Sleep -Milliseconds 400; (Get-Process MTM_Waitlist).MainWindowHandle | Out-Null }
#>
[CmdletBinding()]
param(
    [double] $Seconds = 20,
    [int] $TimeoutMilliseconds = 80,
    [int] $IntervalMilliseconds = 25,
    [string] $ProcessName = 'MTM_Waitlist',
    [scriptblock] $Trigger
)

$ErrorActionPreference = 'Stop'

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class UiResponsiveness {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    public const uint WM_NULL = 0x0000;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
}
"@

$process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1

if (-not $process) {
    throw "No '$ProcessName' window is open, so there is nothing to measure. Start the application first."
}

$handle = $process.MainWindowHandle
Write-Output "Measuring '$($process.ProcessName)' (pid $($process.Id)) for $Seconds second(s)."

# One warm-up ask, so the very first sample is not the one that loads anything lazily.
$warm = [IntPtr]::Zero
[void][UiResponsiveness]::SendMessageTimeout($handle, [UiResponsiveness]::WM_NULL, [IntPtr]::Zero, [IntPtr]::Zero, [UiResponsiveness]::SMTO_ABORTIFHUNG, 1000, [ref]$warm)

$deadline = [datetime]::UtcNow.AddSeconds($Seconds)
$stretches = New-Object System.Collections.Generic.List[object]
$currentStart = $null
$currentEnd = $null
$samples = 0
$timedOut = 0
$triggered = $false

while ([datetime]::UtcNow -lt $deadline) {
    if ($Trigger -and -not $triggered) {
        $triggered = $true
        & $Trigger
    }

    $result = [IntPtr]::Zero
    $started = [datetime]::UtcNow
    $answered = [UiResponsiveness]::SendMessageTimeout(
        $handle, [UiResponsiveness]::WM_NULL, [IntPtr]::Zero, [IntPtr]::Zero,
        [UiResponsiveness]::SMTO_ABORTIFHUNG, [uint32]$TimeoutMilliseconds, [ref]$result)
    $elapsed = ([datetime]::UtcNow - $started).TotalMilliseconds
    $samples++

    if ($answered -eq [IntPtr]::Zero -or $elapsed -ge ($TimeoutMilliseconds - 5)) {
        # Not answered inside the timeout: the thread could not process its own input queue.
        $timedOut++
        if ($null -eq $currentStart) { $currentStart = $started }
        $currentEnd = [datetime]::UtcNow
    }
    elseif ($null -ne $currentStart) {
        $stretches.Add([pscustomobject]@{ From = $currentStart; To = $currentEnd })
        $currentStart = $null
        $currentEnd = $null
    }

    Start-Sleep -Milliseconds $IntervalMilliseconds
}

if ($null -ne $currentStart) {
    $stretches.Add([pscustomobject]@{ From = $currentStart; To = $currentEnd })
}

$stretchReports = @(
    foreach ($stretch in $stretches) {
        [pscustomobject]@{
            Seconds = [Math]::Round(($stretch.To - $stretch.From).TotalSeconds, 2)
            StartedLocal = $stretch.From.ToLocalTime().ToString('HH:mm:ss.fff')
        }
    }
)

$longest = if ($stretchReports.Count -gt 0) { ($stretchReports | Measure-Object -Property Seconds -Maximum).Maximum } else { 0 }
$total = if ($stretchReports.Count -gt 0) { ($stretchReports | Measure-Object -Property Seconds -Sum).Sum } else { 0 }

Write-Output ''
Write-Output "samples: $samples   unresponsive answers: $timedOut"
Write-Output "unresponsive stretches: $($stretchReports.Count)"
Write-Output "longest stretch: $longest s   total unresponsive: $([Math]::Round($total, 2)) s"

foreach ($report in $stretchReports) {
    Write-Output ("  {0,6} s  starting {1}" -f $report.Seconds, $report.StartedLocal)
}

# A person notices a stall of about a second or more. Below that the window answered throughout.
$verdict = if ($longest -ge 1) { "HUNG" } elseif ($timedOut -gt 0) { "busy" } else { "responsive" }
Write-Output ''
Write-Output "verdict: $verdict"

if ($env:UI_RESPONSIVENESS_JSON -eq '1') {
    [pscustomobject]@{
        Samples = $samples
        UnresponsiveAnswers = $timedOut
        LongestStretchSeconds = $longest
        TotalUnresponsiveSeconds = [Math]::Round($total, 2)
        Stretches = $stretchReports
        Verdict = $verdict
    } | ConvertTo-Json -Depth 4
}
