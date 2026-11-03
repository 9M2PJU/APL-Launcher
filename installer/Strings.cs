using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace APL.Setup
{
    /// <summary>
    /// Every visible text is written in English and goes through T(). French comes from the
    /// dictionary below (tutoiement, Samsung's and Apple's French names).
    /// </summary>
    internal static partial class Strings
    {
        internal static bool French;
        internal static CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");
        /// <summary>English texts asked for in French with no translation (listed by --shot).</summary>
        internal static readonly HashSet<string> Missing = new HashSet<string>();

        internal static void Init(string lang)
        {
            French = lang == "fr" || (lang != "en" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr");
            Culture = CultureInfo.GetCultureInfo(French ? "fr-FR" : "en-US");
        }

        internal static string T(string en)
        {
            if (!French || string.IsNullOrEmpty(en)) return en;
            // Keep the spaces around a piece of text (XAML runs next to a link).
            string key = en.Trim();
            if (key.Length == 0 || !key.Any(char.IsLetter)) return en;
            string fr;
            if (!Fr.TryGetValue(key, out fr))
            {
                lock (Missing) Missing.Add(key);
                return en;
            }
            int lead = en.Length - en.TrimStart().Length, trail = en.Length - en.TrimEnd().Length;
            return en.Substring(0, lead) + Typo(fr) + en.Substring(en.Length - trail);
        }

        internal static string T(string en, params object[] args)
        {
            return string.Format(Culture, T(en), args);
        }

        /// <summary>French typography: no-break space before : ; ? ! » and after «.</summary>
        private static string Typo(string s)
        {
            s = Regex.Replace(s, @" ([:;?!»])", " $1");
            return s.Replace("« ", "« ");
        }

        internal static string Percent(double n)
        {
            return ((int)Math.Round(n)).ToString(Culture) + (French ? " %" : "%");
        }

        internal static string Date(string isoDate)
        {
            DateTime d;
            if (!DateTime.TryParseExact(isoDate ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return isoDate;
            return d.ToString(French ? "d MMMM yyyy" : "MMMM d, yyyy", Culture);
        }
    }
}
