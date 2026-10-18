# MCP-First Documentation and Code Sample Retrieval
Use available MCP servers to ground implementation decisions before writing or changing code.
Canonical MCP-first policy (Context7, Microsoft Learn) is maintained in
`.github/instructions/mcp-doc-research.instructions.md` — that file is the single source of truth
for tool names and server roles. Do not restate it here.
# Output Control Invariants
- Be highly terse and concise. Never explain code unless explicitly requested.
- Strip all conversational filler, introductory pleasantries, and summarizing conclusions.
- Prioritize raw information density over prose — but never at the cost of being readable.
- **Do NOT print code blocks to show a change.** Make the edit with a tool. A code block in chat is
  justified only when the person asked to see that code, or when quoting one line that genuinely matters.
- **Fences must be renderable.** A fence starts at column 0. A fence indented by four or more spaces, or
  placed inside a list item, renders as literal backticks and garbles the rest of the reply (GitHub
  Flavored Markdown spec, "Indented Code Block (4 Spaces)"). Content that contains a fence goes inside a
  longer backtick run. Never print a sample block that was only a template for a file you are writing.
- **Anything the person must do comes last**, under the heading `## What you need to do`; if nothing, end
  with the single line `Nothing needed from you.`
- Inside any `/speckit.*` step, `.github/instructions/response-format.instructions.md` (constitution
  Principle VII) governs the reply: plain English, end-user facing, no raw machine output.
## Repo-Specific Focus
For this repository, prioritize MCP-backed validation for:
- WinUI 3 and Windows App SDK APIs
- App notifications and packaging flows
- Local AI and Foundry-related guidance
- Module_Setup shell-workflow implementation (now `MTM_Waitlist.Setup` + app Views) and the SQL queue persistence path for Infor Visual lookups under Database/InforVisual/Queues/Module
## CSV Schema Exploration
- The workspace CSV MCP server is configured in `.mcp.json` as `csv-mcp-server`.
- Its storage root is `docs/database`, which contains the schema exports split into folders by type (`tables/`, `column-details/`, `indexes/`, `foreign-keys/`, …). Each file is named `database-inforvisual-<type>-<object>.csv`.
- Use the CSV server for CSV-specific profiling, filtering, sorting, grouping, validation, and export when its session can be loaded successfully.
- Use code intelligence and search tools for repository code symbols, Context7 for library documentation, and Microsoft Learn for official Windows guidance; the CSV server complements those tools rather than replacing them.
- Verified CSV server capability endpoint: `CSV Editor` version `1.0.0`, with load, filter, sort, group, statistics, profile, validation, quality, anomaly, and export capabilities.
- Current limitation observed on 2026-08-03: `load_csv` fails before creating a session because the server emits a progress notification with text (`"Validating file..."`) where the MCP progress schema expects a numeric value. If this persists, use PowerShell/structured file reads for CSV inspection and record the failure rather than repeatedly retrying the same load operation.
- Do not use CSV mutation tools (`update`, `add`, `remove`, `delete`, or export over source files) against the Infor schema exports unless the user explicitly requests a data change. Prefer read-only analysis and export to a separate temporary/output path.

