"""Raw Grok images -> engine-ready pixel art in Assets/_Game/Art. Usage: python post.py [--only substr]"""
import argparse
import shutil
import numpy as np
from PIL import Image
from specs import SPECS, COPIES, PALETTE, ART, frame_path

def _pal(hexes):
    return np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in hexes], dtype=np.int32)


PAL_FULL = _pal(PALETTE)
PAL_SPRITE = _pal(PALETTE[:9] + [PALETTE[12], PALETTE[17]])   # cream/copper/teal/ink/orange + dark teal + pale teal
PAL = PAL_SPRITE
PAL_CARTOON = _pal(PALETTE[:9] + [PALETTE[12], PALETTE[17]] + PALETTE[19:22])   # + parry pinks and red
CARTOON = ("minions/", "eagle_storm", "eagle_fury", "eagle_ko", "ironwake_damaged", "ironwake_maw", "citadel_cracked", "citadel_core")


def key_magenta(img):
    a = np.array(img.convert("RGB")).astype(np.int32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = ((np.minimum(r, b) - g) > 60) & (r > 130) & (b > 70)
    out = np.dstack([a, np.where(bg, 0, 255)]).astype(np.uint8)
    return Image.fromarray(out)


def snap(img, binary_alpha=True, pal=None):
    pal = PAL if pal is None else pal
    a = np.array(img).astype(np.int32)
    rgb, al = a[..., :3], a[..., 3]
    d = ((rgb[:, :, None, :] - pal[None, None, :, :]) ** 2).sum(-1)
    out_rgb = pal[d.argmin(-1)]
    if binary_alpha:
        al = np.where(al >= 128, 255, 0)
    return Image.fromarray(np.dstack([out_rgb, al]).astype(np.uint8))


def bleed(img):
    """Fill transparent pixels with the mean opaque colour so resize does not pull in magenta."""
    a = np.array(img)
    m = a[..., 3] > 0
    if m.any():
        a[~m, :3] = a[m, :3].mean(0).astype(np.uint8)
    return Image.fromarray(a)


def shrink(img, size, pal=None):
    return snap(bleed(img).resize(size, Image.BOX), pal=pal)


def fit_frames(frames, w, h, pal=None):
    boxes = [f.getchannel("A").getbbox() for f in frames]
    crops = [f.crop(b) for f, b in zip(frames, boxes)]
    mw = max(c.width for c in crops)
    mh = max(c.height for c in crops)
    mx = 0 if min(w, h) < 12 else 1
    scale = min((w - 2 * mx) / mw, (h - 2 * mx) / mh)
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, c in enumerate(crops):
        nw, nh = max(1, round(c.width * scale)), max(1, round(c.height * scale))
        s = shrink(c, (nw, nh), pal)
        sheet.paste(s, (i * w + (w - nw) // 2, (h - nh) // 2))
    return sheet


def process(spec):
    n, w, h, kind = spec["frames"], spec["w"], spec["h"], spec["kind"]
    raws = [Image.open(frame_path(spec, i)) for i in range(n)]
    if kind == "sprite":
        pal = PAL_CARTOON if any(k in spec["path"] for k in CARTOON) else None
        return fit_frames([key_magenta(r) for r in raws], w, h, pal)
    if kind == "bg_opaque":
        return snap(raws[0].convert("RGBA").resize((w, h), Image.BOX), pal=PAL_FULL)
    if kind == "bg_tile":
        full = shrink(key_magenta(raws[0]), (w, h), PAL_FULL)
        half = full.crop((0, 0, w // 2, h))                      # mirror tiling => seamless on X
        out = Image.new("RGBA", (w, h))
        out.paste(half, (0, 0))
        out.paste(half.transpose(Image.FLIP_LEFT_RIGHT), (w // 2, 0))
        return out
    raise ValueError(kind)


# ---- code-drawn UI (exact structure, per imagine skill) -------------------------------------------
CREAM, COPPER, INK, TEAL, SAND = (243, 224, 196), (196, 120, 74), (42, 33, 28), (62, 142, 138), (226, 196, 150)
FONT = {"0": "01110 10001 10001 10001 10001 10001 01110", "1": "00100 01100 00100 00100 00100 00100 01110",
        "2": "01110 10001 00001 00110 01000 10000 11111", "3": "11110 00001 00001 01110 00001 00001 11110",
        "4": "00010 00110 01010 10010 11111 00010 00010", "5": "11111 10000 11110 00001 00001 10001 01110",
        "6": "00110 01000 10000 11110 10001 10001 01110", "7": "11111 00001 00010 00100 01000 01000 01000",
        "8": "01110 10001 10001 01110 10001 10001 01110", "9": "01110 10001 10001 01111 00001 00010 01100"}


def draw_code(spec):
    w, h = spec["w"], spec["h"]
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()

    def rect(x0, y0, x1, y1, c):
        for y in range(y0, y1):
            for x in range(x0, x1):
                px[x, y] = c + (255,)

    p = spec["path"]
    if p.startswith(("bg/", "fx/")):
        return draw_env(spec)
    if p.endswith("digits_sheet.png"):
        for d in "0123456789":
            rows = FONT[d].split()
            ox = int(d) * 16
            for layer, (dx, dy, c) in enumerate(((1, 2, INK), (0, 1, CREAM))):
                for ry, row in enumerate(rows):
                    for rx, bit in enumerate(row):
                        if bit == "1":
                            rect(ox + dx + rx * 3, dy + ry * 3, ox + dx + rx * 3 + 3, dy + ry * 3 + 3, c)
    elif p.endswith(("banner_gameover.png", "banner_victory.png")):
        rect(0, 0, w, h, INK); rect(2, 2, w - 2, h - 2, COPPER); rect(4, 4, w - 4, h - 4, CREAM)
        rect(6, 6, w - 6, 7, SAND); rect(6, h - 7, w - 6, h - 6, SAND)
        LET = {"G": "01110 10001 10000 10111 10001 10001 01110", "A": "01110 10001 10001 11111 10001 10001 10001",
               "M": "10001 11011 10101 10101 10001 10001 10001", "E": "11111 10000 10000 11110 10000 10000 11111",
               "O": "01110 10001 10001 10001 10001 10001 01110", "V": "10001 10001 10001 10001 10001 01010 00100",
               "R": "11110 10001 10001 11110 10100 10010 10001", "Y": "10001 10001 01010 00100 00100 00100 00100",
               "I": "11111 00100 00100 00100 00100 00100 11111", "N": "10001 11001 10101 10101 10011 10001 10001",
               "W": "10001 10001 10001 10101 10101 11011 10001", "U": "10001 10001 10001 10001 10001 10001 01110"}
        text = "YOU WIN" if "victory" in p else "GAME OVER"; sc = 2; adv = 6 * sc
        x0 = (w - (len(text) * adv - sc)) // 2; y0 = (h - 7 * sc) // 2
        for i, ch in enumerate(text):
            if ch == " ":
                continue
            for ry, row in enumerate(LET[ch].split()):
                for rx, bit in enumerate(row):
                    if bit == "1":
                        ox, oy = x0 + i * adv + rx * sc, y0 + ry * sc
                        rect(ox + 1, oy + 1, ox + 1 + sc, oy + 1 + sc, SAND)
                        rect(ox, oy, ox + sc, oy + sc, INK)
    elif p.endswith("player_shot.png"):
        rect(2, 3, 14, 5, hexc("F3E0C4")); rect(10, 2, 14, 6, hexc("F3E0C4")); rect(14, 3, 16, 5, hexc("F3E0C4"))
        rect(0, 3, 4, 5, hexc("E36A2F")); rect(4, 2, 10, 3, hexc("E36A2F")); rect(4, 5, 10, 6, hexc("E36A2F"))
        rect(1, 3, 2, 5, hexc("C4784A"))
    elif p.endswith("warning_bar.png"):
        rect(0, 0, w, h, INK); rect(1, 1, w - 1, h - 1, COPPER); rect(1, 1, 7, h - 1, TEAL); rect(w - 7, 1, w - 1, h - 1, TEAL)
    return im


# ---- code-drawn environment layers ------------------------------------------------------------------
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def dither_gradient(w, h, stops):
    """Vertical ordered-dither gradient. stops = [(t, hex), ...] with t in 0..1 top->bottom."""
    out = np.zeros((h, w, 4), dtype=np.uint8)
    cols = [hexc(c) for _, c in stops]
    ts = [t for t, _ in stops]
    for y in range(h):
        t = y / (h - 1)
        k = max(i for i in range(len(ts)) if ts[i] <= t) if t < ts[-1] else len(ts) - 2
        k = min(k, len(ts) - 2)
        f = (t - ts[k]) / (ts[k + 1] - ts[k])
        for x in range(w):
            c = cols[k + 1] if f > BAYER[y % 4, x % 4] else cols[k]
            out[y, x] = (*c, 255)
    return Image.fromarray(out).copy()


def disc(im, cx, cy, r, color):
    px = im.load()
    for y in range(max(0, cy - r), min(im.height, cy + r + 1)):
        for x in range(max(0, cx - r), min(im.width, cx + r + 1)):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                px[x, y] = hexc(color) + (255,)



def cloud_layer(w, h, seed, items, tones, bank=False):
    """Pixel clouds from stacked discs on a flat base, 3-tone shaded, wrapped horizontally."""
    import random
    rnd = random.Random(seed)
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    light, mid, dark = (hexc(c) + (255,) for c in tones)
    for (cx, base, width, hgt) in items:
        puffs = []
        n = max(3, width // 14)
        for i in range(n):
            t = i / (n - 1)
            r = int(hgt * (0.45 + 0.55 * (1 - abs(t - 0.5) * 2) ** 0.7) * rnd.uniform(0.8, 1.05))
            puffs.append((cx - width // 2 + int(t * width), base - r + 2, max(r, 3)))
        top = min(py - r for _, py, r in puffs)
        for x in range(cx - width, cx + width):
            for y in range(top, base + 1):
                if any((x - px_) ** 2 + (y - py) ** 2 <= r * r for px_, py, r in puffs):
                    f = (y - top) / max(1, base - top)
                    c = light if f < 0.28 else (mid if f < 0.72 else dark)
                    if f < 0.28 and (x + y) % 2 == 0 and f > 0.2:
                        c = mid          # dithered tone edge
                    if 0.66 < f < 0.78 and (x + y) % 2 == 0:
                        c = dark
                    px[x % w, y] = c
        if bank:
            pass
    return im


def draw_env(spec):
    import random
    w, h, p = spec["w"], spec["h"], spec["path"]
    rnd = random.Random(p)
    if p.endswith("dawn_sky.png"):
        im = dither_gradient(w, h, [(0, "8FB7D6"), (0.5, "F3D7B0"), (1, "F6A15A")])
        disc(im, 84, 200, 20, "F3D7B0"); disc(im, 84, 200, 15, "F6C15B"); disc(im, 84, 200, 9, "F3E0C4")
        return im
    if p.endswith("storm_sky.png"):
        return dither_gradient(w, h, [(0, "1C2430"), (1, "3A4658")])
    if p.endswith("sea_sky.png"):
        im = dither_gradient(w, h, [(0, "7EB6C4"), (0.55, "F3D7B0"), (1, "C9854A")])
        disc(im, 340, 168, 16, "C9854A"); disc(im, 340, 168, 11, "F6C15B")
        return im
    if p.endswith("dawn_clouds_far.png"):
        return cloud_layer(w, h, p, [(40, 70, 70, 14), (170, 105, 90, 16), (300, 60, 60, 12), (420, 95, 80, 14)],
                           ("FBE9D2", "EDC9A8", "D3A28F"))
    if p.endswith("dawn_clouds_mid.png"):
        return cloud_layer(w, h, p, [(90, 190, 110, 24), (300, 150, 130, 28), (440, 215, 90, 20)],
                           ("F8E2C4", "E4B08A", "B57F78"))
    if p.endswith("dawn_pillars.png"):
        return cloud_layer(w, h, p, [(x, 268, 130, 34) for x in range(0, 480, 110)],
                           ("D9A58A", "A8706C", "6E4F5E"))
    if p.endswith("storm_clouds.png"):
        return cloud_layer(w, h, p, [(70, 70, 150, 34), (250, 45, 170, 30), (410, 90, 140, 32)],
                           ("6C7A90", "46526A", "262E40"))
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    if p.endswith("storm_rain.png"):
        for _ in range(130):
            x, y, n = rnd.randrange(w), rnd.randrange(h), rnd.choice((5, 6, 7))
            for i in range(n):
                px[(x - i // 2) % w, (y + i) % h] = hexc("D7DEE6") + (255,)
        return im
    if p.endswith("sea_horizon.png"):
        import math
        for x in range(w):
            y = 176 + round(1.4 * math.sin(2 * math.pi * 5 * x / w) + 0.8 * math.sin(2 * math.pi * 13 * x / w))
            for yy in range(y, h):
                px[x, yy] = hexc("1E4E58") + (255,)
            px[x, y] = hexc("3E8E8A") + (255,)
        return im
    if p.endswith("sea_waves.png"):
        import math
        top = int(h * 0.62)
        for x in range(w):
            for yy in range(top, h):
                px[x, yy] = hexc("1E4E58") + (255,)
            for row, (amp, k, ph) in enumerate(((2.5, 6, 0.0), (2.0, 9, 1.7), (1.5, 14, 3.1))):
                y = top + 18 + row * 22 + round(amp * math.sin(2 * math.pi * k * x / w + ph))
                if y < h:
                    px[x, y] = hexc("3E8E8A") + (255,)
                    if y + 1 < h and (x + row) % 3 == 0:
                        px[x, y + 1] = hexc("7ED0C8") + (255,)
            crest = top + round(2.5 * math.sin(2 * math.pi * 7 * x / w))
            px[x, crest] = hexc("F3E0C4") + (255,)
            px[x, crest + 1] = hexc("3E8E8A") + (255,)
        for _ in range(60):   # foam flecks
            x, y = rnd.randrange(w), rnd.randrange(top + 3, h)
            px[x, y] = hexc("F3E0C4") + (255,)
            px[(x + 1) % w, y] = hexc("F3E0C4") + (255,)
        return im
    if p.endswith("sea_spray.png"):
        for _ in range(110):
            x, y = rnd.randrange(w), rnd.randrange(h // 2, h)
            px[x, y] = hexc(rnd.choice(("7ED0C8", "F3E0C4", "3E8E8A"))) + (255,)
        return im
    if p.endswith("wind_streak.png"):
        for row, (x0, ln) in zip((1, 3, 5), ((2, 14), (18, 24), (8, 18))):
            for x in range(x0, min(w, x0 + ln)):
                px[x, row] = hexc("F3E0C4") + (255,)
        return im
    raise ValueError(p)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    for spec in SPECS:
        if a.only not in spec["path"]:
            continue
        dst = ART / spec["path"]
        if spec["kind"] == "code":
            img = draw_code(spec)
        else:
            if not all(frame_path(spec, i).exists() for i in range(spec["frames"])):
                print("skip (raw missing):", spec["path"]); continue
            img = process(spec)
        img.save(dst)
        print("wrote", spec["path"], img.size)
    for d, s in COPIES.items():
        if a.only in d or a.only in s:
            shutil.copyfile(ART / s, ART / d)
            print("copied", s, "->", d)


if __name__ == "__main__":
    main()
