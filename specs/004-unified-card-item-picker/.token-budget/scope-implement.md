# Reading manifest — phase: implement

Generated: 2026-09-13T17:58:00-05:00
Active feature: 004-unified-card-item-picker
Token budget: 36261/12000 (OVER by 24261 — read in five waves; W1 10556, W2 4758, W3 6205, W4 3867, W5 4304)

Target step: `/speckit.companion.implement`. Recorded size is `oversized`
(`.spec-context.json` → `"size": "oversized"`), so the size budget applies the **full** shape:
`tasks.md` + `plan.md` + `data-model.md` + `contracts/`, exactly as
`token-budget-config.yml` → `phase_inputs.implement` lists them. Nothing is trimmed.

`tasks.md` is the executable checklist and the primary input: it names an exact file path per task, and every
verification gate is quoted from `contracts/verification-gates.md` into it. The other three artifacts are read to
supply what a task line deliberately does not repeat — column types, JSON shapes, token sets and the gate wording.

## Read in full

### Wave 1 — the plan of record

- `specs/004-unified-card-item-picker/tasks.md` — all 117 tasks, Phases 1–8 and 51 waves, ~19056 tokens
  - Why: it is the work. Each task line carries its own file path; the wave/`⟶ Wait` structure is the dependency
    map, and the closing sections (Dependencies & Execution Order, Wave summary, Parallel opportunities,
    Independent test points) are what the implement run is sequenced against.

### Wave 2 — the design that constrains the file inventory

- `specs/004-unified-card-item-picker/plan.md` — Summary, Technical Context, Constitution Check, Project
  Structure, Post-Design Re-Check, ~4758 tokens
  - Why: `Project Structure` is the per-project file inventory every task path must agree with; the Constitution
    Check rows are the constraints (stored-procedure-first, no inline SQL, no pixel boundaries) that T113–T115
    re-verify at the gate.

### Wave 3 — the entities and the two schemas the tasks implement

- `specs/004-unified-card-item-picker/data-model.md` — §1–§8, ~4304 tokens
  - Why: §2 is the twenty-three-row catalog T018 rewrites, §3 is the exact column set/type/nullability T004's DDL
    and T019's model must match, §4 is the `detail_fields_json` element shape T020/T085 parse, §7 is the card row
    model T062 reshapes.
- `specs/004-unified-card-item-picker/contracts/item-configuration.md` — §1–§5, ~1720 tokens
  - Why: §2–§4 are the three new procedures' exact signatures and result shapes (T005, T006, T014, T015), which
    T115 validates the live database against.

### Wave 4 — the picker contract the rules tasks implement

- `specs/004-unified-card-item-picker/contracts/request-picker-flow.md` — §1–§6, ~2185 tokens
  - Why: §4/§6 carry the per-Item flow, answer requirement, prompt, limits, options and allotted minutes that
    T007's seed and T018's `CardLine2Template` values must reproduce; §3 is the ordering T030/T031 implement.

### Wave 5 — the card, sort and gate contracts

- `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` — §1–§5, ~1761 tokens
  - Why: §1's anatomy is the shape T064 reduces the card to and T060 rewrites its markup test against; the token
    set and the die's `Home Location` conditional are what T026/T061 implement.
- `specs/004-unified-card-item-picker/contracts/verification-gates.md` — §1–§6, ~2477 tokens
  - Why: the gate wording every Phase 8 tick is checked against, and §4's "against the mock mirror" is the
    wording error T116 corrects in its own note.

## Skim only

- `specs/004-unified-card-item-picker/spec.md` — ~6120 tokens
  - Look for: **Functional Requirements** (FR-001…FR-032) and **Success Criteria** (SC-001…SC-012) — the
    requirements the implement run validates against and the criteria T117 walks;
    **Verbatim Constraints** for the two pinned card lines. The User Scenarios prose is already restated as each
    phase's Goal/Independent Test in `tasks.md`.
- `specs/004-unified-card-item-picker/research.md` — ~8818 tokens
  - Look for: **D1–D21** only, and only where a task line cites a §D reference (T023/D12, T026–T030/D11·D20·D21,
    T045/D16, T060/D17, T063·T096/D10, T075–T078/D7·D8, T085/D13, T101–T109/D15). The rejected-alternative prose
    is a decision record, not an input.
- `.github/instructions/database-schema-rules.instructions.md` — ~900 tokens
  - Look for: the file-per-artifact layout, the naming rules (lowercase snake_case; bare `order` is banned), and
    the `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` / `Bootstrap/update_table_descriptions.sql`
    same-change obligation T016 and T104 satisfy.
- `.github/instructions/test-conventions.instructions.md` — ~1200 tokens
  - Look for: the `Module_<Area>` placement rule for **new** test files (which is what puts
    `WaitlistSortPreferenceServiceTests` under `Module_Core/` and `RequestItemObservedTimeServiceTests` under
    `Module_Settings/Services/`), and `Never weaken a gate to go green`.

## Skip

