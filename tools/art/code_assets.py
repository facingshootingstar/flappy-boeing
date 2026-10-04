"""Code-drawn art for the Cuphead update: font, cards, bullets, fx and living background layers.
Writes straight into Assets/_Game/Art. Usage: python code_assets.py [--only substr]"""
import argparse
import math
import random
import numpy as np
from PIL import Image
from specs import ART
from post import hexc, dither_gradient, cloud_layer

C = {k: hexc(v) for k, v in dict(
    cream="F3E0C4", sand="E2C496", copper="C4784A", rust="A85A32", teal="3E8E8A", pteal="7ED0C8", ink="2A211C",
    orange="E36A2F", yellow="F6C15B", night="1C2430", slate="3A4658", mist="D7DEE6", deep="1E4E58", tan="C9854A",
    apricot="F6A15A", peach="F3D7B0", sky="8FB7D6", foam="D7FFF8", sea="7EB6C4", pink="F07AA8", ppink="FFC3D9",
    red="D8443A", white="FFFFFF").items()}


class Canvas:
    def __init__(self, w, h):
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.im.load()
        self.w, self.h = w, h

    def put(self, x, y, c, a=255, wrap=False):
        if wrap:
            x %= self.w
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, y] = ((C[c] if c in C else hexc(c)) if isinstance(c, str) else tuple(c[:3])) + (a,)

    def get(self, x, y):
        return self.px[x, y] if 0 <= x < self.w and 0 <= y < self.h else (0, 0, 0, 0)

    def rect(self, x0, y0, x1, y1, c, a=255):
        for y in range(y0, y1):
            for x in range(x0, x1):
                self.put(x, y, c, a)

    def disc(self, cx, cy, r, c, a=255, wrap=False):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for x in range(int(cx - r) - 1, int(cx + r) + 2):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.put(x, y, c, a, wrap)

    def outline(self, c="ink", diag=False):
        """1px outline around every opaque pixel."""
        a = np.array(self.im)
        op = a[..., 3] > 0
        ring = np.zeros_like(op)
        offs = [(1, 0), (-1, 0), (0, 1), (0, -1)] + ([(1, 1), (-1, -1), (1, -1), (-1, 1)] if diag else [])
        for dx, dy in offs:
            ring |= np.roll(np.roll(op, dy, 0), dx, 1)
        ring &= ~op
        a[ring] = (C[c] if c in C else hexc(c)) + (255,)
        self.im = Image.fromarray(a)
        self.px = self.im.load()

    def save(self, rel):
        self.im.save(ART / rel)
        print("wrote", rel, self.im.size)


def sheet(frames):
    w, h = frames[0].w, frames[0].h
    out = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        out.paste(f.im, (i * w, 0))
    return out


def save_sheet(rel, frames):
    sheet(frames).save(ART / rel)
    print("wrote", rel, (frames[0].w * len(frames), frames[0].h))


