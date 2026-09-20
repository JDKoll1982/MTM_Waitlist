# Measure the SC-007 / SC-008 reliability window from a run history.
#
# WHY THIS EXISTS
#   SC-007: "The cache is refreshed at least once per configured interval, with >=95% of scheduled cycles
#            succeeding over a 30-day observation window, and every skipped cycle attributable to a logged
#            external-source outage."
#   SC-008: "100% of scheduled backup windows over a 30-day window produce a restorable artifact for every
#            store with backups enabled."
#
#   Neither can be read off the service's own state: `RefreshRunRecordStore` and `BackupArtifactStore` keep only
#   the LATEST run per shape and per store. They answer "what is the state of things now", which is what the
#   status surface needs, and they can never answer "what happened over the last thirty days" - each new run
#   overwrites the evidence for the one before it.
#
#   `RunHistoryStore` (T230) is the append-only record these criteria are stated over: one JSON line per refresh
#   outcome and per backup outcome, service-local, retained for `ReliabilityCriteria.HistoryRetentionDays` = 60
#   days - deliberately longer than the 30-day window, so the window's first day is still on disk when the
#   measurement asks for it. This script is the measurement of that file, so:
#     * during development it runs against a SEEDED history where the success rate is known, which proves the
#       metric and its thresholds behave (the scenarios below are the proof);
#     * on the host, the same script reads the service's own file and the number is real.
#
#   A seeded run PROVES THE METRIC. It does NOT prove the service is reliable. Never report a seeded result as
#   the measured outcome of SC-007/SC-008.
#
# RECORD SHAPE (one JSON object per line) - the exact shape `RunHistoryStore` writes
#   {"record":"refresh","cycleUtc":"2026-09-12T00:00:00Z","shape":"work_order_lookup","outcome":"Succeeded","rows":701,"trigger":"scheduled"}
#   {"record":"refresh","cycleUtc":"...","shape":"inventory_locations","outcome":"Skipped","reason":"sourceUnreachable","trigger":"scheduled"}
#   {"record":"backup","windowUtc":"2026-09-12T02:00:00Z","store":"mtm_waitlist","outcome":"Succeeded","artifactPath":"...","artifactBytes":184320}
#
# USAGE
#   ./tools/measure-reliability-window.ps1 -Scenario healthy   -Generate
#   ./tools/measure-reliability-window.ps1 -Scenario marginal  -Generate
#   ./tools/measure-reliability-window.ps1 -Scenario unattributed -Generate
#   ./tools/measure-reliability-window.ps1 -Scenario broken    -Generate
#   ./tools/measure-reliability-window.ps1                       # measure whatever is in the fixture root
#   ./tools/measure-reliability-window.ps1 -Root "$env:LOCALAPPDATA\MTM_Waitlist.Mock.Service"   # the real host

[CmdletBinding()]
param(
    [ValidateSet('healthy', 'marginal', 'unattributed', 'broken')]
    [string]$Scenario = 'healthy',

    # Write a seeded history for -Scenario before measuring.
    [switch]$Generate,

    # Where the history lives. Defaults to the fixture root so a seeded run cannot be mistaken for a real one;
    # pass -Root "$env:LOCALAPPDATA\MTM_Waitlist.Mock.Service" to measure the host's own history.
    [string]$Root = (Join-Path $env:TEMP 'mtm-reliability-fixture'),

    # Must match ReliabilityCriteria.MeasuredWindowDays. The script asserts it found records, so a divergence
    # between the two shows up as a failed measurement rather than as a quietly perfect score.
    [int]$Days = 30,

    # Seed, so a given scenario is reproducible.
    [int]$Seed = 20260920
)

$ErrorActionPreference = 'Stop'
$historyPath = Join-Path $Root 'run-history.jsonl'

# The five read shapes the service refreshes (specs/001 data-model.md), and the four backed-up stores.
$shapes = @('work_order_lookup', 'operation_sequences', 'subordinate_parts', 'inventory_locations', 'disposition_input')
$stores = @('mtm_waitlist', 'mtm_wip_application_winforms', 'mtm_receiving_application', 'mtm_mock')

