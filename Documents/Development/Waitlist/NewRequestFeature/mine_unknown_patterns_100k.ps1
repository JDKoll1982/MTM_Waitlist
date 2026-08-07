param(
    [string]$CsvPath = "c:/Users/jkoll/source/repos/MTM_Waitlist/Documents/Development/Waitlist/NewRequestFeature/tablesready_parties_MTMLLC_20181003_20260807_1786122938173.csv",
    [int]$MaxRows = 100000,
    [string]$OutPath = "c:/Users/jkoll/source/repos/MTM_Waitlist/Documents/Development/Waitlist/NewRequestFeature/unknown_patterns_100k.txt"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName Microsoft.VisualBasic

function Get-JobClass {
    param(
        [string]$NoteRaw,
        [string]$ServicesRaw
    )

    $note = if ($null -eq $NoteRaw) { "" } else { $NoteRaw.ToLowerInvariant() }
    $services = if ($null -eq $ServicesRaw) { "" } else { $ServicesRaw.ToLowerInvariant() }
    $text = "$note $services"

    if ($text -match "\bscrap\b|\bscr\b|\btrash\b") { return "Scrap" }
    if ($text -match "\bpick\s*up\b|\bpickup\b|\bncm\b|\bfg\b|\bwip\b|\bretrieve\b|\bpick-up\b|pickup_") { return "Pickup" }
    if ($text -match "\bmmc\b|\bcoil\b") { return "Coil" }
    if ($text -match "\bmmf\b|flat\s*stock|flatstock") { return "Flatstock" }
    if ($text -match "\bbring\b|\bdrop\b|\bdeliver\b|\bdropoff\b|\bdrop\s*off\b") { return "Bring" }
    return "Unknown"
}

$stop = @(
    "the","and","for","to","of","a","on","in","is","at","with","from","by","or","an","be","it",
    "no","not","job","press","please","need","line","machine","mtm","llc","mh","material","handler",
    "request","req","get","put","one","two","three","four","five","six","seven","eight","nine","ten"
)
$stopSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($w in $stop) { $null = $stopSet.Add($w) }

$parser = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser($CsvPath)
$parser.TextFieldType = [Microsoft.VisualBasic.FileIO.FieldType]::Delimited
$parser.SetDelimiters(",")
$parser.HasFieldsEnclosedInQuotes = $true

$headers = $parser.ReadFields()
$idx = @{}
for ($i = 0; $i -lt $headers.Length; $i++) {
    $idx[$headers[$i]] = $i
}

foreach ($col in @("note", "services")) {
    if (-not $idx.ContainsKey($col)) {
        throw "Missing required column: $col"
    }
}

$tokenCounts = @{}
$serviceCounts = @{}
$sampleNotes = New-Object System.Collections.Generic.List[string]
$rows = 0
$unknownRows = 0

while (-not $parser.EndOfData -and $rows -lt $MaxRows) {
    try {
        $fields = $parser.ReadFields()
    }
    catch {
        continue
    }

    $rows++
    if ($rows % 2000 -eq 0) {
        $pct = [int](($rows * 100.0) / $MaxRows)
        Write-Progress -Activity "Mining Unknown patterns" -Status "Rows: $rows / $MaxRows" -PercentComplete $pct
    }

    $note = [string]$fields[$idx["note"]]
    $services = [string]$fields[$idx["services"]]

    if ((Get-JobClass -NoteRaw $note -ServicesRaw $services) -ne "Unknown") {
        continue
    }

    $unknownRows++

    if (-not [string]::IsNullOrWhiteSpace($services)) {
        $key = $services.Trim().ToLowerInvariant()
        if ($serviceCounts.ContainsKey($key)) { $serviceCounts[$key]++ } else { $serviceCounts[$key] = 1 }
    }

    if (-not [string]::IsNullOrWhiteSpace($note) -and $sampleNotes.Count -lt 120) {
        $sampleNotes.Add($note)
    }

    if ([string]::IsNullOrWhiteSpace($note)) {
        continue
    }

    $clean = $note.ToLowerInvariant() -replace "[^a-z0-9\s\-/]", " "
    foreach ($token in ($clean -split "\s+")) {
        if ([string]::IsNullOrWhiteSpace($token)) { continue }
        if ($token.Length -lt 2) { continue }
        if ($token -match "^\d+$") { continue }
        if ($stopSet.Contains($token)) { continue }

        if ($tokenCounts.ContainsKey($token)) {
            $tokenCounts[$token]++
        }
        else {
            $tokenCounts[$token] = 1
        }
    }
}

$parser.Close()
Write-Progress -Activity "Mining Unknown patterns" -Completed

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("ROWS_ANALYZED=$rows")
$lines.Add("UNKNOWN_ROWS=$unknownRows")
$lines.Add("")
$lines.Add("TOP_UNKNOWN_SERVICES")
foreach ($entry in ($serviceCounts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 20)) {
    $lines.Add("  $($entry.Key) => $($entry.Value)")
}
$lines.Add("")
$lines.Add("TOP_UNKNOWN_TOKENS")
foreach ($entry in ($tokenCounts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 120)) {
    $lines.Add("  $($entry.Key) => $($entry.Value)")
}
$lines.Add("")
$lines.Add("SAMPLE_UNKNOWN_NOTES")
foreach ($note in $sampleNotes) {
    $lines.Add("  - $note")
}

Set-Content -Path $OutPath -Value $lines -Encoding UTF8
Write-Output "WROTE_UNKNOWN_PATTERNS=$OutPath"