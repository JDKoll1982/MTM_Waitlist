#!/usr/bin/env python3
"""Compose the print-ready workflow PDFs from the recovered sheet specs.

The `.dot` files are the canonical graph description, but Graphviz cannot honour this
repository's print contract: exact US Letter landscape (792 x 612 pt), a 0.5 in clear margin,
a title header and shape legend on every page, 12-16 shapes per sheet and a guaranteed
>= 9.5 pt type size. So each page is drawn here instead, on an explicit swimlane grid:

    lane columns (UI -> ViewModel -> Service -> Store) x row index, in spec order.

That keeps the reading order the spec author wrote, wraps every label by measurement rather
than by character count, and never scales type below the floor.

    python docs/diagrams/tools/render_sheets.py docs/diagrams/tools/manifest.json
    python docs/diagrams/tools/verify_pdfs.py

Requires PyMuPDF (`pip install pymupdf`) and the Segoe UI face that ships with Windows.
"""
import json
import math
import pathlib
import sys

import pymupdf as fitz

PAGE_W, PAGE_H = 792.0, 612.0        # 11 x 8.5 in, landscape
MARGIN = 36.0                        # 0.5 in
HEADER_H = 15.0
LEGEND_H = 19.0
LIVE = fitz.Rect(MARGIN, MARGIN + HEADER_H, PAGE_W - MARGIN, PAGE_H - MARGIN - LEGEND_H)

OUT = pathlib.Path(__file__).resolve().parent.parent
FONT_DIR = pathlib.Path(r"C:\Windows\Fonts")
FONT, FONT_FILE = "SegoeUI", FONT_DIR / "segoeui.ttf"
FONT_B, FONT_B_FILE = "SegoeUI-Bold", FONT_DIR / "segoeuib.ttf"
FACE = fitz.Font(fontfile=str(FONT_FILE))

MIN_SIZE = 9.5
INK = "#111111"
GREY = "#6B737D"

FILL = {
    "ui": "#DCE9FF", "vm": "#EFE3FF", "dec": "#FFF1C9", "svc": "#D6F5F0",
    "db": "#EDEDED", "erp": "#F2E8D5", "mirror": "#FFE0E3", "note": "#FFF1C9",
    "refactor": "#FFD6D6", "conn": "#E8F0FF",
}
BORDER = {
    "ui": "#111111", "vm": "#111111", "dec": "#B7791F", "svc": "#111111",
    "db": "#111111", "erp": "#8A6D3B", "mirror": "#B02A37", "note": "#B7791F",
    "refactor": "#C62828", "conn": "#1F6FEB",
}
LANE_COLOR = {
    "ui": ("#0B3D91", "#1F6FEB"), "vm": ("#4B2A8A", "#8250DF"),
    "svc": ("#0A5A4F", "#0F7B6C"), "data": ("#5A4300", "#8A6D3B"),
}
LANE_ORDER = ["ui", "vm", "svc", "data"]

EDGE = {
    "": "#212A2E", "dash": "#B7791F", "vdash": "#8250DF", "mirror": "#B02A37",
    "warnlink": "#B7791F", "link": "#C62828", "connlink": "#1F6FEB",
    "red": "#C62828", "dots": "#6B737D",
}
DASHED_EDGE = {"dash", "vdash", "mirror", "warnlink", "link", "connlink"}
NO_ARROW = {"link", "warnlink", "connlink"}

LEGEND = [
    ("View (XAML)", "ui"), ("ViewModel", "vm"), ("Decision", "dec"), ("Service", "svc"),
    ("Procedure", "db"), ("Infor Visual", "erp"), ("Cache mirror", "mirror"),
    ("[REFACTOR]", "refactor"), ("Off-page", "conn"),
]


def rgb(hex_colour):
    value = hex_colour.lstrip("#")
    return tuple(int(value[i:i + 2], 16) / 255 for i in (0, 2, 4))


def tw(text, size):
    return FACE.text_length(text, fontsize=size)