## XamlMcp UI Inspection (Debug Diagnostics)
- The workspace MCP server `xamlmcp` is registered in `.vscode/mcp.json` (command `xamlmcp`, args `--enable-driver` for native dialogs/windows). It is the global dotnet tool `XamlMcp.Server` (`xamlmcp check --json` validates the install and reports live instrumented apps).
- **NOT WIRED — verified 2026-09-09 and again 2026-09-13:** the WinUI in-process agent is **absent from this repo**. `XamlMcp` appears in no `.cs` or `.xaml` file and has no `PackageReference` in `MTM_Waitlist.csproj` (re-confirmed 2026-09-13 against every `.cs`/`.csproj` in the workspace), and `App.xaml.cs` contains no `WinUiXamlMcp.Attach()` call. The `xamlmcp` MCP **server** is still registered in `.vscode/mcp.json`, but with no in-process agent it has nothing to attach to — the workflow below cannot succeed until the `XamlMcp.WinUI` package reference and the `#if DEBUG` attach are restored in the app.
- **The CLI is installed and healthy (verified 2026-09-13):** `%USERPROFILE%\.dotnet\tools\xamlmcp.exe`, `check --json` reports `runtime pass (.NET 10.0.12)`, `server-version pass (1.0.0-preview.3)`, `discovery-directory pass` and `desktop-instances warning — found 0 live app(s)`. It is **not on `PATH`**, so invoke it by full path. `0 live app(s)` is the expected result given the agent is absent, not a tooling fault; the `winui3-ui-automation` instruction file carries the detail and the correction to its earlier "CLI is not installed" claim.
- Workflow to inspect the running app:
  1. Run the app in Debug (F5 or the built exe). The agent writes a discovery file under `%LOCALAPPDATA%\XamlMcp\instances\`.
  2. Call `list-apps` → `attach(instanceId)` → `tree` / `search` / `props` / `set-prop` / `screenshot` / `input` / `action` / `wait-for` / `hit-test` / `styles` / `resources` / `assets` / `open-asset`.
  3. `detach` when done; the app prunes its discovery file on graceful shutdown (the server prunes stale records on the next `check`).
- WinUI capability notes: `bindings` and `failures` return `unsupported-capability`; `input` is enabled only for `mechanism: "raw"`; screenshots use `RenderTargetBitmap` (native HWND/airspace/GPU content is not captured); pseudo-class and style-class search are disabled.
- Targets accept an exact `{snapshotId, nodeId}` or a locator (`automationId`, `name`, `type`, `text`, `styleClass`) optionally scoped by `within`; ambiguous locators return typed candidates instead of silently selecting a node.
- Troubleshooting (2026-08-22): if `mcp_xamlmcp_*` tools report "currently disabled by the user" even though they are all checked/enabled in the global Configure Tools dialog, the **running Copilot session has a stale tool snapshot** — it started before the XamlMcp tools were registered/enabled. There is no settings-file gate (no `chat.tools`/MCP allowlist/blacklist). Fix: start a **new chat session** so the tool list refreshes; verify the `xamlmcp` server shows as connected in the MCP panel. First-chance `InspectorRpcException`s appearing in the app debug output are expected: they are the agent's normal JSON-RPC error path for invalid/ambiguous tool requests (e.g., a locator that matches many nodes) and are caught internally — not crashes.
- When any `mcp_*` tool is missing, reports "currently disabled by the user", or throws an unexpected exception, **consult `/memories/repo/mcp-tooling.md` first** for the verified troubleshooting playbook (stale-tool-snapshot fix, CSV server limitation, XamlMcp notes) before re-deriving it from scratch.
- Interaction notes (2026-08-22): Setup/New Request work center card selection is **model-driven** — the blue outline + blue photo frame bind to the item model's `IsSelected` (`WorkCenterSelectionItem`/`SetupWorkstation`), and the grids use `SelectionMode="None"` + `IsItemClickEnabled="True"` + `ItemClick` handlers. `action` select / `set-prop IsSelected` on a `GridViewItem` does **not** show the highlight; use a real click (`input` kind click) to trigger `ItemClick`. In New Request, clicking a card also advances to the next step — click then use **Back** to capture the selected card. Card templates now set `AutomationProperties.Name` (bound to the work center name); note `props` does not enumerate `AutomationProperties.*`, and `search` `name` matches `x:Name` only (`automationId` matches `AutomationProperties.AutomationId`) — use `search` `text` or `hit-test` to locate cards. For cropped on-disk screenshots, XamlMcp's `screenshot` returns a resource URI that can't be saved; use `scripts/ui-automation/capture_app_region.ps1` + `scripts/ui-automation/ocr_png.ps1` (see `/memories/repo/mcp-tooling.md`).

## Code exploration — built-in tools only (owner decision, 2026-10-17)
- **This repo carries no code-graph or indexed-exploration MCP server.** The MCP-based code-exploration
  server was retired on 2026-10-17; do not re-add one, and do not depend on an external index being warm.
- Use the editor's code intelligence (symbol lookup, find references) for navigation, and
  `grep`/`glob`/`view` for text search and reading. Both are always available and need no setup.
- The retired **CodeGraphy** decision (2026-10-11) stands for the same reason: its graph duplicated work the
  built-in tools already do. Its `.codegraphy/` workspace config is gone, and older notes (including
  `/memories/repo/codegraphy-setup.md`) must not be acted on.

---

# Architecture & Structural Guidelines

## Project Snapshot
- WinUI 3 desktop app on .NET 10 (`MTM_Waitlist.csproj`) with per-module class libraries. Non-view code lives in `MTM_Waitlist.*` libraries (e.g. `MTM_Waitlist.Core` holds `PageService`, `NavigationService`); XAML Views stay in the app under `Module_*/Views` (composition root).
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

## AI Feature Guidance (forward-looking standard — NOT yet implemented)
- **Status — verified 2026-09-09:** there is currently **no AI integration** in this repo. `Foundry`, `phi-4-mini`, and `localhost:5272` appear nowhere in code or `appsettings.json`, and `Azure.AI.OpenAI` is not a `PackageReference`. Treat the bullets below as the intended standard for new work, not as a description of existing behavior.
- Default local AI integration target is Foundry Local OpenAI-compatible endpoint: `http://localhost:5272/openai/v1`.
- Default local model name: `phi-4-mini` unless requirements specify otherwise.
- Use `Azure.AI.OpenAI` for chat-completion style calls.
- Keep AI features privacy-first: local inference by default, no external endpoint assumptions, and no hardcoded secrets.
- Add defensive error handling and clear fallback UX when local model service is unavailable.