function New-SeededHistory {
    param([string]$Scenario, [int]$Days, [int]$Seed)

    $rng = [System.Random]::new($Seed)
    $start = [datetime]::UtcNow.Date.AddDays(-$Days)
    $lines = [System.Collections.Generic.List[string]]::new()

    # One refresh cycle a day at 00:30 UTC, one backup window a day at 02:00 UTC.
    for ($d = 0; $d -lt $Days; $d++) {
        $cycle = $start.AddDays($d).AddMinutes(30).ToString('yyyy-MM-ddTHH:mm:ssZ')
        $window = $start.AddDays($d).AddHours(2).ToString('yyyy-MM-ddTHH:mm:ssZ')

        # How many shapes fail in this cycle, and why.
        $failed = 0
        $roll = $rng.NextDouble()
        switch ($Scenario) {
            'healthy' { if ($roll -lt 0.04) { $failed = 1 } }
            'marginal' { if ($roll -lt 0.07) { $failed = 1 } }
            'unattributed' { if ($roll -lt 0.04) { $failed = 1 } }
            'broken' { $failed = 0 }
        }

        for ($s = 0; $s -lt $shapes.Count; $s++) {
            $shape = $shapes[$s]
            if ($failed -gt 0 -and $s -eq ($failed - 1)) {
                # The 'unattributed' scenario is the one where a skip is NOT explained by an outage, which
                # SC-007 forbids outright - whatever the success rate.
                $reason = if ($Scenario -eq 'unattributed') { 'schemaMismatch' } else { 'sourceUnreachable' }
                $outcome = if ($Scenario -eq 'unattributed') { 'Failed' } else { 'Skipped' }
                $lines.Add((@{ record = 'refresh'; cycleUtc = $cycle; shape = $shape; outcome = $outcome; reason = $reason; trigger = 'scheduled' } | ConvertTo-Json -Compress))
            }
            else {
                $lines.Add((@{ record = 'refresh'; cycleUtc = $cycle; shape = $shape; outcome = 'Succeeded'; rows = (100 + $s); trigger = 'scheduled' } | ConvertTo-Json -Compress))
            }
        }

        # Backups: every store, every window. 'broken' drops one store's artifact on one day.
        foreach ($store in $stores) {
            $isBroken = ($Scenario -eq 'broken' -and $d -eq 11 -and $store -eq 'mtm_mock')
            if ($isBroken) {
                $lines.Add((@{ record = 'backup'; windowUtc = $window; store = $store; outcome = 'Failed'; reason = 'mysqldumpNonZeroExit' } | ConvertTo-Json -Compress))
            }
            else {
                $bytes = 180000 + $rng.Next(5000, 900000)
                $lines.Add((@{ record = 'backup'; windowUtc = $window; store = $store; outcome = 'Succeeded'; artifactPath = "C:\Backups\$store\$($start.AddDays($d).ToString('yyyyMMdd')).sql"; artifactBytes = $bytes } | ConvertTo-Json -Compress))
            }
        }
    }

    New-Item -ItemType Directory -Force -Path $Root | Out-Null
    Set-Content -Path $historyPath -Value $lines -Encoding UTF8
    Write-Host ("seeded {0} lines for scenario '{1}' -> {2}" -f $lines.Count, $Scenario, $historyPath)
}

