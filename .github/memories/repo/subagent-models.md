# Subagent model policy (owner instruction, 2026-10-10)

**Every background / subagent task in this repository runs on DeepSeek V4 Flash.**

Pass `model: "deepseek/deepseek-v4-flash"` on the `task` call.

## Why this file exists

The owner noticed that background agents were being launched on GitHub-hosted models (Claude, GPT, Gemini and
similar) and corrected it: only DeepSeek V4 Flash is to be used for subagent work here. This note is the durable,
pushable record so the correction survives the session it was made in.

## Naming

The owner named it **"DeepSeek V4.1 Flash"**. The client's model list has no `v4.1` entry; the identifier that
matches is `deepseek/deepseek-v4-flash`. Use that identifier. If a future client adds a distinct `v4.1`, prefer
that and update this note.

## Scope and precedence

- Applies to **every** agent type that takes a model override — `Explore`, `task`, `general-purpose`,
  `code-review`, `research`, and every custom agent registered for this repo.
- It **overrides** the general default of leaving `model` unset so the runtime can resolve `/subagents`
  preferences, because the owner has explicitly required a specific model for this repository.
- `reasoning_effort` and `context_tier` remain **unset** unless the request in hand names them.

## Prohibited

- Do **not** pass a GitHub-hosted model on a `task` call, and do not treat a GitHub-hosted model as an acceptable
  fallback.
- Do **not** silently downgrade providers if a DeepSeek call fails or DeepSeek V4 Flash is unavailable. Report it
  and stop, rather than substituting a different provider without the owner's say-so.

## Where it is also recorded

`.github/copilot-instructions.md` carries the shorter operative rule under **"Subagent Model"**; this file carries
the rationale and the naming caveat. Keep the two consistent if either changes.
