#!/usr/bin/env python3
"""Generate the Graphviz sources behind the MTM_Waitlist workflow diagrams.

Each sheet is one JSON spec; this script turns it into a `.dot` file in `docs/diagrams/dot/`,
alongside the print-ready PDFs in `docs/diagrams/pdf/`. Style is fixed here so every sheet
matches: landscape, pure white canvas, pastel swimlane fills, 1.5 pt borders, 11 pt labels,
explicit line breaks so nothing clips.

    python docs/diagrams/tools/dotgen.py docs/diagrams/tools/spec-diagram-workorder-new-request.json

Render a single sheet for a quick look (Graphviz lays the graph out afresh):

    "%ProgramFiles%\\Graphviz\\bin\\dot.exe" -Tpdf -Gmargin=0 -o sheet.pdf docs/diagrams/dot/x.dot

For the paged, print-ready PDFs see `render_sheets.py`.
"""
import json
import pathlib
import sys

DOT_DIR = pathlib.Path(__file__).resolve().parent.parent / "dot"

FILL = {
    "ui": ("#DCE9FF", "#111111"),
    "vm": ("#EFE3FF", "#111111"),
    "dec": ("#FFF1C9", "#B7791F"),
    "svc": ("#D6F5F0", "#111111"),
    "db": ("#EDEDED", "#111111"),
    "erp": ("#F2E8D5", "#8A6D3B"),
    "mirror": ("#FFE0E3", "#B02A37"),
    "note": ("#FFF1C9", "#B7791F"),
    "refactor": ("#FFD6D6", "#C62828"),
    "conn": ("#E8F0FF", "#1F6FEB"),
}
SHAPE = {
    "ui": "box", "vm": "box", "svc": "box", "dec": "diamond", "db": "cylinder",
    "erp": "box", "mirror": "component", "note": "note",
    "refactor": "note", "conn": "cds",
}
DASHED = {"note", "refactor", "conn"}

LANE_COLOR = {
    "ui": ("#0B3D91", "#1F6FEB"),
    "vm": ("#4B2A8A", "#8250DF"),
    "svc": ("#0A5A4F", "#0F7B6C"),
    "data": ("#5A4300", "#8A6D3B"),
}

EDGE_STYLE = {
    "": "",
    "dash": ', style=dashed, color="#B7791F"',
    "vdash": ', style=dashed, color="#8250DF"',
    "mirror": ', style=dashed, color="#B02A37", constraint=false',
    "warnlink": ', style=dashed, color="#B7791F", arrowhead=none, constraint=false',
    "link": ', style=dashed, color="#C62828", arrowhead=none, constraint=false',
    "connlink": ', style=dashed, color="#1F6FEB", arrowhead=none, constraint=false',
    "red": ', color="#C62828", penwidth=2.0',
    "dots": ', style=dotted',
}

HEADER = """  graph [
    rankdir=LR, splines=polyline, overlap=false, nodesep=0.45, ranksep=0.55,
    newrank=true, fontname="Segoe UI", fontsize=11, bgcolor="#FFFFFF", pad=0.02
  ];
  node [
    shape=box, style="rounded,filled", fontname="Segoe UI", fontsize=11,
    margin="0.10,0.05", penwidth=1.5, color="#111111", fillcolor="#FFFFFF"
  ];
  edge [
    fontname="Segoe UI", fontsize=11, color="#111111", arrowsize=0.6,
    penwidth=1.5, fontcolor="#111111"
  ];"""


def node_line(node_id, label, kind):
    fill, border = FILL[kind]
    attrs = [f"shape={SHAPE[kind]}", f'label="{label}"', f'fillcolor="{fill}"']
    if kind == "db":
        attrs.append('fontname="Consolas"')
    if border != "#111111":
        attrs.append(f'color="{border}"')
    if kind in DASHED:
        attrs.append('style="filled,dashed"')
    if kind == "dec":
        attrs.append('margin="0.01,0.01"')
    return f"    {node_id} [{', '.join(attrs)}];"


def emit(sheet):
    """Return the `.dot` text for one sheet spec."""
    lanes = sheet["lanes"]
    titles = sheet.get("lane_titles", {})
    order = [lane for lane in lanes if lanes[lane]]
    rows = max((len(nodes) for nodes in lanes.values()), default=0)

    lines = [
        f"// {sheet['title']} — {sheet['sheet']}",
        f"// Source of truth: {sheet['source']}",
        f"digraph {sheet['name']} {{",
        HEADER,
    ]

    for lane in order:
        fc, bc = LANE_COLOR.get(lane, ("#111111", "#111111"))
        lines.append(f"  subgraph cluster_{lane} {{")
        lines.append(f'    label="{titles.get(lane, lane)}"; fontcolor="{fc}"; color="{bc}";')
        lines.append('    style="rounded,dashed"; penwidth=1.5; labeljust=l; labelloc=t; margin=8;')
        for node_id, label, kind in lanes[lane]:
            lines.append(node_line(node_id, label, kind))
        lines.append("  }")

    # Rank rows keep a lane's nodes reading top-to-bottom; the invis chains only carry that order.
    for row in range(rows):
        members = [lanes[lane][row][0] for lane in order if len(lanes[lane]) > row]
        if len(members) > 1:
            lines.append("  { rank=same; " + "; ".join(members) + "; }")
            lines.append("  " + " -> ".join(members) + " [style=invis, constraint=false];")

    for lane in order:
        ids = [node_id for node_id, _, _ in lanes[lane]]
        if len(ids) > 1:
            lines.append("  " + " -> ".join(ids) + " [style=invis, constraint=true];")

    for edge in sheet["edges"]:
        a, b, label, style = (list(edge) + ["", ""])[:4]
        attrs = EDGE_STYLE[style]
        if label:
            attrs = f'label="{label}"' + attrs
        else:
            attrs = attrs.lstrip(", ")
        lines.append(f"  {a} -> {b} [{attrs}];" if attrs else f"  {a} -> {b};")

    lines.append("}")
    return "\n".join(lines) + "\n"


def write(sheet):
    DOT_DIR.mkdir(parents=True, exist_ok=True)
    path = DOT_DIR / (sheet["file"] + ".dot")
    path.write_text(emit(sheet), encoding="utf-8")
    return path.name


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit("usage: dotgen.py <spec.json> [spec.json ...]")
    for argument in sys.argv[1:]:
        spec = json.loads(pathlib.Path(argument).read_text(encoding="utf-8"))
        for sheet in spec["sheets"]:
            print("wrote", write(sheet))
