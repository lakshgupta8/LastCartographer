using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// A townsfolk drawn from sheets (CHR-11): at a post it shows the post's activity when a clip is named for it,
    /// talking wins, the schedule's walk shows the walk, a post without a clip of its own idles; and its colour
    /// follows its place: drawn while the place stands, the fills washing as the stage climbs, the ink removed
    /// when the place is let go.
    /// </summary>
    public class NpcAnimatorTests
    {
        const string Place = "Npc_Anim_Test";
        GameObject _floor, _room, _npc;
        NpcTalker _talker;
        NpcSchedule _schedule;
        NpcAnimator _anim;
        NpcInk _ink;
        Renderer _renderer;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        static readonly int WashId = Shader.PropertyToID("_Wash");
        static readonly int LineFadeId = Shader.PropertyToID("_LineFade");

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        SheetClip Clip(string name, int frames)
        {
            var tex = new Texture2D(frames * 4, 4);
            _textures.Add(tex);
            return new SheetClip { Name = name, Sheet = tex, Frames = frames, Fps = 12f, Loop = true };
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            NpcTalker.Talking = null;
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _room = new GameObject("Room_NpcAnim");
            _room.AddComponent<Room>().RoomId = Place;
            _npc = new GameObject("Sable") { layer = Layer("Trigger") };
            _npc.transform.SetParent(_room.transform, false);
            _npc.transform.position = new Vector3(0f, 0f, 0f);
            _npc.AddComponent<BoxCollider2D>().isTrigger = true;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_npc.transform, false);
            quad.transform.localScale = new Vector3(2f, 2f, 1f);
            quad.transform.localPosition = new Vector3(0f, 1f, 0f);
            _renderer = quad.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));

            _talker = _npc.AddComponent<NpcTalker>();
            _talker.StartNode = "Home";
            _schedule = _npc.AddComponent<NpcSchedule>();
            _schedule.WalkSpeed = 6f;
            _schedule.AddPost(DayPhase.Day, new Vector2(-4f, 0f), "", "mending nets", 1);
            _schedule.AddPost(DayPhase.Dusk, new Vector2(4f, 0f), "", "singing to the water", 1);
            _npc.AddComponent<InkSheetPlayer>().Configure(_renderer, new[] { Clip("idle", 8), Clip("talk", 6), Clip("walk", 8), Clip("asleep", 4), Clip("mending", 8) });
            _anim = _npc.AddComponent<NpcAnimator>();
            _ink = _npc.AddComponent<NpcInk>();
        }

        [TearDown]
        public void TearDown()
        {
            NpcTalker.Talking = null;
            foreach (var go in new[] { _room, _floor }) if (go != null) Object.Destroy(go);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        float Prop(int id)
        {
            var mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(mpb);
            return mpb.HasFloat(id) ? mpb.GetFloat(id) : 0f;
        }

        [UnityTest]
        public IEnumerator ThePostTalkingAndTheWalkPickTheClip()
        {
            DayClock.SetPhase(GameState.World, DayPhase.Day);
            yield return null; yield return null; yield return null;
            Assert.AreEqual("mending nets", _schedule.Activity, "at the day post");
            Assert.AreEqual("mending", _anim.Clip, "the post's activity has a clip of its own");

            NpcTalker.Talking = _talker;
            yield return null;
            Assert.AreEqual("talk", _anim.Clip, "talking wins");
            NpcTalker.Talking = null;
            yield return null;
            Assert.AreEqual("mending", _anim.Clip, "back to the nets");

            DayClock.SetPhase(GameState.World, DayPhase.Dusk);
            yield return null; yield return null;
            Assert.IsTrue(_schedule.IsWalking, "dusk: off to the water");
            Assert.AreEqual("walk", _anim.Clip, "the walk between posts");
            float t = 0f;
            while (_schedule.IsWalking && t < 6f) { t += Time.deltaTime; yield return null; }
            yield return null;
            Assert.AreEqual("singing to the water", _schedule.Activity);
            Assert.AreEqual("idle", _anim.Clip, "no singing clip on this bird: idle at the post");
        }

        [UnityTest]
        public IEnumerator TheColourFollowsThePlace()
        {
            yield return null; yield return null;
            Assert.AreEqual(NpcInkState.Drawn, _ink.State, "a standing place");
            Assert.AreEqual(0f, Prop(WashId), 0.001f);

            FadeStages.Advance(GameState.World, Place, 2);
            yield return null; yield return null;
            Assert.AreEqual(NpcInkState.Fading, _ink.State, "the place two stages gone");
            Assert.AreEqual(NpcInk.FadingWashMax * 2f / FadeStages.Max, Prop(WashId), 0.001f, "the fills wash by stage");
            Assert.AreEqual(0f, Prop(LineFadeId), 0.001f, "the line stays");

            Places.Release(GameState.World, Place);
            yield return null; yield return null;
            Assert.AreEqual(NpcInkState.Remnant, _ink.State, "let go");
            Assert.AreEqual(1f, Prop(WashId), 0.001f, "paper through the fills");
            Assert.AreEqual(1f, Prop(LineFadeId), 0.001f, "the line grey");
        }

        [Test]
        public void TheChoiceOrderIsTalkWalkActivityIdle()
        {
            bool Has(string c) => c == "idle" || c == "talk" || c == "walk" || c == "mending";
            Assert.AreEqual("talk", NpcAnimator.Choose(true, true, "mending nets", Has));
            Assert.AreEqual("walk", NpcAnimator.Choose(false, true, "mending nets", Has));
            Assert.AreEqual("mending", NpcAnimator.Choose(false, false, "mending nets", Has));
            Assert.AreEqual("idle", NpcAnimator.Choose(false, false, "reading the ledger", Has), "no clip for the activity");
            Assert.AreEqual("idle", NpcAnimator.Choose(false, false, "", Has));
            Assert.AreEqual("asleep", NpcAnimator.ActivityClip("asleep under the stilts"));
            Assert.AreEqual("singing", NpcAnimator.ActivityClip("Singing, to the water"));
            Assert.IsNull(NpcAnimator.ActivityClip("  "));
        }
    }
}
