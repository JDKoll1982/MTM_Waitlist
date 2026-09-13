# Captures the running MTM_Waitlist window to a PNG, raising it first.
# -WaitSeconds waits for the main window to appear, so a caller does not have to guess how long startup takes
# (the shell is not shown until the startup session has been validated against the database).
param(
    [string]$Out = "$env:TEMP\mtm_list.png",
    [switch]$Maximize,
    [int]$WaitSeconds = 30
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class MtmCapture {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
}
'@

# A process exists before its window does, and the FIRST window is the splash, not the shell. Wait for a window
# that is actually the shell rather than for "a window": the splash is frameless, so it exposes no caption
# buttons, while the shell exposes Minimize/Restore/Close.
function Test-IsShellWindow {
    param([IntPtr]$WindowHandle)

    try {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
        if (-not $root) { return $false }

        $condition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)
        $buttons = @($root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition))

        return @($buttons | Where-Object { $_.Current.Name -eq 'Minimize' }).Count -gt 0
    }
    catch {
        return $false
    }
}

$deadline = (Get-Date).AddSeconds($WaitSeconds)
$proc = $null
while ((Get-Date) -lt $deadline -and -not $proc) {
    $candidate = Get-Process -Name MTM_Waitlist -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if ($candidate -and (Test-IsShellWindow -WindowHandle $candidate.MainWindowHandle)) { $proc = $candidate }
    if (-not $proc) { Start-Sleep -Milliseconds 500 }
}

if (-not $proc) {
    Write-Error "MTM_Waitlist shell window did not appear within $WaitSeconds second(s)."
    exit 1
}

$hwnd = $proc.MainWindowHandle
[void][MtmCapture]::ShowWindow($hwnd, $(if ($Maximize) { 3 } else { 9 }))
[void][MtmCapture]::BringWindowToTop($hwnd)
[void][MtmCapture]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 900

$raised = ([MtmCapture]::GetForegroundWindow() -eq $hwnd)
Write-Output "foreground-is-app: $raised"

$rect = New-Object MtmCapture+RECT
[void][MtmCapture]::GetWindowRect($hwnd, [ref]$rect)
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
Write-Output "rect: $($rect.Left),$($rect.Top) ${w}x${h}"

$bitmap = New-Object System.Drawing.Bitmap($w, $h)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, [System.Drawing.Size]::new($w, $h))
$bitmap.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
Write-Output $Out
