*Recommended Markdown Viewer: [Markdown Editor](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor2)*

## Getting Started

Browse and address `TODO:` comments in `View -> Task List` to learn the codebase and understand next steps for turning the generated code into production code.

Explore the [WinUI Gallery](https://www.microsoft.com/store/productId/9P3JFPWWDZRC) to learn about available controls and design patterns.

Relaunch Template Studio to modify the project by right-clicking on the project in `View -> Solution Explorer` then selecting `Add -> New Item (Template Studio)`.

## Project Structure (per-module class libraries)

This repo uses a modular, per-module class-library layout. The WinExe **app** (`MTM_Waitlist.csproj`) is the **composition root** — it owns all `Views`, `Controls`, XAML, `App.xaml`, DI wiring, page/navigation registration, and resources — and references every module library. Testable non-view code lives in class libraries that both the app and the test project reference:

- **`MTM_Waitlist.Core`** — core infrastructure, shared contracts/interfaces, and app-agnostic services (e.g. `NavigationService`, `PageService`, `AppServiceLocator`, `IMySqlHelperServer`). Referenced by all modules; references no feature module.
- **`MTM_Waitlist.Shared`** — shared services/models used across modules (e.g. `TooltipService`, `ControlInspectorService`). Depends on `Core`.
- **`MTM_Waitlist.Mock`** — the in-app Infor Visual read fallback: the shape catalog, the live-read executor, and one `IVisualReadFallback<TRequest,TRow>` per read shape. A Visual read that finds Infor Visual unreachable is served from the `mtm_mock` mirror with an identical result shape; a reachable-but-empty result is never replaced. No demo/mock mode exists (`RetiredSymbolAuditTests` enforces this). Depends on `Core`.
- **`MTM_Waitlist.Mock.Service`** — the on-host tray-only service that keeps `mtm_mock` warm (3-hour default cadence on the local-midnight grid), exposes a token-gated HTTP API, and backs up the four MySQL stores. Deployed unpackaged and self-contained; see `MTM_Waitlist.Mock.Service/README.md`.
- **Feature modules** (self-contained; reference only `Core` + `Shared`): `MTM_Waitlist.Startup`, `MTM_Waitlist.Setup`, `MTM_Waitlist.Settings`, `MTM_Waitlist.Reporting`, and the split `MTM_Waitlist.Waitlist.*` (`View`, `NewRequest`, `Controls`).
- **`MTM_Waitlist.Tests`** references the module libraries directly — **not** the WinExe app — so `dotnet test` does not recompile the app's XAML (this avoids the locked `input.json` / dual-build race that occurs when tests pull the app project into their build graph).

Namespaces remain `MTM_Waitlist.Module_*.*` across the split, so XAML `{x:Bind}` bindings and test `using` lines are unchanged. The app (`ServiceRegistrationExtensions`) wires all module DI (`Add*ModuleServices`) and configures `PageService` page mappings.

## Infor Visual cache (`mtm_mock`)

External Infor Visual reads are the only reads that use a cache. The five read shapes the application depends on are
resolved through `MTM_Waitlist.Mock`, which attempts the live Visual query and — **only when Infor Visual is
unreachable** — serves the same result shape from the `mtm_mock` mirror. There is no manual mode and no user action;
the user sees the identical result, and a read-only status indicator reports that cached data is in use and how old
it is.

| Piece | Where |
|---|---|
| Mirror schema + procedures | `Database/Mock/` (`Tables/`, `StoredProcedures/`, `Seeds/`, `Bootstrap/`) |
| In-app fallback | `MTM_Waitlist.Mock/` (`IVisualReadFallback<TRequest,TRow>`, one per shape) |
| On-host refresh + API + backups | `MTM_Waitlist.Mock.Service/` (see its `README.md`) |
| Feature specification | `specs/001-module-mock-visual-fallback/` |

Rules that must not be broken:

- The internal stores (`mtm_waitlist`, `mtm_wip_application_winforms`, `mtm_receiving_application`) are **always**
  read and written live — the cache is never consulted for them, and a failure is reported as a per-screen
  unavailable state with a manual retry (never sample rows, never a persistent banner).
- The cache is only consulted on **unreachability**. A reachable-but-empty live result is a real answer.
- The application never refreshes the cache; that is the on-host service's job, on its own schedule.
- Every data operation goes through a stored procedure — `InlineSqlAuditTests` fails the build on inline SQL, and
  `RetiredSymbolAuditTests` fails it if any retired sample/demo symbol returns.

Master lists under `Database/Mock/` (`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) are **generated** by
`Database/CopilotScripts/build_mtm_mock_masters.ps1`; do not edit them by hand. The deviation from the locked ruleset
is recorded in `Database/Database-Ruleset.md`.

## External Integrations

### Tables Ready

- API key: `5a657e2e-c68e-484c-9249-9db7049a5ada`

## Publishing

For projects with MSIX packaging, right-click on the application project and select `Package and Publish -> Create App Packages...` to create an MSIX package.

For projects without MSIX packaging, follow the [deployment guide](https://docs.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps) or add the `Self-Contained` Feature to enable xcopy deployment.

## CI Pipelines

See [README.md](https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/pipelines/README.md) for guidance on building and testing projects in CI pipelines.

## WinUI 3 Dynamic Scaling & Accessibility Standouts

- Hardcoded pixel boundaries for layout-critical Width or Height values are forbidden for page, card, list, and form containers. Use fluid sizing (Auto and star columns/rows) with bounded constraints such as MinWidth, MinHeight, MaxWidth, and MaxHeight.
- Interactive overlay elements (such as floating action buttons) must define AdaptiveTrigger-backed visual states and must pair each overlay footprint with a matching bottom list/content padding offset so scrollable content always clears the overlay.
- Text-heavy layouts must be resilient to 150%+ text scaling by using ThemeResource-backed text styles, explicit MaxWidth constraints for identity and header text, and TextTrimming set to CharacterEllipsis when no-wrap behavior is required.
- Overflow-prone detail regions should use scrolling containers or wrapping text patterns so users on low-resolution displays can still reach all interactive and informational elements.

## Changelog

See [releases](https://github.com/microsoft/TemplateStudio/releases) and [milestones](https://github.com/microsoft/TemplateStudio/milestones).

## Feedback

Bugs and feature requests should be filed at https://aka.ms/templatestudio.
