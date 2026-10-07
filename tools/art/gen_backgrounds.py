"""Builds 4 parallax layers per region under Art/Backgrounds/<Region>/: Bg (x0.1), Mid (x0.5), Back (x1.0), Fg (x1.2).

Each PNG is 640x360 px (320x180 logical) and tiles horizontally: every object is also drawn one period left/right.
"""
import math
import os
import random
import sys
import pixel_kit as pk
from pixel_kit import hexc, new, pen

W, H = 320, 180
PAL = {  # sky top, sky bottom, far, mid, back, fg, accent
    "Hub": ("#1B1A33", "#C98A4B", "#4A3C5C", "#2E2540", "#6B5A3A", "#120F1E", "#FFC857"),
    "Forest": ("#06121A", "#124A44", "#0F3B33", "#0A2A25", "#2A1C12", "#040E0C", "#27D38C"),
    "Cave": ("#0A0816", "#1B1630", "#241E3C", "#181328", "#2E2748", "#07050E", "#FFC857"),
    "City": ("#0B1626", "#2B4A66", "#1B2B40", "#121E30", "#34455E", "#070D17", "#27B5F7"),
    "Castle": ("#12060C", "#4A1424", "#2A1220", "#1C0C18", "#3A2030", "#080308", "#FF5C57"),
}


def wrap_draw(fn, x):
    for off in (-W, 0, W):
        fn(x + off)


