using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The long roll-call walked down (bounds-walk.md §2): chosen at the pit-head, taken up by each room in turn as Wren
    /// comes down, Runa ahead of her in the room whose verse is due, the Collapse woken by the third verse and walking
    /// the fourth when it falls, every room held, and Runa at the bottom with the count and the stone.
    /// </summary>
    public class HollowveinWalkPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static IEnumerator GoTo(string room)
        {
            var scene = WorldGraph.GreyboxPrefix + room;
            RoomManager.Instance.Transition(scene, "Top");
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
        }

        static FlagPresence RunaIn(string node)
        {
            var t = Object.FindObjectsByType<NpcTalker>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(n => n.StartNode == node);
            Assert.IsNotNull(t, "Runa with " + node);
            return t!.GetComponent<FlagPresence>();
        }

        /// <summary>Walk this room's verse at a quick beat, standing in every bound as it is called.</summary>
        static IEnumerator WalkTheVerse(BoundsWalks.Relay r, int leg)
        {
            var walk = Object.FindObjectsByType<BoundsWalk>(FindObjectsSortMode.None).FirstOrDefault(b => b.Relay == r);
            Assert.IsNotNull(walk, "the room walks a leg");
            Assert.AreEqual(leg, walk!.Leg);
            walk.SecondsPerBeat = 0.3f;
            var wren = Object.FindFirstObjectByType<WrenController>();
            System.Action<BoundsWalk, BoundsWalk.Bound> follow = (_, b) => wren.Teleport(b.Position);
            BoundsWalk.NameCalled += follow;
            try
            {
                yield return RouteReplay.Until(() => walk.State != BoundsWalk.Phase.Idle, 3f, "the walk taken up as she comes in");
                Assert.AreEqual(leg, walk.WholeVerse, "the verse counted across the rooms");
                Assert.AreEqual(4, walk.WholeVerses);
                Assert.AreEqual(5 - leg, RollCallSong.WalkChorus(walk.Id, walk.WholeVerse).Length, "one voice fewer each verse");
                yield return RouteReplay.Until(() => BoundsWalks.LegsWalked(GameState.World, r) == leg + 1, 15f, "verse " + (leg + 1));
            }
            finally { BoundsWalk.NameCalled -= follow; }
        }

        [UnityTest]
        public IEnumerator TheLongRollCallIsWalkedDownAndTheFightIsTheFourthVerse()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var r = BoundsWalks.FindRelay("hollowvein");
            w.Set(BoundsWalks.LearnedFlag, 1);

            yield return _replay.Talk("Runa", "Hollowvein_Runa_Walk", new[] { 0 });   // then we walk it down
            Assert.IsTrue(w.Is(r.BegunKey) && w.Is("emberdown.hollowvein_opened"));
            yield return _replay.Talk("Runa", "Hollowvein_Runa_Walk", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("I'm ahead of you")), "begun: she has gone down");

            yield return GoTo(r.Legs[0]);
            Assert.IsTrue(RunaIn("Hollowvein_Runa_Down").Standing, "Runa ahead, at the adit");
            yield return _replay.Talk("Runa", "Hollowvein_Runa_Down", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Aske's beam first")));
            yield return WalkTheVerse(r, 0);
            Assert.IsFalse(RunaIn("Hollowvein_Runa_Down").Standing, "gone on down");
            Assert.IsFalse(w.Is(r.WakeFlag));

            yield return GoTo(r.Legs[1]);
            Assert.IsTrue(RunaIn("Hollowvein_Runa_Down").Standing, "at the first gallery");
            yield return WalkTheVerse(r, 1);
            yield return GoTo(r.Legs[2]);
            yield return WalkTheVerse(r, 2);
            Assert.IsTrue(w.Is(r.WakeFlag), "the third verse walked: something wakes");

            yield return GoTo(r.Legs[3]);
            var arena = Object.FindFirstObjectByType<BossArena>();
            Assert.IsNotNull(arena);
            Assert.IsFalse(arena!.IsWaitingForFlag, "the Collapse is awake");
            Assert.IsFalse(RunaIn("Hollowvein_Runa_After").Standing, "not walked yet");
            w.Set(Bosses.FlagKey("collapse"), 1);   // the fight won
            yield return RouteReplay.Until(() => BoundsWalks.IsWalked(w, r.Legs[3]), 3f, "the fourth verse walked");
            foreach (var room in r.Legs) Assert.AreEqual(PlaceFate.Held, Places.FateOf(w, room), room + " held");
            Assert.IsTrue(RunaIn("Hollowvein_Runa_After").Standing, "Runa at the bottom");

            yield return _replay.Talk("Runa", "Hollowvein_Runa_After", new int[0]);
            Assert.IsTrue(w.Is("emberdown.hollowvein.walked") && w.Is("keystone.hollowvein"));
            _replay.Heard.Clear();
            yield return _replay.Talk("Runa", "Hollowvein_Runa_After", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("I said the number")), "and after, what she says at the pit-head");
        }

        [UnityTest]
        public IEnumerator TheCollapseSleepsUntilTheVersesAboveAreWalked()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var r = BoundsWalks.FindRelay("hollowvein");
            BoundsWalks.BeginRelay(w, r);
            yield return GoTo(r.Legs[3]);
            Assert.IsTrue(Object.FindFirstObjectByType<BossArena>()!.IsWaitingForFlag, "dropped to the bottom before the verses: nothing wakes");
            w.Set(Bosses.FlagKey("collapse"), 1);
            yield return null; yield return null;
            Assert.IsFalse(BoundsWalks.IsWalked(w, r.Legs[3]), "and nothing is walked out of turn");
        }
    }
}
