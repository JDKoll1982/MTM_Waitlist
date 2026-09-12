# compact_helper.ps1 -- bookkeeping for /speckit.token-budget.compact on Windows.
#
# Mirrors scripts/bash/compact_helper.sh. The slash-command prompt rewrites
# content; this script just snapshots, backs up, summarizes, and stamps.

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("backup_if_needed", "snapshot", "summarize", "has_marker", "stamp")]
    [string]$Command,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$estimate = Join-Path $here "estimate_tokens.ps1"

function Backup-Path {
    param([string]$Path)
    if ($Path.EndsWith(".md")) {
        return ($Path.Substring(0, $Path.Length - 3) + ".full.md")
    } else {
        return "$Path.full"
    }
}

function Snapshot {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return 0 }
    $line = & powershell -NoProfile -File $estimate $Path
    return [int]($line -split "`t")[0]
}

switch ($Command) {
    "backup_if_needed" {
        $f = $Args[0]
        if (-not (Test-Path $f)) { Write-Output "skipped-missing"; break }
        if (-not $f.EndsWith(".md")) { Write-Output "skipped-non-md"; break }
        $b = Backup-Path $f
        if (Test-Path $b) { Write-Output "kept-existing" }
        else { Copy-Item $f $b; Write-Output "created" }
    }
    "snapshot" {
        Write-Output (Snapshot $Args[0])
    }
    "summarize" {
        $orig = $Args[0]; $new = $Args[1]
        $before = Snapshot $orig
        $after  = Snapshot $new
        $pct = if ($before -gt 0) { [Math]::Round(($after - $before) * 100.0 / $before, 1) } else { 0.0 }
        $name = Split-Path -Leaf $new
        # '{3,+5}' is not a valid .NET alignment specifier -- alignment accepts an
        # optional sign and digits, but never a leading '+', so the composite
        # format threw "Input string was not in a correct format" before printing
        # anything. Format the percentage first and align it as a string, matching
        # the bash row's printf '(%+5s%%)' (signed value, right-aligned in 5).
        $pctText = $pct.ToString("F1", [System.Globalization.CultureInfo]::InvariantCulture)
        ('{0,-32} {1,7} -> {2,7} tokens  ({3,5}%)' -f $name, $before, $after, $pctText)
    }
    "has_marker" {
        $f = $Args[0]
        if (-not (Test-Path $f)) { exit 1 }
        if (Select-String -Path $f -Pattern '<!--\s*token-budget: compacted' -Quiet) { exit 0 } else { exit 1 }
    }
    "stamp" {
        $f = $Args[0]; $level = if ($Args.Count -gt 1) { $Args[1] } else { "medium" }
        $base = Split-Path -Leaf (Backup-Path $f)
        $ts = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        $marker = "<!-- token-budget: compacted (level=$level) on $ts; original at $base -->"
        # Read and write UTF-8 explicitly.  Get-Content/Set-Content fall back to
        # the host's ANSI codepage under Windows PowerShell 5.1, which corrupts
        # every non-ASCII character in the artifact on a stamp round-trip.
        $content = [System.IO.File]::ReadAllText($f)
        if ($content -match '<!--\s*token-budget: compacted[^>]*-->') {
            $content = [regex]::Replace($content, '<!--\s*token-budget: compacted[^>]*-->', $marker)
        } else {
            $content = $content.TrimEnd() + "`n`n$marker`n"
        }
        [System.IO.File]::WriteAllText($f, $content, (New-Object System.Text.UTF8Encoding($false)))
    }
}

# Exit codes: only has_marker is meaningful (0 = marked, 1 = not marked).  Every
# other subcommand must exit 0 explicitly, otherwise $LASTEXITCODE leaks the
# result of the last nested native call (e.g. the python3 probe or an inner
# 'exit 1') and callers see a spurious failure.
exit 0
