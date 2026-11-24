#!/usr/bin/env python3
"""Render a UI design's mockups to PNG with headless Microsoft Edge.

A design is one JSON record plus one HTML mockup per variant. This script renders every declared
state at every declared window size, writes the canonical `-before.png`, `-after.png` and
`-comparison.png`, and records what it read and produced in `<-refactor>-render.json`.

    python docs\\ui-designs\\tools\\render_mockup.py --design docs\\ui-designs\\<workflow>\\<screen>\\<refactor>-design.json
    python docs\\ui-designs\\tools\\render_mockup.py --design ... --approve --by "John Koll"

The approval block is written into the design record and covers the mockup sources, the shared
mockup assets and the design-system files the tokens come from. Any later change to those sources
makes `verify_designs.py` fail until the design is re-rendered and re-approved.

Edge is the only rendering dependency and ships with Windows. Set `MTM_EDGE` to override its path.
"""
import argparse
import datetime
import hashlib
import json
import os
import pathlib
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import time

TOOLS = pathlib.Path(__file__).resolve().parent
REPO = TOOLS.parent.parent.parent
LINK = re.compile(r"""(?:href|src)\s*=\s*["']([^"']+)["']""", re.IGNORECASE)
CSS_IMPORT = re.compile(r"""@import\s+(?:url\()?["']([^"']+)["']""", re.IGNORECASE)
REQUIRED_VIEWPORTS = [("wide", 1600, 1000), ("medium", 1280, 800), ("minimum", 900, 500)]
DEFAULT_STATE = "default"
DEFAULT_VIEWPORT = "wide"
STAGE_VIEWPORT = "stage"

# A run folder holds its sources, its renders and its records in three subfolders, and keeps only the
# proposal and the comparison sheets at its own level.
MOCKUPS_DIR = "mockups"
PNGS_DIR = "pngs"
DATA_DIR = "data"

# The comparison sheet's PDF: US Letter landscape, the image at the top and selectable text below it.
PDF_PAGE_W, PDF_PAGE_H, PDF_MARGIN = 792.0, 612.0, 36.0
PDF_FONT = pathlib.Path(r"C:\Windows\Fonts\segoeui.ttf")
PDF_FONT_BOLD = pathlib.Path(r"C:\Windows\Fonts\segoeuib.ttf")
PDF_TEXT_LEFT = 10.5
PDF_HEADING_SIZE = 9.5
PDF_NOTE_SIZE = 9.5
PDF_MIN_TEXT = 9.4


def run_dir(design_path):
    """The run's own folder, whether the record sits in it or in its `data/` subfolder."""
    path = pathlib.Path(design_path).resolve()
    return path.parent.parent if path.parent.name == DATA_DIR else path.parent


def ensure(path):
    path = pathlib.Path(path)
    path.mkdir(parents=True, exist_ok=True)
    return path


def window_kind(design):
    return (design.get("window") or {}).get("kind", "shell")


def canonical_viewport(design):
    """The viewport the canonical images and the comparison are drawn from."""
    return STAGE_VIEWPORT if window_kind(design) == "stage" else DEFAULT_VIEWPORT


class RenderError(Exception):
    """The design cannot be rendered as declared."""


def utf8_stdout():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def sha256_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha256_file(path):
    return sha256_bytes(pathlib.Path(path).read_bytes())


def combined_hash(entries):
    """Hash a {relative path: sha256} map in a stable order."""
    digest = hashlib.sha256()
    for name in sorted(entries):
        digest.update(name.replace("\\", "/").encode("utf-8"))
        digest.update(entries[name].encode("ascii"))
    return digest.hexdigest()


def png_size(path):
    """Read a PNG's pixel size from its IHDR chunk, without a decoding dependency."""
    with open(path, "rb") as handle:
        header = handle.read(24)
    if len(header) < 24 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise RenderError(f"{path} is not a PNG")
    return struct.unpack(">II", header[16:24])


def find_edge(explicit=None):
    if explicit:
        path = pathlib.Path(explicit)
        if path.is_file():
            return path
        raise RenderError(f"Edge not found at {explicit}")
    if os.environ.get("MTM_EDGE"):
        path = pathlib.Path(os.environ["MTM_EDGE"])
        if path.is_file():
            return path
        raise RenderError(f"MTM_EDGE points at {path}, which does not exist")
    candidates = [
        pathlib.Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
        pathlib.Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
    ]
    for candidate in candidates:
        if candidate.is_file():
            return candidate
    raise RenderError("Microsoft Edge was not found; set MTM_EDGE to msedge.exe")