# ------------------------------------------------------------------ font
CHARSET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?.:-+/'%"
GLYPH = dict(zip(CHARSET, """
01110 10001 10001 11111 10001 10001 10001|11110 10001 10001 11110 10001 10001 11110|01110 10001 10000 10000 10000 10001 01110|
11110 10001 10001 10001 10001 10001 11110|11111 10000 10000 11110 10000 10000 11111|11111 10000 10000 11110 10000 10000 10000|
01110 10001 10000 10111 10001 10001 01111|10001 10001 10001 11111 10001 10001 10001|01110 00100 00100 00100 00100 00100 01110|
00111 00010 00010 00010 00010 10010 01100|10001 10010 10100 11000 10100 10010 10001|10000 10000 10000 10000 10000 10000 11111|
10001 11011 10101 10101 10001 10001 10001|10001 10001 11001 10101 10011 10001 10001|01110 10001 10001 10001 10001 10001 01110|
11110 10001 10001 11110 10000 10000 10000|01110 10001 10001 10001 10101 10010 01101|11110 10001 10001 11110 10100 10010 10001|
01111 10000 10000 01110 00001 00001 11110|11111 00100 00100 00100 00100 00100 00100|10001 10001 10001 10001 10001 10001 01110|
10001 10001 10001 10001 10001 01010 00100|10001 10001 10001 10101 10101 10101 01010|10001 10001 01010 00100 01010 10001 10001|
10001 10001 01010 00100 00100 00100 00100|11111 00001 00010 00100 01000 10000 11111|
01110 10001 10011 10101 11001 10001 01110|00100 01100 00100 00100 00100 00100 01110|01110 10001 00001 00110 01000 10000 11111|
11110 00001 00001 01110 00001 00001 11110|00010 00110 01010 10010 11111 00010 00010|11111 10000 11110 00001 00001 10001 01110|
00110 01000 10000 11110 10001 10001 01110|11111 00001 00010 00100 01000 01000 01000|01110 10001 10001 01110 10001 10001 01110|
01110 10001 10001 01111 00001 00010 01100|
00100 00100 00100 00100 00100 00000 00100|01110 10001 00001 00010 00100 00000 00100|00000 00000 00000 00000 00000 01100 01100|
00000 01100 01100 00000 01100 01100 00000|00000 00000 00000 11111 00000 00000 00000|00000 00100 00100 11111 00100 00100 00000|
00001 00010 00010 00100 01000 01000 10000|00100 00100 01000 00000 00000 00000 00000|11001 11010 00010 00100 01000 01011 10011
""".replace("\n", "").split("|")))
CELL_W, CELL_H = 9, 10
WIDE = {"M": "1100011 1110111 1111111 1101011 1100011 1100011 1100011", "W": "1100011 1100011 1100011 1101011 1111111 1110111 1100011"}


def font_sheet():
    frames = []
    for ch in CHARSET:
        cv = Canvas(CELL_W, CELL_H)
        rows = WIDE.get(ch, GLYPH[ch]).split()
        bold = ch not in WIDE
        for ry, row in enumerate(rows):
            for rx, bit in enumerate(row):
                if bit == "1":   # bold: each pixel is 2 wide (M/W use hand-made 7-wide bold shapes instead)
                    cv.put(1 + rx, 1 + ry, "white")
                    if bold:
                        cv.put(2 + rx, 1 + ry, "white")
        cv.outline("ink", diag=True)
        # drop shadow down-right in ink
        a = np.array(cv.im)
        op = a[..., 3] > 0
        sh = np.roll(np.roll(op, 1, 0), 1, 1) & ~op
        a[sh] = C["ink"] + (255,)
        cv.im = Image.fromarray(a)
        frames.append(cv)
    save_sheet("ui/font_sheet.png", frames)


# ------------------------------------------------------------------ ui
def plaque():
    cv = Canvas(24, 24)
    cv.rect(0, 0, 24, 24, "ink")
    cv.rect(1, 1, 23, 23, "copper")
    cv.rect(2, 2, 22, 22, "rust")
    cv.rect(3, 3, 21, 21, "cream")
    cv.rect(4, 4, 20, 5, "sand"); cv.rect(4, 19, 20, 20, "sand")
    for x, y in ((1, 1), (22, 1), (1, 22), (22, 22)):   # rivets
        cv.put(x, y, "yellow")
    cv.save("ui/plaque.png")


def cards():
    """Super meter card: frame 0 = back (charging), frame 1 = face up (ready)."""
    back, face = Canvas(12, 16), Canvas(12, 16)
    for cv in (back, face):
        cv.rect(0, 1, 12, 15, "ink"); cv.rect(1, 0, 11, 16, "ink")
    back.rect(1, 1, 11, 15, "copper")
    for y in range(2, 14):
        for x in range(2, 10):
            if (x + y) % 3 == 0:
                back.put(x, y, "rust")
    back.rect(4, 6, 8, 10, "sand"); back.put(5, 7, "copper"); back.put(6, 8, "copper")
    face.rect(1, 1, 11, 15, "cream")
    face.rect(2, 2, 10, 14, "ppink")
    heart = ["01010", "11111", "11111", "01110", "00100"]
    for ry, row in enumerate(heart):
        for rx, b in enumerate(row):
            if b == "1":
                face.put(3 + rx, 5 + ry, "pink")
    face.put(4, 6, "white")
    face.put(2, 2, "white"); face.put(9, 13, "pink")
    save_sheet("ui/card_super_sheet.png", [back, face])


