<#
.SYNOPSIS
    The control prompt for the on-host MTM_Waitlist.Mock.Service.

.DESCRIPTION
    This is what the desktop shortcut ("MTM mock cache service") opens: a short prompt that says what the
    cache service is doing and lets an operator show its window, restart it, or shut it down.

    It is deliberately one prompt rather than three shortcuts. Each choice is described where it is
    offered, so the person running it can see what it does before choosing it, and a stray double-click
    changes nothing at all: with no choice made, the script closes without touching the service.

    Nothing here needs elevation. Nothing here reads or writes the service's configuration, its
    credential or its backups - a restart reuses exactly the same stored state.

.PARAMETER Action
    Skip the prompt and run one action: ShowUi, Restart or ShutDown. Intended for scripted use.

.PARAMETER TargetPath
    The install folder the service runs from.

.PARAMETER TimeoutSeconds
    How long to wait for the API port after the service is started.

.EXAMPLE
    # what the desktop shortcut runs
    pwsh -NoProfile -ExecutionPolicy Bypass -File .\mock-service-control.ps1

.EXAMPLE
    # run one action without the prompt
    pwsh -NoProfile -ExecutionPolicy Bypass -File .\mock-service-control.ps1 -Action Restart
#>
[CmdletBinding()]
param(
    # ShowUi, Restart or ShutDown. Omit to get the prompt.
    [ValidateSet('ShowUi', 'Restart', 'ShutDown')]
    [string] $Action,

    # The install folder the service runs from.
    [string] $TargetPath = 'C:\Services\MTM_Waitlist.Mock.Service',

    # How long to wait for the API port after a start.
    [int] $TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$serviceExeName = 'MTM_Waitlist.Mock.Service.exe'
$processName = 'MTM_Waitlist.Mock.Service'
$apiPort = 5760
$secretNames = @(
    'MTM_WAITLIST_DB_CONNECTION_STRING'
    'MTM_MYSQL_PASSWORD'
    'INFOR_VISUAL_SQL_USER'
    'INFOR_VISUAL_SQL_PASSWORD'
)
$userEnvironmentKey = 'HKCU:\Environment'

$script:failed = $false

function Write-Step {
    param([string] $Status, [string] $Step, [string] $Detail)
    $colour = switch ($Status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Yellow' } }
    Write-Host ("  [{0}] {1,-22} {2}" -f $Status, $Step, $Detail) -ForegroundColor $colour
    if ($Status -eq 'FAIL') { $script:failed = $true }
}

function Test-ServiceRunning {
    return [bool](Get-Process -Name $processName -ErrorAction SilentlyContinue)
}

function Import-ServiceSecrets {
    # A process started by double-clicking inherits the environment block captured when the desktop
    # started, so the User-scope values have to be pulled in here or the service comes up with no
    # credentials and fails every read.
    $persisted = Get-ItemProperty $userEnvironmentKey -ErrorAction SilentlyContinue
    $missing = [System.Collections.Generic.List[string]]::new()

    foreach ($name in $secretNames) {
        $value = if ($persisted) { $persisted.PSObject.Properties[$name].Value } else { $null }
        if (-not $value) {
            $missing.Add($name)
            continue
        }

        Set-Item -Path "Env:$name" -Value $value
    }

    if ($missing.Count -gt 0) {
        Write-Step 'WARN' 'secrets' "not set at User scope: $($missing -join ', ') - the service will start but its database work will fail"
    }
}

function Wait-ForApiPort {
    param([bool] $Listening)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $bound = [bool](Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue)
        if ($bound -eq $Listening) { return $true }
        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Stop-ServiceProcess {
    $running = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) {
        Write-Step 'PASS' 'stop' 'the service was not running'
        return
    }

    foreach ($process in $running) { $process | Stop-Process -Force }

    # The service exits only on an explicit tray Quit, so this is a forced stop; and a WinUI process can
    # linger briefly, so poll instead of sampling once.
    $leftover = @()
    $deadline = (Get-Date).AddSeconds(30)
    do {
        $leftover = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
        if ($leftover.Count -eq 0) { break }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    if ($leftover.Count -eq 0) {
        Write-Step 'PASS' 'stop' "stopped process $($running.Id -join ', ')"
    }
    else {
        Write-Step 'FAIL' 'stop' "still running: $($leftover.Id -join ', ')"
    }
}

function Get-ServiceExePath {
    $exePath = Join-Path $TargetPath $serviceExeName

    if (-not (Test-Path $exePath)) {
        Write-Step 'FAIL' 'locate' "there is no service at $exePath"
        return $null
    }

    return $exePath
}

function Start-ServiceWindow {
    # The service opens its window only when it is asked to, so this is a launch (or, when it is already
    # running, a launch that hands the request over to the running copy) with the switch that asks.
    $exePath = Get-ServiceExePath
    if (-not $exePath) { return }

    $wasRunning = Test-ServiceRunning

    if (-not $wasRunning) {
        Import-ServiceSecrets
    }

    Start-Process -FilePath $exePath -ArgumentList '--open-status' -WorkingDirectory $TargetPath

    if ($wasRunning) {
        Write-Step 'PASS' 'show ui' 'asked the running service to show its window'
        Write-Host '       (look at the taskbar; the window opens on the Status page)' -ForegroundColor DarkGray
        return
    }

    if (Wait-ForApiPort -Listening $true) {
        Write-Step 'PASS' 'show ui' "started the service, window opening (port $apiPort is listening)"
    }
    else {
        Write-Step 'FAIL' 'show ui' "the service did not start listening on $apiPort within $TimeoutSeconds s"
    }
}

function Start-ServiceProcess {
    $exePath = Get-ServiceExePath
    if (-not $exePath) { return }

    Import-ServiceSecrets
    Start-Process -FilePath $exePath -WorkingDirectory $TargetPath

    if (Wait-ForApiPort -Listening $true) {
        Write-Step 'PASS' 'start' "running, listening on $apiPort"
    }
    else {
        Write-Step 'FAIL' 'start' "nothing is listening on $apiPort after $TimeoutSeconds s"
    }
}

function Show-Menu {
    Write-Host ''
    Write-Host '  MTM mock cache service' -ForegroundColor Cyan
    Write-Host '  ----------------------'
    Write-Host ''
    Write-Host '  1. Show the UI' -ForegroundColor White
    Write-Host '     Opens the service window, where you can see what the cache service is doing'
    Write-Host '     and change its settings. Nothing is stopped or restarted.'
    Write-Host ''
    Write-Host '  2. Restart the service' -ForegroundColor White
    Write-Host '     Stops the service and starts it again. Use this after a deployment, or when the'
    Write-Host '     cached data looks stale and you want it rebuilt now. The window does not open by'
    Write-Host '     itself - choose 1 as well if you want to watch it.'
    Write-Host ''
    Write-Host '  3. Shut the service down' -ForegroundColor White
    Write-Host '     Stops the service. The applications that read the cached data keep working, but'
    Write-Host '     nothing refreshes that data and no backups are taken until it is started again.'
    Write-Host '     It is set to start when you sign in, so it comes back at your next sign-in unless'
    Write-Host '     you turn that off on the window''s Settings page (choose 1 to open it).'
    Write-Host ''
    Write-Host '  Type 1, 2 or 3 and press Enter. Press Enter on its own to close this without' -ForegroundColor DarkGray
    Write-Host '  changing anything.' -ForegroundColor DarkGray
    Write-Host ''
}

function Read-Choice {
    $answer = Read-Host '  Choice'

    switch ($answer.Trim()) {
        '1' { return 'ShowUi' }
        '2' { return 'Restart' }
        '3' { return 'ShutDown' }
        default { return $null }
    }
}

function Invoke-Action {
    param([string] $RequestedAction)

    Write-Host ''
    switch ($RequestedAction) {
        'ShowUi' { Start-ServiceWindow }
        'Restart' {
            Stop-ServiceProcess
            # The stop releases the port; give the runtime a moment before binding it again.
            Start-Sleep -Seconds 2
            Start-ServiceProcess
        }
        'ShutDown' { Stop-ServiceProcess }
    }

    Write-Host ''
    if ($script:failed) {
        Write-Host '  That did not work - see the lines above.' -ForegroundColor Red
        Write-Host '  The service logs only to a debugger, so if this repeats, check that nothing else' -ForegroundColor Yellow
        Write-Host '  holds port 5760 and look for MTM_Waitlist.Mock.Service errors in the Windows Event Log.' -ForegroundColor Yellow
        return 1
    }

    if ($RequestedAction -eq 'ShutDown') {
        Write-Host '  The service is stopped. It starts again the next time you sign in.' -ForegroundColor Green
    }
    else {
        Write-Host '  Done.' -ForegroundColor Green
    }

    return 0
}

# --- main --------------------------------------------------------------------------------------

if ($Action) {
    exit (Invoke-Action -RequestedAction $Action)
}

Show-Menu
$chosen = Read-Choice

if (-not $chosen) {
    Write-Host ''
    Write-Host '  Nothing was changed.' -ForegroundColor DarkGray
    exit 0
}

$code = Invoke-Action -RequestedAction $chosen

# Keep the result on screen when this was started by double-clicking rather than from a shell.
try {
    Write-Host ''
    Write-Host '  Press any key to close...' -ForegroundColor DarkGray
    [void]$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
catch {
    # No interactive console to wait on; the exit code is the result.
}

exit $code