def load_design(path):
    path = pathlib.Path(path)
    if not path.is_file():
        raise RenderError(f"design record not found: {path}")
    try:
        design = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise RenderError(f"{path.name} is not valid JSON: {error}") from error
    for key in ("workflow", "screen", "refactor", "window", "before", "after", "states"):
        if key not in design:
            raise RenderError(f"{path.name} is missing '{key}'")
    return path, design


def viewports(design):
    declared = (design.get("window") or {}).get("viewports") or []
    if not declared:
        raise RenderError("the design declares no viewports")
    out = []
    for entry in declared:
        try:
            out.append({"name": entry["name"], "width": int(entry["width"]),
                        "height": int(entry["height"]), "scale": float(entry.get("scale", 1))})
        except (KeyError, TypeError, ValueError) as error:
            raise RenderError(f"a viewport entry is incomplete: {entry}") from error
    return out


def mockups(design):
    """Return [(variant, mockup path)] for the variants that have an HTML source."""
    found = []
    after = (design.get("after") or {}).get("mockup")
    if not after:
        raise RenderError("the design declares no AFTER mockup")
    found.append(("after", pathlib.Path(after)))
    before = design.get("before") or {}
    if before.get("kind") == "reconstruction":
        if not before.get("mockup"):
            raise RenderError("a reconstructed BEFORE needs a mockup")
        found.append(("before", pathlib.Path(before["mockup"])))
    elif before.get("kind") == "capture":
        if before.get("mockup"):
            raise RenderError("a captured BEFORE has no mockup; the screenshot is the source")
    else:
        raise RenderError("before.kind must be 'capture' or 'reconstruction'")
    return found


def asset_files(html_path, design_dir):
    """The mockup and every local file it links to, one level into CSS imports."""
    seen, queue, missing = set(), [html_path], []
    while queue:
        current = queue.pop()
        resolved = (design_dir / current) if not current.is_absolute() else current
        if resolved in seen or not resolved.is_file():
            continue
        seen.add(resolved)
        if resolved.suffix.lower() not in (".html", ".htm", ".css"):
            continue
        text = resolved.read_text(encoding="utf-8", errors="replace")
        for reference in LINK.findall(text) + CSS_IMPORT.findall(text):
            if reference.startswith(("http:", "https:", "data:", "#", "mailto:")):
                continue
            target = (resolved.parent / reference.split("#")[0]).resolve()
            if target.is_file():
                seen.add(target)
                if target.suffix.lower() == ".css":
                    queue.append(target)
            else:
                missing.append(f"{resolved.name} links to {reference}")
    if missing:
        raise RenderError("a mockup references a file that does not exist: "
                          + "; ".join(sorted(set(missing))))
    return sorted(seen)


def source_snapshot(design, design_dir):
    files = []
    for _, mockup in mockups(design):
        files.extend(asset_files(mockup, design_dir))
    entries = {}
    for path in sorted(set(files)):
        try:
            name = path.relative_to(REPO).as_posix()
        except ValueError:
            name = path.as_posix()
        entries[name] = sha256_file(path)
    return entries


def design_system_snapshot(design):
    declared = (design.get("design_system") or {}).get("resources") or []
    entries = {}
    for name in declared:
        path = REPO / name
        if not path.is_file():
            raise RenderError(f"design-system resource not found: {name}")
        entries[name] = sha256_file(path)
    if not entries:
        raise RenderError("design_system.resources is empty; name the XAML the tokens come from")
    return entries


