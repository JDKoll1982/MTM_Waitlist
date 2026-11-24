#!/usr/bin/env python3
"""Check a UI design proposal: complete, consistent with the workflow maps, and approved.

    python docs\\ui-designs\\tools\\verify_designs.py --all
    python docs\\ui-designs\\tools\\verify_designs.py --design docs\\ui-designs\\<workflow>\\<screen>\\<refactor>-design.json
    python docs\\ui-designs\\tools\\verify_designs.py --all --require-approval

A design is checked for:

* every declared state rendered at every declared window size, at the declared dimensions;
* the three supported window sizes (1600x1000, 1280x800, 900x500) present;
* the canonical BEFORE, AFTER and comparison images;
* a BEFORE that says whether it is a capture or a reconstruction, and names the XAML it came from;
* `elements[]` mapping each design element to a WinUI control and an existing file;
* `workflow_refs` resolving to real sheets and node ids in the workflow specs;
* `proposal.md` carrying the A-E sections and embedding the images;
* an approval whose hash still matches the mockup sources, the design-system XAML and every PNG.

Exit codes: 0 clean, 1 the design has errors (or is unapproved under `--require-approval`), 2 the
verifier could not run.
"""
import argparse
import json
import pathlib
import re
import sys

TOOLS = pathlib.Path(__file__).resolve().parent
REPO = TOOLS.parent.parent.parent

sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(REPO / "docs" / "diagrams" / "tools"))

import diagram_context  # noqa: E402
import render_mockup as render  # noqa: E402

REQUIRED_VIEWPORTS = {"wide": (1600, 1000), "medium": (1280, 800), "minimum": (900, 500)}
STAGE_SIZE = re.compile(r"new SizeInt32\((\d+),\s*(\d+)\)")
HEADINGS = [
    "## A. Current State",
    "## B. Proposed Improvement",
    "## C. Visual Comparison",
    "## D. Expected User Benefits",
    "## E. Implementation Impact",
]
DESIGN_KEYS = ("workflow", "screen", "refactor", "window", "before", "after", "states",
               "elements", "workflow_refs", "design_system", "approval")


class Report:
    def __init__(self, path):
        self.path = path
        self.errors = []
        self.warnings = []

    def error(self, message):
        self.errors.append(message)

    def warn(self, message):
        self.warnings.append(message)

    @property
    def ok(self):
        return not self.errors


def load(path):
    try:
        return json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise render.RenderError(f"{pathlib.Path(path).name} is not valid JSON: {error}") from error


def launch_stage_sizes():
    """The window sizes the host gives each launch stage, read from the code that sets them."""
    path = REPO / "MainWindow.xaml.cs"
    if not path.is_file():
        return set()
    return {(int(width), int(height)) for width, height in STAGE_SIZE.findall(
        path.read_text(encoding="utf-8"))}


def check_viewports(design, report):
    declared = {}
    for entry in (design.get("window") or {}).get("viewports") or []:
        try:
            declared[entry["name"]] = (int(entry["width"]), int(entry["height"]),
                                       float(entry.get("scale", 1)))
        except (KeyError, TypeError, ValueError):
            report.error(f"a viewport entry is incomplete: {entry}")
    kind = render.window_kind(design)
    if kind == "stage":
        if "stage" not in declared:
            report.error("a stage design declares the launch stage's own window size as "
                         "viewport 'stage' (for example 780x640 for sign-in)")
        else:
            width, height, _ = declared["stage"]
            known = launch_stage_sizes()
            if known and (width, height) not in known:
                report.error(f"the stage viewport {width}x{height} is not a launch stage size "
                             "in MainWindow.xaml.cs")
        return declared, "stage"
    if kind != "shell":
        report.error("window.kind must be 'shell' (the three desk sizes) or 'stage' "
                     "(the launch stage's own window size)")
        return declared, "wide"
    for name, (width, height) in REQUIRED_VIEWPORTS.items():
        if name not in declared:
            report.error(f"viewport '{name}' is missing; designs are checked at "
                         f"{width}x{height}")
        elif declared[name][:2] != (width, height):
            report.error(f"viewport '{name}' is {declared[name][0]}x{declared[name][1]}; "
                         f"it must be {width}x{height}")
    return declared, "wide"


