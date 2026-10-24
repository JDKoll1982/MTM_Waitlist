---
name: 'Custom instructions authoring'
description: 'How custom instructions work in VS Code: file types per harness, scopes, user vs workspace locations, applyTo vs description activation, verification, and sync. Use when creating, fixing, or debugging instruction files that are not being applied.'
applyTo: '**/*.instructions.md'
---
# Custom instructions

Verified on VS Code 1.143.1. User-scope path confirmed by `copilot instruction list`, which reports files in `~/.copilot/instructions` as "Personal instructions".

## What they are

Reusable Markdown context applied automatically (or on demand) so guidance does not have to be repeated in every prompt. They do **not** affect inline ghost-text suggestions.

## Instruction types by harness

| Harness | Project-level | Targeted |
| --- | --- | --- |
| Copilot | `.github/copilot-instructions.md` or `AGENTS.md` | `.github/instructions/**/*.instructions.md` |
| Anthropic Claude | `CLAUDE.md` | Markdown in `.claude/rules` |
| OpenAI Codex | `AGENTS.md` | nested `AGENTS.md` files |
| Local | `.github/copilot-instructions.md`, `AGENTS.md`, or `CLAUDE.md` | `.github/instructions/**/*.instructions.md` or `.claude/rules` |

`AGENTS.md` is a cross-agent format, not a Codex-only file. When a team uses several harnesses, prefer one shared format and keep duplicates consistent — there is no general precedence order between instruction files, so conflicting copies are a real failure mode.

## Scopes

| Scope | Use for | Where |
| --- | --- | --- |
| Workspace | Project conventions | Repository |
| User | Personal preferences across projects | Harness user folder |
| Organization | Centrally managed requirements | GitHub org settings |

**User-scope locations (verified):**

| Purpose | Path |
| --- | --- |
| Always-on personal instructions | `~/.copilot/copilot-instructions.md` |
| Targeted personal instructions | `~/.copilot/instructions/**/*.instructions.md` |
| Claude user rules | `~/.claude/rules`, `~/.claude/CLAUDE.md` |

`chat.instructionsFilesLocations` defaults to `{ ".github/instructions": true, ".claude/rules": true, "~/.copilot/instructions": true, "~/.claude/rules": true }`. It still works but is **not recommended** for Copilot workflows: other Copilot clients (CLI, desktop app) do not inherit editor settings. Prefer native harness folders.

## Activation

| Mechanism | Frontmatter | Behavior |
| --- | --- | --- |
| File pattern | `applyTo: '**/*.py'` | Auto-attached when the agent creates or modifies a matching file |
| Task relevance | `description: '...'` | Agent may load it on demand when the description matches the task |
| Manual | neither field | Only attached when you add it to a specific request |

Frontmatter fields: `name` (display name, defaults to filename), `description`, `applyTo` (glob relative to workspace root; `**` matches everything). Claude rules use `paths` (array) instead of `applyTo`.

Guidance:

* Use `applyTo: '**'` only for genuinely global behavior rules; it injects the file into every file-touching request.
* Prefer `description`-driven loading for reference material so it costs no context until relevant.
* Write `description` as "what it covers **and when to use it**" — vague descriptions are why files never load.
* Reference other files with Markdown links using `/` separators so instructions stay portable; relative paths resolve from the instruction file, `~/` resolves from the user home.

## Settings

| Setting | Default | Notes |
| --- | --- | --- |
| `chat.useAgentsMdFile` | `true` | Attach workspace-root `AGENTS.md` (Local harness only) |
| `chat.useNestedAgentsMdFiles` | `false` | Discover nested `AGENTS.md`; requires the above |
| `chat.useClaudeMdFile` | `true` | Use `CLAUDE.md` as always-on instructions |
| `chat.instructionsFilesLocations` | see above | Additional locations; not recommended |
| `chat.useCustomizationsInParentRepositories` | `false` | Monorepo parent-repo discovery |
| `github.copilot.chat.organizationInstructions.enabled` | `true` | Include GitHub org instructions |
| `chat.hookFilesLocations` | `{}` | (Preview) Additional hook file locations |

Deprecated since 1.102: settings-based code-generation and test-generation instructions. Use file-based instructions instead. Still valid as `[{ "text": ... } | { "file": ... }]` arrays: `github.copilot.chat.reviewSelection.instructions`, `github.copilot.chat.commitMessageGeneration.instructions`, `github.copilot.chat.pullRequestDescriptionGeneration.instructions`.

## Verification

Discovery is not compliance. To actually verify a file is used:

1. `Chat: Open Customizations` → confirm the file is listed for the selected harness.
2. Start a fresh chat with the same harness.
3. Ask for a small task with a clear success criterion.
4. Expand **References** in the response and confirm the file was attached.
5. Check the result follows the stated convention.

## Sync

User instructions in harness folders (`~/.copilot/instructions`, `~/.claude/rules`) do **not** roam via Settings Sync. Instructions stored in the VS Code profile (Local harness) do. To sync profile prompts and instructions: Settings Sync: Configure → select "Prompts and Instructions".

## Client behavior differences (verified)

`copilot instruction list --json` on CLI 1.0.98 reports each source's `label`, `location`, `type`, `sourcePath`, and — only when present in the file — `description` and `applyTo`. It states the list is "what THIS process would load", and it did not filter by `applyTo`: every discovered file was reported.

Consequences worth designing around:

* A user-level instruction file is effectively always-on for CLI sessions. Keep these files short and let bulky reference material live in a skill, which loads progressively instead of being injected in full.
* `applyTo` is what makes authoring guidance fire at the right moment in VS Code (for example, editing a `*.agent.md` file attaches the agent-authoring notes).
* When a file's metadata does not appear in the listing, suspect invalid YAML first. An unquoted value containing `: ` (colon-space) is invalid and silently drops the whole frontmatter block.

## Ground rules

* Never edit a workspace instruction file to change personal behavior; use user scope.
* Do not create a `.github/instructions` entry when the guidance is personal.
* Keep instruction files short and imperative. Long prose gets diluted.
* After adding a file, verify with `copilot instruction list` — it prints the exact source paths in use.
