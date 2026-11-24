#!/usr/bin/env python3
"""Tests for `diagram_context.py`, the read-only workflow digest.

Run from the repository root:

    python -m unittest discover -s docs\\diagrams\\tools -p "test_*.py"

The tests use the real diagram set for the shapes the diagrams actually have (sheet counts,
connector pairing, findings) and small temporary fixtures for the failure paths (missing spec,
missing `.dot`, missing PDF, an out-of-range sheet reference, a duplicate manifest entry).
"""
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

TOOLS = pathlib.Path(__file__).resolve().parent
REPO = TOOLS.parent.parent.parent
DIAGRAMS = TOOLS.parent

sys.path.insert(0, str(TOOLS))
import diagram_context as context  # noqa: E402


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=1), encoding="utf-8")


def fixture_spec(badge_target=2, badge_text="NextVM", badge_from=None):
    badge_from = badge_text if badge_from is None else badge_from
    return {"sheets": [
        {
            "file": "fixture-1",
            "name": "Fixture1",
            "title": "Fixture sheet 1 of 2",
            "sheet": "Sheet 1 of 2",
            "source": "FixtureViewModel",
            "lanes": {
                "ui": [["a_start", "Starts here", "ui"]],
                "vm": [["x_next", f"[To Sheet {badge_target}]\\n{badge_text}", "conn"],
                       ["x_flag", "[REFACTOR] fixture finding", "refactor"]],
            },
            "edges": [["a_start", "x_next", "go", ""]],
        },
        {
            "file": "fixture-2",
            "name": "Fixture2",
            "title": "Fixture sheet 2 of 2",
            "sheet": "Sheet 2 of 2",
            "source": "FixtureViewModel",
            "lanes": {
                "vm": [["y_here", f"[From Sheet 1]\\n{badge_from}", "conn"]],
                "svc": [["s_call", "Calls a service", "svc"]],
            },
            "edges": [["y_here", "s_call", "", "plain"]],
        },
    ]}


def build_fixture(root, spec=None, with_dot=True, with_pdf=False, manifest=None):
    tools = root / "tools"
    write_json(tools / "spec-fixture.json", spec if spec is not None else fixture_spec())
    if with_dot:
        nodes = {1: ["a_start", "x_next", "x_flag"], 2: ["y_here", "s_call"]}
        for index, names in nodes.items():
            (root / "dot").mkdir(parents=True, exist_ok=True)
            body = "".join(f"    {name} [shape=box];\n" for name in names)
            (root / "dot" / f"fixture-{index}.dot").write_text(
                f"digraph fixture{index} {{\n{body}}}\n", encoding="utf-8")
    if with_pdf:
        (root / "pdf").mkdir(parents=True, exist_ok=True)
        (root / "pdf" / "fixture.pdf").write_bytes(b"%PDF-1.4\n% fixture\n")
    write_json(tools / "manifest.json", manifest or {"documents": [
        {"workflow": "fixture", "variant": "engineer", "title": "Fixture",
         "output": "fixture.pdf", "spec": "spec-fixture.json"},
    ]})
    return context.DiagramSet(root)


def digest_for(diagrams, document, **kwargs):
    return context.build(document, dot_dir=diagrams.root / "dot",
                         known_outputs=diagrams.known_outputs(), **kwargs)


