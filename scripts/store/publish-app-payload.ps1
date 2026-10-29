#Requires -Version 7.0
<#
.SYNOPSIS
    Publishes the MTM Waitlist application in the shape the MTM Loader expects, and writes the
    application's build-manifest.json.

.DESCRIPTION
    The MTM Loader (`MTM_Loader_WinUI3`) keeps each application's local copy equal to the published
    master copy in `\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Application Store\<sourceFolder>`.
    It trusts one file there and nothing else: the application's `build-manifest.json`, which lists every
    file in the payload with its size and SHA-256. A missing or unparsable manifest does not fail the
    loader, it quietly marks the application not updateable, so the manifest is written by this script
    rather than assembled by hand.

    What this script does, in the order the loader's own release rules require:

      1. Publishes the application from the loader-shaped publish profile (unpackaged, self-contained
         .NET and Windows App SDK, not single-file, not trimmed), into a folder it clears first, so a
         stale file from an older publish cannot ship.
      2. Refuses to continue when the payload is empty or does not contain the executable the catalogue
         names.
      3. Hashes every published file and writes `build-manifest.json` last, so a half-copied folder is
         never advertised as a release.
      4. Re-reads the manifest and verifies the folder against it file by file.
      5. Optionally copies the finished payload to `-Destination` (the store folder), writing the
         manifest there last and verifying the copy.

    It never writes to the shared store unless `-Destination` is given, and it never touches a database.

.PARAMETER Version
    The 3-part version to record. Defaults to the `<Version>` in `Directory.Build.props`, which is the
    single place a person edits it. A value that disagrees with the props file is refused.

.PARAMETER PayloadRoot
    Where the publish output is written. Defaults to `bin\publish\win-x64`, the folder the publish
    profile names.

.PARAMETER Destination
    Optional. The application's master folder on the share, normally
    `\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Application Store\MTM Waitlist Application`.
    Runs only under the deployment account, because users have read-only access to that share.

.PARAMETER ApplicationId
    The loader catalogue id. Must match the `id` in the loader's `catalogue.json`.

.PARAMETER Executable
    The executable the loader catalogue names. Must match `executable` in `catalogue.json`.

.EXAMPLE
    ./scripts/store/publish-app-payload.ps1

    Publishes, writes and verifies the manifest, and changes nothing outside the repository.

.EXAMPLE
    ./scripts/store/publish-app-payload.ps1 -Destination '\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Application Store\MTM Waitlist Application'

    The same, then copies the verified payload to the store as the deployment account.

.NOTES
    Companion document: docs/scripts/scripts-store-publish-app-payload.md
