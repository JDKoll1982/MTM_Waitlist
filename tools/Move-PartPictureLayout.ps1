# Moves the pictures already stored on the picture share under the layout feature 008-part-pictures introduces.
#
#   <root>\work_center\...        ->  <root>\Waitlist\work_center\...
#   <root>\request_item\...       ->  <root>\Waitlist\request_item\...
#   <root>\request_category\...   ->  <root>\Waitlist\request_category\...
#
# The three kinds go into the application's own collection, one folder each, because two of them can carry names
# that differ only by letter case (the item `other` beside the category `Other`) and a file name cannot tell those
# apart (FR-038). Everything else at the root is left exactly where it is: a folder this script does not recognise
# is not this feature's to move, and moving it is how a picture ends up somewhere no reader looks.
#
# This is the file half of FR-036. The row half is `sp_config_images_locations_paths_move` with its reverse plus the
# paired seed `seed_picture_layout_move`, which rewrite the recorded values. Either order is safe: the reader
# resolves both layouts, so a picture is never unresolvable while the move is half done (research D13).
#
# NEVER run this from application startup, and never wire it into the build. Schema and data changes are
# hand-maintained, reviewed and promoted; this one rewrites every picture the application has stored. Run it by
# hand, read the report, and only then run the row half.
#
# Running it twice is safe. After a run the kind folders are gone from the root, so a second run finds nothing to
# do and reports zero moved. Nothing is ever overwritten: a file whose destination already exists is skipped and
# named, so a half-finished earlier attempt can be finished without losing anything.

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    # The picture root, which is <c>ImageStorage.SharedFolderPath</c>. Read from appsettings.json when omitted.
    [string]$Root,

    # Where to read the configured root from when -Root is not given.
    [string]$AppSettingsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolved in the body rather than in the param block: $PSScriptRoot is not populated while default arguments are
# being evaluated under -File, and an empty path there fails before any work is reported.
if ([string]::IsNullOrWhiteSpace($AppSettingsPath)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    $AppSettingsPath = Join-Path (Split-Path -Parent $scriptDirectory) 'appsettings.json'
}

# The three kind folders this feature moves, and the collection they move into. Spelled here as the script's own
# contract; they are the values AppStoragePaths.ApplicationOwnKindFolderNames and PartPictureLayout.WaitlistCollection
# hold, and a change to either belongs in both places.
$CollectionFolder = 'Waitlist'
$KindFolders = @('work_center', 'request_item', 'request_category')

function Resolve-PictureRoot {
    param([string]$Configured, [string]$SettingsPath)

    if (-not [string]::IsNullOrWhiteSpace($Configured)) {
        return $Configured
    }

    if (-not (Test-Path -LiteralPath $SettingsPath)) {
        throw "No -Root was given and no settings file was found at '$SettingsPath'. Pass -Root explicitly."
    }

    $settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
    $value = $settings.ImageStorage.SharedFolderPath

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Neither -Root nor ImageStorage.SharedFolderPath in '$SettingsPath' names a picture root."
    }

    return $value
}

$resolvedRoot = Resolve-PictureRoot -Configured $Root -SettingsPath $AppSettingsPath

Write-Output "Picture root: $resolvedRoot"

if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
    Write-Error "The picture root '$resolvedRoot' is not a folder that can be reached. Nothing was moved."
    exit 1
}

$collectionPath = Join-Path $resolvedRoot $CollectionFolder
$moved = New-Object System.Collections.Generic.List[string]
$skipped = New-Object System.Collections.Generic.List[string]
$leftAlone = New-Object System.Collections.Generic.List[string]

foreach ($entry in Get-ChildItem -LiteralPath $resolvedRoot -Directory) {
    $isKnownKind = $KindFolders -contains $entry.Name.ToLowerInvariant()

    if (-not $isKnownKind) {
        $leftAlone.Add($entry.Name)
        continue
    }

    if ($entry.Name -ceq $CollectionFolder) {
        # Not reachable: the comparison above already excluded it. Kept as a statement of intent rather than a bug.
        $leftAlone.Add($entry.Name)
        continue
    }

    $sourceKindPath = $entry.FullName
    $destinationKindPath = Join-Path $collectionPath $entry.Name

    # Every file, at every depth, so a partially finished earlier run can be completed without merging two folders
    # in one step — which is what would overwrite without asking.
    foreach ($file in Get-ChildItem -LiteralPath $sourceKindPath -Recurse -File) {
        $relative = $file.FullName.Substring($sourceKindPath.Length).TrimStart('\', '/')
        $destination = Join-Path $destinationKindPath $relative

        if (Test-Path -LiteralPath $destination) {
            $skipped.Add("$relative (already at $destinationKindPath)")
            continue
        }

        $destinationFolder = Split-Path -Parent $destination

        if (-not (Test-Path -LiteralPath $destinationFolder -PathType Container)) {
            if ($PSCmdlet.ShouldProcess($destinationFolder, 'Create folder')) {
                New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
            }
        }

        if ($PSCmdlet.ShouldProcess($destination, 'Move picture')) {
            Move-Item -LiteralPath $file.FullName -Destination $destination
            $moved.Add("$relative -> $CollectionFolder\$($entry.Name)")
        }
    }

    # The now-empty kind folder goes, so a second run has nothing to walk and the root reads as the new layout.
    # Only when it is genuinely empty: anything left behind is something this script did not understand, and it
    # stays where it is.
    $remaining = @(Get-ChildItem -LiteralPath $sourceKindPath -Recurse -File)
    if ($remaining.Count -eq 0) {
        if ($PSCmdlet.ShouldProcess($sourceKindPath, 'Remove emptied folder')) {
            Remove-Item -LiteralPath $sourceKindPath -Recurse -Force
        }
    }
    else {
        $leftAlone.Add("$($entry.Name) (kept: $($remaining.Count) file(s) this script does not recognise)")
    }
}

Write-Output ''
Write-Output "Moved:      $($moved.Count)"
foreach ($line in $moved) { Write-Output "  + $line" }

Write-Output "Not moved:  $($skipped.Count)"
foreach ($line in $skipped) {
    Write-Output "  ! $line"
    Write-Output "    The destination already holds a file. Nothing was overwritten; move or remove it by hand first."
}

Write-Output "Left alone: $($leftAlone.Count)"
foreach ($line in $leftAlone) { Write-Output "  - $line" }

Write-Output ''
Write-Output 'Files moved. The recorded paths are rewritten by Database/Seeds/seed_picture_layout_move/create.sql,'
Write-Output 'with its rollback beside it. The reader answers both layouts, so either order is safe.'
