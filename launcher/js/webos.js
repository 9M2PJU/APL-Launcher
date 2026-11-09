/* ============================================================
   webos.js — APL Launcher on LG TVs (webOS): the platform layer.

   Loaded first, and does nothing anywhere else (Samsung, the PC preview).
   On an LG TV it sets APL.platform, which the shared code asks instead of
   Tizen's APIs: apps.js (the apps, opening them), system.js (sound,
   network, picture… through webos-system.js), preview.js (not a preview).

   What a Developer Mode app may do on its own, through luna:
     open an app (com.webos.applicationManager/launch), sound up/down and
     mute, read the network, the model, the sound output.
   What it may not (LG keeps it for its partners): list the installed apps,
   read the app in front, set the volume, change input, turn off.
   Those go through the TV's own remote-control API (the one LG's phone
   remote uses), relayed by the launcher's webOS service
   (webos/service/): the TV asks once, on screen, whether to allow it.

   The remote: Back is 461 on LG and 10009 on Samsung. LG's codes are
   turned into Samsung's here, before anything else sees them, so the rest
   of the launcher reads the same keys on both.
   ============================================================ */
(function (global) {
    "use strict";

    var S = global.webOSSystem || global.PalmSystem;
    if (global.tizen || !global.PalmServiceBridge || !S) return;

    var APL = global.APL = global.APL || {};
    var APP_ID = "com.knikt.apllauncher";
    var SVC = "luna://com.knikt.apllauncher.service/";
    var KEY_STORE = "apl.webos.key", APPS_STORE = "apl.webos.apps";

    function load(k, fallback) { try { var v = JSON.parse(global.localStorage.getItem(k) || "null"); return v === null ? fallback : v; } catch (e) { return fallback; } }
    function save(k, v) { try { global.localStorage.setItem(k, JSON.stringify(v)); } catch (e) {} }
    function t(s, vars) { return APL.t ? APL.t(s, vars) : String(s).replace(/\{(\w+)\}/g, function (m, k) { return vars && k in vars ? vars[k] : m; }); }

    /* ---------- the TV ---------- */
    var device = {};
    try { device = JSON.parse(S.deviceInfo || "{}") || {}; } catch (e) { device = {}; }
    var model = String(device.modelName || "").replace(/\..*$/, "");
    /* OLED: what the TV says (panelType), otherwise LG's model names (OLED55G56LS, OLED65C4…) */
    var oled = device.panelType ? /OLED/i.test(String(device.panelType)) : /^OLED/i.test(model);
    document.documentElement.className += " is-webos";

    /* ---------- luna ---------- */
    var live = [];          // bridges waiting for their answer: a bridge nobody holds can be collected before it answers
    function luna(uri, params, onMsg) {
        var b = new global.PalmServiceBridge();
        live.push(b);
        b.onservicecallback = function (txt) {
            var m = {};
            try { m = JSON.parse(txt); } catch (e) {}
            if (!b._sub) { var i = live.indexOf(b); if (i !== -1) live.splice(i, 1); }
            if (onMsg) onMsg(m);
        };
        b._sub = !!(params && params.subscribe);
        b.call(uri, JSON.stringify(params || {}));
        return b;
    }
    function lunaOnce(uri, params, ms) {
        return new Promise(function (resolve, reject) {
            var done = false;
            var timer = setTimeout(function () { if (!done) { done = true; reject(new Error(t("The TV didn't answer"))); } }, ms || 15000);
            luna(uri, params, function (m) {
                if (done) return;
                done = true; clearTimeout(timer);
                if (m && m.returnValue === false) reject(new Error(m.errorText || t("The TV refused")));
                else resolve(m || {});
            });
        });
    }
    function endSub(b) {
        if (!b) return;
        try { b.cancel(); } catch (e) {}
        var i = live.indexOf(b); if (i !== -1) live.splice(i, 1);
    }

    /* ---------- the TV's remote-control API, through the service ---------- */
    var state = "idle", stateFns = [], conn = null, key = load(KEY_STORE, "");
    function setState(s) {
        if (!s || s === state) return;
        state = s;
        stateFns.forEach(function (fn) { try { fn(s); } catch (e) {} });
    }
    function connect() {
        if (conn) return;
        conn = luna(SVC + "connect", { subscribe: true, key: key || undefined }, function (m) {
            if (m.returnValue === false) {
                // The service isn't there (an old package) or refused us: try again later
                endSub(conn); conn = null;
                setState("error");
                setTimeout(connect, 15000);
                return;
            }
            if (m.key && m.key !== key) { key = m.key; save(KEY_STORE, key); }
            setState(m.state);
        });
    }
    /* Waits for the TV's answer to the question on screen (or for the key it already gave). */
    var readyWaiters = [];
    stateFns.push(function (s) {
        if (s !== "registered" && s !== "refused") return;
        var w = readyWaiters; readyWaiters = [];
        w.forEach(function (fn) { fn(s); });
    });
    function ready(ms) {
        connect();
        if (state === "registered") return Promise.resolve();
        return new Promise(function (resolve, reject) {
            var timer = setTimeout(function () { reject(new Error(t("LG Connect Apps didn't answer"))); }, ms || 20000);
            readyWaiters.push(function (s) {
                clearTimeout(timer);
                if (s === "registered") resolve(); else reject(new Error(t("The TV refused APL Launcher")));
            });
        });
    }
    function ssap(uri, payload, ms) {
        return ready(ms && ms > 20000 ? ms : 20000).then(function () {
            return lunaOnce(SVC + "request", { uri: uri, payload: payload || {} }, ms || 20000);
        }).then(function (m) { return m.payload || {}; });
    }
    /* onMsg(payload) at each answer; returns a function that ends it. */
    function ssapSub(uri, payload, onMsg) {
        var b = null, ended = false, retry = null;
        function start() {
            if (ended) return;
            ready(600000).then(function () {
                if (ended) return;
                b = luna(SVC + "subscribe", { subscribe: true, uri: uri, payload: payload || {} }, function (m) {
                    if (m.returnValue === false) {          // the connection closed: again in a while
                        endSub(b); b = null;
                        if (!ended) retry = setTimeout(start, 10000);
                        return;
                    }
                    if (m.payload) onMsg(m.payload);
                });
            }, function () { if (!ended) retry = setTimeout(start, 30000); });
        }
        start();
        return function () { ended = true; clearTimeout(retry); endSub(b); };
    }
    function button(name) {
        return ready().then(function () { return lunaOnce(SVC + "button", { name: name }); });
    }
    function service(method, params) { return lunaOnce(SVC + method, params || {}); }

    /* ---------- opening apps (luna: no question asked) ---------- */
    function launch(id, params) {
        return lunaOnce("luna://com.webos.applicationManager/launch", { id: id, params: params || {} }).then(function () {});
    }

    /* ---------- the apps: LG's launch points (the apps in LG's own home bar) ---------- */
    var SELF = new RegExp("^" + APP_ID.replace(/\./g, "\\.") + "(\\.|$)");
    /* Not apps people open from a home screen: LG's own panels and helpers */
    var HIDE = /^(com\.webos\.app\.(home|homeconnect|notificationcenter|voice|igallery|accountmanagement|quicksettings|livemenu|channeledit|tvhotkey|inputcommon|factorywin|membership|scheduler|onboarding|crashreport|tips|care365)|com\.webos\.exampleapp)/i;
    var appsCache = load(APPS_STORE, null);
    /* The TV gives icons on https://127.0.0.1:3001 (its own certificate: refused by this page) and
       backgrounds as the apps' files: the launcher's service serves both, with the header that lets
       the launcher read their colours (webos/service/index.js). */
    var ART = "http://127.0.0.1:8767/";
    function iconUrl(u) { return /^https?:\/\/127\.0\.0\.1:300[01]\/resources\//.test(String(u || "")) ? ART + "icon?u=" + encodeURIComponent(u) : null; }
    function fileUrl(p) { return /^\/(media|usr|mnt)\/.*\.(png|jpe?g|webp)$/i.test(String(p || "")) ? ART + "file?p=" + encodeURIComponent(p) : null; }
    /* An app's own background, when it is a picture (not its splash screen: a logo on a plain colour,
       which the launcher's own Top Shelf art does better, as on Samsung) */
    function backdrop(lp) {
        var p = String(lp.bgImage || "");
        if (!p || lp.systemApp || /splash/i.test(p) || !/^\/media\/cryptofs\/apps\//.test(p) || badBg[p]) return null;
        return fileUrl(p);
    }
    /* Some apps name a background the TV doesn't let the service read: tried once, then left out (the
       launcher's own Top Shelf art takes its place, as for any app without one) */
    var BAD_STORE = "apl.webos.badbg", badBg = load(BAD_STORE, {});
    function checkBackdrops(list, then) {
        var todo = list.filter(function (a) { return a.bg; }), left = todo.length, dropped = 0;
        if (!left) return;
        todo.forEach(function (a) {
            var im = new Image();
            im.onload = function () { if (--left === 0 && dropped) then(); };
            im.onerror = function () {
                var p = decodeURIComponent(String(a.bg).split("file?p=")[1] || "");
                if (p) badBg[p] = 1;
                a.bg = null; dropped++;
                if (--left === 0) { save(BAD_STORE, badBg); then(); }
            };
            im.src = a.bg;
        });
    }
    function fromLaunchPoints(lps) {
        var seen = {}, out = [];
        (lps || []).forEach(function (lp) {
            var id = String(lp.id || ""), launchId = id;
            if (!id || SELF.test(id) || HIDE.test(id)) return;
            // A website pinned to LG's home bar: one more launch point of the browser, with its address
            if (lp.lptype === "bookmark" || seen[id]) {
                if (!lp.launchPointId) return;
                id = "lp." + lp.launchPointId;
            }
            if (seen[id]) return;
            seen[id] = true;
            out.push({
                id: id,
                launchId: launchId,
                name: String(lp.title || id),
                icon: iconUrl(lp.largeIcon) || iconUrl(lp.icon) || fileUrl(lp.extraLargeIcon) || null,
                bg: backdrop(lp),
                accent: lp.bgColor || null,
                params: lp.params && Object.keys(lp.params).length ? lp.params : null,
                web: lp.lptype === "bookmark"
            });
        });
        return out;
    }
    function fetchApps() {
        return ssap("ssap://com.webos.applicationManager/listLaunchPoints", {}, 30000).then(function (p) {
            var list = fromLaunchPoints(p.launchPoints);
            if (list.length) {
                appsCache = list; save(APPS_STORE, list);
                checkBackdrops(list, function () {
                    save(APPS_STORE, list);
                    if (APL.refreshHome) APL.refreshHome();
                });
            }
            return list;
        });
    }
    /* At start-up the list kept from last time comes at once, and the TV's own list replaces it as
       soon as it answers. The first time (no answer from the TV yet), Settings and Sources only. */
    var appsChanged = null;
    function loadApps(decorate, order) {
        function done(list) {
            return order(list.map(function (a) {
                var d = decorate(a);
                d.params = a.params;
                if (a.bg && !d.tile) d.bg = a.bg;             // the app's own picture, where the catalogue has no artwork
                return d;
            }));
        }
        if (appsCache && appsCache.length) {
            fetchApps().then(function (list) {
                if (JSON.stringify(list) !== JSON.stringify(appsChanged || [])) {
                    appsChanged = list;
                    if (APL.refreshHome && JSON.stringify(list) !== JSON.stringify(load(APPS_STORE + ".shown", null))) {
                        save(APPS_STORE + ".shown", list);
                        APL.refreshHome();
                    }
                }
            }, function () {});
            save(APPS_STORE + ".shown", appsCache);
            return Promise.resolve(done(appsCache));
        }
        return new Promise(function (resolve) {
            var settled = false;
            function finish(list) { if (settled) return; settled = true; save(APPS_STORE + ".shown", list); resolve(done(list)); }
            fetchApps().then(finish, function () { finish([]); });
            // The question on screen: the Home Screen does not wait for it, it fills in once allowed
            setTimeout(function () {
                if (settled) return;
                finish([]);
                fetchApps().then(function () { if (APL.refreshHome) APL.refreshHome(); }, function () {});
            }, 6000);
        });
    }
    function launchApp(id) {
        var a = null;
        (appsCache || []).forEach(function (x) { if (x.id === id) a = x; });
        return launch(a && a.launchId || id, a && a.params).catch(function (e) {
            throw new Error(t("Couldn't open the app") + (e && e.message ? " (" + e.message + ")" : ""));
        });
    }
    function isInstalled(id) {
        if (!appsCache) return Promise.resolve(false);
        return Promise.resolve(appsCache.some(function (a) { return a.id === id; }));
    }
    /* A website: LG's Web Browser (com.webos.app.browser) opens it */
    function openUrl(url) {
        if (!/^https?:\/\//.test(url)) return Promise.reject(new Error(t("Invalid address")));
        return launch("com.webos.app.browser", { target: url });
    }

    /* ---------- the remote ---------- */
    /* LG -> Samsung codes. Back 461; LG's ⏸ (19) is the play/pause key, which opens the Control Center
       on the Home Screen. Colours, arrows, OK, numbers and ▶ are the same on both. */
    var MAP = { 461: 10009, 19: 10252 };
    function remap(e) {
        var to = MAP[e.keyCode];
        if (!to || e._apl) return;
        e.stopImmediatePropagation();
        e.preventDefault();
        var ev;
        try {
            ev = new KeyboardEvent(e.type, { bubbles: true, cancelable: true, repeat: !!e.repeat, key: e.key, code: e.code });
        } catch (x) {
            ev = document.createEvent("Event");
            ev.initEvent(e.type, true, true);
        }
        try {
            Object.defineProperty(ev, "keyCode", { get: function () { return to; } });
            Object.defineProperty(ev, "which", { get: function () { return to; } });
        } catch (x) {}
        ev._apl = true;
        (e.target || document.body).dispatchEvent(ev);
    }
    global.addEventListener("keydown", remap, true);
    global.addEventListener("keyup", remap, true);

    /* Opened again while running (its icon in LG's home bar): to the front, and like Home on Samsung,
       the Home Screen at rest (launcher.js, "appcontrol"). */
    document.addEventListener("webOSRelaunch", function () {
        try { if (S.activate) S.activate(); } catch (e) {}
        var ev;
        try { ev = new Event("appcontrol"); } catch (x) { ev = document.createEvent("Event"); ev.initEvent("appcontrol", false, false); }
        global.dispatchEvent(ev);
    });

    /* ---------- the question on screen ---------- */
    /* The TV names it "LG Remote App" (LG's own manifest, the only one it accepts): the Home Screen says
       that it is APL Launcher, and fills in once it is allowed. */
    var asked = false;
    stateFns.push(function (s) {
        if (s === "prompt" && !asked) {
            asked = true;
            if (APL.toast) APL.toast(t("The TV asks to allow “LG Remote App”: that's APL Launcher. Choose Allow to see your apps."));
        }
        if (s === "registered" && asked) {
            asked = false;
            if (APL.toast) APL.toast(t("Thank you: APL Launcher can now show your apps."), true);
        }
    });

    connect();

    APL.platform = {
        name: "webos",
        /* The dock's first apps (when installed), as FAVORITES on Samsung: YouTube in TizenTube's place */
        favorites: ["youtube.leanback.v4", "com.knikt.apltv", "app.hotiptv", "netflix"],
        model: model,
        oled: oled,
        device: device,
        luna: luna, lunaOnce: lunaOnce, endSub: endSub,
        ssap: ssap, ssapSub: ssapSub, button: button, service: service,
        state: function () { return state; },
        onState: function (fn) { stateFns.push(fn); },
        reconnect: function () { if (conn) { endSub(conn); conn = null; } state = "idle"; connect(); },
        launch: launch,
        loadApps: loadApps, launchApp: launchApp, isInstalled: isInstalled, openUrl: openUrl,
        cachedApps: function () { return appsCache || []; }
    };
})(window);
