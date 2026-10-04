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
    /// The boss fights' loose pieces (AUD-16, docs/design/enemy-sounds.md §2b): every part a boss makes is heard or is
    /// one its owner speaks for; a part is found by its name or its name's stem; its cues are world one-shots and its
    /// loops sit under the room; a stone feather shatters high and a block of the roof crashes low.
    /// </summary>
    public class PartSoundsTests
    {
        static string Bosses => Path.Combine(Application.dataPath, "_Project/Code/World/Bosses");
        static float[] R(string id) => InkSounds.Render(id);

        static float LowShare(float[] s, float cutoff)
        {
            float g = 1f - (float)Math.Exp(-2 * Math.PI * cutoff / InkSounds.SampleRate), lp = 0f; double low = 0, all = 0;
            foreach (var x in s) { lp += (x - lp) * g; low += lp * lp; all += x * x; }
            return all <= 0 ? 0f : (float)(low / all);
        }

        [Test]
        public void EveryPartABossMakesIsHeardOrSpokenFor()
        {
            var made = Directory.GetFiles(Bosses, "*.cs")
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"BossPart\.Make\(\s*(?:i == 3 \? ""GreatRope"" : )?""([A-Za-z_]+)""").Cast<Match>().Select(m => m.Groups[1].Value))
                .Distinct().ToList();
            Assert.GreaterOrEqual(made.Count, 15, "the parts the bosses make: " + string.Join(", ", made));
            foreach (var name in made)
            {
                bool spoken = EnemySounds.SpokenFor.Any(s => name == s || (s.EndsWith("_") && name.StartsWith(s)));
                var voice = EnemySounds.PartOf(name);
                Assert.IsTrue(voice != null ^ spoken, name + ": heard, or spoken for by its owner (one, not both)");
            }
            foreach (var p in EnemySounds.Parts)
            {
                Assert.IsNotEmpty(p.What, p.Name);
                Assert.IsNotEmpty(p.Cues, p.Name + " is heard at some moment");
                Assert.IsTrue(made.Any(m => m == p.Name || (p.Name.EndsWith("_") && m.StartsWith(p.Name))), p.Name + " is a part some boss makes");
                foreach (var c in p.Cues)
                {
                    var cue = InkSounds.Of(c);
                    Assert.IsNotNull(cue, c);
                    Assert.AreEqual(AudioDirection.Voice.World, cue.Voice, c + ": a world one-shot, kept after the hits");
                    Assert.AreEqual(c == p.Loop, cue.Loop, c + (c == p.Loop ? " loops" : " is played once"));
                    if (cue.Loop) Assert.LessOrEqual(cue.Gain, 0.5f, c + ": a loop sits under the room");
                }
            }
        }

        [Test]
        public void APartIsFoundByItsNameOrItsStem()
        {
            Assert.AreEqual("Feather", EnemySounds.PartOf("Feather").Name);
            Assert.AreEqual("Strike_", EnemySounds.PartOf("Strike_3").Name, "Hale's third stone strike");
            Assert.AreEqual("Frame_", EnemySounds.PartOf("Frame_W").Name);
            Assert.IsNull(EnemySounds.PartOf("Dove_2"), "the Choir speaks for its doves");
            Assert.IsNull(EnemySounds.PartOf("Feathers"), "a name, not a prefix, unless the stem says so");
            Assert.IsNull(EnemySounds.PartOf(null));
            Assert.AreEqual("feather_shatter", EnemySounds.PartOf("Feather").Gone, "a feather meeting the floor");
            Assert.AreEqual("feather_crack", EnemySounds.PartOf("Feather").Struck, "a feather pogoed");
            Assert.AreEqual("rubble_crash", EnemySounds.PartOf("Rubble").Appear, "the block is made where it lands");
            Assert.AreEqual("surge_flow", EnemySounds.PartOf("Surge").Loop);
            Assert.AreEqual("star_walls", EnemySounds.Of("FallenStar").Events["WallRaisings"], "the Star's walls are a count it keeps");
        }

        [Test]
        public void EachPieceSoundsLikeWhatItIs()
        {
            Assert.Greater(LowShare(R("rubble_crash"), 200f), 0.3f, "a block of the roof lands low");
            Assert.Less(LowShare(R("feather_shatter"), 200f), LowShare(R("rubble_crash"), 200f), "a feather shatters higher than rubble crashes");
            Assert.Less(InkSounds.Seconds(R("feather_crack")), InkSounds.Seconds(R("feather_shatter")), "cracked is shorter than shattered");
            Assert.Greater(InkSounds.Seconds(R("fist_ring")), InkSounds.Seconds(R("rubble_break")), "iron rings on");
            Assert.Less(LowShare(R("frame_drawn"), 200f), 0.05f, "a drawn line has nothing low");
        }
    }
}
