# List - and optionally invoke - every element in the running MTM_Waitlist window that accepts InvokePattern.
#
# WHY THIS EXISTS
#   tools/ui-drive.ps1 matches elements by accessible Name, so a button with no name is invisible to it. This app has
#   at least one: the wizard's Submit button on the Confirm step renders its caption as a child Text element and
#   exposes no name of its own, so a name-based click reports "no Button matched [Submit]" even though the button is
#   on screen and enabled. This script finds elements by CAPABILITY (do they accept Invoke?) instead of by name, so
#   unnamed buttons are reachable.
#
# USAGE
#   pwsh -File tools/uia-invoke.ps1 -Label 12-invokables              # list them, with indexes
#   pwsh -File tools/uia-invoke.ps1 -Invoke -Index 7 -Label 13-submit  # invoke the one at index 7, then dump

[CmdletBinding()]
param(
    [switch]$Invoke,

    [int]$Index = -1,

    [string]$Label = 'invokables',

    [string]$EvidenceDir = "$env:TEMP\die-walk"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$Scope          = [System.Windows.Automation.TreeScope]
$InvokePattern  = [System.Windows.Automation.InvokePattern]::Pattern

function Get-AppRoot {
    $p = Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { throw 'MTM_Waitlist is not running.' }
    if ($p.MainWindowHandle -eq 0) { throw 'MTM_Waitlist has no main window yet.' }
    [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
}

function Get-Invokables {
    param($Root)

    $all = $Root.FindAll($Scope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    $rows = New-Object System.Collections.ArrayList

    foreach ($el in $all) {
        $pat = $null
        try   { $ok = $el.TryGetCurrentPattern($InvokePattern, [ref]$pat) }
        catch { $ok = $false }
        if (-not $ok) { continue }

        $r = $el.Current.BoundingRectangle
        $rect = 'empty'
        if (-not [double]::IsNaN($r.Width)) {
            $rect = '{0},{1} {2}x{3}' -f ([int]$r.X), ([int]$r.Y), ([int]$r.Width), ([int]$r.Height)
        }
        $null = $rows.Add([pscustomobject]@{
            Index        = $rows.Count
            ControlType  = $el.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')
            Name         = $el.Current.Name
            AutomationId = $el.Current.AutomationId
            Enabled      = $el.Current.IsEnabled
            OnScreen     = $rect -ne 'empty'
            Rect         = $rect
            Element      = $el
        })
    }
    $rows
}

function Show-Invokables {
    param($Rows, [string]$Label)

    $lines = @('# ' + $Label, "when      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')", '', "## Invokable elements ($($Rows.Count))")
    foreach ($row in $Rows) {
        $lines += ('{0,3}  {1,-12} enabled={2,-5} onscreen={3,-5} {4,-34} {5,-22} {6}' -f `
            $row.Index, $row.ControlType, $row.Enabled, $row.OnScreen, $row.Name, $row.AutomationId, $row.Rect)
    }

    New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
    $path = Join-Path $EvidenceDir ("{0}.txt" -f $Label)
    $lines | Set-Content -Path $path -Encoding utf8

    Write-Host "evidence  : $path" -ForegroundColor DarkGray
    $lines | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
}

$root = Get-AppRoot
$rows = Get-Invokables -Root $root

if (-not $Invoke) {
    Show-Invokables -Rows $rows -Label $Label
    exit 0
}

if ($Index -lt 0 -or $Index -ge $rows.Count) {
    Show-Invokables -Rows $rows -Label $Label
    throw "-Index $Index is out of range; see the listing above."
}

$chosen = $rows[$Index]
Write-Host ("invoking  : index {0} - {1} '{2}'" -f $chosen.Index, $chosen.ControlType, $chosen.Name) -ForegroundColor Green
$chosen.Element.GetCurrentPattern($InvokePattern).Invoke()
Start-Sleep -Milliseconds 1500

# Re-dump the same view ui-drive.ps1 writes, so the evidence trail stays in one format.
& "$PSScriptRoot\ui-drive.ps1" -Action dump -Label $Label -EvidenceDir $EvidenceDir -WaitSeconds 30