def gradient(top, bottom):
    img = new(W, H)
    d = pen(img)
    steps = 12                                   # banded, not smooth: reads as pixel art
    for i in range(steps):
        t = i / (steps - 1)
        c = tuple(round(top[k] + (bottom[k] - top[k]) * t) for k in range(3)) + (255,)
        d.rectangle([0, i * H // steps, W, (i + 1) * H // steps], fill=c)
    return img


def stars(d, rng, n, color):
    for _ in range(n):
        d.point((rng.randrange(W), rng.randrange(H // 2)), fill=color)


def layer_bg(region, rng):
    p = [hexc(v) for v in PAL[region]]
    img = gradient(p[0], p[1])
    d = pen(img)
    if region in ("Hub", "Castle", "Forest"):
        cx, cy, r = {"Hub": (220, 90, 34), "Castle": (90, 70, 30), "Forest": (240, 50, 18)}[region]
        col = {"Hub": "#F6C56B", "Castle": "#C2384A", "Forest": "#CFE8D0"}[region]
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc(col))
        d.ellipse([cx - r - 3, cy - r - 3, cx + r + 3, cy + r + 3], outline=hexc(col)[:3] + (90,))
    if region != "Hub":
        stars(d, rng, 40 if region != "City" else 14, hexc("#CFD8F0"))
    d2 = new(W, H)
    dd = pen(d2)
    for x in range(0, W, 2):                     # distant ridge, periodic so it tiles
        y = 120 + round(10 * math.sin(x / W * 2 * math.pi * 3) + 6 * math.sin(x / W * 2 * math.pi * 7 + 1))
        dd.line([(x, y), (x, H)], fill=p[2])
    img.alpha_composite(d2)
    return img


def layer_mid(region, rng):
    p = [hexc(v) for v in PAL[region]]
    img = new(W, H)
    d = pen(img)

    def tree(x):
        h = rng2.randrange(50, 90)
        d.polygon([(x - 12, 150), (x + 12, 150), (x, 150 - h)], fill=p[3])

    def stalag(x):
        h = rng2.randrange(30, 80)
        d.polygon([(x - 9, 160), (x + 9, 160), (x + 2, 160 - h)], fill=p[3])
        d.polygon([(x - 7, 0), (x + 7, 0), (x - 2, rng2.randrange(30, 70))], fill=p[3])

    def tower(x):
        w, h = rng2.randrange(14, 30), rng2.randrange(50, 120)
        d.rectangle([x - w // 2, 160 - h, x + w // 2, 160], fill=p[3])
        for wy in range(160 - h + 6, 156, 9):
            if rng2.random() < 0.45:
                d.rectangle([x - 2, wy, x, wy + 2], fill=hexc(PAL[region][6]))
        if region == "Castle":
            d.polygon([(x - w // 2, 160 - h), (x + w // 2, 160 - h), (x, 160 - h - 26)], fill=p[3])

    def mount(x):
        h = rng2.randrange(40, 80)
        d.polygon([(x - 30, 160), (x + 30, 160), (x, 160 - h)], fill=p[3])

    fn = {"Forest": tree, "Cave": stalag, "City": tower, "Castle": tower, "Hub": mount}[region]
    rng2 = random.Random(f"mid{region}")
    xs = [i * (W // 8) + rng2.randrange(-6, 6) for i in range(8)]
    state = rng2.getstate()
    for x in xs:
        for off in (-W, 0, W):
            rng2.setstate(state)                 # identical shape for the wrapped copies
            fn(x + off)
        state = rng2.getstate()
    d.rectangle([0, 158, W, H], fill=p[3])
    return img


def layer_back(region, rng):
    p = [hexc(v) for v in PAL[region]]
    img = new(W, H)
    d = pen(img)
    rng2 = random.Random(f"back{region}")
    for i in range(5):
        x = i * (W // 5) + rng2.randrange(-8, 8) + 20
        w = {"Forest": 14, "Cave": 8, "City": 6, "Castle": 12, "Hub": 12}[region]
        for off in (-W, 0, W):
            if region == "Cave":
                d.polygon([(x + off - w, 0), (x + off + w, 0), (x + off, 40 + i * 14)], fill=p[4])
            elif region == "City":
                d.rectangle([x + off - w // 2, 0, x + off + w // 2, 150], fill=p[4])
                d.rectangle([x + off - w, 40, x + off + w, 44], fill=p[4])
                d.ellipse([x + off - 3, 30, x + off + 5, 38], fill=hexc("#8FA3BC")[:3] + (110,))
            else:
                d.rectangle([x + off - w, 0, x + off + w, 172], fill=p[4])
                d.line([(x + off - w, 0), (x + off - w, 172)], fill=hexc(PAL[region][2]))
                if region in ("Castle", "Hub"):
                    d.rectangle([x + off - w - 2, 0, x + off + w + 2, 6], fill=p[4])
    return img


def layer_fg(region, rng):
    p = [hexc(v) for v in PAL[region]]
    img = new(W, H)
    d = pen(img)
    rng2 = random.Random(f"fg{region}")
    for i in range(10):
        x = i * (W // 10) + rng2.randrange(-6, 6)
        h = rng2.randrange(8, 30)
        for off in (-W, 0, W):
            if region == "Castle":
                d.line([(x + off, 0), (x + off, h + 20)], fill=p[5], width=1)
                d.ellipse([x + off - 2, h + 20, x + off + 2, h + 24], outline=p[5])
            else:
                d.polygon([(x + off - 6, 0), (x + off + 6, 0), (x + off, h)], fill=p[5])
            d.polygon([(x + off - 8, H), (x + off + 8, H), (x + off, H - h // 2 - 4)], fill=p[5])
    return img


LAYERS = [("Bg", layer_bg), ("Mid", layer_mid), ("Back", layer_back), ("Fg", layer_fg)]


def build():
    out = {}
    for region in PAL:
        for name, fn in LAYERS:
            img = fn(region, random.Random(f"{region}{name}"))
            img = img.convert("RGBA")
            pk.write_png(pk.upscale(img), os.path.join(pk.ART, "Backgrounds", region, f"{region}_{name}.png"))
            out[(region, name)] = img
    return out


if __name__ == "__main__":
    o = build()
    if len(sys.argv) > 1:
        for region in PAL:
            base = o[(region, "Bg")].copy()
            for n in ("Mid", "Back", "Fg"):
                base.alpha_composite(o[(region, n)])
            pk.write_png(base, f"{sys.argv[1]}_{region}.png")
