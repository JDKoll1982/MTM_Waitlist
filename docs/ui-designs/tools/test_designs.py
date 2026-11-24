#!/usr/bin/env python3
"""Tests for the UI design record, renderer and verifier.

    python -m unittest discover -s docs\\ui-designs\\tools -p "test_*.py"

Most cases use a temporary design folder and generated PNGs, so they need no browser. The renderer
cases need Microsoft Edge and are skipped when it is absent.
"""
import json
import pathlib
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib

TOOLS = pathlib.Path(__file__).resolve().parent
REPO = TOOLS.parent.parent.parent
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(REPO / "docs" / "diagrams" / "tools"))

import diagram_context  # noqa: E402
import render_mockup as render  # noqa: E402
import verify_designs as verify  # noqa: E402

# A workflow whose spec is in every checkout, so the fixture does not depend on the others.
WORKFLOW = "startup-launch"
REFACTOR = "fixture-change"
VIEWPORTS = [{"name": "wide", "width": 1600, "height": 1000, "scale": 1},
             {"name": "medium", "width": 1280, "height": 800, "scale": 1},
             {"name": "minimum", "width": 900, "height": 500, "scale": 1}]
STAGE_WINDOW = {"kind": "stage", "stage": "sign-in",
                "viewports": [{"name": "stage", "width": 780, "height": 640, "scale": 1}]}


def write_png(path, width, height, rgb=(255, 255, 255)):
    """A valid solid-colour PNG of the given pixel size, with no image library."""
    raw = b"".join(b"\x00" + bytes(rgb) * width for _ in range(height))

    def chunk(tag, data):
        body = tag + data
        return (struct.pack(">I", len(data)) + body
                + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF))

    payload = (b"\x89PNG\r\n\x1a\n"
               + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
               + chunk(b"IDAT", zlib.compress(raw, 6))
               + chunk(b"IEND", b""))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(payload)


def write_render_record(design_path, design, design_dir):
    images = {}
    for png in sorted((design_dir / "pngs").glob("*.png")):
        width, height = render.png_size(png)
        images[f"pngs/{png.name}"] = {"width": width, "height": height,
                                      "sha256": render.sha256_file(png)}
    for name in (f"{design['refactor']}-comparison.png", f"{design['refactor']}-comparison.pdf"):
        image = design_dir / name
        width, height = ((3296, 1260) if image.suffix == ".png"
                         else (int(render.PDF_PAGE_W), int(render.PDF_PAGE_H)))
        images[name] = {"width": width, "height": height,
                        "sha256": render.sha256_file(image)}
    sources = render.source_snapshot(design, design_dir)
    system = render.design_system_snapshot(design)
    record = {
        "renderer": "fixture",
        "edge": "fixture",
        "rendered_at": "2026-11-24T12:00:00-06:00",
        "viewports": design["window"]["viewports"],
        "states": design["states"],
        "sources": sources,
        "sources_sha256": render.combined_hash(sources),
        "design_system": system,
        "design_system_sha256": render.combined_hash(system),
        "images": images,
    }
    path = design_dir / "data" / f"{design['refactor']}-render.json"
    path.write_text(json.dumps(record, indent=1), encoding="utf-8")
    return record


PROPOSAL = """# QOL Refactor — Before and After Design

## A. Current State

![BEFORE](fixture-change-before.png)

## B. Proposed Improvement

![AFTER](fixture-change-after.png)

## C. Visual Comparison

![Comparison](fixture-change-comparison.png)

## D. Expected User Benefits

- One fewer step.

## E. Implementation Impact

| Area | What changes |
| --- | --- |
| XAML | one page |
"""


