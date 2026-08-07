param(
    [string]$CsvPath = "c:/Users/jkoll/source/repos/MTM_Waitlist/Documents/Development/Waitlist/NewRequestFeature/tablesready_parties_MTMLLC_20181003_20260807_1786122938173.csv",
    [int]$MaxRows = 100000,
    [string]$OutPath = "c:/Users/jkoll/source/repos/MTM_Waitlist/Documents/Development/Waitlist/NewRequestFeature/jobtype_summary_100k_refined.txt"
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

    $type = "Unknown"
    $subtype = "General"

    if ($text -match "\bscrap\b|\bscr\b|\btrash\b") {
        $type = "Scrap"
        $subtype = "General"
    }
    elseif ($text -match "\bpick\s*up\b|\bpickup\b|\bncm\b|\bfg\b|\bwip\b|\bretrieve\b|\bpick-up\b|pickup_") {
        $type = "Pickup"
        if ($text -match "\bncm\b|pickup_ncm") {
            $subtype = "Pickup NCM"
        }
        elseif ($text -match "\bfg\b|pickup_fg") {
            $subtype = "Pickup FG"
        }
        elseif ($text -match "\bwip\b|pickup_wip") {
            $subtype = "Pickup WIP"
        }
        else {
            $subtype = "Pickup Other"
        }
    }
    elseif ($text -match "\bmmc\b|\bcoil\b") {
        $type = "Coil"
        $subtype = "MMC/Coil"
    }
    elseif ($text -match "\bmmf\b|flat\s*stock|flatstock") {
        $type = "Flatstock"
        $subtype = "MMF/Flatstock"
    }
    elseif ($text -match "\bbring\b|\bdrop\b|\bdeliver\b|\bdropoff\b|\bdrop\s*off\b") {
        $type = "Bring"
        $subtype = "General"
    }

    return @($type, $subtype)
}

$parser = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser($CsvPath)
$parser.TextFieldType = [Microsoft.VisualBasic.FileIO.FieldType]::Delimited
$parser.SetDelimiters(",")
$parser.HasFieldsEnclosedInQuotes = $true

$headers = $parser.ReadFields()
$idx = @{}
for ($i = 0; $i -lt $headers.Length; $i++) {
    $idx[$headers[$i]] = $i
}

foreach ($col in @("created_time", "served_time", "note", "services")) {
    if (-not $idx.ContainsKey($col)) {
        throw "Missing required column: $col"
    }
}

$typeAgg = @{}
$subAgg = @{}
$rows = 0
$servedRows = 0

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
        Write-Progress -Activity "Refined job type analysis" -Status "Rows: $rows / $MaxRows" -PercentComplete $pct
    }

    $createdText = [string]$fields[$idx["created_time"]]
    $servedText = [string]$fields[$idx["served_time"]]
    $noteText = [string]$fields[$idx["note"]]
    $servicesText = [string]$fields[$idx["services"]]

    $class = Get-JobClass -NoteRaw $noteText -ServicesRaw $servicesText
    $type = $class[0]
    $subtype = $class[1]

    if (-not $typeAgg.ContainsKey($type)) {
        $typeAgg[$type] = [pscustomobject]@{ Count = 0; SumMin = 0.0; ServedCount = 0 }
    }

    if (-not $subAgg.ContainsKey($subtype)) {
        $subAgg[$subtype] = [pscustomobject]@{ Count = 0; SumMin = 0.0; ServedCount = 0; Type = $type }
    }

    $typeAgg[$type].Count++
    $subAgg[$subtype].Count++

    if ([string]::IsNullOrWhiteSpace($createdText) -or [string]::IsNullOrWhiteSpace($servedText)) {
        continue
    }

    try {
        $created = [datetimeoffset]$createdText
        $served = [datetimeoffset]$servedText
    }
    catch {
        continue
    }

    $mins = ($served - $created).TotalMinutes
    if ($mins -lt 0) {
        continue
    }

    $servedRows++
    $typeAgg[$type].SumMin += $mins
    $typeAgg[$type].ServedCount++
    $subAgg[$subtype].SumMin += $mins
    $subAgg[$subtype].ServedCount++
}

$parser.Close()
Write-Progress -Activity "Refined job type analysis" -Completed

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("ROWS_ANALYZED=$rows")
$lines.Add("ROWS_WITH_VALID_DURATION=$servedRows")
$lines.Add("")
$lines.Add("JOB_TYPES (count and avg minutes)")

foreach ($entry in ($typeAgg.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending)) {
    $avg = if ($entry.Value.ServedCount -gt 0) {
        [math]::Round($entry.Value.SumMin / $entry.Value.ServedCount, 2)
    }
    else {
        0
    }

    $lines.Add("  $($entry.Key): count=$($entry.Value.Count), served_for_avg=$($entry.Value.ServedCount), avg_minutes=$avg")
}

$lines.Add("")
$lines.Add("SUB_JOB_TYPES (count and avg minutes)")

foreach ($entry in ($subAgg.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending)) {
    $avg = if ($entry.Value.ServedCount -gt 0) {
        [math]::Round($entry.Value.SumMin / $entry.Value.ServedCount, 2)
    }
    else {
        0
    }

    $lines.Add("  $($entry.Key) [type=$($entry.Value.Type)]: count=$($entry.Value.Count), served_for_avg=$($entry.Value.ServedCount), avg_minutes=$avg")
}

Set-Content -Path $OutPath -Value $lines -Encoding UTF8
Write-Output "WROTE_SUMMARY=$OutPath"