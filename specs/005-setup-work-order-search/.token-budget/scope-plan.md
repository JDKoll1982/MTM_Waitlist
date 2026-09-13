# Reading manifest — phase: plan

Generated: 2026-09-13T13:20:00-05:00
Active feature: 005-setup-work-order-search
Token budget: ~14200/12000 (OVER by ~2200 — read in two waves; Wave 1 = ~7900, Wave 2 = ~6300)

Target step: `/speckit.companion.plan`. Recorded size is `normal` (`.spec-context.json` → `"size": "normal"`),
so the size budget applies the **full** shape: `plan.md` + `research.md` + `data-model.md` + `contracts/`.
Nothing is trimmed, and `Project Structure` and the `Constitution Check` are both required.

## Read in full

### Wave 1 — to write `plan.md` and `research.md`

- `specs/005-setup-work-order-search/spec.md` — the whole file: User Scenarios & Testing, Edge Cases,
  FR-001…FR-022, Key Entities, Success Criteria, Assumptions, Verbatim Constraints, ~2950 tokens
  - Why: the FRs are what the plan must satisfy; the Edge Cases carry the padding trap; Key Entities
    pre-shape `data-model.md`; the Verbatim Constraints are the identifier contract `contracts/` copies
    exactly.
- `specs/005-setup-work-order-search/checklists/requirements.md` — the whole file, ~1050 tokens
  - Why: carries the reasoning behind the three recorded decisions and the "deliberately absent, and why"
    list (the design decisions this plan is expected to make).
- `.specify/memory/constitution.md` — Core Principles I–VI, Additional Constraints, Development Workflow
  & Quality Gates, ~2400 tokens
  - Why: the `Constitution Check` gate needs one row per principle (I–VI), and the added constraints carry
    the stored-procedure / no-mock / MCP-first rules the design must not violate.
- `MTM_Waitlist.Setup/Services/WorkOrderValidationService.cs` — the whole file, ~600 tokens
  - Why: the normalisation rule (`^(?:WO-)?(\d{5,6})$`, padded to six) is the load-bearing distinction the
    plan builds on: partials search **unpadded**, complete forms pad as today (FR-004/FR-006).
- `MTM_Waitlist.Setup/ViewModels/SetupWorkOrderViewModel.cs` + `Module_Setup/Views/SetupWorkOrderPage.xaml.cs`
  — the search path and the `LostFocus` → `AutoSearchOnWorkOrderBlurAsync` entry point, ~1600 tokens
  - Why: the modal is raised on leaving the field, reusing this moment; and FR-012 forbids the entry step
    from holding its own matching logic, so the plan must name what stays here.
- `MTM_Waitlist.Mock/Models/VisualReadShape.cs` — the whole file, ~1000 tokens
  - Why: **the design precedent**. A registered source described once as data, with its artifact names
    derived from a stable key. The mechanism that satisfies FR-013/FR-014/FR-018/FR-019 is modelled on it.
- `MTM_Waitlist.Mock/Services/VisualReadFallback.cs` + `Contracts/IVisualReadFallback.cs`, ~1500 tokens
  - Why: FR-019's boundary verbatim — live first, mirror only on unreachability, a reachable-but-empty
    result is a real answer. The search read must ride this and not invent a parallel path.
- `MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs` — shape 1 entry + header, ~700 tokens
  - Why: the registration shape a new `work_order_search` source copies.

### Wave 2 — to write `data-model.md` and `contracts/`

- `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_get/create.sql` +
  `Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql` +
  `Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql` (header + predicate),
  ~2600 tokens
  - Why: the live/mirror projection identity is the contract `contracts/` must pin for the new search shape,
    including the addressable-key predicate the population already uses.
- `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_refresh/create.sql` — header, contract, steps,
  ~1100 tokens
  - Why: the atomic-swap refresh pattern a new shape's refresh procedure copies (MySQL 5.7, no window
    functions — which constrains how a match count is computed).
- `MTM_Waitlist.Setup/Services/SetupLookupService.cs` + `Setup/Services/DependencyInjection/…cs`, ~1500 tokens
  - Why: how a shape's fallback is injected and consumed today; the picker's reader registration follows it.
