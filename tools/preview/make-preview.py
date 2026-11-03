"""Prépare l'aperçu en ligne du launcher (page + fichiers) dans un dossier.

Usage : python3 tools/preview/make-preview.py <dossier_de_sortie>
La page produite (apl-launcher.html) n'a pas de <html>/<head>/<body> : elle est
faite pour une page hébergée qui ajoute elle-même ce squelette. Le launcher est
identique à celui de la TV ; js/preview.js ajoute la télécommande à l'écran.
"""
import json, os, re, shutil, sys

out = sys.argv[1] if len(sys.argv) > 1 else "dist/apercu"
L = os.path.join(os.path.dirname(__file__), "..", "..", "launcher")
html = open(os.path.join(L, "index.html"), encoding="utf-8").read()
body = html[html.index("<body"):]
body = body[body.index(">") + 1: body.rindex("</body>")]
# Pas d'API Samsung ni de clés dans l'aperçu
body = re.sub(r'\s*<!-- API Samsung[^>]*-->\s*', "\n", body)
body = re.sub(r'\s*<script src="\$WEBAPIS[^"]*"></script>', "", body)
body = re.sub(r'\s*<script src="js/keys\.local\.js"></script>', "", body)
body = re.sub(r'\s*<!--[^>]*keys[^>]*-->', "", body)
# boot.js charge les scripts listés dans js/build.js : ils sont tous publiés avec la page
scripts = re.findall(r'<script src="([^"]+)"', body)
build = json.load(open(os.path.join(L, "version.json"), encoding="utf-8"))
scripts += [s for s in build["scripts"] if ".local." not in s and s not in scripts]
# Les avatars (sans secret) sont gardés s'ils existent, avant profiles.js
if os.path.exists(os.path.join(L, "js", "avatars.local.js")):
    scripts.insert(scripts.index("js/profiles.js"), "js/avatars.local.js")

head = """<title>APL Launcher</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap">
<link rel="stylesheet" href="css/launcher.css">
<style>
  /* Police : SF Pro si présente, sinon Inter (proche de SF Pro). */
  html body { font-family: "SF Pro Display", -apple-system, BlinkMacSystemFont, Inter, "Segoe UI", Roboto, sans-serif; }
</style>
<script>(function () { function f() { document.body.classList.add("mode-shelf"); } if (document.body) f(); else document.addEventListener("DOMContentLoaded", f); })();</script>
"""
os.makedirs(out, exist_ok=True)
open(os.path.join(out, "apl-launcher.html"), "w", encoding="utf-8").write(head + body.strip() + "\n")

files = ["css/launcher.css"] + scripts
for root, _, names in os.walk(os.path.join(L, "images")):
    for n in names:
        if n.startswith("."):
            continue
        files.append(os.path.relpath(os.path.join(root, n), L).replace(os.sep, "/"))
for f in files:
    os.makedirs(os.path.dirname(os.path.join(out, f)), exist_ok=True)
    shutil.copy(os.path.join(L, f), os.path.join(out, f))
json.dump(files, open(os.path.join(out, "files.json"), "w"), indent=0)
print(f"{len(files)} fichiers + apl-launcher.html dans {out}")
