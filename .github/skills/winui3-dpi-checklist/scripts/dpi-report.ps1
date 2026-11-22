$ErrorActionPreference = "SilentlyContinue"

Write-Output "== WinUI 3 DPI report =="
Write-Output ""

# System DPI and equivalent scale percent from the user's desktop registry.
$metrics = Get-ItemProperty 'HKCU:\Control Panel\Desktop\WindowMetrics'
$desktop = Get-ItemProperty 'HKCU:\Control Panel\Desktop'
$logPixels = $metrics.AppliedDPI
if (-not $logPixels -or $logPixels -le 0) { $logPixels = $desktop.LogPixels }
if (-not $logPixels -or $logPixels -le 0) {
    # Fall back to the Win8 scaling flag → default 96 when unset/100%.
    $logPixels = 96
    Write-Output "System DPI: not reported (defaulting to 96 → 100%)"
}
if ($logPixels -and $logPixels -gt 0) {
    $pct = [math]::Round(($logPixels / 96) * 100)
    Write-Output ("System DPI (AppliedDPI): {0}  →  scale {1}%" -f $logPixels, $pct)
}

# Best-effort per-monitor DPI from the per-monitor settings store.
$perMonitor = Get-ChildItem 'HKCU:\Control Panel\Desktop\PerMonitorSettings' -ErrorAction SilentlyContinue
if ($perMonitor) {
    Write-Output ""
    Write-Output "Per-monitor DPI values:"
    foreach ($entry in $perMonitor) {
        $props = Get-ItemProperty $entry.PSPath
        $dpi = $props.DpiValue
        if (-not $dpi) { $dpi = $props.LogPixels }
        Write-Output ("  {0}: {1}" -f $entry.PSChildName, $dpi)
    }
} else {
    Write-Output "Per-monitor DPI values: unavailable"
}

# Connected monitors and resolutions.
Write-Output ""
Write-Output "Monitors (Win32_DesktopMonitor):"
$monitors = Get-CimInstance Win32_DesktopMonitor -ErrorAction SilentlyContinue
if ($monitors) {
    foreach ($m in $monitors) {
        $name = $m.Name
        if (-not $name) { $name = $m.Description }
        Write-Output ("  {0}  {1}x{2}" -f $name, $m.ScreenWidth, $m.ScreenHeight)
    }
} else {
    Write-Output "  none reported"
}

Write-Output ""
Write-Output "Video controllers (current resolution):"
$gpus = Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue
if ($gpus) {
    foreach ($g in $gpus) {
        Write-Output ("  {0}  {1}x{2}" -f $g.Name, $g.CurrentHorizontalResolution, $g.CurrentVerticalResolution)
    }
} else {
    Write-Output "  none reported"
}

Write-Output ""
Write-Output "== Manual test checklist =="
Write-Output "See references/checklist.md in this skill, or run:"
Write-Output "  pwsh -NoProfile -File .github\skills\winui3-dpi-checklist\scripts\dpi-report.ps1 -Checklist"
Write-Output ""

if ($args -contains '-Checklist' -or $PSBoundParameters.ContainsKey('Checklist')) {
    $checklist = Join-Path $PSScriptRoot '..\references\checklist.md'
    if (Test-Path -LiteralPath $checklist) {
        Get-Content -LiteralPath $checklist | Write-Output
    }
}
