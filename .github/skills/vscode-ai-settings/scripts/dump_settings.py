#!/usr/bin/env python3
"""Dump the AI-related settings that this VS Code build actually ships.

Defaults drift between releases and the published documentation lags. This reads
the shipping registry directly:

  * core settings  -> <app>/out/vs/workbench/workbench.desktop.main.js
  * extension settings -> <app>/extensions/*/package.json (contributes.configuration)

Usage:
  python dump_settings.py                        # auto-detect the newest install
  python dump_settings.py --app "C:\\path\\to\\resources\\app"
  python dump_settings.py --json out.json
  python dump_settings.py --check chat.agent.maxRequests chat.mcp.access
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import re
import sys

PREFIXES = ("chat.", "inlineChat.", "imageCarousel.")
AI_EXT = re.compile(r"^(chat\.|github\.copilot|inlineChat|imageCarousel|"
                    r"workbench\.settings\.showAISearchToggle)", re.I)


def find_app() -> str | None:
    home = os.path.expanduser("~")
    bases = [
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "Microsoft VS Code"),
        os.path.join(home, "AppData", "Local", "Programs", "Microsoft VS Code"),
        os.path.join(os.environ.get("PROGRAMFILES", ""), "Microsoft VS Code"),
        os.path.join(os.environ.get("PROGRAMFILES(X86)", ""), "Microsoft VS Code"),
        "/usr/share/code",
        "/usr/lib/code",
        "/Applications/Visual Studio Code.app/Contents/Resources/app",
    ]
    for base in bases:
        if not base or not os.path.isdir(base):
            continue
        # Windows/Insiders layout puts a version hash between base and resources
        for cand in sorted(glob.glob(os.path.join(base, "*", "resources", "app"))):
            if os.path.isdir(os.path.join(cand, "out")):
                return cand
        cand = os.path.join(base, "resources", "app")
        if os.path.isdir(os.path.join(cand, "out")):
            return cand
    return None


def brace_match(s: str, start: int) -> int:
    depth, i, in_str, n = 0, start, None, len(s)
    while i < n:
        c = s[i]
        if in_str:
            if c == "\\":
                i += 2
                continue
            if c == in_str:
                in_str = None
            i += 1
            continue
        if c in "\"'`":
            in_str = c
            i += 1
            continue
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    return -1


def parse_default(frag: str):
    m = re.search(r'(?<![\w$.])default:\s*'
                  r'(!0|!1|"[^"]*"|-?\d+(?:\.\d+)?(?:e\d+)?|\[[^\]]*\]|\{[^{}]*\}|[A-Za-z_$][\w$.]*)'
                  r'(\s*\?\?\s*(!0|!1|-?\d+(?:\.\d+)?(?:e\d+)?|"[^"]*"))?', frag)
    if not m:
        return "__none__"
    token = m.group(1)
    fallback = m.group(3)
    if token in ("!0", "!1") or token.startswith('"') or re.match(r'^-?\d', token) \
            or token.startswith("[") or token.startswith("{"):
        pass
    elif fallback:
        token = fallback          # dynamic default: use the literal fallback
    else:
        return "__dynamic__"
    if token == "!0":
        return True
    if token == "!1":
        return False
    if token.startswith('"'):
        return token[1:-1]
    if token.startswith("["):
        try:
            return json.loads(token)
        except Exception:
            return token
    if token.startswith("{"):
        return "{}"
    try:
        f = float(token)
        return int(f) if f == int(f) else f
    except Exception:
        return token


def grab(frag: str, name: str):
    m = re.search(name + r':\[([^\]]*)\]', frag)
    if not m:
        return None
    return [v.strip().strip('"') for v in m.group(1).split(",") if v.strip()]


def core_settings(app: str) -> dict:
    bundle = os.path.join(app, "out", "vs", "workbench", "workbench.desktop.main.js")
    if not os.path.exists(bundle):
        return {}
    txt = open(bundle, encoding="utf-8", errors="replace").read()

    out = {}

    def record(key: str, frag: str) -> None:
        if key in out or len(frag) > 12000:
            return
        tm = re.search(r'type:"(\w+)"', frag)
        if not tm:
            return
        out[key] = {
            "key": key,
            "type": tm.group(1),
            "default": parse_default(frag),
            "enum": grab(frag, "enum"),
            "tags": grab(frag, "tags") or [],
            "deprecated": bool(re.search(r'(?<![\w$.])(markdownDeprecationMessage|deprecationMessage):', frag)),
            "source": "core",
        }

    for m in re.compile(r'"((?:chat|inlineChat|imageCarousel)\.[A-Za-z0-9_.]+)"\s*:\s*\{').finditer(txt):
        end = brace_match(txt, m.end() - 1)
        if end > 0:
            record(m.group(1), txt[m.end() - 1:end])

    # Computed keys: IDENT = "chat.something" ... [IDENT]: { ... }
    ident_to_key: dict[str, set[str]] = {}
    for m in re.finditer(r'([A-Za-z_$][\w$]{0,10})\s*=\s*"((?:chat|inlineChat|imageCarousel)\.[A-Za-z0-9_.]+)"', txt):
        ident_to_key.setdefault(m.group(1), set()).add(m.group(2))
    for ident, keys in ident_to_key.items():
        if len(keys) != 1:
            continue
        key = next(iter(keys))
        if key in out:
            continue
        am = re.search(r'\[' + re.escape(ident) + r'\]\s*:\s*\{', txt)
        if not am:
            continue
        end = brace_match(txt, am.end() - 1)
        if end > 0:
            record(key, txt[am.end() - 1:end])

    return out


def ext_settings(app: str) -> dict:
    out = {}
    root = os.path.join(app, "extensions")
    if not os.path.isdir(root):
        return out
    for d in sorted(os.listdir(root)):
        pj = os.path.join(root, d, "package.json")
        if not os.path.exists(pj):
            continue
        try:
            data = json.load(open(pj, encoding="utf-8-sig"))
        except Exception:
            continue
        conf = (data.get("contributes") or {}).get("configuration")
        blocks = [conf] if isinstance(conf, dict) else [c for c in conf or [] if isinstance(c, dict)]
        for b in blocks:
            for k, spec in (b.get("properties") or {}).items():
                if not isinstance(spec, dict) or not AI_EXT.search(k):
                    continue
                out.setdefault(k, {
                    "key": k,
                    "type": spec.get("type", ""),
                    "default": spec.get("default", "__none__"),
                    "enum": spec.get("enum"),
                    "tags": spec.get("tags") or [],
                    "deprecated": bool(spec.get("deprecationMessage") or spec.get("markdownDeprecationMessage")),
                    "source": "ext:" + data.get("name", d),
                })
    return out


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--app")
    p.add_argument("--json")
    p.add_argument("--check", nargs="*", default=None,
                   help="print only these keys")
    p.add_argument("--tagged", choices=["experimental", "preview"])
    p.add_argument("--deprecated", action="store_true")
    args = p.parse_args()

    app = args.app or find_app()
    if not app or not os.path.isdir(app):
        print("could not locate the VS Code app folder; pass --app")
        return 2

    rows = {}
    rows.update(core_settings(app))
    for k, v in ext_settings(app).items():
        rows.setdefault(k, v)

    if args.check:
        rc = 0
        for key in args.check:
            r = rows.get(key)
            if not r:
                print("NOT FOUND  " + key)
                rc = 1
                continue
            print("%-56s %-10s default=%r tags=%s%s"
                  % (key, r["type"], r["default"], r["tags"],
                     "  DEPRECATED" if r["deprecated"] else ""))
            if r["enum"]:
                print(" " * 58 + "enum=" + str(r["enum"]))
        return rc

    sel = sorted(rows.values(), key=lambda r: r["key"])
    if args.deprecated:
        sel = [r for r in sel if r["deprecated"]]
    if args.tagged:
        sel = [r for r in sel if args.tagged in (r["tags"] or [])]

    if args.json:
        json.dump(sel, open(args.json, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
        print("wrote %d settings to %s" % (len(sel), args.json))
        return 0

    print("app: " + app)
    print("AI settings: %d (core %d, extension %d)"
          % (len(rows), sum(1 for r in rows.values() if r["source"] == "core"),
             sum(1 for r in rows.values() if r["source"] != "core")))
    print("deprecated: %d | experimental/preview: %d\n"
          % (sum(1 for r in rows.values() if r["deprecated"]),
             sum(1 for r in rows.values() if any(t in ("experimental", "preview") for t in r["tags"]))))
    for r in sel:
        flag = "X" if r["deprecated"] else " "
        tags = (" [" + ",".join(r["tags"]) + "]") if r["tags"] else ""
        enum = ("  enum=" + str(r["enum"])) if r["enum"] else ""
        print("%s %-56s %-8s default=%r%s%s"
              % (flag, r["key"], r["type"], r["default"], tags, enum))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