## Cached-Fallback & Helper Server Guidance (supersedes the retired mock-toggle standard)
- **There is no manual demo/mock mode, and none may be reintroduced** (FR-003/FR-014, constitution II). The retired
  `Feature.InforVisualMockData` / `Feature.RecvMockData` keys, `MockToggleService`, the mock routing/auto-force
  stack, the sample catalogs and `ISampleDataService` are all deleted; `RetiredSymbolAuditTests` fails the build
  if any of them returns. Do not add a feature toggle that substitutes demo data.
- **Internal stores are always read and written live.** `mtm_waitlist`, `mtm_wip_application_winforms` and
  `mtm_receiving_application` are the operational stores: every read and write goes to MySQL, and a failure is
  reported as a per-screen unavailable state (`Store`/`LastAttemptUtc`/`RetryCount`/`NextRetryUtc` + a manual
  retry), never substituted with sample rows and never turned into a persistent banner (FR-001, FR-021).
- **External reads fall back automatically, never on request.** Only Infor Visual reads use the cache: the five
  read shapes are resolved through `MTM_Waitlist.Mock` (`IVisualReadFallback<TRequest,TRow>`), which serves
  `sp_visual_<shape>_get` from the `mtm_mock` mirror with an identical result shape (FR-002/FR-004) when the
  reachability probe's **settled verdict** is `Cached` (it skips the live attempt entirely, and the verdict is
  established during startup so the shell never opens mid-decision) or when a live attempt finds the source
  **unreachable**. The verdict is the probe's, never the read's own failure; one successful probe returns it to
  `Live` and the next read goes live again. A **reachable-but-empty** live result is a real answer and is never
  replaced by cached data. The cache is never consulted for an internal-store read (FR-027).
- **The application never refreshes the cache.** Keeping the mirror warm is the on-host
  `MTM_Waitlist.Mock.Service`'s job (3-hour default cadence on the local-midnight grid). The app only *probes*
  reachability and reports cached-data age (FR-025).
- **Every data operation goes through a stored procedure** (constitution III): no inline or hard-coded statement
  text in application code, DML through the non-query seam that reports affected rows, and every schema artifact
  ships `create.sql` + `rollback.sql` in the same change. `InlineSqlAuditTests` (T098) fails the build otherwise.
- Module_Setup dunnage workflow now mirrors receiving-app UI patterns (type selection, part selection, tabbed review) but saves setup pair assignments instead of label-data rows.
- Quick Add in Module_Setup writes dunnage type/part definitions to `mtm_receiving_application` and is restricted to roles: Admin, Developer, Plant Manager, Setup Lead, Production Lead.

---

# Multi-File Agentic Execution Workflow
When executing complex, cross-file architectural edits, you must strictly move through these three sequential phases:

1. **Phase 1: Blueprint Discovery**
   - Use code intelligence (symbol lookup, find references) and `grep`/`glob` to map out the Views, ViewModels, and registration structures.
   - Ground architectural choices using documentation snippets gathered from `mcp_context7_get-library-docs`.
   - You must map out your file dependencies completely before touching any codebase code.

2. **Phase 2: Minimal Diff Application**
   - Apply small, high-fidelity edits matching the existing structural patterns of the project.
   - Do not orphan classes; modify both the layout layers, view-model structures, and dependency injection entries in the same pass.

3. **Phase 3: Compilation Validation**
   - Use your active **.NET Install Tool** and environment configurations to check your project builds cleanly.
   - Run a terminal compilation pass using your build execution tools (`dotnet build`).
   - **Self-Healing Loop**: If compiler errors occur, immediately copy the error text back into `mcp_context7_resolve-library-id` or your documentation lookups to cross-reference formatting rules. Attempt this self-healing fix twice before throwing a failure back to the user.

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

## Subagent Model (owner instruction, 2026-10-11)
- **Every background/subagent task MUST run on DeepSeek V4.1 Flash** — pass `model: "deepseek/deepseek-flash"`
  on the `task` call.