- `MTM_Waitlist.Setup/Contracts/Services/ISetupDialogService.cs` + `Services/SetupDialogService.cs`, ~900 tokens
  - Why: the injectable modal seam the picker dialog copies, so the unit gates run headless.
- `Database/Mock/README.md` + `Database/Mock/Validation/all_shapes_roundtrip/validate.sql`, ~700 tokens
  - Why: what "keep the master lists in sync" means here — the `mtm_mock` masters are **generated** by
    `Database/CopilotScripts/build_mtm_mock_masters.ps1` (approved deviation), and a new shape extends the
    roundtrip validation.

## Skim only

- `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs` — the marker set and the raw-SQL allowlist, ~800 tokens
  - Look for: which markers fail the build (so the design carries no statement text and no run-time-assembled
    procedure name), and that only `.cs` outside the test project is scanned.
- `MTM_Waitlist.Setup/Services/SetupWorkflowService.cs` — `FindBestScrapTypeMatch` / `NormalizeForFuzzy` /
  `ComputeLevenshteinDistance` (L490–630) only, ~900 tokens
  - Look for: the existing Levenshtein best-match helper the spec names as a *candidate* for reuse. The plan
    must decide and say why, so the question is answered from the code, not from memory.
- `.specify/templates/plan-template.md` — section skeleton, ~400 tokens
  - Look for: the section order only. The template's Technical Context block and its two HTML comment blocks
    are droppable, and the source-code option tree is the placeholder this plan replaces with real paths.
- `Database/Database-Ruleset.md` — "Naming Standards (Locked)", "Allowed and Banned Terms", "Artifact Layout
  and Release Governance" incl. the `mtm_mock` master-list review note, ~1400 tokens
  - Look for: table/procedure naming, the banned-term list (`order` allowed only in the `work_order`
    compound), and the paired `create.sql`/`rollback.sql` rule.

## Skip

- `specs/005-setup-work-order-search/.spec-context.json` — read for the size only (`normal`, `unattended`);
  the `coverage` map restates the FRs already read in `spec.md`.
- `specs/001-module-mock-visual-fallback/*` (~40k tokens) — the fallback's own spec, plan, research and
  tasks. The parts that bind this plan (the shape model, the fallback algorithm, the refresh contract) are
  read directly from the source files above; the delivered feature's history is not design input.
- `specs/002…`, `specs/003…`, `specs/004…` — other delivered work.
- `capabilities/startup/spec.md`, `capabilities/startup-diagnostics/spec.md` — `.spec-context.json` records
  `"last_action": "living specs evaluated — no capabilities matched"`, so there is no `livingSpecs.loaded`
  record to reuse and no `.arch.md` tier to pull. Re-resolve only if the changed-file set maps to these areas.
- `living-specs.yml`, `living-specs` resolver — needed only for a fresh resolution, which the record above
  already ruled out.
- `WeekendProject/**` — the module workstream docs; the retired-systems boundary they describe is restated as
  FR-018/FR-019 in the spec.
- `Module_Setup/Views/SetupWorkOrderPage.xaml` in full — the modal is a separate dialog view; only the
  existing `TextBox` with its `LostFocus` handler is relevant and is read with the code-behind above.
- `.specify/extensions/companion/commands/speckit.companion.plan.md` — the command prompt itself; the host
  loads it, so counting it here would double-count.

## Notes

- **Over budget as a single pass, comfortably read in two.** Waves 1 and 2 each fit under the 12000 cap; the
  overage is only the sum, and the two halves are genuinely independent (prose plan + research, then the
  design documents).
- **The estimate method is the `chars/4` fallback** — `estimate_tokens.ps1` cannot import `tiktoken` on this
  workstation. Treat the figures as ±15%.
- **Manifest location.** Written to the token-budget extension's own convention,
  `specs/<feature>/.token-budget/scope-<phase>.md` (per `.specify/extensions/token-budget/commands/scope.md`),
  not the `token-budget-scope.md` fallback in the feature root. The feature `.gitignore` only ignores
  `.trace.jsonl`, so this folder is not ignored.
- **Nothing was modified** except this file: `spec.md`, the checklist, the constitution, the templates and all
  source files are untouched.
