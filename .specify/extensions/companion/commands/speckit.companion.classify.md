---
description: "Classify the change size (small | normal | oversized) so the Companion workflow can right-size the pipeline"
---

<!-- speckit-companion:part communication -->
## How to talk to the person — plain English, end-user facing

Constitution Principle VII governs this reply. It applies to the **chat channel only** — files and
artifacts are unaffected.

- **Plain English, end-user facing.** Say what changed and what it means, in ordinary words. No jargon
  walls, no acronyms that were never defined, no narration of your own reasoning, and never recap the
  request back to the person.
- **Never paste raw material.** No tool output, terminal transcripts, JSON, stack traces, file dumps,
  diffs, or whole artifact bodies. If one line matters, quote that one line.
- **One short line per step.** Report progress as single sentences. Never paste the task list, never
  announce a command before running it, never repeat a step already reported.
- **Fences must render.** A fence starts at **column 0**: a fence indented by four or more spaces, or
  placed inside a list item, becomes a literal code block and garbles everything after it (GFM,
  "Indented Code Block (4 Spaces)"). If the content contains a fence, wrap it in a **longer** backtick run
  than the inner one. Never print a sample block that was only a template for a file you are writing.
- **No machine directives in chat** (`EXECUTE_COMMAND:` and the like) unless the host needs them to run.
- **The last section is what the person must do**, headed exactly `## What you need to do`, as a short
  numbered list in the order they should do it. Nothing to do → end with the single line
  `Nothing needed from you.` Never bury an action in the middle of the reply.
<!-- /speckit-companion:part communication -->

<!-- speckit-companion:part interpreter -->
## Resolve the script interpreter once, then reuse it

Every command in this pipeline is written `python3 …`, which is right on Linux and macOS and **wrong on
Windows**: `python3` there is a Microsoft Store app-execution alias, not an interpreter. It answers
"Python was not found; run without arguments to install from the Microsoft Store", so the call never runs
and the bookkeeping is silently lost. Resolve the interpreter **once**:

1. `python3 --version` — prints a version? Use `python3`.
2. Otherwise `python --version` — prints a version? Use `python`.
3. Otherwise `py --version` — prints a version? Use `py`.
4. If none answers: say so in one plain line, stop trying, continue without the bookkeeping, and **never
   hand-edit `.spec-context.json` and never tick a `tasks.md` box yourself** — report the loss so it stays
   visible instead.

Retry a failing interpreter **at most once**, and never paste its failure text into the chat.
<!-- /speckit-companion:part interpreter -->

# Classify Change Size

Emit a single complexity signal — `small`, `normal`, or `oversized` — that the Companion
workflow's routing step reads to right-size the pipeline. On the workflow path there is no
`complexityFastPath` on/off setting: the thresholds live here, in the workflow, not in a
VS Code toggle.

This is a **thin, read-only** step. It does not write `.spec-context.json` and does not create or
edit any spec files; it only reports a size.

## Heuristic (thresholds live here, not in a setting)

Estimate the scope of the change from `spec.md` (and `plan.md`/`tasks.md` if they already exist):

<!-- speckit-companion:part sizing -->
- **small** — the change plausibly touches **≤ 5 files** and decomposes into **≤ 10 tasks**.
- **oversized** — the change clearly exceeds the small bar by a wide margin (broad multi-subsystem
  work, many new files, or a long task list).
- **normal** — anything in between (the default).

The two constants (5 files / 10 tasks) are the same guardrail the old `complexityFastPath` used.
<!-- /speckit-companion:part sizing -->
When unsure, prefer `normal` — the routing step's safe default is the full pipeline, so an
ambiguous estimate never skips a phase.

## Output

Print exactly one line so the size is visible in the run log:

```text
[companion] size=<small|normal|oversized>
```

Expose the same value as structured output `size` (so a `switch` node can read
`steps.classify.output.size`). Routing contract:

<!-- speckit-companion:part routing -->
- `small` → the workflow folds toward implement (less ceremony).
- `oversized` → the workflow prints a visible warning and still runs the **full** pipeline — it
  never silently skips a phase.
- `normal` (and any unresolved value) → the full pipeline.
<!-- /speckit-companion:part routing -->
