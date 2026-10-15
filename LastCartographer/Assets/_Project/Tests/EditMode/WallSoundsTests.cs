using System;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren on a wall and her feet in a fading place (AUD-17, docs/design/footsteps.md §4): the Talonhold's catch is short,
    /// the slide's scrape a loop that meets itself under the room, the push off a scuff and air, each on every surface a
    /// wall can be (AUD-21): talons high on a hard wall, a soft wall more its own rub than a scratch, iron screeching; a
    /// step thins a stage at a time and, from the stage the ground's drawing goes, is on the page.
    /// </summary>
    public class WallSoundsTests
    {
        static float[] R(string id) => InkSounds.Render(id);

        static FootstepSounds.Surface[] All => Enum.GetValues(typeof(FootstepSounds.Surface)).Cast<FootstepSounds.Surface>().ToArray();

        static float LowShare(float[] s, float cutoff)
        {
            float g = 1f - (float)Math.Exp(-2 * Math.PI * cutoff / InkSounds.SampleRate), lp = 0f; double low = 0, all = 0;
            foreach (var x in s) { lp += (x - lp) * g; low += lp * lp; all += x * x; }
            return all <= 0 ? 0f : (float)(low / all);
        }

        [Test]
        public void EveryWallHasACatchAScrapeAndAPushOff()
        {
            foreach (var s in All)
            {
                foreach (var id in new[] { FootstepSounds.ClingCue(s), FootstepSounds.SlideCue(s), FootstepSounds.KickCue(s) })
                {
                    var c = InkSounds.Of(id);
                    Assert.IsNotNull(c, id);
                    Assert.AreEqual(InkSounds.Kind.Wren, c.Kind, id + " is hers");
                    Assert.AreEqual(AudioDirection.Voice.World, c.Voice, id + ": ranked with her steps, not her strikes");
                }
                var slide = InkSounds.Of(FootstepSounds.SlideCue(s));
                Assert.IsTrue(slide.Loop, s + ": the scrape loops while she slides");
                Assert.LessOrEqual(slide.Gain, 0.5f, s + ": under the room");
                Assert.IsFalse(InkSounds.Of(FootstepSounds.ClingCue(s)).Loop);
                Assert.IsFalse(InkSounds.Of(FootstepSounds.KickCue(s)).Loop);
                Assert.Less(InkSounds.Seconds(R(FootstepSounds.ClingCue(s))), 0.2f, s + ": a catch, not a drag");
                var r = R(slide.Id);
                float inside = 0f;
                for (int i = 1; i < r.Length; i++) inside = Math.Max(inside, Math.Abs(r[i] - r[i - 1]));
                Assert.LessOrEqual(Math.Abs(r[0] - r[r.Length - 1]), inside + 0.01f, s + ": the scrape meets itself");
            }
            Assert.AreEqual(All.Length * 3, All.SelectMany(s => new[] { FootstepSounds.ClingCue(s), FootstepSounds.SlideCue(s), FootstepSounds.KickCue(s) }).Distinct().Count(), "each its own");
        }

        [Test]
        public void EachWallSoundsLikeWhatItIs()
        {
            foreach (var s in new[] { FootstepSounds.Surface.Stone, FootstepSounds.Surface.Paper })
                Assert.Greater(RollCallSong.PitchOf(R(FootstepSounds.ClingCue(s)), 0, 2048, 400f, 4000f), 1000f, s + ": talons are high on a hard wall");
            Assert.Greater(LowShare(R(FootstepSounds.SlideCue(FootstepSounds.Surface.Moss)), 800f), LowShare(R(FootstepSounds.SlideCue(FootstepSounds.Surface.Stone)), 800f),
                "moss is a soft rub, stone a scratch");
            Assert.Greater(FootstepSounds.WallOf(FootstepSounds.Surface.Iron).q, 2f * FootstepSounds.WallOf(FootstepSounds.Surface.Stone).q, "iron screeches: its scrape is narrow");
            foreach (var s in All) Assert.Greater(FootstepSounds.WallOf(s).hard, 0f, s + " is something to grip");
            Assert.Less(FootstepSounds.WallOf(FootstepSounds.Surface.Moss).hard, FootstepSounds.WallOf(FootstepSounds.Surface.Wood).hard);
            CollectionAssert.AreNotEqual(R(FootstepSounds.KickCue(FootstepSounds.Surface.Wood)), R(FootstepSounds.KickCue(FootstepSounds.Surface.Stone)), "the push off sounds of the wall pushed from");
        }

        [Test]
        public void AFadingPlaceThinsHerStepsAndThenTheyAreOnThePage()
        {
            Assert.AreEqual(1f, FootstepSounds.FadeGain(0), "a whole place: whole steps");
            for (int stage = 1; stage <= FadeStages.Max; stage++)
                Assert.Less(FootstepSounds.FadeGain(stage), FootstepSounds.FadeGain(stage - 1), "thinner at stage " + stage);
            Assert.Greater(FootstepSounds.FadeGain(FadeStages.Max), 0f, "an erased place is a whisper underfoot, not nothing");
            Assert.AreEqual(FootstepSounds.FadeGain(FadeStages.Max), FootstepSounds.FadeGain(FadeStages.Max + 3), "and no thinner past it");
            foreach (FootstepSounds.Surface s in Enum.GetValues(typeof(FootstepSounds.Surface)))
            {
                for (int stage = 0; stage < FootstepSounds.PaperStage; stage++)
                    Assert.AreEqual(s, FootstepSounds.UnderFade(s, stage), s + " is itself at stage " + stage);
                for (int stage = FootstepSounds.PaperStage; stage <= FadeStages.Max; stage++)
                    Assert.AreEqual(FootstepSounds.Surface.Paper, FootstepSounds.UnderFade(s, stage), s + ": the ground's drawing gone at stage " + stage + ", the page underfoot");
            }
            Assert.Less(FootstepSounds.PaperStage, FadeStages.Max, "the page shows before the place is erased");
        }
    }
}