- **Do not confuse it with `deepseek/deepseek-v4-flash`.** The provider (`vizards.deepseek-v4-for-copilot`)
  ships both, and they are *different models*:
  | Model id | Display name | Version |
  | --- | --- | --- |
  | `deepseek-flash` | **DeepSeek V4.1 Flash** | `v4.1` ← the one to use |
  | `deepseek-v4-flash` | DeepSeek V4 Flash | `v4` |
  | `deepseek-v4-flash-vision-exp` | DeepSeek V4 Flash (vision, experimental) | `v4` |
  | `deepseek-v4-pro` | DeepSeek V4 Pro | `v4` |
  Verified 2026-10-11 by reading `out/consts.js` in
  `%USERPROFILE%\.vscode\extensions\vizards.deepseek-v4-for-copilot-0.9.4`: the entry with
  `name: 'DeepSeek V4.1 Flash'` carries `id: 'deepseek-flash'`. An earlier version of this section wrongly
  claimed the two were the same model and told agents to use `v4-flash`; that was wrong and is corrected here.
- This **overrides** the usual "leave `model` unset and let the runtime resolve `/subagents` preferences" default,
  because the owner has explicitly required a specific model for this repo's subagents.
- It applies to every agent type that accepts a model override — `Explore`, `task`, `general-purpose`,
  `code-review`, `research`, and the custom agents registered for this repo.
- Do **not** substitute a GitHub-hosted model (Claude, GPT, Gemini, Grok, Kimi, MAI) on a `task` call, and do not
  fall back to one because a DeepSeek call failed. If DeepSeek V4 Flash is unavailable, say so and stop rather
  than silently using a different provider.
- `reasoning_effort` and `context_tier` stay unset unless the request in hand names them.

## Workstation Elevation (this environment)
- **The normal loop needs no elevation.** `dotnet build`, `dotnet test`, launching the unpackaged `MTM_Waitlist.exe`,
  running `MTM_Waitlist.Mock.Service.exe` from a user-writable folder, and the service's per-user
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry all run as the signed-in developer account.
- **When administrator rights are genuinely required** — repairing the Windows App SDK/MSIX tooling, writing
  machine-wide registry keys or services/firewall rules, or driving an **elevated** window with UI Automation —
  use the local admin account recorded in `.github/memories/repo/workstation-elevation.md`. Raise the prompt and
  type the password there rather than passing it through a command line.
- That note carries an **owner-approved exception** to this repository's "no hardcoded secrets" / "sanitize
  credentials" rules, scoped to that one workstation. Do not carry the pattern into `appsettings.json`, code,
  tests, other docs, or any other host.
