---
applyTo: "**/*"
---

# WinUI 3 UI Automation for Runtime Verification

How to drive and inspect the running MTM_Waitlist app from PowerShell so navigation, startup
routing, and window state can be verified without a human looking at the screen.

## Bottom line

- WinUI 3 exposes a full UI Automation (UIA) tree, the same tree Narrator reads.
- Windows PowerShell already ships the `UIAutomationClient` and `UIAutomationTypes` assemblies,
  so no install, package, or code change is needed.
- `NavigationViewItem` navigation can be performed programmatically with
  `SelectionItemPattern.Select()`. You do not have to move the mouse or click coordinates.
- This is the technique that works for this repository today. The registered `xamlmcp` server
  cannot attach yet; see [Relationship to XamlMcp](#relationship-to-xamlmcp).
- If a desktop-automation MCP (for example WinApp MCP) is added later, prefer it for screenshots
  and visual diffs, and keep this PowerShell recipe for quick state reads.

## When to use this

Use it after any change where the observable behavior is "which page appears" or "what the
window looks like", and a build or unit test cannot prove the outcome. Examples:

- Startup routing (the app must land on a specific page rather than an empty shell).
- Shell navigation between `NavigationView` destinations.
- Window sizing, centering, maximize, and restore-size changes.
- Smoke checks where a startup dialog means the app never reached the shell.

## Workflow

1. Build the app.
2. Launch the built executable.
3. Dump the visible text to see which page rendered.
4. Read the window placement to confirm size and maximized state.
5. Navigate with `SelectionItemPattern` and re-read text and placement.
6. Close the app.

Do each step as its own command. The round trip between commands gives the UI thread time to
catch up, which removes the need for blind sleeps.

## 1. Launch the app

```powershell
$exe = "c:\Users\jkoll\source\repos\MTM_Waitlist\bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe"
Start-Process $exe
```

- The project is `<OutputType>WinExe</OutputType>`, so `Start-Process` returns immediately and the
  shell prompt comes back. That is expected, not a failure, and console output is not captured.
- For behavior that runs before the shell loads, use startup diagnostics instead of stdout. The
  `StartupDebugLog` helper writes to `Debug.WriteLine` (visible only when a debugger is attached)
  and forwards to `StartupLogService`, which appends JSON Lines to
  `%LOCALAPPDATA%\MTM_Waitlist\Logs\Startup\startup_daily_<yyyy_MM_dd>.jsonl`. That directory is
  overridable through `StartupLoggingOptions.HostedVmLogDirectory`.
- Check the newest file in that folder before trusting it. A file older than your run means the
  startup log service did not start, so fall back to the UIA text dump and a debugger session.
- Read the UIA text dump as the primary signal. It shows what the user sees, which the startup log
  does not.

## 2. Connect to the window

```powershell
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
$p = Get-Process MTM_Waitlist | Select-Object -First 1
if ($p.MainWindowHandle -eq 0) { "no window yet" }
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
```

`MainWindowHandle` is `0` until the first top-level window exists. Re-run this step rather than
adding a sleep loop.

### Locate a window by process id

Use this when `MainWindowHandle` does not resolve, or when several windows exist and you must know
which one you are talking to.

```powershell
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
$windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$windows | ForEach-Object { "$($_.Current.ControlType.ProgrammaticName) | '$($_.Current.Name)' | hwnd=$($_.Current.NativeWindowHandle)" }
```

Never locate the MTM_Waitlist window by title. `MainWindow` is a `WinUIEx.WindowEx` and the title
has been observed to report the framework default `WinUI Desktop` instead of the localized
`MTM_Waitlist` name.

## 3. Read the current page

Text elements are the cheapest, most stable signal of what is on screen.

```powershell
$textCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $textCondition) | ForEach-Object { $_.Current.Name } | Where-Object { $_ -ne '' }
```

Read the header text first. The shell page title identifies the active destination, which is the
fastest proof that routing or startup navigation worked.

## 4. Read window placement and size

Window geometry is not exposed as a UIA property, so call `user32` directly.

```powershell
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class Win32WindowState {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct WINDOWPLACEMENT {
        public int length, flags, showCmd;
        public POINT minPosition, maxPosition;
        public RECT normalPosition;
    }
    [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
}
"@

$h = $p.MainWindowHandle
$placement = New-Object Win32WindowState+WINDOWPLACEMENT
$placement.length = [System.Runtime.InteropServices.Marshal]::SizeOf($placement)
[void][Win32WindowState]::GetWindowPlacement($h, [ref]$placement)
$rect = New-Object Win32WindowState+RECT
[void][Win32WindowState]::GetWindowRect($h, [ref]$rect)

"showCmd=$($placement.showCmd) actual=$($rect.Right - $rect.Left)x$($rect.Bottom - $rect.Top) restore=$($placement.normalPosition.Right - $placement.normalPosition.Left)x$($placement.normalPosition.Bottom - $placement.normalPosition.Top)"
```

`showCmd` values: `1` normal, `2` minimized, `3` maximized. The `normalPosition` rectangle is the
size the window returns to when you un-maximize, which is how you confirm that a maximize still
leaves a sensible restore size.

`Add-Type` fails if the type already exists in the session. Use the same snippet in one session, or
open a fresh terminal before redefining it.

## 5. Navigate

`NavigationViewItem` automation peers report `ControlType.ListItem` and support
`SelectionItemPattern`. Selecting is equivalent to a user click and raises the same navigation.

```powershell
$navCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Work Center Setup')
$item = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $navCondition) | Select-Object -First 1
$item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
```

Resolution order for any control:

1. `AutomationProperties.AutomationId` when the control declares one.
2. The localized `Name`.
3. `ControlType` plus `FindAll` index when several elements share a name.

Pattern fallbacks when the primary pattern is unsupported:

- `SelectionItemPattern.Select()` for `NavigationViewItem`, `ListViewItem`, and tab-like items.
- `InvokePattern.Invoke()` for `Button`.
- Real mouse input as a last resort, for custom-templated controls whose only automation children
  are `Text` elements. Warning: this moves the user's cursor.

`$item` below is the element you resolved in the step above. For a templated card, resolve the
`Text` element it exposes and click that `BoundingRectangle` center.

```powershell
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class MouseInput {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, IntPtr extra);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero);
    }
}
"@

$r = $item.Current.BoundingRectangle
[MouseInput]::Click([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
```

After navigating, re-run steps 3 and 4. The point of the exercise is to confirm that the new page
rendered and that the window geometry did not change unexpectedly.

## 6. Close the app

```powershell
Get-Process MTM_Waitlist -ErrorAction SilentlyContinue | Stop-Process -Force
```

## MTM_Waitlist anchors

| What | Where |
| ---- | ----- |
| Shell and navigation | `Module_Core/Views/ShellPage.xaml`, `NavigationViewControl` |
| Nav destination: Waitlist | `x:Uid="Shell_WaitlistView"`, label `Waitlist` |
| Nav destination: Work Center Setup | `x:Uid="Shell_ModuleSetup"`, label `Work Center Setup` |
| Content frame | `x:Name="NavigationFrame"` |
| Window | `MainWindow.xaml`, `WinUIEx.WindowEx` with `PersistenceId="MainWindow"` |
| Executable | `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe` |

Nav labels come from `Strings/en-us/Resources.resw`. A `x:Uid` rename or label edit changes the
automation `Name`, so dump names again after touching resources instead of trusting a stale script.

`PersistenceId` means WinUIEx persists window placement between runs. Close the app before
measuring, and launch fresh before concluding that a sizing change did or did not take effect.

## What this catches

- Startup that never reaches the shell, for example a database connection `ContentDialog`. The text
  dump surfaces the failure message and its `Retry` and `Close` buttons immediately.
- Startup or navigation that lands on the wrong page, or on an empty shell.
- Window regressions such as a window that shows before it is activated, or a page that resizes the
  main window and silently un-maximizes it.

## Relationship to XamlMcp

- `.github/copilot-instructions.md` documents `xamlmcp` as the intended in-app UI inspector, and
  records it as **NOT WIRED**: the `XamlMcp.WinUI` in-process agent is absent (no attach call in
  `App.xaml.cs` and no `PackageReference`), so the server has nothing to attach to.
- Until that agent is restored, use the PowerShell UIA recipe in this file. It needs no app change
  and no restored package reference.
- When XamlMcp is wired again, prefer it for `tree`, `search`, `props`, `set-prop`, `action`,
  `wait-for`, and `screenshot`, because it sees XAML node identity, style classes, and resources
  that UIA does not expose.
- Keep this recipe for window-state reads even then. XamlMcp screenshots use `RenderTargetBitmap`
  and do not capture native HWND or airspace content, and it has no equivalent of
  `GetWindowPlacement` for maximize and restore-size checks.
- `xamlmcp check --json` reporting a live instrumented app is the signal that attaching is
  possible. If it reports nothing, fall back to this file.
- Keep `.github/copilot-instructions.md` as the source of truth for server status. Update it when
  the agent is restored rather than restating status here.

## Rules

- Verify the running app; do not add automation hooks to production code. If a control needs a
  stable hook, add `AutomationProperties.AutomationId` in XAML as the deliberate fix.
- Do not modify the build to make automation easier. Automate the same artifact users run.
- Run one automation session at a time, and avoid it while the user is typing. Automation takes
  focus and can move the cursor.
- Match privilege levels. A non-elevated shell cannot automate an elevated window.
- Treat UI driving as read-only by default. Do not trigger destructive actions such as delete,
  approve, or submit unless the user asked for that flow.
- Always close the app when finished so no orphan process is left holding database connections.
- Prefer a build plus a focused test for logic. Use UI automation for what only the running app can
  prove.