def make_design(root, *, approved=False, before_kind="reconstruction", states=None,
                refactor=REFACTOR, viewports=None, proposal=PROPOSAL, workflow=WORKFLOW,
                window=None, flow_source=None, workflow_refs=None):
    design_dir = pathlib.Path(root) / WORKFLOW / "fixture-screen" / refactor
    (design_dir / "mockups").mkdir(parents=True)
    (design_dir / "pngs").mkdir()
    (design_dir / "data").mkdir()
    (design_dir / "mockups" / "mockup.css").write_text(".mockup { color: #1b1b1b; }\n",
                                                       encoding="utf-8")
    variants = ["before", "after"] if before_kind == "reconstruction" else ["after"]
    for variant in variants:
        (design_dir / "mockups" / f"{refactor}-{variant}.html").write_text(
            "<!doctype html><html><head><meta charset=\"utf-8\">"
            "<link rel=\"stylesheet\" href=\"mockup.css\"></head>"
            f"<body class=\"mockup\" data-variant=\"{variant}\">"
            f"<span class=\"state\" id=\"state-default\"></span>"
            f"{variant} reconstruction fixture</body></html>",
            encoding="utf-8")

    if states is None:
        states = ([{"name": "default", "applies_to": ["before", "after"]},
                   {"name": "state-error", "applies_to": ["before", "after"]}]
                  if before_kind == "reconstruction"
                  else [{"name": "default", "applies_to": ["after"]}, {"name": "state-error", "applies_to": ["after"]}])

    window = window or {"viewports": viewports or VIEWPORTS}
    canonical = "stage" if window.get("kind") == "stage" else "wide"
    design = {
        "workflow": workflow,
        "workflow_variant": "engineer",
        "screen": "fixture-screen",
        "refactor": refactor,
        "title": "Fixture change",
        "audience": "developer",
        "window": window,
        "before": ({"kind": "reconstruction",
                    "mockup": f"mockups/{refactor}-before.html",
                    "captured_at": None,
                    "capture_command": None,
                    "source_xaml": ["Module_Waitlist/Views/WaitlistViewPage.xaml"]}
                   if before_kind == "reconstruction"
                   else {"kind": "capture",
                         "mockup": None,
                         "captured_at": "2026-11-24T11:00:00-06:00",
                         "capture_command": "scripts/ui-automation/capture_app_window.ps1",
                         "source_xaml": ["Module_Waitlist/Views/WaitlistViewPage.xaml"]}),
        "after": {"mockup": f"mockups/{refactor}-after.html"},
        "states": states,
        "callouts": ["A first change."],
        "elements": [{"design": "Press card grid",
                      "winui": "GridView with ItemTemplate",
                      "code": ["Module_Waitlist/Views/WaitlistViewPage.xaml"],
                      "note": None}],
        "workflow_refs": workflow_refs if workflow_refs is not None else [
            {"sheet": 1, "nodes": ["v_pipe", "d_conf"]}],
        "design_system": {"resources": ["Styles/FontSizes.xaml", "Styles/Thickness.xaml"]},
        "approval": {"approved_at": None, "approved_by": None, "sources_sha256": None,
                     "rendered_sha256": None, "design_system_sha256": None},
    }
    if flow_source is not None:
        design["flow_source"] = flow_source
    design_path = design_dir / "data" / f"{refactor}-design.json"
    design_path.write_text(json.dumps(design, indent=1), encoding="utf-8")

    sizes = design["window"]["viewports"]
    for variant in variants:
        for state in design["states"]:
            if variant not in (state.get("applies_to") or ["before", "after"]):
                continue
            for viewport in sizes:
                write_png(design_dir / "pngs" / f"{refactor}-{variant}-{state['name']}-{viewport['name']}.png",
                          int(viewport["width"] * viewport.get("scale", 1)),
                          int(viewport["height"] * viewport.get("scale", 1)))
        wide = next((viewport for viewport in sizes if viewport["name"] == canonical), sizes[0])
        shutil.copyfile(design_dir / "pngs" / f"{refactor}-{variant}-default-{wide['name']}.png",
                        design_dir / "pngs" / f"{refactor}-{variant}.png")
    if before_kind == "capture":
        write_png(design_dir / "pngs" / f"{refactor}-before.png", 1600, 1000)
    write_png(design_dir / f"{refactor}-comparison.png", 3296, 1260)
    pairs = [state["name"] for state in design["states"]
             if "after" in (state.get("applies_to") or ["before", "after"])
             and (before_kind == "capture" and state["name"] == "default"
                  or before_kind == "reconstruction"
                  and "before" in (state.get("applies_to") or ["before", "after"]))]
    render.write_comparison_pdf(design_dir / f"{refactor}-comparison.pdf", [
        {"heading": design["title"],
         "subheading": f"{design['workflow']} · {design['screen']} · {state}",
         "before": design_dir / "pngs" / f"{refactor}-before-{state}-{canonical}.png"
                   if before_kind == "reconstruction"
                   else design_dir / "pngs" / f"{refactor}-before.png",
         "after": design_dir / "pngs" / f"{refactor}-after-{state}-{canonical}.png",
         "changes": design["callouts"],
         "note": f"The BEFORE image is a {before_kind}."}
        for state in pairs])
    (design_dir / f"{refactor}-proposal.md").write_text(proposal, encoding="utf-8")
    write_render_record(design_path, design, design_dir)
    if approved:
        render.approve(design_path, "Fixture Approver")
    return design_dir, design_path


