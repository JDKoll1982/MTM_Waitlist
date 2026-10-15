---
applyTo: "**/*.{xaml,cs}"
---

# WinUI 3 Modern API Rules
- For WinUI 3 and Windows App SDK API questions, use your documentation servers before finalizing code loops.
- Generate and maintain WinUI 3 code with `Microsoft.UI.Xaml` namespaces. Never introduce legacy `Windows.UI.Xaml` namespaces.
- Use `ContentDialog` for confirmations and inline result display flows.
- Keep layouts Fluent and accessible with clear spacing and readable contrast.
- Bind views through MVVM patterns already used in this repo.
- Keep navigation integrated with existing shell/navigation services instead of creating parallel navigation stacks.
- Ensure UI-thread safety for window and XAML interactions. Keep WinUI/dispatcher work on the UI thread; shutdown code should be idempotent and avoid using disposed services.
- Guard MSIX-only APIs with `RuntimeHelper.IsMSIX` (`AppNotificationService`, `LocalSettingsService`, `SettingsViewModel`).
- Do not call `ConfigureAwait()` on `ContentDialog.ShowAsync()`; it returns `IAsyncOperation<ContentDialogResult>` and must be awaited directly.
- When helper methods return nullable value tuples, guard or destructure before reading `.Item1`/`.Item2` to avoid `CS1061` nullability errors.
- If a new converter or XAML resource is introduced, register the matching `xmlns` and `x:Key` in `App.xaml` in the same change; missing registration causes XAML compile failures such as `WMC0001`.
- Validate WinUI patches with a compile/test pass before closing the task.
- **Every tooltip goes through `shared:TooltipBehavior`,** never `ToolTipService.ToolTip`. `TooltipBehavior.ResourceKey` names the localized text (the developer map is preferred when the signed-in role is developer), `FallbackText` is the English fallback, and `AssociatedFiles` (pipe-separated) feeds the control inspector. A value-echo tooltip sets `FallbackText` only. A control that declares **neither** gets no tooltip — that is deliberate, because the tooltip service's placeholder ("More details") would otherwise be drawn on controls whose only tooltip was the value already on screen. The rule is enforced by `TooltipCoverageTests`.
- **A diagnostic surface that is opened over a dialog must be a `Window`, not a page.** A `ContentDialog` owns its `XamlRoot` and blocks it, so a page navigated to behind the dialog can never be seen; a window has no `XamlRoot` to be blocked by. This is why the control inspector is `ControlInspectorDetailWindow` (`specs/011-identifier-first-signin/contracts/window-stage-contract.md` §1 records the scope: it is a diagnostic window, not a stage).
- **NEVER TOUCH GENERATED ARTIFACTS**: Avoid touching files under `obj/` or generated `*.g.cs` / `*.g.i.cs` build artifacts.
