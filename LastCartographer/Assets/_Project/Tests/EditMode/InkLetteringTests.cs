using System.Linq;
using NUnit.Framework;
using OWSBG.UI;

namespace OWSBG.Tests
{
    /// <summary>
    /// Her hand's lettering (docs/design/atlas-map.md §5): words in spaced capitals with each word's first a size
    /// larger, every letter leaning and sitting off the line a little and the same each time, the pen running dry along
    /// a word and dipped again at the next.
    /// </summary>
    public class InkLetteringTests
    {
        [Test]
        public void TheHandLettersCapitalsWithLargerInitials()
        {
            var plan = InkLettering.Plan("The Saltmarrow");
            Assert.AreEqual("THESALTMARROW", new string(plan.Select(l => l.Char).ToArray()), "capitals, the spaces between words left to the layout");
            CollectionAssert.AreEqual(new[] { 'T', 'S' }, plan.Where(l => l.Initial).Select(l => l.Char), "each word's first");
            foreach (var l in plan) Assert.AreEqual(l.Initial ? 1f : InkLettering.SmallCaps, l.Scale, l.Char + ": the initial larger, the rest small capitals");
            Assert.AreEqual(new[] { 0, 0, 0, 1 }, plan.Take(4).Select(l => l.Word).ToArray());
            Assert.IsEmpty(InkLettering.Plan(""));
            Assert.IsEmpty(InkLettering.Plan("   "));
            Assert.AreEqual("ÉCRIT", new string(InkLettering.Plan("écrit").Select(l => l.Char).ToArray()), "any language letters the same way");
        }

        [Test]
        public void EveryLetterWandersALittleAndTheSameEachTime()
        {
            var plan = InkLettering.Plan("Emberdown");
            CollectionAssert.AreEqual(plan.Select(l => (l.Lean, l.Rise)), InkLettering.Plan("Emberdown").Select(l => (l.Lean, l.Rise)), "the same hand every time");
            foreach (var l in plan)
            {
                Assert.LessOrEqual(Mathf(l.Lean), InkLettering.MaxLean, l.Char + " leans a little");
                Assert.LessOrEqual(Mathf(l.Rise), InkLettering.MaxRise, l.Char + " sits near the line");
            }
            Assert.Greater(plan.Select(l => l.Lean).Distinct().Count(), plan.Count / 2, "a hand, not a typeface: the letters lean their own ways");
            Assert.AreNotEqual(plan.Select(l => l.Lean), InkLettering.Plan("Emberdowns").Take(plan.Count).Select(l => l.Lean), "other words, other wanders");
        }

        [Test]
        public void ThePenRunsDryAlongAWordAndIsDippedAtTheNext()
        {
            var plan = InkLettering.Plan("The Saltmarrow");
            foreach (var l in plan.Where(l => l.Initial)) Assert.AreEqual(1f, l.Ink, "a fresh dip at each word");
            var salt = plan.Where(l => l.Word == 1).ToList();
            for (int i = 1; i < InkLettering.Dip; i++) Assert.Less(salt[i].Ink, salt[i - 1].Ink, "drier along the word");
            Assert.AreEqual(InkLettering.DryInk, salt[InkLettering.Dip - 1].Ink, 1e-4f, "as dry as it gets");
            Assert.AreEqual(1f, salt[InkLettering.Dip].Ink, "dipped again in a long word");
        }

        static float Mathf(float v) => v < 0 ? -v : v;
    }
}
