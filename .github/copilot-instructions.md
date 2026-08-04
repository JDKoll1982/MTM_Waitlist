---
applyTo: "**/*"
---

# MCP-First Documentation and Code Sample Retrieval
Use available MCP servers to ground implementation decisions before writing or changing code.

## Serena MCP (Repository Exploration)
- Use Serena MCP tools (like `find_symbol`, `find_declaration`, or `get_symbols_overview`) for indexed codebase exploration and faster cross-file navigation.
- Prefer Serena-driven lookup before fallback text search when identifying existing implementations.
- Use Serena findings to narrow and validate edits before applying code changes.

## General Libraries and Frameworks
- Always resolve a library first with `context7_resolve-library-id`.
- Then fetch focused docs with `context7_get-library-docs`.
- Prefer MCP documentation over memory for APIs that may have changed.

## Repo-Specific Focus
For this repository, prioritize MCP-backed validation for:
- WinUI 3 and Windows App SDK APIs
- App notifications and packaging flows
- Local AI and Foundry-related guidance- Module_Setup shell-workflow implementation and the SQL queue persistence path for Infor Visual lookups under Database/InforVisual/Queues/Module
## CSV Schema Exploration
- The workspace CSV MCP server is configured in `.vscode/mcp.json` as `csv-mcp-server`.
- Its storage root is `Documents/Development/InforVisual/DatabaseCSVFiles`, which contains the schema exports split into folders by type.
- Use the CSV server for CSV-specific profiling, filtering, sorting, grouping, validation, and export when its session can be loaded successfully.
- Use Serena for repository code symbols, Context7 for library documentation, and Microsoft Learn for official Windows guidance; the CSV server complements those tools rather than replacing them.
- Verified CSV server capability endpoint: `CSV Editor` version `1.0.0`, with load, filter, sort, group, statistics, profile, validation, quality, anomaly, and export capabilities.
- Current limitation observed on 2026-08-03: `load_csv` fails before creating a session because the server emits a progress notification with text (`"Validating file..."`) where the MCP progress schema expects a numeric value. If this persists, use PowerShell/structured file reads for CSV inspection and record the failure rather than repeatedly retrying the same load operation.
- Do not use CSV mutation tools (`update`, `add`, `remove`, `delete`, or export over source files) against the Infor schema exports unless the user explicitly requests a data change. Prefer read-only analysis and export to a separate temporary/output path.

---

# Architecture & Structural Guidelines

## Project Snapshot
- WinUI 3 desktop app on .NET 10 (`MTM_Waitlist.csproj`) with module-owned core services under `Module_Core`.
- The app uses a single host-managed window and a Template Studio-style shell/navigation setup.

## Architecture to Preserve (DI & Routing)
- `App.xaml.cs` builds the Dependency Injection (DI) host and registers pages, view models, and services; prefer adding dependencies there rather than creating objects manually.
- `ActivationService`, `NavigationService`, `NavigationViewService`, and `PageService` drive page navigation by view-model full name.
- `ShellPage.xaml` is the main shell; `MainWindow.xaml.cs` handles window-level lifecycle and title-bar/theme hooks.

## Code Patterns in This Repo
- View models commonly derive from `ObservableRecipient` and use MVVM Toolkit source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Navigation-aware view models implement `INavigationAware` and load data in `OnNavigatedTo`.
- UI strings are localized through `"ResourceKey".GetLocalized()`.
- Custom title-bar behavior is centralized in `Helpers/TitleBarHelper.cs` and uses `App.AppTitlebar`.

## AI Feature Guidance (Repo Standard)
- Default local AI integration target is Foundry Local OpenAI-compatible endpoint: `http://localhost:5272/openai/v1`.
- Default local model name: `phi-4-mini` unless requirements specify otherwise.
- Use `Azure.AI.OpenAI` for chat-completion style calls.
- Keep AI features privacy-first: local inference by default, no external endpoint assumptions, and no hardcoded secrets.
- Add defensive error handling and clear fallback UX when local model service is unavailable.

## Mock Data & Helper Server Guidance
- The app now supports a boolean app-data setting named `Feature.UseMockData` persisted through the existing local settings storage path rather than the database.
- When mock data is enabled, helper services for read-only and read/write interactions should short-circuit to the mock-data path and call the sample-data service instead of continuing to backend execution.
- The shared routing pattern is: `SearchButton -> helper server -> mock-data setting check -> sample-data service -> requested action`.
- Implement helper-server behavior through DI-registered services that depend on `ILocalSettingsService` and `ISampleDataService`.

---

# Multi-File Agentic Execution Workflow
When executing complex, cross-file architectural edits, you must strictly move through these three sequential phases:

1. **Phase 1: Blueprint Discovery**
   - Use **Serena MCP** tool commands (`find_symbol`, `get_symbols_overview`) to map out the Views, ViewModels, and registration structures.
   - Ground architectural choices using documentation snippets gathered from `context7_get-library-docs`.
   - You must map out your file dependencies completely before touching any codebase code.

2. **Phase 2: Minimal Diff Application**
   - Use Serena's code insertion tools if necessary, or apply small, high-fidelity edits matching the existing structural patterns of the project.
   - Do not orphan classes; modify both the layout layers, view-model structures, and dependency injection entries in the same pass.

3. **Phase 3: Compilation Validation**
   - Use your active **.NET Install Tool** and environment configurations to check your project builds cleanly.
   - Run a terminal compilation pass using your build execution tools (`dotnet build`).
   - **Self-Healing Loop**: If compiler errors occur, immediately copy the error text back into `context7_resolve-library-id` or your documentation lookups to cross-reference formatting rules. Attempt this self-healing fix twice before throwing a failure back to the user.

---
applyTo: "**/{Package.appxmanifest,Package.appinstaller,*.csproj}"
---

# Packaging Guidance
Keep packaging flows aligned with modern Windows app packaging practices.

- When asked for CLI packaging examples, prefer `winapp pack --output ./publish`.
- Preserve existing package identity settings unless the task explicitly requests identity changes.
- For Store-readiness changes, update display name, description, and publisher consistently across manifest metadata layers simultaneously.
- Cross-reference packaging modifications against `RuntimeHelper.IsMSIX` dependencies to prevent breaking runtime activation pathways.

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
- **NEVER TOUCH GENERATED ARTIFACTS**: Avoid touching files under `obj/` or generated `*.g.cs` / `*.g.i.cs` build artifacts.
