# APL Launcher root scripts for LG TVs (webOS)

The scripts installed at every boot on a rooted LG TV. They go in
`/var/lib/webosbrew/init.d/`, which the Homebrew Channel runs with
`run-parts` after each start-up.

| File | What it does | Starts |
|---|---|---|
| `init.d/10-apl-block-updates` | Blocks LG's 6 firmware-update hostnames (`snu`, `snu-dev`, `nsu`, `su`, `su-dev`, `su-ssl`.lge.com) in `/etc/hosts`. | at boot |
| `init.d/11-apl-block-privacy` | Blocks 21 LG ad/telemetry/content-recognition hostnames, from the SAFE tier of furkan-bayrak/lg-tv-blocklist (CC BY 4.0). Netflix, Prime, HBO and YouTube keep working. | at boot |
| `init.d/12-apl-debloat` | After 60 s, runs `bin/apl-debloat-stop.sh`. | at boot |
| `bin/apl-debloat-stop.sh` | Stops LG's `adoverlay-service`, `acr2` (viewing tracking / LivePlus), `remotelogger`, `service-logger`, `contentminer`, `nudge`, `sportsalarm`, and adds 3 more ad servers to `/etc/hosts`. Four groups (`ads`, `tracking`, `logs`, `suggestions`): one of `/media/developer/apl-debloat/OFF-<group>` pauses that group. `OFF` pauses the whole thing. | on demand or by `12-apl-debloat` |
| `bin/apl-debloat-undo.sh` | Puts every group back: lifts the bind-mounts, restarts the systemd units, removes the 3 hostnames. | on demand |

Based on scripts/debloat-stop.sh and debloat-undo.sh in
[lasharor/cleanyourtv-WebOS](https://github.com/lasharor/cleanyourtv-WebOS)
(MIT), with the launcher check removed (the debloat runs regardless of
which app is Home) and the config.json dependency dropped (each group is
on by default; a file in `/media/developer/apl-debloat` turns one off).

## Install on the TV (one-off, SSH as root)

```sh
# bin/ runs apl-debloat-stop.sh from /media/developer/apl-debloat/
mkdir -p /media/developer/apl-debloat /var/lib/webosbrew/init.d
cd /tmp
URL=https://raw.githubusercontent.com/Knikt/APL-Launcher-for-TizenOS/claude/gallant-northcutt-5aae3a/webos/root
curl -fsSL -o /media/developer/apl-debloat/apl-debloat-stop.sh "$URL/bin/apl-debloat-stop.sh"
curl -fsSL -o /media/developer/apl-debloat/apl-debloat-undo.sh "$URL/bin/apl-debloat-undo.sh"
curl -fsSL -o /var/lib/webosbrew/init.d/12-apl-debloat         "$URL/init.d/12-apl-debloat"
chmod +x /media/developer/apl-debloat/*.sh /var/lib/webosbrew/init.d/12-apl-debloat

# Test it now (reversible). Watch for errors.
sh /media/developer/apl-debloat/apl-debloat-stop.sh

# Undo everything:
# sh /media/developer/apl-debloat/apl-debloat-undo.sh
```

Pause one group without undoing it:

```sh
touch /media/developer/apl-debloat/OFF-ads
touch /media/developer/apl-debloat/OFF-tracking
touch /media/developer/apl-debloat/OFF-logs
touch /media/developer/apl-debloat/OFF-suggestions
touch /media/developer/apl-debloat/OFF                 # the whole thing
```

## What breaks and what doesn't

Verified safe (from CleanYourTV's testing and the upstream blocklist's
evidence base): Netflix, Prime Video, HBO Max, YouTube, Disney+, the LG
app store, app updates, sign-ins, the launcher.

Expected to break: LG's ad overlays, LivePlus / content recognition,
sports alerts, "New & Hot" nudges, Live Plus viewing tracking, LG's
telemetry logs.

Not touched: `admanager` (HBO Max and Disney+ wait for it at start-up,
CleanYourTV issue 47), the firmware-update path (already blocked by
`10-apl-block-updates`), the microphones, CEC/ARC/eARC, HDMI inputs,
AirPlay and casting, HbbTV, ThinQ.
