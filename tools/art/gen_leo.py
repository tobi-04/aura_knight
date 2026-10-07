"""Generates Leo, the Order of the Sun knight (GDD section 0 #7), as a 32x32 logical cell (64x64 px at PPU 32).

Brown hair, dark armour #1B2233 with gold trim #C99A3B, green scarf, sun shield. Drawn facing right.
Run: python3 gen_leo.py [preview.png]
"""
import math
import sys
from PIL import ImageDraw
import pixel_kit as pk
from pixel_kit import hexc, new, pen

C = {k: hexc(v) for k, v in dict(
    navy="#1B2233", navy_l="#2E3B58", navy_d="#121827", gold="#C99A3B", gold_l="#F0CC6E", gold_d="#8A6422",
    hair="#6B4226", hair_l="#8F5C35", hair_d="#452814", skin="#E3B48C", skin_d="#B98A66", eye="#12202E",
    scarf="#1FA77A", scarf_l="#47DBA1", scarf_d="#146B50", cape="#3A1F2B", cape_d="#26141C",
    steel="#DCE6F5", blade="#EBC96B", blade_l="#FFF3BF", white="#FFFFFF", shadow="#0B0F1C").items()}
CX = 14
HIP = 21


class palette:
    """Context manager that swaps colours in C (used to reskin Leo's rig for the Night Knight)."""

    def __init__(self, overrides):
        self.overrides, self.saved = {k: hexc(v) for k, v in overrides.items()}, {}

    def __enter__(self):
        self.saved = dict(C)
        C.update(self.overrides)

    def __exit__(self, *exc):
        C.clear()
        C.update(self.saved)

FEET = 30


def pose(**kw):
    base = dict(dy=0, hdy=0, back=(CX - 3, FEET), front=(CX + 3, FEET), tilt=0, sword=None, sword_front=False,
                shield=(CX + 5, 19), scarf=0, cape=0, eyes="open", slash=None, flash=0.0, speed=False)
    base.update(kw)
    return base


def line(d, a, b, color, width=1):
    d.line([a, b], fill=color, width=width)


def draw_leg(d, hip, foot, shade):
    fx, fy = foot
    line(d, hip, (fx, fy - 3), C["navy"] if not shade else C["navy_d"], 3)
    d.rectangle([fx - 2, fy - 3, fx + 2, fy], fill=C["navy_d"])      # boot
    d.rectangle([fx - 2, fy - 3, fx + 2, fy - 3], fill=C["gold"])    # gold cuff
    d.point((fx + 2, fy), fill=C["gold_d"])                          # toe cap


def draw_sword(d, hx, hy, angle, length):
    a = math.radians(angle)
    tip = (round(hx + length * math.cos(a)), round(hy - length * math.sin(a)))
    base = (round(hx + 2 * math.cos(a)), round(hy - 2 * math.sin(a)))
    line(d, base, tip, C["blade"])
    step = (1, 0) if abs(tip[1] - base[1]) >= abs(tip[0] - base[0]) else (0, 1)   # second line = 2 px wide blade
    line(d, (base[0] + step[0], base[1] + step[1]), (tip[0] + step[0], tip[1] + step[1]), C["blade"])
    d.point(tip, fill=C["blade_l"])
    nx, ny = -math.sin(a), -math.cos(a)                              # crossguard perpendicular to blade
    for s in (-2, -1, 0, 1, 2):
        d.point((round(base[0] + nx * s), round(base[1] + ny * s)), fill=C["gold"])
    d.point((round(hx), round(hy)), fill=C["gold_d"])
    return tip


def draw_shield(d, sx, sy):
    kite = [(sx - 4, sy - 6), (sx + 4, sy - 6), (sx + 4, sy), (sx, sy + 6), (sx - 4, sy)]
    d.polygon(kite, fill=C["gold"])
    inner = [(sx - 3, sy - 5), (sx + 3, sy - 5), (sx + 3, sy), (sx, sy + 4), (sx - 3, sy)]
    d.polygon(inner, fill=C["navy_d"])
    d.rectangle([sx - 1, sy - 3, sx + 1, sy - 1], fill=C["gold_l"])   # sun disc
    for p in [(sx, sy - 5), (sx - 3, sy - 2), (sx + 3, sy - 2), (sx, sy + 1), (sx - 2, sy - 4), (sx + 2, sy - 4)]:
        d.point(p, fill=C["gold"])
    d.point((sx, sy - 2), fill=C["gold"])


