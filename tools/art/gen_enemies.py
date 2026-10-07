"""Builds the 8 enemy sheets (GDD 7.3) under Art/Enemies/<Variant>/.

Bat, Ghost, StoneSpider start from Kenney "Tiny Dungeon" (CC0, 16 px, x2) and are animated/recoloured here.
NightKnight reuses Leo's rig with a dark palette. ThornBug, MushroomHopper, PatrolRobot, ScrapZapper are drawn here.
Every sheet has the same clip rows: Idle, Move, Attack, Hurt, Death. Cells are 24x24 logical (48 px), NightKnight 32x32.
"""
import math
import os
import random
import sys
from PIL import Image
import pixel_kit as pk
import gen_leo
from pixel_kit import hexc, new, pen, place

CELL = (24, 24)
TD = os.path.join(pk.CACHE, "tiny-dungeon", "Tilemap", "tilemap_packed.png")


def kenney_tile(col, row):
    if not os.path.exists(TD):
        raise SystemExit("Kenney Tiny Dungeon not found; run tools/art/fetch_sources.sh first")
    return Image.open(TD).convert("RGBA").crop((col * 16, row * 16, col * 16 + 16, row * 16 + 16))


def to_project_outline(img):
    """Kenney's maroon outline -> the project's near-black outline so all sprites share one edge colour."""
    px = img.load()
    dark = min((px[x, y][:3] for x in range(img.width) for y in range(img.height) if px[x, y][3]), key=sum)
    return pk.recolor(img, {dark: pk.OUTLINE[:3]})


def finish(name, idle, move, attack, cell=CELL, fps=(5, 8, 10), hurt=None, death=None):
    sheet = pk.Sheet(name, *cell)
    h, d = pk.standard_hurt_death(idle[0])
    sheet.add("Idle", idle, fps[0], True)
    sheet.add("Move", move, fps[1], True)
    sheet.add("Attack", attack, fps[2], False)
    sheet.add("Hurt", hurt or [place(f, cell) if f.size != cell else f for f in h], 10, False)
    sheet.add("Death", death or [place(f, cell) if f.size != cell else f for f in d], 8, False)
    sheet.save(os.path.join(pk.ART, "Enemies", name))
    return [idle, move, attack, h, d]


def in_cell(img, dx=0, dy=0):
    return place(img, CELL, dx, dy)


# ---------- Kenney based ----------
def bat():
    base = to_project_outline(kenney_tile(0, 10))

    def flap(up):
        f = pk.region_shift(base, (0, 0, 5, 16), 0, -up)
        return pk.region_shift(f, (11, 0, 16, 16), 0, -up)

    frames = [in_cell(flap(u), 0, -2 + b) for u, b in ((0, 0), (2, 1), (3, 2), (1, 1))]
    move = [in_cell(flap(u), 2, -3 + b) for u, b in ((3, 0), (1, 1), (-1, 2), (1, 1))]
    attack = [in_cell(pk.shift(flap(u), x, y), 0, 0) for u, x, y in ((3, -2, -4), (1, 0, 0), (0, 3, 3), (1, 5, 5))]
    return finish("Bat", frames, move, attack, fps=(8, 12, 12))


def ghost():
    base = to_project_outline(kenney_tile(1, 10))
    base = base.copy()
    px = base.load()
    for y in range(16):
        for x in range(16):
            r, g, b, a = px[x, y]
            if a and (r, g, b) != pk.OUTLINE[:3]:
                px[x, y] = (r, g, b, 215)
    idle = [in_cell(pk.shift(base, 0, y), 0, -3) for y in (0, 1, 2, 1)]
    move = [in_cell(pk.shift(pk.shear(base, 1, 12), x, y), 0, -3) for x, y in ((0, 0), (1, 1), (2, 2), (1, 1))]
    attack = [in_cell(pk.shift(base, x, y), 0, -3) for x, y in ((-2, 0), (-3, -1), (2, 1), (5, 2))]
    return finish("Ghost", idle, move, attack)


