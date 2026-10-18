import base64
import datetime
import json
import os
import re
import sys
import urllib.request
import urllib.error

CFG = os.path.join(os.environ["USERPROFILE"], ".wakatime.cfg")

# Read api key from the wakatime config (never printed).
api_key = None
try:
    with open(CFG, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            m = re.match(r"\s*api_key\s*=\s*(\S+)", line)
            if m:
                api_key = m.group(1).strip()
                break
except Exception as e:
    print("Could not read config:", e)

if not api_key:
    print("No api_key found in", CFG)
    sys.exit(1)

print("API key loaded (length", len(api_key), "chars)")

auth = "Basic " + base64.b64encode(api_key.encode("utf-8")).decode("ascii")


def fetch(url):
    req = urllib.request.Request(url, headers={"Authorization": auth, "Accept": "application/json"})
    with urllib.request.urlopen(req, timeout=20) as resp:
        return json.loads(resp.read().decode("utf-8"))


def fetch_today_heartbeat_spans(date_str, projects):
    """Fetch today's heartbeats and print min->max local time per requested project."""
    url = f"https://wakatime.com/api/v1/users/current/heartbeats?date={date_str}"
    try:
        payload = fetch(url)
    except Exception as e:
        print("heartbeats request failed:", type(e).__name__, e)
        return
    hbs = payload.get("data", [])
    print(f"== heartbeats {date_str}: {len(hbs)} entries ==")
    by_project = {}
    for hb in hbs:
        proj = hb.get("project") or "(none)"
        by_project.setdefault(proj, []).append(hb.get("time"))
    for proj in projects:
        times = by_project.get(proj)
        if not times:
            print(f"  {proj}: no heartbeats")
            continue
        lo = datetime.datetime.fromtimestamp(min(times)).strftime("%H:%M")
        hi = datetime.datetime.fromtimestamp(max(times)).strftime("%H:%M")
        print(f"  {proj}: {lo} -> {hi}  ({len(times)} heartbeats)")


def summarize_data(data):
    if not data:
        return
    print("== Data returned by API ==")
    for d in data:
        total = d.get("grand_total", {})
        rng = d.get("range", {})
        print(f"- {rng.get('date', '?')} total={total.get('text', '?')} "
              f"({total.get('decimal', '?')}h, active={total.get('total_seconds', 0)}s)")
        print("   raw range:", json.dumps(rng))
        projs = d.get("projects", [])
        if projs:
            projs = sorted(projs, key=lambda p: p.get("total_seconds", 0), reverse=True)
            print("  projects:")
            for p in projs[:8]:
                print(f"    - {p.get('name', '?')}: {p.get('text', '?')}")
        langs = d.get("languages", [])
        if langs:
            langs = sorted(langs, key=lambda l: l.get("total_seconds", 0), reverse=True)
            print("  languages:")
            for l in langs[:8]:
                print(f"    - {l.get('name', '?')}: {l.get('text', '?')}")


for rng in ("last_7_days", "last_30_days"):
    url = f"https://wakatime.com/api/v1/users/current/summaries?range={rng}"
    print("\n===== RANGE:", rng, "=====")
    try:
        payload = fetch(url)
        summarize_data(payload.get("data", []))
    except urllib.error.HTTPError as e:
        print("HTTP error", e.code, e.reason)
        try:
            print(e.read().decode("utf-8", "replace")[:500])
        except Exception:
            pass
    except Exception as e:
        print("Request failed:", type(e).__name__, e)

print("\n===== TODAY HEARTBEAT SPANS =====")
fetch_today_heartbeat_spans("2026-08-22", ["MTM_Waitlist", "rules"])
