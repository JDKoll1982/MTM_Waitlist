# Reading manifest — phase: plan

Generated: 2026-09-13T12:40:07-05:00
Active feature: 004-unified-card-item-picker
Token budget: 18533/12000 (OVER by 6533 — read in two waves; Wave 1 = 11835, Wave 2 = 6698)

Target step: `/speckit.companion.plan`. Recorded size is `oversized` (`.spec-context.json` → `"size": "oversized"`),
so the size budget applies the **full** shape: `plan.md` + `research.md` + `data-model.md` + `contracts/`. Nothing is
trimmed, and `Project Structure` and the `Constitution Check` are both required.

## Read in full

### Wave 1 — to write `plan.md` and `research.md`

- `specs/004-unified-card-item-picker/spec.md` — User Scenarios & Testing + Edge Cases (L14–169), Requirements
  incl. FR-001…FR-032 and Key Entities (L170–257), Assumptions (L282–315), Verbatim Constraints (L316–353),
  ~5491 tokens
  - Why: the FRs are what the plan must satisfy, the Key Entities pre-shape `data-model.md`, and the Verbatim
    Constraints are the identifier contract `contracts/` must copy exactly.
- `.specify/memory/constitution.md` — Core Principles I–VI (L12–110), Additional Constraints (L111–135),
  Development Workflow & Quality Gates (L136–150), ~2374 tokens
  - Why: the `Constitution Check` gate needs one row per principle (I–VI), and the added constraints carry the
    stored-procedure / no-mock / MCP-first rules the plan must not violate.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — §16 "The revised design (supersedes §14)" (L1149–1371, §16.1–§16.15),
  ~3569 tokens
  - Why: the approved design. Carries what the spec deliberately leaves to `plan.md` / `data-model.md`: the Item
    metadata table shape (§16.3), what a request stores (§16.2), seeds and install (§16.8), retirements (§16.9),
    verification (§16.10), and the closing decisions (§16.13–§16.15).
- `.specify/templates/plan-template.md` — section skeleton only: Summary + Technical Context (L1–13),
  Constitution Check (L30–36), Project Structure (L36–50), Complexity Tracking (L97–104), ~401 tokens
  - Why: the output shape for `plan.md`. Read the headings, not the two HTML comment blocks (see Skip).

### Wave 2 — to write `data-model.md` and `contracts/`

- `WeekendProject/Documents/unified-item-picker-workflows.md` — header (L1–11), §1–§4 per-Item flows (L12–423),
  §5 the common Item-picker shell (L424–454), §6 lifecycle after creation (L455–476), ~3739 tokens
  - Why: `contracts/` must carry each Item's availability rule and the answer it captures, and §5/§6 are the
    shared shape every Item's contract inherits.
- `WeekendProject/Documents/Request-Config-Template.csv` — 23 rows × 20 columns, ~2959 tokens
  - Why: FR-027 makes this the *sole* source of the per-Item field definitions, so `data-model.md`'s
    configuration row and `contracts/`' field lists must be drawn from these columns, not invented. Columns 19–20
    are retired-mock location notes and can be skimmed (~350 tokens).

## Skim only

- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — §13 "Brainstorm log" (L531–809), ~5248 tokens
  - Look for: the **"Settled scope for the next spec (after Q1–Q9)"** subsection (L741–789) only.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — §15 "Deep dive — the CSV and the seeded database" (L927–1148),
  ~4159 tokens
  - Look for: **§15.3 "the three findings that change the wiring"** (L980–999), the Q13 answer (L1012), and the
    Q17–Q18 sample-removal answers (L1118–1148). The rest is restated by §16.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — §5 "Workstream 2" (L167–244), ~1334 tokens
  - Look for: **§5.1 "Authoritative input — carry verbatim as requirements"** (L183–192) only; §5.2–§5.7 are the
    pre-§16 phasing and are superseded by §16's design.
- `specs/004-unified-card-item-picker/checklists/requirements.md` — whole file, 1018 tokens
  - Look for: the "Deliberately absent, and why" note — it records that the **'Other' card's full-width grid span
    is left to `plan.md`** — and the FR-029…FR-032 append note.
- `specs/003-waitlist-handler-fulfilment/spec.md` — the card-anatomy constraint (L315–330, the "DO NOT REGRESS"
  block), ~489 tokens
  - Look for: the exact constraint and its matching test that this feature **supersedes in writing** (recorded
    supersession, rewritten rather than deleted).
- `specs/004-unified-card-item-picker/spec.md` — Success Criteria → Measurable Outcomes (L258–281), 422 tokens
  - Look for: nothing the plan consumes; verification outcomes belong to `tasks`/`implement`.
