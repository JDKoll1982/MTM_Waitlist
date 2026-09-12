<#
.SYNOPSIS
    Installs or redeploys MTM_Waitlist.Mock.Service onto the Infor Visual / MySQL cache host.

.DESCRIPTION
    One idempotent deployment step for the host half of the Module_Mock cached fallback. It:

      0. asks what you are trying to do, when it is run with no argument at all (see GUIDED MODE below);
      1. reports what this machine can reach - whether it is the cache host, whether Infor Visual answers, and
         whether the MySQL host answers (see ANY HOST below);
      2. stops the running service process, if any;
      3. deletes the previous deployment folder (state under %LOCALAPPDATA% is kept unless -PurgeState);
      4. publishes (with -Publish) and/or copies the publish output to the install folder;
      5. verifies the copied deployment is complete and self-contained;
      6. installs and verifies the environment secrets the service resolves at runtime;
      7. starts the service and health-checks it (tray-only, port bound, API refusing unauthenticated
         calls, single-instance redirect);
      8. puts one shortcut on the operator's desktop that opens the service's control prompt - show the
         window, restart, or shut down - replacing any shortcut of that name so a redeploy cannot leave a
         stale target behind;
      9. prints a PASS/FAIL summary and exits non-zero if anything failed.

    The equivalent manual procedure is MTM_Waitlist.Mock.Service/README.md sections 2-4; that file
    remains the narrative authority, this script is the executable form of it.

.GUIDED MODE - NO ARGUMENT
    Run this file with NO argument (double-click it, right-click - Run with PowerShell, or just
    `.\install-mock-service.ps1`) and it asks what you are trying to do instead of guessing: deploy,
    publish-then-deploy, install to another folder, or a clean sweep that also deletes the service's stored
    state. It then asks the four independent questions (publish, secrets, start and health-check, shortcut)
    and one destructive one (delete the stored state), shows a summary with the equivalent command line, and
    waits for a yes/no before touching anything.

    **Passing ANY argument skips the prompts entirely**, so the scriptable path and the hand-run path are the
    same code with the same exit codes. `-NonInteractive` is the explicit way to say "no prompts, use the
    defaults" when no other argument is needed.

    In guided mode the script waits for Enter after the summary, so a window that closes on exit still lets
    the result be read.

.ANY HOST - READ THIS BEFORE RUNNING
    The service installs on any machine. What it can DO there is decided by reachability, not by the
    installation: it refreshes the mtm_mock mirror only where Infor Visual answers, and backs up or restores
    only the stores it can connect to. On a machine that reaches neither, it still serves the mirror over the
    API and the tray - which is exactly what a workstation needs.

    This script therefore REPORTS rather than refuses. It never fails a deployment because the machine is not
    the cache host: the cache host is named by the MySQL `Server=` value in appsettings.json (default
    `172.16.1.104`, verified 2026-09-11 as `V-MTMFG-5`), and a machine that is not it is installed as a
    mirror-only host with a warning that says so.

    The per-store answer is the service's own: it probes each store at startup and every minute, disables the
    work it cannot do, and reports the reason on the status surface and in `GET /api/status`.

.EXAMPLE
    # the ordinary way: ask me what I am trying to do
    .\install-mock-service.ps1

.EXAMPLE
    # the normal automation case: publish fresh, deploy, install/verify secrets, start and health-check
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -Publish

.EXAMPLE
    # redeploy an existing publish output without touching the service's stored state
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -NonInteractive

