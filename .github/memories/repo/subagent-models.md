# Subagent model policy (owner instruction, 2026-10-11)

**Every background / subagent task in this repository runs on DeepSeek V4.1 Flash.**

Pass `model: "deepseek/deepseek-flash"` on the `task` call.

## Why this file exists

The owner noticed that background agents were being launched on GitHub-hosted models (Claude, GPT, Gemini and
similar) and corrected it: only DeepSeek V4.1 Flash is to be used for subagent work here. This note is the durable,
pushable record so the correction survives the session it was made in.

## Naming — and the mistake this file used to make

The owner named the model **"DeepSeek V4.1 Flash"**. An earlier version of this note claimed the client had no
`v4.1` entry and that `deepseek/deepseek-v4-flash` was the identifier to use, with a promise to revisit "if a
future client adds a distinct `v4.1`". **That was wrong**, and the owner caught it on 2026-10-11.

The provider extension `vizards.deepseek-v4-for-copilot` ships **four** models, and two of them are easily
confused. Verified 2026-10-11 by reading `out/consts.js` in
`%USERPROFILE%\.vscode\extensions\vizards.deepseek-v4-for-copilot-0.9.4`:

| Model id | Display name | Version |
| --- | --- | --- |
| `deepseek-flash` | **DeepSeek V4.1 Flash** | `v4.1` ← **the one to use** |
| `deepseek-v4-flash` | DeepSeek V4 Flash | `v4` |
| `deepseek-v4-flash-vision-exp` | DeepSeek V4 Flash (vision, experimental) | `v4` |
| `deepseek-v4-pro` | DeepSeek V4 Pro | `v4` |

The entry carrying `name: 'DeepSeek V4.1 Flash'` is `id: 'deepseek-flash'`, so the identifier to pass is
`deepseek/deepseek-flash`. **`deepseek/deepseek-v4-flash` is a different, older model (`v4`) and is not what the
owner asked for.** The two names differ by three characters and both are plausible, which is exactly why the
table above is here rather than a one-line rule.

## Scope and precedence

- Applies to **every** agent type that takes a model override — `Explore`, `task`, `general-purpose`,
  `code-review`, `research`, and every custom agent registered for this repo.
- It **overrides** the general default of leaving `model` unset so the runtime can resolve `/subagents`
  preferences, because the owner has explicitly required a specific model for this repository.
- `reasoning_effort` and `context_tier` remain **unset** unless the request in hand names them.

## Prohibited

- Do **not** pass a GitHub-hosted model on a `task` call, and do not treat a GitHub-hosted model as an acceptable
  fallback.
- Do **not** pass `deepseek/deepseek-v4-flash` (v4) in place of `deepseek/deepseek-flash` (v4.1).
- Do **not** silently downgrade providers if a DeepSeek call fails or DeepSeek V4.1 Flash is unavailable. Report it
  and stop, rather than substituting a different provider without the owner's say-so.

## Where it is also recorded

`.github/copilot-instructions.md` carries the shorter operative rule under **"Subagent Model"**, including the
same model table; this file carries the rationale and the naming history. Keep the two consistent if either
changes.
