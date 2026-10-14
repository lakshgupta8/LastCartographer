using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The things thrown at Wren (AUD-19, docs/design/enemy-sounds.md §2c): everything any code throws is a shot with
    /// its own sounds; a shot is heard leaving (or its thrower's move is), flying in a loop under the room, and ending
    /// four ways; a name no shot knows is ink; pulp is lower and wetter than ink, and an unwritten shot has nothing low.
    /// </summary>
    public class ShotSoundsTests
    {
        static string World => Path.Combine(Application.dataPath, "_Project/Code/World");
        static float[] R(string id) => InkSounds.Render(id);

        static float LowShare(float[] s, float cutoff)
        {
            float g = 1f - (float)Math.Exp(-2 * Math.PI * cutoff / InkSounds.SampleRate), lp = 0f; double low = 0, all = 0;
            foreach (var x in s) { lp += (x - lp) * g; low += lp * lp; all += x * x; }
            return all <= 0 ? 0f : (float)(low / all);
        }

        [Test]
        public void EverythingThrownIsAShotHeardLeavingFlyingAndEnding()
        {
            var thrown = Directory.GetFiles(World, "*.cs", SearchOption.AllDirectories)
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"EnemyProjectile\.Spawn\(\s*""([A-Za-z_]+)""").Cast<Match>().Select(m => m.Groups[1].Value))
                .Distinct().ToList();
            CollectionAssert.IsSupersetOf(thrown, new[] { "Pulp", "Flick" }, "the wasp's pellet and Hale's ink: " + string.Join(", ", thrown));
            foreach (var name in thrown)
                Assert.AreEqual(name, EnemySounds.ShotOf(name).Name, name + " is thrown as its own shot");
            foreach (var s in EnemySounds.Shots)
            {
                Assert.IsNotEmpty(s.What, s.Name);
                Assert.Contains(s.Name, thrown, s.Name + " is a shot something throws");
                Assert.IsNotNull(s.Fly, s.Name + " is heard in flight");
                foreach (ShotEnd how in Enum.GetValues(typeof(ShotEnd)))
                    Assert.IsNotNull(EnemySounds.EndCue(s, how), s.Name + " is heard ending " + how);
                CollectionAssert.AllItemsAreUnique(new[] { s.Her, s.Ground, s.Time, s.Unwritten }, s.Name + ": each way it ends is its own sound");
                foreach (var c in s.Cues)
                {
                    var cue = InkSounds.Of(c);
                    Assert.IsNotNull(cue, c);
                    Assert.AreEqual(AudioDirection.Voice.World, cue.Voice, c + ": a world one-shot, kept after the hits");
                    Assert.AreEqual(c == s.Fly, cue.Loop, c + (c == s.Fly ? " loops" : " is played once"));
                    if (cue.Loop) Assert.LessOrEqual(cue.Gain, 0.3f, c + ": a flight sits under the room");
                }
            }
            Assert.IsNull(EnemySounds.ShotOf("Pulp").Launch, "the wasp's spit is its leaving");
            Assert.AreEqual("flick_throw", EnemySounds.ShotOf("Flick").Launch, "Hale's flick has no move of its own to sound");
            Assert.AreEqual("pellet_land", EnemySounds.EndCue(EnemySounds.ShotOf("Pulp"), ShotEnd.Ground), "the pellet's pat on the ground, as it was");
            Assert.AreEqual(EnemySounds.ShotOf("Pulp").Unwritten, EnemySounds.ShotOf("Flick").Unwritten, "the Unwriter rubs out anything the same");
        }

        [Test]
        public void ANameNoShotKnowsIsInk()
        {
            Assert.AreEqual(EnemySounds.DefaultShot, EnemySounds.ShotOf("Shot").Name);
            Assert.AreEqual(EnemySounds.DefaultShot, EnemySounds.ShotOf(null).Name);
            Assert.AreEqual("Flick", EnemySounds.DefaultShot);
            Assert.AreEqual("Pulp", EnemySounds.ShotOf("Pulp").Name);
        }

        [Test]
        public void PulpIsLowerAndWetterThanInk()
        {
            Assert.Greater(LowShare(R("pellet_splat"), 300f), LowShare(R("flick_splash"), 300f), "a lump of pulp slaps lower than ink splashes");
            Assert.Greater(LowShare(R("pellet_fly"), 2000f), LowShare(R("flick_fly"), 2000f), "ink hisses higher than pulp whizzes");
            Assert.Less(InkSounds.Seconds(R("flick_dry")), 0.35f, "drying is soon over");
            Assert.Less(LowShare(R("shot_unwritten"), 200f), 0.05f, "rubbed out: nothing low");
            foreach (var loop in new[] { "pellet_fly", "flick_fly" })
            {
                var s = R(loop);
                float inside = 0f;
                for (int i = 1; i < s.Length; i++) inside = Math.Max(inside, Math.Abs(s[i] - s[i - 1]));
                Assert.LessOrEqual(Math.Abs(s[0] - s[s.Length - 1]), inside + 0.01f, loop + " meets itself");
            }
        }
    }
}
