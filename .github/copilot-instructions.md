# Copilot Instructions

## Project Snapshot
- WinUI 3 desktop app on .NET 10 (`MTM_Waitlist.csproj`) with a companion core library (`MTM_Waitlist.Core`).
- The app uses a single host-managed window and a Template Studio-style shell/navigation setup.

## Architecture to Preserve
- `App.xaml.cs` builds the DI host and registers pages, view models, and services; prefer adding dependencies there rather than creating objects manually.
- `ActivationService`, `NavigationService`, `NavigationViewService`, and `PageService` drive page navigation by view-model full name.
- `ShellPage.xaml` is the main shell; `MainWindow.xaml.cs` handles window-level lifecycle and title-bar/theme hooks.

## Code Patterns in This Repo
- View models commonly derive from `ObservableRecipient` and use MVVM Toolkit source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Navigation-aware view models implement `INavigationAware` and load data in `OnNavigatedTo`.
- UI strings are localized through `"ResourceKey".GetLocalized()`.
- Custom title-bar behavior is centralized in `Helpers/TitleBarHelper.cs` and uses `App.AppTitlebar`.

## Platform/Runtime Rules
- Guard MSIX-only APIs with `RuntimeHelper.IsMSIX` (`AppNotificationService`, `LocalSettingsService`, `SettingsViewModel`).
- Avoid touching files under `obj/` or generated `*.g.cs` / `*.g.i.cs` artifacts.
- Keep WinUI/dispatcher work on the UI thread; shutdown code should be idempotent and avoid using disposed services.

## Developer Workflow
- Prefer small, minimal diffs that fit the existing Template Studio structure.
- Validate changes with a full build (`dotnet build` or Visual Studio build) after code edits.
- There are no dedicated test projects in the solution right now; build validation is the primary safety check.

## Helpful Files
- `App.xaml.cs` - host setup, service registration, app lifetime.
- `Views/ShellPage.xaml(.cs)` - shell layout and navigation wiring.
- `ViewModels/*.cs` - MVVM Toolkit patterns and navigation behavior.
- `Services/*.cs` - theme, settings, navigation, activation, and notifications.
- `Helpers/*.cs` - localization, title bar, runtime, and navigation helpers.
