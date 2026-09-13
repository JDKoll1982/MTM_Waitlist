# Reading manifest — phase: tasks

Generated: 2026-09-13T13:05:00-05:00
Active feature: 004-unified-card-item-picker
Token budget: 31524/12000 (OVER by 19524 — read in four waves; W1 = 10645, W2 = 8531, W3 = 8133, W4 = 4215)

Target step: `/speckit.companion.tasks`. Recorded size is `oversized` (`.spec-context.json` → `"size": "oversized"`),
so the size budget applies the **full** shape: phases, one phase per user story, explicit waves with join points,
per-story Goal / Independent Test / Checkpoint blocks, and a closing Dependencies & Execution Order section.
Nothing is trimmed.

`token-budget-config.yml` → `scope.phase_inputs.tasks` = `["plan.md", "data-model.md", "contracts/", "research.md"]`.
`spec.md` is added to this manifest beyond that list: the task list is organised **by user story** and each task is
tagged `[US#]`, so the four stories' boundaries and the full FR-001…FR-032 set are load-bearing inputs, not context.

## Read in full

### Wave 1 — to fix the phase shape and the story boundaries

- `specs/004-unified-card-item-picker/spec.md` — whole file (User Scenarios & Testing, the four stories with their
  priorities and acceptance scenarios, Edge Cases, FR-001…FR-032, Key Entities, SC-001…SC-012, Assumptions,
  Verbatim Constraints), ~6092 tokens
  - Why: the phases are the stories, in priority order; the `[US#]` tags and the `[P]` decisions both come from
    here. The Verbatim Constraints pin the four Categories, the twenty-three Item codes, the two wrong-material
    first lines, the die options, the two suppressing scrap values and the five sort options — every one of which
    a task line must reproduce exactly.
- `specs/004-unified-card-item-picker/plan.md` — whole file (Summary, Technical Context, Constitution Check,
  Project Structure with the Documentation and Source Code trees, Structure Decision), ~4553 tokens
  - Why: the `Project Structure` tree is the task list's file inventory. Every task names a concrete file, and
    this is where the file set — new, re-keyed and retired — is enumerated. The Structure Decision also fixes what
    does **not** move between projects, which is what keeps the wave layout honest.

### Wave 2 — to place the three corrections and the retirements

- `specs/004-unified-card-item-picker/research.md` — whole file (Sources consulted, D1–D21), ~8531 tokens
  - Why: D11/D12/D20/D21 are tasks in their own right (availability before the list, the scrap gate through the
    canonical predicate, the five-Item job-independent set plus Category filtering, the merged pickup Item's
    coil-or-flatstock correction and the dead `pickup-flatstock` arm). D15/D17 fix the retirement order and the
    recorded supersession. D19 is a build guard. D2/D3/D4/D8/D9/D10/D13 each name a file the tasks must touch.

### Wave 3 — to write the schema, model and picker tasks

- `specs/004-unified-card-item-picker/data-model.md` — whole file (§1–§10 and State transitions), ~4261 tokens
  - Why: the exact column names and types of `waitlist_request_item_configs`, the two request columns, the card's
    element-to-source table, the Line 2 token set and the availability-snapshot order are all copied into tasks.
- `specs/004-unified-card-item-picker/contracts/item-configuration.md` — whole file, ~1716 tokens
  - Why: the stored-artifact table (five artifacts, each with its paired `create.sql`/`rollback.sql`) and the four
    consumer contracts are the schema tasks' acceptance conditions.
- `specs/004-unified-card-item-picker/contracts/request-picker-flow.md` — whole file, ~2156 tokens
  - Why: the per-Item contract table (availability, captures, Line 2) is the source for the picker-rule tasks and
    for the visibility-matrix tests; §3 fixes where the availability pass runs; §5 is the scrap matrix verbatim.

### Wave 4 — to finish the card and sort tasks and the verification gates

