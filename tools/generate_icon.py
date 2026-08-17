#!/usr/bin/env python3
"""Generates Assets/app.ico and the two feature-icon PNGs from the same shapes as
Assets/logo.svg.

No SVG rasterizer (Inkscape/ImageMagick/cairosvg/Pillow) is available in this
environment, so every icon is drawn procedurally: each pixel is classified
against rectangles/polygons/circles/bezier curves, using only the Python
standard library (zlib for PNG compression). Re-run this script after editing
logo.svg's shapes to keep the generated assets in sync.

Usage: python3 tools/generate_icon.py
"""

from __future__ import annotations

import math
import struct
import zlib
from pathlib import Path
from typing import Callable

# ---------------------------------------------------------------------------
# Shared geometry: the "ClearOut Primary - Dark Mode" trash-can + sweeping-arrow
# mark. Coordinates are fractions of a 256x256 viewBox, matching logo.svg, so
# this script and the SVG describe the same logo from a single set of numbers.
# The trash-can silhouette (CAN_BODY/LID/HANDLE) is reused as-is (solid fill for
# the main app icon, outline-only for the Scan/Cleanup feature icon).
# ---------------------------------------------------------------------------

TILE = dict(x0=15 / 256, y0=15 / 256, x1=241 / 256, y1=241 / 256, r=50 / 256)
TILE_TOP = (0x13, 0x8A, 0x96)  # lighter teal, top of the app-icon tile gradient
TILE_BOTTOM = (0x0C, 0x6D, 0x77)  # ClearOut Primary Dark Mode teal
HIGHLIGHT = dict(cx=128 / 256, cy=68 / 256, rx=90 / 256, ry=38 / 256)

LID = dict(x0=76 / 256, y0=90 / 256, x1=180 / 256, y1=102 / 256, r=5 / 256)
HANDLE = dict(x0=116 / 256, y0=76 / 256, x1=140 / 256, y1=92 / 256, r=4 / 256)
CAN_BODY = (
    (84 / 256, 102 / 256),   # top-left
    (172 / 256, 102 / 256),  # top-right
    (158 / 256, 198 / 256),  # bottom-right (tapered in)
    (98 / 256, 198 / 256),   # bottom-left (tapered in)
)
CAN_RIB_XS = (110 / 256, 128 / 256, 146 / 256)
CAN_RIB_Y0, CAN_RIB_Y1 = 115 / 256, 185 / 256
CAN_STROKE = 4 / 256
RIB_STROKE = 3 / 256

GOLD = (0xDD, 0xA1, 0x5E)
GOLD_DARK = (0x9F, 0x74, 0x44)  # can/lid/handle border + rib shading
ARROW_COLOR = GOLD

TEAL = (0x0C, 0x6D, 0x77)  # feature-icon glyph color (Scan/Cleanup, Maintenance)
LIGHT_GRAY = (0xE6, 0xE9, 0xEC)  # Maintenance icon tile background

TRANSPARENT = (0, 0, 0, 0)
SUPERSAMPLE = 3  # per-axis; 3x3 = 9 samples per pixel for anti-aliased edges
SIZES = (16, 32, 48, 64, 128, 256)
FEATURE_ICON_SIZE = 256


# ---------------------------------------------------------------------------
# Generic geometry helpers
# ---------------------------------------------------------------------------

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


def point_in_quad(px: float, py: float, quad: tuple) -> bool:
    a, b, c, d = quad
    return point_in_triangle(px, py, (a, b, c)) or point_in_triangle(px, py, (a, c, d))


def polygon_edge_distance(px: float, py: float, poly: tuple) -> float:
    n = len(poly)
    return min(
        point_segment_distance(px, py, poly[i][0], poly[i][1], poly[(i + 1) % n][0], poly[(i + 1) % n][1])
        for i in range(n)
    )


def lerp_color(a: tuple, b: tuple, t: float) -> tuple:
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def quad_bezier(t: float, p0: tuple, p1: tuple, p2: tuple) -> tuple:
    x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0]
    y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]
    return (x, y)


def make_arrowhead(tip_base: tuple, forward: tuple, length: float, back: float, half_width: float) -> tuple:
    """A triangle arrowhead whose tip extends `length` past `tip_base` along the
    unit vector `forward`, with a base flared `half_width` to each side (using
    the perpendicular of `forward`), pulled back `back` from `tip_base`."""
    fx, fy = forward
    px, py = -fy, fx  # perpendicular, unit length since forward is unit length
    tip = (tip_base[0] + fx * length, tip_base[1] + fy * length)
    base_cx, base_cy = tip_base[0] - fx * back, tip_base[1] - fy * back
    left = (base_cx + px * half_width, base_cy + py * half_width)
    right = (base_cx - px * half_width, base_cy - py * half_width)
    return (tip, left, right)


def angle_in_arc(angle_deg: float, start_deg: float, span_deg: float) -> bool:
    return (angle_deg - start_deg) % 360 <= span_deg


