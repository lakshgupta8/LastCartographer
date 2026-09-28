using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Localisation wiring (PRG-19): the locale and its tables, the pseudo-locale, and the three things that must stay in
    /// step with what is written: every Yarn line has an id, the pseudo-locale's dialogue table has every line, and the
    /// English UI table is exactly what the code says. Also a lint: no player-facing literal skips the tables.
    /// When one of the last four fails, run -executeMethod OWSBG.Setup.LocalizationSetup.Refresh.
    /// </summary>
    public class LocalizationTests
    {
        static string ProjectDir => Path.Combine(Application.dataPath, "_Project");

        [SetUp] public void SetUp() { Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey); }
        [TearDown] public void TearDown() { Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey); }

        [Test]
        public void EnglishIsEnglishAndThePseudoLocaleIsPlainlyNot()
        {
            Assert.AreEqual(Loc.Base, Loc.Locale, "no choice saved, and no table for this machine's language: English");
            Assert.AreEqual("the ink runs out", Loc.T("hud.death", "the ink runs out"));
            Assert.AreEqual("Slot 2", Loc.F("desk.row.slot", "Slot {0}", 2));

            Assert.IsTrue(Loc.SetLocale(Loc.Pseudo));
            var p = Loc.T("x", "Hello {0}, [b]the[/b] <i>coast</i>");
            Assert.IsTrue(Loc.LooksPseudo(p), p);
            StringAssert.DoesNotContain("Hello", p, "every letter it can accent is accented");
            StringAssert.Contains("{0}", p, "placeholders are kept");
            StringAssert.Contains("[b]", p, "Yarn markup is kept as it is");
            StringAssert.Contains("[/b]", p);
            StringAssert.Contains("<i>", p, "rich text tags too");
            StringAssert.Contains("</i>", p);
            Assert.Greater(p.Length, "Hello {0}, [b]the[/b] <i>coast</i>".Length + 5, "longer, as other languages are");
            StringAssert.Contains("7", Loc.F("y", "{0} scraps", 7), "and it still formats");
            StringAssert.StartsWith("Sable: «", Loc.Pseudoize("Sable: Fair price.", keepSpeaker: true), "a dialogue line keeps its speaker, so the view still finds the name");
        }

        [Test]
        public void ATableAnswersWhatItHasAndEnglishTheRest()
        {
            Loc.Register("fr", new Dictionary<string, string>
            {
                { "hud.death", "l'encre s'épuise" },
                { "journal.purse", "Graines d'iris : {1}     Vélin : {0}" },
            });
            Assert.AreEqual(Loc.Base, Loc.FromSystem(SystemLanguage.German), "no German table: English");
            Assert.AreEqual("fr", Loc.FromSystem(SystemLanguage.French), "a French table: French");
            Assert.IsTrue(Loc.SetLocale("fr"));
            Assert.AreEqual("l'encre s'épuise", Loc.T("hud.death", "the ink runs out"));
            Assert.AreEqual("Drafting desk", Loc.T("desk.title", "Drafting desk"), "a key the table lacks shows its English");
            Assert.AreEqual("Graines d'iris : 12     Vélin : 3", Loc.F("journal.purse", "Vellum scraps: {0}     Iris seeds: {1}", 3, 12), "a translation may reorder the pieces");
            CollectionAssert.Contains(Loc.Locales, "fr");
        }

        [Test]
        public void UnknownLocalesAreRefusedAndTheChoiceIsKept()
        {
            Assert.IsFalse(Loc.SetLocale("xx"));
            Assert.AreEqual(Loc.Base, Loc.Locale);
            var seen = new List<string>();
            System.Action<string> on = seen.Add;
            Loc.Changed += on;
            try
            {
                Assert.IsTrue(Loc.SetLocale(Loc.Pseudo));
                Assert.IsTrue(Loc.SetLocale(Loc.Pseudo));
                CollectionAssert.AreEqual(new[] { Loc.Pseudo }, seen, "said once, when it changed");
            }
            finally { Loc.Changed -= on; }
            Loc.Reset();
            Assert.AreEqual(Loc.Pseudo, Loc.Locale, "the choice outlives the session");
        }

        [Test]
        public void TablesAreCsvThatRoundTrips()
        {
            var fields = new[] { "plain", "with, comma", "with \"quotes\"", "two\nlines", " spaced " };
            var line = string.Join(",", fields.Select(Loc.CsvField));
            var rows = Loc.ParseCsv("key,text\n" + line + "\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(fields, rows[1]);
        }

        // ---- In step with what is written --------------------------------------------------------------------------

        static readonly Regex LineTag = new Regex(@"#line:([0-9a-z]+)\b");

        /// <summary>Every line of dialogue and every option in the Yarn files, with where it is.</summary>
        static IEnumerable<(string file, int line, string text)> YarnContent()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectDir, "Dialogue"), "*.yarn", SearchOption.AllDirectories).OrderBy(f => f))
            {
                var all = File.ReadAllLines(f);
                bool body = false;
                for (int i = 0; i < all.Length; i++)
                {
                    var s = all[i].Trim();
                    if (s == "---") { body = true; continue; }
                    if (s == "===") { body = false; continue; }
                    if (!body || s.Length == 0 || s.StartsWith("//") || s.StartsWith("<<")) continue;
                    yield return (Path.GetFileName(f), i + 1, s);
                }
            }
        }

        [Test]
        public void EveryLineOfDialogueHasItsOwnId()
        {
            var missing = new List<string>();
            var seen = new Dictionary<string, string>();
            var twice = new List<string>();
            int n = 0;
            foreach (var (file, line, text) in YarnContent())
            {
                n++;
                var m = LineTag.Match(text);
                if (!m.Success) { missing.Add(file + ":" + line + " " + text); continue; }
                if (seen.TryGetValue(m.Groups[1].Value, out var at)) twice.Add(m.Value + " at " + at + " and " + file + ":" + line);
                else seen[m.Groups[1].Value] = file + ":" + line;
            }
            Assert.Greater(n, 800, "the whole game's dialogue was read");
            CollectionAssert.IsEmpty(missing, "every line and option carries #line: (run LocalizationSetup.Refresh)");
            CollectionAssert.IsEmpty(twice, "and no two share one");
        }

        [Test]
        public void ThePseudoLocaleHasEveryLine()
        {
            var ids = new HashSet<string>(YarnContent().Select(c => LineTag.Match(c.text)).Where(m => m.Success).Select(m => "line:" + m.Groups[1].Value));
            var path = Path.Combine(ProjectDir, "Dialogue/Localisation/" + Loc.Pseudo + ".csv");
            Assert.IsTrue(File.Exists(path), path);
            var rows = Loc.ParseCsv(File.ReadAllText(path));
            Assert.AreEqual("language", rows[0][0]);
            int idCol = rows[0].IndexOf("id"), textCol = rows[0].IndexOf("text");
            var table = rows.Skip(1).Where(r => r.Count > textCol).ToDictionary(r => r[idCol], r => r[textCol]);
            CollectionAssert.AreEquivalent(ids, table.Keys, "the pseudo table is the lines as written now (run LocalizationSetup.Refresh)");
            foreach (var kv in table) Assert.IsTrue(Loc.LooksPseudo(kv.Value), kv.Key + ": " + kv.Value);
        }

        static Dictionary<string, string> HarvestCode(out List<string> conflicts)
        {
            var keys = new Dictionary<string, string>();
            conflicts = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectDir, "Code"), "*.cs", SearchOption.AllDirectories))
                foreach (var (key, english) in Loc.Harvest(File.ReadAllText(f)))
                {
                    if (keys.TryGetValue(key, out var had) && had != english) conflicts.Add(key + ": \"" + had + "\" / \"" + english + "\"");
                    keys[key] = english;
                }
            return keys;
        }

        [Test]
        public void TheEnglishUiTableIsWhatTheCodeSays()
        {
            var code = HarvestCode(out var conflicts);
            CollectionAssert.IsEmpty(conflicts, "one key, one English");
            Assert.Greater(code.Count, 80, "the UI and the captions go through the tables");
            var rows = Loc.ParseCsv(File.ReadAllText(Path.Combine(ProjectDir, "Settings/Localization/ui." + Loc.Base + ".csv")));
            Assert.AreEqual("key", rows[0][0]);
            var table = rows.Skip(1).Where(r => r.Count >= 2).ToDictionary(r => r[0], r => r[1]);
            CollectionAssert.AreEquivalent(code.Keys, table.Keys, "ui.en.csv has every key in the code and no other (run LocalizationSetup.Refresh)");
            foreach (var kv in code) Assert.AreEqual(kv.Value, table[kv.Key], kv.Key);
        }

        [Test]
        public void NoPlayerTextSkipsTheTables()
        {
            var labels = new Regex(@"InkTheme\.Text\(\s*""[^""]*""\s*,\s*""[^""]*[A-Za-z][^""]*""");
            var assigned = new Regex(@"\.text\s*=\s*""[^""]*[A-Za-z]");
            var captions = new Regex(@"Captions\.Show\(\s*""");
            var found = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectDir, "Code"), "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    var s = lines[i];
                    if (s.TrimStart().StartsWith("//") || s.TrimStart().StartsWith("///")) continue;
                    if (labels.IsMatch(s) || assigned.IsMatch(s) || captions.IsMatch(s)) found.Add(Path.GetFileName(f) + ":" + (i + 1) + " " + s.Trim());
                }
            }
            CollectionAssert.IsEmpty(found, "a player-facing literal goes through Loc.T / Loc.F / InkTheme.Say");
        }
    }
}
