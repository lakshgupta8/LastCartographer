#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The gates pass in the built rooms (docs/design/gates.md): a story gate bars the way and says so until its flag,
    /// then lets her through; an ability gate opens the moment she learns the ability; a soft gap has no bar; the
    /// Threshold's Wardens lower their lances on Halvard's word; and a fall past a room's bottom costs a mask and
    /// puts her back on the last ground she stood on.
    /// </summary>
    public class GatesPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static IEnumerator Enter(string scene, string spawn)
        {
            var rm = RoomManager.Instance;
            rm.Transition(scene, spawn);
            yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 15f, scene);
            yield return null;
        }

        static RoomTransition Exit(string name) => Room.Current!.GetComponentsInChildren<RoomTransition>(true).First(t => t.name == "Transition_" + name);
        static WrenController Wren() => Object.FindFirstObjectByType<WrenController>();

        [UnityTest]
        public IEnumerator AStoryGateBarsTheWayAndSaysSoUntilItsFlag()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Halden_Lowmarket_3", "West");
            var south = Exit("To_E");
            Assert.AreEqual("Greybox_Windreach_Stones_1", south.TargetScene);
            Assert.AreEqual("act2.started", south.Flag);
            Assert.IsTrue(south.HasGate && !south.IsOpen && south.IsBarred, "the south road is shut before the act break");
            Assert.IsNotNull(south.Bar!.GetComponent<GateBar>());
            Assert.AreEqual(LayerMask.NameToLayer("Ground"), south.Bar.layer, "the bar is ground she can't pass");

            var said = new System.Collections.Generic.List<string>();
            void Heard(string text, float seconds) => said.Add(text);
            Captions.Shown += Heard;
            var wren = Wren();
            wren.Teleport(new Vector2(south.transform.position.x, 0.5f));
            for (int i = 0; i < 12; i++) yield return null;
            Captions.Shown -= Heard;
            Assert.AreEqual("Greybox_Halden_Lowmarket_3", RoomManager.Instance.CurrentRoom, "she is not let through");
            Assert.IsFalse(RoomManager.Instance.IsTransitioning);
            Assert.AreEqual(1, said.Count(s => s == "The way is shut. Not yet."), "the bar says so, once");

            wren.Teleport(new Vector2(8f, 0.5f));
            yield return null;
            GameState.World.Set("act2.started", true);
            Assert.IsTrue(south.IsOpen);
            Assert.IsFalse(south.IsBarred, "the flag opens it at once");
            wren.Teleport(new Vector2(south.transform.position.x, 0.5f));
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == "Greybox_Windreach_Stones_1" && !RoomManager.Instance.IsTransitioning, 15f, "the Steppe");
            var back = Exit("To_W");
            Assert.AreEqual("act2.started", back.Flag, "the same gate from the other side");
            Assert.IsFalse(back.IsBarred);
        }

        [UnityTest]
        public IEnumerator AnAbilityGateOpensWhenSheLearnsTheAbilityAndASoftGapNeverBars()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Emberdown_Chimneys_1", "West");
            var climb = Exit("To_Up");
            Assert.AreEqual(Ability.Talonhold, climb.Needs);
            Assert.IsFalse(climb.Soft);
            Assert.IsTrue(climb.IsBarred, "the shaft is shut without Talonhold");
            Assert.AreEqual("Not without Talonhold.", climb.ShutLine);
            Wren().GetComponent<AbilitySet>().Unlock(Ability.Talonhold);
            Assert.IsTrue(climb.IsOpen);
            Assert.IsFalse(climb.IsBarred, "Runa's lesson opens it the moment it is learned");

            yield return Enter("Greybox_Saltmarrow_BoneBridge", "West");
            var gap = Exit("To_E");
            Assert.AreEqual(Ability.Wingbeat, gap.Needs);
            Assert.IsTrue(gap.Soft);
            Assert.IsNull(gap.Bar, "a soft gap has nothing in it");
            Assert.IsTrue(gap.IsOpen, "skill may cross it; the game notices");
        }

        [UnityTest]
        public IEnumerator TheThresholdsWardensLowerTheirLancesOnHalvardsWord()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            Licence.Revoke(w);
            yield return Enter("Greybox_Greyfold_Threshold_1", "West");
            var line = Room.Current!.GetComponentsInChildren<Warden>(true).Where(x => x.StandDownFlag == "act2.halvard_third").ToList();
            Assert.AreEqual(3, line.Count, "three on the line");
            Assert.IsTrue(line.All(x => x.Hostile && !x.IsStoodDown), "unlicensed, she is hunted");

            w.Set("act2.halvard_third", true);
            Assert.IsTrue(line.All(x => x.IsStoodDown && !x.Hostile), "\"Wardens. Lances down. Nobody counts her.\"");
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(line.All(x => Mathf.Abs(x.GetComponent<Rigidbody2D>().linearVelocity.x) < 0.01f), "they stand where they are");
            Assert.IsTrue(line.All(x => x.State == Warden.Move.Patrol));
        }

        [UnityTest]
        public IEnumerator AFallPastTheRoomsBottomCostsAMaskAndReturnsHer()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Saltmarrow_BoneBridge", "West");
            var wren = Wren();
            var vitals = wren.GetComponent<WrenVitals>();
            var fall = Room.Current!.GetComponentInChildren<FallCatch>();
            Assert.IsNotNull(fall, "a catch under the room");
            Assert.Less(fall.transform.position.y, -6f, "below everything the room stands");
            yield return RouteReplay.Until(() => wren.IsGrounded && fall.SafeKnown, 5f, "her feet on the ground");
            var safe = fall.LastSafe;
            int masks = vitals.Masks;
            Assert.Greater(masks, 1);

            wren.Teleport(new Vector2(-1f, fall.transform.position.y));   // the whale's gap, and nothing under it
            yield return RouteReplay.Until(() => fall.Falls == 1, 3f, "the catch");
            yield return null;
            Assert.AreEqual(masks - 1, vitals.Masks, "a mask");
            Assert.Less(Vector2.Distance(wren.Position, safe), 1.5f, "back on the last ground she stood on");
            Assert.IsFalse(vitals.IsDead);
            Assert.AreEqual("Greybox_Saltmarrow_BoneBridge", RoomManager.Instance.CurrentRoom, "not a death: no return to the desk");
        }
    }
}
