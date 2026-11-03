"""Checks run by CI and before each release.

    python3 tools/check.py

- every launcher script parses (node --check);
- version.json and js/build.js match launcher/config.xml and list existing files;
- every image referenced by the catalogs exists;
- no private file (keys, certificates, local avatars) is tracked by git.
"""
import json, os, re, subprocess, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
L = os.path.join(ROOT, "launcher")
errors = []

def fail(msg):
    errors.append(msg)

# 1. scripts parse
for name in sorted(os.listdir(os.path.join(L, "js"))):
    if name.endswith(".js"):
        r = subprocess.run(["node", "--check", os.path.join(L, "js", name)], capture_output=True, text=True)
        if r.returncode:
            fail("syntax: js/%s\n%s" % (name, r.stderr.strip()))
for name in sorted(os.listdir(os.path.join(L, "bridge"))):
    if name.endswith(".js"):
        r = subprocess.run(["node", "--check", os.path.join(L, "bridge", name)], capture_output=True, text=True)
        if r.returncode:
            fail("syntax: bridge/%s\n%s" % (name, r.stderr.strip()))

# 2. release manifest
config = open(os.path.join(L, "config.xml"), encoding="utf-8").read()
version = re.search(r'<widget[^>]*\sversion="([^"]+)"', config).group(1)
manifest = json.load(open(os.path.join(L, "version.json"), encoding="utf-8"))
if manifest["version"] != version:
    fail("version.json is %s but config.xml is %s: run tools/make-build.py" % (manifest["version"], version))
build = open(os.path.join(L, "js", "build.js"), encoding="utf-8").read()
if '"version": "%s"' % version not in build:
    fail("js/build.js does not match config.xml: run tools/make-build.py")
for path in manifest["scripts"] + manifest["styles"] + manifest["images"]:
    if ".local." not in path and not os.path.exists(os.path.join(L, path)):
        fail("missing file listed in version.json: " + path)
index = open(os.path.join(L, "index.html"), encoding="utf-8").read()
for s in ("js/build.js", "js/boot.js"):
    if s not in index:
        fail("index.html must load " + s)

# 3. catalog images
for cat in ("catalog.js", "devices.js"):
    text = open(os.path.join(L, "js", cat), encoding="utf-8").read()
    for path in re.findall(r'"(images/[^"]+)"', text):
        if not os.path.exists(os.path.join(L, path)):
            fail("%s references a missing image: %s" % (cat, path))

# 4. nothing private tracked
tracked = subprocess.run(["git", "-C", ROOT, "ls-files"], capture_output=True, text=True).stdout.split()
for f in tracked:
    if re.search(r"(\.local\.js$|\.p12$|\.pwd$|password|images/avatars/(custom|memoji)/)", f, re.I):
        fail("private file tracked by git: " + f)

if errors:
    print("\n".join("✗ " + e for e in errors))
    sys.exit(1)
print("✓ all checks passed (version %s, %d scripts, %d images)" % (version, len(manifest["scripts"]), len(manifest["images"])))