def check_workflow(design, report, diagrams):
    workflow = design.get("workflow")
    try:
        document, _ = diagrams.select(workflow, design.get("workflow_variant"))
    except (render.RenderError, diagram_context.DiagramError) as error:
        check_unmapped_flow(design, report, str(error))
        return
    spec = json.loads(document["spec_path"].read_text(encoding="utf-8"))
    sheets = spec.get("sheets") or []
    for reference in design.get("workflow_refs") or []:
        try:
            number = int(reference["sheet"])
        except (KeyError, TypeError, ValueError):
            report.error(f"a workflow_ref has no sheet: {reference}")
            continue
        if not 1 <= number <= len(sheets):
            report.error(f"workflow_ref points at sheet {number}, but {workflow} has "
                         f"{len(sheets)} sheets")
            continue
        known = {node[0] for nodes in sheets[number - 1].get("lanes", {}).values()
                 for node in nodes}
        for node in reference.get("nodes") or []:
            if node not in known:
                report.error(f"workflow_ref node '{node}' is not on sheet {number} of "
                             f"{workflow}; the spec has no such node")


def check_unmapped_flow(design, report, reason):
    """A flow the diagram set does not map must name its own source instead of citing nodes."""
    sources = design.get("flow_source") or []
    if design.get("workflow_refs"):
        report.error(f"{reason}; a flow outside the diagram set cannot cite workflow_refs")
        return
    if not sources:
        report.error(f"{reason}; add flow_source naming the files that describe this flow")
        return
    missing = [source for source in sources if not (REPO / source).is_file()]
    if missing:
        report.error("flow_source names files that do not exist: " + ", ".join(missing))
        return
    report.warn(f"workflow '{design.get('workflow')}' is not mapped in docs/diagrams; the flow "
                f"source is recorded instead ({', '.join(sources)})")


def check_before(design, design_dir, declared, report):
    before = design.get("before") or {}
    kind = before.get("kind")
    if kind == "capture":
        if before.get("mockup"):
            report.error("a captured BEFORE has no mockup; remove before.mockup")
        if not before.get("captured_at"):
            report.error("a captured BEFORE needs before.captured_at")
        if not before.get("capture_command"):
            report.warn("before.capture_command is empty; record how the capture was taken")
        canonical = design_dir / f"{design['refactor']}-before.png"
        if not canonical.is_file():
            report.error(f"the captured BEFORE is missing: {canonical.name}")
        else:
            width, height = render.png_size(canonical)
            if width < 900 or height < 500:
                report.error(f"the captured BEFORE is {width}x{height}; it must be at least "
                             "900x500")
        for state in design.get("states") or []:
            if "before" in (state.get("applies_to") or []):
                report.error(f"state '{state.get('name')}' applies to before, but a capture has "
                             "no renderable states; set applies_to to [\"after\"]")
    elif kind == "reconstruction":
        if not before.get("mockup"):
            report.error("a reconstructed BEFORE needs before.mockup")
        else:
            mockup = design_dir / before["mockup"]
            if not mockup.is_file():
                report.error(f"the BEFORE mockup is missing: {before['mockup']}")
            elif "reconstruction" not in mockup.read_text(encoding="utf-8", errors="replace"):
                report.warn("the BEFORE mockup does not carry the word 'reconstruction'; the "
                            "reader must not mistake it for a screenshot")
        sources = before.get("source_xaml") or []
        if not sources:
            report.error("a reconstructed BEFORE must name the XAML it was built from")
        for source in sources:
            if not (REPO / source).is_file():
                report.error(f"before.source_xaml names a file that does not exist: {source}")
    else:
        report.error("before.kind must be 'capture' or 'reconstruction'")


