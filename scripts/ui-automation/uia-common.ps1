# Shared UI Automation primitives for the MTM_Waitlist driving scripts.
#
# WHY THIS FILE EXISTS
#   Four scripts had grown their own copy of "find the app's window", "wait for the control to appear" and "wait for
#   the page to settle", and the four copies disagreed: two polled every 400 ms, two every 500 ms, one waited on
#   caption buttons (which the sign-in window also has, so it called the sign-in window a shell and stopped), and
#   every script that clicked something then slept a flat 1200-1500 ms and hoped.
#
#   A flat sleep is wrong in both directions. Too short and the walk reads the page before it has redrawn; too long
#   and every step of a fifty-step walk pays for the worst step in it. The app tells us when it is done: the number
#   of elements in the tree stops changing. That is what Wait-MtmUiSettled watches, so a step costs what the app
#   actually takes - measured at 0.05 s to 1.2 s for a card rebuild - instead of a guessed constant.
#
# USAGE
#   . "$PSScriptRoot\uia-common.ps1"
#   $root = Wait-MtmShell                       # the shell, recognised by its own navigation element
#   Invoke-MtmElement -Id 'WaitlistViewPage_AddButton'
#   Wait-MtmUiSettled -Root $root
#
#   The timing knobs below are script-scope by design: a caller that wants a slower or faster walk sets them after
#   dot-sourcing rather than rewriting a helper.

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$UiaScope  = [System.Windows.Automation.TreeScope]
$UiaCT     = [System.Windows.Automation.ControlType]
$UiaAny    = [System.Windows.Automation.Condition]::TrueCondition
$UiaInvoke = [System.Windows.Automation.InvokePattern]::Pattern
$UiaValue  = [System.Windows.Automation.ValuePattern]::Pattern
$UiaSelect = [System.Windows.Automation.SelectionItemPattern]::Pattern

# How long between two looks at the tree. 120 ms is below the time a person notices and above the cost of a UIA
# walk, so polling is cheaper than waiting.
$MtmPollMilliseconds = 120

# How many consecutive identical readings mean "the page has stopped changing", and how long to keep asking.
$MtmSettleSamples        = 2
$MtmSettleTimeoutSeconds = 6

# ...and how long the tree must stay unchanged before the page counts as settled. A sample count is not enough on
# its own: a page that has emptied itself to rebuild is momentarily still, so two quick identical readings called it
# settled and the walk read a list that had not been drawn yet. Requiring quiet for a period does not have that
# hole - the rebuild's own changes push the deadline out.
$MtmStableMilliseconds = 600

# The default ceiling for "wait until this exists". Individual waits still accept their own.
$MtmWaitSeconds = 60

# The shell is recognised by its own navigation pane, not by its caption buttons: the sign-in window has caption
# buttons too, and a walk that mistook it for the shell stopped before the shell existed.
$MtmShellAutomationId = 'ShellPage_Navigation'

$MtmProcessName = 'MTM_Waitlist'

function Get-MtmAppProcess {
    Get-Process $MtmProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
}

# Every top-level window the app owns. The sign-in window, the machine-setup window, the splash and the shell are
# separate windows, and which one is "main" changes as the app moves between them.
function Get-MtmAppWindows {
    param([string]$TitlePattern = '*')

    $process = Get-MtmAppProcess
    if (-not $process) { return @() }

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id)

    return @([System.Windows.Automation.AutomationElement]::RootElement.FindAll($UiaScope::Children, $condition)) |
        Where-Object { $_.Current.Name -like $TitlePattern }
}

function Get-MtmAppRoot {
    $process = Get-MtmAppProcess
    if (-not $process) { return $null }
    if ($process.MainWindowHandle -eq 0) { return $null }

    [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
}

function Find-MtmById {
    param($Root, [string]$Id)

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)

    return $Root.FindFirst($UiaScope::Descendants, $condition)
}

# By id across every window, because which window is "main" changes as the app moves from splash to sign-in to
# shell, and a control the walk is waiting for may arrive in a window that did not exist when the wait began.
function Wait-MtmById {
    param(
        [string]$Id,
        [string]$What = 'control',
        [int]$TimeoutSeconds = 0,
        [int]$PollMilliseconds = 0)

    if ($TimeoutSeconds -le 0) { $TimeoutSeconds = $MtmWaitSeconds }
    if ($PollMilliseconds -le 0) { $PollMilliseconds = $MtmPollMilliseconds }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($window in Get-MtmAppWindows) {
            $element = Find-MtmById -Root $window -Id $Id
            if ($element) { return $element }
        }
        Start-Sleep -Milliseconds $PollMilliseconds
    }

    $titles = @(Get-MtmAppWindows | ForEach-Object { $_.Current.Name })
    throw "No $What with automation id '$Id' appeared within $TimeoutSeconds s. Windows: $($titles -join ' | ')"
}

function Wait-MtmWindow {
    param([int]$TimeoutSeconds = 0, [int]$PollMilliseconds = 0)

    if ($TimeoutSeconds -le 0) { $TimeoutSeconds = $MtmWaitSeconds }
    if ($PollMilliseconds -le 0) { $PollMilliseconds = $MtmPollMilliseconds }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $root = Get-MtmAppRoot
        if ($root) { return $root }
        Start-Sleep -Milliseconds $PollMilliseconds
    }
    throw "No main window appeared within $TimeoutSeconds s."
}

