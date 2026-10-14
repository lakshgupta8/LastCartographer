using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Brann speaks before his fight (emberdown-arc.md, the story pass): when the cold furnace's doors shut he says the
    /// survey is already written and asks her to stand aside, and the fight waits until he has; a retry after a death goes
    /// straight to the fight.
    /// </summary>
    public class BrannSpeaksFirstTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static void Put(WrenController wren, float x, float y)
        {
            var rb = wren.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.position = new Vector2(x, y); rb.linearVelocity = Vector2.zero; }
            wren.transform.position = new Vector3(x, y, wren.transform.position.z);
        }

        [UnityTest]
        public IEnumerator HeSaysWhyAsTheDoorsShutAndTheFightWaits()
        {
            yield return _replay.Boot();
            var scene = WorldGraph.GreyboxPrefix + "Emberdown_Stair_3";
            RoomManager.Instance.Transition(scene, "Bottom");
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 15f, scene);
            yield return null;

            var talk = Object.FindFirstObjectByType<ArenaIntroTalk>();
            Assert.IsNotNull(talk, "the cold furnace's arena speaks first");
            Assert.AreEqual("Stair_Brann", talk!.Node);
            var arena = talk.Arena;
            Assert.IsNotNull(arena);
            var wren = Object.FindFirstObjectByType<WrenController>();
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();   // armed: she came in outside the arena

            Put(wren, 2f, 1f);
            yield return RouteReplay.Until(() => arena.State == BossArena.ArenaState.Intro, 5f, "the doors shut");
            yield return RouteReplay.Until(() => DialogueService.Instance!.IsRunning, 5f, "Brann speaks");
            for (int i = 0; i < 120; i++) yield return null;
            Assert.AreEqual(BossArena.ArenaState.Intro, arena.State, "the fight waits while he talks");

            yield return _replay.Follow("Brann", "Stair_Brann", new[] { 0 });   // they didn't ask for it
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("The survey is already written")));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Stand aside")));
            Assert.IsTrue(GameState.World.Is("emberdown.brann.met"));
            yield return RouteReplay.Until(() => arena.State == BossArena.ArenaState.Fighting, 5f, "then the fight");

            // A retry: nothing more to say, straight to the fight.
            arena.ResetFight();
            Put(wren, -16f, 0.2f);
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Put(wren, 2f, 1f);
            int talking = 0;
            for (float t = 0f; arena.State != BossArena.ArenaState.Fighting && t < 5f; t += Time.deltaTime)
            {
                if (DialogueService.Instance!.IsRunning) talking++;
                yield return null;
            }
            Assert.AreEqual(BossArena.ArenaState.Fighting, arena.State, "the retry's fight");
            Assert.Less(talking, 10, "he does not say it twice: the scene stops as it starts");
        }
    }
}