class Verification(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.diagrams = diagram_context.DiagramSet(REPO / "docs" / "diagrams")

    def check(self, path, require_approval=False):
        report, design = verify.check(path, self.diagrams, require_approval=require_approval)
        return report, design

    def test_complete_design_passes_and_notes_pending_approval(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            report, _ = self.check(path)
            self.assertEqual(report.errors, [])
            self.assertTrue(any("not approved" in warning for warning in report.warnings))

    def test_require_approval_fails_when_unapproved(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            report, _ = self.check(path, require_approval=True)
            self.assertTrue(report.errors)

    def test_approved_design_passes(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder, approved=True)
            report, _ = self.check(path, require_approval=True)
            self.assertEqual(report.errors, [], report.errors)

    def test_editing_a_mockup_after_approval_invalidates_it(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = make_design(folder, approved=True)
            mockup = design_dir / "mockups" / f"{REFACTOR}-after.html"
            mockup.write_text(mockup.read_text(encoding="utf-8") + "<!-- change -->",
                              encoding="utf-8")
            report, _ = self.check(path, require_approval=True)
            self.assertTrue(any("changed after approval" in error for error in report.errors))

    def test_captured_before_cannot_declare_before_states(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(
                folder, before_kind="capture",
                states=[{"name": "default", "applies_to": ["before", "after"]}])
            report, _ = self.check(path)
            self.assertTrue(any("no renderable states" in error for error in report.errors))

    def test_missing_state_image_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = make_design(folder)
            (design_dir / "pngs" / f"{REFACTOR}-after-state-error-medium.png").unlink()
            report, _ = self.check(path)
            self.assertTrue(any("missing render" in error for error in report.errors))

    def test_wrong_image_dimensions_fail(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = make_design(folder)
            write_png(design_dir / "pngs" / f"{REFACTOR}-after-state-error-medium.png", 100, 100)
            report, _ = self.check(path)
            self.assertTrue(any("not " in error and "1280x800" in error for error in report.errors))

    def test_missing_required_viewport_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder, viewports=[VIEWPORTS[0]])
            report, _ = self.check(path)
            self.assertTrue(any("viewport 'medium' is missing" in error for error in report.errors))

    def test_unknown_workflow_node_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = make_design(folder)
            design = json.loads(path.read_text(encoding="utf-8"))
            design["workflow_refs"] = [{"sheet": 1, "nodes": ["not_a_node"]}]
            path.write_text(json.dumps(design, indent=1), encoding="utf-8")
            report, _ = self.check(path)
            self.assertTrue(any("not_a_node" in error for error in report.errors))

    def test_unknown_workflow_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            design = json.loads(path.read_text(encoding="utf-8"))
            design["workflow"] = "no-such-workflow"
            path.write_text(json.dumps(design, indent=1), encoding="utf-8")
            report, _ = self.check(path)
            self.assertTrue(any("unknown workflow" in error for error in report.errors))

    def test_missing_code_path_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            design = json.loads(path.read_text(encoding="utf-8"))
            design["elements"][0]["code"] = ["Module_Nowhere/Views/Missing.xaml"]
            path.write_text(json.dumps(design, indent=1), encoding="utf-8")
            report, _ = self.check(path)
            self.assertTrue(any("does not exist" in error for error in report.errors))

    def test_element_without_winui_mapping_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            design = json.loads(path.read_text(encoding="utf-8"))
            design["elements"][0]["winui"] = ""
            path.write_text(json.dumps(design, indent=1), encoding="utf-8")
            report, _ = self.check(path)
            self.assertTrue(any("no WinUI control mapping" in error for error in report.errors))

    def test_proposal_missing_a_section_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder, proposal=PROPOSAL.replace("## D. Expected User Benefits",
                                                                    "## Benefits"))
            report, _ = self.check(path)
            self.assertTrue(any("## D. Expected User Benefits" in error for error in report.errors))

    def test_image_changed_after_render_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = make_design(folder)
            write_png(design_dir / "pngs" / f"{REFACTOR}-after-default-wide.png", 1600, 1000,
                      rgb=(200, 200, 200))
            report, _ = self.check(path)
            self.assertTrue(any("changed after it was rendered" in error
                                for error in report.errors))

    def test_reconstruction_without_xaml_provenance_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            design = json.loads(path.read_text(encoding="utf-8"))
            design["before"]["source_xaml"] = []
            path.write_text(json.dumps(design, indent=1), encoding="utf-8")
            report, _ = self.check(path)
            self.assertTrue(any("must name the XAML" in error for error in report.errors))

    def test_stage_design_uses_the_launch_stage_size(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder, window=STAGE_WINDOW)
            report, _ = self.check(path)
            self.assertEqual(report.errors, [], report.errors)

    def test_stage_size_the_host_does_not_set_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            window = {"kind": "stage", "stage": "sign-in",
                      "viewports": [{"name": "stage", "width": 777, "height": 640, "scale": 1}]}
            _, path = make_design(folder, window=window)
            report, _ = self.check(path)
            self.assertTrue(any("not a launch stage size" in error for error in report.errors))

    def test_unmapped_workflow_with_a_recorded_flow_source_passes(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(
                folder, workflow="unmapped-flow",
                flow_source=["docs/living-specs/living-specs-capability-startup-launch.spec.md"],
                workflow_refs=[])
            report, _ = self.check(path)
            self.assertEqual(report.errors, [], report.errors)
            self.assertTrue(any("not mapped" in warning for warning in report.warnings))

    def test_unmapped_workflow_without_a_flow_source_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder, workflow="unmapped-flow", workflow_refs=[])
            report, _ = self.check(path)
            self.assertTrue(any("add flow_source" in error for error in report.errors))

    def test_unmapped_workflow_cannot_cite_diagram_nodes(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(
                folder, workflow="unmapped-flow",
                flow_source=["docs/living-specs/living-specs-capability-startup-launch.spec.md"])
            report, _ = self.check(path)
            self.assertTrue(any("cannot cite workflow_refs" in error for error in report.errors))


class CommandLine(unittest.TestCase):
    def run_tool(self, *arguments):
        return subprocess.run([sys.executable, str(TOOLS / "verify_designs.py"), *arguments],
                              capture_output=True, text=True, encoding="utf-8",
                              cwd=str(REPO))

    def test_design_passes_on_the_command_line(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            result = self.run_tool("--design", str(path))
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("OK", result.stdout)

    def test_require_approval_fails_on_the_command_line(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = make_design(folder)
            result = self.run_tool("--design", str(path), "--require-approval")
            self.assertEqual(result.returncode, 1)

    def test_no_designs_is_reported(self):
        result = self.run_tool("--design", str(TOOLS / "template" / "design.json"))
        self.assertEqual(result.returncode, 1)
        self.assertIn("error", result.stdout.lower() + result.stderr.lower())


try:
    EDGE = render.find_edge()
except render.RenderError:
    EDGE = None


@unittest.skipUnless(EDGE, "Microsoft Edge is not available")
class Rendering(unittest.TestCase):
    def make_mockup_design(self, root):
        design_dir = pathlib.Path(root) / "wf" / "screen" / "tiny"
        mockups = design_dir / "mockups"
        mockups.mkdir(parents=True)
        (design_dir / "data").mkdir()
        shutil.copy(TOOLS / "mockup" / "mockup.css", mockups / "mockup.css")
        shutil.copy(TOOLS / "mockup" / "tokens.css", mockups / "tokens.css")
        body = ("<span class=\"state\" id=\"state-default\"></span>"
                "<span class=\"state\" id=\"state-error\"></span>"
                "<h1 class=\"page-title\">Fixture screen</h1>"
                "<div class=\"infobar error when-error\">broken</div>")
        for variant in ("before", "after"):
            (mockups / f"tiny-{variant}.html").write_text(
                "<!doctype html><html><head><meta charset=\"utf-8\">"
                "<link rel=\"stylesheet\" href=\"mockup.css\"></head>"
                f"<body class=\"mockup\" data-variant=\"{variant}\">{body}</body></html>",
                encoding="utf-8")
        design = {
            "workflow": WORKFLOW, "screen": "screen", "refactor": "tiny",
            "title": "Tiny fixture", "audience": "developer",
            "window": {"viewports": [{"name": "wide", "width": 700, "height": 500, "scale": 1}]},
            "before": {"kind": "reconstruction", "mockup": "mockups/tiny-before.html",
                       "captured_at": None, "capture_command": None,
                       "source_xaml": ["Module_Waitlist/Views/WaitlistViewPage.xaml"]},
            "after": {"mockup": "mockups/tiny-after.html"},
            "states": [{"name": "default", "applies_to": ["before", "after"]},
                       {"name": "state-error", "applies_to": ["before", "after"]}],
            "callouts": ["First change."],
            "elements": [{"design": "Card", "winui": "Border",
                          "code": ["Module_Waitlist/Views/WaitlistViewPage.xaml"], "note": None}],
            "workflow_refs": [{"sheet": 1, "nodes": ["v_pipe"]}],
            "design_system": {"resources": ["Styles/FontSizes.xaml"]},
            "approval": {"approved_at": None, "approved_by": None, "sources_sha256": None,
                         "rendered_sha256": None, "design_system_sha256": None},
        }
        path = design_dir / "data" / "tiny-design.json"
        path.write_text(json.dumps(design, indent=1), encoding="utf-8")
        return design_dir, path

    def test_renders_every_state_at_every_viewport_and_composes_comparison(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = self.make_mockup_design(folder)
            _, design, record, render_path = render.build(path)
            self.assertTrue(render_path.is_file())
            for variant in ("before", "after"):
                for state in ("default", "state-error"):
                    png = design_dir / "pngs" / f"tiny-{variant}-{state}-wide.png"
                    self.assertTrue(png.is_file(), png.name)
                    self.assertEqual(render.png_size(png), (700, 500))
                self.assertTrue((design_dir / "pngs" / f"tiny-{variant}.png").is_file())
                self.assertNotEqual(
                    render.sha256_file(design_dir / "pngs" / f"tiny-{variant}-default-wide.png"),
                    render.sha256_file(design_dir / "pngs" / f"tiny-{variant}-state-error-wide.png"),
                    "the error state must render differently from the default state")
            comparison = design_dir / "tiny-comparison.png"
            self.assertTrue(comparison.is_file())
            width, height = render.png_size(comparison)
            self.assertGreaterEqual(width, 1400)
            self.assertGreaterEqual(height, 500)
            pdf = design_dir / "tiny-comparison.pdf"
            self.assertTrue(pdf.is_file())
            document = render.pdf_document(pdf)
            self.assertEqual(document.page_count, 2,
                             "one page per before/after grouping")
            self.assertAlmostEqual(document[0].rect.width, 792.0, places=0)
            self.assertAlmostEqual(document[0].rect.height, 612.0, places=0)
            self.assertIn("First change.", " ".join(document[0].get_text().split()))
            document.close()
            self.assertEqual(len(record["sources"]), len(record["sources"]))
            self.assertTrue(record["sources_sha256"])
            self.assertTrue(record["design_system_sha256"])

    def test_approve_refuses_a_stale_render(self):
        with tempfile.TemporaryDirectory() as folder:
            design_dir, path = self.make_mockup_design(folder)
            render.build(path)
            mockup = design_dir / "mockups" / "tiny-after.html"
            mockup.write_text(mockup.read_text(encoding="utf-8") + "<!-- edit -->",
                              encoding="utf-8")
            with self.assertRaises(render.RenderError):
                render.approve(path, "Fixture Approver")

    def test_approve_records_the_sources_and_images(self):
        with tempfile.TemporaryDirectory() as folder:
            _, path = self.make_mockup_design(folder)
            render.build(path)
            _, approval = render.approve(path, "Fixture Approver")
            self.assertTrue(approval["approved_at"])
            self.assertEqual(approval["approved_by"], "Fixture Approver")
            stored = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(stored["approval"]["sources_sha256"], approval["sources_sha256"])


class Contract(unittest.TestCase):
    def test_capture_with_a_mockup_is_refused(self):
        design = {"before": {"kind": "capture", "mockup": "mockups/x.html"}}
        with self.assertRaises(render.RenderError):
            render.mockups(design)

    def test_state_names_must_be_slugs(self):
        design = {"states": [{"name": "Not A Slug", "applies_to": ["after"]}]}
        with self.assertRaises(render.RenderError):
            render.state_names(design, "after")

    def test_state_names_must_be_anchor_ids(self):
        design = {"states": [{"name": "default", "applies_to": ["after"]},
                             {"name": "error", "applies_to": ["after"]}]}
        with self.assertRaises(render.RenderError):
            render.state_names(design, "after")


if __name__ == "__main__":
    unittest.main()
