<#
.SYNOPSIS
    Builds a multi-resolution .ico from a square source PNG.

.DESCRIPTION
    Writes a PNG-payload ICO (Vista+). PNG payloads are used for every frame rather
    than classic BMP/DIB entries because DIB frames above 48x48 need a separate AND
    mask, which System.Drawing does not emit. Windows Explorer, the taskbar and
    WinUIEx all read PNG-payload frames.

.PARAMETER Source
    Source PNG. Square is expected; non-square images are stretched to a square frame.

.PARAMETER Destination
    .ico to write. Parent folder must exist.

.PARAMETER Sizes
    Frame sizes to embed, largest last. Defaults to the Windows icon set.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Source,
    [Parameter(Mandatory)][string]$Destination,
    [int[]]$Sizes = @(16, 24, 32, 48, 64, 128, 256)
)

Add-Type -AssemblyName System.Drawing

function New-IconFramePng {
    param([System.Drawing.Image]$Image, [int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($Image, 0, 0, $Size, $Size)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()

    $ms.Dispose()
    $bmp.Dispose()

    return , $bytes
}

$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $Source).ProviderPath)

$frames = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($size in $Sizes) {
    $frames.Add((New-IconFramePng -Image $sourceImage -Size $size))
}
$sourceImage.Dispose()

$stream = [System.IO.File]::Create((Join-Path (Get-Location) $Destination))
$writer = New-Object System.IO.BinaryWriter($stream)

# ICONDIR
$writer.Write([uint16]0)              # reserved
$writer.Write([uint16]1)              # type: icon
$writer.Write([uint16]$frames.Count)

# ICONDIRENTRY table, then the payloads
$offset = 6 + (16 * $frames.Count)
for ($i = 0; $i -lt $frames.Count; $i++) {
    $dimension = $Sizes[$i]
    # 0 means 256 in the single-byte dimension fields.
    $dimensionByte = [byte]$(if ($dimension -ge 256) { 0 } else { $dimension })

    $writer.Write($dimensionByte)     # width
    $writer.Write($dimensionByte)     # height
    $writer.Write([byte]0)            # palette count
    $writer.Write([byte]0)            # reserved
    $writer.Write([uint16]1)          # colour planes
    $writer.Write([uint16]32)         # bits per pixel
    $writer.Write([uint32]$frames[$i].Length)
    $writer.Write([uint32]$offset)

    $offset += $frames[$i].Length
}

foreach ($frame in $frames) {
    $writer.Write($frame)
}

$writer.Flush()
$writer.Dispose()
$stream.Dispose()

Write-Host ("{0}  <-  {1}  ({2} frames, {3} bytes)" -f $Destination, $Source, $frames.Count, (Get-Item -LiteralPath $Destination).Length)