## Workstation Service Secrets (this environment)
- **The `MTM_Waitlist.Mock.Service` host is the shared MySQL / Infor Visual host at `172.16.1.104`**
  (`V-MTMFG-5.mantoolmfg.com`), *not* the `MTMFG-161` development workstation — `workstation-elevation.md` draws that
  same distinction. The service is installed at `C:\Services\MTM_Waitlist.Mock.Service\`.
- **The agent must not run the deployment script unless its environment IS the server.** Use
  `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` (README beside it) — it publishes, redeploys, installs
  and verifies the secrets, starts the service, health-checks it, and **refuses to run with exit code 2 when the local
  machine's own IPv4 addresses do not include the expected server address**. That means VS Code must be running on the
  server itself. Do not bypass the guard (`-AllowNonServerHost`) or hand-roll the steps on another machine; hand the
  deployment to someone on the host instead. Full rule and rationale:
  `docs/operations/operations-mock-service-install-script.md`.
- **Owner-approved exception (2026-09-11):** the agent may **set and re-set the service's secrets as per-user
  (`HKCU\Environment`) environment variables on that host without asking again**. The variables, their values,
  their source of truth, the verification steps and the guardrails are recorded in
  `.github/memories/repo/workstation-secrets.md` — read that note before touching them. In short:
  `MTM_WAITLIST_DB_CONNECTION_STRING` (shared MySQL connection string the cache and all four stores reuse),
  `MTM_MYSQL_PASSWORD`, `INFOR_VISUAL_SQL_USER`, `INFOR_VISUAL_SQL_PASSWORD`. The install script sets them for you.
- The exception is scoped to **that host and these development credentials only**. Do not carry the pattern into
  `appsettings.json`, code, tests, other docs, or any other host, and do not route the values through a prompt, a
  command line, or a log.
- The service's own state is **not** in the install folder: configuration, run records and backups live under
  `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\`. Deleting only the install folder keeps the credential and backups.
## Known Build Quirks
- `PRI175` / `PRI224 root node not found` during `dotnet build` is usually stale PRI artifacts or a running `MTM_Waitlist.exe` locking the output — not a code error. Stop the running app, delete stale `*.pri` under `obj/`/`bin/`, and rebuild before debugging the code.
- `WMC9999: Could not find any resources appropriate for the specified culture ... ErrorMessages.resources` during `dotnet build` is a **MASKED XAML error, not an environment problem**. This machine's WindowsAppSDK 2.3.1 `XamlCompiler` (in `tools\net472` of the `microsoft.windowsappsdk.winui` package) is missing its `ErrorMessages.resources` satellite, so the compiler cannot report the underlying cause — any real XAML compile error (bad type, bad binding, wrong member name) surfaces only as this generic WMC9999. Treat WMC9999 as "there is a real XAML error somewhere; the tool can't tell you where." To surface the real error: temporarily introduce a deliberate C# error (e.g. duplicate a command) so the compiler reports the actual file/type problem, or bisect by simplifying the recently-changed XAML files.
- CommunityToolkit.Mvvm `[RelayCommand]` STRIPS a trailing `Async` from the method name when generating the command: `private async Task ContinueToReviewAsync()` must be bound in XAML as `Command="{x:Bind ViewModel.ContinueToReviewCommand}"` (NOT `...AsyncCommand`). Binding the wrong name is a silent XAML failure that shows up only as the masked `WMC9999` above.
- **Do not add explicit `<Page>` items for Views that live in the app project.** The SDK's default items already include `Module_*/Views/**/*.xaml`, so naming them again raises `NETSDK1022` (duplicate `Page` items). When a View must be excluded, use a `Remove` item (as the `mtm_waitlist.Mock.Service` exclusion does), never an `Include`.

## Destructive-Tooling Traps (read before running anything under `Database/`)

- **`.github/scripts/validate-database-schema.ps1` destroyed the live `mtm_waitlist` on 2026-10-16.** Its repair path applied `Database/Bootstrap/create_database.sql`, whose first statement is `DROP DATABASE IF EXISTS mtm_waitlist;` **to a store that already existed and was merely one deploy behind** — every table came back empty, and the store was recovered from the Mock service's 01:00 dump. Fixed: the bootstrap now applies **only when the database is genuinely absent**, and **schema repair is opt-in (`-ApplySchema`)**. A plain run reports a gap and exits; it never writes. Full record: `docs/defects/defects-database-critical-schema-validator-dropped-the-live-waitlist-store.md`.
- **`-DatabaseName` does not redirect that script's install.** Every artifact under `Database/Tables/**` carries its own `USE mtm_waitlist;`, so the install always targets `mtm_waitlist` whatever the connection string names. Never treat `-DatabaseName` as a sandbox.
- **The plant host has `log_bin = OFF`.** There are no MySQL binary logs off `172.16.1.104`, so there is no point-in-time recovery: the Mock service's daily dump under `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\backups\<database>\` is the only copy, and it is a day old. Treat every write to a store on that host as irreversible for the last day.
- **Several repo scripts write when run.** `refresh_schema_drift.ps1 -Apply`, `verify_mtm_mock_deploy.ps1` (drops `mtm_mock`), and `validate-database-schema.ps1 -ApplySchema` all rewrite a store. Read the switch before running any of them; the read-only forms are the drift report with no `-Apply`, `build_mtm_mock_masters.ps1 -Check`, and `Database/Mock/Validation/read_shape_metadata/validate.sql`.

<!-- BEGIN token-budget concise-mode -->
<!-- toggled: 2026-09-12 -->

## Token Budget — concise mode (active)

When executing any `/speckit.*` command (constitution, specify,
clarify, plan, tasks, analyze, implement, checklist,
token-budget.*), follow these output rules:

- Do not narrate plans, intentions, or steps. Run them.
- Do not recap the user's prompt back to them.
- Do not announce file writes ("I'll create...", "Now writing..."). Just write.
- After completing the command, output only:
  1. The list of files created or changed, one per line.
  2. Any blocking question or unmet assumption, in one sentence.
  3. The single line "Done." if there is nothing else to report.
- Tables, fenced code, and structured data inside artifacts are
  unaffected — this rule governs only the chat-channel prose around
  them.
- Override on request: if the user explicitly asks "explain", "walk
  me through", "why", or "what did you do", drop concise mode for
  that single reply and answer normally.

These rules apply only inside `/speckit.*` workflows. Conversational
replies outside SDD steps are not affected.

<!-- END token-budget concise-mode -->