- `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` — whole file, ~1748 tokens
  - Why: the one-layout anatomy, the two wrong-material first lines, the picture resolution order and the five sort
    options with the overdue invariant — the US2 and US3 tasks' acceptance conditions.
- `specs/004-unified-card-item-picker/contracts/verification-gates.md` — whole file, ~2467 tokens
  - Why: the four gates (build, tests, live DB, scripted UI), the per-area test table this feature must add, the
    `FR-020` two-gate clarification, the requirement-to-gate map that the Polish phase validates against, and §6's
    documentation-in-the-same-change list.

## Skim only

- `specs/004-unified-card-item-picker/checklists/requirements.md` — whole file, ~1021 tokens
  - Look for: the "Deliberately absent, and why" note (the 'Other' span is a plan value) and the 2026-09-13 refresh
    note naming FR-002, FR-029, FR-030, FR-031 and FR-032 as the changed requirements.
- `.specify/extensions/token-budget/token-budget-config.yml` — the `scope` and `compact` blocks only, ~600 tokens
  - Look for: `max_total_tokens: 12000` and `phase_inputs.tasks`; the `concise`/`slim` blocks are not inputs.
- `.github/instructions/database-schema-rules.instructions.md` — the naming, artifact-layout and
  master-list-maintenance rules, ~900 tokens
  - Look for: the file-per-artifact layout and the `Bootstrap/update_table_descriptions.sql` obligation, which the
    schema tasks must state; `order` is a banned bare identifier (so `detail_fields_json`'s `order` key stays
    inside JSON, never a column).
- `.github/instructions/test-conventions.instructions.md` — the `Test code style` and `Running the suite` sections,
  ~1200 tokens
  - Look for: the `Module_<Area>` rule for **new** test files (which corrects two paths the plan spelled under the
    older `Services/` folder) and the `Never weaken a gate to go green` rule the verification tasks cite.
- `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` and
  `MTM_Waitlist.Setup/ViewModels/SetupDunnageTypeViewModel.cs` — the two rules D12/D21 correct, ~1100 tokens
  - Look for: the exact shape of `RequiredJobPart`'s switch (including the dead `pickup-flatstock` arm) and whether
    `HasScrapDecision` is static or instance — it is an **instance** property reading `SelectedScrapType`.

## Skip

- `.specify/memory/constitution.md` (~2475) — already read in the `plan` phase and already reflected as the
  Constitution Check rows in `plan.md`; the tasks restate the principles as gates, not as new decisions.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §13–§16 (~15000) — the seed. Its every decision this feature consumes is
  already distilled into `spec.md`, `plan.md`, `research.md` (D1–D21) and `contracts/`; re-reading it would
  duplicate the largest single artifact in the repository for no new task.
- `WeekendProject/Documents/Request-Config-Template.csv` (~2959) and
  `WeekendProject/Documents/unified-item-picker-workflows.md` (~3739) — the two documents of record. FR-027 makes
  them **design-time** inputs, consumed once when the configuration seed is authored; the task that authors that
  seed names them, and the tasks themselves carry the per-Item facts from `contracts/request-picker-flow.md` §4
  instead. Reading them here would also be the first step toward the build dependence FR-027 forbids.
- `specs/003-waitlist-handler-fulfilment/**` (~large) — the superseded card anatomy. The Polish task names the
  constraint file, the line and the matching test; the rewrite is written against the new anatomy in
  `contracts/card-and-identifier.md` §1, not against the old text.
- `specs/001-module-mock-visual-fallback/**`, `specs/002-truthful-data-and-controls/**` — delivered, unrelated work.
- `.specify/templates/tasks-template.md` — the phase/wave skeleton and the line format are dictated by the
  `speckit.companion.tasks` command body itself, which already holds the template.
- `.specify/extensions/companion/commands/speckit.companion.tasks.md` (~6000) — the command prompt; the host loads
  it, so counting it here would double-count.
