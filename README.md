# MTM Waitlist

A WinUI 3 plant-floor application for the manufacturing floor: operators, setup leads and handlers raise
waitlist requests for a part at a work centre, and the floor works them through accept, give back, cancel,
complete, detail and print. It runs unpackaged on .NET 10 and talks to MySQL for its own stores and to
Infor Visual for ERP data.

## Requirements

| | |
|---|---|
| OS | Windows 10 1809 (build 17763) or later |
| SDK | .NET 10 SDK |
| Runtime (build machine) | none beyond the SDK - the app and the Windows App SDK are both published self-contained |
| Database (running the app) | MySQL 5.7 reachable at the configured host, plus Infor Visual SQL Server |

## Build and test

```powershell
dotnet build MTM_Waitlist.sln
dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj
```

Run the app straight from the build output - no packaging step is involved (`WindowsPackageType` is `None`):

```powershell
dotnet run --project MTM_Waitlist.csproj
```

The test project references the module class libraries **directly, never the WinExe app**, so a test run does
not recompile the app's XAML. That is deliberate: pulling the app project into the test build graph causes a
locked `input.json` / dual-build race.

## Solution layout

`MTM_Waitlist.csproj` is the **composition root**: it owns every view, control, XAML file, `App.xaml`, the DI
container, page/navigation registration and the resource dictionaries, and it references every module library.
Testable non-view code lives in the libraries below, which both the app and the tests reference.

| Project | Responsibility |
|---|---|
| `MTM_Waitlist.Core` | Infrastructure and shared contracts - `NavigationService`, `PageService`, the service locator, the MySQL helper seam. References no feature module. |
| `MTM_Waitlist.Shared` | Cross-module services and helpers - storage paths, the picture layout contract, tooltip and control-inspector services. Depends on `Core`. |
| `MTM_Waitlist.Logging` | The diagnostics seam every module writes through: `ILogService`, the `ILogger` provider, the exception-chain serializer and the runtime/view/store context gatherer. Depends on `Core` alone. |
| `MTM_Waitlist.Startup` | The launch: the step sequence as data, the runner that holds each step to its stated maximum, the append-only activity feed, this machine's configuration, identity and sign-in. The host calls it through one method and two events. |
| `MTM_Waitlist.Mock` | The in-app Infor Visual read fallback - the shape catalog, the live-read executor and one `IVisualReadFallback<TRequest,TRow>` per read shape. |
| `MTM_Waitlist.Mock.Service` | The on-host tray-only service that keeps the `mtm_mock` mirror warm, exposes a token-gated HTTP API and backs up the four MySQL stores. Runs on the MySQL/Infor Visual host, not on a workstation. |
| `MTM_Waitlist.Setup` | Work-order setup: search a work order, pick a work centre and dunnage, save the setup pair assignments. |
| `MTM_Waitlist.Settings` | Settings and administration: storage paths, request-item configuration, permissions, search, About. |
| `MTM_Waitlist.Waitlist.View` | The waitlist list, its line cards, the status flyout and the detail surface. |
| `MTM_Waitlist.Waitlist.NewRequest` | Raising a request: part, work centre, quantity, hopper artwork, review. |
| `MTM_Waitlist.Waitlist.Controls` | Controls shared by the waitlist surfaces. |
| `MTM_Waitlist.Reporting` | Reporting surfaces. |
| `MTM_Waitlist.Tests` | All tests, organised per module. |

Namespaces stay `MTM_Waitlist.Module_*.*` across the split, so XAML `{x:Bind}` paths and test `using` lines are
unaffected. `ServiceRegistrationExtensions` wires every module's DI (`Add*ModuleServices`) and the `PageService`
page mappings.

Views stay in the app under `Module_*/Views`; page and view-model registration happens in `App.xaml.cs`
(`AddTransient` / `AddSingleton` plus the `PageService` map), and navigation resolves a page by its view model's
full name through `NavigationService`.

## Data access

**Internal stores are always live.** `mtm_waitlist`, `mtm_wip_application_winforms` and
`mtm_receiving_application` are read and written live on every operation. A failure is reported as a per-screen
unavailable state with a manual retry - never as sample rows and never as a persistent banner.

**Only Infor Visual reads use a cache.** The eight read shapes the app depends on resolve through
`MTM_Waitlist.Mock`, which attempts the live Visual query and serves the same result shape from the `mtm_mock`
mirror only when the reachability probe has settled that Infor Visual is unreachable, or when a live attempt
finds it unreachable. A reachable-but-empty result is a real answer and is never replaced. There is no manual
mode, and no user action: the person sees the identical result, with a read-only indicator saying cached data is
in use and how old it is. The application never refreshes the mirror - that is the on-host service's job.

