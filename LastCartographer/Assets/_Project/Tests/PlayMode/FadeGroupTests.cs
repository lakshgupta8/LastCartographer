#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>A FadeGroup follows its place's stage: ink thins, lit layers wash to paper, layers drop out in order; Yarn drives it.</summary>
    public class FadeGroupTests
    {
        GameObject? _room, _fore, _mid, _ground, _dialogue;
        FadeGroup? _group;
        Material? _inkMat, _litMat;
        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            _room = new GameObject("Room_Fade");
            var room = _room.AddComponent<Room>();
            room.RoomId = "Fade_Test";
            _group = _room.AddComponent<FadeGroup>();

            var inkShader = Shader.Find("OWSBG/InkSprite");
            Assert.IsNotNull(inkShader);
            _inkMat = new Material(inkShader);
            _litMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _litMat.SetColor(BaseColorId, new Color(0.3f, 0.33f, 0.24f));

            _fore = Quad("Paper_Fore", _inkMat);
            _mid = Quad("Paper_Mid", _litMat);
            _ground = Quad("Floor", _litMat);
            _group.AddLayer(_fore.GetComponent<Renderer>(), 3);
            _group.AddLayer(_mid.GetComponent<Renderer>(), 4);
            _group.AddLayer(_ground.GetComponent<Renderer>(), 5);
        }

        GameObject Quad(string name, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(_room!.transform, false);
            q.GetComponent<Renderer>().sharedMaterial = m;
            return q;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _dialogue, _room }) if (go != null) Object.Destroy(go);
            if (_inkMat != null) Object.Destroy(_inkMat);
            if (_litMat != null) Object.Destroy(_litMat);
            GameState.NewGame();
        }

        static float InkOf(GameObject go)
        {
            var mpb = new MaterialPropertyBlock();
            go.GetComponent<Renderer>().GetPropertyBlock(mpb);
            return mpb.GetFloat(InkId);
        }

        static Color ColorOf(GameObject go)
        {
            var mpb = new MaterialPropertyBlock();
            go.GetComponent<Renderer>().GetPropertyBlock(mpb);
            return mpb.GetColor(BaseColorId);
        }

        static IEnumerator Settle(FadeGroup g, float seconds = 8f)
        {
            float t = 0f;
            while (g.IsAnimating && t < seconds) { t += Time.deltaTime; yield return null; }
            yield return null;
            Assert.IsFalse(g.IsAnimating, "the group lands on its target");
        }

        [UnityTest]
        public IEnumerator InkThinsLitLayersWashAndLayersDropInOrder()
        {
            yield return null;
            Assert.AreEqual("Fade_Test", _group!.PlaceId, "place id defaults to the room");
            Assert.AreEqual(1f, _group.Ink);
            Assert.AreEqual(1f, InkOf(_fore!), 0.001f);
            var drawn = ColorOf(_mid!);

            Assert.IsTrue(FadeStages.Advance(GameState.World, "Fade_Test", 2));
            yield return null;
            Assert.IsTrue(_group.IsAnimating, "a stage change animates");
            yield return Settle(_group);
            Assert.AreEqual(0.55f, _group.Ink, 0.01f);
            Assert.AreEqual(0.55f, InkOf(_fore!), 0.01f, "ink material follows _Ink");
            var washed = ColorOf(_mid!);
            Assert.Greater(washed.r, drawn.r, "lit paper washes toward the paper colour");
            Assert.IsTrue(_fore!.GetComponent<Renderer>().enabled && _mid!.GetComponent<Renderer>().enabled);

            FadeStages.Advance(GameState.World, "Fade_Test", 3);
            yield return null;
            Assert.IsTrue(_fore.GetComponent<Renderer>().enabled, "no dropout while the ink is still leaving");
            yield return Settle(_group);
            Assert.IsFalse(_fore.GetComponent<Renderer>().enabled, "foreground drops at 3");
            Assert.IsTrue(_mid!.GetComponent<Renderer>().enabled, "mid stays until 4");

            FadeStages.Advance(GameState.World, "Fade_Test", 4);
            yield return Settle(_group);
            Assert.IsFalse(_mid.GetComponent<Renderer>().enabled, "mid drops at 4");
            Assert.IsTrue(_ground!.GetComponent<Renderer>().enabled, "the floor never drops");
            Assert.AreEqual(0f, _group.Ink, 0.001f);

            // A fresh group on the same place snaps straight to the stage.
            var other = _room!.AddComponent<FadeGroup>();
            other.AddLayer(_ground.GetComponent<Renderer>(), 5);
            yield return null;
            Assert.AreEqual(0f, other.Ink, 0.001f);
            Assert.IsFalse(other.IsAnimating);
        }

        const string Script = @"
title: Start
---
Dotha: I'll sing them to the water, then.
<<fade Fade_Test 2>>
<<if fade_stage(""Fade_Test"") >= 2>>
    Dotha: It's going already.
<<endif>>
===
";

        [UnityTest]
        public IEnumerator YarnAdvancesAStage()
        {
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            Assert.AreEqual(2, FadeStages.Get(GameState.World, "Fade_Test"));
            CollectionAssert.Contains(presenter.Lines, "Dotha|It's going already.");
            yield return Settle(_group!);
            Assert.AreEqual(0.55f, _group!.Ink, 0.01f);
        }
    }
}
