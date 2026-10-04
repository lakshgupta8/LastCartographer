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
    /// The boss fights' loose pieces in play (AUD-16, docs/design/enemy-sounds.md §2b): the Gatekeeper's stone feathers
    /// shatter where they meet the floor; the Brood's fire burns while it is there and not after; a part its owner
    /// speaks for has no voice of its own.
    /// </summary>
    public class PartVoiceTests
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
            _lx = Bank.ListenerX;
            _room = new GameObject("Room_PartVoice");
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

        [UnityTest]
        public IEnumerator TheGatekeepersFeathersShatterOnTheFloor()
        {
            var gate = Build<Gatekeeper>("gatekeeper");
            yield return null;
            gate.BeginFight();
            yield return null;
            Bank.Hush();
            gate.ForceAttack(Gatekeeper.Attack.Feathers);
            yield return Until(() => gate.Feathers.Count > 0, 3f, "the feathers to fall");
            var feather = gate.Feathers[0];
            yield return null;
            Assert.AreEqual("Feather", feather.GetComponent<PartVoice>().Voice.Name, "every part wears a voice");
            yield return Until(() => Bank.Recent.Contains("feather_shatter"), 3f, "a feather to shatter");
            Assert.IsFalse(Bank.Recent.Contains("feather_crack"), "nothing struck them");
        }

        [UnityTest]
        public IEnumerator TheBroodsFireBurnsWhileItIsThere()
        {
            var brood = Build<ReedmotherBrood>("reedmother_brood");
            yield return null;
            brood.BeginFight();
            yield return null;
            brood.LightTheFire();
            yield return null;
            yield return null;
            var voice = brood.Fire!.GetComponent<PartVoice>();
            Assert.AreEqual("fire_burn", voice.Looping, "the fire burning in the beds");
            Object.Destroy(brood.Fire.gameObject);
            yield return null;
            Assert.IsTrue(voice == null, "gone with the fire");
        }

        [UnityTest]
        public IEnumerator APartItsOwnerSpeaksForHasNoVoice()
        {
            var bells = Build<HalfCathedralBells>("bells");
            yield return null;
            yield return null;
            yield return null;
            var rope = _room!.GetComponentsInChildren<BossPart>().First(p => p.name.StartsWith("Rope_"));
            var voice = rope.GetComponent<PartVoice>();
            Assert.IsNotNull(voice, "it wears one");
            Assert.IsNull(voice.Voice, "but the Bells speak for their ropes");
            Assert.IsFalse(voice.enabled);
            Assert.IsNotNull(bells);
        }
    }
}
