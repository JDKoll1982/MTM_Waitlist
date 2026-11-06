# AGENTS.md

Orientation for AI agents working in this repository. Keep this file short; every line is paid on
every request.

## Commands (verified against the solution, not prose)

```powershell
dotnet build MTM_Waitlist.sln
dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj
dotnet run --project MTM_Waitlist.csproj
```

There is no CI workflow and no `.editorconfig`. Warnings are not errors. The test project carries
15 `*AuditTests` guardrails that fail the build on architecture regressions — read the failing test
before assuming the failure is environmental.

## Stack

.NET 10, `net10.0-windows10.0.19041.0`, WinUI 3 on Windows App SDK 2.4.0, CommunityToolkit.Mvvm
8.4.2, CommunityToolkit.WinUI.UI.Controls 7.1.2, DevWinUI 10.8.0, WinUIEx 2.9.2. Fifteen projects,
one target framework each, shared pins in `Directory.Build.props`.

## Layout

- `MTM_Waitlist.csproj` — app and composition root. XAML views live in `Module_*/Views/` and `.cs`
  beside them; SDK default globs include them, so **never add explicit `<Page>` items** (that is
  `NETSDK1022`).
- `MTM_Waitlist.Core` — `PageService`, `NavigationService`; `MTM_Waitlist.Shared`, `.Logging`,
  `.Settings`, `.Startup`, `.Setup`, `.Mock`, `.Mock.Service`, `.GenericRequest[.Data]`,
  `.Waitlist.Controls|View|NewRequest`.
- `Services/DependencyInjection/ServiceRegistrationExtensions.cs` — register pages, view models and
  services here, not ad hoc.
- `Database/` — stored procedures, tables, seeds, Infor Visual queue SQL. `docs/database/` — Infor
  Visual schema CSV exports: tab-delimited, nested across 11 type folders, **no files at the root**,
  so path a query to the subfolder.
- `specs/`, `.specify/` — Spec Kit feature specifications.

## Where changes actually land

Six-month churn concentrates in `Strings/en-us/Resources.resw`,
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`,
`Database/StoredProcedures/AllSPs.sql`, `MTM_Waitlist.csproj`, and
`Module_Settings/Views/SettingsPage.xaml`. Start orientation there rather than reading the tree.

Six files exceed 1000 lines (`SettingsHub/ViewModels/SettingsViewModel.cs` is 2623). Slice them with
`git log -L<start>,<end>:<file>` or `git blame -C` instead of reading them whole.

## Conventions

- Commit subjects: mixed style. Roughly a third use `feat:`/`fix:`/`chore:` prefixes, most do not;
  subjects average ~80 characters and generally carry no issue reference. Some commits use an `NNN:`
  spec-task prefix. Match the surrounding history rather than importing Conventional Commits.
- View models derive from `ObservableRecipient`, use `[ObservableProperty]`/`[RelayCommand]`, and
  implement `INavigationAware` with data loading in `OnNavigatedTo`.
- UI strings resolve through `"ResourceKey".GetLocalized()`.

## Data rules

- Every data operation goes through a stored procedure; `InlineSqlAuditTests` fails the build on
  inline statement text. Schema artifacts ship `create.sql` with `rollback.sql`.
- Internal stores are always read and written live. There is no demo/mock mode, and
  `RetiredSymbolAuditTests` fails the build if one is reintroduced.
- `appsettings.json` is tracked and carries connection strings. Never print, copy or commit a secret
  value.

## Build gotchas

- `PRI175` / `PRI224 root node not found` — stale PRI artifacts or a running `MTM_Waitlist.exe`
  holding the output. Stop the app, clear `*.pri` under `obj/` and `bin/`, rebuild.
- `WMC9999: Could not find any resources appropriate for the specified culture` — a **masked XAML
  error**, not an environment fault. Bisect the recently changed XAML files.
- `[RelayCommand]` strips a trailing `Async`: `ContinueToReviewAsync()` binds as
  `ContinueToReviewCommand`.
- `CS0579 Duplicate 'System.Reflection.AssemblyTitleAttribute'` pointing at a *nested* `obj\` folder
  (for example a leftover `Temp*.Preview\obj\`) — `MTM_Waitlist.csproj` compiles `**\*.cs` from the
  project directory, and MSBuild only excludes the top-level `bin\` and `obj\`. Find leftovers with
  `git status --ignored` and remove the stale temp project before rebuilding.

## Instructions in force

Always applied: `.github/copilot-instructions.md`, plus
`.github/instructions/{mcp-doc-research,response-format,spec-kit}.instructions.md`. Scoped by glob:
`test-conventions`, `database-schema-rules`, `startup-rebuild`, `winui3-adoption`,
`winui3-api-rules`, `winui3-ui-automation`, `csharp-xaml-naming-rules`, `packaging`.

## Before editing

Check `git status` and the working diff first. Never discard uncommitted work, rewrite history,
reset, clean, or force-push.
