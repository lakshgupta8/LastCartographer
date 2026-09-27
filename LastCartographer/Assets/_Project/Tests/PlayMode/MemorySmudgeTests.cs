#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Death's smudge in the running game (GDD 6, PRG-17): dying drops what she bound where she fell as a
    /// MemorySmudge, the return leaves it behind, striking it down brings the memories back, and a second death
    /// moves the drop rather than losing it.
    /// </summary>
    public class MemorySmudgeTests
    {
        GameObject? _floor, _wren, _room, _systems;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        ScriptedInput? _input;
        PlayerRespawn? _respawn;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(6f, 0f));

            _room = new GameObject("Room_Smudge");
            var room = _room.AddComponent<Room>();
            room.RoomId = "Smudge_Test";
            var desk = new GameObject("Spawn_Desk");
            desk.transform.SetParent(_room.transform, false);
            desk.transform.position = new Vector3(-8f, 0f, 0f);
            GameState.World.RespawnSpawn = "Desk";

            _systems = new GameObject("Systems");
            _respawn = _systems.AddComponent<PlayerRespawn>();
            _systems.AddComponent<MemoryDrops>();
        }

        [TearDown]
        public void TearDown()
        {
            if (MemoryDrops.Current != null) Object.Destroy(MemoryDrops.Current.gameObject);
            foreach (var go in new[] { _systems, _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator Die()
        {
            bool respawned = false;
            _respawn!.Respawned += () => respawned = true;
            for (int i = 0; i < _vitals!.MaxMasks; i++)
            {
                float t = 0f;
                while (_vitals.IsInvulnerable && t < 3f) { t += Time.deltaTime; yield return null; }
                Assert.IsTrue(_vitals.Damage(1), "hit " + i + " lands");
            }
            Assert.IsTrue(_vitals.IsDead);
            float u = 0f;
            while (!respawned && u < 5f) { u += Time.deltaTime; yield return null; }
            Assert.IsTrue(respawned, "the return");
        }

        [UnityTest]
        public IEnumerator DeathDropsHerMemoriesAsASmudgeAndStrikingItDownRecoversThem()
        {
            yield return null;   // systems bind to her
            var w = GameState.World;
            Memories.Bind(w, "isolde.first_sight");
            float fellAt = _ctrl!.Position.x;

            yield return Die();
            Assert.AreEqual(-8f, _ctrl.Position.x, 0.05f, "back at the desk");
            Assert.IsEmpty(w.BoundMemories, "out of reach");
            Assert.IsTrue(Memories.HasDrop(w));
            Assert.AreEqual("Smudge_Test", w.DropRoom);
            CollectionAssert.AreEqual(new[] { "isolde.first_sight" }, w.DroppedMemories);
            var smudge = MemoryDrops.Current;
            Assert.IsNotNull(smudge, "a smudge stands where she fell");
            Assert.AreEqual(fellAt, smudge!.transform.position.x, 0.6f);
            Assert.AreEqual("MemorySmudge", smudge.Family);
            Assert.AreEqual(0, IrisSeedDrops.DropsFor(smudge.Family), "it drops no seeds");
            Assert.AreEqual(3, smudge.MaxHealth);

            // Walk back and strike it down while it is drawn.
            smudge.ForceDrawn(false);
            Assert.IsFalse(smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.left }), "undrawn frames cannot be hit");
            smudge.ForceDrawn(true);
            for (int i = 0; i < 3; i++) Assert.IsTrue(smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.left }));
            Assert.IsTrue(smudge.IsDead);
            Assert.IsNull(MemoryDrops.Current);
            Assert.IsFalse(Memories.HasDrop(w));
            CollectionAssert.AreEqual(new[] { "isolde.first_sight" }, w.BoundMemories, "hers again");
        }

        [UnityTest]
        public IEnumerator ASecondDeathMovesTheDropAndLosesNothing()
        {
            yield return null;
            var w = GameState.World;
            Memories.Bind(w, "isolde.first_sight");
            yield return Die();
            var first = MemoryDrops.Current;
            Assert.IsNotNull(first);

            // She binds something new on the way back, and dies again before reaching the smudge.
            Memories.Bind(w, "sable.boats_back");
            _ctrl!.Teleport(new Vector2(-2f, 0f));
            yield return Die();
            var second = MemoryDrops.Current;
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second, "a new smudge at the new spot");
            Assert.IsTrue(first == null || first.IsDead || !first.gameObject.activeInHierarchy, "the old one is gone");
            Assert.AreEqual(-2f, second!.transform.position.x, 0.6f);
            CollectionAssert.AreEquivalent(new[] { "isolde.first_sight", "sable.boats_back" }, w.DroppedMemories, "folded forward");

            second.ForceDrawn(true);
            for (int i = 0; i < 3; i++) second.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            CollectionAssert.AreEquivalent(new[] { "isolde.first_sight", "sable.boats_back" }, w.BoundMemories);
        }

        [UnityTest]
        public IEnumerator NothingBoundLeavesNoSmudge()
        {
            yield return null;
            yield return Die();
            Assert.IsFalse(Memories.HasDrop(GameState.World));
            Assert.IsNull(MemoryDrops.Current);
        }
    }
}