# ---------------------------------------------------------------------------
# Sweeping-arrow geometry (shared derived constants for the main app icon)
# ---------------------------------------------------------------------------

ARROW_P0 = (52 / 256, 45 / 256)
ARROW_P1 = (145 / 256, 55 / 256)
ARROW_P2 = (120 / 256, 97 / 256)
ARROW_STROKE = 12 / 256
_ARROW_SEGMENT_COUNT = 24
ARROW_POLYLINE = [quad_bezier(i / _ARROW_SEGMENT_COUNT, ARROW_P0, ARROW_P1, ARROW_P2) for i in range(_ARROW_SEGMENT_COUNT + 1)]

_arrow_tangent_dx = ARROW_P2[0] - ARROW_P1[0]
_arrow_tangent_dy = ARROW_P2[1] - ARROW_P1[1]
_arrow_tangent_len = math.hypot(_arrow_tangent_dx, _arrow_tangent_dy)
ARROW_FORWARD = (_arrow_tangent_dx / _arrow_tangent_len, _arrow_tangent_dy / _arrow_tangent_len)
ARROW_HEAD = make_arrowhead(ARROW_P2, ARROW_FORWARD, length=24 / 256, back=4 / 256, half_width=16 / 256)


# ---------------------------------------------------------------------------
# Main app icon: teal tile, gold trash can + sweeping arrow ("Primary - Dark Mode")
# ---------------------------------------------------------------------------

def main_icon_color(u: float, v: float) -> tuple[int, int, int, int]:
    """Topmost shape wins: arrow, handle, lid, can body (with ribs), then tile."""
    if point_in_triangle(u, v, ARROW_HEAD):
        return (*ARROW_COLOR, 255)

    half_arrow = ARROW_STROKE / 2
    for i in range(len(ARROW_POLYLINE) - 1):
        ax, ay = ARROW_POLYLINE[i]
        bx, by = ARROW_POLYLINE[i + 1]
        if point_segment_distance(u, v, ax, ay, bx, by) <= half_arrow:
            return (*ARROW_COLOR, 255)

    half_handle = CAN_STROKE / 2
    handle_d = rounded_rect_sdf(u, v, HANDLE["x0"], HANDLE["y0"], HANDLE["x1"], HANDLE["y1"], HANDLE["r"])
    if handle_d <= half_handle:
        return (*GOLD_DARK, 255) if abs(handle_d) <= half_handle else (*GOLD, 255)

    lid_d = rounded_rect_sdf(u, v, LID["x0"], LID["y0"], LID["x1"], LID["y1"], LID["r"])
    if lid_d <= half_handle:
        return (*GOLD_DARK, 255) if abs(lid_d) <= half_handle else (*GOLD, 255)

    if point_in_quad(u, v, CAN_BODY):
        half_can = CAN_STROKE / 2
        if polygon_edge_distance(u, v, CAN_BODY) <= half_can:
            return (*GOLD_DARK, 255)
        half_rib = RIB_STROKE / 2
        for rib_x in CAN_RIB_XS:
            if point_segment_distance(u, v, rib_x, CAN_RIB_Y0, rib_x, CAN_RIB_Y1) <= half_rib:
                return (*GOLD_DARK, 255)
        return (*GOLD, 255)

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


# ---------------------------------------------------------------------------
# Feature icon: Scan/Cleanup — teal-outlined trash can + magnifying glass,
# transparent background, wired into the Leftover Cleaner ("residue scan") tab.
# ---------------------------------------------------------------------------

MAGNIFIER = dict(cx=180 / 256, cy=78 / 256, r=32 / 256)
MAGNIFIER_STROKE = 10 / 256
_magnifier_angle = math.radians(45)
_magnifier_edge = (
    MAGNIFIER["cx"] + MAGNIFIER["r"] * math.cos(_magnifier_angle),
    MAGNIFIER["cy"] + MAGNIFIER["r"] * math.sin(_magnifier_angle),
)
MAGNIFIER_HANDLE_END = (
    _magnifier_edge[0] + 30 / 256 * math.cos(_magnifier_angle),
    _magnifier_edge[1] + 30 / 256 * math.sin(_magnifier_angle),
)
OUTLINE_STROKE = 8 / 256


