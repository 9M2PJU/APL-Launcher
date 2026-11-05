# Changelog

## 1.0.16
- Settings are reorganised (#22, thanks skjnldsv): **Appearance** gathers the theme, the Top Shelf, visual effects and background motion; **Home Screen** the apps shown, their order and the way back to the launcher; **Remote and Devices** the play/pause button, Alexa and Bluetooth.
- One **Top Shelf** menu, in the order the launcher tries things: What You're Watching, What's Popular, then **Otherwise** for apps without artwork. Sheets of Light are now the default there for anyone who never picked a style; Black replaces the old App Backgrounds switch (turned into Black when it was off). Tried on the M8: the new menus in French, Otherwise on Sheets of Light.
- *Start on APL Launcher* moves to **General & Privacy**, above Close Apps at Start-Up, and Advanced Features goes away (#23, thanks skjnldsv). Autorun Smart Hub leaves the launcher's Settings: Start on APL Launcher turns it on, APL Overlay switches it around standby, and turning it off alone kept the launcher from showing at power-on. It is still in Samsung's own settings.
- *Settings › Support › About This Screen › Link to Vetro* says how the launcher reaches Vetro: through its bridge, with the time it took, or the bridge's error. Without the bridge, orders still go out but nothing can be read back, so the screen's settings say Unavailable: on a Tizen 6.5 Frame (#21) the picture values came back in under a second from the PC, never in the launcher. On the M8: "Through the bridge (107 ms)".
- Install guide: on Tizen 6.x with Apps2Samsung, install the launcher alone and map Home to it.
- Samsung TV Plus is no longer listed through its deep linker on Tizen 6.5: launched alone it doesn't open TV Plus (reported in #21, where Prime Video and Apple TV now work). The Samsung TV Plus app from the store is listed as before.

## 1.0.15
- APL Launcher Setup installs the launcher on its own on Tizen 6.x screens. APL Overlay, the bridge and the start-up service of the full package are .NET apps for Tizen 7 and newer: on The Frame 2022 (Tizen 6.5) they don't run, and since the launcher reaches Vetro through that bridge, the settings stayed empty and Bluetooth devices didn't connect (#21). The launcher's own package has a web bridge instead. Setup reads the screen's Tizen version through Vetro; Tizen 7 and newer get the full package as before. Tried from this PC: both packages built and signed, the right one chosen for the M8 (Tizen 9); not tried on a Tizen 6 screen.
- Prime Video and Apple TV show in the launcher when Samsung preinstalled them: their Samsung ids were filtered out with the screen's own services (Apple TV is com.samsung.tv.aria-video), and on Tizen 6.5 Samsung TV Plus too, through its deep linker (#21). Seen on the M8: Apple TV in the grid.
- The Top Shelf empties what it kept when the screen's country changes: trending videos fetched for France stayed up to 6 hours after the update to 1.0.14.

## 1.0.14
- *Start on APL Launcher* no longer brings Samsung's home at every wake. It needs Autorun Smart Hub on for the launcher to show a second after a full start, but with it on the screen also opened Samsung's home after each standby. APL Overlay now turns Autorun off while the screen sleeps and back on once it is awake: a wake goes back to the app you were watching, and a full start still shows the launcher at once. Only on screens that really come back from standby, learned at the first wake (The Frame's deep standby is a full start and is left as it was). Tried on the M8: two wakes with Autorun off by hand (back on the launcher, back on AquaPlay) and two with APL Overlay switching it (back on AquaPlay, no Samsung home; Autorun on again 16 s after the wake); a full start, the launcher in front 1.5 s after power-on.
- Home pressed in the minute after a wake, while Vetro restarts, still reaches the screen, which opens its home: APL Overlay now replaces it at once (half a second on screen on the M8) instead of after a check that the busy screen delayed to 3 s.
- APL Boot also puts the launcher in front of Samsung TV Plus after a full start: with Autorun off (after a power cut during standby) the M8 showed TV Plus until then.
- The Top Shelf follows the screen's country: trending videos, top albums, game prices and what each streaming service offers were those of France on every screen. The country is the one Samsung's store uses on the screen, read through Vetro once and kept (#21, reported from India).

## 1.0.13
- Top Shelf: your own content first. Apps that fill Samsung's home with rows (Plex's Continue Watching, Spotify's mixes) now fill the Top Shelf the same way, and ▲ then OK opens the item itself (#14, thanks skjnldsv). *Settings › Apps › Top Shelf Content* sets what comes first.
- Sheets of Light, a new *Top Shelf Background* choice: soft sheets of light in the app's colors (#13, thanks skjnldsv). Their colors now follow the logo: as bright as the logo's color (yellows no longer turn olive, orange no longer brown or red), and a logo with two colors (Samsung TV Plus, Pluto TV) gets both.
- Liquid Glass for the dock, the Control Center and the status pill, with no live blur (#18, thanks skjnldsv). Over live Top Shelf content the dock is clear glass: the frosted copy showed big pixels there on the M8.
- Home always brings the Home Screen back to rest, from deep in Settings or full screen, and the screen saver no longer greets you on the way back (#17, thanks skjnldsv).
- *Settings › ⏯ in Apps*: Control Center (as before) or Play and Pause (#16, thanks skjnldsv).
- Sources fits five inputs or more on screen (#19), and with a single profile the status pill shows the time alone (#15, thanks skjnldsv).
- APL Overlay opens the launcher through the screen itself first, Vetro only if that fails: after a wake, Samsung's home showed for several seconds while Vetro restarted, now for under a second.

## 1.0.12
- Sources: *Add to Home Screen* (hold OK on an input) was the last of 16 cards, below the bottom of the screen. It now comes first, and the cards are smaller so all of them fit. Reported on Reddit.
- *Settings › Apps › Top Shelf Background* (was *Background for Other Apps*): a new choice, *APL Wallpaper for Every App*, puts the same wallpaper behind every app, including those with artwork of their own. Continue Watching and the Dynamic Top Shelf then sit on it too. The other choices only change the apps without artwork, and now say so. The APL wallpaper itself is now a plain dark gradient.

## 1.0.11
- APL Launcher Setup installs the full package, like Apps2Samsung (#9, thanks exzos28): it downloads the release's package, adds your personal files and signs it with your certificate, so the PC no longer needs the C# compiler or .NET 6. A screen set up before moves by itself: the buttons go to the new APL Overlay first, then the old separate package is removed. Tried on the M8: keys, websites, settings and *Start on APL Launcher* kept.
- APL Overlay no longer closes Netflix and YouTube's engine over and over: the screen preloads them and starts them again a minute after they are closed, about 15 times an hour. An app that comes back by itself twice in a row is now left alone, until it is used.

## 1.0.10
- APL Overlay: Home pressed in the first minute after power-on, before Vetro takes the key, opened Samsung's home and could leave it there. APL Overlay now brings the launcher back without Vetro, also when Samsung's home was already in front when it started. Tried on the M8: five presses before Vetro was up, the launcher back each time in under half a second.
- Alexa on the microphone button didn't work after a full start once the launcher showed early: APL Overlay asked Vetro once, before it was up. It now tries every 15 s until Vetro runs it (on the M8: about 70 s after power-on).
- Both are in APL Overlay: install again with APL Launcher Setup, or the full package with Apps2Samsung.

## 1.0.9
- Tizen 6.x: the Home Screen stayed black with no apps. The stylesheet used the CSS `inset` property, which the browser engine of Tizen 6.5 and older doesn't know, so every app icon was 0×0. It now uses `top`, `right`, `bottom` and `left`; newer screens look the same. Found by a Reddit user.
- *Settings › Advanced Features › Start Screen Option › Start on APL Launcher*: the screen makes the launcher its home app and shows it about a second after power-on, instead of about a minute (#12, thanks skjnldsv). Measured on the M8: 1.3 s instead of 64 s. Turning it off puts the original values back; Restore does too, and the Optimizer leaves Autorun Smart Hub on while the launcher is the home app.
- The start-up cleanup and the remote's key-repeat fix gave up silently when Vetro wasn't answering yet, which happens after a full start: they now try again every 15 s for 2 minutes. On the M8 the key repeat had stayed at 350 ms.
- The Frame: APL Overlay reaches Vetro through the screen's LAN address like the bridge, the launcher is back right after a wake, and the full package's bridge reads the screen state straight from the platform (#11, thanks skjnldsv).
- APL Overlay never closes The Frame's Art Mode any more.

## 1.0.8
- A start-up that doesn't finish no longer leaves a black screen: after 20 s on screen without the Home Screen, the launcher shows the errors it met, its version and the screen's model, to send in a GitHub issue.

## 1.0.7
- APL Launcher Setup: a certificate made with Tizen Studio's Certificate Manager is accepted again. When no Apps2Samsung certificate was valid, Setup stopped with "the TizenTube one has expired" before looking at the Tizen Studio profiles, whatever the PC had. It now uses the profile, says when no certificate was found at all, and checks the signed package: an expired certificate is reported with its date instead of being refused by the screen.

## 1.0.6
- Releases also carry `apl-launcher-full-vX.Y.Z.wgt`: the launcher and APL Overlay in one package for Apps2Samsung, with a .NET bridge and a start-up service (#8, thanks exzos28). APL Launcher Setup is unchanged.
- Without APL Overlay, the launcher can give the Home button back to itself again: its bridge now relays the Remapper rules (and the microphone switch), which it refused before. This is in the package: install it again (APL Launcher Setup or the .wgt).

## 1.0.5
- Sources shows every HDMI input of the screen. Tizen 9 no longer lists the inputs to apps, so every screen showed the Smart Monitor's (one HDMI and USB-C): they are now read from the screen through Vetro, with the device Samsung recognized on each input.
- An input can go on the Home Screen like an app: in Sources, hold OK on it › Add to Home Screen. Its tile shows the chosen device, and Edit Home Screen moves it into the dock.

## 1.0.4
- The Frame and other screens: the bridge reaches Vetro through the screen's own network address when the local one is blocked, brings the launcher back about 1 second after a wake-up instead of 15, and also after leaving Art Mode (#5, #6, thanks skjnldsv). The bridge is in the package: install it again (APL Launcher Setup or the .wgt).
- Without APL Overlay, the launcher still gives Home back to itself and the other buttons to Samsung, and now says so (#2, thanks exzos28).
- A Smart Hub tile to reach Samsung's home if you need it, hidden at first: Settings › Apps › Show on Home Screen (#7, thanks exzos28).
- Settings › Apps › Background for Other Apps: for apps with no artwork of their own, their logo on their colors, their colors only, or one APL wallpaper for all of them.
- Smoother Top Shelf on apps with Continue Watching cards (Stremio, AquaPlay, Hot Player): coming up from the grid, the cards wait for the end of the animation, and the app's data is read afterwards and less often.
- The screen's resolution shows the panel's (4K) and the interface's (1920 × 1080).

## 1.0.3
- Apps2Samsung's TV-Store now shows in the app grid: it was taken for a second Samsung App Store and hidden (#4, thanks exzos28).

## 1.0.2
- Back from sleep on the launcher: screens that wake up on the TV input or Samsung's home (The Frame 2025, which has no *Autorun Last App* setting) now open APL Launcher again when it was in front at power-off. An HDMI input, for example a console that switched the screen on, is left alone. This is in the launcher package: install it again (APL Launcher Setup or the .wgt for Apps2Samsung).
- The Frame: Art Mode's app is no longer closed at start-up or after a minute in the background.
- Releases also carry the launcher alone as a .wgt for Apps2Samsung, for installing without a Windows PC (#1, thanks exzos28).

## 1.0.1
- Smoother videos: the Samsung apps that open by themselves in the background (Samsung TV Plus, Prime Video, Samsung's video player, My Contents…) now close after 1 minute instead of 10 when you never opened them. They reopen at every wake-up and held about 250 MB that the app you watch needs. Apps you use keep the 10 minutes. Comes with APL Overlay: run APL Launcher Setup again to get it.

## 1.0.0
First public version.
- tvOS-style home screen: full-screen Top Shelf, glass dock, 6-column grid, Control Center (even over a video), Apple TV-style Settings covering every Samsung settings page, and Sources with a device picker.
- Live Top Shelf for streaming, music, game and sports apps, with film and series ratings; official logos for 50+ apps and automatic artwork for the others.
- *Continue watching* for AquaPlay, Hot Player and Stremio: title artwork from TMDB, channel logos, episode, time left and progress bar.
- Remote buttons: Home twice for the app switcher, hold Home or press ⏯ for the Control Center; Samsung TV Plus opens AquaPlay and Prime Video opens TizenTube when they are installed.
- Alexa on the microphone button, with a Siri-like glow while you speak.
- Household profiles with Memoji avatars; aerial screen saver with Apple's videos in 4K.
- Apps left in the background for 10 minutes close by themselves, never the one in front.
- Automatic updates from GitHub with automatic rollback; optimizer, and Restore to undo everything.
- **APL Launcher Setup**, a Windows app with Install, Optimize, Restore and Tools pages, instead of .bat files. Nothing to install to run it.
- English and French: the launcher follows the screen's language (or Settings › General & Privacy › Language), APL Overlay follows the launcher, and APL Launcher Setup and its scripts follow Windows.
- Needs Vetro on the screen; the installer puts only APL Launcher and APL Overlay on it.
