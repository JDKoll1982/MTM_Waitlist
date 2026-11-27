# MTM_Waitlist — Repository Instructions

## Repo-Specific Focus
The prioritized MCP-backed validation list lives in `.github/instructions/mcp-doc-research.instructions.md`.
Repository-specific additions:
- Module_Setup shell-workflow implementation (now `MTM_Waitlist.Setup` + app Views) and the SQL queue persistence path for Infor Visual lookups under `Database/InforVisual/Queues/Module_*/`.
- When an MCP tool is missing, reports itself disabled, or throws unexpectedly, read
  `.github/memories/repo/mcp-tooling.md` before re-deriving the fix.

## Infor Visual Schema Exports
- The schema exports live under `docs/database`, split into 11 folders by type (`tables/`, `column-details/`, `indexes/`, `foreign-keys/`, …). Each file is named `database-inforvisual-<type>-<object>.csv`. **Every export is tab-delimited, not comma-delimited**, and **no file sits at the root** — path a read to its subfolder.
- Read them with `Import-Csv -Delimiter "`t"`, or with `Get-Content` for a quick look. Treat them as read-only reference data; do not edit them.

## UI Automation (what works today)
- `scripts/ui-automation/` drives the real application window through `UIAutomationClient`. Six driver faults that stopped a driven run were found and fixed on 2026-10-19 (an empty `-Exe` default from `$PSScriptRoot` in a parameter default, a thrown connection string, a hardcoded sign-in account, a missing `TogglePattern` arm, single-look finds, and a `NumberBox` that ignored a `ValuePattern` write). The recipe that works on this workstation, and each fault with its fix, are in
  **`.github/memories/repo/ui-automation-drive.md`** and **§11 of `docs/scripts/scripts-automation-ui-automation.md`** — read them before debugging a driven run, because every one of those faults complains in the app's voice.

## Code exploration — built-in tools only (owner decision, 2026-10-17)
- **This repo carries no code-graph or indexed-exploration MCP server.** The MCP-based code-exploration
  server was retired on 2026-10-17; do not re-add one, and do not depend on an external index being warm.
- Use the editor's code intelligence (symbol lookup, find references) for navigation, and
  `grep`/`glob`/`view` for text search and reading. Both are always available and need no setup.
- The retired **CodeGraphy** decision (2026-10-11) stands for the same reason: its graph duplicated work the
  built-in tools already do. Its `.codegraphy/` workspace config is gone, and older notes that mention it
  must not be acted on.

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
- Custom title-bar behavior is centralized in `MTM_Waitlist.Core/Helpers/TitleBarHelper.cs` and uses `App.AppTitlebar`.

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
- **External reads fall back automatically, never on request.** Only Infor Visual reads use the cache: the twelve
  read shapes are resolved through `MTM_Waitlist.Mock` (`IVisualReadFallback<TRequest,TRow>`), which serves
  `sp_visual_<shape>_get` from the `mtm_mock` mirror with an identical result shape (FR-002/FR-004) when the
  reachability probe's **settled verdict** is `Cached` (it skips the live attempt entirely, and the verdict is
  established during startup so the shell never opens mid-decision) or when a live attempt finds the source
  **unreachable**. The verdict is the probe's, never the read's own failure; one successful probe returns it to
  `Live` and the next read goes live again. A **reachable-but-empty** live result is a real answer and is never
  replaced by cached data. The cache is never consulted for an internal-store read (FR-027).
- **The generic request's reads travel shapes like every other Visual read.** Its part completions, sequence list,
  operation state and part-wide quantity are shapes 9, 10, 11 and 7, and its die search is shape 12, so the whole
  generic flow keeps working off the plant network. Do not reach `IVisualQueryExecutor` directly from a generic
  request read: that is what left those screens answering "unavailable" while the work-order flow kept working
  (`VisualReadRouteAuditTests`).
- **The application never refreshes the cache.** Keeping the mirror warm is the on-host
  `MTM_Waitlist.Mock.Service`'s job (3-hour default cadence on the local-midnight grid). The app only *probes*
  reachability and reports cached-data age (FR-025).
