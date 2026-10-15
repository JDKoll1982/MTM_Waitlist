# Drive MTM_Waitlist from a cold launch to the Settings page and report whether the app survives the visit.
#
# WHY THIS SHAPE
#   The script the crash was first chased with waited on an automation id of 'Settings', which the shell never
#   exposes: the navigation pane's Settings entry is the built-in NavigationView item and reports 'SettingsItem'.
#   The wait therefore expired and the walk never opened the page it was written to open. Two further traps are
#   fixed here as well, and both cost a run each when they were learnt:
#
#     1. The sign-in window carries Minimize/Maximize/Close, so a 'has caption buttons' shell test calls the
#        sign-in window a shell and stops before the pane exists. The shell is recognised by its own navigation
#        element, 'ShellPage_Navigation'.
#     2. The store connection string must NOT be overridden. appsettings.json names the shared store; pointing
#        MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING at a local database that does not exist makes the launch decide
#        this computer is unconfigured and the app opens the machine-setup window instead of sign-in.
#
#   The walk is assisted: a failing assertion here is evidence about the app, and the evidence file written beside
#   it is what makes the failure reviewable. The check at the end is about RESPONSIVENESS as well as existence:
#   an app whose window has stopped answering is not "alive" in any sense that matters, and reporting it as alive
#   is how a hang gets mistaken for a pass.
#
# USAGE
#   $env:MTM_WAITLIST_DEV_PIN = '<pin>'
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/ui-settings-drive.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/ui-settings-drive.ps1 -KeepOpen
#
# The PIN is read from MTM_WAITLIST_DEV_PIN rather than typed as a parameter, so a development credential does
# not end up in a command line or in a shell's history. -Pin still overrides it for a one-off run.
#
# EXIT CODES
#   0  the app was still running AND answering after visiting Settings
#   1  the app died, or stopped answering, while Settings was opening (the failure this script exists to catch)
#   2  the walk never reached Settings

[CmdletBinding()]
param(
    [string]$User = 'jkoll',

    [string]$Pin = $env:MTM_WAITLIST_DEV_PIN,

    [int]$WaitSeconds = 180,

    # How long the app is given to stay up and answering after the Settings item is selected. This is a soak, not a
    # settle: the page's own readiness is waited for by polling, and this period is how long the app is watched
    # afterwards for a crash or a hang that only shows itself once the page is live.
    [int]$SettleSeconds = 6,

    [switch]$KeepOpen,

    [string]$EvidenceDir = "$env:TEMP\mtm-settings-walk",

    [string]$Exe = ''
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\uia-common.ps1"

if ([string]::IsNullOrWhiteSpace($Pin))
{
    throw 'Set MTM_WAITLIST_DEV_PIN, or pass -Pin, to the account this walk signs in with.'
}

# $PSScriptRoot is not populated when it is used as a parameter default in this host, so the default is resolved
# here instead of in the param block.
if ([string]::IsNullOrWhiteSpace($Exe)) {
    $Exe = Join-Path (Split-Path -Parent $PSCommandPath) '..\bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe'
    $Exe = [System.IO.Path]::GetFullPath($Exe)
}

$Scope = $UiaScope
$CT    = $UiaCT
$Inv   = $UiaInvoke
$Val   = $UiaValue
$Sel   = $UiaSelect

New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
$log = New-Object System.Collections.ArrayList
function Say([string]$line) {
    Write-Host $line
    [void]$log.Add($line)
}

function Get-Windows { @(Get-MtmAppWindows) }

function Find-ById($root, [string]$id) { Find-MtmById -Root $root -Id $id }

function Get-Texts($root) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $CT::Text)
    return @($root.FindAll($Scope::Descendants, $c) | ForEach-Object { $_.Current.Name } | Where-Object { $_ })
}

# --- launch -------------------------------------------------------------------------------------------------
[void](Stop-MtmApp)
Remove-Item Env:MTM_WAITLIST_DB_CONNECTION_STRING -ErrorAction SilentlyContinue
Remove-Item Env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING -ErrorAction SilentlyContinue

if (-not (Test-Path $Exe)) { throw "Executable not found: $Exe" }
Start-Process -FilePath $Exe
Say 'launched'

# --- walk to the shell --------------------------------------------------------------------------------------
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$signedIn = $false
$identifierDone = $false
$shell = $null
$lastStatus = ''

