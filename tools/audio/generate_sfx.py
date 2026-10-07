#!/usr/bin/env python3
"""Generates every Aura Knight SFX as a mono 16-bit 44.1 kHz WAV. Deterministic (fixed seeds): same output every run.

usage: python3 tools/audio/generate_sfx.py [output_dir]
default output: Assets/_Project/Audio/SFX
File naming: <SfxId>_<variant>.wav, where <SfxId> is a member of AuraKnight.Audio.SfxId.
"""
import os
import sys
import numpy as np
from synth import *


def burst(rng, dur, cutoff, power=3.0):
    n = seconds(dur)
    return lowpass(osc("noise", 0, n, rng), cutoff) * decay(n, power)


def thump(f0, f1, dur, power=2.5):
    n = seconds(dur)
    return osc("sine", glide(f0, f1, n), n) * decay(n, power)


def tone(kind, f0, f1, dur, rng=None, duty=0.5, power=2.0, curve=1.0):
    n = seconds(dur)
    return osc(kind, glide(f0, f1, n, curve), n, rng, duty) * decay(n, power)


def notes(kind, midis, step, length, duty=0.5, power=1.5):
    """Sequence of short notes (arpeggio); every note lasts `length` seconds, starts every `step` seconds."""
    out = np.zeros(seconds(step * (len(midis) - 1)) + seconds(length) + 1)
    for i, m in enumerate(midis):
        n = seconds(length)
        seg = osc(kind, midi_to_hz(m), n, None, duty) * decay(n, power)
        s = seconds(step * i)
        out[s:s + n] += seg
    return out


def footstep(rng, cut):
    return mix(burst(rng, 0.07, cut, 3.0) * 0.8, thump(140, 70, 0.08) * 0.7)


def whoosh(rng, dur, lo, hi, peak=0.5):
    n = seconds(dur)
    x = osc("noise", 0, n, rng)
    sweep = np.linspace(lo, hi, n)
    y = np.empty(n)
    acc = 0.0
    for i in range(n):
        a = 1.0 - np.exp(-2 * np.pi * sweep[i] / SR)
        acc += a * (x[i] - acc)
        y[i] = acc
    t = np.linspace(0, 1, n)
    return y * np.sin(np.pi * t ** peak)


