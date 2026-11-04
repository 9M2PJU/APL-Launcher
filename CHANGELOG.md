# Changelog

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
