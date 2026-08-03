"""
Offline preview of SurfaceStyles.TerrainTextureStyles — a faithful port of the
C# baking code, so the seven sea-floor styles can be inspected before being
wired into the app. Also previews the three water pattern modes being added to
Custom/StylizedWaterSurface.
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "surface_preview")
os.makedirs(OUT, exist_ok=True)
SIZE = 256

NAMES = ["Classic sand", "Golden Ripples", "Coral Rubble",
         "Volcanic Ash", "Silty Mud", "Pebble Field", "Bleached Shells"]

M32 = 0xFFFFFFFF

def chash(x, y, seed):
    n = (x * 374761393 + y * 668265263 + seed * 1013904223) & M32
    n = ((n ^ (n >> 13)) * 1274126177) & M32
    n ^= n >> 16
    return (n & 0xFFFFFF) / 16777215.0

def wrap(i, n): return ((i % n) + n) % n

def value_noise(u, v, cells, seed):
    x = u * cells; y = v * cells
    x0 = math.floor(x); y0 = math.floor(y)
    fx = x - x0; fy = y - y0
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    a = chash(wrap(x0, cells),     wrap(y0, cells),     seed)
    b = chash(wrap(x0 + 1, cells), wrap(y0, cells),     seed)
    c = chash(wrap(x0, cells),     wrap(y0 + 1, cells), seed)
    d = chash(wrap(x0 + 1, cells), wrap(y0 + 1, cells), seed)
    return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy

def fbm(u, v, base_cells, octaves, seed):
    s = 0.0; amp = 0.5; tot = 0.0; cells = base_cells
    for o in range(octaves):
        s += value_noise(u, v, cells, seed + o * 101) * amp
        tot += amp; amp *= 0.5; cells *= 2
    return s / tot if tot > 0 else 0.0

def worley(u, v, cells, seed):
    x = u * cells; y = v * cells
    xi = math.floor(x); yi = math.floor(y)
    f1 = f2 = 9.0; cell_id = 0.0
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx = xi + dx; cy = yi + dy
            wx = wrap(cx, cells); wy = wrap(cy, cells)
            px = cx + chash(wx, wy, seed)
            py = cy + chash(wx, wy, seed + 77)
            d = (px - x) ** 2 + (py - y) ** 2
            if d < f1:
                f2 = f1; f1 = d; cell_id = chash(wx, wy, seed + 123)
            elif d < f2:
                f2 = d
    return math.sqrt(f1), math.sqrt(f2), cell_id

def lerp(a, b, t): return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
def smoothstep01(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
def inv_lerp(a, b, v):
    return 0.0 if b == a else max(0.0, min(1.0, (v - a) / (b - a)))
def pick(idx, palette):
    return palette[min(int(idx * len(palette)), len(palette) - 1)]

def pixel(style, u, v, px, py, seed):
    if style == 2:  # Coral Rubble
        f1, f2, cid = worley(u, v, 12, seed)
        chunk = max(0.0, min(1.0, 1 - f1 * 1.8))
        h = chunk * 0.8 + fbm(u, v, 32, 2, seed + 9) * 0.2
        cell = pick(cid, [(0.95, 0.92, 0.88), (0.93, 0.72, 0.72),
                          (0.90, 0.60, 0.50), (0.80, 0.72, 0.85)])
        gap = (0.42, 0.38, 0.36)
        k = smoothstep01(inv_lerp(0.12, 0.35, chunk))
        cell_s = tuple(c * (0.6 + 0.4 * chunk) for c in cell)
        return lerp(gap, cell_s, k), h
    if style == 3:  # Volcanic Ash
        grain = fbm(u, v, 24, 4, seed)
        h = grain
        col = lerp((0.07, 0.07, 0.08), (0.24, 0.24, 0.27), grain)
        if chash(px, py, seed + 3) > 0.996:
            col = (0.62, 0.57, 0.50); h = min(1.0, h + 0.35)
        return col, h
    if style == 4:  # Silty Mud
        blotch = fbm(u, v, 6, 3, seed)
        fine = fbm(u, v, 24, 2, seed + 4)
        h = blotch * 0.7 + fine * 0.3
        return lerp((0.30, 0.27, 0.18), (0.52, 0.47, 0.30), h), h
    if style == 5:  # Pebble Field
        f1, f2, cid = worley(u, v, 16, seed)
        pebble = max(0.0, min(1.0, 1 - f1 * 2))
        h = pebble ** 0.8
        if cid > 0.7:
            stone = (0.55, 0.45, 0.35)
        else:
            stone = lerp((0.45, 0.42, 0.38), (0.64, 0.62, 0.57), cid / 0.7)
        gap_sand = (0.75, 0.68, 0.52)
        k = smoothstep01(inv_lerp(0.05, 0.25, pebble))
        stone_s = tuple(c * (0.6 + 0.4 * pebble) for c in stone)
        return lerp(gap_sand, stone_s, k), h
    if style == 6:  # Bleached Shells
        grain = fbm(u, v, 32, 3, seed)
        h = grain * 0.5
        col = lerp((0.90, 0.87, 0.78), (0.99, 0.97, 0.90), grain)
        f1, f2, cid = worley(u, v, 20, seed + 31)
        if cid > 0.55:
            fleck = 1 - smoothstep01(inv_lerp(0, 0.16, f1))
            col = lerp(col, (0.72, 0.63, 0.48), fleck * 0.8)
            h += fleck * 0.4
        return col, h
    # 1: Golden Ripples
    warp = fbm(u, v, 8, 3, seed)
    band = 0.5 + 0.5 * math.sin((v * 14 + warp * 2.2) * 2 * math.pi)
    grain = fbm(u, v, 32, 3, seed + 5)
    h = band * 0.55 + grain * 0.45
    return lerp((0.78, 0.63, 0.42), (0.98, 0.92, 0.72), h), h

def height_to_normals(h):
    k = 2.2
    n = np.zeros((SIZE, SIZE, 3))
    for y in range(SIZE):
        ym = (y - 1) % SIZE; yp = (y + 1) % SIZE
        for x in range(SIZE):
            xm = (x - 1) % SIZE; xp = (x + 1) % SIZE
            dx = h[y, xp] - h[y, xm]
            dy = h[yp, x] - h[ym, x]
            v = np.array([-dx * k, -dy * k, 1.0])
            v /= np.linalg.norm(v)
            n[y, x] = [v[0] * 0.5 + 0.5, v[1] * 0.5 + 0.5, 1.0]
    return n

def bake(style):
    seed = 1000 + style * 733
    alb = np.zeros((SIZE, SIZE, 3)); h = np.zeros((SIZE, SIZE))
    for y in range(SIZE):
        v = y / SIZE
        for x in range(SIZE):
            u = x / SIZE
            c, hh = pixel(style, u, v, x, y, seed)
            alb[y, x] = c; h[y, x] = hh
    return alb, h

def lit(alb, nrm, h):
    """Quick preview shade: albedo * normal-mapped diffuse, like UnderwaterSand."""
    n = nrm * 2.0 - 1.0
    n[..., 2] = np.sqrt(np.clip(1 - n[..., 0] ** 2 - n[..., 1] ** 2, 0, 1))
    L = np.array([0.35, -0.35, 0.87]); L /= np.linalg.norm(L)
    lam = np.clip((n @ L) * 0.5 + 0.62, 0, 1.3)
    return np.clip(alb * lam[..., None], 0, 1)

def label(img, text):
    im = Image.fromarray((img * 255).astype(np.uint8))
    im = im.resize((SIZE, SIZE), Image.LANCZOS)
    canvas = Image.new("RGB", (SIZE, SIZE + 20), (255, 255, 255))
    canvas.paste(im, (0, 0))
    d = ImageDraw.Draw(canvas)
    d.text((4, SIZE + 4), text, fill=(40, 60, 70))
    return canvas

def main():
    tiles_alb, tiles_lit, tiles_nrm = [], [], []
    for style in range(1, 7):
        alb, h = bake(style)
        nrm = height_to_normals(h)
        tiles_alb.append(label(alb, NAMES[style] + " — albedo"))
        tiles_lit.append(label(lit(alb, nrm, h), NAMES[style] + " — lit"))
        tiles_nrm.append(label(nrm, NAMES[style] + " — normal"))
        print("baked", NAMES[style])

    def sheet(tiles, name, cols=3):
        rows = (len(tiles) + cols - 1) // cols
        w, hgt = tiles[0].size
        sh = Image.new("RGB", (cols * w + (cols + 1) * 8, rows * hgt + (rows + 1) * 8), (247, 250, 251))
        for i, t in enumerate(tiles):
            r, c = divmod(i, cols)
            sh.paste(t, (8 + c * (w + 8), 8 + r * (hgt + 8)))
        sh.save(os.path.join(OUT, name))
        print("wrote", name, sh.size)

    sheet(tiles_alb, "styles_albedo.png")
    sheet(tiles_lit, "styles_lit.png")
    sheet(tiles_nrm, "styles_normal.png")

    # tileability check: 2x2 repeat of one style
    alb, h = bake(1)
    im = Image.fromarray((alb * 255).astype(np.uint8))
    tile = Image.new("RGB", (SIZE * 2, SIZE * 2))
    for i in range(2):
        for j in range(2):
            tile.paste(im, (i * SIZE, j * SIZE))
    tile.save(os.path.join(OUT, "tileability_golden_ripples.png"))
    print("wrote tileability_golden_ripples.png")

if __name__ == "__main__":
    main()
