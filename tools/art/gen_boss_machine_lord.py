"""Boss 3: Cỗ Máy Nổi Loạn (RogueMachine, 64x64 logical) and boss 4: Chúa Tể Malakor (Malakor, 48x64)."""
import math
import pixel_kit as pk
from pixel_kit import hexc, new, pen
from boss_common import build_boss

IRON, IRON_L, IRON_D = hexc("#5C6578"), hexc("#8A95AC"), hexc("#323A4B")
RUST, FIRE, FIRE_L = hexc("#8A4B2D"), hexc("#FF7A3B"), hexc("#FFD27A")
STEAM, LASER = hexc("#DDE6F2"), hexc("#FF3B5C")


def machine(dy=0, left=0, right=0, laser=0, steam=0, glow=0, lean=0):
    """left/right: piston arm extension (0 raised ... 3 slammed)."""
    img = new(64, 64)
    d = pen(img)
    g = 62
    d.rectangle([6, g - 6, 57, g], fill=hexc("#232A3B"))                          # treads
    for x in range(8, 56, 6):
        d.rectangle([x, g - 4, x + 2, g - 1], fill=hexc("#3F485C"))
    d.rectangle([14, 14 + dy, 49, g - 6], fill=IRON)                              # main body
    d.rectangle([14, 14 + dy, 49, 18 + dy], fill=IRON_L)
    d.rectangle([46, 20 + dy, 49, g - 8], fill=IRON_D)
    d.rectangle([18, 30 + dy, 45, 52 + dy], fill=hexc("#1A2030"))                  # boiler window
    d.ellipse([22, 33 + dy, 41, 50 + dy], fill=RUST if not glow else FIRE)
    d.ellipse([27, 38 + dy, 36, 47 + dy], fill=FIRE if glow else hexc("#B25A32"))
    if glow:
        d.ellipse([29, 40 + dy, 34, 45 + dy], fill=FIRE_L)
    for x in (18, 28, 38, 45):
        d.ellipse([x, 22 + dy, x + 2, 24 + dy], fill=IRON_D)                      # rivets
    d.rectangle([22, 6 + dy, 41, 16 + dy], fill=IRON_D)                           # head
    d.rectangle([25, 9 + dy, 38, 12 + dy], fill=LASER if laser else hexc("#FFB03B"))
    d.line([(31, 2 + dy), (31, 6 + dy)], fill=IRON_L)
    d.rectangle([29, 0 + dy, 33, 3 + dy], fill=LASER if laser else hexc("#C99A3B"))
    for x, ext in ((6, left), (50, right)):                                       # piston arms
        top = 20 + dy
        d.rectangle([x, top, x + 7, top + 5 + ext * 6], fill=IRON_L)
        d.rectangle([x - 1, top + 5 + ext * 6, x + 8, top + 11 + ext * 6], fill=IRON_D)
        d.rectangle([x + 2, top + 11 + ext * 6, x + 5, min(top + 17 + ext * 6, g - 6)], fill=IRON_L)
    for sx in (24, 36, 44):                                                       # steam stacks
        d.rectangle([sx, 10 + dy - 4 * 0, sx + 2, 13 + dy], fill=IRON_D) if sx > 40 else None
    if laser:
        d.rectangle([0, 10 + dy, 24, 11 + dy], fill=LASER)
        d.rectangle([0, 10 + dy, 24, 10 + dy], fill=hexc("#FFD2DA"))
    if steam:
        for i in range(steam * 3):
            r = 3 + i
            d.ellipse([50 - r + i * 2, 4 - i + dy, 50 + r + i * 2, 4 + r - i + dy], outline=STEAM)
    return pk.bevel(pk.outline(img))


def build_machine():
    idle = [machine(dy=d, glow=g) for d, g in ((0, 0), (0, 1), (1, 1), (1, 0))]
    move = [machine(dy=d, left=l, right=1 - l) for d, l in ((0, 0), (1, 1), (0, 1), (1, 0))]
    a1 = [machine(left=0, right=0), machine(left=0, right=3), machine(left=3, right=3, dy=1), machine(left=1, right=1)]
    a2 = [machine(), machine(laser=0, glow=1), machine(laser=1, glow=1), machine(laser=1, glow=1, dy=1)]
    a3 = [machine(glow=1), machine(steam=1, glow=1), machine(steam=2, glow=1), machine(steam=3, glow=0)]
    return build_boss("RogueMachine", (64, 64), idle, move, [a1, a2, a3])


