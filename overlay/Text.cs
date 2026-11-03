using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tizen.Applications;

namespace APL
{
    /// <summary>
    /// The panel's language, as in launcher/js/i18n.js: the code is written in English and French comes
    /// from the table below, keyed by the English text. The launcher sends its language with each launch
    /// (apl_lang); the last one is kept in the data folder, so the panel speaks it after a restart before
    /// the launcher says anything. With nothing kept, the screen's language decides.
    /// </summary>
    internal static class Text
    {
        private const string FileName = "lang.txt";

        private static string language;
        private static bool stored;

        private static readonly Dictionary<string, string> French = new Dictionary<string, string>
        {
            // Control Center
            { "Sleep", "Veille" },
            { "Sleep Timer", "Minuterie" },
            { "timer|Off", "Désactivée" },
            { "Settings", "Réglages" },
            { "All Settings", "Tous les réglages" },
            { "Game Mode", "Mode Jeu" },
            { "On", "Activé" },
            { "Off", "Désactivé" },
            { "After switching to HDMI or USB-C", "Après un passage sur HDMI ou USB-C" },
            { "Picture Mode", "Mode Image" },
            { "Mute", "Muet" },
            { "Brightness", "Luminosité" },
            { "Game Mode: switch to HDMI or USB-C first", "Mode Jeu : passe d'abord sur HDMI ou USB-C" },
            { "Turning on Game Mode…", "Activation du Mode Jeu…" },
            { "Turning off Game Mode…", "Désactivation du Mode Jeu…" },
            { "Game Mode on", "Mode Jeu activé" },
            { "Game Mode off", "Mode Jeu désactivé" },
            { "The screen refused Game Mode for now", "L'écran a refusé le Mode Jeu pour le moment" },
            // Picture modes
            { "Dynamic", "Dynamique" },
            { "Eco", "Éco" },
            { "Movie", "Cinéma" },
            { "Game", "Jeu" },
            { "Automatic", "Automatique" },
            // Audio and Sources
            { "TV Speakers", "Haut-parleurs TV" },
            { "Samsung screen", "Écran Samsung" },
            { "Connected", "Connecté" },
            { "Not Connected", "Non connecté" },
            { "Switching to the screen's speakers…", "Son sur les haut-parleurs de l'écran…" },
            { "Connecting to “{0}”…", "Connexion à « {0} »…" },
            { "Sound on “{0}”", "Son sur « {0} »" },
            { "“{0}” is not responding: open the case or turn on the headphones.", "« {0} » ne répond pas : ouvre le boîtier ou allume le casque." },
            { "Console, PC, set-top box", "Console, PC, décodeur" },
            { "Laptop, Mac, tablet", "PC portable, Mac, tablette" },
            // Alexa
            { "Alexa: {0}", "Alexa : {0}" },
            { "{0} (off)", "{0} (arrêt)" },
            { "Screen", "Écran" },
            { "Home", "Accueil" },
        };

        /// <summary>"en" or "fr".</summary>
        public static string Language
        {
            get
            {
                if (language == null) language = Load();
                return language;
            }
        }

        /// <summary>
        /// The text in the current language, English when it has no translation. A context before '|'
        /// tells apart texts that read the same in English but not in French; it is never shown.
        /// </summary>
        public static string T(string english)
        {
            string text;
            if (Language == "fr" && French.TryGetValue(english, out text)) return text;
            int bar = english.IndexOf('|');
            return bar < 0 ? english : english.Substring(bar + 1);
        }

        /// <summary>A template such as "Sound on “{0}”", with its values filled in.</summary>
        public static string T(string english, params object[] values)
        {
            return string.Format(T(english), values);
        }

        /// <summary>The launcher's language: true when it changed. Labels follow at the next panel build.</summary>
        public static bool SetLanguage(string value)
        {
            string lang = value.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en";
            bool changed = lang != Language;
            language = lang;
            if (changed || !stored)
            {
                try
                {
                    File.WriteAllText(SavedFile(), lang);
                    stored = true;
                }
                catch (Exception ex) { Program.Note("language: " + ex.Message); }
            }
            return changed;
        }

        private static string Load()
        {
            try
            {
                string file = SavedFile();
                if (File.Exists(file))
                {
                    string saved = File.ReadAllText(file).Trim();
                    if (saved == "fr" || saved == "en")
                    {
                        stored = true;
                        return saved;
                    }
                }
            }
            catch (Exception ex) { Program.Note("language: " + ex.Message); }
            return ScreenLanguage();
        }

        /// <summary>French on a French screen ("fr_FR.UTF-8" in LANG, or a French culture), else English.</summary>
        private static string ScreenLanguage()
        {
            string lang = Environment.GetEnvironmentVariable("LANG") ?? "", culture = "";
            try { culture = CultureInfo.CurrentUICulture.Name ?? ""; }
            catch (Exception) { }
            bool french = lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase) || culture.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            return french ? "fr" : "en";
        }

        private static string SavedFile()
        {
            return System.IO.Path.Combine(Application.Current.DirectoryInfo.Data, FileName);
        }
    }
}
