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
    /// The Bone Bridge crossing (NAR-04, bible [F 3.4]): Sable stands at the bridge while the whale's commission is
    /// taken, rows Wren under the bones once, does not sing along, and is gone from the bridge after.
    /// </summary>
    public class BoneBridgeSableTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        [UnityTest]
        public IEnumerator SheRowsHerUnderOnceWhileTheCommissionIsTaken()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var go = new GameObject("Sable_Bridge_Test");
            new GameObject("Drawing").transform.SetParent(go.transform, false);
            var at = go.AddComponent<FlagPresence>();
            at.Configure(Commissions.StateKey("saltmarrow.bone_bridge"), "saltmarrow.bone_bridge.rowed", 0, (int)CommissionState.Taken);
            yield return null;
            Assert.IsFalse(at.Standing, "nothing posted: the quay keeps her");
            w.Set(Commissions.StateKey("saltmarrow.bone_bridge"), (int)CommissionState.Posted);
            Assert.IsFalse(at.Standing, "posted is not taken");
            w.Set(Commissions.StateKey("saltmarrow.bone_bridge"), (int)CommissionState.Taken);
            Assert.IsTrue(at.Standing, "taken: she waits at the bridge");

            yield return _replay.Talk("Sable", "BoneBridge_Sable", new[] { 0 });   // it's a list; those are names
            Assert.IsTrue(w.Is("saltmarrow.bone_bridge.rowed"));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("nobody knows the words")), "she does not sing along");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("A miner would say names")), "and points at Emberdown without naming it");
            Assert.IsFalse(_replay.Heard.Any(l => l.StartsWith("Sable:") && l.Contains("roll-call")), "the song is never named here");
            Assert.AreEqual(1, Voices.Count(w, Voice.Surveyor, "saltmarrow"));
            Assert.IsFalse(at.Standing, "rowed: she is back at the quay");

            yield return _replay.Talk("Sable", "BoneBridge_Sable", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("not a ferry")));
            Object.Destroy(go);
        }
    }
}