def wrap(label, width, size):
    """Break a label to fit `width`, never mid-word unless a single word is itself too wide."""
    out = []
    for line in label.split("\n"):
        if tw(line, size) <= width:
            out.append(line)
            continue
        current = ""
        for word in line.split(" "):
            trial = word if not current else current + " " + word
            if tw(trial, size) <= width:
                current = trial
                continue
            if current:
                out.append(current)
            while tw(word, size) > width and len(word) > 1:
                cut = max(1, int(len(word) * width / tw(word, size)))
                while cut > 1 and tw(word[:cut], size) > width:
                    cut -= 1
                out.append(word[:cut])
                word = word[cut:]
            current = word
        if current:
            out.append(current)
    return out


class Sheet:
    def __init__(self, spec):
        self.spec = spec
        self.lanes = {}
        for key, nodes in spec["lanes"].items():
            if not nodes:
                continue
            cleaned = [self._clean(node) for node in nodes]
            soft = [node for node in cleaned if node[2] in ("note", "refactor", "conn")]
            hard = [node for node in cleaned if node[2] not in ("note", "refactor", "conn")]
            self.lanes[key] = hard + soft
        self.columns = [lane for lane in LANE_ORDER if lane in self.lanes]
        self.rows = max(len(nodes) for nodes in self.lanes.values())
        self.used_labels = []
        self.boxes = []

    @staticmethod
    def _clean(node):
        node_id, label, kind = node
        for bad, good in (("\\n", "\n"), ("「", '"'), ("」", '"'), ("＋", "+"),
                          ("→", "->"), ("…", "...")):
            label = label.replace(bad, good)
        return (node_id, label, kind)

    def layout(self, size, line_h, pad_x, pad_y, gutter, row_gap):
        available = LIVE.width - 8 - gutter * (len(self.columns) - 1)
        desired = {}
        for lane in self.columns:
            widest = max(max(tw(part, size) for part in node[1].split("\n"))
                         for node in self.lanes[lane])
            desired[lane] = max(widest + 2 * pad_x, 90.0)
        total_desired = sum(desired.values())
        lane_w = {
            lane: (desired[lane] * available / total_desired
                   if total_desired > available else desired[lane])
            for lane in self.columns
        }
        row_h = []
        for row in range(self.rows):
            tallest = 0.0
            for lane in self.columns:
                if len(self.lanes[lane]) <= row:
                    continue
                lines = wrap(self.lanes[lane][row][1], lane_w[lane] - 2 * pad_x, size)
                tallest = max(tallest, len(lines) * line_h + 2 * pad_y)
            row_h.append(tallest)
        total_w = sum(lane_w.values()) + gutter * (len(self.columns) - 1)
        total_h = sum(row_h) + row_gap * (self.rows - 1)
        return total_w, total_h, lane_w, row_h

    def fit(self):
        """Largest type size that still fits the live area; never below the 9.5 pt floor."""
        labels = [edge[2].replace("\n", " ") for edge in self.spec["edges"]
                  if len(edge) > 2 and edge[2]]
        widest = max((tw(text, 10.0) for text in labels), default=0.0)
        chosen = None
        for size in (11.0, 10.75, 10.5, 10.25, 10.0, 9.75, 9.5):
            line_h = size * 1.26
            gutter = max(40.0, widest + 12.0)
            row_gap = max(14.0, size * 1.7)
            geo = (size, line_h, 7.0, 4.0, gutter, row_gap) + self.layout(
                size, line_h, 7.0, 4.0, gutter, row_gap)
            chosen = geo
            if geo[6] <= LIVE.width - 4 and geo[7] <= LIVE.height - 4:
                return geo
        return chosen

    def draw(self, page, geo):
        size, line_h, pad_x, pad_y, gutter, row_gap, total_w, total_h, lane_w, row_h = geo
        x0 = LIVE.x0 + (LIVE.width - total_w) / 2
        y0 = LIVE.y0 + 26.0
        leftover = (LIVE.y1 - 6) - (y0 + total_h)
        if leftover > 0 and self.rows > 1:
            extra = min(leftover / (self.rows - 1), 60.0)
            row_h = list(row_h)
            row_gap += extra
            total_h += extra * (self.rows - 1)

        band, x = {}, x0
        for lane in self.columns:
            band[lane] = (x, x + lane_w[lane])
            x += lane_w[lane] + gutter

        for lane in self.columns:
            bx0, bx1 = band[lane]
            title_colour, border_colour = LANE_COLOR[lane]
            page.draw_rect(fitz.Rect(bx0 - 6, y0 - 24, bx1 + 6, y0 + total_h + 6),
                           color=rgb(border_colour), width=0.9, dashes="[3 3] 0", radius=0.15)
            title = self.spec.get("lane_titles", {}).get(lane, lane)
            page.insert_text((bx0 + 2, y0 - 10), title, fontsize=9.6, fontname=FONT_B,
                             fontfile=str(FONT_B_FILE), color=rgb(title_colour))

        boxes, y = {}, y0
        for row in range(self.rows):
            for lane in self.columns:
                if len(self.lanes[lane]) <= row:
                    continue
                node_id, label, kind = self.lanes[lane][row]
                lines = wrap(label, lane_w[lane] - 2 * pad_x, size)
                height = len(lines) * line_h + 2 * pad_y
                cx = (band[lane][0] + band[lane][1]) / 2
                top = y + (row_h[row] - height) / 2
                box = fitz.Rect(cx - lane_w[lane] / 2, top, cx + lane_w[lane] / 2, top + height)
                boxes[node_id] = box
                self._box(page, box, kind, lines, size, line_h)
            y += row_h[row] + row_gap

        self.boxes = list(boxes.values())
        for edge in self.spec["edges"]:
            a, b, label, style = (list(edge) + ["", ""])[:4]
            if a in boxes and b in boxes:
                self._arrow(page, boxes[a], boxes[b], label, style, size)

    def _box(self, page, box, kind, lines, size, line_h):
        if kind == "dec":
            mid = (box.y0 + box.y1) / 2
            cx = (box.x0 + box.x1) / 2
            half = min(box.width / 2, 62.0)
            points = [fitz.Point(cx, box.y0), fitz.Point(cx + half, mid),
                      fitz.Point(cx, box.y1), fitz.Point(cx - half, mid)]
            page.draw_polyline(points + [points[0]], color=rgb(BORDER[kind]),
                               fill=rgb(FILL[kind]), width=1.5, closePath=True)
        else:
            dashed = "[3 2] 0" if kind in ("note", "refactor", "conn") else None
            page.draw_rect(box, color=rgb(BORDER[kind]), fill=rgb(FILL[kind]),
                           width=1.5, dashes=dashed, radius=0.18)
            if kind == "db":
                page.draw_line(fitz.Point(box.x0 + 3, box.y0 + 7),
                               fitz.Point(box.x1 - 3, box.y0 + 7),
                               color=rgb(BORDER[kind]), width=0.9)

        text_y = box.y0 + (box.height - len(lines) * line_h) / 2 + line_h * 0.76
        for line in lines:
            width = tw(line, size)
            page.insert_text((box.x0 + (box.width - width) / 2, text_y),
                             line, fontsize=size, fontname=FONT, fontfile=str(FONT_FILE),
                             color=rgb(INK))
            text_y += line_h

    def _arrow(self, page, src, dst, label, style, size):
        colour = rgb(EDGE.get(style, "#212A2E"))
        dashed = "[3 3] 0" if style in DASHED_EDGE else None
        width = 2.0 if style == "red" else 1.2
        arrow = style not in NO_ARROW

        if abs((src.x0 + src.x1) - (dst.x0 + dst.x1)) < 4:
            if style in NO_ARROW:
                return          # a same-column note link would only cross the boxes between them
            start = fitz.Point((src.x0 + src.x1) / 2, src.y1 + 1)
            end = fitz.Point((dst.x0 + dst.x1) / 2, dst.y0 - 1)
            page.draw_line(start, end, color=colour, width=width, dashes=dashed)
            if arrow:
                self._head(page, start, end, colour)
            if label:
                self._label(page, label, start.x + 5, (start.y + end.y) / 2 + 3, size, colour,
                            anchor="left")
            return

        right = dst.x0 > src.x1
        sx = src.x1 + 1 if right else src.x0 - 1
        ex = dst.x0 - 1 if right else dst.x1 + 1
        sy, ey = (src.y0 + src.y1) / 2, (dst.y0 + dst.y1) / 2
        mid = (sx + ex) / 2
        p1, p2 = fitz.Point(sx, sy), fitz.Point(mid, sy)
        p3, p4 = fitz.Point(mid, ey), fitz.Point(ex, ey)
        for start, end in ((p1, p2), (p2, p3), (p3, p4)):
            page.draw_line(start, end, color=colour, width=width, dashes=dashed)
        if arrow:
            self._head(page, p3, p4, colour)
        if label:
            self._label(page, label, mid, (sy + ey) / 2 + 3, size, colour, anchor="middle")

    def _label(self, page, text, cx, cy, size, colour, anchor="middle"):
        text = text.replace("\n", " ")
        label_size = max(MIN_SIZE, size - 1)
        width = tw(text, label_size)
        height = label_size + 2
        for nudge in (0, 11, -11, 22, -22, 33, -33, 44, -44):
            if anchor == "middle":
                rect = fitz.Rect(cx - width / 2 - 1.5, cy - height + 2 + nudge,
                                 cx + width / 2 + 1.5, cy + 2 + nudge)
            else:
                rect = fitz.Rect(cx - 1.5, cy - height + 2 + nudge,
                                 cx + width + 1.5, cy + 2 + nudge)
            if not any(rect.intersects(used) for used in self.used_labels) and not any(
                    rect.intersects(box) for box in self.boxes):
                break
        self.used_labels.append(rect)
        page.draw_rect(rect, color=None, fill=(1, 1, 1), width=0)
        page.insert_text((rect.x0 + 1.5, rect.y1 - 2), text, fontsize=label_size,
                         fontname=FONT, fontfile=str(FONT_FILE), color=colour)

    @staticmethod
    def _head(page, start, end, colour):
        angle = math.atan2(end.y - start.y, end.x - start.x)
        left = fitz.Point(end.x - 6.5 * math.cos(angle - 0.42), end.y - 6.5 * math.sin(angle - 0.42))
        right = fitz.Point(end.x - 6.5 * math.cos(angle + 0.42), end.y - 6.5 * math.sin(angle + 0.42))
        page.draw_polyline([end, left, right, end], color=colour, fill=colour, width=0.9)


