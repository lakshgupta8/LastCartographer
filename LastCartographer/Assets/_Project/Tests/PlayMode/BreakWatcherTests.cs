#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>The break watcher (world-map §2): it wakes with the game, and a room only a soft gap reaches gets one line, a scrap and a flag, once.</summary>
    public class BreakWatcherTests
    {
        [SetUp] public void SetUp() => GameState.NewGame();
        [TearDown] public void TearDown() => GameState.NewGame();

        [UnityTest]
        public IEnumerator ARoomPastASoftGapIsNoticedOnceWithALine()
        {
            yield return null;
            var watcher = BreakWatcher.Instance;
            Assert.IsNotNull(watcher, "booted with the first scene");
            string? shown = null;
            System.Action<string, float> onShown = (t, _) => shown = t;
            Captions.Shown += onShown;
            try
            {
                Assert.IsNull(watcher!.OnRoom("Greybox_Saltmarrow_A"), "the quay is no break");
                Assert.IsNull(shown);
                int scraps = Commissions.Scraps(GameState.World);
                Assert.AreEqual("Saltmarrow.SaltChapel", watcher.OnRoom("Greybox_Saltmarrow_Chapel"), "past the lighthouse gap with no Wingbeat");
                Assert.IsNotNull(shown, "one line");
                StringAssert.Contains("Nobody comes this way", shown);
                Assert.AreEqual(scraps + SequenceBreaks.ScrapReward, Commissions.Scraps(GameState.World));
                Assert.AreEqual("Saltmarrow.SaltChapel", watcher.LastNoticed);
                shown = null;
                Assert.IsNull(watcher.OnRoom("Greybox_Saltmarrow_Chapel"), "once");
                Assert.IsNull(shown);
            }
            finally { Captions.Shown -= onShown; }
        }
    }
}
