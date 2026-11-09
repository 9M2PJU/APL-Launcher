"""Builds APL Launcher for LG TVs (webOS): an .ipk with the launcher and its helper service.

    python tools/make-webos.py                    your own TV: your keys, avatars, fonts and sounds (never publish it)
    python tools/make-webos.py --release          no personal file (apl-launcher-vX.Y.Z-lg-webos.ipk + SHA256)
    python tools/make-webos.py --personal DIR     take the personal files from another launcher/ folder
                                                  (a git worktree has none of its own)

Then, with LG's webOS CLI (npm install -g @webos-tools/cli) and the TV in Developer Mode:
    ares-install -d <tv> dist/com.knikt.apllauncher_<version>_all.ipk
    ares-launch -d <tv> com.knikt.apllauncher

The launcher is the same code as on Samsung (launcher/): js/webos.js finds out it runs on webOS and gives
the rest of the code LG's apps, keys and settings (js/webos-system.js, js/webos-settings.js). Samsung's own
files stay out: config.xml, the Tizen bridge, the webapis script line.
  app id      com.knikt.apllauncher
  service id  com.knikt.apllauncher.service (webos/service/)
The version comes from launcher/config.xml.
"""
import hashlib, json, os, re, shutil, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
L = os.path.join(ROOT, "launcher")
APP_ID = "com.knikt.apllauncher"
SERVICE_ID = APP_ID + ".service"

release = "--release" in sys.argv
out = os.path.join(ROOT, "dist")
if "--out" in sys.argv:
    out = os.path.abspath(sys.argv[sys.argv.index("--out") + 1])
personal = L
if "--personal" in sys.argv:
    personal = os.path.abspath(sys.argv[sys.argv.index("--personal") + 1])

config = open(os.path.join(L, "config.xml"), encoding="utf-8").read()
version = re.search(r'<widget[^>]*\sversion="([^"]+)"', config).group(1)
work = os.path.join(out, "webos")
app_dir, svc_dir = os.path.join(work, "app"), os.path.join(work, "service")
shutil.rmtree(work, ignore_errors=True)
os.makedirs(app_dir); os.makedirs(svc_dir)

# ---- the launcher: everything in launcher/ but Samsung's own files and the personal ones
SKIP_TOP = {"config.xml", "bridge", "version.json", ".manifest.tmp", "author-signature.xml", "signature1.xml"}
PRIVATE = ("js/keys.local.js", "js/avatars.local.js", "js/keys.example.js")
def is_personal(rel):
    return (rel.startswith(("fonts/", "sounds/")) and not rel.endswith((".css", ".md", ".txt"))) or \
           rel.startswith(("images/avatars/custom/", "images/avatars/memoji/"))
for base, dirs, files in os.walk(L):
    for f in files:
        src = os.path.join(base, f)
        rel = os.path.relpath(src, L).replace("\\", "/")
        if rel.split("/")[0] in SKIP_TOP or rel.endswith(".wgt") or rel in PRIVATE or is_personal(rel):
            continue
        dst = os.path.join(app_dir, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)

# index.html without Samsung's API script (it does not exist on LG)
html = open(os.path.join(L, "index.html"), encoding="utf-8").read()
html = re.sub(r'\s*<!-- Samsung API[^>]*-->\s*<script src="\$WEBAPIS/webapis/webapis\.js"></script>', "", html)
if "$WEBAPIS" in html:
    sys.exit("index.html: Samsung's script line was not found, update tools/make-webos.py")
open(os.path.join(app_dir, "index.html"), "w", encoding="utf-8", newline="\n").write(html)

# Keys, avatars, fonts, sounds: yours on your own TV; empty keys and none of the rest in the published package
keys_src = os.path.join(personal, "js", "keys.local.js")
if release or not os.path.exists(keys_src):
    keys_src = os.path.join(L, "js", "keys.example.js")
shutil.copy2(keys_src, os.path.join(app_dir, "js", "keys.local.js"))
copied = []
if not release:
    for base, dirs, files in os.walk(personal):
        for f in files:
            src = os.path.join(base, f)
            rel = os.path.relpath(src, personal).replace("\\", "/")
            if rel == "js/avatars.local.js" or is_personal(rel):
                dst = os.path.join(app_dir, rel)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(src, dst)
                copied.append(rel)

# Icons: 80 x 80 and 130 x 130 (LG's sizes), from the 512 x 512 one; the splash: the logo on black
from PIL import Image
icon = Image.open(os.path.join(L, "icon.png")).convert("RGBA")
icon.resize((80, 80), Image.LANCZOS).save(os.path.join(app_dir, "icon-80.png"))
icon.resize((130, 130), Image.LANCZOS).save(os.path.join(app_dir, "icon-130.png"))
splash = Image.new("RGB", (1920, 1080), (0, 0, 0))
logo = icon.resize((300, 300), Image.LANCZOS)
splash.paste(logo, ((1920 - 300) // 2, (1080 - 300) // 2), logo)
splash.save(os.path.join(app_dir, "splash.png"))

appinfo = {
    "id": APP_ID,
    "version": version,
    "vendor": "Knikt (Gero Corp.)",
    "type": "web",
    "main": "index.html",
    "title": "APL Launcher",
    "icon": "icon-80.png",
    "largeIcon": "icon-130.png",
    "splashBackground": "splash.png",
    "bgColor": "#000000",
    "iconColor": "#1c1c1e",
    "resolution": "1920x1080",
    "disableBackHistoryAPI": True,
    "handlesRelaunch": True,
    "transparent": False,
    "appDescription": "Apple TV style Home Screen",
}
json.dump(appinfo, open(os.path.join(app_dir, "appinfo.json"), "w", encoding="utf-8", newline="\n"), indent=2)

# ---- the helper service
for f in ("index.js", "ssap.js"):
    shutil.copy2(os.path.join(ROOT, "webos", "service", f), os.path.join(svc_dir, f))
json.dump({"name": SERVICE_ID, "version": version, "main": "index.js", "description": "APL Launcher helper"},
          open(os.path.join(svc_dir, "package.json"), "w", encoding="utf-8", newline="\n"), indent=2)
json.dump({"id": SERVICE_ID, "description": "LG Connect Apps relay and sleep timer for APL Launcher",
           "services": [{"name": SERVICE_ID, "description": "APL Launcher helper"}]},
          open(os.path.join(svc_dir, "services.json"), "w", encoding="utf-8", newline="\n"), indent=2)

print("APL Launcher", version, "for webOS:", "release" if release else "personal (%d files of yours)" % len(copied), "->", work)

if "--no-package" in sys.argv:
    sys.exit(0)
ares = shutil.which("ares-package") or shutil.which("ares-package.cmd")
if not ares:
    sys.exit("ares-package not found: npm install -g @webos-tools/cli")
subprocess.check_call([ares, "--no-minify", "-o", out, app_dir, svc_dir])
ipk = os.path.join(out, "%s_%s_all.ipk" % (APP_ID, version))
if release:
    final = os.path.join(out, "apl-launcher-v%s-lg-webos.ipk" % version)
    shutil.move(ipk, final)
    ipk = final
    h = hashlib.sha256(open(ipk, "rb").read()).hexdigest()
    open(os.path.join(out, "SHA256SUMS-ipk.txt"), "w", encoding="utf-8", newline="\n").write("%s  %s\n" % (h, os.path.basename(ipk)))
print("package:", ipk, os.path.getsize(ipk), "bytes")
