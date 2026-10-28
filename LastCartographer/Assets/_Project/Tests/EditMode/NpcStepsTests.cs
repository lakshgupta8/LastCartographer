using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// Other birds' feet (AUD-23, docs/design/footsteps.md §2b): a bigger bird strides further and steps lower, a
    /// smaller one shorter and higher, and every one is lighter than Wren.
    /// </summary>
    public class NpcStepsTests
    {
        [Test]
        public void ABirdsStepsAreItsSize()
        {
            float sparrow = 1.6f, goose = 2.6f;
            Assert.Greater(FootstepSounds.NpcStride(goose), FootstepSounds.NpcStride(sparrow), "a goose's step is longer");
            Assert.Greater(FootstepSounds.NpcPitch(sparrow), FootstepSounds.NpcPitch(goose), "a sparrow's is higher");
            Assert.Greater(FootstepSounds.NpcGain(goose), FootstepSounds.NpcGain(sparrow), "and lighter");
            foreach (var h in new[] { 0.3f, 1f, sparrow, 2f, goose, 6f })
            {
                Assert.Less(FootstepSounds.NpcGain(h), 1f, h + ": never as loud as her own feet");
                Assert.Greater(FootstepSounds.NpcGain(h), 0f);
                Assert.GreaterOrEqual(FootstepSounds.NpcStride(h), 0.4f, h + ": a stride, not a shuffle");
                Assert.That(FootstepSounds.NpcPitch(h), Is.InRange(0.75f, 1.3f), h + ": the same ground, not a different one");
            }
        }
    }
}
