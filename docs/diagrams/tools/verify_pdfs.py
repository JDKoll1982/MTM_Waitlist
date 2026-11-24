#!/usr/bin/env python3
"""Check every workflow PDF against the print contract.

Asserts, for every page:
  * page size is exactly 792 x 612 pt (11 x 8.5 in, landscape);
  * no text sits inside the 0.5 in margin band;
  * the smallest text is at least 9.5 pt.

Then writes a 300 DPI PNG of every page to a scratch folder for a visual look.

    python docs/diagrams/tools/verify_pdfs.py
"""
import pathlib
import sys

import pymupdf as fitz

PAGE_W, PAGE_H = 792.0, 612.0
MARGIN = 36.0
MIN_TEXT = 9.4          # the floor is 9.5 pt; the tolerance absorbs float rounding in the PDF

OUT = pathlib.Path(__file__).resolve().parent.parent
PNG = pathlib.Path.home() / "AppData/Local/Temp/mtm-diagrams/png"


def check(path, png_dir):
    problems = []
    pdf = fitz.open(path)
    smallest, largest = 99.0, 0.0
    for number, page in enumerate(pdf, 1):
        box = page.rect
        if abs(box.width - PAGE_W) > 0.01 or abs(box.height - PAGE_H) > 0.01:
            problems.append(f"page {number}: size {box.width}x{box.height} pt")

        for block in page.get_text("dict")["blocks"]:
            for line in block.get("lines", []):
                for span in line["spans"]:
                    x0, y0, x1, y1 = span["bbox"]
                    smallest = min(smallest, span["size"])
                    largest = max(largest, span["size"])
                    if (x0 < MARGIN - 0.5 or y0 < MARGIN - 0.5
                            or x1 > PAGE_W - MARGIN + 0.5 or y1 > PAGE_H - MARGIN + 0.5):
                        problems.append(
                            f"page {number}: text in margin at ({x0:.1f},{y0:.1f}) {span['text'][:32]!r}")

        if png_dir is not None:
            png_dir.mkdir(parents=True, exist_ok=True)
            page.get_pixmap(dpi=300).save(
                str(png_dir / f"{path.stem}-p{number}.png"))

    if smallest < MIN_TEXT:
        problems.append(f"smallest text {smallest:.1f} pt")
    return problems, smallest, largest


def main():
    png_dir = None if "--no-png" in sys.argv else PNG
    failed = 0
    for path in sorted(OUT.glob("*.pdf")):
        problems, smallest, largest = check(path, png_dir)
        status = "OK" if not problems else "FAIL"
        print(f"{path.name:48s} {status:4s} text {smallest:.1f}-{largest:.1f} pt")
        for problem in problems[:8]:
            print("   ", problem)
        failed += 1 if problems else 0
    if failed:
        sys.exit(f"{failed} document(s) failed the print contract")
    print(f"\nAll documents: {int(PAGE_W)}x{int(PAGE_H)} pt, text >= {MIN_TEXT} pt, "
          f"nothing in the {MARGIN / 72:.1f} in margin band.")


if __name__ == "__main__":
    main()
