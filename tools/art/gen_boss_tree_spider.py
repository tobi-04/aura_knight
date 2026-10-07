"""Boss 1: Gốc Cây Mục Bóng Tối (RootTree, 48x48 logical) and boss 2: Nhện Đá Khổng Lồ (GiantStoneSpider, 64x48)."""
import math
import pixel_kit as pk
from pixel_kit import hexc, new, pen
from boss_common import build_boss

BARK, BARK_L, BARK_D = hexc("#4A3322"), hexc("#6B4B33"), hexc("#2A1C12")
LEAF, LEAF_D, DEAD = hexc("#1F3A24"), hexc("#12261A"), hexc("#8A5A2B")
CORE, CORE_L = hexc("#FFC857"), hexc("#FFF0B0")
EYE = hexc("#9BFF6B")


def branch(d, start, ang, length, width=3):
    a = math.radians(ang)
    end = (round(start[0] + length * math.cos(a)), round(start[1] - length * math.sin(a)))
    d.line([start, end], fill=BARK, width=width)
    for off in (-35, 0, 35):                                              # twig fingers
        b = math.radians(ang + off)
        d.line([end, (round(end[0] + 4 * math.cos(b)), round(end[1] - 4 * math.sin(b)))], fill=BARK_D)
    return end


def tree(sway=0, dy=0, mouth=0, arm_l=200, arm_r=-20, stab=0, seeds=False, glow=0, sweep=False):
    img = new(48, 48)
    d = pen(img)
    g = 46
    for tx, ty in ((6, g), (12, g + 1), (24, g + 1), (36, g + 1), (42, g)):  # roots
        d.line([(24, g - 6 + dy), (tx, min(ty, 47))], fill=BARK_D, width=3)
    if stab:                                                             # roots erupting in front of the boss
        for i, x in enumerate((38, 42, 46)):
            h = max(0, stab * 7 - i * 2)
            d.polygon([(x - 2, g), (x + 2, g), (x, g - h)], fill=BARK_L)
    d.polygon([(14, 41 + dy), (34, 41 + dy), (36, 16 + dy), (12, 16 + dy)], fill=BARK)  # trunk
    for x in (16, 21, 27, 31):
        d.line([(x, 19 + dy), (x + 1, 40 + dy)], fill=BARK_D)
    d.line([(13, 17 + dy), (14, 40 + dy)], fill=BARK_L)
    d.ellipse([6 + sway, 0 + dy, 42 + sway, 20 + dy], fill=LEAF)          # withered crown
    d.ellipse([10 + sway, 2 + dy, 24 + sway, 12 + dy], fill=LEAF_D)
    for x, y in ((9, 7), (15, 3), (30, 4), (37, 9), (24, 1), (34, 14)):
        d.rectangle([x + sway, y + dy, x + 1 + sway, y + 1 + dy], fill=DEAD)
    for x in (11, 36):
        d.rectangle([x, 21 + dy, x + 2, 24 + dy], fill=hexc("#3F5B2E"))   # moss
    d.rectangle([17, 22 + dy, 20, 24 + dy], fill=EYE)                    # eyes
    d.rectangle([28, 22 + dy, 31, 24 + dy], fill=EYE)
    d.line([(16, 20 + dy), (21, 22 + dy)], fill=BARK_D)
    d.line([(32, 20 + dy), (27, 22 + dy)], fill=BARK_D)
    if mouth:                                                            # open mouth reveals the glowing core
        d.ellipse([17, 28 + dy, 31, 38 + dy], fill=hexc("#150C08"))
        d.ellipse([20, 31 + dy, 28, 37 + dy], fill=CORE)
        d.rectangle([22, 33 + dy, 25, 35 + dy], fill=CORE_L)
    else:
        d.line([(18, 32 + dy), (30, 32 + dy)], fill=hexc("#150C08"), width=2)
        if glow:
            d.rectangle([22, 31 + dy, 25, 32 + dy], fill=CORE)
    hl = branch(d, (13, 21 + dy), arm_l, 15)
    hr = branch(d, (35, 21 + dy), arm_r, 15)
    if seeds:
        for i in range(3):
            d.ellipse([hr[0] + 2 + i * 4, hr[1] - 5 - i * 2, hr[0] + 5 + i * 4, hr[1] - 2 - i * 2], fill=hexc("#8BD450"))
    if sweep:
        d.arc([8, 4, 62, 60], 280, 350, fill=hexc("#C8B58A"), width=2)
    return pk.bevel(pk.outline(img))


