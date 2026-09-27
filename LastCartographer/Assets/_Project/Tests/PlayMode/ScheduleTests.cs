#nullable enable
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
    /// <summary>Hub life (PRG-15): an NPC keeps its posts by the hour, loops them in an anchored place, and Yarn reads the clock.</summary>
    public class ScheduleTests
    {
        GameObject? _floor, _room, _npc, _dialogue;
        NpcSchedule? _schedule;
        NpcTalker? _talker;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _room = new GameObject("Room_Sched");
            _room.AddComponent<Room>().RoomId = "Sched_Test";
            _npc = new GameObject("Sable") { layer = Layer("Trigger") };
            _npc.transform.SetParent(_room.transform, false);
            _npc.transform.position = new Vector3(0f, 2f, 0f);
            _npc.AddComponent<BoxCollider2D>().isTrigger = true;
            _talker = _npc.AddComponent<NpcTalker>();
            _talker.StartNode = "Home";
            _schedule = _npc.AddComponent<NpcSchedule>();
            _schedule.WalkSpeed = 40f;
            _schedule.AddPost(DayPhase.Day, new Vector2(-4f, 0f), "", "mending nets");
            _schedule.AddPost(DayPhase.Dusk, new Vector2(4f, 0f), "Water", "singing to the water", 1);
            _schedule.AddPost(DayPhase.Night, new Vector2(-4f, 0f), "Asleep", "asleep");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _dialogue, _room, _floor }) if (go != null) Object.Destroy(go);
            GameState.NewGame();
        }

        IEnumerator UntilAt(string activity, float seconds = 6f)
        {
            float t = 0f;
            while (!(_schedule!.Current != null && _schedule.Activity == activity && !_schedule.IsWalking) && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(activity, _schedule!.Activity, "the NPC reaches its post");
            Assert.IsFalse(_schedule.IsWalking);
        }

        [UnityTest]
        public IEnumerator NpcWalksToItsPostWhenThePhaseChanges()
        {
            yield return null;
            Assert.AreEqual("mending nets", _schedule!.Activity, "a room that loads mid-day snaps to the post");
            Assert.AreEqual(-4f, _npc!.transform.position.x, 0.01f);
            Assert.AreEqual(0f, _npc.transform.position.y, 0.05f, "feet on the floor");
            Assert.AreEqual("Home", _talker!.StartNode, "a post without a node keeps the talker's own");

            DayClock.SetPhase(GameState.World, DayPhase.Dusk);
            yield return null; yield return null;
            Assert.IsTrue(_schedule.IsWalking, "dusk: off to the water");
            Assert.Greater(_npc.transform.localScale.x, 0f, "facing the way it walks");
            yield return UntilAt("singing to the water");
            Assert.AreEqual(4f, _npc.transform.position.x, 0.06f);
            Assert.AreEqual("Water", _talker.StartNode, "what it says depends on where it stands");

            DayClock.SetPhase(GameState.World, DayPhase.Dawn);
            yield return UntilAt("asleep");
            Assert.AreEqual("Asleep", _talker.StartNode, "before dawn's own post exists, the night's holds");
            Assert.AreEqual(-4f, _npc.transform.position.x, 0.06f);

            DayClock.SetPhase(GameState.World, DayPhase.Day);
            yield return UntilAt("mending nets");
            Assert.AreEqual("Home", _talker.StartNode);
        }

        [UnityTest]
        public IEnumerator AnAnchoredPlaceLoopsItsPosts()
        {
            yield return null;
            Assert.IsFalse(_schedule!.IsLooping);
            Assert.IsTrue(Places.Anchor(GameState.World, "Sched_Test"));
            Assert.IsTrue(_schedule.IsLooping, "the day is locked here: the schedule loops");
            _schedule.LoopSeconds = 0.08f;
            var seen = new HashSet<string>();
            float t = 0f;
            while (seen.Count < 3 && t < 6f)
            {
                if (_schedule.Current != null) seen.Add(_schedule.Activity);
                t += Time.deltaTime;
                yield return null;
            }
            CollectionAssert.AreEquivalent(new[] { "mending nets", "singing to the water", "asleep" }, seen, "the same posts, in order, forever");
            DayClock.SetPhase(GameState.World, DayPhase.Night);
            Assert.IsTrue(_schedule.IsLooping, "the clock outside does not reach in");
        }

        const string Script = @"
title: Start
---
<<clock night>>
<<if phase() == ""night"">>
    Sable: Quay's shut.
<<endif>>
<<sleep>>
<<if phase() == ""dawn"" and day() == 2>>
    Sable: Morning.
<<endif>>
===
";

        [UnityTest]
        public IEnumerator YarnSetsAndReadsTheClock()
        {
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            CollectionAssert.Contains(presenter.Lines, "Sable|Quay's shut.");
            CollectionAssert.Contains(presenter.Lines, "Sable|Morning.");
            Assert.AreEqual(2, DayClock.Day(GameState.World));
        }
    }
}
