"""Generate raw art with Grok CLI headless. Usage: python gen.py [--only substr] [--force] [--jobs N]"""
import argparse
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from specs import SPECS, frame_path, build_prompt, RAW, ART

GROK = r"C:\Users\ADMIN\.grok\bin\grok.exe"


def run_frame(spec, i, force):
    out = frame_path(spec, i)
    if out.exists() and not force:
        return out, "cached"
    out.parent.mkdir(parents=True, exist_ok=True)
    prompt = build_prompt(spec, i)
    ref = spec["ref"]
    if i > 0 and spec["frames"] > 1:
        ref_img = frame_path(spec, 0)          # keep frames of one sheet consistent
    elif ref:
        ref_img = RAW / (ref[:-4] + "__f0.png")  # derive from the master of another file
    else:
        ref_img = None
    if ref_img is not None and not ref_img.exists():
        return out, f"missing reference {ref_img.name}"
    if ref_img is None:
        tool = (f"Call the image_gen tool exactly once with aspect_ratio {spec['ar']} and this prompt verbatim: "
                f"\"{prompt}\"")
    else:
        tool = (f"Call the image_edit tool exactly once with image {ref_img} and this prompt verbatim "
                f"(keep the same character design and palette, change only what is described): \"{prompt}\"")
    task = (f"{tool}. Then save/copy the resulting image as a PNG to {out}. Do nothing else. "
            "Reply with only the saved path.")
    try:
        p = subprocess.run([GROK, "-p", task, "--cwd", str(RAW.parent), "--always-approve"],
                           capture_output=True, text=True, timeout=420, encoding="utf-8", errors="replace")
    except subprocess.TimeoutExpired:
        return out, "timeout"
    if not out.exists():
        return out, "FAILED: " + (p.stdout + p.stderr).strip()[-300:]
    return out, "ok"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--jobs", type=int, default=3)
    a = ap.parse_args()
    specs = [s for s in SPECS if s["kind"] != "code" and a.only in s["path"]]
    # frame 0 first (masters), then dependents, so references exist
    for stage, pick in (("masters", lambda s, i: i == 0 and not s["ref"]),
                        ("derived", lambda s, i: i == 0 and s["ref"]),
                        ("frames", lambda s, i: i > 0)):
        jobs = [(s, i) for s in specs for i in range(s["frames"]) if pick(s, i)]
        if not jobs:
            continue
        print(f"== {stage}: {len(jobs)} images", flush=True)
        with ThreadPoolExecutor(a.jobs) as ex:
            for out, st in ex.map(lambda j: run_frame(j[0], j[1], a.force), jobs):
                print(f"  {out.relative_to(RAW)}: {st}", flush=True)


if __name__ == "__main__":
    sys.exit(main())
