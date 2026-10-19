# Walk every reachable surface of MTM_Waitlist and audit each one for controls a person cannot identify.
#
# WHY THIS SHAPE
#   "Give the unnamed controls a name" is only answerable against the whole surface, and the surface is not one
#   screen: it is the sign-in window, the shell's three pages, the card list, a request's detail page, every step
#   of the new-request wizard, and the flyouts that hang off the shell. Auditing one of them and calling the job
#   done is how the unnamed buttons on the Settings page stay unnamed.
#
#   So the walk is DATA - a list of steps, each a navigation and the anchor to settle on - and each step ends in a
#   full audit whose report lands beside the others. The summary at the end is the answer to "is anything left
#   unnamed", and the per-surface files are the evidence for each one.
#
# NON-MUTATING BY CONSTRUCTION
#   Every step navigates, expands, or opens. Nothing in the walk accepts, cancels, completes, gives back, submits,
#   or saves: a walk that changes the data it is inspecting cannot be run twice against the same store. The one
#   exception is deliberate and stated here - the walk signs in, because the shell is behind the sign-in stage.
#
# USAGE
#   $env:MTM_WAITLIST_DEV_PIN = '<pin>'      # or -Pin
#   pwsh -File scripts/ui-automation/uia-surface-walk.ps1
#   pwsh -File scripts/ui-automation/uia-surface-walk.ps1 -KeepOpen -EvidenceDir "$env:TEMP\surface"
#   pwsh -File scripts/ui-automation/uia-surface-walk.ps1 -FailOnFindings      # exit 1 when any surface has an unnamed control
#
# EXIT CODES
#   0  every surface was audited; with -FailOnFindings, only when nothing was unnamed
#   1  -FailOnFindings was set and a surface has an unnamed control
#   2  the walk could not reach a surface it was asked to audit

