#nullable enable
using System.Collections;
using System.IO;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The test round's recorder on the real game (PRO-04): rooms in the order entered with their seconds, deaths in
    /// the room they happened in, nodes read, the longest stretch with no new room, the Lamp-Keeper's fall finishing
    /// the round once; the session written open and then closed; and -continue starting from the last desk's save.
    /// </summary>
    public class PlaytestRecorderTests
    {
        readonly RouteReplay _replay = new RouteReplay();
        string _dir = "";
        const int Slot = 97;

        [SetUp]
        public void SetUp()
        {
            RouteReplay.Prepare();
            _dir = Path.Combine(Path.GetTempPath(), "owsbg-playtest-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (PlaytestRun.Instance != null) Object.Destroy(PlaytestRun.Instance.gameObject);
            Bootstrap.ContinueOverride = null;
            Bootstrap.ContinueSlot = 0;
            if (File.Exists(GameState.SlotPath(Slot))) File.Delete(GameState.SlotPath(Slot));
            yield return RouteReplay.Unload();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static IEnumerator Enter(string scene, string spawn)
        {
            var rm = RoomManager.Instance;
            rm.Transition(scene, spawn);
            yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 20f, scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSessionFollowsThePlayer()
        {
            yield return _replay.Boot();
            string path = Path.Combine(_dir, "ana.json");
            var run = PlaytestRun.Begin("ana", path);
            var rec = PlaytestRun.Recorder!;
            yield return null; yield return null;
            string first = RoomManager.Instance.CurrentRoom;
            Assert.AreEqual(first, rec.Room, "the room already open is the first");

            for (int i = 0; i < 30; i++) yield return null;
            float lostBefore = rec.SinceNewRoom;
            Assert.Greater(lostBefore, 0f);
            // The boardwalk, not the lighthouse: that room is the Lamp-Keeper's arena, and a real fight would count.
            yield return Enter("Greybox_Saltmarrow_Boardwalk", "West");
            Assert.AreEqual("Greybox_Saltmarrow_Boardwalk", rec.Room);
            Assert.Less(rec.SinceNewRoom, lostBefore, "a new room resets the stretch");
            Assert.GreaterOrEqual(rec.Session.longestLost, lostBefore, "and the longest stretch is kept");

            // A death, in the room it happens in.
            var wren = WrenController.Current!;
            var vitals = wren.GetComponent<WrenVitals>();
            float t = 0f;
            while (!vitals.IsDead && t < 5f) { vitals.Damage(vitals.MaxMasks); t += Time.deltaTime; yield return null; }
            Assert.IsTrue(vitals.IsDead, "she died for the count");
            yield return null;
            var snap = rec.Snapshot(false);
            Assert.AreEqual(1, snap.deaths);
            CollectionAssert.AreEqual(new[] { first, "Greybox_Saltmarrow_Boardwalk" }, snap.rooms, "rooms in the order first entered");
            Assert.AreEqual(1, snap.roomDeaths[1], "the death on the boardwalk");
            Assert.Greater(snap.roomSeconds[0], 0f); Assert.Greater(snap.roomSeconds[1], 0f);

            // A line read.
            int nodes = rec.Session.nodesRead;
            Assert.IsTrue(DialogueService.Instance!.StartNode("Quay_Sable_First"));
            yield return null;
            Assert.AreEqual(nodes + 1, rec.Session.nodesRead);
            DialogueService.Instance.Stop();

            // The finish line, once.
            int finished = 0;
            rec.Finished += () => finished++;
            rec.OnFightStarted(Playtest.FinishBoss);
            rec.OnFightStarted(Playtest.FinishBoss);
            Assert.IsFalse(rec.Session.finished);
            rec.OnFightWon(Playtest.FinishBoss);
            rec.OnFightWon(Playtest.FinishBoss);
            Assert.IsTrue(rec.Session.finished);
            Assert.AreEqual(1, finished, "the round ends once");
            Assert.AreEqual(2, rec.Snapshot(false).AttemptsAt(Playtest.FinishBoss));
            Assert.IsTrue(rec.Snapshot(false).Beat(Playtest.FinishBoss));

            // Written open, then closed.
            run.SaveNow(false);
            var open = Playtest.Session.FromJson(File.ReadAllText(path));
            Assert.IsFalse(open.closed); Assert.AreEqual("ana", open.tester); Assert.AreEqual(Playtest.Round, open.round);
            run.SaveNow(true);
            Assert.IsTrue(Playtest.Session.FromJson(File.ReadAllText(path)).closed);
        }

        [UnityTest]
        public IEnumerator ContinueStartsFromTheLastDesk()
        {
            // A save as a desk leaves it: the lighthouse, a flag set; then the game started with -continue.
            GameState.World.RespawnRoom = "Greybox_Saltmarrow_Lighthouse";
            GameState.World.RespawnSpawn = "West";
            GameState.World.Set("playtest.continue_probe", 7);
            GameState.Save(Slot);
            GameState.NewGame();
            Assert.AreEqual(0, GameState.World.Get("playtest.continue_probe"));
            Bootstrap.ContinueOverride = true;
            Bootstrap.ContinueSlot = Slot;

            yield return _replay.Boot();
            Assert.AreEqual("Greybox_Saltmarrow_Lighthouse", RoomManager.Instance.CurrentRoom, "in the room she rested in");
            Assert.AreEqual(7, GameState.World.Get("playtest.continue_probe"), "with the save's flags");
        }
    }
}
