"""
Preview of the three water pattern modes for Custom/StylizedWaterSurface.
Mode 0 is the existing caustic interference field (unchanged); modes 1 and 2 are
the new ones being added, ported here first so they can be seen before shipping.
Uses the same UwHash21 / UwValueNoise definitions as UnderwaterCommon.hlsl.
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "surface_preview")
os.makedirs(OUT, exist_ok=True)
N = 300

def frac(x): return x - np.floor(x)

def uw_hash21(px, py):
    """float2 p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x*p.y);"""
    x = frac(px * 123.34); y = frac(py * 456.21)
    d = x * (x + 45.32) + y * (y + 45.32)
    x = x + d; y = y + d
    return frac(x * y)

def uw_value_noise(px, py):
    ix = np.floor(px); iy = np.floor(py)
    fx = px - ix; fy = py - iy
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    a = uw_hash21(ix, iy)
    b = uw_hash21(ix + 1, iy)
    c = uw_hash21(ix, iy + 1)
    d = uw_hash21(ix + 1, iy + 1)
    return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy

def uw_voronoi_f1(px, py):
    ix = np.floor(px); iy = np.floor(py)
    fx = px - ix; fy = py - iy
    best = np.full_like(px, 8.0)
    for gy in (-1, 0, 1):
        for gx in (-1, 0, 1):
            ox = uw_hash21(ix + gx, iy + gy)
            oy = uw_hash21(ix + gx + 17.3, iy + gy + 17.3)
            dx = gx + ox - fx
            dy = gy + oy - fy
            best = np.minimum(best, dx * dx + dy * dy)
    return np.sqrt(best)

def uw_voronoi_edge(px, py, time):
    """F2-F1 with slowly drifting cell centres -> an undulating thin web."""
    ix = np.floor(px); iy = np.floor(py)
    fx = px - ix; fy = py - iy
    f1 = np.full_like(px, 8.0); f2 = np.full_like(px, 8.0)
    for gy in (-1, 0, 1):
        for gx in (-1, 0, 1):
            hx = uw_hash21(ix + gx, iy + gy)
            hy = uw_hash21(ix + gx + 17.3, iy + gy + 17.3)
            ox = 0.5 + 0.45 * np.sin(time + hx * 6.2832)
            oy = 0.5 + 0.45 * np.sin(time * 1.17 + hy * 6.2832)
            dx = gx + ox - fx
            dy = gy + oy - fy
            d = dx * dx + dy * dy
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    return np.sqrt(f2) - np.sqrt(f1)

def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)

# ── mode 0: existing caustics ────────────────────────────────────────────────
def caustics(u, v, time):
    sharp = 0.005
    TWO_PI = 6.28318530718
    px = np.fmod(u, TWO_PI) - 250.0
    py = np.fmod(v, TWO_PI) - 250.0
    ix = px.copy(); iy = py.copy()
    c = np.ones_like(px)
    for n in range(3):
        t = time * (1.0 - (3.5 / (n + 1)))
        nix = px + (np.cos(t - ix) + np.sin(t + iy))
        niy = py + (np.sin(t - iy) + np.cos(t + ix))
        ix, iy = nix, niy
        with np.errstate(divide='ignore', invalid='ignore'):
            ax = px / (np.sin(ix + t) / sharp)
            ay = py / (np.cos(iy + t) / sharp)
            c = c + 1.0 / np.sqrt(ax * ax + ay * ay)
    c = 1.17 - np.power(np.abs(c / 3.0), 1.4)
    return np.clip(np.power(np.abs(c), 8.0), 0, 1)

# ── mode 1: voronoi ripple web ───────────────────────────────────────────────
def pattern_ripple(u, v, time, inner=0.25, width=0.14):
    # The preset _PatternScale is tuned for the caustic field; a voronoi cell at
    # that scale is sub-metre, so the ripple mode rescales internally to give
    # metre-scale cells. Bright thin lines along cell borders, undulating.
    u = u * inner; v = v * inner
    edge = uw_voronoi_edge(u, v, time * 0.5)
    web = 1.0 - smoothstep(0.0, width, edge)
    # break the uniform polygon look: low-frequency drift fades the web in and out
    web *= 0.45 + 0.55 * uw_value_noise(u * 0.6 + time * 0.08, v * 0.6)
    return np.clip(web * 1.25, 0, 1)

# ── mode 2: directional streaks ──────────────────────────────────────────────
def pattern_streaks(u, v, time, surge=(1.0, 0.35)):
    dx, dy = surge
    L = math.hypot(dx, dy); dx, dy = dx / L, dy / L
    tx = (u * dx + v * dy) * 0.25          # stretch along flow
    ty = (-u * dy + v * dx) * 2.5          # compress across it
    n = uw_value_noise(tx + time * 0.6, ty)
    n = n * 0.7 + uw_value_noise(tx * 2.3 + time * 0.9, ty * 2.3 + 4.1) * 0.3
    return np.clip(np.power(n, 2.5) * 1.8, 0, 1)

def render(fn, scale, time, label):
    xs = np.linspace(0, 40, N)          # 40 m of water surface
    u, v = np.meshgrid(xs, xs)
    p = fn(u * scale, v * scale, time)
    water = np.array([0.18, 0.55, 0.62])
    light = np.array([0.85, 0.98, 0.95])
    col = water[None, None, :] * (1 - p[..., None] * 0.85) + light[None, None, :] * (p[..., None] * 0.9)
    img = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8))
    canvas = Image.new("RGB", (N, N + 20), (255, 255, 255))
    canvas.paste(img, (0, 0))
    ImageDraw.Draw(canvas).text((4, N + 4), label, fill=(40, 60, 70))
    return canvas

def main():
    tiles = [
        render(caustics, 0.30, 3.0, "0 - caustic interference (existing)"),
        render(lambda u,v,t: pattern_ripple(u,v,t,0.18,0.12), 1.40, 3.0, "1 - web, inner 0.18 w 0.12"),
        render(lambda u,v,t: pattern_ripple(u,v,t,0.18,0.12), 1.40, 6.4, "1 - web, t+3.4s (animates)"),
        render(pattern_streaks, 0.80, 3.0, "2 - directional streaks (Drifting Current)"),
    ]
    w, h = tiles[0].size
    sheet = Image.new("RGB", (4 * w + 40, h + 16), (247, 250, 251))
    for i, t in enumerate(tiles):
        sheet.paste(t, (8 + i * (w + 8), 8))
    sheet.save(os.path.join(OUT, "water_patterns.png"))
    print("wrote water_patterns.png", sheet.size)

    # a second time sample to confirm the patterns actually animate
    later = [
        render(caustics, 0.30, 5.2, "0 - t+2.2s"),
        render(pattern_ripple, 1.40, 5.2, "1 - t+2.2s"),
        render(pattern_streaks, 0.80, 5.2, "2 - t+2.2s"),
    ]
    sheet2 = Image.new("RGB", (3 * w + 32, h + 16), (247, 250, 251))
    for i, t in enumerate(later):
        sheet2.paste(t, (8 + i * (w + 8), 8))
    sheet2.save(os.path.join(OUT, "water_patterns_t2.png"))
    print("wrote water_patterns_t2.png")

if __name__ == "__main__":
    main()
