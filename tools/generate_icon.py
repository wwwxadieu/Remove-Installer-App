#!/usr/bin/env python3
"""Generates Assets/app.ico from the same shapes as Assets/logo.svg.

No SVG rasterizer (Inkscape/ImageMagick/cairosvg/Pillow) is available in this
environment, so the icon is drawn procedurally: each pixel is classified
against the same rectangles/polygons/circle used in logo.svg, using only the
Python standard library (zlib for PNG compression). Re-run this script after
editing logo.svg's shapes to keep app.ico in sync.

Usage: python3 tools/generate_icon.py
"""

from __future__ import annotations

import math
import struct
import zlib
from pathlib import Path

# Shape coordinates, as fractions of the 256x256 logo.svg viewBox, so this
# script and the SVG describe the same logo from a single set of numbers.
TILE = dict(x0=15 / 256, y0=15 / 256, x1=241 / 256, y1=241 / 256, r=50 / 256)
TILE_TOP = (0x6F, 0xA8, 0xF5)
TILE_BOTTOM = (0x2F, 0x6F, 0xE0)

HIGHLIGHT = dict(cx=128 / 256, cy=68 / 256, rx=90 / 256, ry=38 / 256)

LEFT_FLAP = ((77 / 256, 118 / 256), (109 / 256, 118 / 256), (66 / 256, 87 / 256))
RIGHT_FLAP = ((147 / 256, 118 / 256), (179 / 256, 118 / 256), (190 / 256, 87 / 256))
FLAP_FILL = (0xEF, 0xE9, 0xDA)
FLAP_BORDER = (0xC9, 0xC2, 0xAE)
FLAP_STROKE = 3 / 256

BOX = dict(x0=77 / 256, y0=118 / 256, x1=179 / 256, y1=190 / 256, r=8 / 256)
BOX_FILL = (0xF5, 0xF2, 0xE9)
BOX_BORDER = (0xC9, 0xC2, 0xAE)
BOX_STROKE = 3 / 256
FOLD_LINE = ((77 / 256, 133 / 256), (179 / 256, 133 / 256))
FOLD_STROKE = 2 / 256

ARROW_SHAFT = dict(x0=118 / 256, y0=68 / 256, x1=138 / 256, y1=110 / 256)
ARROW_HEAD = ((103 / 256, 110 / 256), (155 / 256, 110 / 256), (129 / 256, 138 / 256))
ARROW_COLOR = (0x1E, 0x4F, 0xBF)

BADGE = dict(cx=205 / 256, cy=205 / 256, r=48 / 256)
BADGE_COLOR = (0xE1, 0x36, 0x36)
X_STROKE = 10 / 256
X_LINES = (
    ((188 / 256, 188 / 256), (222 / 256, 222 / 256)),
    ((222 / 256, 188 / 256), (188 / 256, 222 / 256)),
)

TRANSPARENT = (0, 0, 0, 0)
SUPERSAMPLE = 3  # per-axis; 3x3 = 9 samples per pixel for anti-aliased edges
SIZES = (16, 32, 48, 64, 128, 256)


def rounded_rect_sdf(u: float, v: float, x0: float, y0: float, x1: float, y1: float, r: float) -> float:
    qx = min(max(u, x0 + r), x1 - r)
    qy = min(max(v, y0 + r), y1 - r)
    return math.hypot(u - qx, v - qy) - r


def point_segment_distance(px: float, py: float, ax: float, ay: float, bx: float, by: float) -> float:
    abx, aby = bx - ax, by - ay
    length_sq = abx * abx + aby * aby
    t = 0.0 if length_sq == 0 else max(0.0, min(1.0, ((px - ax) * abx + (py - ay) * aby) / length_sq))
    cx, cy = ax + t * abx, ay + t * aby
    return math.hypot(px - cx, py - cy)


def _tri_sign(px: float, py: float, ax: float, ay: float, bx: float, by: float) -> float:
    return (px - bx) * (ay - by) - (ax - bx) * (py - by)


def point_in_triangle(px: float, py: float, tri: tuple) -> bool:
    (ax, ay), (bx, by), (cx, cy) = tri
    d1 = _tri_sign(px, py, ax, ay, bx, by)
    d2 = _tri_sign(px, py, bx, by, cx, cy)
    d3 = _tri_sign(px, py, cx, cy, ax, ay)
    has_neg = d1 < 0 or d2 < 0 or d3 < 0
    has_pos = d1 > 0 or d2 > 0 or d3 > 0
    return not (has_neg and has_pos)


def triangle_edge_distance(px: float, py: float, tri: tuple) -> float:
    (ax, ay), (bx, by), (cx, cy) = tri
    return min(
        point_segment_distance(px, py, ax, ay, bx, by),
        point_segment_distance(px, py, bx, by, cx, cy),
        point_segment_distance(px, py, cx, cy, ax, ay),
    )


