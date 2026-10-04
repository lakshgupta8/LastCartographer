#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The islands drawn (docs/design/blank-generator.md §3): every kind of island the drift can hold, the five
    /// authored ones, a generic one and a half-island, is built from its place's own kit, greyed and torn out of the
    /// white, its people in the bodies they were met in and Remnant, the crowd standing round; the art is let go with
    /// the room. Each island's picture is written to logs/islands/ to be looked at.
    /// </summary>
    public class IslandDrawingTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(Islands.ScenePrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
        }

        static IEnumerator GoTo(string scene)
        {
            RoomManager.Instance.Transition(scene, "Start");
            yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
        }

        /// <summary>The camera's view of the room as she stands at <paramref name="x"/>, written to logs/islands/.</summary>
        static IEnumerator Picture(string scene, float x, string suffix = "")
        {
            var wren = Object.FindFirstObjectByType<WrenController>();
            wren.Teleport(new Vector2(x, 0.6f));
            for (int i = 0; i < 40; i++) yield return null;
            var cam = Camera.main;
            if (cam == null) yield break;
            var rt = new RenderTexture(1280, 720, 24);
            var before = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = before;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory("logs/islands");
            File.WriteAllBytes("logs/islands/" + scene + suffix + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator EveryKindOfIslandIsDrawnAsThePlaceItWas()
        {
            yield return Boot();
            var w = GameState.World;
            // All five authored islands, a generic island and a half-island.
            w.Set("saltmarrow.dotha.decided", 2);
            w.Set("emberdown.hollowvein.buried", 1);
            Places.Release(w, "Verdance_Aldermere_2");
            w.Set("verdance.gate.inn_visited", 1);
            Places.Release(w, "Halden_Lowmarket_2");
            Places.Release(w, "Emberdown_Baths_2");
            Places.Anchor(w, "Windreach_Camp_1");
            w.Set("place.Windreach_Camp_1.weakened", 1);
            var drifts = Islands.Drifting(w);
            Assert.AreEqual(7, drifts.Count, string.Join(", ", drifts.Select(d => d.Scene)));
            Assert.IsTrue(drifts.Any(d => d.IsGeneric && !d.IsHalf) && drifts.Any(d => d.IsHalf), "a generic island and a half-island drift");

            foreach (var drift in drifts)
            {
                var look = IslandLooks.For(drift);
                Assert.IsNotNull(look, drift.Scene + " has a look");
                yield return GoTo(drift.Scene);
                var room = Room.Current;
                string at = drift.Scene + ": ";

                // The place's own strip behind the island, greyed; its far strip; the Blank above; torn out of the white.
                var mid = room.transform.Find("Paper_" + look.Mid)?.GetComponent<Renderer>();
                Assert.IsNotNull(mid, at + "the place's strip, " + look.Mid);
                Assert.AreEqual(look.MidMaterial, mid!.sharedMaterial.name, at + "drawn from its kit");
                var block = new MaterialPropertyBlock();
                mid.GetPropertyBlock(block);
                Assert.AreEqual(look.Wash, block.GetFloat("_Wash"), 1e-4f, at + "greyed as far as the island's wash");
                Assert.IsNotNull(room.transform.Find("Paper_Blank_Farther_Grey"), at + "the Blank's grey above");
                Assert.AreEqual(4, room.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("White_Mid_") || t.name.StartsWith("White_Far_")),
                    at + "torn out of the white either side, at the strip and at the far strip");
                var ground = room.transform.Find("Island").GetComponent<Renderer>();
                Assert.AreEqual(look.TileMaterial, ground.sharedMaterial.name, at + "standing on its own floor");
                foreach (var p in look.Props) Assert.IsNotNull(room.transform.Find("Prop_" + p.Name), at + "its " + p.Name);

                // Its people: the speaker in the body they were met in, the crowd round them, all of them Remnant.
                var talker = room.GetComponentInChildren<NpcTalker>();
                var sheets = talker.GetComponent<InkSheetPlayer>();
                Assert.IsNotNull(sheets, at + "the speaker is drawn");
                Assert.IsTrue(sheets.Has("idle") && sheets.Has("talk"), at + "with " + look.Speaker + "'s idle and talk");
                StringAssert.StartsWith(look.Speaker + "_", sheets.Clips[0].Sheet.name, at + "from " + look.Speaker + "'s sheets");
                Assert.AreEqual(NpcInkState.Remnant, talker.GetComponent<NpcInk>().State, at + "a Remnant");
                Assert.AreEqual(1, talker.GetComponentsInChildren<Renderer>().Length, at + "the grey quad is gone");
                var crowd = room.GetComponentsInChildren<NpcAnimator>().Where(a => a.GetComponent<NpcTalker>() == null).ToList();
                Assert.AreEqual(look.Crowd.Length, crowd.Count, at + "the crowd standing round");
                foreach (var c in crowd)
                {
                    Assert.AreEqual(look.CrowdActivity, c.Activity, at + c.name + " stands " + look.CrowdActivity);
                    Assert.AreEqual(NpcInkState.Remnant, c.GetComponent<NpcInk>().State, at + c.name + " is a Remnant");
                    Assert.Greater(Mathf.Abs(c.transform.position.x - IslandBuilder.TalkerX), 1.5f, at + c.name + " is not on top of the speaker");
                }
                // An unnamed person speaks with the face of the look they stand in, greyed (portraits.md §1).
                var face = talker.GetComponent<PortraitFace>();
                if (drift.IsGeneric)
                {
                    Assert.IsNotNull(face, at + "the talker lends its face");
                    Assert.AreEqual(look.Speaker, face!.Body, at + "the face of the look they stand in");
                    Assert.AreEqual(Portraits.Prefix + look.Speaker, face.Sheet.name, at + "the look's portrait sheet");
                    var svc = DialogueService.Instance!;
                    var view = UiRoot.Instance.GetComponent<DialogueView>();
                    NpcTalker.Talking = talker;
                    Assert.IsTrue(svc.StartNode(drift.Node), drift.Node);
                    yield return Until(() => view.IsVisible && !string.IsNullOrEmpty(view.LineText), 5f, drift.Node + "'s first line");
                    Assert.AreEqual("Remnant", view.PortraitSpeaker, at + "the Remnant has a face");
                    Assert.AreSame(face.Sheet, view.PortraitTexture, at + "and it is the look's");
                    Assert.AreEqual(1f, view.PortraitGrey, at + "in grey");
                    svc.Stop();
                    NpcTalker.Talking = null;
                    yield return null;
                }
                else Assert.IsNull(face, at + "a named speaker's face is their own");

                var held = room.GetComponent<AddressableArt.Held>();
                Assert.IsNotNull(held, at + "the room holds its art");
                Assert.Greater(held.Count, 5, at + "the kit, the floor and the sheets");
                yield return Picture(drift.Scene, -1f);
                yield return Picture(drift.Scene, -11.5f, "_edge");   // at the island's end, where the white tears in
            }

            // Leaving lets the art go with the room.
            yield return GoTo(drifts[0].Scene);
            Assert.IsFalse(SceneManager.GetSceneByName(drifts[drifts.Count - 1].Scene).isLoaded, "the last island is gone, and its handles with it");
        }
    }
}
