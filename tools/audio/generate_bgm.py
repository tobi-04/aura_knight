#!/usr/bin/env python3
"""Generates the Aura Knight BGM as chiptune loops: <track>_explore.ogg and <track>_combat.ogg (combat only for region tracks).

Both layers of a track share tempo, bar count and chords and have the exact same sample length, so the game can crossfade
between them while keeping them in sync (timeSamples). Every layer is a full arrangement (combat is the busier one).
Deterministic: fixed seeds per track. Needs numpy and ffmpeg (libvorbis not required, the native vorbis encoder is used).

usage: python3 tools/audio/generate_bgm.py [output_dir]      default: Assets/_Project/Audio/BGM
"""
import os
import subprocess
import sys
import tempfile
import numpy as np
from synth import *

MAJOR = [0, 2, 4, 5, 7, 9, 11]
MINOR = [0, 2, 3, 5, 7, 8, 10]
DORIAN = [0, 2, 3, 5, 7, 9, 10]
PHRYG = [0, 1, 3, 5, 7, 8, 10]

# name: root midi (bass octave root), scale, bpm, chord degrees per bar (8 bars, played twice = 16 bars), wave of lead, combat?
TRACKS = {
    "hub":    dict(root=48, scale=MAJOR, bpm=88, prog=[0, 0, 5, 5, 3, 3, 4, 4], lead="tri", combat=True, seed=11),
    "forest": dict(root=50, scale=DORIAN, bpm=100, prog=[0, 0, 6, 6, 5, 5, 6, 6], lead="square", combat=True, seed=12),
    "cave":   dict(root=45, scale=MINOR, bpm=76, prog=[0, 0, 1, 1, 0, 0, 4, 4], lead="tri", combat=True, seed=13),
    "city":   dict(root=52, scale=PHRYG, bpm=116, prog=[0, 1, 0, 6, 0, 1, 5, 6], lead="saw", combat=True, seed=14),
    "castle": dict(root=50, scale=MINOR, bpm=108, prog=[0, 0, 3, 3, 5, 5, 4, 4], lead="square", combat=True, seed=15),
    "boss":   dict(root=48, scale=MINOR, bpm=150, prog=[0, 0, 5, 5, 3, 3, 4, 4], lead="saw", combat=False, seed=16, intense=True),
    "ending": dict(root=48, scale=MAJOR, bpm=72, prog=[0, 4, 5, 3, 0, 4, 5, 3], lead="tri", combat=False, seed=17),
}
BARS = 16