def heart_icon():
    cv = Canvas(12, 11)
    heart = ["0110110", "1111111", "1111111", "1111111", "0111110", "0011100", "0001000"]
    for ry, row in enumerate(heart):
        for rx, b in enumerate(row):
            if b == "1":
                cv.put(2 + rx, 2 + ry, "red")
    cv.put(3, 3, "ppink"); cv.put(4, 3, "white")
    cv.outline("ink")
    cv.save("ui/heart.png")


def white_px():
    cv = Canvas(4, 4); cv.rect(0, 0, 4, 4, "white"); cv.save("ui/white.png")


# ------------------------------------------------------------------ bullets
def orb(rel, body, core, rim="ink", size=16):
    cv = Canvas(size, size)
    c = (size - 1) / 2
    cv.disc(c, c, size / 2 - 1.6, rim)
    cv.disc(c, c, size / 2 - 2.6, body)
    cv.disc(c + 0.5, c + 0.5, size / 2 - 4.6, core)
    cv.put(int(c) - 2, int(c) - 2, "white"); cv.put(int(c) - 1, int(c) - 2, "white")
    return cv


def bullets():
    pink = orb("", "pink", "ppink")
    pink.put(7, 2, "white"); pink.put(7, 13, "ppink"); pink.put(2, 7, "ppink"); pink.put(13, 7, "ppink")   # parry sparkle points
    pink.save("bullets/orb_pink.png")

    for name, vane, edge, tip in (("feather", "cream", "copper", "teal"), ("feather_pink", "ppink", "pink", "pink")):
        cv = Canvas(16, 8)
        for x in range(1, 15):   # vanes taper toward the tip (right)
            half = 3 if x < 11 else (2 if x < 13 else 1)
            for y in range(4 - half, 4 + half):
                cv.put(x, y, vane if (x + y) % 4 else edge)
        for x in range(0, 15):
            cv.put(x, 4, "ink" if x < 3 else edge)   # quill
        cv.put(14, 3, tip); cv.put(15, 3, tip); cv.put(13, 2, tip)
        cv.outline("ink")
        cv.save(f"bullets/{name}.png")

    cv = Canvas(16, 12)
    cv.rect(1, 1, 15, 11, "ink"); cv.rect(2, 2, 14, 10, "sand"); cv.rect(2, 2, 14, 4, "cream")
    cv.rect(2, 8, 14, 10, "tan")
    for x, y in ((6, 4), (7, 5), (7, 6), (8, 7), (11, 3), (11, 4)):
        cv.put(x, y, "rust")
    cv.save("bullets/brick.png")

    cv = Canvas(24, 12)
    cv.rect(6, 4, 19, 8, "copper"); cv.rect(6, 4, 19, 5, "apricot")
    cv.rect(19, 5, 21, 7, "cream"); cv.put(21, 5, "cream"); cv.put(21, 6, "cream"); cv.put(22, 5, "cream"); cv.put(22, 6, "cream")
    cv.rect(6, 2, 9, 4, "teal"); cv.rect(6, 8, 9, 10, "teal")
    cv.rect(10, 5, 13, 7, "pteal")
    cv.rect(2, 5, 6, 7, "yellow"); cv.put(1, 5, "orange"); cv.put(1, 6, "orange"); cv.put(0, 6, "orange")
    cv.rect(3, 4, 6, 5, "orange"); cv.rect(3, 7, 6, 8, "orange")
    cv.outline("ink")
    cv.save("bullets/ex_rocket.png")

    for name, cols in (("super_beam", ("white", "foam", "yellow", "apricot", "orange")),
                       ("laser", ("white", "ppink", "pink", "red", "red"))):
        cv = Canvas(8, 24)
        for y in range(24):
            d = abs(y - 11.5)
            k = 0 if d < 3 else 1 if d < 5.5 else 2 if d < 8 else 3 if d < 10 else 4
            for x in range(8):
                cv.put(x, y, cols[k], 255 if k < 4 else 200)
        for x in range(0, 8, 4):
            cv.put(x, 6, "white"); cv.put(x + 2, 17, "white")
        cv.save(f"bullets/{name}.png")

    flak = orb("", "slate", "ink", size=16)
    flak.rect(7, 2, 9, 5, "orange"); flak.put(7, 1, "yellow"); flak.put(8, 1, "yellow")
    flak.save("bullets/flak.png")