#>
[CmdletBinding()]
param(
    [string] $Version = '',
    [string] $PayloadRoot = '',
    [string] $Destination = '',
    [string] $ApplicationId = 'waitlist',
    [string] $Executable = 'MTM_Waitlist.exe'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot 'Directory.Build.props'
$binaryRoot = Join-Path $repoRoot 'bin'

function Stop-WithRefusal {
    param([string] $Reason)
    Write-Host "[publish] refused: $Reason" -ForegroundColor Red
    exit 2
}

if (-not $PayloadRoot) {
    $PayloadRoot = Join-Path $binaryRoot 'publish\win-x64'
}

# ── 1. Agree the version ────────────────────────────────────────────────────────────────────────────
$declared = [regex]::Match(
    (Get-Content -LiteralPath $propsPath -Raw),
    '<Version>\s*(?<version>\d+\.\d+\.\d+)\s*</Version>').Groups['version'].Value

if (-not $declared) {
    Stop-WithRefusal "Directory.Build.props declares no 3-part <Version>."
}

if ($Version -and $Version -ne $declared) {
    Stop-WithRefusal "-Version $Version disagrees with Directory.Build.props ($declared). Edit the props file, which is the only place the version is written."
}

$Version = if ($Version) { $Version } else { $declared }

$executablePath = Join-Path $PayloadRoot $Executable

# ── 2. Clear the payload folder, then publish ───────────────────────────────────────────────────────
# `dotnet publish` adds to a folder; it never removes what an earlier publish left behind. A stale file
# would then be hashed into the manifest and copied to every workstation, so the folder is cleared first.
$resolvedBinaryRoot = [System.IO.Path]::GetFullPath($binaryRoot)
$resolvedPayloadRoot = [System.IO.Path]::GetFullPath($PayloadRoot)

if (-not $resolvedPayloadRoot.StartsWith($resolvedBinaryRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-WithRefusal "-PayloadRoot must be inside $resolvedBinaryRoot, so a mistake cannot delete a real folder."
}

if (Test-Path -LiteralPath $resolvedPayloadRoot) {
    Remove-Item -LiteralPath $resolvedPayloadRoot -Recurse -Force
}

Write-Host "[publish] publishing $Version to $resolvedPayloadRoot"

# `-p:Platform=x64` is passed globally, not only through the profile: the referenced module libraries build
# for the platform the publish names, and without it they build as AnyCPU and Win2D warns that its native
# assembly may not be referenced correctly (WIN2D0001).
& dotnet publish (Join-Path $repoRoot 'MTM_Waitlist.csproj') -p:PublishProfile=win-x64-selfcontained -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    Stop-WithRefusal "dotnet publish failed with exit code $LASTEXITCODE."
}

# ── 3. Refuse an unusable payload ───────────────────────────────────────────────────────────────────
$files = @(Get-ChildItem -LiteralPath $resolvedPayloadRoot -Recurse -File -Force)

if ($files.Count -eq 0) {
    Stop-WithRefusal "the publish output is empty."
}

if (-not (Test-Path -LiteralPath $executablePath)) {
    Stop-WithRefusal "the payload does not contain $Executable, which is what the loader catalogue names."
}

# ── 4. Hash every file, then write the manifest last ───────────────────────────────────────────────
$entries = foreach ($file in $files | Sort-Object { $_.FullName }) {
    [pscustomobject]@{
        path   = [System.IO.Path]::GetRelativePath($resolvedPayloadRoot, $file.FullName).Replace('\', '/')
        size   = $file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    applicationId = $ApplicationId
    version       = $Version
    publishedUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    executable    = $Executable
    fileCount     = $entries.Count
    totalBytes    = ($entries | Measure-Object -Property size -Sum).Sum
    files         = $entries
}

$manifestPath = Join-Path $resolvedPayloadRoot 'build-manifest.json'
$temporaryPath = "$manifestPath.tmp"

$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $temporaryPath -Encoding utf8NoBOM
Move-Item -LiteralPath $temporaryPath -Destination $manifestPath -Force

# A hash over the canonical file list, which is what the loader's own release manifest records. It is
# printed for the release log; two publishes of the same payload must print the same value.
$canonical = ($entries | ForEach-Object { "$($_.path)|$($_.size)|$($_.sha256)" }) -join "`n"
$canonicalHash = [System.Convert]::ToHexString(
    [System.Security.Cryptography.SHA256]::HashData(
        [System.Text.Encoding]::UTF8.GetBytes($canonical))).ToLowerInvariant()

# ── 5. Verify a folder against its manifest ────────────────────────────────────────────────────────
function Test-Payload {
    param([string] $Folder)

    $path = Join-Path $Folder 'build-manifest.json'
    if (-not (Test-Path -LiteralPath $path)) {
        throw "$Folder has no build-manifest.json."
    }

    $read = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $listed = @{}
    foreach ($entry in $read.files) {
        $listed[$entry.path] = $entry
    }

    $onDisk = @(Get-ChildItem -LiteralPath $Folder -Recurse -File -Force |
        Where-Object { $_.Name -ne 'build-manifest.json' } |
        ForEach-Object { [System.IO.Path]::GetRelativePath($Folder, $_.FullName).Replace('\', '/') })

    $missing = @($listed.Keys | Where-Object { $onDisk -notcontains $_ })
    $unlisted = @($onDisk | Where-Object { -not $listed.ContainsKey($_) })

    if ($missing.Count -gt 0) { throw "listed but not present: $($missing -join ', ')" }
    if ($unlisted.Count -gt 0) { throw "present but not listed: $($unlisted -join ', ')" }

    foreach ($relative in $onDisk) {
        $entry = $listed[$relative]
        $full = Join-Path $Folder $relative

        if ((Get-Item -LiteralPath $full).Length -ne $entry.size) {
            throw "size differs: $relative"
        }

        $hash = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne $entry.sha256) {
            throw "hash differs: $relative"
        }
    }

    return $read
}

Write-Host "[publish] verifying $resolvedPayloadRoot against its manifest"
$null = Test-Payload -Folder $resolvedPayloadRoot

# ── 6. Copy to the store, if asked ─────────────────────────────────────────────────────────────────
if ($Destination) {
    $resolvedDestination = [System.IO.Path]::GetFullPath($Destination)

    if ($resolvedDestination.StartsWith($repoRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-WithRefusal "-Destination must be the application's master folder on the share, not a folder inside this repository."
    }

    # A version is never reused with a different payload, because two workstations would then hold
    # different builds under the same number and the loader could not tell them apart.
    $existingManifestPath = Join-Path $resolvedDestination 'build-manifest.json'
    if (Test-Path -LiteralPath $existingManifestPath) {
        $existing = Get-Content -LiteralPath $existingManifestPath -Raw | ConvertFrom-Json

        if ($existing.version -eq $Version) {
            $existingCanonical = ($existing.files | ForEach-Object { "$($_.path)|$($_.size)|$($_.sha256)" }) -join "`n"
            $existingHash = [System.Convert]::ToHexString(
                [System.Security.Cryptography.SHA256]::HashData(
                    [System.Text.Encoding]::UTF8.GetBytes($existingCanonical))).ToLowerInvariant()

            if ($existingHash -ne $canonicalHash) {
                Stop-WithRefusal "the store already holds $Version with a different payload. Bump the version rather than reusing one that is already published."
            }

            Write-Host "[publish] the store already holds this exact payload for $Version; nothing to copy"
            exit 0
        }

        if ([version]$existing.version -gt [version]$Version) {
            Stop-WithRefusal "the store already holds $($existing.version), which is newer than $Version. Publishing it would offer every workstation an older build."
        }
    }

    Write-Host "[publish] copying the payload to $resolvedDestination"
    New-Item -ItemType Directory -Force -Path $resolvedDestination | Out-Null

    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($resolvedPayloadRoot, $file.FullName)
        $target = Join-Path $resolvedDestination $relative
        $targetFolder = Split-Path -Parent $target

        if (-not (Test-Path -LiteralPath $targetFolder)) {
            New-Item -ItemType Directory -Force -Path $targetFolder | Out-Null
        }

        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    # The manifest is written last: a folder that is still being filled is never advertised.
    Copy-Item -LiteralPath $manifestPath -Destination $existingManifestPath -Force

    Write-Host "[publish] verifying the copy at $resolvedDestination"
    $null = Test-Payload -Folder $resolvedDestination
}

# ── 7. Report ──────────────────────────────────────────────────────────────────────────────────────
$totalMegabytes = [math]::Round($manifest.totalBytes / 1MB, 1)

Write-Host ''
Write-Host "[publish] $ApplicationId $Version"
Write-Host "[publish] payload       $resolvedPayloadRoot"
Write-Host "[publish] files         $($manifest.fileCount)  ($totalMegabytes MB)"
Write-Host "[publish] manifest      $manifestPath"
Write-Host "[publish] payload hash  $($canonicalHash.Substring(0, 16))…"

if ($Destination) {
    Write-Host "[publish] store         $([System.IO.Path]::GetFullPath($Destination))"
}

Write-Host "[publish] verified against build-manifest.json"
exit 0
