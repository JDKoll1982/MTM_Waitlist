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
#   pwsh -File scripts/ui-automation/ui-drive.ps1 -Action launch
#   pwsh -File scripts/ui-automation/ui-drive.ps1 -Action dump     -Label 01-workcenter
#   pwsh -File scripts/ui-automation/ui-drive.ps1 -Action click-item   -Name '100-07'        -Label 02-workcenter-picked
#   pwsh -File scripts/ui-automation/ui-drive.ps1 -Action click-button -Name 'Add'          -Label 03-add-request
#   pwsh -File scripts/ui-automation/ui-drive.ps1 -Action kill
#
#   -Name is a match pattern, tried in order: exact, then prefix, then contains. Pass several comma-separated
#   candidates to try in preference order: -Name 'Pickup Die,Pickup Die:,Die'.
#
# TIMING
#   Nothing here sleeps for a guessed interval. Every wait polls the app for the thing it is waiting for - a
#   control, the shell, or the page itself becoming still - at 120 ms, and the step after a click continues as soon
#   as the page has stopped redrawing rather than after a flat 1.5 s. The primitives live in uia-common.ps1, which
#   this script dot-sources, so the other driving scripts wait the same way.
#
#   -NoEvidence skips the evidence dump. The dump walks the whole tree, so a walk that is only driving (not
#   recording) runs markedly faster with it; the default keeps the record.

[CmdletBinding()]
param(
    [ValidateSet('launch', 'dump', 'click-item', 'click-button', 'click-id', 'kill', 'signin', 'wait-for-id')]
    [string]$Action = 'dump',

    [string[]]$Name,

    # Sign-in credentials. Passed in rather than defaulted: this file is committed, and a working PIN in source is
    # a credential in source.
    [string]$Identifier,
    [string]$Pin,

    # The automation id -Action wait-for-id waits for, and prints the whole subtree of when it appears.
    [string]$AutomationId,

    [string]$Label = 'step',

    [string]$EvidenceDir = "$env:TEMP\die-walk",

    [int]$WaitSeconds = 90,

    # Skip the evidence dump: a driving-only step does not need the tree walked three times.
    [switch]$NoEvidence,

    [string]$Exe = "$PSScriptRoot\..\..\bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe",

    # Left empty by default and taken from the environment on launch: this machine already carries
    # MTM_WAITLIST_DB_CONNECTION_STRING at user scope, so the credential never has to appear on a command
    # line or in this file.
    [string]$ConnectionString = ''
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\uia-common.ps1"

$CT     = $UiaCT
$Scope  = $UiaScope
$Invoke = $UiaInvoke

# This script's own wait budget, which the shared helpers honour.
$MtmWaitSeconds = $WaitSeconds

function Get-AppProcess { Get-MtmAppProcess }

function Get-AppRoot { Get-MtmAppRoot }

function Wait-AppRoot { Wait-MtmWindow -TimeoutSeconds $WaitSeconds }

# Anything that CLICKS waits for the real shell rather than the splash: the splash is a window too and answers
# MainWindowHandle seconds before the shell exists, so a click aimed at it finds nothing and the whole chain after
# it fails. The shell is recognised by its own navigation pane - the sign-in window has caption buttons as well, so
# "has caption buttons" would call the sign-in window a shell. (-Action dump deliberately does not wait: seeing the
# splash is occasionally the point.)
function Wait-ShellRoot { Wait-MtmShell -TimeoutSeconds $WaitSeconds }

# One walk of the tree, grouped by control type. The evidence used to be written with a separate FindAll per control
# type, which walked the whole tree three times per step.
function Get-WindowInventory {
    param($Root)

    $byType = @{}
    foreach ($el in @($Root.FindAll($Scope::Descendants, $UiaAny))) {
        $name = $el.Current.Name
        if ([string]::IsNullOrWhiteSpace($name)) { continue }

        $type = $el.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')
        if (-not $byType.ContainsKey($type)) { $byType[$type] = New-Object System.Collections.ArrayList }
        [void]$byType[$type].Add($name)
    }

    return $byType
}

function Get-ByControlType {
    param($Root, $ControlType)

    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType)
    $Root.FindAll($Scope::Descendants, $c) | ForEach-Object { $_.Current.Name } | Where-Object { $_ }
}

