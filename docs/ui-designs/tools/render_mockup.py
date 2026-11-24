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


def comparison_page(design, design_dir, wide, scale, css_href):
    refactor = design["refactor"]
    before = f"../{refactor}-before.png"
    after = f"../{refactor}-after.png"
    callouts = "\n".join(f"      <li>{text}</li>" for text in design.get("callouts") or [])
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>{design.get('title') or refactor} — BEFORE and AFTER</title>
  <link rel="stylesheet" href="{css_href}">
</head>
<body style="background:#ffffff;">
  <div style="padding:24px 24px 0;font-family:'Segoe UI',sans-serif;">
    <div style="font-size:20px;font-weight:700;color:#1b1b1b;">{design.get('title') or refactor}</div>
    <div style="font-size:13px;color:#5d5d5d;">{design['workflow']} · {design['screen']} · viewport {wide['width']}×{wide['height']}</div>
  </div>
  <div class="compare">
    <figure>
      <figcaption>BEFORE — CURRENT IMPLEMENTATION</figcaption>
      <img src="{before}" width="{wide['width'] * scale}" alt="BEFORE, current implementation">
    </figure>
    <figure>
      <figcaption>AFTER — PROPOSED QOL IMPROVEMENT</figcaption>
      <img src="{after}" width="{wide['width'] * scale}" alt="AFTER, proposed improvement">
    </figure>
  </div>
  <div style="padding:0 24px 24px;font-family:'Segoe UI',sans-serif;">
    <div style="font-weight:700;font-size:14px;margin-bottom:8px;">What changed</div>
    <ol class="callout-list">
{callouts}
    </ol>
    <div style="margin-top:12px;font-size:12px;color:#5d5d5d;">
      The BEFORE image is a {design['before'].get('kind')}. Rendered {len(design.get('states') or [])} state(s) at {len(design['window']['viewports'])} window size(s).
    </div>
  </div>
</body>
</html>
"""


def build(design_path, edge_path=None):
    design_path, design = load_design(design_path)
    design_dir = design_path.parent
    refactor = design["refactor"]
    edge = find_edge(edge_path)
    sizes = viewports(design)
    sources = source_snapshot(design, design_dir)
    system = design_system_snapshot(design)

    images = {}
    work_root = pathlib.Path(tempfile.mkdtemp(prefix="mtm-mockup-run-"))
    try:
        for variant, mockup in mockups(design):
            html_path = design_dir / mockup
            if not html_path.is_file():
                raise RenderError(f"mockup not found: {html_path}")
            for viewport in sizes:
                for state in state_names(design, variant):
                    name = f"{refactor}-{variant}-{state}-{viewport['name']}.png"
                    size = render_page(edge, html_path, design_dir / name, viewport["width"],
                                       viewport["height"], viewport["scale"], state, work_root)
                    images[name] = {"width": size[0], "height": size[1],
                                    "sha256": sha256_file(design_dir / name)}
                default_name = f"{refactor}-{variant}-{DEFAULT_STATE}-{viewport['name']}.png"
                default_hash = (images.get(default_name) or {}).get("sha256")
                for state in state_names(design, variant):
                    if state == DEFAULT_STATE or not default_hash:
                        continue
                    entry = images[f"{refactor}-{variant}-{state}-{viewport['name']}.png"]
                    if entry["sha256"] == default_hash:
                        raise RenderError(
                            f"state '{state}' rendered identically to '{DEFAULT_STATE}' for "
                            f"{variant} at {viewport['name']}; the mockup does not respond to it — "
                            f"check for an element with id '{state}' and a rule in mockup.css that "
                            "reveals its content")
            canonical = f"{refactor}-{variant}.png"
            name_of_canonical = canonical_viewport(design)
            wide_name = f"{refactor}-{variant}-{DEFAULT_STATE}-{name_of_canonical}.png"
            if wide_name not in images:
                raise RenderError(f"the {variant} mockup has no '{DEFAULT_STATE}' state at the "
                                  f"'{name_of_canonical}' viewport")
            shutil.copyfile(design_dir / wide_name, design_dir / canonical)
            size = png_size(design_dir / canonical)
            images[canonical] = {"width": size[0], "height": size[1],
                                 "sha256": sha256_file(design_dir / canonical)}

        wide = next((entry for entry in sizes if entry["name"] == canonical_viewport(design)),
                    sizes[0])
        compare_html = design_dir / "mockups" / f"{refactor}-compare.html"
        compare_html.parent.mkdir(parents=True, exist_ok=True)
        css_href = pathlib.PurePath(os.path.relpath(TOOLS / "mockup" / "mockup.css",
                                                    compare_html.parent)).as_posix()
        compare_html.write_text(comparison_page(design, design_dir, wide, wide["scale"],
                                                css_href),
                                encoding="utf-8")
        compare_width = int(wide["width"] * wide["scale"] * 2 + 96)
        compare_height = int(wide["height"] * wide["scale"] + 260)
        compare_name = f"{refactor}-comparison.png"
        size = render_page(edge, compare_html, design_dir / compare_name, compare_width,
                           compare_height, 1, DEFAULT_STATE, work_root)
        images[compare_name] = {"width": size[0], "height": size[1],
                                "sha256": sha256_file(design_dir / compare_name)}
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
    render_path = design_dir / f"{refactor}-render.json"
    render_path.write_text(json.dumps(record, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    return design_path, design, record, render_path


def approve(design_path, by):
    design_path, design = load_design(design_path)
    if not by or not by.strip():
        raise RenderError("--approve needs --by \"<name>\"")
    render_path = design_path.parent / f"{design['refactor']}-render.json"
    if not render_path.is_file():
        raise RenderError("no render record: run the renderer first")
    record = json.loads(render_path.read_text(encoding="utf-8"))
    current = source_snapshot(design, design_path.parent)
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
    parser = argparse.ArgumentParser(description="Render a UI design's mockups to PNG.")
    parser.add_argument("--design", required=True, help="path to <refactor>-design.json")
    parser.add_argument("--approve", action="store_true",
                        help="record approval of the current render in the design record")
    parser.add_argument("--by", help="approver name, required with --approve")
    parser.add_argument("--edge", help="path to msedge.exe")
    args = parser.parse_args(argv)
    try:
        if args.approve:
            path, approval = approve(args.design, args.by)
            print(f"approved {path.name} by {approval['approved_by']} "
                  f"at {approval['approved_at']}")
            return 0
        path, design, record, render_path = build(args.design, args.edge)
        print(f"rendered {len(record['images'])} image(s) for {design['refactor']} "
              f"at {len(record['viewports'])} window size(s)")
        print(f"record: {render_path}")
        print(f"sources: {record['sources_sha256'][:12]}  "
              f"design system: {record['design_system_sha256'][:12]}")
        return 0
    except RenderError as error:
        sys.stderr.write(f"error: {error}\n")
        return 2


if __name__ == "__main__":
    sys.exit(main())
