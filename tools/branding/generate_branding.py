#!/usr/bin/env python3
"""
SharpShot branding generator - the single source of truth for the SharpShot mark
(Concept A: a geometric "S" built from two capture-frame brackets and two silver
ribbon segments) and for every asset derived from it.

Generates:
  assets/branding/*.svg, *.png                   vector + raster logo variants
  assets/icons/*.ico, *.png                      Windows icons (app + tray)
  src/SharpShot.App/Assets/*                     runtime assets (ico, notification logo, MSIX tiles)
  src/SharpShot.App/Components/LogoGeometry.cs   XAML path data (C# constants)

Requires: Python 3.10+, Pillow (pip install pillow) and, for the outlined wordmark,
fontTools (pip install fonttools) plus the Segoe UI Variable font (Windows 11).
Without fontTools/the font the horizontal lock-up falls back to <text> in SVG and
to a generic font in PNG.

Run from the repository root:  python tools/branding/generate_branding.py

All geometry comes from pixel measurements of the approved references in
public/assets (02 identity board, 04 app icon, 01 concept board); see
docs/design-system/branding.md for the anatomy and tools/branding/compare.py for
the visual validation sheet.
"""
from __future__ import annotations

import io
import math
import os
import struct
import sys
import warnings
from dataclasses import dataclass, replace
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
BRANDING = ROOT / "assets" / "branding"
ICONS = ROOT / "assets" / "icons"
APP_ASSETS = ROOT / "src" / "SharpShot.App" / "Assets"
APP_COMPONENTS = ROOT / "src" / "SharpShot.App" / "Components"

BOX = 256.0          # logical symbol box (the mark is authored inside a 256x256 square)
BOX_MARGIN = 20.0    # distance between the S bounding box and the symbol box (top/bottom)
TILE_RADIUS = 0.22   # app-icon tile corner radius, fraction of the tile size
TILE_MARK = 0.65     # S height inside the app-icon tile, fraction of the tile size
TILE_MARK_SMALL = 0.70  # same, for tiles of 32 px and below (optical compensation)

# Palette (see docs/design-system/branding.md)
BACKGROUND = "#0D1017"
SURFACE = "#1B2230"
PRIMARY = "#FFAA35"
PRIMARY_HOVER = "#FF7A00"
TEXT_PRIMARY = "#F5F7FA"
TEXT_SECONDARY = "#94A3B8"
SILVER_LIGHT = "#F5F7FA"
SILVER_DARK = "#C9D1DC"
SILVER_ON_LIGHT_A = "#94A3B8"
SILVER_ON_LIGHT_B = "#5B6B80"
SILVER_TRAY_A = "#D3DBE4"      # bare marks on unknown backgrounds (tray, 16 px): dimmer silver that
SILVER_TRAY_B = "#9AA6B6"      # still reads on a light taskbar
SILVER_EDGE_A = "#9AA5B4"      # ribbon thickness strip
SILVER_EDGE_B = "#6E7A8A"
TAGLINE_ON_LIGHT = "#5B6B80"
TILE_BORDER = "#2A3447"
TILE_HIGHLIGHT = "#4A5870"

SEGOE_VARIABLE = Path(r"C:\Windows\Fonts\SegUIVar.ttf")
WORDMARK_INSTANCE = "Bold Display"
TAGLINE_INSTANCE = "Regular"
TAGLINE = "Capturas de tela com máxima qualidade"

Point = tuple[float, float]


# --------------------------------------------------------------------------- geometry

@dataclass(frozen=True)
class MarkParams:
    """Anatomy of the mark, expressed relative to the S height (= 1.0) or to the arm
    thickness ("x arm"). Defaults are the measured values of the approved references.
    """
    arm: float = 0.164          # vertical arm thickness / S height (16 of 97 px, 17 of 104 px)
    bar: float = 0.90           # horizontal bar thickness, x arm (the bars are slightly thinner)
    aspect: float = 0.89        # S width / S height
    bar_len: float = 0.63       # bar length from the outer edge to the far top corner / S height
    bar_end_deg: float = 12.0   # slant of the bar's free end, degrees from vertical ("/" direction)
    outer_r: float = 1.15       # outer corner radius, x arm
    outer_n: float = 1.35       # superellipse exponent of the outer corner (2.0 = circle; <2 = flatter)
    inner_r: float = 0.35       # inner corner fillet radius, x arm (0 = sharp)
    cut_y: float = 0.47         # where the arm chamfer starts on the inner edge / S height
    cut_deg: float = 41.0       # chamfer angle, degrees from horizontal (silver ends are parallel)
    gap: float = 0.20           # gap between the chamfer and the silver piece, x arm
    silver_deg: float = 15.0    # slope of the silver ribbon, degrees from horizontal
    silver_t: float = 0.95      # silver ribbon thickness (perpendicular), x arm
    silver_len: float = 0.43    # silver ribbon top-edge length / S height
    silver_inset: float = 0.05  # x of the ribbon's near top corner, measured from the inner edge, x arm
    edge: float = 0.09          # darker "thickness" strip along the ribbon's lower edge, x arm


NORMAL_PARAMS = MarkParams()
# Optical variant for marks rendered below ~26 px: bolder strokes, no inner fillet,
# wider gaps so the four pieces stay separable on a 16/24 px taskbar icon.
SMALL_PARAMS = replace(NORMAL_PARAMS, arm=0.19, bar=0.95, outer_r=1.05, inner_r=0.0,
                       gap=0.32, silver_t=0.92, silver_len=0.41, silver_inset=0.0, edge=0.0)


