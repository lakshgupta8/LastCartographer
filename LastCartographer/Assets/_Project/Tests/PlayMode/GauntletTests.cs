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
    /// The gauntlets (CMB-18, combat doc §9) and the two abilities they finished (CMB-04): Inkthread and the Windmemory
    /// glide. A fall costs a mask and returns her to solid ground, never her last mask; the Road's cobbles exist only in
    /// her lantern-radius; and a plain runner crosses each open gauntlet with its ability, and falls without it.
    /// </summary>
    public class GauntletTests
    {
        GameObject _root = null!, _wren = null!;
        WrenController _ctrl = null!;
        ScriptedInput _input = null!;
        AbilitySet _abilities = null!;
        WrenVitals _vitals = null!;
        Inkwell _ink = null!;

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
            _root = new GameObject("Room_Gauntlet");
            _root.AddComponent<Room>();
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _abilities = _wren.AddComponent<AbilitySet>();
            _ink = _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _input = new ScriptedInput();
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _ctrl.Teleport(new Vector2(-40f, 50f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            if (_wren != null) Object.Destroy(_wren);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        GameObject Floor(float x0, float x1, float top)
        {
            var f = new GameObject("Floor") { layer = Layer("Ground") };
            f.transform.SetParent(_root.transform, false);
            f.transform.position = new Vector3((x0 + x1) * 0.5f, top - 0.5f, 0f);
            f.AddComponent<BoxCollider2D>().size = new Vector2(x1 - x0, 1f);
            return f;
        }

        GauntletKit Build(string id)
        {
            var kit = GauntletKits.Build(id, _root.transform, Vector2.zero);
            Assert.IsNotNull(kit, id);
            return kit;
        }

        // ---- the abilities ----------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheThreadPullsHerToTheAnchorAheadForItsInk()
        {
            Floor(-10f, 10f, 0f);
            var anchor = TetherAnchor.Spawn(new Vector2(5f, 4f), float.PositiveInfinity);
            anchor.transform.SetParent(_root.transform, true);
            _ctrl.Teleport(new Vector2(0f, 0f));
            yield return Fixed(3);

            _input.PressThread();
            yield return Fixed(3);
            Assert.IsFalse(_ctrl.IsThreading, "no Inkthread yet");

            _abilities.Unlock(Ability.Inkthread);
            int refused = 0;
            _ctrl.ThreadRefused += () => refused++;
            _input.PressThread();
            yield return Fixed(2);
            Assert.AreEqual(1, refused, "no ink, no thread");

            _ink.Add(9);
            int pips = _ink.Pips;
            _input.PressThread();
            yield return Fixed(2);
            Assert.IsTrue(_ctrl.IsThreading, "the thread goes out");
            Assert.AreEqual(pips - 2, _ink.Pips, "two pips (combat doc 4)");
            Assert.AreEqual((Vector2)anchor.transform.position, _ctrl.ThreadTarget);
            float nearest = 99f;
            yield return Until(() =>
            {
                nearest = Mathf.Min(nearest, Vector2.Distance(_ctrl.Position + Vector2.up * 0.55f, anchor.transform.position));
                return !_ctrl.IsThreading;
            }, 2f, "the pull");
            Assert.LessOrEqual(nearest, _ctrl.threadArrive + 0.5f, "pulled to the anchor");
            Assert.Greater(_ctrl.Velocity.y, 0f, "and a hop off it");

            // The Ferryman's thread comes cheap.
            var charters = _wren.AddComponent<CharterSet>();
            yield return null;
            GameState.World.Equipment.OwnedCharters.Add(CharterKind.Ferryman);
            GameState.World.Equipment.SetCharter(CharterKind.Ferryman);
            Assert.AreEqual(1, _ctrl.ThreadCost);
            Object.Destroy(charters);
        }

        [UnityTest]
        public IEnumerator AMarkedEnemyIsAnAnchor()
        {
            Floor(-10f, 10f, 0f);
            var go = new GameObject("Crab") { layer = Layer("Enemy") };
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(5f, 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            go.AddComponent<Rigidbody2D>();
            var crab = go.AddComponent<MarshCrab>();
            _ctrl.Teleport(new Vector2(0f, 0f));
            _abilities.Unlock(Ability.Inkthread);
            _ink.Add(9);
            yield return Fixed(3);
            _input.PressThread();
            yield return Fixed(2);
            Assert.IsFalse(_ctrl.IsThreading, "unmarked, it is only a crab");
            crab.Mark(5f);
            _input.PressThread();
            yield return Fixed(2);
            Assert.IsTrue(_ctrl.IsThreading, "marked by the compass-dart, it is a thread point");
        }

        [UnityTest]
        public IEnumerator WindmemoryGlides()
        {
            _ctrl.Teleport(new Vector2(0f, 30f));
            _input.JumpHeld = true;
            yield return Fixed(40);
            Assert.Less(_ctrl.Velocity.y, -_ctrl.glideMaxFall - 1f, "without its wings she drops");
            Assert.IsFalse(_ctrl.IsGliding);

            _abilities.Unlock(Ability.Windmemory);
            _ctrl.Teleport(new Vector2(0f, 30f));
            yield return Fixed(40);
            Assert.IsTrue(_ctrl.IsGliding, "holding jump on the way down");
            Assert.GreaterOrEqual(_ctrl.Velocity.y, -_ctrl.glideMaxFall - 0.01f, "a slow fall");
            _input.JumpHeld = false;
            yield return Fixed(20);
            Assert.IsFalse(_ctrl.IsGliding, "let go, and she falls");
        }

        // ---- the gauntlet rules -----------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AFallCostsAMaskAndReturnsHerToSolidGround()
        {
            var kit = Build("lamp_posts");
            _ctrl.Teleport(new Vector2(-13f, 0f));
            yield return Fixed(5);
            Assert.AreEqual(-13f, kit.Gauntlet.LastSafe.x, 0.1f, "the bank is solid ground");
            int masks = _vitals.Masks;
            _ctrl.Teleport(new Vector2(-7f, 1f));
            yield return Until(() => kit.Gauntlet.Falls == 1, 3f, "the tide");
            Assert.AreEqual(masks - 1, _vitals.Masks, "a mask");
            Assert.AreEqual(-13f, _ctrl.Position.x, 0.1f, "back on the bank");

            yield return Until(() => !_vitals.IsInvulnerable, 3f, "i-frames");
            _vitals.SetMaxMasks(1);   // down to her last mask
            Assert.AreEqual(1, _vitals.Masks);
            _ctrl.Teleport(new Vector2(-7f, 1f));
            yield return Until(() => kit.Gauntlet.Falls == 2, 3f, "the tide again");
            Assert.AreEqual(1, _vitals.Masks, "never a full death");
            Assert.IsFalse(_vitals.IsDead);
        }

        [UnityTest]
        public IEnumerator TheRoadIsThereOnlyInHerLanternRadius()
        {
            var kit = Build("road_that_stops");
            _ctrl.Teleport(new Vector2(-12.5f, 0f));
            yield return Fixed(3);
            Assert.IsTrue(kit.LanternPlatforms.All(p => !p.IsDrawn), "without Clarity, outlines");
            _abilities.Unlock(Ability.Clarity);
            yield return Fixed(2);
            Assert.IsTrue(kit.LanternPlatforms[0].IsDrawn, "the first cobbles, in reach");
            Assert.IsTrue(kit.LanternPlatforms.Skip(1).All(p => !p.IsDrawn), "the rest still outlines");
            Assert.IsFalse(kit.LanternPlatforms[1].GetComponent<BoxCollider2D>().enabled, "and not there to stand on");
        }

        [UnityTest]
        public IEnumerator TheGapsAndClimbsAskForTheirAbility()
        {
            yield return null;
            float g = 2f * _ctrl.maxJumpHeight / (_ctrl.timeToApex * _ctrl.timeToApex);
            float fall = Mathf.Sqrt(2f * _ctrl.maxJumpHeight / (g * _ctrl.fallGravityMultiplier));
            float jumpReach = _ctrl.runSpeed * (_ctrl.timeToApex + fall);
            foreach (var p in Gauntlets.All)
            {
                var kit = Build(p.Id);
                if (kit.WidestGap > 0f) Assert.Greater(kit.WidestGap, jumpReach, p.Id + ": no plain jump crosses it");
                if (kit.TallestClimb > 0f && (p.Needs & Ability.Talonhold) != 0) Assert.Greater(kit.TallestClimb, _ctrl.maxJumpHeight, p.Id + ": no jump climbs it");
                Object.Destroy(kit.Gauntlet.gameObject);
            }
            Assert.LessOrEqual(8f, jumpReach + _ctrl.dashDistance, "the lamp posts: a jump and a Wingbeat");
        }

        // ---- a plain runner through each open gauntlet ------------------------------------------------------------------

        readonly System.Collections.Generic.List<string> _trace = new System.Collections.Generic.List<string>();

        bool GroundAhead()
        {
            var from = _ctrl.Position + new Vector2(0.5f * _ctrl.Facing, 0.2f);
            return Physics2D.Raycast(from, Vector2.down, 0.6f, LayerMask.GetMask("Ground")).collider != null;
        }

        /// <summary>An anchor ahead she isn't already at, measured as the thread measures: from her middle.</summary>
        bool AnchorAhead()
        {
            var p = _ctrl.Position + Vector2.up * 0.55f;
            return Object.FindObjectsByType<TetherAnchor>(FindObjectsSortMode.None).Any(a =>
            {
                float d = Vector2.Distance(a.transform.position, p);
                return a.transform.position.x - p.x > 0.25f && d > _ctrl.threadArrive && d <= _ctrl.threadRange;
            });
        }

        /// <summary>Run right, jump at edges, Wingbeat on the way down, thread to anchors ahead; until across, or a fall.</summary>
        IEnumerator Run(GauntletKit kit, bool fullJumps, float seconds)
        {
            _ctrl.Teleport(kit.Start);
            yield return Fixed(3);
            _ink.Add(9);
            _input.Move = Vector2.right;
            _input.JumpHeld = fullJumps;
            bool dashed = false;
            int threadWait = 0;
            float t = 0f;
            _trace.Clear();
            bool wasThreading = false;
            while (!kit.Gauntlet.Finished && kit.Gauntlet.Falls == 0 && t < seconds)
            {
                if (_ctrl.IsThreading != wasThreading || t % 0.25f < Time.fixedDeltaTime)
                    _trace.Add($"t={t:F2} p=({_ctrl.Position.x:F1},{_ctrl.Position.y:F1}) v=({_ctrl.Velocity.x:F1},{_ctrl.Velocity.y:F1}) thr={_ctrl.IsThreading} g={_ctrl.IsGrounded} ink={_ink.Pips}");
                wasThreading = _ctrl.IsThreading;
                if (_ctrl.IsGrounded) dashed = false;
                if (_ctrl.IsGrounded && !GroundAhead()) _input.PressJump();
                if (!_ctrl.IsGrounded && _ctrl.Velocity.y < 0f && !dashed && !_ctrl.IsThreading && _abilities.Has(Ability.Wingbeat)) { _input.PressDash(); dashed = true; }
                if (threadWait > 0) threadWait--;
                else if (!_ctrl.IsThreading && (!_ctrl.IsGrounded || !GroundAhead()) && AnchorAhead()) { _input.PressThread(); threadWait = 10; }
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            _input.Move = Vector2.zero;
            _input.JumpHeld = false;
        }

        IEnumerator WithoutThenWith(string id, Ability ability, bool fullJumps)
        {
            var kit = Build(id);
            yield return Run(kit, fullJumps, 8f);
            Assert.AreEqual(1, kit.Gauntlet.Falls, id + ": without " + ability + " she falls");
            Assert.IsFalse(kit.Gauntlet.Finished);
            Object.Destroy(kit.Gauntlet.gameObject);
            yield return Fixed(2);

            _abilities.Unlock(ability);
            _vitals.RestoreAll();
            kit = Build(id);
            yield return Fixed(2);
            yield return Run(kit, fullJumps, 12f);
            Assert.AreEqual(0, kit.Gauntlet.Falls, id + ": with " + ability + " she does not fall\n" + string.Join("\n", _trace));
            Assert.IsTrue(kit.Gauntlet.Finished, id + ": and she crosses");
            Assert.IsTrue(GameState.World.Is(Gauntlets.FlagKey(id)), "the flag");
            GameState.World.Numbers.TryGetValue("$vellum_scraps", out var scraps);
            Assert.AreEqual(1f, scraps, "a scrap, once");
        }

        [UnityTest] public IEnumerator TheLampPostsAskForTheWingbeat() { yield return WithoutThenWith("lamp_posts", Ability.Wingbeat, true); }
        [UnityTest] public IEnumerator TheCanopyAsksForTheThread() { yield return WithoutThenWith("canopy_threads", Ability.Inkthread, true); }
        [UnityTest] public IEnumerator TheUpdraftsAskForWindmemory() { yield return WithoutThenWith("updrafts", Ability.Windmemory, true); }
        [UnityTest] public IEnumerator TheRoadAsksForClarity() { yield return WithoutThenWith("road_that_stops", Ability.Clarity, false); }
    }

    /// <summary>Every gauntlet has a room RoomManager travels to.</summary>
    public class GauntletRoomsTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(Gauntlets.ScenePrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
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

        [UnityTest]
        public IEnumerator EachGauntletRoomLoads()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            Assert.IsTrue(RoomManager.Generators.Contains(GauntletRooms.Build));
            Assert.IsNull(GauntletRooms.Build("Gauntlet_Nowhere"));
            var wren = Object.FindFirstObjectByType<WrenController>()!;
            foreach (var p in Gauntlets.All)
            {
                var scene = Gauntlets.SceneFor(p.Id);
                RoomManager.Instance!.Transition(scene, "Start");
                yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
                var g = Room.Current!.GetComponentInChildren<Gauntlet>();
                Assert.IsNotNull(g, scene + " has its gauntlet");
                Assert.AreEqual(p.Id, g.Id);
                Assert.Less(Vector2.Distance(wren.Position, Room.Current.FindSpawn("Start").position), 1.5f, "she starts on its first ground");
            }
        }
    }
}
