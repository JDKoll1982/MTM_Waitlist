# Drive the running MTM_Waitlist window by UI Automation, one deliberate action per invocation.
#
# WHY THIS SHAPE
#   The walk has to be driven against what the app actually renders, not against guessed control ids. So this script
#   does one thing per call - launch, dump, click one item, click one button - and writes the window's visible
#   items, texts and buttons to an evidence file every time. Its output is the evidence for the task it serves.
#
#   Each command runs in its own PowerShell session, so the UIAutomation assemblies are loaded here rather than
#   assumed to survive between calls. The window is always located by PROCESS ID, never by title - this app's title
#   is not stable (the splash and sign-in windows report different names).
#
# USAGE
#   pwsh -File tools/ui-drive.ps1 -Action launch
#   pwsh -File tools/ui-drive.ps1 -Action dump     -Label 01-workcenter
#   pwsh -File tools/ui-drive.ps1 -Action click-item   -Name '100-7'        -Label 02-workcenter-picked
#   pwsh -File tools/ui-drive.ps1 -Action click-button -Name 'Add'          -Label 03-add-request
#   pwsh -File tools/ui-drive.ps1 -Action kill
#
#   -Name is a match pattern, tried in order: exact, then prefix, then contains. Pass several comma-separated
#   candidates to try in preference order: -Name 'Pickup Die,Pickup Die:,Die'.

[CmdletBinding()]
param(
    [ValidateSet('launch', 'dump', 'click-item', 'click-button', 'kill')]
    [string]$Action = 'dump',

    [string[]]$Name,

    [string]$Label = 'step',

    [string]$EvidenceDir = "$env:TEMP\die-walk",

    [int]$WaitSeconds = 90,

    [string]$Exe = "$PSScriptRoot\..\bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe",

    [string]$ConnectionString = 'Server=127.0.0.1;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;AllowPublicKeyRetrieval=True;'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$CT     = [System.Windows.Automation.ControlType]
$Scope  = [System.Windows.Automation.TreeScope]
$Invoke = [System.Windows.Automation.InvokePattern]::Pattern

function Get-AppProcess {
    Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Get-AppRoot {
    $p = Get-AppProcess
    if (-not $p) { return $null }
    if ($p.MainWindowHandle -eq 0) { return $null }
    [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
}

function Wait-AppRoot {
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    while ((Get-Date) -lt $deadline) {
        $r = Get-AppRoot
        if ($r) { return $r }
        Start-Sleep -Milliseconds 500
    }
    throw "No main window appeared within $WaitSeconds s. The app may be showing a startup dialog - run -Action dump to see its text."
}

# The splash is a window too, and it answers MainWindowHandle seconds before the shell exists. A click aimed at the
# splash finds nothing and the whole chain after it fails, so anything that CLICKS waits for the real shell: the
# frameless splash has no caption buttons, the shell does. (-Action dump deliberately does not wait - seeing the
# splash is occasionally the point.)
function Test-ShellReady {
    param($Root)

    if (-not $Root) { return $false }

    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $CT::Button)
    return @($Root.FindAll($Scope::Descendants, $c) | Where-Object { $_.Current.Name -eq 'Minimize' }).Count -gt 0
}

function Wait-ShellRoot {
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    while ((Get-Date) -lt $deadline) {
        $r = Get-AppRoot
        if (Test-ShellReady -Root $r) { return $r }
        Start-Sleep -Milliseconds 500
    }
    throw "The shell did not appear within $WaitSeconds s. The app may be stuck on a startup dialog - run -Action dump to see its text."
}

function Get-ByControlType {
    param($Root, $ControlType)
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType)
    $Root.FindAll($Scope::Descendants, $c) | ForEach-Object { $_.Current.Name } | Where-Object { $_ }
}

function Write-Evidence {
    param($Root, [string]$Label, [string]$Note = '')

    New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
    $path = Join-Path $EvidenceDir ("{0}.txt" -f $Label)

    $items   = @(Get-ByControlType -Root $Root -ControlType $CT::ListItem)
    $texts   = @(Get-ByControlType -Root $Root -ControlType $CT::Text)
    $buttons = @(Get-ByControlType -Root $Root -ControlType $CT::Button)

    $body = @(
        "# $Label"
        "when      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "window    : '$(($Root.Current.Name))'  hwnd=$($Root.Current.NativeWindowHandle)"
        if ($Note) { "note      : $Note" }
        ""
        "## ListItems ($($items.Count))"
        $items
        ""
        "## Buttons ($($buttons.Count))"
        $buttons
        ""
        "## Texts ($($texts.Count))"
        $texts
    )
    $body | Set-Content -Path $path -Encoding utf8

    Write-Host "evidence  : $path" -ForegroundColor DarkGray
    Write-Host "items     : $($items.Count)  [$($items -join ' | ')]" -ForegroundColor Cyan
    Write-Host "buttons   : $($buttons.Count)  [$($buttons -join ' | ')]" -ForegroundColor Cyan
    Write-Host "texts     : $($texts.Count)" -ForegroundColor DarkGray
    $texts | ForEach-Object { Write-Host "            $_" -ForegroundColor Gray }
}

function Find-Element {
    param($Root, $ControlType, [string[]]$Patterns)

    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType)
    $found = $Root.FindAll($Scope::Descendants, $c)

    # Exact first, then prefix, then contains - so 'Die' cannot steal a card called 'Die 9003-A' when
    # an exact match exists elsewhere.
    foreach ($pass in @('exact', 'prefix', 'contains')) {
        foreach ($pat in $Patterns) {
            foreach ($el in $found) {
                $n = $el.Current.Name
                if ([string]::IsNullOrWhiteSpace($n)) { continue }
                $hit = switch ($pass) {
                    'exact'    { $n -eq $pat }
                    'prefix'   { $n -like "$pat*" }
                    'contains' { $n -like "*$pat*" }
                }
                if ($hit) { return $el }
            }
        }
    }
    return $null
}

