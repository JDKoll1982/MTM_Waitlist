import os

DB = os.path.join(os.environ["USERPROFILE"], ".wakatime", "offline_heartbeats.bdb")
LOG = os.path.join(os.environ["USERPROFILE"], ".wakatime", "wakatime.log")

print("== offline_heartbeats.bdb header (first 32 bytes) ==")
with open(DB, "rb") as f:
    head = f.read(32)
    print(head.hex(" "))
    # ASCII interpretation
    print(repr(head))

print("\n== python bdb modules ==")
for mod in ("berkeleydb", "bsddb3", "bsddb"):
    try:
        m = __import__(mod)
        print(f"{mod}: OK {getattr(m, '__version__', '?')}")
    except Exception as e:
        print(f"{mod}: NOT available ({type(e).__name__})")

print("\n== extension global storage ==")
for base in (os.path.join(os.environ.get("APPDATA", ""), "Code", "User", "globalStorage"),
             os.path.join(os.environ.get("APPDATA", ""), "Code", "User", "workspaceStorage")):
    if not os.path.isdir(base):
        print(f"{base}: missing")
        continue
    for name in os.listdir(base):
        if "wakatime" in name.lower():
            p = os.path.join(base, name)
            print(f"{base}\\{name}")
            if os.path.isdir(p):
                for root, dirs, files in os.walk(p):
                    for f in files:
                        fp = os.path.join(root, f)
                        try:
                            print("   ", fp, os.path.getsize(fp))
                        except OSError:
                            print("   ", fp, "?")
            else:
                try:
                    print("   size", os.path.getsize(p))
                except OSError:
                    pass

print("\n== wakatime.log first 40 lines ==")
try:
    with open(LOG, "r", encoding="utf-8", errors="replace") as f:
        for i, line in enumerate(f):
            if i >= 40:
                break
            print(line.rstrip()[:200])
except Exception as e:
    print("ERR", e)
