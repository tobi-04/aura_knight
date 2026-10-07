"""Tiny chiptune/bfxr-style synthesis toolkit (numpy only). Everything is deterministic: pass a seeded Generator."""
import wave
import numpy as np

SR = 44100


def _phase(freq, n):
    """Phase in cycles for a scalar or per-sample frequency array."""
    f = np.full(n, float(freq)) if np.isscalar(freq) else np.asarray(freq, dtype=float)
    return np.cumsum(f) / SR


def glide(f0, f1, n, curve=1.0):
    """Frequency ramp f0 -> f1 over n samples (curve > 1 bends the movement toward the end)."""
    x = np.linspace(0.0, 1.0, n) ** curve
    return f0 + (f1 - f0) * x


def osc(kind, freq, n, rng=None, duty=0.5):
    ph = _phase(freq, n)
    cyc = ph % 1.0
    if kind == "square":
        return np.where(cyc < duty, 1.0, -1.0)
    if kind == "saw":
        return 2.0 * cyc - 1.0
    if kind == "tri":
        return 4.0 * np.abs(cyc - 0.5) - 1.0
    if kind == "sine":
        return np.sin(2 * np.pi * ph)
    if kind == "noise":
        return rng.uniform(-1.0, 1.0, n)
    raise ValueError(kind)


def adsr(n, a=0.005, d=0.05, s=0.7, r=0.05):
    """Envelope with times in seconds; the release is placed at the end of the n samples."""
    na, nd, nr = int(a * SR), int(d * SR), int(r * SR)
    env = np.empty(n)
    sus = max(n - na - nd - nr, 0)
    parts = [np.linspace(0, 1, na, endpoint=False), np.linspace(1, s, nd, endpoint=False),
             np.full(sus, s), np.linspace(s, 0, nr)]
    cat = np.concatenate(parts)
    env[:] = 0
    env[:min(n, len(cat))] = cat[:n]
    return env


def decay(n, power=2.0):
    return (1.0 - np.linspace(0, 1, n)) ** power


def lowpass(x, cutoff):
    """One-pole low-pass; cutoff in Hz (scalar)."""
    a = 1.0 - np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += a * (v - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def bitcrush(x, bits=6, hold=1):
    levels = 2 ** (bits - 1)
    y = np.round(x * levels) / levels
    if hold > 1:
        y = np.repeat(y[::hold], hold)[: len(x)]
    return y


def vibrato(f, n, rate=6.0, depth=0.03):
    t = np.arange(n) / SR
    return f * (1.0 + depth * np.sin(2 * np.pi * rate * t))


def midi_to_hz(m):
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def seconds(x):
    return int(x * SR)


def mix(*layers):
    n = max(len(l) for l in layers)
    out = np.zeros(n)
    for l in layers:
        out[: len(l)] += l
    return out


def normalize_peak(x, peak=0.9):
    m = np.max(np.abs(x))
    return x if m == 0 else x * (peak / m)


def normalize_rms(x, rms_db=-20.0, ceiling=0.95):
    rms = np.sqrt(np.mean(x ** 2))
    y = x * (10 ** (rms_db / 20.0) / max(rms, 1e-9))
    m = np.max(np.abs(y))
    return y * (ceiling / m) if m > ceiling else y


def write_wav(path, x):
    pcm = np.clip(x, -1.0, 1.0)
    data = (pcm * 32767).astype("<i2").tobytes()
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)
