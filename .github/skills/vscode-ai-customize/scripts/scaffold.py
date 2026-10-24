#!/usr/bin/env python3
"""Scaffold VS Code AI customization files with valid frontmatter.

Prints the generated content and the destination path. It never writes files
unless --write is passed, and it refuses to overwrite an existing file unless
--force is also passed.
"""

from __future__ import annotations

import argparse
import os
import sys

BODY = {
    "instruction": """# {name}

## Conventions

* <state the rule imperatively>
* <state the next rule>

## Validation

* <the command to run after changing a matching file>
""",
    "prompt": """{name} task.

## Inputs

${{input:target}} ${{selection}}

## Steps

1. <first step>
2. <second step>

## Done when

* <objective success criterion>
""",
    "agent": """You are {name}.

## Responsibilities

* <what this role does>

## Constraints

* Do not edit files outside the stated scope.
""",
    "skill": """# {name}

<One paragraph on what this skill achieves.>

## Steps

1. <first step>
2. <second step>
""",
}


def quote(value: str) -> str:
    escaped = value.replace("'", "''")
    return "'" + escaped + "'"


def build_frontmatter(kind: str, name: str, args: argparse.Namespace) -> list[str]:
    lines = ["---", "name: " + quote(name)]

    if kind == "instruction":
        if args.description:
            lines.append("description: " + quote(args.description))
        if args.apply_to:
            lines.append("applyTo: " + quote(args.apply_to))

    elif kind == "prompt":
        if args.description:
            lines.append("description: " + quote(args.description))
        if args.hint:
            lines.append("argument-hint: " + quote(args.hint))
        if args.agent:
            lines.append("agent: " + quote(args.agent))
        if args.tools:
            lines.append("tools: [" + ", ".join(quote(t) for t in args.tools) + "]")

    elif kind == "agent":
        if args.description:
            lines.append("description: " + quote(args.description))
        if args.hint:
            lines.append("argument-hint: " + quote(args.hint))
        if args.tools:
            lines.append("tools: [" + ", ".join(quote(t) for t in args.tools) + "]")
        if args.model:
            lines.append("model: " + quote(args.model))
        if args.reasoning_effort:
            lines.append("reasoning-effort: " + quote(args.reasoning_effort))
        if args.subagent_only:
            lines.append("user-invocable: false")
        if args.no_subagent:
            lines.append("disable-model-invocation: true")

    elif kind == "skill":
        # description is mandatory for skills
        if not args.description:
            raise SystemExit("skill requires --description (capability + when to use it)")
        lines.append("description: " + quote(args.description))
        if args.hint:
            lines.append("argument-hint: " + quote(args.hint))
        if args.fork:
            lines.append("context: fork")

    lines.append("---")
    return lines


def destination(kind: str, name: str, scope: str) -> str:
    home = os.path.expanduser("~")
    slug = name.strip().lower().replace(" ", "-")
    user_root = {
        "instruction": os.path.join(home, ".copilot", "instructions"),
        "agent": os.path.join(home, ".copilot", "agents"),
        "skill": os.path.join(home, ".copilot", "skills"),
        "prompt": os.path.join(home, ".copilot", "prompts"),
    }
    workspace_root = {
        "instruction": os.path.join(".github", "instructions"),
        "agent": os.path.join(".github", "agents"),
        "skill": os.path.join(".github", "skills"),
        "prompt": os.path.join(".github", "prompts"),
    }

    if scope == "user":
        base = user_root[kind]
        if kind == "skill":
            return os.path.join(base, slug, "SKILL.md")
        if kind == "agent":
            return os.path.join(base, slug + ".agent.md")
        return os.path.join(base, slug + ".instructions.md" if kind == "instruction"
                            else slug + ".prompt.md")

    base = workspace_root[kind]
    if kind == "skill":
        return os.path.join(base, slug, "SKILL.md")
    if kind == "agent":
        return os.path.join(base, slug + ".agent.md")
    if kind == "instruction":
        return os.path.join(base, slug + ".instructions.md")
    return os.path.join(base, slug + ".prompt.md")


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--kind", required=True,
                   choices=["instruction", "prompt", "agent", "skill"])
    p.add_argument("--name", required=True, help="display name / identifier")
    p.add_argument("--scope", default="workspace", choices=["workspace", "user"])
    p.add_argument("--description", default="")
    p.add_argument("--apply-to", default="")
    p.add_argument("--hint", default="")
    p.add_argument("--agent", default="")
    p.add_argument("--tools", default="")
    p.add_argument("--model", default="")
    p.add_argument("--reasoning-effort", default="",
                   choices=["", "low", "medium", "high", "xhigh", "max"])
    p.add_argument("--subagent-only", action="store_true")
    p.add_argument("--no-subagent", action="store_true")
    p.add_argument("--fork", action="store_true")
    p.add_argument("--write", action="store_true")
    p.add_argument("--force", action="store_true")
    args = p.parse_args()
    args.tools = [t.strip() for t in args.tools.split(",") if t.strip()]

    if args.kind == "skill" and "/" in args.name or ":" in args.name:
        print("warning: skill names must be bare identifiers; "
              "prefixes like org/skill fail to load", file=sys.stderr)

    content = "\n".join(build_frontmatter(args.kind, args.name, args)) + "\n" + \
        BODY[args.kind].format(name=args.name)

    dest = destination(args.kind, args.name, args.scope)
    print("# destination: " + os.path.abspath(dest))
    print(content)

    if args.write:
        if os.path.exists(dest) and not args.force:
            print("refusing to overwrite (use --force): " + dest, file=sys.stderr)
            return 2
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        with open(dest, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(content)
        print("# wrote " + os.path.abspath(dest), file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
