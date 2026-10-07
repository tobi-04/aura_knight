"""Shared helpers for the Aura Knight procedural pixel art generators.

All art is drawn on a *logical* grid and upscaled x2 with nearest neighbour, so every sprite in the
game shows 2x2 pixel blocks at PPU 32 (a 16 px logical tile becomes a 32 px game tile).
"""
import json
import os
from PIL import Image, ImageDraw

SCALE = 2
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(REPO, "Assets", "_Project", "Art")
CACHE = os.environ.get("AURA_ART_CACHE", os.path.join(REPO, "tools", "art", ".cache"))


def hexc(value, alpha=255):
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


OUTLINE = hexc("#0B0F1C")


def new(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def pen(img):
    return ImageDraw.Draw(img)


def outline(img, color=OUTLINE, diagonal=False):
    """Adds a 1 px outline around the opaque pixels (4-neighbour by default)."""
    w, h = img.size
    src = img.load()
    out = img.copy()
    dst = out.load()
    steps = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if diagonal:
        steps += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
    for y in range(h):
        for x in range(w):
            if src[x, y][3] != 0:
                continue
            for dx, dy in steps:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] != 0 and src[nx, ny] != color:
                    dst[x, y] = color
                    break
    return out


def shear(img, tilt, pivot_y):
    """Shifts rows horizontally: rows above pivot_y lean by up to `tilt` pixels (positive = right)."""
    w, h = img.size
    out = new(w, h)
    for y in range(h):
        shift = round(tilt * (pivot_y - y) / max(pivot_y, 1)) if y < pivot_y else 0
        out.paste(img.crop((0, y, w, y + 1)), (shift, y))
    return out


def shift(img, dx, dy):
    out = new(*img.size)
    out.paste(img, (dx, dy))
    return out


def flip(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def upscale(img, k=SCALE):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def tint(img, rgb, amount):
    """Blends opaque pixels towards rgb (used for recolours and hit flashes)."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (round(r + (rgb[0] - r) * amount), round(g + (rgb[1] - g) * amount),
                            round(b + (rgb[2] - b) * amount), a)
    return out


def write_png(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)


class Sheet:
    """Rows of animation frames (logical-resolution images) packed into one upscaled PNG + JSON manifest."""

    def __init__(self, name, cell_w, cell_h, pivot=(0.5, 0.5)):
        self.name, self.cw, self.ch, self.pivot = name, cell_w, cell_h, pivot
        self.anims = []

    def add(self, anim, frames, fps, loop):
        for f in frames:
            if f.size != (self.cw, self.ch):
                raise ValueError(f"{self.name}/{anim}: frame {f.size} != cell {(self.cw, self.ch)}")
        self.anims.append((anim, frames, fps, loop))

    def save(self, folder):
        cols = max(len(a[1]) for a in self.anims)
        sheet = new(cols * self.cw, len(self.anims) * self.ch)
        meta = {"name": self.name, "cellWidth": self.cw * SCALE, "cellHeight": self.ch * SCALE,
                "pivotX": self.pivot[0], "pivotY": self.pivot[1], "pixelsPerUnit": 32, "anims": []}
        for row, (anim, frames, fps, loop) in enumerate(self.anims):
            for i, f in enumerate(frames):
                sheet.paste(f, (i * self.cw, row * self.ch))
            meta["anims"].append({"name": anim, "row": row, "frames": len(frames), "fps": fps, "loop": loop})
        write_png(upscale(sheet), os.path.join(folder, self.name + ".png"))
        with open(os.path.join(folder, self.name + ".sheet.json"), "w") as fh:
            json.dump(meta, fh, indent=2)
            fh.write("\n")


def preview(frames_rows, path, bg=(52, 56, 72, 255), zoom=2):
    """Writes a contact sheet for eyeballing (not part of the game assets)."""
    w = max(sum(f.width for f in row) for row in frames_rows)
    h = sum(max(f.height for f in row) for row in frames_rows)
    img = Image.new("RGBA", (w, h), bg)
    y = 0
    for row in frames_rows:
        x = 0
        for f in row:
            img.alpha_composite(f, (x, y))
            x += f.width
        y += max(f.height for f in row)
    write_png(upscale(img, zoom), path)


def bevel(img, light=0.18, shadow=0.28):
    """Cheap pixel-art shading: opaque pixels with empty space above/left get lighter, below/right darker."""
    w, h = img.size
    src = img.load()
    out = img.copy()
    dst = out.load()

    def empty(x, y):
        return not (0 <= x < w and 0 <= y < h) or src[x, y][3] == 0

    for y in range(h):
        for x in range(w):
            r, g, b, a = src[x, y]
            if not a:
                continue
            if empty(x, y - 1) or empty(x - 1, y):
                k, tgt = light, 255
            elif empty(x, y + 1) or empty(x + 1, y):
                k, tgt = shadow, 0
            else:
                continue
            dst[x, y] = (round(r + (tgt - r) * k), round(g + (tgt - g) * k), round(b + (tgt - b) * k), a)
    return out


def dither_fade(img, level):
    """Removes opaque pixels with a 4x4 ordered dither: level 0 = intact, 1 = gone."""
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            if px[x, y][3] and level * 16 > bayer[y % 4][x % 4] + 0.5:
                px[x, y] = (0, 0, 0, 0)
    return out


def region_shift(img, box, dx, dy):
    """Moves the pixels inside box=(x0,y0,x1,y1) by (dx,dy), leaving the rest in place."""
    part = img.crop(box)
    out = img.copy()
    out.paste(new(*part.size), box[:2])
    out.alpha_composite(part, (box[0] + dx, box[1] + dy))
    return out


def recolor(img, mapping):
    """Replaces exact RGB values (mapping {(r,g,b): (r,g,b)}), keeping alpha."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in mapping:
                px[x, y] = mapping[(r, g, b)] + (a,)
    return out


def place(sprite, cell, dx=0, dy=0, bottom=True):
    """Pastes a sprite into a cell-sized transparent image (bottom-centre anchored by default)."""
    out = new(*cell)
    x = (cell[0] - sprite.width) // 2 + dx
    y = (cell[1] - sprite.height - 1 if bottom else (cell[1] - sprite.height) // 2) + dy
    out.alpha_composite(sprite, (x, y))
    return out


def standard_hurt_death(idle_frame, fall=True):
    """Generic Hurt (white flash + recoil) and Death (flash, topple, dither fade) frames from one idle frame."""
    flash = tint(idle_frame, (255, 255, 255), 0.7)
    hurt = [shift(flash, -2, 0), shift(tint(idle_frame, (255, 90, 90), 0.35), -1, 0)]
    death = [shift(flash, -2, 0), dither_fade(shift(idle_frame, -2, 1), 0.3), dither_fade(shift(idle_frame, -3, 2), 0.6),
             dither_fade(shift(idle_frame, -3, 3), 0.85)]
    if fall:
        death = [death[0]] + [shift(f, 0, i + 1) for i, f in enumerate(death[1:])]
    return hurt, death
