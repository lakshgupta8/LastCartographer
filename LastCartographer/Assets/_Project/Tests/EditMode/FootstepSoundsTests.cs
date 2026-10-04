using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;
using Surface = OWSBG.Core.FootstepSounds.Surface;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's feet on the ground (AUD-12, docs/design/footsteps.md): every ground tile the paper kits draw says what a
    /// step on it is, and every region has a ground where no tile says; every surface has three takes and a landing,
    /// quiet and the first dropped; each sounds like its ground (moss nearly nothing, stone hard, wood a knock, iron a
    /// clank, water a splash); the stride keeps time with the run clip; a long fall lands louder than a hop.
    /// </summary>
    public class FootstepSoundsTests
    {
        static float[] R(string id) => InkSounds.Render(id);

        [Test]
        public void EveryKitTileAndEveryRegionSaysWhatTheGroundIs()
        {
            string env = Path.Combine(Application.dataPath, "_Project/Art/Environment");
            Assert.IsTrue(Directory.Exists(env), env);
            var tiles = Directory.GetFiles(env, "Ground_*.png", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension).Distinct().ToList();
            Assert.GreaterOrEqual(tiles.Count, 25, "the seven kits' tiles");
            foreach (var t in tiles) Assert.IsNotNull(FootstepSounds.OfMaterial("M_" + t), t + " says what a step on it is");

            Assert.AreEqual(Surface.Wood, FootstepSounds.OfMaterial("M_Ground_Boardwalk"));
            Assert.AreEqual(Surface.Wood, FootstepSounds.OfMaterial("M_Ground_Boardwalk_Weak (Instance)"), "an instance reads as its tile");
            Assert.AreEqual(Surface.Water, FootstepSounds.OfMaterial("Ground_Shallows"));
            Assert.AreEqual(Surface.Iron, FootstepSounds.OfMaterial("M_Ground_Iron"));
            Assert.AreEqual(Surface.Ash, FootstepSounds.OfMaterial("M_Ground_Ash"));
            Assert.AreEqual(Surface.Moss, FootstepSounds.OfMaterial("M_Ground_Moss"));
            Assert.AreEqual(Surface.Grass, FootstepSounds.OfMaterial("M_Ground_Turf"));
            Assert.AreEqual(Surface.Sand, FootstepSounds.OfMaterial("M_Ground_WhiteSand"));
            Assert.AreEqual(Surface.Paper, FootstepSounds.OfMaterial("M_Ground_Crayon"), "the Blank's scribbled boards are paper");
            Assert.IsNull(FootstepSounds.OfMaterial("M_Greybox"), "the greybox's grey says nothing");
            Assert.IsNull(FootstepSounds.OfMaterial("M_Ground_Nowhere"));
            Assert.IsNull(FootstepSounds.OfMaterial(null));
            Assert.IsNull(FootstepSounds.OfMaterial("Paper_Far"), "a backdrop is no ground");

            foreach (Region r in Enum.GetValues(typeof(Region))) Assert.IsTrue(Enum.IsDefined(typeof(Surface), FootstepSounds.OfRegion(r)), r.ToString());
            Assert.AreEqual(Surface.Wood, FootstepSounds.OfRegion(Region.Saltmarrow), "the coast is boards");
            Assert.AreEqual(Surface.Moss, FootstepSounds.OfRegion(Region.Verdance));
            Assert.AreEqual(Surface.Paper, FootstepSounds.OfRegion(Region.Blank), "the Blank is paper");
            Assert.AreEqual(Surface.Paper, FootstepSounds.OfRegion(null), "nowhere is paper");
        }

        [Test]
        public void EverySurfaceHasThreeTakesAndALandingQuietAndFirstDropped()
        {
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var takes = Enumerable.Range(0, FootstepSounds.Takes).Select(k => FootstepSounds.StepCue(s, k)).ToList();
                Assert.AreEqual(3, takes.Distinct().Count(), s + ": three takes");
                Assert.AreEqual(takes[0], FootstepSounds.StepCue(s, 3), "they turn");
                Assert.AreEqual(takes[2], FootstepSounds.StepCue(s, -1), "either way");
                foreach (var id in takes.Append(FootstepSounds.LandCue(s)))
                {
                    var c = InkSounds.Of(id);
                    Assert.IsNotNull(c, id);
                    Assert.AreEqual(InkSounds.Kind.Wren, c.Kind, id + " is hers");
                    Assert.AreEqual(AudioDirection.Voice.World, c.Voice, id + ": a step is the first thing dropped");
                    Assert.LessOrEqual(c.Gain, 0.5f, id + ": quiet");
                    Assert.IsFalse(c.Loop);
                }
                var a = R(takes[0]); var b = R(takes[1]);
                Assert.That(a, Is.Not.EqualTo(b), s + ": no two takes alike");
                Assert.Less(InkSounds.Seconds(a), 0.17f, s + ": a step is short");
                Assert.Greater(InkSounds.Seconds(R(FootstepSounds.LandCue(s))), InkSounds.Seconds(a), s + ": a landing is longer than a step");
            }
        }

        [Test]
        public void EachGroundSoundsLikeItself()
        {
            string Step(Surface s) => FootstepSounds.StepCue(s, 1);
            Assert.Less(InkSounds.Of(Step(Surface.Moss)).Gain, InkSounds.Of(Step(Surface.Stone)).Gain, "moss is nearly nothing");
            Assert.Less(RollCallSong.EnergyAbove(R(Step(Surface.Moss)), 800f), 0.3f, "and dull");
            Assert.Greater(RollCallSong.EnergyAbove(R(Step(Surface.Stone)), 800f), 0.6f, "stone is a hard click");
            Assert.Less(RollCallSong.EnergyAbove(R(Step(Surface.Wood)), 600f), 0.6f, "wood is a hollow knock");
            Assert.Greater(RollCallSong.EnergyAbove(R(Step(Surface.Ash)), 1500f), 0.5f, "ash crunches");
            Assert.Greater(RollCallSong.EnergyAbove(R(Step(Surface.Grass)), 1000f), 0.6f, "grass swishes");
            float iron = RollCallSong.PitchOf(R(Step(Surface.Iron)), 0, 2048, 400f, 2000f);
            Assert.That(iron, Is.InRange(380f, 900f), "iron clanks in its plate's register, low for a ring");
            Assert.Greater(InkSounds.Seconds(R(Step(Surface.Water))), InkSounds.Seconds(R(Step(Surface.Stone))), "a splash takes longer than a click");
            Assert.LessOrEqual(InkSounds.Of(Step(Surface.Paper)).Gain, 0.2f, "on paper, a dry tick and hardly that");
        }

        [Test]
        public void TheStrideKeepsTimeWithTheRunAndAFallLandsLouder()
        {
            Assert.AreEqual(4.5f, FootstepSounds.Stride(9f, 1f), 0.001f, "a one-second loop at 9 u/s: two footfalls, 4.5 units apart");
            Assert.AreEqual(FootstepSounds.DefaultStride, FootstepSounds.Stride(9f, 0f), 0.001f, "no run clip: the default");
            Assert.GreaterOrEqual(FootstepSounds.Stride(1f, 0.1f), 0.8f, "never a patter");
            Assert.AreEqual(9f / FootstepSounds.DefaultStride, 3.46f, 0.1f, "about three and a half steps a second at a full run");
            Assert.AreEqual(0.5f, FootstepSounds.LandGain(0f), 0.001f, "a hop lands at half");
            Assert.AreEqual(1f, FootstepSounds.LandGain(2f), 0.001f, "a long fall lands whole");
            Assert.Greater(FootstepSounds.LandGain(0.4f), FootstepSounds.LandGain(0.1f));
        }
    }
}
