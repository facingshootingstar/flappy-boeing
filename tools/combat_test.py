"""Difficulty test: bot is invincible through the Flappy phase, mortal from the first boss on.
On death it presses RETRY (checkpoint) and plays again, up to N attempts.
python combat_test.py <prefix> [timeScale] [attempts]"""
import sys
import time
from umcp import call
from watch import watch, code

prefix = sys.argv[1]
ts = sys.argv[2] if len(sys.argv) > 2 else "2"
attempts = int(sys.argv[3]) if len(sys.argv) > 3 else 3
BOT = ('var ap=new GameObject("AP").AddComponent<AutoPilot>(); ap.player=GameObject.FindFirstObjectByType<PlayerController>().transform; '
       'ap.god={god}; ap.timeScale=' + ts + 'f; return "bot";')

call("manage_editor", {"action": "stop"}); time.sleep(2)
call("manage_editor", {"action": "play"}); time.sleep(6)
print(code(BOT.format(god="true")), flush=True)


def mortal():
    print("  combat -> god off:", code('GameState.God=false; return "ok";'), flush=True)


for n in range(attempts):
    res = watch(f"{prefix}{n}", 1200, mortal if n == 0 else None)
    print(f"attempt {n}: {res}", flush=True)
    if res != "dead":
        break
    print("  retry:", code('GameState.I.Retry(); return "ok";'), flush=True)
    time.sleep(1.5)
    print(" ", code(BOT.format(god="false")), flush=True)
print("done", flush=True)
