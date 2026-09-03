"""Waveform + spectrogram contact sheets, so a batch of sounds can be
checked by eye: envelope shape, length, brightness, formant bands, clipping."""
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from scipy import signal
import synth as S


def sheet(items, path, cols=4):
    """items: list of (name, samples)."""
    rows = (len(items) + cols - 1) // cols
    fig, axes = plt.subplots(rows * 2, cols, figsize=(cols * 3.6, rows * 3.4),
                             facecolor="#17161f")
    axes = np.array(axes).reshape(rows * 2, cols)
    for i, (name, x) in enumerate(items):
        r, c = (i // cols) * 2, i % cols
        aw, asg = axes[r, c], axes[r + 1, c]
        tt = np.arange(len(x)) / S.SR
        aw.plot(tt, x, color="#e08a3c", lw=0.5)
        aw.set_ylim(-1, 1); aw.set_xlim(0, max(0.05, tt[-1] if len(tt) else 0.05))
        aw.set_title(f"{name}  {len(x)/S.SR:.2f}s  pk {np.max(np.abs(x)):.2f}",
                     color="#e8e4f0", fontsize=8, loc="left")
        f, tsp, Sxx = signal.spectrogram(x, S.SR, nperseg=512, noverlap=384)
        asg.pcolormesh(tsp, f / 1000, 10 * np.log10(Sxx + 1e-12), cmap="magma",
                       vmin=-100, vmax=-20, shading="auto")
        asg.set_ylim(0, 10); asg.set_ylabel("kHz", color="#9c96b0", fontsize=7)
        for a in (aw, asg):
            a.set_facecolor("#201e2b"); a.tick_params(colors="#9c96b0", labelsize=6)
            for sp in a.spines.values(): sp.set_color("#332f44")
    for j in range(len(items), rows * cols):
        axes[(j // cols) * 2, j % cols].axis("off"); axes[(j // cols) * 2 + 1, j % cols].axis("off")
    plt.tight_layout(pad=0.6)
    plt.savefig(path, dpi=80, facecolor="#17161f")
    plt.close(fig)
    return path
