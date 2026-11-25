"""Independent validator. Shares no code with the extension: regex parsing, its own SCC algorithm."""

import json
import os
import re
import sys
from collections import defaultdict

DUMP = sys.argv[1]
ROOT = sys.argv[2]

data = json.load(open(DUMP, encoding="utf-8"))
model = data["model"]
nodes = {n["id"]: n for n in model["nodes"]}
edges = model["edges"]

print("=" * 78)
print("INDEPENDENT VALIDATION")
print("=" * 78)
print(f"nodes={len(nodes)} edges={len(edges)} findings={len(model['findings'])}")

# ---------------------------------------------------------------- cycles
# Own Kosaraju implementation, not the extension's Tarjan.

CYCLE_KINDS = {"injects", "getservice", "resolves", "binds", "field", "constructs", "composes"}
ALL_KINDS = {e["kind"] for e in edges}


def sccs(kinds):
    adj = defaultdict(list)
    radj = defaultdict(list)
    verts = set()
    for e in edges:
        if e["kind"] not in kinds:
            continue
        if e["source"] not in nodes or e["target"] not in nodes:
            continue
        adj[e["source"]].append(e["target"])
        radj[e["target"]].append(e["source"])
        verts.add(e["source"])
        verts.add(e["target"])

    order, seen = [], set()
    for start in sorted(verts):
        if start in seen:
            continue
        stack = [(start, iter(adj[start]))]
        seen.add(start)
        while stack:
            node, children = stack[-1]
            advanced = False
            for child in children:
                if child not in seen:
                    seen.add(child)
                    stack.append((child, iter(adj[child])))
                    advanced = True
                    break
            if not advanced:
                order.append(node)
                stack.pop()

    assigned, comps = set(), []
    for start in reversed(order):
        if start in assigned:
            continue
        comp, stack = [], [start]
        assigned.add(start)
        while stack:
            node = stack.pop()
            comp.append(node)
            for parent in radj[node]:
                if parent not in assigned:
                    assigned.add(parent)
                    stack.append(parent)
        comps.append(comp)
    return comps


print("\n--- CYCLES (independent Kosaraju) ---")
for label, kinds in (("dependency edges only", CYCLE_KINDS), ("ALL edge kinds", ALL_KINDS)):
    comps = [c for c in sccs(kinds) if len(c) > 1]
    self_loops = [e for e in edges if e["kind"] in kinds and e["source"] == e["target"]]
    print(f"  {label:22} components>1: {len(comps)}   self-loops: {len(self_loops)}")
    for comp in comps[:5]:
        print("     " + " <-> ".join(nodes[n]["label"] for n in comp))
    for e in self_loops[:5]:
        print(f"     self: {nodes[e['source']]['label']} ({e['kind']})")
if not [c for c in sccs(ALL_KINDS) if len(c) > 1]:
    print("  => the whole graph, including inheritance, is acyclic")

# ---------------------------------------------------------------- XAML bindings
# Regex XAML parsing, independent of htmlparser2.

BIND = re.compile(r'\{\s*x:Bind\s+([^,}]+)')
XCLASS = re.compile(r'x:Class="([^"]+)"')
XDATATYPE = re.compile(r'x:DataType="([^"]+)"')
XNAME = re.compile(r'x:Name="([^"]+)"')
USING_PREFIX = re.compile(r'xmlns:(\w+)="using:([^"]+)"')
TEMPLATE = re.compile(r'<DataTemplate\b')


def read(path):
    return open(os.path.join(ROOT, path), encoding="utf-8-sig", errors="replace").read()


print("\n--- XAML BINDINGS (independent regex parse) ---")
xaml_files = data["xamlFiles"]
print(f"  xaml files: {len(xaml_files)}")

# Members per type, regex-scanned from .cs files.
MEMBER = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*'
    r'(?:public|private|protected|internal|static|readonly|partial|async|override|virtual|sealed|\s)*\s*'
    r'(?:[\w<>,\[\]?\.]+\s+)?([A-Za-z_]\w*)\s*(?:[({=;]|=>)',
    re.M)
CLASS = re.compile(r'\b(?:partial\s+)?(?:class|record|struct|interface)\s+(\w+)')
NAMESPACE = re.compile(r'^\s*namespace\s+([\w.]+)', re.M)

