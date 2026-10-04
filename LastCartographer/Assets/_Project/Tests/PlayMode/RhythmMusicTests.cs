#nullable enable
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The rhythm bosses' music in play (AUD-13, docs/design/music.md §3d): the Collapse's beat sings on through hitstop,
    /// so it keeps real time; its theme comes with the fight and lands on the fight's beat, and after a pause it is put
    /// back on it; the Complete Survey's theme changes ink, key and beat with its phase and keeps the new beat.
    /// </summary>
    public class RhythmMusicTests
    {
        GameObject? _room;
        MusicDriver Driver => MusicDriver.Instance!;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(MusicDriver.Instance);
            MixDriver.Instance!.RoomOverride = "Persistent";
            MixDriver.Instance.Mixer.Snap(Mix.Snapshot.Explore);
            MixDriver.Instance.Mixer.Tick(30f);
            Driver.RoomOverride = null;
            _room = new GameObject("Room_Rhythm");
            _room.AddComponent<Room>();
            yield return AudioClock.Measure();
        }

        [TearDown]
        public void TearDown()
        {
            if (_room != null) Object.Destroy(_room);
            Pause.End();
            Time.timeScale = 1f;
            Driver.RoomOverride = null;
            MixDriver.Instance!.RoomOverride = null;
            GameState.NewGame();
        }

        T Build<T>(string id) where T : Boss => (T)BossKits.Build(id, _room!.transform, new Vector2(300f, 0f)).Boss;

        IEnumerator Until(System.Func<bool> done, float seconds, string what)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator TheCollapsesBeatSingsOnThroughHitstop()
        {
            var c = Build<Collapse>("collapse");
            c.waitSeconds = 999f;   // no attacks: only the chorus
            yield return null;
            c.BeginFight();
            double b0 = c.BeatsInto, frozen0 = Hitstop.FrozenSeconds;
            float r0 = Time.realtimeSinceStartup;
            float prev = Options.Hitstop;
            Options.Hitstop = 1f;
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    Hitstop.Request(8);
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                yield return new WaitForSecondsRealtime(0.2f);
                yield return new WaitForFixedUpdate();
            }
            finally { Options.Hitstop = prev; }
            Assert.Greater(Hitstop.FrozenSeconds - frozen0, 0.5, "eight hits held time still for over half a second");
            double sung = (c.BeatsInto - b0) * c.BeatSeconds, real = Time.realtimeSinceStartup - r0;
            Assert.AreEqual(real, sung, 0.12, "the chorus sang on through every freeze: its beats are real time");
        }

        [UnityTest]
        public IEnumerator TheCollapsesThemeLandsOnTheFightsBeatAndIsPutBackAfterAPause()
        {
            AudioClock.NeedsRealtime();
            Driver.RoomOverride = "Greybox_Emberdown_Hollow_1";
            var c = Build<Collapse>("collapse");
            c.waitSeconds = 0.3f;
            yield return null;
            var theme = Score.ThemeOfBoss("Collapse");
            c.BeginFight();
            yield return Until(() => Driver.Wanted == theme, 5f, "the first telegraph to bring its theme");
            yield return Until(() => Driver.Current == theme, 60f, "its theme to render");
            yield return new WaitForSecondsRealtime((float)MusicDriver.LeadIn + MusicDriver.FadeSeconds + 0.5f);
            for (int i = 0; i < 20; i++)
            {
                Assert.Less(Mathf.Abs(Driver.BeatDrift), MusicDriver.ResyncSeconds * 2f, "the pulse lands on the fight's beat (" + Driver.BeatDrift + " s)");
                yield return null;
            }
            int resyncs = Driver.Resyncs;
            Pause.Begin();
            yield return new WaitForSecondsRealtime(0.7f);
            Pause.End();
            yield return Until(() => Driver.Resyncs > resyncs, 2f, "the music put back on the beat");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less(Mathf.Abs(Driver.BeatDrift), MusicDriver.ResyncSeconds * 2f, "back on the fight's beat after the pause");
        }

        [UnityTest]
        public IEnumerator TheSurveysThemeChangesInkWithItsPhaseAndKeepsTheNewBeat()
        {
            AudioClock.NeedsRealtime();
            Driver.RoomOverride = "Greybox_Halden_Observatory_1";
            var s = Build<CompleteSurvey>("complete_survey");
            yield return null;
            s.BeginFight();
            var tide = Score.ThemeOfBoss("CompleteSurvey", Region.Halden, 1);
            yield return Until(() => Driver.Current == tide, 60f, "the tide's theme");
            Assert.AreEqual(Region.Saltmarrow, Driver.Current!.Region, "phase 1 in the coast's key, though fought in Halden");
            typeof(Boss).GetMethod("AdvanceToPhase", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(s, new object[] { 2 });
            Assert.AreEqual(2, s.Phase);
            var ash = Score.ThemeOfBoss("CompleteSurvey", Region.Halden, 2);
            yield return null;
            Assert.AreSame(ash, Driver.Wanted, "the ink changes with the phase");
            yield return Until(() => Driver.Current == ash, 60f, "the ash's theme");
            Assert.AreEqual(s.BeatSeconds, Driver.Current!.Beat, 1e-4f, "at Emberdown's beat, the phase's");
            yield return new WaitForSecondsRealtime((float)MusicDriver.LeadIn + MusicDriver.FadeSeconds + 0.5f);
            for (int i = 0; i < 20; i++)
            {
                Assert.Less(Mathf.Abs(Driver.BeatDrift), MusicDriver.ResyncSeconds * 2f, "on the new beat (" + Driver.BeatDrift + " s)");
                yield return null;
            }
        }
    }
}
