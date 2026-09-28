#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The late Charters (CMB-17, combat doc 5): the Ferryman's reach and reel, the Unwriter's unwriting and dearer Bind,
    /// the Remnant's drained colour; each rewrites the kit when worn, and each is handed over where the story says.
    /// </summary>
    public class LateChartersTests
    {
        GameObject _room = null!, _wren = null!;
        WrenController _ctrl = null!;
        WrenVitals _vitals = null!;
        QuillStrike _strike = null!;
        Flourishes _fl = null!;
        CharterSet _charters = null!;

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
            _room = new GameObject("Room_Test");
            _room.AddComponent<Room>();
            var floor = new GameObject("Floor") { layer = Layer("Ground") };
            floor.transform.SetParent(_room.transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _strike = _wren.AddComponent<QuillStrike>();
            _fl = _wren.AddComponent<Flourishes>();
            _charters = _wren.AddComponent<CharterSet>();
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

        Smudge Target(float x)
        {
            var go = new GameObject("Smudge") { layer = Layer("Enemy") };
            go.transform.SetParent(_room.transform, false);
            go.transform.position = new Vector3(x, 0.6f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            go.AddComponent<Rigidbody2D>();
            var s = go.AddComponent<Smudge>();
            s.ForceDrawn(true);
            s.SetMaxHealth(40);
            return s;
        }

        void Own(params CharterKind[] kinds) { foreach (var k in kinds) GameState.World.Equipment.OwnedCharters.Add(k); }
        void Wear(CharterKind k) { Assert.IsTrue(GameState.World.Equipment.SetCharter(k), "wear " + k); }

        [UnityTest]
        public IEnumerator EachLateCharterRewritesTheKit()
        {
            yield return null;
            var e = GameState.World.Equipment;
            Assert.AreEqual(6, _charters.Profiles.Count, "three base Charters and three found in the world");
            Assert.IsFalse(e.SetCharter(CharterKind.Ferryman), "not until it is handed over");

            Own(CharterKind.Ferryman, CharterKind.Unwriter, CharterKind.Remnant);
            Wear(CharterKind.Ferryman);
            CollectionAssert.AreEqual(new[] { "Hook", "Swing", "Reel" }, _strike.Combo.Select(s => s.Name).ToArray());
            Assert.IsTrue(_strike.Combo[2].Pulls, "the reel");
            Assert.AreEqual(FlourishKind.Longstroke, _fl.DefaultKind);
            Assert.AreEqual(1, _charters.Current.InkthreadCost, "the thread comes cheap");
            Assert.AreEqual(3, _vitals.BindCost);

            Wear(CharterKind.Unwriter);
            CollectionAssert.AreEqual(new[] { "Hush", "Hush", "Toll" }, _strike.Combo.Select(s => s.Name).ToArray());
            Assert.AreEqual(FlourishKind.Blot, _fl.DefaultKind);
            Assert.AreEqual(4, _vitals.BindCost, "Bind costs four");
            Assert.IsTrue(_charters.Current.ErasesProjectiles);

            Wear(CharterKind.Remnant);
            Assert.Greater(_strike.Drain, 0.3f, "every strike drains colour");
            Assert.AreEqual(FlourishKind.Crosshatch, _fl.DefaultKind);

            Wear(CharterKind.Surveyor);
            Assert.AreEqual(3, _vitals.BindCost, "back to three");
            Assert.AreEqual(0f, _strike.Drain);
            Assert.AreEqual(2, _charters.Current.InkthreadCost);
            foreach (CharterKind k in System.Enum.GetValues(typeof(CharterKind)))
                Assert.AreEqual(k, CharterProfile.For(k).Kind, k + " has a profile");
        }

        [UnityTest]
        public IEnumerator TheReelPullsWhatItHits()
        {
            var a = Target(3f);
            var b = Target(8f);
            yield return Fixed(2);
            Assert.IsTrue(a.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right, Pulls = true }));
            Assert.Less(a.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "reeled toward her");
            Assert.IsTrue(b.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }));
            Assert.Greater(b.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "a plain strike shoves");
        }

        [UnityTest]
        public IEnumerator TheRemnantDrainsColourAndGreySlows()
        {
            var s = Target(3f);
            s.RecolourDelay = 0.2f;
            s.RecolourSeconds = 0.3f;
            yield return Fixed(2);
            var drain = new HitInfo { Damage = 1, Direction = Vector2.right, Drain = CharterProfile.Remnant().Drain };
            s.TakeHit(drain);
            Assert.Less(s.Colour, 1f);
            Assert.IsFalse(s.IsGrey);
            s.TakeHit(drain);
            s.TakeHit(drain);
            Assert.IsTrue(s.IsGrey, "three strikes and it is grey");
            Assert.IsTrue(s.IsSlowed, "and slow");
            yield return Until(() => s.Colour >= 1f, 2f, "the colour to come back");
            Assert.IsFalse(s.IsGrey);
        }

        [UnityTest]
        public IEnumerator GreyTheDrawingHasNothingToRedrawWith()
        {
            var kit = BossKits.Build("corras_drawing", _room.transform, new Vector2(0f, 20f));
            kit.Arena.IntroSeconds = 0.05f;
            var cd = (CorrasDrawing)kit.Boss;
            cd.standSeconds = 999f;
            _ctrl.Teleport(new Vector2(3f, 20f));
            yield return Until(() => kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the fight");
            var drain = new HitInfo { Damage = 1, Direction = Vector2.right, Drain = CharterProfile.Remnant().Drain };
            for (int i = 0; i < 3; i++)
            {
                yield return Until(() => cd.IsDrawn, 2f, "a drawn frame");
                Assert.IsTrue(cd.TakeHit(drain));
            }
            Assert.IsTrue(cd.IsGrey, "its colour drained");
            int hp = cd.Health;
            Assert.IsTrue(cd.TakeHit(drain), "grey, nothing is redrawn");
            Assert.IsTrue(cd.TakeHit(drain));
            Assert.AreEqual(hp - 2, cd.Health, "the Remnant Charter's answer");
        }

        [UnityTest]
        public IEnumerator OnlyTheUnwriterUnwritesWhatIsThrown()
        {
            _ctrl.Teleport(new Vector2(0f, 0f));
            yield return Fixed(3);
            var p = EnemyProjectile.Spawn("Shot", _room.transform, new Vector2(6f, 0.6f), new Vector2(-4f, 0f), new Vector2(0.35f, 0.35f), 1, null);
            yield return Fixed(2);
            Assert.IsFalse(p.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right, Source = _wren }), "the Surveyor's quill goes through it");
            Own(CharterKind.Unwriter);
            Wear(CharterKind.Unwriter);
            int erased = 0;
            System.Action<EnemyProjectile> count = _ => erased++;
            EnemyProjectile.Erased += count;
            try { Assert.IsTrue(p.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right, Source = _wren }), "unwritten"); }
            finally { EnemyProjectile.Erased -= count; }
            Assert.AreEqual(1, erased);
            Assert.IsTrue(p.IsSpent);

            int masks = _vitals.Masks;
            var q = EnemyProjectile.Spawn("Shot", _room.transform, new Vector2(4f, 0.6f), new Vector2(-8f, 0f), new Vector2(0.35f, 0.35f), 1, null);
            yield return Until(() => q == null, 2f, "the next one to arrive");
            Assert.AreEqual(masks - 1, _vitals.Masks, "unstruck, it lands");
        }

        [UnityTest]
        public IEnumerator HaleFlicksInkAtRange()
        {
            var kit = BossKits.Build("hale", _room.transform, new Vector2(0f, 20f));
            kit.Arena.IntroSeconds = 0.05f;
            var h = (Hale)kit.Boss;
            h.standSeconds = 999f;
            _ctrl.Teleport(new Vector2(4f, 20f));
            yield return Until(() => kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the duel");
            Assert.IsTrue(Hale.PatternFor(2).Contains(Hale.Attack.Flick), "the flick from phase 2");
            Assert.LessOrEqual(Hale.PatternFor(2).Distinct().Count(), 4);
            Assert.LessOrEqual(Hale.PatternFor(3).Distinct().Count(), 4);
            int masks = _vitals.Masks;
            h.ForceAttack(Hale.Attack.Flick);
            yield return Until(() => h.Flicks == 1, 2f, "the flick");
            Assert.IsNotNull(h.LastFlick);
            Assert.Less(h.LastFlick.Velocity.x, 0f, "at her");
            yield return Until(() => _vitals.Masks < masks, 3f, "the ink to land");
        }

        [UnityTest]
        public IEnumerator TheChoirHandsOverTheUnwritersCharter()
        {
            var kit = BossKits.Build("choir", _room.transform, new Vector2(0f, 20f));
            kit.Arena.IntroSeconds = 0.05f;
            var ch = (Choir)kit.Boss;
            Assert.AreEqual(CharterKind.Unwriter, kit.Arena.RewardCharter, "from the sheet");
            _ctrl.Teleport(new Vector2(1f, 20f));
            yield return Until(() => kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the song");
            Assert.IsFalse(GameState.World.Equipment.OwnsCharter(CharterKind.Unwriter));
            while (!ch.IsDead) ch.Doves.First(d => d.gameObject.activeSelf).TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.IsTrue(GameState.World.Equipment.OwnsCharter(CharterKind.Unwriter), "from the last dove's wool");
        }
    }

    /// <summary>Sable and Ilse hand over theirs in the shipped scripts, through &lt;&lt;charter&gt;&gt;.</summary>
    public class LateChartersDialogueTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; EndingsRunner.AutoWalk = false; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            EndingsRunner.AutoWalk = true;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            ScreenFade.Clear();
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator Talk(string node)
        {
            var svc = DialogueService.Instance!;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            Assert.IsTrue(svc.StartNode(node), node);
            float t = 0f;
            yield return null;
            while (svc.IsRunning && t < 30f)
            {
                if (presenter.IsShowingOptions) presenter.Choose(0);
                else if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(svc.IsRunning, node + " ends");
        }

        [UnityTest]
        public IEnumerator SableAndIlseHandOverTheirs()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            var w = GameState.World;
            var e = w.Equipment;
            Assert.IsFalse(e.OwnsCharter(CharterKind.Ferryman));
            w.Set("act2.started", true);
            w.Set("saltmarrow.sable.aury", true);
            yield return Talk("Chain_Sable_Tether");
            Assert.IsTrue(w.Is("sable.tether_sold"));
            Assert.IsTrue(e.OwnsCharter(CharterKind.Ferryman), "with the cord");

            Assert.IsFalse(e.OwnsCharter(CharterKind.Remnant));
            yield return Talk("Hollow_Ilse");
            Assert.IsTrue(e.OwnsCharter(CharterKind.Remnant), "grey is a colour you can carry");
            Assert.IsTrue(e.SetCharter(CharterKind.Remnant), "and it can be worn");
        }
    }
}
