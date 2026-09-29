using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The localisation-ready pass (NAR-18): counts in words by each language's plural rules; lists joined as the
    /// language joins them; every catalog string with its own key and one English; the catalogs answering in the
    /// player's language, English as the fallback; and the walks and bosses the scenes carry being the catalogs' own.
    /// </summary>
    public class DataTextTests
    {
        static string ProjectDir => Path.Combine(Application.dataPath, "_Project");

        [SetUp] public void SetUp() { Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey); }
        [TearDown] public void TearDown() { Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey); Atlas.Reset(); }

        [Test]
        public void PluralsFollowEachLanguagesRules()
        {
            Assert.AreEqual(Loc.Plural.One, Loc.PluralOf("en", 1));
            Assert.AreEqual(Loc.Plural.Other, Loc.PluralOf("en", 0));
            Assert.AreEqual(Loc.Plural.One, Loc.PluralOf("fr", 0), "French: zero is singular");
            Assert.AreEqual(Loc.Plural.Other, Loc.PluralOf("fr", 2));
            Assert.AreEqual(Loc.Plural.One, Loc.PluralOf("ru", 21));
            Assert.AreEqual(Loc.Plural.Few, Loc.PluralOf("ru", 23));
            Assert.AreEqual(Loc.Plural.Many, Loc.PluralOf("ru", 12));
            Assert.AreEqual(Loc.Plural.Many, Loc.PluralOf("ru", 25));
            Assert.AreEqual(Loc.Plural.Few, Loc.PluralOf("pl", 3));
            Assert.AreEqual(Loc.Plural.Many, Loc.PluralOf("pl", 21), "Polish: 21 is many, not one");
            Assert.AreEqual(Loc.Plural.Other, Loc.PluralOf("ja", 1), "no plural in Japanese");

            Assert.AreEqual("1 scrap", Loc.P("desk.scraps", 1, "{0} scrap", "{0} scraps"));
            Assert.AreEqual("3 scraps", Loc.P("desk.scraps", 3, "{0} scrap", "{0} scraps"));
            Assert.AreEqual("0 scraps", Loc.P("desk.scraps", 0, "{0} scrap", "{0} scraps"));

            Loc.Register("ru", new Dictionary<string, string>
            {
                { "desk.scraps.one", "{0} обрывок" }, { "desk.scraps.few", "{0} обрывка" }, { "desk.scraps.many", "{0} обрывков" },
                { "travel.hours.other", "{0} ч." },
            });
            Loc.SetLocale("ru");
            Assert.AreEqual("21 обрывок", Loc.P("desk.scraps", 21, "{0} scrap", "{0} scraps"));
            Assert.AreEqual("3 обрывка", Loc.P("desk.scraps", 3, "{0} scrap", "{0} scraps"));
            Assert.AreEqual("11 обрывков", Loc.P("desk.scraps", 11, "{0} scrap", "{0} scraps"));
            Assert.AreEqual("5 ч.", Loc.P("travel.hours", 5, "{0} hour", "{0} hours"), "a table with only .other uses it for every count");
            Assert.AreEqual("2 masks", Loc.P("desk.masks.count", 2, "{0} mask", "{0} masks"), "a key the table lacks: English");

            Loc.SetLocale(Loc.Pseudo);
            var pseudo = Loc.P("desk.scraps", 1, "{0} scrap", "{0} scraps");
            Assert.IsTrue(Loc.LooksPseudo(pseudo));
            StringAssert.Contains("1", pseudo);
        }

        [Test]
        public void ListsAreJoinedAsTheLanguageJoinsThem()
        {
            Assert.AreEqual("", Loc.List(new string[0]));
            Assert.AreEqual("a", Loc.List(new[] { "a" }));
            Assert.AreEqual("a and b", Loc.List(new[] { "a", "b" }));
            Assert.AreEqual("a, b and c", Loc.List(new[] { "a", "b", "c" }));
            Loc.Register("fr", new Dictionary<string, string> { { "list.and", "{0} et {1}" } });
            Loc.SetLocale("fr");
            Assert.AreEqual("a, b et c", Loc.List(new[] { "a", "b", "c" }));
            Assert.AreEqual("the first time she saw you et the count of boats that came back",
                Memories.Describe(new[] { "isolde.first_sight", "sable.boats_back" }), "memories go through it");
        }

        [Test]
        public void EveryCatalogStringHasItsOwnKey()
        {
            var all = WorldText.Everything();
            Assert.Greater(all.Count, 150, "the catalogs are listed");
            var seen = new Dictionary<string, string>();
            foreach (var (key, english, source) in all)
            {
                StringAssert.IsMatch(@"^[a-z_]+\.", key, source);
                Assert.IsFalse(string.IsNullOrWhiteSpace(english), key);
                if (seen.TryGetValue(key, out var had)) Assert.AreEqual(had, english, "one key, one English: " + key);
                seen[key] = english;
            }
            foreach (var prefix in new[] { "place.", "vantage.", "waypoint.", "memory.", "commission.", "hub.", "boss.", "gauntlet.", "stock.", "walk.", "abilities.", "instrument.", "charter." })
                Assert.IsTrue(seen.Keys.Any(k => k.StartsWith(prefix)), prefix + " is listed");
            Assert.AreEqual(CommissionCatalog.All.Count, seen.Keys.Count(k => k.StartsWith("commission.") && k.EndsWith(".title")), "every commission's title");
            Assert.AreEqual(Bosses.All.Count, seen.Keys.Count(k => k.StartsWith("boss.") && k.EndsWith(".name")), "every boss's name");
        }

        [Test]
        public void TheCatalogsSpeakThePlayersLanguage()
        {
            var def = CommissionCatalog.All[0];
            var boss = Bosses.All[0];
            var stock = Economy.Stock[0];
            string English() => string.Join("|", new[]
            {
                Atlas.PlaceName("Saltmarrow_A"), Atlas.VantageName("Saltmarrow_A/Reedmother"), Atlas.WaypointName("desk.Saltmarrow_A"),
                InstrumentInfo.Of(InstrumentKind.CompassDart).Name, InstrumentInfo.Of(InstrumentKind.CompassDart).Blurb,
                CharterProfile.For(CharterKind.Warden).LocalName, Commissions.TitleOf(def), Commissions.StepOf(def, 0),
                Bosses.NameOf(boss.Name), Bosses.LineOf(boss.Name, 1, boss.Entry), Memories.Name("dotha.nine_songs"),
                FadeStages.Display(2), Places.Display(PlaceFate.Held), Economy.PitchOf(stock),
                BoundsWalks.BoundName("merrows_end", "Dotha's stoop"), AbilityNames.Of(Ability.Talonhold), Commissions.HubName(def.Hub),
            });

            var english = English().Split('|');
            Assert.AreEqual("The Drowned Quay", english[0]);
            Assert.AreEqual("Compass-dart", english[3]);
            Assert.AreEqual("Warden's Charter", english[5]);
            Assert.AreEqual(def.Title, english[6]);
            Assert.AreEqual(boss.Name, english[8]);
            Assert.AreEqual("washing", english[11]);
            Assert.AreEqual("Talonhold", english[15]);

            Loc.SetLocale(Loc.Pseudo);
            foreach (var s in English().Split('|')) Assert.IsTrue(Loc.LooksPseudo(s), "pseudo: " + s);

            Loc.Register("fr", new Dictionary<string, string>
            {
                { "place.Saltmarrow_A", "Le Quai noyé" }, { "instrument.CompassDart.name", "Fléchette-boussole" },
                { "charter.Warden.name", "Charte du Gardien" }, { Commissions.Key(def, "title"), "Titre" },
                { "boss." + boss.Id + ".name", "Le Boss" }, { "fade.washing", "délavé" },
                { BoundsWalks.BoundKey("merrows_end", "Dotha's stoop"), "le perron de Dotha" },
            });
            Loc.SetLocale("fr");
            var fr = English().Split('|');
            Assert.AreEqual("Le Quai noyé", fr[0]);
            Assert.AreEqual("Fléchette-boussole", fr[3]);
            Assert.AreEqual("Charte du Gardien", fr[5]);
            Assert.AreEqual("Titre", fr[6]);
            Assert.AreEqual("Le Boss", fr[8]);
            Assert.AreEqual("délavé", fr[11]);
            Assert.AreEqual("le perron de Dotha", fr[14]);
            Assert.AreEqual(english[1], fr[1], "what the table lacks stays English");
        }

        [Test]
        public void TheScenesCarryTheCatalogsWords()
        {
            // The walk ProjectSetup builds says what BoundsWalks.Words lists.
            var setup = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/Setup/ProjectSetup.cs"));
            var verses = BoundsWalks.Words.Values.SelectMany(w => w.verses).ToList();
            var bounds = BoundsWalks.Words.Values.SelectMany(w => w.bounds).ToList();
            var calls = Regex.Matches(setup, @"AddVerse\(\s*""([^""]+)""(.*?)\);", RegexOptions.Singleline);
            Assert.Greater(calls.Count, 0);
            foreach (Match m in calls)
            {
                CollectionAssert.Contains(verses, m.Groups[1].Value, "a verse title the catalog lacks");
                foreach (Match b in Regex.Matches(m.Groups[2].Value, @"\(\s*""([^""]+)""\s*,\s*new Vector2"))
                    CollectionAssert.Contains(bounds, b.Groups[1].Value, "a bound the catalog lacks");
            }

            // A boss placed in a scene is known by its sheet's name, so its name and lines have keys.
            int named = 0;
            foreach (var scene in Directory.GetFiles(Path.Combine(ProjectDir, "Scenes"), "*.unity", SearchOption.AllDirectories))
                foreach (Match m in Regex.Matches(File.ReadAllText(scene), @"_bossName: (.+)"))
                {
                    named++;
                    Assert.IsNotNull(Bosses.Named(m.Groups[1].Value.Trim()), Path.GetFileName(scene) + ": " + m.Groups[1].Value);
                }
            Assert.Greater(named, 0);

            // A desk or lamp in a scene is in the atlas's catalog.
            Atlas.Reset();
            foreach (var scene in Directory.GetFiles(Path.Combine(ProjectDir, "Scenes"), "*.unity", SearchOption.AllDirectories))
                foreach (Match m in Regex.Matches(File.ReadAllText(scene), @"_waypointId: (\S+)"))
                    Assert.IsNotNull(Atlas.FindWaypoint(m.Groups[1].Value), Path.GetFileName(scene) + ": " + m.Groups[1].Value);
        }
    }
}