def check_images(design, design_dir, declared, canonical, report):
    refactor = design["refactor"]
    states = design.get("states") or []
    if not states:
        report.error("the design declares no states")
    variants = ["after"]
    if (design.get("before") or {}).get("kind") == "reconstruction":
        variants.append("before")
    for variant in variants:
        applicable = [state["name"] for state in states
                      if variant in (state.get("applies_to") or ["before", "after"])]
        if "default" not in applicable:
            report.error(f"the {variant} variant has no 'default' state")
        for viewport in declared:
            width, height, scale = declared[viewport]
            for state in applicable:
                name = f"{refactor}-{variant}-{state}-{viewport}.png"
                path = design_dir / name
                if not path.is_file():
                    report.error(f"missing render: {name}")
                    continue
                actual = render.png_size(path)
                expected = (int(width * scale), int(height * scale))
                if actual != expected:
                    report.error(f"{name} is {actual[0]}x{actual[1]}, not "
                                 f"{expected[0]}x{expected[1]}")
    for variant in variants:
        name = f"{refactor}-{variant}.png"
        if not (design_dir / name).is_file():
            report.error(f"missing canonical image: {name}")
    comparison = design_dir / f"{refactor}-comparison.png"
    if not comparison.is_file():
        report.error(f"missing comparison image: {comparison.name}")
    elif canonical in declared:
        width, height, scale = declared[canonical]
        actual = render.png_size(comparison)
        if actual[0] < int(width * scale * 2) or actual[1] < int(height * scale):
            report.error(f"{comparison.name} is {actual[0]}x{actual[1]}; it must hold two "
                         f"{int(width * scale)}x{int(height * scale)} images side by side")


def check_elements(design, report):
    elements = design.get("elements") or []
    if not elements:
        report.error("elements is empty: the design must map to WinUI controls")
    for index, element in enumerate(elements, 1):
        label = element.get("design") or f"element {index}"
        if not element.get("design"):
            report.error(f"element {index} has no 'design' label")
        if not element.get("winui"):
            report.error(f"'{label}' has no WinUI control mapping")
        code = element.get("code") or []
        if not code:
            report.error(f"'{label}' names no code path")
        for path in code:
            if not (REPO / path).exists():
                report.error(f"'{label}' names a path that does not exist: {path}")


def check_proposal(design, design_dir, report):
    refactor = design["refactor"]
    proposal = design_dir / f"{refactor}-proposal.md"
    if not proposal.is_file():
        report.error(f"missing proposal: {proposal.name}")
        return
    text = proposal.read_text(encoding="utf-8", errors="replace")
    for heading in HEADINGS:
        if heading not in text:
            report.error(f"the proposal is missing the section '{heading}'")
    for image in ("before", "after", "comparison"):
        if f"{refactor}-{image}.png" not in text:
            report.error(f"the proposal does not embed {refactor}-{image}.png")


def check_render_and_approval(design, design_path, report, require_approval):
    design_dir = design_path.parent
    refactor = design["refactor"]
    render_path = design_dir / f"{refactor}-render.json"
    if not render_path.is_file():
        report.error(f"missing render record: {render_path.name}; run render_mockup.py")
        return
    record = json.loads(render_path.read_text(encoding="utf-8"))

    current_sources = render.source_snapshot(design, design_dir)
    if render.combined_hash(current_sources) != record.get("sources_sha256"):
        report.warn("the mockups changed after the last render; render again")
    try:
        current_system = render.design_system_snapshot(design)
    except render.RenderError as error:
        report.error(str(error))
        current_system = None
    if current_system and render.combined_hash(current_system) != record.get("design_system_sha256"):
        report.warn("the design-system XAML changed after the last render; render again")

    for name, image in (record.get("images") or {}).items():
        path = design_dir / name
        if not path.is_file():
            report.error(f"the render record lists {name}, which is gone")
        elif render.sha256_file(path) != image.get("sha256"):
            report.error(f"{name} changed after it was rendered; render again")

    approval = design.get("approval") or {}
    if not approval.get("approved_at"):
        (report.error if require_approval else report.warn)(
            "the design is not approved yet; present it and record approval with "
            "render_mockup.py --approve --by \"<name>\"")
        return
    if render.combined_hash(current_sources) != approval.get("sources_sha256"):
        report.error("the mockups changed after approval; the approval is invalid — re-render "
                     "and re-approve")
    if current_system and render.combined_hash(current_system) != approval.get("design_system_sha256"):
        report.error("the design-system XAML changed after approval; re-render and re-approve")
    approved_images = approval.get("rendered_sha256") or {}
    for name, image in (record.get("images") or {}).items():
        if approved_images.get(name) != image.get("sha256"):
            report.error(f"{name} is not the approved image; re-approve after rendering")
    if not approval.get("approved_by"):
        report.error("the approval names no approver")


