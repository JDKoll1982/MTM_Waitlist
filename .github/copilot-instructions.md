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

## XamlMcp UI Inspection (Debug Diagnostics)
- The workspace MCP server `xamlmcp` is registered in `.vscode/mcp.json` (command `xamlmcp`, args `--enable-driver` for native dialogs/windows). It is the global dotnet tool `XamlMcp.Server` (`xamlmcp check --json` validates the install and reports live instrumented apps).
- The WinUI in-process agent (`XamlMcp.WinUI` 1.0.0-preview.3, already referenced in `MTM_Waitlist.csproj`) is attached in `App.xaml.cs` under `#if DEBUG` (`WinUiXamlMcp.Attach()` + `RegisterWindow` for the splash, login, and main windows). Nothing listens in Release builds.
- Workflow to inspect the running app:
  1. Run the app in Debug (F5 or the built exe). The agent writes a discovery file under `%LOCALAPPDATA%\XamlMcp\instances\`.
  2. Call `list-apps` → `attach(instanceId)` → `tree` / `search` / `props` / `set-prop` / `screenshot` / `input` / `action` / `wait-for` / `hit-test` / `styles` / `resources` / `assets` / `open-asset`.
  3. `detach` when done; the app prunes its discovery file on graceful shutdown (the server prunes stale records on the next `check`).
- WinUI capability notes: `bindings` and `failures` return `unsupported-capability`; `input` is enabled only for `mechanism: "raw"`; screenshots use `RenderTargetBitmap` (native HWND/airspace/GPU content is not captured); pseudo-class and style-class search are disabled.
- Targets accept an exact `{snapshotId, nodeId}` or a locator (`automationId`, `name`, `type`, `text`, `styleClass`) optionally scoped by `within`; ambiguous locators return typed candidates instead of silently selecting a node.
- Troubleshooting (2026-08-22): if `mcp_xamlmcp_*` tools report "currently disabled by the user" even though they are all checked/enabled in the global Configure Tools dialog, the **running Copilot session has a stale tool snapshot** — it started before the XamlMcp tools were registered/enabled. There is no settings-file gate (no `chat.tools`/MCP allowlist/blacklist). Fix: start a **new chat session** so the tool list refreshes; verify the `xamlmcp` server shows as connected in the MCP panel. First-chance `InspectorRpcException`s appearing in the app debug output are expected: they are the agent's normal JSON-RPC error path for invalid/ambiguous tool requests (e.g., a locator that matches many nodes) and are caught internally — not crashes.
- When any `mcp_*` tool is missing, reports "currently disabled by the user", or throws an unexpected exception, **consult `/memories/repo/mcp-tooling.md` first** for the verified troubleshooting playbook (stale-tool-snapshot fix, CSV server limitation, XamlMcp notes) before re-deriving it from scratch.
- Interaction notes (2026-08-22): Setup/New Request work center card selection is **model-driven** — the blue outline + blue photo frame bind to the item model's `IsSelected` (`WorkCenterSelectionItem`/`SetupWorkstation`), and the grids use `SelectionMode="None"` + `IsItemClickEnabled="True"` + `ItemClick` handlers. `action` select / `set-prop IsSelected` on a `GridViewItem` does **not** show the highlight; use a real click (`input` kind click) to trigger `ItemClick`. In New Request, clicking a card also advances to the next step — click then use **Back** to capture the selected card. Card templates now set `AutomationProperties.Name` (bound to the work center name); note `props` does not enumerate `AutomationProperties.*`, and `search` `name` matches `x:Name` only (`automationId` matches `AutomationProperties.AutomationId`) — use `search` `text` or `hit-test` to locate cards. For cropped on-disk screenshots, XamlMcp's `screenshot` returns a resource URI that can't be saved; use `tools/capture_app_region.ps1` + `tools/ocr_png.ps1` (see `/memories/repo/mcp-tooling.md`).

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
- Default local AI integration target is Foundry Local OpenAI-compatible endpoint: `http://172.16.1.104:5272/openai/v1`.
- Default local model name: `phi-4-mini` unless requirements specify otherwise.
- Use `Azure.AI.OpenAI` for chat-completion style calls.
- Keep AI features privacy-first: local inference by default, no external endpoint assumptions, and no hardcoded secrets.
- Add defensive error handling and clear fallback UX when local model service is unavailable.

