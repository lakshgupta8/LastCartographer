#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The full-playthrough matrix (PRO-05): every ending in every region order the map allows, and every sequence
    /// break the soft gaps permit, replayed through the shipped scripts (<see cref="RouteReplay"/>). A lock in any
    /// of them (a dimmed option, a node that won't start, a zone out of reach) fails that playthrough by name.
    /// </summary>
    public class PlaythroughMatrixTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        /// <summary>The matrix's names: the test runner lists one case each.</summary>
        public static IEnumerable<string> Names => Playthroughs.Names().ToList();

        [UnityTest]
        public IEnumerator EveryPlaythroughReachesItsEnding([ValueSource(nameof(Names))] string name)
        {
            var p = Playthroughs.Named(name);
            Assert.IsNotNull(p, name + " is in the matrix");
            yield return _replay.Boot();
            yield return _replay.Steps(name, p!.Steps, p.Ending, p.AllowSoft, p.BreakStep);
        }
    }
}
