using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The tuning pass (CMB-19, docs/design/tuning.md): telegraph frames fall with the tier, boss health follows tier and
    /// access with every sheet in the table, enemy families, the Inkwell's trades, the Charters' quill damage against
    /// the Surveyor's, and the scenes carrying the table's numbers.
    /// </summary>
    public class TuningTests
    {
        [Test]
        public void TelegraphsShortenWithTheTier()
        {
            CollectionAssert.AreEqual(new[] { 12, 11, 10, 8 }, Enumerable.Range(1, 4).Select(Tuning.TelegraphFloor).ToArray(), "combat doc 8: 12+ at Tier I, 8+ at Tier IV");
            CollectionAssert.AreEqual(new[] { 18, 16, 14, 12 }, Enumerable.Range(1, 4).Select(Tuning.TypicalTelegraph).ToArray(), "300 ms at Tier I down to 200 ms at Tier IV");
            for (int t = 1; t <= 4; t++)
                Assert.GreaterOrEqual(Tuning.TypicalTelegraph(t) - Tuning.TelegraphSpread, Tuning.TelegraphFloor(t), "the typical read never reaches down to the floor");
            Assert.AreEqual(12, Tuning.TelegraphFloor(0), "out of range clamps");
            Assert.AreEqual(8, Tuning.TelegraphFloor(9));
            Assert.IsTrue(Tuning.TypicalFits(3, 15.5f));
            Assert.IsFalse(Tuning.TypicalFits(4, 16f));
        }

        [Test]
        public void BossHealthFollowsTierAndAccess()
        {
            foreach (var sheet in Bosses.All)
            {
                Assert.IsTrue(Tuning.TryBoss(sheet.Id, out var t), sheet.Id + " is in the table");
                Assert.AreEqual(sheet.Tier, t.Tier, sheet.Id + ": the table's tier is the sheet's");
                Assert.Greater(t.Health, 0, sheet.Id);
            }
            Assert.AreEqual(Bosses.All.Count, Tuning.Bosses.Count(), "and nothing else");

            for (int tier = 2; tier <= 4; tier++) Assert.Greater(Tuning.BossBase(tier), Tuning.BossBase(tier - 1), "more strikes each tier");
            Assert.Less(Tuning.AccessScale(BossAccess.Paced), Tuning.AccessScale(BossAccess.Windowed));
            Assert.Less(Tuning.AccessScale(BossAccess.Windowed), Tuning.AccessScale(BossAccess.Open));

            // An Open boss is the tier's base; a boss of a higher tier with the same access never has less health.
            foreach (var a in Tuning.Bosses.Where(b => b.Access != BossAccess.Counted))
            foreach (var b in Tuning.Bosses.Where(b => b.Access == a.Access && b.Tier > a.Tier))
                Assert.GreaterOrEqual(b.Health, a.Health, b.Id + " (tier " + b.Tier + ") against " + a.Id + " (tier " + a.Tier + ")");

            Assert.AreEqual(24, Tuning.BossHealth("lamp_keeper"), "the first fight keeps its 24");
            Assert.AreEqual(30, Tuning.BossHealth("halvard"));
            Assert.AreEqual(4, Tuning.BossHealth("bells"), "four ropes");
            Assert.AreEqual(38, Tuning.BossHealth("voss"));
            Assert.AreEqual(25, Tuning.BossHealth("complete_survey"));
            Assert.AreEqual(0, Tuning.BossHealth("nobody"));
        }

        [Test]
        public void EnemyFamiliesAreTunedAndHitForOne()
        {
            foreach (var e in Tuning.Enemies)
            {
                Assert.GreaterOrEqual(e.Health, 2, e.Family + ": nothing dies to a stray strike");
                Assert.LessOrEqual(e.Health, 5, e.Family + ": no enemy is a boss");
                Assert.AreEqual(Tuning.Hit, e.Contact, e.Family + ": contact takes one mask");
            }
            Assert.IsTrue(Tuning.TryEnemy("ReedSkimmer", out var skimmer));
            Assert.IsTrue(Tuning.TryEnemy("Warden", out var warden));
            Assert.Less(skimmer.Health, warden.Health, "fodder falls quicker than a lancer");
            Assert.IsFalse(Tuning.TryEnemy("TrainingDummy", out _), "families the table doesn't know keep their scene's numbers");
        }

        [Test]
        public void TheInkwellIsATempoResource()
        {
            // Every pip is a strike earned: a damage Flourish on one target gives back at least the strikes it cost.
            float crosshatch = Tuning.DamagePerPip(Tuning.CrosshatchHits * Tuning.CrosshatchDamage, Tuning.CrosshatchCost);
            float longstroke = Tuning.DamagePerPip(Tuning.LongstrokeDamage, Tuning.LongstrokeCost);
            Assert.GreaterOrEqual(longstroke, 1f, "Longstroke is never a loss on one target (it was 2 for 3)");
            Assert.GreaterOrEqual(crosshatch, 2f, "Crosshatch doubles the ink it spends");
            Assert.Greater(crosshatch, longstroke, "combat doc 4: Crosshatch is the best single-target damage");
            Assert.Greater(Tuning.BlotCost, Tuning.CrosshatchCost, "Blot is the dear one: it answers swarms, not bosses");

            Assert.AreEqual(3, Tuning.BindCost / Tuning.InkPerStrike, "three strikes a mask");
            float surveyor = CharterProfile.Surveyor().InkGainMultiplier;
            Assert.AreEqual(2.4f, Tuning.BindCost / (Tuning.InkPerStrike * surveyor), 1e-4f, "the Surveyor's quicker ink: 2.4");
            Assert.AreEqual(3, Tuning.InkMax / Tuning.BindCost, "a full well is three masks");
            Assert.Greater(Tuning.TincturePips, Tuning.BindCost, "a tincture is more than one Bind");
            Assert.Less(Tuning.TincturePips, Tuning.InkMax, "and less than a full well");
            Assert.Less(Tuning.InkthreadCost, Tuning.BindCost, "the thread is cheap enough to use in a fight");
            Assert.AreEqual(10f, Tuning.VantageSecondsPerPip);

            // What the fights pay: even the least-struck boss earns more ink than a full well.
            foreach (var b in Tuning.Bosses.Where(b => b.Access != BossAccess.Counted))
                Assert.Greater(b.Health * Tuning.InkPerStrike, Tuning.InkMax, b.Id);
        }

        [Test]
        public void NoCharterOutstripsTheSurveyorByMuch()
        {
            float surveyor = CharterProfile.Surveyor().QuillDamagePerSecond;
            Assert.AreEqual(3.6f, surveyor, 0.01f, "slash, slash, thrust: 3 damage in 50 frames");
            foreach (CharterKind k in System.Enum.GetValues(typeof(CharterKind)))
            {
                var p = CharterProfile.For(k);
                float ratio = p.QuillDamagePerSecond / surveyor;
                Assert.GreaterOrEqual(ratio, 0.85f, k + " is not a handicap");
                Assert.LessOrEqual(ratio, 1.75f, k + " pays for its damage elsewhere (masks, reach, dash)");
            }
            Assert.Greater(CharterProfile.Drifter().QuillDamagePerSecond, surveyor, "the glass Charter hits fastest");
            Assert.AreEqual(4, CharterProfile.Drifter().MaskCap, "because she has four masks");
        }

        [Test]
        public void TheScenesCarryTheTablesNumbers()
        {
            var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
            try
            {
                var persistent = Open("Assets/_Project/Scenes/Persistent/Persistent.unity", opened);
                var fl = Find<Flourishes>(persistent);
                Assert.AreEqual(Tuning.LongstrokeDamage, fl.longstrokeDamage, "Wren's Longstroke");
                Assert.AreEqual(Tuning.CrosshatchCost, fl.crosshatchCost);
                Assert.AreEqual(Tuning.LongstrokeCost, fl.longstrokeCost);
                Assert.AreEqual(Tuning.BlotCost, fl.blotCost);
                Assert.AreEqual(Tuning.CrosshatchHits, fl.crosshatchHits);
                Assert.AreEqual(Tuning.BindCost, new SerializedObject(Find<WrenVitals>(persistent)).FindProperty("_bindCost").intValue);
                Assert.AreEqual(Tuning.InkMax, new SerializedObject(Find<Inkwell>(persistent)).FindProperty("_maxPips").intValue);
                Assert.AreEqual(Tuning.TincturePips, Find<InstrumentBelt>(persistent).tincturePips);
                Assert.AreEqual(Tuning.InkthreadCost, Find<WrenController>(persistent).threadCost);

                var lighthouse = Open("Assets/_Project/Scenes/Greybox/Greybox_Saltmarrow_Lighthouse.unity", opened);
                var lk = Find<LampKeeper>(lighthouse);
                Assert.AreEqual(Tuning.BossHealth("lamp_keeper"), lk.MaxHealth);
                Assert.AreEqual(18, lk.beamTelegraphFrames, "Tier I reads at 300 ms again");
                Assert.AreEqual(20, lk.diveTelegraphFrames);

                var chapel = Open("Assets/_Project/Scenes/Greybox/Greybox_Saltmarrow_Chapel.unity", opened);
                var hv = Find<Halvard>(chapel);
                Assert.AreEqual(Tuning.BossHealth("halvard"), hv.MaxHealth);
                CollectionAssert.AreEqual(new[] { 14, 17, 19, 22 },
                    new[] { hv.thrustTelegraphFrames, hv.lungeTelegraphFrames, hv.surveyTelegraphFrames, hv.countTelegraphFrames });
            }
            finally
            {
                for (int i = opened.Count - 1; i >= 0; i--) EditorSceneManager.CloseScene(opened[i], true);
            }
        }

        static UnityEngine.SceneManagement.Scene Open(string path, System.Collections.Generic.List<UnityEngine.SceneManagement.Scene> opened)
        {
            var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            opened.Add(s);
            return s;
        }

        static T Find<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<T>(true);
                if (c != null) return c;
            }
            Assert.Fail(typeof(T).Name + " in " + scene.name);
            return null;
        }
    }
}
