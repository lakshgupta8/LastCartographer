#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Halvard's second and third hunts (CMB-12, boss sheet 6.3), each built from its kit and fought to its answers: on
    /// the Seven Bridges the cord lance goes out level and comes back low, a survey marks spans and the count cuts them
    /// from under her, and phase 3 is the last span; at the Threshold the white eats the arena from the west and he
    /// stops counting in phase 3. The first hunt is unchanged: its pattern, its kit and its chapel floor.
    /// </summary>
    public class HalvardHuntsTests
    {
        GameObject? _room, _wren;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        AbilitySet? _abilities;
        BossKit? _kit;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _room = new GameObject("Room_Arena");
            _room.AddComponent<Room>();

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _abilities = _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<InstrumentBelt>();
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _ctrl.Teleport(new Vector2(-4f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_room != null) Object.Destroy(_room);
            if (_wren != null) Object.Destroy(_wren);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        /// <summary>Wound him a hit a frame until the condition holds (hurtstun may refuse some), within a few seconds.</summary>
        static IEnumerator WoundUntil(Halvard h, System.Func<bool> cond, string what)
        {
            for (int i = 0; i < 400 && !cond(); i++)
            {
                h.TakeHit(new HitInfo { Damage = 4, Direction = Vector2.right });
                yield return new WaitForFixedUpdate();
            }
            Assert.IsTrue(cond(), "wounding him never reached " + what);
        }

        Halvard Build(string id)
        {
            _kit = BossKits.Build(id, _room!.transform, Vector2.zero);
            Assert.IsNotNull(_kit, id + " has a kit");
            _kit!.Arena.IntroSeconds = 0.05f;
            _kit.Arena.RetryIntroSeconds = 0.02f;
            return (Halvard)_kit.Boss;
        }

        IEnumerator StartFight(float wrenX)
        {
            _ctrl!.Teleport(new Vector2(wrenX, 0f));
            yield return Fixed(2);
            yield return Until(() => _kit!.Arena.State == BossArena.ArenaState.Fighting, 3f, "the fight to start");
            yield return Fixed(2);
        }

        /// <summary>Three hunts, three kits: the patterns keep to the sheet and the tuning rules (at most four attacks a phase, every attack in some phase, the tier's telegraph floor).</summary>
        [Test]
        public void TheThreeHuntsKeepToTheirSheets()
        {
            foreach (Halvard.Hunt hunt in System.Enum.GetValues(typeof(Halvard.Hunt)))
                for (int p = 1; p <= 3; p++)
                {
                    var pattern = Halvard.PatternFor(hunt, p);
                    Assert.LessOrEqual(pattern.Distinct().Count(), 4, hunt + " phase " + p + ": no phase has more than four attacks");
                    Assert.IsFalse(pattern.Contains(Halvard.Attack.None));
                }
            CollectionAssert.AreEqual(Halvard.PatternFor(1), Halvard.PatternFor(Halvard.Hunt.Chapel, 1), "the first hunt is as it was");
            Assert.IsFalse(Halvard.PatternFor(Halvard.Hunt.Chapel, 3).Contains(Halvard.Attack.Throw), "no cord lance in the chapel");
            for (int p = 1; p <= 3; p++)
            {
                Assert.IsTrue(Halvard.PatternFor(Halvard.Hunt.Bridges, p).Contains(Halvard.Attack.Throw), "the bridges throw in every phase");
                Assert.IsTrue(Halvard.PatternFor(Halvard.Hunt.Threshold, p).Contains(Halvard.Attack.Throw), "the Threshold throws in every phase");
            }
            Assert.IsFalse(Halvard.PatternFor(Halvard.Hunt.Bridges, 3).Contains(Halvard.Attack.Survey), "nothing to mark on the last span");
            Assert.IsFalse(Halvard.PatternFor(Halvard.Hunt.Threshold, 3).Contains(Halvard.Attack.Survey), "he stops counting");
            Assert.IsFalse(Halvard.PatternFor(Halvard.Hunt.Threshold, 3).Contains(Halvard.Attack.Count));
            Assert.IsTrue(Halvard.PatternFor(Halvard.Hunt.Threshold, 1).Contains(Halvard.Attack.Count), "but counts before that");
        }

        [UnityTest]
        public IEnumerator TheKitsAreAuditedLikeTheOthers()
        {
            foreach (var id in BossKits.LaterHunts)
            {
                var h = Build(id);
                yield return null;
                Assert.AreEqual(Bosses.Find(id).Tier, h.Tier, id + "'s tier from its sheet");
                Assert.AreEqual(id == "halvard_2" ? Halvard.Hunt.Bridges : Halvard.Hunt.Threshold, h.CurrentHunt);
                Assert.AreEqual(Tuning.BossHealth(id), h.MaxHealth, id + "'s health from the table");
                var kit = h.Kit().ToList();
                Assert.AreEqual(5, kit.Count, id + ": thrust, lunge, survey, count and the cord lance");
                int floor = Tuning.TelegraphFloor(h.Tier);
                foreach (var a in kit)
                {
                    Assert.AreNotEqual(0, a.Phases, id + " " + a.Name + " is in some phase");
                    Assert.GreaterOrEqual(a.Telegraph, floor, id + " " + a.Name + " reads at the tier's floor");
                    if (a.Kind == AttackKind.Strike) Assert.AreEqual(Tuning.Hit, a.Damage, id + " " + a.Name);
                }
                Assert.IsTrue(kit.First(a => a.Name == "Cord lance").In(1) && kit.First(a => a.Name == "Cord lance").In(3));
                Object.Destroy(_room);
                _room = new GameObject("Room_Arena"); _room.AddComponent<Room>();
            }
        }

        [UnityTest]
        public IEnumerator OnTheBridgesTheCordLanceGoesOutLevelAndComesBackLow()
        {
            var h = Build("halvard_2");
            yield return StartFight(10f);   // inside the cord's reach of him (he stands at 15.5)
            Assert.IsTrue(h.HasSpans, "the floor under the arena is six spans now");
            Assert.AreEqual(6, h.Sections);
            Assert.AreEqual(0, h.Throws);
            int masks = _vitals!.Masks;
            h.ForceAttack(Halvard.Attack.Throw);
            yield return Until(() => h.IsLanceOut, 2f, "the throw");
            Assert.AreEqual(1, h.Throws);
            Assert.AreEqual("throw", h.Clip, "the release");
            var lance = h.CordLance!;
            Assert.IsNotNull(lance);
            Assert.Less(lance.Position.x, h.transform.position.x, "thrown toward her, west");
            Assert.AreEqual(h.floorY + h.lanceHeight, lance.Position.y, 0.01f, "level, at lance height");
            float startX = lance.Position.x;
            yield return Fixed(3);
            Assert.Less(lance.Position.x, startX, "and flying");
            yield return Until(() => _vitals.Masks < masks, 2f, "the lance to reach her");
            Assert.AreEqual(masks - 1, _vitals.Masks, "a strike");
            yield return Until(() => h.Current == Halvard.Move.Recall, 3f, "the recall");
            Assert.IsTrue(h.IsLanceOut);
            Assert.AreEqual(h.floorY + h.recallHeight, h.CordLance!.Position.y, 0.01f, "it comes back low: jump it");
            yield return Until(() => !h.IsLanceOut, 3f, "the lance home");
            Assert.AreEqual(Halvard.Move.Recover, h.Current);
            Assert.IsNull(h.CordLance);
        }

        [UnityTest]
        public IEnumerator OnTheBridgesTheCountCutsTheMarkedSpanFromUnderHer()
        {
            var h = Build("halvard_2");
            yield return StartFight(5f);
            int under = h.SectionOf(5f);
            Assert.IsFalse(h.IsCut(under));
            Assert.AreEqual(6, h.SpansStanding);
            h.ForceAttack(Halvard.Attack.Survey);
            yield return Until(() => h.Surveys == 1, 3f, "the survey");
            Assert.IsTrue(h.Marks.Any(m => h.SectionOf(m) == under), "the span under her is marked");
            Assert.IsTrue(h.Marks.All(m => Mathf.Abs(m - h.SectionCentre(h.SectionOf(m))) < 0.01f), "marks sit on spans");
            yield return Until(() => h.Current == Halvard.Move.Stand, 3f, "him to recover");
            _ctrl!.Teleport(new Vector2(h.SectionCentre(under), 0f));
            yield return Fixed(2);
            float yBefore = _wren!.transform.position.y;
            h.ForceAttack(Halvard.Attack.Count);
            yield return Until(() => h.Counts == 1, 3f, "the count");
            yield return Fixed(2);   // the tally ticks as the call ends; the cut lands on the count's first frame
            Assert.IsTrue(h.IsCut(under), "the marked span is cut");
            Assert.AreEqual(5, h.SpansStanding);
            Assert.IsFalse(h.IsCut(h.SectionOf(h.transform.position.x)), "never his own");
            yield return Until(() => _wren.transform.position.y < yBefore - 1f, 3f, "her to fall through the gap");
            Assert.AreEqual(0, h.Marks.Count, "the marks went with the count");
        }

        [UnityTest]
        public IEnumerator PhaseThreeOnTheBridgesIsTheLastSpanAndTheEndRestoresTheFloor()
        {
            var h = Build("halvard_2");
            yield return StartFight(5f);
            var floorCols = _room!.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Floor").ToList();
            Assert.IsTrue(floorCols.All(c => !c.enabled), "the room's floor stands aside while the spans stand in");
            var kept = _room.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Floor_Kept" && c.enabled).ToList();
            Assert.AreEqual(2, kept.Count, "its ends past the arena are kept, one each side");
            Assert.IsTrue(kept.Any(c => c.bounds.max.x <= h.arenaMinX + 0.01f) && kept.Any(c => c.bounds.min.x >= h.arenaMaxX - 0.01f), "cut at the arena's edges");
            // Her damage carries him to phase 3 by health.
            yield return WoundUntil(h, () => h.Phase >= 3, "phase 3");
            yield return Fixed(2);
            Assert.LessOrEqual(h.SpansStanding, h.lastSpanSections, "phase 3 is fought on the last span");
            Assert.Greater(h.SpansStanding, 0);
            int hers = h.SectionOf(_wren!.transform.position.x);
            Assert.IsFalse(h.IsCut(hers), "the span she stands on is kept");
            Assert.IsFalse(h.IsCut(h.SectionOf(h.transform.position.x)), "and he stands on it too");
            Assert.AreEqual(0, h.Marks.Count);
            // Zero: he withdraws and leaves the span standing (the room's floor comes back).
            yield return WoundUntil(h, () => h.Health <= 0, "zero");
            yield return Until(() => _kit!.Arena.State == BossArena.ArenaState.Won, 6f, "the fight won");
            yield return Fixed(2);
            Assert.IsTrue(floorCols.All(c => c.enabled), "he leaves the span standing");
            Assert.IsFalse(_room.GetComponentsInChildren<BoxCollider2D>().Any(c => c.name == "Floor_Kept"), "and the kept ends go");
            Assert.IsFalse(h.HasSpans);
        }

        [UnityTest]
        public IEnumerator AtTheThresholdTheWhiteEatsTheArenaAndHeStopsCounting()
        {
            var h = Build("halvard_3");
            _abilities!.Unlock(Ability.Clarity);
            yield return StartFight(5f);
            Assert.AreEqual(Halvard.Hunt.Threshold, h.CurrentHunt);
            Assert.AreEqual(h.arenaMinX, h.WhiteX, 0.01f, "nothing eaten yet");
            Assert.IsNull(h.White);
            Assert.IsTrue(h.IsCounting);
            h.ForceAttack(Halvard.Attack.Survey);
            yield return Until(() => h.Surveys == 1, 3f, "a survey: he still counts");
            Assert.Greater(h.Marks.Count, 0);
            yield return WoundUntil(h, () => h.Phase >= 2, "phase 2");
            yield return Fixed(2);
            float width = h.arenaMaxX - h.arenaMinX;
            Assert.AreEqual(h.arenaMinX + width * h.whiteAtPhaseTwo, h.WhiteX, 0.01f, "a quarter gone at phase 2");
            Assert.IsNotNull(h.White, "a white patch nothing holds her in");
            Assert.IsTrue(h.White!.Contains(new Vector2(h.arenaMinX + 0.5f, 1f)));
            Assert.IsFalse(h.White.Contains(new Vector2(h.WhiteX + 0.5f, 1f)));
            Assert.GreaterOrEqual(h.transform.position.x, h.WhiteX, "he keeps east of it");
            yield return WoundUntil(h, () => h.Phase >= 3, "phase 3");
            yield return Fixed(2);
            Assert.AreEqual(h.arenaMinX + width * h.whiteAtPhaseThree, h.WhiteX, 0.01f, "half gone at phase 3");
            Assert.IsFalse(h.IsCounting, "halfway through he stops counting");
            Assert.AreEqual(0, h.Marks.Count, "the marks are gone with the count");
            Assert.IsFalse(Halvard.PatternFor(Halvard.Hunt.Threshold, 3).Contains(Halvard.Attack.Count));
            yield return WoundUntil(h, () => h.Health <= 0, "zero");
            yield return Until(() => _kit!.Arena.State == BossArena.ArenaState.Won, 6f, "the fight won");
            yield return Fixed(2);
            Assert.IsNull(h.White, "the white is the fight's");
            Assert.AreEqual(h.arenaMinX, h.WhiteX, 0.01f);
        }
    }
}
