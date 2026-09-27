#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>The staged Lamp-Keeper carries her sheet (NAR-06): name, tier and the three lines in the built room match the data.</summary>
    public class BossSheetPlayTests
    {
        [SetUp] public void SetUp() { GameState.NewGame(); Bosses.Reset(); Bosses.EnsureDefaults(); }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator TheLampKeeperReadsFromHerSheet()
        {
            var handle = Addressables.LoadSceneAsync("Greybox_Saltmarrow_Lighthouse", LoadSceneMode.Additive);
            yield return handle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status);
            var arena = Object.FindFirstObjectByType<BossArena>(FindObjectsInactive.Include);
            Assert.IsNotNull(arena, "the lighthouse has its arena");
            var sheet = Bosses.Find(arena!.BossId);
            Assert.IsNotNull(sheet, "the arena's boss id has a sheet: " + arena.BossId);
            Assert.AreEqual(sheet!.FlagKey, arena.FlagKey);
            var boss = arena.Boss;
            Assert.AreEqual(sheet.Name, boss.BossName);
            Assert.AreEqual(sheet.Tier, boss.Tier);
            for (int p = 1; p <= 3; p++) Assert.AreEqual(sheet.Lines[p - 1], boss.PhaseLine(p), "phase " + p);
        }
    }
}
