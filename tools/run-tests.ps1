# Runs the test suite with hang detection, so a test that never finishes is named instead of running forever.
#
# `--blame-hang` makes the runner abort a test that exceeds the stated timeout and report which test it was,
# which is the only way to identify a hang from the outside: without it the run simply never returns and nothing
# says why.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-tests.ps1
#       Runs the logging slice, which is the one known to hang.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-tests.ps1 -All
#       Runs the whole suite with hang detection.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\run-tests.ps1 -Modules Module_Mock_Service
#       Runs only the tests whose fully-qualified name contains one of the given substrings. Several may be
#       given, comma separated: -Modules Module_Logging,StoreLogCoverage

[CmdletBinding()]
param(
    [string[]]$Modules = @('Module_Logging', 'LogStoreLevel', 'StoreLogCoverage', 'StoreLogWriter'),
    [switch]$All,
    [string]$LogPath = '.specify\test-run.txt',
    [int]$HangTimeoutSeconds = 90
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    # A build node that outlives the run is one of the ways the next run waits for ever.
    $env:MSBUILDDISABLENODEREUSE = '1'

    # And a test host left behind by an earlier hang holds the test assembly, which makes this run wait on a lock
    # it can never get: that is what a run that "never starts" is. Cleared first, every time.
    $killScript = Join-Path $PSScriptRoot 'kill-tests.ps1'
    if (Test-Path -LiteralPath $killScript) {
        & $killScript
        Write-Output ''
    }

    $arguments = @(
        'test'
        'MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj'
        '-c', 'Debug'
        '-p:Platform=x64'
        '/m:1'
        '--settings', 'MTM_Waitlist.Tests\MTM_Waitlist.Tests.runsettings'
        '--blame-hang'
        '--blame-hang-timeout', ("{0}s" -f $HangTimeoutSeconds)
        '--blame-hang-dump-type', 'none'
        '--logger', 'console;verbosity=normal'
    )

    if (-not $All) {
        $filter = ($Modules | Where-Object { $_ } | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
        $arguments += @('--filter', $filter)
        Write-Output ("Filter: {0}" -f $filter)
    }
    else {
        Write-Output 'Filter: none (whole suite)'
    }

    Write-Output ("Hang timeout: {0}s    Log: {1}" -f $HangTimeoutSeconds, $LogPath)

    & 'C:\Program Files\dotnet\dotnet.exe' @arguments *> $LogPath

    $exitCode = $LASTEXITCODE
    Write-Output ("EXIT={0}" -f $exitCode)

    if (Test-Path -LiteralPath $LogPath) {
        $log = Get-Content -LiteralPath $LogPath -Raw

        # The lines that matter: what passed or failed, and anything the hang detector wrote.
        $interesting = Select-String -InputObject $log -Pattern 'error|failed|hang|Timeout|Passed!|Failed!|Test Run' -AllMatches |
            Select-Object -First 40 |
            ForEach-Object { $_.Line }

        if ($interesting) {
            Write-Output '--- from the log ---'
            $interesting | ForEach-Object { Write-Output $_ }
        }
    }
}
finally {
    Pop-Location
}
