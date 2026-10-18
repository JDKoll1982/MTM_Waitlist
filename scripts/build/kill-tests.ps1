# Force-kills test processes left running in the background.
#
# A run that never finishes leaves a `testhost` (and sometimes a `dotnet test` / `vstest.console`) holding the
# test assembly, the output folder and the runner's port. The next run then appears to "start" and never does
# anything, because it is waiting for a lock the previous run still holds. This kills those, and only those:
#   * testhost / vstest.console are always test processes;
#   * a dotnet.exe is killed only when its own command line says `test` or `vstest` or `testhost`, so the
#     language server and the build nodes are left alone.
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build/kill-tests.ps1

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$candidates = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='testhost.exe' OR Name='vstest.console.exe'" -ErrorAction SilentlyContinue

# A bisect run drives its own `dotnet test` children module by module. Killing only the child is not enough: the
# driver immediately starts the next module, and two builds on one project collide over the XAML compiler's
# intermediate files. So the driver is stopped first, and it is matched by its own command line so nothing else
# that happens to be a shell is touched.
$drivers = Get-CimInstance Win32_Process -Filter "Name='powershell.exe' OR Name='pwsh.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -match 'bisect-tests\.ps1' }

foreach ($driver in $drivers) {
    Write-Output ("Stopping the bisect driver {0}." -f $driver.ProcessId)
    Stop-Process -Id $driver.ProcessId -Force -ErrorAction SilentlyContinue
}

if (-not $candidates) {
    Write-Output 'No test processes were running.'
    return
}

$victims = New-Object System.Collections.ArrayList

foreach ($process in $candidates) {
    $commandLine = if ($process.CommandLine) { $process.CommandLine -replace '\s+', ' ' } else { '' }

    $isAlwaysTestProcess = $process.Name -ne 'dotnet.exe'
    $isDotnetTestHost = $process.Name -eq 'dotnet.exe' -and $commandLine -match '\btest\b|vstest|testhost'

    if ($isAlwaysTestProcess -or $isDotnetTestHost) {
        [void]$victims.Add([PSCustomObject]@{
            ProcessId   = $process.ProcessId
            Name        = $process.Name
            CommandLine = $commandLine
        })
    }
}

if ($victims.Count -eq 0) {
    Write-Output 'No test processes were running.'
    return
}

Write-Output ("Killing {0} test process(es):" -f $victims.Count)
foreach ($victim in $victims) {
    Write-Output ("  {0} {1} :: {2}" -f $victim.ProcessId, $victim.Name, $victim.CommandLine)
}

foreach ($victim in $victims) {
    Stop-Process -Id $victim.ProcessId -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Milliseconds 300

$stillRunning = Get-Process -Id ($victims | ForEach-Object { $_.ProcessId }) -ErrorAction SilentlyContinue

if ($stillRunning) {
    Write-Output 'Still running after the kill (they were not force-killable):'
    foreach ($process in $stillRunning) {
        Write-Output ("  {0} {1}" -f $process.Id, $process.ProcessName)
    }
}
else {
    Write-Output 'All test processes are gone.'
}
