#!/usr/bin/env python3
"""Validate MCP server configuration and optionally probe endpoints.

Checks JSON shape, transport fields, unresolved ${input:...} placeholders, and
with --probe performs a real initialize + tools/list round trip.

Never prints secret values: headers and env values are reported by shape only.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import ssl
import sys
import urllib.error
import urllib.request

PLACEHOLDER = re.compile(r"\$\{input:([^}]+)\}")
HTTP_OK = (200, 201, 202, 204)


def load(path: str) -> tuple[dict, str]:
    raw = open(path, encoding="utf-8-sig").read()
    data = json.loads(raw)
    for key in ("servers", "mcpServers"):
        if isinstance(data.get(key), dict):
            return data[key], key
    raise SystemExit("no 'servers' or 'mcpServers' object found in " + path)


def shape(value: str) -> str:
    v = str(value)
    if not v:
        return "empty"
    if v.startswith("${"):
        return "UNRESOLVED PLACEHOLDER"
    if len(v) > 24:
        return "set (%d chars, prefix %r...)" % (len(v), v[:8])
    return "set (short)"


def static_check(name: str, cfg: dict, inputs: set[str]) -> list[str]:
    problems = []
    transport = str(cfg.get("type", cfg.get("transport", "stdio"))).lower()

    if transport in ("http", "sse", "streamable-http"):
        if not cfg.get("url"):
            problems.append("remote server has no 'url'")
        if cfg.get("command"):
            problems.append("remote server also defines 'command' (copy-paste error)")
        url = str(cfg.get("url", ""))
        if url and not url.startswith(("http://", "https://")):
            problems.append("'url' is not http(s): " + url)
        if url.startswith("http://") and "localhost" not in url and "127.0.0.1" not in url:
            problems.append("plain http:// to a non-loopback host: " + url)
    elif transport == "stdio":
        if not cfg.get("command"):
            problems.append("stdio server has no 'command'")
        if cfg.get("url"):
            problems.append("stdio server also defines 'url' (copy-paste error)")
    else:
        problems.append("unknown transport: " + transport)

    for key in ("headers", "env"):
        section = cfg.get(key)
        if not isinstance(section, dict):
            continue
        for hname, hval in section.items():
            raw = str(hval)
            for ref in PLACEHOLDER.findall(raw):
                if ref not in inputs:
                    problems.append(
                        "%s[%s] references undeclared input '%s'" % (key, hname, ref))
                else:
                    problems.append(
                        "%s[%s] depends on prompt input '%s' - if it is never resolved the "
                        "server receives the literal placeholder and only tool calls fail"
                        % (key, hname, ref))

    args = cfg.get("args")
    if args is not None and not isinstance(args, list):
        problems.append("'args' must be an array")
    if isinstance(args, list) and any(a == "@latest" for a in args):
        problems.append("unpinned @latest in args; pin a version for shared configs")

    return problems


def read_sse(text: str) -> dict | None:
    for line in text.splitlines():
        if line.startswith("data:"):
            payload = line[5:].strip()
            try:
                return json.loads(payload)
            except Exception:
                continue
    try:
        return json.loads(text)
    except Exception:
        return None


def rpc(url: str, headers: dict, method: str, params: dict, rid: int,
        timeout: float) -> tuple[int, dict | None]:
    body = json.dumps({
        "jsonrpc": "2.0", "id": rid, "method": method, "params": params,
    }).encode("utf-8")
    req = urllib.request.Request(url, data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json, text/event-stream")
    for k, v in headers.items():
        req.add_header(k, v)
    ctx = ssl.create_default_context()
    with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:
        raw = resp.read().decode("utf-8", "replace")
        return resp.status, read_sse(raw)


def probe(name: str, cfg: dict, timeout: float) -> list[str]:
    url = cfg.get("url")
    if not url:
        return ["probe skipped: no url (stdio servers are not probed)"]
    headers = {k: str(v) for k, v in (cfg.get("headers") or {}).items()}
    out = []
    try:
        status, init = rpc(url, headers, "initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "mcp-doctor", "version": "1.0"},
        }, 1, timeout)
        label = "initialize"
        if status not in HTTP_OK:
            out.append("%s -> HTTP %d" % (label, status))
            return out
        info = (init or {}).get("result", {}).get("serverInfo", {})
        if info:
            out.append("initialize -> HTTP %d, server %s %s"
                       % (status, info.get("name", "?"), info.get("version", "")))
        else:
            err = (init or {}).get("error")
            out.append("initialize -> HTTP %d but no result: %s" % (status, err))

        status, listed = rpc(url, headers, "tools/list", {}, 2, timeout)
        tools = (listed or {}).get("result", {}).get("tools", [])
        if tools:
            names = [t.get("name", "?") for t in tools]
            out.append("tools/list -> %d tool(s): %s"
                       % (len(names), ", ".join(names[:8]) + ("..." if len(names) > 8 else "")))
        else:
            out.append("tools/list -> no tools returned: %s" % ((listed or {}).get("error"),))

        if tools:
            first = tools[0].get("name")
            schema = tools[0].get("inputSchema", {}) or {}
            if not schema.get("required"):
                status, called = rpc(url, headers, "tools/call", {
                    "name": first, "arguments": {},
                }, 3, timeout)
                res = (called or {}).get("result", {})
                if res.get("isError"):
                    text = ""
                    for part in res.get("content", []):
                        if part.get("type") == "text":
                            text = str(part.get("text", ""))[:200]
                    out.append("tools/call %s -> isError: true  <<< REAL FAILURE: %s"
                               % (first, text))
                elif res:
                    out.append("tools/call %s -> ok" % first)
    except urllib.error.HTTPError as exc:
        out.append("HTTP error %d for %s" % (exc.code, url))
    except Exception as exc:
        out.append("probe failed: %s: %s" % (type(exc).__name__, exc))
    return out


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--config", required=True)
    p.add_argument("--probe", action="store_true",
                   help="perform initialize + tools/list against http servers")
    p.add_argument("--timeout", type=float, default=20.0)
    args = p.parse_args()

    path = os.path.expanduser(args.config)
    if not os.path.exists(path):
        print("missing config: " + path)
        return 2

    print("config: " + os.path.abspath(path))
    try:
        servers, top_key = load(path)
    except SystemExit as exc:
        print(str(exc))
        return 2
    except json.JSONDecodeError as exc:
        print("invalid JSON: " + str(exc))
        return 2

    try:
        root_inputs = json.loads(open(path, encoding="utf-8-sig").read()).get("inputs") or []
    except Exception:
        root_inputs = []
    inputs = {str(i.get("id")) for i in root_inputs if isinstance(i, dict)}
    if inputs:
        print("declared inputs: " + ", ".join(sorted(inputs)))
    print("servers under '%s': %d" % (top_key, len(servers)))

    total = 0
    for name, cfg in servers.items():
        if not isinstance(cfg, dict):
            print("\n[%s] not an object" % name)
            total += 1
            continue
        transport = str(cfg.get("type", cfg.get("transport", "stdio"))).lower()
        print("\n[%s] transport=%s" % (name, transport))
        for key in ("headers", "env"):
            for hname, hval in (cfg.get(key) or {}).items():
                print("  %s %s: %s" % (key, hname, shape(hval)))

        problems = static_check(name, cfg, inputs)
        for item in problems:
            print("  WARN " + item)
            total += 1

        if args.probe and transport in ("http", "sse", "streamable-http"):
            for line in probe(name, cfg, args.timeout):
                print("  " + line)
                if "REAL FAILURE" in line or "no tools" in line:
                    total += 1

    print("\n%d issue(s) found" % total)
    return 1 if total else 0


if __name__ == "__main__":
    raise SystemExit(main())
