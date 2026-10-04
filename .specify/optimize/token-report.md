# Token Usage Report

**Generated**: 2026-09-27 by `/speckit.token-budget` (optimize.tokens)
**Target context window**: 200,000 tokens
**Estimator**: chars ÷ 4.0. No `optimize-config.yml` exists, so the extension defaults apply
(`chars_per_token: 4.0`, `target_context_window: 200000`, `file_growth_percent: 20`,
`governance_budget_percent: 15`, `max_constitution_tokens: 3000`). At 3.5 chars/token every
figure below rises roughly 14%.

**Run**: first. No previous report existed, so there is no trend to compare against.

---

## Governance files

| File | Exists | Chars | Est. tokens | Load timing | Notes |
|---|---|---|---|---|---|
| `.github/copilot-instructions.md` | Yes | 24,069 | 6,017 | Always | Largest always-loaded file |
| `.specify/memory/constitution.md` | Yes | 13,334 | 3,333 | Always on spec-kit steps | Real content, no redirect |
| `.specify/memory/constitution-history.md` | Yes | 9,228 | 2,307 | On reference | Amendment history; cited by the Sync Impact Report |
| `CLAUDE.md` | No | — | — | — | — |
| `AGENTS.md` | No | — | — | — | — |
| `.ai/rules/*.md` | No | — | — | — | Directory absent |
| `.github/instructions/winui3-ui-automation.instructions.md` | Yes | 22,785 | 5,696 | Always (`applyTo: **/*`) | Largest single governance file |
| `.github/instructions/test-conventions.instructions.md` | Yes | 10,153 | 2,538 | Always | Scoped to `MTM_Waitlist.Tests/**` |
| `.github/instructions/mcp-doc-research.instructions.md` | Yes | 6,296 | 1,574 | Always | — |
| `.github/instructions/spec-kit.instructions.md` | Yes | 4,720 | 1,180 | Always | — |
| `.github/instructions/database-schema-rules.instructions.md` | Yes | 4,191 | 1,047 | Always | Scoped to `Database/**/*.sql` |
| `.github/instructions/csharp-xaml-naming-rules.instructions.md` | Yes | 3,575 | 893 | Always | Scoped to `**/*.{cs,xaml,resw}` |
| `.github/instructions/response-format.instructions.md` | Yes | 2,907 | 726 | Always | — |
| `.github/instructions/winui3-api-rules.instructions.md` | Yes | 1,616 | 404 | Always | Scoped to `**/*.{xaml,cs}` |
| `.github/instructions/packaging.instructions.md` | Yes | 629 | 157 | Always | Scoped to packaging files |
| `.github/memories/repo/*.md` + `README.md` (6 files) | Yes | 30,743 | 7,682 | On reference | `infor-visual-disposition.md` alone is 2,947 |
| `.github/skills/*/SKILL.md` (2 files) | Yes | 7,641 | 1,910 | On invocation | — |

**Repo governance total: 35,464 tokens** — 17.7% of the 200K target, above the configured
`governance_budget_percent: 15`. (Corrected 2026-09-27: the first version of this table omitted
`.specify/memory/constitution-history.md` — 2,307 tokens — because the measurement script excluded any
file with `constitution` in its name. The per-session figures below are unaffected: that file loads on
reference, not every session.)

Loaded every session but living outside the repository, so easy to overlook:

| File | Chars | Est. tokens | Load timing |
|---|---|---|---|
| 6 × user `*.instructions.md` (`%APPDATA%\Code\User\prompts`) | 27,470 | 6,866 | Always |
| `~/.claude/rules/context7.md` | 1,824 | 456 | Always |
| Memory store (`...\copilot-chat\memory-tool\memories`, 6 files) | 9,364 | 2,341 | Auto-loaded |

---

## Extension commands, ranked by token cost

Per invocation, not per session. Measured from the 77 `.github/agents/*.agent.md` bodies, which
carry the real content; the 77 `.github/prompts/*.prompt.md` wrappers total 3,136 tokens and are
negligible.

| Extension | Commands | Total tokens | Largest command | Its tokens |
|---|---|---|---|---|
| companion | 21 | 51,614 | `companion.specify` | 8,982 |
| core (constitution/specify/plan/tasks/implement/checklist/analyze/clarify/converge) | 13 | 39,277 | `checklist` | 5,420 |
| optimize | 3 | 10,359 | `optimize.run` | 5,353 |
| loop | 5 | 6,364 | `loop.define` | 2,032 |
| token-budget | 4 | 5,885 | `token-budget.concise` | 1,838 |
| ci-guard | 5 | 5,757 | `ci-guard.drift` | 1,348 |
| changelog | 4 | 4,143 | `changelog.notify` | 1,163 |
| orchestrator | 4 | 3,404 | `orchestrator.conflicts` | 927 |
| pr-bridge | 3 | 3,193 | `pr-bridge.generate` | 1,304 |
| ralph | 2 | 2,801 | `ralph.run` | 1,515 |
| ship | 1 | 2,702 | `ship.run` | 2,702 |
| conduct | 1 | 2,645 | `conduct.run` | 2,645 |
| git | 5 | 2,525 | `git.feature` | 832 |
| superspec | 5 | 2,478 | `superspec.brainstorm` | 528 |
| fix-findings | 1 | 1,351 | `fix-findings.run` | 1,351 |

**Total: 144,498 tokens across 77 command bodies** (mean 1,877).

---

## Per-session estimates

Baseline means: repo instruction files + `copilot-instructions` + user instruction files + the
context7 rule + auto-loaded memory + the agent and skill name lists.

