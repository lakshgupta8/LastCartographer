#nullable enable
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The bosses' own voices in play (AUD-15, docs/design/enemy-sounds.md §2a): the Bells toll as a ring lands, after
    /// its tell, and the great bell tolls an octave under in phase 3; Brann's floor is heard as the heat shifts; the
    /// Gatekeeper's sweep grinds along the floor after the sweep's tell, never with it.
    /// </summary>
    public class BossVoiceTests
    {
        GameObject? _room;
        float _lx;
        InkSoundBank Bank => InkSoundBank.Instance!;

        IEnumerator Until(System.Func<bool> done, float seconds, string what)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(InkSoundBank.Instance, "booted with the first scene");
            Bank.Hush();
            _lx = Bank.ListenerX;   // the arena under the listener: every sound here is on screen
            _room = new GameObject("Room_BossVoice");
            _room.AddComponent<Room>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_room != null) Object.Destroy(_room);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        T Build<T>(string id) where T : Boss => (T)BossKits.Build(id, _room!.transform, new Vector2(_lx - 9f, 0f)).Boss;
        int At(string cue) => Bank.Recent.ToList().LastIndexOf(cue);

        [UnityTest]
        public IEnumerator TheBellsTollAsARingLandsAndTheGreatBellAnOctaveUnder()
        {
            var bells = Build<HalfCathedralBells>("bells");
            yield return null;
            bells.BeginFight();
            yield return null;
            Bank.Hush();
            bells.ForceRing(0);
            yield return Until(() => Bank.Recent.Contains("bells_toll"), 4f, "the toll");
            Assert.Greater(At("bells_toll"), Bank.Recent.ToList().IndexOf("tell_window"), "the chime is the read; the toll lands after it");
            Assert.GreaterOrEqual(bells.Rings, 1);
            Assert.AreEqual("bells_toll", bells.GetComponent<EnemyVoice>().LastEvent);
            typeof(Boss).GetMethod("AdvanceToPhase", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(bells, new object[] { 3 });
            Assert.AreEqual(3, bells.Phase);
            yield return null;
            bells.ForceRing(3);
            yield return Until(() => Bank.Recent.Contains("bells_toll_p3"), 4f, "the great bell");
            Assert.AreEqual("bells_toll_p3", bells.GetComponent<EnemyVoice>().LastEvent, "phase 3's own take: the great bell");
        }

        [UnityTest]
        public IEnumerator BrannsFloorIsHeardAsTheHeatShifts()
        {
            var brann = Build<Brann>("brann");
            yield return null;
            brann.BeginFight();
            int shifts = brann.Shifts;
            yield return Until(() => brann.Shifts > shifts, brann.shiftSeconds * 2f + 1f, "the floor to shift");
            yield return null;
            Assert.That(Bank.Recent, Does.Contain("brann_shift"), "the grates clank as the heat moves");
        }

        [UnityTest]
        public IEnumerator TheGatekeepersSweepGrindsAfterItsTellNotWithIt()
        {
            var gate = Build<Gatekeeper>("gatekeeper");
            yield return null;
            gate.BeginFight();
            yield return null;
            Bank.Hush();
            gate.ForceAttack(Gatekeeper.Attack.Sweep);
            yield return Until(() => gate.Clip == "telegraph", 2f, "the sweep's read");
            yield return null;
            Assert.IsFalse(Bank.Recent.Any(c => c.StartsWith("gate_")), "the read is the tell's: nothing of its own yet");
            yield return Until(() => Bank.Recent.Contains("gate_sweep"), 3f, "the sweep");
            Assert.Greater(At("gate_sweep"), Bank.Recent.ToList().FindIndex(c => c.StartsWith("tell_")), "the grind after the tell");
            Assert.AreEqual("gate_sweep", gate.GetComponent<EnemyVoice>().LastMove);
        }
    }
}