- `capabilities/startup/spec.md`, `capabilities/startup-diagnostics/spec.md` (~5400 combined) —
  `.spec-context.json` records `"last_action": "living specs evaluated — no capabilities matched"`, so there is no
  `livingSpecs.loaded` record to reuse. Re-resolve only if a task's changed-file set actually maps to those areas.
- Any `<spec>.arch.md` — no capability was loaded, so no architecture tier exists to pull.
- `living-specs.yml` (163) — resolver configuration, needed only when re-resolving changed files.

## Notes

- **Over budget as a single pass, by design.** The complete input set is ~31524 tokens against
  `scope.max_total_tokens` of 12000, an overage of 19524. The four named artifacts of a `tasks` phase alone
  (`research.md` 8531 + contracts/ 8087 + plan.md 4553 + data-model.md 4261) total 25432, so even dropping
  `spec.md` entirely would not fit one window. This is structural: the design of this feature is carried by four
  documents, not one.
- **Recommended handling — read in four waves, every one under cap:**
  - Wave 1 (~10645, 1355 free): `spec.md` + `plan.md` → the phase shape and the file inventory.
  - Wave 2 (~8531, 3469 free): `research.md` → the corrections and retirements as first-class tasks.
  - Wave 3 (~8133, 3867 free): `data-model.md` + `contracts/item-configuration.md` +
    `contracts/request-picker-flow.md` → the schema, model and picker tasks.
  - Wave 4 (~4215, 7785 free): `contracts/card-and-identifier.md` + `contracts/verification-gates.md` → the card,
    sort and gate tasks.
  - The two contract files were split 2 + 2 rather than read as one 8087 block purely to keep Wave 3 clear of the
    cap once `data-model.md` joins it.
- **Compaction would not close the gap.** `token-budget-config.yml` preserves `Functional Requirements`,
  `User Stories`, `Constraints`, `Success Criteria`, `Data Model`, `Contracts` and `Tasks` — which is the whole of
  `spec.md`, the whole of `data-model.md` and the whole of `contracts/`. `/speckit.token-budget.compact` therefore
  cannot help this phase; it would strip `research.md`'s rationale, which is the source of D12/D20/D21 as tasks.
- **No compacted siblings exist.** Neither `spec.md` nor `checklists/requirements.md` has a `.full.md` backup, so
  step 2 of the scope algorithm yields nothing.
- **Estimate method.** `estimate_tokens.ps1` could not import `tiktoken` (`python3` is not on `PATH` on this
  workstation — it is a Microsoft Store stub that exits 9009), so every figure above is the script's `chars/4`
  fallback, measured with `Get-Content -Raw` per file. Treat them as ±10–15%.
- **Manifest location.** Written to the token-budget extension's own convention,
  `specs/<feature>/.token-budget/scope-<phase>.md`, beside `scope-plan.md`. The feature `.gitignore` ignores only
  `.trace.jsonl`, so this folder is not ignored.
- **Nothing was modified** by writing this manifest: `spec.md`, `plan.md`, `research.md`, `data-model.md`,
  `contracts/`, the constitution and all source files are untouched.

## Correction carried from reading (drives three task lines)

Two facts found by reading the code, which the plan's prose does not survive as written and which the task list
therefore encodes explicitly:

1. **`MTM_Waitlist.Settings` does not reference `MTM_Waitlist.Setup`.** Settings references Core, Shared and Mock
   only; Setup references Core, Shared and Mock only. The canonical `HasScrapDecision` is an **instance property**
   on `SetupDunnageTypeViewModel`, so `RequestItemPickerRules` cannot call it — and it returns `true` for
   `No Scrap`, which is the opposite of FR-031. The rule must be an extracted static predicate in
   `MTM_Waitlist.Core` (the one project both reference), reached through the availability snapshot.
2. **`RequestJobPartAvailability` carries no scrap value.** Its six positional flags have no scrap member, so the
   snapshot the picker filters on cannot express FR-031 at all until the record is extended. `plan.md`'s
   "five flags" table in `contracts/request-picker-flow.md` §3 omits it too.
