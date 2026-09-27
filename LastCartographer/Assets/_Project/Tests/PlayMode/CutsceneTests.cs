#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;
using Yarn.Unity;

namespace OWSBG.Tests
{
    /// <summary>Presenter that holds every line until the test releases it.</summary>
    sealed class HoldingPresenter : DialoguePresenterBase
    {
        public bool Release;
        public int LinesShown;

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
        public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            LinesShown++;
            while (!Release && !token.IsNextContentRequested) await YarnTask.Yield();
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
            => YarnTask<DialogueOption?>.FromResult(dialogueOptions[0]);
    }

    /// <summary>The cutscene pipeline (PRG-16): clips move actors and fade, dialogue holds the timeline, Yarn waits for a cutscene.</summary>
    public class CutsceneTests
    {
        GameObject? _floor, _wren, _actor, _csGo, _dialogue;
        WrenController? _ctrl;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            ScreenFade.Clear();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _ctrl.Teleport(Vector2.zero);
            _actor = new GameObject("Isolde");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _dialogue, _csGo, _actor, _wren, _floor }) if (go != null) Object.Destroy(go);
            ScreenFade.Clear();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        Cutscene MakeCutscene(string id, TimelineAsset timeline)
        {
            _csGo = new GameObject("Cutscene_" + id);
            _csGo.SetActive(false);
            var director = _csGo.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = timeline;
            var cs = _csGo.AddComponent<Cutscene>();
            cs.Id = id;
            _csGo.SetActive(true);
            return cs;
        }

        [UnityTest]
        public IEnumerator ClipsMoveTheActorFadeTheScreenAndFreezeWren()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var moves = timeline.CreateTrack<PlayableTrack>("Moves");
            var moveClip = moves.CreateClip<ActorMoveClip>();
            moveClip.start = 0; moveClip.duration = 0.6;
            var move = (ActorMoveClip)moveClip.asset;
            move.From = new Vector2(0f, 0f); move.To = new Vector2(4f, 0f);
            move.Actor.exposedName = "actor";
            var fades = timeline.CreateTrack<PlayableTrack>("Fade");
            var fadeClip = fades.CreateClip<PaperFadeClip>();
            fadeClip.start = 0; fadeClip.duration = 0.6;
            var fade = (PaperFadeClip)fadeClip.asset;
            fade.From = 1f; fade.To = 0f;

            var cs = MakeCutscene("test_move", timeline);
            cs.SetReference("actor", _actor!.transform);
            yield return null;
            Assert.AreSame(cs, Cutscene.Find("test_move"));

            bool completed = false;
            cs.Completed += _ => completed = true;
            cs.Play();
            Assert.IsTrue(cs.IsPlaying);
            Assert.IsTrue(_ctrl!.Frozen, "Wren waits through a cutscene");
            yield return null;   // the rebuilt graph evaluates on the next frame
            Assert.AreEqual(1f, ScreenFade.Level, 0.05f, "the fade starts solid");
            float t = 0f;
            bool movedMidway = false;
            while (cs.IsPlaying && t < 5f)
            {
                if (_actor.transform.position.x > 0.5f && _actor.transform.position.x < 3.5f) movedMidway = true;
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(cs.IsPlaying, "the cutscene ends by itself");
            Assert.IsTrue(completed);
            Assert.IsTrue(movedMidway, "the actor passes through the middle");
            Assert.AreEqual(4f, _actor.transform.position.x, 0.01f, "the actor lands on the target");
            Assert.AreEqual(0f, ScreenFade.Level, 0.01f, "the fade clears");
            Assert.IsFalse(_ctrl.Frozen, "Wren is released");
            Assert.IsNull(Cutscene.Current);
        }

        const string HeldScript = @"
title: Start
---
Isolde: Stop. Here is far enough.
===
";

        [UnityTest]
        public IEnumerator ADialogueClipHoldsTheTimelineUntilTheConversationEnds()
        {
            var project = RuntimeYarnBuilder.Build(HeldScript);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<HoldingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var talk = timeline.CreateTrack<PlayableTrack>("Talk");
            var clip = talk.CreateClip<DialogueNodeClip>();
            clip.start = 0.05; clip.duration = 0.2;
            ((DialogueNodeClip)clip.asset).Node = "Start";
            var tail = timeline.CreateTrack<PlayableTrack>("Tail");
            var tailClip = tail.CreateClip<PaperFadeClip>();
            tailClip.start = 0.3; tailClip.duration = 0.3;
            ((PaperFadeClip)tailClip.asset).From = 0.5f; ((PaperFadeClip)tailClip.asset).To = 0f;

            var cs = MakeCutscene("test_talk", timeline);
            yield return null;
            cs.Play();
            float tt = 0f;
            while (!service.IsRunning && tt < 5f) { tt += Time.deltaTime; yield return null; }
            Assert.IsTrue(service.IsRunning, "the clip starts the node");
            Assert.AreEqual(1, presenter.LinesShown);
            Assert.IsTrue(cs.IsPaused, "the timeline waits");
            double held = cs.Director.time;
            tt = 0f;
            while (tt < 0.5f) { tt += Time.deltaTime; yield return null; }
            Assert.IsTrue(cs.IsPlaying);
            Assert.AreEqual(held, cs.Director.time, 0.001, "time does not move while the line is up");

            presenter.Release = true;
            for (int i = 0; i < 60 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            float t = 0f;
            while (cs.IsPlaying && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(cs.IsPlaying, "the timeline runs to the end once the talk is done");
            Assert.IsFalse(cs.IsPaused);
            Assert.AreEqual(0f, ScreenFade.Level, 0.01f, "the clip after the talk still ran");
        }

        const string YarnScript = @"
title: Start
---
Isolde: Look at me.
<<cutscene test_walk>>
Isolde: Then look beside the white.
===
";

        [UnityTest]
        public IEnumerator YarnWaitsForACutsceneCommand()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var moves = timeline.CreateTrack<PlayableTrack>("Moves");
            var moveClip = moves.CreateClip<ActorMoveClip>();
            moveClip.start = 0; moveClip.duration = 0.4;
            var move = (ActorMoveClip)moveClip.asset;
            move.From = Vector2.zero; move.To = new Vector2(6f, 0f);
            move.Actor.exposedName = "actor";
            var cs = MakeCutscene("test_walk", timeline);
            cs.SetReference("actor", _actor!.transform);
            yield return null;

            var project = RuntimeYarnBuilder.Build(YarnScript);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            float actorXAtSecondLine = -1f;
            bool playingAtSecondLine = true;
            presenter.OnLine = text =>
            {
                if (text.Contains("beside")) { actorXAtSecondLine = _actor.transform.position.x; playingAtSecondLine = cs.IsPlaying; }
            };
            Assert.IsTrue(service.StartNode("Start"));
            float t = 0f;
            while (service.IsRunning && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(service.IsRunning);
            Assert.AreEqual(2, presenter.Lines.Count);
            Assert.IsFalse(playingAtSecondLine, "the second line comes after the cutscene");
            Assert.AreEqual(6f, actorXAtSecondLine, 0.01f, "the actor had arrived by then");
        }
    }
}
