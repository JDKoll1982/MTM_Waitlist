# Reports the version each built binary actually carries, so a bump can be seen rather than assumed.
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$debugRoot = Join-Path $repoRoot 'bin\x64\Debug'

# Searched rather than listed by path: the app carries a runtime identifier and the libraries do not, so their
# output folders differ and a hard-coded list would silently report the wrong thing.
$binaries = @(
    'MTM_Waitlist.dll',
    'MTM_Waitlist.exe',
    'MTM_Waitlist.Settings.dll',
    'MTM_Waitlist.Shared.dll',
    'MTM_Waitlist.Mock.Service.dll'
)

foreach ($name in $binaries) {
    $found = @(Get-ChildItem -LiteralPath $debugRoot -Recurse -Filter $name -File -ErrorAction SilentlyContinue)
    if ($found.Count -eq 0) {
        $serviceRoot = Join-Path $repoRoot 'MTM_Waitlist.Mock.Service\bin\x64\Debug'
        $found = @(Get-ChildItem -LiteralPath $serviceRoot -Recurse -Filter $name -File -ErrorAction SilentlyContinue)
    }

    if ($found.Count -eq 0) {
        Write-Output ("{0,-32} not built" -f $name)
        continue
    }

    foreach ($file in $found) {
        $info = $file.VersionInfo
        Write-Output ("{0,-32} file={1}  product={2}" -f $name, $info.FileVersion, $info.ProductVersion)
    }
}

# Case sensitive on purpose: the XML declaration's `version="1.0"` also matches a case-insensitive search.
$manifest = Join-Path $repoRoot 'Package.appxmanifest'
$identity = Select-String -LiteralPath $manifest -Pattern 'Version="([\d\.]+)"' -CaseSensitive | Select-Object -First 1
Write-Output ("{0,-32} {1}" -f 'MSIX identity version', $identity.Matches[0].Groups[1].Value)

$props = Join-Path $repoRoot 'Directory.Build.props'
$declared = Select-String -LiteralPath $props -Pattern '<Version>([\d\.]+)</Version>' -CaseSensitive | Select-Object -First 1
Write-Output ("{0,-32} {1}" -f 'declared in Directory.Build.props', $declared.Matches[0].Groups[1].Value)
