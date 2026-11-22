# WinUI 3 windowing — full rules

## Decisions (from the scaling master)

- Resize reflows content; no whole-window zoom.
- Remember window size and position between sessions — each window separately.
- Enforce a sensible minimum window size.
- Offer a true fullscreen mode.
- Apply screen-setting changes live (automatic in WinUI 3).
- Minimum usable width: 500 epx (snap-layout compatible; 330 epx if you need wider support).
- No special portrait handling — desktop/landscape only.
- Title bar: standard for main/settings windows; custom for modal/focused windows; document both.

## Minimum size — the interop pattern

WinUI 3 has no native min/max size. Handle `WM_GETMINMAXINFO` through the window's `Subclass`:

```csharp
// Conceptual — verify the P/Invoke signature against this SDK before using.
const int WM_GETMINMAXINFO = 0x0024;

[StructLayout(LayoutKind.Sequential)]
struct POINT { public int X; public int Y; }

[StructLayout(LayoutKind.Sequential)]
struct MINMAXINFO
{
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
}

// In the min-size handler: convert 500 epx to physical pixels for this window's DPI,
// then set ptMinTrackSize.X / .Y.
```

The correct conversion is `epx * windowDpi / 96`. Get the window DPI with `GetDpiForWindow(hwnd)`
(not `GetDpiForSystem`), so the minimum stays correct when the window is dragged between monitors.

Sources: [How to Set a Minimum Window Size in WinUI](https://albertakhmetov.com/posts/2025/how-to-set-a-minimum-window-size-in-winui/)
and [WindowSizeLimiterWinUI3](https://github.com/MEHDIMYADI/WindowSizeLimiterWinUI3) (reference only —
explain the gap, don't add the dependency).

## Fullscreen

```csharp
var appWindow = window.AppWindow;
appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
// Leave: appWindow.SetPresenter(AppWindowPresenterKind.Default);
```

## Per-window size/position persistence

Persist `Window.AppWindow.Position` and `Size` (in physical pixels) per window, keyed by a stable
per-window id. Restore on `Window.Activated`/`Launch`, clamped to the current work area. The
`WindowManager` helper in WinUIEx covers this; prefer teaching the underlying `AppWindow` calls so
the behaviour is not hidden behind a package.

## Snap layouts

- Support a minimum width ≤ 500 epx so the app works in every snap zone.
- The maximize button automatically shows snap layouts on Windows 11; nothing to implement.

## Title bar — when to use which

| Window | Title bar |
| --- | --- |
| Main window | Standard system title bar |
| Settings window | Standard system title bar |
| Modal / focused workflow | Custom edge-to-edge (`ExtendsContentIntoTitleBar = true` + `SetTitleBar`) |

Custom-title-bar gotchas:
- Set `ExtendsContentIntoTitleBar = true` in the window constructor, before the first frame.
- Keep interactive content out of the caption-button hit area (`SetTitleBar` defines the drag
  region; anything overlapping the caption buttons steals their clicks).
- On Windows 10 and with transparency effects off, custom title bars must still provide contrast.

## Orientation

Desktop/landscape only unless the app genuinely ships to tablets; do not add portrait triggers for a
windowed desktop app.