def header(page, title, sheet_label):
    page.insert_text((MARGIN, 46.5), title, fontsize=9.8, fontname=FONT_B,
                     fontfile=str(FONT_B_FILE), color=rgb(INK))
    width = tw(sheet_label, 9.5)
    page.insert_text((PAGE_W - MARGIN - width, 46.5), sheet_label, fontsize=9.5,
                     fontname=FONT, fontfile=str(FONT_FILE), color=rgb("#4A4A4A"))
    page.draw_line(fitz.Point(MARGIN, 49), fitz.Point(PAGE_W - MARGIN, 49),
                   color=rgb(GREY), width=0.6)


def legend(page):
    y = PAGE_H - MARGIN - LEGEND_H
    page.draw_line(fitz.Point(MARGIN, y - 1.5), fitz.Point(PAGE_W - MARGIN, y - 1.5),
                   color=rgb(GREY), width=0.6)
    slot = (PAGE_W - 2 * MARGIN) / len(LEGEND)
    for index, (text, kind) in enumerate(LEGEND):
        left = MARGIN + slot * index
        page.draw_rect(fitz.Rect(left + 3, y + 4.5, left + 14, y + 14.5),
                       color=rgb(BORDER[kind]), fill=rgb(FILL[kind]), width=0.9, radius=0.15)
        page.insert_text((left + 17, y + 13), text, fontsize=9.5, fontname=FONT,
                         fontfile=str(FONT_FILE), color=rgb(INK))


def build(manifest_path):
    manifest_path = pathlib.Path(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    written = []
    for document in manifest["documents"]:
        spec = json.loads((manifest_path.parent / document["spec"]).read_text(encoding="utf-8"))
        sheets = spec["sheets"]
        for index, sheet in enumerate(sheets, 1):
            sheet["sheet"] = f"Sheet {index} of {len(sheets)}"
        pdf = fitz.open()
        for sheet in sheets:
            page = pdf.new_page(width=PAGE_W, height=PAGE_H)
            header(page, sheet["title"], sheet["sheet"])
            drawing = Sheet(sheet)
            drawing.draw(page, drawing.fit())
            legend(page)
        target = OUT / document["output"]
        pdf.save(str(target), garbage=4, deflate=True)
        pdf.close()
        written.append((target.name, len(sheets)))
    return written


if __name__ == "__main__":
    source = sys.argv[1] if len(sys.argv) > 1 else pathlib.Path(__file__).parent / "manifest.json"
    for name, pages in build(source):
        print(f"{name}: {pages} page(s)")
