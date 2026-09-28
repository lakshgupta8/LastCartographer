using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// Localisation (PRG-19): the locale, and every string the player reads that isn't dialogue.
    ///
    /// Code says what it shows in English with a key, <c>Loc.T("hud.death", "the ink runs out")</c>, or formats it,
    /// <c>Loc.F("travel.arrived", "{0} on the road. {1}, day {2}.", ...)</c>, so a translation can move the pieces. The
    /// English in code is the source; <c>ui.en.csv</c> is harvested from it for translators, and a table per locale
    /// (<c>Resources/Localization/ui.&lt;locale&gt;.csv</c>: key, text, comment) answers everything else. A key a
    /// table lacks shows its English. Dialogue is Yarn's: every line carries a <c>#line:</c> id and each locale is a
    /// strings CSV on the Yarn project; the dialogue runner follows <see cref="Locale"/>.
    ///
    /// <c>en-XA</c> is the pseudo-locale: every string as «Àççéñţéď ţéxţ ···», longer and bracketed, so anything that
    /// didn't come through the tables shows up plainly in English, and anything too tight for a longer language clips.
    /// </summary>
    public static class Loc
    {
        public const string Base = "en";
        public const string Pseudo = "en-XA";
        public const string PrefsKey = "owsbg.locale";
        public const string TablePath = "Localization/ui.";

        /// <summary>The locale changed; views re-text themselves.</summary>
        public static event Action<string> Changed;

        static string _locale;
        static readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();
        static readonly List<string> _extra = new List<string>();

        /// <summary>The locale the player reads: saved choice, else the system's language if we have it, else English.</summary>
        public static string Locale
        {
            get
            {
                if (_locale == null)
                {
                    string saved = null;
                    try { saved = PlayerPrefs.GetString(PrefsKey, ""); } catch { }
                    _locale = IsSupported(saved) ? saved : FromSystem(Application.systemLanguage);
                }
                return _locale;
            }
        }

        /// <summary>Every locale the game can show: English, the pseudo-locale, and any with a UI table.</summary>
        public static IReadOnlyList<string> Locales
        {
            get
            {
                var list = new List<string> { Base, Pseudo };
                foreach (var l in _extra) if (!list.Contains(l)) list.Add(l);
                return list;
            }
        }

        /// <summary>The locales the options page offers (DES-14): all of them, less the pseudo-locale outside development builds.</summary>
        public static IReadOnlyList<string> Choosable
        {
            get
            {
                var list = new List<string>(Locales);
                if (!Debug.isDebugBuild) list.Remove(Pseudo);
                return list;
            }
        }

        /// <summary>A language as its own speakers name it: shown in that language whatever the locale, as pickers do.</summary>
        public static string NativeName(string locale) => locale switch
        {
            Base => "English",
            Pseudo => "Pseudo (en-XA)",
            "fr" => "Français", "de" => "Deutsch", "es" => "Español", "it" => "Italiano", "pt" => "Português",
            "ja" => "日本語", "ko" => "한국어", "zh-Hans" => "简体中文", "zh-Hant" => "繁體中文", "ru" => "Русский",
            "pl" => "Polski", "nl" => "Nederlands",
            _ => locale,
        };

        /// <summary>A locale shipped with a UI table (or registered by a test): it becomes selectable.</summary>
        public static void Register(string locale, IDictionary<string, string> table = null)
        {
            if (string.IsNullOrEmpty(locale)) return;
            if (!_extra.Contains(locale) && locale != Base && locale != Pseudo) _extra.Add(locale);
            if (table != null) _tables[locale] = new Dictionary<string, string>(table);
        }

        public static bool IsSupported(string locale)
        {
            if (string.IsNullOrEmpty(locale)) return false;
            if (locale == Base || locale == Pseudo || _extra.Contains(locale)) return true;
            return Table(locale).Count > 0;
        }

        /// <summary>Switch; saved for next time. An unknown locale is refused.</summary>
        public static bool SetLocale(string locale)
        {
            if (!IsSupported(locale)) return false;
            if (!_extra.Contains(locale) && locale != Base && locale != Pseudo) _extra.Add(locale);
            bool changed = Locale != locale;
            _locale = locale;
            try { PlayerPrefs.SetString(PrefsKey, locale); } catch { }
            if (changed) Changed?.Invoke(locale);
            return true;
        }

        /// <summary>The closest locale we ship for a system language: the two-letter code if it has a table, else English.</summary>
        public static string FromSystem(SystemLanguage language)
        {
            string code = language switch
            {
                SystemLanguage.French => "fr", SystemLanguage.German => "de", SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it", SystemLanguage.Portuguese => "pt", SystemLanguage.Japanese => "ja",
                SystemLanguage.Korean => "ko", SystemLanguage.ChineseSimplified => "zh-Hans", SystemLanguage.ChineseTraditional => "zh-Hant",
                SystemLanguage.Russian => "ru", SystemLanguage.Polish => "pl", SystemLanguage.Dutch => "nl",
                _ => Base,
            };
            return code != Base && IsSupported(code) ? code : Base;
        }

        /// <summary>Tests: forget the locale and the loaded tables.</summary>
        public static void Reset()
        {
            _locale = null;
            _tables.Clear();
            _extra.Clear();
        }

        /// <summary>A string for the player: the locale's, else English (dressed as the pseudo-locale when that is on).</summary>
        public static string T(string key, string english)
        {
            var locale = Locale;
            if (locale != Base && Table(locale).TryGetValue(key, out var text) && !string.IsNullOrEmpty(text)) return text;
            return locale == Pseudo ? Pseudoize(english) : english;
        }

        /// <summary>A formatted string: {0}, {1}... placed by the translation.</summary>
        public static string F(string key, string english, params object[] args)
        {
            var pattern = T(key, english);
            try { return string.Format(CultureInfo.InvariantCulture, pattern, args); }
            catch (FormatException) { return string.Format(CultureInfo.InvariantCulture, english, args); }
        }

        static Dictionary<string, string> Table(string locale)
        {
            if (_tables.TryGetValue(locale, out var t)) return t;
            t = new Dictionary<string, string>();
            TextAsset asset = null;
            try { asset = Resources.Load<TextAsset>(TablePath + locale); } catch { }
            if (asset != null)
                foreach (var row in ParseCsv(asset.text))
                    if (row.Count >= 2 && !string.IsNullOrEmpty(row[0]) && row[0] != "key") t[row[0]] = row[1];
            _tables[locale] = t;
            return t;
        }

        // ---- The pseudo-locale ---------------------------------------------------------------------------------------

        const string From = "aceinorsuyACEINORSUYdtlDTL";
        const string To = "àçéîñöŕšûýÀÇÉÎÑÖŔŠÛÝďţļĎŢĻ";
        static readonly Regex Protected = new Regex(@"\{[^}]*\}|\[[^\]]*\]|<[^>]*>");
        static readonly Regex Speaker = new Regex(@"^[A-Za-z][A-Za-z ]*:\s");

        /// <summary>
        /// English as the pseudo-locale: accented, a third longer, «bracketed». Placeholders ({0}), markup ([b]) and tags
        /// (&lt;b&gt;) are kept as they are; so is a dialogue line's "Speaker: " when <paramref name="keepSpeaker"/>.
        /// </summary>
        public static string Pseudoize(string english, bool keepSpeaker = false)
        {
            if (string.IsNullOrEmpty(english)) return english;
            var sb = new StringBuilder();
            string body = english;
            if (keepSpeaker)
            {
                var m = Speaker.Match(english);
                if (m.Success) { sb.Append(m.Value); body = english.Substring(m.Length); }
            }
            sb.Append('«');
            int at = 0, letters = 0;
            foreach (Match p in Protected.Matches(body))
            {
                letters += Accent(body, at, p.Index, sb);
                sb.Append(p.Value);
                at = p.Index + p.Length;
            }
            letters += Accent(body, at, body.Length, sb);
            int pad = (letters + 2) / 3;
            if (pad > 0) sb.Append(' ').Append('·', pad);
            sb.Append('»');
            return sb.ToString();
        }

        static int Accent(string s, int from, int to, StringBuilder sb)
        {
            int n = 0;
            for (int i = from; i < to; i++)
            {
                char c = s[i];
                int k = From.IndexOf(c);
                sb.Append(k >= 0 ? To[k] : c);
                if (char.IsLetter(c)) n++;
            }
            return n;
        }

        public static bool LooksPseudo(string s) => !string.IsNullOrEmpty(s) && s.IndexOf('«') >= 0 && s.IndexOf('»') > s.IndexOf('«');

        // ---- Tables ------------------------------------------------------------------------------------------------

        /// <summary>RFC 4180 CSV: commas, quoted fields with doubled quotes, newlines inside quotes.</summary>
        public static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else quoted = false;
                    }
                    else field.Append(c);
                    continue;
                }
                if (c == '"') quoted = true;
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(field.ToString()); field.Clear(); rows.Add(row); row = new List<string>(); }
                else field.Append(c);
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
            return rows;
        }

        public static string CsvField(string s)
        {
            if (s == null) return "";
            bool quote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 || s.StartsWith(" ") || s.EndsWith(" ");
            return quote ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        // ---- Harvest -----------------------------------------------------------------------------------------------

        static readonly Regex Call = new Regex(@"Loc\.(?:T|F)\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""");
        static readonly Regex Say = new Regex(@"\bSay\(\s*""(?:[^""\\]|\\.)*""\s*,\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""");

        /// <summary>
        /// Every <c>Loc.T/F("key", "English")</c> and UI <c>Say("element", "key", "English", ...)</c> in a source file,
        /// English unescaped. The table's source.
        /// </summary>
        public static List<(string key, string english)> Harvest(string source)
        {
            var list = new List<(string, string)>();
            var code = Regex.Replace(source ?? "", @"^[ \t]*//.*$", "", RegexOptions.Multiline);   // examples in comments are not strings
            foreach (Match m in Call.Matches(code)) list.Add((Unescape(m.Groups[1].Value), Unescape(m.Groups[2].Value)));
            foreach (Match m in Say.Matches(code)) list.Add((Unescape(m.Groups[1].Value), Unescape(m.Groups[2].Value)));
            return list;
        }

        static string Unescape(string s) => Regex.Unescape(s);
    }
}