def check(design_path, diagrams, require_approval=False):
    design_path = pathlib.Path(design_path)
    report = Report(design_path)
    try:
        design = load(design_path)
    except render.RenderError as error:
        report.error(str(error))
        return report, None
    for key in DESIGN_KEYS:
        if key not in design:
            report.error(f"the design record is missing '{key}'")
    if report.errors:
        return report, design
    if design_path.name != f"{design['refactor']}-design.json":
        report.error(f"the design file must be named {design['refactor']}-design.json")
    if design_path.parent.name != design["refactor"]:
        report.error(f"each run lives in its own folder: move this design to "
                     f".../{design['screen']}/{design['refactor']}/ (the folder must be named "
                     f"'{design['refactor']}')")
    declared, canonical = check_viewports(design, report)
    check_workflow(design, report, diagrams)
    check_before(design, design_path.parent, declared, report)
    check_images(design, design_path.parent, declared, canonical, report)
    check_elements(design, report)
    check_proposal(design, design_path.parent, report)
    check_render_and_approval(design, design_path, report, require_approval)
    return report, design


def discover():
    return sorted(path for path in (REPO / "docs" / "ui-designs").rglob("*-design.json")
                  if "tools" not in path.parts)


def main(argv=None):
    render.utf8_stdout()
    parser = argparse.ArgumentParser(description="Verify UI design proposals.")
    parser.add_argument("--design", action="append", default=[],
                        help="a design record; repeatable")
    parser.add_argument("--all", action="store_true", help="check every design under docs/ui-designs")
    parser.add_argument("--require-approval", action="store_true",
                        help="treat an unapproved design as an error")
    parser.add_argument("--json", action="store_true", help="print the reports as JSON")
    args = parser.parse_args(argv)

    targets = list(args.design)
    if args.all or not targets:
        targets.extend(str(path) for path in discover())
    if not targets:
        if args.all:
            print("no design records under docs/ui-designs; nothing to check")
            return 0
        sys.stderr.write("error: no design records found\n")
        return 2

    try:
        diagrams = render_diagrams()
    except render.RenderError as error:
        sys.stderr.write(f"error: {error}\n")
        return 2

    reports, failed = [], 0
    for target in targets:
        report, design = check(target, diagrams, require_approval=args.require_approval)
        failed += 0 if report.ok else 1
        reports.append({"design": str(target),
                        "workflow": (design or {}).get("workflow"),
                        "refactor": (design or {}).get("refactor"),
                        "errors": report.errors, "warnings": report.warnings})

    if args.json:
        sys.stdout.write(json.dumps({"reports": reports}, indent=1, ensure_ascii=False) + "\n")
    else:
        for entry in reports:
            name = pathlib.Path(entry["design"]).name
            status = "OK" if not entry["errors"] else "FAIL"
            print(f"{name:48s} {status}")
            for error in entry["errors"]:
                print(f"   error: {error}")
            for warning in entry["warnings"]:
                print(f"   note:  {warning}")
        print(f"\n{len(reports)} design(s) checked, {failed} with errors.")
    return 1 if failed else 0


def render_diagrams():
    return diagram_context.DiagramSet(REPO / "docs" / "diagrams")


if __name__ == "__main__":
    sys.exit(main())
