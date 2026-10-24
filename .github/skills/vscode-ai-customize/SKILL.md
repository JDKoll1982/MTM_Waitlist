---
name: vscode-ai-customize
description: Decide which VS Code AI customization type to use and scaffold it with correct frontmatter. Use when someone asks where to put a rule, wants a reusable prompt, needs a custom agent or subagent, wants a skill packaged, or is unsure whether guidance belongs in instructions, a prompt file, an agent, or a skill.
argument-hint: '[what you want to build]'
---

# Build the right VS Code AI customization

Pick the smallest mechanism that solves the problem. Putting a repeatable workflow into always-on instructions wastes context on every request.

## Decision table

| Need | Use | Why not the others |
| --- | --- | --- |
| A convention that must always hold (style, architecture, safety) | Instructions | Prompts are manual; skills may never load |
| Rules that only matter for certain file types | Instructions with `applyTo` | An always-on file dilutes attention |
| A task you trigger by name, repeatedly | Prompt file | Instructions cannot be invoked on demand |
| A task that must also work in Copilot CLI and the desktop app | Skill | Profile prompt files do not travel |
| A role with restricted tools or a different model | Custom agent | Instructions cannot restrict tools |
| Work that should not pollute the main context | Subagent (`user-invocable: false`) | A normal agent fills the main thread |
| A capability with scripts and reference files | Skill | Prompt files cannot carry assets well |
| Deterministic automation on agent events | Hook | Model-driven guidance is not deterministic |

Ask in order: *Is this a rule or a workflow?* Rules go to instructions. *Is it repeatable and named?* Prompt file or skill. *Does it need its own tools or model?* Agent.

## Scopes

| Scope | Instructions | Prompt | Agent | Skill |
| --- | --- | --- | --- | --- |
| Workspace | `.github/instructions`, `.github/copilot-instructions.md`, `AGENTS.md` | `.github/prompts` | `.github/agents` | `.github/skills` |
| Personal | `~/.copilot/instructions`, `~/.copilot/copilot-instructions.md` | VS Code profile `prompts` folder | `~/.copilot/agents` | `~/.copilot/skills`, `~/.agents/skills` |

Personal rules go in user scope. Never edit a repository file to change only your own behavior.

## Scaffold

Run the helper to generate a correctly formed file:

```bash
python scripts/scaffold.py --kind instruction --name "Python testing" --apply-to "**/*.py"
python scripts/scaffold.py --kind prompt --name "review-pr" --hint "[pr number]"
python scripts/scaffold.py --kind agent --name "planner" --tools read,search
python scripts/scaffold.py --kind skill --name "test-runner" --scope user
```

It prints the file content and the exact destination path. Review before saving; never overwrite an existing file blindly.

## Frontmatter that actually works

* Quote every scalar that contains `:` followed by a space. An unquoted `Description: does X: Y` makes the whole frontmatter block invalid and the file is loaded with no metadata — the single most common silent failure.
* `applyTo` is a glob relative to the workspace root. `**` matches everything; prefer narrow patterns.
* Write `description` as *capability + trigger*: "Analyze failing Playwright tests. Use when a test run fails or a flaky test is reported." A vague description is why a skill or instruction never loads.
* Skill `name` must be a bare identifier. `org/skill` or `org:skill` silently fails to load.
* Agent `user-invocable: false` hides it from the picker but keeps it callable as a subagent. `disable-model-invocation: true` does the opposite.

## Verify, do not assume

Discovery is not compliance.

```bash
copilot instruction list        # what a CLI session would load
copilot skill list              # personal / builtin / plugin skills
copilot --agent nonexistent -p x   # error message lists available agents
```

In VS Code, open **Chat: Open Customizations** and confirm the file is listed for the selected harness, then run a small task and check the **References** list to prove it was attached.

Note: the CLI lists every discovered instruction source and does not filter by `applyTo`, so treat personal instruction files as always-on and keep them short.

## Reference files

* [scaffold.py](./scripts/scaffold.py) - generates frontmatter-correct files for each kind
* [frontmatter.md](./frontmatter.md) - field-by-field reference and valid examples
