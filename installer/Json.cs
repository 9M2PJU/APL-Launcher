using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace APL.Setup
{
    /// <summary>
    /// Just enough JSON for the scripts' marker lines and Vetro's /status (no NuGet, and
    /// DataContractJsonSerializer would need classes for every shape). Objects become
    /// Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double.
    /// </summary>
    internal static class Json
    {
        internal static object Parse(string text)
        {
            int i = 0;
            object v = Value(text, ref i);
            Skip(text, ref i);
            if (i != text.Length) throw new FormatException("Unexpected text after JSON at " + i);
            return v;
        }

        internal static bool TryParse(string text, out object value)
        {
            try { value = Parse(text); return true; }
            catch { value = null; return false; }
        }

        // ---------- reading helpers (tolerant: PowerShell's ConvertTo-Json turns 1-item arrays into the item) ----------

        internal static object Get(object obj, string key)
        {
            var d = obj as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }

        internal static string Str(object obj, string key)
        {
            object v = Get(obj, key);
            if (v == null) return null;
            if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
            if (v is bool) return (bool)v ? "true" : "false";
            return v as string;
        }

        internal static bool Bool(object obj, string key)
        {
            object v = Get(obj, key);
            if (v is bool) return (bool)v;
            if (v is double) return (double)v != 0;
            var s = v as string;
            return s != null && (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1");
        }

        internal static int Int(object obj, string key)
        {
            object v = Get(obj, key);
            if (v is double) return (int)(double)v;
            int n;
            return v is string && int.TryParse((string)v, out n) ? n : 0;
        }

        internal static List<object> List(object obj, string key)
        {
            object v = Get(obj, key);
            if (v == null) return new List<object>();
            var l = v as List<object>;
            return l ?? new List<object> { v };
        }

        // ---------- writing ----------

        internal static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        // ---------- parser ----------

        private static void Skip(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return String(s, ref i);
            if (Word(s, ref i, "true")) return true;
            if (Word(s, ref i, "false")) return false;
            if (Word(s, ref i, "null")) return null;
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            double d;
            if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                throw new FormatException("Bad JSON value at " + start);
            return d;
        }

        private static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        private static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            i++;
            Skip(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                string key = String(s, ref i);
                Skip(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("Expected : at " + i);
                i++;
                d[key] = Value(s, ref i);
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw new FormatException("Expected , or } at " + i);
            }
        }

        private static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++;
            Skip(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return l; }
                throw new FormatException("Expected , or ] at " + i);
            }
        }

        private static string String(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') throw new FormatException("Expected string at " + i);
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("Bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: sb.Append(e); break;   // \" \\ \/
                }
            }
            throw new FormatException("Unterminated string");
        }
    }
}
