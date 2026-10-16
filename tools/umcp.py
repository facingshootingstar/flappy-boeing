"""Minimal MCP-over-HTTP client for the Unity MCP server (used because the Claude session cannot hot-load MCP tools).
Usage: python umcp.py list | python umcp.py call <tool> '<json args>'"""
import json
import sys
import urllib.request

URL = "http://127.0.0.1:8080/mcp"
H = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}


def post(body, sid=None, timeout=300):
    h = dict(H)
    if sid:
        h["mcp-session-id"] = sid
    r = urllib.request.urlopen(urllib.request.Request(URL, json.dumps(body).encode(), h), timeout=timeout)
    txt = r.read().decode()
    data = [l[5:].strip() for l in txt.splitlines() if l.startswith("data:")]
    return r.headers.get("mcp-session-id"), (json.loads(data[-1]) if data else None)


def session():
    sid, _ = post({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "cc", "version": "1"}}})
    post({"jsonrpc": "2.0", "method": "notifications/initialized"}, sid)
    return sid


# tools outside the default "core" group; groups are per MCP session, so enable before each call
GROUPS = {"execute_code": "scripting_ext", "manage_scriptable_object": "scripting_ext",
          "run_tests": "testing", "get_test_job": "testing"}


def call(tool, args):
    sid = session()
    if tool in GROUPS:
        post({"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {
            "name": "manage_tools", "arguments": {"action": "activate", "group": GROUPS[tool]}}}, sid)
    _, r = post({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": tool, "arguments": args}}, sid)
    return r


if __name__ == "__main__":
    if sys.argv[1] == "list":
        sid = session()
        _, r = post({"jsonrpc": "2.0", "id": 2, "method": "tools/list"}, sid)
        for t in r["result"]["tools"]:
            print(t["name"], "-", t.get("description", "").split("\n")[0][:110])
    else:
        r = call(sys.argv[2], json.loads(sys.argv[3]) if len(sys.argv) > 3 else {})
        res = r.get("result", r)
        print(chr(10).join(c.get("text", "") for c in res.get("content", []))[:6000] if "content" in res else json.dumps(r)[:3000])