def stone_spider():
    base = to_project_outline(kenney_tile(2, 10))
    stone = {}
    px = base.load()
    for y in range(16):
        for x in range(16):
            r, g, b, a = px[x, y]
            if a and (r, g, b) != pk.OUTLINE[:3]:
                lum = (r * 3 + g * 5 + b * 2) // 10
                stone[(r, g, b)] = (round(lum * 0.78), round(lum * 0.84), round(lum * 0.98))
    base = pk.recolor(base, stone)

    def legs(phase):
        f = pk.region_shift(base, (0, 3, 4, 14), 0, phase)
        return pk.region_shift(f, (12, 3, 16, 14), 0, -phase)

    idle = [in_cell(legs(p)) for p in (0, 0, 1, 0)]
    move = [in_cell(pk.shift(legs(p), x, 0)) for p, x in ((1, -1), (0, 0), (-1, 1), (0, 0))]
    attack = [in_cell(pk.shift(legs(p), x, y)) for p, x, y in ((0, 0, 2), (1, 2, -2), (0, 4, -3), (0, 3, 0))]
    return finish("StoneSpider", idle, move, attack, fps=(4, 10, 10))


def night_knight():
    cell = (32, 32)
    dark = dict(navy="#2A1F3F", navy_l="#43315F", navy_d="#171027", gold="#8EA2C8", gold_l="#D7E3F7", gold_d="#56668A",
                hair="#3C4156", hair_l="#5C637F", hair_d="#232636", skin="#251A33", skin_d="#171027", eye="#FF3B5C",
                scarf="#A8243A", scarf_l="#FF3B5C", scarf_d="#6B1526", cape="#1A1226", cape_d="#0F0A18")
    with gen_leo.palette(dark):
        idle = [gen_leo.render(gen_leo.idle(i)) for i in range(4)]
        move = [gen_leo.render(gen_leo.run(i)) for i in range(6)]
        attack = [gen_leo.render(gen_leo.attack1(i)) for i in range(4)]
        hurt = [gen_leo.render(gen_leo.hurt(i)) for i in range(2)]
        death = [gen_leo.death(i) for i in range(5)]
    return finish("NightKnight", idle, move, attack, cell=cell, fps=(6, 10, 12), hurt=hurt, death=death)


# ---------- drawn here ----------
def _draw_bug(t, lunge=0, legs=0):
    img = new(20, 14)
    d = pen(img)
    shell, shell_l, shell_d = hexc("#4B7F3A"), hexc("#7FB04F"), hexc("#2F5426")
    thorn, thorn_l = hexc("#D8C98A"), hexc("#F7EDBE")
    for x in (3, 7, 11):                                           # thorns along the back
        h = 4 + lunge
        d.polygon([(x, 6), (x + 2, 6), (x + 1, 6 - h)], fill=thorn)
        d.point((x + 1, 6 - h), fill=thorn_l)
    d.ellipse([1, 4, 15, 11], fill=shell)
    d.arc([1, 4, 15, 11], 200, 340, fill=shell_l)
    d.line([(8, 5), (8, 10)], fill=shell_d)
    d.ellipse([13, 6, 19, 11], fill=hexc("#2A3A22"))              # head
    d.point((17, 8), fill=hexc("#FF5C57"))
    d.point((18, 7), fill=hexc("#FF5C57"))
    for i, x in enumerate((4, 8, 12)):                              # legs
        off = legs if i % 2 == 0 else -legs
        d.line([(x, 11), (x + off - 1, 13)], fill=hexc("#1F2A18"))
        d.line([(x + 1, 11), (x + 1 + off, 13)], fill=hexc("#1F2A18"))
    return pk.bevel(pk.outline(img), 0.12, 0.2)


