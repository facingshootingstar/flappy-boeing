"""Check every sprite against the manifest. Usage: python verify.py"""
import numpy as np
from PIL import Image
from specs import SPECS, ART, PALETTE

ALLOWED = {tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) for h in PALETTE}
ALLOWED |= {(243, 224, 196), (196, 120, 74), (42, 33, 28), (62, 142, 138)}
bad = 0
for s in SPECS:
    f = ART / s["path"]
    msgs = []
    if not f.exists():
        print("MISSING", s["path"]); bad += 1; continue
    im = Image.open(f).convert("RGBA")
    a = np.array(im).astype(int)
    exp_w = s["w"] * (s["frames"] if s["kind"] == "sprite" else 1)
    if im.size != (exp_w, s["h"]):
        msgs.append(f"size {im.size} != {(exp_w, s['h'])}")
    al = a[..., 3]
    if ((al > 0) & (al < 255)).sum() and not s["path"].endswith("safe_lane.png"):
        msgs.append("semi-alpha")
    op = al > 0
    cols = {tuple(c) for c in a[op][:, :3]}
    off = cols - ALLOWED
    if off:
        msgs.append(f"{len(off)} off-palette colours")
    if s["kind"] == "bg_tile":
        wrap = np.abs(a[:, 0] - a[:, -1]).mean()
        inner = np.mean([np.abs(a[:, i] - a[:, i + 1]).mean() for i in range(a.shape[1] - 1)])
        if wrap > inner * 1.5 + 0.5:
            msgs.append(f"seam wrap={wrap:.1f} typical={inner:.1f}")
        if op.mean() < 0.005:
            msgs.append("layer nearly empty")
    elif s["kind"] == "bg_opaque" and not (al == 255).all():
        msgs.append("sky not opaque")
    elif s["kind"] == "sprite":
        if op.mean() < 0.03:
            msgs.append(f"very sparse ({op.mean():.2f})")
        w = s["w"]
        for i in range(s["frames"]):
            fr = al[:, i * w:(i + 1) * w]
            if not fr.any():
                msgs.append(f"frame {i} empty")
    print(("WARN " if msgs else "ok   ") + s["path"] + ("  " + "; ".join(msgs) if msgs else ""))
    bad += bool(msgs)
print(f"\n{len(SPECS) - bad}/{len(SPECS)} clean")
