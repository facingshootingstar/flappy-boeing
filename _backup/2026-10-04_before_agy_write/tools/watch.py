"""Play-test watcher: polls GameState every few seconds, prints one line per state change and screenshots each new boss phase.
python watch.py <prefix> [maxSeconds]"""
import json
import sys
import time
from umcp import call
from ux import text

Q = ('var g=GameState.I; if(g==null) return "none"; var b=Health.Boss; '
     'return g.Score+"|"+g.Combat+"|"+g.BossIndex+"|"+g.BossPhase+"|"+g.Alive+"|"+g.Victory+"|"+(b?b.hp.ToString("F0"):"-")+"|"+GameState.I.HitsTaken'
     '+"|"+(1f/Time.smoothDeltaTime).ToString("F0")+"|"+(BulletPool.I?BulletPool.I.Active.Count:0);')


def code(c):
    r = json.loads(text(call("execute_code", {"action": "execute", "code": c})))
    return (r.get("data") or {}).get("result") or r.get("message")


def shot(name):
    call("manage_camera", {"action": "screenshot", "screenshot_file_name": name + ".png"})
    print("  shot " + name, flush=True)


def watch(prefix, limit=900, on_combat=None):
    """Returns 'win', 'dead' or 'timeout'. on_combat() runs once when combat starts."""
    start = time.time()
    last, shots, combat_seen = None, set(), False
    while time.time() - start < limit:
        try:
            s = code(Q)
        except Exception as e:  # editor busy / domain reload
            s = "err " + str(e)[:80]
        parts = s.split("|")
        if len(parts) >= 6:
            score, combat, bi, ph, alive, win = parts[:6]
            if combat == "True" and not combat_seen:
                combat_seen = True
                if on_combat:
                    on_combat()
            key = (combat, bi, ph, alive, win)
            if key != last:
                print(f"[{time.time() - start:5.0f}s] score={score} combat={combat} boss={bi} phase={ph} alive={alive} win={win} hp={parts[6]} hits={parts[7]} fps={parts[8]} bullets={parts[9]}", flush=True)
                last = key
                tag = f"{prefix}_b{bi}p{ph}" if combat == "True" else f"{prefix}_flappy"
                if alive == "False":
                    tag = f"{prefix}_{'win' if win == 'True' else 'dead'}"
                if tag not in shots:
                    shots.add(tag)
                    time.sleep(2.5)
                    shot(tag)
                if alive == "False":
                    time.sleep(4)
                    shot(tag + "2")
                    print("  end", flush=True)
                    return "win" if win == "True" else "dead"
        else:
            print(f"[{time.time() - start:5.0f}s] {s[:200]}", flush=True)
        time.sleep(3)
    return "timeout"


if __name__ == "__main__":
    watch(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 900)