switch ($Action) {

    'launch' {
        if (Get-AppProcess) {
            Write-Host 'The app is already running - not starting a second instance.' -ForegroundColor Yellow
        }
        else {
            if (-not (Test-Path $Exe)) { throw "Executable not found: $Exe" }

            # Both are required: the plain one is not enough, the app still fails its startup session check without
            # the STARTUP_ variant. Set on this process so the child inherits them.
            $env:MTM_WAITLIST_DB_CONNECTION_STRING = $ConnectionString
            $env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING = $ConnectionString

            Start-Process -FilePath $Exe
            Write-Host "launched  : $Exe" -ForegroundColor Green
        }
        $root = Wait-AppRoot
        Write-Host "window    : '$($root.Current.Name)'" -ForegroundColor Green
        Write-Evidence -Root $root -Label $Label -Note 'after launch'
    }

    'dump' {
        $root = Wait-AppRoot
        Write-Evidence -Root $root -Label $Label
    }

    'click-item' {
        if (-not $Name) { throw 'click-item needs -Name.' }
        $root = Wait-ShellRoot
        $el = Find-Element -Root $root -ControlType $CT::ListItem -Patterns $Name
        if (-not $el) {
            $available = @(Get-ByControlType -Root $root -ControlType $CT::ListItem)
            throw "No ListItem matched [$($Name -join ', ')]. Available: $($available -join ' | ')"
        }
        $el.GetCurrentPattern($Invoke).Invoke()
        Write-Host "clicked   : '$($el.Current.Name)'" -ForegroundColor Green
        Start-Sleep -Milliseconds 1200
        Write-Evidence -Root (Wait-AppRoot) -Label $Label -Note "after clicking item '$($el.Current.Name)'"
    }

    'click-button' {
        if (-not $Name) { throw 'click-button needs -Name.' }
        $root = Wait-ShellRoot
        $el = Find-Element -Root $root -ControlType $CT::Button -Patterns $Name
        if (-not $el) {
            $available = @(Get-ByControlType -Root $root -ControlType $CT::Button)
            throw "No Button matched [$($Name -join ', ')]. Available: $($available -join ' | ')"
        }
        $el.GetCurrentPattern($Invoke).Invoke()
        Write-Host "clicked   : '$($el.Current.Name)'" -ForegroundColor Green
        Start-Sleep -Milliseconds 1500
        Write-Evidence -Root (Wait-AppRoot) -Label $Label -Note "after clicking button '$($el.Current.Name)'"
    }

    'kill' {
        Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
        Start-Sleep -Milliseconds 800
        $left = (Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Measure-Object).Count
        Write-Host "instances remaining: $left" -ForegroundColor Green
    }
}