# ------------------------------------------------------------------ fx
def explosion():
    """Cuphead-ish puffy smoke burst: flash -> fireball -> cream smoke puffs -> wisps."""
    rnd = random.Random(7)
    frames = []
    puffs = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.55, 1.0)) for _ in range(7)]
    for f in range(6):
        cv = Canvas(48, 48)
        cx = cy = 23.5
        if f == 0:
            for a in range(8):
                ang = a * math.pi / 4
                for r in range(0, 11 if a % 2 == 0 else 6):
                    cv.put(int(cx + math.cos(ang) * r), int(cy + math.sin(ang) * r), "white")
            cv.disc(cx, cy, 5, "yellow"); cv.disc(cx, cy, 3, "white")
        elif f == 1:
            cv.disc(cx, cy, 12, "orange"); cv.disc(cx - 1, cy - 1, 9, "yellow"); cv.disc(cx - 2, cy - 2, 5, "white")
            for ang, k in puffs[:4]:
                cv.disc(cx + math.cos(ang) * 11, cy + math.sin(ang) * 11, 5 * k + 2, "apricot")
        else:
            spread = 8 + f * 4
            rad = {2: 9, 3: 8, 4: 6, 5: 3.5}[f]
            for ang, k in puffs:
                x, y = cx + math.cos(ang) * spread * k, cy + math.sin(ang) * spread * k - f
                cv.disc(x, y, rad * (0.7 + 0.4 * k), "sand" if f > 3 else "cream")
                cv.disc(x - 1, y - 1, rad * (0.7 + 0.4 * k) - 2, "cream" if f > 3 else "white" if f == 2 else "cream")
            if f == 2:
                cv.disc(cx, cy, 6, "orange"); cv.disc(cx, cy, 3, "yellow")
            if f == 3:
                cv.disc(cx, cy, 3, "apricot")
        if f >= 1:
            cv.outline("ink")
        frames.append(cv)
    save_sheet("fx/explosion_sheet.png", frames)


