#!/usr/bin/env python3
"""
Visual + numeric validation of the generated SharpShot branding against the
approved references in public/assets.

  python tools/branding/compare.py            # writes assets/branding/comparison-sheet.png
                                              # and prints the fidelity report

The numeric report renders the mark at exactly the reference's S height, classifies
both images into orange / silver / background masks and compares, row by row, the
left and right extents of the orange and silver areas (mean and max deviation in
reference pixels) plus the intersection-over-union of each colour mask.
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_branding as gb  # noqa: E402

PUBLIC = gb.ROOT / "public" / "assets"
OUT = gb.BRANDING / "comparison-sheet.png"

# Reference crops: (file, crop box) - the mark fills the crop with some margin.
REF_MARKS = {
    "02 board - logo topo": ("02-identidade-aprovada-e-ui.png", (36, 86, 142, 198)),
    "01 Conceito A": ("01-branding-conceitos.png", (1058, 178, 1190, 324)),
    "04 ícone (marca)": ("04-icone-aplicativo-referencia.png", (30, 40, 155, 170)),
}
REF_TILE = ("04-icone-aplicativo-referencia.png", (9, 17, 170, 186))
REF_TILE_BOARD = ("02-identidade-aprovada-e-ui.png", (45, 264, 204, 428))
REF_HORIZONTAL = ("03-logo-horizontal-referencia.png", (0, 10, 420, 135))
REF_ICO_ROW = ("02-identidade-aprovada-e-ui.png", (36, 676, 424, 756))
REF_APPS_ROW = ("02-identidade-aprovada-e-ui.png", (1030, 858, 1420, 950))

DARK_BG = gb.hex_to_rgb(gb.BACKGROUND)
LIGHT_BG = gb.hex_to_rgb(gb.TEXT_PRIMARY)
TASKBAR_DARK = (32, 32, 32)
TASKBAR_LIGHT = (243, 243, 243)


# --------------------------------------------------------------------------- masks / metric

def classify(img: Image.Image) -> Image.Image:
    """'L' image: 1 = orange, 2 = silver, 0 = background."""
    rgb = img.convert("RGB")
    out = Image.new("L", rgb.size, 0)
    src = rgb.load()
    dst = out.load()
    for y in range(rgb.height):
        for x in range(rgb.width):
            r, g, b = src[x, y]
            if r > 170 and 70 < g < 215 and b < 110 and r - b > 90:
                dst[x, y] = 1
            elif r > 140 and g > 140 and b > 140 and abs(r - b) < 45:
                dst[x, y] = 2
    return out


def bbox_of(mask: Image.Image) -> tuple[int, int, int, int]:
    px = mask.load()
    xs, ys = [], []
    for y in range(mask.height):
        for x in range(mask.width):
            if px[x, y]:
                xs.append(x)
                ys.append(y)
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def render_like(ref_mask: Image.Image, geo: gb.Geometry, bg: tuple[int, int, int], ss: int = 6) -> Image.Image:
    """Renders the mark so that its S bounding box matches the reference's bounding box."""
    x0, y0, x1, y1 = bbox_of(ref_mask)
    s = y1 - y0
    w, h = ref_mask.size
    out = Image.new("RGBA", (w * ss, h * ss), bg + (255,))
    for _, pts, kind in geo.layers():
        colors = (gb.PRIMARY, gb.PRIMARY_HOVER) if kind == "orange" else (gb.SILVER_LIGHT, gb.SILVER_DARK)
        gb.fill_shape(out, [((x0 + x * s) * ss, (y0 + y * s) * ss) for x, y in pts], colors, None)
    return out.resize((w, h), Image.LANCZOS)


def extents(mask: Image.Image, value: int) -> dict[int, tuple[int, int]]:
    px = mask.load()
    res = {}
    for y in range(mask.height):
        xs = [x for x in range(mask.width) if px[x, y] == value]
        if xs:
            res[y] = (xs[0], xs[-1])
    return res