def thorn_bug():
    idle = [in_cell(_draw_bug(0, 0, 0), 0, dy) for dy in (0, 0, -1, 0)]
    move = [in_cell(_draw_bug(0, 0, l), 0, 0) for l in (1, 0, -1, 0)]
    attack = [in_cell(_draw_bug(0, lu, 1), x, 0) for lu, x in ((0, -2), (2, 0), (2, 3), (1, 4))]
    return finish("ThornBug", idle, move, attack, fps=(4, 10, 10))


def _draw_mushroom(squash=0, stretch=0, spores=False, tilt=0):
    img = new(18, 22)
    d = pen(img)
    top = 5 + squash - stretch
    cap, cap_l, cap_d = hexc("#8E3FA8"), hexc("#B565CF"), hexc("#5A2470")
    stalk, stalk_d = hexc("#EADFB8"), hexc("#BFB38A")
    d.rectangle([6, top + 7, 11, 18 - stretch // 2], fill=stalk)
    d.rectangle([10, top + 7, 11, 18 - stretch // 2], fill=stalk_d)
    d.rectangle([5, 18 - stretch, 7, 20 - stretch], fill=hexc("#5C4A38"))
    d.rectangle([10, 18 - stretch, 12, 20 - stretch], fill=hexc("#5C4A38"))
    d.point((8, top + 10), fill=pk.OUTLINE)
    d.point((10, top + 10), fill=pk.OUTLINE)
    d.line([(8, top + 12), (10, top + 12)], fill=hexc("#7A5A50"))
    d.pieslice([0, top - 3 - squash, 17, top + 8], 180, 360, fill=cap)
    d.rectangle([0, top + 3, 17, top + 7], fill=cap)
    d.line([(1, top + 7), (16, top + 7)], fill=cap_d)
    d.arc([1, top - 2 - squash, 16, top + 6], 200, 280, fill=cap_l)
    for sx, sy in ((4, top + 1), (9, top - 1 - squash // 2), (13, top + 2)):
        d.rectangle([sx, sy, sx + 1, sy + 1], fill=hexc("#F2E27A"))
    if spores:
        for sx, sy in ((1, 3), (16, 2), (8, 0), (3, 8), (15, 9)):
            d.point((sx, sy), fill=hexc("#8FEA6B"))
    return pk.outline(img)


def mushroom_hopper():
    idle = [in_cell(_draw_mushroom(s), 0, 0) for s in (0, 1, 0, 0)]
    hop = [in_cell(_draw_mushroom(squash=2), 0, 0), in_cell(_draw_mushroom(stretch=2), 1, -4),
           in_cell(_draw_mushroom(stretch=1), 2, -6), in_cell(_draw_mushroom(squash=1), 3, -1)]
    attack = [in_cell(_draw_mushroom(squash=2), -1, 0), in_cell(_draw_mushroom(stretch=2, spores=True), 1, -3),
              in_cell(_draw_mushroom(stretch=1, spores=True), 3, -4), in_cell(_draw_mushroom(squash=1, spores=True), 4, 0)]
    return finish("MushroomHopper", idle, hop, attack, fps=(4, 8, 10))


def _draw_robot(bob=0, tread=0, arm=0, glow=False):
    img = new(18, 22)
    d = pen(img)
    steel, steel_l, steel_d = hexc("#5B6B86"), hexc("#8497B5"), hexc("#37425A")
    d.line([(8, 1 + bob), (8, 3 + bob)], fill=steel_d)
    d.rectangle([7, 0 + bob, 9, 1 + bob], fill=hexc("#FF5C57") if glow else hexc("#C99A3B"))
    d.rectangle([3, 3 + bob, 14, 11 + bob], fill=steel)                       # head/torso block
    d.rectangle([3, 3 + bob, 14, 4 + bob], fill=steel_l)
    d.rectangle([5, 5 + bob, 13, 8 + bob], fill=hexc("#0F1626"))              # visor
    d.rectangle([9, 6 + bob, 13, 7 + bob], fill=hexc("#FF5C57") if glow else hexc("#FFB03B"))
    d.rectangle([4, 12 + bob, 13, 15 + bob], fill=steel_d)                    # chassis
    d.rectangle([6, 13 + bob, 7, 13 + bob], fill=hexc("#27D38C"))
    d.rectangle([2, 16, 15, 20], fill=hexc("#232A3B"))                        # tread
    for x in range(3 + tread % 3, 15, 3):
        d.rectangle([x, 17, x, 19], fill=hexc("#4B556D"))
    if arm:
        d.rectangle([14, 8 + bob, 14 + arm, 9 + bob], fill=steel_l)           # extended arm / cutter
        d.rectangle([14 + arm, 6 + bob, 15 + arm, 11 + bob], fill=hexc("#D9E2F2"))
    return pk.bevel(pk.outline(img))


def patrol_robot():
    idle = [in_cell(_draw_robot(b, 0), 0, 0) for b in (0, 0, 1, 0)]
    move = [in_cell(_draw_robot(b, t), 0, 0) for b, t in ((0, 0), (1, 1), (0, 2), (1, 3))]
    attack = [in_cell(_draw_robot(0, 0, a, True), x, 0) for a, x in ((0, -1), (2, 0), (4, 1), (2, 0))]
    return finish("PatrolRobot", idle, move, attack, fps=(4, 8, 10))


def _draw_zapper(glow=0, arcs=0, seed=1):
    img = new(24, 24)
    d = pen(img)
    rust, rust_l, rust_d = hexc("#8A5A3C"), hexc("#B07A4A"), hexc("#5A3A28")
    plate, plate_d = hexc("#7D8697"), hexc("#4B5365")
    d.ellipse([3, 12, 21, 23], fill=rust_d)                                    # scrap heap
    d.ellipse([5, 13, 19, 22], fill=rust)
    d.rectangle([4, 16, 9, 19], fill=plate)                                    # bent plates
    d.rectangle([15, 15, 20, 18], fill=plate_d)
    d.rectangle([7, 14, 10, 15], fill=rust_l)
    d.ellipse([13, 18, 19, 23], outline=hexc("#2B2F3A"), width=2)             # tyre
    d.rectangle([11, 7, 12, 17], fill=plate)                                   # coil mast
    for y in (8, 10, 12, 14):
        d.line([(10, y), (13, y)], fill=hexc("#C99A3B"))
    ball = hexc("#27B5F7") if glow < 2 else hexc("#BFEBFF")
    d.ellipse([9, 2, 14, 7], fill=ball)
    d.point((11, 4), fill=hexc("#FFFFFF"))
    rng = random.Random(seed)
    for _ in range(arcs * 3):                                                  # lightning arcs radiating from the ball
        a, x, y = rng.uniform(0, 2 * math.pi), 11.5, 4.5
        for _s in range(rng.randint(3, 6)):
            nx, ny = x + math.cos(a) * 2, y + math.sin(a) * 2
            d.line([(round(x), round(y)), (round(nx), round(ny))], fill=hexc("#BFEBFF"))
            x, y, a = nx, ny, a + rng.uniform(-0.9, 0.9)
    return pk.bevel(pk.outline(img))


def scrap_zapper():
    idle = [in_cell(_draw_zapper(g), 0, 0) for g in (0, 1, 0, 1)]
    attack = [in_cell(_draw_zapper(1)), in_cell(_draw_zapper(2, 1, 3)), in_cell(_draw_zapper(2, 3, 4)), in_cell(_draw_zapper(2, 2, 5))]
    return finish("ScrapZapper", idle, list(idle), attack, fps=(4, 4, 10))


BUILDERS = [bat, ghost, stone_spider, night_knight, thorn_bug, mushroom_hopper, patrol_robot, scrap_zapper]


def build():
    all_rows = {}
    for fn in BUILDERS:
        all_rows[fn.__name__] = fn()
    return all_rows


if __name__ == "__main__":
    rows = build()
    if len(sys.argv) > 1:
        flat = []
        for name, anims in rows.items():
            flat.append([f if f.size[0] <= 32 else f for a in anims for f in a])
        pk.preview(flat, sys.argv[1], zoom=2)
