using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The tuning audit (CMB-19, docs/design/tuning.md): every boss's kit, as the fight plays it, against the rules.
    /// No attack under its tier's floor; a strike takes one mask and a slam two, with at least the tier's typical read;
    /// each boss's strikes sit around the tier's typical read; four attacks a phase at most; health from the table.
    /// Enemies take their family's numbers, and every slam shakes the camera.
    /// </summary>
    public class TuningAuditTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        readonly List<GameObject> _made = new List<GameObject>();

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

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
            Shake.Stop();
            _room = new GameObject("Room_Tuning");
            _room.AddComponent<Room>();

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
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _ctrl.Teleport(new Vector2(-4f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
            if (_room != null) Object.Destroy(_room);
            if (_wren != null) Object.Destroy(_wren);
            Shake.Stop();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        /// <summary>A boss built into its room by the editor (the Lamp-Keeper, Halvard), standing on its sheet.</summary>
        T Rig<T>(string id, Vector2 at) where T : Boss
        {
            var go = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            _made.Add(go);
            go.transform.position = at;
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1.5f);
            go.AddComponent<Rigidbody2D>();
            var boss = go.AddComponent<T>();
            boss.ApplySheet(Bosses.Find(id));
            return boss;
        }

        static float Median(List<int> xs)
        {
            var s = xs.OrderBy(x => x).ToList();
            int n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) * 0.5f;
        }

        [UnityTest]
        public IEnumerator EveryBossKeepsTheRules()
        {
            var bosses = new List<(string id, Boss boss)>();
            for (int k = 0; k < BossKits.Ids.Length; k++)
                bosses.Add((BossKits.Ids[k], BossKits.Build(BossKits.Ids[k], _room.transform, new Vector2(0f, 40f * (k + 1))).Boss));
            bosses.Add(("lamp_keeper", Rig<LampKeeper>("lamp_keeper", new Vector2(0f, -40f))));
            bosses.Add(("halvard", Rig<Halvard>("halvard", new Vector2(0f, -80f))));
            yield return null;

            var failures = new List<string>();
            var table = new StringBuilder("[OWSBG] the kits as the tuning tables read them (CMB-19)\n");
            foreach (var (id, boss) in bosses)
            {
                var kit = boss.Kit().ToList();
                int tier = boss.Tier, floor = Tuning.TelegraphFloor(tier), typical = Tuning.TypicalTelegraph(tier);
                table.AppendLine($"{id} (tier {tier}, {boss.MaxHealth} health, floor {floor}, typical {typical})");
                if (kit.Count == 0) { failures.Add(id + ": no kit"); continue; }
                if (boss.MaxHealth != Tuning.BossHealth(id)) failures.Add($"{id}: health {boss.MaxHealth}, the table says {Tuning.BossHealth(id)}");

                foreach (var a in kit)
                {
                    table.AppendLine("    " + a + " phases " + string.Join("", Enumerable.Range(1, 3).Where(a.In)));
                    if (a.Phases == 0) failures.Add($"{id} {a.Name}: in no phase");
                    bool read = a.Kind != AttackKind.Shape || a.Telegraph > 0;
                    if (read && a.Telegraph < floor) failures.Add($"{id} {a.Name}: {a.Telegraph} frames, under the tier's floor of {floor}");
                    switch (a.Kind)
                    {
                        case AttackKind.Strike:
                            if (a.Damage != Tuning.Hit) failures.Add($"{id} {a.Name}: a strike takes {Tuning.Hit}, not {a.Damage}");
                            break;
                        case AttackKind.Slam:
                            if (a.Damage != Tuning.Slam) failures.Add($"{id} {a.Name}: a slam takes {Tuning.Slam}, not {a.Damage}");
                            if (a.Telegraph < typical) failures.Add($"{id} {a.Name}: a slam reads at least the tier's typical {typical}, not {a.Telegraph}");
                            break;
                        case AttackKind.Window:
                            if (a.Damage != 0 && a.Damage != Tuning.Hit) failures.Add($"{id} {a.Name}: a window takes {Tuning.Hit} at most");
                            break;
                        case AttackKind.Shape:
                            if (a.Damage != 0) failures.Add($"{id} {a.Name}: a shape hurts no one");
                            break;
                    }
                }

                var strikes = kit.Where(a => a.Kind == AttackKind.Strike).Select(a => a.Telegraph).ToList();
                if (strikes.Count > 0 && !Tuning.TypicalFits(tier, Median(strikes)))
                    failures.Add($"{id}: its strikes read {Median(strikes)} frames at the median; tier {tier} sits at {typical} ± {Tuning.TelegraphSpread}");

                for (int p = 1; p <= boss.PhaseCount; p++)
                {
                    int distinct = kit.Where(a => a.In(p)).Select(a => a.Name).Distinct().Count();
                    if (distinct == 0) failures.Add($"{id}: nothing in phase {p}");
                    if (distinct > 4) failures.Add($"{id}: {distinct} attacks in phase {p} (four at most, combat doc 8)");
                }
            }
            Debug.Log(table.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator TheSlamsAreTheBossesTheTableSays()
        {
            // The four attacks that come down on floor they marked first, and nothing else.
            var slams = new List<string>();
            for (int k = 0; k < BossKits.Ids.Length; k++)
            {
                var kit = BossKits.Build(BossKits.Ids[k], _room.transform, new Vector2(0f, 40f * (k + 1)));
                slams.AddRange(kit.Boss.Kit().Where(a => a.Kind == AttackKind.Slam).Select(a => BossKits.Ids[k] + "." + a.Name).Distinct());
            }
            slams.AddRange(Rig<LampKeeper>("lamp_keeper", new Vector2(0f, -40f)).Kit().Where(a => a.Kind == AttackKind.Slam).Select(a => "lamp_keeper." + a.Name));
            slams.AddRange(Rig<Halvard>("halvard", new Vector2(0f, -80f)).Kit().Where(a => a.Kind == AttackKind.Slam).Select(a => "halvard." + a.Name));
            yield return null;
            CollectionAssert.AreEquivalent(new[] { "collapse.Rubble", "gatekeeper.Landing", "fallen_star.Slam", "corras_drawing.Stomp" }, slams);
        }

        [UnityTest]
        public IEnumerator ASlamTakesTwoMasksAndShakesTheCamera()
        {
            var kit = BossKits.Build("collapse", _room.transform, Vector2.zero);
            kit.Arena.IntroSeconds = 0.05f;
            kit.Arena.RetryIntroSeconds = 0.02f;
            var c = (Collapse)kit.Boss;
            c.waitSeconds = 999f;
            float x = c.SectionCentre(1);
            _ctrl.Teleport(new Vector2(x, 0f));
            yield return Until(() => kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the doors to shut");

            int masks = _vitals.Masks;
            Shake.Stop();
            c.ForceAttack(Collapse.Attack.Rubble);
            yield return Until(() => c.Rubbles == 1 && c.Current == Collapse.Move.Wait, 3f, "the rubble to land");
            Assert.AreEqual(masks - Tuning.Slam, _vitals.Masks, "two masks");
            Assert.IsTrue(Shake.Active, "the hard shake");
            Assert.AreEqual(Tuning.SlamShake * Options.Shake, Shake.Amplitude, 1e-4f, "scaled by the player's shake (DES-14)");
        }

        [UnityTest]
        public IEnumerator AStrikeStillTakesOne()
        {
            var kit = BossKits.Build("collapse", _room.transform, Vector2.zero);
            kit.Arena.IntroSeconds = 0.05f;
            var c = (Collapse)kit.Boss;
            c.waitSeconds = 999f;
            _ctrl.Teleport(new Vector2(c.SectionCentre(2), 0f));
            yield return Until(() => kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the doors to shut");
            int masks = _vitals.Masks;
            Shake.Stop();
            c.ForceAttack(Collapse.Attack.Surge);
            yield return Until(() => _vitals.Masks < masks, 4f, "the surge to reach her");
            Assert.AreEqual(masks - Tuning.Hit, _vitals.Masks, "one mask");
            Assert.IsFalse(Shake.Active, "no shake for a strike (combat doc 1)");
        }

        [UnityTest]
        public IEnumerator EnemiesTakeTheirFamilysNumbers()
        {
            foreach (var family in new[] { typeof(ReedSkimmer), typeof(MarshCrab), typeof(Smudge), typeof(Cantor), typeof(Warden) })
            {
                var go = new GameObject(family.Name) { layer = Layer("Enemy") };
                _made.Add(go);
                go.transform.position = new Vector3(30f + _made.Count * 4f, 5f, 0f);
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var e = (Enemy)go.AddComponent(family);
                Assert.IsTrue(Tuning.TryEnemy(e.Family, out var tune), family.Name + " is in the table");
                Assert.AreEqual(tune.Health, e.MaxHealth, family.Name);
                Assert.AreEqual(tune.Health, e.Health, family.Name + " starts whole");
                Assert.AreEqual(Tuning.Hit, e.ContactDamage, family.Name);
            }
            yield return null;
        }
    }
}
