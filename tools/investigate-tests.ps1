# Reports what is holding the test runner, what the last hang-detecting run said, and how expensive the
# repository-wide audit walks are.
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File tools\investigate-tests.ps1

[CmdletBinding()]
param(
    [string]$LogPath = '.specify\log-hang.txt'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Output '=== test processes =='
    $candidates = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='testhost.exe' OR Name='vstest.console.exe'" -ErrorAction SilentlyContinue
    $testProcesses = @()
    foreach ($process in $candidates) {
        $commandLine = if ($process.CommandLine) { $process.CommandLine -replace '\s+', ' ' } else { '' }
        if ($process.Name -ne 'dotnet.exe' -or $commandLine -match '\btest\b|vstest|testhost') {
            $testProcesses += ("  {0} {1} :: {2}" -f $process.ProcessId, $process.Name, $commandLine)
        }
    }
    if ($testProcesses.Count -eq 0) { Write-Output '  none' } else { $testProcesses | ForEach-Object { Write-Output $_ } }

    Write-Output ''
    Write-Output ('=== {0} ===' -f $LogPath)
    if (Test-Path -LiteralPath $LogPath) {
        # The redirect writes UTF-16 and keeps the file open for the length of the run, so it is opened with
        # sharing rather than read exclusivly: that is what lets a run still in progress be watched.
        try {
            $stream = [System.IO.File]::Open(
                (Resolve-Path -LiteralPath $LogPath).Path,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::ReadWrite)
            try {
                $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::Unicode)
                $text = $reader.ReadToEnd()
            }
            finally {
                $stream.Dispose()
            }

            $lines = $text -split "`r?`n" | Where-Object { $_ -match '\S' }
            Write-Output ("  lines: {0}" -f $lines.Count)
            $lines | Select-Object -Last 40 | ForEach-Object { Write-Output ("  {0}" -f $_) }
        }
        catch {
            Write-Output ("  could not read the log: {0}" -f $_.Exception.Message)
        }
    }
    else {
        Write-Output '  (no log yet)'
    }

    Write-Output ''
    Write-Output '=== repository walk cost ==='
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $all = [System.IO.Directory]::EnumerateFiles($repoRoot, '*', [System.IO.SearchOption]::AllDirectories)
    $allCount = 0
    foreach ($file in $all) { $allCount++ }
    $stopwatch.Stop()
    Write-Output ("  every file under the repository: {0} files in {1:n1}s" -f $allCount, $stopwatch.Elapsed.TotalSeconds)

    foreach ($folder in 'bin', 'obj', '.git') {
        $path = Join-Path $repoRoot $folder
        if (Test-Path -LiteralPath $path) {
            $stopwatch.Restart()
            $count = 0
            foreach ($file in [System.IO.Directory]::EnumerateFiles($path, '*', [System.IO.SearchOption]::AllDirectories)) { $count++ }
            $stopwatch.Stop()
            Write-Output ("  under {0}: {1} files in {2:n1}s" -f $folder, $count, $stopwatch.Elapsed.TotalSeconds)
        }
    }

    Write-Output ''
    Write-Output '=== scanned-extension files that a repository audit would read ==='
    $extensions = @('.cs', '.xaml', '.resw', '.sql', '.csproj', '.json')
    $excluded = @('bin', 'obj', '.git', 'TestResults', 'specs', 'defects')
    $readable = 0
    $wouldBeRead = 0
    $stopwatch.Restart()
    foreach ($file in [System.IO.Directory]::EnumerateFiles($repoRoot, '*', [System.IO.SearchOption]::AllDirectories)) {
        if ($extensions -notcontains [System.IO.Path]::GetExtension($file).ToLowerInvariant()) { continue }
        $readable++
        # Windows PowerShell runs on .NET Framework, which has no Path.GetRelativePath, so the prefix is trimmed.
        $relative = if ($file.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $file.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        }
        else {
            $file
        }
        $isExcluded = $false
        $separator = [System.IO.Path]::DirectorySeparatorChar
        foreach ($name in $excluded) {
            # IndexOf rather than Contains: the two-argument Contains overload is .NET Core only and this script
            # has to run on Windows PowerShell, which is .NET Framework.
            $atStart = $relative.IndexOf($name + $separator, [System.StringComparison]::OrdinalIgnoreCase) -eq 0
            $inTheMiddle = $relative.IndexOf($separator + $name + $separator, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            if ($atStart -or $inTheMiddle) {
                $isExcluded = $true
                break
            }
        }
        if (-not $isExcluded) { $wouldBeRead++ }
    }
    $stopwatch.Stop()
    Write-Output ("  scanned extensions anywhere: {0}" -f $readable)
    Write-Output ("  of those, outside the excluded folders (what an audit reads): {0}" -f $wouldBeRead)
    Write-Output ("  enumeration took {0:n1}s" -f $stopwatch.Elapsed.TotalSeconds)
}
finally {
    Pop-Location
}
