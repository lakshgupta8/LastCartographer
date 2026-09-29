#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Every readable piece of the environmental pass (NAR-15, <see cref="Dressing"/>) read through the shipped Yarn
    /// project, as a new game finds it and again once its places have been decided: each one runs to its end with
    /// nothing to choose, and the world is exactly as it was before it was read.
    /// </summary>
    public class DressingReadTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        IEnumerator ReadEverything(string when)
        {
            foreach (var p in Dressing.All.Where(p => p.Readable))
            {
                string before = GameState.ToJson(GameState.World);
                yield return _replay.Talk(when + ", " + p.Id, p.Node, new int[0]);
                Assert.AreEqual(before, GameState.ToJson(GameState.World), when + ": reading " + p.Node + " changes nothing");
            }
        }

        [UnityTest]
        public IEnumerator EveryPieceReadsAndChangesNothing()
        {
            yield return _replay.Boot();
            yield return ReadEverything("a new game");

            // The places the conditional pieces watch, decided.
            var w = GameState.World;
            w.Set("emberdown.hollowvein.walked", true);
            Places.Hold(w, "Saltmarrow_B");
            Places.Release(w, "Verdance_Aldermere_2");
            Places.Anchor(w, "Halden_Lowmarket_2");
            yield return ReadEverything("the places decided");
        }
    }
}
