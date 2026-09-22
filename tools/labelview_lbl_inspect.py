"""Inspect a TEKLYNX LABELVIEW / CODESOFT ``.lbl`` file without opening LABELVIEW.

A ``.lbl`` file is an OLE2 (Compound File Binary) container. This script lists the
streams it holds and extracts the printable text from each one, which is enough to
recover a label's variable names, its printer and driver choice, and any embedded
printer code (ZPL / IPL / EPL) or VB script the label carries.

Usage:
    python tools/labelview_lbl_inspect.py "<path to label.lbl>" [--outdir DIR]
    python tools/labelview_lbl_inspect.py "<path to label.lbl>" --grep TEXT [--grep TEXT ...]
    python tools/labelview_lbl_inspect.py "<path to label.lbl>" --keywords

Exit codes: 0 = ok, 2 = file is not an OLE2 container, 3 = unreadable stream.
"""

from __future__ import annotations

import argparse
import os
import re
import sys

import olefile

# Printable ASCII run, and the same for UTF-16LE (LABELVIEW stores some strings wide).
ASCII_RUN = re.compile(rb"[\x20-\x7e]{4,}")
WIDE_RUN = re.compile(rb"(?:[\x20-\x7e]\x00){4,}")

# What a label carries that an implementation cares about.
KEYWORDS = [
    "ZPL", "ZPLII", "EPL", "IPL", "DPL", "PCL", "Zebra", "Sato", "Datamax", "Intermec",
    "Toshiba", "Cab", "Cognitive", "dpi", "DPI", "203", "300", "600",
    "Print", "Printer", "Driver", "Port", "Queue", "Spool",
    "Barcode", "Symbology", "Code128", "Code 128", "Code39", "Code 39", "UCC", "EAN", "I2of5",
    "Prompt", "WhenPrinted", "Variable", "Counter", "Serial", "Input",
    "DB", "Database", "ODBC", "Query", "SQL", "Table", "Field",
    "VBScript", "Script", "Macro", "JobModifier", "Job Modifier",
]


def ascii_runs(blob: bytes, minimum: int = 4) -> list[str]:
    return [m.group().decode("ascii", "replace") for m in re.finditer(rb"[\x20-\x7e]{%d,}" % minimum, blob)]


def wide_runs(blob: bytes) -> list[str]:
    out: list[str] = []
    for match in WIDE_RUN.finditer(blob):
        text = match.group().decode("utf-16-le", "replace")
        out.append(text)
    return out


def dump_stream(blob: bytes) -> str:
    """Printable text from a stream, one entry per line, ASCII first then wide."""
    lines = ascii_runs(blob)
    lines.extend(wide_runs(blob))
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("label", help="path to the .lbl file")
    parser.add_argument("--outdir", help="write one text file per stream into this directory")
    parser.add_argument("--grep", action="append", default=[], help="print streams and lines matching this text (repeatable)")
    parser.add_argument("--keywords", action="store_true", help="count hits for the built-in keyword set")
    parser.add_argument("--min-run", type=int, default=4, help="shortest printable run to keep (default 4)")
    args = parser.parse_args(argv)

    if not olefile.isOleFile(args.label):
        print(f"NOT_AN_OLE2_CONTAINER: {args.label}", file=sys.stderr)
        return 2

    container = olefile.OleFileIO(args.label)
    try:
        entries = container.listdir(streams=True, storages=True)
        embedded_object = container.exists("\x01Ole10Native")
        print(f"file            : {args.label}")
        print(f"embedded object : {'yes' if embedded_object else 'no'}")
        print(f"streams/storages: {len(entries)}")
        print("-" * 72)

        texts: dict[str, str] = {}
        for entry in entries:
            key = "/".join(entry)
            if container.get_type(entry) != olefile.STGTY_STREAM:
                print(f"  [storage] {key}")
                continue
            try:
                blob = container.openstream(entry).read()
            except Exception as exc:  # stream may be unreadable / not a stream we can open
                print(f"  [unreadable] {key}: {exc}", file=sys.stderr)
                continue
            text = dump_stream(blob)
            texts[key] = text
            print(f"  [stream ] {key:52} {len(blob):>9,} bytes, {len(text.splitlines()):>5} text lines")

        if args.outdir:
            os.makedirs(args.outdir, exist_ok=True)
            for key, text in texts.items():
                # Stream names can begin with control characters (the OLE property streams start with \x05),
                # which Windows rejects in a file name.
                safe = re.sub(r"[^0-9A-Za-z._-]", "_", key)
                target = os.path.join(args.outdir, safe + ".txt")
                with open(target, "w", encoding="utf-8") as handle:
                    handle.write(text)
            print("-" * 72)
            print(f"dumped {len(texts)} stream(s) to {args.outdir}")

        if args.keywords:
            print("-" * 72)
            blob_all = "\n".join(texts.values())
            for word in KEYWORDS:
                count = len(re.findall(re.escape(word), blob_all, re.IGNORECASE))
                if count:
                    print(f"  {word:<16} {count}")

        if args.grep:
            print("-" * 72)
            for word in args.grep:
                print(f"== {word}")
                pattern = re.compile(re.escape(word), re.IGNORECASE)
                for key, text in texts.items():
                    for number, line in enumerate(text.splitlines(), 1):
                        if pattern.search(line):
                            print(f"  {key}:{number}: {line[:200]}")
    finally:
        container.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
