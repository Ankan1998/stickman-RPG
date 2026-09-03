"""Render every sound x 3 variants, run QA, encode OGG, write the manifest.

    python audio/build_audio.py out/stickman-rpg-audio
"""
import json, os, subprocess, sys, time, zlib
from concurrent.futures import ThreadPoolExecutor
import numpy as np
import synth as S
import sfx_combat, sfx_voices, sfx_magic, sfx_ui

VARIANTS = 3
GROUPS = [("combat", sfx_combat.SOUNDS), ("voices", sfx_voices.SOUNDS),
          ("magic", sfx_magic.SOUNDS), ("ui", sfx_ui.SOUNDS)]


def seed(name, k):
    return zlib.crc32(f"{name}#{k}".encode()) & 0xFFFFFFFF


def vary(x, k, name):
    """Variant-level colour on top of each generator's own jitter: a small
    resample (pitch + length together, like a real take) and a gain nudge."""
    if k == 0:
        return x
    small = name.startswith("ui_")
    r = 1 + (0.012 if small else 0.028) * (1 if k % 2 else -1) * (1 + 0.4 * (k // 2))
    idx = np.arange(0, len(x), r)
    y = np.interp(idx, np.arange(len(x)), x).astype(np.float32)
    return y * (1 - 0.04 * (k % 2))


def qa(x):
    peak = float(np.max(np.abs(x))) if len(x) else 0.0
    rms = float(np.sqrt(np.mean(x ** 2))) if len(x) else 0.0
    dc = float(np.mean(x)) if len(x) else 0.0
    clipped = int(np.sum(np.abs(x) >= 0.999))
    lead = int(np.argmax(np.abs(x) > 0.01)) / S.SR if peak > 0.01 else 0
    # A click is a big sample-to-sample jump where the sound is otherwise
    # quiet. Transients inside the loud part are the sound, not a defect.
    w = S.n(0.005)
    env = np.sqrt(np.convolve(x ** 2, np.ones(w) / w, mode="same")) if len(x) > w else np.abs(x)
    d = np.abs(np.diff(x))
    quiet = env[:-1] < 0.12 * (np.max(env) + 1e-9)
    jump = float(np.max(d[quiet])) if np.any(quiet) else 0.0
    return dict(peak=round(peak, 3), rms_db=round(20 * np.log10(rms + 1e-9), 1),
                dc=round(dc, 4), clipped=clipped, lead_silence=round(lead, 3),
                max_jump=round(jump, 3), dur=round(len(x) / S.SR, 3))


def build(root):
    t0 = time.time()
    man = dict(name="Stickman RPG Audio Pack", version="1.0.0",
               format=dict(sample_rate=S.SR, channels=1, wav="16-bit PCM",
                           ogg="Vorbis q5 (~160 kbps)"),
               variants=VARIANTS, sounds=[], categories={})
    wav_jobs = []
    for group, sounds in GROUPS:
        gdir = os.path.join(root, group)
        os.makedirs(gdir, exist_ok=True)
        for s in sounds:
            files = []
            worst = dict(clipped=0, max_jump=0.0)
            for k in range(VARIANTS):
                rng = np.random.default_rng(seed(s["name"], k))
                raw = s["fn"](rng)
                y = S.finish(raw, rng=np.random.default_rng(seed(s["name"], k) ^ 0x5bd1e995), **s["finish"])
                y = vary(y, k, s["name"])
                y = S.normalize(S.fade(y), 0.89)
                fn = f"{s['name']}_{k + 1}.wav"
                p = os.path.join(gdir, fn)
                S.write_wav(p, y)
                q = qa(y)
                worst["clipped"] += q["clipped"]
                worst["max_jump"] = max(worst["max_jump"], q["max_jump"])
                files.append(dict(wav=f"{group}/{fn}", ogg=f"{group}/{s['name']}_{k + 1}.ogg", **q))
                wav_jobs.append(p)
            rec = dict(name=s["name"], category=s.get("category", group), group=group,
                       blurb=s["blurb"], variants=files,
                       loop=False, suggested_volume_db=0)
            if "family" in s:
                rec["family"] = s["family"]; rec["kind"] = s["kind"]
            man["sounds"].append(rec)
            man["categories"].setdefault(rec["category"], []).append(s["name"])
    t1 = time.time()

    def enc(p):
        o = p[:-4] + ".ogg"
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", p, "-c:a", "libvorbis",
                        "-q:a", "5", o], check=True)
        return os.path.getsize(o)
    with ThreadPoolExecutor(8) as ex:
        ogg_bytes = sum(ex.map(enc, wav_jobs))
    man["totals"] = dict(sounds=len(man["sounds"]), files_per_format=len(wav_jobs),
                         wav_bytes=sum(os.path.getsize(p) for p in wav_jobs),
                         ogg_bytes=ogg_bytes,
                         render_seconds=round(t1 - t0, 1), encode_seconds=round(time.time() - t1, 1))
    with open(os.path.join(root, "manifest.json"), "w") as f:
        json.dump(man, f, indent=2)
    return man


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "out/stickman-rpg-audio"
    os.makedirs(out, exist_ok=True)
    m = build(out)
    print(json.dumps(m["totals"], indent=2))
    bad = [(s["name"], v["wav"], v) for s in m["sounds"] for v in s["variants"]
           if v["clipped"] > 0 or v["max_jump"] > 0.2 or v["lead_silence"] > 0.05 or v["dur"] > 2.5 or v["peak"] < 0.5]
    print(len(bad), "QA flags")
    for b in bad[:20]:
        print("  ", b[1], {k: b[2][k] for k in ("peak", "clipped", "max_jump", "lead_silence", "dur")})
