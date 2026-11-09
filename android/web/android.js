/* ============================================================
   android.js — APL Launcher on Android TV and Google TV.

   The Android app (android/, MainActivity.kt) shows the launcher's page
   in a WebView and gives it window.APLAndroid (Bridge.kt). This file,
   added to the page by tools/make-android.py (never on Samsung), turns
   the launcher's Samsung calls into Android ones:
     the apps (names, icons, TV banners), opening them, deleting them;
     sound, network, device, storage, picture size;
     the remote (Samsung's key codes, sent by Android), Home, typing;
     sleep and the Sleep Timer (accessibility service, KeyService.kt);
     Settings and the Control Center: what Android can't do without root
     (picture, Game Mode, Samsung's services…) is left out, Android's own
     screens are offered instead.
   It runs after the launcher's modules and before launcher.js starts.
   The Control Center over other apps (cc.html) uses it too ("overlay").
   ES5, like the rest of the launcher.
   ============================================================ */
(function (global) {
    "use strict";

    var B = global.APLAndroid;
    if (!B) return;
    var APL = global.APL, sys = APL.sys;
    var OVERLAY = !!(B.overlay && B.overlay());

    function json(s, d) { try { var v = JSON.parse(s); return v === null || v === undefined ? d : v; } catch (e) { return d; } }
    var info = json(B.info(), {});
    function fresh() { info = json(B.info(), info); return info; }

    /* ---------- French for what only Android says ---------- */
    var FR = global.APL_LANG_FR;
    if (FR) {
        var add = {
            "This needs root on Android.": "Il faut le root sur Android.",
            "Turn on APL Launcher in Android's Accessibility settings first (Settings › Android TV › Remote Buttons).":
                "Activez d'abord APL Launcher dans l'accessibilité d'Android (Réglages › Android TV › Touches de la télécommande).",
            "Sources are not available on Android yet.": "Les sources ne sont pas encore disponibles sur Android.",
            "Not used on Android": "Pas utilisé sur Android",
            "Android TV": "Android TV",
            "Google TV": "Google TV",
            "Home App": "App d'accueil",
            "Make APL Launcher the Home App": "Faire d'APL Launcher l'app d'accueil",
            "The app Home on the remote opens, and the one the device starts on.": "L'app qu'ouvre le bouton Accueil de la télécommande, et celle sur laquelle l'appareil démarre.",
            "Android asks which app to use: choose APL Launcher. If nothing opens, docs/android.md has the adb command.":
                "Android demande quelle app utiliser : choisissez APL Launcher. Si rien ne s'ouvre, docs/android.md donne la commande adb.",
            "APL Launcher is already the Home app.": "APL Launcher est déjà l'app d'accueil.",
            "This device has no setting for it: see docs/android.md (adb).": "Cet appareil n'a pas ce réglage : voir docs/android.md (adb).",
            "Remote Buttons": "Touches de la télécommande",
            "Turn On the Remote Buttons": "Activer les touches de la télécommande",
            "In Accessibility, choose APL Launcher and turn it on. Then: hold Home for the Control Center over any app, ⏯ opens it too, the Sleep Timer and Sleep work, and on Google TV Home opens APL Launcher.":
                "Dans Accessibilité, choisissez APL Launcher et activez-le. Ensuite : maintenez Accueil pour le Centre de contrôle sur n'importe quelle app, ⏯ l'ouvre aussi, la minuterie de mise en veille et la veille marchent, et sur Google TV le bouton Accueil ouvre APL Launcher.",
            "Home Button": "Bouton Accueil",
            "Android TV and Google TV keep their own launcher on the Home button, even once APL Launcher is chosen. APL Launcher: the remote buttons (Accessibility) take it and open APL Launcher.":
                "Android TV et Google TV gardent leur propre launcher sur le bouton Accueil, même quand APL Launcher est choisi. APL Launcher : les touches de la télécommande (Accessibilité) le prennent et ouvrent APL Launcher.",
            "Android chose APL Launcher, but this device keeps its own launcher on Home: turn on the remote buttons below.":
                "Android a choisi APL Launcher, mais cet appareil garde son propre launcher sur Accueil : activez les touches de la télécommande, juste en dessous.",
            "Over Apps": "Sur les apps",
            "Accessibility": "Accessibilité",
            "Allowed": "Autorisé",
            "Not set up": "Pas configuré",
            "How the Control Center opens over an app: through the remote buttons (Accessibility), or the “draw over other apps” permission given with adb.":
                "Comment le Centre de contrôle s'ouvre par-dessus une app : par les touches de la télécommande (Accessibilité), ou l'autorisation « superposition » donnée avec adb.",
            "Root": "Root",
            "Not needed": "Pas nécessaire",
            "Found, not used": "Présent, pas utilisé",
            "APL Launcher works without root. With root, a later version could close every app and reach deeper settings.":
                "APL Launcher marche sans root. Avec le root, une version future pourrait fermer toutes les apps et atteindre des réglages plus profonds.",
            "Setup guide: github.com/Knikt/APL-Launcher-for-TizenOS, docs/android.md.": "Guide d'installation : github.com/Knikt/APL-Launcher-for-TizenOS, docs/android.md.",
            "Home app, remote buttons, the Control Center over apps.": "App d'accueil, touches de la télécommande, Centre de contrôle sur les apps.",
            "Android Settings": "Réglages Android",
            "Android's own settings: Wi-Fi, picture, sound, date, language, accounts, apps, reset.": "Les réglages d'Android : Wi-Fi, image, son, date, langue, comptes, apps, réinitialisation.",
            "Picture Settings": "Réglages de l'image",
            "The device's picture settings, in Android.": "Les réglages de l'image de l'appareil, dans Android.",
            "Sound Settings": "Réglages du son",
            "Sound output, surround and the device's sound settings, in Android.": "Sortie audio, son surround et réglages du son de l'appareil, dans Android.",
            "Remotes and Accessories": "Télécommandes et accessoires",
            "Bluetooth headphones, speakers, game controllers and remotes, in Android.": "Casques, enceintes, manettes et télécommandes Bluetooth, dans Android.",
            "Network Settings": "Réglages du réseau",
            "Wi-Fi and Ethernet, in Android.": "Wi-Fi et Ethernet, dans Android.",
            "Manage Apps": "Gérer les apps",
            "Storage, permissions, force stop, in Android.": "Stockage, autorisations, forcer l'arrêt, dans Android.",
            "An app left in the background for more than 10 minutes is closed by Android (through the remote buttons, Accessibility). The app in front, music playing in the background and the launcher are never closed.":
                "Une app restée plus de 10 minutes en arrière-plan est fermée par Android (par les touches de la télécommande, Accessibilité). L'app au premier plan, la musique en arrière-plan et le launcher ne sont jamais fermés.",
            "Asks Android to close the apps left in the background. They reopen normally, but start over from the beginning.":
                "Demande à Android de fermer les apps laissées en arrière-plan. Elles se rouvrent normalement, mais repartent du début.",
            "When an app closes, or the device starts, APL Launcher instead of the device's own home screen. Needs the remote buttons (Accessibility).":
                "Quand une app se ferme, ou au démarrage de l'appareil, APL Launcher plutôt que l'écran d'accueil de l'appareil. Il faut les touches de la télécommande (Accessibilité).",
            "About This Device": "À propos de cet appareil",
            "Android Version": "Version d'Android",
            "Updates come with a new APK, from the GitHub releases.": "Les mises à jour arrivent avec un nouvel APK, depuis les versions sur GitHub.",
            "{mb} MB freed": "{mb} Mo libérés",
            "Nothing to free: the apps in the background were already closed.": "Rien à libérer : les apps en arrière-plan étaient déjà fermées.",
            "Opens the device's own home screen ({name}). Press Home to come back.": "Ouvre l'écran d'accueil de l'appareil ({name}). Appuyez sur Accueil pour revenir.",
            "Home Screen of the device": "Accueil de l'appareil",
            "Ethernet": "Ethernet",
            "Other": "Autre"
        };
        for (var k in add) if (Object.prototype.hasOwnProperty.call(add, k) && !Object.prototype.hasOwnProperty.call(FR, k)) FR[k] = add[k];
    }
    var T = APL.t;
    function needsService() { return T("Turn on APL Launcher in Android's Accessibility settings first (Settings › Android TV › Remote Buttons)."); }
    function fail(msg) { return Promise.reject(new Error(msg)); }
    function needsRoot() { return fail(T("This needs root on Android.")); }
    function toast(m, quiet) { if (APL.toast) APL.toast(m, quiet); }

    /* ---------- the remote: Android sends each key here (Samsung's codes) ---------- */
    global.__aplKey = function (type, code, repeat) {
        var e;
        try { e = new KeyboardEvent(type, { bubbles: true, cancelable: true, repeat: !!repeat }); }
        catch (x) { e = document.createEvent("Event"); e.initEvent(type, true, true); }
        try {
            Object.defineProperty(e, "keyCode", { get: function () { return code; } });
            Object.defineProperty(e, "which", { get: function () { return code; } });
            if (!e.repeat && repeat) Object.defineProperty(e, "repeat", { get: function () { return true; } });
        } catch (x) {}
        (document.activeElement || document.body).dispatchEvent(e);
    };

    /* ---------- what Android tells the page ---------- */
    var handlers = {};
    function listen(name, fn) { (handlers[name] = handlers[name] || []).push(fn); }
    global.__aplEvent = function (name, data) {
        (handlers[name] || []).forEach(function (fn) {
            try { fn(data); } catch (e) { try { B.log("event " + name + ": " + e.message); } catch (x) {} }
        });
    };

    /* Typing (folder and profile names): the keys go to Android's keyboard meanwhile */
    function isField(n) { return !!n && (n.tagName === "INPUT" || n.tagName === "TEXTAREA" || n.isContentEditable); }
    document.addEventListener("focusin", function (e) { if (isField(e.target)) B.editing(true); }, true);
    document.addEventListener("focusout", function (e) { if (isField(e.target)) B.editing(false); }, true);

    /* ---------- APL.sys: the device ---------- */
    sys.PREVIEW = false;
    sys.platform = "android";
    if (sys.setDefault) {
        sys.setDefault("autoUpdate", false);      // updates come with the APK: boot.js runs the installed version
        sys.setDefault("bootCleanup", false);     // Samsung's own apps at power-on
        sys.setDefault("alexaBridge", false);
        sys.setDefault("catchHome", true);       // Home opens APL Launcher (KeyService.kt), where the device keeps its own launcher
    }

    sys.audio = {
        getVolume: function () { return json(B.volume(), {}).level || 0; },
        setVolume: function (v) { return B.setVolume(Math.round(v)); },
        isMute: function () { return !!json(B.volume(), {}).muted; },
        setMute: function (m) { B.setMute(!!m); },
        outputMode: function () { return T("Automatic"); }
    };

    var net = null, netAt = 0;
    sys.network = function () {
        // Settings reads it once per row: asked of Android at most every 3 s
        if (net && Date.now() - netAt < 3000) return net;
        var n = json(B.network(), {});
        var type = n.type === "Wi-Fi" || n.type === "Ethernet" ? n.type : n.type === "Mobile" ? "Mobile" : n.connected ? T("Other") : T("Disconnected");
        net = { connected: !!n.connected, type: type, ssid: n.ssid || "—", signal: n.signal || 0, ip: n.ip || "—",
                mac: n.mac || "—", gateway: n.gateway || "—", dns: n.dns || "—" };
        netAt = Date.now();
        return net;
    };

    function deviceName() {
        var m = String(info.manufacturer || ""), model = String(info.model || "");
        if (!m || model.toLowerCase().indexOf(m.toLowerCase()) === 0) return model || "Android TV";
        return m.charAt(0).toUpperCase() + m.slice(1) + " " + model;
    }
    sys.product = function () {
        return { model: info.model || "—", name: deviceName(), firmware: info.build || "—", tizen: "Android " + (info.release || "?") };
    };
    function screen() { return json(B.display(), {}); }
    sys.display = function () {
        var d = screen();
        return Promise.resolve(d.panel && d.ui && d.panel !== d.ui ? T("{panel} (interface {ui})", { panel: d.panel, ui: d.ui }) : (d.panel || d.ui || "—"));
    };
    function gb(bytes) { var n = (bytes / 1e9).toFixed(1); return T("{n} GB", { n: APL.lang === "fr" ? n.replace(".", ",") : n }); }
    sys.storage = function () {
        var s = json(B.storage(), {});
        return Promise.resolve(s.total ? T("{free} free of {total}", { free: gb(s.free), total: gb(s.total) }) : "—");
    };
    sys.hdr = function () { var d = screen(); return d.hdr === undefined ? "—" : d.hdr ? T("Supported") : T("Not supported"); };

    /* The remote's buttons the launcher presses itself: through the accessibility service */
    var GLOBAL = { KEY_HOME: "home", KEY_EXIT: "home", KEY_RETURN: "back", KEY_MENU: "recents" };
    sys.remoteKey = function (key) {
        if (key === "KEY_POWER") return sys.sleepNow();
        if (GLOBAL[key] && B.globalAction(GLOBAL[key])) return Promise.resolve();
        return fail(needsService());
    };
    sys.sleepNow = function () { return B.sleep() ? Promise.resolve() : fail(needsService()); };
    sys.openSamsungSettings = function (path) { B.openSettings("settings"); return Promise.resolve(path); };
    sys.switchSource = function () { return fail(T("Sources are not available on Android yet.")); };
    sys.registerKeys = function () {};

    /* Vetro and the screen's root commands: none on Android. Deleting an app (launcher.js asks Vetro) is
       Android's own "Uninstall?" instead. */
    sys.vetroAlive = function () { return Promise.resolve(false); };
    sys.vetroLink = function () { return Promise.resolve(T("Not used on Android")); };
    sys.noVetro = function () { return false; };
    sys.vetroGet = function () { return needsRoot(); };
    sys.vetroPost = function (path, body) {
        if (path === "/components/action" && body && body.action === "uninstall") return Promise.resolve({ ok: B.uninstall(String(body.appId || "")) });
        return needsRoot();
    };
    /* The App Switcher closes an app with Samsung's app_launcher -k: Android closes it, as it is in the background */
    sys.exec = function (cmd) {
        var m = /^app_launcher -k ([A-Za-z0-9._]+)[^;]*; true$/.exec(String(cmd || ""));
        if (m && info.canClose) { B.freeMemory(JSON.stringify([m[1]])); return Promise.resolve({ ok: true, output: "" }); }
        return needsRoot();
    };
    sys.runningApps = function () { return Promise.resolve(json(B.running(), [])); };

    /* Picture, Game Mode and the screen's other settings: Samsung's, through Vetro */
    sys.game = {
        get: function () { return Promise.resolve({ available: false, on: false }); },
        set: function () { return needsRoot(); },
        applyOnInput: function () { return Promise.resolve(); }
    };
    var nativeInfo = sys.native && sys.native.info;
    sys.native = {
        get: function () { return fail(T("Unavailable")); },
        set: function () { return fail(T("Unavailable")); },
        reset: function () { return needsRoot(); },
        info: function (id) { return nativeInfo ? nativeInfo(id) : null; }
    };
    sys.tvSettings = function () { return fail(T("Unavailable")); };
    sys.tvItem = function () { return Promise.resolve(null); };
    sys.tvValue = function () { return Promise.resolve(null); };
    sys.tvAdjust = function () { return fail(T("Unavailable")); };
    sys.setPictureMode = function () { return fail(T("Unavailable")); };

    /* Sleep Timer: Android counts (SleepReceiver.kt), the launcher and the Control Center show it */
    sys.armSleepTimer = function (minutes) {
        var at = minutes ? Date.now() + minutes * 60000 : 0;
        sys.setPref("sleepTimer", minutes);
        sys.setPref("sleepAt", at);
        B.setSleepAt(at);
        if (minutes && !fresh().accessibility) toast(needsService());
    };
    sys.readSleepTimer = function () {
        var at = +B.sleepAt() || 0;
        if (at < Date.now()) at = 0;
        if (sys.pref("sleepAt") !== at) sys.setPref("sleepAt", at);
        if (!at && sys.pref("sleepTimer")) sys.setPref("sleepTimer", 0);
        return Promise.resolve();
    };

    /* Settings the native side reads too (KeyService.kt) */
    function syncNative() {
        B.setNativePref("pauseKey", String(sys.pref("pauseKey") || "controlcenter"));
        B.setNativePref("idleClose", sys.pref("idleClose") ? "1" : "0");
        B.setNativePref("homeReturn", sys.pref("homeReturn") ? "1" : "0");
        B.setNativePref("catchHome", sys.pref("catchHome") ? "1" : "0");
    }
    ["pauseKey", "idleClose", "homeReturn", "catchHome"].forEach(function (k) { sys.onPref(k, syncNative); });
    syncNative();

    /* ---------- the apps ---------- */
    var waiting = [];
    listen("apps", function (list) { var w = waiting; waiting = []; w.forEach(function (fn) { fn(list || []); }); });
    function nativeApps() {
        return new Promise(function (resolve) {
            waiting.push(resolve);
            if (waiting.length === 1) B.requestApps();
        });
    }
    APL.loadApps = function () {
        return nativeApps().then(function (list) {
            var byId = {};
            list.forEach(function (a) { byId[a.id] = a; });
            var apps = APL.appsFromList(list.map(function (a) {
                // Android's Settings app, next to the launcher's own Settings tile
                var name = /^com\.android\.tv\.settings$|^com\.google\.android\.tv\.settings$/.test(a.id) ? T("Android Settings") : a.name;
                return { id: a.id, name: name, icon: a.icon || null };
            }));
            // A TV app's banner is its tile, where the catalogue has no artwork of its own for it
            apps.forEach(function (a) {
                var n = byId[a.id];
                if (n && n.banner && !a.tile) { a.tile = n.banner; a.banner = true; }
            });
            return apps;
        });
    };
    APL.launchApp = function (id) { return B.launch(String(id)) ? Promise.resolve() : fail(T("Can't launch the app")); };
    APL.isInstalled = function (id) { return Promise.resolve(!!B.installed(String(id))); };
    APL.openUrl = function (url) { return B.openUrl(String(url)) ? Promise.resolve() : fail(T("Can't open Internet")); };
    APL.launchDeepLink = function () { return fail(T("Can't launch the app")); };
    APL.launchTizenTube = function () { return fail(T("Can't launch the app")); };

    /* An app installed, updated or removed: launcher.js reloads the list (the way it listens to Tizen's) */
    var pkgListener = null;
    listen("packages", function () { if (pkgListener && pkgListener.oninstalled) pkgListener.oninstalled(); });

    function freeMemory() {
        var ids = ((APL.allApps && APL.allApps()) || []).filter(function (a) { return !a.virtual; }).map(function (a) { return a.id; });
        var r = json(B.freeMemory(JSON.stringify(ids)), {});
        // Android doesn't say which of them were open: what was freed tells
        toast(r.freed > 0 ? T("{mb} MB freed", { mb: r.freed }) : T("Nothing to free: the apps in the background were already closed."), true);
    }

    /* ---------- APL.platform: the few places where launcher.js and controlcenter.js ask ---------- */
    var homeName = String(B.systemHomeName() || "") || T("Home Screen of the device");
    APL.platform = {
        name: "android",
        packages: { setPackageInfoEventListener: function (l) { pkgListener = l; } },
        /* The launcher's own tiles: no Sources yet; Smart Hub becomes the device's own home */
        virtualApps: function (list) {
            return list.filter(function (v) { return v.id !== "apl.sources"; }).map(function (v) {
                if (v.id !== "apl.samsunghome") return v;
                v.name = homeName;
                v.run = function () { if (!B.openSystemHome()) toast(T("Can't launch the app")); };
                return v;
            });
        },
        /* The Control Center's tiles: no Game Mode, AirPlay, Sources or brightness; Audio is Android's Bluetooth */
        ccItems: function (items) {
            return items.filter(function (it) {
                return it.type !== "backlight" && it.icon !== "gamepad" && it.icon !== "airplay" && it.icon !== "source";
            }).map(function (it) {
                if (it.icon === "airplayAudio") {
                    it.sub = function () { return T("Remotes and Accessories"); };
                    it.select = function () { closeCc(); B.openSettings("bluetooth"); };
                }
                return it;
            });
        }
    };
    function closeCc() { if (APL.controlCenter && APL.controlCenter.isOpen()) APL.controlCenter.close(); }

    /* ---------- Settings: Android's version of the tree ---------- */
    function menu(label, icon, children, desc) { return { type: "menu", label: label, icon: icon, children: children, desc: desc }; }
    function infoRow(label, get, desc) { return { type: "info", label: label, get: get, desc: desc }; }
    function action(label, run, desc, icon) { return { type: "action", label: label, run: run, desc: desc, icon: icon }; }
    function opener(label, which, desc, icon) { return action(label, function () { B.openSettings(which); }, desc, icon); }
    function onOff(v) { return v ? T("On") : T("Off"); }

    var SAMSUNG_TYPES = { native: 1, tv: 1, tvmode: 1, game: 1, service: 1, samsung: 1 };
    /* Rows that are Samsung's only (Vetro, the screen's own settings) go; then menus left empty */
    function strip(list) {
        return list.filter(function (it) { return !SAMSUNG_TYPES[it.type] && it.vetro !== true; }).map(function (it) {
            if (it.type === "menu" && it.children) it.children = strip(it.children);
            return it;
        }).filter(function (it) { return it.type !== "menu" || (it.children && it.children.length); });
    }
    function find(list, label) {
        for (var i = 0; i < list.length; i++) if (list[i].label === T(label)) return list[i];
        return null;
    }
    function without(list, labels) {
        var drop = labels.map(function (l) { return T(l); });
        return list.filter(function (it) { return drop.indexOf(it.label) === -1; });
    }
    function after(list, label, items) {
        var at = -1;
        for (var i = 0; i < list.length; i++) if (list[i].label === T(label)) at = i;
        Array.prototype.splice.apply(list, [at === -1 ? list.length : at + 1, 0].concat(items));
    }

    function androidMenu() {
        var rows = [
            infoRow(T("Home App"), function () {
                var i = fresh();
                return i.defaultHome || (i.accessibility && sys.pref("catchHome")) ? "APL Launcher" : (homeName || "—");
            },
                T("The app Home on the remote opens, and the one the device starts on.")),
            action(T("Make APL Launcher the Home App"), function () {
                var r = B.requestHome();
                if (r === "held") toast(T("APL Launcher is already the Home app."), true);
                else if (r === "kept") toast(T("Android chose APL Launcher, but this device keeps its own launcher on Home: turn on the remote buttons below."));
                else if (r === "role" || r === "settings") toast(T("Android asks which app to use: choose APL Launcher. If nothing opens, docs/android.md has the adb command."), true);
                else toast(T("This device has no setting for it: see docs/android.md (adb)."));
            }, T("Android asks which app to use: choose APL Launcher. If nothing opens, docs/android.md has the adb command."), "grid"),
            infoRow(T("Remote Buttons"), function () { return onOff(fresh().accessibility); },
                T("In Accessibility, choose APL Launcher and turn it on. Then: hold Home for the Control Center over any app, ⏯ opens it too, the Sleep Timer and Sleep work, and on Google TV Home opens APL Launcher.")),
            opener(T("Turn On the Remote Buttons"), "accessibility",
                T("In Accessibility, choose APL Launcher and turn it on. Then: hold Home for the Control Center over any app, ⏯ opens it too, the Sleep Timer and Sleep work, and on Google TV Home opens APL Launcher."), "remote")
        ];
        if (!info.defaultHome) {
            rows.push({ type: "choice", label: T("Home Button"), key: "catchHome", options: [[true, "APL Launcher"], [false, homeName]],
                        desc: T("Android TV and Google TV keep their own launcher on the Home button, even once APL Launcher is chosen. APL Launcher: the remote buttons (Accessibility) take it and open APL Launcher.") });
        }
        rows.push(infoRow(T("Over Apps"), function () {
            var i = fresh();
            return i.accessibility ? T("Accessibility") : i.overlayAllowed ? T("Allowed") : T("Not set up");
        }, T("How the Control Center opens over an app: through the remote buttons (Accessibility), or the “draw over other apps” permission given with adb.")));
        rows.push(infoRow(T("Root"), function () { return fresh().root ? T("Found, not used") : T("Not needed"); },
            T("APL Launcher works without root. With root, a later version could close every app and reach deeper settings.")));
        rows.push(infoRow("docs/android.md", function () { return ""; }, T("Setup guide: github.com/Knikt/APL-Launcher-for-TizenOS, docs/android.md.")));
        return menu(info.googleTv ? "Google TV" : "Android TV", "display", rows, T("Home app, remote buttons, the Control Center over apps."));
    }

    var baseTree = APL.settingsTree;
    APL.settingsTree = function () {
        var root = baseTree();
        var top = strip(root.children);
        top = without(top, ["Gaming", "Sources"]);

        var appearance = find(top, "Appearance"), shelf = appearance && find(appearance.children, "Top Shelf");
        if (shelf) shelf.children = without(shelf.children, ["What You're Watching"]);   // Samsung's home rows

        var home = find(top, "Home Screen");
        if (home) {
            var show = find(home.children, "Show on Home Screen");
            if (show && show.children[0] && show.children[0].id === "apl.samsunghome") {
                show.children[0].label = homeName;
                show.children[0].desc = T("Opens the device's own home screen ({name}). Press Home to come back.", { name: homeName });
            }
            var ret = find(home.children, "Return to the Launcher");
            if (ret) {
                if (!info.defaultHome) ret.desc = T("When an app closes, or the device starts, APL Launcher instead of the device's own home screen. Needs the remote buttons (Accessibility).");
                else home.children = without(home.children, ["Return to the Launcher"]);     // Android does it: Home really is APL Launcher
            }
        }

        var remote = find(top, "Remote and Devices");
        if (remote) {
            remote.children = without(remote.children, ["Alexa (Echo Dot)", "Bluetooth"]);
            remote.children.push(opener(T("Turn On the Remote Buttons"), "accessibility",
                T("In Accessibility, choose APL Launcher and turn it on. Then: hold Home for the Control Center over any app, ⏯ opens it too, the Sleep Timer and Sleep work, and on Google TV Home opens APL Launcher."), "remote"));
            remote.children.push(opener(T("Remotes and Accessories"), "bluetooth", T("Bluetooth headphones, speakers, game controllers and remotes, in Android."), "bluetooth"));
        }

        var picture = find(top, "Picture");
        var pictureRows = [infoRow(T("Screen Resolution"), sys.display), infoRow("HDR", function () { return sys.hdr(); }),
                           opener(T("Picture Settings"), "display", T("The device's picture settings, in Android."), "display")];
        if (picture) picture.children = pictureRows;
        else after(top, "Users and Accounts", [menu(T("Picture"), "display", pictureRows, T("Picture Mode, colors and fine-tuning."))]);

        var sound = find(top, "Sound");
        if (sound) {
            sound.children = without(sound.children, ["Digital Output Audio Format", "Sound Output", "Expert Settings"]);
            sound.children.push(opener(T("Sound Settings"), "sound", T("Sound output, surround and the device's sound settings, in Android."), "speaker"));
            sound.children.push(opener(T("Remotes and Accessories"), "bluetooth", T("Bluetooth headphones, speakers, game controllers and remotes, in Android."), "bluetooth"));
        }

        var cnx = find(top, "Connection");
        if (cnx) {
            cnx.children = without(cnx.children, ["AirPlay", "HDMI"]);
            cnx.children.push(opener(T("Network Settings"), "wifi", T("Wi-Fi and Ethernet, in Android."), "wifi"));
        }

        var apps = find(top, "Apps");
        if (apps) {
            // Android 14 and later let an app close nothing but its own processes: that's for root there
            if (info.canClose) {
                apps.children.push({ type: "toggle", label: T("Close Unused Apps"), key: "idleClose",
                    desc: T("An app left in the background for more than 10 minutes is closed by Android (through the remote buttons, Accessibility). The app in front, music playing in the background and the launcher are never closed.") });
                apps.children.push(action(T("Free Up Memory"), freeMemory,
                    T("Asks Android to close the apps left in the background. They reopen normally, but start over from the beginning."), "reset"));
            }
            apps.children.push(opener(T("Manage Apps"), "apps", T("Storage, permissions, force stop, in Android."), "apps"));
        }

        var general = find(top, "General & Privacy");
        if (general) {
            general.children = without(general.children, ["Samsung Services", "Power and Energy Saving"]);
            general.children.push(opener(T("Android Settings"), "settings", T("Android's own settings: Wi-Fi, picture, sound, date, language, accounts, apps, reset."), "gear"));
        }

        var support = find(top, "Support");
        if (support) {
            var upd = find(support.children, "Launcher Update"), ver = upd && find(upd.children, "Version");
            support.children = without(support.children, ["Launcher Update"]);
            if (ver) { ver.desc = T("Updates come with a new APK, from the GitHub releases."); support.children.unshift(ver); }
            var about = find(support.children, "About This Screen");
            if (about) {
                about.label = T("About This Device");
                about.children = without(about.children, ["Link to Vetro"]);
                var tz = find(about.children, "Tizen Version");
                if (tz) tz.label = T("Android Version");
            }
        }

        top.splice(1, 0, androidMenu());
        root.children = top;
        return root;
    };

    /* ---------- the Control Center over the apps (cc.html) ---------- */
    if (OVERLAY) {
        var shownAt = 0;
        var stub = function (a) { return function () { B.launcherAction(a); }; };
        APL.settings = { open: function (section) { B.launcherAction(section ? "settings:" + section : "settings"); },
                         close: function () {}, isOpen: function () { return false; } };
        APL.search = { open: stub("search"), isOpen: function () { return false; } };
        APL.screensaver = { start: stub("screensaver"), isActive: function () { return false; } };
        APL.openSources = function () {};
        if (APL.profilesUI) APL.profilesUI.open = stub("profiles");
        APL.alert = function (o) { toast((o && (o.title || o.message)) || ""); };
        APL.toast = function (msg, quiet) {
            if (quiet && sys.pref("dnd")) return;
            var t = document.getElementById("toast");
            if (!t) return;
            t.textContent = msg;
            t.classList.add("is-visible");
            clearTimeout(t._t);
            t._t = setTimeout(function () { t.classList.remove("is-visible"); }, 2600);
        };
        function theme() {
            var a = sys.pref("appearance"), h = new Date().getHours();
            document.body.classList.toggle("theme-light", a === "light" || (a === "auto" && h >= 7 && h < 20));
            document.body.classList.toggle("perf", !sys.pref("visualEffects"));
        }
        /* The launcher may have changed a setting since this page was loaded: it is read again */
        function snapshot() {
            try {
                var s = global.localStorage, p = JSON.parse(s.getItem("apl.profiles") || "{}");
                return (s.getItem("apl.prefs") || "") + "|" + (s.getItem("apl.profiles") || "") + "|" + (s.getItem("apl.prefs." + p.current) || "");
            } catch (e) { return ""; }
        }
        var seen = snapshot();
        global.__aplCcOpen = function () {
            if (snapshot() !== seen) { global.location.reload(); return; }    // opens again by itself once loaded
            theme();
            shownAt = Date.now();
            if (APL.controlCenter && !APL.controlCenter.isOpen()) APL.controlCenter.open();
        };
        global.__aplCcClosed = function () { closeCc(); };
        // The Control Center closed itself (Back, an item chosen): its window goes too
        new MutationObserver(function () {
            if (!document.body.classList.contains("has-cc") && Date.now() - shownAt > 200 && B.overlayShown()) B.closeOverlay();
        }).observe(document.body, { attributes: true, attributeFilter: ["class"] });
        B.ccReady();
        if (B.overlayShown()) global.__aplCcOpen();
        return;
    }

    /* ---------- the Home Screen ---------- */
    /* Home on the remote: the same event as on Samsung, so launcher.js does what it does there
       (back to the first app, twice for the App Switcher) */
    function appcontrol() {
        var ev = document.createEvent("Event");
        ev.initEvent("appcontrol", false, false);
        global.dispatchEvent(ev);
    }
    listen("home", appcontrol);

    /* What the Control Center over an app asked for */
    function runAction(a) {
        if (!a) return;
        if (a !== "controlcenter") closeCc();
        if (a === "controlcenter") { if (APL.controlCenter) APL.controlCenter.toggle(); }
        else if (a === "settings") APL.settings.open();
        else if (a.indexOf("settings:") === 0) APL.settings.open(a.substr(9));
        else if (a === "search") { if (APL.search) APL.search.open(); }
        else if (a === "screensaver") { if (APL.screensaver) APL.screensaver.start(); }
        else if (a === "profiles") { if (APL.profilesUI) APL.profilesUI.open(); }
        else if (a === "switcher") { if (APL.switcher && !APL.switcher.isOpen()) APL.switcher.open(); }
    }
    listen("action", function (a) { setTimeout(function () { runAction(String(a || "")); }, 50); });
    listen("homeRole", function () { fresh(); });
    listen("accessibility", function () { fresh(); });

    /* Back on the Home Screen: what the Control Center over an app changed (Do Not Disturb, Sleep Timer) */
    document.addEventListener("visibilitychange", function () {
        if (document.hidden) return;
        fresh();
        sys.readSleepTimer();
        try {
            var p = JSON.parse(global.localStorage.getItem("apl.profiles") || "{}");
            var mine = JSON.parse(global.localStorage.getItem("apl.prefs." + p.current) || "{}");
            if ("dnd" in mine && mine.dnd !== sys.pref("dnd")) sys.setPref("dnd", mine.dnd);
        } catch (e) {}
    });

    /* Once the launcher has started: what it was opened for */
    setTimeout(function wait() {
        if (!global.APL_BOOT_OK) { setTimeout(wait, 300); return; }
        runAction(String(B.takeAction() || ""));
    }, 300);
})(window);
