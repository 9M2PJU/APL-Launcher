# Optimizer

**Optimize**, in APL Launcher Setup, makes the screen lighter and quieter. It works through Vetro and follows its safeguards:
- **protected** components are never touched;
- neither is anything the system needs (Vetro role *keep*: TV, home and menus, system), nor Bluetooth and HDMI-CEC.

## What it does

| Area | Action |
|---|---|
| Downloaded apps you do not use | **Uninstalled**, after a separate question. They can be reinstalled from the App Store. |
| Samsung components you do not use (ads, tracking, Bixby and voice extras, Ambient mode, store and hotel modes, diagnostics, SmartThings) | Auto-start and auto-restart **turned off**, then stopped. Nothing is erased. Alexa and the microphone hub stay, so Alexa keeps the remote's microphone button. |
| Vetro switches | *All ads and tracking* and *Bixby* turned off |
| DNS filter on the screen | About a dozen Samsung ad and tracking hosts blocked (Samsung TV Plus is left alone) |
| Leftovers | Old installation files cleared |
| Smart Hub | No longer opens by itself at power-on |
| Samsung's YouTube app | **Uninstalled** if you answer yes to its own question: TizenTube replaces it. Only the app named exactly *YouTube* is concerned. It can be reinstalled from the App Store. |

**Always kept:**
- the launcher, Vetro, your favourite apps and Samsung Internet;
- TizenBrew and its service, and TizenTube;
- Steam Link and Moonlight;
- the App Store and the Samsung account it needs;
- HDMI sources, Samsung TV Plus and USB playback;
- AirPlay, Multi View and screen mirroring;
- everything the system needs.

## How it runs

1. It reads everything installed (about 30 seconds).
2. It shows three lists: **kept**, **to uninstall** and **to turn off**.
3. It asks whether to uninstall the listed apps. Answer no to keep them all.
4. It asks for a final confirmation.
5. It saves a backup in `%APPDATA%\APL-Launcher\optimisation-<date>.json`, then applies the changes.

## Undo

**Restore** reads the latest backup. It turns the silenced components back on and puts back Smart Hub autorun, the DNS filter and the Vetro switches. Uninstalled apps come back from the App Store.

## TizenBrew or TizenTube stopped working

In APL Launcher Setup › Tools, run **Repair TizenBrew**. It checks, through Vetro:
- that TizenBrew, its service and TizenTube are installed;
- that Developer Mode is on with *Host PC IP* `127.0.0.1` (TizenBrew needs it);
- that nothing they need was turned off by the optimizer or blocked by the DNS filter;
- that the screen can reach the sites TizenBrew downloads its modules from.

It shows what it found and fixes what it safely can, after asking. It saves a report in `diagnostic-tizenbrew.txt`.

## DNS blocklist for your network

[`extras/dns-blocklist-samsung.txt`](../extras/dns-blocklist-samsung.txt) is a hosts-format list for Pi-hole, AdGuard Home, NextDNS or a router. Nothing changes on the screen, so it is fully reversible.

Apply it to a group that contains only the screen. The end of the file lists the domains you must not block.

## Ad components (reference)

Vetro turns off the ad and content-recognition (ACR) components itself. For reference, these Samsung components serve ads and analytics:

```
content-grab-service, adplatform.ADPlayer, com.samsung.tv.ad-web-framework,
com.samsung.tv.contents-recognition-dbmgr, com.samsung.tv.iacr, com.samsung.tv.targeted-ad-service,
com.samsung.tv.canalysis-service, com.samsung.tv.adplayer, com.samsung.tv.acr, com.samsung.tv.cis,
com.samsung.tv.nvision-analysis-dbmgr, com.samsung.tv.screen-analysis-dbmgr, com.samsung.tv.pisa-service,
com.samsung.tizen.samsung-analytics, com.samsung.tv.adagent-service
```

Never turn off the home screen agent, the privacy choices screen or `display-manager`: the interface breaks without them.

## Without Vetro

On the screen, open *Settings › General & Privacy › Terms & Privacy* and turn off:
- **Viewing Information Services** (ACR);
- **Interest-Based Advertisements**.

Combined with the DNS blocklist, this removes most ads without root.

`scripts/10-debloat-apps.ps1 -TvIp <ip> [-Apply]` uninstalls a fixed list of optional third-party apps over Developer Mode.