def build_tree():
    idle = [tree(sway=s, dy=d) for s, d in ((0, 0), (1, 0), (1, 1), (0, 1))]
    move = [tree(sway=s, dy=d, arm_l=215 - s * 6, arm_r=-35 + s * 6) for s, d in ((0, 0), (1, 1), (0, 0), (-1, 1))]
    a1 = [tree(dy=1, stab=0, arm_l=250, arm_r=-70), tree(stab=1, arm_l=250, arm_r=-70), tree(stab=3, glow=1), tree(stab=2, glow=1)]
    a2 = [tree(arm_r=40), tree(arm_r=75, mouth=0), tree(arm_r=95, seeds=True, mouth=1), tree(arm_r=20, seeds=True)]
    a3 = [tree(arm_l=240, arm_r=120, dy=1), tree(arm_r=60, sway=-1), tree(arm_r=0, sweep=True, sway=2), tree(arm_r=-30, sway=1)]
    exposed = [tree(mouth=1, glow=1, dy=1), tree(mouth=1, glow=1, dy=2)]
    return build_boss("RootTree", (48, 48), idle, move, [a1, a2, a3], {"Exposed": (exposed, 4, True)})


# ---- Giant Stone Spider ----
ROCK, ROCK_L, ROCK_D = hexc("#6D7488"), hexc("#98A1B8"), hexc("#3C4256")
SPEYE = hexc("#FF5C57")


def spider(dy=0, leg=0, raise_front=0, mouth=0, web=False, drop=0):
    img = new(64, 48)
    d = pen(img)
    by = 22 + dy
    for i, (hx, tx, ty) in enumerate(((22, 4, 40), (24, 8, 44), (27, 14, 46), (30, 20, 47),
                                      (42, 60, 40), (40, 56, 44), (37, 50, 46), (34, 44, 47))):
        wob = leg if i % 2 == 0 else -leg
        front = i in (0, 4)
        ty2 = ty - (raise_front * 8 if front else 0) + wob
        mid = ((hx + tx) // 2, min(by - 10, ty2 - 12) + (6 if not front else 0))
        d.line([(hx, by + 4), mid], fill=ROCK_D, width=3)
        d.line([mid, (tx, ty2 + drop)], fill=ROCK, width=3)
    d.ellipse([14, by - 6, 50, by + 18], fill=ROCK)                        # abdomen
    d.ellipse([18, by - 4, 34, by + 6], fill=ROCK_L)
    for x, y in ((24, by + 8), (32, by + 2), (40, by + 9), (44, by + 3)):   # cracks / plates
        d.line([(x, y), (x + 4, y + 3)], fill=ROCK_D)
    d.line([(32, by - 4), (32, by + 14)], fill=ROCK_D)
    d.ellipse([40, by - 2, 58, by + 14], fill=ROCK)                         # head
    for ex, ey in ((46, by + 2), (51, by + 1), (49, by + 5), (54, by + 5)):
        d.rectangle([ex, ey, ex + 1, ey + 1], fill=SPEYE)
    if mouth:
        d.rectangle([52, by + 9, 57, by + 12], fill=hexc("#150C08"))
    if web:
        for i in range(4):
            d.line([(58 + i, by + 10), (63, by + 10 + (i - 2) * 3)], fill=hexc("#E8EEF8"))
    return pk.bevel(pk.outline(img))


def build_spider():
    idle = [spider(dy=d, leg=l) for d, l in ((0, 0), (0, 1), (1, 0), (0, -1))]
    move = [spider(dy=0, leg=l) for l in (3, 0, -3, 0)]
    a1 = [spider(dy=-12, leg=2, drop=-4), spider(dy=-14, leg=0, raise_front=1, drop=-6), spider(dy=10, leg=-1), spider(dy=4, leg=-2)]
    a2 = [spider(dy=1, mouth=0), spider(dy=2, mouth=1), spider(dy=2, mouth=1, web=True), spider(dy=1, web=True)]
    a3 = [spider(raise_front=1), spider(raise_front=1, mouth=1), spider(raise_front=1, mouth=1, leg=2), spider()]
    return build_boss("GiantStoneSpider", (64, 48), idle, move, [a1, a2, a3])
