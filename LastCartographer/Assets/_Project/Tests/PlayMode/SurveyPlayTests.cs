#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The survey loop in the running game (DES-02, PRG-10): a Cantor's bell erases the place and blanks its
    /// ink, re-surveying draws it back; and fast travel from a desk to a lit lamp through the atlas page.
    /// </summary>
    public class SurveyPlayTests
    {
        GameObject? _floor, _wren, _room, _vantageGo, _cantorGo, _layer;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        ScriptedInput? _input;
        FadeGroup? _group;
        Material? _litMat;
        string _lastCaption = "";

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            Captions.Shown += OnCaption;
        }

        void OnCaption(string text, float seconds) { _lastCaption = text; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Captions.Shown -= OnCaption;
            Bootstrap.SkipPrologueOverride = null;
            foreach (var go in new[] { _cantorGo, _vantageGo, _layer, _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            if (_litMat != null) Object.Destroy(_litMat);
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_"))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            ScreenFade.Clear();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        void BuildScratch()
        {
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<InstrumentBelt>();
            _ctrl.Teleport(new Vector2(-14f, 0f));

            _room = new GameObject("Room_Erase");
            var room = _room.AddComponent<Room>();
            room.RoomId = "Erase_Test";
            _group = _room.AddComponent<FadeGroup>();
            _litMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _layer = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _layer.name = "Paper_Mid";
            _layer.transform.SetParent(_room.transform, false);
            _layer.GetComponent<Renderer>().sharedMaterial = _litMat;
            _group.AddLayer(_layer.GetComponent<Renderer>(), 4);

            _vantageGo = new GameObject("Vantage_Post") { layer = Layer("Trigger") };
            _vantageGo.transform.SetParent(_room.transform, false);
            _vantageGo.transform.position = new Vector3(-8f, 0f, 0f);
            var col = _vantageGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(2.4f, 2f); col.offset = new Vector2(0f, 1f);
            var vp = _vantageGo.AddComponent<VantagePoint>();
            vp.VantageId = "Erase_Test/Post";
        }

        IEnumerator SurveyAt(Vector2 pos)
        {
            _ctrl!.Teleport(pos);
            yield return Fixed(3);
            _input!.SurveyHeld = true;
            for (int i = 0; i < 400 && !GameState.World.IsSurveyed("Erase_Test/Post"); i++) yield return new WaitForFixedUpdate();
            _input.SurveyHeld = false;
            Assert.IsTrue(GameState.World.IsSurveyed("Erase_Test/Post"), "the hold draws the vantage");
        }

        [UnityTest]
        public IEnumerator CantorTollsErasesThePlaceAndReSurveyDrawsItBack()
        {
            BuildScratch();
            var w = GameState.World;
            yield return SurveyAt(new Vector2(-8f, 0f));
            StringAssert.StartsWith("Drawn:", _lastCaption);
            Assert.IsTrue(FadeStages.Advance(w, "Erase_Test", 1));
            yield return null;
            Assert.AreEqual(1, _group!.Stage);

            _cantorGo = new GameObject("Cantor") { layer = Layer("Enemy") };
            _cantorGo.transform.SetParent(_room!.transform, false);
            _cantorGo.transform.position = new Vector3(4f, 2.6f, 0f);
            _cantorGo.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.9f);
            _cantorGo.AddComponent<Rigidbody2D>();
            var cantor = _cantorGo.AddComponent<Cantor>();
            Assert.AreEqual("Erase_Test", cantor.PlaceId, "the bell rings over the room it lives in");
            Assert.AreEqual(EnemyAnswer.Longstroke, cantor.Answer);
            yield return Fixed(3);
            Assert.IsFalse(cantor.IsRinging, "far away: it only drifts");

            int masks = _vitals!.Masks;
            _ctrl!.Teleport(new Vector2(2f, 0f));
            for (int i = 0; i < 120 && !cantor.IsRinging; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(cantor.IsRinging, "the bell rises first");
            Assert.AreEqual(masks, _vitals.Masks, "no harm during the ring");
            Assert.AreEqual(0, cantor.Tolls);
            for (int i = 0; i < 80 && cantor.Tolls == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, cantor.Tolls);
            Assert.Less(_vitals.Masks, masks, "under the bell hurts");
            Assert.IsTrue(Atlas.IsErased(w, "Erase_Test"), "the place is erased");
            Assert.IsFalse(w.IsSurveyed("Erase_Test/Post"), "the post is off the page");
            Assert.AreEqual(FadeStages.Max, _group.Stage);
            StringAssert.StartsWith("Erased:", _lastCaption);
            float t = 0f;
            while (_group.Ink > 0.001f && t < 6f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(0f, _group.Ink, 0.01f, "the ink leaves the page");
            Assert.IsFalse(_layer!.GetComponent<Renderer>().enabled, "the paper layer drops out");

            Object.Destroy(_cantorGo); _cantorGo = null;
            yield return null;
            yield return SurveyAt(new Vector2(-8f, 0f));
            Assert.IsFalse(Atlas.IsErased(w, "Erase_Test"));
            Assert.AreEqual(1, _group.Stage, "back to the stage it had");
            StringAssert.StartsWith("Drawn again:", _lastCaption);
            t = 0f;
            while (_group.Ink < FadeStages.InkFor(1) - 0.001f && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(FadeStages.InkFor(1), _group.Ink, 0.01f, "the ink draws back");
            Assert.IsTrue(_layer.GetComponent<Renderer>().enabled);
        }

        [UnityTest]
        public IEnumerator FastTravelFromTheDeskToTheLitLamp()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == "Greybox_Saltmarrow_A" && !RoomManager.Instance.IsTransitioning, 10f, "room A");
            var rm = RoomManager.Instance;
            var wren = Object.FindFirstObjectByType<WrenController>();
            var ui = UiRoot.Instance;
            Assert.IsNotNull(ui);
            var atlas = ui.GetComponent<AtlasView>();
            var journal = ui.GetComponent<JournalView>();
            Assert.IsNotNull(atlas, "the persistent scene has the atlas page");
            var w = GameState.World;

            // At the quay's desk: known now, but nothing is drawn, so nowhere to go.
            wren.Teleport(new Vector2(-4.5f, 0f));
            for (int i = 0; i < 60 && TravelPoint.Nearby == null; i++) yield return new WaitForFixedUpdate();
            Assert.IsNotNull(TravelPoint.Nearby, "standing at the desk");
            Assert.AreEqual(WaypointKind.Desk, TravelPoint.Nearby!.Kind);
            Assert.IsTrue(Atlas.IsKnown(w, "desk.Saltmarrow_A"));
            atlas.Open(wren);
            yield return null;
            Assert.IsTrue(atlas.IsOpen && journal.IsOpen && journal.IsHosted, "the journal opens on the right page");
            Assert.IsTrue(wren.Frozen);
            Assert.IsFalse(atlas.CanTravel, "nothing else is on the page");
            Assert.IsFalse(atlas.Confirm());
            var quay = atlas.Panel.Q("place-Saltmarrow_A");
            Assert.IsNotNull(quay);
            StringAssert.Contains("○ the Reedmother", AtlasView.VantageText(quay));
            StringAssert.Contains("· here", ((Label)quay.Q("here")).text);
            atlas.Close();
            Assert.IsFalse(wren.Frozen);

            // The quay drawn, the Lamp-Keeper beaten and her lamp stood under: the lighthouse is a destination.
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            Atlas.Survey(w, "Saltmarrow_Lighthouse/Lamp");
            w.Set("boss.lamp_keeper.defeated", true);
            Atlas.Discover(w, "lamp.Saltmarrow_Lighthouse");
            atlas.Open(wren);
            yield return null;
            CollectionAssert.AreEqual(new[] { "lamp.Saltmarrow_Lighthouse" }, atlas.Destinations.Select(d => d.Id).ToArray());
            StringAssert.Contains("● the Reedmother", AtlasView.VantageText(atlas.Panel.Q("place-Saltmarrow_A")));
            Assert.IsNotNull(atlas.Panel.Q("dest-lamp.Saltmarrow_Lighthouse"));
            float before = DayClock.Time(w);
            float hours = Travel.Hours(Atlas.FindWaypoint("desk.Saltmarrow_A"), Atlas.FindWaypoint("lamp.Saltmarrow_Lighthouse"));
            Assert.IsTrue(atlas.Confirm(), "J travels");
            Assert.IsFalse(atlas.IsOpen);
            yield return Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_Lighthouse" && !rm.IsTransitioning && !FastTravel.IsTravelling, 15f, "the lighthouse");
            Assert.AreEqual(hours, FastTravel.LastHours, 1e-4f, "the road along the coast");
            Assert.AreEqual(before + hours / 24f, DayClock.Time(w), 0.004f, "and the day moved on by it (PRG-21)");
            Assert.AreEqual(3f, wren.Position.x, 0.6f, "Wren stands under the lamp");
            Assert.AreEqual(0f, ScreenFade.Level, 0.01f, "the paper lifts");
            Assert.IsFalse(wren.Frozen);

            // Under the lit lamp: the way back is on the page.
            for (int i = 0; i < 60 && (TravelPoint.Nearby == null || TravelPoint.Nearby.Kind != WaypointKind.Lamp); i++) yield return new WaitForFixedUpdate();
            Assert.IsNotNull(TravelPoint.Nearby);
            Assert.AreEqual(WaypointKind.Lamp, TravelPoint.Nearby!.Kind);
            Assert.IsTrue(TravelPoint.Nearby.IsLit);
            atlas.Open(wren);
            yield return null;
            CollectionAssert.AreEqual(new[] { "desk.Saltmarrow_A" }, atlas.Destinations.Select(d => d.Id).ToArray(), "the lighthouse desk is not known yet");
            atlas.Close();
        }
    }
}
