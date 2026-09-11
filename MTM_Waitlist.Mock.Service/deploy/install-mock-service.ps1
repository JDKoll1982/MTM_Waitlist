<#
.SYNOPSIS
    Installs or redeploys MTM_Waitlist.Mock.Service onto the Infor Visual / MySQL cache host.

.DESCRIPTION
    One idempotent deployment step for the on-host half of the Module_Mock cached fallback. It:

      1. refuses to run unless the LOCAL MACHINE IS THE CACHE HOST (see SERVER-ONLY below);
      2. stops the running service process, if any;
      3. deletes the previous deployment folder (state under %LOCALAPPDATA% is kept unless -PurgeState);
      4. publishes (with -Publish) and/or copies the publish output to the install folder;
      5. verifies the copied deployment is complete and self-contained;
      6. installs and verifies the environment secrets the service resolves at runtime;
      7. starts the service and health-checks it (tray-only, port bound, API refusing unauthenticated
         calls, single-instance redirect);
      8. prints a PASS/FAIL summary and exits non-zero if anything failed.

    The equivalent manual procedure is MTM_Waitlist.Mock.Service/README.md sections 2-4; that file
    remains the narrative authority, this script is the executable form of it.

.SERVER-ONLY — READ THIS BEFORE RUNNING
    The service belongs on the host that can reach both MySQL and Infor Visual. Running it from a
    developer workstation would install a tray-only auto-starting service on the wrong machine and
    point `HKCU\...\Run` at whatever folder it was launched from.

    The script therefore verifies that one of this machine's own IPv4 addresses is the expected
    server address (default: the MySQL `Server=` value in appsettings.json, i.e. 172.16.1.104) and
    REFUSES TO RUN otherwise, with exit code 2. In other words: **VS Code (and therefore the agent)
    must be running on the server.** `-AllowNonServerHost` exists only for a deliberately chosen
    non-server host and must be justified in the change that uses it.

    Verified identity of the cache host (2026-09-11): `V-MTMFG-5.mantoolfg.com` = `172.16.1.104`.

.EXAMPLE
    # the normal case: publish fresh, deploy, install/verify secrets, start and health-check
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -Publish

.EXAMPLE
    # redeploy an existing publish output without touching the service's stored state
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1

.EXAMPLE
    # a clean sweep: also delete the service's configuration, credential and backups
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -PurgeState
#>
[CmdletBinding()]
param(
    # Install folder. Must be final before the first run: a first run registers auto-start with the path it was launched from.
    [string] $TargetPath = 'C:\Services\MTM_Waitlist.Mock.Service',

    # Publish output to deploy from. Defaults to the project's folder-publish profile output.
    [string] $SourcePath,

    # Expected cache host. Defaults to the MySQL Server= value in appsettings.json.
    [string] $ServerHost,

    # Run `dotnet publish` (Release, win-x64-selfcontained) before copying.
    [switch] $Publish,

    # Do not set or verify the environment secrets.
    [switch] $SkipSecrets,

    # Do not start the service or run the health checks.
    [switch] $SkipServiceStart,

    # Also delete %LOCALAPPDATA%\MTM_Waitlist.Mock.Service — configuration, the DPAPI credential and backups.
    [switch] $PurgeState,

    # Bypass the server-only guard. Deliberate use only; say why in the change that uses it.
    [switch] $AllowNonServerHost,

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

if (-not $SourcePath) {
    $SourcePath = Join-Path $repoRoot 'MTM_Waitlist.Mock.Service\bin\publish\win-x64'
}

$script:results = [System.Collections.Generic.List[object]]::new()

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
    exit $ExitCode
}

function Test-Failed {
    return [bool]($script:results | Where-Object { $_.Status -eq 'FAIL' })
}

Write-Host ''
Write-Host '============================================================' -ForegroundColor Cyan
Write-Host ' MTM_Waitlist.Mock.Service — install / redeploy' -ForegroundColor Cyan
Write-Host '============================================================' -ForegroundColor Cyan
Write-Host "  repo    : $repoRoot"
Write-Host "  source  : $SourcePath"
Write-Host "  target  : $TargetPath"
Write-Host ''

# ---------------------------------------------------------------------------------------------
# 1. Server-only guard
# ---------------------------------------------------------------------------------------------
Write-Host '--- 1. server-only guard ---' -ForegroundColor Cyan

$appSettingsPath = Join-Path $repoRoot 'appsettings.json'
if (-not $ServerHost) {
    $ServerHost = ([regex]::Match((Get-Content $appSettingsPath -Raw | ConvertFrom-Json).StartupDatabaseOptions.ConnectionString,
        'Server=([^;]+)')).Groups[1].Value
}
if (-not $ServerHost) { Stop-Deployment 'Could not determine the expected server host.' 3 }