function Write-Evidence {
    param($Root, [string]$Label, [string]$Note = '')

    if ($NoEvidence) { return }
    if (-not $Root) { $Root = Wait-AppRoot }

    New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
    $path = Join-Path $EvidenceDir ("{0}.txt" -f $Label)

    $inventory = Get-WindowInventory -Root $Root
    $items   = @($inventory['ListItem'])
    $texts   = @($inventory['Text'])
    $buttons = @($inventory['Button'])
    $edits   = @($inventory['Edit'])

    $body = @(
        "# $Label"
        "when      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "window    : '$(($Root.Current.Name))'  hwnd=$($Root.Current.NativeWindowHandle)"
        if ($Note) { "note      : $Note" }
        ''
        "## ListItems ($($items.Count))"
        $items
        ''
        "## Buttons ($($buttons.Count))"
        $buttons
        ''
        "## Edits ($($edits.Count))"
        $edits
        ''
        "## Texts ($($texts.Count))"
        $texts
    )
    $body | Set-Content -Path $path -Encoding utf8

    Write-Host "evidence  : $path" -ForegroundColor DarkGray
    Write-Host "items     : $($items.Count)  [$($items -join ' | ')]" -ForegroundColor Cyan
    Write-Host "buttons   : $($buttons.Count)  [$($buttons -join ' | ')]" -ForegroundColor Cyan
    Write-Host "edits     : $($edits.Count)  [$($edits -join ' | ')]" -ForegroundColor DarkGray
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

function Get-AppWindows { Get-MtmAppWindows }

function Find-ById { param($Root, [string]$Id) Find-MtmById -Root $Root -Id $Id }

# By id across every window the app owns, because which window is "main" changes as the app moves from splash to
# sign-in to shell.
function Wait-ById { param([string]$Id, [string]$What = 'control') Wait-MtmById -Id $Id -What $What -TimeoutSeconds $WaitSeconds }

# Typed into the control the way a person types rather than written straight to the model, so anything the field
# does on input (validation, enabling the button, clearing an error) actually happens.
function Set-Text { param($Element, [string]$Text) Set-MtmText -Element $Element -Text $Text }

function Invoke-ById {
    param([string]$Id, [string]$What = 'button')

    $el = Wait-ById -Id $Id -What $What
    Invoke-MtmElement -Element $el -What $Id
    Write-Host "pressed   : $Id" -ForegroundColor Green
    return $el
}

# The app's signed-in landing place, which is what every later click is rooted at.
function Get-WindowOwning {
    param([string]$Id)

    return @(Get-AppWindows | Where-Object { Find-ById -Root $_ -Id $Id }) | Select-Object -First 1
}

switch ($Action) {

    'launch' {
        if (Get-AppProcess) {
            Write-Host 'The app is already running - not starting a second instance.' -ForegroundColor Yellow
        }
        else {
            if (-not (Test-Path $Exe)) { throw "Executable not found: $Exe" }

            # The app needs both: the plain one for the stores, and the STARTUP_ variant to clear its startup
            # session check. Taken from this process's environment when -ConnectionString was not passed, which is
            # how the value reaches the app without ever being written down.
            if (-not $ConnectionString) { $ConnectionString = $env:MTM_WAITLIST_DB_CONNECTION_STRING }
            if (-not $ConnectionString) {
                throw 'No connection string. Pass -ConnectionString or set MTM_WAITLIST_DB_CONNECTION_STRING.'
            }

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
        # A navigation entry is selected rather than invoked, so this goes through the same fallback a button does.
        Invoke-MtmElement -Element $el -What "list item '$($el.Current.Name)'"
        Write-Host "clicked   : '$($el.Current.Name)'" -ForegroundColor Green
        # Wait for the page to stop redrawing instead of sleeping a guess: a card rebuild takes 0.05-1.2 s, and a
        # fixed 1200 ms charged every step for the slowest one.
        Wait-MtmUiSettled -Root (Get-AppRoot)
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
        Invoke-MtmElement -Element $el -What "button '$($el.Current.Name)'"
        Write-Host "clicked   : '$($el.Current.Name)'" -ForegroundColor Green
        Wait-MtmUiSettled -Root (Get-AppRoot)
        Write-Evidence -Root (Wait-AppRoot) -Label $Label -Note "after clicking button '$($el.Current.Name)'"
    }

    'click-id' {
        # For the controls a name-based find cannot reach: an unnamed button is invisible to Find-Element, so the
        # id is the only handle on it. Necessary while such a control exists, and useful afterwards for the ones
        # whose name is localised.
        if (-not $AutomationId) { throw 'click-id needs -AutomationId.' }
        $el = Wait-ById -Id $AutomationId -What 'control'
        Invoke-MtmElement -Element $el -What "control '$AutomationId'"
        Write-Host "clicked   : $AutomationId  ('$($el.Current.Name)')" -ForegroundColor Green
        Wait-MtmUiSettled -Root (Get-AppRoot)
        Write-Evidence -Root (Wait-AppRoot) -Label $Label -Note "after clicking '$AutomationId'"
    }

    'signin' {
        if (-not $Identifier) { throw 'signin needs -Identifier.' }
        if (-not $Pin) { throw 'signin needs -Pin.' }

        # Two stages, and the second field does not exist until the first is submitted. Each control is found by
        # automation id rather than by name: the fields carry no accessible name until validation has run once,
        # so a name-based find reports nothing on a clean sign-in.
        Set-Text -Element (Wait-ById -Id 'SignInWindow_Identifier' -What 'identifier field') -Text $Identifier
        Write-Host "typed     : identifier" -ForegroundColor Green
        Invoke-ById -Id 'SignInWindow_ContinueButton' | Out-Null

        Set-Text -Element (Wait-ById -Id 'SignInWindow_Credential' -What 'credential field') -Text $Pin
        Write-Host "typed     : pin" -ForegroundColor Green
        Invoke-ById -Id 'SignInWindow_SignInButton' | Out-Null

        # Sign-in reaches the store, so this is the slow step and the one that can fail on its own.
        $root = Wait-ShellRoot
        Write-Host "signed in : shell is up" -ForegroundColor Green
        Write-Evidence -Root $root -Label $Label -Note 'after signing in'
    }

    'wait-for-id' {
        if (-not $AutomationId) { throw 'wait-for-id needs -AutomationId.' }
        $el = Wait-ById -Id $AutomationId -What 'control'
        Write-Host "found     : $AutomationId  ('$($el.Current.Name)')" -ForegroundColor Green

        # Evidence comes from the window that owns the control, not the desktop root: the root's descendants are
        # every window on the machine, which is neither useful nor quick to walk.
        Write-Evidence -Root (Get-WindowOwning -Id $AutomationId) -Label $Label -Note "after $AutomationId appeared"
    }

    'kill' {
        $stopped = Stop-MtmApp
        $left = (Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Measure-Object).Count
        if (-not $stopped) {
            Write-Host 'the process did not exit within 15 s' -ForegroundColor Yellow
        }
        Write-Host "instances remaining: $left" -ForegroundColor Green
    }
}
