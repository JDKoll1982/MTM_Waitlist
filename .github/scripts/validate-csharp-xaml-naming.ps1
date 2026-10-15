param(
    [string[]]$Files = @()
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

if (-not $Files -or $Files.Count -eq 0) {
    $Files = Get-ChildItem -Recurse -File -Include *.cs, *.xaml, *.resw |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
    ForEach-Object { Resolve-Path -Relative $_.FullName }
}

$targetFiles = $Files |
Where-Object { $_ -match '\.(cs|xaml|resw)$' } |
ForEach-Object { $_.Replace('/', '\') }

if (-not $targetFiles -or $targetFiles.Count -eq 0) {
    Write-Host 'No C#/XAML/RESW files to validate.'
    exit 0
}

$errors = New-Object System.Collections.Generic.List[string]
$disallowedAbbrev = @('cfg', 'usr', 'msg', 'auth')
# Keys exempt from the `Feature_Element.Property` key shape.
#
# `AppDisplayName`/`AppDescription`/`AppNotificationSamplePayload` are platform-reserved names.
#
# The remaining entries are the declared cell labels the waitlist card resolves (specs/012-waitlist-card-layout,
# FR-104 and data-model.md §9). A cell's label IS the resource key: the Item declaration in
# `Database/Seeds/seed_waitlist_request_item_configs/create.sql` declares the label, and the card resolves it at
# bind time through `GetLocalized()`. Renaming the key to the `Feature_Element.Property` shape would stop it
# matching the declaration and the cell's label would draw as its own raw key, so the bare-label shape is the only
# one that can work here. Every one of them is a label a shipped declaration actually carries - the guard is
# deliberately not pattern-matched to allow bare labels generally, so a *new* undeclared bare-label key still fails.
$reswKeyExceptions = @(
    'AppDisplayName', 'AppDescription', 'AppNotificationSamplePayload',
    'Die Location', 'Home Location', 'Scrap Type', 'Coil Number', 'Size', '# of Skids', 'Weight of Coil',
    'Die Shop Direction', 'Part Type', 'Item Type', 'Total Qty In House',
    'Coil description', 'Average coil weight', 'Quantity in house', 'Die', 'Component', 'Dunnage part',
    'Pickup location', 'Part', 'Quantity', 'Destination', 'Correct coil', 'Correct flatstock',
    'Lugger Location', 'Part description', 'Quantity on hand', 'Quantity involved', 'Scrap lugger',
    'Request description', 'Reason', 'Pickup work center', 'Requesting work center', 'Work center')

function Add-Error {
    param([string]$File, [int]$Line, [string]$Message)
    $errors.Add(("{0}:{1} {2}" -f $File, $Line, $Message))
}

function Contains-DisallowedAbbrev {
    param([string]$Value)
    $lower = $Value.ToLowerInvariant()
    foreach ($a in $disallowedAbbrev) {
        if ($lower -match "(^|_|\b)$a($|_|\b)") {
            return $a
        }
    }
    return $null
}

function Is-PascalCase {
    param([string]$Value)
    return $Value -match '^[A-Z][A-Za-z0-9]*$'
}

foreach ($relative in $targetFiles) {
    $fullPath = Join-Path $repoRoot $relative.TrimStart('.', '\')
    if (-not (Test-Path $fullPath)) {
        continue
    }

    $lines = Get-Content -Path $fullPath

    if ($relative -match '\.cs$') {
        $typeMatch = Select-String -Path $fullPath -Pattern '^[ \t]*public[ \t]+(?:sealed[ \t]+|abstract[ \t]+|partial[ \t]+|static[ \t]+)*(class|record|interface|enum)[ \t]+([A-Za-z_][A-Za-z0-9_]*)' | Select-Object -First 1
        if ($typeMatch) {
            $typeName = $typeMatch.Matches[0].Groups[2].Value
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
            if ($baseName.EndsWith('.xaml', [System.StringComparison]::OrdinalIgnoreCase)) {
                $baseName = [System.IO.Path]::GetFileNameWithoutExtension($baseName)
            }
            if ($typeName -ne $baseName) {
                Add-Error -File $relative -Line $typeMatch.LineNumber -Message "Public type '$typeName' must match file name '$baseName'"
            }
            if ($typeMatch.Matches[0].Groups[1].Value -eq 'interface' -and -not $typeName.StartsWith('I')) {
                Add-Error -File $relative -Line $typeMatch.LineNumber -Message "Interface '$typeName' must start with I"
            }
            if (-not (Is-PascalCase $typeName)) {
                Add-Error -File $relative -Line $typeMatch.LineNumber -Message "Type '$typeName' must be PascalCase"
            }
            $bad = Contains-DisallowedAbbrev -Value $typeName
            if ($bad) {
                Add-Error -File $relative -Line $typeMatch.LineNumber -Message "Type '$typeName' contains disallowed abbreviation '$bad'"
            }
        }

        # A method carrying a test attribute is named by the test-method rule, not the Async rule: the two cannot
        # both hold, because a test name ends in what it asserts rather than in "Async". The attribute is tracked
        # here so the declaration that follows it can be told apart from every other method.
        #
        # Three segments are accepted as well as four: a test that covers a whole type names no method, so forcing
        # a fourth segment would repeat what the third already says.
        $pendingTestAttribute = $false
        $testMethodPattern = '^(Database|UI|Model|ViewModel|Service|Helper|Audit|Converter)_[A-Za-z0-9]+_[A-Za-z0-9]+(_[A-Za-z0-9]+)?$'

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $lineNo = $i + 1

            if ($line -match '^[ \t]*\[(TestMethod|DataTestMethod)\]') {
                $pendingTestAttribute = $true
            }

            if ($line -match '^[ \t]*private[ \t]+static[ \t]+readonly[ \t]+[A-Za-z0-9_<>\[\],\.?]+[ \t]+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=.*)?;\s*$') {
                $name = $Matches[1]
                if ($name -notmatch '^s_[a-z][A-Za-z0-9]*$') {
                    Add-Error -File $relative -Line $lineNo -Message "Private static readonly field '$name' must be s_camelCase"
                }
            }
            elseif ($line -match '^[ \t]*private[ \t]+(?!const\b)(?!static\s+readonly\b)(?:readonly[ \t]+)?[A-Za-z0-9_<>\[\],\.?]+[ \t]+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=.*)?;\s*$') {
                $name = $Matches[1]
                if ($name -notmatch '^_[a-z][A-Za-z0-9]*$') {
                    Add-Error -File $relative -Line $lineNo -Message "Private field '$name' must be _camelCase"
                }
            }

            $isTestMethodDeclaration =
                $pendingTestAttribute -and
                ($line -match '^[ \t]*(public|private|internal|protected)[ \t]+(?:async[ \t]+)?(?:Task(?:<[^>]+>)?|void)[ \t]+([A-Za-z_][A-Za-z0-9_]*)\s*\(')

            if ($isTestMethodDeclaration) {
                $methodName = $Matches[2]
                if ($methodName -notmatch $testMethodPattern) {
                    Add-Error -File $relative -Line $lineNo -Message "Test method '$methodName' must be named Category_File_Method_TestName"
                }
                $pendingTestAttribute = $false
            }
            elseif ($line -match '^[ \t]*(public|private|internal|protected)[ \t]+(?:async[ \t]+)?Task(?:<[^>]+>)?[ \t]+([A-Za-z_][A-Za-z0-9_]*)\s*\(') {
                $methodName = $Matches[2]
                if ($line -match '\basync\b' -and -not $methodName.EndsWith('Async')) {
                    Add-Error -File $relative -Line $lineNo -Message "Async Task method '$methodName' must end with Async"
                }
            }
        }
    }
    elseif ($relative -match '\.xaml$') {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $lineNo = $i + 1

            $nameMatches = [regex]::Matches($line, 'x:Name\s*=\s*"([^"]+)"')
            foreach ($m in $nameMatches) {
                $name = $m.Groups[1].Value
                if (-not (Is-PascalCase $name)) {
                    Add-Error -File $relative -Line $lineNo -Message "x:Name '$name' must be PascalCase"
                }
                $bad = Contains-DisallowedAbbrev -Value $name
                if ($bad) {
                    Add-Error -File $relative -Line $lineNo -Message "x:Name '$name' contains disallowed abbreviation '$bad'"
                }
            }

            $stateMatches = [regex]::Matches($line, '<VisualState\s+[^>]*x:Name\s*=\s*"([^"]+)"')
            foreach ($m in $stateMatches) {
                $stateName = $m.Groups[1].Value
                if (-not (Is-PascalCase $stateName)) {
                    Add-Error -File $relative -Line $lineNo -Message "VisualState '$stateName' must be PascalCase"
                }
            }
        }
    }
    elseif ($relative -match '\.resw$') {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $lineNo = $i + 1
            $m = [regex]::Match($line, '<data\s+name="([^"]+)"')
            if ($m.Success) {
                $key = $m.Groups[1].Value
                if ($reswKeyExceptions -contains $key) {
                    continue
                }
                if ($key -notmatch '^[A-Za-z][A-Za-z0-9]*(?:_[A-Za-z][A-Za-z0-9]*)+\.[A-Za-z][A-Za-z0-9]*$') {
                    Add-Error -File $relative -Line $lineNo -Message "RESW key '$key' must match Feature_Element.Property"
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Grandfathered findings — reported as passable.
#
# `naming-grandfathered.txt` lists the findings that were already failing before feature 012
# (specs/012-waitlist-card-layout) started. The owner's instruction on 2026-10-11 was to grandfather the
# pre-existing ones and fix the feature's own, so a listed finding is not a failure and never appears in the
# report. The file records the reason, the format and the rules for changing it.
#
# A missing or unreadable file is not an error: the list is empty, every finding fails, and the reason is
# stated once. A key is the guard's own path and message WITHOUT the line number, because line numbers move
# and a line-keyed list would quietly grant a pass to whatever shifted into that line.
# ---------------------------------------------------------------------------
$grandfatheredKeys = New-Object System.Collections.Generic.HashSet[string]
$grandfatherFile = Join-Path $PSScriptRoot 'naming-grandfathered.txt'

if (Test-Path -LiteralPath $grandfatherFile) {
    try {
        Get-Content -LiteralPath $grandfatherFile |
        Where-Object { $_.Trim().Length -gt 0 -and -not $_.StartsWith('#') } |
        ForEach-Object { [void]$grandfatheredKeys.Add(($_.Trim() -replace '\\', '\')) }
    }
    catch {
        Write-Host "Could not read $grandfatherFile, so no finding is grandfathered: $($_.Exception.Message)"
    }
}
else {
    Write-Host "No grandfather list at $grandfatherFile, so every finding fails."
}

# The key for one finding: its path and its message, with no line number.
function Get-FindingKey {
    param([string]$Finding)

    $separator = $Finding.IndexOf(' ')
    if ($separator -lt 0) {
        return $Finding
    }

    $location = $Finding.Substring(0, $separator)
    $message = $Finding.Substring($separator + 1).Trim()

    # Drop the line number: `file.cs:123` -> `file.cs`, and the leading `.\` the enumeration adds, so a key
    # matches the list however the file was reached.
    $lastColon = $location.LastIndexOf(':')
    $asFile = if ($lastColon -gt 1) { $location.Substring(0, $lastColon) } else { $location }

    return "$(($asFile -replace '^\.\\', ''))|$message"
}

$grandfatheredCount = 0
$newErrors = New-Object System.Collections.Generic.List[string]

foreach ($e in $errors) {
    if ($grandfatheredKeys.Contains((Get-FindingKey $e))) {
        $grandfatheredCount++
        continue
    }

    $newErrors.Add($e)
}

if ($newErrors.Count -gt 0) {
    Write-Host 'C#/XAML naming compliance failed:'
    foreach ($e in $newErrors) {
        Write-Host "- $e"
    }
    Write-Host "New: $($newErrors.Count). Grandfathered: $grandfatheredCount."
    exit 1
}

Write-Host "C#/XAML naming compliance passed for $($targetFiles.Count) file(s). Grandfathered: $grandfatheredCount. New: 0."