**Every data operation goes through a stored procedure.** Inline SQL fails the build (`InlineSqlAuditTests`), and
every schema artifact ships `create.sql` and `rollback.sql` in the same change. Master files under `Database/`
(`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) are generated by
`Database/CopilotScripts/build_mtm_mock_masters.ps1` - do not edit them by hand.

## Deployment

### Desktop client

The app is unpackaged and self-contained, so deployment is publish-and-copy - no MSIX, no elevation:

```powershell
# without the profile MSBuild reports NETSDK1198 and silently produces a framework-dependent build
dotnet publish MTM_Waitlist.csproj -p:PublishProfile=win-x64-selfcontained

$target = "$env:LOCALAPPDATA\Programs\MTM_Waitlist"
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item .\bin\publish\win-x64\* $target -Recurse -Force
```

Close the app first - the copy fails on the locked `MTM_Waitlist.exe`. A deployed client also needs
`MTM_MOCK_SERVICE_ENDPOINT` and `MTM_MOCK_SERVICE_USER` (or the equivalent `MockServiceClient` section) before it
can ask the service for an early refresh.

### On-host cache service

`MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` publishes, redeploys, installs and proves the
environment secrets, starts the service and health-checks it. It is **server-only**: it refuses to run unless one
of the local machine's IPv4 addresses is the expected cache host, because the service belongs on the host that
reaches MySQL and Infor Visual. See
[operations-mock-service-install-script.md](docs/operations/operations-mock-service-install-script.md).

## Documentation

All project documentation lives under [docs/](docs/), in one folder per category, with every file named
`<category>-<feature>-<name>.<ext>`.

| Folder | What is in it |
|---|---|
| [docs/database/](docs/database/) | The Infor Visual schema export - 5,866 CSVs, one folder per export type (`tables/`, `column-details/`, `indexes/`, `foreign-keys/`, …). This is the CSV MCP server's storage root, configured in `.mcp.json`. |
| [docs/defects/](docs/defects/) | Closed defect records, one per defect, with severity in the file name. |
| [docs/guides/](docs/guides/) | Task guides: [picture file naming](docs/guides/guides-pictures-image-naming.md) and [LABELVIEW label printing integration](docs/guides/guides-labelprinting-labelview-integration.md). |
| [docs/living-specs/](docs/living-specs/) | The living-spec capability for `startup-launch` and the [drift ledger](docs/living-specs/living-specs-drift-ledger.md) that records what the drift detector can and cannot see. |
| [docs/modules/](docs/modules/) | Per-module test notes for the Mock library and the Mock service. |
| [docs/operations/](docs/operations/) | Runbooks: [live database and Mock host updates](docs/operations/operations-database-live-and-mock-updates.md), the [Mock service deploy and run guide](docs/operations/operations-mock-service-deploy-and-run.md), the [installer](docs/operations/operations-mock-service-install-script.md) and the [plant-host next run](docs/operations/operations-waitlist-next-run.md). |
| [docs/reference/](docs/reference/) | The split Infor VISUAL system-wide user guide, one chapter per file, starting at [the index](docs/reference/reference-inforvisual-index.md). |
| [docs/research/](docs/research/) | Research and decision records: [Phase 6 disposition status codes](docs/research/research-inforvisual-phase6-disposition-status-codes.md) and the [waitlist card layout pre-spec](docs/research/research-waitlist-card-layout-pre-spec.md). |
| [docs/startup/](docs/startup/) | The [startup rebuild design brief](docs/startup/startup-rebuild-design-brief.md) that the launch surface is built from. |
| [docs/scripts/](docs/scripts/) | [Script index](docs/scripts/scripts-automation-index.md) and [UI Automation guide](docs/scripts/scripts-automation-ui-automation.md) for everything under [scripts/](scripts/). |

Adjacent, and deliberately not moved into `docs/`:

- [specs/](specs/) - Spec Kit features (`spec.md`, `plan.md`, `tasks.md`, contracts, checklists) and their run
  journals. One folder per feature, numbered in order.
- [Database/](Database/) - schema artifacts and the [database ruleset](Database/Database-Ruleset.md).
- [.github/](.github/) - agent instructions, prompts, skills and repo memories.

## Working in this repository

- **Spec first.** Anything non-trivial runs through Spec Kit in order: `constitution` -> `specify` -> `clarify` ->
  `plan` -> `tasks` -> `analyze` -> `implement`. See `.github/instructions/spec-kit.instructions.md`.
- **Docs serve the build rule, not the reverse.** A gate in `MTM_Waitlist.Tests` fails the build if a retired
  sample/demo symbol returns (`RetiredSymbolAuditTests`) or if inline SQL appears (`InlineSqlAuditTests`).
  `AcceptanceSurfaceTests` reads [the startup brief](docs/startup/startup-rebuild-design-brief.md) from disk, so
  that file's section headings are part of the build.
- **Localization.** UI strings resolve through `"ResourceKey".GetLocalized()`; resources live in
  [Strings/](Strings/).

## Troubleshooting

| Symptom | Cause |
|---|---|
| `PRI175` / `PRI224 root node not found` | Stale PRI artifacts, or a running `MTM_Waitlist.exe` locking the output. Stop the app, delete `*.pri` under `obj/` and `bin/`, rebuild. |
| `WMC9999: Could not find any resources ... ErrorMessages.resources` | A **masked XAML error**, not an environment fault - this machine's `XamlCompiler` is missing its satellite resource, so a real XAML problem has no way to be reported. Bisect the recently changed XAML, or introduce a deliberate C# error so the compiler names the file. |
| A `[RelayCommand]` binding does nothing | CommunityToolkit.Mvvm strips a trailing `Async` from the method name, so `ContinueToReviewAsync()` binds as `ContinueToReviewCommand`, not `ContinueToReviewAsyncCommand`. |
| `NETSDK1198` on publish | The publish profile was omitted - pass `-p:PublishProfile=win-x64-selfcontained`. |

## Changelog

User-facing changes are recorded in [CHANGELOG.md](CHANGELOG.md).