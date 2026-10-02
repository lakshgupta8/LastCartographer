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
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The store's screenshots and trailer clips, captured from the game as it plays (ENV-13, docs/design/marketing-assets.md).
    /// It runs only when asked: Unity started with -captureMarketing (the batch runner does not honour [Explicit], so
    /// without the switch each test ignores itself and the suite never rewrites docs/marketing). The camera and the UI
    /// panel render into one texture, read back to PNG: the shots to docs/marketing/screenshots/, each clip's frames to
    /// logs/capture/&lt;clip&gt;/ for tools/marketing/encode_clips.sh to make into video.
    /// </summary>
    [Explicit("captures the store's assets; run with -testFilter MarketingCaptureTests")]
    public class MarketingCaptureTests
    {
        readonly RouteReplay _replay = new RouteReplay();
        RenderTexture? _rt;
        Camera? _cam;
        UIDocument? _doc;

        public const string Switch = "-captureMarketing";

        [SetUp]
        public void SetUp()
        {
            if (!System.Environment.GetCommandLineArgs().Contains(Switch))
                Assert.Ignore("captures the store's assets; run with -testFilter MarketingCaptureTests " + Switch);
            RouteReplay.Prepare();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            if (_doc != null) _doc.panelSettings.targetTexture = null;
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) _rt.Release();
            yield return RouteReplay.Unload();
        }

        static string Out(string relative)
        {
            var path = Path.GetFullPath(Path.Combine("..", relative));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        void Target(int width, int height)
        {
            if (_rt != null) { _rt.Release(); }
            _rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            _cam = Camera.main;
            _cam.targetTexture = _rt;
            _doc = UiRoot.Instance.GetComponent<UIDocument>();
            _doc.panelSettings.targetTexture = _rt;
            _doc.panelSettings.clearColor = false;
        }

        void Save(string path)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt!.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        /// <summary>The HUD, and the captions a room says on entry (a route's margin note), on or off together.</summary>
        static void ShowHud(bool on)
        {
            UiRoot.Instance.Hud.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            UiRoot.Instance.Caption.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Into the room by its spawn, then <paramref name="along"/> units on from it; the camera given time to settle.</summary>
        static IEnumerator Stand(string? scene, string? spawn, float along)
        {
            var rm = RoomManager.Instance;
            if (!string.IsNullOrEmpty(scene) && rm.CurrentRoom != scene)
            {
                rm.Transition(scene!, spawn!);
                yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 20f, scene!);
            }
            var wren = WrenController.Current!;
            var room = Room.Current;
            var at = room != null && spawn != null ? room.FindSpawn(spawn) : null;
            Vector2 from = at != null ? (Vector2)at.position : (Vector2)wren.transform.position;
            wren.Teleport(from + new Vector2(along, 0.4f));
            var vitals = wren.GetComponent<WrenVitals>();
            for (int i = 0; i < 150; i++) { vitals.RestoreAll(); yield return null; }   // a crab nearby does not wear her down for the picture
            DialogueService.Instance!.Stop();   // whatever the room opened on entry (a cutscene's line, a voice in the white)
            for (int i = 0; i < 10; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureTheScreenshots()
        {
            yield return _replay.Boot();
            Target(Marketing.ShotWidth, Marketing.ShotHeight);
            foreach (var shot in Marketing.Shots)
            {
                DialogueService.Instance!.Stop();
                yield return Stand(shot.Scene, shot.Spawn, shot.Along);
                ShowHud(shot.Hud);
                if (shot.Node != null)
                {
                    Assert.IsTrue(DialogueService.Instance.StartNode(shot.Node), shot.Node);
                    for (int i = 0; i < 40; i++) yield return null;
                }
                _cam!.Render();
                yield return null;
                Save(Out("docs/marketing/screenshots/" + shot.Id + ".png"));
            }
            DialogueService.Instance!.Stop();

            // The key art's ground: the Wind Gate at four times the pixels, no HUD.
            Target(Marketing.KeyArtWidth, Marketing.KeyArtHeight);
            var gate = Marketing.Shots.First(s => s.Id == "windreach_gate");
            yield return Stand(gate.Scene, gate.Spawn, gate.Along);
            ShowHud(false);
            var wren = WrenController.Current!;
            var quad = wren.GetComponentsInChildren<Renderer>();
            foreach (var r in quad) r.enabled = false;               // she is drawn into the key art large, not small in it
            _cam!.Render();
            yield return null;
            Save(Out("logs/capture/key_art_ground.png"));
            foreach (var r in quad) r.enabled = true;
            ShowHud(true);
        }

        [UnityTest]
        public IEnumerator CaptureTheTrailerClips()
        {
            yield return _replay.Boot();
            Target(Marketing.ShotWidth, Marketing.ShotHeight);
            ShowHud(false);
            var wren = WrenController.Current!;
            var abilities = wren.GetComponent<AbilitySet>();
            var original = wren.Input;
            foreach (var clip in Marketing.Clips)
            {
                DialogueService.Instance!.Stop();
                wren.Input = original;
                yield return Stand(clip.Scene, clip.Spawn, clip.Along);
                foreach (var a in clip.Abilities) abilities.Unlock(a);
                var input = new ScriptedInput();
                wren.Input = input;
                if (clip.Node != null) Assert.IsTrue(DialogueService.Instance.StartNode(clip.Node), clip.Node);

                var dir = Out("logs/capture/" + clip.Id + "/x");
                dir = Path.GetDirectoryName(dir)!;
                foreach (var old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
                Time.captureFramerate = Marketing.ClipFps;
                int frame = 0;
                foreach (var step in clip.Steps)
                {
                    for (int i = 0; i < step.Frames; i++)
                    {
                        input.Move = new Vector2(step.Move, 0f);
                        input.JumpHeld = step.JumpHeld;
                        if (i == 0) { if (step.Jump) input.PressJump(); if (step.Dash) input.PressDash(); if (step.Attack) input.PressAttack(); }
                        yield return null;
                        Save(Path.Combine(dir, "frame_" + frame.ToString("0000") + ".png"));
                        frame++;
                    }
                }
                Time.captureFramerate = 0;
                Assert.Greater(frame, 0, clip.Id);
            }
            wren.Input = original;
            DialogueService.Instance!.Stop();
            ShowHud(true);
        }
    }
}
