# Counts the dunnage pictures on the share and in each candidate local folder.
$ErrorActionPreference = 'Continue'

$share = '\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\Shared\Images\Dunnage'
$receivingLocal = Join-Path $env:LOCALAPPDATA 'MTM_Receiving_Application\Assets\DunnageImages'
$receivingMachine = Join-Path $env:ProgramFiles 'MTM_Receiving_Application\Assets\DunnageImages'
$ours = Join-Path $env:LOCALAPPDATA 'MTM_Waitlist\ImageCache\dunnage'

function Show-Folder([string]$label, [string]$path) {
    if (Test-Path -LiteralPath $path) {
        $pictures = @(Get-ChildItem -LiteralPath $path -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.png', '.jpg', '.jpeg' })
        Write-Output ("{0}`n    exists : yes`n    path   : {1}`n    pictures: {2}" -f $label, $path, $pictures.Count)
    }
    else {
        Write-Output ("{0}`n    exists : no`n    path   : {1}" -f $label, $path)
    }
}

Show-Folder 'the shared dunnage root' $share
Show-Folder 'receiving app, per-user' $receivingLocal
Show-Folder 'receiving app, machine-wide' $receivingMachine
Show-Folder 'our own cache' $ours

if (Test-Path -LiteralPath $share) {
    Write-Output ''
    Write-Output 'the share, by folder:'
    Get-ChildItem -LiteralPath $share -Directory -ErrorAction SilentlyContinue |
        ForEach-Object {
            $count = @(Get-ChildItem -LiteralPath $_.FullName -File -ErrorAction SilentlyContinue).Count
            Write-Output ("    {0,-10} {1}" -f $_.Name, $count)
        }
}
