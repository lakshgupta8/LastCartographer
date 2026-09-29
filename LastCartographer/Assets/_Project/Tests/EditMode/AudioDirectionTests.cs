using System;
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
    /// The audio direction (AUD-01, docs/design/audio-direction.md): every region scored to the bible's mood, every
    /// rhythm in the game on its region's beat, the roll-call's forms, and the combat rules held against the mix
    /// and the telegraph floors.
    /// </summary>
    public class AudioDirectionTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void EveryRegionIsScoredToTheBiblesMood()
        {
            var bible = File.ReadAllText(Path.Combine(RepoRoot, "docs/story/story-bible.md"));
            foreach (Region r in Enum.GetValues(typeof(Region)))
            {
                var s = AudioDirection.Of(r);
                Assert.That(s.Beat, Is.InRange(0.4f, 2f), r + "'s beat");
                Assert.That(s.Silence, Is.InRange(0f, 0.9f), r + " is scored some of the time");
                Assert.GreaterOrEqual(s.Instruments.Length, 3, r + "'s instruments");
                Assert.GreaterOrEqual(s.AmbienceLayers.Length, 2, r + "'s ambience");
                Assert.IsNotEmpty(s.Mode, r.ToString()); Assert.IsNotEmpty(s.Brief, r.ToString());
                if (r != Region.Blank) StringAssert.Contains("**Mood:** " + s.Mood, bible, r + "'s mood is the bible's");
            }
            var silentest = AudioDirection.All.OrderByDescending(s => s.Silence).First();
            Assert.AreEqual(Region.Verdance, silentest.Region, "the Verdance is near-silent (bible 4.3)");
            Assert.AreEqual(Region.Halden, AudioDirection.All.OrderBy(s => s.Beat).First().Region, "Halden's clock is the fastest");
            Assert.AreEqual(100f, AudioDirection.Of(Region.Halden).Bpm, 0.01f);
        }

        [Test]
        public void EveryRhythmInTheGameIsOnItsRegionsBeat()
        {
            // The Complete Survey's three phases are the three regions its inks are named for.
            var go = new GameObject("cs");
            try
            {
                go.AddComponent<Rigidbody2D>(); go.AddComponent<BoxCollider2D>();   // what an enemy requires
                var cs = go.AddComponent<CompleteSurvey>();
                var regions = new[] { Region.Saltmarrow, Region.Emberdown, Region.Halden };
                for (int i = 0; i < regions.Length; i++)
                {
                    StringAssert.StartsWith(regions[i].ToString(), CompleteSurvey.Inks[i]);
                    Assert.AreEqual(AudioDirection.BeatOf(regions[i]), cs.beatSeconds[i], 1e-4f, CompleteSurvey.Inks[i]);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            var go2 = new GameObject("collapse");   // one boss to an object
            try
            {
                go2.AddComponent<Rigidbody2D>(); go2.AddComponent<BoxCollider2D>();
                var collapse = go2.AddComponent<Collapse>();
                Assert.AreEqual(AudioDirection.BeatOf(Region.Emberdown), collapse.beatSeconds, 1e-4f, "the Collapse keeps Emberdown's time");
            }
            finally { UnityEngine.Object.DestroyImmediate(go2); }

            // The Merrow's End walk, as the scene sets it.
            var scene = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scenes/Greybox/Greybox_Saltmarrow_B.unity"));
            var m = Regex.Match(scene, @"_secondsPerBeat: ([0-9.]+)");
            Assert.IsTrue(m.Success, "the walk's beat is in the scene");
            float walk = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.IsTrue(AudioDirection.OnGrid(walk, Region.Saltmarrow), "Merrow's End walks on whole Saltmarrow beats: " + walk);

            // Smudges: a beat drawn, a beat undrawn, wherever they are.
            foreach (Region r in Enum.GetValues(typeof(Region)))
            {
                var (drawn, undrawn) = AudioDirection.SmudgeCycle(r);
                Assert.AreEqual(AudioDirection.BeatOf(r), drawn); Assert.AreEqual(AudioDirection.BeatOf(r), undrawn);
            }

            Assert.IsTrue(AudioDirection.OnGrid(2.4f, Region.Emberdown), "three of Emberdown's");
            Assert.IsFalse(AudioDirection.OnGrid(3.5f, Region.Saltmarrow), "the old walk was off the beat");
            Assert.IsFalse(AudioDirection.OnGrid(0.3f, Region.Saltmarrow), "less than a beat is no beat");
            Assert.AreEqual(0.9f, AudioDirection.BeatOfScene("Greybox_Saltmarrow_B").Value, 1e-4f);
            Assert.IsNull(AudioDirection.BeatOfScene("Feel_Course"));
        }

        [Test]
        public void TheRollCallIsOneTuneInEveryForm()
        {
            var phrase = AudioDirection.RollCall.Phrase;
            Assert.AreEqual(BoundsWalk.CallAhead, AudioDirection.RollCall.PickupBeats, "the name is called half a beat ahead, as the walk calls it");
            float body = AudioDirection.RollCall.Beats(phrase) - AudioDirection.RollCall.PickupBeats;
            Assert.AreEqual(Math.Round(body), body, 1e-4, "after the pickup, whole beats");
            Assert.AreEqual(0, phrase.Last().Pitch, "the answer comes home to the tonic");
            Assert.AreEqual(7, AudioDirection.RollCall.Name[0].Pitch, "the name on the reciting tone");
            foreach (var n in phrase) Assert.That(n.Pitch, Is.InRange(0, 12), "within an octave: anyone can sing it");

            CollectionAssert.AreEqual(phrase, AudioDirection.RollCall.Reversed(AudioDirection.RollCall.Reversed(phrase)));
            CollectionAssert.AreEqual(phrase, AudioDirection.RollCall.Inverted(AudioDirection.RollCall.Inverted(phrase)));
            Assert.AreEqual(0, AudioDirection.RollCall.Reversed(phrase)[0].Pitch, "the Blank's form starts where the tune ends");
            var whale = AudioDirection.RollCall.Augmented(phrase, 2f);
            Assert.AreEqual(2f * AudioDirection.RollCall.Beats(phrase), AudioDirection.RollCall.Beats(whale), 1e-4f);
            Assert.AreEqual(2f * AudioDirection.BeatOf(Region.Saltmarrow), AudioDirection.BeatOf(Region.Blank), 1e-4f,
                "faded things sing at half speed: the whale's roll-call is Saltmarrow's at the Blank's beat");

            var uses = AudioDirection.RollCallUses;
            Assert.AreEqual(1, uses.Count(u => u.Who == "everyone"), "only the true ending has everyone");
            StringAssert.Contains("9.2", uses.Single(u => u.Who == "everyone").Where);
            StringAssert.Contains("reversed", uses.Single(u => u.Where.StartsWith("The Blank")).Form);
            StringAssert.Contains("augmented ×2", uses.Single(u => u.Who == "the faded whale").Form);
            StringAssert.Contains("one voice fewer", uses.Single(u => u.Where.StartsWith("Hollowvein")).Form);
        }

        [Test]
        public void TheCombatRulesHoldAgainstTheMixAndTheFloors()
        {
            // A tell for every kind of attack, by the same name.
            CollectionAssert.AreEquivalent(Enum.GetNames(typeof(AttackKind)), Enum.GetNames(typeof(AudioDirection.Tell)));
            foreach (AttackKind k in Enum.GetValues(typeof(AttackKind)))
                Assert.AreEqual(k.ToString(), AudioDirection.TellOf(k.ToString()).ToString());
            Assert.AreEqual(AudioDirection.Tell.Strike, AudioDirection.TellOf("Nonsense"));

            // No tell outlasts the shortest read it could announce.
            int floor = Enumerable.Range(1, 4).Min(t => Tuning.TelegraphFloor(t));
            foreach (AudioDirection.Tell t in Enum.GetValues(typeof(AudioDirection.Tell)))
                Assert.LessOrEqual(AudioDirection.TellFrames(t), floor, t + " is heard within the fastest read");

            // The tells ride the Sfx bus: nothing but a pause lowers it far, and no duck lowers it at all.
            foreach (var s in Mix.Snapshots)
            {
                if (s == Mix.Snapshot.Paused) continue;
                Assert.GreaterOrEqual(Mix.Of(s).Gain[(int)Mix.Bus.Sfx], AudioDirection.TellBusFloor, s + " keeps the tells");
            }
            foreach (var d in Mix.Ducks)
            {
                Assert.AreEqual(1f, Mix.Of(d).Floor[(int)Mix.Bus.Sfx], d + " never ducks a tell");
                if (d != Mix.Duck.Line) Assert.AreEqual(1f, Mix.Of(d).Floor[(int)Mix.Bus.Dialogue], d + ": a fight never talks over a voice");
            }

            var p = AudioDirection.Priority;
            Assert.AreEqual(p.Length, p.Distinct().Count());
            Assert.AreEqual("telegraph tells", p[0], "the read is kept first");
            Assert.AreEqual("music", p[p.Length - 1], "the music gives way first");
            Assert.Greater(AudioDirection.VoiceLimit, 8);
        }

        [Test]
        public void AFadeTakesTheAmbienceAwayALayerAtATime()
        {
            foreach (Region r in Enum.GetValues(typeof(Region)))
            {
                int n = AudioDirection.Of(r).AmbienceLayers.Length;
                Assert.AreEqual(n, AudioDirection.AmbienceLayersAt(r, 0), r + " whole");
                int last = n;
                for (int stage = 1; stage < FadeStages.Max; stage++)
                {
                    int now = AudioDirection.AmbienceLayersAt(r, stage);
                    Assert.LessOrEqual(now, last, r + " thins at stage " + stage);
                    Assert.GreaterOrEqual(now, 1, r + " keeps a layer till it's gone");
                    last = now;
                }
                Assert.AreEqual(0, AudioDirection.AmbienceLayersAt(r, FadeStages.Max), r + " erased is silent");
            }
            Assert.AreEqual(4, AudioDirection.AmbienceLayersAt(Region.Saltmarrow, 1), "one layer a stage");
        }
    }
}