def render_page(edge, html_path, output, width, height, scale, state, work_dir):
    # `default` is the page with nothing targeted; every other state is the mockup's anchor id.
    fragment = "" if state == DEFAULT_STATE else f"#{state}"
    url = pathlib.Path(html_path).resolve().as_uri() + fragment
    output = pathlib.Path(output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        output.unlink()
    profile = pathlib.Path(tempfile.mkdtemp(prefix="mtm-mockup-", dir=work_dir))
    command = [
        str(edge), "--headless=new", "--disable-gpu", "--hide-scrollbars",
        "--no-first-run", "--no-default-browser-check", "--disable-extensions",
        "--disable-crash-reporter", "--disable-breakpad", "--disable-background-networking",
        f"--user-data-dir={profile}",
        f"--window-size={int(width * scale)},{int(height * scale)}",
        f"--force-device-scale-factor={scale}",
        f"--screenshot={output}", url,
    ]
    # Edge writes the image and then keeps its browser process alive, so wait for the file and stop it.
    process = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    deadline = time.time() + 60
    settled = False
    try:
        while time.time() < deadline:
            if output.is_file() and output.stat().st_size > 0:
                size = output.stat().st_size
                time.sleep(0.25)
                if output.stat().st_size == size:
                    settled = True
                    break
            elif process.poll() is not None:
                break
            time.sleep(0.2)
    finally:
        if process.poll() is None:
            process.kill()
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            pass
        shutil.rmtree(profile, ignore_errors=True)
    if not settled:
        raise RenderError(f"Edge produced no image for {html_path.name} ({state}, "
                          f"{int(width * scale)}x{int(height * scale)}) within 60 s")
    actual = png_size(output)
    expected = (int(width * scale), int(height * scale))
    if actual[0] < expected[0] or actual[1] < expected[1]:
        raise RenderError(f"{output.name} is {actual[0]}x{actual[1]}, smaller than the declared "
                          f"{expected[0]}x{expected[1]}")
    return actual


def state_names(design, variant):
    names = []
    for state in design.get("states") or []:
        name = state.get("name")
        applies = state.get("applies_to") or ["before", "after"]
        if not name:
            raise RenderError("a state has no name")
        if not re.fullmatch(r"[a-z0-9][a-z0-9-]*", name):
            raise RenderError(f"state name {name!r} must be lower-case, digits and dashes")
        if name != DEFAULT_STATE and not name.startswith("state-"):
            raise RenderError(
                f"state '{name}' must be the mockup's anchor id, so it begins 'state-' "
                f"(for example 'state-error'). 'default' is the page with nothing targeted.")
        if variant in applies:
            names.append(name)
    if not names:
        raise RenderError(f"no state applies to the {variant} mockup")
    if DEFAULT_STATE not in names:
        raise RenderError(f"the {variant} mockup needs a '{DEFAULT_STATE}' state")
    return names


def state_label(state):
    """The caption a comparison row carries for one state."""
    if state == DEFAULT_STATE:
        return "Default state"
    words = state[len("state-"):] if state.startswith("state-") else state
    return words.replace("-", " ").capitalize() + " state"


def comparison_pairs(design):
    """The BEFORE/AFTER pairs this design has, one row each.

    Every state both variants render is a pair of its own: a design that shows an error state beside its
    default has two before/after groupings, and the comparison draws both. A captured BEFORE has no
    renderable states, so it is compared on the default state alone.
    """
    refactor = design["refactor"]
    viewport = canonical_viewport(design)
    before = design.get("before") or {}
    states = state_names(design, "after")
    if before.get("kind") == "reconstruction":
        rendered_before = set(state_names(design, "before"))
        states = [name for name in states if name in rendered_before]
    else:
        states = [DEFAULT_STATE]
    if DEFAULT_STATE not in states:
        states.insert(0, DEFAULT_STATE)
    return [(state,
             f"{refactor}-before-{state}-{viewport}.png",
             f"{refactor}-after-{state}-{viewport}.png")
            for state in states]


def comparison_page(design, rows, wide, scale, css_href):
    """One sheet: a header, one BEFORE/AFTER row per state, then the callouts."""
    refactor = design["refactor"]
    width = int(wide["width"] * scale)
    height = int(wide["height"] * scale)
    blocks = []
    for state, before_name, after_name in rows:
        blocks.append(f"""  <div class="cmp-block">
    <div class="cmp-caption">{state_label(state)}</div>
    <div class="cmp-row">
      <img src="../{before_name}" width="{width}" height="{height}" alt="BEFORE, current implementation, {state_label(state)}">
      <img src="../{after_name}" width="{width}" height="{height}" alt="AFTER, proposed improvement, {state_label(state)}">
    </div>
  </div>""")
    callouts = "\n".join(f"      <li>{text}</li>" for text in design.get("callouts") or [])
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>{design.get('title') or refactor} — BEFORE and AFTER</title>
  <link rel="stylesheet" href="{css_href}">
  <style>
    .cmp-head {{ display: flex; gap: 24px; padding: 16px 24px 8px; font-family: 'Segoe UI', sans-serif; font-size: 13px; font-weight: 700; }}
    .cmp-head div {{ width: {width}px; }}
    .cmp-block {{ padding: 0 24px 28px; font-family: 'Segoe UI', sans-serif; }}
    .cmp-caption {{ font-size: 12px; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; color: #5d5d5d; padding-bottom: 6px; }}
    .cmp-row {{ display: flex; gap: 24px; align-items: flex-start; }}
    .cmp-row img {{ display: block; border: 1px solid #d0d0d0; }}
  </style>
</head>
<body style="background:#ffffff;">
  <div style="padding:24px 24px 0;font-family:'Segoe UI',sans-serif;">
    <div style="font-size:20px;font-weight:700;color:#1b1b1b;">{design.get('title') or refactor}</div>
    <div style="font-size:13px;color:#5d5d5d;">{design['workflow']} · {design['screen']} · viewport {wide['width']}×{wide['height']} · {len(rows)} before/after grouping(s)</div>
  </div>
  <div class="cmp-head">
    <div>BEFORE — CURRENT IMPLEMENTATION</div>
    <div>AFTER — PROPOSED QOL IMPROVEMENT</div>
  </div>
{chr(10).join(blocks)}
  <div style="padding:0 24px 24px;font-family:'Segoe UI',sans-serif;">
    <div style="font-weight:700;font-size:14px;margin-bottom:8px;">What changed</div>
    <ol class="callout-list">
{callouts}
    </ol>
    <div style="margin-top:12px;font-size:12px;color:#5d5d5d;">
      The BEFORE image is a {design['before'].get('kind')}. Rendered {len(design.get('states') or [])} state(s) at {len(design['window']['viewports'])} window size(s); this sheet compares {len(rows)} of them.
    </div>
  </div>
</body>
</html>
"""


def comparison_height(rows, image_height, callouts):
    """The page height a comparison needs, so Edge's viewport does not clip it."""
    per_block = image_height + 52
    footer = 60 + 20 * len(callouts)
    return 126 + per_block * len(rows) + footer + 40


def pdf_document(path):
    """Open a PDF with PyMuPDF, or say what is missing."""
    try:
        import pymupdf as fitz
    except ImportError as error:  # pragma: no cover - the package is present on this machine
        raise RenderError("the comparison PDF needs PyMuPDF: pip install pymupdf") from error
    return fitz.open(str(path))


def wrap_lines(text, measure, size, width):
    """Break text into lines that fit a width, measured with the font it is drawn in."""
    lines, current = [], ""
    for word in str(text).split():
        trial = f"{current} {word}".strip()
        if not current or measure(trial, size) <= width:
            current = trial
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def write_comparison_pdf(output, pages):
    """One landscape page per before/after grouping: the two images on top, the change in text below.

    The text is drawn as text, not baked into the image, so a reader can search it, copy it and quote
    it, and a checker can read it back with `page.get_text()`.
    """
    if not PDF_FONT.is_file() or not PDF_FONT_BOLD.is_file():
        raise RenderError("the comparison PDF needs the Segoe UI face in C:\\Windows\\Fonts")
    try:
        import pymupdf
    except ImportError as error:  # pragma: no cover
        raise RenderError("the comparison PDF needs PyMuPDF: pip install pymupdf") from error

    face = pymupdf.Font(fontfile=str(PDF_FONT))
    measure = face.text_length
    content_width = PDF_PAGE_W - 2 * PDF_MARGIN

    document = pymupdf.open()
    for entry in pages:
        page = document.new_page(width=PDF_PAGE_W, height=PDF_PAGE_H)
        page.insert_text((PDF_MARGIN, PDF_MARGIN + 10), entry["heading"], fontsize=14,
                         fontname="SegoeUI", fontfile=str(PDF_FONT_BOLD),
                         color=(0.106, 0.106, 0.106))
        page.insert_text((PDF_MARGIN, PDF_MARGIN + 26), entry["subheading"],
                         fontsize=PDF_HEADING_SIZE,
                         fontname="SegoeUI", fontfile=str(PDF_FONT),
                         color=(0.365, 0.365, 0.365))
        page.draw_line(pymupdf.Point(PDF_MARGIN, PDF_MARGIN + 34),
                       pymupdf.Point(PDF_PAGE_W - PDF_MARGIN, PDF_MARGIN + 34),
                       color=(0.72, 0.72, 0.72), width=0.6)

        items = []
        for number, change in enumerate(entry.get("changes") or [], 1):
            items.extend(wrap_lines(f"{number}. {change}", measure, PDF_TEXT_LEFT, content_width))
        note_lines = wrap_lines(entry.get("note") or "", measure, PDF_NOTE_SIZE, content_width)
        block = 18 + 14 * len(items) + (10 + 12 * len(note_lines) if note_lines else 0)
        text_top = PDF_PAGE_H - PDF_MARGIN - block

        image_top = PDF_MARGIN + 48
        image_bottom = text_top - 14
        label_h = 14
        available_h = image_bottom - image_top - label_h
        available_w = (content_width - 16) / 2

        placed = []
        for path in (entry["before"], entry["after"]):
            width, height = png_size(path)
            scale = min(available_w / width, available_h / height)
            placed.append((path, width * scale, height * scale))
        total = placed[0][1] + placed[1][1] + 16
        x = PDF_MARGIN + max(0.0, (content_width - total) / 2)
        labels = ("BEFORE — CURRENT IMPLEMENTATION", "AFTER — PROPOSED QOL IMPROVEMENT")
        for index, (path, draw_w, draw_h) in enumerate(placed):
            page.insert_text((x, image_top), labels[index], fontsize=PDF_HEADING_SIZE,
                             fontname="SegoeUI", fontfile=str(PDF_FONT_BOLD),
                             color=(0.365, 0.365, 0.365))
            top = image_top + label_h
            page.insert_image(pymupdf.Rect(x, top, x + draw_w, top + draw_h), filename=str(path))
            page.draw_rect(pymupdf.Rect(x, top, x + draw_w, top + draw_h),
                           color=(0.82, 0.82, 0.82), width=0.6)
            x += draw_w + 16

        y = text_top + 10
        page.insert_text((PDF_MARGIN, y), "What changes", fontsize=11,
                         fontname="SegoeUI", fontfile=str(PDF_FONT_BOLD),
                         color=(0.106, 0.106, 0.106))
        y += 16
        for line in items:
            page.insert_text((PDF_MARGIN, y), line, fontsize=PDF_TEXT_LEFT,
                             fontname="SegoeUI", fontfile=str(PDF_FONT),
                             color=(0.106, 0.106, 0.106))
            y += 14
        if note_lines:
            y += 8
            for line in note_lines:
                page.insert_text((PDF_MARGIN, y), line, fontsize=PDF_NOTE_SIZE,
                                 fontname="SegoeUI", fontfile=str(PDF_FONT),
                                 color=(0.365, 0.365, 0.365))
                y += 12

    output = pathlib.Path(output)
    ensure(output.parent)
    document.save(str(output), garbage=4, deflate=True)
    document.close()
    return output


def build(design_path, edge_path=None):
    design_path, design = load_design(design_path)
    run = run_dir(design_path)
    refactor = design["refactor"]
    edge = find_edge(edge_path)
    sizes = viewports(design)
    sources = source_snapshot(design, run)
    system = design_system_snapshot(design)
    pngs = ensure(run / PNGS_DIR)
    mockup_dir = ensure(run / MOCKUPS_DIR)
    data_dir = ensure(run / DATA_DIR)

    images = {}
    work_root = pathlib.Path(tempfile.mkdtemp(prefix="mtm-mockup-run-"))
    try:
        for variant, mockup in mockups(design):
            html_path = run / mockup
            if not html_path.is_file():
                raise RenderError(f"mockup not found: {html_path}")
            for viewport in sizes:
                for state in state_names(design, variant):
                    name = f"{refactor}-{variant}-{state}-{viewport['name']}.png"
                    size = render_page(edge, html_path, pngs / name, viewport["width"],
                                       viewport["height"], viewport["scale"], state, work_root)
                    images[f"{PNGS_DIR}/{name}"] = {"width": size[0], "height": size[1],
                                                    "sha256": sha256_file(pngs / name)}
                default_name = f"{refactor}-{variant}-{DEFAULT_STATE}-{viewport['name']}.png"
                default_hash = (images.get(f"{PNGS_DIR}/{default_name}") or {}).get("sha256")
                for state in state_names(design, variant):
                    if state == DEFAULT_STATE or not default_hash:
                        continue
                    entry = images[f"{PNGS_DIR}/{refactor}-{variant}-{state}-{viewport['name']}.png"]
                    if entry["sha256"] == default_hash:
                        raise RenderError(
                            f"state '{state}' rendered identically to '{DEFAULT_STATE}' for "
                            f"{variant} at {viewport['name']}; the mockup does not respond to it — "
                            f"check for an element with id '{state}' and a rule in mockup.css that "
                            "reveals its content")
            canonical = f"{refactor}-{variant}.png"
            name_of_canonical = canonical_viewport(design)
            wide_name = f"{refactor}-{variant}-{DEFAULT_STATE}-{name_of_canonical}.png"
            if f"{PNGS_DIR}/{wide_name}" not in images:
                raise RenderError(f"the {variant} mockup has no '{DEFAULT_STATE}' state at the "
                                  f"'{name_of_canonical}' viewport")
            shutil.copyfile(pngs / wide_name, pngs / canonical)
            size = png_size(pngs / canonical)
            images[f"{PNGS_DIR}/{canonical}"] = {"width": size[0], "height": size[1],
                                                 "sha256": sha256_file(pngs / canonical)}

        wide = next((entry for entry in sizes if entry["name"] == canonical_viewport(design)),
                    sizes[0])
        pairs = comparison_pairs(design)
        for _, before_name, after_name in pairs:
            for name in (before_name, after_name):
                if f"{PNGS_DIR}/{name}" not in images:
                    raise RenderError(f"the comparison needs {name}, which the render did not "
                                      "produce; the state must apply to both variants")
        compare_html = mockup_dir / f"{refactor}-compare.html"
        css_href = pathlib.PurePath(os.path.relpath(TOOLS / "mockup" / "mockup.css",
                                                    compare_html.parent)).as_posix()
        compare_html.write_text(comparison_page(design, pairs, wide, wide["scale"], css_href),
                                encoding="utf-8")
        compare_width = int(wide["width"] * wide["scale"] * 2 + 96)
        compare_height = comparison_height(pairs, int(wide["height"] * wide["scale"]),
                                           design.get("callouts") or [])
        compare_name = f"{refactor}-comparison.png"
        size = render_page(edge, compare_html, run / compare_name, compare_width,
                           compare_height, 1, DEFAULT_STATE, work_root)
        images[compare_name] = {"width": size[0], "height": size[1],
                                "sha256": sha256_file(run / compare_name),
                                "rows": len(pairs)}

        pdf_name = f"{refactor}-comparison.pdf"
        write_comparison_pdf(run / pdf_name, [
            {"heading": design.get("title") or refactor,
             "subheading": f"{design['workflow']} · {design['screen']} · viewport "
                           f"{wide['width']}×{wide['height']} · {state_label(state)}",
             "before": pngs / before_name,
             "after": pngs / after_name,
             "changes": design.get("callouts") or [],
             "note": f"The BEFORE image is a {design['before'].get('kind')}. This page compares the "
                     f"{state_label(state).lower()}."}
            for state, before_name, after_name in pairs
        ])
        images[pdf_name] = {"width": int(PDF_PAGE_W), "height": int(PDF_PAGE_H),
                            "sha256": sha256_file(run / pdf_name), "pages": len(pairs)}
    finally:
        shutil.rmtree(work_root, ignore_errors=True)

    record = {
        "renderer": "headless Microsoft Edge",
        "edge": str(edge),
        "rendered_at": datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
        "viewports": sizes,
        "states": design.get("states") or [],
        "sources": sources,
        "sources_sha256": combined_hash(sources),
        "design_system": system,
        "design_system_sha256": combined_hash(system),
        "images": dict(sorted(images.items())),
    }
    render_path = data_dir / f"{refactor}-render.json"
    render_path.write_text(json.dumps(record, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    return design_path, design, record, render_path


def workflow_comparison_page(workflow, rows, column, css_href):
    """One sheet for a whole workflow: a row per design, each the design's own BEFORE/AFTER pair."""
    blocks = []
    for row in rows:
        blocks.append(f"""  <div class="wf-block">
    <div class="wf-caption"><b>{row['screen']} · {row['refactor']}</b> — {row['title']}</div>
    <div class="wf-row">
      <img src="{row['before_href']}" width="{column}" height="{row['before_height']}" alt="BEFORE, {row['refactor']}">
      <img src="{row['after_href']}" width="{column}" height="{row['after_height']}" alt="AFTER, {row['refactor']}">
    </div>
  </div>""")
    drawn = sum(row["block_height"] for row in rows)
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>{workflow} — every BEFORE and AFTER</title>
  <link rel="stylesheet" href="{css_href}">
  <style>
    .wf-head {{ display: flex; gap: 24px; padding: 16px 24px 8px; font-family: 'Segoe UI', sans-serif; font-size: 13px; font-weight: 700; }}
    .wf-head div {{ width: {column}px; }}
    .wf-block {{ padding: 0 24px 28px; font-family: 'Segoe UI', sans-serif; }}
    .wf-caption {{ font-size: 13px; color: #1b1b1b; padding-bottom: 6px; }}
    .wf-row {{ display: flex; gap: 24px; align-items: flex-start; }}
    .wf-row img {{ display: block; border: 1px solid #d0d0d0; }}
  </style>
</head>
<body style="background:#ffffff;">
  <div style="padding:24px 24px 0;font-family:'Segoe UI',sans-serif;">
    <div style="font-size:20px;font-weight:700;color:#1b1b1b;">{workflow} — every BEFORE and AFTER</div>
    <div style="font-size:13px;color:#5d5d5d;">{len(rows)} design(s). One row each, drawn at {column} px wide; every state is compared in that design's own sheet.</div>
  </div>
  <div class="wf-head">
    <div>BEFORE — CURRENT IMPLEMENTATION</div>
    <div>AFTER — PROPOSED QOL IMPROVEMENT</div>
  </div>
{chr(10).join(blocks)}
  <div style="padding:0 24px 24px;font-family:'Segoe UI',sans-serif;font-size:12px;color:#5d5d5d;">
    A BEFORE that is a reconstruction says so in its own sheet. Approval is recorded per design, never for this sheet.
  </div>
</body>
</html>
"""


def workflow_design_paths(workflow_dir):
    """Every design record under one workflow folder, in screen then refactor order."""
    found = [path for path in pathlib.Path(workflow_dir).rglob("*-design.json")
             if "tools" not in path.parts]
    records = []
    for path in found:
        try:
            design = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise RenderError(f"{path.name} is not valid JSON: {error}") from error
        records.append((path, design))
    return sorted(records, key=lambda entry: (entry[1].get("screen") or "",
                                              entry[1].get("refactor") or ""))


def build_workflow(workflow_dir, edge_path=None):
    """Compose one sheet holding every design in a workflow, one row per design.

    This is the sheet a reader opens to see the whole proposal for a workflow at once; the per-design
    comparison remains the one that carries every state. It reads the canonical PNGs the renderer already
    wrote, so each design must have been rendered first.
    """
    workflow_dir = pathlib.Path(workflow_dir).resolve()
    if not workflow_dir.is_dir():
        raise RenderError(f"not a folder: {workflow_dir}")
    designs = workflow_design_paths(workflow_dir)
    if not designs:
        raise RenderError(f"no design records under {workflow_dir}")
    workflow = workflow_dir.name
    edge = find_edge(edge_path)

    rows, missing, nested = [], [], []
    for path, design in designs:
        if design.get("workflow") != workflow:
            nested.append(f"{pathlib.PurePath(path.relative_to(workflow_dir))} declares "
                          f"'{design.get('workflow')}'")
            continue
        refactor = design.get("refactor")
        if not refactor or path.name != f"{refactor}-design.json":
            raise RenderError(f"{path.name} does not match its refactor name")
        run = run_dir(path)
        before = run / PNGS_DIR / f"{refactor}-before.png"
        after = run / PNGS_DIR / f"{refactor}-after.png"
        absent = [png.name for png in (before, after) if not png.is_file()]
        if absent:
            missing.append(f"{refactor} ({', '.join(absent)})")
            continue
        rows.append({"path": path, "design": design, "refactor": refactor,
                     "screen": design.get("screen") or "",
                     "title": design.get("title") or refactor,
                     "before": before, "after": after,
                     "before_size": png_size(before), "after_size": png_size(after)})
    if nested:
        raise RenderError(
            "a workflow is its own folder directly under docs/ui-designs, and workflows are never "
            "nested: " + "; ".join(nested))
    if missing:
        raise RenderError("render these designs first: " + "; ".join(missing))
    if not rows:
        raise RenderError(f"no rendered designs under {workflow_dir}")

    column = max(max(row["before_size"][0], row["after_size"][0]) for row in rows)
    mockup_dir = ensure(workflow_dir / MOCKUPS_DIR)
    data_dir = ensure(workflow_dir / DATA_DIR)
    for row in rows:
        for key in ("before", "after"):
            width, height = row[f"{key}_size"]
            scaled = int(round(height * column / width))
            row[f"{key}_height"] = scaled
            row[f"{key}_href"] = pathlib.PurePath(
                os.path.relpath(row[key], mockup_dir)).as_posix()
        row["block_height"] = max(row["before_height"], row["after_height"]) + 52

    html_path = mockup_dir / f"{workflow}-compare.html"
    css_href = pathlib.PurePath(os.path.relpath(TOOLS / "mockup" / "mockup.css",
                                                html_path.parent)).as_posix()
    html_path.write_text(workflow_comparison_page(workflow, rows, column, css_href),
                         encoding="utf-8")
    page_width = column * 2 + 96
    page_height = 126 + sum(row["block_height"] for row in rows) + 80
    output = workflow_dir / f"{workflow}-comparison.png"
    work_root = pathlib.Path(tempfile.mkdtemp(prefix="mtm-mockup-workflow-"))
    try:
        size = render_page(edge, html_path, output, page_width, page_height, 1, DEFAULT_STATE,
                           work_root)
    finally:
        shutil.rmtree(work_root, ignore_errors=True)

    pdf_path = workflow_dir / f"{workflow}-comparison.pdf"
    write_comparison_pdf(pdf_path, [
        {"heading": row["title"],
         "subheading": f"{workflow} · {row['screen']} · {row['refactor']} · default state",
         "before": row["before"],
         "after": row["after"],
         "changes": row["design"].get("callouts") or [],
         "note": f"The BEFORE image is a {row['design'].get('before', {}).get('kind')}. "
                 f"Every state of this design is compared in "
                 f"{row['refactor']}-comparison.pdf inside its own folder."}
        for row in rows
    ])

    record = {
        "renderer": "headless Microsoft Edge",
        "edge": str(edge),
        "rendered_at": datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
        "workflow": workflow,
        "column": column,
        "designs": [
            {"design": pathlib.PurePath(row["path"].relative_to(workflow_dir)).as_posix(),
             "refactor": row["refactor"],
             "screen": row["screen"],
             "sources": {f"{PNGS_DIR}/{row['before'].name}": sha256_file(row["before"]),
                         f"{PNGS_DIR}/{row['after'].name}": sha256_file(row["after"])}}
            for row in rows
        ],
        "image": {"name": output.name, "width": size[0], "height": size[1],
                  "sha256": sha256_file(output)},
        "pdf": {"name": pdf_path.name, "pages": len(rows),
                "sha256": sha256_file(pdf_path)},
    }
    record_path = data_dir / f"{workflow}-comparison.json"
    record_path.write_text(json.dumps(record, indent=1, ensure_ascii=False) + "\n",
                           encoding="utf-8")
    return workflow, rows, record, output, record_path


def approve(design_path, by):
    design_path, design = load_design(design_path)
    if not by or not by.strip():
        raise RenderError("--approve needs --by \"<name>\"")
    run = run_dir(design_path)
    render_path = run / DATA_DIR / f"{design['refactor']}-render.json"
    if not render_path.is_file():
        raise RenderError("no render record: run the renderer first")
    record = json.loads(render_path.read_text(encoding="utf-8"))
    current = source_snapshot(design, run)
    if combined_hash(current) != record.get("sources_sha256"):
        raise RenderError("the mockups changed after the last render; render again before approving")
    system = design_system_snapshot(design)
    if combined_hash(system) != record.get("design_system_sha256"):
        raise RenderError("the design-system XAML changed after the last render; render again")
    design["approval"] = {
        "approved_at": datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
        "approved_by": by.strip(),
        "sources_sha256": record["sources_sha256"],
        "rendered_sha256": {name: image["sha256"] for name, image in record["images"].items()},
        "design_system_sha256": record["design_system_sha256"],
    }
    design_path.write_text(json.dumps(design, indent=2, ensure_ascii=False) + "\n",
                           encoding="utf-8")
    return design_path, design["approval"]


def main(argv=None):
    utf8_stdout()
    parser = argparse.ArgumentParser(
        description="Render a UI design's mockups, or one comparison sheet for a whole workflow.")
    parser.add_argument("--design", help="path to <refactor>-design.json")
    parser.add_argument("--workflow",
                        help="path to a workflow folder, e.g. docs\\ui-designs\\startup-launch; "
                             "writes <workflow>-comparison.png holding every design in it")
    parser.add_argument("--approve", action="store_true",
                        help="record approval of the current render in the design record")
    parser.add_argument("--by", help="approver name, required with --approve")
    parser.add_argument("--edge", help="path to msedge.exe")
    args = parser.parse_args(argv)
    try:
        if args.workflow:
            if args.design or args.approve:
                raise RenderError("--workflow cannot be combined with --design or --approve: "
                                  "approval is recorded per design")
            workflow, rows, record, output, record_path = build_workflow(args.workflow, args.edge)
            print(f"composed {output.name} for {workflow} from {len(rows)} design(s)")
            print(f"record: {record_path}")
            return 0
        if not args.design:
            raise RenderError("pass --design <path> or --workflow <folder>")
        if args.approve:
            path, approval = approve(args.design, args.by)
            print(f"approved {path.name} by {approval['approved_by']} "
                  f"at {approval['approved_at']}")
            return 0
        path, design, record, render_path = build(args.design, args.edge)
        print(f"rendered {len(record['images'])} image(s) for {design['refactor']} "
              f"at {len(record['viewports'])} window size(s); "
              f"{len(comparison_pairs(design))} before/after grouping(s)")
        print(f"record: {render_path}")
        print(f"sources: {record['sources_sha256'][:12]}  "
              f"design system: {record['design_system_sha256'][:12]}")
        return 0
    except RenderError as error:
        sys.stderr.write(f"error: {error}\n")
        return 2


if __name__ == "__main__":
    sys.exit(main())
