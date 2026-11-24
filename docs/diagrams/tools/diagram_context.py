#!/usr/bin/env python3
"""Print a read-only digest of a workflow's diagrams, so an agent can cite them.

A workflow ships as one JSON spec per variant. The spec generates the `.dot` sources in
`docs/diagrams/dot/`, and `render_sheets.py` turns the same spec into the PDFs in
`docs/diagrams/pdf/`. This script joins the three and prints what the workflow does — sheets, lanes,
nodes, edges, `[REFACTOR]` findings and cross-sheet connectors — so an agent can reference diagram
content instead of guessing from a picture.

    python docs/diagrams/tools/diagram_context.py --list
    python docs/diagrams/tools/diagram_context.py --workflow workorder-new-request
    python docs/diagrams/tools/diagram_context.py --workflow workorder-new-request --variant friendly
    python docs/diagrams/tools/diagram_context.py --workflow workorder-new-request --sheet 4 --inspect-pdf --png
    python docs/diagrams/tools/diagram_context.py --spec <spec.json> --pdf-file <file.pdf> --label "imported"

Nothing under `docs/diagrams/` is written. `--png` writes page images to the temp folder only, for the
agent to look at. Exit code 0 means a digest was produced (it may still carry warnings); 2 means the
request could not be satisfied at all.

The JSON spec is the source of truth. The `.dot` file is generated from it, so a disagreement between
the two is reported and the spec wins. A PDF can be described but never edited from here.

Requires PyMuPDF (`pip install pymupdf`) for `--inspect-pdf` and `--png`; everything else is stdlib.
"""
import argparse
import json
import pathlib
import re
import sys
import tempfile

DIAGRAMS = pathlib.Path(__file__).resolve().parent.parent
LANES = ["ui", "vm", "svc", "data"]
LANE_FALLBACK = {"ui": "UI", "vm": "ViewModel", "svc": "Service", "data": "Store / ERP"}

CONNECTOR = re.compile(r"^\[(?P<kind>[A-Za-z ]+?)\s+sheet\s+(?P<sheet>\d+)\](?P<rest>.*)$",
                       re.IGNORECASE | re.DOTALL)
DOCUMENT_REF = re.compile(r"\bDiagram\s+(\d+)\b", re.IGNORECASE)
DOT_NODE = re.compile(r"^\s{4,}([A-Za-z_]\w*)\s*\[shape=", re.MULTILINE)

PNG_DIR = pathlib.Path(tempfile.gettempdir()) / "mtm-diagrams" / "context"


class DiagramError(Exception):
    """The request cannot be satisfied — a bad workflow name, or no usable sources."""


def utf8_stdout():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def clean(label):
    """Undo the spec's literal `\\n` and collapse stray whitespace."""
    return " ".join(str(label).replace("\\n", "\n").split())


def cell(text):
    return str(text).replace("|", "\\|")