class RealDiagramSet(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.diagrams = context.DiagramSet(DIAGRAMS)

    def test_every_workflow_has_two_variants(self):
        workflows = self.diagrams.workflows()
        self.assertEqual(sorted(workflows), ["generic-new-request", "generic-workcenter-setup",
                                             "workorder-new-request",
                                             "workorder-workcenter-setup"])
        for name, documents in workflows.items():
            self.assertEqual(sorted(document["variant"] for document in documents),
                             ["engineer", "friendly"], name)

    def test_sheet_counts_match_the_documented_set(self):
        expected = {("workorder-new-request", "engineer"): 5,
                    ("workorder-workcenter-setup", "engineer"): 5,
                    ("generic-new-request", "engineer"): 4,
                    ("generic-workcenter-setup", "engineer"): 2,
                    ("workorder-new-request", "friendly"): 2,
                    ("workorder-workcenter-setup", "friendly"): 2,
                    ("generic-new-request", "friendly"): 2,
                    ("generic-workcenter-setup", "friendly"): 2}
        for key, sheets in expected.items():
            document, _ = self.diagrams.select(*key)
            summary = self.diagrams.summary(document)
            self.assertEqual(summary["sheets"], sheets, key)
            self.assertEqual(summary["dots"], sheets, f"dot files for {key}")
            self.assertTrue(summary["pdf"], f"pdf for {key}")

    def test_engineer_digest_carries_structure_and_findings(self):
        document, defaulted = self.diagrams.select("workorder-new-request")
        self.assertTrue(defaulted)
        self.assertEqual(document["variant"], "engineer")
        digest = digest_for(self.diagrams, document)
        self.assertEqual(len(digest["sheets"]), 5)
        first = digest["sheets"][0]
        self.assertTrue(first["nodes"] and first["edges"])
        self.assertIn("no role gate", " ".join(node["label"] for node in first["findings"]))
        self.assertEqual(digest["warnings"], [])

    def test_connectors_pair_and_cross_documents_are_reported(self):
        document, _ = self.diagrams.select("workorder-new-request")
        digest = digest_for(self.diagrams, document)
        sheet_one = next(sheet for sheet in digest["sheets"] if sheet["index"] == 1)
        statuses = {link["text"]: link["status"] for link in sheet_one["connectors"]}
        self.assertEqual(statuses["CategoryVM"], "paired")
        self.assertEqual(statuses["[IsGeneric] → Diagram 3"], "cross-document")

    def test_variant_defaults_to_engineer_and_says_so(self):
        document, defaulted = self.diagrams.select("generic-new-request")
        self.assertTrue(defaulted)
        self.assertEqual(document["variant"], "engineer")

    def test_list_names_every_document(self):
        text = context.render_list(self.diagrams)
        for name in self.diagrams.workflows():
            self.assertIn(name, text)

    def test_digest_does_not_touch_the_diagram_set(self):
        def snapshot():
            return {path: hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in sorted(DIAGRAMS.rglob("*"))
                    if path.is_file() and "context" not in path.parts}
        before = snapshot()
        document, _ = self.diagrams.select("generic-workcenter-setup")
        digest_for(self.diagrams, document, sheet=1)
        self.assertEqual(before, snapshot())


class FixtureCases(unittest.TestCase):
    def test_complete_fixture_reports_no_warnings(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, with_dot=True, with_pdf=True)
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            self.assertEqual(digest["warnings"], [])
            self.assertEqual(digest["sources"]["dot"]["matched"], 2)
            statuses = {link["text"]: link["status"]
                        for link in digest["cross_sheet"]}
            self.assertEqual(statuses["NextVM"], "paired")

    def test_missing_dot_is_reported_without_losing_the_structure(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, with_dot=False, with_pdf=True)
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            self.assertTrue(any("dot" in warning for warning in digest["warnings"]))
            self.assertEqual(digest["sources"]["dot"]["matched"], 0)
            self.assertEqual(len(digest["sheets"]), 2)

    def test_missing_pdf_is_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, with_pdf=False)
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            self.assertTrue(any("PDF missing" in warning for warning in digest["warnings"]))
            self.assertFalse(digest["sources"]["pdf"]["present"])

    def test_pdf_only_source_says_structure_is_unavailable(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, with_dot=False, with_pdf=True)
            (root / "tools" / "spec-fixture.json").unlink()
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            self.assertEqual(digest["sheets"], [])
            self.assertTrue(any("editable graph structure is unavailable" in warning
                                for warning in digest["warnings"]))

    def test_connector_to_a_missing_sheet_warns(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, spec=fixture_spec(badge_target=9), with_pdf=True)
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            self.assertTrue(any("does not exist" in warning for warning in digest["warnings"]))
            self.assertTrue(any(link["status"] == "missing sheet"
                                for link in digest["cross_sheet"]))

    def test_one_way_connector_is_labelled_not_an_error(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, spec=fixture_spec(badge_text="Nobody",
                                                             badge_from="Something else"),
                                     with_pdf=True)
            document, _ = diagrams.select("fixture")
            digest = digest_for(diagrams, document)
            statuses = {link["text"]: link["status"] for link in digest["cross_sheet"]}
            self.assertEqual(statuses["Nobody"], "one-way")
            self.assertEqual(digest["warnings"], [])

    def test_duplicate_manifest_entry_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            entry = {"workflow": "fixture", "variant": "engineer", "title": "Fixture",
                     "output": "fixture.pdf", "spec": "spec-fixture.json"}
            with self.assertRaises(context.DiagramError):
                build_fixture(root, manifest={"documents": [entry, dict(entry)]})

    def test_unknown_workflow_and_variant_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root)
            with self.assertRaises(context.DiagramError):
                diagrams.select("no-such-workflow")
            with self.assertRaises(context.DiagramError):
                diagrams.select("fixture", "no-such-variant")

    def test_sheet_out_of_range_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            diagrams = build_fixture(root, with_pdf=True)
            document, _ = diagrams.select("fixture")
            with self.assertRaises(context.DiagramError):
                digest_for(diagrams, document, sheet=7)


class CommandLine(unittest.TestCase):
    def run_tool(self, *arguments):
        return subprocess.run([sys.executable, str(TOOLS / "diagram_context.py"), *arguments],
                              capture_output=True, text=True, encoding="utf-8",
                              cwd=str(REPO))

    def test_list_exits_zero(self):
        result = self.run_tool("--list")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("workorder-new-request", result.stdout)

    def test_unknown_workflow_exits_two(self):
        result = self.run_tool("--workflow", "nope")
        self.assertEqual(result.returncode, 2)
        self.assertIn("unknown workflow", result.stderr)

    def test_strict_exits_one_only_on_warnings(self):
        ok = self.run_tool("--workflow", "workorder-new-request", "--variant", "engineer",
                           "--strict")
        self.assertEqual(ok.returncode, 0, ok.stderr)


if __name__ == "__main__":
    unittest.main()
