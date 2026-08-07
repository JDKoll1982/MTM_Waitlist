$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

$sqlFiles = Get-ChildItem -Path "Database" -Recurse -File -Include *.sql
if (-not $sqlFiles) {
    Write-Host "No SQL files found under Database/."
    exit 0
}

$errors = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param(
        [string]$File,
        [int]$Line,
        [string]$Message
    )

    $errors.Add(("{0}:{1} {2}" -f $File, $Line, $Message))
}

function Is-LowerSnakeCase {
    param([string]$Name)
    return $Name -match '^[a-z][a-z0-9_]*$'
}

function Validate-IdentifierBase {
    param(
        [string]$File,
        [string]$Name,
        [int]$Line,
        [string]$Kind
    )

    if (-not (Is-LowerSnakeCase $Name)) {
        Add-Error -File $File -Line $Line -Message "$Kind '$Name' must be lowercase snake_case"
    }

    if ($Name.Length -gt 64) {
        Add-Error -File $File -Line $Line -Message "$Kind '$Name' exceeds MySQL 64-character identifier limit"
    }

    $segments = $Name.Split('_', [System.StringSplitOptions]::RemoveEmptyEntries)
    $bannedAbbrev = @('cfg', 'usr', 'ws', 'sess', 'rpt')
    foreach ($segment in $segments) {
        if ($bannedAbbrev -contains $segment) {
            Add-Error -File $File -Line $Line -Message "$Kind '$Name' contains banned abbreviation '$segment'"
        }
    }

    $bannedWords = @('class', 'delete', 'order', 'type')
    if ($Name -eq 'value_type') {
        return
    }

    foreach ($word in $bannedWords) {
        if ($segments -contains $word) {
            Add-Error -File $File -Line $Line -Message "$Kind '$Name' contains banned word '$word'"
        }
    }
}

function Validate-TableName {
    param(
        [string]$File,
        [string]$Name,
        [int]$Line
    )

    Validate-IdentifierBase -File $File -Name $Name -Line $Line -Kind 'Table'

    $segments = $Name.Split('_', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($segments.Count -lt 3) {
        Add-Error -File $File -Line $Line -Message "Table '$Name' must follow {category}_{table}_{action_or_purpose} with at least 3 segments"
    }
}

function Validate-ColumnName {
    param(
        [string]$File,
        [string]$Name,
        [int]$Line,
        [string]$Type
    )

    Validate-IdentifierBase -File $File -Name $Name -Line $Line -Kind 'Column'

    if (($Type -match '^(DATETIME|TIMESTAMP)$') -and -not $Name.EndsWith('_utc')) {
        Add-Error -File $File -Line $Line -Message "Datetime/timestamp column '$Name' must end with _utc"
    }

    if ($Type -eq 'TINYINT(1)') {
        if (-not ($Name.StartsWith('is_') -or $Name.StartsWith('has_') -or $Name.EndsWith('_bool'))) {
            Add-Error -File $File -Line $Line -Message "Boolean column '$Name' must start with is_/has_ or end with _bool for typed setting values"
        }
    }
}

foreach ($file in $sqlFiles) {
    $relative = Resolve-Path -Relative $file.FullName
    $content = Get-Content -Path $file.FullName

    if ($file.Name -notmatch '^\d{4}__[a-z0-9_]+(_rollback)?\.sql$' -and $file.Name -notmatch '^seed_[a-z0-9_]+\.sql$') {
        Add-Error -File $relative -Line 1 -Message "Filename '$($file.Name)' does not match approved naming patterns"
    }

    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        $lineNo = $i + 1

        if ($line -match '"') {
            Add-Error -File $relative -Line $lineNo -Message 'Quoted identifiers are not allowed unless unavoidable'
        }

        if ($line -match '^\s*CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+([a-z0-9_]+)\s*\(') {
            Validate-TableName -File $relative -Name $Matches[1] -Line $lineNo
        }

        if ($line -match '^\s*CONSTRAINT\s+([a-z0-9_]+)\s+FOREIGN\s+KEY') {
            $fkName = $Matches[1]
            Validate-IdentifierBase -File $relative -Name $fkName -Line $lineNo -Kind 'Foreign key constraint'
            if ($fkName -notmatch '^fk_[a-z0-9_]+_[a-z0-9_]+_[a-z0-9_]+$') {
                Add-Error -File $relative -Line $lineNo -Message "Foreign key constraint '$fkName' must follow fk_<from_table>_<to_table>_<column>"
            }
        }

        if ($line -match '^\s*UNIQUE\s+KEY\s+([a-z0-9_]+)\s*\(') {
            $uqName = $Matches[1]
            Validate-IdentifierBase -File $relative -Name $uqName -Line $lineNo -Kind 'Unique key'
            if (-not $uqName.StartsWith('uq_')) {
                Add-Error -File $relative -Line $lineNo -Message "Unique key '$uqName' must start with uq_"
            }
        }

        if ($line -match '^\s*KEY\s+([a-z0-9_]+)\s*\(') {
            $idxName = $Matches[1]
            Validate-IdentifierBase -File $relative -Name $idxName -Line $lineNo -Kind 'Index'
            if (-not $idxName.StartsWith('idx_')) {
                Add-Error -File $relative -Line $lineNo -Message "Index '$idxName' must start with idx_"
            }
        }

        if ($line -match '^\s*([a-z0-9_]+)\s+(DATETIME|TIMESTAMP|TINYINT\(1\)|BIGINT|SMALLINT|VARCHAR\(\d+\)|CHAR\(\d+\)|TEXT|MEDIUMTEXT|DECIMAL\(\d+,\d+\)|VARBINARY\(\d+\))') {
            $columnName = $Matches[1]
            $columnType = $Matches[2]

            $keywordLikeNames = @('returns', 'primary', 'constraint', 'unique', 'key', 'create', 'drop', 'set', 'begin', 'end', 'return')
            if ($keywordLikeNames -contains $columnName) {
                continue
            }

            Validate-ColumnName -File $relative -Name $columnName -Line $lineNo -Type $columnType
        }
    }

    $raw = Get-Content -Path $file.FullName -Raw
    $tableBlocks = [regex]::Matches($raw, 'CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+([a-z0-9_]+)\s*\((.*?)\)\s*ENGINE=', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($block in $tableBlocks) {
        $tableName = $block.Groups[1].Value
        $body = $block.Groups[2].Value

        if ($body -notmatch '(?m)^\s*id\s+BIGINT\s+NOT\s+NULL\s+AUTO_INCREMENT') {
            Add-Error -File $relative -Line 1 -Message "Table '$tableName' must define primary key column 'id BIGINT NOT NULL AUTO_INCREMENT'"
        }

        if ($body -notmatch '(?m)^\s*public_id\s+CHAR\(36\)\s+NOT\s+NULL') {
            Add-Error -File $relative -Line 1 -Message "Table '$tableName' must define column 'public_id CHAR(36) NOT NULL'"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host 'SQL naming compliance failed:'
    foreach ($error in $errors) {
        Write-Host "- $error"
    }
    exit 1
}

Write-Host "SQL naming compliance passed for $($sqlFiles.Count) file(s)."