- **Every data operation goes through a stored procedure** (constitution III): no inline or hard-coded statement
  text in application code, DML through the non-query seam that reports affected rows, and every schema artifact
  ships `create.sql` + `rollback.sql` in the same change. `InlineSqlAuditTests` (T098) fails the build otherwise.
- Module_Setup dunnage workflow now mirrors receiving-app UI patterns (type selection, part selection, tabbed review) but saves setup pair assignments instead of label-data rows.
- Quick Add in Module_Setup writes dunnage type/part definitions to `mtm_receiving_application` and is restricted to roles: Admin, Developer, Plant Manager, Setup Lead, Production Lead.

---

# Interaction & Execution Guidance

## Ask Clarifying Questions
- When a requirement is ambiguous or has multiple reasonable interpretations, ask a small number of clarifying questions before implementing (use the questions tool when available). Do not guess on ambiguous scope, naming, behavior, or layout.
- Respect terse feedback loops: keep questions focused and minimal.

## Persona Adherence
- When a task names a persona (Database Engineer, Backend Engineer, Frontend Engineer, Tech Lead, Full Stack Engineer), adopt that persona's working style and output format. See `.github/skills/checklist-execution/SKILL.md` for the persona styles.
- When the user references a checklist file (`checklist.md`, a `tasks.md` under `specs/`, or any task checklist) or asks to "work through" / "execute" / "validate" a checklist, **auto-invoke the `checklist-execution` skill** and drive it task-by-task using each task's declared persona — do not wait for the user to re-paste the skill instructions or drive every task manually.

## Large-Task Resilience
- For whole-repo exploration or validation (e.g., "validate all tasks in `<checklist>`", "summarize all docs"), delegate read-only research to the `Explore` subagent instead of reading many files into the main context.
- Work in small, verifiable steps and state progress as you go. If a long operation fails or is interrupted, explicitly say what completed and resume from that point rather than restarting from scratch.

## Subagent Model (owner instruction, 2026-10-11)
- **Every background/subagent task MUST run on `deepseek/deepseek-flash`** (DeepSeek V4.1 Flash),
  passed as `model:` on the `task` call.
- It is **not** the same model as `deepseek/deepseek-v4-flash` (V4). Do not substitute a GitHub-hosted
  model, and do not fall back to one if DeepSeek is unavailable: say so and stop.
- `reasoning_effort` and `context_tier` stay unset unless the request in hand names them.
- Provenance, the full model-id table and the earlier correction are in
  `.github/memories/repo/subagent-models.md`.

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
## Database Environment (MySQL Server 8.0, MySQL Workbench)
- **MySQL Server 8.0 is the only engine.** The plant host `172.16.1.104` and the local development instance on
  `127.0.0.1:3306` both run **8.0.46** (verified 2026-11-02), so there is one server version to target and no
  cross-version syntax split. The local instance is the `MySQL80` service.