$localAddresses = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -ne '127.0.0.1' } | Select-Object -ExpandProperty IPAddress)

$expectedAddresses = @($ServerHost)
if ($ServerHost -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    $expectedAddresses = @(
        try { [System.Net.Dns]::GetHostAddresses($ServerHost) | Where-Object { $_.AddressFamily -eq 'InterNetwork' } | ForEach-Object { $_.IPAddressToString } }
        catch { @() }
    )
}

$serverMatched = [bool](@($localAddresses | Where-Object { $expectedAddresses -contains $_ }).Count)
Write-Host "  this machine : $([System.Net.Dns]::GetHostName())  addresses: $($localAddresses -join ', ')"
Write-Host "  expected host: $ServerHost  addresses: $($expectedAddresses -join ', ')"

if ($serverMatched) {
    Add-Result 'server host guard' 'PASS' "local machine is the cache host ($ServerHost)"
}
elseif ($AllowNonServerHost) {
    Add-Result 'server host guard' 'WARN' "-AllowNonServerHost given: deploying to a machine that is NOT $ServerHost"
}
else {
    Stop-Deployment @"
REFUSING TO RUN: this machine is not the cache host.

  this machine : $(($localAddresses -join ', '))
  expected host: $ServerHost

The service must be installed on the host that reaches both MySQL and Infor Visual — run VS Code on
the server itself (verified identity 2026-09-11: V-MTMFG-5 = 172.16.1.104).

If this non-server host is deliberate, re-run with -AllowNonServerHost and record why.
"@ 2
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
        Add-Result 'purge service state' 'WARN' "deleted $stateRoot (configuration, credential, backups) — a new credential must be generated"
    }
    else {
        Add-Result 'purge service state' 'PASS' 'no state folder existed'
    }
}
else {
    Add-Result 'keep service state' 'PASS' "state kept at $stateRoot"
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
        Add-Result 'publish' 'FAIL' 'publish profile win-x64-selfcontained was not applied (NETSDK1198) — the build would not be self-contained'
    }
    else {
        Add-Result 'publish' 'PASS' "dotnet publish (Release, win-x64-selfcontained) with no warnings"
    }
}
else {
    Add-Result 'publish' 'SKIP' 'reusing the existing publish output (-Publish not given)'
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
    'assets\WindowIcon.ico',
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
    Add-Result 'required content present' 'FAIL' "missing: $($missing -join ', ')"
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
        $current = $persisted.PSObject.Properties[$name].Value
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
        $value = $persisted.PSObject.Properties[$name].Value
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

        $dbFailures = @()
        foreach ($database in @('mtm_mock', 'mtm_waitlist', 'mtm_wip_application_winforms', 'mtm_receiving_application')) {
            $builder = [MySqlConnector.MySqlConnectionStringBuilder]::new($effective['MTM_WAITLIST_DB_CONNECTION_STRING'])
            $builder.Database = $database
            try {
                $connection = [MySqlConnector.MySqlConnection]::new($builder.ConnectionString)
                $connection.Open(); $connection.Dispose()
            }
            catch { $dbFailures += "$database ($($_.Exception.Message))" }
        }
        if ($dbFailures.Count -eq 0) {
            Add-Result 'MySQL connections' 'PASS' 'mtm_mock, mtm_waitlist, mtm_wip_application_winforms, mtm_receiving_application'
        }
        else {
            Add-Result 'MySQL connections' 'FAIL' ($dbFailures -join '; ')
        }

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

    # Authenticated endpoints must refuse without the shared credential.
    $statusUrl = "http://localhost:$apiPort/api/status"
    $httpCode = (& curl.exe -s -o NUL -w '%{http_code}' $statusUrl 2>$null)
    if ("$httpCode" -eq '401') {
        Add-Result 'API refuses without credential' 'PASS' "GET /api/status -> 401"
    }
    else {
        Add-Result 'API refuses without credential' 'FAIL' "GET /api/status -> '$httpCode' (expected 401)"
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
# Summary
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '=== SUMMARY ===' -ForegroundColor Cyan
$script:results | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

$failed = @($script:results | Where-Object { $_.Status -eq 'FAIL' })
$warned = @($script:results | Where-Object { $_.Status -eq 'WARN' })
if ($failed.Count -eq 0) {
    Write-Host "DEPLOYMENT OK — $($script:results.Count) checks, $($warned.Count) warning(s)." -ForegroundColor Green
    Write-Host "Install folder : $TargetPath"
    Write-Host "Service state  : $stateRoot"
    Write-Host ''
    exit 0
}
Write-Host "DEPLOYMENT FAILED — $($failed.Count) check(s) failed." -ForegroundColor Red
Write-Host ''
exit 1
