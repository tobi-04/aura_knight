"""Builds one tileset sheet per region (Hub/Forest/Cave/City/Castle) under Art/Tilesets/<Region>/Tiles_<Region>.png.

Cells are 16 logical px (32 px at PPU 32). Layout (cols x rows, 3 wide): rows 0-2 ground 3x3 (TL T TR / L C R / BL B BR),
rows 3-5 wall 3x3, row 6 one-way platform (L M R), row 7 spikes (up, plus 2 decor variants). A .tiles.json lists sprite names.
"""
import json
import os
import random
import sys
import pixel_kit as pk
from pixel_kit import hexc, new, pen

T = 16
REGIONS = {  # base, light, dark, cap, cap_light, accent, style
    "Hub": ("#7A6A4A", "#9C8A60", "#4F432E", "#C99A3B", "#F0CC6E", "#FFC857", "block"),
    "Forest": ("#4A3322", "#6B4B33", "#2A1C12", "#2FA36B", "#5FE0A0", "#27D38C", "dirt"),
    "Cave": ("#3A3550", "#544E70", "#211E32", "#6A5F8E", "#8E83B8", "#FFC857", "rock"),
    "City": ("#4B5A72", "#6C7E9A", "#2C3648", "#8A95AC", "#C5D0E4", "#27B5F7", "plate"),
    "Castle": ("#2E2434", "#463850", "#18121C", "#6B2A3A", "#FF5C57", "#FF5C57", "brick"),
}


def texture(region, seed):
    base, light, dark, cap, cap_l, accent, style = [hexc(v) if v.startswith("#") else v for v in REGIONS[region]]
    rng = random.Random(seed)
    img = new(T, T)
    d = pen(img)
    d.rectangle([0, 0, T - 1, T - 1], fill=base)
    if style in ("brick", "block", "plate"):
        h = 4 if style == "brick" else 8
        for row in range(T // h):
            y = row * h
            d.line([(0, y), (T - 1, y)], fill=dark)
            off = 0 if row % 2 == 0 or style != "brick" else 4
            for x in range(off, T + 8, 8 if style == "brick" else 16):
                d.line([(x % T, y), (x % T, y + h - 1)], fill=dark)
            if style == "plate":
                for x in (2, 13):
                    d.point((x, y + 2), fill=light)
    else:
        for _ in range(26):
            d.point((rng.randrange(T), rng.randrange(T)), fill=rng.choice([light, dark]))
        if style == "rock":
            for _ in range(2):
                d.point((rng.randrange(2, T - 2), rng.randrange(2, T - 2)), fill=accent)
    return img


def tile(region, top, bottom, left, right, seed=1, wall=False):
    base, light, dark, cap, cap_l, accent, style = [hexc(v) if v.startswith("#") else v for v in REGIONS[region]]
    img = texture(region, seed + (7 if wall else 0))
    if wall:
        img = pk.tint(img, (0, 0, 0), 0.25)
    d = pen(img)
    if top:
        d.rectangle([0, 0, T - 1, 2], fill=cap)
        d.line([(0, 0), (T - 1, 0)], fill=cap_l)
        if style == "dirt":
            for x in range(1, T, 3):
                d.line([(x, 3), (x, 4 + x % 2)], fill=cap)
        elif style in ("brick", "plate", "block"):
            d.line([(0, 3), (T - 1, 3)], fill=dark)
    if bottom:
        d.line([(0, T - 1), (T - 1, T - 1)], fill=dark)
    if left:
        d.line([(0, 0 if not top else 1), (0, T - 1)], fill=dark if not top else cap)
    if right:
        d.line([(T - 1, 0 if not top else 1), (T - 1, T - 1)], fill=dark)
    return img


def platform(region, part):
    cap, cap_l = hexc(REGIONS[region][3]), hexc(REGIONS[region][4])
    dark, base = hexc(REGIONS[region][2]), hexc(REGIONS[region][0])
    img = new(T, T)
    d = pen(img)
    x0 = 0 if part != "L" else 2
    x1 = T - 1 if part != "R" else T - 3
    d.rectangle([x0, 0, x1, 5], fill=base)
    d.rectangle([x0, 0, x1, 1], fill=cap)
    d.line([(x0, 0), (x1, 0)], fill=cap_l)
    d.line([(x0, 5), (x1, 5)], fill=dark)
    if part == "M":
        for x in (3, 11):
            d.line([(x, 2), (x, 4)], fill=dark)
    return img


def spikes(region, variant):
    dark, light = hexc("#8A93A6"), hexc("#E6ECF6")
    img = new(T, T)
    d = pen(img)
    d.rectangle([0, 13, T - 1, T - 1], fill=hexc(REGIONS[region][2]))
    for i in range(4):
        x = i * 4
        d.polygon([(x, 13), (x + 3, 13), (x + 1 + (variant % 2), 3 + variant)], fill=dark)
        d.line([(x + 1, 12), (x + 1, 5 + variant)], fill=light)
    return pk.outline(img)


def build_region(region, out_dir):
    t = {}
    names = [["TL", "T", "TR"], ["L", "C", "R"], ["BL", "B", "BR"]]
    flags = lambda r, c: (r == 0, r == 2, c == 0, c == 2)
    cells = []
    for wall in (False, True):
        for r in range(3):
            row = []
            for c in range(3):
                top, bottom, left, right = flags(r, c)
                row.append(tile(region, top and not wall, bottom, left, right, seed=r * 3 + c, wall=wall))
            cells.append(row)
    cells.append([platform(region, "L"), platform(region, "M"), platform(region, "R")])
    cells.append([spikes(region, 0), spikes(region, 1), spikes(region, 2)])
    sheet = new(3 * T, len(cells) * T)
    meta = {"name": f"Tiles_{region}", "cell": T * pk.SCALE, "columns": 3, "rows": len(cells), "sprites": []}
    groups = ["Ground", "Ground", "Ground", "Wall", "Wall", "Wall", "Platform", "Spikes"]
    for r, row in enumerate(cells):
        for c, img in enumerate(row):
            sheet.alpha_composite(img, (c * T, r * T))
            if r < 3 or 3 <= r < 6:
                part = names[r % 3][c]
            else:
                part = ["L", "M", "R"][c] if r == 6 else ["Up", "UpB", "UpC"][c]
            meta["sprites"].append({"name": f"{region}_{groups[r]}_{part}", "col": c, "row": r})
    pk.write_png(pk.upscale(sheet), os.path.join(out_dir, f"Tiles_{region}.png"))
    with open(os.path.join(out_dir, f"Tiles_{region}.tiles.json"), "w") as fh:
        json.dump(meta, fh, indent=2)
        fh.write("\n")
    return sheet


def build():
    sheets = {}
    for region in REGIONS:
        sheets[region] = build_region(region, os.path.join(pk.ART, "Tilesets", region))
    return sheets


if __name__ == "__main__":
    s = build()
    if len(sys.argv) > 1:
        pk.preview([[v for v in s.values()]], sys.argv[1], zoom=2)
