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
        public IEnumerator AnotherRegionBringsItsOwnBed()
        {
            yield return PlayTheCoast();
            Driver.RoomOverride = "Greybox_Greyfold_Edge";
            MixDriver.Instance!.RoomOverride = "Greybox_Greyfold_Edge";
            yield return null;
            Assert.AreEqual(Region.Greyfold, Driver.Wanted);
            yield return Until(() => Driver.Current == Region.Greyfold, 60f);
            Assert.AreEqual(Region.Greyfold, Driver.Current);
            Assert.IsNull(Driver.Source("reeds"), "the coast's layers are gone");
            Assert.IsNotNull(Driver.Source("footsteps, too close"), "the Greyfold's are here");
            Assert.AreEqual(2, Driver.LayersLeft);
        }
    }
}
