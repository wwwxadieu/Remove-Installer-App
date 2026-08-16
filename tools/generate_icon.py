#!/usr/bin/env python3
"""Generates Assets/app.ico from the same shapes as Assets/logo.svg.

No SVG rasterizer (Inkscape/ImageMagick/cairosvg/Pillow) is available in this
environment, so the icon is drawn procedurally: each pixel is classified
against the same rectangles/circle/lines used in logo.svg, using only the
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
WINDOW = dict(x0=26 / 256, y0=51 / 256, x1=200 / 256, y1=220 / 256, r_top=23 / 256, r_bottom=23 / 256)
TITLEBAR = dict(x0=26 / 256, y0=51 / 256, x1=200 / 256, y1=86 / 256, r_top=23 / 256, r_bottom=0.0)
BADGE = dict(cx=205 / 256, cy=205 / 256, r=61 / 256)
X_MARK_STROKE = 12 / 256
X_MARK_LINES = (
    ((183 / 256, 183 / 256), (227 / 256, 227 / 256)),
    ((227 / 256, 183 / 256), (183 / 256, 227 / 256)),
)

WINDOW_COLOR = (0x3B, 0x6F, 0xE0, 0xFF)
TITLEBAR_COLOR = (0x1E, 0x4F, 0xBF, 0xFF)
BADGE_COLOR = (0xE1, 0x36, 0x36, 0xFF)
X_COLOR = (0xFF, 0xFF, 0xFF, 0xFF)
TRANSPARENT = (0, 0, 0, 0)

SUPERSAMPLE = 3  # per-axis; 3x3 = 9 samples per pixel for anti-aliased edges
SIZES = (16, 32, 48, 64, 128, 256)


def rounded_rect_sdf(u: float, v: float, x0: float, y0: float, x1: float, y1: float, r_top: float, r_bottom: float) -> float:
    r = r_top if v < (y0 + y1) / 2 else r_bottom
    qx = min(max(u, x0 + r), x1 - r)
    qy = min(max(v, y0 + r), y1 - r)
    return math.hypot(u - qx, v - qy) - r


def point_segment_distance(px: float, py: float, ax: float, ay: float, bx: float, by: float) -> float:
    abx, aby = bx - ax, by - ay
    length_sq = abx * abx + aby * aby
    t = 0.0 if length_sq == 0 else max(0.0, min(1.0, ((px - ax) * abx + (py - ay) * aby) / length_sq))
    cx, cy = ax + t * abx, ay + t * aby
    return math.hypot(px - cx, py - cy)


def shape_color(u: float, v: float) -> tuple[int, int, int, int]:
    """Topmost shape wins: X mark, then the delete badge, then the titlebar, then the window body."""
    half_stroke = X_MARK_STROKE / 2
    for (ax, ay), (bx, by) in X_MARK_LINES:
        if point_segment_distance(u, v, ax, ay, bx, by) <= half_stroke:
            if math.hypot(u - BADGE["cx"], v - BADGE["cy"]) <= BADGE["r"]:
                return X_COLOR

    if math.hypot(u - BADGE["cx"], v - BADGE["cy"]) <= BADGE["r"]:
        return BADGE_COLOR

    if rounded_rect_sdf(u, v, **TITLEBAR) <= 0:
        return TITLEBAR_COLOR

    if rounded_rect_sdf(u, v, **WINDOW) <= 0:
        return WINDOW_COLOR

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

    out_path = Path(__file__).resolve().parent.parent / "src" / "RemoveInstallerApp" / "Assets" / "app.ico"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(ico_bytes)
    print(f"Wrote {out_path} ({len(ico_bytes)} bytes, sizes {SIZES})")


if __name__ == "__main__":
    main()