def _rad(deg: float) -> float:
    return math.radians(deg)


class Geometry:
    """Builds the four shapes of the mark.

    Shapes are authored in "S space" (origin = top-left of the S bounding box,
    S height = 1.0). `box_layers()` maps them into the 256x256 symbol box with
    BOX_MARGIN above and below the S. The mark is point-symmetric: the bottom
    bracket and the upper silver piece are 180-degree rotations of the top bracket
    and the lower silver piece.
    """

    ARC_STEPS = 12

    def __init__(self, params: MarkParams = NORMAL_PARAMS) -> None:
        self.p = params

    # -- S-space shapes --------------------------------------------------------
    def top_bracket(self) -> list[Point]:
        p = self.p
        a = p.arm
        b = p.bar * a
        r = p.outer_r * a
        e = 2.0 / p.outer_n
        pts: list[Point] = []
        # Outer corner: superellipse from (0, r) up to (r, 0).
        for i in range(self.ARC_STEPS + 1):
            th = _rad(90.0 * i / self.ARC_STEPS)
            pts.append((r - r * math.cos(th) ** e, r - r * math.sin(th) ** e))
        # Top bar with its slanted free end.
        pts.append((p.bar_len, 0.0))
        pts.append((p.bar_len - b * math.tan(_rad(p.bar_end_deg)), b))
        # Inner corner (small concave fillet or sharp).
        ri = p.inner_r * a
        if ri > 0:
            for i in range(self.ARC_STEPS // 2 + 1):
                th = _rad(270.0 - 90.0 * i / (self.ARC_STEPS // 2))
                pts.append((a + ri + ri * math.cos(th), b + ri + ri * math.sin(th)))
        else:
            pts.append((a, b))
        # Arm, ending in the chamfer (inner edge -> outer edge).
        cy = p.cut_y
        pts.append((a, cy))
        pts.append((0.0, cy + a * math.tan(_rad(p.cut_deg))))
        return pts

    def lower_silver(self) -> list[Point]:
        p = self.p
        a = p.arm
        cut = _rad(p.cut_deg)
        slope = _rad(p.silver_deg)
        c0 = (a, p.cut_y)                                  # chamfer start (inner edge)
        d = (-math.cos(cut), math.sin(cut))                # along the chamfer, downwards
        n = (math.sin(cut), math.cos(cut))                 # perpendicular, towards the ribbon
        g = p.gap * a
        x0 = a + p.silver_inset * a
        lam = (c0[0] + g * n[0] - x0) / math.cos(cut)
        p0 = (x0, c0[1] + g * n[1] + lam * math.sin(cut))  # near top corner
        length = p.silver_len
        p1 = (p0[0] + length * math.cos(slope), p0[1] + length * math.sin(slope))
        end = p.silver_t * a / math.sin(cut + slope)
        p3 = (p0[0] + end * d[0], p0[1] + end * d[1])
        p2 = (p1[0] + end * d[0], p1[1] + end * d[1])
        return [p0, p1, p2, p3]

    def rotate(self, pts: list[Point]) -> list[Point]:
        cx, cy = self.p.aspect / 2.0, 0.5
        return [(2 * cx - x, 2 * cy - y) for x, y in pts]

    def upper_silver(self) -> list[Point]:
        return self.rotate(self.lower_silver())

    def bottom_bracket(self) -> list[Point]:
        return self.rotate(self.top_bracket())

    def layers(self) -> list[tuple[str, list[Point], str]]:
        """(name, S-space points, kind) of the four main shapes, in paint order."""
        return [
            ("TopBracket", self.top_bracket(), "orange"),
            ("LowerSilver", self.lower_silver(), "silver"),
            ("UpperSilver", self.upper_silver(), "silver"),
            ("BottomBracket", self.bottom_bracket(), "orange"),
        ]

    def edge_layers(self) -> list[tuple[str, list[Point], str]]:
        """Darker strips along the lower edge of each ribbon (the ribbon's visible
        thickness in the references). Optional detail: skipped in the monochrome
        variants and below ~26 px."""
        p = self.p
        cut = _rad(p.cut_deg)
        d = (-math.cos(cut), math.sin(cut))
        e = p.edge * p.arm / math.sin(cut + _rad(p.silver_deg))

        def strip(a: Point, b: Point) -> list[Point]:
            return [a, b, (b[0] - e * d[0], b[1] - e * d[1]), (a[0] - e * d[0], a[1] - e * d[1])]

        lo = self.lower_silver()
        up = self.upper_silver()
        return [("LowerSilverEdge", strip(lo[3], lo[2]), "edge"),
                ("UpperSilverEdge", strip(up[1], up[0]), "edge")]

    def painted_layers(self, depth: bool = True) -> list[tuple[str, list[Point], str]]:
        """All shapes in paint order (edges right after their ribbon)."""
        main = self.layers()
        if not depth or self.p.edge <= 0:
            return main
        edges = dict((n.replace("Edge", ""), (n, pts, k)) for n, pts, k in self.edge_layers())
        out = []
        for name, pts, kind in main:
            out.append((name, pts, kind))
            if name in edges:
                out.append(edges[name])
        return out

    def axis_lines(self) -> tuple[list[float], list[float]]:
        """S-space x and y coordinates of the axis-aligned edges (used for pixel snapping)."""
        p = self.p
        a, b, w = p.arm, p.bar * p.arm, p.aspect
        return [0.0, a, w - a, w], [0.0, b, 1.0 - b, 1.0]

    # -- symbol box ------------------------------------------------------------
    @property
    def s_height_in_box(self) -> float:
        return BOX - 2 * BOX_MARGIN

    def box_transform(self) -> tuple[float, float, float]:
        """(scale, dx, dy) mapping S space into the 256 box, S centred horizontally."""
        s = self.s_height_in_box
        return s, (BOX - self.p.aspect * s) / 2.0, BOX_MARGIN

    def to_box(self, pts: list[Point]) -> list[Point]:
        s, dx, dy = self.box_transform()
        return [(dx + x * s, dy + y * s) for x, y in pts]

    def box_layers(self, depth: bool = False) -> list[tuple[str, list[Point], str]]:
        return [(name, self.to_box(pts), kind) for name, pts, kind in self.painted_layers(depth)]


NORMAL = Geometry(NORMAL_PARAMS)
SMALL = Geometry(SMALL_PARAMS)


def geometry_for(mark_px: float) -> Geometry:
    """Optical size: the small variant for marks whose S height is below ~26 px."""
    return SMALL if mark_px < 26 else NORMAL


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16))


def path_data(points: list[Point]) -> str:
    return "M " + " L ".join(f"{x:.1f},{y:.1f}" for x, y in points) + " Z"


# --------------------------------------------------------------------------- text (fontTools)

class TextFace:
    """Glyph outlines of one named instance of a (variable) TrueType font."""

    def __init__(self, path: Path, instance_name: str, tracking: float = 0.0) -> None:
        from fontTools.ttLib import TTFont

        font = TTFont(str(path))
        location = None
        if "fvar" in font:
            names = font["name"]
            for inst in font["fvar"].instances:
                if names.getDebugName(inst.subfamilyNameID) == instance_name:
                    location = inst.coordinates
                    break
            if location is None:
                raise ValueError(f"instance {instance_name!r} not found in {path}")
        self.font = font
        self.upem = font["head"].unitsPerEm
        os2 = font["OS/2"]
        self.cap_height = getattr(os2, "sCapHeight", 0) or 0.7 * self.upem
        self.cmap = font.getBestCmap()
        # Variation applied through the glyph set (gvar + HVAR); the full instancer
        # fails on Segoe UI Variable's GPOS table.
        self.glyphset = font.getGlyphSet(location=location)
        self.tracking = tracking  # extra advance per glyph, in em

    def size_for_cap(self, cap_px: float) -> float:
        return cap_px * self.upem / self.cap_height

    def _glyphs(self, text: str) -> list[str]:
        return [self.cmap[ord(c)] for c in text]

    def _advance_units(self, glyph: str) -> float:
        return self.glyphset[glyph].width + self.tracking * self.upem

    def advance(self, text: str, size: float) -> float:
        return sum(self._advance_units(g) for g in self._glyphs(text)) * size / self.upem

    def svg_path(self, text: str, size: float, x: float, y: float) -> str:
        """SVG path data for `text` with its origin (baseline start) at (x, y)."""
        from fontTools.pens.svgPathPen import SVGPathPen
        from fontTools.pens.transformPen import TransformPen

        k = size / self.upem
        parts = []
        for g in self._glyphs(text):
            pen = SVGPathPen(self.glyphset, ntos=lambda v: f"{v:.1f}")
            self.glyphset[g].draw(TransformPen(pen, (k, 0, 0, -k, x, y)))
            cmd = pen.getCommands()
            if cmd:
                parts.append(cmd)
            x += self._advance_units(g) * k
        return " ".join(parts)

    def contours(self, text: str, size: float, x: float, y: float) -> list[list[list[Point]]]:
        """Flattened outlines in pixel space: one list of contours per glyph."""
        from fontTools.pens.basePen import BasePen

        class FlattenPen(BasePen):
            def __init__(self, glyphset, steps: int = 12):
                super().__init__(glyphset)
                self.steps = steps
                self.result: list[list[Point]] = []
                self._cur: list[Point] = []

            def _moveTo(self, p):
                self._cur = [p]

            def _lineTo(self, p):
                self._cur.append(p)

            def _curveToOne(self, p1, p2, p3):
                p0 = self._cur[-1]
                for i in range(1, self.steps + 1):
                    t = i / self.steps
                    m = 1 - t
                    self._cur.append((m ** 3 * p0[0] + 3 * m * m * t * p1[0] + 3 * m * t * t * p2[0] + t ** 3 * p3[0],
                                      m ** 3 * p0[1] + 3 * m * m * t * p1[1] + 3 * m * t * t * p2[1] + t ** 3 * p3[1]))

            def _qCurveToOne(self, p1, p2):
                p0 = self._cur[-1]
                for i in range(1, self.steps + 1):
                    t = i / self.steps
                    m = 1 - t
                    self._cur.append((m * m * p0[0] + 2 * m * t * p1[0] + t * t * p2[0],
                                      m * m * p0[1] + 2 * m * t * p1[1] + t * t * p2[1]))

            def _closePath(self):
                if len(self._cur) > 2:
                    self.result.append(self._cur)
                self._cur = []

            def _endPath(self):
                self._closePath()

        k = size / self.upem
        glyphs = []
        for g in self._glyphs(text):
            pen = FlattenPen(self.glyphset)
            self.glyphset[g].draw(pen)
            glyphs.append([[(x + px * k, y - py * k) for px, py in c] for c in pen.result])
            x += self._advance_units(g) * k
        return glyphs


_FACES: dict[str, TextFace | None] = {}
_TRACKING = {WORDMARK_INSTANCE: -0.02, TAGLINE_INSTANCE: 0.0}  # em; the reference wordmark is set tight


def text_face(instance: str) -> TextFace | None:
    if instance not in _FACES:
        face = None
        if SEGOE_VARIABLE.exists():
            try:
                face = TextFace(SEGOE_VARIABLE, instance, _TRACKING.get(instance, 0.0))
            except Exception as exc:  # fontTools missing or font unreadable
                warnings.warn(f"wordmark outlines unavailable ({exc}); falling back to <text>")
        else:
            warnings.warn("Segoe UI Variable not found; wordmark falls back to <text>")
        _FACES[instance] = face
    return _FACES[instance]


def draw_text_mask(mask: Image.Image, glyphs: list[list[list[Point]]]) -> None:
    """Fills flattened glyph outlines into an 'L' mask (outer contours - holes)."""
    draw = ImageDraw.Draw(mask)
    for contours in glyphs:
        if not contours:
            continue
        areas = []
        for c in contours:
            a = 0.0
            for (x0, y0), (x1, y1) in zip(c, c[1:] + c[:1]):
                a += x0 * y1 - x1 * y0
            areas.append(a)
        outer_sign = 1 if areas[max(range(len(areas)), key=lambda i: abs(areas[i]))] >= 0 else -1
        for c, a in zip(contours, areas):
            if (a >= 0) == (outer_sign > 0):
                draw.polygon(c, fill=255)
        for c, a in zip(contours, areas):
            if (a >= 0) != (outer_sign > 0):
                draw.polygon(c, fill=0)


# --------------------------------------------------------------------------- lock-up layout

@dataclass(frozen=True)
class Lockup:
    """Horizontal lock-up measured on reference 03 (S height = 97 px there)."""
    mark_left: float = 24.0            # S left edge in the canvas
    text_gap: float = 0.216            # S right edge -> wordmark, / S height
    wordmark_cap: float = 0.40         # wordmark cap height / S height
    wordmark_baseline: float = 0.546   # wordmark baseline below the S top / S height
    tagline_cap: float = 0.113         # tagline cap height / S height
    tagline_baseline: float = 0.845    # tagline baseline below the S top / S height
    right_pad: float = 0.15            # right padding / S height
    height: float = BOX


def lockup_metrics() -> dict[str, float]:
    lk = Lockup()
    s = NORMAL.s_height_in_box
    aspect = NORMAL.p.aspect
    word = text_face(WORDMARK_INSTANCE)
    tag = text_face(TAGLINE_INSTANCE)
    text_x = lk.mark_left + aspect * s + lk.text_gap * s
    word_size = word.size_for_cap(lk.wordmark_cap * s) if word else lk.wordmark_cap * s / 0.7
    tag_size = tag.size_for_cap(lk.tagline_cap * s) if tag else lk.tagline_cap * s / 0.7
    word_w = word.advance("SharpShot", word_size) if word else 2.72 * s
    tag_w = tag.advance(TAGLINE, tag_size) if tag else 2.7 * s
    width = text_x + max(word_w, tag_w) + lk.right_pad * s
    return {
        "mark_dx": lk.mark_left - NORMAL.box_transform()[1],   # symbol-box offset
        "text_x": text_x,
        "word_size": word_size,
        "word_baseline": BOX_MARGIN + lk.wordmark_baseline * s,
        "tag_size": tag_size,
        "tag_baseline": BOX_MARGIN + lk.tagline_baseline * s,
        "sharp_w": word.advance("Sharp", word_size) if word else word_w * 0.55,
        "width": math.ceil(width),
        "height": lk.height,
    }


# --------------------------------------------------------------------------- SVG

def _gradient_defs(silver: tuple[str, str]) -> str:
    return f"""  <defs>
    <linearGradient id="orange" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="{PRIMARY}"/>
      <stop offset="1" stop-color="{PRIMARY_HOVER}"/>
    </linearGradient>
    <linearGradient id="silver" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="{silver[0]}"/>
      <stop offset="1" stop-color="{silver[1]}"/>
    </linearGradient>
    <linearGradient id="silverEdge" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="{SILVER_EDGE_A}"/>
      <stop offset="1" stop-color="{SILVER_EDGE_B}"/>
    </linearGradient>
    <linearGradient id="tile" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{SURFACE}"/>
      <stop offset="1" stop-color="{BACKGROUND}"/>
    </linearGradient>
    <linearGradient id="tileHighlight" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{TILE_HIGHLIGHT}"/>
      <stop offset="0.35" stop-color="{TILE_BORDER}"/>
    </linearGradient>
  </defs>
"""


def svg_mark_paths(geo: Geometry, fill_orange: str, fill_silver: str, depth: bool = True,
                   indent: str = "    ") -> str:
    fills = {"orange": fill_orange, "silver": fill_silver, "edge": "url(#silverEdge)"}
    return "\n".join(
        f'{indent}<path id="{name}" d="{path_data(pts)}" fill="{fills[kind]}"/>'
        for name, pts, kind in geo.box_layers(depth))


def svg_symbol(geo: Geometry, *, silver: tuple[str, str] = (SILVER_LIGHT, SILVER_DARK),
               mono: str | None = None, tile: bool = False) -> str:
    """Symbol SVG. mono="#rrggbb" or "currentColor" gives the single-colour version."""
    defs = "" if mono else _gradient_defs(silver)
    fill_orange = mono or "url(#orange)"
    fill_silver = mono or "url(#silver)"
    tile_el = ""
    transform = ""
    if tile:
        r = TILE_RADIUS * BOX
        k = TILE_MARK * BOX / geo.s_height_in_box
        t = (BOX - BOX * k) / 2.0
        tile_el = (f'  <rect x="1" y="1" width="{BOX - 2:.0f}" height="{BOX - 2:.0f}" rx="{r:.1f}" '
                   f'fill="url(#tile)" stroke="url(#tileHighlight)" stroke-width="2"/>\n')
        transform = f' transform="translate({t:.2f} {t:.2f}) scale({k:.4f})"'
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {BOX:.0f} {BOX:.0f}" '
            f'width="{BOX:.0f}" height="{BOX:.0f}">\n{defs}{tile_el}  <g{transform}>\n'
            f'{svg_mark_paths(geo, fill_orange, fill_silver, depth=mono is None)}\n  </g>\n</svg>\n')