- **MySQL Workbench 8.0 is the client and inspection tool.** `mysql.exe` from the Server 8.0 install is the
  command-line equivalent; both live under `C:\Program Files\MySQL\`.
- **MAMP is retired and must not come back.** It is no longer installed as a service, is not used to reach any
  store, and must not appear in a script, an instruction, a memory note or a document. A MAMP client-discovery
  branch in a script is dead code — delete it rather than restoring it.
- Which server to read is a decision, not a default: `127.0.0.1` holds only what was deployed to it (`mtm_mock`,
  and `mtm_waitlist` only if something deployed it), while `172.16.1.104` holds every operational store and the
  warm mirror. `.github/memories/repo/mysql-hosts-and-mock-capture.md` carries the detail.

## Known Build Quirks
- `PRI175` / `PRI224 root node not found` during `dotnet build` is usually stale PRI artifacts or a running `MTM_Waitlist.exe` locking the output — not a code error. Stop the running app, delete stale `*.pri` under `obj/`/`bin/`, and rebuild before debugging the code.
- `WMC9999: Could not find any resources appropriate for the specified culture ... ErrorMessages.resources` during `dotnet build` is a **MASKED XAML error, not an environment problem**. This machine's WindowsAppSDK 2.4.0 `XamlCompiler` (in `tools\net472` of the `microsoft.windowsappsdk.winui` package) is missing its `ErrorMessages.resources` satellite, so the compiler cannot report the underlying cause — any real XAML compile error (bad type, bad binding, wrong member name) surfaces only as this generic WMC9999. Treat WMC9999 as "there is a real XAML error somewhere; the tool can't tell you where." To surface the real error: temporarily introduce a deliberate C# error (e.g. duplicate a command) so the compiler reports the actual file/type problem, or bisect by simplifying the recently-changed XAML files.
- CommunityToolkit.Mvvm `[RelayCommand]` STRIPS a trailing `Async` from the method name when generating the command: `private async Task ContinueToReviewAsync()` must be bound in XAML as `Command="{x:Bind ViewModel.ContinueToReviewCommand}"` (NOT `...AsyncCommand`). Binding the wrong name is a silent XAML failure that shows up only as the masked `WMC9999` above.
- **Do not add explicit `<Page>` items for Views that live in the app project.** The SDK's default items already include `Module_*/Views/**/*.xaml`, so naming them again raises `NETSDK1022` (duplicate `Page` items). When a View must be excluded, use a `Remove` item (as the `mtm_waitlist.Mock.Service` exclusion does), never an `Include`.
- `CS0579 Duplicate 'System.Reflection.AssemblyTitleAttribute'` pointing at a *nested* `obj\` folder (for example a leftover `Temp*.Preview\obj\`) happens because `MTM_Waitlist.csproj` compiles `**\*.cs` from the project directory and MSBuild only excludes the top-level `bin\` and `obj\`. Find the leftover with `git status --ignored` and remove the stale temp project before rebuilding.
- When an edit spans layers, change the view model, its XAML and its DI registration in the same pass, and never orphan a class: a half-applied multi-file change is the most common source of a silent `WMC9999`.

## Destructive-Tooling Traps (read before running anything under `Database/`)

- **There is no install script and no schema validator any more.** `Database/install_local_database.bat`, its `.vbs`
  GUI twin, and `.github/scripts/validate-database-schema.ps1` were deleted on 2026-11-16. A store is created or
  brought up to date with **one** tool: `Database/DeployDatabase.cmd` (or `dotnet run --project
  MTM_Database_Deployment`). It shows every difference before it writes, and it copies the chosen database into
  `{database}_backup` before its first statement. Full guide:
  `docs/guides/guides-database-deployment-tool.md`. Do not reintroduce a check-then-repair script: that shape is
  what destroyed the live `mtm_waitlist` on 2026-10-16 (record:
  `docs/defects/defects-database-critical-schema-validator-dropped-the-live-waitlist-store.md`).
- **Every script names `mtm_waitlist`, and the tool alone re-points it.** Each artifact under `Database/**` opens
  with `USE mtm_waitlist;` so it can be pasted into a client, and that line sits underneath a header comment. The
  deployment tool rewrites it to the database chosen on the first page; nothing else does. A hand-run script, or a
  new tool that forgets this, writes to the live store whatever connection string it was given.
- **The plant host has `log_bin = OFF`.** There are no MySQL binary logs off `172.16.1.104`, so there is no
  point-in-time recovery: the Mock service's daily dump under
  `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\backups\<database>\` is the only other copy, and it is a day old. Treat
  every write to a store on that host as irreversible for the last day, and treat `{database}_backup` as the only
  undo a deployment has.
- **Several repo scripts write when run.** `refresh_schema_drift.ps1 -Apply` and `verify_mtm_mock_deploy.ps1`
  (drops `mtm_mock`) rewrite a store. Read the switch before running either; the read-only forms are the drift
  report with no `-Apply`, `build_mtm_mock_masters.ps1 -Check`, and
  `Database/Mock/Validation/read_shape_metadata/validate.sql`.

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