[CmdletBinding()]
param(
    # The account to sign in as. The store holds more than one person, and the one whose rows this machine can drive
    # is the person signed in here, not a name baked into a committed script: a hardcoded name meant the walk signed
    # in as an account this workstation does not hold and then reported the shell as never appearing, which reads like
    # a broken app rather than a wrong argument. MTM_WAITLIST_DEV_USER overrides it, -User overrides both.
    [string]$User = $(if ($env:MTM_WAITLIST_DEV_USER) { $env:MTM_WAITLIST_DEV_USER } else { $env:USERNAME }),

    # Read from the environment rather than defaulted, for the same reason ui-settings-drive.ps1 does: this file is
    # committed, and a working PIN in source is a credential in source.
    [string]$Pin = $env:MTM_WAITLIST_DEV_PIN,

    [string]$EvidenceDir = "$env:TEMP\surface-walk",

    [switch]$KeepOpen,

    [switch]$FailOnFindings,

    # The work centre whose new-request wizard the walk opens, and the items whose answer pages it visits. Two
    # journeys, because the answer page belongs to the item.
    [string]$WorkCenter = '100-03',
    [string]$Item = 'Die',
    [string]$SecondItem = 'Pickup Hopper (do not return)',
    [string]$Die = 'FGT0279-02',

    [string]$Exe = ''
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\uia-common.ps1"

if ([string]::IsNullOrWhiteSpace($Pin)) {
    throw 'Set MTM_WAITLIST_DEV_PIN, or pass -Pin, to the account this walk signs in with.'
}

if ([string]::IsNullOrWhiteSpace($Exe)) {
    $Exe = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe'))
}

$audit = Join-Path $PSScriptRoot 'uia-surface-audit.ps1'

# In-app navigation is decided in under a second; sixty seconds per missed control is how a walk spends seven
# minutes reporting that it is lost. The launch and sign-in waits keep the longer budget, because those really are
# waiting on a store.
$script:InAppWaitSeconds = 12

New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null

# Each step: a label, how to get there, and what to settle on. 'What' is one of id | name | item | none.
#
# The order matters and is not arbitrary: the walk is one journey, so a step that leaves the list (the detail page)
# has to come back before a step that needs the list again. A step that cannot find its control is reported as not
# reached rather than silently skipped, because "not reached" and "nothing unnamed there" are different answers.
$steps = @(
    @{ Label = '10-signin';               What = 'none'; Target = '';                                          Anchor = 'SignInWindow_Identifier'; Note = 'the sign-in stage, before signing in' }
    @{ Label = '11-waitlist';             What = 'none'; Target = '';                                          Anchor = 'WaitlistViewPage_CardList'; Note = 'the card list' }
    @{ Label = '12-detail';               What = 'name'; Target = 'Open details';                              Anchor = ''; Note = "a request's detail page" }
    @{ Label = '13-waitlist-again';       What = 'item'; Target = 'Waitlist,Back';                             Anchor = 'WaitlistViewPage_CardList'; Note = 'back on the card list' }
    @{ Label = '14-wizard-workcenter';    What = 'name'; Target = 'Add Waitlist Item';                         Anchor = ''; Note = 'the new-request wizard, work centre step' }
    @{ Label = '15-wizard-category';      What = 'item'; Target = $WorkCenter;                                 Anchor = ''; Note = 'the wizard, category step' }
    @{ Label = '16-wizard-item';          What = 'item'; Target = 'Pickup';                                    Anchor = ''; Note = 'the wizard, item step' }
    @{ Label = '17-wizard-pick';          What = 'item'; Target = $Item;                                       Anchor = ''; Note = 'the wizard, pick step' }
    @{ Label = '18-wizard-tiles';         What = 'item'; Target = $Die;                                        Anchor = ''; Note = 'the wizard, tile step' }
    @{ Label = '19-wizard-summary';       What = 'name'; Target = 'Continue';                                  Anchor = ''; Note = 'the wizard, summary step' }
    @{ Label = '20-waitlist-again';       What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'leaving the wizard without submitting' }

    # A second journey, because the answer page belongs to the ITEM: the die flow shows the die page, and the hopper
    # flow shows the hopper page with its size, weight and skid answers. One journey would leave four of the
    # wizard's pages unaudited while the walk reported that the surface was covered.
    @{ Label = '21-wizard2-workcenter';   What = 'name'; Target = 'Add Waitlist Item';                         Anchor = ''; Note = 'the wizard again, for the hopper item' }
    @{ Label = '22-wizard2-category';     What = 'item'; Target = $WorkCenter;                                 Anchor = ''; Note = 'the wizard, category step' }
    @{ Label = '23-wizard2-pick';         What = 'item'; Target = 'Pickup';                                    Anchor = ''; Note = 'the wizard, pick step' }
    @{ Label = '24-wizard2-answer';       What = 'item'; Target = $SecondItem;                                 Anchor = ''; Note = 'the hopper answer page' }
    # An answer, then the rest of the flow, so the pages BEHIND the hopper answers are audited rather than assumed.
    # Continue is gated on an answer here, so clicking it without choosing one leaves the walk auditing the same
    # page again and calling it coverage.
    @{ Label = '24a-wizard2-size';        What = 'radio'; Target = '';                                        Anchor = ''; Note = 'the hopper page, with a size chosen' }
    @{ Label = '24b-wizard2-next';        What = 'name'; Target = 'Continue';                                  Anchor = ''; Note = 'the wizard, next step behind the hopper answers' }
    @{ Label = '24c-wizard2-next';        What = 'name'; Target = 'Continue';                                  Anchor = ''; Note = 'the wizard, next step again' }
    @{ Label = '25-waitlist-again';       What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'leaving the second journey' }

    # One flyout per turn, with the list re-selected in between. An open flyout light-dismisses the next click, so a
    # walk that opens three in a row audits the first one three times - which is exactly what it did before the
    # repeat detection above caught it.
    @{ Label = '26-sort-flyout';          What = 'name'; Target = 'Sort by';                                   Anchor = ''; Note = 'the sort flyout' }
    @{ Label = '26a-close-flyout';        What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'closing the flyout' }
    @{ Label = '27-facility-flyout';      What = 'id';   Target = 'ShellPage_FacilitySelectorDefault';        Anchor = ''; Note = 'the facility flyout'; Retry = $true }
    @{ Label = '27a-close-flyout';        What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'closing the flyout' }
    @{ Label = '28-status-flyout';        What = 'status'; Target = '';                                        Anchor = ''; Note = "the handler's condition flyout" }
    @{ Label = '28a-close-flyout';        What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'closing the flyout' }
    @{ Label = '29-user-flyout';          What = 'id';   Target = 'ShellPage_CurrentUserButtonDefault';       Anchor = ''; Note = 'the signed-in-user flyout'; Retry = $true }
    @{ Label = '29a-close-flyout';        What = 'item'; Target = 'Waitlist';                                  Anchor = 'WaitlistViewPage_CardList'; Note = 'closing the flyout' }
    @{ Label = '30-workcenter-setup';     What = 'item'; Target = 'Work Center Setup';                         Anchor = ''; Note = 'the work centre setup page' }
    @{ Label = '31-settings';             What = 'item'; Target = 'Settings';                                  Anchor = ''; Note = 'the settings page' }
)

function Invoke-Step {
    param($Step)

    switch ($Step.What) {
        'none' { }
        'id' { [void](Wait-MtmById -Id $Step.Target -What 'control') }
        # Several candidates, tried in order: 'Waitlist,Back' is "the nav entry, or the page's own Back button".
        'name' {
            foreach ($candidate in ($Step.Target -split ',')) {
                try {
                    [void](Wait-MtmNamed -Name $candidate.Trim() -ControlType 'Button' -What "button '$($candidate.Trim())'" -TimeoutSeconds $script:InAppWaitSeconds)
                    return
                }
                catch { continue }
            }
            throw "no button matched [$($Step.Target)]"
        }
        'item' {
            foreach ($candidate in ($Step.Target -split ',')) {
                try {
                    [void](Wait-MtmNamed -Name $candidate.Trim() -ControlType 'ListItem' -What "list item '$($candidate.Trim())'" -TimeoutSeconds $script:InAppWaitSeconds)
                    return
                }
                catch { continue }
            }
            throw "no list item matched [$($Step.Target)]"
        }
        'status' { }
        # The answer pages gate Continue behind an answer, so the walk gives one: the first offered choice is enough
        # to move the flow on, and choosing it changes nothing that is saved.
        'radio' {
            $condition = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::RadioButton)

            $picked = $false
            foreach ($window in Get-MtmAppWindows) {
                $radios = @($window.FindAll($UiaScope::Descendants, $condition)) | Where-Object { $_.Current.IsEnabled }
                if ($radios.Count -eq 0) { continue }
                Invoke-MtmElement -Element $radios[0] -What "radio '$($radios[0].Current.Name)'"
                Wait-MtmUiSettled -Root (Get-MtmAppRoot)
                $picked = $true
                break
            }
            if (-not $picked) { throw 'no answer choice to pick' }
        }
    }
}

