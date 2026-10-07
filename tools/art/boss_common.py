"""Shared plumbing for the boss sheets: standard clip set (Idle, Move, Attack1-3, Hurt, Death) from pose functions."""
import os
import pixel_kit as pk


def build_boss(name, cell, idle, move, attacks, extra=None, fps=(5, 7, 10)):
    """idle/move: lists of frames; attacks: 3 lists of frames; extra: {clip: (frames, fps, loop)}."""
    sheet = pk.Sheet(name, *cell)
    sheet.add("Idle", idle, fps[0], True)
    sheet.add("Move", move, fps[1], True)
    for i, frames in enumerate(attacks, 1):
        sheet.add(f"Attack{i}", frames, fps[2], False)
    hurt, death = pk.standard_hurt_death(idle[0], fall=False)
    death = [death[0]] + [pk.shift(f, 0, 2 * (i + 1)) for i, f in enumerate(death[1:])]
    sheet.add("Hurt", hurt, 10, False)
    sheet.add("Death", death, 6, False)
    for clip, (frames, f, loop) in (extra or {}).items():
        sheet.add(clip, frames, f, loop)
    sheet.save(os.path.join(pk.ART, "Bosses", name))
    return [idle, move] + attacks + [hurt, death] + [v[0] for v in (extra or {}).values()]