def render(p):
    img = new(32, 32)
    d = pen(img)
    dy, hdy = p["dy"], p["hdy"]
    hip = (CX, HIP + dy)
    ht = 1 + dy + hdy
    # cape (behind everything)
    w = p["cape"]
    d.polygon([(CX - 3, 12 + dy), (CX - 5, 12 + dy), (CX - 9 + w, 23 + dy), (CX - 8 + w, 26 + dy),
               (CX - 6 + w, 24 + dy), (CX - 5 + w, 28 + dy), (CX - 3, 22 + dy)], fill=C["cape"])
    d.line([(CX - 5, 13 + dy), (CX - 8 + w, 24 + dy)], fill=C["cape_d"])
    # sword held behind the body when idle-ish
    if p["sword"] and not p["sword_front"]:
        draw_sword(d, *p["sword"])
    draw_leg(d, (hip[0] - 1, hip[1]), p["back"], True)
    # torso
    d.rectangle([CX - 4, 12 + dy, CX + 3, 21 + dy], fill=C["navy"])
    d.rectangle([CX + 2, 13 + dy, CX + 3, 20 + dy], fill=C["navy_l"])
    d.rectangle([CX - 4, 12 + dy, CX - 2, 13 + dy], fill=C["gold"])          # pauldron
    d.rectangle([CX + 2, 12 + dy, CX + 3, 12 + dy], fill=C["gold"])
    d.rectangle([CX - 4, 19 + dy, CX + 3, 19 + dy], fill=C["gold"])          # belt
    d.rectangle([CX, 19 + dy, CX + 1, 20 + dy], fill=C["gold_l"])           # buckle
    d.point((CX - 1, 15 + dy), fill=C["gold_d"])
    d.polygon([(CX - 1, 20 + dy), (CX + 2, 20 + dy), (CX + 2, 25 + dy), (CX - 1, 25 + dy)], fill=C["navy_l"])  # tabard
    d.line([(CX - 1, 20 + dy), (CX - 1, 25 + dy)], fill=C["scarf_d"])
    d.line([(CX + 2, 20 + dy), (CX + 2, 25 + dy)], fill=C["scarf_d"])
    draw_leg(d, (hip[0] + 1, hip[1]), p["front"], False)
    # head
    d.rectangle([CX - 2, ht + 3, CX + 3, ht + 8], fill=C["skin"])
    d.rectangle([CX - 2, ht + 8, CX + 3, ht + 8], fill=C["skin_d"])
    d.rectangle([CX - 4, ht + 1, CX + 3, ht + 4], fill=C["hair"])              # hair cap
    d.rectangle([CX - 5, ht + 3, CX - 3, ht + 9], fill=C["hair_d"])            # hair at the back
    d.rectangle([CX - 2, ht + 1, CX + 1, ht + 1], fill=C["hair_l"])
    for x in (CX + 1, CX + 3):                                                 # fringe locks over the brow
        d.point((x, ht + 5), fill=C["hair"])
    for x, y in ((CX - 1, ht), (CX + 1, ht), (CX - 3, ht + 1)):                # messy tufts
        d.point((x, y), fill=C["hair_l"])
    if p["eyes"] == "open":
        d.point((CX + 2, ht + 6), fill=C["eye"])
        d.point((CX + 3, ht + 6), fill=C["scarf_l"])
    else:
        d.line([(CX + 1, ht + 6), (CX + 3, ht + 6)], fill=C["skin_d"])
    # scarf collar + tail
    d.rectangle([CX - 4, ht + 9, CX + 3, ht + 11], fill=C["scarf"])
    d.rectangle([CX - 4, ht + 11, CX + 3, ht + 11], fill=C["scarf_d"])
    d.rectangle([CX - 3, ht + 9, CX + 2, ht + 9], fill=C["scarf_l"])
    s = p["scarf"]
    for i in range(1, 7):
        wob = round(math.sin(i * 0.9 + s) * 1.2)
        d.rectangle([CX - 4 - i, ht + 9 + wob + i // 4, CX - 4 - i, ht + 10 + wob + i // 4], fill=C["scarf"] if i % 2 else C["scarf_d"])
    # shield first, sword arm over it when swinging
    draw_shield(d, *p["shield"])
    if p["sword"] and p["sword_front"]:
        hx, hy = p["sword"][:2]
        line(d, (CX + 2, 14 + dy), (hx, hy), C["navy_l"], 2)                  # arm
        d.point((hx, hy), fill=C["gold"])
        draw_sword(d, *p["sword"])
    if p["slash"]:
        cx, cy, r, a0, a1 = p["slash"]
        d.arc([cx - r, cy - r, cx + r, cy + r], a0, a1, fill=C["steel"], width=2)
        d.arc([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], a0 + 8, a1 - 8, fill=C["blade_l"], width=1)
    img = pk.outline(pk.shear(img, p["tilt"], HIP + dy) if p["tilt"] else img, pk.OUTLINE)
    if p["speed"]:
        sd = pen(img)
        for y in (14, 19, 24):
            sd.line([(1, y), (5 + y % 3, y)], fill=C["steel"])
    if p["flash"]:
        img = pk.tint(img, (255, 255, 255), p["flash"])
    return img


def idle(n):
    dy = [0, 0, 1, 1][n]
    return pose(dy=dy, hdy=[0, 1, 0, 0][n], scarf=n * 0.8, cape=n % 2,
                back=(CX - 3, FEET), front=(CX + 3, FEET), sword=(CX - 3, 18 + dy, 235, 9))


def run(n):
    t = n / 6 * 2 * math.pi
    f = (CX + 1 + round(4 * math.sin(t)), FEET - round(max(0, math.cos(t)) * 3))
    b = (CX - 1 - round(4 * math.sin(t)), FEET - round(max(0, -math.cos(t)) * 3))
    return pose(dy=-round(abs(math.sin(t))), front=f, back=b, tilt=2, scarf=n * 1.1, cape=-2 - n % 2,
                sword=(CX - 3, 17, 205, 9), shield=(CX + 6, 19))


def jump(n):
    return pose(dy=-1, back=(CX - 2, 26 + n), front=(CX + 4, 24 + n), tilt=1, scarf=2 + n, cape=-3,
                sword=(CX - 3, 16, 215, 9), shield=(CX + 6, 17))


def fall(n):
    return pose(dy=0, back=(CX - 4, 29), front=(CX + 4, 28 - n), scarf=-1 - n, cape=2, hdy=0,
                sword=(CX - 3, 15, 130, 9), shield=(CX + 6, 18 - n))


def attack1(n):
    arms = [dict(sword=(CX + 1, 10, 125, 11), tilt=-1),
            dict(sword=(CX + 7, 14, 12, 12), slash=(CX + 3, 16, 12, 285, 360), tilt=2),
            dict(sword=(CX + 7, 18, -40, 12), slash=(CX + 3, 16, 12, 330, 40), tilt=2),
            dict(sword=(CX + 5, 18, -25, 10), tilt=0)][n]
    return pose(sword_front=True, front=(CX + 4, FEET), back=(CX - 4, FEET), shield=(CX + 3, 20), **arms)


def attack2(n):
    arms = [dict(sword=(CX + 5, 22, -45, 11), tilt=1),
            dict(sword=(CX + 8, 17, 15, 12), slash=(CX + 3, 18, 12, 300, 20), tilt=3),
            dict(sword=(CX + 5, 9, 80, 12), slash=(CX + 3, 18, 12, 20, 100), tilt=2),
            dict(sword=(CX + 4, 12, 60, 10), tilt=0)][n]
    return pose(sword_front=True, front=(CX + 5, FEET), back=(CX - 4, FEET), shield=(CX + 2, 19), **arms)


def air_attack(n):
    p = attack1(n)
    p.update(back=(CX - 3, 26), front=(CX + 3, 27))
    return p


def attack_up(n):
    sw = [(CX + 3, 9, 100, 10), (CX + 3, 5, 90, 12), (CX + 3, 5, 90, 12), (CX + 3, 8, 80, 9)][n]
    sl = [None, (CX + 3, 8, 11, 200, 340), (CX + 3, 8, 11, 230, 310), None][n]
    return pose(sword_front=True, sword=sw, slash=sl, shield=(CX + 6, 20))


def air_attack_up(n):
    p = attack_up(n)
    p.update(back=(CX - 3, 26), front=(CX + 3, 27))
    return p


def air_attack_down(n):
    sw = [(CX + 3, 22, -80, 8), (CX + 2, 25, -90, 11), (CX + 2, 25, -90, 11)][n]
    sl = [None, (CX + 2, 22, 7, 40, 140), (CX + 2, 22, 7, 50, 130)][n]
    return pose(sword_front=True, sword=sw, slash=sl, back=(CX - 3, 23), front=(CX + 4, 22), dy=-3,
                shield=(CX + 6, 14))


def dash(n):
    return pose(dy=3, tilt=5, back=(CX - 8, 28 + n), front=(CX + 3, 27), scarf=-3 + n, cape=-6, speed=True,
                sword=(CX - 3, 19, 190, 10), shield=(CX + 7, 21), hdy=1)


def slide(n):
    return pose(dy=8, hdy=1, tilt=5, back=(CX - 3, 30), front=(CX + 9, 30 - n), scarf=-2, cape=-5,
                sword=(CX - 3, 26, 195, 9), shield=(CX + 6, 27))


def wall_slide(n):
    return pose(dy=1, back=(CX + 1, 25 + n), front=(CX + 3, 28), tilt=0, scarf=3 + n, cape=1,
                shield=(CX + 6, 17), sword=(CX - 3, 16, 250, 9))


def hurt(n):
    return pose(dy=n, tilt=-3, eyes="closed", back=(CX - 5, FEET), front=(CX + 1, FEET - n), cape=3, scarf=2,
                sword=(CX - 3, 14, 150, 9), shield=(CX + 4, 18), flash=0.65 if n == 0 else 0.0)


def death(n):
    base = pose(eyes="closed", cape=3)
    if n == 0:
        base.update(dy=1, tilt=-4, back=(CX - 5, FEET), front=(CX + 2, FEET), flash=0.5, sword=(CX - 3, 15, 150, 9))
        return render(base)
    if n == 1:
        base.update(dy=6, tilt=-3, back=(CX - 5, FEET), front=(CX + 4, FEET), shield=(CX + 6, 24), sword=(CX - 4, 22, 200, 9))
        return render(base)
    standing = render(pose(eyes="closed", back=(CX - 3, FEET), front=(CX + 3, FEET), sword=(CX - 3, 18, 235, 9)))
    lying = standing.rotate(-90)                                  # fall onto the back: a clean 90 degree turn
    box = lying.getbbox()
    lying = pk.shift(lying, 0, 31 - box[3])                       # rest on the floor row
    return pk.shift(lying, [0, 0, -1, 0, 1][n], 0)


def swim(n):
    return pose(dy=-2, tilt=9, back=(CX - 8, 25 + (n % 2) * 3), front=(CX - 5, 28 - (n % 2) * 3), scarf=n,
                cape=-6, sword=(CX - 3, 15, 200, 9), shield=(CX + 6, 17), hdy=1)


ANIMS = [  # name, frame builder, frame count, fps, loop
    ("Idle", idle, 4, 6, True), ("Run", run, 6, 12, True), ("Jump", jump, 2, 8, True), ("Fall", fall, 2, 8, True),
    ("Attack1", attack1, 4, 16, False), ("Attack2", attack2, 4, 16, False), ("AttackUp", attack_up, 4, 16, False),
    ("AirAttack", air_attack, 4, 16, False), ("AirAttackUp", air_attack_up, 4, 16, False),
    ("AirAttackDown", air_attack_down, 3, 14, False), ("Dash", dash, 2, 12, True), ("Slide", slide, 2, 8, True),
    ("WallSlide", wall_slide, 2, 8, True), ("Hurt", hurt, 2, 8, False), ("Death", death, 5, 8, False),
    ("Swim", swim, 4, 8, True)]


def build(out_dir=None):
    out_dir = out_dir or pk.ART + "/Characters/Leo"
    sheet = pk.Sheet("Leo", 32, 32)
    rows = []
    for name, fn, count, fps, loop in ANIMS:
        frames = []
        for i in range(count):
            r = fn(i)
            frames.append(r if hasattr(r, "size") else render(r))
        sheet.add(name, frames, fps, loop)
        rows.append(frames)
    sheet.save(out_dir)
    return rows


if __name__ == "__main__":
    rows = build()
    if len(sys.argv) > 1:
        pk.preview(rows, sys.argv[1], zoom=3)