# By accessible name, tried exact then prefix then contains - the order ui-drive.ps1 uses, so 'Die' cannot steal a
# card called 'Die 9003-A' when an exact match exists.
function Wait-MtmNamed {
    param([string]$Name, [string]$ControlType, [string]$What, [int]$TimeoutSeconds = 0)

    if ($TimeoutSeconds -le 0) { $TimeoutSeconds = $MtmWaitSeconds }

    $type = [System.Windows.Automation.ControlType]::$ControlType
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $type)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($window in Get-MtmAppWindows) {
            $found = @($window.FindAll($UiaScope::Descendants, $condition))
            foreach ($pass in @('exact', 'prefix', 'contains')) {
                foreach ($element in $found) {
                    $n = $element.Current.Name
                    if ([string]::IsNullOrWhiteSpace($n)) { continue }
                    $hit = switch ($pass) {
                        'exact' { $n -eq $Name }
                        'prefix' { $n -like "$Name*" }
                        'contains' { $n -like "*$Name*" }
                    }
                    if ($hit) {
                        Invoke-MtmElement -Element $element -What $What
                        Wait-MtmUiSettled -Root (Get-MtmAppRoot)
                        return $element
                    }
                }
            }
        }
        Start-Sleep -Milliseconds $MtmPollMilliseconds
    }
    throw "No $ControlType named '$Name' appeared within $TimeoutSeconds s."
}

