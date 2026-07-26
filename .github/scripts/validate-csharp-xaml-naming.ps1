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
$reswKeyExceptions = @('AppDisplayName', 'AppDescription', 'AppNotificationSamplePayload')

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

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $lineNo = $i + 1

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

            if ($line -match '^[ \t]*(public|private|internal|protected)[ \t]+(?:async[ \t]+)?Task(?:<[^>]+>)?[ \t]+([A-Za-z_][A-Za-z0-9_]*)\s*\(') {
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

if ($errors.Count -gt 0) {
    Write-Host 'C#/XAML naming compliance failed:'
    foreach ($e in $errors) {
        Write-Host "- $e"
    }
    exit 1
}

Write-Host "C#/XAML naming compliance passed for $($targetFiles.Count) file(s)."