class DiagramSet:
    def __init__(self, root=DIAGRAMS):
        self.root = pathlib.Path(root)
        self.tools = self.root / "tools"
        self.dot_dir = self.root / "dot"
        self.pdf_dir = self.root / "pdf"
        self.manifest_path = self.tools / "manifest.json"
        self.documents = self._read_manifest()

    def _read_manifest(self):
        if not self.manifest_path.is_file():
            raise DiagramError(f"manifest not found: {self.manifest_path}")
        try:
            data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise DiagramError(f"manifest is not valid JSON: {error}") from error
        entries = data.get("documents")
        if not isinstance(entries, list) or not entries:
            raise DiagramError("manifest declares no documents")
        documents, seen = [], set()
        for entry in entries:
            absent = [key for key in ("workflow", "variant", "output", "spec") if not entry.get(key)]
            if absent:
                raise DiagramError(f"manifest entry is missing {', '.join(absent)}: {entry}")
            key = (entry["workflow"], entry["variant"])
            if key in seen:
                raise DiagramError(f"manifest maps {key[0]} / {key[1]} twice")
            seen.add(key)
            document = dict(entry)
            document["spec_path"] = self.tools / entry["spec"]
            document["pdf_path"] = self.pdf_dir / entry["workflow"] / entry["output"]
            documents.append(document)
        return documents

    def workflows(self):
        grouped = {}
        for document in self.documents:
            grouped.setdefault(document["workflow"], []).append(document)
        return grouped

    def select(self, workflow, variant=None):
        if not workflow:
            raise DiagramError("a workflow is required (try --list)")
        matches = [document for document in self.documents if document["workflow"] == workflow]
        if not matches:
            known = ", ".join(sorted(self.workflows()))
            raise DiagramError(f"unknown workflow {workflow!r}; known workflows: {known}")
        by_variant = {document["variant"]: document for document in matches}
        if variant:
            if variant not in by_variant:
                available = ", ".join(sorted(by_variant))
                raise DiagramError(f"{workflow} has no {variant!r} variant; available: {available}")
            return by_variant[variant], False
        if len(by_variant) == 1:
            return next(iter(by_variant.values())), False
        chosen = by_variant.get("engineer") or sorted(by_variant)[0]
        return by_variant[chosen["variant"]], True

    def known_outputs(self):
        return {document["output"] for document in self.documents}

    def summary(self, document):
        sheets = node_total = None
        spec_present = document["spec_path"].is_file()
        if spec_present:
            try:
                spec = json.loads(document["spec_path"].read_text(encoding="utf-8"))
                sheets = len(spec.get("sheets") or [])
                node_total = sum(len(nodes) for sheet in spec.get("sheets") or []
                                 for nodes in sheet.get("lanes", {}).values())
            except (json.JSONDecodeError, OSError):
                sheets = None
        dots = 0
        if spec_present and sheets:
            dots = sum(1 for name in self._dot_names(document)
                   if (self.dot_folder(document) / name).is_file())
        return {
            "spec": spec_present,
            "sheets": sheets,
            "nodes": node_total,
            "dots": dots,
            "pdf": document["pdf_path"].is_file(),
        }

    def dot_folder(self, document):
        """Each workflow's Graphviz sources live in their own folder: dot/<workflow>/."""
        return self.dot_dir / document["workflow"]

    def _dot_names(self, document):
        if not document["spec_path"].is_file():
            return []
        try:
            spec = json.loads(document["spec_path"].read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            return []
        return [sheet.get("file", "") + ".dot" for sheet in spec.get("sheets") or []]


def connector(label):
    text = clean(label)
    match = CONNECTOR.match(text)
    if match and match.group("kind").strip().lower() in ("to", "from"):
        return {"kind": match.group("kind").strip().lower(),
                "target": int(match.group("sheet")),
                "text": match.group("rest").strip()}
    reference = DOCUMENT_REF.search(text)
    if reference:
        return {"kind": "document", "target": int(reference.group(1)), "text": text}
    return {"kind": "other", "target": None, "text": text}


def sheet_digest(index, total, raw):
    nodes = []
    for lane, lane_nodes in (raw.get("lanes") or {}).items():
        for node in lane_nodes:
            node_id, label, kind = (list(node) + ["", ""])[:3]
            nodes.append({"lane": lane, "id": node_id, "label": clean(label), "kind": kind})
    edges = []
    for edge in raw.get("edges") or []:
        source, target, label, style = (list(edge) + ["", ""])[:4]
        edges.append({"from": source, "to": target, "label": clean(label) if label else "",
                      "style": style or ""})
    return {
        "index": index,
        "total": total,
        "file": raw.get("file", ""),
        "title": clean(raw.get("title", "")),
        "source": raw.get("source", ""),
        "lane_titles": raw.get("lane_titles") or {},
        "nodes": nodes,
        "edges": edges,
        "dot": {"name": raw.get("file", "") + ".dot", "present": False, "agrees": None},
        "findings": [node for node in nodes if node["kind"] == "refactor"],
        "connectors": [],
    }


def check_dot_sources(sheets, dot_dir):
    warnings = []
    if dot_dir is None:
        return warnings
    for sheet in sheets:
        path = dot_dir / sheet["dot"]["name"]
        if not sheet["file"] or not path.is_file():
            sheet["dot"]["present"] = False
            warnings.append(f"sheet {sheet['index']}: {sheet['dot']['name']} is missing — "
                            "regenerate it with dotgen.py; the spec is still readable")
            continue
        sheet["dot"]["present"] = True
        text = path.read_text(encoding="utf-8")
        expected = {node["id"] for node in sheet["nodes"]}
        found = set(DOT_NODE.findall(text))
        missing, extra = sorted(expected - found), sorted(found - expected)
        sheet["dot"]["agrees"] = not missing and not extra
        if missing or extra:
            warnings.append(
                f"sheet {sheet['index']}: the .dot file disagrees with the spec "
                f"(missing {missing or 'none'}, extra {extra or 'none'}) — the spec is authoritative; "
                "rebuild the .dot file")
    return warnings


def cross_sheet(sheets):
    warnings, links = [], []
    badges = []
    for sheet in sheets:
        for node in sheet["nodes"]:
            if node["kind"] != "conn":
                continue
            parsed = connector(node["label"])
            parsed.update({"sheet": sheet["index"], "id": node["id"]})
            badges.append(parsed)
    for badge in badges:
        if badge["kind"] in ("to", "from"):
            target = badge["target"]
            if not 1 <= target <= len(sheets):
                warnings.append(
                    f"sheet {badge['sheet']}: connector {badge['text'] or '(unlabelled)'} points at "
                    f"sheet {target}, which does not exist ({len(sheets)} sheets)")
                links.append({**badge, "status": "missing sheet"})
                continue
            wanted = "from" if badge["kind"] == "to" else "to"
            partner = next((other for other in badges
                            if other["kind"] == wanted and other["sheet"] == target
                            and other["target"] == badge["sheet"]
                            and other["text"].casefold() == badge["text"].casefold()), None)
            links.append({**badge, "status": "paired" if partner else "one-way"})
        elif badge["kind"] == "document":
            links.append({**badge, "status": "cross-document"})
        else:
            links.append({**badge, "status": "unparsed"})
    for sheet in sheets:
        sheet["connectors"] = [link for link in links if link["sheet"] == sheet["index"]]
    return links, warnings


def pdf_status(document, known_outputs):
    warnings, extra = [], []
    if not document["pdf_path"].is_file():
        warnings.append(f"PDF missing: {document['output']} — no visual reference; "
                        "rebuild it with render_sheets.py")
    stem = document["output"][:-4] if document["output"].endswith(".pdf") else document["output"]
    if document["pdf_path"].parent.is_dir():
        for candidate in sorted(document["pdf_path"].parent.glob(stem + "*.pdf")):
            if candidate.name != document["output"] and candidate.name not in known_outputs:
                extra.append(candidate.name)
    if extra:
        warnings.append(f"possible other version(s) of {document['output']}: {', '.join(extra)} — "
                        "confirm which file the reader should use before relying on either")
    return {"name": document["output"], "present": document["pdf_path"].is_file(),
            "versions": extra, "pages": None}, warnings


def inspect_pdf(path, max_text=700):
    try:
        import pymupdf as fitz
    except ImportError:
        return {"available": False,
                "reason": "PyMuPDF is not installed (pip install pymupdf), so the PDF was not read"}
    pages = []
    with fitz.open(path) as pdf:
        for number, page in enumerate(pdf, 1):
            spans = [span["text"].strip()
                     for block in page.get_text("dict")["blocks"]
                     for line in block.get("lines", [])
                     for span in line["spans"]]
            spans = [span for span in spans if span]
            text = " · ".join(spans)
            pages.append({"page": number, "spans": len(spans), "text": text[:max_text],
                          "truncated": len(text) > max_text})
    return {"available": True, "pages": pages}


def write_pngs(path, out_dir=PNG_DIR, dpi=150):
    import pymupdf as fitz
    out_dir.mkdir(parents=True, exist_ok=True)
    written = []
    with fitz.open(path) as pdf:
        for number, page in enumerate(pdf, 1):
            target = out_dir / f"{path.stem}-p{number}.png"
            page.get_pixmap(dpi=dpi).save(str(target))
            written.append(str(target))
    return written


def build(document, sheet=None, inspect=False, png=False, dot_dir=None,
          known_outputs=None, adhoc_dot=None, max_text=700):
    warnings = []
    spec_present = document["spec_path"].is_file()
    sheets = []
    if spec_present:
        try:
            spec = json.loads(document["spec_path"].read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise DiagramError(f"{document['spec']} is not valid JSON: {error}") from error
        raw_sheets = spec.get("sheets") or []
        if not raw_sheets:
            raise DiagramError(f"{document['spec']} describes no sheets")
        sheets = [sheet_digest(index, len(raw_sheets), raw)
                  for index, raw in enumerate(raw_sheets, 1)]
    else:
        warnings.append(f"spec missing: {document['spec']} — the editable graph structure is "
                        "unavailable; only the PDF can be described")

    if adhoc_dot is not None:
        if len(sheets) > 1:
            warnings.append("an ad-hoc .dot file was supplied, but sheet-by-sheet pairing needs the "
                            "dot/ folder convention; the file was not matched to a sheet")
        elif sheets:
            sheets[0]["dot"]["name"] = pathlib.Path(adhoc_dot).name
            dot_dir = pathlib.Path(adhoc_dot).parent
    warnings.extend(check_dot_sources(sheets, dot_dir if sheets else None))

    links, link_warnings = cross_sheet(sheets)
    warnings.extend(link_warnings)

    pdf, pdf_warnings = pdf_status(document, known_outputs or set())
    warnings.extend(pdf_warnings)
    png_files = []
    if inspect:
        if not pdf["present"]:
            warnings.append("PDF content was not inspected because the PDF is missing")
        else:
            pdf["content"] = inspect_pdf(document["pdf_path"], max_text)
            if not pdf["content"].get("available"):
                warnings.append(pdf["content"]["reason"])
            if png and pdf["content"].get("available"):
                png_files = write_pngs(document["pdf_path"])

    chosen = sheets
    if sheet is not None:
        if not sheets:
            raise DiagramError("no spec is available, so a single sheet cannot be selected")
        if not 1 <= sheet <= len(sheets):
            raise DiagramError(f"sheet {sheet} does not exist; {document['spec']} has {len(sheets)}")
        chosen = [sheets[sheet - 1]]

    return {
        "workflow": document["workflow"],
        "variant": document["variant"],
        "title": document.get("title") or document["workflow"],
        "sources": {"spec": {"name": document["spec"], "present": spec_present},
                    "dot": {"directory": str(dot_dir), "matched": sum(1 for s in sheets
                                                                      if s["dot"]["present"]),
                            "of": len(sheets)},
                    "pdf": pdf},
        "sheets": chosen,
        "cross_sheet": links,
        "warnings": warnings,
        "notes": [],
        "png": png_files,
    }


def render_markdown(digest):
    lines = [f"# {digest['title']} — workflow digest", ""]
    lines.append(f"Workflow `{digest['workflow']}` · variant `{digest['variant']}` · "
                 f"{len(digest['sheets'])} sheet(s) shown")
    lines.append("")
    lines.append("A diagram records current behaviour. It is a reference for a change, not "
                 "authorisation to change that behaviour or its business rules.")
    lines.append("")
    for note in digest.get("notes", []):
        lines.append(f"Note: {note}")
        lines.append("")
    lines.append("## Sources")
    lines.append("")
    lines.append("| Artifact | Location | Status |")
    lines.append("| --- | --- | --- |")
    sources = digest["sources"]
    lines.append(f"| JSON spec (authoritative) | `{cell(sources['spec']['name'])}` | "
                 f"{'present' if sources['spec']['present'] else 'missing'} |")
    lines.append(f"| Graphviz `.dot` | `dot/` | {sources['dot']['matched']} of "
                 f"{sources['dot']['of']} sheets present |")
    lines.append(f"| PDF (visual) | `pdf/{cell(sources['pdf']['name'])}` | "
                 f"{'present' if sources['pdf']['present'] else 'missing'} |")
    if sources["pdf"]["versions"]:
        lines.append("")
        lines.append("Other files that may be another version: "
                     + ", ".join(f"`{cell(name)}`" for name in sources["pdf"]["versions"]))
    lines.append("")

    for sheet in digest["sheets"]:
        lines.append(f"## Sheet {sheet['index']} of {sheet['total']} — {sheet['title'] or sheet['file']}")
        lines.append("")
        if sheet["source"]:
            lines.append(f"Spec notes: {sheet['source']}")
            lines.append("")
        lanes = [lane for lane in LANES if any(node["lane"] == lane for node in sheet["nodes"])]
        lanes += sorted({node["lane"] for node in sheet["nodes"]} - set(lanes))
        for number, lane in enumerate(lanes, 1):
            title = sheet["lane_titles"].get(lane, LANE_FALLBACK.get(lane, lane))
            lines.append(f"{number}. {title}")
        lines.append("")
        lines.append("| Lane | Id | Kind | Label |")
        lines.append("| --- | --- | --- | --- |")
        for node in sheet["nodes"]:
            lines.append(f"| {cell(node['lane'])} | `{cell(node['id'])}` | {cell(node['kind'])} | "
                         f"{cell(node['label'])} |")
        lines.append("")
        if sheet["edges"]:
            lines.append("| From | To | Label | Style |")
            lines.append("| --- | --- | --- | --- |")
            for edge in sheet["edges"]:
                lines.append(f"| `{cell(edge['from'])}` | `{cell(edge['to'])}` | "
                             f"{cell(edge['label'])} | {cell(edge['style']) or 'plain'} |")
            lines.append("")
        if sheet["findings"]:
            lines.append("Findings flagged `[REFACTOR]`:")
            lines.append("")
            for finding in sheet["findings"]:
                lines.append(f"- `{cell(finding['id'])}` — {cell(finding['label'])}")
            lines.append("")
        if sheet["connectors"]:
            lines.append("Connectors:")
            lines.append("")
            for link in sheet["connectors"]:
                target = link["target"] if link["target"] is not None else "?"
                lines.append(f"- `{cell(link['id'])}` — {link['kind']} {target}: "
                             f"{cell(link['text']) or '(no label)'} ({link['status']})")
            lines.append("")

    pdf = digest["sources"]["pdf"]
    if pdf.get("content"):
        lines.append("## PDF pages")
        lines.append("")
        if len(digest["sheets"]) < (digest["sheets"][0]["total"] if digest["sheets"] else 1):
            lines.append("These pages cover the whole document; the sheet filter above does not "
                         "limit them.")
            lines.append("")
        if not pdf["content"].get("available"):
            lines.append(f"Not read: {pdf['content']['reason']}")
        else:
            lines.append("| Page | Spans | Text |")
            lines.append("| --- | --- | --- |")
            for page in pdf["content"]["pages"]:
                more = " …" if page["truncated"] else ""
                lines.append(f"| {page['page']} | {page['spans']} | {cell(page['text'])}{more} |")
        lines.append("")
    if digest["png"]:
        lines.append("Page images for visual inspection:")
        lines.append("")
        for path in digest["png"]:
            lines.append(f"- `{path}`")
        lines.append("")

    lines.append("## Warnings")
    lines.append("")
    if digest["warnings"]:
        for warning in digest["warnings"]:
            lines.append(f"- {warning}")
    else:
        lines.append("- None. Every source this digest names was read.")
    return "\n".join(lines) + "\n"


def render_list(diagrams, as_json=False):
    rows = []
    for workflow, documents in sorted(diagrams.workflows().items()):
        for document in sorted(documents, key=lambda item: item["variant"]):
            rows.append({"workflow": workflow, "variant": document["variant"],
                         "title": document.get("title") or workflow,
                         **diagrams.summary(document)})
    if as_json:
        return json.dumps({"workflows": rows}, indent=1, ensure_ascii=False) + "\n"
    lines = ["| Workflow | Variant | Sheets | Nodes | `.dot` | PDF | Title |",
             "| --- | --- | --- | --- | --- | --- | --- |"]
    for row in rows:
        sheets = row["sheets"] if row["sheets"] is not None else "?"
        dots = f"{row['dots']}/{row['sheets']}" if row["sheets"] else "?"
        lines.append(f"| `{row['workflow']}` | {row['variant']} | {sheets} | "
                     f"{row['nodes'] if row['nodes'] is not None else '?'} | {dots} | "
                     f"{'present' if row['pdf'] else 'missing'} | {cell(row['title'])} |")
    lines.append("")
    lines.append("Name a workflow on the next run: `--workflow <name>` "
                 "(`--variant friendly` for the plain-language version).")
    return "\n".join(lines) + "\n"


def main(argv=None):
    utf8_stdout()
    parser = argparse.ArgumentParser(
        description="Print a read-only digest of a workflow's diagrams.")
    parser.add_argument("--list", action="store_true", help="list the workflows in the manifest")
    parser.add_argument("--workflow", help="workflow name, as listed by --list")
    parser.add_argument("--variant", choices=["engineer", "friendly"])
    parser.add_argument("--sheet", type=int, help="show only this sheet (1-based)")
    parser.add_argument("--spec", help="ad-hoc spec file, for diagrams outside the manifest")
    parser.add_argument("--dot", help="ad-hoc Graphviz file (single-sheet specs only)")
    parser.add_argument("--pdf-file", help="ad-hoc PDF file")
    parser.add_argument("--label", default="ad-hoc workflow", help="name for an ad-hoc digest")
    parser.add_argument("--inspect-pdf", action="store_true", help="read the PDF's text per page")
    parser.add_argument("--png", action="store_true",
                        help="write page images to the temp folder (implies --inspect-pdf)")
    parser.add_argument("--format", choices=["md", "json"], default="md")
    parser.add_argument("--strict", action="store_true", help="exit 1 when the digest warns")
    parser.add_argument("--root", default=None, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    try:
        diagrams = DiagramSet(args.root or DIAGRAMS)
        if args.list:
            sys.stdout.write(render_list(diagrams, as_json=args.format == "json"))
            return 0

        if args.spec or args.pdf_file or args.dot:
            spec = pathlib.Path(args.spec) if args.spec else pathlib.Path("<none>")
            pdf = pathlib.Path(args.pdf_file) if args.pdf_file else pathlib.Path("<none>")
            document = {
                "workflow": args.label, "variant": "ad-hoc", "title": args.label,
                "spec": spec.name, "spec_path": spec,
                "output": pdf.name, "pdf_path": pdf,
            }
            digest = build(document, sheet=args.sheet, inspect=args.inspect_pdf or args.png,
                           png=args.png, dot_dir=None,
                           known_outputs=diagrams.known_outputs(),
                           adhoc_dot=pathlib.Path(args.dot) if args.dot else None)
        else:
            document, defaulted = diagrams.select(args.workflow, args.variant)
            digest = build(document, sheet=args.sheet, inspect=args.inspect_pdf or args.png,
                           png=args.png,
                           dot_dir=diagrams.dot_folder(document),
                           known_outputs=diagrams.known_outputs())
            if defaulted:
                digest.setdefault("notes", []).append(
                    f"no variant given, so the {document['variant']} version was used; "
                    "pass --variant friendly for the plain-language version")
    except DiagramError as error:
        sys.stderr.write(f"error: {error}\n")
        return 2

    if args.format == "json":
        sys.stdout.write(json.dumps(digest, indent=1, ensure_ascii=False) + "\n")
    else:
        sys.stdout.write(render_markdown(digest))
    if args.strict and digest["warnings"]:
        sys.stderr.write(f"{len(digest['warnings'])} warning(s)\n")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