# The condition flyout hangs off the one card whose status word this person is allowed to set, so the walk finds the
# enabled status-word button rather than guessing which card it is.
function Open-StatusFlyout {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'WaitlistLineCardView_StatusWordButton')

    foreach ($window in Get-MtmAppWindows) {
        $buttons = @($window.FindAll($UiaScope::Descendants, $condition)) | Where-Object { $_.Current.IsEnabled }
        if ($buttons.Count -gt 0) {
            Invoke-MtmElement -Element $buttons[0] -What 'the handler status word button'
            Wait-MtmUiSettled -Root (Get-MtmAppRoot)
            return $true
        }
    }
    return $false
}

# Runs the audit for one step and reads the two numbers this walk reasons about out of it: how many findings it
# reported, and the page's control signature.
#
# The signature is the interactive count and the mix. Two steps with the same signature are very probably the same
# page, and a walk that counts a page twice reports coverage it does not have - which is exactly what happened when
# Continue was clicked on a page that would not advance until an answer was chosen.
function Invoke-Audit {
    param($Step)

    $auditArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $audit, '-Label', $Step.Label, '-EvidenceDir', $EvidenceDir)
    if ($Step.Anchor) { $auditArgs += @('-AnchorId', $Step.Anchor) }
    $output = & pwsh @auditArgs 2>&1 | Out-String

    $count = 0
    $line = ($output -split "`n" | Where-Object { $_ -match '^findings' } | Select-Object -First 1)
    if ($line -match 'findings\s*:\s*(\d+)') { $count = [int]$Matches[1] }

    $signatureLine = ($output -split "`n" | Where-Object { $_ -match '^interactive\s*:' } | Select-Object -First 1)
    $signature = if ($signatureLine) { $signatureLine.Trim() } else { '' }

    return [pscustomobject]@{ Count = $count; Signature = $signature }
}

# --- launch and sign in ---------------------------------------------------------------------------------------

Write-Host 'launching' -ForegroundColor Cyan
if (-not (Stop-MtmApp)) { throw 'A previous instance would not exit.' }
Start-Process -FilePath $Exe
[void](Wait-MtmById -Id 'SignInWindow_Identifier' -What 'the sign-in field')

# --- the walk -------------------------------------------------------------------------------------------------

$surfaceFindings = New-Object System.Collections.ArrayList
$unreachable = New-Object System.Collections.ArrayList
$repeats = New-Object System.Collections.ArrayList
$signedIn = $false

