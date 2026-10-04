"""Screenshot a lane telegraph (eagle dive / ironwake ram) in slow motion.
python catch_lane.py <shot-name> [bossIndex]"""
import sys
import time
from umcp import call
from watch import code, shot

name = sys.argv[1]
boss = sys.argv[2] if len(sys.argv) > 2 else "0"
call("manage_editor", {"action": "stop"}); time.sleep(2)
call("manage_editor", {"action": "play"}); time.sleep(6)
print(code('var ap=new GameObject("AP").AddComponent<AutoPilot>(); ap.player=GameObject.FindFirstObjectByType<PlayerController>().transform; '
           'ap.god=true; ap.timeScale=3f; return "bot";'), flush=True)
LANE = ('foreach (var t in Telegraph.All) if (t.halfWidth > 0.72f && t.halfLength > 5f) return "lane"; '
        'return GameState.I.Combat + " " + GameState.I.BossIndex;')
slow = False
for _ in range(900):
    r = code(LANE)
    if not slow and r == "True " + boss:
        code('CameraFx.BaseScale = 0.35f; return "slow";'); slow = True
        print("slow-mo", flush=True)
    if slow and r == "lane":
        time.sleep(0.6)
        shot(name)
        print("shot", name, flush=True)
        break
    time.sleep(0.15)
code('CameraFx.BaseScale = 1f; return "";')