# ---- Malakor ----
ROBE, ROBE_L, ROBE_D = hexc("#2B1F3F"), hexc("#43315F"), hexc("#150F24")
MEYE, MGOLD = hexc("#FF3B5C"), hexc("#8EA2C8")


def malakor(dy=0, sway=0, slash=0, thrust=0, arms_up=0, aura=0, blade_ang=-60):
    img = new(48, 64)
    d = pen(img)
    cx = 22 + sway
    if aura:
        for r in range(aura):
            d.ellipse([cx - 14 - r * 3, 22 - r * 3 + dy, cx + 14 + r * 3, 60 + r * 2], outline=hexc("#6B2A9A"))
    d.polygon([(cx - 6, 20 + dy), (cx + 6, 20 + dy), (cx + 14, 60), (cx - 14, 60)], fill=ROBE)   # robe
    d.polygon([(cx - 14, 60), (cx - 10, 52), (cx - 6, 61), (cx - 2, 53), (cx + 3, 62), (cx + 8, 53), (cx + 14, 60)], fill=ROBE_D)
    d.line([(cx, 24 + dy), (cx, 58)], fill=ROBE_L)
    d.rectangle([cx - 8, 20 + dy, cx + 8, 24 + dy], fill=MGOLD)                    # pauldron collar
    d.ellipse([cx - 6, 6 + dy, cx + 6, 20 + dy], fill=ROBE_D)                      # hood
    d.rectangle([cx - 3, 12 + dy, cx + 4, 15 + dy], fill=hexc("#07040C"))
    d.rectangle([cx - 2, 13 + dy, cx - 1, 13 + dy], fill=MEYE)
    d.rectangle([cx + 2, 13 + dy, cx + 3, 13 + dy], fill=MEYE)
    for hx, hang in ((cx - 5, 150), (cx + 5, 30)):                                 # crown horns
        a = math.radians(hang)
        d.line([(hx, 9 + dy), (round(hx + 7 * math.cos(a)), round(9 + dy - 8 * math.sin(a)))], fill=MGOLD, width=2)
    sx, sy = cx + 8, 26 + dy                                                       # sword arm
    hy = 16 + dy if arms_up else 34 + dy
    hx = cx + 12 + thrust * 4
    d.line([(sx, sy), (hx, hy)], fill=ROBE_L, width=3)
    a = math.radians(blade_ang + slash)
    tip = (round(hx + 22 * math.cos(a)), round(hy - 22 * math.sin(a)))
    d.line([(hx, hy), tip], fill=hexc("#A9B6D6"), width=2)
    d.line([(hx - 3, hy - 1), (hx + 3, hy + 1)], fill=MGOLD, width=2)
    if slash:
        d.arc([hx - 20, hy - 24, hx + 20, hy + 16], 290, 350, fill=hexc("#B58CFF"), width=2)
    if arms_up:
        d.line([(cx - 8, 26 + dy), (cx - 14, 12 + dy)], fill=ROBE_L, width=3)
    else:
        d.line([(cx - 8, 26 + dy), (cx - 12, 38 + dy)], fill=ROBE_L, width=3)
    return pk.bevel(pk.outline(img), 0.1, 0.2)


def build_malakor():
    idle = [malakor(dy=d, sway=s) for d, s in ((0, 0), (-1, 0), (-2, 1), (-1, 0))]
    move = [malakor(dy=-1 - (i % 2), sway=s) for i, s in enumerate((-2, 0, 2, 0))]
    a1 = [malakor(blade_ang=100, arms_up=1), malakor(blade_ang=40, slash=1, thrust=1), malakor(blade_ang=-10, slash=1, thrust=2), malakor(blade_ang=-50)]
    a2 = [malakor(), pk.dither_fade(malakor(thrust=1), 0.7), pk.dither_fade(malakor(sway=6, thrust=3, blade_ang=0), 0.3), malakor(sway=8, thrust=3, blade_ang=0)]
    a3 = [malakor(arms_up=1, blade_ang=95), malakor(arms_up=1, blade_ang=95, aura=1), malakor(arms_up=1, blade_ang=95, aura=2), malakor(arms_up=1, blade_ang=95, aura=3)]
    return build_boss("Malakor", (48, 64), idle, move, [a1, a2, a3], fps=(5, 7, 10))
