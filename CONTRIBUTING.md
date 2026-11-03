# Contributing

Thanks for helping! Bug reports, screen compatibility reports and pull requests are welcome.

## Reporting a bug

Use the *Bug report* template. Include the screen model, the Tizen version, the launcher version (*Settings › Support › About This Screen*) and a photo of any message.

## Making changes

- Read [docs/development.md](docs/development.md) first. The launcher is plain **ES5** JavaScript, with no framework and no build step, because Tizen's web runtime is old.
- Try your change in the PC preview: APL Launcher Setup › Tools › Preview on this PC, or open `launcher/index.html` in Chrome. Use the arrows, Enter (hold it for the app menu), Esc and C.
- Run the checks before opening a pull request:

  ```bash
  python3 tools/check.py
  ```

- For a release, raise the version in `launcher/config.xml` and run `python3 tools/make-build.py --notes "…"`.
- Never commit `launcher/js/keys.local.js`, certificates (`*.p12`) or passwords. `.gitignore` already excludes them.

## Translations

The on-screen interface is in French. An English translation (strings in `settings-tree.js`, `controlcenter.js`, `sources.js`, `appmenu.js`, `launcher.js` and `updater.js`) would be a great contribution.