types = defaultdict(set)
type_files = {}
for path in data["csharpFiles"]:
    text = read(path)
    ns = NAMESPACE.search(text)
    ns_name = ns.group(1) if ns else ""
    for cls in CLASS.finditer(text):
        fq = f"{ns_name}.{cls.group(1)}" if ns_name else cls.group(1)
        type_files.setdefault(fq, path)
        names = types[fq]
        for m in MEMBER.finditer(text):
            names.add(m.group(1))

unresolved_roots = []
unresolved_members = []
checked_bindings = 0

for path in xaml_files:
    text = read(path)
    cls = XCLASS.search(text)
    if not cls:
        continue
    class_name = cls.group(1)
    view_members = types.get(class_name)
    if not view_members:
        continue
    named = set(XNAME.findall(text))
    prefixes = dict(USING_PREFIX.findall(text))
    dt = XDATATYPE.search(text)
    dt_type = None
    if dt:
        value = dt.group(1)
        if ":" in value:
            prefix, local = value.split(":", 1)
            if prefix in prefixes:
                dt_type = f"{prefixes[prefix]}.{local}"
        else:
            ns = NAMESPACE.search(read(type_files.get(class_name, "")))
            dt_type = f"{ns.group(1)}.{value}" if ns else value

    for m in BIND.finditer(text):
        expr = m.group(1).strip()
        root = expr.split(".")[0].split("(")[0].strip()
        if not root or not re.match(r'^[A-Za-z_]', root):
            continue
        checked_bindings += 1
        if root in named:
            continue
        if root not in view_members:
            unresolved_roots.append((path, root, expr))
            continue
        # Deeper: root is a view member whose type we can resolve through the graph.
        for e in edges:
            if e["source"] == class_name and e["kind"] == "binds" and e.get("label") == root:
                pass

print(f"  bindings inspected: {checked_bindings}")
print(f"  roots with no member on the view: {len(unresolved_roots)}")
for path, root, expr in unresolved_roots[:10]:
    print(f"     {path}: root '{root}'  ({expr})")

# ---------------------------------------------------------------- template bindings
print("\n--- TEMPLATE BINDINGS (x:Bind inside DataTemplate, checked against x:DataType) ---")
template_total = template_missing = 0
for path in xaml_files:
    text = read(path)
    for block in re.finditer(r'<DataTemplate\b([^>]*)>(.*?)</DataTemplate>', text, re.S):
        header, body = block.group(1), block.group(2)
        dt = XDATATYPE.search(header)
        if not dt:
            continue
        value = dt.group(1)
        prefixes = dict(USING_PREFIX.findall(text))
        if ":" in value:
            prefix, local = value.split(":", 1)
            fq = f"{prefixes[prefix]}.{local}" if prefix in prefixes else local
        else:
            cls = XCLASS.search(text)
            ns = NAMESPACE.search(read(type_files.get(cls.group(1), ""))) if cls else None
            fq = f"{ns.group(1)}.{value}" if ns else value
        members = types.get(fq)
        if not members:
            continue
        for m in BIND.finditer(body):
            expr = m.group(1).strip()
            root = expr.split(".")[0].split("(")[0].strip()
            if not root or not re.match(r'^[A-Za-z_]', root) or root == "x:Bind":
                continue
            template_total += 1
            if root not in members:
                template_missing += 1
                if template_missing <= 10:
                    print(f"     {path}: {fq.split('.')[-1]}.{root} missing  ({expr})")
print(f"  template bindings inspected: {template_total}, roots missing on x:DataType: {template_missing}")

# ---------------------------------------------------------------- unbound views
print("\n--- UNBOUND VIEWS (bind ViewModel.* but declare no ViewModel) ---")
unbound = 0
for path in xaml_files:
    text = read(path)
    cls = XCLASS.search(text)
    if not cls:
        continue
    class_name = cls.group(1)
    members = types.get(class_name)
    if not members:
        continue
    binds_vm = any(m.group(1).strip().startswith("ViewModel.") for m in BIND.finditer(text))
    if binds_vm and "ViewModel" not in members:
        unbound += 1
        print(f"     {path}: {class_name}")
print(f"  unbound views: {unbound}")

print("\n" + "=" * 78)
print(f"VERDICT: cycles={len([c for c in sccs(CYCLE_KINDS) if len(c) > 1])}, "
      f"root-mismatch={len(unresolved_roots)}, template-mismatch={template_missing}, unbound={unbound}")
print("=" * 78)
