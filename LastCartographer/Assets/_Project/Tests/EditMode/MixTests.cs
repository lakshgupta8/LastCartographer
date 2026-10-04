using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The mix (AUD-09, docs/design/audio-mix.md): each snapshot's shape, the moves between them, the ducks'
    /// envelopes, the fade stage's filter, the player's volumes on top, decibels, and a room's region.
    /// </summary>
    public class MixTests
    {
        static readonly Dictionary<Options.Volume, float> Vol = new Dictionary<Options.Volume, float>();

        static Mixer Fresh()
        {
            Vol.Clear();
            foreach (Options.Volume v in System.Enum.GetValues(typeof(Options.Volume))) Vol[v] = 1f;
            return new Mixer { Volume = v => Vol[v] };
        }

        static void Run(Mixer m, float seconds, float dt = 1f / 60f) { for (float t = 0f; t < seconds; t += dt) m.Tick(dt); }

        [Test]
        public void EverySnapshotHasItsShape()
        {
            foreach (var s in Mix.Snapshots)
            {
                var spec = Mix.Of(s);
                Assert.AreEqual(1f, spec.Gain[(int)Mix.Bus.Master], s + ": the master is the player's");
                Assert.Greater(spec.In, 0f, s + " moves in over time");
                Assert.Greater(spec.Out, 0f, s + " moves out over time");
                foreach (var b in Mix.Buses) { Assert.That(spec.Gain[(int)b], Is.InRange(0f, 1f), s + "/" + b); Assert.Greater(spec.Cutoff[(int)b], 0f); }
            }
            var explore = Mix.Of(Mix.Snapshot.Explore);
            foreach (var b in Mix.Buses) { Assert.AreEqual(1f, explore.Gain[(int)b], "explore is the room as designed"); Assert.AreEqual(Mix.Open, explore.Cutoff[(int)b]); }
            Assert.Less(Mix.Of(Mix.Snapshot.Combat).Gain[(int)Mix.Bus.Ambience], 1f, "combat steps the ambience back");
            Assert.Less(Mix.Of(Mix.Snapshot.Boss).Gain[(int)Mix.Bus.Ambience], Mix.Of(Mix.Snapshot.Combat).Gain[(int)Mix.Bus.Ambience], "a boss more so");
            Assert.Greater(Mix.Of(Mix.Snapshot.Boss).Out, Mix.Of(Mix.Snapshot.Boss).In, "the aftermath is slow");
            var dialogue = Mix.Of(Mix.Snapshot.Dialogue);
            Assert.Less(dialogue.Gain[(int)Mix.Bus.Music], 1f); Assert.AreEqual(1f, dialogue.Gain[(int)Mix.Bus.Dialogue], "the voice is never ducked by its own state");
            var blank = Mix.Of(Mix.Snapshot.Blank);
            Assert.Less(blank.Cutoff[(int)Mix.Bus.Music], Mix.Open, "the Blank is under a low-pass");
            Assert.Less(blank.Cutoff[(int)Mix.Bus.Ambience], Mix.Open);
            var paused = Mix.Of(Mix.Snapshot.Paused);
            Assert.AreEqual(0f, paused.Gain[(int)Mix.Bus.Sfx], "nothing in the room plays while paused");
            Assert.AreEqual(0f, paused.Gain[(int)Mix.Bus.Dialogue]);
            Assert.AreEqual(1f, paused.Gain[(int)Mix.Bus.Ui], "the page still speaks");
        }

        [Test]
        public void AMoveTakesItsTimeAndSettles()
        {
            var m = Fresh();
            Assert.AreEqual(Mix.Snapshot.Explore, m.Current);
            m.Go(Mix.Snapshot.Boss);
            Assert.AreEqual(Mix.Snapshot.Boss, m.Target);
            Assert.AreEqual(Mix.Snapshot.Explore, m.Current, "not there yet");
            Assert.IsTrue(m.Moving);
            float last = 1f;
            Run(m, 0.5f);
            float mid = m.SnapshotGain(Mix.Bus.Ambience);
            Assert.That(mid, Is.InRange(0.31f, 0.99f), "halfway in, between the two");
            Run(m, 0.6f);
            Assert.IsFalse(m.Moving);
            Assert.AreEqual(Mix.Snapshot.Boss, m.Current);
            Assert.AreEqual(0.3f, m.SnapshotGain(Mix.Bus.Ambience), 1e-4f);

            // Monotone on the way back, over the boss's Out.
            m.Go(Mix.Snapshot.Explore);
            last = m.SnapshotGain(Mix.Bus.Ambience);
            for (int i = 0; i < 60; i++) { m.Tick(1f / 60f); float g = m.SnapshotGain(Mix.Bus.Ambience); Assert.GreaterOrEqual(g, last - 1e-5f, "rising"); last = g; }
            Assert.IsTrue(m.Moving, "the aftermath takes 2.5 s; one second in it is still moving");
            Run(m, 2f);
            Assert.AreEqual(Mix.Snapshot.Explore, m.Current);
            Assert.AreEqual(1f, m.SnapshotGain(Mix.Bus.Ambience), 1e-4f);

            // A change of mind mid-move starts from where it is, not from the old snapshot.
            m.Go(Mix.Snapshot.Paused);
            Run(m, 0.05f);
            float partway = m.SnapshotGain(Mix.Bus.Sfx);
            Assert.That(partway, Is.InRange(0.01f, 0.99f));
            m.Go(Mix.Snapshot.Explore);
            m.Tick(1f / 60f);
            Assert.Greater(m.SnapshotGain(Mix.Bus.Sfx), partway - 1e-5f);
            m.Snap(Mix.Snapshot.Blank);
            Assert.AreEqual(Mix.Snapshot.Blank, m.Current);
            Assert.AreEqual(2500f, m.Cutoff(Mix.Bus.Music));
        }

        [Test]
        public void ADuckPressesThenLetsGo()
        {
            var m = Fresh();
            Assert.AreEqual(1f, m.DuckGain(Mix.Bus.Music));
            m.Trigger(Mix.Duck.Line);
            var d = Mix.Of(Mix.Duck.Line);
            Assert.AreEqual(0f, m.DuckLevel(Mix.Duck.Line), "just triggered: the attack hasn't begun");
            m.Tick(d.Attack / 2f);
            Assert.AreEqual(0.5f, m.DuckLevel(Mix.Duck.Line), 1e-4f);
            m.Tick(d.Attack / 2f);
            Assert.AreEqual(1f, m.DuckLevel(Mix.Duck.Line), 1e-4f);
            Assert.AreEqual(d.Floor[(int)Mix.Bus.Music], m.DuckGain(Mix.Bus.Music), 1e-4f, "holding at the floor");
            Assert.AreEqual(1f, m.DuckGain(Mix.Bus.Dialogue), "the voice itself is untouched");
            m.Tick(d.Hold + d.Release / 2f);
            Assert.AreEqual(0.5f, m.DuckLevel(Mix.Duck.Line), 1e-3f, "halfway through the release");
            m.Tick(d.Release);
            Assert.AreEqual(0f, m.DuckLevel(Mix.Duck.Line));
            Assert.AreEqual(1f, m.DuckGain(Mix.Bus.Music));

            // Two ducks: the deeper one wins on each bus.
            m.Trigger(Mix.Duck.Impact); m.Trigger(Mix.Duck.Bind);
            m.Tick(0.06f);
            float ambience = m.DuckGain(Mix.Bus.Ambience);
            Assert.AreEqual(Mathf.Min(Mix.Of(Mix.Duck.Impact).Floor[(int)Mix.Bus.Ambience], Mix.Of(Mix.Duck.Bind).Floor[(int)Mix.Bus.Ambience]), ambience, 1e-4f);
            foreach (var duck in Mix.Ducks)
            {
                var spec = Mix.Of(duck);
                Assert.AreEqual(1f, spec.Floor[(int)spec.Voice], duck + " never ducks its own bus");
                Assert.Greater(spec.Release, spec.Attack, duck + " lets go slower than it presses");
            }
        }

        [Test]
        public void CombatOutlastsTheLastBlow()
        {
            var m = Fresh();
            Assert.IsFalse(m.InCombat);
            m.NoteCombat();
            Assert.IsTrue(m.InCombat);
            Run(m, Mix.CombatHold - 1f);
            Assert.IsTrue(m.InCombat, "still, a second before");
            m.NoteCombat();
            Run(m, Mix.CombatHold - 1f);
            Assert.IsTrue(m.InCombat, "each blow starts the hold again");
            Run(m, 1.1f);
            Assert.IsFalse(m.InCombat);
        }

        [Test]
        public void TheFadeStageFiltersTheAmbience()
        {
            Assert.AreEqual(Mix.Open, Mix.StageCutoff(0));
            float last = Mix.Open;
            for (int s = 1; s <= 3; s++) { Assert.Less(Mix.StageCutoff(s), last, "stage " + s + " is duller than " + (s - 1)); last = Mix.StageCutoff(s); }
            Assert.AreEqual(Mix.StageCutoff(3), Mix.StageCutoff(9), "clamped");
            var m = Fresh();
            m.Stage = 2;
            Assert.AreEqual(Mix.StageCutoff(2), m.Cutoff(Mix.Bus.Ambience));
            Assert.AreEqual(Mix.Open, m.Cutoff(Mix.Bus.Music), "the stage is the ambience's alone");
            m.Snap(Mix.Snapshot.Blank);
            Assert.AreEqual(Mathf.Min(1800f, Mix.StageCutoff(2)), m.Cutoff(Mix.Bus.Ambience), "the duller of the two");
        }

        [Test]
        public void ThePlayersVolumesSitOnTop()
        {
            var m = Fresh();
            Assert.AreEqual(1f, m.Gain(Mix.Bus.Music));
            Vol[Options.Volume.Master] = 0.5f;
            Vol[Options.Volume.Music] = 0.5f;
            Vol[Options.Volume.Sound] = 0.8f;
            Vol[Options.Volume.Voices] = 0f;
            Assert.AreEqual(0.5f, m.Gain(Mix.Bus.Master), 1e-4f);
            Assert.AreEqual(0.25f, m.Gain(Mix.Bus.Music), 1e-4f, "master × music");
            Assert.AreEqual(0.4f, m.Gain(Mix.Bus.Sfx), 1e-4f, "master × sound");
            Assert.AreEqual(0.4f, m.Gain(Mix.Bus.Ambience), 1e-4f, "ambience and the UI follow sound");
            Assert.AreEqual(0.4f, m.Gain(Mix.Bus.Ui), 1e-4f);
            Assert.AreEqual(0f, m.Gain(Mix.Bus.Dialogue));
            m.Snap(Mix.Snapshot.Dialogue);
            m.Trigger(Mix.Duck.Line);
            m.Tick(Mix.Of(Mix.Duck.Line).Attack);   // pressed all the way, not yet letting go
            float expected = 0.5f * 0.5f * Mix.Of(Mix.Snapshot.Dialogue).Gain[(int)Mix.Bus.Music] * Mix.Of(Mix.Duck.Line).Floor[(int)Mix.Bus.Music];
            Assert.AreEqual(expected, m.Gain(Mix.Bus.Music), 1e-4f, "volumes × snapshot × duck");
            Vol[Options.Volume.Master] = 3f;
            Assert.LessOrEqual(m.Gain(Mix.Bus.Sfx), 0.8f, "never louder than designed");
        }

        [Test]
        public void DecibelsAndRegions()
        {
            Assert.AreEqual(0f, Mix.ToDb(1f), 1e-4f);
            Assert.AreEqual(-6.02f, Mix.ToDb(0.5f), 0.01f);
            Assert.AreEqual(-80f, Mix.ToDb(0f));
            Assert.AreEqual(0.5f, Mix.FromDb(Mix.ToDb(0.5f)), 1e-4f);
            Assert.AreEqual(0f, Mix.FromDb(-80f));

            Assert.AreEqual(Region.Saltmarrow, Mix.RegionOf("Greybox_Saltmarrow_A"));
            Assert.AreEqual(Region.Blank, Mix.RegionOf("Greybox_Blank_Island1"));
            Assert.AreEqual(Region.Blank, Mix.RegionOf("Island_Merrow"), "the Blank's islands are runtime rooms (AUD-08)");
            Assert.AreEqual(Region.Halden, Mix.RegionOf("Epilogue_Halden_JourneymansHall"), "the epilogue's stand-ins are named for their zones");
            Assert.AreEqual(Region.Greyfold, Mix.RegionOf("Greybox_Greyfold_Edge"));
            Assert.IsNull(Mix.RegionOf("Persistent"));
            Assert.IsNull(Mix.RegionOf(null));
            var planned = RoomPlans.All[0];
            Assert.AreEqual(System.Enum.Parse(typeof(Region), planned.Zone.Split('.')[0]), Mix.RegionOf(planned.Id), "a planned room by its zone");
        }

        [Test]
        public void TheVolumesAreOptionsInTenths()
        {
            Options.ResetToDefaults();
            try
            {
                foreach (Options.Volume v in System.Enum.GetValues(typeof(Options.Volume))) Assert.AreEqual(1f, Options.Get(v), v + " starts as designed");
                Options.Set(Options.Volume.Music, 0.33f);
                Assert.AreEqual(0.3f, Options.Get(Options.Volume.Music), 1e-4f, "tenths");
                Options.Set(Options.Volume.Master, -1f);
                Assert.AreEqual(0f, Options.Get(Options.Volume.Master));
                Options.Set(Options.Volume.Voices, 2f);
                Assert.AreEqual(1f, Options.Get(Options.Volume.Voices));
                Options.Load();
                Assert.AreEqual(0.3f, Options.Get(Options.Volume.Music), 1e-4f, "kept");
                Assert.AreEqual(Options.Volume.Music, Mix.VolumeOf(Mix.Bus.Music));
                Assert.AreEqual(Options.Volume.Sound, Mix.VolumeOf(Mix.Bus.Ambience));
                Assert.AreEqual(Options.Volume.Voices, Mix.VolumeOf(Mix.Bus.Dialogue));
                foreach (var k in Options.Keys) if (k.Contains("volume")) StringAssert.StartsWith(Options.Prefix + "volume.", k);
            }
            finally { Options.ResetToDefaults(); }
        }
    }
}
