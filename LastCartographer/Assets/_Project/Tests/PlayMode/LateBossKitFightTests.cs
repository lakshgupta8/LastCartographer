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
    /// The late bosses (CMB-14, boss sheets 6.8 to 6.10), each built from its kit and sheet and fought to its answers:
    /// Oriel's mirror of Wren's own Charter, her one Bind and her stand-down; Hale's duel over the nine stones; the
    /// Fallen Star's iron, its fist, its walls and the heat that lifts her over them.
    /// </summary>
    public class LateBossKitFightTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        InstrumentBelt _belt;
        AbilitySet _abilities;
        BossKit _kit;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static HitInfo Strike(Vector2 dir) => new HitInfo { Damage = 1, Direction = dir };

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
            _belt = _wren.AddComponent<InstrumentBelt>();
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

        BossKit Build(string id)
        {
            _kit = BossKits.Build(id, _room.transform, Vector2.zero);
            Assert.IsNotNull(_kit, id + " has a kit");
            _kit.Arena.IntroSeconds = 0.05f;
            _kit.Arena.RetryIntroSeconds = 0.02f;
            return _kit;
        }

        IEnumerator StartFight(float wrenX)
        {
            _ctrl.Teleport(new Vector2(wrenX, 0f));
            yield return Until(() => _kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the doors to shut");
            Assert.AreEqual(1, _kit.Boss.Phase);
        }

        IEnumerator Retry(float wrenX)
        {
            _kit.Arena.ResetFight();
            _ctrl.Teleport(new Vector2(-4f, 0f));
            yield return Fixed(5);
            yield return StartFight(wrenX);
        }

        IEnumerator Recovered()
        {
            yield return Until(() => !_vitals.IsInvulnerable, 3f, "her i-frames to end");
            _vitals.RestoreAll();
        }

        static float Scraps() { GameState.World.Numbers.TryGetValue("$vellum_scraps", out var s); return s; }

        void Won(string id, float scrapsBefore)
        {
            var w = GameState.World;
            Assert.IsTrue(_kit.Boss.IsDead, id + " is down");
            Assert.AreEqual(BossArena.ArenaState.Won, _kit.Arena.State);
            Assert.IsTrue(w.Is(Bosses.FlagKey(id)), "the flag the sheet names");
            Assert.AreEqual(scrapsBefore + Bosses.Find(id).Scraps, w.Numbers["$vellum_scraps"], "the sheet's scraps");
            Assert.IsFalse(_kit.DoorW.activeSelf, "the doors open");
        }

        [Test]
        public void TheLateKitsKeepToFourAttacksAPhase()
        {
            for (int p = 1; p <= 3; p++)
            {
                Assert.LessOrEqual(Oriel.PatternFor(p).Distinct().Count(), 4);
                Assert.LessOrEqual(Hale.PatternFor(p).Distinct().Count(), 4);
                Assert.LessOrEqual(FallenStar.PatternFor(p).Distinct().Count(), 4);
            }
            Assert.IsTrue(Oriel.PatternFor(2).Contains(Oriel.Attack.Flourish) && !Oriel.PatternFor(1).Contains(Oriel.Attack.Flourish), "her Flourish from phase 2");
            Assert.IsTrue(FallenStar.PatternFor(2).Contains(FallenStar.Attack.Walls) && !FallenStar.PatternFor(1).Contains(FallenStar.Attack.Walls), "walls from phase 2");
        }

        // ---- Oriel -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator OrielFightsWithHerOwnCharterReversed()
        {
            var w = GameState.World;
            w.Equipment.OwnedCharters.Add(CharterKind.Warden);
            Assert.IsTrue(w.Equipment.SetCharter(CharterKind.Warden));
            var o = (Oriel)Build("oriel").Boss;
            o.standSeconds = 999f;
            yield return StartFight(o.transform.position.x - 1.5f);
            Assert.AreEqual(CharterKind.Warden, o.Mirror, "Wren's Charter, read as the fight begins");
            CollectionAssert.AreEqual(new[] { "Overhead", "Shove", "Sweep" }, o.MirrorCombo.Select(s => s.Name).ToArray(), "her combo, reversed");
            Assert.AreEqual(FlourishKind.Blot, o.MirrorFlourish);

            int masks = _vitals.Masks;
            int deepest = -1;
            o.ForceAttack(Oriel.Attack.Combo);
            yield return Fixed(2);
            Assert.IsTrue(o.IsTelegraphing);
            Assert.GreaterOrEqual(o.TelegraphLeft, o.MinTelegraphFrames - 2, "every step is read before it lands");
            float t = 0f;
            while (o.CurrentAttack == Oriel.Attack.Combo && t < 4f) { deepest = Mathf.Max(deepest, o.StepIndex); t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.AreEqual(2, deepest, "all three steps");
            Assert.AreEqual(3, o.Strikes);
            Assert.Less(_vitals.Masks, masks, "the mirror lands");
        }

        [UnityTest]
        public IEnumerator TheMirrorCanBeParried()
        {
            var o = (Oriel)Build("oriel").Boss;
            o.standSeconds = 999f;
            _belt.Equip(0, InstrumentKind.SightingLens);
            yield return StartFight(o.transform.position.x - 1.5f);
            int masks = _vitals.Masks;
            o.ForceAttack(Oriel.Attack.Combo);
            yield return Fixed(2);
            yield return Until(() => o.TelegraphLeft <= 2, 2f, "the first step to come");
            Assert.IsTrue(_belt.TryUse(0), "the lens goes up");
            yield return Until(() => o.Parried == 1, 1f, "the parry");
            Assert.IsTrue(o.IsStaggered, "a parried mirror staggers her");
            Assert.AreEqual(masks, _vitals.Masks);
            Assert.AreNotEqual(Oriel.Move.Strike, o.Current, "and the combo ends");
        }

        [UnityTest]
        public IEnumerator SheFlourishesThenBindsOnceAtAThird()
        {
            var o = (Oriel)Build("oriel").Boss;
            o.standSeconds = 999f;
            yield return StartFight(o.transform.position.x - 1.5f);
            Assert.AreEqual(FlourishKind.Crosshatch, o.MirrorFlourish, "the Surveyor's Flourish");
            for (int i = 0; i < 11; i++) o.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(2, o.Phase);
            yield return Recovered();
            int masks = _vitals.Masks;
            o.ForceAttack(Oriel.Attack.Flourish);
            yield return Until(() => o.Flourishes == 1 && o.Current != Oriel.Move.Flourish, 2f, "her Flourish");
            Assert.AreEqual(masks - 1, _vitals.Masks, "Wren's own Flourish, turned on her");

            for (int i = 0; i < 10; i++) o.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, o.Phase);
            CollectionAssert.AreEqual(new[] { Oriel.Attack.Step, Oriel.Attack.Bind }, o.Queued.ToArray(), "at a third: step clear, then Bind");
            int hp = o.Health;
            o.ForceAttack(Oriel.Attack.Bind);
            Assert.IsTrue(o.IsBinding);
            yield return Until(() => !o.IsBinding, 3f, "the Bind");
            Assert.AreEqual(1, o.BindsCompleted);
            Assert.AreEqual(hp + Mathf.RoundToInt(o.MaxHealth / 3f), o.Health, "a third back");
            Assert.IsTrue(o.BindSpent);
            o.ForceAttack(Oriel.Attack.Bind);
            Assert.IsFalse(o.IsBinding, "once");

            // Denied: struck while she binds.
            yield return Retry(o.transform.position.x - 1.5f);
            Assert.IsFalse(o.BindSpent, "a retry gives her the Bind back");
            for (int i = 0; i < 21; i++) o.TakeHit(Strike(Vector2.right));
            yield return Fixed(10);
            o.ForceAttack(Oriel.Attack.Bind);
            yield return Fixed(10);
            hp = o.Health;
            Assert.IsTrue(o.TakeHit(Strike(Vector2.right)));
            Assert.AreEqual(1, o.BindsDenied, "denied");
            Assert.IsFalse(o.IsBinding);
            yield return Fixed(o.bindFrames + 10);
            Assert.AreEqual(hp - 1, o.Health, "nothing back");
        }

        [UnityTest]
        public IEnumerator BeatenCleanSheStandsTheWardensDown()
        {
            var w = GameState.World;
            Licence.Revoke(w);
            w.Set(Licence.ReportSentFlag, true);
            Assert.IsTrue(Licence.WardensHostile(w));
            var o = (Oriel)Build("oriel").Boss;
            o.standSeconds = 999f;
            yield return StartFight(2f);
            float scraps = Scraps();
            while (!o.IsDead) o.TakeHit(Strike(Vector2.right));
            Won("oriel", scraps);
            Assert.AreEqual(0, o.MasksTaken);
            Assert.IsTrue(o.StoodDown);
            Assert.IsTrue(w.Is(Licence.StoodDownFlag), "not a mask lost: she stands the Wardens down");
            Assert.IsFalse(Licence.WardensHostile(w), "whatever the report said");
        }

        [UnityTest]
        public IEnumerator AMaskLostAndTheyHuntOn()
        {
            var w = GameState.World;
            var o = (Oriel)Build("oriel").Boss;
            o.standSeconds = 999f;
            yield return StartFight(2f);
            Assert.IsTrue(_vitals.Damage(1, Vector2.zero));
            Assert.AreEqual(1, o.MasksTaken);
            while (!o.IsDead) o.TakeHit(Strike(Vector2.right));
            Assert.IsFalse(o.StoodDown);
            Assert.IsFalse(w.Is(Licence.StoodDownFlag), "a mask lost: they hunt until the Threshold");
        }

        // ---- Hale --------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HeSightsTheStonesAndSheDrawsThemFirst()
        {
            var h = (Hale)Build("hale").Boss;
            h.standSeconds = 999f;
            yield return StartFight(2f);
            Assert.AreEqual(Hale.StoneCount, h.stoneXs.Count);
            Assert.AreEqual(0, h.Claimed);

            h.ForceAttack(Hale.Attack.Sight);
            int first = h.SightTarget;
            Assert.GreaterOrEqual(first, 0, "his lens goes up at a stone");
            yield return Until(() => h.Sights == 1, 2f, "his sighting");
            Assert.AreEqual(Hale.Owner.Hale, h.OwnerOf(first), "sighted: his");

            // She stands at the next stone and draws it while he sights it.
            int next = Enumerable.Range(0, Hale.StoneCount).Where(i => h.OwnerOf(i) == Hale.Owner.None)
                .OrderBy(i => Mathf.Abs(h.stoneXs[i] - h.transform.position.x)).First();
            yield return Until(() => h.Current == Hale.Move.Stand || h.Current == Hale.Move.Recover, 2f, "him to finish");
            _ctrl.Teleport(new Vector2(h.stoneXs[next], 0f));
            yield return Until(() => h.ClaimProgress > 0.5f, 2f, "her survey to be under way");
            h.ForceAttack(Hale.Attack.Sight);
            Assert.AreEqual(next, h.SightTarget, "he goes for the same stone");
            yield return Until(() => h.OwnerOf(next) == Hale.Owner.Wren, 2f, "her survey");
            yield return Until(() => h.Current != Hale.Move.Telegraph, 2f, "his sighting to end");
            Assert.AreEqual(1, h.SightsDenied, "she drew it first");
            Assert.AreEqual(1, h.WrenStones);

            // He calls the count: his stone strikes, hers does not.
            yield return Recovered();
            int masks = _vitals.Masks;
            h.ForceAttack(Hale.Attack.Call);
            yield return Until(() => h.Calls == 1 && h.Current == Hale.Move.Recover, 2f, "the count");
            Assert.AreEqual(masks, _vitals.Masks, "her own stone is safe");

            _ctrl.Teleport(new Vector2(h.stoneXs[first], 0f));
            yield return Fixed(3);
            h.ForceAttack(Hale.Attack.Call);
            yield return Until(() => h.Pillars.Count == 1, 2f, "his stone to strike");
            var pillar = h.Pillars[0];
            Assert.IsFalse(pillar.TakeHit(Strike(Vector2.right)));
            Assert.IsTrue(pillar.TakeHit(Strike(Vector2.down)), "the strike can be pogoed");
            yield return Until(() => h.Current == Hale.Move.Recover, 2f, "the strike to end");
            Assert.AreEqual(masks - 1, _vitals.Masks, "standing on his stone, it strikes her");
        }

        [UnityTest]
        public IEnumerator ThePhasesFollowTheStonesNotTheWounds()
        {
            var h = (Hale)Build("hale").Boss;
            h.standSeconds = 999f;
            yield return StartFight(2f);   // between the first two stones: she draws none
            for (int i = 0; i < 25; i++) h.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(1, h.Phase, "a wounded surveyor is still at the first stone");

            for (int k = 1; k <= Hale.PhaseThreeAt; k++)
            {
                h.ForceAttack(Hale.Attack.Sight);
                yield return Until(() => h.Sights == k, 2f, "sighting " + k);
                Assert.AreEqual(k >= Hale.PhaseThreeAt ? 3 : k >= Hale.PhaseTwoAt ? 2 : 1, h.Phase, k + " stones");
                yield return Until(() => h.Current == Hale.Move.Recover || h.Current == Hale.Move.Stand, 1f, "the lens down");
            }
            Assert.AreEqual("Nine. No. Tear it. Tear it, I can't.", h.PhaseLine(3));

            var lens = InstrumentInfo.Of(InstrumentKind.SightingLens);
            Assert.AreEqual(lens.Cooldown, InstrumentBelt.CooldownOf(lens, GameState.World), 0.001f);
            float scraps = Scraps();
            while (!h.IsDead) h.TakeHit(Strike(Vector2.right));
            Won("hale", scraps);
            Assert.AreEqual(lens.Cooldown * 0.5f, InstrumentBelt.CooldownOf(lens, GameState.World), 0.001f, "Hale's lens: half the wait");
        }

        // ---- the Fallen Star ---------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator IronEverywhereButTheSeamAndTheFistToPogo()
        {
            var st = (FallenStar)Build("fallen_star").Boss;
            st.standSeconds = 999f;
            yield return StartFight(st.transform.position.x + 3f);
            int hp = st.Health;
            Assert.IsFalse(st.TakeHit(Strike(Vector2.right)), "the quill drifts off the iron");
            Assert.IsFalse(st.TakeHit(Strike(Vector2.up)));
            Assert.AreEqual(2, st.StrikesDrifted);
            Assert.IsTrue(st.TakeHit(Strike(Vector2.down)), "the seam on top");
            Assert.AreEqual(hp - 1, st.Health);

            int masks = _vitals.Masks;
            st.ForceAttack(FallenStar.Attack.Slam);
            yield return Until(() => st.Slams == 1 && st.Current == FallenStar.Move.Recover, 2f, "the slam");
            Assert.AreEqual(masks - 1, _vitals.Masks, "it came down on her");
            Assert.IsNotNull(st.Fist, "the fist stays down");
            Assert.IsFalse(st.Fist.TakeHit(Strike(Vector2.right)));
            Assert.IsTrue(st.Fist.TakeHit(Strike(Vector2.down)), "iron to pogo from");
            yield return Until(() => st.Fist == null, 2f, "the fist to lift");
        }

        [UnityTest]
        public IEnumerator ItRaisesWallsThenBurnsAndOnlyTheHeatGoesOver()
        {
            var st = (FallenStar)Build("fallen_star").Boss;
            st.standSeconds = 999f;
            float mid = st.transform.position.x;
            yield return StartFight(mid + 4f);
            for (int i = 0; i < 12; i++) st.TakeHit(Strike(Vector2.down));
            Assert.AreEqual(2, st.Phase);
            st.ForceAttack(FallenStar.Attack.Walls);
            yield return Until(() => st.WallRaisings == 1, 2f, "the walls");
            Assert.AreEqual(2, st.Walls.Count, "iron either side of her");
            Assert.Less(st.Walls[0].transform.position.x, _ctrl.Position.x);
            Assert.Greater(st.Walls[1].transform.position.x, _ctrl.Position.x);
            Assert.AreEqual(st.wallHeight, st.Walls[0].GetComponent<BoxCollider2D>().size.y, 0.01f);
            Assert.IsTrue(st.Updrafts.All(u => u == null), "no heat yet");

            for (int i = 0; i < 11; i++) st.TakeHit(Strike(Vector2.down));
            Assert.AreEqual(3, st.Phase);
            Assert.IsTrue(st.IsBurning);
            Assert.AreEqual(st.burningWallHeight, st.Walls[1].GetComponent<BoxCollider2D>().size.y, 0.01f, "burning, the walls grow past any jump");
            Assert.AreEqual(2, st.Updrafts.Count(u => u != null), "and the heat rises beside them");

            var draft = st.Updrafts[1];
            _ctrl.Teleport(new Vector2(draft.transform.position.x, 0f));
            yield return Fixed(30);
            Assert.Less(_ctrl.Position.y, 1f, "without Windmemory the heat is only heat");
            _abilities.Unlock(Ability.Windmemory);
            float top = 0f;
            float t = 0f;
            while (t < 2f) { top = Mathf.Max(top, _ctrl.Position.y); t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.Greater(top, st.burningWallHeight, "with it, over the wall");
            Assert.Greater(draft.Lifts, 0);

            float scraps = Scraps();
            while (!st.IsDead) st.TakeHit(Strike(Vector2.down));
            Won("fallen_star", scraps);
            Assert.AreEqual(0, st.Walls.Count, "the iron goes back into the ground");
        }
    }
}
