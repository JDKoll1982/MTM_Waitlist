---
name: 'Agent skills'
description: 'Authoring SKILL.md skills: frontmatter, personal vs project locations, progressive loading, forked context, and discovery checks. Use when packaging a specialized capability with scripts or reference files.'
applyTo: '**/SKILL.md'
---
# Agent skills (`SKILL.md`)

Verified on VS Code 1.143.1 and Copilot CLI 1.0.98. Personal discovery confirmed: `~/.copilot/skills/_probe/SKILL.md` appeared under "Personal skills" in `copilot skill list`.

## What they are

Skills are folders of instructions, scripts, and resources an agent loads **when relevant**. Agent Skills is an open standard shared by Copilot in VS Code, Copilot CLI, the Copilot app, and the cloud agent — so a skill travels further than prompt files or profile-scoped customizations.

## Locations

| Scope | Paths |
| --- | --- |
| Project | `.github/skills/`, `.claude/skills/`, `.agents/skills/` |
| Personal | `~/.copilot/skills/`, `~/.claude/skills/`, `~/.agents/skills/` |
| Plugin | Bundled inside an installed plugin |
| Custom | Directories added with `copilot skill add <directory>` |

Each skill is a directory containing `SKILL.md` plus optional supporting files:

```
~/.copilot/skills/test-runner/
  SKILL.md
  test-template.js
```

## Frontmatter

| Field | Required | Notes |
| --- | --- | --- |
| `name` | Yes | Skill identifier; used as the slash command |
| `description` | Yes | What it does **and when to use it**. Max 1024 characters |
| `argument-hint` | No | Hint shown when invoked as a slash command |
| `user-invocable` | No | Default `true`. `false` hides it from `/` but still allows automatic loading |
| `disable-model-invocation` | No | Default `false`. `true` requires manual `/` invocation |
| `context` | No | `fork` runs the skill in a forked subagent context |

Naming rule: do **not** add namespace prefixes to `name`. Values like `myorg/skill` or `myorg:skill` **silently fail to load**. Plugin-provided skills get the plugin name prefixed automatically (for example `/my-plugin:test-runner`).

## Body

* Write imperative instructions, plus the decision rules the agent cannot infer.
* Reference bundled files with Markdown links or `#file:` relative to `SKILL.md`, e.g. `[template](./test-template.js)`. Unreferenced files may never be read.
* `~/` resolves from your user home for shared resources.
* Keep `SKILL.md` itself lean; push long material into referenced files.

## Forked context

Setting `context: fork` runs the skill in a separate subagent so its exploration does not consume the main context. It requires `github.copilot.chat.skillTool.enabled` (`false` by default) in VS Code.

## Operating rules

* `description` is the routing key. If the agent never loads the skill, rewrite the description before touching anything else.
* Skills are progressive: only frontmatter is read until the skill is selected. That is the reason to keep `SKILL.md` small.
* Verify discovery with `copilot skill list` — it prints personal skills, builtin skills, and plugin skills separately.
* Install third-party skills only after reviewing publisher, source, and included files. A skill can carry executable scripts.
* Prefer a skill over a prompt file when the capability must work in the CLI and the desktop app too.
