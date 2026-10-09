# Shows what the blame collector recorded, which is how a hung run names the test it stopped on.
#
# `--blame` writes a Sequence.xml listing the tests up to the hang, and `--blame-hang` additionally reports the
# test that exceeded its timeout. Both land under a TestResults folder beside the test assembly.
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File tools\show-blame.ps1

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Output '=== blame artifacts ==='
    $found = Get-ChildItem -Path $repoRoot -Recurse -Include 'Sequence.xml', '*.trx', '*.hangdump' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\\.git\\' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 10

    if (-not $found) {
        Write-Output '  none found'
    }

    foreach ($file in $found) {
        Write-Output ("  {0}  {1:u}  {2:n0} bytes" -f $file.FullName, $file.LastWriteTimeUtc, $file.Length)
    }

    Write-Output ''
    Write-Output '=== last tests recorded in the newest sequence file ==='
    $sequence = $found | Where-Object { $_.Name -eq 'Sequence.xml' } | Select-Object -First 1

    if (-not $sequence) {
        Write-Output '  no Sequence.xml'
    }
    else {
        $xml = [xml](Get-Content -LiteralPath $sequence.FullName -Raw)
        $entries = @($xml.TestSequence.InnerXml -split "`r?`n" | Where-Object { $_ -match 'Test' })
        $entries | Select-Object -Last 15 | ForEach-Object { Write-Output ("  {0}" -f $_.Trim()) }
    }
}
finally {
    Pop-Location
}
