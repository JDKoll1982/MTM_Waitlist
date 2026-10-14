$ErrorActionPreference = 'Stop'
$mysql = 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe'
$env:MYSQL_PWD = 'root'
$root = 'c:\Users\jkoll\source\repos\MTM_Waitlist'

function Norm([string]$s) {
    if (-not $s) { return '' }
    $s = [regex]::Replace($s, '(?s)/\*.*?\*/', '')
    $keep = @()
    foreach ($l in ($s -split "`r?`n")) { if ($l.TrimStart() -notmatch '^--') { $keep += $l } }
    $s = ($keep -join "`n")
    $s = $s -replace '`', ''
    $s = [regex]::Replace($s, '(?is)\bdefiner\s*=\s*[^\s]+\s+', '')
    $s = [regex]::Replace($s, '(?is)\bdelimiter\s+[^\s]+', '')
    $s = [regex]::Replace($s, '(?is)\bsql_security\s+\w+', '')
    $s = $s -replace '\$\$', ''
    $s = [regex]::Replace($s, '\s+', '')
    return $s.ToLowerInvariant()
}

function Live-Def([string]$name) {
    $raw = ("show create procedure ``mtm_waitlist``.``$name``;") | & $mysql -h 172.16.1.104 -P 3306 -u root -N -B 2>$null
    foreach ($p in ($raw -split "`t")) { if ($p -match '(?i)^CREATE\s+(?:ALGORITHM|DEFINER|SQL\s+SECURITY|PROCEDURE)') { return $p } }
    return ''
}

$name = 'sp_auth_roles_list'
$live = Norm (Live-Def $name)
$text = Get-Content (Join-Path $root 'Database\StoredProcedures\AllSPs.sql') -Raw
$m = [regex]::Match($text, "(?is)CREATE\s+PROCEDURE\s+$name\b.*?(?=CREATE\s+PROCEDURE|\z)")
$repo = Norm $m.Value

$o = "LIVE_LEN=$($live.Length) REPO_LEN=$($repo.Length)"
$n = [Math]::Min($live.Length, $repo.Length)
$idx = -1
for ($i = 0; $i -lt $n; $i++) { if ($live[$i] -ne $repo[$i]) { $idx = $i; break } }
$o += "`nFIRST_DIFF_AT=$idx"
if ($idx -ge 0) {
    $s = [Math]::Max(0, $idx - 50); $e = [Math]::Min($n, $idx + 150)
    $o += "`nLIVE: ..." + $live.Substring($s, $e - $s)
    $o += "`nREPO: ..." + $repo.Substring($s, $e - $s)
} else {
    if ($live.Length -gt $n) { $o += "`nLIVE_TAIL: " + $live.Substring($n) }
    if ($repo.Length -gt $n) { $o += "`nREPO_TAIL: " + $repo.Substring($n) }
}
$o += "`n--- raw live ---`n" + (Live-Def $name)
$o | Out-File -Encoding utf8 (Join-Path $root '.specify\_live\debug2.txt')
Write-Output 'ok'
