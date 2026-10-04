"""Delegate read-only research to the Antigravity CLI (agy) so Claude reads a short answer
instead of whole files, logs, screenshots or web pages.

python tools/agy_ask.py "<question>" [-n 25] [-m gemini-3.8-flash-medium] [-t 300]
python tools/agy_ask.py -f prompt.txt

agy runs with read-only permissions (~/.gemini/antigravity-cli/settings.json denies writes).
The cleaned answer is printed; the raw answer is kept in tools/.agy_logs/.
"""
import argparse
import datetime
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AGY = os.path.join(os.environ.get("LOCALAPPDATA", ""), "agy", "bin", "agy.exe")
LOGS = os.path.join(ROOT, "tools", ".agy_logs")

CONTEXT = """Project: "Flappy Poeing", Unity 6 2D pixel game (built-in pipeline, ortho camera 480x270, PPU 32).
Score 0-29 = Flappy phase; from 30 = WASD shooter vs 3 bosses (Tempest Eagle, Ironwake, Sundown Citadel), 3 phases each.
Code: Assets/_Game/Scripts (Editor/SceneBuilder.cs builds the scene). Python helpers: tools/. Screenshots: Assets/Screenshots.
HUD: top = boss HP bar with phase ticks; bottom-left = "HP n" card + 5 super-meter cards (pink = filled), not hearts."""

RULES = """RULES: You are a read-only research assistant. Never create, edit, move or delete files and never run builds.
Answer in terse English bullets. Cite code as path:line (relative to the project root, no file:// links).
Quote at most 8 lines of code, only when asked. Say "unsure" instead of guessing. Hard limit: {n} lines."""


def clean(text):
    text = re.sub(r"\[([^\]]+)\]\(file:///[^)]*\)", r"\1", text)
    text = text.replace(ROOT.replace("\\", "/") + "/", "")
    lines = [l for l in text.splitlines()
             if not l.startswith("No further tools needed") and "subagent" not in l.lower()]
    return re.sub(r"\n{3,}", "\n\n", "\n".join(lines)).strip()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("question", nargs="?")
    ap.add_argument("-f", "--file")
    ap.add_argument("-n", "--lines", type=int, default=25)
    ap.add_argument("-m", "--model", default="gemini-3.8-flash-medium")
    ap.add_argument("-t", "--timeout", type=int, default=300)
    a = ap.parse_args()
    q = open(a.file, encoding="utf-8").read() if a.file else a.question
    if not q:
        sys.exit("need a question or -f file")
    prompt = f"{CONTEXT}\n\n{RULES.format(n=a.lines)}\n\nTASK:\n{q}"
    r = subprocess.run([AGY, "--model", a.model, "--print-timeout", f"{a.timeout}s", "-p", prompt],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    raw = (r.stdout or "") + (("\n[stderr]\n" + r.stderr) if r.stderr.strip() else "")
    os.makedirs(LOGS, exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    with open(os.path.join(LOGS, stamp + ".md"), "w", encoding="utf-8") as f:
        f.write(f"# Q\n{q}\n\n# A (exit {r.returncode})\n{raw}\n")
    print(clean(r.stdout or raw))


if __name__ == "__main__":
    main()
