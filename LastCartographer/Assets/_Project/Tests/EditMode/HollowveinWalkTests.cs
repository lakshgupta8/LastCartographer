using System.IO;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The long roll-call walked down four rooms (bounds-walk.md §2, <see cref="BoundsWalks.Relay"/>): a verse a room, the
    /// Collapse as the fourth, one voice fewer each verse, and every room held at the end.
    /// </summary>
    public class HollowveinWalkTests
    {
        static readonly string[] Rooms = { "Emberdown_Hollow_1", "Emberdown_Hollow_2", "Emberdown_Hollow_3", "Emberdown_Hollow_4" };

        static string SceneText(string room) => File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Greybox/Greybox_" + room + ".unity"));

        [Test]
        public void ItGoesDownAVerseARoomAndTheFightIsTheFourth()
        {
            var r = BoundsWalks.FindRelay("hollowvein");
            Assert.IsNotNull(r);
            CollectionAssert.AreEqual(Rooms, r.Legs);
            Assert.AreEqual(Bosses.FlagKey("collapse"), r.FightFlag);
            Assert.IsNull(BoundsWalks.FindRelay("merrows_end"), "the other walks stay in their rooms");

            var w = new WorldState();
            Assert.IsFalse(BoundsWalks.IsLegDue(w, r, 0), "not before the pit-head");
            Assert.IsTrue(BoundsWalks.BeginRelay(w, r));
            Assert.IsTrue(BoundsWalks.IsLegDue(w, r, 0));
            Assert.IsFalse(BoundsWalks.IsLegDue(w, r, 1), "the adit first");
            Assert.IsFalse(BoundsWalks.WalkLeg(w, r, 1), "a verse out of turn walks nothing");
            Assert.AreEqual(0, BoundsWalks.LegsWalked(w, r));

            Assert.IsFalse(BoundsWalks.WalkLeg(w, r, 0));
            Assert.IsFalse(BoundsWalks.WalkLeg(w, r, 1));
            Assert.IsFalse(w.Is(r.WakeFlag), "nothing wakes before the third verse is walked");
            Assert.IsFalse(BoundsWalks.WalkLeg(w, r, 2));
            Assert.IsTrue(w.Is(r.WakeFlag), "the third verse walked: the Collapse may wake");
            foreach (var room in Rooms) Assert.IsFalse(BoundsWalks.IsWalked(w, room), "not held yet");

            Assert.IsTrue(BoundsWalks.WalkLeg(w, r, 3), "the fight won walks the fourth, and the whole");
            foreach (var room in Rooms)
            {
                Assert.IsTrue(BoundsWalks.IsWalked(w, room), room + " walked");
                Assert.AreEqual(PlaceFate.Held, Places.FateOf(w, room), room + " held");
            }
            Assert.IsFalse(BoundsWalks.IsLegDue(w, r, 3));
            Assert.IsFalse(BoundsWalks.BeginRelay(w, r), "walked once is walked");
        }

        [Test]
        public void TheChorusLosesAVoiceEachVerse()
        {
            for (int v = 0; v < 4; v++)
            {
                var chorus = RollCallSong.WalkChorus("hollowvein", v);
                Assert.AreEqual(5 - v, chorus.Length, "verse " + (v + 1));
                Assert.AreEqual("runa", chorus[0], "Runa leads every verse");
            }
            Assert.AreEqual(5, RollCallSong.WalkChorus("hollowvein").Length);
            Assert.AreEqual(RollCallSong.WalkChorus("kettils_rest").Length, RollCallSong.WalkChorus("kettils_rest", 2).Length, "Kettil's Rest keeps its voices");
        }

        [Test]
        public void TheRoomsCarryTheirLegs()
        {
            for (int i = 0; i < 3; i++)
            {
                var text = SceneText(Rooms[i]);
                StringAssert.Contains("_relayId: hollowvein", text, Rooms[i] + " walks a leg");
                StringAssert.Contains("_leg: " + i, text);
                StringAssert.Contains("m_Name: Runa_Greybox", text, "Runa stands there while its verse is due");
            }
            StringAssert.Contains("Aske's beam", SceneText(Rooms[0]), "every bound a miner's name");
            var bottom = SceneText(Rooms[3]);
            StringAssert.Contains("_requiresFlag: walk.hollowvein.wakes", bottom, "the Collapse waits for the third verse");
            StringAssert.Contains("m_Name: WalkRelay_hollowvein", bottom, "and the fight won walks the fourth");
            StringAssert.Contains("_startNode: Hollowvein_Runa_After", bottom, "Runa waits at the bottom");
            StringAssert.DoesNotContain("Hollowvein_Runa_After", SceneText(Rooms[0]), "not at the top, for nothing");
        }
    }
}