function Wait-MtmShell {
    param(
        [string]$ShellAutomationId = '',
        [int]$TimeoutSeconds = 0,
        [int]$PollMilliseconds = 0)

    if (-not $ShellAutomationId) { $ShellAutomationId = $MtmShellAutomationId }
    if ($TimeoutSeconds -le 0) { $TimeoutSeconds = $MtmWaitSeconds }
    if ($PollMilliseconds -le 0) { $PollMilliseconds = $MtmPollMilliseconds }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($window in Get-MtmAppWindows) {
            if (Find-MtmById -Root $window -Id $ShellAutomationId) { return $window }
        }
        Start-Sleep -Milliseconds $PollMilliseconds
    }
    throw "The shell ('$ShellAutomationId' never appeared) did not come up within $TimeoutSeconds s."
}

# How many elements the window currently holds. Cheap enough to sample, and it moves whenever the page redraws -
# by itself when a card rebuilds, and when a page navigates.
function Get-MtmElementCount {
    param($Root)

    if (-not $Root) { return -1 }
    try { return @($Root.FindAll($UiaScope::Descendants, $UiaAny)).Count } catch { return -1 }
}

# The replacement for a flat sleep: watch the tree until it has stopped changing for a period, then hand control
# back. A page that is already settled costs that period; a page that is still rebuilding costs exactly as long as
# the rebuild takes, because every change pushes the quiet deadline out.
function Wait-MtmUiSettled {
    param(
        $Root,
        [int]$TimeoutSeconds = 0,
        [int]$StableMilliseconds = 0,
        [int]$PollMilliseconds = 0)

    if ($TimeoutSeconds -le 0) { $TimeoutSeconds = $MtmSettleTimeoutSeconds }
    if ($StableMilliseconds -le 0) { $StableMilliseconds = $MtmStableMilliseconds }
    if ($PollMilliseconds -le 0) { $PollMilliseconds = $MtmPollMilliseconds }
    if (-not $Root) { return }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastCount = -1
    $lastChange = Get-Date

    while ((Get-Date) -lt $deadline) {
        $count = Get-MtmElementCount -Root $Root

        if ($count -ne $lastCount) {
            $lastCount = $count
            $lastChange = Get-Date
        }
        elseif (((Get-Date) - $lastChange).TotalMilliseconds -ge $StableMilliseconds) {
            return
        }

        Start-Sleep -Milliseconds $PollMilliseconds
    }
}

# Waits for an automation id to exist and then for the page to settle. The anchor is what makes an audit honest on a
# page that loads asynchronously: 'the page is still' means nothing until the page that was asked for has arrived.
function Wait-MtmSettledAnchor {
    param(
        [string]$AnchorId,
        [int]$TimeoutSeconds = 0,
        [int]$StableMilliseconds = 0)

    if ($AnchorId) {
        $anchor = Wait-MtmById -Id $AnchorId -What 'page anchor' -TimeoutSeconds $TimeoutSeconds
        Wait-MtmUiSettled -Root $anchor -StableMilliseconds $StableMilliseconds
        return $anchor
    }

    $root = Get-MtmAppRoot
    Wait-MtmUiSettled -Root $root -StableMilliseconds $StableMilliseconds
    return $root
}

# Press whatever the element is. A button accepts Invoke; a list row or navigation entry is *selected*, which is a
# different pattern with a different method - calling Invoke on a SelectionItemPattern is how the first version of
# this failed. The fallback matters because the navigation pane's entries are not Invoke-capable, and a walk that
# assumed Invoke reported "Unsupported Pattern" for a row that was perfectly clickable.
function Invoke-MtmElement {
    param($Element, [string]$What = 'element')

    if (-not $Element) { throw "Nothing to invoke for $What." }

    $pattern = $null
    $ok = $false
    try { $ok = $Element.TryGetCurrentPattern($UiaInvoke, [ref]$pattern) } catch { $ok = $false }
    if ($ok) {
        $pattern.Invoke()
        return
    }

    $pattern = $null
    $ok = $false
    try { $ok = $Element.TryGetCurrentPattern($UiaSelect, [ref]$pattern) } catch { $ok = $false }
    if ($ok) {
        $pattern.Select()
        return
    }

    throw "The $What ('$($Element.Current.Name)', $($Element.Current.ControlType.ProgrammaticName)) accepts neither Invoke nor SelectionItem."
}

function Set-MtmText {
    param($Element, [string]$Text)

    if (-not $Element) { throw 'No element to type into.' }
    $pattern = $Element.GetCurrentPattern($UiaValue)
    $pattern.SetValue($Text)
}

# Waits for the process to actually be gone, rather than sleeping a guess and hoping.
function Stop-MtmApp {
    param([int]$TimeoutSeconds = 15)

    Get-Process $MtmProcessName -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (-not (Get-MtmAppProcess)) { return $true }
        Start-Sleep -Milliseconds $MtmPollMilliseconds
    }
    return $false
}
