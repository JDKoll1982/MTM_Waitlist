# capture_app_region.ps1
# Captures cropped regions of the running MTM_Waitlist window to PNG files.
# Coordinates are XamlMcp "root-logical" (window client area) coordinates.
# Usage examples:
#   .\capture_app_region.ps1 -OutDir "Documents\2026-08-23-User-Update-Email" -Name "demo.png" -X 340 -Y 190 -W 1160 -H 540
#   .\capture_app_region.ps1 -OutDir "..." -Full
param(
    [string]$OutDir,
    [string]$Name = "capture.png",
    [double]$X = 0,
    [double]$Y = 0,
    [double]$W = 100,
    [double]$H = 100,
    [switch]$Full
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Capture {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

$proc = Get-Process -Name "MTM_Waitlist" -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $proc) { Write-Error "MTM_Waitlist window not found."; exit 1 }
$hwnd = $proc.MainWindowHandle

# Bring to foreground so CopyFromScreen captures it (it is visible on screen).
[Win32Capture]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 300

$winRect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($hwnd, [ref]$winRect) | Out-Null
$pt = New-Object Win32Capture+POINT
$pt.X = 0; $pt.Y = 0
[Win32Capture]::ClientToScreen($hwnd, [ref]$pt) | Out-Null
$clientOriginX = $pt.X
$clientOriginY = $pt.Y
$clientRect = New-Object Win32Capture+RECT
[Win32Capture]::GetClientRect($hwnd, [ref]$clientRect) | Out-Null
$clientW = $clientRect.Right - $clientRect.Left
$clientH = $clientRect.Bottom - $clientRect.Top

Write-Output "Window rect: $($winRect.Left),$($winRect.Top) size $($winRect.Right-$winRect.Left)x$($winRect.Bottom-$winRect.Top)"
Write-Output "Client origin (screen): $clientOriginX,$clientOriginY  client size: ${clientW}x${clientH}"

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
$path = Join-Path $OutDir $Name

if ($Full) {
    $sx = $clientOriginX; $sy = $clientOriginY; $sw = $clientW; $sh = $clientH
} else {
    $sx = [int]($clientOriginX + $X)
    $sy = [int]($clientOriginY + $Y)
    $sw = [int]$W
    $sh = [int]$H
}

$bmp = New-Object System.Drawing.Bitmap($sw, $sh)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($sx, $sy, 0, 0, (New-Object System.Drawing.Size($sw, $sh)))
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "Saved $path  (${sw}x${sh} at screen $sx,$sy)"