def scan_cleanup_icon_color(u: float, v: float) -> tuple[int, int, int, int]:
    """Topmost shape wins: magnifying glass, then the trash-can outline."""
    half_mag = MAGNIFIER_STROKE / 2
    ring_d = math.hypot(u - MAGNIFIER["cx"], v - MAGNIFIER["cy"]) - MAGNIFIER["r"]
    if abs(ring_d) <= half_mag:
        return (*TEAL, 255)
    if point_segment_distance(u, v, _magnifier_edge[0], _magnifier_edge[1],
                               MAGNIFIER_HANDLE_END[0], MAGNIFIER_HANDLE_END[1]) <= half_mag:
        return (*TEAL, 255)

    half_outline = OUTLINE_STROKE / 2
    handle_d = rounded_rect_sdf(u, v, HANDLE["x0"], HANDLE["y0"], HANDLE["x1"], HANDLE["y1"], HANDLE["r"])
    if abs(handle_d) <= half_outline:
        return (*TEAL, 255)
    lid_d = rounded_rect_sdf(u, v, LID["x0"], LID["y0"], LID["x1"], LID["y1"], LID["r"])
    if abs(lid_d) <= half_outline:
        return (*TEAL, 255)
    if polygon_edge_distance(u, v, CAN_BODY) <= half_outline:
        return (*TEAL, 255)

    return TRANSPARENT


# ---------------------------------------------------------------------------
# Feature icon: Maintenance / "Deep Clean" — teal orbital double-arrow on a
# light-gray tile, wired into the Disk Cleanup tab.
# ---------------------------------------------------------------------------

MAINT_CENTER = (128 / 256, 128 / 256)
MAINT_RADIUS = 58 / 256
MAINT_STROKE = 18 / 256
MAINT_ARC_A_START, MAINT_ARC_A_SPAN = -60, 140
MAINT_ARC_B_START, MAINT_ARC_B_SPAN = 120, 140
MAINT_HEAD_LEN, MAINT_HEAD_BACK, MAINT_HEAD_HALF_WIDTH = 26 / 256, 6 / 256, 20 / 256


def _arc_point(angle_deg: float) -> tuple:
    rad = math.radians(angle_deg)
    return (MAINT_CENTER[0] + MAINT_RADIUS * math.cos(rad), MAINT_CENTER[1] + MAINT_RADIUS * math.sin(rad))


def _arc_arrowhead(end_angle_deg: float) -> tuple:
    rad = math.radians(end_angle_deg)
    forward = (-math.sin(rad), math.cos(rad))  # tangent, direction of increasing angle
    return make_arrowhead(_arc_point(end_angle_deg), forward, MAINT_HEAD_LEN, MAINT_HEAD_BACK, MAINT_HEAD_HALF_WIDTH)


MAINT_HEAD_A = _arc_arrowhead(MAINT_ARC_A_START + MAINT_ARC_A_SPAN)
MAINT_HEAD_B = _arc_arrowhead(MAINT_ARC_B_START + MAINT_ARC_B_SPAN)


def maintenance_icon_color(u: float, v: float) -> tuple[int, int, int, int]:
    """Topmost shape wins: the two arrowheads, then the two ring arcs, then the tile."""
    if point_in_triangle(u, v, MAINT_HEAD_A) or point_in_triangle(u, v, MAINT_HEAD_B):
        return (*TEAL, 255)

    r_dist = math.hypot(u - MAINT_CENTER[0], v - MAINT_CENTER[1])
    if abs(r_dist - MAINT_RADIUS) <= MAINT_STROKE / 2:
        angle_deg = math.degrees(math.atan2(v - MAINT_CENTER[1], u - MAINT_CENTER[0]))
        if angle_in_arc(angle_deg, MAINT_ARC_A_START, MAINT_ARC_A_SPAN) or \
                angle_in_arc(angle_deg, MAINT_ARC_B_START, MAINT_ARC_B_SPAN):
            return (*TEAL, 255)

    tile_d = rounded_rect_sdf(u, v, TILE["x0"], TILE["y0"], TILE["x1"], TILE["y1"], TILE["r"])
    if tile_d <= 0:
        return (*LIGHT_GRAY, 255)

    return TRANSPARENT


# ---------------------------------------------------------------------------
# Rendering / encoding (shared by every icon)
# ---------------------------------------------------------------------------

def render(shape_fn: Callable[[float, float], tuple[int, int, int, int]], size: int) -> bytes:
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
                    sr, sg, sb, sa = shape_fn(u, v)
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
    assets_dir = Path(__file__).resolve().parent.parent / "src" / "ClearOut" / "Assets"
    assets_dir.mkdir(parents=True, exist_ok=True)

    png_by_size = {size: encode_png(render(main_icon_color, size), size) for size in SIZES}
    ico_bytes = build_ico(png_by_size)
    ico_path = assets_dir / "app.ico"
    ico_path.write_bytes(ico_bytes)
    print(f"Wrote {ico_path} ({len(ico_bytes)} bytes, sizes {SIZES})")

    for name, shape_fn in (
        ("icon-scan-cleanup.png", scan_cleanup_icon_color),
        ("icon-maintenance.png", maintenance_icon_color),
    ):
        png_bytes = encode_png(render(shape_fn, FEATURE_ICON_SIZE), FEATURE_ICON_SIZE)
        out_path = assets_dir / name
        out_path.write_bytes(png_bytes)
        print(f"Wrote {out_path} ({len(png_bytes)} bytes, {FEATURE_ICON_SIZE}px)")


if __name__ == "__main__":
    main()