def lerp_color(a: tuple, b: tuple, t: float) -> tuple:
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def shape_color(u: float, v: float) -> tuple[int, int, int, int]:
    """Topmost shape wins: X mark, badge, arrow, box, flaps, then the tile background."""
    half_x = X_STROKE / 2
    if math.hypot(u - BADGE["cx"], v - BADGE["cy"]) <= BADGE["r"]:
        for (ax, ay), (bx, by) in X_LINES:
            if point_segment_distance(u, v, ax, ay, bx, by) <= half_x:
                return (255, 255, 255, 255)
        return (*BADGE_COLOR, 255)

    in_shaft = ARROW_SHAFT["x0"] <= u <= ARROW_SHAFT["x1"] and ARROW_SHAFT["y0"] <= v <= ARROW_SHAFT["y1"]
    if in_shaft or point_in_triangle(u, v, ARROW_HEAD):
        return (*ARROW_COLOR, 255)

    box_d = rounded_rect_sdf(u, v, BOX["x0"], BOX["y0"], BOX["x1"], BOX["y1"], BOX["r"])
    half_box_stroke = BOX_STROKE / 2
    if box_d <= half_box_stroke:
        if abs(box_d) <= half_box_stroke:
            return (*BOX_BORDER, 255)
        (fx0, fy0), (fx1, fy1) = FOLD_LINE
        if point_segment_distance(u, v, fx0, fy0, fx1, fy1) <= FOLD_STROKE / 2:
            return (*BOX_BORDER, 255)
        return (*BOX_FILL, 255)

    half_flap_stroke = FLAP_STROKE / 2
    for flap in (LEFT_FLAP, RIGHT_FLAP):
        if point_in_triangle(u, v, flap):
            if triangle_edge_distance(u, v, flap) <= half_flap_stroke:
                return (*FLAP_BORDER, 255)
            return (*FLAP_FILL, 255)

    tile_d = rounded_rect_sdf(u, v, TILE["x0"], TILE["y0"], TILE["x1"], TILE["y1"], TILE["r"])
    if tile_d <= 0:
        t = min(1.0, max(0.0, (v - TILE["y0"]) / (TILE["y1"] - TILE["y0"])))
        base = lerp_color(TILE_TOP, TILE_BOTTOM, t)

        ex = (u - HIGHLIGHT["cx"]) / HIGHLIGHT["rx"]
        ey = (v - HIGHLIGHT["cy"]) / HIGHLIGHT["ry"]
        if ex * ex + ey * ey <= 1:
            base = lerp_color(base, (255, 255, 255), 0.15)

        return (*base, 255)

    return TRANSPARENT


def render(size: int) -> bytes:
    """Returns raw RGBA pixel bytes, row-major, top-to-bottom."""
    pixels = bytearray(size * size * 4)
    offsets = [(i + 0.5) / SUPERSAMPLE for i in range(SUPERSAMPLE)]
    sample_count = SUPERSAMPLE * SUPERSAMPLE

    for y in range(size):
        for x in range(size):
            r = g = b = a = 0
            for oy in offsets:
                v = (y + oy) / size
                for ox in offsets:
                    u = (x + ox) / size
                    sr, sg, sb, sa = shape_color(u, v)
                    r += sr * sa
                    g += sg * sa
                    b += sb * sa
                    a += sa

            if a > 0:
                r = round(r / a)
                g = round(g / a)
                b = round(b / a)
                a = round(a / sample_count)
            else:
                a = 0

            idx = (y * size + x) * 4
            pixels[idx] = r
            pixels[idx + 1] = g
            pixels[idx + 2] = b
            pixels[idx + 3] = a

    return bytes(pixels)


def encode_png(rgba: bytes, size: int) -> bytes:
    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)  # 8-bit RGBA, no interlace

    raw = bytearray()
    stride = size * 4
    for y in range(size):
        raw.append(0)  # filter type 0 (none) per scanline
        raw.extend(rgba[y * stride:(y + 1) * stride])

    idat = zlib.compress(bytes(raw), 9)

    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", idat) + chunk(b"IEND", b"")


def build_ico(png_by_size: dict[int, bytes]) -> bytes:
    sizes = sorted(png_by_size)
    header = struct.pack("<HHH", 0, 1, len(sizes))

    entries = bytearray()
    offset = 6 + 16 * len(sizes)
    images = bytearray()
    for size in sizes:
        png = png_by_size[size]
        dim_byte = 0 if size >= 256 else size  # 0 means 256 in the ICO format
        entries += struct.pack(
            "<BBBBHHII",
            dim_byte, dim_byte,  # width, height
            0,  # color count (0 = no palette, true color)
            0,  # reserved
            1,  # color planes
            32,  # bits per pixel
            len(png),
            offset,
        )
        images += png
        offset += len(png)

    return bytes(header) + bytes(entries) + bytes(images)


def main() -> None:
    png_by_size = {size: encode_png(render(size), size) for size in SIZES}
    ico_bytes = build_ico(png_by_size)

    out_path = Path(__file__).resolve().parent.parent / "src" / "UnInstall" / "Assets" / "app.ico"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(ico_bytes)
    print(f"Wrote {out_path} ({len(ico_bytes)} bytes, sizes {SIZES})")


if __name__ == "__main__":
    main()
