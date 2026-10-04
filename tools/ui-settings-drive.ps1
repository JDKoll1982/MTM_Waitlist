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
#   it is what makes the failure reviewable.
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
#   0  the app was still running after visiting Settings
#   1  the app died while Settings was opening (the crash this script exists to catch)

[CmdletBinding()]
param(
    [string]$User = 'jkoll',

    [string]$Pin = $env:MTM_WAITLIST_DEV_PIN,

    [int]$WaitSeconds = 180,

    # How long to let the page settle after the Settings item is selected, before asking whether the app is alive.
    [int]$SettleSeconds = 6,

    [switch]$KeepOpen,

    [string]$EvidenceDir = "$env:TEMP\mtm-settings-walk",

    [string]$Exe = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes

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

$Scope = [System.Windows.Automation.TreeScope]
$CT    = [System.Windows.Automation.ControlType]
$Inv   = [System.Windows.Automation.InvokePattern]::Pattern
$Val   = [System.Windows.Automation.ValuePattern]::Pattern
$Sel   = [System.Windows.Automation.SelectionItemPattern]::Pattern

New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
$log = New-Object System.Collections.ArrayList
function Say([string]$line) {
    Write-Host $line
    [void]$log.Add($line)
}

function Get-Windows {
    $p = Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { return @() }
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
    $wins = [System.Windows.Automation.AutomationElement]::RootElement.FindAll($Scope::Children, $cond)
    $out = @(); foreach ($w in $wins) { $out += $w }
    return $out
}

function Find-ById($root, [string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $root.FindFirst($Scope::Descendants, $c)
}

function Get-Texts($root) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $CT::Text)
    return @($root.FindAll($Scope::Descendants, $c) | ForEach-Object { $_.Current.Name } | Where-Object { $_ })
}

# --- launch -------------------------------------------------------------------------------------------------
Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600
Remove-Item Env:MTM_WAITLIST_DB_CONNECTION_STRING -ErrorAction SilentlyContinue
Remove-Item Env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING -ErrorAction SilentlyContinue

if (-not (Test-Path $Exe)) { throw "Executable not found: $Exe" }
Start-Process -FilePath $Exe
Say 'launched'

# --- walk to the shell --------------------------------------------------------------------------------------
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$signedIn = $false
$shell = $null
$lastStatus = ''

while ((Get-Date) -lt $deadline) {
    foreach ($w in (Get-Windows)) {
        $st = Find-ById $w 'SplashPage_Status'
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
            Start-Sleep -Seconds 2
            continue
        }

        $name = Find-ById $w 'SignInWindow_SignInName'
        if ($name -and -not $signedIn) {
            $name.GetCurrentPattern($Val).SetValue($User)
            (Find-ById $w 'SignInWindow_Credential').GetCurrentPattern($Val).SetValue($Pin)
            (Find-ById $w 'SignInWindow_SignInButton').GetCurrentPattern($Inv).Invoke()
            $signedIn = $true
            Say 'signed in'
            Start-Sleep -Seconds 2
            continue
        }

        if (Find-ById $w 'ShellPage_Navigation') { $shell = $w; break }
    }
    if ($shell) { break }
    Start-Sleep -Milliseconds 400
}

if (-not $shell) {
    Say 'FAIL: the shell never appeared.'
    Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Stop-Process -Force
    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    exit 1
}

$shell = Get-Windows | Where-Object { Find-ById $_ 'ShellPage_Navigation' } | Select-Object -First 1
Say "shell ready: '$($shell.Current.Name)'"

# --- open Settings ------------------------------------------------------------------------------------------
$settings = Find-ById $shell 'SettingsItem'
if (-not $settings) {
    Say 'FAIL: no element with automation id SettingsItem in the shell.'
    Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Stop-Process -Force
    $log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8
    exit 1
}

Say "selecting settings: '$($settings.Current.Name)'"
$settings.GetCurrentPattern($Sel).Select()
Say 'settings selected'

Start-Sleep -Seconds $SettleSeconds

# --- did it survive? ----------------------------------------------------------------------------------------
$p = Get-Process MTM_Waitlist -ErrorAction SilentlyContinue
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
Say "RESULT: ALIVE - $($texts.Count) text elements on the page"
$texts | Select-Object -First 25 | ForEach-Object { Say "  $_" }
$log | Set-Content (Join-Path $EvidenceDir 'walk.txt') -Encoding utf8

if (-not $KeepOpen) {
    Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Stop-Process -Force
}

exit 0
