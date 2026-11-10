<div align="center">

# APL Launcher

**An Apple TV–style home screen for your TV.**
Samsung (Tizen) · LG (webOS) · Android TV and Google TV

Created by Knikt (Gero Corp.)

![Home screen](docs/images/home.jpg)

**[▶ Try it in your browser](https://knikt.github.io/APL-Launcher/)**: nothing to install, nothing touches your TV.

[Install](#install) · [Which TV?](#which-tv) · [Debloat](#debloat-built-in) · [What's new](CHANGELOG.md) · [Help](#help) · [Français](README.fr.md)

</div>

---

## What you get

- **The Apple TV home screen**: a full-screen Top Shelf for the app you are on, a Liquid Glass dock, a grid of apps, folders, smooth motion and sounds.
- **Continue Watching** from your apps (IPTV players, Stremio, [APL TV](https://github.com/Knikt/APL-TV)), and what is trending on Netflix, Prime Video, Disney+, YouTube and more.
- **Control Center**, **Search**, **Settings** (your TV's own picture settings included), **Sources** for the HDMI inputs, **profiles** for the household, an **aerial screen saver**.
- **Home opens APL Launcher**, twice Home for the App Switcher, and the TV starts on it in about a second.
- **A built-in debloat**: ads, tracking and useless apps or services removed. [More below](#debloat-built-in).
- **Updates by itself**, in **English and French**.

| | | |
|---|---|---|
| ![Grid](docs/images/grid.jpg) | ![Control Center](docs/images/control-center.jpg) | ![Settings](docs/images/settings.jpg) |
| ![Search](docs/images/search.jpg) | ![Sources](docs/images/sources.jpg) | ![Profiles](docs/images/profiles.jpg) |

## Which TV?

| TV | Without root | With root |
|---|---|---|
| **Samsung** (Tizen 7 and later; Tizen 6 in part) | The launcher as an app, installed with [Apps2Samsung](https://github.com/Apps2Samsung/Apps2Samsung) | **Everything**, with [Vetro](https://github.com/andrearicci93/vetro) on the screen |
| **LG** (webOS) | The launcher, through LG's Developer Mode (needs a PC) | **Everything**, with the Homebrew Channel ([rooted](https://e.nya.je/getroot/)) |
| **Android TV, Google TV** | **Everything**: no root needed | — |

What root adds: Home opens APL Launcher and the TV starts on it, the Control Center over apps (Samsung) or on the Guide button (LG), closing the apps left open, the TV's picture settings inside the launcher, and the debloat. The Setup lists it for your TV. What works on which model: [docs/compatibility.md](docs/compatibility.md).

## Install

You need a Windows 10 or 11 PC on the same network as the TV.

1. **Download APL Launcher Setup** (`APL-Launcher-Setup.exe`) from the [latest release](https://github.com/Knikt/APL-Launcher/releases/latest) and open it. If Windows says the app is not signed: *More info › Run anyway*.
2. **Choose your TV** and tell whether it is rooted. The Setup shows what you get, and what is missing without root.

   <img src="docs/images/setup/en-your-tv.png" alt="APL Launcher Setup: choose your TV" width="720">

3. **Type the TV's IP address** (or click **Find**) and click **Install**. Add your free TMDB, YouTube and Fanart.tv keys if you have them: they bring the live Top Shelf and the artwork.

   <img src="docs/images/setup/en-install-lg.png" alt="APL Launcher Setup: install on an LG TV" width="720">

That's all: the Setup builds APL Launcher with your settings, installs it, turns on what root allows, and opens it on the TV. On Samsung it also needs Tizen Studio and your Samsung certificate on the PC (the Setup checks them and tells you what to do). The full guide, step by step: [docs/installation.md](docs/installation.md).

### Without a PC

- **Samsung**: [Apps2Samsung](https://github.com/Apps2Samsung/Apps2Samsung) installs `apl-launcher-full-vX.Y.Z.wgt` from your phone.
- **LG, rooted**: in the **Homebrew Channel**, *Settings › Add repository*, type
  `https://raw.githubusercontent.com/Knikt/APL-Launcher/main/webos/repo/apps.json`, then install **APL Launcher** from the list. For the root features, run once from an SSH app on your phone (user `root`, password `alpine`):
  `wget -qO- https://raw.githubusercontent.com/Knikt/APL-Launcher/main/webos/root/install.sh | sh`
- **Android TV**: with the **Downloader** app on the TV, open
  `https://github.com/Knikt/APL-Launcher/releases/latest/download/apl-launcher-android.apk`
  and install it. APL Launcher then asks for what Home needs.

## Debloat built in

| TV | What it removes | Undo |
|---|---|---|
| **Samsung** (with Vetro) | The Setup's **Optimize** page: unused apps, Samsung's ads, viewing tracking and Bixby, ad domains blocked. It shows the full list and waits for your OK. [Details](docs/optimizer.md) | **Restore** page |
| **LG** (rooted) | LG's ads, LivePlus viewing tracking, telemetry, suggestions and a dozen unneeded services (about 390 MB of memory freed on an OLED G5). The TV's software updates are blocked, so they can't remove the root. AirPlay, Chromecast, ThinQ and the streaming apps keep working. [Details](webos/root/README.md) | One command, or the Setup's switch |
| **Android TV** | Nothing is removed: the TV's own launcher just stays behind APL Launcher. | — |

Nothing is deleted from the TV's system: it is all switched off, and comes back.

## Updates

- **Samsung**: APL Launcher updates itself from this repository, and goes back to the installed version if a new one fails to start.
- **LG**: the Homebrew Channel shows the new versions (when installed from its repository), or run the Setup again.
- **Android TV**: run the Setup again, or install the new `.apk`.

What changed: [CHANGELOG.md](CHANGELOG.md).

## APL TV

**[APL TV](https://github.com/Knikt/APL-TV)** is the IPTV player that goes with APL Launcher: Live TV, a TV guide, movies and series from your own M3U playlists, Xtream accounts and Jellyfin, Emby or Plex servers, in the same style. On Samsung, LG and Android TV. Its *Continue Watching* shows on APL Launcher's Home Screen. It comes with no channels and no provider: use it with content you have the right to watch.

## Help

- [Installation, step by step](docs/installation.md) · [Compatibility](docs/compatibility.md) · [User guide](docs/user-guide.md) · [Troubleshooting](docs/troubleshooting.md)
- Samsung: [Optimize](docs/optimizer.md) · LG: [root scripts](webos/root/README.md) · Android: [Home button and more](docs/android.md)
- For developers: [Development](docs/development.md) · [Visuals and APIs](docs/visuals-and-apis.md) · [Setup](installer/README.md)

**Is it safe?** Nothing in the TV's system is deleted. On Samsung, **Restore** undoes everything; on LG, `sh /media/developer/apl-debloat/uninstall.sh`; on Android, uninstall the app.

**Can it play Android apps on Samsung or LG?** No: Tizen and webOS can't run them, root or not.

Issues and pull requests are welcome: [CONTRIBUTING.md](CONTRIBUTING.md). Thanks to skjnldsv, exzos28, praneetchoudhary0-droid and BPHvZ for theirs.

## Support APL Launcher

APL Launcher is free, open source and made by one person in my spare time. If you like it, you can buy me a coffee on [Ko-fi](https://ko-fi.com/knikt). Never required: everything stays free.

[![Support me on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/knikt)

## License, trademarks and credits

The source code is released under the [MIT License](LICENSE).

APL Launcher is an independent project. It is not affiliated with, endorsed by or sponsored by Apple, Samsung, LG, Google or Amazon. Apple, Apple TV, tvOS, AirPlay, AirPods, Siri and Memoji are trademarks of Apple Inc. Samsung, Tizen and Smart Hub, LG and webOS, Android, Google TV and Chromecast, Amazon and Alexa are trademarks of their owners. App, service and device logos belong to their owners and are shown only to identify them.

APL Launcher is created by Knikt (Gero Corp.).

Vetro by andrearicci93 · TizenBrew and TizenTube by reisxd · the Homebrew Channel by webosbrew · LG debloat based on [CleanYourTV](https://github.com/lasharor/cleanyourtv-WebOS) and [Own Your Glass](https://github.com/JulioFerrero/own-your-glass) (MIT), blocklist from [lg-tv-blocklist](https://github.com/furkan-bayrak/lg-tv-blocklist) (CC BY 4.0) · icons from [dashboard-icons](https://github.com/homarr-labs/dashboard-icons), [Simple Icons](https://simpleicons.org), [selfh.st/icons](https://github.com/selfhst/icons) and [tv-logos](https://github.com/tv-logo/tv-logos) · aerial video list from [OrangeJedi/Aerial](https://github.com/OrangeJedi/Aerial) · film and series data from [TMDB](https://www.themoviedb.org) (*this product uses the TMDB API but is not endorsed or certified by TMDB*) and ratings from Stremio's public Cinemeta service · what each service streams now, for *Dynamic Backdrops*, from [JustWatch](https://www.justwatch.com) · artwork from [Fanart.tv](https://fanart.tv) · sports data from [TheSportsDB](https://www.thesportsdb.com) · 3D avatars from [Fluent Emoji](https://github.com/microsoft/fluentui-emoji) (MIT) · Memoji downloaded at install time from [wimell/tapback-memojis](https://github.com/wimell/tapback-memojis) (not included in this repository).
