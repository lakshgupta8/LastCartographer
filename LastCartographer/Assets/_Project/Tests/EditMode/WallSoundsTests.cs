using System;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren on a wall and her feet in a fading place (AUD-17, docs/design/footsteps.md §4): the Talonhold's catch is short
    /// and high, the slide's scrape a loop that meets itself under the room, the push off a scuff and air; a step thins a
    /// stage at a time and, from the stage the ground's drawing goes, is on the page.
    /// </summary>
    public class WallSoundsTests
    {
        static float[] R(string id) => InkSounds.Render(id);

        [Test]
        public void TheWallHasACatchAScrapeAndAPushOff()
        {
            foreach (var id in new[] { FootstepSounds.ClingCue, FootstepSounds.SlideCue, FootstepSounds.KickCue })
            {
                var c = InkSounds.Of(id);
                Assert.IsNotNull(c, id);
                Assert.AreEqual(InkSounds.Kind.Wren, c.Kind, id + " is hers");
                Assert.AreEqual(AudioDirection.Voice.World, c.Voice, id + ": ranked with her steps, not her strikes");
            }
            Assert.IsTrue(InkSounds.Of(FootstepSounds.SlideCue).Loop, "the scrape loops while she slides");
            Assert.LessOrEqual(InkSounds.Of(FootstepSounds.SlideCue).Gain, 0.5f, "under the room");
            Assert.IsFalse(InkSounds.Of(FootstepSounds.ClingCue).Loop);
            Assert.Less(InkSounds.Seconds(R(FootstepSounds.ClingCue)), 0.2f, "a catch, not a drag");
            Assert.Greater(RollCallSong.PitchOf(R(FootstepSounds.ClingCue), 0, 2048, 400f, 4000f), 1000f, "talons are high");
            var slide = R(FootstepSounds.SlideCue);
            float inside = 0f;
            for (int i = 1; i < slide.Length; i++) inside = Math.Max(inside, Math.Abs(slide[i] - slide[i - 1]));
            Assert.LessOrEqual(Math.Abs(slide[0] - slide[slide.Length - 1]), inside + 0.01f, "the scrape meets itself");
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
