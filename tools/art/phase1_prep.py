"""Phase 1 art: key the real backdrop, seam skies, spin Poe's prop.

The plates are pale magenta with a gray corner fringe. Sampling a big corner
patch catches the fringe and leaves the field. Walk in from each corner, key
every stable color found there, and require the field itself to go clear.

python tools/art/phase1_prep.py
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
IMG = r"C:\Users\ADMIN\.grok\sessions\D%3A%5Clearning%20material%5CFlappy%20Boeing\01a157e9-5c09-7251-8bff-e238e5c39ce8\images"
ART = os.path.join(ROOT, "Assets", "_Game", "Art")
RAW = os.path.join(ROOT, "tools", "art", "raw", "parallax")

# rel -> (source, crop (l, r) or None, key, fade edges, opaque seam)
LAYERS = {
    "dawn/sky": ("3.jpg", None, False, False, True),
    "dawn/far": ("5.jpg", None, True, True, False),
    "dawn/mid": ("9.jpg", None, True, True, False),
    "dawn/near": ("8.jpg", None, True, True, False),
    "dawn/front": ("4.jpg", None, True, True, False),
    "storm/sky": ("2.jpg", (0.18, 0.82), False, False, True),
    "storm/far": ("1.jpg", None, True, True, False),
    "storm/mid": ("10.jpg", None, True, True, False),
    "storm/near": ("18.jpg", None, True, True, False),
    "storm/front": ("21.jpg", None, True, True, False),
    "sea/sky": ("23.jpg", None, False, False, True),
    "sea/far": ("25.jpg", None, True, True, False),
    "sea/mid": ("24.jpg", None, True, True, False),
    "sea/near": ("26.jpg", None, True, True, False),
    "sea/front": ("27.jpg", None, True, True, False),
    # props are one sprite, not a scrolling band: no edge fade (that punches the tile)
    "props/grass": ("30.jpg", None, True, False, False),
    "props/pit": ("29.jpg", None, True, False, False),
}


def load(name):
    return Image.open(os.path.join(IMG, name)).convert("RGBA")


def _runs_from(rgb, y0, x0, dy, dx, span):
    h, w = rgb.shape[:2]
    runs = []
    y, x = y0, x0
    run = [rgb[y, x].astype(np.int16)]
    for _t in range(1, span):
        y += dy
        x += dx
        if y < 0 or x < 0 or y >= h or x >= w:
            break
        cur = rgb[y, x].astype(np.int16)
        if np.max(np.abs(cur - run[-1])) > 26:
            if len(run) >= 3:
                runs.append((len(run), np.median(np.stack(run), axis=0)))
            run = [cur]
        else:
            run.append(cur)
    if len(run) >= 3:
        runs.append((len(run), np.median(np.stack(run), axis=0)))
    return runs


def corner_runs(rgb):
    """Runs of stable color walking in from each corner. The long run is the field; a short first run is the paper fringe."""
    h, w = rgb.shape[:2]
    span = int(min(h, w) * 0.28)
    starts = ((0, 0, 1, 1), (0, w - 1, 1, -1), (h - 1, 0, -1, 1), (h - 1, w - 1, -1, -1))
    return [_runs_from(rgb, *s, span) for s in starts]


def field_color(rgb, runs):
    """The corner color that actually fills the top of the plate, ignoring a paper border."""
    h, w = rgb.shape[:2]
    top = rgb[6: max(16, h // 5), w // 10: w - w // 10].astype(np.int16)
    cands = [max(r, key=lambda item: item[0])[1] for r in runs if r]
    if not cands:
        return np.median(rgb[:8, :8].astype(np.int16).reshape(-1, 3), axis=0)
    best, best_n = cands[0], -1
    for c in cands:
        n = int((np.max(np.abs(top - c.astype(np.int16)), axis=-1) < 22).sum())
        if n > best_n:
            best, best_n = c, n
    return best


def key_backdrop(im, tol=32, feather=14):
    """Drop the backdrop. Bottom corners are often the painting, so only colors near the top field are keyed."""
    rgb = np.array(im.convert("RGB"))
    h, w = rgb.shape[:2]
    runs = corner_runs(rgb)
    field = field_color(rgb, runs)
    use = [field]
    for group in runs:
        for _n, c in group:
            if np.max(np.abs(c.astype(np.int16) - field.astype(np.int16))) < 42:
                use.append(c)
    src = rgb.astype(np.int16)
    dist = np.min([np.max(np.abs(src - c.astype(np.int16)), axis=-1) for c in use], axis=0)
    # A paper fringe is a short run before the field. Clear it only as deep as that run, and only in the corner.
    field_i = field.astype(np.int16)
    for y0, x0, dy, dx in ((0, 0, 1, 1), (0, w - 1, 1, -1), (h - 1, 0, -1, 1), (h - 1, w - 1, -1, -1)):
        depth = 0
        y, x = y0, x0
        for _step in range(64):
            if y < 0 or x < 0 or y >= h or x >= w:
                break
            if np.max(np.abs(src[y, x] - field_i)) < tol:
                break
            depth += 1
            y += dy
            x += dx
        if depth < 2 or depth >= 64:
            continue
        box = depth + 6
        y1, x1 = y0 + dy * box, x0 + dx * box
        ys = slice(min(y0, y1), max(y0, y1))
        xs = slice(min(x0, x1), max(x0, x1))
        fringe = src[y0, x0]
        local = np.max(np.abs(src[ys, xs] - fringe), axis=-1)
        dist[ys, xs] = np.minimum(dist[ys, xs], local)
    t = np.clip((dist.astype(np.float32) - tol) / feather, 0, 1)
    out = np.array(im)
    out[..., 3] = np.minimum(out[..., 3], (255 * t).astype(np.uint8))
    out[..., 3] = np.where(dist < tol, 0, out[..., 3])
    for y0, x0 in ((0, 0), (0, w - 10), (h - 10, 0), (h - 10, w - 10)):
        patch = dist[y0:y0 + 10, x0:x0 + 10]
        if np.median(patch) < tol:
            out[y0:y0 + 10, x0:x0 + 10, 3] = 0
    return Image.fromarray(out), field, dist


def fade_edges(im, frac=0.07):
    a = np.array(im)
    w = a.shape[1]
    band = max(4, int(w * frac))
    ramp = np.linspace(0, 1, band, dtype=np.float32)
    a[:, :band, 3] = (a[:, :band, 3].astype(np.float32) * ramp).astype(np.uint8)
    a[:, w - band:, 3] = (a[:, w - band:, 3].astype(np.float32) * ramp[::-1]).astype(np.uint8)
    return Image.fromarray(a)


def seam_opaque(im, frac=0.08):
    rgb = np.array(im.convert("RGB"))
    w = rgb.shape[1]
    band = max(8, int(w * frac))
    out = rgb.copy()
    t = np.linspace(0, 1, band, dtype=np.float32)[:, None]
    left = rgb[:, :band].astype(np.float32)
    right = rgb[:, w - band:].astype(np.float32)
    out[:, :band] = (left * t + right * (1 - t)).astype(np.uint8)
    return Image.fromarray(out).convert("RGBA")


def save(im, rel):
    path = os.path.join(ART, *rel.split("/"))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)
    raw = os.path.join(RAW, *rel.split("/"))
    os.makedirs(os.path.dirname(raw), exist_ok=True)
    im.save(raw)
    return path


def build_beam():
    """Soft cone. The lamp pivot sits on the left edge, so the bright end is the left."""
    w, h = 512, 96
    xs = np.linspace(0, 1, w)[None, :]
    ys = np.linspace(-1, 1, h)[:, None]
    half = 0.12 + xs * 0.88
    fade = np.clip((1 - xs) * (1 - np.abs(ys) / np.maximum(half, 1e-3)), 0, 1)
    fade = np.where(np.abs(ys) < half, fade, 0)
    a = np.zeros((h, w, 4), np.uint8)
    a[..., 0] = 255
    a[..., 1] = 236
    a[..., 2] = 176
    a[..., 3] = (fade * 170).astype(np.uint8)
    return Image.fromarray(a)


def build_blades():
    """Four wood blades, hub at the canvas center so Spinner can turn them."""
    n = 480
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    dr = ImageDraw.Draw(im)
    cx = cy = n // 2
    wood = (128, 74, 40, 255)
    ink = (48, 30, 20, 255)
    length, base, tip = 198, 16, 34
    for k in range(4):
        ang = k * np.pi / 2 + np.pi / 2
        fwd = np.array([np.cos(ang), np.sin(ang)])
        side = np.array([-fwd[1], fwd[0]])
        tip_p = np.array([cx, cy]) + fwd * length
        a = np.array([cx, cy]) + side * base
        b = np.array([cx, cy]) - side * base
        c = tip_p + side * tip * 0.35
        d = tip_p - side * tip * 0.35
        poly = [tuple(p) for p in (a, c, d, b)]
        dr.polygon(poly, fill=wood, outline=ink)
        rib = [tuple(np.array([cx, cy]) + fwd * 20), tuple(tip_p)]
        dr.line(rib, fill=ink, width=3)
    dr.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), fill=(92, 58, 36, 255), outline=ink)
    dr.ellipse((cx - 8, cy - 8, cx + 8, cy + 8), fill=(210, 186, 140, 255))
    return im


def drop_ground_line(im):
    """A thin dark rule under the posed planes. Wheels are short; the rule spans the frame."""
    a = np.array(im)
    h, w = a.shape[:2]
    dark = (a[..., 3] > 50) & (a[..., 0] < 80) & (a[..., 1] < 80) & (a[..., 2] < 80)
    opaque = a[..., 3] > 40
    for y in range(h):
        xs = np.where(dark[y])[0]
        if len(xs) < 40:
            continue
        if xs[-1] - xs[0] < w * 0.45:
            continue
        if dark[y].sum() < 0.65 * max(1, opaque[y].sum()):
            continue
        a[y, :, 3] = np.where(dark[y], 0, a[y, :, 3])
    return Image.fromarray(a)


def crop_box(im):
    a = np.array(im)
    ys, xs = np.where(a[..., 3] > 16)
    if len(xs) == 0:
        return im
    pad = int(max(int(xs.max() - xs.min()), int(ys.max() - ys.min())) * 0.04) + 2
    return im.crop((
        max(0, int(xs.min()) - pad), max(0, int(ys.min()) - pad),
        min(a.shape[1], int(xs.max()) + pad + 1), min(a.shape[0], int(ys.max()) + pad + 1),
    ))


def align_bottoms(frames):
    cropped = [crop_box(f) for f in frames]
    w = max(f.width for f in cropped)
    h = max(f.height for f in cropped)
    out = []
    for f in cropped:
        canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        canvas.paste(f, ((w - f.width) // 2, h - f.height), f)
        out.append(canvas)
    return out


def find_hub(im):
    """Center of the rightmost cream blob. That blob is the spinner, not the belly."""
    from scipy import ndimage
    a = np.array(im)
    al, r, g, b = a[..., 3], a[..., 0], a[..., 1], a[..., 2]
    cream = (al > 140) & (r > 175) & (g > 150) & (b > 110) & (r.astype(np.int16) - b.astype(np.int16) < 90)
    op = np.where(al > 40)[1]
    cut = float(op.max() - (op.max() - op.min()) * 0.2)
    lab, n = ndimage.label(cream)
    best = None
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        bw, bh = int(xs.max() - xs.min()), int(ys.max() - ys.min())
        if len(xs) < 25 or max(bw, bh) > 90 or min(bw, bh) < 8:
            continue
        if float(np.median(xs)) < cut:
            continue
        if best is None or len(xs) > best[0]:
            best = (len(xs), xs, ys)
    if best is None:
        raise SystemExit("prop hub not found on poe_fly_0")
    _n, xs, ys = best
    cx, cy = float(np.median(xs)), float(np.median(ys))
    rad = 0.5 * float(max(xs.max() - xs.min(), ys.max() - ys.min()))
    if not 10 <= rad <= 58:
        raise SystemExit("bad hub rad %s at %s,%s n %s" % (rad, cx, cy, len(xs)))
    return cx, cy, rad


def blade_mask(im, cx, cy, rad):
    """A blade is a narrow run of paint through the hub's x. The fuselage run is wide, so it stays.

    ponytail: row runs, not a hand-traced spline. Ceiling is a blade that merges into the nose
    for more than a few rows. Upgrade: paint the four angles with image_edit.
    """
    a = np.array(im)
    fg = a[..., 3] > 40
    h, _w = fg.shape
    kill = np.zeros((h, fg.shape[1]), np.bool_)
    for y in range(h):
        xs = np.where(fg[y])[0]
        if len(xs) == 0:
            continue
        cuts = np.where(np.diff(xs) > 2)[0]
        starts = np.r_[0, cuts + 1]
        ends = np.r_[cuts, len(xs) - 1]
        for s, e in zip(starts, ends):
            x0, x1 = int(xs[s]), int(xs[e])
            width = x1 - x0
            if 22 <= width <= 78 and x0 <= cx <= x1 and x0 > cx - 70:
                kill[y, x0:x1 + 1] = True
    yy, xx = np.ogrid[:h, : fg.shape[1]]
    spinner = (xx - cx) ** 2 + (yy - cy) ** 2 <= (rad * 1.05) ** 2
    kill[spinner] = False
    for sign in (-1, 1):
        rows = np.where(kill.any(axis=1) & ((np.arange(h) - cy) * sign > rad))[0]
        if len(rows) == 0:
            continue
        edge = int(rows.max() if sign < 0 else rows.min())
        span = np.where(kill[edge])[0]
        if len(span) == 0:
            continue
        y_a, y_b = sorted((edge, int(cy)))
        sl = slice(int(span.min()), int(span.max()) + 1)
        kill[y_a:y_b, sl] |= fg[y_a:y_b, sl]
    kill[spinner] = False
    upper = kill & (yy < cy)
    ys, xs = np.where(upper)
    if len(xs) < 40:
        raise SystemExit("upper blade mask too small")
    length = float(np.hypot(xs - cx, ys - cy).max())
    if xs.min() < cx - 80:
        raise SystemExit("blade mask reached the fuselage")
    return kill, length


def spin_frames(body, kill, cx, cy, rad, length):
    """Keep the fuselage. Disc + the real upper blade at four angles.

    ponytail: one cut-out blade, not a redraw. Ceiling is a single painted blade;
    a bent or chipped blade repeats at every angle. Upgrade: four image_edits.
    """
    a = np.array(body)
    upper = kill & (np.ogrid[: a.shape[0], : a.shape[1]][0] < cy)
    side = int(length + rad + 6)
    x0, y0 = int(round(cx)) - side, int(round(cy)) - side
    x1, y1 = x0 + side * 2, y0 + side * 2
    patch = np.zeros((side * 2, side * 2, 4), np.uint8)
    src = a[max(0, y0):y1, max(0, x0):x1]
    py0, px0 = max(0, -y0), max(0, -x0)
    sub = patch[py0:py0 + src.shape[0], px0:px0 + src.shape[1]]
    um = upper[max(0, y0):y1, max(0, x0):x1]
    sub[..., :] = np.where(um[..., None], src, sub)
    blade = Image.fromarray(patch)
    print("cutout", int((patch[..., 3] > 20).sum()))
    a[..., 3] = np.where(kill, 0, a[..., 3])
    naked = Image.fromarray(a)
    frames = []
    for deg in (0, 90, 180, 270):
        frame = naked.copy()
        disc = Image.new("RGBA", frame.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(disc)
        for i, frac in enumerate((1.0, 0.78, 0.56, 0.36)):
            rr = max(4, int(length * frac))
            col = (148, 96, 48, 78 - i * 14)
            d.ellipse((cx - rr, cy - rr, cx + rr, cy + rr), outline=col, width=max(3, int(length * 0.05)))
        spun = blade.rotate(-deg, resample=Image.BICUBIC, center=(side, side))
        disc.paste(spun, (x0, y0), spun)
        frames.append(Image.alpha_composite(frame, disc))
    return frames


def wheel_angle(im):
    """Degrees of the tail-wheel to main-wheel line. The tail sits higher by design."""
    from scipy import ndimage
    a = np.array(im)
    h = a.shape[0]
    dark = (a[..., 3] > 160) & (a[..., 0] < 55) & (a[..., 1] < 55) & (a[..., 2] < 55)
    dark[: int(h * 0.45)] = False
    lab, n = ndimage.label(dark)
    wheels = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if len(xs) < 25:
            continue
        bw = int(xs.max() - xs.min()) + 1
        bh = int(ys.max() - ys.min()) + 1
        if min(bw, bh) < 8 or max(bw, bh) > min(bw, bh) * 1.7:
            continue
        if len(xs) < 0.35 * bw * bh:
            continue
        wheels.append((len(xs), float(xs.mean()), float(ys.mean())))
    if len(wheels) < 2:
        return None
    wheels.sort(reverse=True)
    cents = sorted((x, y) for _n, x, y in wheels[:2])
    (x1, y1), (x2, y2) = cents
    return float(np.degrees(np.arctan2(y2 - y1, x2 - x1)))


def assert_plate_gone(path, field, src_rgb):
    out = np.array(Image.open(path).convert("RGBA"))
    dist = np.max(np.abs(src_rgb.astype(np.int16) - np.array(field, np.int16)), axis=-1)
    plate = dist < 16
    bad = plate & (out[..., 3] > 6)
    frac = float(bad.mean())
    h, w = out.shape[:2]
    corners = [int(out[2, 2, 3]), int(out[2, w - 3, 3]), int(out[h - 3, 2, 3]), int(out[h - 3, w - 3, 3])]
    print("  plate", os.path.basename(path), "leftover", round(frac, 4), "corners", corners, "field", [int(v) for v in field])
    if frac > 0.008:
        raise SystemExit("backdrop still on " + path)
    # top corners are backdrop on every layer; bottom corners may be the painted ground
    if corners[0] != 0 or corners[1] != 0:
        raise SystemExit("top corner alpha not 0 on " + path)


def main():
    os.makedirs(os.path.join(ROOT, "tools", "art", "ref"), exist_ok=True)
    load("7.jpg").save(os.path.join(ROOT, "tools", "art", "ref", "poe_ref.png"))

    fields = {}
    sources = {}
    for rel, (src, crop, do_key, fade, seam) in LAYERS.items():
        im = load(src)
        if crop:
            w, _h = im.size
            im = im.crop((int(w * crop[0]), 0, int(w * crop[1]), im.height))
        field = None
        src_rgb = np.array(im.convert("RGB"))
        if do_key:
            im, field, _dist = key_backdrop(im)
        if seam:
            im = seam_opaque(im)
        if fade:
            im = fade_edges(im)
        if do_key:
            arr = np.array(im)
            hh, ww = arr.shape[:2]
            for y0, x0 in ((0, 0), (0, ww - 4), (hh - 4, 0), (hh - 4, ww - 4)):
                p = arr[y0:y0 + 4, x0:x0 + 4]
                p[..., 3] = np.where(p[..., 3] < 32, 0, p[..., 3])
                arr[y0:y0 + 4, x0:x0 + 4] = p
            im = Image.fromarray(arr)
        path = save(im, "parallax/" + rel + ".png")
        zero = float((np.array(im)[..., 3] == 0).mean())
        print("layer", rel, im.size, "clear", round(zero, 3))
        if do_key:
            top = np.array(im)[: im.height // 5, :, 3]
            top_clear = float((top == 0).mean())
            if rel.startswith("props/"):
                # grass blades and pit rock reach the top of the plate
                need, floor = 0.05, 0.06
            elif rel.endswith("front"):
                need, floor = 0.45, 0.02
            elif rel.endswith("near"):
                need, floor = 0.55, 0.12
            elif rel.endswith("mid"):
                need, floor = 0.75, 0.12
            else:
                need, floor = 0.88, 0.12
            if top_clear < need:
                raise SystemExit("top of %s still has a plate (%s)" % (rel, round(top_clear, 3)))
            if 1 - zero < floor:
                raise SystemExit("%s lost its paint, opaque %s" % (rel, round(1 - zero, 3)))
        if field is not None:
            fields[path] = (field, src_rgb)

    base, _field, _d = key_backdrop(load("14.jpg"))
    base = drop_ground_line(base)
    level = wheel_angle(base)
    print("level angle", None if level is None else round(level, 2))
    cx, cy, rad = find_hub(base)
    kill, length = blade_mask(base, cx, cy, rad)
    print("hub", round(cx, 1), round(cy, 1), "rad", round(rad, 1), "blade", round(length, 1), "kill", int(kill.sum()))
    fly = spin_frames(base, kill, cx, cy, rad, length)
    a0, a1 = np.array(fly[0]), np.array(fly[1])
    right = (a1[..., 3] > 180) & (a0[..., 3] < 30)
    up = (a0[..., 3] > 180) & (a1[..., 3] < 30)
    rys, rxs = np.where(right)
    uys, uxs = np.where(up)
    print("prop move right", len(rxs), "up", len(uxs))
    if len(rxs) < 30 or len(uxs) < 30 or float(rxs.mean()) < cx + 20 or float(uys.mean()) > cy - 20:
        raise SystemExit("prop did not turn")

    posed = []
    for name in ("20.jpg", "19.jpg", "22.jpg"):  # climb, dive, hit
        im, field, _d = key_backdrop(load(name))
        im = drop_ground_line(im)
        posed.append(im)
        src_rgb = np.array(load(name).convert("RGB"))
        # checked after align, against the uncropped key first
        pre = os.path.join(RAW, "_pre_" + name + ".png")
        im.save(pre)
        assert_plate_gone(pre, field, src_rgb)
        os.remove(pre)

    frames = align_bottoms(fly + posed)
    names = ["poe_fly_0", "poe_fly_1", "poe_fly_2", "poe_fly_3", "poe_climb", "poe_dive", "poe_hit"]
    for im, name in zip(frames, names):
        path = save(im, "player/" + name + ".png")
        a = np.array(im)
        h, w = a.shape[:2]
        corners = [int(a[1, 1, 3]), int(a[1, w - 2, 3]), int(a[h - 2, 1, 3]), int(a[h - 2, w - 2, 3])]
        ang = wheel_angle(im)
        opaque = float((a[..., 3] > 200).mean())
        print("poe", name, im.size, "corners", corners, "angle", None if ang is None else round(ang, 2), "opaque", round(opaque, 3))
        if any(c != 0 for c in corners):
            raise SystemExit("poe corner not clear " + name)
        if int(a[..., 3].max()) < 200:
            raise SystemExit("poe lost its paint " + name)
        if level is not None and ang is not None and abs(ang - level) > 8:
            print("  wheel angle off", name, "ref", round(level, 2))
        if not (0.08 < opaque < 0.62):
            raise SystemExit("poe opaque fraction " + name)

    for path, (field, src_rgb) in fields.items():
        assert_plate_gone(path, field, src_rgb)

    save(build_beam(), "parallax/props/beam.png")
    save(build_blades(), "parallax/props/blades.png")

    # contact sheet on green so a pink plate cannot hide
    thumbs = []
    for rel in list(LAYERS) + ["player/" + n for n in names]:
        if rel.startswith("player"):
            p = os.path.join(ART, "player", rel.split("/")[1] + ".png")
        else:
            p = os.path.join(ART, "parallax", rel + ".png")
        im = Image.open(p).convert("RGBA")
        s = im.resize((int(220 * im.width / im.height), 220), Image.BILINEAR)
        bg = Image.new("RGBA", s.size, (30, 170, 70, 255))
        thumbs.append(Image.alpha_composite(bg, s).convert("RGB"))
    cols = 6
    row_h = 220
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new("RGB", (sum(t.width for t in thumbs[:cols]), row_h * rows), (20, 20, 20))
    x = y = 0
    for i, t in enumerate(thumbs):
        if x + t.width > sheet.width:
            x = 0
            y += row_h
        sheet.paste(t, (x, y))
        x += t.width
    prev = os.path.join(RAW, "_preview.png")
    sheet.save(prev)
    print("PREVIEW", prev)
    print("CHECK OK")


if __name__ == "__main__":
    main()
