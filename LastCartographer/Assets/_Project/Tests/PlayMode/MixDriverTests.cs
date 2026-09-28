#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The mix in the game (AUD-09): the driver wakes with the first scene with a source per bus, follows the state
    /// (paused over a fight over the Blank over combat), reads the place's fade stage, and puts the player's volumes
    /// on the sources.
    /// </summary>
    public class MixDriverTests
    {
        MixDriver Driver => MixDriver.Instance!;

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            Options.ResetToDefaults();
            Assert.IsNotNull(MixDriver.Instance, "booted with the first scene");
            Driver.RoomOverride = "Persistent";
            Driver.Mixer.Snap(Mix.Snapshot.Explore);
            Driver.Mixer.Tick(30f);   // any duck or combat hold from an earlier test is long gone
        }

        [TearDown]
        public void TearDown()
        {
            Driver.RoomOverride = null;
            Options.ResetToDefaults();
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator ItWakesWithTheGameWithASourcePerBus()
        {
            yield return null;
            Assert.AreSame(Driver.Mixer, Mix.Live, "the live mix is the driver's");
            Assert.IsNull(Driver.Source(Mix.Bus.Master), "the master is a number, not a source");
            foreach (var bus in Mix.Buses)
            {
                if (bus == Mix.Bus.Master) continue;
                var src = Driver.Source(bus);
                Assert.IsNotNull(src, bus + " has a source");
                Assert.AreEqual(0f, src.spatialBlend, bus + " is 2D");
                Assert.IsFalse(src.playOnAwake);
                Assert.IsTrue(src.ignoreListenerPause, bus + " is the mix's to shape while paused");
            }
            Assert.IsNotNull(Driver.Filter(Mix.Bus.Music)); Assert.IsNotNull(Driver.Filter(Mix.Bus.Ambience));
            Assert.IsNull(Driver.Filter(Mix.Bus.Sfx), "the room's sounds are never filtered");
            Assert.IsTrue(Driver.Source(Mix.Bus.Music).loop && Driver.Source(Mix.Bus.Ambience).loop);
            Driver.Loop(Mix.Bus.Music, null);
            Driver.Play(Mix.Bus.Sfx, null);
            Assert.IsFalse(Driver.Source(Mix.Bus.Music).isPlaying, "nothing to play yet, nothing thrown");
        }

        [UnityTest]
        public IEnumerator TheSnapshotFollowsTheState()
        {
            yield return null;
            Assert.AreEqual(Mix.Snapshot.Explore, Driver.Decide());
            Mix.Note(Mix.Duck.Impact);
            Assert.IsTrue(Driver.Mixer.InCombat, "a blow is combat");
            yield return null;
            Assert.AreEqual(Mix.Snapshot.Combat, Driver.Mixer.Target);
            Assert.Greater(Driver.Mixer.DuckLevel(Mix.Duck.Impact), 0f, "and the ambience blinks for it");

            Driver.RoomOverride = "Greybox_Blank_Island1";
            Assert.AreEqual(Mix.Snapshot.Blank, Driver.Decide(), "the Blank over combat");
            Pause.Begin();
            try
            {
                Assert.AreEqual(Mix.Snapshot.Paused, Driver.Decide(), "paused over everything");
                yield return null;
                Assert.AreEqual(Mix.Snapshot.Paused, Driver.Mixer.Target);
                Assert.Less(Driver.Source(Mix.Bus.Sfx).volume, 1f, "already on its way down");
            }
            finally { Pause.End(); }
            yield return null;
            Assert.AreEqual(Mix.Snapshot.Blank, Driver.Mixer.Target);
            Driver.RoomOverride = "Persistent";
            Driver.Mixer.Tick(Mix.CombatHold + 1f);
            yield return null;
            Assert.AreEqual(Mix.Snapshot.Explore, Driver.Mixer.Target, "the hold has run out");
        }

        [UnityTest]
        public IEnumerator TheFadeStageDullsTheAmbience()
        {
            Driver.RoomOverride = "Greybox_Saltmarrow_A";
            yield return null;
            Assert.AreEqual(0, Driver.Mixer.Stage);
            Assert.AreEqual(Mix.Open, Driver.Filter(Mix.Bus.Ambience).cutoffFrequency, 1f);
            FadeStages.Advance(GameState.World, "Saltmarrow_A", 2);
            yield return null;
            Assert.AreEqual(2, Driver.Mixer.Stage, "the place the room draws");
            Assert.AreEqual(Mix.StageCutoff(2), Driver.Filter(Mix.Bus.Ambience).cutoffFrequency, 1f);
            Assert.AreEqual(Mix.Open, Driver.Filter(Mix.Bus.Music).cutoffFrequency, 1f, "the music is not the place's");
        }

        [UnityTest]
        public IEnumerator ThePlayersVolumesReachTheSources()
        {
            yield return null;
            Assert.AreEqual(1f, Driver.Source(Mix.Bus.Music).volume, 1e-3f);
            Options.Set(Options.Volume.Music, 0.5f);
            Options.Set(Options.Volume.Sound, 0.2f);
            Options.Set(Options.Volume.Master, 0.5f);
            yield return null;
            Assert.AreEqual(0.25f, Driver.Source(Mix.Bus.Music).volume, 1e-3f);
            Assert.AreEqual(0.1f, Driver.Source(Mix.Bus.Sfx).volume, 1e-3f);
            Assert.AreEqual(0.1f, Driver.Source(Mix.Bus.Ui).volume, 1e-3f);
            Assert.AreEqual(0.5f, Driver.Source(Mix.Bus.Dialogue).volume, 1e-3f, "voices at the master alone");
        }
    }
}
