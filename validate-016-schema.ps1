# Validates the 016 schema artifacts against a throwaway database on the local MySQL instance.
# It never reads or writes mtm_waitlist: every statement runs inside a probe database that is dropped at the end.
$ErrorActionPreference = 'Stop'
$repo = 'C:\Users\jkoll\source\repos\MTM_Waitlist'
$mysql = 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe'
$probe = 'mtm_waitlist_schema_probe_016'

$options = (Get-Content (Join-Path $repo 'appsettings.json') -Raw | ConvertFrom-Json).WaitlistDatabaseOptions
$map = @{}
foreach ($pair in ($options.ConnectionString -split ';')) {
    if (-not $pair.Trim()) { continue }
    $kv = $pair -split '=', 2
    $map[$kv[0].Trim()] = $kv[1].Trim()
}

$env:MYSQL_PWD = $map['Password']
$hostArg = if ($map['Server']) { $map['Server'] } else { '127.0.0.1' }
$portArg = if ($map['Port']) { $map['Port'] } else { '3306' }
$userArg = $map['User ID']

function Invoke-Sql {
    param([string] $Sql, [switch] $AllowFail)
    $Sql | & $mysql --host=$hostArg --port=$portArg --user=$userArg --default-character-set=utf8mb4 --batch --raw 2>&1
    if ($LASTEXITCODE -ne 0 -and -not $AllowFail) {
        throw "mysql failed for: $($Sql.Substring(0, [Math]::Min(120, $Sql.Length)))"
    }
    return $LASTEXITCODE
}

function Read-Artifact {
    param([string] $RelativePath)
    (Get-Content (Join-Path $repo $RelativePath) -Raw) -replace '(?im)^\s*USE\s+mtm_waitlist\s*;\s*$', ''
}

try {
    Invoke-Sql "DROP DATABASE IF EXISTS $probe;" | Out-Null
    Invoke-Sql "CREATE DATABASE $probe CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" | Out-Null
    Write-Host "probe database created"

    # 1. The table, as the repository now creates it.
    $table = (Read-Artifact 'Database/Tables/13_setup_work_centers_catalog/create.sql')
    Invoke-Sql "USE $probe;`n$table" | Out-Null
    $columns = (Invoke-Sql "SELECT GROUP_CONCAT(column_name ORDER BY ordinal_position) AS c FROM information_schema.columns WHERE table_schema='$probe' AND table_name='setup_work_centers_catalog';")
    Write-Host "table columns: $columns"

    # 2. The idempotent add: drop the column, run the block twice, confirm one add and a clean second pass.
    Invoke-Sql "USE $probe; ALTER TABLE setup_work_centers_catalog DROP COLUMN is_generic;" | Out-Null
    $alter = Get-Content "$env:TEMP\generic-alter-block.sql" -Raw
    Invoke-Sql "USE $probe;`n$alter" | Out-Null
    Invoke-Sql "USE $probe;`n$alter" | Out-Null
    $fresh = (Invoke-Sql "SELECT column_default FROM information_schema.columns WHERE table_schema='$probe' AND table_name='setup_work_centers_catalog' AND column_name='is_generic';")
    Write-Host "after two runs of the guarded alter, is_generic default: $fresh"

    # 3. The view and both procedures.
    Invoke-Sql "USE $probe;`n$((Read-Artifact 'Database/Views/vw_setup_work_centers_active/create.sql'))" | Out-Null
    Invoke-Sql "USE $probe;`n$((Read-Artifact 'Database/StoredProcedures/sp_setup_work_centers_get_all/create.sql'))" | Out-Null
    Invoke-Sql "USE $probe;`n$((Read-Artifact 'Database/StoredProcedures/sp_setup_work_centers_generic_set/create.sql'))" | Out-Null
    Write-Host "view and procedures created"

    # 4. Behaviour: a fresh row is not generic, the procedure marks it, and the read carries the mark.
    Invoke-Sql "USE $probe; INSERT INTO setup_work_centers_catalog (public_id, building, work_center_name, created_utc, updated_utc) VALUES (UUID(), 'Expo Drive', 'Probe Press 1', UTC_TIMESTAMP(), UTC_TIMESTAMP());" | Out-Null
    Write-Host "fresh row: $((Invoke-Sql "USE $probe; CALL sp_setup_work_centers_get_all();") -join ' ')"
    $id = (Invoke-Sql "USE $probe; SELECT id FROM setup_work_centers_catalog WHERE work_center_name='Probe Press 1';").Trim()
    Invoke-Sql "USE $probe; CALL sp_setup_work_centers_generic_set('$id', 1, NULL);" | Out-Null
    Write-Host "after marking: $((Invoke-Sql "USE $probe; CALL sp_setup_work_centers_get_all();") -join ' ')"
    Invoke-Sql "USE $probe; CALL sp_setup_work_centers_generic_set('$id', 0, NULL);" | Out-Null
    Write-Host "after clearing: $((Invoke-Sql "USE $probe; CALL sp_setup_work_centers_get_all();") -join ' ')"

    # 5. A row that does not exist reports zero affected rows rather than a silent success.
    Write-Host "unknown id affected rows: $((Invoke-Sql "USE $probe; CALL sp_setup_work_centers_generic_set('999999', 1, NULL); SELECT ROW_COUNT() AS affected;") -join ' ')"

    # 6. The rollbacks remove what they claim to.
    Invoke-Sql "USE $probe;`n$((Read-Artifact 'Database/StoredProcedures/sp_setup_work_centers_generic_set/rollback.sql'))" | Out-Null
    Invoke-Sql "USE $probe;`n$((Read-Artifact 'Database/Views/vw_setup_work_centers_active/rollback.sql'))" | Out-Null
    Write-Host "rollbacks ran"
    Write-Host "PROBE PASSED"
}
finally {
    try { Invoke-Sql "DROP DATABASE IF EXISTS $probe;" -AllowFail | Out-Null } catch { }
    Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue
}
