using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>Wren's three voices (bible 2.3): a tally over the game and per region, and the one chosen most.</summary>
    public class VoicesTests
    {
        [Test]
        public void TheTallyCountsTheGameAndTheRegion()
        {
            var w = new WorldState();
            Assert.AreEqual(Voice.Surveyor, Voices.Dominant(w), "a journeyman starts as a surveyor");
            Voices.Record(w, Voice.Warden, "halden");
            Voices.Record(w, Voice.Warden, "halden");
            Voices.Record(w, Voice.Drift, "emberdown");
            Voices.Record(w, Voice.Drift, "emberdown");
            Voices.Record(w, Voice.Drift);
            Assert.AreEqual(2, Voices.Count(w, Voice.Warden));
            Assert.AreEqual(2, Voices.Count(w, Voice.Warden, "halden"));
            Assert.AreEqual(0, Voices.Count(w, Voice.Warden, "emberdown"));
            Assert.AreEqual(3, Voices.Count(w, Voice.Drift));
            Assert.AreEqual(Voice.Drift, Voices.Dominant(w));
            Assert.AreEqual(Voice.Warden, Voices.Dominant(w, "halden"));
            Assert.AreEqual("voice.halden.warden", Voices.Key(Voice.Warden, "halden"));
            Assert.IsTrue(Voices.TryParse("WARDEN", out var v) && v == Voice.Warden);
            Assert.IsFalse(Voices.TryParse("bard", out _));
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual(2, Voices.Count(back, Voice.Warden, "halden"), "the save keeps the tally");
        }
    }
}