while ((Get-Date) -lt $deadline) {
    foreach ($w in (Get-Windows)) {
        $st = Find-ById $w 'SplashPage_Progress'
        if ($st -and $st.Current.Name -ne $lastStatus) {
            $lastStatus = $st.Current.Name
            Say "splash: $lastStatus"
        }

        $np = Find-ById $w 'SignInWindow_NewPassword'
        if ($np) {
            $np.GetCurrentPattern($Val).SetValue($Pin)
            (Find-ById $w 'SignInWindow_ConfirmPassword').GetCurrentPattern($Val).SetValue($Pin)
            (Find-ById $w 'SignInWindow_SubmitPasswordButton').GetCurrentPattern($Inv).Invoke()
            Say 'answered the set-a-PIN prompt'
            Wait-MtmUiSettled -Root $w
            continue
        }

        # The sign-in stage takes the identifier first: type it, confirm it, then the PIN stage appears.
        $identifier = Find-ById $w 'SignInWindow_Identifier'
        if ($identifier -and -not $identifierDone) {
            $identifier.GetCurrentPattern($Val).SetValue($User)
            (Find-ById $w 'SignInWindow_ContinueButton').GetCurrentPattern($Inv).Invoke()
            $identifierDone = $true
            Say 'identifier confirmed'
            Wait-MtmUiSettled -Root $w
            continue
        }

        $credential = Find-ById $w 'SignInWindow_Credential'
        if ($credential -and -not $signedIn) {
            $credential.GetCurrentPattern($Val).SetValue($Pin)
            (Find-ById $w 'SignInWindow_SignInButton').GetCurrentPattern($Inv).Invoke()
            $signedIn = $true
            Say 'signed in'
            Wait-MtmUiSettled -Root $w
            continue
        }

        if (Find-ById $w 'ShellPage_Navigation') { $shell = $w; break }
    }
    if ($shell) { break }
    Start-Sleep -Milliseconds $MtmPollMilliseconds
}

if (-not $shell) {
    Say 'FAIL: the shell never appeared.'
    [void](Stop-MtmApp)
    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    exit 1
}

$shell = Get-Windows | Where-Object { Find-ById $_ 'ShellPage_Navigation' } | Select-Object -First 1
Say "shell ready: '$($shell.Current.Name)'"

# --- open Settings ------------------------------------------------------------------------------------------
$settings = Find-ById $shell 'SettingsItem'
if (-not $settings) {
    Say 'FAIL: no element with automation id SettingsItem in the shell.'
    [void](Stop-MtmApp)
    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    exit 1
}

Say "selecting settings: '$($settings.Current.Name)'"
$settings.GetCurrentPattern($Sel).Select()
Say 'settings selected'

# The soak, watched rather than slept through: the app is asked every 120 ms whether it is still there and still
# answering, so a crash or a hang is reported at the moment it happens instead of after the whole period.
$soakUntil = (Get-Date).AddSeconds($SettleSeconds)
$hungSamples = 0
while ((Get-Date) -lt $soakUntil) {
    $watched = Get-MtmAppProcess
    if (-not $watched) { break }

    if ($watched.Responding) { $hungSamples = 0 } else { $hungSamples++ }
    Start-Sleep -Milliseconds $MtmPollMilliseconds
}

# --- did it survive? ----------------------------------------------------------------------------------------
$p = Get-MtmAppProcess
if (-not $p) {
    Say 'RESULT: CRASHED'

    $fault = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'MTM_Waitlist\faults') -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($fault) {
        Say "fault     : $($fault.Name)"
        Say (Get-Content $fault.FullName -Raw)
        $fault | Copy-Item -Destination (Join-Path $EvidenceDir $fault.Name) -Force
    }

    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    exit 1
}

$root = Get-Windows | Select-Object -First 1
$texts = if ($root) { Get-Texts $root } else { @() }

# A window that exists but does not answer is the failure this walk is most likely to meet, and calling it ALIVE is
# how a hang gets mistaken for a pass. The page is read as well, because a hung window usually still reports its
# last good text.
if ($hungSamples -ge 5) {
    Say "RESULT: HUNG - the window stopped answering during the first $SettleSeconds s after Settings was selected"
    Say "text elements still on the page: $($texts.Count)"
    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    if (-not $KeepOpen) { [void](Stop-MtmApp) }
    exit 1
}

Say "RESULT: ALIVE - $($texts.Count) text elements on the page, answering throughout"
$texts | Select-Object -First 25 | ForEach-Object { Say "  $_" }
$log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8

if (-not $KeepOpen) {
    [void](Stop-MtmApp)
}

exit 0
