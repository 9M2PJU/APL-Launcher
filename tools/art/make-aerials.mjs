// ============================================================
//  make-aerials.mjs — rewrites launcher/js/aerials.js, the list of Apple's
//  "Aerial" videos for the screen saver.
//
//  Source: github.com/OrangeJedi/Aerial (videos.json, with the English place
//  names; localized/fr.json gives the French names that set the order).
//  Each video: [place, type, time of day, 4K url, 1080p url].
//  The 4K is HEVC: the screen decodes it, one at a time. Recent videos that
//  Apple only publishes in "240p" are left out: the screen refuses them
//  ("no supported source").
//
//  Usage:  node tools/art/make-aerials.mjs     (Node 18 or later, no packages)
// ============================================================
import { writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const RAW = "https://raw.githubusercontent.com/OrangeJedi/Aerial/master/";
const OUT = join(dirname(fileURLToPath(import.meta.url)), "../../launcher/js/aerials.js");

const get = async (path) => {
    const r = await fetch(RAW + path);
    if (!r.ok) throw new Error(path + ": " + r.status);
    return r.json();
};

// The place: the video's English group name, "ISS (Space)" spelled out
const RENAME = { "ISS (Space)": "International Space Station" };
const place = (v) => RENAME[v.accessibilityLabel] || v.accessibilityLabel;
// The order: by French place name (localized/fr.json, with the launcher's own two French names), then by URL
const FR_RENAME = { "Sous l’eau": "Fonds marins", "Terre": "Station spatiale internationale" };
const frPlace = (v) => { const n = (fr[v.id] && fr[v.id].group) || v.accessibilityLabel; return FR_RENAME[n] || n; };

const [videos, fr] = await Promise.all([get("videos.json"), get("localized/fr.json")]);
const list = videos
    .filter((v) => v.src && v.src.H2654k && v.src.H2641080p)
    .map((v) => [frPlace(v), place(v), v.type, v.timeOfDay, v.src.H2654k, v.src.H2641080p])
    .sort((a, b) => a[0].localeCompare(b[0], "fr") || a[5].localeCompare(b[5]))
    .map((a) => a.slice(1));

const head = `/* ============================================================
   aerials.js — the "Aerial" videos of the Apple TV screen saver.
   ${list.length} videos streamed by Apple (sylvan.apple.com): in 4K HEVC, and in
   1080p H.264 if the 4K doesn't play. List taken from the open-source project
   github.com/OrangeJedi/Aerial.
   Format: [place (English, translated when shown), type, time of day, 4K url, 1080p url]
   Regenerate: node tools/art/make-aerials.mjs — personal use.
   ============================================================ */
window.APL_AERIALS = [
`;
writeFileSync(OUT, head + list.map((a) => JSON.stringify(a)).join(",\n") + "\n];\n");
console.log(list.length + " videos written to " + OUT);
