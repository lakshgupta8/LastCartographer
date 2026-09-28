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
    /// The endgame bosses (CMB-16, boss sheets 6.12 to 6.15), each built from its kit and sheet and fought to its
    /// answers: the Bells' shrinking radius and the ropes she can only cut when she can see them; Corra's Drawing, its
    /// redrawn limbs and the small one; the Archivist's drawings, her drawing and the closing frame; the Complete
    /// Survey's named ground on the beat, its pools and the Sky.
    /// </summary>
    public class EndgameBossFightTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        Inkwell _ink;
        Flourishes _fl;
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
            Shader.SetGlobalFloat(CorrasDrawing.OutlineGlobal, 0f);
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
            w.Numbers.TryGetValue("$vellum_scraps", out var now);
            Assert.AreEqual(scrapsBefore + Bosses.Find(id).Scraps, now, "the sheet's scraps");
            Assert.IsFalse(_kit.DoorW.activeSelf, "the doors open");
        }

        [Test]
        public void TheEndgameKitsKeepToFourAttacksAPhase()
        {
            for (int p = 1; p <= 3; p++)
            {
                Assert.LessOrEqual(CorrasDrawing.PatternFor(p).Distinct().Count(), 4);
                Assert.LessOrEqual(Archivist.PatternFor(p).Distinct().Count(), 4);
            }
            CollectionAssert.AreEqual(new[] { 0 }, HalfCathedralBells.BellsOf(1).ToArray(), "one bell");
            CollectionAssert.AreEqual(new[] { 1, 2 }, HalfCathedralBells.BellsOf(2).ToArray(), "two, in canon");
            CollectionAssert.AreEqual(new[] { 3 }, HalfCathedralBells.BellsOf(3).ToArray(), "the great bell");
            CollectionAssert.AreEqual(new[] { Archivist.Attack.Frame }, Archivist.PatternFor(3).ToArray(), "at the last he stops drawing, and holds");
            Assert.AreEqual(3, CompleteSurvey.Inks.Length, "three regions' ink");
        }

        // ---- the Half-Cathedral Bells ------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EachRingShrinksHerRadiusAndTheVantageSteadiesIt()
        {
            var b = (HalfCathedralBells)Build("bells").Boss;
            b.gapSeconds = 999f;
            yield return StartFight(3f);
            Assert.AreEqual(b.fullRadius, b.Radius, 0.001f);
            float r = b.Radius;
            b.ForceRing(0);
            yield return Until(() => b.Rings == 1, 2f, "the first ring");
            Assert.AreEqual(r - b.shrinkPerRing, b.Radius, 0.001f, "each ring shrinks what she can see");
            Assert.AreEqual(b.Radius, Shader.GetGlobalFloat(HalfCathedralBells.RadiusGlobal), 0.001f, "for the lantern-radius pass");
            while (b.Radius > b.minRadius + 0.001f)
            {
                int rings = b.Rings;
                b.ForceRing(0);
                yield return Until(() => b.Rings > rings, 2f, "a ring");
            }
            int masks = _vitals.Masks;
            b.ForceRing(0);
            yield return Until(() => b.WhiteTakes == 1, 2f, "the white to take a mask");
            Assert.AreEqual(masks - 1, _vitals.Masks, "nothing left to shrink: it takes her");

            _ctrl.Teleport(new Vector2(b.vantageX, 0f));
            yield return Until(() => b.Steadies == 1, 2f, "the vantage");
            Assert.AreEqual(b.fullRadius, b.Radius, 0.001f, "the vantage Isolde taught her steadies it");
        }

        [UnityTest]
        public IEnumerator SheCutsARopeOnlyWhenSheCanSeeIt()
        {
            var b = (HalfCathedralBells)Build("bells").Boss;
            b.gapSeconds = 999f;
            yield return StartFight(3f);
            b.ForceRing(0);
            yield return Until(() => b.Rings == 1, 2f, "a ring");
            b.ForceRing(0);
            yield return Until(() => b.Rings == 2, 2f, "another");
            Assert.Greater(Mathf.Abs(b.ropeXs[0] - _ctrl.Position.x), b.Radius, "the rope is out of her radius");
            Assert.IsFalse(b.Ropes[0].TakeHit(Strike(Vector2.right)), "she cannot cut what she cannot see");
            Assert.IsFalse(b.Ropes[1].TakeHit(Strike(Vector2.right)), "nor a bell that is not ringing yet");

            _ctrl.Teleport(new Vector2(b.ropeXs[0] - 1f, 0f));
            yield return Fixed(2);
            Assert.IsTrue(b.Ropes[0].TakeHit(Strike(Vector2.right)), "near enough to see its outline");
            Assert.AreEqual(2, b.Phase, "two bells, in canon");
            Assert.AreEqual("For the ones who stayed.", b.PhaseLine(2), "read as the first is silenced");

            b.Reveal(0.3f);
            Assert.Greater(Mathf.Abs(b.ropeXs[2] - _ctrl.Position.x), b.Radius);
            Assert.IsTrue(b.Ropes[2].TakeHit(Strike(Vector2.right)), "the Field lantern shows everything");
            _ctrl.Teleport(new Vector2(b.ropeXs[1] + 0.5f, 0f));
            yield return Fixed(2);
            Assert.IsTrue(b.Ropes[1].TakeHit(Strike(Vector2.left)));
            Assert.AreEqual(3, b.Phase, "the great bell");

            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(b.IsRevealed);
            _ctrl.Teleport(new Vector2(b.ropeXs[3] - 1.5f, 0f));
            yield return Fixed(2);
            Assert.Less(b.Radius, b.greatRopeNeedsRadius);
            Assert.IsFalse(b.Ropes[3].TakeHit(Strike(Vector2.right)), "its rope is in the white");
            _ctrl.Teleport(new Vector2(b.vantageX, 0f));
            yield return Until(() => b.Radius >= b.fullRadius - 0.001f, 2f, "the vantage to steady her");
            _ctrl.Teleport(new Vector2(b.ropeXs[3] - 1.5f, 0f));
            yield return Fixed(2);
            float scraps = Scraps();
            Assert.IsTrue(b.Ropes[3].TakeHit(Strike(Vector2.right)), "wide enough, now");
            Won("bells", scraps);
            Assert.AreEqual(4, b.Cuts);
        }

        // ---- Corra's Drawing ---------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StrikeTheDrawnFramesAndLeaveTheSmallOneBe()
        {
            var cd = (CorrasDrawing)Build("corras_drawing").Boss;
            cd.standSeconds = 999f;
            yield return StartFight(3f);
            Assert.IsTrue(cd.TakeHit(Strike(Vector2.right)), "a drawn limb");
            Assert.IsFalse(cd.IsDrawn, "struck, it is redrawn");
            Assert.IsFalse(cd.TakeHit(Strike(Vector2.right)), "and nothing is drawn to strike");
            yield return Until(() => cd.IsDrawn, 2f, "the line to come back");
            Assert.IsTrue(cd.TakeHit(Strike(Vector2.right)));

            cd.redrawSeconds = 0.05f;
            while (cd.Phase < 2)
            {
                yield return Until(() => cd.IsDrawn, 2f, "a drawn frame");
                cd.TakeHit(Strike(Vector2.right));
            }
            Assert.IsTrue(cd.SmallDrawn, "it draws a second Voss, small, holding its hand");
            int hp = cd.Health;
            Assert.IsTrue(cd.Small.TakeHit(Strike(Vector2.right)));
            Assert.AreEqual(1, cd.SmallStruck);
            Assert.AreEqual(hp + cd.smallHeal, cd.Health, "I drew him bigger");
            Assert.IsFalse(cd.SmallDrawn);
            yield return Until(() => cd.SmallDrawn, cd.smallRedrawSeconds + 1f, "the small one to be drawn back");

            while (cd.Phase < 3)
            {
                yield return Until(() => cd.IsDrawn, 2f, "a drawn frame");
                cd.TakeHit(Strike(Vector2.right));
            }
            Assert.IsTrue(cd.IsOutline, "the crayon runs out");
            Assert.AreEqual(1f, Shader.GetGlobalFloat(CorrasDrawing.OutlineGlobal), 0.001f, "and the room's colour goes");
            yield return Until(() => cd.IsDrawn, 2f, "a drawn frame");
            cd.ForceAttack(CorrasDrawing.Attack.Swipe);
            yield return Fixed(2);
            Assert.LessOrEqual(cd.TelegraphLeft, cd.MinTelegraphFrames, "faster in outline, never under the tier's floor");

            float scraps = Scraps();
            while (!cd.IsDead)
            {
                yield return Until(() => cd.IsDrawn || cd.IsDead, 2f, "a drawn frame");
                cd.TakeHit(Strike(Vector2.right));
            }
            Won("corras_drawing", scraps);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(CorrasDrawing.OutlineGlobal), 0.001f);
            Assert.IsNull(cd.Small);
        }

        [UnityTest]
        public IEnumerator ItsSwipeAndStompLand()
        {
            var cd = (CorrasDrawing)Build("corras_drawing").Boss;
            cd.standSeconds = 999f;
            var b = cd.GetComponent<Collider2D>().bounds;
            yield return StartFight(b.min.x - 2f);
            int masks = _vitals.Masks;
            cd.ForceAttack(CorrasDrawing.Attack.Swipe);
            yield return Until(() => cd.Swipes == 1 && cd.Current == CorrasDrawing.Move.Recover, 2f, "the swipe");
            Assert.AreEqual(masks - 1, _vitals.Masks, "a crayon arm");
            yield return Recovered();
            masks = _vitals.Masks;
            _ctrl.Teleport(new Vector2(4f, 0f));
            cd.ForceAttack(CorrasDrawing.Attack.Stomp);
            yield return Until(() => cd.Stomps == 1 && cd.Current == CorrasDrawing.Move.Recover, 2f, "the stomp");
            Assert.AreEqual(masks - Tuning.Slam, _vitals.Masks, "where the crayon marked: a slam takes two (CMB-19)");
        }

        // ---- the Archivist -----------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HisDrawingsHoldWhileHisQuillIsOnThem()
        {
            var ar = (Archivist)Build("archivist").Boss;
            ar.standSeconds = 999f;
            yield return StartFight(5f);
            Assert.IsFalse(ar.Hand.TakeHit(Strike(Vector2.up)), "a hand that is not drawing is only a hand");
            ar.ForceAttack(Archivist.Attack.Draw);
            yield return Until(() => ar.Draws == 1, 2f, "the drawing");
            Assert.IsTrue(ar.IsDrawing);
            Assert.AreEqual(3, ar.Drawings.Count, "two walls and a floor");
            var xs = ar.Drawings.Where(d => d.name == "DrawnWall").Select(d => d.transform.position.x).OrderBy(x => x).ToArray();
            Assert.Less(xs[0], _ctrl.Position.x);
            Assert.Greater(xs[1], _ctrl.Position.x, "either side of her");
            Assert.AreEqual(LayerMask.NameToLayer("Ground"), ar.Drawings[0].layer, "real while his quill is on them");

            int hp = ar.Health;
            Assert.IsTrue(ar.Hand.TakeHit(Strike(Vector2.up)), "strike his quill hand");
            Assert.AreEqual(1, ar.Unmade);
            Assert.AreEqual(0, ar.Drawings.Count, "the drawing comes undone");
            Assert.IsTrue(ar.IsStaggered);
            Assert.AreEqual(hp - 1, ar.Health);

            yield return Until(() => !ar.IsStaggered && ar.Current == Archivist.Move.Stand, 3f, "him to stand");
            ar.ForceAttack(Archivist.Attack.Draw);
            yield return Until(() => ar.Draws == 2, 2f, "the next drawing");
            yield return Until(() => ar.Drawings.Count == 0, ar.drawSeconds + 1f, "his quill to lift");
            Assert.IsFalse(ar.IsDrawing, "the quill lifts and the walls go");

            yield return Recovered();
            int masks = _vitals.Masks;
            ar.ForceAttack(Archivist.Attack.Swoop);
            yield return Until(() => ar.Swoops == 1 && ar.Current == Archivist.Move.Recover, 4f, "the swoop");
            Assert.Less(_vitals.Masks, masks, "low across the floor");
        }

        [UnityTest]
        public IEnumerator HerDrawingHuntsHerAndTheFrameCloses()
        {
            var ar = (Archivist)Build("archivist").Boss;
            ar.standSeconds = 999f;
            yield return StartFight(5f);
            for (int i = 0, n = BossHits.ToPhase(ar, 2); i < n; i++) ar.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(2, ar.Phase);
            int masks = _vitals.Masks;
            ar.ForceAttack(Archivist.Attack.DrawWren);
            yield return Until(() => ar.WrenDrawn == 1, 2f, "her drawing");
            Assert.IsNotNull(ar.WrenDrawing);
            Assert.IsFalse(ar.WrenDrawing.TakeHit(Strike(Vector2.right)), "the quill passes through: it is her");
            yield return Until(() => _vitals.Masks < masks, 6f, "her drawing's jab");
            Assert.GreaterOrEqual(ar.WrenDrawingJabs, 1);
            Assert.IsTrue(ar.Hand.TakeHit(Strike(Vector2.up)));
            Assert.IsNull(ar.WrenDrawing, "his hand struck, her drawing is unmade too");

            yield return Recovered();
            for (int i = 0, n = BossHits.ToPhase(ar, 3); i < n; i++) ar.TakeHit(Strike(Vector2.right));
            Assert.AreEqual(3, ar.Phase);
            Assert.IsTrue(ar.FrameDrawn, "the Atlas frame round the arena");
            Assert.AreEqual(Archivist.Move.Holding, ar.Current, "he stops drawing, and holds");
            Assert.AreEqual(ar.arenaMinX, ar.FrameLeft, 0.001f);
            _ctrl.Teleport(new Vector2(1f, 0f));
            yield return Until(() => ar.FrameLeft > ar.arenaMinX, 2f, "a beat");
            Assert.AreEqual(ar.arenaMinX + ar.wingspan, ar.FrameLeft, 0.001f, "a wingspan a beat");
            yield return Until(() => ar.PageTakes >= 1, 2f, "the page to take her");

            _vitals.RestoreAll();   // outside the frame the page keeps taking her: answer from where she stands
            float left = ar.FrameLeft;
            var edge = _room.GetComponentsInChildren<BossPart>().First(p => p.name == "Frame_W");
            Assert.IsFalse(edge.TakeHit(Strike(Vector2.right)), "the quill does not move the frame");
            _ink.Add(9);
            Assert.AreEqual(1, _ctrl.Facing);
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Longstroke));
            yield return Until(() => ar.FramePushes == 1, 1f, "the Longstroke to push the edge");
            Assert.Less(ar.FrameLeft, left + ar.wingspan, "pushed back out");

            float scraps = Scraps();
            while (!ar.IsDead) ar.TakeHit(Strike(Vector2.right));
            Won("archivist", scraps);
            Assert.IsFalse(ar.FrameDrawn);
        }

        // ---- the Complete Survey -----------------------------------------------------------------------------------------

        /// <summary>Walk the bounds: stand on the next named ground every frame until the condition holds.</summary>
        IEnumerator Walk(CompleteSurvey cs, System.Func<bool> until, float seconds, string what)
        {
            float t = 0f;
            while (!until() && t < seconds)
            {
                if (cs.NextNamed >= 0) _ctrl.Teleport(new Vector2(cs.SectionCentre(cs.NextNamed), 0f));
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Assert.IsTrue(until(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator BeOnTheNamedGroundOnTheBeat()
        {
            var cs = (CompleteSurvey)Build("complete_survey").Boss;
            yield return StartFight(cs.SectionCentre(0));
            Assert.AreEqual(0, cs.NextNamed, "the first ground is named a beat ahead");
            yield return Until(() => cs.Beat == 1, 2f, "the first beat");
            Assert.AreEqual(0, cs.Named);
            Assert.AreEqual(0, cs.Fixes, "on the named ground: safe");
            Assert.AreEqual(1, cs.NextNamed, "Saltmarrow's tide walks a section a beat");
            Assert.AreEqual("Saltmarrow's tide", cs.Ink);

            int masks = _vitals.Masks;
            yield return Until(() => cs.Beat == 2, 2f, "the next beat");
            Assert.AreEqual(1, cs.Fixes, "off it, the ink fixes her where she stands");
            Assert.IsTrue(cs.IsFixing && _ctrl.Frozen);
            Assert.AreEqual(masks - 1, _vitals.Masks);
            yield return Until(() => !cs.IsFixing, 2f, "the fix to pass");
            Assert.IsFalse(_ctrl.Frozen);

            _vitals.RestoreAll();   // no waiting about: the next beat is coming
            yield return Walk(cs, () => cs.Beat == 7, 8f, "the verse's last beats");
            Assert.AreEqual(1, cs.Fixes, "walking the bounds, nothing fixes her");
            Assert.Greater(cs.SafeBeats, 4);
            Assert.AreEqual(-1, cs.NextNamed, "the chorus breathes next");
            _ctrl.Teleport(new Vector2(cs.SectionCentre(5), 0f));
            yield return Until(() => cs.Beat == 9, 3f, "the break");
            Assert.IsTrue(cs.InVerseBreak);
            Assert.AreEqual(-1, cs.Named);
            Assert.AreEqual(1, cs.Fixes, "between verses nothing is named: time to Bind");
        }

        [UnityTest]
        public IEnumerator PoolsWoundTheAtlasAndThePhasesQuicken()
        {
            var cs = (CompleteSurvey)Build("complete_survey").Boss;
            yield return StartFight(cs.SectionCentre(0));
            Assert.IsFalse(cs.TakeHit(Strike(Vector2.down)), "the Atlas itself cannot be struck");
            yield return Walk(cs, () => cs.Pools.Count >= 1, 6f, "ink to pool");
            int hp = cs.Health;
            Assert.IsTrue(cs.Pools[0].TakeHit(Strike(Vector2.down)), "strike the ink where it pools");
            Assert.AreEqual(hp - 1, cs.Health);
            Assert.AreEqual(1, cs.PoolsStruck);

            while (cs.Phase < 2) cs.ForcePool(2).TakeHit(Strike(Vector2.down));
            Assert.AreEqual(2, cs.Stride, "Emberdown's ash walks two a beat");
            Assert.AreEqual(0.75f, cs.BeatLength, 0.001f);
            Assert.AreEqual("Emberdown's ash", cs.Ink);
            while (cs.Phase < 3) cs.ForcePool(2).TakeHit(Strike(Vector2.down));
            Assert.AreEqual(3, cs.Stride);
            Assert.AreEqual(0.6f, cs.BeatLength, 0.001f, "Halden's late afternoon, fastest");
            Assert.AreEqual("Wren Halloway, journeyman. Drawn. Stay drawn.", cs.PhaseLine(2));

            int before = cs.Beat;
            yield return Walk(cs, () => cs.Beat >= before + 3, 4f, "three quick beats");

            float scraps = Scraps();
            while (!cs.IsDead) cs.ForcePool(4).TakeHit(Strike(Vector2.down));
            Won("complete_survey", scraps);
            Assert.IsTrue(_abilities.Has(Ability.Sky), "the Open World: she flies, once");
            Assert.IsFalse(_ctrl.Frozen);
            Assert.AreEqual(0, cs.Pools.Count);
        }
    }
}
