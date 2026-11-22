param(
    [string]$Path = "."
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "Path not found: $Path"
    exit 1
}

$root = (Resolve-Path -LiteralPath $Path).Path

$files = Get-ChildItem -LiteralPath $root -Recurse -File -Include @('*.xaml', '*.cs') |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' }

$rules = @(
    @{ Name = 'Hardcoded FontSize';         Pattern = 'FontSize\s*=\s*"\d';                                  FileType = 'xaml' },
    @{ Name = 'Numeric fixed Width';        Pattern = '\sWidth\s*=\s*"\d';                                   FileType = 'xaml' },
    @{ Name = 'Numeric fixed Height';       Pattern = '\sHeight\s*=\s*"\d';                                  FileType = 'xaml' },
    @{ Name = 'Literal # color in XAML';    Pattern = '="#[0-9A-Fa-f]{6,8}';                                  FileType = 'xaml' },
    @{ Name = 'DisplayInformation.GetForCurrentView (throws in WinUI 3)'; Pattern = 'DisplayInformation\s*\.\s*GetForCurrentView'; FileType = 'cs' }
)

$gridRule = @{ Name = 'Margin/Padding not on 4 epx grid'; FileType = 'xaml' }
$spacingRule = @{ Name = 'Spacing not on 4 epx grid'; FileType = 'xaml' }

$hits = @{ }

foreach ($file in $files) {
    $ext = $file.Extension.TrimStart('.').ToLowerInvariant()
    $lines = Get-Content -LiteralPath $file.FullName

    foreach ($rule in $rules) {
        if ($rule.FileType -ne $ext) { continue }
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match $rule.Pattern) {
                $key = $rule.Name
                if (-not $hits.ContainsKey($key)) { $hits[$key] = [System.Collections.Generic.List[string]]::new() }
                $text = ($lines[$i].Trim())
                if ($text.Length -gt 120) { $text = $text.Substring(0, 120) + '…' }
                $hits[$key].Add("$($file.FullName):$($i + 1) — $text")
            }
        }
    }

    if ($ext -eq 'xaml') {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '(Margin|Padding)\s*=\s*"([^"]*)"') {
                $value = $Matches[2]
                $numbers = [regex]::Matches($value, '\d+(\.\d+)?') | ForEach-Object { [double]$_.Value }
                $bad = $numbers | Where-Object { $_ -ne [math]::Round($_ / 4) * 4 }
                if ($bad) {
                    $key = $gridRule.Name
                    if (-not $hits.ContainsKey($key)) { $hits[$key] = [System.Collections.Generic.List[string]]::new() }
                    $text = $lines[$i].Trim()
                    if ($text.Length -gt 120) { $text = $text.Substring(0, 120) + '…' }
                    $hits[$key].Add("$($file.FullName):$($i + 1) — $text")
                }
            }

            # Spacing values are gaps too: a 10 epx gap is 15 physical pixels at 150% scaling.
            if ($lines[$i] -match '(Spacing|RowSpacing|ColumnSpacing)\s*=\s*"(\d+(?:\.\d+)?)"') {
                $gap = [double]$Matches[2]
                if ($gap -ne [math]::Round($gap / 4) * 4) {
                    $key = $spacingRule.Name
                    if (-not $hits.ContainsKey($key)) { $hits[$key] = [System.Collections.Generic.List[string]]::new() }
                    $text = $lines[$i].Trim()
                    if ($text.Length -gt 120) { $text = $text.Substring(0, 120) + '…' }
                    $hits[$key].Add("$($file.FullName):$($i + 1) — $text")
                }
            }
        }
    }
}

$total = 0
$order = @(
    'Hardcoded FontSize',
    'Numeric fixed Width',
    'Numeric fixed Height',
    'Margin/Padding not on 4 epx grid',
    'Spacing not on 4 epx grid',
    'Literal # color in XAML',
    'DisplayInformation.GetForCurrentView (throws in WinUI 3)'
)

foreach ($name in $order) {
    if ($hits.ContainsKey($name)) {
        $count = $hits[$name].Count
        $total += $count
        Write-Output ""
        Write-Output "### $name ($count)"
        foreach ($h in $hits[$name]) { Write-Output "  $h" }
    }
}

Write-Output ""
Write-Output "Scanned $($files.Count) files under $root"
Write-Output "Total findings: $total"
if ($total -eq 0) {
    Write-Output "Clean."
    exit 0
} else {
    Write-Output "Review each hit: a numeric Width/Height on a fixed glyph/icon surface may be intentional."
    exit 0
}
