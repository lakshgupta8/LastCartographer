using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.Tests
{
    /// <summary>
    /// The bosses' own voices (AUD-15, docs/design/enemy-sounds.md §2a): every boss is heard doing what it does, never
    /// reading (no move on a clip the fight telegraphs with); its events are counts it keeps; the Bells toll in the
    /// Greyfold's D with the great bell an octave under; a few die their own way.
    /// </summary>
    public class BossSoundsTests
    {
        static IEnumerable<Type> BossTypes => typeof(Boss).Assembly.GetTypes().Where(t => typeof(Boss).IsAssignableFrom(t) && !t.IsAbstract);
        static float[] R(string id) => InkSounds.Render(id);

        /// <summary>The clips each boss telegraphs with (its Clip's Move.Telegraph arm): the read, whose sound is the tell's.</summary>
        static readonly Dictionary<string, string[]> Reads = new Dictionary<string, string[]>
        {
            ["Collapse"] = new[] { "rumble", "reach", "surge" },
            ["Gatekeeper"] = new[] { "shake", "telegraph", "fly" },
            ["Hale"] = new[] { "sight", "call", "telegraph" },
            ["Halvard"] = new[] { "survey", "call", "aim", "telegraph" },
            ["Voss"] = new[] { "anchor", "telegraph" },
            ["FallenStar"] = new[] { "raise", "telegraph", "walk" },
            ["CorrasDrawing"] = new[] { "lift", "telegraph", "lift_outline", "telegraph_outline" },
            ["ReedmotherBrood"] = new[] { "call", "telegraph" },
        };
        static string[] ReadsOf(string family) => Reads.TryGetValue(family, out var r) ? r : new[] { "telegraph" };

        /// <summary>A sound's strength at one frequency (Goertzel): the tests' tuning fork.</summary>
        static double Power(float[] s, double hz, int count = 12000)
        {
            double w = 2 * Math.PI * hz / InkSounds.SampleRate, k = 2 * Math.Cos(w), a = 0, b = 0;
            for (int i = 0; i < Math.Min(count, s.Length); i++) { double c = s[i] + k * a - b; b = a; a = c; }
            return a * a + b * b - k * a * b;
        }

        /// <summary>The share of a sound's energy under a cutoff, through a one-pole low-pass.</summary>
        static float LowShare(float[] s, float cutoff)
        {
            float g = 1f - (float)Math.Exp(-2 * Math.PI * cutoff / InkSounds.SampleRate), lp = 0f; double low = 0, all = 0;
            foreach (var x in s) { lp += (x - lp) * g; low += lp * lp; all += x * x; }
            return all <= 0 ? 0f : (float)(low / all);
        }

        /// <summary>Cues a boss shares with the plain enemies: a Warden's lance is a Warden's, a Cantor dove's toll a Cantor's.</summary>
        static readonly string[] Shared = { "warden_thrust", "warden_measure", "cantor_toll", "ember_crackle" };

        [Test]
        public void EveryBossIsHeardDoingWhatItDoesNeverReading()
        {
            var types = BossTypes.ToList();
            Assert.GreaterOrEqual(types.Count, 15, "fifteen bosses");
            foreach (var t in types)
            {
                var v = EnemySounds.Of(t.Name);
                Assert.IsTrue(EnemySounds.Has(t.Name), t.Name + " has a voice");
                var own = v.Moves.Values.Concat(v.Loops.Values).Concat(v.Events.Values).Distinct().ToList();
                if (t.Name != "CompleteSurvey") Assert.IsNotEmpty(own, t.Name + " is heard doing something, not only struck");
                foreach (var clip in v.Moves.Keys)
                    CollectionAssert.DoesNotContain(ReadsOf(t.Name), clip, t.Name + ": " + clip + " is a read; the tell is its sound");
                if (t.Name == "Collapse") Assert.AreEqual("collapse_fall", v.Moves["shake"], "the Collapse's shake is the fall itself, not a read");
                foreach (var kv in v.Events)
                {
                    var p = t.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                    Assert.IsNotNull(p, t.Name + " keeps a count named " + kv.Key);
                    Assert.AreEqual(typeof(int), p.PropertyType, t.Name + "." + kv.Key + " is a count");
                    var c = InkSounds.Of(kv.Value);
                    Assert.AreEqual(AudioDirection.Voice.World, c.Voice, kv.Value + ": an event is a world one-shot, as a move is");
                    Assert.IsFalse(c.Loop);
                }
                if (v.OwnDeath != null)
                {
                    Assert.AreEqual(v.OwnDeath, v.Death);
                    Assert.AreEqual(AudioDirection.Voice.Enemy, InkSounds.Of(v.Death).Voice, t.Name + "'s own death is still an enemy death");
                }
            }
            // A boss's cues are its own, but for the few it shares with what it is (a Warden's lance, a Cantor's toll).
            var plain = EnemySounds.Voices.Where(v => types.All(t => t.Name != v.Family)).SelectMany(v => v.Cues).ToHashSet();
            foreach (var t in types)
            {
                var v = EnemySounds.Of(t.Name);
                foreach (var c in v.Moves.Values.Concat(v.Loops.Values).Concat(v.Events.Values))
                    if (plain.Contains(c)) CollectionAssert.Contains(Shared, c, t.Name + " borrows " + c);
            }
        }

        [Test]
        public void EachBossSoundsLikeWhatItIs()
        {
            Assert.AreEqual(EnemySounds.Material.Earth, EnemySounds.Of("Gatekeeper").Material, "the Gatekeeper is stone");
            Assert.AreEqual(EnemySounds.Material.Wing, EnemySounds.Of("LampKeeper").Material, "the Lamp-Keeper is a gannet");
            Assert.AreEqual("bells_toll", EnemySounds.Of("HalfCathedralBells").Events["Rings"], "a ring is heard as it lands, taking the light");
            Assert.AreEqual("bells_cut", EnemySounds.Of("HalfCathedralBells").Events["Cuts"]);
            Assert.AreEqual("brann_shift", EnemySounds.Of("Brann").Events["Shifts"], "the furnace's floor shifting");
            Assert.AreEqual("corra_redraw", EnemySounds.Of("CorrasDrawing").Events["LimbsRedrawn"]);
            Assert.AreEqual("corra_line", EnemySounds.MoveCue("CorrasDrawing", "swipe_outline"), "in outline, a thin pencil line");
            Assert.AreEqual("oriel_strike", EnemySounds.MoveCue("Oriel", "strike3"));
            Assert.AreEqual("gate_fly", EnemySounds.LoopCue("Gatekeeper", "pass"), "the wingbeats go on through a pass");

            // The Bells: a tower bell in the Greyfold's D, the great bell an octave under it, longer; the last hum sags.
            Assert.AreEqual("bells_toll_p3", EnemySounds.PhaseCue("bells_toll", 3), "the great bell in phase 3");
            Assert.AreEqual("bells_toll", EnemySounds.PhaseCue("bells_toll", 1));
            Assert.AreEqual("bells_cut", EnemySounds.PhaseCue("bells_cut", 3), "a cue with no phase take is itself");
            var toll = R("bells_toll"); var great = R("bells_toll_p3");
            double d4 = EnemySounds.NaveBellHz;
            Assert.AreEqual(EnemySounds.CantorHz / 4f, d4, 0.01, "the nave's bell: the Cantor's D, two octaves down");
            var semis = Enumerable.Range(-6, 13).Select(k => d4 * Math.Pow(2, k / 12.0)).ToArray();
            Assert.AreEqual(d4, semis.OrderByDescending(hz => Power(toll, hz)).First(), 0.01, "the toll strikes in D");
            Assert.Greater(Power(great, d4 / 2), Power(great, d4), "the great bell an octave under");
            Assert.Greater(Power(toll, d4), Power(toll, d4 / 2));
            Assert.Greater(InkSounds.Seconds(great), InkSounds.Seconds(toll), "the great bell rings longer");
            Assert.Greater(InkSounds.Seconds(toll), 2f, "a tower bell, not a handbell");
            Assert.Greater(InkSounds.Seconds(R("bells_death")), 2.5f, "the last hum let go slowly");
            Assert.Less(InkSounds.Seconds(R("bell_choke")), InkSounds.Seconds(R("bell_hurt")), "a bell choked off is shorter than one struck");

            // Heavy things are low, light things are short; the Star falls longest.
            foreach (var heavy in new[] { "gate_land", "star_slam", "star_fall", "collapse_fall" })
                Assert.Greater(LowShare(R(heavy), 200f), 0.3f, heavy + ": a heavy thing is low");
            Assert.Less(LowShare(R("corra_line"), 200f), 0.05f, "a pencil line has nothing low");
            Assert.Greater(InkSounds.Seconds(R("star_fall")), InkSounds.Seconds(R("ember_death")) * 2f, "the Star's fall is its own, and long");
            Assert.Less(InkSounds.Seconds(R("corra_small")), InkSounds.Seconds(R("corra_redraw")), "the small one is drawn back quicker");
            Assert.Less(InkSounds.Of("archivist_swoop").Gain, 0.5f, "an owl's swoop is almost nothing");
            foreach (var loop in new[] { "lamp_beam", "brann_hold", "oriel_bind", "gate_fly", "star_walk", "star_burn", "corra_scribble", "archivist_draw" })
                Assert.IsTrue(InkSounds.Of(loop).Loop, loop + " loops");
        }
    }
}
