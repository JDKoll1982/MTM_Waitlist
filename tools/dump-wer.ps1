$ErrorActionPreference = 'SilentlyContinue'
$out = New-Object System.Text.StringBuilder

[void]$out.AppendLine('=== CrashDumps ===')
Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'CrashDumps') -Filter '*.dmp' |
    Select-Object -First 10 Name, Length, LastWriteTime |
    ForEach-Object { [void]$out.AppendLine("$($_.Name) $($_.Length) $($_.LastWriteTime)") }

[void]$out.AppendLine('=== WER Temp dumps ===')
Get-ChildItem -Path 'C:\ProgramData\Microsoft\Windows\WER\Temp' -Filter '*.dmp' |
    Select-Object -First 10 Name, Length, LastWriteTime |
    ForEach-Object { [void]$out.AppendLine("$($_.Name) $($_.Length) $($_.LastWriteTime)") }

$roots = @(
    'C:\ProgramData\Microsoft\Windows\WER\ReportArchive',
    'C:\ProgramData\Microsoft\Windows\WER\ReportQueue'
)

foreach ($root in $roots) {
    [void]$out.AppendLine("=== $root ===")
    $dirs = Get-ChildItem -Path $root -Directory -Filter '*MTM_Waitlist*' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 2

    foreach ($dir in $dirs) {
        [void]$out.AppendLine("--- $($dir.Name) ($($dir.LastWriteTime)) ---")
        $wer = Get-ChildItem -Path $dir.FullName -Filter 'Report.wer' | Select-Object -First 1
        if (-not $wer) { [void]$out.AppendLine('no Report.wer'); continue }

        $lines = Get-Content -LiteralPath $wer.FullName -Encoding Unicode
        foreach ($line in $lines) {
            if ($line -match '^(Sig|DynamicSig|Stowed|Exception|AppName|EventType|FriendlyEventName|ReportDescription|UI\[|LoadedModule\[0\])') {
                [void]$out.AppendLine($line)
            }
            elseif ($line -match 'Exception|Stowed|MTM_Waitlist|0x8013') {
                [void]$out.AppendLine("  > $line")
            }
        }
    }
}

$target = Join-Path (Split-Path -Parent $PSScriptRoot) '.specify\wer.txt'
[System.IO.File]::WriteAllText($target, $out.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "wer written=$target"