## Mock Data & Helper Server Guidance
- The app now supports app-data-backed mock toggles persisted through the existing local settings storage path rather than the database:
   - `Feature.InforVisualMockData` (default On)
   - `Feature.RecvMockData` (default Off)
- When the relevant mock toggle is enabled, helper services for read-only and read/write interactions should short-circuit to the mock-data path and call the sample-data service instead of continuing to backend execution.
- The shared routing pattern is: `SearchButton -> helper server -> mock-data setting check -> sample-data service -> requested action`.
- Implement helper-server behavior through DI-registered services that depend on `ILocalSettingsService` and `ISampleDataService`.
- Module_Setup dunnage workflow now mirrors receiving-app UI patterns (type selection, part selection, tabbed review) but saves setup pair assignments instead of label-data rows.
- Quick Add in Module_Setup writes dunnage type/part definitions to `mtm_receiving_application` and is restricted to roles: Admin, Developer, Plant Manager, Setup Lead, Production Lead.

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
- Do not call `ConfigureAwait()` on `ContentDialog.ShowAsync()`; it returns `IAsyncOperation<ContentDialogResult>` and must be awaited directly.
- When helper methods return nullable value tuples, guard or destructure before reading `.Item1`/`.Item2` to avoid `CS1061` nullability errors.
- If a new converter or XAML resource is introduced, register the matching `xmlns` and `x:Key` in `App.xaml` in the same change; missing registration causes XAML compile failures such as `WMC0001`.
- Validate WinUI patches with a compile/test pass before closing the task.
- **NEVER TOUCH GENERATED ARTIFACTS**: Avoid touching files under `obj/` or generated `*.g.cs` / `*.g.i.cs` build artifacts.

---

# Interaction & Execution Guidance

## Ask Clarifying Questions
- When a requirement is ambiguous or has multiple reasonable interpretations, ask a small number of clarifying questions before implementing (use the questions tool when available). Do not guess on ambiguous scope, naming, behavior, or layout.
- Respect terse feedback loops: keep questions focused and minimal.

## Persona Adherence
- When a task names a persona (Database Engineer, Backend Engineer, Frontend Engineer, Tech Lead, Full Stack Engineer), adopt that persona's working style and output format. See `.github/skills/checklist-execution/SKILL.md` for the persona styles.
- When the user references a checklist file (`checklist.md`, `WAITLIST_REQUEST_WORKFLOW_TASKS.md`, or any task checklist) or asks to "work through" / "execute" / "validate" a checklist, **auto-invoke the `checklist-execution` skill** and drive it task-by-task using each task's declared persona — do not wait for the user to re-paste the skill instructions or drive every task manually.

## Large-Task Resilience
- For whole-repo exploration or validation (e.g., "validate all tasks in `<checklist>`", "summarize all docs"), delegate read-only research to the `Explore` subagent instead of reading many files into the main context.
- Work in small, verifiable steps and state progress as you go. If a long operation fails or is interrupted, explicitly say what completed and resume from that point rather than restarting from scratch.

## Known Build Quirks
- `PRI175` / `PRI224 root node not found` during `dotnet build` is usually stale PRI artifacts or a running `MTM_Waitlist.exe` locking the output — not a code error. Stop the running app, delete stale `*.pri` under `obj/`/`bin/`, and rebuild before debugging the code.
- `WMC9999: Could not find any resources appropriate for the specified culture ... ErrorMessages.resources` during `dotnet build` is a **MASKED XAML error, not an environment problem**. This machine's WindowsAppSDK 2.3.0 `XamlCompiler` (in `tools\net472` of the `microsoft.windowsappsdk.winui` package) is missing its `ErrorMessages.resources` satellite, so the compiler cannot report the underlying cause — any real XAML compile error (bad type, bad binding, wrong member name) surfaces only as this generic WMC9999. Treat WMC9999 as "there is a real XAML error somewhere; the tool can't tell you where." To surface the real error: temporarily introduce a deliberate C# error (e.g. duplicate a command) so the compiler reports the actual file/type problem, or bisect by simplifying the recently-changed XAML files.
- CommunityToolkit.Mvvm `[RelayCommand]` STRIPS a trailing `Async` from the method name when generating the command: `private async Task ContinueToReviewAsync()` must be bound in XAML as `Command="{x:Bind ViewModel.ContinueToReviewCommand}"` (NOT `...AsyncCommand`). Binding the wrong name is a silent XAML failure that shows up only as the masked `WMC9999` above.