DIFF_COLORS = {
    (1, 1): (255, 150, 60),     # orange in both
    (2, 2): (215, 222, 232),    # silver in both
    (1, 0): (255, 40, 40),      # orange only in the reference
    (0, 1): (255, 0, 255),      # orange only in the new render
    (2, 0): (40, 220, 90),      # silver only in the reference
    (0, 2): (60, 120, 255),     # silver only in the new render
    (1, 2): (255, 255, 0),      # orange in ref, silver in new
    (2, 1): (0, 255, 255),      # silver in ref, orange in new
}


def diff_image(ref: Image.Image, mine: Image.Image) -> Image.Image:
    """Colour-coded mask difference (see DIFF_COLORS)."""
    a, b = classify(ref), classify(mine)
    out = Image.new("RGB", a.size, (20, 24, 32))
    pa, pb, po = a.load(), b.load(), out.load()
    for y in range(a.height):
        for x in range(a.width):
            c = DIFF_COLORS.get((pa[x, y], pb[x, y]))
            if c:
                po[x, y] = c
    return out


def fidelity(ref: Image.Image, mine: Image.Image) -> dict[str, float]:
    a, b = classify(ref), classify(mine)
    report = {}
    for value, name in ((1, "laranja"), (2, "prata")):
        ea, eb = extents(a, value), extents(b, value)
        rows = sorted(set(ea) & set(eb))
        dl = [abs(ea[y][0] - eb[y][0]) for y in rows]
        dr = [abs(ea[y][1] - eb[y][1]) for y in rows]
        pa, pb = a.load(), b.load()
        inter = union = 0
        for y in range(a.height):
            for x in range(a.width):
                ia, ib = pa[x, y] == value, pb[x, y] == value
                inter += ia and ib
                union += ia or ib
        report[f"{name} esq média"] = sum(dl) / max(len(dl), 1)
        report[f"{name} esq máx"] = max(dl, default=0)
        report[f"{name} dir média"] = sum(dr) / max(len(dr), 1)
        report[f"{name} dir máx"] = max(dr, default=0)
        report[f"{name} IoU"] = inter / max(union, 1)
        report[f"{name} linhas só ref"] = len(set(ea) - set(eb))
        report[f"{name} linhas só nova"] = len(set(eb) - set(ea))
    return report


# --------------------------------------------------------------------------- sheet helpers

FONT = gb.load_font(15, bold=False)
FONT_B = gb.load_font(18, bold=True)


def scale(img: Image.Image, h: int, nearest: bool = False) -> Image.Image:
    w = round(img.width * h / img.height)
    return img.resize((w, h), Image.NEAREST if nearest else Image.LANCZOS)


def on_bg(img: Image.Image, bg: tuple[int, int, int], pad: int = 0) -> Image.Image:
    out = Image.new("RGBA", (img.width + 2 * pad, img.height + 2 * pad), bg + (255,))
    out.alpha_composite(img.convert("RGBA"), (pad, pad))
    return out


def zoom(img: Image.Image, k: int) -> Image.Image:
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def overlay(ref: Image.Image, mine: Image.Image, alpha: float = 0.55) -> Image.Image:
    base = ref.convert("RGBA")
    top = mine.convert("RGBA")
    a = top.getchannel("A").point(lambda v: int(v * alpha))
    top.putalpha(a)
    base.alpha_composite(top)
    return base


class Sheet:
    def __init__(self, width: int = 1900) -> None:
        self.width = width
        self.rows: list[Image.Image] = []

    def title(self, text: str) -> None:
        img = Image.new("RGBA", (self.width, 34), (24, 28, 36, 255))
        ImageDraw.Draw(img).text((12, 8), text, font=FONT_B, fill=(245, 247, 250))
        self.rows.append(img)

    def row(self, items: list[tuple[Image.Image, str]], gap: int = 18) -> None:
        h = max(i.height for i, _ in items) + 26
        img = Image.new("RGBA", (self.width, h + 10), (14, 17, 23, 255))
        d = ImageDraw.Draw(img)
        x = 12
        for im, caption in items:
            img.alpha_composite(im.convert("RGBA"), (x, 4))
            d.text((x, im.height + 8), caption, font=FONT, fill=(200, 208, 220))
            x += max(im.width, int(d.textlength(caption, font=FONT))) + gap
        self.rows.append(img)

    def text(self, lines: list[str]) -> None:
        img = Image.new("RGBA", (self.width, 20 * len(lines) + 10), (14, 17, 23, 255))
        d = ImageDraw.Draw(img)
        for i, line in enumerate(lines):
            d.text((12, 4 + 20 * i), line, font=FONT, fill=(200, 208, 220))
        self.rows.append(img)

    def save(self, path: Path) -> None:
        h = sum(r.height for r in self.rows)
        out = Image.new("RGBA", (self.width, h), (14, 17, 23, 255))
        y = 0
        for r in self.rows:
            out.alpha_composite(r, (0, y))
            y += r.height
        out.convert("RGB").save(path)


