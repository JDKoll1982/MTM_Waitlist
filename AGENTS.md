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
18 `*AuditTests` guardrails that fail the build on architecture regressions — read the failing test
before assuming the failure is environmental.

## Stack

.NET 10, `net10.0-windows10.0.19041.0`, WinUI 3. Every package the solution references:
`Microsoft.WindowsAppSDK` 2.4.0, `CommunityToolkit.Mvvm` 8.4.2, `CommunityToolkit.WinUI.UI.Controls`
and `CommunityToolkit.WinUI.UI.Animations` 7.1.2, `CommunityToolkit.WinUI.Controls.SettingsControls`
8.2.251219, `DevWinUI` 11.1.0, `WinUIEx` 2.9.2, `Microsoft.Xaml.Behaviors.WinUI.Managed` 3.0.1,
`MySqlConnector` 2.6.1, `Microsoft.Data.SqlClient` 6.1.1,
`Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Abstractions`,
`Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`
and `Microsoft.Extensions.Options` at 10.0.10, `System.Security.Cryptography.ProtectedData` 10.0.0 and `System.Security.Cryptography.Pkcs` 9.0.21 (transitive
through `Microsoft.Data.SqlClient`, pinned in `Directory.Build.props`), `Microsoft.Windows.SDK.BuildTools`, `MSTest.TestAdapter` and `MSTest.TestFramework` 3.7.0,
`Microsoft.NET.Test.Sdk` 17.13.0, `coverlet.collector` 6.0.2. 17 projects, one target framework each,
shared pins in `Directory.Build.props`.

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

Eight files exceed 1000 lines (`MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs` is 2623). Slice them with
`git log -L<start>,<end>:<file>` or `git blame -C` instead of reading them whole.

## Conventions

- Commit subjects: mixed style. Roughly a third use `feat:`/`fix:`/`chore:` prefixes, most do not;
  subjects average ~80 characters and generally carry no issue reference. Some commits use an `NNN:`
  spec-task prefix. Match the surrounding history rather than importing Conventional Commits.
- View models derive from `ObservableRecipient`, use `[ObservableProperty]`/`[RelayCommand]`, implement
  `INavigationAware` with data loading in `OnNavigatedTo`, and resolve UI strings through
  `"ResourceKey".GetLocalized()`. The detail lives in `.github/copilot-instructions.md` and
  `.github/instructions/winui3-api-rules.instructions.md`.

## Data rules

- Every data operation goes through a stored procedure, and internal stores are always read and written
  live with no demo/mock mode; `InlineSqlAuditTests` and `RetiredSymbolAuditTests` fail the build
  otherwise. The rules live in `.github/instructions/database-schema-rules.instructions.md` and
  `.github/copilot-instructions.md`.
- `appsettings.json` is tracked and carries connection strings. Never print, copy or commit a secret
  value.

## Build gotchas

- `PRI175` / `PRI224` (stale PRI artifacts or a running app), `WMC9999` (a masked XAML error, never an
  environment fault), `[RelayCommand]` stripping a trailing `Async` from the generated command name, and
  `CS0579` from a leftover nested `obj\` project all have documented fixes. Read "Known Build Quirks"
  in `.github/copilot-instructions.md` before assuming a failure is environmental.

## Instructions in force

Always applied: `.github/copilot-instructions.md`, plus
`.github/instructions/{mcp-doc-research,response-format,spec-kit,document-authoring}.instructions.md`.
Scoped by glob: `test-conventions`, `database-schema-rules`, `startup-rebuild`, `winui3-adoption`,
`winui3-api-rules`, `winui3-scaling-routing`, `winui3-ui-automation`, `csharp-xaml-naming-rules`,
`packaging`, `diagram-authoring`. Files under `.github/instructions/records/` carry no `applyTo`, so
they load only when something points at them.

## Before editing

Check `git status` and the working diff first. Never discard uncommitted work, rewrite history,
reset, clean, or force-push.
