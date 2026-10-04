"""Shortcuts over umcp.py for play-testing.
python ux.py code "<C# body returning a string>"
python ux.py shot <name>            -> Assets/Screenshots/<name>.png
python ux.py log [count] [filter]   -> recent console lines (errors/warnings/logs), optional substring filter
python ux.py clear"""
import json
import sys
from umcp import call


def text(r):
    res = r.get("result", r)
    return "\n".join(c.get("text", "") for c in res.get("content", [])) if "content" in res else json.dumps(r)


def main():
    cmd = sys.argv[1]
    if cmd == "code":
        print(text(call("execute_code", {"action": "execute", "code": sys.argv[2]}))[:4000])
    elif cmd == "shot":
        print(text(call("manage_camera", {"action": "screenshot", "screenshot_file_name": sys.argv[2] + ".png"}))[:300])
    elif cmd == "clear":
        call("read_console", {"action": "clear"})
    elif cmd == "log":
        n = int(sys.argv[2]) if len(sys.argv) > 2 else 40
        flt = sys.argv[3] if len(sys.argv) > 3 else None
        d = json.loads(text(call("read_console", {"types": ["error", "warning", "log"], "count": n})))
        for m in d.get("data") or []:
            s = m if isinstance(m, str) else m.get("message", "")
            s = s.split("\n")[0]
            if flt is None or flt in s:
                print(s[:240])


if __name__ == "__main__":
    main()
