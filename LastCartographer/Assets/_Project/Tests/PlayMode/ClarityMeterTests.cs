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
    /// The Clarity meter (PRG-18): it runs down in the white and fills again on held ground; empty, the white gives her
    /// back to the last held ground for a mask, never her last; without Clarity it gives her back at once; it grows with
    /// the story; a lost Remnant's touch takes more; and it is her lantern-radius, which the Field lantern widens, the
    /// bells hold, the Road's cobbles follow, and which lights only the Greyfold, the Blank and the white patches.
    /// </summary>
    public class ClarityMeterTests
    {
        GameObject _root = null!, _wren = null!;
        Room _room = null!;
        WrenController _ctrl = null!;
        AbilitySet _abilities = null!;
        WrenVitals _vitals = null!;
        ClarityMeter _meter = null!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }
        static IEnumerator Seconds(float s) { float t = 0f; while (t < s) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); } }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _root = new GameObject("Room_Clarity");
            _room = _root.AddComponent<Room>();
            var floor = new GameObject("Floor") { layer = Layer("Ground") };
            floor.transform.SetParent(_root.transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _abilities = _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _meter = _wren.GetComponent<ClarityMeter>();
            _ctrl.Teleport(new Vector2(-5f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            if (_wren != null) Object.Destroy(_wren);
            Lantern.Release(_root);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        UntetheredZone White(float x0, float x1) =>
            UntetheredZone.Make("White", _root.transform, new Vector2((x0 + x1) * 0.5f, 4f), new Vector2(x1 - x0, 10f));

        [UnityTest]
        public IEnumerator EveryWrenCarriesAMeter()
        {
            Assert.IsNotNull(_meter, "the controller brings the meter with it");
            Assert.AreEqual(1, _wren.GetComponents<ClarityMeter>().Length);
            yield return Fixed(3);
            Assert.AreEqual(0, _meter.Level);
            Assert.IsFalse(_meter.IsUntethered);
            Assert.AreEqual(Clarity.FullRadius(0), _meter.Radius, 0.001f);
        }

        [UnityTest]
        public IEnumerator ItRunsDownInTheWhiteAndFillsOnHeldGround()
        {
            _abilities.Unlock(Ability.Clarity);
            White(0f, 20f);
            yield return Fixed(3);
            Assert.AreEqual(1, _meter.Level);
            Assert.AreEqual(Clarity.Capacity(1), _meter.Seconds, 0.001f, "full to start");
            Assert.IsFalse(_meter.IsUntethered, "held ground");

            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Seconds(2f);
            Assert.IsTrue(_meter.IsUntethered, "in the white");
            Assert.AreEqual(Clarity.Capacity(1) - 2f, _meter.Seconds, 0.15f, "a second a second");
            Assert.AreEqual(0, _meter.Empties);

            _ctrl.Teleport(new Vector2(-5f, 0f));
            yield return Seconds(1.1f);
            Assert.IsFalse(_meter.IsUntethered);
            Assert.IsTrue(_meter.IsFull, "it fills again on held ground");
        }

        [UnityTest]
        public IEnumerator EmptyTheWhiteGivesHerBackForAMaskNeverHerLast()
        {
            _abilities.Unlock(Ability.Clarity);
            White(0f, 20f);
            yield return Fixed(5);
            Assert.AreEqual(-5f, _meter.LastTethered.x, 0.05f, "the last held ground she stood on");
            int masks = _vitals.Masks;
            int emptied = 0;
            _meter.Emptied += _ => emptied++;

            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Until(() => _meter.Empties == 1, Clarity.Capacity(1) + 1f, "the meter to empty");
            Assert.AreEqual(1, emptied);
            Assert.AreEqual(-5f, _ctrl.Position.x, 0.1f, "drawn back to held ground");
            Assert.AreEqual(masks - 1, _vitals.Masks, "for a mask");
            Assert.IsTrue(_meter.IsFull, "and the meter full again");

            _vitals.SetMaxMasks(1);
            yield return Fixed(70);   // her i-frames pass
            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Until(() => _meter.Empties == 2, Clarity.Capacity(1) + 1f, "the meter to empty again");
            Assert.AreEqual(1, _vitals.Masks, "never her last");
            Assert.IsFalse(_vitals.IsDead);
        }

        [UnityTest]
        public IEnumerator WithoutClarityTheWhiteGivesHerBackAtOnce()
        {
            White(0f, 20f);
            yield return Fixed(5);
            int masks = _vitals.Masks;
            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Fixed(3);
            Assert.AreEqual(1, _meter.Empties, "the gate: no Clarity, no white");
            Assert.AreEqual(-5f, _ctrl.Position.x, 0.1f);
            Assert.AreEqual(masks - 1, _vitals.Masks);
        }

        [UnityTest]
        public IEnumerator ItGrowsWithTheStoryAndTheWhiteHoldsHerLonger()
        {
            _abilities.Unlock(Ability.Clarity);
            White(0f, 20f);
            yield return Fixed(3);
            string? caption = null;
            System.Action<string, float> seen = (text, _) => caption = text;
            Captions.Shown += seen;
            try
            {
                int grew = 0;
                _meter.Grew += _ => grew++;
                // Level 1: eight seconds in the white is too long.
                _ctrl.Teleport(new Vector2(6f, 0f));
                yield return Seconds(8f);
                Assert.AreEqual(1, _meter.Empties, "six seconds is all she has at first");
                _ctrl.Teleport(new Vector2(-5f, 0f));
                yield return Seconds(2f);

                GameState.World.Set(Clarity.BellsFlag, true);
                yield return Fixed(2);
                Assert.AreEqual(2, _meter.Level, "the bells silenced: Clarity grows");
                Assert.AreEqual(1, grew);
                Assert.AreEqual("Clarity grows.", caption);
                Assert.AreEqual(Clarity.Capacity(2), _meter.Seconds, 0.001f, "and fills");
                Assert.AreEqual(Clarity.FullRadius(2), _meter.Radius, 0.001f, "and sees further");

                _ctrl.Teleport(new Vector2(6f, 0f));
                yield return Seconds(8f);
                Assert.AreEqual(1, _meter.Empties, "now eight seconds is not too long");
                _ctrl.Teleport(new Vector2(-5f, 0f));
                yield return Seconds(3f);

                GameState.World.Set(Clarity.Act3Flag, true);
                yield return Fixed(2);
                Assert.AreEqual(3, _meter.Level, "Act 3: grows again");
                Assert.AreEqual(2, grew);
                Assert.AreEqual(Clarity.Capacity(3), _meter.Capacity, 0.001f);
            }
            finally { Captions.Shown -= seen; }
        }

        [UnityTest]
        public IEnumerator ALostRemnantsTouchTakesClarityToo()
        {
            _abilities.Unlock(Ability.Clarity);
            White(0f, 20f);
            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Fixed(3);
            int masks = _vitals.Masks;
            var lost = LostRemnant.Spawn(_root.transform, new Vector2(7.5f, 0.8f));
            yield return Until(() => _vitals.Masks < masks, 3f, "the lost Remnant to reach her");
            yield return Fixed(1);
            Assert.Less(_meter.Seconds, Clarity.Capacity(1) - Clarity.RemnantHitSeconds,
                "more than the white alone would have taken by now (" + _meter.Seconds.ToString("F2") + " left)");
            Assert.AreEqual(masks - 1, _vitals.Masks, "and a mask");
            Assert.AreEqual("LostRemnant", lost.Family);
            Assert.IsTrue(lost.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "the quill answers it");
        }

        [UnityTest]
        public IEnumerator TheRadiusIsTheMeterTheLanternWidensItTheBellsHoldIt()
        {
            _abilities.Unlock(Ability.Clarity);
            White(0f, 20f);
            yield return Fixed(3);
            yield return null;
            Assert.AreEqual(Clarity.FullRadius(1), _meter.Radius, 0.001f, "full");

            _ctrl.Teleport(new Vector2(6f, 0f));
            yield return Seconds(4.2f);
            yield return null;
            Assert.Less(_meter.Fraction, Clarity.NarrowsBelow);
            Assert.Less(_meter.Radius, Clarity.FullRadius(1) - 0.5f, "below half the meter it narrows");
            Assert.AreEqual(Clarity.Radius(1, _meter.Fraction), _meter.Radius, 0.1f);

            _ctrl.Teleport(new Vector2(-5f, 0f));
            yield return Seconds(2f);
            _meter.Reveal(10f);   // the Field lantern
            yield return Fixed(1);
            yield return null;
            Assert.AreEqual(Clarity.FullRadius(1) + Clarity.LanternExtends, _meter.Radius, 0.001f, "the Field lantern widens it");

            Lantern.Hold(_root, 2f);
            yield return null;
            Assert.AreEqual(2f, _meter.Radius, 0.001f, "a fight that takes her radius holds it");
            Assert.AreEqual(2f, Shader.GetGlobalFloat(Lantern.RadiusGlobal), 0.001f);
            Lantern.Release(_root);
            yield return null;
            Assert.AreEqual(Clarity.FullRadius(1) + Clarity.LanternExtends, _meter.Radius, 0.001f, "and lets it go");
        }

        [UnityTest]
        public IEnumerator TheBellsHoldTheRadiusOnlyWhileTheyRing()
        {
            var kit = BossKits.Build("bells", _root.transform, new Vector2(20f, 0f));
            var bells = (HalfCathedralBells)kit.Boss;
            yield return Fixed(3);
            Assert.IsFalse(Lantern.IsHeld, "dormant bells take nothing");
            bells.BeginFight();
            yield return Fixed(1);
            Assert.IsTrue(Lantern.IsHeld, "ringing, they hold it");
            yield return null;
            Assert.AreEqual(bells.Radius, _meter.Radius, 0.001f);
            bells.ResetFight();
            Assert.IsFalse(Lantern.IsHeld, "a retry gives it back");
        }

        [UnityTest]
        public IEnumerator TheRoadsCobblesFollowHerRadius()
        {
            var kit = GauntletKits.Build("road_that_stops", _root.transform, Vector2.zero);
            _abilities.Unlock(Ability.Clarity);
            _ctrl.Teleport(new Vector2(-12.5f, 0f));
            yield return Fixed(3);
            yield return null;
            yield return Fixed(2);
            Assert.AreEqual(_meter.Radius, kit.LanternPlatforms[0].Reach, 0.001f, "the cobbles' reach is her radius");
            Assert.IsTrue(kit.LanternPlatforms[0].IsDrawn, "3.5 away, within five");
            Assert.IsFalse(kit.LanternPlatforms[1].IsDrawn);

            Lantern.Hold(_root, 2f);
            yield return null;
            yield return Fixed(2);
            Assert.IsFalse(kit.LanternPlatforms[0].IsDrawn, "a narrower radius, and the next cobble goes to outline");
            Lantern.Release(_root);
            _meter.Reveal(10f);
            yield return Fixed(1);
            yield return null;
            yield return Fixed(2);
            Assert.IsTrue(kit.LanternPlatforms[0].IsDrawn, "the Field lantern draws it back");
        }

        [UnityTest]
        public IEnumerator OnlyTheGreyfoldTheBlankAndTheWhiteAreDrawnRoundHerLantern()
        {
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(_root.transform, false);
            camGo.transform.position = new Vector3(-5f, 1f, -18f);
            camGo.AddComponent<Camera>();
            _abilities.Unlock(Ability.Clarity);

            _room.RoomId = "Greybox_Saltmarrow_A";
            yield return Seconds(0.2f);
            yield return null;
            Assert.IsFalse(_meter.IsLanternLit, "Saltmarrow keeps its colour");
            Assert.AreEqual(0f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f);

            _room.RoomId = "Greyfold_Pool_1";
            yield return Seconds(0.8f);
            yield return null;
            Assert.IsTrue(_meter.IsLanternLit, "the white shore");
            Assert.AreEqual(1f, _meter.LanternStrength, 0.001f, "the picture gets there in half a second");
            Assert.AreEqual(1f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f);
            var centre = Shader.GetGlobalVector(Lantern.CentreGlobal);
            Assert.AreEqual(0.5f, centre.x, 0.02f, "she is mid-frame");
            Assert.Greater(centre.z, 0.05f, "a radius on screen");

            _room.RoomId = "Greyfold_EdgeCamp_2";
            yield return Seconds(0.8f);
            yield return null;
            Assert.IsFalse(_meter.IsLanternLit, "the last place colour reaches by itself");
            Assert.AreEqual(0f, _meter.LanternStrength, 0.001f);

            White(-8f, -2f);
            yield return Fixed(2);
            yield return null;
            Assert.IsTrue(_meter.IsLanternLit, "a white patch is drawn round her lantern wherever it is");

            Object.Destroy(_wren);
            _wren = null!;
            yield return null;
            Assert.AreEqual(0f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f, "and nothing is left white once she's gone");
        }
    }
}