- `specs/004-unified-card-item-picker/spec.md` — header / Input block (L1–13), 181 tokens
  - Look for: the two documents of record, already named above.
- `.specify/memory/constitution.md` — Governance (L151–164), 241 tokens; SYNC IMPACT REPORT comment (L1–9),
  ~104 tokens
  - Look for: nothing actionable — the last-amended version only.
- `WeekendProject/Documents/unified-item-picker-workflows.md` — §7 "Unified card representation" + Deliberate
  exclusions (L497–510), 146 tokens
  - Look for: nothing beyond spec FR-005…FR-009.
- `WeekendProject/Documents/Request-Config-Template.csv` — columns 19–20 (`Primary code file(s)`,
  `Mock / current location (file~region)`), ~350 tokens
  - Look for: pointers into the code being retired; not design input.

## Skip

- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — §14 "Design, first pass" (L810–927), ~1820 tokens — the heading itself
  reads **"SUPERSEDED by §16"**; every decision is restated in §16.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` — title block + §1–§4 and §6–§12 (L1–166, L245–530), ~8809 tokens —
  other workstreams (analytics, admin, user management, startup polish, docs hygiene) and the completed Workstream 1.
- `.specify/templates/plan-template.md` — HTML comment blocks (L14–29 Technical Context guidance, L51–96
  source-code option tree), ~512 tokens — droppable template instructions/examples per `drop_sections`.
- `.specify/templates/spec-template.md` (1173), `tasks-template.md`, `checklist-template.md`,
  `constitution-template.md`, `agent-file-template.md` — not inputs to `plan`; `tasks-template.md` belongs to the
  `tasks` phase.
- `specs/001-module-mock-visual-fallback/spec.md`, `specs/002-truthful-data-and-controls/spec.md` — other, delivered
  work; the one superseded constraint lives in `specs/003` and is listed as a skim above.
- `capabilities/startup/spec.md` (~3128) and `capabilities/startup-diagnostics/spec.md` (~2283) — `.spec-context.json`
  records `"last_action": "living specs evaluated — no capabilities matched"`, so there is no `livingSpecs.loaded`
  record to reuse and nothing to pull. Re-resolve only if the plan's changed-file set actually maps to these areas.
- Any `<spec>.arch.md` — no capability was loaded, so no architecture tier exists to pull.
- `living-specs.yml` (163) — resolver configuration, needed only if re-resolving changed files.
- `.specify/extensions/companion/commands/speckit.companion.plan.md` (5793) — the command prompt itself; the host
  loads it, so counting it here would double-count.

## Notes

- **Over budget as a single pass.** The complete `oversized` input set is ~18533 tokens against a
  `scope.max_total_tokens` of 12000, an overage of 6533. This is structural, not padding: an oversized feature
  whose design lives in two documents of record outside the feature directory cannot fit one 12k window.
- **Recommended handling — read in two waves, both under cap:**
  - Wave 1 (~11835, 165 free): `spec.md` (minus Success Criteria) + `constitution.md` (minus Governance) +
    seed §16 + the `plan-template.md` skeleton → writes `plan.md` and `research.md`.
  - Wave 2 (~6698, 5302 free): the two documents of record → writes `data-model.md` and `contracts/`.
  - If 165 free in Wave 1 is too thin, move `plan-template.md` (~401) to skim; the plan's section names are listed
    in the command itself.
- **Compaction would not close the gap.** `token-budget-config.yml` preserves `Functional Requirements`,
  `User Stories`, `Constraints` and `Success Criteria`, so compacting `spec.md` yields little, and the two
  documents of record sit outside the feature directory so `/speckit.token-budget.compact` does not target them.
  Prefer the wave split over aggressive compaction here.
- **No compacted siblings exist.** `spec.md` and `checklists/requirements.md` have no `.full.md` backups, so no
  savings are available from step 2 of the scope algorithm.
- **Estimate method.** `estimate_tokens.ps1` could not import `tiktoken` (`python3` is not on `PATH` on this
  workstation), so every figure above is the script's `chars/4` fallback. Treat them as ±10–15% and re-measure
  with the same script if any region looks surprisingly large at read time.
- **Manifest location.** Written to the token-budget extension's own convention,
  `specs/<feature>/.token-budget/scope-<phase>.md` (per `.specify/extensions/token-budget/commands/scope.md`), not
  the `token-budget-scope.md` fallback in the feature root. The feature `.gitignore` only ignores `.trace.jsonl`,
  so this folder is not ignored.
- **Nothing was modified** except this file: `spec.md`, the constitution, the seed, the templates and all source
  files are untouched.