function Measure-History {
    param([string]$Path)

    if (-not (Test-Path $Path)) { throw "No run history at '$Path'. Use -Generate to seed one." }

    $refresh = [System.Collections.Generic.List[object]]::new()
    $backup = [System.Collections.Generic.List[object]]::new()

    foreach ($line in Get-Content -Path $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $o = $line | ConvertFrom-Json
        switch ($o.record) {
            'refresh' { $refresh.Add($o) }
            'backup' { $backup.Add($o) }
        }
    }

    # ---- SC-007 ------------------------------------------------------------------------------------
    # SC-007 is stated over SCHEDULED cycles. A refresh a caller asked for is not one of them, and counting it
    # would let the rate be improved by asking for more refreshes - which is the opposite of the property the
    # criterion is about. A record written before `trigger` existed carries none and is kept.
    $refresh = @($refresh | Where-Object { -not $_.trigger -or $_.trigger -eq 'scheduled' })

    # A cycle is the set of shapes refreshed at one scheduled time. It SUCCEEDS when every shape succeeded.
    # Any non-success must be attributable to a logged external-source outage.
    $cycles = $refresh | Group-Object -Property cycleUtc
    $succeeded = 0
    $unattributed = [System.Collections.Generic.List[string]]::new()

    foreach ($cycle in $cycles) {
        $notSucceeded = @($cycle.Group | Where-Object { $_.outcome -ne 'Succeeded' })
        if ($notSucceeded.Count -eq 0) { $succeeded++ }
        else {
            foreach ($n in $notSucceeded) {
                if ($n.reason -ne 'sourceUnreachable') {
                    $unattributed.Add(("{0} shape={1} outcome={2} reason={3}" -f $cycle.Name, $n.shape, $n.outcome, $n.reason))
                }
            }
        }
    }

    $scheduledCount = $cycles.Count
    $rate = if ($scheduledCount -eq 0) { 0 } else { $succeeded / $scheduledCount }

    # "refreshed at least once per configured interval" needs its own check: an interval where nothing ran at all
    # leaves no record, so a missing day would otherwise be invisible and the rate would look better than reality.
    $expectedCycles = $Days
    $cyclesMissing = [math]::Max(0, $expectedCycles - $scheduledCount)

    $sc007 = ($cyclesMissing -eq 0) -and ($rate -ge 0.95) -and ($unattributed.Count -eq 0)

    # ---- SC-008 ------------------------------------------------------------------------------------
    # 100% of scheduled windows, for every store with backups enabled, produced a restorable artifact.
    # "Restorable" here is the record's own claim: an artifact path AND a non-zero size - a zero-length file
    # is never a success (FR-013).
    #
    # A window belongs to ONE store, not to all four. Each store is scheduled independently and one store's
    # schedule must not change another's (FR-009), so requiring every store in every window would fail a
    # perfectly healthy service whose stores run at different times of day. What the criterion asks is that
    # each store's own windows all produced an artifact.
    #
    # A store with no record at all is reported as missing rather than skipped. "No evidence" must not read as
    # "perfect": a store whose schedule never fired is exactly the failure this criterion exists to catch, and
    # it is invisible if the check only walks stores the history already mentions.
    $storeRecords = @($backup | Where-Object { $_.store })
    $storesInHistory = @($storeRecords | ForEach-Object { $_.store } | Sort-Object -Unique)
    $silentStores = @($stores | Where-Object { $storesInHistory -notcontains $_ })
    $windows = $storeRecords | Group-Object -Property { '{0}|{1}' -f $_.windowUtc, $_.store }
    $missing = [System.Collections.Generic.List[string]]::new()

    foreach ($store in $silentStores) {
        $missing.Add(("store={0} outcome=noRecordAtAll - the store produced no window in this history" -f $store))
    }

    foreach ($w in $windows) {
        $run = $w.Group | Where-Object { $_.outcome -eq 'Succeeded' -and $_.artifactPath -and [int]$_.artifactBytes -gt 0 } | Select-Object -First 1
        if ($null -eq $run) {
            $first = $w.Group | Select-Object -First 1
            $missing.Add(("{0} store={1} outcome={2}" -f $first.windowUtc, $first.store, $first.outcome))
        }
    }

    $sc008 = ($missing.Count -eq 0)

    # ---- report ------------------------------------------------------------------------------------
    Write-Host ''
    Write-Host ("SC-007  scheduled cycles: {0} of {1} expected   succeeded: {2}   rate: {3:P2}   threshold: 95%" -f $scheduledCount, $expectedCycles, $succeeded, $rate)
    if ($cyclesMissing -gt 0) {
        Write-Host ("        {0} interval(s) produced NO cycle at all - an interval that never ran is not a success" -f $cyclesMissing)
    }
    if ($unattributed.Count -gt 0) {
        Write-Host ("       UNATTRIBUTED non-successes ({0}) - SC-007 fails on these regardless of the rate:" -f $unattributed.Count)
        $unattributed | Select-Object -First 5 | ForEach-Object { Write-Host "         $_" }
    }
    Write-Host ("       verdict: {0}" -f $(if ($sc007) { 'PASS' } else { 'FAIL' }))
    Write-Host ''
    Write-Host ("SC-008  backup windows: {0}   stores seen: {1} of {2}   windows missing an artifact: {3}" -f $windows.Count, $storesInHistory.Count, $stores.Count, $missing.Count)
    if ($missing.Count -gt 0) {
        $missing | Select-Object -First 5 | ForEach-Object { Write-Host "         $_" }
    }
    Write-Host ("       verdict: {0}" -f $(if ($sc008) { 'PASS' } else { 'FAIL' }))
    Write-Host ''

    [pscustomobject]@{
        Scenario          = $Scenario
        ScheduledCycles   = $scheduledCount
        ExpectedCycles    = $expectedCycles
        MissingIntervals  = $cyclesMissing
        SucceededCycles   = $succeeded
        SuccessRate       = [math]::Round($rate, 4)
        Unattributed      = $unattributed.Count
        SC007             = $(if ($sc007) { 'PASS' } else { 'FAIL' })
        BackupWindows     = $windows.Count
        StoresSeen        = $storesInHistory.Count
        MissingArtifacts  = $missing.Count
        SC008             = $(if ($sc008) { 'PASS' } else { 'FAIL' })
        Evidence          = $(if ($Generate) { 'seeded - proves the metric, not the service' } else { "read from $historyPath" })
    }
}

if ($Generate) { New-SeededHistory -Scenario $Scenario -Days $Days -Seed $Seed }
Measure-History -Path $historyPath
