"""
Tiny procedural-audio toolkit. numpy + scipy.signal, nothing else.

Same philosophy as the art: every sound is CODE. A slash is "a noise burst
swept through a bandpass with this envelope", not a sample somebody
recorded, so retuning the whole pack is a constant change and re-run.

Conventions: mono float32 in -1..1, 44.1 kHz. Every generator returns a
numpy array; `write_wav` turns it into a 16-bit PCM file.
"""

import math
import wave
import numpy as np
from scipy import signal

SR = 44100
TAU = 2 * math.pi


# ---------------------------------------------------------------------------
#  time / envelopes
# ---------------------------------------------------------------------------

def n(dur):
    return int(SR * dur)


def t(dur):
    return np.arange(n(dur)) / SR


def env_adsr(dur, a=0.005, d=0.05, s=0.6, r=0.1, curve=2.0):
    N = n(dur)
    A, D, R = n(a), n(d), n(r)
    S = max(0, N - A - D - R)
    parts = [np.linspace(0, 1, A, endpoint=False) ** (1 / curve) if A else np.zeros(0),
             (1 - (1 - s) * np.linspace(0, 1, D, endpoint=False) ** (1 / curve)) if D else np.zeros(0),
             np.full(S, s),
             (s * (1 - np.linspace(0, 1, R)) ** curve) if R else np.zeros(0)]
    e = np.concatenate(parts)
    if len(e) < N:
        e = np.pad(e, (0, N - len(e)))
    return e[:N].astype(np.float32)


