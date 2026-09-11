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
- Check the newest file in that folder before trusting it — **but do not conclude the log service is
  down from this folder alone**. `LocalSettings.json` may override
  `Startup.Logging.HostedVmLogDirectory`; on `MTMFG-161` it points at the shared path
  `X:\Software Development\Live Applications\MTM_Waitlist\Logs`, so the **local** daily folder is
  frozen at `startup_daily_2026_07_30.jsonl` while `startup_forwarded_<yyyy_MM_dd>.jsonl` on `X:` is
  current. Read both before deciding. (`X:` is a real mapped drive on this workstation.)
- Read the UIA text dump as the primary signal. It shows what the user sees, which the startup log
  does not.
- **To get past the startup gate on a workstation whose `appsettings.json` still points at `localhost`,
  both connection overrides are needed** (`MTM_WAITLIST_DB_CONNECTION_STRING` alone was not enough on
  2026-09-11):

  ```powershell
  $cs = 'Server=172.16.1.104;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;AllowPublicKeyRetrieval=True;'
  $env:MTM_WAITLIST_DB_CONNECTION_STRING = $cs
  $env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING = $cs
  ```

  With only the first set, the app still showed *"Could not validate startup session from the
  database"*; with both set it reached the **Sign in** window. That is the difference between testing
  navigation and testing nothing but the failure dialog.
- Reaching the **shell** needs a sign-in as well, so a full navigation test needs credentials for a
  valid account. Without them, steps 3 (header text) and 5 (navigation) cannot be exercised — only the
  pre-shell windows can.

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

Never locate the MTM_Waitlist window by title. The title is **not stable**: `MainWindow` is a
`WinUIEx.WindowEx`, and on 2026-09-11 the splash window reported the framework default
`WinUI Desktop` while the sign-in window reported `Sign in`. A title lookup is therefore not merely
wrong for this app, it is wrong for *some windows and states and not others* — which is worse,
because it works until it does not. Use the process id (`FromHandle($p.MainWindowHandle)`) or
enumerate the process's top-level windows as above.

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

`Add-Type` in this workspace behaves the **opposite** way to what a shared session would suggest, so
this is worth stating plainly (verified 2026-09-11 on Windows PowerShell 5.1.26100.9444):

- **Each command runs in its own session, so a type defined in one step is NOT available in the next.**
  A later step that only calls `[Win32WindowState]::GetWindowPlacement(...)` fails with *"Cannot find
  type"*. Re-declare the type in every command that needs it, and prefer putting the declaration and
  its use in the same command.
- **Adding the same definition twice inside one session does not fail.** The warning about
  "`Add-Type` fails if the type already exists" did not reproduce here; the second call was a no-op
  success. Do not rely on an exception to tell you whether the type is loaded — test for it with
  `([System.Management.Automation.PSTypeName]'Win32WindowState').Type` if you need to branch.

The practical upshot: combine the window-state read with its own `Add-Type` in a single command rather
than assuming the type survives from the previous step.

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
  dump surfaces the failure message immediately.
  **Observed (2026-09-11)** — the dump read exactly:

  ```text
  Launching MTM Waitlist
  Could not validate startup session from the database. Try again.
  We could not connect to the database. Retry or close the application.
  ```

  The buttons are a **separate query**: `Retry` and `Close` are `ControlType.Button`, so they do *not*
  appear in the `ControlType.Text` dump. Enumerate `ControlType.Button` to see them:

  ```powershell
  $bc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button)
  $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,$bc) | ForEach-Object { $_.Current.Name }
  # Retry
  # Close
  ```
- Startup or navigation that lands on the wrong page, or on an empty shell.
- Window regressions such as a window that shows before it is activated, or a page that resizes the
  main window and silently un-maximizes it.

## Verification status (2026-09-11, current Debug build)

The recipes above were exercised against the built app
(`bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`, 2026-09-11 07:58) on
`MTMFG-161`:

| Claim | Result |
|---|---|
| Executable path | **correct** — file present and current |
| `Start-Process` returns immediately and console output is not captured | **correct** |
| `MainWindowHandle` resolves once a window exists | **correct** |
| Window lookup by process id | **correct** — 1 top-level window found |
| Never locate by window title | **confirmed, and worse than documented** — splash reported `WinUI Desktop`, sign-in reported `Sign in` |
| Text dump identifies the page | **correct** — returned the failure dialog, then the sign-in text |
| Buttons appear in the text dump | **incorrect as written** — they are `ControlType.Button`; corrected above |
| `GetWindowPlacement` / `GetWindowRect` recipe | **correct** — returned `showCmd=1`, `760x460` (splash), `820x760` (sign-in) |
| `showCmd` 1/2/3 = normal/minimized/maximized | **consistent** (`1` on both windows) |
| Startup log staleness check | **the guidance was wrong and is corrected above** — the local daily folder IS frozen (newest `startup_daily_2026_07_30.jsonl`), but that is a `LocalSettings.json` redirect to the shared `X:` path, **not** a stopped log service: `X:\…\Logs\startup_forwarded_2026_09_11.jsonl` is current and was being written during the run |
| Close recipe leaves no orphan | **correct** — 0 instances afterwards |
| `Add-Type` fails on a duplicate type | **did not reproduce** — see the note in step 4; the real hazard here is the opposite (types do not survive between commands) |

**Not verified.** Step 5 (navigation with `SelectionItemPattern`) and the shell header text of step 3.
Both need the app past the **Sign in** gate, and no valid account credential was used for this test
run, so the shell was never reached. The `NavigationViewItem` → `ControlType.ListItem` +
`SelectionItemPattern` claim therefore remains untested against this build; treat it as the next thing
to check when a sign-in is available.

## Relationship to XamlMcp

- `.github/copilot-instructions.md` documents `xamlmcp` as the intended in-app UI inspector, and
  records it as **NOT WIRED**: the `XamlMcp.WinUI` in-process agent is absent (no attach call in
  `App.xaml.cs` and no `PackageReference`), so the server has nothing to attach to.
- **The CLI is not installed on this machine either (verified 2026-09-11).** `xamlmcp` is not on
  `PATH`, and `%USERPROFILE%\.dotnet\tools` (with its `.store`) is **empty** for the `jkoll` profile —
  so `xamlmcp check --json` cannot be run here at all. That is stronger than "not wired": there is no
  tool to attach even if the in-process agent were restored. Check both before spending time on the
  server, and note that the tool would have to be installed per profile.
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
- Match privilege levels. A non-elevated shell cannot automate an elevated window. When this workstation needs
  administrator rights, use the account recorded in `.github/memories/repo/workstation-elevation.md`; raise the
  prompt and type the password there rather than passing it through a command line.
- Treat UI driving as read-only by default. Do not trigger destructive actions such as delete,
  approve, or submit unless the user asked for that flow.
- Always close the app when finished so no orphan process is left holding database connections.
- Prefer a build plus a focused test for logic. Use UI automation for what only the running app can
  prove.