def build(name, r):
    n_ = seconds
    if name == "Footstep_1": return footstep(r, 900)
    if name == "Footstep_2": return footstep(r, 1300) * 0.9
    if name == "Jump": return tone("square", 220, 540, 0.18, duty=0.4, power=1.3)
    if name == "Land": return mix(burst(r, 0.1, 700, 3), thump(110, 45, 0.14))
    if name == "Dash": return mix(whoosh(r, 0.22, 600, 5000, 0.6), tone("saw", 320, 140, 0.2) * 0.3)
    if name == "Slide":
        n = n_(0.32)
        trem = 0.7 + 0.3 * np.sin(2 * np.pi * 28 * np.arange(n) / SR)
        return lowpass(osc("noise", 0, n, r), 1800) * trem * adsr(n, 0.01, 0.05, 0.8, 0.12)
    if name == "WallSlide":
        n = n_(0.28)
        sq = 0.5 + 0.5 * osc("square", 38, n)
        return highpass(lowpass(osc("noise", 0, n, r), 4500), 900) * sq * adsr(n, 0.01, 0.04, 0.8, 0.08)
    if name == "SwordSwing": return whoosh(r, 0.16, 800, 6500, 0.9)
    if name == "SwordHit":
        return mix(burst(r, 0.09, 6000, 4), tone("square", 200, 55, 0.14, duty=0.3) * 0.7, thump(120, 50, 0.12) * 0.6)
    if name == "PlayerHurt":
        f = vibrato(glide(420, 110, n_(0.28)), n_(0.28), 22, 0.05)
        return mix(osc("saw", f, n_(0.28)) * decay(n_(0.28), 1.6) * 0.8, burst(r, 0.1, 4000, 3) * 0.5)
    if name == "PlayerDie":
        n = n_(0.9)
        body = bitcrush(osc("square", glide(440, 45, n, 0.6), n, None, 0.45) * decay(n, 1.5), 6, 2)
        return mix(body * 0.8, burst(r, 0.5, 1200, 2) * 0.4)
    if name == "AuraWind":
        return mix(whoosh(r, 0.35, 500, 4000, 0.8) * 0.6, tone("tri", 300, 900, 0.3, power=1.2) * 0.5)
    if name == "AuraFire":
        n = n_(0.35)
        crackle = (r.uniform(0, 1, n) > 0.93) * r.uniform(-1, 1, n)
        return mix(tone("saw", 140, 320, 0.35, power=1.3) * 0.6, lowpass(crackle, 6000) * decay(n, 1) * 1.2)
    if name == "AuraWater":
        n = n_(0.4)
        f = vibrato(glide(380, 760, n), n, 14, 0.12)
        return osc("sine", f, n) * adsr(n, 0.02, 0.1, 0.6, 0.15)
    if name == "AuraUnlock": return notes("square", [72, 76, 79, 84], 0.09, 0.3, 0.3, 1.2)
    if name == "SkillWind": return mix(whoosh(r, 0.5, 400, 6000, 0.7), tone("tri", 200, 700, 0.4, power=1.0) * 0.35)
    if name == "SkillFire":
        return mix(tone("saw", 700, 120, 0.4, power=1.5) * 0.7, burst(r, 0.35, 3500, 1.5) * 0.7)
    if name == "SkillWater":
        n = n_(0.5)
        up = glide(180, 640, n // 2)
        dn = glide(640, 280, n - n // 2)
        return osc("sine", np.concatenate([up, dn]), n) * adsr(n, 0.02, 0.1, 0.7, 0.2)
    if name == "Coin": return notes("square", [83, 88], 0.07, 0.2, 0.5, 1.5)
    if name == "Chest":
        return mix(thump(160, 60, 0.18) * 0.8, np.concatenate([np.zeros(n_(0.12)), notes("square", [67, 71, 74, 79, 83], 0.07, 0.28, 0.25, 1.4) * 0.7]))
    if name == "Altar": return notes("sine", [67, 71, 74, 79], 0.16, 0.9, 0.5, 1.6)
    if name == "UiTap": return tone("square", 880, 880, 0.045, duty=0.5, power=1.5) * 0.8
    if name == "UiBack": return tone("square", 660, 420, 0.08, duty=0.5, power=1.5) * 0.8
    if name == "BossRoar":
        n = n_(1.3)
        f = vibrato(glide(110, 55, n, 0.7), n, 17, 0.09)
        grr = lowpass(bitcrush(osc("saw", f, n), 5, 3), 2200) * adsr(n, 0.08, 0.2, 0.8, 0.5)
        return mix(grr, lowpass(osc("noise", 0, n, r), 900) * adsr(n, 0.1, 0.2, 0.5, 0.5) * 0.6)
    if name == "EnemyHit": return mix(tone("square", 320, 110, 0.1, duty=0.35), burst(r, 0.06, 5000, 3) * 0.6)
    if name == "EnemyDie":
        return mix(bitcrush(tone("square", 340, 50, 0.38, duty=0.4, power=1.3), 6, 2) * 0.8, burst(r, 0.3, 1800, 2) * 0.5)
    if name == "Respawn": return notes("tri", [60, 64, 67, 72, 76], 0.08, 0.35, power=1.3)
    raise KeyError(name)


NAMES = ["Footstep_1", "Footstep_2", "Jump", "Land", "Dash", "Slide", "WallSlide", "SwordSwing", "SwordHit",
         "PlayerHurt", "PlayerDie", "AuraWind", "AuraFire", "AuraWater", "AuraUnlock", "SkillWind", "SkillFire",
         "SkillWater", "Coin", "Chest", "Altar", "UiTap", "UiBack", "BossRoar", "EnemyHit", "EnemyDie", "Respawn"]


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Audio", "SFX")
    os.makedirs(out, exist_ok=True)
    total = 0
    for i, name in enumerate(NAMES):
        rng = np.random.default_rng(1000 + i)
        x = normalize_peak(build(name, rng), 0.9)
        # 3 ms fade in/out removes clicks at the cut points
        f = min(seconds(0.003), len(x) // 2)
        x[:f] *= np.linspace(0, 1, f)
        x[-f:] *= np.linspace(1, 0, f)
        path = os.path.join(out, name + ".wav")
        write_wav(path, x)
        total += os.path.getsize(path)
        print(f"{name:14s} {len(x) / SR:5.2f}s")
    print(f"total {total / 1024:.0f} KB")


if __name__ == "__main__":
    main()
