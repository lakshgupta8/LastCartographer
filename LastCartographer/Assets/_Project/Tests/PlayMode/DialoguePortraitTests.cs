#nullable enable
using System.Collections;
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
    /// The dialogue page's portraits (CHR-13), through the persistent scene and the shipped Yarn project: a speaker
    /// with a face shows it, the beak moving while the line is new and resting after; a thing that speaks shows none;
    /// Wren's choices show none; a Remnant speaks in grey; and the bird in the room decides how grey a speaker is.
    /// </summary>
    public class DialoguePortraitTests
    {
        DialogueService _svc = null!;
        DialogueView _view = null!;
        ViewDialoguePresenter _presenter = null!;
        GameObject? _stand;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_stand != null) Object.Destroy(_stand);
            Bootstrap.SkipPrologueOverride = null;
            DialogueService.Instance?.Stop();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        IEnumerator FirstLine(string node)
        {
            _svc.Stop();
            yield return null;
            Assert.IsTrue(_svc.StartNode(node), node);
            yield return Until(() => _view.IsVisible && !string.IsNullOrEmpty(_view.LineText), 5f, node + "'s first line");
        }

        [UnityTest]
        public IEnumerator ASpeakerWithAFaceShowsItTalkingThenResting()
        {
            yield return Boot();
            Assert.AreEqual(Portraits.Faces.Count, _view.PortraitSheets.Count, "the page carries every face");

            yield return FirstLine("Quay_Sable_First");
            Assert.AreEqual("Sable", _view.SpeakerText);
            Assert.AreEqual("Sable", _view.PortraitSpeaker, "her face beside her line");
            Assert.IsTrue(_view.IsTalking);
            Assert.AreEqual(0f, _view.PortraitGrey, "Sable is drawn");

            bool sawTalk = false, sawRest = false;
            float until = Time.unscaledTime + DialogueView.TalkSeconds(_view.LineText) + 0.3f;
            while (Time.unscaledTime < until)
            {
                if (_view.IsTalking) { sawTalk |= _view.PortraitFrame == Portraits.Talk; sawRest |= _view.PortraitFrame == Portraits.Rest; }
                yield return null;
            }
            Assert.IsTrue(sawTalk && sawRest, "the beak opens and shuts while the line is new");
            Assert.IsFalse(_view.IsTalking);
            Assert.AreEqual(Portraits.Rest, _view.PortraitFrame, "and rests once it has been said");
            _svc.Stop();
        }

        [UnityTest]
        public IEnumerator AThingThatSpeaksShowsNoFace()
        {
            yield return Boot();
            yield return FirstLine("Camp_Ashes_Ahead");
            Assert.AreEqual("Ashes", _view.SpeakerText);
            Assert.IsTrue(Portraits.Faceless.Contains("Ashes"));
            Assert.IsNull(_view.PortraitSpeaker, "the ashes have no face");

            // A face, then a choice: Wren chooses without a portrait.
            GameState.World.Set(Camp.NightKey, 1);
            yield return FirstLine(CampSite.BedrollNode);
            _presenter.Advance();
            yield return Until(() => _presenter.IsShowingOptions, 5f, "the bedroll's options");
            yield return null;
            Assert.IsNull(_view.PortraitSpeaker, "her choices have no face");
            _presenter.Choose(1);
            yield return Until(() => { if (_presenter.IsShowingLine) _presenter.Advance(); return !_svc.IsRunning; }, 5f, "the bedroll to end");
        }

        [UnityTest]
        public IEnumerator ARemnantSpeaksInGrey()
        {
            yield return Boot();
            Assert.IsTrue(Portraits.RemnantAtRest.Contains("Corvin"));
            yield return FirstLine("Capital_Corvin_Argue");
            Assert.AreEqual("Corvin", _view.PortraitSpeaker);
            Assert.AreEqual(1f, _view.PortraitGrey, "no Corvin in the room: his rest state, a Remnant");
            _svc.Stop();
        }

        [UnityTest]
        public IEnumerator ALineIsSaidInItsMood()
        {
            yield return Boot();
            // Corvin's question is tagged #face:wary: the tag reaches the page through Yarn's metadata and wins over
            // the question mark, and the portrait shows that mood's row of the sheet, talking and then resting in it.
            yield return FirstLine("Capital_Corvin_Argue");
            StringAssert.EndsWith("?", _view.LineText);
            Assert.AreEqual(Portraits.Wary, _view.PortraitMood, "his tag, not his question mark");
            float until = Time.unscaledTime + DialogueView.TalkSeconds(_view.LineText) + 0.3f;
            while (Time.unscaledTime < until)
            {
                Assert.AreEqual(Portraits.Wary, _view.PortraitMood, "the beak moves inside the mood");
                yield return null;
            }
            Assert.AreEqual(Portraits.Rest, _view.PortraitFrame);
            var face = _view.FaceUv;
            Assert.AreEqual(1f / Portraits.Moods.Length, face.height, 1e-5f, "a row of the sheet");
            Assert.AreEqual(1f - (Portraits.Wary + 1f) / Portraits.Moods.Length, face.y, 1e-5f, "the wary row, counted from the top");
            Assert.AreEqual(Portraits.Rest / (float)Portraits.Frames.Length, face.x, 1e-5f, "resting");
            _svc.Stop();
        }

        [UnityTest]
        public IEnumerator TheBirdInTheRoomDecidesHowGrey()
        {
            yield return Boot();
            // Sable is drawn at rest; stand her in the room as the talker, on a place two stages faded, then make her a Remnant.
            float before = DialogueView.WashOf("Sable");
            _stand = new GameObject("Npc_Sable");
            _stand.AddComponent<BoxCollider2D>().isTrigger = true;
            NpcTalker.Talking = _stand.AddComponent<NpcTalker>();
            var ink = _stand.AddComponent<NpcInk>();
            ink.PlaceId = "Test_Portrait_Place";
            GameState.World.Set(FadeStages.Key("Test_Portrait_Place"), 2);
            yield return null; yield return null;
            Assert.AreEqual(NpcInkState.Fading, ink.State);
            float expected = NpcInk.FadingWashMax * 2f / FadeStages.Max;
            Assert.AreEqual(expected, DialogueView.WashOf("Sable"), 1e-4f, "fading with her place");

            yield return FirstLine("Quay_Sable_First");
            Assert.AreEqual(expected, _view.PortraitGrey, 1e-4f, "the portrait greys as far as she has");

            ink.Rest = NpcInkState.Remnant;
            yield return null; yield return null;
            Assert.AreEqual(1f, DialogueView.WashOf("Sable"), "a Remnant in the room");

            Object.Destroy(_stand); _stand = null;
            yield return null;
            Assert.IsNull(NpcTalker.Talking);
            Assert.AreEqual(before, DialogueView.WashOf("Sable"), "gone again: as she was");
            _svc.Stop();
        }
    }
}
