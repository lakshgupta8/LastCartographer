#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The music in the game (AUD-04): the driver wakes with the game and plays the room's region theme as stems in
    /// step, its combat drive comes in with combat, a boss's theme arrives with the first telegraph and adds a stem a
    /// phase on the bar line and resolves on the answer when the boss falls, and every stem sits under the Music
    /// bus's gain and low-pass.
    /// </summary>
    public class MusicDriverTests
    {
        sealed class Lamp : Boss
        {
            public override string Family => "LampKeeper";
            protected override void Tick(float dt) { }
            public void Poke() => Tell(AttackKind.Strike);
        }

        sealed class Cinder : Boss
        {
            public override string Family => "Brann";
            protected override void Tick(float dt) { }
            public void Poke() => Tell(AttackKind.Strike);
        }

        sealed class Stone : Boss
        {
            public override string Family => "Hale";   // an optional: no theme of its own
            protected override void Tick(float dt) { }
            public void Poke() => Tell(AttackKind.Strike);
        }

        GameObject? _boss;
        MusicDriver Driver => MusicDriver.Instance!;

        T MakeBoss<T>() where T : Boss
        {
            _boss = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            _boss.transform.position = new Vector3(6f, 1f, 0f);
            _boss.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            _boss.AddComponent<Rigidbody2D>();
            return _boss.AddComponent<T>();
        }

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(MusicDriver.Instance, "booted with the first scene");
            Assert.IsNotNull(MixDriver.Instance);
            MixDriver.Instance!.RoomOverride = "Persistent";
            MixDriver.Instance.Mixer.Snap(Mix.Snapshot.Explore);
            MixDriver.Instance.Mixer.Tick(30f);
            Driver.RoomOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_boss != null) Object.Destroy(_boss);
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
            Driver.RoomOverride = "Greybox_Saltmarrow_A";
            var salt = Score.ThemeOf(Region.Saltmarrow);
            yield return null;
            Assert.AreSame(salt, Driver.Wanted, "the coast's theme is asked for");
            yield return Until(() => Driver.Current == salt, 30f);
            Assert.AreSame(salt, Driver.Current, "rendered on a worker and started");
            yield return new WaitForSecondsRealtime((float)MusicDriver.LeadIn + 0.2f);
        }

        [UnityTest]
        public IEnumerator TheRegionThemePlaysAsStemsInStep()
        {
            Assert.IsNull(Driver.Decide(), "no room, no theme");
            yield return PlayTheCoast();
            var salt = Score.ThemeOf(Region.Saltmarrow);
            var sources = salt.Stems.Select(s => Driver.Source(s.Id)!).ToArray();
            foreach (var src in sources) { Assert.IsNotNull(src); Assert.IsTrue(src.isPlaying, src.name); Assert.IsTrue(src.loop); Assert.AreEqual(0f, src.spatialBlend); }
            int min = sources.Min(s => s.timeSamples), max = sources.Max(s => s.timeSamples);
            Assert.Less(max - min, Score.SampleRate / 20, "every stem on the same clock: within 50 ms");
            Assert.AreEqual(salt.LoopSeconds, sources[0].clip.length, 0.01f, "the loop is the theme's bars");
            yield return new WaitForSecondsRealtime(MusicDriver.FadeSeconds + 0.2f);
            foreach (var stem in salt.Stems)
            {
                if (stem.Combat) { Assert.AreEqual(0f, Driver.Level(stem.Id), 0.001f, "the drive waits for a fight"); continue; }
                Assert.AreEqual(stem.Level, Driver.Level(stem.Id), 0.01f, stem.Id + " at its level");
                Assert.AreEqual(stem.Level * Mix.Live!.Gain(Mix.Bus.Music), Driver.Source(stem.Id)!.volume, 0.01f, stem.Id + " under the Music bus");
                Assert.IsNotNull(Driver.Filter(stem.Id), stem.Id + " is filtered like the bus");
            }
        }

        [UnityTest]
        public IEnumerator CombatBringsTheDriveIn()
        {
            yield return PlayTheCoast();
            var drive = Score.ThemeOf(Region.Saltmarrow).Stems.Single(s => s.Combat);
            Assert.IsFalse(Driver.StemOn(drive));
            Mix.Note(Mix.Duck.Impact);
            yield return null;
            Assert.IsTrue(Driver.StemOn(drive), "a blow landed: the fight's pulse");
            yield return new WaitForSecondsRealtime(MusicDriver.FadeSeconds + 0.2f);
            Assert.AreEqual(drive.Level, Driver.Level(drive.Id), 0.01f, "faded in, not cut");
            MixDriver.Instance!.Mixer.Tick(Mix.CombatHold + 1f);
            yield return null;
            Assert.IsFalse(Driver.StemOn(drive), "the fight over, it goes");
        }

        [UnityTest]
        public IEnumerator ABossThemeArrivesWithTheFirstTelegraphAddsAStemAPhaseAndResolves()
        {
            yield return PlayTheCoast();
            _boss = new GameObject("LampKeeper") { layer = Layer("Enemy") };
            _boss.transform.position = new Vector3(6f, 1f, 0f);
            _boss.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            _boss.AddComponent<Rigidbody2D>();
            var lamp = _boss.AddComponent<Lamp>();
            yield return null;
            var theme = Score.ThemeOfBoss("LampKeeper");
            lamp.BeginFight();
            yield return null;
            Assert.AreSame(Score.ThemeOf(Region.Saltmarrow), Driver.Wanted, "the doors close: still the room's theme (the approach is the ambience's)");
            lamp.Poke();
            yield return null;
            Assert.AreSame(theme, Driver.Wanted, "the first telegraph: her theme");
            yield return Until(() => Driver.Current == theme, 30f);
            Assert.AreSame(theme, Driver.Current);
            Assert.AreEqual(1, Driver.Phase);
            yield return new WaitForSecondsRealtime((float)MusicDriver.LeadIn + MusicDriver.FadeSeconds + 0.2f);
            foreach (var s in theme.Stems) Assert.AreEqual(s.Phase <= 1 ? s.Level : 0f, Driver.Level(s.Id), 0.02f, s.Id + " in phase 1");

            for (int i = 0; i < 40 && lamp.Phase < 2; i++) lamp.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.AreEqual(2, lamp.Phase, "under two thirds: phase 2");
            yield return null;
            Assert.AreEqual(2, Driver.PendingPhase, "the second line waits for the bar line");
            yield return Until(() => Driver.Phase == 2, theme.BarSeconds + 1f);
            Assert.AreEqual(2, Driver.Phase, "taken on the bar line");
            Assert.IsTrue(Driver.StemOn(theme.Stem("voices")));
            Assert.IsFalse(Driver.StemOn(theme.Stem("bells")), "the toll waits for phase 3");

            lamp.TakeHit(new HitInfo { Damage = lamp.Health, Direction = Vector2.right });
            Assert.IsTrue(lamp.IsDead);
            yield return null;
            Assert.IsTrue(Driver.IsResolving, "the theme resolves on the roll-call's answer");
            Assert.IsNull(Driver.Current, "the stems are gone");
            Assert.IsNull(Driver.Decide(), "and nothing plays over the answer");
            Assert.IsNull(Driver.Boss);
        }

        [UnityTest]
        public IEnumerator ARegionChangeHandsOverOnTheBarLineAndCrossesOver()
        {
            yield return PlayTheCoast();
            yield return new WaitForSecondsRealtime(MusicDriver.FadeSeconds + 0.2f);
            yield return Until(() => Driver.OutgoingCount == 0, 30f);   // an earlier test's theme (the Steppe's, since ENV-07) hands over to the coast on its own bar line first
            var salt = Score.ThemeOf(Region.Saltmarrow);
            var ember = Score.ThemeOf(Region.Emberdown);
            double coastStart = Driver.StartAt;
            Driver.RoomOverride = "Greybox_Emberdown_Rest_1";
            yield return null;
            Assert.AreSame(ember, Driver.Wanted, "the highland's theme is asked for");
            if (!Driver.IsReady(ember)) Assert.AreSame(salt, Driver.Current, "the coast plays on while it renders (an earlier test may have rendered it already)");
            yield return Until(() => Driver.Current == ember, 60f);
            Assert.AreSame(ember, Driver.Current, "rendered and scheduled");
            double at = Driver.StartAt;
            double bars = (at - coastStart) / salt.BarSeconds;
            Assert.AreEqual(System.Math.Round(bars), bars, 0.01, "the highland's stems are scheduled on the coast's bar line (" + bars + " bars in)");
            Assert.Greater(at, AudioSettings.dspTime, "which is still ahead");
            Assert.AreEqual(salt.Stems.Count, Driver.OutgoingCount, "the coast's stems are still going, to hand over there");
            Assert.Greater(Driver.OutgoingVolume, 0f);
            Assert.AreEqual(0f, Driver.Level("lead"), 0.001f, "the highland waits for its bar line");
            yield return Until(() => AudioSettings.dspTime >= at + 0.4, (float)salt.BarSeconds + 1f);
            Assert.Greater(Driver.Level("lead"), 0f, "past the bar line the highland's lead is coming in");
            Assert.Less(Driver.Level("lead"), ember.Stem("lead").Level * 0.6f, "over the crossfade, not cut");
            Assert.Greater(Driver.OutgoingVolume, 0f, "and the coast is going out under it");
            Assert.Less(Driver.OutgoingVolume, salt.Stems.Max(s => s.Level) * Mix.Live!.Gain(Mix.Bus.Music), "lower than it was");
            yield return new WaitForSecondsRealtime(MusicDriver.CrossfadeSeconds);
            Assert.AreEqual(0, Driver.OutgoingCount, "the coast is gone");
            Assert.AreEqual(ember.Stem("lead").Level, Driver.Level("lead"), 0.02f, "the highland at its level");
            Assert.AreEqual(ember.LoopSeconds, Driver.Source("lead")!.clip.length, 0.01f, "its loop: nine bars and one of rest");
        }

        [UnityTest]
        public IEnumerator ABossThemeChangesALayerOnTheBarLineAndAnOptionalFightsToItsRegionsMotif()
        {
            // Brann in the Furnace Stair: his theme at the first telegraph; phase 3 takes the furnace's roar away on the bar line and brings the glow.
            Driver.RoomOverride = "Greybox_Emberdown_Stair_3";
            var ember = Score.ThemeOf(Region.Emberdown);
            yield return Until(() => Driver.Current == ember, 60f);
            var brann = MakeBoss<Cinder>();
            yield return null;
            var theme = Score.ThemeOfBoss("Brann");
            brann.BeginFight();
            brann.Poke();
            yield return null;
            Assert.AreSame(theme, Driver.Wanted, "the first telegraph: his theme");
            yield return Until(() => Driver.Current == theme, 60f);
            Assert.AreSame(theme, Driver.Current);
            yield return new WaitForSecondsRealtime((float)MusicDriver.LeadIn + MusicDriver.FadeSeconds + 0.2f);
            Assert.IsTrue(Driver.StemOn(theme.Stem("bed")), "the furnace roars in phase 1");
            Assert.IsFalse(Driver.StemOn(theme.Stem("glow")));
            for (int i = 0; i < 80 && brann.Phase < 3; i++) brann.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.AreEqual(3, brann.Phase, "under a third: the furnace is dark");
            yield return null;
            Assert.AreEqual(3, Driver.PendingPhase);
            yield return Until(() => Driver.Phase == 3, theme.BarSeconds + 1f);
            Assert.AreEqual(3, Driver.Phase, "taken on the bar line");
            Assert.IsFalse(Driver.StemOn(theme.Stem("bed")), "the roar leaves: a layer changed, not only added");
            Assert.IsTrue(Driver.StemOn(theme.Stem("glow")), "only his brass glows");
            Assert.IsTrue(Driver.StemOn(theme.Stem("voices")), "the cross-cuts stay");
            yield return new WaitForSecondsRealtime(MusicDriver.FadeSeconds + 0.2f);
            Assert.AreEqual(0f, Driver.Level("bed"), 0.01f, "faded out");
            Assert.AreEqual(theme.Stem("glow").Level, Driver.Level("glow"), 0.02f, "faded in");
            brann.TakeHit(new HitInfo { Damage = brann.Health, Direction = Vector2.right });
            yield return null;
            Assert.IsTrue(Driver.IsResolving, "resolves in Emberdown's key");
            Object.Destroy(_boss); _boss = null;
            yield return Until(() => !Driver.IsResolving, 20f);

            // Surveyor Hale at the Nine Stones: no theme of her own, so Windreach's motif fought in, the flute in the second phase.
            Driver.RoomOverride = "Greybox_Windreach_Stones_1";
            var wind = Score.ThemeOf(Region.Windreach);
            yield return null;
            Assert.AreSame(wind, Driver.Wanted, "Windreach's theme in a Windreach room (" + Driver.Room + ")");
            var hale = MakeBoss<Stone>();
            yield return null;
            hale.BeginFight();
            hale.Poke();
            yield return null;
            var shared = Score.SharedThemeOf(Region.Windreach);
            Assert.AreSame(shared, Driver.Wanted, "an optional fights to its region's motif");
            yield return Until(() => Driver.Current == shared, 60f);
            Assert.AreSame(shared, Driver.Current);
            Assert.IsTrue(Driver.StemOn(shared.Stem("drive")), "the drive from the first telegraph");
            Assert.IsFalse(Driver.StemOn(shared.Stem("lead")), "the flute waits for the second phase");
            Assert.AreEqual(wind.Bars * wind.BarSeconds, Driver.Source("bed")!.clip.length, 0.01f, "the loop without its rest");
        }

        [UnityTest]
        public IEnumerator TheBlanksIslandsPlayTheBlankAndACodaEndsTheGame()
        {
            Driver.RoomOverride = "Island_Merrow";
            var blank = Score.ThemeOf(Region.Blank);
            yield return null;
            Assert.AreSame(blank, Driver.Wanted, "an island is the Blank's room");
            yield return Until(() => Driver.Current == blank, 60f);
            Assert.AreSame(blank, Driver.Current);
            Assert.AreEqual(blank.LoopSeconds, Driver.Source("lead")!.clip.length, 0.01f, "three bars and two of the white");
            Driver.BeginCoda(Ending.Fixed);
            var coda = Score.CodaOf(Ending.Fixed);
            yield return null;
            Assert.AreSame(coda, Driver.Wanted, "the ending's coda over everything");
            yield return Until(() => Driver.Current == coda, 60f);
            Assert.AreSame(coda, Driver.Current, "handed over on the Blank's bar line");
            Driver.RoomOverride = "Greybox_Saltmarrow_A";
            yield return null;
            Assert.AreSame(coda, Driver.Wanted, "no room changes it");
            GameState.NewGame();
            yield return null;
            Assert.IsNull(Driver.Coda, "a new game lets it go");
            Assert.AreSame(Score.ThemeOf(Region.Saltmarrow), Driver.Wanted);
        }

        [UnityTest]
        public IEnumerator TheStemsSitUnderTheMusicBus()
        {
            yield return PlayTheCoast();
            yield return new WaitForSecondsRealtime(MusicDriver.FadeSeconds + 0.2f);
            var bed = Driver.Source("bed")!;
            Assert.AreEqual(Mix.Open, Driver.Filter("bed")!.cutoffFrequency, 1f);
            Pause.Begin();
            try
            {
                yield return new WaitForSecondsRealtime(0.4f);
                var paused = Mix.Of(Mix.Snapshot.Paused);
                Assert.AreEqual(Score.ThemeOf(Region.Saltmarrow).Stem("bed").Level * paused.Gain[(int)Mix.Bus.Music], bed.volume, 0.02f, "through a wall: the theme keeps going, low");
                Assert.AreEqual(paused.Cutoff[(int)Mix.Bus.Music], Driver.Filter("bed")!.cutoffFrequency, 1f, "and dull");
                Assert.IsTrue(bed.isPlaying, "time stands still; the music does not");
            }
            finally { Pause.End(); }
        }
    }
}