- `.specify/memory/constitution.md` (~2475) — already distilled into `plan.md`'s Constitution Check rows and into
  the Hard Constraints the host command restates; re-reading it adds no task.
- `.specify/templates/*` — every artifact the implement phase touches already exists; no template is resolved.
- `specs/003-waitlist-handler-fulfilment/spec.md` — T111 names the constraint, the line and the matching test;
  the rewrite is written against `contracts/card-and-identifier.md` §1, not against the superseded text.
- `specs/001-*`, `specs/002-*` — delivered, unrelated work.
- `WeekendProject/Documents/Request-Config-Template.csv` and
  `WeekendProject/Documents/unified-item-picker-workflows.md` — FR-027 design documents of record. They were
  consumed once, at `tasks` time, into `contracts/request-picker-flow.md` §4/§6 and `data-model.md` §3/§4. Reading
  them during `implement` is the first step toward the build dependence FR-027 forbids; T002 exists to fail the
  build if any application file ever references them.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — the seed, already distilled into `spec.md` / `plan.md` /
  `research.md`; the largest artifact in the repository and the least incremental.
- `capabilities/` and `living-specs.yml` — `.spec-context.json` records
  `"last_action": "living specs evaluated — no capabilities matched"`, so there is no `livingSpecs.loaded` set to
  account for at completion.
- `.specify/extensions/companion/commands/speckit.companion.implement.md` (~6000) — the host loads it; counting it
  here would double-count.
- `.specify/companion.yml` — verified empty; there are no `before`/`after` node hooks to run.

## Notes

- **Over budget as a single pass, by design.** The complete input set is ~36261 tokens against
  `scope.max_total_tokens` of 12000, an overage of 24261. `tasks.md` alone (19056) exceeds the cap, and it cannot
  be dropped: it is the checklist the phase executes. This is structural for an `oversized` feature whose work is
  spread across four design documents.
- **`tasks.md` is read once, in full, and then held.** Every later wave is re-read from the file only at a phase
  boundary, because the run executes task-by-task rather than document-by-document.
- **Recommended handling — read in five waves, every one under cap:**
  - Wave 1 (~19056, over cap on its own — read in three passes: Phases 1–2, Phases 3–5, Phases 6–8 plus the
    closing sections) → the checklist itself.
  - Wave 2 (~4758, 7242 free): `plan.md` → the file inventory and the constitution obligations.
  - Wave 3 (~6024, 5976 free): `data-model.md` + `contracts/item-configuration.md` → the schema and model tasks.
  - Wave 4 (~2185, 9815 free): `contracts/request-picker-flow.md` → the picker/seed tasks.
  - Wave 5 (~4238, 7762 free): `contracts/card-and-identifier.md` + `contracts/verification-gates.md` → the card,
    sort and gate tasks.
- **Compaction would not close the gap.** `token-budget-config.yml` preserves `Functional Requirements`,
  `User Stories`, `Constraints`, `Success Criteria`, `Data Model`, `Contracts` and `Tasks` — which is the whole of
  `spec.md`, `data-model.md` and `contracts/`, plus the whole of `tasks.md`. There is no safe subset to strip.
- **No compacted siblings exist.** Neither `spec.md` nor `checklists/requirements.md` has a `.full.md` backup, so
  step 2 of the scope algorithm yields nothing.
- **Estimate method.** `estimate_tokens.ps1` cannot import `tiktoken` (`python3` is not on `PATH` on this
  workstation — it is a Microsoft Store stub that exits 9009), so every figure above is the script's `chars/4`
  fallback, measured with `Get-Item`/`Length` per file. Treat them as ±10–15%.
- **Manifest location.** Written to the token-budget extension's own convention,
  `specs/<feature>/.token-budget/scope-<phase>.md`, beside `scope-plan.md` and `scope-tasks.md`. The feature
  `.gitignore` ignores only `.trace.jsonl`, so this folder is not ignored.
- **Nothing was modified** by writing this manifest: `spec.md`, `plan.md`, `research.md`, `data-model.md`,
  `contracts/`, the constitution and all source files are untouched. The only write is this file.

## Corrections carried into this phase

Three facts found by reading the code during `tasks`, which drive task lines the implement run must honour:

1. **`MTM_Waitlist.Settings` does not reference `MTM_Waitlist.Setup`.** The canonical scrap rule must be a
   `static` predicate extracted into `MTM_Waitlist.Core` (T024) — the only project both reference — reached
   through the availability snapshot, never by calling `SetupDunnageTypeViewModel.HasScrapDecision`, which is an
   instance property that returns `true` for `No Scrap`, the opposite of FR-031.
2. **`RequestJobPartAvailability` carries no scrap value.** T023 extends the record; without it the snapshot the
   picker filters on cannot express FR-031 at all.
3. **`contracts/verification-gates.md` §4 says "against the mock mirror" for the UI walk.** That is a wording
   error: `mtm_mock` serves external Infor Visual reads only, and the internal stores are read and written live
   (constitution II). T116 records the correction in place.
