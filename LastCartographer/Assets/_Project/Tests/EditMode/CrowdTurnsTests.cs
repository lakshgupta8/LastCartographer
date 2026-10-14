using NUnit.Framework;
using OWSBG.Narrative;

namespace OWSBG.Tests
{
    /// <summary>
    /// A crowd that turns to watch Wren go by (docs/design/blank-generator.md §3): within reach they face her, either
    /// side; once watching they hold on until she is further off than she came in; then they turn back the way they
    /// were stood; the nearer she is, the sooner they turn.
    /// </summary>
    public class CrowdTurnsTests
    {
        [Test]
        public void TheyFaceHerWithinReachAndTurnBackWhenSheHasGone()
        {
            Assert.AreEqual((1, true), CrowdTurns.Want(-1, 2f, false), "she is to their right: they turn right");
            Assert.AreEqual((-1, true), CrowdTurns.Want(1, -2f, false), "to their left: they turn left");
            Assert.AreEqual((1, true), CrowdTurns.Want(1, CrowdTurns.Reach, false), "at the edge of reach");
            Assert.AreEqual((-1, false), CrowdTurns.Want(-1, CrowdTurns.Reach + 0.5f, false), "not yet near enough: as they were stood");
            Assert.AreEqual((1, true), CrowdTurns.Want(-1, CrowdTurns.Reach + 0.5f, true), "once watching, they hold on a little further");
            Assert.AreEqual((-1, false), CrowdTurns.Want(-1, CrowdTurns.LetGo + 0.1f, true), "and let her go past that");
            Assert.Greater(CrowdTurns.LetGo, CrowdTurns.Reach, "no flicker at the edge");
        }

        [Test]
        public void TheNearestTurnFirst()
        {
            Assert.Less(CrowdTurns.Beat(0f, 1f), CrowdTurns.Beat(0f, 5f), "the nearer she is, the sooner");
            Assert.Less(CrowdTurns.Beat(0f, 2f), CrowdTurns.Beat(CrowdTurns.MaxOwnBeat, 2f), "and each has a beat of their own");
            Assert.Less(CrowdTurns.Beat(CrowdTurns.MaxOwnBeat, CrowdTurns.LetGo), 1f, "but nobody takes a second about it");
        }
    }
}