.EXAMPLE
    # a clean sweep: also delete the service's configuration, credential and backups
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -PurgeState
#>
[CmdletBinding()]
param(
    # Install folder. Must be final before the first run: a first run registers auto-start with the path it was
    # launched from. Omitted, the script chooses: the established machine-level folder when this account can
    # create it, otherwise the per-user folder - see INSTALL FOLDER below.
    [string] $TargetPath,

    # Publish output to deploy from. Defaults to the project's folder-publish profile output.
    [string] $SourcePath,

    # Expected cache host. Defaults to the MySQL Server= value in appsettings.json. Used only to report
    # whether this machine IS that host; it never refuses a deployment.
    [string] $ServerHost,

    # Port the Infor Visual reachability report probes. Named instances resolve through the SQL Browser
    # service, which a TCP probe cannot see, so this is configurable rather than inferred.
    [int] $VisualPort = 1433,

    # Run `dotnet publish` (Release, win-x64-selfcontained) before copying.
    [switch] $Publish,

    # Do not set or verify the environment secrets.
    [switch] $SkipSecrets,

    # Do not start the service or run the health checks.
    [switch] $SkipServiceStart,

    # Also delete %LOCALAPPDATA%\MTM_Waitlist.Mock.Service - configuration, the DPAPI credential and backups.
    [switch] $PurgeState,

    # The desktop that receives the restart shortcut. Hardcoded to the operator account by owner decision.
    [string] $DesktopPath = 'C:\Users\jkoll\Desktop',

    # Additional desktops that also receive the shortcut, best effort: a profile that is not on this machine
    # is skipped and an unwritable folder is a warning, never a failure. Hardcoded to the second operator
    # profile by owner request (the agent's notes record jkoll at work, johnk at home).
    [string[]] $AdditionalDesktopPaths = @('C:\Users\johnk\Desktop'),

    # Do not create or replace the desktop shortcut.
    [switch] $SkipDesktopShortcut,

    # Skip the guided prompts even when no other argument is passed. Passing ANY argument also skips them,
    # so this exists to say "I mean the defaults" out loud in automation.
    [switch] $NonInteractive,

    # Seconds to wait for the service to start listening.
    [int] $HealthCheckTimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$projectPath = Join-Path $repoRoot 'MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.csproj'
$serviceExeName = 'MTM_Waitlist.Mock.Service.exe'
$processName = 'MTM_Waitlist.Mock.Service'
$runKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$runValueName = 'MTM_Waitlist.Mock.Service'
$stateRoot = Join-Path $env:LOCALAPPDATA 'MTM_Waitlist.Mock.Service'
$apiPort = 5760
$controlScriptName = 'mock-service-control.ps1'
# The pane's Status entry uses a BitmapIcon, so the PNG has to ship with the app. Without it the icon
# silently renders as nothing rather than failing loudly, so the deploy guards treat it as required.
$statusIconRelativePath = 'Assets\Icons\service-status.png'

if (-not $SourcePath) {
    $SourcePath = Join-Path $repoRoot 'MTM_Waitlist.Mock.Service\bin\publish\win-x64'
}

$script:results = [System.Collections.Generic.List[object]]::new()

# Set by the guided workflow below; read by the summary so a window that was opened by double-clicking the
# script is not closed before the result can be read.
$script:guidedMode = $false

function Add-Result {
    param([string] $Step, [string] $Status, [string] $Detail)
    $script:results.Add([pscustomobject]@{ Step = $Step; Status = $Status; Detail = $Detail })
    $colour = switch ($Status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Yellow' } }
    Write-Host ("  [{0}] {1,-28} {2}" -f $Status, $Step, $Detail) -ForegroundColor $colour
}

function Stop-Deployment {
    param([string] $Message, [int] $ExitCode)
    Write-Host ''
    Write-Host $Message -ForegroundColor Red
    Write-Host ''
    Write-Host '=== SUMMARY ===' -ForegroundColor Cyan
    $script:results | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
    Wait-GuidedClose
    exit $ExitCode
}

function Test-Failed {
    return [bool]($script:results | Where-Object { $_.Status -eq 'FAIL' })
}

function Test-TcpEndpoint {
    param([string] $HostName, [int] $Port, [int] $TimeoutMilliseconds = 3000)

    if (-not $HostName) { return $false }

    $client = $null
    try {
        $client = [System.Net.Sockets.TcpClient]::new()
        $connect = $client.ConnectAsync($HostName, $Port)
        if (-not $connect.Wait($TimeoutMilliseconds)) { return $false }
        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        if ($client) { $client.Dispose() }
    }
}

# ---------------------------------------------------------------------------------------------
# Install folder
#
# The installed location is a machine-level folder, which is right on the cache host where the service is
# installed once for the machine. It is not always usable: an account may not be able to create it, and a
# service that runs per user belongs in a per-user folder anyway. So the default is RESOLVED rather than
# assumed, by testing whether this account can actually create the folder it intends to use, and the choice
# and the reason are reported in the banner so nobody has to guess which one is in play.
# ---------------------------------------------------------------------------------------------
$establishedInstallFolder = 'C:\Services\MTM_Waitlist.Mock.Service'
$perUserInstallFolder = Join-Path $env:LOCALAPPDATA 'Programs\MTM_Waitlist.Mock.Service'

$targetPathGiven = [bool]$PSBoundParameters['TargetPath']
$targetPathSource = 'given with -TargetPath'

function Test-CanUseFolder {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }

    try {
        New-Item -ItemType Directory -Path $Path -Force -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

# ---------------------------------------------------------------------------------------------
# Is the publish output stale?
#
# Deploying what is already on disk is the fast path, and it is the right one when the output is current.
# It is the wrong one the moment a source or asset changes - the copy succeeds, the deployment validates,
# and the missing piece only shows up as a FAIL in step 5 or as a service that cannot find its icon. So the
# question is answered before the copy, and the guided workflow defaults to rebuilding when the answer is yes.
# ---------------------------------------------------------------------------------------------
function Test-PublishOutputStale {
    param([string] $OutputPath)

    if (-not (Test-Path $OutputPath)) { return $true }

    # Missing content is unambiguous: the output cannot be current if it does not hold the files the project
    # says must ship beside the app.
    foreach ($requiredInOutput in @($serviceExeName, 'Assets\mock-service.ico', $statusIconRelativePath)) {
        if (-not (Test-Path (Join-Path $OutputPath $requiredInOutput))) { return $true }
    }

    $newestSource = Get-ChildItem -Path (Join-Path $repoRoot 'MTM_Waitlist.Mock.Service') -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension -in @('.cs', '.xaml', '.resw', '.csproj', '.ico', '.png', '.json', '.ps1') `
                -and $_.Name -ne 'install-mock-service.ps1' `
                -and $_.FullName -notmatch '\\(bin|obj)\\'
        } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    $newestOutput = Get-ChildItem -Path $OutputPath -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $newestSource -or -not $newestOutput) { return $false }

    return $newestSource.LastWriteTime -gt $newestOutput.LastWriteTime
}

if ($targetPathGiven) {
    if (-not (Test-CanUseFolder $TargetPath)) {
        Stop-Deployment @"
The install folder cannot be created by this account.

  requested      : $TargetPath
  default instead: $establishedInstallFolder when this machine can create it, otherwise $perUserInstallFolder

Pass a folder this account can write to, or leave -TargetPath off and let the script choose.
"@ 3
    }
}
elseif (Test-Path $establishedInstallFolder) {
    # An existing deployment is never moved: auto-start, the shortcut and the control script all point at it.
    $TargetPath = $establishedInstallFolder
    $targetPathSource = 'default - an existing deployment is already here'
}
elseif (Test-CanUseFolder $establishedInstallFolder) {
    $TargetPath = $establishedInstallFolder
    $targetPathSource = 'default - the machine-level location'
}
else {
    $TargetPath = $perUserInstallFolder
    $targetPathSource = "default - $establishedInstallFolder cannot be created by this account, so the per-user folder is used"

    if (-not (Test-CanUseFolder $TargetPath)) {
        Stop-Deployment @"
No install folder could be used.

  neither '$establishedInstallFolder'
  nor     '$TargetPath'
  could be created by this account. Pass one it can write to with -TargetPath.
"@ 3
    }
}

$resolvedTargetPath = $TargetPath

# ---------------------------------------------------------------------------------------------
# Guided workflow
#
# Running the script with NO argument asks what you are trying to do and builds the same parameter set the
# command line would have. Passing ANY argument skips it entirely, so the scriptable and the
# double-click path are the same code with the same exit codes.
# ---------------------------------------------------------------------------------------------

function Wait-GuidedClose {
    if (-not $script:guidedMode) { return }

    Write-Host ''
    [void](Read-Host 'Press Enter to close')
}

function Read-GuidedChoice {
    param(
        [string] $Caption,
        [string] $Message,
        [System.Management.Automation.Host.ChoiceDescription[]] $Choices,
        [int] $Default = 0
    )

    return $Host.UI.PromptForChoice($Caption, $Message, $Choices, $Default)
}

function Read-GuidedYesNo {
    param([string] $Caption, [string] $Message, [bool] $DefaultYes = $true)

    $choices = @(
        [System.Management.Automation.Host.ChoiceDescription]::new('&Yes', 'Yes'),
        [System.Management.Automation.Host.ChoiceDescription]::new('&No', 'No')
    )

    $default = if ($DefaultYes) { 0 } else { 1 }

    return (Read-GuidedChoice -Caption $Caption -Message $Message -Choices $choices -Default $default) -eq 0
}

function Get-GuidedCommandLine {
    $parts = @('.\install-mock-service.ps1')

    foreach ($pair in @(
        @{ Given = $Publish; Flag = '-Publish' },
        @{ Given = $PurgeState; Flag = '-PurgeState' },
        @{ Given = $SkipSecrets; Flag = '-SkipSecrets' },
        @{ Given = $SkipServiceStart; Flag = '-SkipServiceStart' },
        @{ Given = $SkipDesktopShortcut; Flag = '-SkipDesktopShortcut' }))
    {
        if ($pair.Given) { $parts += $pair.Flag }
    }

    if ($TargetPath -ne $resolvedTargetPath) {
        $parts += "-TargetPath '$TargetPath'"
    }

    return ($parts -join ' ')
}

# Guided mode needs a console that can answer a prompt. A redirected stdin cannot, so it falls through to the
# non-interactive defaults rather than failing on a prompt nobody can see.
$canPrompt = [Environment]::UserInteractive
if ($canPrompt) {
    try { $canPrompt = -not [Console]::IsInputRedirected }
    catch { $canPrompt = $false }
}

if ($PSBoundParameters.Count -eq 0 -and -not $NonInteractive -and $canPrompt) {
    $script:guidedMode = $true

    Write-Host ''
    Write-Host '============================================================' -ForegroundColor Cyan
    Write-Host ' Guided deployment' -ForegroundColor Cyan
    Write-Host '============================================================' -ForegroundColor Cyan
    Write-Host ' No argument was passed, so this asks what you are trying to do.' -ForegroundColor Gray
    Write-Host ' Passing any argument skips these prompts - that is what automation should do.' -ForegroundColor Gray
    Write-Host ''

    $intentChoices = @(
        [System.Management.Automation.Host.ChoiceDescription]::new('&Deploy', 'Deploy the publish output that is already on disk. The service state is kept.'),
        [System.Management.Automation.Host.ChoiceDescription]::new('&Publish and deploy', 'Build a fresh Release publish output first, then deploy it.'),
        [System.Management.Automation.Host.ChoiceDescription]::new('&Clean sweep', 'Deploy and also delete ALL service state: configuration, backups and run records.'),
        [System.Management.Automation.Host.ChoiceDescription]::new('&Quit', 'Change nothing and exit.')
    )

    $intent = Read-GuidedChoice -Caption 'Deployment' -Message 'What are you trying to do?' -Choices $intentChoices -Default 0

    if ($intent -eq 3) {
        Write-Host ''
        Write-Host 'Nothing was changed.' -ForegroundColor Yellow
        Wait-GuidedClose
        exit 0
    }

    Write-Host ''

    # The folder question is asked on every path, with the resolved default already in the prompt, so the
    # default is something you can see and accept rather than something you have to know.
    while ($true) {
        $typedPath = Read-Host "Install folder [$TargetPath]"

        if ([string]::IsNullOrWhiteSpace($typedPath)) { break }

        $candidate = $typedPath.Trim()

        if (Test-CanUseFolder $candidate) {
            $TargetPath = $candidate
            $targetPathSource = 'chosen in the guided workflow'
            break
        }

        Write-Host ("  '{0}' cannot be created by this account. Try another path, or press Enter for {1}." -f $candidate, $TargetPath) -ForegroundColor Yellow
    }

    $publishRecommended = Test-PublishOutputStale -OutputPath $SourcePath

    $Publish = Read-GuidedYesNo `
        -Caption 'Build' `
        -Message $(if ($publishRecommended) {
            'Build a fresh Release publish output first? RECOMMENDED - the output on disk is missing required content or older than the project.'
        }
        else {
            'Build a fresh Release publish output first? Slower, but the deployed build is then the one in the working tree.'
        }) `
        -DefaultYes ($publishRecommended -or ($intent -eq 1))

    $SkipSecrets = -not (Read-GuidedYesNo `
        -Caption 'Secrets' `
        -Message 'Set and verify the MySQL and Infor Visual environment variables on this machine?' `
        -DefaultYes $true)

    $SkipServiceStart = -not (Read-GuidedYesNo `
        -Caption 'Service' `
        -Message 'Start the service and run the health checks once the copy is done?' `
        -DefaultYes $true)

    $SkipDesktopShortcut = -not (Read-GuidedYesNo `
        -Caption 'Shortcut' `
        -Message "Put the restart shortcut on this machine's desktops?" `
        -DefaultYes $true)

    $PurgeState = Read-GuidedYesNo `
        -Caption 'State' `
        -Message "ALSO delete the service's stored state? That removes the configuration, every backup artifact and the run records." `
        -DefaultYes ($intent -eq 2)

    Write-Host ''
    Write-Host '--- what will run ---' -ForegroundColor Cyan
    Write-Host ("  install folder : {0}" -f $TargetPath)
    Write-Host ("                   {0}" -f $targetPathSource) -ForegroundColor Gray
    Write-Host ("  publish first  : {0}" -f $(if ($Publish) { 'yes' } else { 'no - the publish output on disk is deployed as-is' }))
    Write-Host ("  secrets        : {0}" -f $(if ($SkipSecrets) { 'skipped' } else { 'set and verified' }))
    Write-Host ("  service start  : {0}" -f $(if ($SkipServiceStart) { 'skipped' } else { 'started and health-checked' }))
    Write-Host ("  shortcut       : {0}" -f $(if ($SkipDesktopShortcut) { 'skipped' } else { 'placed on this machine' }))
    Write-Host ("  service state  : {0}" -f $(if ($PurgeState) { 'DELETED (configuration, backups, run records)' } else { 'kept' }))
    Write-Host ("  source         : {0}" -f $SourcePath)
    Write-Host ''
    Write-Host '  Equivalent command line:' -ForegroundColor Gray
    Write-Host ("    {0}" -f (Get-GuidedCommandLine)) -ForegroundColor Gray
    Write-Host ''

    if (-not (Read-GuidedYesNo -Caption 'Confirm' -Message 'Proceed with this deployment?' -DefaultYes $true)) {
        Write-Host ''
        Write-Host 'Nothing was changed.' -ForegroundColor Yellow
        Wait-GuidedClose
        exit 0
    }

    Write-Host ''
}

Write-Host ''
Write-Host '============================================================' -ForegroundColor Cyan
Write-Host ' MTM_Waitlist.Mock.Service - install / redeploy' -ForegroundColor Cyan
Write-Host '============================================================' -ForegroundColor Cyan
Write-Host "  repo    : $repoRoot"
Write-Host "  source  : $SourcePath"
Write-Host "  target  : $TargetPath"
Write-Host "            ($targetPathSource)" -ForegroundColor Gray
Write-Host ''

# ---------------------------------------------------------------------------------------------
# 1. Host disposition - what this machine can reach (never fatal)
# ---------------------------------------------------------------------------------------------
Write-Host '--- 1. host disposition ---' -ForegroundColor Cyan

$appSettingsPath = Join-Path $repoRoot 'appsettings.json'
$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
$mysqlConnectionString = $appSettings.StartupDatabaseOptions.ConnectionString
$mysqlHost = ([regex]::Match($mysqlConnectionString, 'Server=([^;]+)')).Groups[1].Value
$mysqlPortMatch = [regex]::Match($mysqlConnectionString, 'Port=([0-9]+)')
$mysqlPort = if ($mysqlPortMatch.Success) { [int]$mysqlPortMatch.Groups[1].Value } else { 3306 }

if (-not $ServerHost) { $ServerHost = $mysqlHost }

$visualServer = $appSettings.InforVisualDatabaseOptions.Server

# The cache-host comparison is REPORTING, not a gate: the service installs anywhere, and what it can do there
# is decided at runtime by what it can reach.
$localAddresses = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -ne '127.0.0.1' } | Select-Object -ExpandProperty IPAddress)

$expectedAddresses = @($ServerHost)
if ($ServerHost -and $ServerHost -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    $expectedAddresses = @(
        try { [System.Net.Dns]::GetHostAddresses($ServerHost) | Where-Object { $_.AddressFamily -eq 'InterNetwork' } | ForEach-Object { $_.IPAddressToString } }
        catch { @() }
    )
}

$serverMatched = [bool](@($localAddresses | Where-Object { $expectedAddresses -contains $_ }).Count)
Write-Host "  this machine : $([System.Net.Dns]::GetHostName())  addresses: $($localAddresses -join ', ')"
Write-Host "  cache host   : $ServerHost  addresses: $($expectedAddresses -join ', ')"

if ($serverMatched) {
    Add-Result 'cache host' 'PASS' "this machine is the cache host ($ServerHost)"
}
else {
    Add-Result 'cache host' 'WARN' "not the cache host ($ServerHost); installing as a mirror-only host - refresh and store work are enabled only where they can run"
}

# Infor Visual: refresh needs it. A TCP probe is the honest expectation-setter here (the service does the real
# probe with the SQL provider); a name that does not resolve and a port that does not answer both mean the same
# thing to an operator - refresh will be disabled on this machine.
if (-not $visualServer) {
    Add-Result 'infor visual' 'WARN' 'no InforVisualDatabaseOptions.Server in appsettings.json; the service will disable refresh here until it is configured'
}
else {
    $visualAddresses = @(
        try { [System.Net.Dns]::GetHostAddresses($visualServer) | Where-Object { $_.AddressFamily -eq 'InterNetwork' } | ForEach-Object { $_.IPAddressToString } }
        catch { @() }
    )

    $visualReachable = [bool](@($visualAddresses | Where-Object { Test-TcpEndpoint -HostName $_ -Port $VisualPort }).Count)
    Write-Host "  infor visual : $visualServer  addresses: $($visualAddresses -join ', ')  port: $VisualPort"

    if ($visualReachable) {
        Add-Result 'infor visual' 'PASS' "$visualServer answered on port $VisualPort; refresh runs on this machine"
    }
    else {
        Add-Result 'infor visual' 'WARN' "$visualServer did not answer on port $VisualPort; the service will disable refresh on this machine"
    }
}

# MySQL: the four stores' backups and restores need it. The per-store answer is the service's own - it probes
# each database, because a reachable server with a missing database is not a store that can be backed up.
if (-not $mysqlHost) {
    Add-Result 'mysql host' 'WARN' 'no MySQL Server= in appsettings.json; the service will disable backup and restore for every store until it is configured'
}
elseif (Test-TcpEndpoint -HostName $mysqlHost -Port $mysqlPort) {
    Add-Result 'mysql host' 'PASS' "$mysqlHost answered on port $mysqlPort; the service probes each store and disables only the ones it cannot reach"
}
else {
    Add-Result 'mysql host' 'WARN' "$mysqlHost did not answer on port $mysqlPort; the service will disable backup and restore for every store it cannot reach"
}


# ---------------------------------------------------------------------------------------------
# 2. Stop the running service
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 2. stop the running service ---' -ForegroundColor Cyan

$running = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
if ($running.Count -eq 0) {
    Add-Result 'stop running service' 'PASS' 'no instance was running'
}
else {
    foreach ($process in $running) { $process | Stop-Process -Force }
    try {
        $running | Wait-Process -Timeout 30 -ErrorAction Stop
    }
    catch {
        # Timed out; the poll below reports the real state. Never fail on the wait alone: a WinUI
        # process can linger briefly after termination, so a single immediate sample is unreliable and
        # reported "1 process survived" while the API port was already free (observed 2026-09-11).
    }

    $leftover = @()
    $stopDeadline = (Get-Date).AddSeconds(15)
    do {
        $leftover = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
        if ($leftover.Count -eq 0) { break }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $stopDeadline)

    if ($leftover.Count -eq 0) {
        Add-Result 'stop running service' 'PASS' "stopped $($running.Count) process(es): $($running.Id -join ', ')"
    }
    else {
        Add-Result 'stop running service' 'FAIL' "$($leftover.Count) process(es) survived: $($leftover.Id -join ', ')"
    }
}

# The design exits only on an explicit tray Quit, so a forced stop is the only scriptable option.
$listeners = @(Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue)
if ($listeners.Count -gt 0) {
    Add-Result 'release API port' 'WARN' "port $apiPort is still held by pid(s) $((@($listeners.OwningProcess) | Sort-Object -Unique) -join ', ')"
}
else {
    Add-Result 'release API port' 'PASS' "port $apiPort is free"
}

# ---------------------------------------------------------------------------------------------
# 3. Delete the previous deployment
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 3. delete the previous deployment ---' -ForegroundColor Cyan

if (Test-Path $TargetPath) {
    $existing = @(Get-ChildItem $TargetPath -Recurse -File -ErrorAction SilentlyContinue)
    try {
        Remove-Item $TargetPath -Recurse -Force -ErrorAction Stop
        Add-Result 'delete previous deployment' 'PASS' "removed $($existing.Count) file(s) from $TargetPath"
    }
    catch {
        Add-Result 'delete previous deployment' 'FAIL' $_.Exception.Message
    }
}
else {
    Add-Result 'delete previous deployment' 'PASS' 'nothing was deployed at that path'
}

if ($PurgeState) {
    if (Test-Path $stateRoot) {
        Remove-Item $stateRoot -Recurse -Force
        Add-Result 'purge service state' 'WARN' "deleted $stateRoot (configuration, credential, backups) - a new credential must be generated"
    }
    else {
        Add-Result 'purge service state' 'PASS' 'no state folder existed'
    }
}
else {
    Add-Result 'keep service state' 'PASS' "state kept at $stateRoot"
}

# ---------------------------------------------------------------------------------------------
# 3b. Upgrade step (T156): drop retired shared-credential material
# ---------------------------------------------------------------------------------------------
# T147 retired the shared API credential, but an install that ran a pre-T147 build still carries the DPAPI
# blob it wrote. The service strips it on load; this also cleans the file — and any backup copy of it — for
# an install that is never re-saved, so the secret cannot outlive the feature that owned it.
if (-not $PurgeState) {
    $configName = 'service-configuration.json'
    $retiredNames = @('CredentialProtected', 'CredentialCreatedUtc', 'Credential')
    $scrubbed = @()

    $candidates = @()
    if (Test-Path $stateRoot) {
        $candidates = @(Get-ChildItem -Path $stateRoot -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "$configName*" })
    }

    foreach ($candidate in $candidates) {
        $json = $null
        try {
            $json = Get-Content -LiteralPath $candidate.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            # Unparseable: leave it alone. The service's own load path also declines to rewrite a file it
            # cannot read, so malformed content stays visible as the clue it is.
            continue
        }

        if ($null -eq $json -or $null -eq $json.Api) { continue }

        $hit = $false
        foreach ($name in $retiredNames) {
            $property = $json.Api.PSObject.Properties | Where-Object { $_.Name -eq $name }
            if ($property) {
                $json.Api.PSObject.Properties.Remove($property.Name)
                $hit = $true
            }
        }

        if ($hit) {
            [System.IO.File]::WriteAllText($candidate.FullName, ($json | ConvertTo-Json -Depth 12))
            $scrubbed += $candidate.Name
        }
    }

    if ($scrubbed.Count -gt 0) {
        Add-Result 'purge retired credential' 'PASS' ("removed from: " + ($scrubbed -join ', '))
    }
    else {
        Add-Result 'purge retired credential' 'PASS' 'no retired credential material found'
    }
}

# ---------------------------------------------------------------------------------------------
# 4. Publish and copy
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 4. publish and deploy ---' -ForegroundColor Cyan

if ($Publish) {
    $publishOutput = & dotnet publish $projectPath -c Release -p:Platform=x64 -p:PublishProfile=win-x64-selfcontained /m:1 /nodeReuse:false 2>&1
    $publishExit = $LASTEXITCODE
    $profileWarning = $publishOutput | Select-String -Pattern 'NETSDK1198'
    if ($publishExit -ne 0) {
        Add-Result 'publish' 'FAIL' "dotnet publish exited $publishExit"
        $publishOutput | Select-Object -Last 15 | ForEach-Object { Write-Host "      $_" }
    }
    elseif ($profileWarning) {
        Add-Result 'publish' 'FAIL' 'publish profile win-x64-selfcontained was not applied (NETSDK1198) - the build would not be self-contained'
    }
    else {
        Add-Result 'publish' 'PASS' "dotnet publish (Release, win-x64-selfcontained) with no warnings"
    }
}
else {
    # Reusing the output is the fast path; a STALE output is not, and saying so here keeps the failure out of
    # step 5, where it would read as a deployment problem rather than an un-built change.
    if (Test-PublishOutputStale -OutputPath $SourcePath) {
        Add-Result 'publish' 'WARN' 'reusing an existing publish output that is missing content or older than the project - re-run with -Publish to rebuild it'
    }
    else {
        Add-Result 'publish' 'SKIP' 'reusing the existing publish output (-Publish not given)'
    }
}

if (-not (Test-Path (Join-Path $SourcePath $serviceExeName))) {
    Stop-Deployment "No publish output at $SourcePath. Run again with -Publish, or pass -SourcePath." 3
}

New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
Copy-Item -Path (Join-Path $SourcePath '*') -Destination $TargetPath -Recurse -Force
Add-Result 'copy to install folder' 'PASS' "copied to $TargetPath"

# ---------------------------------------------------------------------------------------------
# 5. Verify the deployment
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 5. verify the deployment ---' -ForegroundColor Cyan

$sourceFiles = @(Get-ChildItem $SourcePath -Recurse -File)
$targetFiles = @(Get-ChildItem $TargetPath -Recurse -File)
if ($sourceFiles.Count -eq $targetFiles.Count) {
    Add-Result 'file count matches source' 'PASS' "$($targetFiles.Count) files"
}
else {
    Add-Result 'file count matches source' 'FAIL' "source $($sourceFiles.Count) vs target $($targetFiles.Count)"
}

$required = @(
    $serviceExeName,
    $controlScriptName,
    'assets\mock-service.ico',
    $statusIconRelativePath,
    'Database\InforVisual\Queues\Module_Mock\Populations\work_order_lookup_population.sql',
    'Database\InforVisual\Queues\Module_Mock\Populations\operation_sequences_population.sql',
    'Database\InforVisual\Queues\Module_Mock\Populations\subordinate_parts_population.sql',
    'Database\InforVisual\Queues\Module_Mock\Populations\inventory_locations_population.sql',
    'Database\InforVisual\Queues\Module_Mock\Populations\disposition_input_population.sql',
    'Database\InforVisual\Queues\Module_Setup\Queries\LookupWorkOrder.sql',
    'Database\InforVisual\Queues\Module_Waitlist\Queries\GetDispositionInput.sql',
    'Database\Mock.Service\Restore',
    'Microsoft.WindowsAppRuntime.dll'
)
$missing = @($required | Where-Object { -not (Test-Path (Join-Path $TargetPath $_)) })
if ($missing.Count -eq 0) {
    Add-Result 'required content present' 'PASS' "$($required.Count) required paths (scripts, tray icon, restore artifacts)"
}
else {
    Add-Result 'required content present' 'FAIL' "missing: $($missing -join ', ') - the publish output is probably stale; re-run with -Publish"
}

# Self-contained: the host must not need a .NET or Windows App SDK runtime installed.
$runtimeMarkers = @('System.Private.CoreLib.dll', 'hostfxr.dll', 'coreclr.dll')
$missingRuntime = @($runtimeMarkers | Where-Object { -not (Test-Path (Join-Path $TargetPath $_)) })
$runtimeConfig = Join-Path $TargetPath 'MTM_Waitlist.Mock.Service.runtimeconfig.json'
$includedFrameworks = $false
if (Test-Path $runtimeConfig) {
    $includedFrameworks = [bool]((Get-Content $runtimeConfig -Raw | ConvertFrom-Json).runtimeOptions.includedFrameworks)
}
if ($missingRuntime.Count -eq 0 -and $includedFrameworks) {
    Add-Result 'self-contained runtime' 'PASS' "$($runtimeMarkers.Count) runtime markers present; runtimeconfig uses includedFrameworks"
}
else {
    Add-Result 'self-contained runtime' 'FAIL' "missing: $($missingRuntime -join ', '); includedFrameworks=$includedFrameworks"
}

# The corrected work-order addressing guard (T144) must be in the deployed population reads.
$lookupPop = Join-Path $TargetPath 'Database\InforVisual\Queues\Module_Mock\Populations\work_order_lookup_population.sql'
$popText = Get-Content $lookupPop -Raw
if ($popText -match 'SUBSTRING\(LTRIM\(RTRIM\(wo\.BASE_ID\)\), 4, 6\) NOT LIKE' -and $popText -notmatch "RIGHT\('000000'") {
    Add-Result 'population reads current' 'PASS' 'deployed population reads carry the corrected addressable-key guard'
}
else {
    Add-Result 'population reads current' 'FAIL' 'deployed work_order_lookup population does not carry the corrected guard'
}

# ---------------------------------------------------------------------------------------------
# 6. Secrets
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 6. secrets ---' -ForegroundColor Cyan

if ($SkipSecrets) {
    Add-Result 'secrets' 'SKIP' '-SkipSecrets given'
}
else {
    # Values are derived from appsettings.json so this script is not a second source of truth, and
    # the Visual login falls back to the value that file already carries.
    $config = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    $waitlistCs = $config.StartupDatabaseOptions.ConnectionString.Replace('User ID', 'User Id')
    if ($waitlistCs -notmatch 'AllowPublicKeyRetrieval') { $waitlistCs = "$waitlistCs AllowPublicKeyRetrieval=True;" }

    $desired = [ordered]@{
        'MTM_WAITLIST_DB_CONNECTION_STRING' = $waitlistCs
        'MTM_MYSQL_PASSWORD'                = ([regex]::Match($waitlistCs, 'Password=([^;]*)')).Groups[1].Value
        'INFOR_VISUAL_SQL_USER'             = $config.InforVisualDatabaseOptions.User
        'INFOR_VISUAL_SQL_PASSWORD'         = $config.InforVisualDatabaseOptions.Password
    }

    $userEnvironmentKey = 'HKCU:\Environment'
    $persisted = Get-ItemProperty $userEnvironmentKey -ErrorAction SilentlyContinue
    $changed = [System.Collections.Generic.List[string]]::new()
    foreach ($name in $desired.Keys) {
        if (-not $desired[$name]) { Add-Result 'secrets' 'FAIL' "$name has no source value in appsettings.json"; break }

        # Property access on a key that does not exist yields $null, and under Set-StrictMode that is an error
        # rather than a null - which is how a first run on a machine with no secrets set used to abort here.
        $existing = $persisted.PSObject.Properties[$name]
        $current = if ($existing) { $existing.Value } else { $null }

        if ($current -ne $desired[$name]) {
            [Environment]::SetEnvironmentVariable($name, $desired[$name], 'User')
            $changed.Add($name)
        }
    }
    if ($changed.Count -gt 0) {
        Add-Result 'install secrets' 'PASS' "set at User scope: $($changed -join ', ')"
    }
    else {
        Add-Result 'install secrets' 'PASS' 'all four were already set to the expected values'
    }

    # Read back from HKCU\Environment: what a freshly started service will actually inherit.
    $persisted = Get-ItemProperty $userEnvironmentKey
    $bad = @()
    foreach ($name in $desired.Keys) {
        $existing = $persisted.PSObject.Properties[$name]
        $value = if ($existing) { $existing.Value } else { $null }
        if (-not $value -or $value -ne $desired[$name]) { $bad += $name }
    }
    if ($bad.Count -eq 0) {
        Add-Result 'secrets persisted' 'PASS' 'all four read back from HKCU\Environment and match'
    }
    else {
        Add-Result 'secrets persisted' 'FAIL' "mismatched or empty: $($bad -join ', ')"
    }

    # Applying them to THIS process is what makes the started service receive them. Writing a User-scope
    # variable does not update any existing environment block, and Start-Process hands the child a copy of
    # this process's block - so without this the service starts with no credentials and every read fails
    # with "Access denied for user ''@'localhost' (using password: NO)". Learned the hard way, 2026-09-11.
    foreach ($name in $desired.Keys) {
        [Environment]::SetEnvironmentVariable($name, $desired[$name], 'Process')
    }
    $notInProcess = @($desired.Keys | Where-Object {
            [Environment]::GetEnvironmentVariable($_, 'Process') -ne $desired[$_]
        })
    if ($notInProcess.Count -eq 0) {
        Add-Result 'secrets inherited by child' 'PASS' 'this process now carries all four, so the service it starts inherits them'
    }
    else {
        Add-Result 'secrets inherited by child' 'FAIL' "not present in this process: $($notInProcess -join ', ')"
    }
    $effective = $desired

    # Prove they work: the service never runs on a config it cannot connect with.
    $clientBin = Join-Path $repoRoot 'bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64'
    if (-not (Test-Path (Join-Path $clientBin 'MySqlConnector.dll'))) {
        Add-Result 'connection proofs' 'SKIP' "build the solution first: $clientBin does not exist"
    }
    else {
        [void][System.Reflection.Assembly]::LoadFrom((Join-Path $clientBin 'MySqlConnector.dll'))
        [void][System.Reflection.Assembly]::LoadFrom((Join-Path $clientBin 'Microsoft.Data.SqlClient.dll'))

        # Which server these credentials are proved against.
        #
        # The secrets are set from appsettings.json, so they name the plant host. A machine that cannot reach
        # it has NOT been given wrong credentials, and the service falls back to this machine's own MySQL - so
        # the proof follows the same rule: the configured host when it answers, otherwise this machine's server
        # when that answers, otherwise the credentials cannot be proved from here and it is reported as such.
        # Only a proof that actually ran can FAIL.
        $waitlistBuilder = [MySqlConnector.MySqlConnectionStringBuilder]::new($effective['MTM_WAITLIST_DB_CONNECTION_STRING'])
        $configuredHost = $waitlistBuilder.Server
        $configuredPort = if ($waitlistBuilder.Port -gt 0) { [int]$waitlistBuilder.Port } else { 3306 }

        $proofHost = $configuredHost
        $proofSubstitution = $null

        if (-not (Test-TcpEndpoint -HostName $configuredHost -Port $configuredPort)) {
            if (Test-TcpEndpoint -HostName 'localhost' -Port $configuredPort) {
                $proofHost = 'localhost'
                $proofSubstitution = "$configuredHost did not answer, so the credentials were proved against this machine's MySQL, which is what the service falls back to here"
            }
            else {
                $proofHost = $null
            }
        }

        if (-not $proofHost) {
            Add-Result 'MySQL connections' 'WARN' "neither $configuredHost nor this machine's MySQL answered, so the credentials could not be proved from here; the service disables the MySQL work it cannot do"
        }
        else {
            $dbFailures = @()
            foreach ($database in @('mtm_mock', 'mtm_waitlist', 'mtm_wip_application_winforms', 'mtm_receiving_application')) {
                $builder = [MySqlConnector.MySqlConnectionStringBuilder]::new($effective['MTM_WAITLIST_DB_CONNECTION_STRING'])
                $builder.Database = $database
                $builder.Server = $proofHost
                try {
                    $connection = [MySqlConnector.MySqlConnection]::new($builder.ConnectionString)
                    $connection.Open(); $connection.Dispose()
                }
                catch { $dbFailures += "$database ($($_.Exception.Message))" }
            }
            if ($dbFailures.Count -eq 0) {
                $detail = 'mtm_mock, mtm_waitlist, mtm_wip_application_winforms, mtm_receiving_application'
                if ($proofSubstitution) { $detail = "$detail - $proofSubstitution" }
                Add-Result 'MySQL connections' 'PASS' $detail
            }
            else {
                # A reachable server that refused these credentials is a real failure: no fallback can hide a
                # wrong password, and the service would fail the same way.
                Add-Result 'MySQL connections' 'FAIL' ($dbFailures -join '; ')
            }
        }

        $visualServer = $config.InforVisualDatabaseOptions.Server
        $visualAddresses = @(
            try { [System.Net.Dns]::GetHostAddresses($visualServer) | Where-Object { $_.AddressFamily -eq 'InterNetwork' } | ForEach-Object { $_.IPAddressToString } }
            catch { @() }
        )
        $visualReachable = [bool](@($visualAddresses | Where-Object { Test-TcpEndpoint -HostName $_ -Port $VisualPort }).Count)

        if (-not $visualReachable) {
            # Nothing to prove and no local stand-in exists, so this is reported, not failed. Refresh needs
            # Infor Visual and the service will disable it on this machine (T167).
            Add-Result 'Infor Visual connection' 'WARN' "$visualServer did not answer on port $VisualPort, so the login could not be proved from here; the service disables refresh on this machine"
        }
        else {
            $visualBuilder = [Microsoft.Data.SqlClient.SqlConnectionStringBuilder]::new()
            $visualBuilder['Data Source'] = $config.InforVisualDatabaseOptions.Server
            $visualBuilder['Initial Catalog'] = $config.InforVisualDatabaseOptions.Database
            $visualBuilder['User ID'] = $effective['INFOR_VISUAL_SQL_USER']
            $visualBuilder['Password'] = $effective['INFOR_VISUAL_SQL_PASSWORD']
            $visualBuilder['TrustServerCertificate'] = $true
            $visualBuilder['Encrypt'] = $false
            $visualBuilder['Connect Timeout'] = 15
            try {
                $connection = [Microsoft.Data.SqlClient.SqlConnection]::new($visualBuilder.ConnectionString)
                $connection.Open()
                $command = $connection.CreateCommand(); $command.CommandText = 'SELECT SUSER_SNAME();'
                Add-Result 'Infor Visual connection' 'PASS' "login=$($command.ExecuteScalar()) on $($config.InforVisualDatabaseOptions.Server)/$($config.InforVisualDatabaseOptions.Database)"
                $connection.Dispose()
            }
            catch {
                Add-Result 'Infor Visual connection' 'FAIL' $_.Exception.Message
            }
        }
    }
}

if (Test-Failed) {
    Stop-Deployment 'Deployment failed its validation; the service was not started. See the summary above.' 1
}

# ---------------------------------------------------------------------------------------------
# 7. Start and health-check
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 7. start and health-check ---' -ForegroundColor Cyan

if ($SkipServiceStart) {
    Add-Result 'service start' 'SKIP' '-SkipServiceStart given'
}
else {
    Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

    $exePath = Join-Path $TargetPath $serviceExeName
    $started = Start-Process $exePath -PassThru
    $deadline = (Get-Date).AddSeconds($HealthCheckTimeoutSeconds)
    $listening = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        if (Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue) { $listening = $true; break }
    }

    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($process -and $process.Responding) {
        Add-Result 'service process' 'PASS' "pid=$($process.Id) responding"
    }
    else {
        Add-Result 'service process' 'FAIL' 'the service is not running'
    }

    if ($listening) {
        Add-Result 'API port bound' 'PASS' "$apiPort is listening"
    }
    else {
        Add-Result "API port bound" 'FAIL' "nothing is listening on $apiPort after $HealthCheckTimeoutSeconds s"
    }

    if ($process) {
        # Tray-only: the documented state is no main window at all.
        $windowCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id)
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $windowCondition)
        if ($windows.Count -eq 0 -and $process.MainWindowHandle -eq 0) {
            Add-Result 'tray-only (no window)' 'PASS' 'no top-level window, MainWindowHandle=0'
        }
        else {
            Add-Result 'tray-only (no window)' 'FAIL' "$($windows.Count) top-level window(s) found; MainWindowHandle=$($process.MainWindowHandle)"
        }
    }

    # Authenticated endpoints must refuse a caller that names no operator (T147).
    $statusUrl = "http://localhost:$apiPort/api/status"
    $httpCode = (& curl.exe -s -o NUL -w '%{http_code}' $statusUrl 2>$null)
    if ("$httpCode" -eq '401') {
        Add-Result 'API refuses an unnamed caller' 'PASS' "GET /api/status -> 401"
    }
    else {
        Add-Result 'API refuses an unnamed caller' 'FAIL' "GET /api/status -> '$httpCode' (expected 401)"
    }

    # Single instance: a second launch must redirect and exit.
    $second = Start-Process $exePath -PassThru
    [void]$second.WaitForExit(15000)
    $survivors = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
    if ($second.HasExited -and $survivors.Count -eq 1) {
        Add-Result 'single instance' 'PASS' 'a second launch redirected and exited'
    }
    else {
        Add-Result 'single instance' 'FAIL' "second copy exited=$($second.HasExited); survivors=$($survivors.Count)"
    }

    # Auto-start is on by default, so the first run registers it pointing at this install folder.
    $runEntry = (Get-ItemProperty $runKeyPath -ErrorAction SilentlyContinue).PSObject.Properties[$runValueName].Value
    if ($runEntry -eq $exePath -or $runEntry -eq "`"$exePath`"") {
        Add-Result 'auto-start registration' 'PASS' 'HKCU Run entry points at this install folder'
    }
    elseif ($runEntry) {
        Add-Result 'auto-start registration' 'WARN' "registered, but points elsewhere: $runEntry"
    }
    else {
        Add-Result 'auto-start registration' 'WARN' 'no HKCU Run entry (expected on a first run with AutoStartAtLogon=true)'
    }
}