def svg_horizontal(dark: bool) -> str:
    m = lockup_metrics()
    text_primary = TEXT_PRIMARY if dark else BACKGROUND
    silver = (SILVER_LIGHT, SILVER_DARK) if dark else (SILVER_ON_LIGHT_A, SILVER_ON_LIGHT_B)
    tagline_color = TEXT_SECONDARY if dark else TAGLINE_ON_LIGHT
    bg = BACKGROUND if dark else TEXT_PRIMARY
    word = text_face(WORDMARK_INSTANCE)
    tag = text_face(TAGLINE_INSTANCE)
    if word and tag:
        text = (
            f'  <!-- Wordmark outlined from Segoe UI Variable ({WORDMARK_INSTANCE} / {TAGLINE_INSTANCE}) -->\n'
            f'  <path fill="{text_primary}" d="{word.svg_path("Sharp", m["word_size"], m["text_x"], m["word_baseline"])}"/>\n'
            f'  <path fill="{PRIMARY}" d="{word.svg_path("Shot", m["word_size"], m["text_x"] + m["sharp_w"], m["word_baseline"])}"/>\n'
            f'  <path fill="{tagline_color}" d="{tag.svg_path(TAGLINE, m["tag_size"], m["text_x"], m["tag_baseline"])}"/>\n')
    else:
        family = "Segoe UI Variable Display, Segoe UI, Arial, sans-serif"
        text = (
            f'  <text x="{m["text_x"]:.1f}" y="{m["word_baseline"]:.1f}" font-family="{family}" font-weight="700" '
            f'font-size="{m["word_size"]:.1f}" fill="{text_primary}">Sharp<tspan fill="{PRIMARY}">Shot</tspan></text>\n'
            f'  <text x="{m["text_x"]:.1f}" y="{m["tag_baseline"]:.1f}" font-family="{family}" font-weight="400" '
            f'font-size="{m["tag_size"]:.1f}" fill="{tagline_color}">{TAGLINE}</text>\n')
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {m["width"]} {m["height"]:.0f}" '
            f'width="{m["width"]}" height="{m["height"]:.0f}">\n{_gradient_defs(silver)}'
            f'  <rect width="{m["width"]}" height="{m["height"]:.0f}" fill="{bg}"/>\n'
            f'  <g transform="translate({m["mark_dx"]:.2f} 0)">\n'
            f'{svg_mark_paths(NORMAL, "url(#orange)", "url(#silver)")}\n  </g>\n{text}</svg>\n')


