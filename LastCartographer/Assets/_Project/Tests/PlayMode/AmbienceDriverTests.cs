#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The ambience in the game (AUD-05): the driver wakes with the game and plays the room's region's layers as
    /// loops of different lengths under the Ambience bus; the place's fade stage takes the last layer away and dulls
    /// the rest through the bus's filter; an erased place is silent; another region's room brings its own layers.
    /// </summary>
    public class AmbienceDriverTests
    {
        AmbienceDriver Driver => AmbienceDriver.Instance!;
        const string Room = "Greybox_Saltmarrow_Amb";

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(AmbienceDriver.Instance, "booted with the first scene");
            Assert.IsNotNull(MixDriver.Instance);
            MixDriver.Instance!.Mixer.Snap(Mix.Snapshot.Explore);
            MixDriver.Instance.Mixer.Tick(30f);
            Driver.RoomOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            Driver.RoomOverride = null;
            MixDriver.Instance!.RoomOverride = null;
            Pause.End();
            GameState.NewGame();
        }

        IEnumerator Until(System.Func<bool> done, float seconds)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        IEnumerator PlayTheCoast()
        {
            Driver.RoomOverride = Room;
            MixDriver.Instance!.RoomOverride = Room;
            yield return null;
            Assert.AreEqual(Region.Saltmarrow, Driver.Wanted);
            yield return Until(() => Driver.Current == Region.Saltmarrow, 60f);
            Assert.AreEqual(Region.Saltmarrow, Driver.Current, "rendered on a worker and started");
            yield return new WaitForSecondsRealtime(AmbienceDriver.FadeSeconds + 0.3f);
        }

        [UnityTest]
        public IEnumerator TheRoomsRegionPlaysItsLayersUnderTheBus()
        {
            Assert.IsNull(Driver.Wanted, "no room, no bed");
            yield return PlayTheCoast();
            var layers = Ambience.Of(Region.Saltmarrow);
            Assert.AreEqual(5, layers.Count);
            var lengths = layers.Select(l => Driver.Source(l.Name)!.clip.length).ToArray();
            Assert.AreEqual(lengths.Length, lengths.Distinct().Count(), "loops of different lengths");
            float gain = Mix.Live!.Gain(Mix.Bus.Ambience);
            foreach (var l in layers)
            {
                var src = Driver.Source(l.Name)!;
                Assert.IsTrue(src.isPlaying && src.loop, l.Name);
                Assert.AreEqual(l.Level, Driver.Level(l.Name), 0.02f, l.Name + " at its level");
                Assert.AreEqual(l.Level * gain, src.volume, 0.02f, l.Name + " under the Ambience bus");
                Assert.AreEqual(Mix.Open, Driver.Filter(l.Name)!.cutoffFrequency, 1f, "a standing place: open");
            }
            Assert.AreEqual(5, Driver.LayersLeft);
        }

        [UnityTest]
        public IEnumerator TheFadeTakesALayerAStageAndDullsTheRest()
        {
            yield return PlayTheCoast();
            var layers = Ambience.Of(Region.Saltmarrow);
            FadeStages.Advance(GameState.World, "Saltmarrow_Amb", 2);
            Assert.AreEqual(2, Driver.Stage);
            Assert.AreEqual(3, Driver.LayersLeft, "two stages gone: two layers gone");
            yield return new WaitForSecondsRealtime(AmbienceDriver.FadeSeconds + 0.4f);
            Assert.AreEqual(0f, Driver.Level(layers[4].Name), 0.01f, "the bell buoy went first");
            Assert.AreEqual(0f, Driver.Level(layers[3].Name), 0.01f, "then the gulls");
            Assert.Greater(Driver.Level(layers[2].Name), 0f, "the rain stays");
            Assert.AreEqual(Mix.StageCutoff(2), Driver.Filter(layers[0].Name)!.cutoffFrequency, 1f, "and what is left is dulled");
            FadeStages.Advance(GameState.World, "Saltmarrow_Amb", FadeStages.Max);
            Assert.AreEqual(0, Driver.LayersLeft, "erased: silent");
            yield return new WaitForSecondsRealtime(AmbienceDriver.FadeSeconds + 0.4f);
            foreach (var l in layers) Assert.AreEqual(0f, Driver.Level(l.Name), 0.01f, l.Name + " gone");
        }

        [UnityTest]
        public IEnumerator TheSparseLayersComeFromAPointInTheRoom()
        {
            yield return PlayTheCoast();
            var layers = Ambience.Of(Region.Saltmarrow);
            foreach (var l in layers)
            {
                var src = Driver.Source(l.Name)!;
                if (!l.Point)
                {
                    Assert.AreEqual(0f, src.spatialBlend, l.Name + " is everywhere");
                    Assert.IsNull(Driver.Position(l.Name));
                    continue;
                }
                Assert.AreEqual(1f, src.spatialBlend, l.Name + " comes from a point");
                Assert.AreEqual(AudioRolloffMode.Linear, src.rolloffMode);
                Assert.AreEqual(AmbienceDriver.PointNear, src.minDistance, 0.01f);
                Assert.AreEqual(0f, src.dopplerLevel);
                var p = Driver.Position(l.Name)!.Value;
                Assert.That(p.x, Is.InRange(-AmbienceDriver.DefaultSpan / 2f, AmbienceDriver.DefaultSpan / 2f), l.Name + " inside the span");
                Assert.AreEqual(p.x, src.transform.position.x, 0.001f); Assert.AreEqual(p.y, src.transform.position.y, 0.001f);
            }
            Assert.IsFalse(layers[0].Point, "the tide is everywhere"); Assert.IsFalse(layers[2].Point, "so is the rain");
            Assert.IsTrue(layers[3].Point && layers[4].Point, "the gulls and the bell buoy are somewhere");
            var gulls = Driver.Position("gulls far off")!.Value; var buoy = Driver.Position("a bell buoy")!.Value;
            Assert.Greater(gulls.y, buoy.y, "the gulls are high, the buoy low");
            Assert.AreNotEqual(gulls.x, buoy.x, "not in the same place");
            var again = Ambience.PointIn(layers[4], Room, -8f, 8f, 0f, 8f);
            Assert.AreEqual(buoy.x, again.x, 0.001f); Assert.AreEqual(buoy.y, again.y, 0.001f, "the same room, the same spot, every time");
            Assert.AreNotEqual(again.x, Ambience.PointIn(layers[4], "Greybox_Saltmarrow_B", -8f, 8f, 0f, 8f).x, "another room, another spot");
        }

        /// <summary>The crossfade runs on the DSP clock: where that is not real time (<see cref="AudioClock"/>) its test steps aside.</summary>
        [UnitySetUp]
        public IEnumerator MeasureTheClock() => AudioClock.Measure();

        [UnityTest]
        public IEnumerator AnotherRegionBringsItsOwnBed()
        {
            AudioClock.NeedsRealtime();
            yield return PlayTheCoast();
            Driver.RoomOverride = "Greybox_Greyfold_Edge";
            MixDriver.Instance!.RoomOverride = "Greybox_Greyfold_Edge";
            yield return null;
            Assert.AreEqual(Region.Greyfold, Driver.Wanted);
            yield return Until(() => Driver.Current == Region.Greyfold, 60f);
            Assert.AreEqual(Region.Greyfold, Driver.Current);
            Assert.IsNull(Driver.Source("reeds"), "the coast's layers are no longer the room's");
            Assert.IsNotNull(Driver.Source("footsteps, too close"), "the Greyfold's are here");
            Assert.AreEqual(2, Driver.LayersLeft);
            // AUD-06: a crossfade, not a cut: the coast's layers go out over the fade as the Greyfold's come in.
            Assert.AreEqual(Ambience.Of(Region.Saltmarrow).Count, Driver.OutgoingCount, "the coast's layers are still going out");
            Assert.Greater(Driver.OutgoingVolume, 0f);
            yield return new WaitForSecondsRealtime(AmbienceDriver.FadeSeconds * 0.5f);
            Assert.Greater(Driver.OutgoingVolume, 0f, "halfway: still there, lower");
            Assert.Greater(Driver.Level("footsteps, too close"), 0f, "and the Greyfold's coming in under them");
            yield return new WaitForSecondsRealtime(AmbienceDriver.FadeSeconds * 0.6f);
            Assert.AreEqual(0, Driver.OutgoingCount, "faded and gone");
        }
    }
}
