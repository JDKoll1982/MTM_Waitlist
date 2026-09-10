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

    # Banned words per database-schema-rules.instructions.md ("Reserved/Banned Terms and
    # Abbreviations"). `type` was previously enforced here but is NOT part of the documented
    # ruleset, and it produced false positives on legitimate columns such as `scope_type`
    # and `selected_dunnage_type_id`.
    $bannedWords = @('class', 'delete', 'order')
    if ($Name -eq 'value_type') {
        return
    }

    # `order` is banned to avoid the bare SQL keyword, but it is core domain vocabulary in this
    # repo as the compound `work_order` (e.g. work_order, normalized_work_order,
    # open_work_order_quantity). Exempt that compound rather than renaming established
    # Infor Visual terms. A bare `order` identifier is still rejected.
    if ($Name -match 'work_order') {
        $bannedWords = $bannedWords | Where-Object { $_ -ne 'order' }
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

# Returns the file's lines with comment text and string-literal content removed, so that a
# check for quoted identifiers only sees actual SQL code. A single left-to-right pass tracks
# both line comments (-- and #) and single-quoted literals, which correctly handles:
#   - apostrophes inside comments (e.g. "user's \"My Requests\""), which previously desynced a
#     whole-file regex into treating comment text as code;
#   - multi-line string literals such as JSON payloads in Mock validate.sql files;
#   - doubled '' escapes inside literals.
# Line count is preserved so reported line numbers stay accurate.
function Get-SanitizedSqlLines {
    param([string[]]$Lines)

    $result = New-Object System.Collections.Generic.List[string]
    $inStringLiteral = $false

    foreach ($rawLine in $Lines) {
        $kept = New-Object System.Text.StringBuilder
        $i = 0
        while ($i -lt $rawLine.Length) {
            $ch = $rawLine[$i]

            if ($inStringLiteral) {
                if ($ch -eq "'") {
                    if (($i + 1) -lt $rawLine.Length -and $rawLine[$i + 1] -eq "'") { $i += 2; continue }
                    $inStringLiteral = $false
                }
                $i++
                continue
            }

            if ($ch -eq '-' -and ($i + 1) -lt $rawLine.Length -and $rawLine[$i + 1] -eq '-') { break }
            if ($ch -eq '#') { break }

            if ($ch -eq "'") {
                if (($i + 1) -lt $rawLine.Length -and $rawLine[$i + 1] -eq "'") { $i += 2; continue }
                $inStringLiteral = $true
                $i++
                continue
            }

            [void]$kept.Append($ch)
            $i++
        }

        $result.Add($kept.ToString())
    }

    return $result
}

foreach ($file in $sqlFiles) {
    $relative = Resolve-Path -Relative $file.FullName
    $content = Get-Content -Path $file.FullName
    $sanitizedLines = Get-SanitizedSqlLines -Lines $content

    # Approved filename shapes for the current file-per-artifact layout. The previous rule
    # checked the retired FluentMigrator layout (NNNN__name.sql / seed_*.sql), which no longer
    # exists in this repo - it failed every one of the ~302 files under Database/.
    $approvedFileNamePatterns = @(
        '^(create|rollback|validate)\.sql$',        # Database/<Artifact>/<NN_name>/...
        '^All(Tables|SPs|Views|Funct|Seeds)\.sql$', # aggregate files
        '^create_database\.sql$',                   # Bootstrap
        '^update_table_descriptions\.sql$',         # Bootstrap
        '^(sp|fn|vw|tr)_[a-z0-9_]+\.sql$',          # module stored procedures / functions / views
        '^(Get|List|Lookup)[A-Za-z0-9]*\.sql$',     # Infor Visual + MySQL queue query scripts
        '^migrate_[a-z0-9_]+\.sql$',                # one-off seed migrations
        '^\d{2}_[a-z0-9_]+\.sql$'                   # numbered seed config scripts
    )
    $fileNameApproved = $false
    foreach ($pattern in $approvedFileNamePatterns) {
        if ($file.Name -match $pattern) { $fileNameApproved = $true; break }
    }
    if (-not $fileNameApproved) {
        Add-Error -File $relative -Line 1 -Message "Filename '$($file.Name)' does not match approved naming patterns"
    }

    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        $lineNo = $i + 1

        # Comments are not identifiers; string literals were masked above.
        $identifierCheckLine = $sanitizedLines[$i] -replace '--.*$', ''
        $identifierCheckLine = $identifierCheckLine -replace '#.*$', ''

        if ($identifierCheckLine -match '"') {
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
