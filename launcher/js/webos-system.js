/* ============================================================
   webos-system.js — APL.sys on LG TVs (webOS).

   system.js builds APL.sys for Samsung (Tizen's APIs, Vetro), then hands
   it here (APL.platform.adaptSys) on an LG TV: what this TV can do takes
   the place of each part, the rest (preferences, profiles, sounds) stays.

     sound       luna com.webos.audio (mute, up/down, the level), and the
                 exact level through LG Connect Apps (ssap://audio/…)
     network     luna com.palm.connectionmanager
     the TV      the model and webOS version (luna, the page's deviceInfo)
     inputs      the HDMI / Live TV apps (com.webos.app.hdmi1…)
     off, timer  LG Connect Apps (ssap://system/turnOff), the timer kept by
                 the launcher's service while the launcher is in the background
     picture     LG Connect Apps (ssap://settings/…), webos-settings.js
   Root commands (Vetro on Samsung) don't exist here: they say so.
   ============================================================ */
(function (global) {
    "use strict";

    var APL = global.APL = global.APL || {};
    var P = APL.platform;
    if (!P || P.name !== "webos") return;

    function t(s, vars) { return APL.t ? APL.t(s, vars) : s; }
    function noRoot() { return Promise.reject(new Error(t("This needs a rooted TV on LG"))); }

    /* ---------- sound ---------- */
    var snd = { volume: 0, mute: false, output: "" };
    function readVolume(m) {
        if (!m) return;
        var st = m.volumeStatus || m;
        if (typeof st.volume === "number") snd.volume = st.volume;
        if (typeof st.muteStatus === "boolean") snd.mute = st.muteStatus;
        else if (typeof m.muted === "boolean") snd.mute = m.muted;
        else if (typeof m.mute === "boolean") snd.mute = m.mute;
        if (st.soundOutput) snd.output = st.soundOutput;
    }
    P.lunaOnce("luna://com.webos.audio/getVolume", {}).then(readVolume, function () {});
    P.lunaOnce("luna://com.webos.audio/getSoundOutput", {}).then(function (m) { if (m.soundOutput) snd.output = m.soundOutput; }, function () {});
    // The exact level as it changes (any remote, any app), once LG Connect Apps is allowed
    P.ssapSub("ssap://audio/getVolume", { subscribe: true }, readVolume);
    var OUTPUTS = {
        tv_speaker: "TV Speaker", external_speaker: "Audio Out", external_optical: "Optical Out", external_arc: "HDMI ARC",
        lineout: "Line Out", headphone: "Headphones", tv_external_speaker: "TV Speaker + Audio Out",
        tv_speaker_headphone: "TV Speaker + Headphones", bt_soundbar: "Bluetooth", soundbar: "Soundbar"
    };
    function outputName(o) { return OUTPUTS[o] ? t(OUTPUTS[o]) : (o || "—"); }

    var volTimer = null;
    var audio = {
        getVolume: function () { return snd.volume; },
        setVolume: function (v) {
            v = Math.max(0, Math.min(100, Math.round(v)));
            var from = snd.volume;
            snd.volume = v;
            clearTimeout(volTimer);
            volTimer = setTimeout(function () {
                P.ssap("ssap://audio/setVolume", { volume: v }, 8000).catch(function () {
                    // Not allowed yet: the steps the TV lets any app take
                    var n = Math.min(30, Math.abs(v - from)), up = v > from;
                    (function step() {
                        if (n-- <= 0) return;
                        P.lunaOnce("luna://com.webos.audio/" + (up ? "volumeUp" : "volumeDown"), {}).then(step, function () {});
                    })();
                });
            }, 120);
            return v;
        },
        isMute: function () { return snd.mute; },
        setMute: function (m) {
            snd.mute = !!m;
            P.lunaOnce("luna://com.webos.audio/setMuted", { muted: !!m }).catch(function () {
                return P.ssap("ssap://audio/setMute", { mute: !!m });
            }).catch(function (e) { if (APL.toast) APL.toast(e.message); });
        },
        outputMode: function () { return outputName(snd.output); },
        output: function () { return snd.output; },
        setOutput: function (o) { snd.output = o; },
        outputName: outputName
    };

    /* ---------- network ---------- */
    var net = { connected: false, type: t("Unknown"), ssid: "—", signal: 0, ip: "—", mac: "—", gateway: "—", dns: "—" };
    function readNet(m) {
        if (!m || m.returnValue === false) return;
        var w = m.wired || {}, f = m.wifi || {};
        var on = w.state === "connected" ? w : f.state === "connected" ? f : null;
        net.connected = !!on;
        net.type = w.state === "connected" ? "Ethernet" : f.state === "connected" ? "Wi-Fi" : t("Disconnected");
        net.ssid = f.state === "connected" ? (f.ssid || "—") : "—";
        net.ip = on && on.ipAddress || "—";
        net.gateway = on && on.gateway || "—";
        net.dns = on && (on.dns1 || on.dns2) || "—";
        if (f.state === "connected" && typeof f.signalLevel === "number") net.signal = Math.max(0, Math.min(5, Math.round(f.signalLevel)));
    }
    P.luna("luna://com.palm.connectionmanager/getStatus", { subscribe: true }, readNet);
    P.lunaOnce("luna://com.palm.connectionmanager/getinfo", {}).then(function (m) {
        var mac = (m.wiredInfo && m.wiredInfo.macAddress) || (m.wifiInfo && m.wifiInfo.macAddress);
        if (mac) net.mac = String(mac).toUpperCase();
    }, function () {});
    function network() { return net; }

    /* ---------- the TV ---------- */
    var info = { model: P.model || "—", name: "LG " + (P.model || "TV"), firmware: "—", tizen: "—" };
    P.lunaOnce("luna://com.webos.service.tv.systemproperty/getSystemInfo", { keys: ["modelName", "firmwareVersion", "sdkVersion"] }).then(function (m) {
        if (m.firmwareVersion) info.firmware = m.firmwareVersion;
        if (m.modelName && info.model === "—") info.model = String(m.modelName).replace(/\..*$/, "");
        if (m.sdkVersion && info.tizen === "—") info.tizen = m.sdkVersion;
    }, function () {});
    P.lunaOnce("luna://com.webos.service.systemservice/osInfo/query", { parameters: ["webos_release", "webos_name", "webos_imagename"] }).then(function (m) {
        if (m.webos_release) info.tizen = m.webos_release + (info.tizen !== "—" && info.tizen !== m.webos_release ? " (SDK " + info.tizen + ")" : "");
    }, function () {});
    /* LG's names: OLED55G56LS is an OLED G5, 55 inches */
    (function () {
        var m = /^OLED(\d{2})([A-Z])(\d)/.exec(info.model);
        if (m) info.name = "LG OLED " + m[2] + m[3] + " " + m[1] + "″";
        else if (/^\d{2}[A-Z]{2}\d/.test(info.model)) info.name = "LG " + info.model.slice(2, 6) + " " + info.model.slice(0, 2) + "″";
    })();
    function product() { return info; }

    var dev = P.device || {};
    function display() {
        var ui = (dev.screenWidth || 1920) + " × " + (dev.screenHeight || 1080);
        var panel = dev.uhd8K ? "7680 × 4320" : dev.uhd ? "3840 × 2160" : "";
        return Promise.resolve(panel && panel !== ui ? t("{panel} (interface {ui})", { panel: panel, ui: ui }) : ui);
    }
    /* LG's OLEDs all show Dolby Vision, HDR10 and HLG; the TV tells nothing of it to an app */
    function hdr() {
        var kinds = [];
        if (dev.dolbyVision) kinds.push("Dolby Vision");
        if (dev.hdr10) kinds.push("HDR10");
        if (!kinds.length && P.oled) kinds = ["Dolby Vision", "HDR10", "HLG"];
        return kinds.length ? kinds.join(", ") : "—";
    }

    /* ---------- the remote, off, inputs ---------- */
    var BUTTONS = { KEY_HOME: "HOME", KEY_RETURN: "BACK", KEY_EXIT: "EXIT", KEY_MENU: "MENU", KEY_INFO: "INFO", KEY_SOURCE: "INPUT_HUB" };
    function remoteKey(key) {
        if (key === "KEY_POWER") return sleepNow();
        return P.button(BUTTONS[key] || String(key).replace(/^KEY_/, ""));
    }
    function sleepNow() {
        return P.ssap("ssap://system/turnOff", {}, 8000).then(function () {});
    }
    /* Samsung's numbers, as sources.js gives them: TV 0, HDMI 1 to 4 = 13 to 16. On LG each input is an app. */
    function switchSource(type) {
        var id = type === 0 ? "com.webos.app.livetv" : type >= 13 && type <= 16 ? "com.webos.app.hdmi" + (type - 12) : "";
        if (!id) return Promise.reject(new Error(t("This input isn't supported on this screen")));
        return P.launch(id);
    }
    /* The TV's inputs, for Sources: its HDMI ports, named as in LG's input list when renamed there */
    P.readPorts = function () {
        return P.ssap("ssap://tv/getExternalInputList", {}, 10000).then(function (p) {
            var ports = (p.devices || []).filter(function (d) { return /^HDMI_\d$/.test(d.id || ""); }).map(function (d) {
                return { type: "HDMI", number: d.port || parseInt(String(d.id).slice(5), 10), label: d.modified && d.label ? String(d.label) : "" };
            });
            if (!ports.length) return null;
            ports.unshift({ type: "TV", number: 1 });
            return ports;
        }, function () { return null; });
    };

    /* LG's own Settings, for what the launcher doesn't do itself */
    function openSettings() { return P.launch("com.palm.app.settings").catch(function () { return P.button("MENU"); }); }

    /* ---------- sleep timer: counted by the service, the launcher being in the background then ---------- */
    function armSleepTimer(sys, minutes) {
        var at = minutes ? Date.now() + minutes * 60000 : 0;
        sys.setPref("sleepTimer", minutes);
        sys.setPref("sleepAt", at);
        P.service("sleep", { minutes: minutes }).catch(function (e) { if (APL.toast) APL.toast(e.message); });
    }
    function readSleepTimer(sys) {
        return P.service("status", {}).then(function (m) {
            var at = m.sleepAt && m.sleepAt > Date.now() ? m.sleepAt : 0;
            sys.setPref("sleepAt", at);
            if (!at) sys.setPref("sleepTimer", 0);
        }, function () {});
    }

    /* ---------- Developer Mode ----------
       When its 1000 hours run out, the TV deletes every app installed through it, this one included. The
       launcher's service renews it the way the Developer Mode app's Extend button does: checked once a day
       while the launcher runs, renewed once fewer than 960 hours are left. */
    var DEV_KEY = "apl.webos.devmode", dev = (function () { try { return JSON.parse(global.localStorage.getItem(DEV_KEY) || "null") || {}; } catch (e) { return {}; } })();
    function devCheck() {
        if (dev.at && Date.now() - dev.at < 20 * 3600 * 1000) return;
        P.service("devmode", { extend: true }).then(function (m) {
            dev = { at: Date.now(), hours: m.hours, renewed: m.extended ? Date.now() : dev.renewed || 0 };
            try { global.localStorage.setItem(DEV_KEY, JSON.stringify(dev)); } catch (e) {}
        }, function (e) { dev.error = e.message; });
    }
    setTimeout(devCheck, 8000);
    setInterval(devCheck, 3600 * 1000);
    P.devmode = function () {
        if (dev.hours === undefined) return dev.error ? t("Unavailable") : "…";
        var left = Math.max(0, dev.hours - Math.floor((Date.now() - (dev.at || Date.now())) / 3600000));
        return t("{h} h left, renewed by the launcher", { h: left });
    };

    /* ---------- how the launcher reaches the TV (Settings › About, in place of "Link to Vetro") ---------- */
    function link() {
        var s = P.state();
        var text = { registered: t("Allowed"), prompt: t("Waiting for you to allow it on the TV"), refused: t("Refused on the TV"),
                     connecting: t("Connecting…"), error: t("Unavailable"), idle: t("Not connected") }[s] || s;
        return Promise.resolve(text);
    }

    P.adaptSys = function (sys, internals) {
        var D = internals && internals.DEFAULTS;
        if (D) {
            if (P.oled) D.screensaverClockMove = 2;      // as on Samsung's OLEDs: the clock goes round the corners
            // Samsung-only parts: no Smart Hub rows, no Vetro, no APL Overlay, no updates from GitHub yet
            D.hubShelf = false; D.homeReturn = false; D.alexaBridge = false; D.idleClose = false; D.bootCleanup = false; D.autoUpdate = false;
        }
        sys.platform = "webos";
        sys.audio = audio;
        sys.network = network;
        sys.product = product;
        sys.display = display;
        sys.storage = function () { return Promise.resolve("—"); };
        sys.hdr = hdr;
        sys.remoteKey = remoteKey;
        sys.openSamsungSettings = openSettings;
        sys.openTvSettings = openSettings;
        sys.sleepNow = sleepNow;
        sys.switchSource = switchSource;
        sys.runningApps = function () { return Promise.resolve([]); };
        sys.registerKeys = function () {};
        sys.armSleepTimer = function (m) { armSleepTimer(sys, m); };
        sys.readSleepTimer = function () { return readSleepTimer(sys); };
        sys.vetroAlive = function () { return Promise.resolve(false); };
        sys.vetroLink = link;
        sys.vetroGet = noRoot; sys.vetroPost = noRoot; sys.exec = noRoot;
        sys.noVetro = function () { return false; };         // the rows that need Vetro are not shown here (webos-settings.js)
        sys.isOled = function () { return !!P.oled; };
        sys.game = {
            get: function () { return Promise.resolve({ available: false, on: false }); },
            set: function () { return Promise.reject(new Error(t("Game Optimizer is in the TV's own settings"))); },
            applyOnInput: function () { return Promise.resolve(); }
        };
        P.sys = sys;
    };
})(window);