# --------------------------------------------------------------------------- raster

def gradient_image(size: tuple[int, int], c0: str, c1: str, diagonal: bool = True) -> Image.Image:
    w, h = max(size[0], 1), max(size[1], 1)
    r0, g0, b0 = hex_to_rgb(c0)
    r1, g1, b1 = hex_to_rgb(c1)
    n = 64
    img = Image.new("RGB", (n, n))
    px = img.load()
    denom = 2 * (n - 1) if diagonal else (n - 1)
    for y in range(n):
        for x in range(n):
            t = ((x + y) / denom) if diagonal else (y / denom)
            px[x, y] = (round(r0 + (r1 - r0) * t), round(g0 + (g1 - g0) * t), round(b0 + (b1 - b0) * t))
    return img.resize((w, h), Image.BILINEAR)


def fill_shape(out: Image.Image, pts: list[Point], colors: tuple[str, str] | None, mono: str | None) -> None:
    """Fills one polygon with its own diagonal gradient (SVG objectBoundingBox semantics)."""
    mask = Image.new("L", out.size, 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    x0, y0 = max(int(math.floor(min(xs))), 0), max(int(math.floor(min(ys))), 0)
    x1, y1 = min(int(math.ceil(max(xs))) + 1, out.width), min(int(math.ceil(max(ys))) + 1, out.height)
    if x1 <= x0 or y1 <= y0:
        return
    if mono:
        fill = Image.new("RGBA", (x1 - x0, y1 - y0), hex_to_rgb(mono) + (255,))
    else:
        fill = gradient_image((x1 - x0, y1 - y0), colors[0], colors[1]).convert("RGBA")
    out.paste(fill, (x0, y0), mask.crop((x0, y0, x1, y1)))


def draw_tile(out: Image.Image, canvas: int, ss: int) -> None:
    """Dark rounded-square tile: vertical gradient, 1 px border, lighter top edge."""
    radius = int(round(TILE_RADIUS * canvas))
    border = max(1, ss)  # 1 device pixel at the target size
    tile_mask = Image.new("L", (canvas, canvas), 0)
    ImageDraw.Draw(tile_mask).rounded_rectangle((0, 0, canvas - 1, canvas - 1), radius=radius, fill=255)
    out.paste(gradient_image((canvas, canvas), SURFACE, BACKGROUND, diagonal=False).convert("RGBA"),
              (0, 0), tile_mask)
    ring = Image.new("L", (canvas, canvas), 0)
    ImageDraw.Draw(ring).rounded_rectangle((0, 0, canvas - 1, canvas - 1), radius=radius, outline=255, width=border)
    border_fill = gradient_image((canvas, canvas), TILE_HIGHLIGHT, TILE_BORDER, diagonal=False)
    # highlight only over the top ~35 % of the tile, plain border colour below
    hl = Image.new("L", (canvas, canvas), 0)
    hl.paste(Image.linear_gradient("L").resize((canvas, int(canvas * 0.35))).transpose(Image.FLIP_TOP_BOTTOM), (0, 0))
    border_fill = Image.composite(border_fill, Image.new("RGB", (canvas, canvas), hex_to_rgb(TILE_BORDER)), hl)
    out.paste(border_fill.convert("RGBA"), (0, 0), ring)


def render_mark(size: int, geo: Geometry | None = None, *, tile: bool = False,
                silver: tuple[str, str] = (SILVER_LIGHT, SILVER_DARK), mono: str | None = None,
                supersample: int | None = None, snap: bool | None = None,
                mark_frac: float | None = None) -> Image.Image:
    """Renders the mark (optionally on the app-icon tile) as an RGBA image of `size` px."""
    if mark_frac is None:
        if tile:
            mark_frac = TILE_MARK if size > 32 else TILE_MARK_SMALL
        else:
            mark_frac = (BOX - 2 * BOX_MARGIN) / BOX
    mark_px = mark_frac * size
    if geo is None:
        geo = geometry_for(mark_px)
    if supersample is None:
        supersample = 16 if size <= 64 else 8
    if snap is None:
        snap = mark_px < 40
    ss = supersample
    canvas = size * ss
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    if tile:
        draw_tile(out, canvas, ss)

    # S space -> device pixels (target size), then -> supersampled canvas.
    s = mark_px
    dx = (size - geo.p.aspect * s) / 2.0
    dy = (size - s) / 2.0
    xl, yl = geo.axis_lines()

    def device(pt: Point) -> Point:
        x, y = dx + pt[0] * s, dy + pt[1] * s
        if snap:
            if any(abs(pt[0] - v) < 1e-6 for v in xl):
                x = math.floor(x + 0.5)
            if any(abs(pt[1] - v) < 1e-6 for v in yl):
                y = math.floor(y + 0.5)
        return x, y

    palette = {"orange": (PRIMARY, PRIMARY_HOVER), "silver": silver, "edge": (SILVER_EDGE_A, SILVER_EDGE_B)}
    for _, pts, kind in geo.painted_layers(depth=mono is None and mark_px >= 26):
        dev = [device(p) for p in pts]
        fill_shape(out, [(x * ss, y * ss) for x, y in dev], palette[kind], mono)
    out = out.resize((size, size), Image.LANCZOS)
    # Lanczos ringing can leave alpha of 1-5/255 just outside curved edges; keep
    # "no tile / no mark" pixels truly transparent (matters for the ICO AND mask).
    r, g, b, a = out.split()
    out = Image.merge("RGBA", (r, g, b, a.point(lambda v: 0 if v < 8 else v)))
    return out


def render_horizontal(dark: bool, height: int = 256) -> Image.Image:
    m = lockup_metrics()
    k = height / BOX
    width = int(round(m["width"] * k))
    bg = hex_to_rgb(BACKGROUND if dark else TEXT_PRIMARY) + (255,)
    img = Image.new("RGBA", (width, height), bg)
    silver = (SILVER_LIGHT, SILVER_DARK) if dark else (SILVER_ON_LIGHT_A, SILVER_ON_LIGHT_B)
    mark = render_mark(int(round(BOX * k)), NORMAL, tile=False, silver=silver, snap=False)
    img.alpha_composite(mark, (int(round(m["mark_dx"] * k)), 0))
    text_primary = hex_to_rgb(TEXT_PRIMARY if dark else BACKGROUND)
    tagline_color = hex_to_rgb(TEXT_SECONDARY if dark else TAGLINE_ON_LIGHT)
    word = text_face(WORDMARK_INSTANCE)
    tag = text_face(TAGLINE_INSTANCE)
    if word and tag:
        ss = 4
        runs = [
            (word, "Sharp", m["word_size"], m["text_x"], m["word_baseline"], text_primary),
            (word, "Shot", m["word_size"], m["text_x"] + m["sharp_w"], m["word_baseline"], hex_to_rgb(PRIMARY)),
            (tag, TAGLINE, m["tag_size"], m["text_x"], m["tag_baseline"], tagline_color),
        ]
        for face, text, size, x, y, color in runs:
            mask = Image.new("L", (width * ss, height * ss), 0)
            draw_text_mask(mask, face.contours(text, size * k * ss, x * k * ss, y * k * ss))
            mask = mask.resize((width, height), Image.LANCZOS)
            img.paste(Image.new("RGBA", (width, height), color + (255,)), (0, 0), mask)
    else:  # fallback: Pillow text with whatever font is available
        font = load_font(int(m["word_size"] * k))
        draw = ImageDraw.Draw(img)
        x, y = m["text_x"] * k, m["word_baseline"] * k
        draw.text((x, y), "Sharp", font=font, fill=text_primary, anchor="ls")
        draw.text((x + draw.textlength("Sharp", font=font), y), "Shot", font=font, fill=hex_to_rgb(PRIMARY), anchor="ls")
        draw.text((x, m["tag_baseline"] * k), TAGLINE, font=load_font(int(m["tag_size"] * k), bold=False),
                  fill=tagline_color, anchor="ls")
    return img


def load_font(size: int, bold: bool = True) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [(str(SEGOE_VARIABLE), (WORDMARK_INSTANCE,) if bold else (TAGLINE_INSTANCE,)),
                  (r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf", ()),
                  (r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf", ())]
    for path, variations in candidates:
        if os.path.exists(path):
            font = ImageFont.truetype(path, size)
            for name in variations:
                try:
                    font.set_variation_by_name(name)
                    break
                except Exception:
                    continue
            return font
    return ImageFont.load_default()


# --------------------------------------------------------------------------- ICO

def write_ico(path: Path, images: list[Image.Image]) -> None:
    """Writes a multi-size ICO. Sizes <= 48 use BMP entries (maximum compatibility),
    larger sizes use PNG entries (the standard for 64/128/256)."""
    entries: list[tuple[Image.Image, bytes]] = []
    for img in sorted(images, key=lambda i: i.width):
        img = img.convert("RGBA")
        if img.width <= 48:
            entries.append((img, bmp_entry(img)))
        else:
            buf = io.BytesIO()
            img.save(buf, format="PNG", optimize=True)
            entries.append((img, buf.getvalue()))

    header = struct.pack("<HHH", 0, 1, len(entries))
    directory = b""
    offset = 6 + 16 * len(entries)
    for img, data in entries:
        w = 0 if img.width >= 256 else img.width
        h = 0 if img.height >= 256 else img.height
        directory += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
    path.write_bytes(header + directory + b"".join(d for _, d in entries))


def bmp_entry(img: Image.Image) -> bytes:
    w, h = img.size
    info = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, w * h * 4, 0, 0, 0, 0)
    pixels = bytearray()
    px = img.load()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = px[x, y]
            pixels += bytes((b, g, r, a))
    row_bytes = ((w + 31) // 32) * 4
    mask = bytearray()
    for y in range(h - 1, -1, -1):
        row = bytearray(row_bytes)
        for x in range(w):
            if px[x, y][3] == 0:
                row[x // 8] |= 0x80 >> (x % 8)
        mask += row
    return info + bytes(pixels) + bytes(mask)


# --------------------------------------------------------------------------- C# geometry

def write_geometry_cs(path: Path) -> None:
    layers = {name: pts for name, pts, _ in NORMAL.box_layers(depth=True)}
    content = f"""// <auto-generated>
// Generated by tools/branding/generate_branding.py. Do not edit by hand.
// XAML path data for the SharpShot mark (Concept A) inside a {BOX:.0f}x{BOX:.0f} box.
// Paint order: TopBracket, LowerSilver, UpperSilver, BottomBracket.
// Brackets use the orange gradient ({PRIMARY} -> {PRIMARY_HOVER}), silver pieces the silver
// gradient ({SILVER_LIGHT} -> {SILVER_DARK}), both diagonal per shape (top-left -> bottom-right).
// LowerSilverEdge / UpperSilverEdge are optional: thin strips ({SILVER_EDGE_A} -> {SILVER_EDGE_B})
// painted over the lower edge of each ribbon to suggest its thickness; draw them after
// the corresponding ribbon when the mark is rendered at 26 px or more.
// </auto-generated>

namespace SharpShot.App.Components;

internal static class LogoGeometry
{{
    public const double BoxSize = {BOX:.0f};

    public const string TopBracket = "{path_data(layers['TopBracket'])}";
    public const string LowerSilver = "{path_data(layers['LowerSilver'])}";
    public const string UpperSilver = "{path_data(layers['UpperSilver'])}";
    public const string BottomBracket = "{path_data(layers['BottomBracket'])}";

    public const string LowerSilverEdge = "{path_data(layers['LowerSilverEdge'])}";
    public const string UpperSilverEdge = "{path_data(layers['UpperSilverEdge'])}";
}}
"""
    path.write_text(content, encoding="utf-8")


# --------------------------------------------------------------------------- icon plan

APP_SIZES = [16, 24, 32, 48, 64, 128, 256]


def app_icon(size: int) -> Image.Image:
    """Application icon: the mark on the dark tile from 24 px up (identity board,
    "Ícones para Windows"); at 16 px the bare mark, with the dimmer silver so it
    also reads on light title bars / Explorer."""
    if size >= 24:
        return render_mark(size, tile=True)
    return render_mark(size, tile=False, silver=(SILVER_TRAY_A, SILVER_TRAY_B))


def tray_icon(size: int) -> Image.Image:
    """Tray icon: bare mark at every size (consistent across DPI scales), with a
    slightly dimmer silver so the ribbon still reads on a light taskbar."""
    return render_mark(size, tile=False, silver=(SILVER_TRAY_A, SILVER_TRAY_B))


# --------------------------------------------------------------------------- main

def main() -> int:
    for d in (BRANDING, ICONS, APP_ASSETS, APP_COMPONENTS):
        d.mkdir(parents=True, exist_ok=True)

    # Vector variants
    (BRANDING / "sharpshot-symbol.svg").write_text(svg_symbol(NORMAL), encoding="utf-8")
    (BRANDING / "sharpshot-symbol-on-light.svg").write_text(
        svg_symbol(NORMAL, silver=(SILVER_ON_LIGHT_A, SILVER_ON_LIGHT_B)), encoding="utf-8")
    (BRANDING / "sharpshot-symbol-mono-white.svg").write_text(svg_symbol(NORMAL, mono=TEXT_PRIMARY), encoding="utf-8")
    (BRANDING / "sharpshot-symbol-mono-dark.svg").write_text(svg_symbol(NORMAL, mono=BACKGROUND), encoding="utf-8")
    (BRANDING / "sharpshot-monochrome.svg").write_text(svg_symbol(NORMAL, mono="currentColor"), encoding="utf-8")
    (BRANDING / "sharpshot-app-icon.svg").write_text(svg_symbol(NORMAL, tile=True), encoding="utf-8")
    (BRANDING / "sharpshot-horizontal-dark.svg").write_text(svg_horizontal(dark=True), encoding="utf-8")
    (BRANDING / "sharpshot-horizontal-light.svg").write_text(svg_horizontal(dark=False), encoding="utf-8")

    # Raster logo exports
    render_mark(512, NORMAL, tile=False).save(BRANDING / "sharpshot-symbol-512.png")
    render_mark(512, NORMAL, tile=False, silver=(SILVER_ON_LIGHT_A, SILVER_ON_LIGHT_B)).save(
        BRANDING / "sharpshot-symbol-on-light-512.png")
    render_mark(512, NORMAL, tile=True).save(BRANDING / "sharpshot-app-icon-512.png")
    render_horizontal(True).save(BRANDING / "sharpshot-horizontal-dark.png")
    render_horizontal(False).save(BRANDING / "sharpshot-horizontal-light.png")

    # Icons: real per-size renders (never upscaled), app icon + tray icon
    app_images = []
    tray_images = []
    for s in APP_SIZES:
        app_img = app_icon(s)
        tray_img = tray_icon(s)
        app_images.append(app_img)
        tray_images.append(tray_img)
        app_img.save(ICONS / f"sharpshot-{s}.png")
        tray_img.save(ICONS / f"tray-{s}.png")
    write_ico(ICONS / "sharpshot.ico", app_images)
    write_ico(ICONS / "tray.ico", tray_images)

    # Runtime assets for the app
    write_ico(APP_ASSETS / "SharpShot.ico", app_images)
    write_ico(APP_ASSETS / "Tray.ico", tray_images)
    render_mark(256, NORMAL, tile=True).save(APP_ASSETS / "NotificationLogo.png")

    # MSIX tile set (scale-200)
    def tile_png(name: str, w: int, h: int, mark_px: int, *, tile: bool = False, bg: bool = False,
                 mono: str | None = None) -> None:
        img = Image.new("RGBA", (w, h), hex_to_rgb(BACKGROUND) + (255,) if bg else (0, 0, 0, 0))
        mark = render_mark(mark_px, tile=tile, mono=mono)
        img.alpha_composite(mark, ((w - mark_px) // 2, (h - mark_px) // 2))
        img.save(APP_ASSETS / name)

    tile_png("Square44x44Logo.scale-200.png", 88, 88, 88, tile=True)
    tile_png("Square44x44Logo.targetsize-24_altform-unplated.png", 24, 24, 24, tile=True)
    tile_png("Square150x150Logo.scale-200.png", 300, 300, 200, tile=True)
    tile_png("Wide310x150Logo.scale-200.png", 620, 300, 200, tile=True)
    tile_png("StoreLogo.png", 50, 50, 50, tile=True)
    tile_png("LockScreenLogo.scale-200.png", 48, 48, 40, mono=TEXT_PRIMARY)   # badge logos are white glyphs
    splash = Image.new("RGBA", (1240, 600), hex_to_rgb(BACKGROUND) + (255,))
    lockup = render_horizontal(True, height=200)
    splash.alpha_composite(lockup, ((1240 - lockup.width) // 2, (600 - lockup.height) // 2))
    splash.save(APP_ASSETS / "SplashScreen.scale-200.png")

    write_geometry_cs(APP_COMPONENTS / "LogoGeometry.cs")
    print("Branding generated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