def degree_midi(root, scale, deg):
    return root + 12 * (deg // 7) + scale[deg % 7]


def place(buf, seg, start):
    end = min(len(buf), start + len(seg))
    if start < len(buf):
        buf[start:end] += seg[: end - start]


def note(kind, midi, dur, vol, rng, duty=0.5, a=0.004, r=0.04):
    n = max(seconds(dur), 8)
    w = osc(kind, midi_to_hz(midi), n, rng, duty)
    return w * adsr(n, a, 0.06, 0.65, min(r, dur / 2)) * vol


def kick(rng):
    n = seconds(0.16)
    return osc("sine", glide(150, 45, n, 0.5), n) * decay(n, 2.0) * 0.9


def snare(rng):
    n = seconds(0.14)
    return (lowpass(osc("noise", 0, n, rng), 7000) * 0.6 + osc("tri", 190, n) * 0.4) * decay(n, 2.5) * 0.6


def hat(rng):
    n = seconds(0.04)
    return highpass(osc("noise", 0, n, rng), 6000) * decay(n, 2.0) * 0.25


def melody_line(rng, scale_len, bars):
    """Seeded random walk over scale degrees; returns per-bar list of (beat_offset, degree_offset, beats)."""
    rhythms = [[(0, 1.5), (1.5, 0.5), (2, 1), (3, 1)], [(0, 1), (1, 1), (2, 2)], [(0, 2), (2, 1), (3, 1)], [(0, 0.5), (0.5, 0.5), (1, 1), (2, 2)]]
    deg = 4
    out = []
    for b in range(bars):
        bar = []
        for off, ln in rhythms[int(rng.integers(len(rhythms)))]:
            deg = int(np.clip(deg + int(rng.choice([-2, -1, -1, 0, 1, 1, 2])), 0, 9))
            bar.append((off, deg, ln))
        out.append(bar)
    return out


def render(spec, layer):
    rng = np.random.default_rng(spec["seed"] * 7 + (1 if layer == "combat" else 0))
    mel_rng = np.random.default_rng(spec["seed"])  # same melody contour in both layers
    bpm = spec["bpm"]
    beat = int(round(SR * 60.0 / bpm))
    bar = beat * 4
    total = bar * BARS
    root, scale = spec["root"], spec["scale"]
    prog = spec["prog"]
    combat = layer == "combat" or spec.get("intense", False)
    buf = np.zeros(total + SR)  # tail room so notes can ring past the loop point, folded back below
    melody = melody_line(mel_rng, len(scale), BARS)

    for b in range(BARS):
        deg = prog[b % len(prog)]
        base = bar * b
        chord = [degree_midi(root + 12, scale, deg + k) for k in (0, 2, 4)]
        bass_midi = degree_midi(root - 12, scale, deg)
        # bass
        if combat:
            for e in range(8):
                place(buf, note("saw" if spec["lead"] == "saw" else "square", bass_midi + (12 if e % 4 == 3 else 0), beat / SR * 0.45, 0.30, rng, 0.25), base + e * beat // 2)
        else:
            place(buf, note("tri", bass_midi, beat * 3.6 / SR, 0.5, rng, r=0.2), base)
            place(buf, note("tri", bass_midi, beat * 0.4 / SR, 0.4, rng), base + beat * 3)
        # arpeggio / pad
        steps = 8 if combat else 4
        for s in range(steps):
            m = chord[s % 3] + (12 if s % 6 >= 3 else 0)
            place(buf, note("square", m, beat / SR * (0.45 if combat else 0.9) * (4 / steps * 0.5 + 0.5), 0.13 if combat else 0.1, rng, 0.25), base + s * (bar // steps))
        # melody (explore: every other bar breathes, combat: always on, an octave brighter)
        if combat or b % 4 != 3:
            for off, d, ln in melody[b]:
                m = degree_midi(root + 12 + (12 if combat else 0), scale, deg + d)
                place(buf, note(spec["lead"], m, ln * beat / SR * 0.92, 0.2 if combat else 0.17, rng, 0.5, r=0.08), base + int(off * beat))
        # drums
        if combat:
            for q in range(4):
                if q in (0, 2) or spec.get("intense"):
                    place(buf, kick(rng), base + q * beat)
                if q in (1, 3):
                    place(buf, snare(rng), base + q * beat)
            for e in range(8):
                place(buf, hat(rng), base + e * (beat // 2) + beat // 4 * 0)
        elif spec["bpm"] > 90 and b % 2 == 1:
            place(buf, hat(rng), base + 2 * beat)
    out = buf[:total].copy()
    out[: len(buf) - total] += buf[total:]  # fold the tail so the loop point is seamless
    return out


def encode_ogg(x, path):
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, "t.wav")
        write_wav(wav, x)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "vorbis", "-strict", "-2",
                        "-ar", "32000", "-ac", "2", path], check=True)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Audio", "BGM")
    os.makedirs(out, exist_ok=True)
    for name, spec in TRACKS.items():
        layers = ["explore"] + (["combat"] if spec["combat"] else [])
        mixes = {l: render(spec, l) for l in layers}
        # one shared gain per track keeps the explore/combat balance (combat is intentionally a little louder)
        ref = np.sqrt(np.mean(mixes["explore"] ** 2))
        gain = (10 ** (-20.0 / 20.0)) / max(ref, 1e-9)
        for l, x in mixes.items():
            y = x * gain
            peak = np.max(np.abs(y))
            if peak > 0.95:
                y *= 0.95 / peak
            path = os.path.join(out, f"{name}_{l}.ogg")
            encode_ogg(y, path)
            print(f"{name}_{l}: {len(y) / SR:5.1f}s  {os.path.getsize(path) / 1024:6.0f} KB")


if __name__ == "__main__":
    main()