def ref_crop(spec: tuple[str, tuple[int, int, int, int]]) -> Image.Image:
    return Image.open(PUBLIC / spec[0]).convert("RGB").crop(spec[1])


# --------------------------------------------------------------------------- main

def main() -> int:
    sheet = Sheet()
    sheet.title("SharpShot - validação da marca: referência | novo render na mesma caixa do S | sobreposição 55 % | "
                "diferença (vermelho/verde = só na ref, magenta/azul = só no novo)")

    reports = []
    for label, spec in REF_MARKS.items():
        ref = ref_crop(spec)
        mine = render_like(classify(ref), gb.NORMAL, DARK_BG)
        rep = fidelity(ref, mine)
        reports.append((label, rep))
        k = 2
        sheet.row([(zoom(ref, k), f"ref: {label}"), (zoom(mine, k), "novo (mesma caixa do S)"),
                   (zoom(overlay(ref, mine), k), "sobreposição"), (zoom(diff_image(ref, mine), k), "diferença")])
    lines = []
    for label, rep in reports:
        lines.append(f"{label}: " + "  ".join(
            f"{k}={v:.2f}" if isinstance(v, float) else f"{k}={v}" for k, v in rep.items()))
    sheet.text(lines)

    # Horizontal lock-up vs reference 03
    sheet.title("Logotipo horizontal - referência 03 (1:1 do board 02) x novo (dark e light)")
    ref = ref_crop(REF_HORIZONTAL)
    dark = Image.open(gb.BRANDING / "sharpshot-horizontal-dark.png")
    light = Image.open(gb.BRANDING / "sharpshot-horizontal-light.png")
    # same S height: reference S is 97 px tall, ours is 216 px in a 256 canvas
    k = 97 / gb.NORMAL.s_height_in_box
    sheet.row([(scale(ref, int(ref.height * 1.0)), "ref 03 (1x)"),
               (dark.resize((round(dark.width * k), round(dark.height * k)), Image.LANCZOS), "novo dark, S na mesma altura"),
               (light.resize((round(light.width * k), round(light.height * k)), Image.LANCZOS), "novo light")])
    sheet.row([(scale(dark, 150), "sharpshot-horizontal-dark.png"), (scale(light, 150), "sharpshot-horizontal-light.png")])

    # App icon vs reference 04 and board tile
    sheet.title("Ícone do aplicativo - referência 04 e tile do board 02 x novo (256 px)")
    ref04 = ref_crop(REF_TILE)
    refb = ref_crop(REF_TILE_BOARD)
    icon = Image.open(gb.ICONS / "sharpshot-256.png")
    sheet.row([(scale(ref04, 256), "ref 04 (ampliada)"), (on_bg(icon, DARK_BG), "sharpshot-256.png"),
               (overlay(scale(ref04, 256), icon), "sobreposição"),
               (scale(refb, 256), "ref 02 'Logotipo Final'"),
               (scale(Image.open(gb.BRANDING / "sharpshot-app-icon-512.png"), 256), "sharpshot-app-icon-512.png")])

    # Small sizes
    sheet.title("Tamanhos pequenos - ícone do app (sharpshot.ico) 16/24/32/48 em 1x e 6x (nearest) sobre fundo escuro e claro")
    z = 6
    row = []
    for s in (16, 24, 32, 48):
        im = Image.open(gb.ICONS / f"sharpshot-{s}.png")
        row.append((on_bg(im, TASKBAR_DARK, 4), f"{s} px 1x"))
        row.append((zoom(on_bg(im, TASKBAR_DARK, 2), z), f"{s} px {z}x escuro"))
        row.append((zoom(on_bg(im, TASKBAR_LIGHT, 2), z), f"{s} px {z}x claro"))
    sheet.row(row[:9])
    sheet.row(row[9:])
    sheet.row([(scale(ref_crop(REF_ICO_ROW), 90), "ref 02 'Ícones para Windows'"),
               (scale(ref_crop(REF_APPS_ROW), 90), "ref 02 'Aplicações da Marca'")])

    sheet.title("Ícone da bandeja (tray.ico) 16/24/32 - 1x e 8x sobre barra escura (#202020) e clara (#F3F3F3) + alternativas em 16 px")
    row = []
    for s in (16, 24, 32):
        im = Image.open(gb.ICONS / f"tray-{s}.png")
        row.append((on_bg(im, TASKBAR_DARK, 4), f"tray {s} 1x"))
        row.append((zoom(on_bg(im, TASKBAR_DARK, 2), 8), f"tray {s} 8x escuro"))
        row.append((zoom(on_bg(im, TASKBAR_LIGHT, 2), 8), f"tray {s} 8x claro"))
    sheet.row(row[:6])
    sheet.row(row[6:])
    alt_tile = gb.render_mark(16, tile=True)
    alt_plain = gb.render_mark(16, tile=False)
    alt_normal = gb.render_mark(16, gb.NORMAL, tile=False, snap=False)
    sheet.row([(zoom(on_bg(alt_tile, TASKBAR_DARK, 2), 8), "alt: tile 16 escuro"),
               (zoom(on_bg(alt_tile, TASKBAR_LIGHT, 2), 8), "alt: tile 16 claro"),
               (zoom(on_bg(alt_plain, TASKBAR_DARK, 2), 8), "alt: prata padrão 16 escuro"),
               (zoom(on_bg(alt_plain, TASKBAR_LIGHT, 2), 8), "alt: prata padrão 16 claro"),
               (zoom(on_bg(alt_normal, TASKBAR_DARK, 2), 8), "alt: geometria normal, sem snap"),
               (zoom(on_bg(alt_normal, TASKBAR_LIGHT, 2), 8), "idem, claro")])

    # Backgrounds and variants
    sheet.title("Variantes - símbolo sobre fundo escuro e claro, monocromáticas, ícone 128/64")
    sym = Image.open(gb.BRANDING / "sharpshot-symbol-512.png")
    sym_light = Image.open(gb.BRANDING / "sharpshot-symbol-on-light-512.png")
    sheet.row([(on_bg(scale(sym, 200), DARK_BG), "símbolo em #0D1017"),
               (on_bg(scale(sym_light, 200), LIGHT_BG), "símbolo on-light em #F5F7FA"),
               (on_bg(gb.render_mark(200, gb.NORMAL, mono=gb.TEXT_PRIMARY), DARK_BG), "mono branco"),
               (on_bg(gb.render_mark(200, gb.NORMAL, mono=gb.BACKGROUND), LIGHT_BG), "mono escuro"),
               (on_bg(Image.open(gb.ICONS / "sharpshot-128.png"), DARK_BG, 8), "app 128"),
               (on_bg(Image.open(gb.ICONS / "sharpshot-64.png"), LIGHT_BG, 8), "app 64 em fundo claro"),
               (on_bg(Image.open(gb.APP_ASSETS / "LockScreenLogo.scale-200.png"), (60, 60, 70), 8), "LockScreenLogo")])

    sheet.save(OUT)
    print(f"wrote {OUT}")
    for label, rep in reports:
        print(label)
        for k, v in rep.items():
            print(f"   {k:22s} {v:.3f}" if isinstance(v, float) else f"   {k:22s} {v}")
    if "--diff-dir" in sys.argv:  # per-reference diff images, 4x, for close inspection
        out_dir = Path(sys.argv[sys.argv.index("--diff-dir") + 1])
        out_dir.mkdir(parents=True, exist_ok=True)
        for label, spec in REF_MARKS.items():
            ref = ref_crop(spec)
            mine = render_like(classify(ref), gb.NORMAL, DARK_BG)
            zoom(diff_image(ref, mine), 4).save(out_dir / f"diff_{label[:2]}.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