def small_fx():
    cv = Canvas(16, 16)
    for a in range(8):
        ang = a * math.pi / 4
        for r in range(0, 7 if a % 2 == 0 else 4):
            cv.put(int(7.5 + math.cos(ang) * r), int(7.5 + math.sin(ang) * r), "yellow" if r > 2 else "white")
    cv.disc(7.5, 7.5, 2, "white")
    cv.save("fx/muzzle_flash.png")

    cv = Canvas(32, 32)
    for a in range(4):
        ang = a * math.pi / 2
        for r in range(0, 15):
            w = max(0, 3 - r // 4)
            for t in range(-w, w + 1):
                x = 15.5 + math.cos(ang) * r - math.sin(ang) * t
                y = 15.5 + math.sin(ang) * r + math.cos(ang) * t
                cv.put(int(x), int(y), "pink" if abs(t) == w and w > 0 else "ppink")
    cv.disc(15.5, 15.5, 4, "ppink"); cv.disc(15.5, 15.5, 2, "white")
    cv.outline("ink")
    cv.save("fx/parry_star.png")

    frames = []
    for f in range(3):
        cv = Canvas(40, 16)
        for i in range(3):
            ang = f * 2 * math.pi / 9 + i * 2 * math.pi / 3
            x, y = 19.5 + math.cos(ang) * 14, 7.5 + math.sin(ang) * 4
            for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                cv.put(int(x) + dx, int(y) + dy, "yellow" if (dx, dy) != (0, 0) else "white")
        cv.outline("ink")
        frames.append(cv)
    save_sheet("fx/ko_stars_sheet.png", frames)

    frames = []
    for f in range(4):
        cv = Canvas(16, 16)
        r = 2.5 + f * 1.3
        cv.disc(7.5, 8 - f, r, "slate" if f < 2 else "mist", 255)
        cv.disc(6.5, 7 - f, max(1, r - 2), "mist" if f < 2 else "white")
        if f < 3:
            cv.outline("ink")
        frames.append(cv)
    save_sheet("fx/smoke_sheet.png", frames)


# ------------------------------------------------------------------ backgrounds
def periodic(x, w, terms):
    return sum(a * math.sin(2 * math.pi * k * x / w + ph) for a, k, ph in terms)


def hills(rel, base, terms, tones, props, seed):
    """Rolling hills band tiling on X (all frequencies are integer multiples of the width)."""
    w, h = 480, 270
    cv = Canvas(w, h)
    rnd = random.Random(seed)
    light, mid, dark = tones
    tops = [int(base + periodic(x, w, terms)) for x in range(w)]
    for x in range(w):
        t = tops[x]
        slope = tops[(x + 1) % w] - tops[x - 1]
        for y in range(t, h):
            d = y - t
            c = light if d < 3 and slope <= 0 else (mid if d < 22 else dark)
            if d == 0:
                c = light if slope <= 0 else mid
            if 18 < d < 26 and (x + y) % 2 == 0:
                c = dark
            cv.put(x, y, c)
    for kind, x in props:
        t = tops[x % w]
        if kind == "tree":
            r = rnd.choice((4, 5, 6))
            cv.rect(x, t - r, x + 2, t + 2, "rust")
            cv.disc(x + 1, t - r - 2, r, dark, wrap=True); cv.disc(x, t - r - 3, r - 2, mid, wrap=True)
            cv.put(x - 1, t - r - 5, light, wrap=True)
        elif kind == "barn":
            for y in range(t - 12, t + 2):
                for xx in range(x, x + 16):
                    cv.put(xx, y, "copper" if y > t - 9 else "rust", wrap=True)
            for i in range(9):
                for xx in range(x - 1 + i, x + 17 - i):
                    cv.put(xx, t - 12 - i // 2, "ink" if i < 1 else "rust", wrap=True)
            cv.rect(x + 6, t - 6, x + 10, t + 2, "ink")
            cv.put(x + 7, t - 5, "sand"); cv.put(x + 8, t - 4, "sand")
        elif kind == "windmill":
            for y in range(t - 26, t + 2):
                half = 2 + (y - (t - 26)) // 9
                for xx in range(x - half, x + half + 1):
                    cv.put(xx, y, "cream" if xx < x else "sand", wrap=True)
            cv.rect(x - 2, t - 29, x + 3, t - 25, "copper")
            for k, (dx, dy) in enumerate(((1, -1), (1, 1), (-1, 1), (-1, -1))):
                for r in range(2, 13):
                    cv.put(x + dx * r, t - 27 + dy * r, "ink", wrap=True)
                    if r > 4:
                        cv.put(x + dx * r + dx, t - 27 + dy * r, "cream", wrap=True)
        elif kind == "fence":
            for xx in range(x, x + 40):
                cv.put(xx, tops[xx % w] - 3, "cream", wrap=True)
                if xx % 6 == 0:
                    for y in range(5):
                        cv.put(xx, tops[xx % w] - 5 + y, "cream", wrap=True)
    cv.save(rel)


def sun_rays():
    cv = Canvas(192, 192)
    c = 95.5
    for y in range(192):
        for x in range(192):
            dx, dy = x - c, y - c
            r = math.hypot(dx, dy)
            if r < 14 or r > 95:
                continue
            ang = math.atan2(dy, dx)
            if math.sin(ang * 12) > 0.35:
                a = int(110 * (1 - r / 95) ** 0.8)
                if a > 6:
                    cv.put(x, y, "yellow", a)
    cv.save("bg/dawn_sun_rays.png")


def puff_cloud(w, h, seed, items, tones):
    """One-piece puffy cloud: overlapping discs sitting on a rounded base band, 3-tone banded, no wrap-around."""
    rnd = random.Random(seed)
    light, mid, dark = tones
    mask, top_of = np.zeros((h, w), bool), {}
    for k, (cx, base, width, hgt) in enumerate(items):
        rb = max(3.0, hgt * 0.45)
        n = max(3, int(width / (hgt * 1.15)) + 1)
        discs = [(x, base - rb, rb) for x in range(cx - width // 2, cx + width // 2 + 1, 2)]
        for i in range(n):
            t = i / (n - 1)
            r = hgt * (0.6 + 0.4 * (1 - abs(t - 0.5) * 2) ** 0.7) * rnd.uniform(0.85, 1.05)
            discs.append((cx - width / 2 + t * width + rnd.uniform(-1.5, 1.5), base - rb - r * 0.55, r))
        top = min(cy - r for _, cy, r in discs)
        yy, xx = np.mgrid[0:h, 0:w]
        m = np.zeros((h, w), bool)
        for dx, dy, r in discs:
            m |= (xx - dx) ** 2 + (yy - dy) ** 2 <= r * r
        m &= yy <= base - 1
        for y, x in zip(*np.nonzero(m)):
            top_of[(x, y)] = (top, base)
        mask |= m
    cv = Canvas(w, h)
    for (x, y), (top, base) in top_of.items():
        f = (y - top) / max(1, base - top)
        c = light if f < 0.4 else (mid if f < 0.78 else dark)
        if 0.32 < f < 0.4 and (x + y) % 2 == 0:
            c = mid
        if 0.72 < f < 0.84 and (x + y) % 2 == 0:
            c = dark
        cv.put(int(x), int(y), c)
    return cv


def amb_clouds():
    soft = ("FFFFFF", "F3E0C4", "E2C496")
    specs = [("bg/amb_cloud_1.png", 96, 40, [(48, 36, 60, 13)], soft),
             ("bg/amb_cloud_2.png", 64, 32, [(32, 28, 38, 10)], soft),
             ("bg/amb_cloud_3.png", 128, 48, [(44, 44, 54, 13), (84, 42, 52, 16)], ("FBE9D2", "F3D7B0", "E2C496")),
             ("bg/storm_cloud_puff.png", 96, 40, [(48, 36, 60, 13)], ("6C7A90", "46526A", "262E40")),
             ("bg/dusk_cloud.png", 128, 32, [(46, 28, 62, 8), (86, 27, 54, 9)], ("F6A15A", "C4784A", "A85A32"))]
    for rel, w, h, items, tones in specs:
        cv = puff_cloud(w, h, rel, items, tones)
        cv.outline("ink" if "storm" not in rel else "night")
        cv.save(rel)


def balloon():
    cv = Canvas(24, 40)
    for y in range(2, 24):
        half = int(10 * math.sqrt(max(0, 1 - ((y - 12) / 11.5) ** 2)))
        if y > 18:
            half = max(half, 3)
        for x in range(12 - half, 12 + half):
            stripe = ((x - 12) * 10 // max(1, half)) // 3
            cv.put(x, y, "copper" if stripe % 2 else "cream")
    for x in range(8, 16):
        cv.put(x, 21, "orange")
    for y in range(24, 32):
        cv.put(9 + (y - 24) // 4, y, "ink"); cv.put(14 - (y - 24) // 4, y, "ink")
    cv.rect(9, 32, 15, 37, "rust"); cv.rect(9, 32, 15, 33, "tan")
    cv.put(8, 6, "white"); cv.put(8, 7, "white"); cv.put(9, 5, "white")
    cv.outline("ink")
    cv.save("bg/amb_balloon.png")


def gulls():
    frames = []
    for up in (True, False):
        cv = Canvas(16, 12)
        body = [(7, 6), (8, 6), (9, 6), (6, 6), (10, 5)]
        for x, y in body:
            cv.put(x, y, "cream")
        cv.put(11, 5, "orange")
        wing = [(5, 5), (4, 4), (3, 3), (2, 3), (9, 5), (10, 4), (11, 3), (12, 3)] if up else \
               [(5, 7), (4, 8), (3, 8), (2, 9), (9, 7), (10, 8), (11, 8), (12, 9)]
        for x, y in wing:
            cv.put(x, y, "mist")
        cv.outline("ink")
        frames.append(cv)
    save_sheet("bg/amb_gull_sheet.png", frames)


def sparkle():
    cv = Canvas(8, 8)
    for d in range(-3, 4):
        cv.put(3 + d, 3, "foam" if abs(d) < 3 else "pteal")
        cv.put(3, 3 + d, "foam" if abs(d) < 3 else "pteal")
    cv.put(3, 3, "white")
    cv.save("bg/sea_glitter.png")


def storm_layers():
    w, h = 480, 270
    rnd = random.Random("rainfar")
    cv = Canvas(w, h)
    for _ in range(160):
        x, y = rnd.randrange(w), rnd.randrange(h)
        for i in range(4):
            cv.put(x - i // 2, (y + i) % h, "slate", 255, wrap=True)
    cv.save("bg/storm_rain_far.png")

    im = cloud_layer(w, h, "stormmid", [(60, 260, 150, 40), (230, 268, 170, 44), (400, 255, 150, 38)],
                     ("46526A", "3A4658", "262E40"))
    cv = Canvas(w, h); cv.im = im; cv.px = im.load()
    cv.save("bg/storm_clouds_mid.png")

    # foreground: dark scalloped banks hugging the top and bottom edges
    cv = Canvas(w, h)
    for x in range(w):
        top = 10 + int(periodic(x, w, ((4, 9, 0.3), (2, 23, 1.1), (1.5, 41, 2.0))))
        bot = h - 12 - int(periodic(x, w, ((4, 7, 1.7), (2, 19, 0.4), (1.5, 37, 2.6))))
        for y in range(0, top):
            cv.put(x, y, "night" if y < top - 3 else "slate")
        for y in range(bot, h):
            cv.put(x, y, "night" if y > bot + 3 else "slate")
    cv.save("bg/storm_clouds_near.png")


def sea_layers():
    w = 480
    frames = []
    for f in range(4):
        cv = Canvas(w, 96)
        ph = f * math.pi / 2
        for x in range(w):
            top = 10 + round(periodic(x, w, ((3, 8, ph), (1.5, 19, -ph * 2))))
            for y in range(top, 96):
                band = (y - top) // 18
                c = "teal" if band == 0 else "deep"
                if band >= 1 and (y + x // 3) % 23 == 0:
                    c = "teal"
                cv.put(x, y, c)
            cv.put(x, top, "foam"); cv.put(x, top + 1, "cream" if (x + f) % 5 else "pteal")
            for row, (amp, k, rp) in enumerate(((2, 11, 0.5), (1.5, 17, 2.2), (1, 29, 4.0))):
                y = top + 22 + row * 20 + round(amp * math.sin(2 * math.pi * k * x / w + rp + ph))
                if y < 96 and (x // 4 + row + f) % 3 != 0:
                    cv.put(x, y, "pteal" if row == 0 else "teal")
        frames.append(cv)
    save_sheet("bg/sea_waves_sheet.png", frames)

    cv = Canvas(w, 270)
    for x in range(w):
        for row, (amp, k, ph, y0) in enumerate(((2, 9, 0.2, 258), (1.5, 15, 2.1, 264))):
            y = y0 + round(amp * math.sin(2 * math.pi * k * x / w + ph))
            for yy in range(y, 270):
                cv.put(x, yy, "foam" if yy == y else ("cream" if row == 0 else "pteal"), 230)
    cv.save("bg/sea_foam_near.png")

    cv = Canvas(w, 270)
    horizon = 176
    for cx, half, hgt in ((70, 30, 10), (150, 14, 5), (330, 44, 16), (420, 18, 6)):
        for x in range(cx - half, cx + half):
            t = 1 - abs(x - cx) / half
            top = horizon - int(hgt * (t ** 0.6))
            for y in range(top, horizon + 1):
                cv.put(x, y, "deep" if y > top + 1 else "teal")
    lx, ly = 340, horizon - 16
    for y in range(ly - 22, ly):
        half = 2 + (y - (ly - 22)) // 10
        for x in range(lx - half, lx + half + 1):
            cv.put(x, y, "cream" if ((y // 4) % 2 == 0) else "red")
    cv.rect(lx - 3, ly - 26, lx + 4, ly - 22, "ink"); cv.rect(lx - 2, ly - 25, lx + 3, ly - 23, "yellow")
    cv.rect(lx - 3, ly - 28, lx + 4, ly - 26, "red")
    cv.save("bg/sea_islands.png")

    cv = Canvas(64, 9)   # lighthouse beam, origin on the left edge
    for x in range(64):
        half = 1 + x * 3 // 64
        for y in range(4 - half, 5 + half):
            cv.put(x, y, "yellow", int(150 * (1 - x / 64)))
    cv.save("bg/lighthouse_beam.png")


def dusk_layers():
    im = dither_gradient(480, 270, [(0, "1C2430"), (0.3, "3A4658"), (0.55, "A85A32"), (0.72, "E36A2F"), (1, "F6C15B")])
    cv = Canvas(480, 270); cv.im = im; cv.px = im.load()
    cv.disc(330, 196, 34, "orange"); cv.disc(330, 196, 28, "apricot"); cv.disc(330, 196, 20, "yellow")
    for y, ln in ((182, 60), (190, 44), (205, 70)):
        for x in range(330 - ln, 330 + ln):
            if (x + y) % 3:
                cv.put(x, y, "copper")
    rnd = random.Random(3)
    for _ in range(40):
        x, y = rnd.randrange(480), rnd.randrange(70)
        cv.put(x, y, "mist" if rnd.random() < 0.3 else "slate")
    cv.save("bg/dusk_sky.png")


ALL = {
    "font": font_sheet, "plaque": plaque, "cards": cards, "heart": heart_icon, "white": white_px,
    "bullets": bullets, "explosion": explosion, "fx": small_fx,
    "hills": lambda: (
        hills("bg/dawn_hills_far.png", 214, ((5, 2, 0.4), (3, 5, 1.3), (1.5, 11, 2.2)),
              ("F3D7B0", "E4B08A", "D3A28F"), [("tree", x) for x in (30, 95, 210, 260, 380, 450)] + [("windmill", 300)], 1),
        hills("bg/dawn_hills_near.png", 238, ((6, 3, 2.0), (3, 7, 0.4), (1, 17, 1.0)),
              ("7ED0C8", "3E8E8A", "1E4E58"),
              [("tree", x) for x in (15, 60, 180, 330, 360, 440)] + [("barn", 120), ("fence", 230), ("windmill", 400)], 2)),
    "rays": sun_rays, "clouds": amb_clouds, "balloon": balloon, "gulls": gulls, "sparkle": sparkle,
    "storm": storm_layers, "sea": sea_layers, "dusk": dusk_layers,
}


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    for d in ("ui", "bullets", "fx", "bg", "minions"):
        (ART / d).mkdir(parents=True, exist_ok=True)
    for k, fn in ALL.items():
        if a.only in k:
            fn()
