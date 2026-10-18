# Runs the suite one module at a time, each bounded by a wall-clock limit, and reports which module hangs.
#
# Why this exists: `dotnet test` cannot tell you that it is stuck — a hung test simply never returns, and the
# blame collector only fires for a test that is still inside its own body. A module whose run outlives the wall
# clock is killed and reported as HANG, so the hanging area is identified by elimination rather than by waiting.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build/bisect-tests.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build/bisect-tests.ps1 -TimeoutSeconds 240
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build/bisect-tests.ps1 -Modules Module_Settings,Module_Startup

[CmdletBinding()]
param(
    [string[]]$Modules = @(
        'Core',
        'Module_Core',
        'Module_Logging',
        'Module_Mock',
        'Module_Mock_Service',
        'Module_Settings',
        'Module_Setup',
        'Module_Shared',
        'Module_Startup',
        'Module_Waitlist',
        'Services'
    ),
    [int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$project = Join-Path $repoRoot 'MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj'
$settings = Join-Path $repoRoot 'MTM_Waitlist.Tests\MTM_Waitlist.Tests.runsettings'
$killScript = Join-Path $PSScriptRoot 'kill-tests.ps1'

$results = New-Object System.Collections.ArrayList

foreach ($module in $Modules) {
    Write-Output ''
    Write-Output ('=== {0} (limit {1}s) ===' -f $module, $TimeoutSeconds)

    # A host left by the previous module would hold the assembly and make this module look hung too.
    & $killScript | Out-Null

    $log = Join-Path $repoRoot ('.specify\bisect-{0}.log' -f $module)
    $errorLog = $log + '.err'

    $argumentList = @(
        'test'
        $project
        '-c', 'Debug'
        '-p:Platform=x64'
        '/m:1'
        '--settings', $settings
        '--blame-hang'
        '--blame-hang-timeout', '60s'
        '--blame-hang-dump-type', 'none'
        '--filter', ("FullyQualifiedName~{0}" -f $module)
        '--logger', 'console;verbosity=minimal'
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $dotnet -ArgumentList $argumentList -PassThru -NoNewWindow -RedirectStandardOutput $log -RedirectStandardError $errorLog
    $exited = $process.WaitForExit($TimeoutSeconds * 1000)
    $stopwatch.Stop()

    if (-not $exited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        & $killScript | Out-Null
        $verdict = 'HANG'
    }
    elseif ($process.ExitCode -eq 0) {
        $verdict = 'PASS'
    }
    else {
        $verdict = ('FAIL(exit {0})' -f $process.ExitCode)
    }

    Write-Output ('  verdict: {0}   elapsed: {1:n1}s' -f $verdict, $stopwatch.Elapsed.TotalSeconds)
    [void]$results.Add([PSCustomObject]@{ Module = $module; Verdict = $verdict; Seconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1) })

    if (Test-Path -LiteralPath $log) {
        $tail = Get-Content -LiteralPath $log -Tail 6 -ErrorAction SilentlyContinue
        foreach ($line in $tail) { Write-Output ('  | {0}' -f $line) }
    }
}

Write-Output ''
Write-Output '=== summary ==='
$results | ForEach-Object { Write-Output ('  {0,-20} {1,-16} {2,8:n1}s' -f $_.Module, $_.Verdict, $_.Seconds) }
