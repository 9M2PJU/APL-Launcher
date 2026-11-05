#!/usr/bin/env python3
"""Install an already Partner-signed APL Launcher WGT through Vetro on macOS."""

import argparse
import base64
import hashlib
import json
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

CHUNK_SIZE = 36_000


def request(base, path, payload=None, timeout=60):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(
        f"{base}{path}", data=data,
        headers={"Content-Type": "application/json"} if data is not None else {},
    )
    with urllib.request.urlopen(req, timeout=timeout) as response:
        return json.load(response)


def exec_vetro(base, command, timeout=60):
    result = request(base, "/exec", {"cmd": command}, timeout)
    if not result.get("ok"):
        raise RuntimeError(result.get("stderr") or result.get("error") or "Vetro rejected command")
    return str(result.get("output", ""))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tv_ip", help="TV IPv4 address (Vetro must be reachable on port 8099)")
    parser.add_argument("package", type=Path, help="Partner-signed full .wgt package")
    parser.add_argument("--vetro-port", type=int, default=8099, help="Vetro HTTP port (default: 8099)")
    parser.add_argument("--no-launch", action="store_true", help="Do not launch after installation")
    args = parser.parse_args()
    if not 1 <= args.vetro_port <= 65535:
        parser.error("Vetro port must be between 1 and 65535")
    if not re.fullmatch(r"(?:[0-9]{1,3}\.){3}[0-9]{1,3}", args.tv_ip):
        parser.error("TV address must be an IPv4 address")
    if not args.package.is_file() or args.package.suffix.lower() != ".wgt":
        parser.error("package must be an existing .wgt file")

    package = args.package.read_bytes()
    with __import__("zipfile").ZipFile(args.package) as archive:
        config = archive.read("config.xml").decode("utf-8")
        match = re.search(r'<widget[^>]*\bid="([^"]+)"[^>]*\bversion="([^"]+)"', config)
        if not match:
            raise RuntimeError("WGT config.xml does not contain an app ID and version")
        app_id, version = match.groups()
        if not re.fullmatch(r"APLauncher(?:\.[A-Za-z0-9_.-]+)?", app_id) or not re.fullmatch(r"[A-Za-z0-9_.+-]+", version):
            raise RuntimeError("WGT contains an unexpected app ID or version")
        if "tizen-manifest.xml" not in archive.namelist():
            raise RuntimeError("Use the full hybrid package (not the launcher-only WGT)")
    base = f"http://{args.tv_ip}:{args.vetro_port}"
    status = request(base, "/status", timeout=10)
    if not (status.get("agent") or {}).get("connected"):
        raise RuntimeError("Vetro agent is not connected; check Vetro on the TV")

    remote = "/tmp/apl-launcher-macos.wgt"
    encoded = base64.b64encode(package).decode("ascii")
    exec_vetro(base, f": > {remote}.b64")
    for offset in range(0, len(encoded), CHUNK_SIZE):
        chunk = encoded[offset:offset + CHUNK_SIZE]
        exec_vetro(base, f"printf '%s' '{chunk}' >> {remote}.b64")
        print(f"Sending package: {min(100, (offset + CHUNK_SIZE) * 100 // len(encoded))}%", end="\r", flush=True)
    digest = hashlib.sha256(package).hexdigest()
    remote_hash = exec_vetro(base, f"base64 -d {remote}.b64 > {remote} && rm -f {remote}.b64 && chmod 644 {remote} && sha256sum {remote}")
    if digest not in remote_hash:
        exec_vetro(base, f"rm -f {remote} {remote}.b64")
        raise RuntimeError("Package transfer failed SHA-256 verification")

    try:
        try:
            exec_vetro(base, f"pkgcmd -i -t wgt -p {remote} 2>&1", timeout=120)
        except Exception as exc:
            print(f"pkgcmd reported: {exc}", file=sys.stderr)
        installed = False
        for attempt in range(6):
            check = exec_vetro(base, f"pkgcmd -l 2>/dev/null | grep -iF '[{app_id.split('.')[0]}]' | grep -cF 'version [{version}]' || true")
            overlay = exec_vetro(base, f"app_launcher -l 2>/dev/null | grep -c '{app_id.split('.')[0]}.Overlay' || true")
            if check.strip().endswith("1") and overlay.strip().endswith("1"):
                installed = True
                break
            if attempt < 5:
                time.sleep(4)
        if not installed:
            try:
                request(base, "/install", {"path": remote}, timeout=180)
            except Exception as exc:
                print(f"Vetro installer reported: {exc}", file=sys.stderr)
            time.sleep(4)
            check = exec_vetro(base, f"pkgcmd -l 2>/dev/null | grep -iF '[{app_id.split('.')[0]}]' | grep -cF 'version [{version}]' || true")
            overlay = exec_vetro(base, f"app_launcher -l 2>/dev/null | grep -c '{app_id.split('.')[0]}.Overlay' || true")
            installed = check.strip().endswith("1") and overlay.strip().endswith("1")
        if not installed:
            raise RuntimeError(f"TV did not confirm installation of {app_id} {version}; verify the package is signed for this TV")
    finally:
        try:
            exec_vetro(base, f"rm -f {remote} {remote}.b64")
        except Exception:
            pass
    if not args.no_launch:
        try:
            request(base, "/apps/launch", {"appId": app_id}, timeout=25)
        except (urllib.error.URLError, TimeoutError) as exc:
            print(f"Installed; automatic launch failed: {exc}", file=sys.stderr)
    print(f"Installed {app_id} {version}.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, RuntimeError, urllib.error.URLError, KeyError, ValueError) as error:
        print(f"Installation failed: {error}", file=sys.stderr)
        sys.exit(1)