foreach ($step in $steps) {

    if ($step.Label -eq '11-waitlist' -and -not $signedIn) {
        Write-Host 'signing in' -ForegroundColor Cyan
        Set-MtmText -Element (Wait-MtmById -Id 'SignInWindow_Identifier' -What 'identifier field') -Text $User
        Invoke-MtmElement -Element (Wait-MtmById -Id 'SignInWindow_ContinueButton' -What 'continue button') -What 'Continue'
        Set-MtmText -Element (Wait-MtmById -Id 'SignInWindow_Credential' -What 'PIN field') -Text $Pin
        Invoke-MtmElement -Element (Wait-MtmById -Id 'SignInWindow_SignInButton' -What 'sign-in button') -What 'Sign in'
        try {
            [void](Wait-MtmShell -TimeoutSeconds 120)
        }
        catch {
            # The usual cause is not the app: a credential the store does not accept leaves the app sitting on the
            # sign-in stage, and the bare "shell never appeared" reads like a launch failure. Name the account and the
            # way to change it, because nothing else in the output distinguishes the two.
            throw "Signed in as '$User', and the shell did not appear within 120 s. That account or PIN was refused, or this computer has no configuration. Pass -User or set MTM_WAITLIST_DEV_USER to the account the store holds."
        }
        $signedIn = $true
    }

    try {
        if ($step.What -eq 'status') {
            if (-not (Open-StatusFlyout)) {
                [void]$unreachable.Add("$($step.Label): no card offers a status word this person may set")
                continue
            }
        }
        else {
            Invoke-Step -Step $step
        }
    }
    catch {
        [void]$unreachable.Add("$($step.Label): $($_.Exception.Message)")
        continue
    }

    $result = Invoke-Audit -Step $step
    $repeatOf = ''
    if ($result.Signature -and $script:lastSignature -eq $result.Signature) { $repeatOf = $script:lastLabel }

    # A step whose page did not change is retried once. The header flyouts are the reason: an open flyout
    # light-dismisses the next click, so a click that was meant to open one is eaten by the one still closing. The
    # second click has nothing to dismiss, and the retry only happens where the step asked for it.
    if ($repeatOf -and $step.Retry) {
        Write-Host "  retrying: the click did not open anything (still the page from $repeatOf)" -ForegroundColor DarkYellow
        try {
            Invoke-Step -Step $step
            $result = Invoke-Audit -Step $step
            $repeatOf = ''
            if ($result.Signature -and $script:lastSignature -eq $result.Signature) { $repeatOf = $script:lastLabel }
        }
        catch {
            [void]$unreachable.Add("$($step.Label) (retry): $($_.Exception.Message)")
        }
    }

    $script:lastSignature = $result.Signature
    $script:lastLabel = $step.Label
    $count = $result.Count

    Write-Host ('{0,-22} {1,-2} finding(s)  {2}{3}' -f $step.Label, $count, $step.Note,
        $(if ($repeatOf) { "   [same page as $repeatOf]" } else { '' })) `
        -ForegroundColor $(if ($count) { 'Yellow' } elseif ($repeatOf) { 'DarkYellow' } else { 'Green' })

    if ($repeatOf) { [void]$repeats.Add($step.Label) }

    if ($count -gt 0) {
        [void]$surfaceFindings.Add([pscustomobject]@{ Step = $step.Label; Count = $count; Report = (Join-Path $EvidenceDir "$($step.Label).txt") })
    }
}

if (-not $KeepOpen) {
    if (Stop-MtmApp) { Write-Host 'the app was closed' -ForegroundColor DarkGray }
}

# --- summary --------------------------------------------------------------------------------------------------

$total = ($surfaceFindings | Measure-Object -Property Count -Sum).Sum
if (-not $total) { $total = 0 }

Write-Host ''
Write-Host "surfaces audited : $($steps.Count - $unreachable.Count) of $($steps.Count)  (distinct pages: $($steps.Count - $unreachable.Count - $repeats.Count))" -ForegroundColor Cyan
Write-Host "findings         : $total across $(@($surfaceFindings).Count) surface(s)" -ForegroundColor $(if ($total) { 'Yellow' } else { 'Green' })

foreach ($r in $repeats) {
    Write-Host "  SAME PAGE AS THE STEP BEFORE: $r - the step did not navigate, so treat its audit as a repeat" -ForegroundColor DarkYellow
}

foreach ($s in $surfaceFindings) {
    Write-Host "  $($s.Step): $($s.Count) finding(s) in $($s.Report)" -ForegroundColor Yellow
    Select-String -Path $s.Report -Pattern 'unnamed|generic-name|name-is-the-id' |
        ForEach-Object { Write-Host "      $($_.Line.Trim())" -ForegroundColor DarkYellow }
}

foreach ($u in $unreachable) {
    Write-Host "  NOT REACHED: $u" -ForegroundColor Magenta
}

if ($FailOnFindings -and $total -gt 0) { exit 1 }
if ($unreachable.Count -gt 0 -and -not $surfaceFindings.Count) { exit 2 }
exit 0