| Session type | Tokens | % of 8K | % of 32K | % of 128K | % of 200K | % of 1M |
|---|---|---|---|---|---|---|
| Baseline (governance only) | 33,142 | 414% | 104% | 26% | 17% | 3% |
| + constitution | 36,475 | 456% | 114% | 28% | 18% | 4% |
| + largest command (`companion.specify`, 8,982) | 42,124 | 527% | 132% | 33% | 21% | 4% |
| + `companion.implement` and this feature's reading set | 106,713 | 1,334% | 334% | 83% | **53%** | 11% |

The last row is the one that matters: implementing `010-startup-rebuild` costs **53% of a 200K
window before a single line of source is read**. The reading set is 66,003 tokens —
`tasks.md` 22,708, `plan.md` 9,021, `research.md` 8,350, `spec.md` 6,673, `data-model.md` 6,224,
the five contracts 12,512, the checklist 515.

---

## Feature artifacts (`010-startup-rebuild`)

28 files, **146,320 tokens**. Two very different halves:

- **Reading set the steps actually load: 66,003 tokens** (listed above).
- **Evidence and analysis the steps never read: ~80,300 tokens** — the seven `analysis-*.md`
  files (42,681), `MTM_Waitlist_Exception_Logging_Reference.md` (8,262), `concerns-log.md`
  (6,062), `removal-proof.md` (4,723), `local-state-test-migration.md` (4,644), both `findings*`
  files (5,933), `security-review-logging.md` (2,969), `baseline.md` (1,374), `progress.md` (43).

A `/speckit.token-budget.compact` run against this set on the same day returned 0.0% on every
artifact: there is no template scaffolding, no hedge prose and no blank-run padding in them, so
nothing here is recoverable by compaction. The reduction has to come from *scope*, not from
rewriting.

---

## Optimization suggestions

Ranked by measured saving.

1. **Retarget `winui3-ui-automation.instructions.md`** — 5,696 tokens, attached to every request
   via `applyTo: **/*`. A large part of it is a dated verification log and superseded notes about
   the retired file-based log path. Estimated saving **2,500–4,500 tokens per session**.
2. **Slim the nine always-attached instruction files** — 14,215 tokens firing on every request
   regardless of the task. Estimated saving **3,000–6,000 tokens per session**.
3. **Trim `tasks.md`'s parenthetical rationale** — 22,708 tokens, re-read on *every* implement
   turn. Estimated saving **8,000–12,000 tokens per turn**.
4. **Audit `.github/memories/repo/`** — 7,682 tokens across six files; `copilot-instructions.md`
   names only three. Up to **2,947 tokens per read** for `infor-visual-disposition.md`.
5. **Constitution exceeds its own threshold** — 3,333 tokens against `max_constitution_tokens: 3000`.
6. **Do not hand-edit the companion command bodies.** `companion.*` is 51,614 tokens across 21
   commands. The bulk is the shared `step-start`, `timing`, `unattended` and `orchestrator` parts
   repeated into each body; they are generated by the extension from `presets/_parts/`, so a local
   trim would be overwritten on update. Raise it upstream instead.

---

## Applied in this run

Applied (2026-09-27):

- **Suggestion 1, content half — DONE.** The dated verification log was moved verbatim out of
  `.github/instructions/winui3-ui-automation.instructions.md` (3,282 chars) into
  `records/ui-automation-verification.md`, and the section was replaced with a pointer to it.
  Verified: the block is byte-identical in the record and no longer present in the instruction file,
  and every heading survives. The file fell from **5,696 to 4,989 tokens: 706 saved on every
  session**. No rule was changed.
- **Suggestion 1, `applyTo` half — DONE, narrower than proposed.** `applyTo` went from `**/*` to
  `**/*.{xaml,cs,ps1,resw}`, which is every case the file actually serves (UI code, the PowerShell
  automation tooling, and the `.resw` files the automation names come from) rather than only
  `{xaml,cs}`. The saving is conditional: the file no longer attaches to markdown, SQL or config work.
- **Suggestion 4 — DONE as an audit; no edit was warranted.** Every repository memory file is
  pointed at from `.github/memories/README.md`'s index, so nothing is orphaned and nothing was
  deleted. Note for future runs: `github-artifact-staleness.md` records that two files must **not**
  be deleted, so this set is not a deletion candidate.

Declined, with the reason:

- **Suggestion 2.** Six of the nine instruction files are already scoped
  (`Database/**/*.sql`, `**/*.{cs,xaml,resw}`, `MTM_Waitlist.Tests/**`, packaging paths,
  `**/*.{xaml,cs}`, and now the UI-automation file). The three that still carry `applyTo: **/*` are
  `mcp-doc-research` (constitution Principle IV, NON-NEGOTIABLE), `response-format` (Principle VII)
  and `spec-kit` (governs every `/speckit.*` step). Narrowing any of them stops it applying to the
  work it exists to govern, so the "saving" would be paid for with lost enforcement.
- **Suggestion 3.** `tasks.md` is the executable checklist: each line's citations are the
  traceability it exists to carry, its header promises that mirroring, and it is additionally parsed
  by the companion task-sync hooks. A content trim there is a specification decision, not a token
  optimisation.
- **Suggestion 5.** `.github/instructions/spec-kit.instructions.md` requires the constitution to be
  amended only through `/speckit.constitution`, which propagates to dependent artifacts and emits a
  Sync Impact Report. Hand-editing it to save ~330 tokens is not available.
- **Suggestion 6.** Nothing local to apply; the shared companion parts are generated upstream.

**Net effect of this run: ~706 tokens saved on every session, plus the UI-automation guidance no
longer attaching to work it does not describe.**