# ---------------------------------------------------------------------------------------------
# 8. Desktop shortcut
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '--- 8. desktop shortcut ---' -ForegroundColor Cyan

if ($SkipDesktopShortcut) {
    Add-Result 'desktop shortcut' 'SKIP' '-SkipDesktopShortcut given'
}
else {
    # The label is read from the app's own resources so the desktop entry and the window title cannot
    # drift apart; the file name has to stay legal, so any path character is folded to a space.
    $appTitle = 'MTM mock cache service'
    $reswPath = Join-Path $repoRoot 'MTM_Waitlist.Mock.Service\Strings\en-us\Resources.resw'
    if (Test-Path $reswPath) {
        $titleEntry = ([xml](Get-Content $reswPath -Raw)).root.data | Where-Object { $_.name -eq 'Service_Shell.Title' }
        if ($titleEntry -and $titleEntry.value) { $appTitle = $titleEntry.value }
    }
    $appTitle = $appTitle -replace '[\\/:*?"<>|]', ' '

    $shortcutName = $appTitle
    $controlScriptPath = Join-Path $TargetPath $controlScriptName

    # The configured path is the operator's desktop. It is absent when the profile's Desktop is
    # redirected (this host redirects jkoll's into OneDrive), and a shortcut written to a path nobody can
    # see is worse than no shortcut, so fall back to the shell's Desktop known folder - which is jkoll's,
    # because this script runs as that account - and report which one was used.
    $desktopSource = $DesktopPath
    $resolvedDesktop = $DesktopPath
    if (-not (Test-Path $resolvedDesktop)) {
        $knownDesktop = [Environment]::GetFolderPath('Desktop')
        if ($knownDesktop -and (Test-Path $knownDesktop)) {
            $resolvedDesktop = $knownDesktop
            $desktopSource = "$DesktopPath is absent; resolved to $knownDesktop"
        }
    }
    $shortcutPath = Join-Path $resolvedDesktop "$shortcutName.lnk"

    # Prefer PowerShell 7 where the host has it, and fall back to Windows PowerShell, which every Windows
    # installation carries. The restart script is written to parse under both, and under neither does it
    # need elevation.
    $powerShellPath = $null
    $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCommand) { $powerShellPath = $pwshCommand.Source }
    if (-not $powerShellPath) {
        $powerShellPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    }

    if (-not (Test-Path $DesktopPath) -and -not (Test-Path $resolvedDesktop)) {
        Add-Result 'desktop shortcut' 'FAIL' "there is no desktop folder at $DesktopPath"
    }
    elseif (-not (Test-Path $powerShellPath)) {
        Add-Result 'desktop shortcut' 'FAIL' "there is no PowerShell interpreter at $powerShellPath"
    }
    elseif (-not (Test-Path $controlScriptPath)) {
        Add-Result 'desktop shortcut' 'FAIL' "$controlScriptName was not deployed to $TargetPath"
    }
    else {
        $existed = Test-Path $shortcutPath
        $failure = $null
        try {
            # Owner decision: an existing shortcut of this name is REPLACED rather than left alone, so a
            # redeploy after the install folder moves cannot leave a shortcut pointing at nothing.
            if ($existed) { Remove-Item $shortcutPath -Force -ErrorAction Stop }

            $shell = New-Object -ComObject WScript.Shell
            try {
                $shortcut = $shell.CreateShortcut($shortcutPath)
                $shortcut.TargetPath = $powerShellPath
                # No -Action: the shortcut opens the prompt, so the operator picks what happens and can
                # read what each choice does first.
                $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$controlScriptPath`""
                $shortcut.WorkingDirectory = $TargetPath
                $shortcut.IconLocation = "$(Join-Path $TargetPath $serviceExeName),0"
                $shortcut.Description = "Show, restart or shut down $appTitle."
                $shortcut.Save()

                # Read the saved file back: a shortcut that silently kept an old target would be worse
                # than no shortcut, because nothing would tell the operator why it did nothing.
                $saved = $shell.CreateShortcut($shortcutPath)
                if (-not (Test-Path $shortcutPath)) { $failure = 'the shortcut was not created' }
                elseif ($saved.TargetPath -ne $powerShellPath) { $failure = "target is '$($saved.TargetPath)'" }
                elseif ($saved.Arguments -notlike "*$controlScriptPath*") { $failure = "arguments are '$($saved.Arguments)'" }
            }
            finally {
                if ($shell) { [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell) }
            }
        }
        catch {
            $failure = $_.Exception.Message
        }

        if ($failure) {
            Add-Result 'desktop shortcut' 'FAIL' "$shortcutPath : $failure"
        }
        elseif ($existed) {
            Add-Result 'desktop shortcut' 'PASS' "replaced $shortcutPath (opens $controlScriptName)"
        }
        else {
            Add-Result 'desktop shortcut' 'PASS' "created $shortcutPath (opens $controlScriptName); $desktopSource"
        }

        # Also attempt the same shortcut on the other operator profile. Best effort by design: this is a
        # convenience for whoever signs in there, so a profile that is not on this machine is skipped and an
        # unwritable folder is a warning — either way the deployment still succeeds.
        if (-not $failure -and $AdditionalDesktopPaths) {
            foreach ($extraDesktop in $AdditionalDesktopPaths) {
                $extraTarget = $extraDesktop

                # Which folder the profile's desktop ACTUALLY is. A redirected Desktop leaves the literal
                # path behind as an empty husk on some machines, so "it exists" is not the answer - the
                # OneDrive-redirected folder is where the shell shows things, and GetFolderPath only answers
                # for the account running this script. The redirected folder therefore wins when it exists.
                $profileRoot = Split-Path -Parent $extraDesktop
                $redirected = @(
                    Get-ChildItem -Path $profileRoot -Directory -Filter 'OneDrive*' -ErrorAction SilentlyContinue |
                        ForEach-Object { Join-Path $_.FullName 'Desktop' } |
                        Where-Object { Test-Path $_ }
                )
                if ($redirected.Count -gt 0) { $extraTarget = $redirected[0] }

                if (-not (Test-Path $extraTarget)) {
                    Add-Result 'desktop shortcut (extra)' 'SKIP' "$extraTarget is not on this machine"
                    continue
                }

                if ($extraTarget -eq $resolvedDesktop) {
                    # The primary desktop already received it; writing it twice would be two rows for one icon.
                    Add-Result 'desktop shortcut (extra)' 'SKIP' "$extraTarget is already covered by the shortcut above"
                    continue
                }

                $extraShortcutPath = Join-Path $extraTarget "$shortcutName.lnk"
                try {
                    if (Test-Path $extraShortcutPath) { Remove-Item $extraShortcutPath -Force -ErrorAction Stop }

                    $extraShell = New-Object -ComObject WScript.Shell
                    try {
                        $extraShortcut = $extraShell.CreateShortcut($extraShortcutPath)
                        $extraShortcut.TargetPath = $powerShellPath
                        $extraShortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$controlScriptPath`""
                        $extraShortcut.WorkingDirectory = $TargetPath
                        $extraShortcut.IconLocation = "$(Join-Path $TargetPath $serviceExeName),0"
                        $extraShortcut.Description = "Show, restart or shut down $appTitle."
                        $extraShortcut.Save()
                    }
                    finally {
                        if ($extraShell) { [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($extraShell) }
                    }

                    Add-Result 'desktop shortcut (extra)' 'PASS' "created $extraShortcutPath (opens $controlScriptName)"
                }
                catch {
                    Add-Result 'desktop shortcut (extra)' 'WARN' "$extraShortcutPath : $($_.Exception.Message)"
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '=== SUMMARY ===' -ForegroundColor Cyan
$script:results | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

$failed = @($script:results | Where-Object { $_.Status -eq 'FAIL' })
$warned = @($script:results | Where-Object { $_.Status -eq 'WARN' })
if ($failed.Count -eq 0) {
    Write-Host "DEPLOYMENT OK - $($script:results.Count) checks, $($warned.Count) warning(s)." -ForegroundColor Green
    Write-Host "Install folder : $TargetPath"
    Write-Host "Service state  : $stateRoot"
    Write-Host ''
    Wait-GuidedClose
    exit 0
}
Write-Host "DEPLOYMENT FAILED - $($failed.Count) check(s) failed." -ForegroundColor Red
Write-Host ''
Wait-GuidedClose
exit 1
