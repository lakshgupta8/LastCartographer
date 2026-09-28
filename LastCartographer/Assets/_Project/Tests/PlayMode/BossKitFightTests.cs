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
    /// The mid-game bosses (CMB-13, boss sheets 6.4 to 6.7), each built from its kit and sheet and fought to its
    /// answers: the Collapse only where and when the lamp is lit, Brann's hot floor and both lances, the Choir's bells
    /// and the Cantor rule, the Gatekeeper's feathers, its rise, its torn roots and its belly.
    /// </summary>
    public class BossKitFightTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        Inkwell _ink;
        Flourishes _fl;
        BossKit _kit;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static HitInfo Strike(Vector2 dir, GameObject source = null) => new HitInfo { Damage = 1, Direction = dir, Source = source };

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
            _wren.AddComponent<AbilitySet>();
            _ink = _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _fl = _wren.AddComponent<Flourishes>();
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
            Assert.IsTrue(_kit.DoorW.activeSelf && _kit.DoorE.activeSelf, "the doors are shut");
            Assert.AreEqual(1, _kit.Boss.Phase);
        }

        IEnumerator Recovered()
        {
            yield return Until(() => !_vitals.IsInvulnerable, 3f, "her i-frames to end");
            _vitals.RestoreAll();
        }

        void Won(string id, float scrapsBefore)
        {
            var w = GameState.World;
            Assert.IsTrue(_kit.Boss.IsDead, id + " is down");
            Assert.AreEqual(BossArena.ArenaState.Won, _kit.Arena.State);
            Assert.IsTrue(w.Is(Bosses.FlagKey(id)), "the flag the sheet names");
            Assert.AreEqual(scrapsBefore + Bosses.Find(id).Scraps, w.Numbers["$vellum_scraps"], "the sheet's scraps");
            Assert.IsFalse(_kit.DoorW.activeSelf, "the doors open");
        }

        static float Scraps() { GameState.World.Numbers.TryGetValue("$vellum_scraps", out var s); return s; }

        // ---- every kit ---------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryKitStandsOnItsSheet()
        {
            for (int k = 0; k < BossKits.Ids.Length; k++)
            {
                var id = BossKits.Ids[k];
                var kit = BossKits.Build(id, _room.transform, new Vector2(0f, 40f * k));
                var sheet = Bosses.Find(id);
                Assert.AreEqual(sheet.Name, kit.Boss.BossName, id);
                Assert.AreEqual(sheet.Tier, kit.Boss.Tier, id + "'s tier");
                for (int p = 1; p <= 3; p++) Assert.AreEqual(sheet.Lines[p - 1], kit.Boss.PhaseLine(p), id + "'s line " + p);
                Assert.AreEqual(id, kit.Arena.BossId);
                Assert.AreEqual(sheet.Scraps, kit.Arena.VellumScraps, id + "'s scraps");
                Assert.AreEqual(3, kit.Boss.PhaseCount);
                Assert.IsFalse(kit.DoorW.activeSelf || kit.DoorE.activeSelf, "doors open until she steps in");
                Assert.AreEqual(Mathf.Max(8, 12 - (sheet.Tier - 1) * 4 / 3), kit.Boss.MinTelegraphFrames, id + ": the tier's telegraph floor, between tier I's 12 frames and tier IV's 8");
            }
            for (int p = 1; p <= 3; p++)
            {
                Assert.LessOrEqual(Collapse.PatternFor(p).Distinct().Count(), 4, "four attacks a phase at most");
                Assert.LessOrEqual(Brann.PatternFor(p).Distinct().Count(), 4);
                Assert.LessOrEqual(Gatekeeper.PatternFor(p).Distinct().Count(), 4);
            }
            yield return null;
        }

        // ---- the Collapse ------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheCollapseIsDrawnOnlyWhereAndWhenTheLampIsLit()
        {
            var c = (Collapse)Build("collapse").Boss;
            c.waitSeconds = 999f;
            yield return StartFight(2f);
            var lit = new System.Collections.Generic.List<int>();
            int beat = -1;
            float t = 0f;
            while (lit.Count < 5 && t < 6f)
            {
                if (c.Beat != beat) { beat = c.Beat; lit.Add(c.LitLamp); }
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, lit, "the chorus lights the lamps in turn");

            yield return Until(() => c.IsDrawn, 2f, "a lit beat");
            Assert.AreEqual(c.SectionCentre(c.LitLamp), c.transform.position.x, 0.01f, "it is drawn in the lit section");
            int hp = c.Health;
            Assert.IsTrue(c.TakeHit(Strike(Vector2.right)), "struck on the beat");
            Assert.AreEqual(hp - 1, c.Health);
            yield return Until(() => !c.IsDrawn, 2f, "the dark part of the beat");
            Assert.IsFalse(c.TakeHit(Strike(Vector2.right)), "between beats there is nothing to strike");
        }

        [UnityTest]
        public IEnumerator RubbleFallsWhereTheDustRisesAndThePogoClearsTheLamp()
        {
            var c = (Collapse)Build("collapse").Boss;
            c.waitSeconds = 999f;
            float x1 = c.SectionCentre(1);
            yield return StartFight(x1);
            int masks = _vitals.Masks;
            c.ForceAttack(Collapse.Attack.Rubble);
            yield return Fixed(2);
            Assert.IsTrue(c.IsTelegraphing, "the dust rises first");
            yield return Until(() => c.Rubbles == 1 && c.Current == Collapse.Move.Wait, 3f, "the rubble to land");
            Assert.AreEqual(masks - 1, _vitals.Masks, "it came down on her");
            Assert.AreEqual(1, c.Rubble.Count);
            Assert.AreEqual(x1, c.Rubble[0].Position.x, 0.05f, "where she stood");
            Assert.IsTrue(c.IsLampBlocked(1), "and it lies under the second lamp");
            yield return Until(() => c.Beat % c.lampCount == 1, 4f, "the second lamp's beat");
            Assert.AreEqual(-1, c.LitLamp, "that beat is lost under the rubble");

            var block = c.Rubble[0];
            Assert.IsFalse(block.TakeHit(Strike(Vector2.right, _wren)), "a side strike does nothing to it");
            Assert.IsTrue(block.TakeHit(Strike(Vector2.down, _wren)), "the pogo breaks it");
            Assert.AreEqual(1, c.RubbleBroken);
            Assert.IsFalse(c.IsLampBlocked(1));
            int beatBroken = c.Beat;
            yield return Until(() => c.Beat > beatBroken && c.Beat % c.lampCount == 1, 4f, "the second lamp's next beat");
            Assert.AreEqual(1, c.LitLamp, "the lamp lights");
        }

        [UnityTest]
        public IEnumerator TheSurgeHurtsWhoStandsAndTheLongstrokeCutsIt()
        {
            var c = (Collapse)Build("collapse").Boss;
            c.waitSeconds = 999f;
            yield return StartFight(3f);
            int masks = _vitals.Masks;
            c.ForceAttack(Collapse.Attack.Surge);
            yield return Until(() => c.Surge != null, 2f, "the surge");
            Assert.Greater(c.Surge.Position.x, 10f, "it comes from the far end");
            yield return Until(() => c.Surge == null, 4f, "the surge to pass");
            Assert.AreEqual(masks - 1, _vitals.Masks, "it ran through her");

            yield return Recovered();
            _ink.Add(9);
            c.ForceAttack(Collapse.Attack.Surge);
            yield return Until(() => c.Surge != null && c.Surge.Position.x - _ctrl.Position.x < 5f, 4f, "the surge to come close");
            Assert.IsFalse(c.Surge.TakeHit(Strike(Vector2.right, _wren)), "the quill does nothing to ink on the floor");
            Assert.AreEqual(1, _ctrl.Facing, "she faces it");
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Longstroke));
            yield return Until(() => c.SurgesCut == 1, 1f, "the Longstroke to cut it");
            Assert.IsNull(c.Surge);
            Assert.AreEqual(masks, _vitals.Masks, "cut before it reached her");
        }

        [UnityTest]
        public IEnumerator PhaseThreeReachesForTheLampsButNeverTheLast()
        {
            var c = (Collapse)Build("collapse").Boss;
            c.waitSeconds = 999f;
            yield return StartFight(1f);
            yield return Until(() => c.IsDrawn, 2f, "a lit beat");
            for (int i = 0; i < 19; i++) c.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, c.Phase, "a third left");

            c.ForceAttack(Collapse.Attack.Reach);
            int target = c.ReachingFor;
            Assert.GreaterOrEqual(target, 0, "it reaches for a lamp");
            yield return Until(() => c.Current == Collapse.Move.Wait, 3f, "the reach to finish");
            Assert.IsTrue(c.IsLampOut(target), "unstruck, it puts the lamp out");
            yield return Until(() => c.Beat % c.lampCount == target, 4f, "the out lamp's beat");
            Assert.AreEqual(-1, c.LitLamp, "that beat is lost");

            c.ForceAttack(Collapse.Attack.Reach);
            yield return Until(() => c.IsDrawn, 2f, "a lit beat while it reaches");
            Assert.IsTrue(c.TakeHit(Strike(Vector2.right)));
            Assert.AreEqual(1, c.ReachesStopped, "struck while reaching");
            Assert.AreEqual(1, c.LampsOut, "the lamp stays lit");

            for (int k = 0; k < 5 && c.LampsOut < c.lampCount - 1; k++)
            {
                c.ForceAttack(Collapse.Attack.Reach);
                yield return Until(() => c.Current == Collapse.Move.Wait, 3f, "a reach");
            }
            Assert.AreEqual(c.lampCount - 1, c.LampsOut);
            c.ForceAttack(Collapse.Attack.Reach);
            yield return Until(() => c.Current == Collapse.Move.Wait, 3f, "the last reach");
            Assert.AreEqual(c.lampCount - 1, c.LampsOut, "never the last lamp");

            float scraps = Scraps();
            yield return Until(() => c.IsDrawn, 5f, "the last lamp's beat");
            // Strike on the lit part of each beat; a nested wait can resume a frame late, past the window's edge.
            for (float t = 0f; !c.IsDead && t < 20f; t += Time.deltaTime)
            {
                if (c.IsDrawn) Assert.IsTrue(c.TakeHit(Strike(Vector2.right)), "a strike on a lit beat lands");
                else yield return null;
            }
            Assert.IsTrue(c.IsDead);
            Won("collapse", scraps);
        }

        // ---- Brann -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheFurnaceBurnsTheHotSectionsAndGlowsBeforeItShifts()
        {
            var b = (Brann)Build("brann").Boss;
            b.standSeconds = 999f;
            yield return StartFight(b.SectionCentre(0));
            Assert.AreEqual(4, b.HotCount, "a third of the floor is cool");
            Assert.IsFalse(b.IsHot(0));
            int masks = _vitals.Masks;
            yield return Fixed(20);
            Assert.AreEqual(masks, _vitals.Masks, "cool iron is safe");

            _ctrl.Teleport(new Vector2(b.SectionCentre(1), 0f));
            yield return Until(() => b.Burns == 1, 1f, "the hot section to burn");
            Assert.AreEqual(masks - 1, _vitals.Masks);
            _ctrl.Teleport(new Vector2(b.SectionCentre(0), 0f));
            yield return Recovered();

            yield return Until(() => Enumerable.Range(0, b.sections).Any(b.IsWarming), 4f, "the next hot sections to glow");
            int warming = Enumerable.Range(0, b.sections).First(b.IsWarming);
            Assert.IsFalse(b.IsHot(warming), "it glows before it burns");
            yield return Until(() => b.Shifts == 1, 2f, "the shift");
            Assert.IsTrue(b.IsHot(warming), "then it burns");
            CollectionAssert.AreEqual(Brann.HotPattern(1, 1, b.sections), Enumerable.Range(0, b.sections).Select(b.IsHot).ToArray());
        }

        [UnityTest]
        public IEnumerator BothLancesCrossCutAndTheHoldBreaksOnlyToALongstroke()
        {
            var b = (Brann)Build("brann").Boss;
            b.standSeconds = 999f;
            b.shiftSeconds = 999f;
            yield return StartFight(b.SectionCentre(3));
            for (int i = 0; i < 13; i++) b.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(2, b.Phase);
            Assert.AreEqual(3, b.HotCount, "half the floor is cool");
            Assert.IsFalse(b.IsHot(3), "and she stands on the cool half");

            _ctrl.Teleport(new Vector2(b.transform.position.x + 1.5f, 0f));
            Assert.IsFalse(b.IsHot(b.SectionOf(_ctrl.Position.x)), "beside him, on cool iron");
            int masks = _vitals.Masks;
            b.ForceAttack(Brann.Attack.CrossCut);
            yield return Until(() => b.CrossCuts == 1 && b.Current == Brann.Move.Recover, 2f, "the cross-cut");
            Assert.AreEqual(masks - 1, _vitals.Masks, "both lances at once: jump it");
            yield return Recovered();

            _ctrl.Teleport(new Vector2(b.SectionCentre(3), 0f));
            _ink.Add(9);
            b.ForceAttack(Brann.Attack.Hold);
            yield return Until(() => b.IsHolding, 2f, "the hold");
            int hp = b.Health;
            Assert.IsFalse(b.TakeHit(Strike(Vector2.right, _wren)), "the quill stops on the lances");
            Assert.IsTrue(b.TakeHit(Strike(Vector2.down, _wren)), "the pogo lands over them");
            Assert.IsTrue(b.IsHolding, "and the line holds");
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Longstroke));
            yield return Until(() => b.HoldsBroken == 1, 1f, "the Longstroke to break the hold");
            Assert.IsFalse(b.IsHolding);
            Assert.IsTrue(b.IsStaggered, "it staggers him");
            Assert.Less(b.Health, hp - 1);
        }

        [UnityTest]
        public IEnumerator InTheDarkOnlyHisBrassGlows()
        {
            var b = (Brann)Build("brann").Boss;
            b.standSeconds = 999f;
            yield return StartFight(b.SectionCentre(0));
            for (int i = 0; i < 25; i++) b.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, b.Phase);
            Assert.IsTrue(b.IsDark);
            Assert.AreEqual(0, b.HotCount, "the furnace is out");
            int burns = b.Burns;
            _ctrl.Teleport(new Vector2(b.SectionCentre(1), 0f));
            yield return Fixed(30);
            Assert.AreEqual(burns, b.Burns, "nothing burns in the dark");

            Assert.AreEqual(0.25f, b.Glow, 0.001f);
            b.ForceAttack(Brann.Attack.Thrust);
            yield return Fixed(2);
            Assert.IsTrue(b.IsTelegraphing);
            Assert.AreEqual(1f, b.Glow, 0.001f, "the telegraph is the glow");

            float scraps = Scraps();
            while (!b.IsDead) b.TakeHit(Strike(Vector2.down));
            Won("brann", scraps);
        }

        // ---- the Choir ---------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ThreeDovesRingInTurnAndEachBellErasesTheSquare()
        {
            var kit = Build("choir");
            var ch = (Choir)kit.Boss;
            var w = GameState.World;
            Atlas.Survey(w, "Verdance_Aldermere_2/Square");
            yield return StartFight(1f);
            Assert.AreEqual(3, ch.ActiveDoves);
            Assert.AreEqual(3, ch.Doves.Count(d => d.gameObject.activeSelf));
            Assert.IsFalse(ch.TakeHit(Strike(Vector2.right)), "the song itself cannot be struck, only the doves");

            float t = 0f;
            while (ch.Tolls < 1 && t < 6f)
            {
                Assert.LessOrEqual(ch.RingingCount, 1, "in turn: one bell at a time");
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(1, ch.Tolls);
            Assert.AreEqual(1, ch.PlatformsErased, "a platform goes white");
            Assert.IsTrue(Atlas.IsErased(w, ch.placeId), "and Aldermere leaves the page");
            Assert.AreEqual(1, ch.Erasures);

            Assert.IsTrue(Atlas.Survey(w, "Verdance_Aldermere_2/Square"), "re-survey between verses");
            Assert.IsFalse(Atlas.IsErased(w, ch.placeId));
            while (ch.Tolls < 2 && t < 12f)
            {
                Assert.LessOrEqual(ch.RingingCount, 1);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(2, ch.PlatformsErased);
            Assert.AreEqual(2, ch.Erasures, "the next bell erases it again");
            Assert.IsNotNull(kit.Vantage, "the square's vantage stands by the door");
        }

        [UnityTest]
        public IEnumerator AStrikeOnARingingDoveStopsItsBell()
        {
            var ch = (Choir)Build("choir").Boss;
            ch.gapSeconds = 999f;
            yield return StartFight(1f);
            ch.ForceRing(1);
            yield return Fixed(3);
            Assert.IsTrue(ch.IsRinging(1));
            int hp = ch.Health;
            Assert.IsTrue(ch.Doves[1].TakeHit(Strike(Vector2.right, _wren)));
            Assert.AreEqual(hp - 1, ch.Health, "the doves share one health");
            Assert.IsFalse(ch.IsRinging(1), "the bell stops");
            Assert.AreEqual(1, ch.Cancelled);
            yield return Fixed(ch.RingFrames + 5);
            Assert.AreEqual(0, ch.Tolls, "and never tolls");
            Assert.IsTrue(ch.Doves[2].TakeHit(Strike(Vector2.up, _wren)), "a quiet dove can be struck too");
            Assert.AreEqual(1, ch.Cancelled);
        }

        [UnityTest]
        public IEnumerator TwoInCanonThenOneAlone()
        {
            var ch = (Choir)Build("choir").Boss;
            yield return StartFight(1f);
            for (int i = 0; i < 9; i++) ch.Doves[0].TakeHit(Strike(Vector2.right, _wren));
            Assert.AreEqual(2, ch.Phase);
            Assert.AreEqual(2, ch.ActiveDoves);
            Assert.IsFalse(ch.IsDoveActive(2), "the east dove has left");
            yield return Until(() => ch.RingingCount == 2, 6f, "two bells in canon");
            Assert.IsTrue(ch.IsRinging(0) && ch.IsRinging(1));

            for (int i = 0; i < 8; i++) ch.Doves[0].TakeHit(Strike(Vector2.right, _wren));
            Assert.AreEqual(3, ch.Phase);
            Assert.AreEqual(1, ch.ActiveDoves);
            Assert.IsTrue(ch.IsDoveActive(0) && !ch.IsDoveActive(1), "one dove, singing alone");
            int tolls = ch.Tolls;
            float t = 0f;
            while (ch.Tolls < tolls + 2 && t < 6f)
            {
                Assert.LessOrEqual(ch.RingingCount, 1, "alone");
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(tolls + 2, ch.Tolls);

            float scraps = Scraps();
            while (!ch.IsDead) Assert.IsTrue(ch.Doves[0].TakeHit(Strike(Vector2.right, _wren)));
            Won("choir", scraps);
            Assert.IsFalse(ch.Doves.Any(d => d.gameObject.activeSelf), "the square is quiet");
        }

        // ---- the Gatekeeper ----------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator OnTheGroundItSweepsTheFloorAndDropsFeathers()
        {
            var g = (Gatekeeper)Build("gatekeeper").Boss;
            g.standSeconds = 999f;
            yield return Fixed(2);
            Assert.AreEqual(3, g.Roots.Count, "the roots hold");
            Assert.AreEqual(3, _room.GetComponentsInChildren<TetherAnchor>().Length, "as Inkthread anchors");
            yield return StartFight(g.transform.position.x + 2.2f);
            int masks = _vitals.Masks;
            g.ForceAttack(Gatekeeper.Attack.Sweep);
            yield return Until(() => g.Sweeps == 1 && g.Current == Gatekeeper.Move.Recover, 2f, "the sweep");
            Assert.AreEqual(masks - 1, _vitals.Masks, "the wing sweeps the floor");
            yield return Recovered();

            masks = _vitals.Masks;
            _ctrl.Teleport(new Vector2(g.transform.position.x + 4.5f, 0f));
            g.ForceAttack(Gatekeeper.Attack.Feathers);
            yield return Until(() => g.FeatherVolleys == 1, 2f, "the feathers");
            Assert.AreEqual(3, g.Feathers.Count, "three feathers, where the shadows were");
            yield return Until(() => g.Feathers.Count == 0, 3f, "the feathers to land");
            Assert.AreEqual(masks - 1, _vitals.Masks, "one came down on her");

            yield return Recovered();
            g.ForceAttack(Gatekeeper.Attack.Feathers);
            yield return Until(() => g.FeatherVolleys == 2, 2f, "the next feathers");
            var f = g.Feathers[0];
            Assert.IsFalse(f.TakeHit(Strike(Vector2.right, _wren)), "stone, from the side");
            Assert.IsTrue(f.TakeHit(Strike(Vector2.down, _wren)), "the pogo breaks it");
            Assert.AreEqual(1, g.FeathersPogoed);
            Assert.AreEqual(2, g.Feathers.Count);
        }

        [UnityTest]
        public IEnumerator ItRisesThenTearsItsRootsAndOnlyTheBellyLands()
        {
            var kit = Build("gatekeeper");
            var g = (Gatekeeper)kit.Boss;
            g.standSeconds = 999f;
            float restY = g.transform.position.y;
            yield return StartFight(2f);
            for (int i = 0; i < 11; i++) g.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(2, g.Phase);
            yield return Until(() => g.Current != Gatekeeper.Move.Rise, 3f, "the rise");
            Assert.AreEqual(g.perchY, g.transform.position.y, 0.05f, "at the top of the gate");
            Assert.AreEqual(3, g.Roots.Count, "still rooted: the threads go up to it");
            Assert.IsTrue(g.TakeHit(Strike(Vector2.right)), "up here any strike lands");

            for (int i = 0; i < 11; i++) g.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, g.Phase);
            yield return Fixed(2);
            Assert.IsTrue(g.RootsTorn);
            Assert.AreEqual(0, g.Roots.Count, "the roots tear free");
            Assert.AreEqual(g.passY, g.transform.position.y, 0.05f, "it flies, low and heavy");
            Assert.IsFalse(g.TakeHit(Strike(Vector2.right)), "stone on top");
            Assert.IsFalse(g.TakeHit(Strike(Vector2.down)), "and from above");
            Assert.IsTrue(g.TakeHit(Strike(Vector2.up)), "open underneath");

            float x0 = g.transform.position.x;
            g.ForceAttack(Gatekeeper.Attack.Pass);
            yield return Until(() => g.Passes == 1 && g.Current == Gatekeeper.Move.Recover, 5f, "a pass");
            Assert.Greater(Mathf.Abs(g.transform.position.x - x0), 10f, "across the gate");

            kit.Arena.ResetFight();
            yield return Fixed(2);
            Assert.AreEqual(3, g.Roots.Count, "a retry finds it rooted again");
            Assert.AreEqual(restY, g.transform.position.y, 0.05f, "on its plinth");
            Assert.AreEqual(g.MaxHealth, g.Health);

            yield return Recovered();
            _ctrl.Teleport(new Vector2(-4f, 0f));
            yield return Fixed(5);
            yield return StartFight(2f);
            for (int i = 0; i < 22; i++) g.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, g.Phase);
            float scraps = Scraps();
            while (!g.IsDead) Assert.IsTrue(g.TakeHit(Strike(Vector2.up)));
            Won("gatekeeper", scraps);
        }
    }
}