def env_decay(dur, k=6.0, attack=0.002):
    """Exponential decay: hits, thumps, plucks."""
    x = t(dur)
    e = np.exp(-k * x / dur)
    A = n(attack)
    if A:
        e[:A] *= np.linspace(0, 1, A)
    F = max(2, len(e) // 20)                  # always land on zero: no end-click
    e[-F:] *= np.linspace(1, 0, F)
    return e.astype(np.float32)


def env_points(dur, pts):
    """Breakpoint envelope: pts = [(time_frac, value), ...]."""
    x = np.linspace(0, 1, n(dur))
    xs, ys = zip(*pts)
    return np.interp(x, xs, ys).astype(np.float32)


# ---------------------------------------------------------------------------
#  sources
# ---------------------------------------------------------------------------

def _phase(freq, N):
    f = np.broadcast_to(np.asarray(freq, dtype=np.float64), (N,))
    return np.cumsum(f) / SR


def sine(freq, dur):
    return np.sin(TAU * _phase(freq, n(dur))).astype(np.float32)


def saw(freq, dur):
    p = _phase(freq, n(dur)) % 1.0
    return (2 * p - 1).astype(np.float32)


def square(freq, dur, width=0.5):
    p = _phase(freq, n(dur)) % 1.0
    return np.where(p < width, 1.0, -1.0).astype(np.float32)


def tri(freq, dur):
    p = _phase(freq, n(dur)) % 1.0
    return (4 * np.abs(p - 0.5) - 1).astype(np.float32)


def pulse_train(freq, dur, width=0.08):
    """Glottal-ish source for voices: narrow pulses, lots of harmonics."""
    p = _phase(freq, n(dur)) % 1.0
    return np.where(p < width, 1.0, -0.05).astype(np.float32)


def noise(dur, rng, kind="white"):
    x = rng.standard_normal(n(dur)).astype(np.float32) * 0.5
    if kind == "pink":
        b = [0.049922035, -0.095993537, 0.050612699, -0.004408786]
        a = [1, -2.494956002, 2.017265875, -0.522189400]
        x = signal.lfilter(b, a, x).astype(np.float32) * 3.0
    elif kind == "brown":
        x = np.cumsum(x) / 40.0
        x = x - np.mean(x)
    return x


def sweep(f0, f1, dur, curve=1.0):
    """Frequency array going f0 -> f1. curve>1 bends early, <1 late."""
    x = np.linspace(0, 1, n(dur)) ** curve
    return (f0 * (f1 / f0) ** x).astype(np.float64)  # exponential glide


def vibrato(freq, dur, rate=6.0, depth=0.02):
    x = t(dur)
    return np.broadcast_to(freq, (n(dur),)) * (1 + depth * np.sin(TAU * rate * x))


def jitter(freq, dur, rng, amount=0.03, rate=30.0):
    """Slow random pitch wobble; makes a voice sound alive."""
    N = n(dur)
    k = max(2, int(dur * rate))
    pts = rng.uniform(-amount, amount, k)
    w = np.interp(np.linspace(0, 1, N), np.linspace(0, 1, k), pts)
    return np.broadcast_to(freq, (N,)) * (1 + w)


# ---------------------------------------------------------------------------
#  filters
# ---------------------------------------------------------------------------

def _sos(kind, fc, order=2, q=None):
    fc = float(np.clip(fc, 20, SR / 2 - 100))
    if kind == "bp":
        bw = fc / (q or 1.0)
        lo, hi = max(20, fc - bw / 2), min(SR / 2 - 100, fc + bw / 2)
        return signal.butter(order, [lo, hi], btype="band", fs=SR, output="sos")
    return signal.butter(order, fc, btype={"lp": "low", "hp": "high"}[kind],
                         fs=SR, output="sos")


def lp(x, fc, order=2):
    return signal.sosfilt(_sos("lp", fc, order), x).astype(np.float32)


def hp(x, fc, order=2):
    return signal.sosfilt(_sos("hp", fc, order), x).astype(np.float32)


def bp(x, fc, q=2.0, order=2):
    return signal.sosfilt(_sos("bp", fc, order, q), x).astype(np.float32)


def lp_sweep(x, fc_array, order=2, chunk=256):
    """Time-varying lowpass, processed in short chunks with state carried."""
    out = np.zeros_like(x)
    zi = None
    for i in range(0, len(x), chunk):
        fc = float(np.mean(fc_array[i:i + chunk]))
        sos = _sos("lp", fc, order)
        if zi is None:
            zi = signal.sosfilt_zi(sos) * x[0]
        out[i:i + chunk], zi = signal.sosfilt(sos, x[i:i + chunk], zi=zi)
    return out.astype(np.float32)


def bp_sweep(x, fc_array, q=2.0, chunk=256):
    out = np.zeros_like(x)
    zi = None
    for i in range(0, len(x), chunk):
        fc = float(np.mean(fc_array[i:i + chunk]))
        sos = _sos("bp", fc, 2, q)
        if zi is None:
            zi = signal.sosfilt_zi(sos) * 0
        out[i:i + chunk], zi = signal.sosfilt(sos, x[i:i + chunk], zi=zi)
    return out.astype(np.float32)


def resonator(x, fc, q=12.0):
    """Peaky bandpass - a ringing body. Stack several for a formant."""
    w0 = TAU * fc / SR
    alpha = math.sin(w0) / (2 * q)
    b = [alpha, 0, -alpha]
    a = [1 + alpha, -2 * math.cos(w0), 1 - alpha]
    return signal.lfilter(b, a, x).astype(np.float32) * (q * 0.5)


def formants(x, fs, qs=None, gains=None):
    """Parallel resonators at formant frequencies fs (Hz)."""
    qs = qs or [10] * len(fs)
    gains = gains or [1.0] * len(fs)
    out = np.zeros_like(x)
    for f, q, g in zip(fs, qs, gains):
        out += resonator(x, f, q) * g
    return out


# ---------------------------------------------------------------------------
#  shaping / effects
# ---------------------------------------------------------------------------

def drive(x, amount=2.0):
    return np.tanh(x * amount).astype(np.float32) / math.tanh(min(amount, 4))


def bitcrush(x, bits=10, rate_div=1):
    """The 'chunky retro' fingerprint: fewer bits, optionally fewer samples."""
    q = 2 ** (bits - 1)
    y = np.round(x * q) / q
    if rate_div > 1:
        y = np.repeat(y[::rate_div], rate_div)[:len(x)]
    return y.astype(np.float32)


def reverb_fast(x, size=0.06, mix=0.16, decay=0.5, rng=None):
    """Convolution with a synthetic exponential-noise impulse. Fast, good enough."""
    rng = rng or np.random.default_rng(7)
    L = int(SR * size * 5)
    ir = rng.standard_normal(L).astype(np.float32) * np.exp(-np.linspace(0, 8, L) * (1 - decay))
    ir = lp(ir, 5000)
    ir /= (np.sqrt(np.sum(ir ** 2)) + 1e-9)
    wet = signal.fftconvolve(x, ir)
    dry = np.pad(x, (0, len(wet) - len(x)))
    return (dry * (1 - mix) + wet * mix * 3.0).astype(np.float32)


def delay(x, time=0.12, fb=0.35, mix=0.3):
    d = n(time)
    out = np.pad(x, (0, d * 5)).astype(np.float32)
    y = out.copy()
    for i in range(d, len(out)):
        y[i] += fb * y[i - d]
    return (out * (1 - mix) + y * mix).astype(np.float32)


def tremolo(x, rate=8.0, depth=0.5):
    e = 1 - depth * (0.5 + 0.5 * np.sin(TAU * rate * np.arange(len(x)) / SR))
    return (x * e).astype(np.float32)


# ---------------------------------------------------------------------------
#  assembly
# ---------------------------------------------------------------------------

def mix(*layers):
    L = max(len(l) for l in layers)
    out = np.zeros(L, dtype=np.float32)
    for l in layers:
        out[:len(l)] += l
    return out


def seq(*parts, gap=0.0):
    g = np.zeros(n(gap), dtype=np.float32)
    out = []
    for p in parts:
        out.append(p)
        out.append(g)
    return np.concatenate(out[:-1]) if out else np.zeros(0, dtype=np.float32)


def at(x, offset, total=None):
    """Place x at `offset` seconds inside a silent buffer."""
    o = n(offset)
    L = total and n(total) or (o + len(x))
    out = np.zeros(max(L, o + len(x)), dtype=np.float32)
    out[o:o + len(x)] += x
    return out


def normalize(x, peak=0.89):
    m = float(np.max(np.abs(x))) if len(x) else 0
    return (x / m * peak).astype(np.float32) if m > 1e-6 else x


def fade(x, fin=0.002, fout=0.02):
    x = x.copy()
    a, b = n(fin), n(fout)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b and b < len(x):
        x[-b:] *= np.linspace(1, 0, b)
    return x


def trim(x, thresh=0.003, tail=0.03):
    """Drop silence at the ends, keeping a short tail."""
    idx = np.where(np.abs(x) > thresh)[0]
    if len(idx) == 0:
        return x
    a = max(0, idx[0] - n(0.002))
    b = min(len(x), idx[-1] + n(tail))
    return x[a:b]


def finish(x, crush_bits=11, rate_div=1, room=0.12, peak=0.89, rng=None):
    """House style: normalise, gentle crush, a whisper of stone room, DC-free."""
    # Remove DC with a filter, not by subtracting the mean: subtracting shifts
    # the final sample off zero and the reverb's zero-padding then clicks.
    x = hp(x, 28)
    x = normalize(x, 0.95)
    if crush_bits:
        x = bitcrush(x, crush_bits, rate_div)
    if room:
        x = reverb_fast(x, size=0.05, mix=room, decay=0.45, rng=rng)
    x = trim(x)
    x = fade(x)
    return normalize(x, peak)


# ---------------------------------------------------------------------------
#  output
# ---------------------------------------------------------------------------

def write_wav(path, x):
    pcm = np.clip(x, -1, 1)
    pcm = (pcm * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    return path


def read_wav(path):
    with wave.open(path, "rb") as w:
        data = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2")
    return data.astype(np.float32) / 32767.0
