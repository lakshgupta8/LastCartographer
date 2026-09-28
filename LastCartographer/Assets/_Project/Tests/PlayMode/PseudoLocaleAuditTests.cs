#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The pseudo-locale as an audit (NAR-18): the game is booted in en-XA with something on every page (a commission
    /// taken, a place drawn, Instruments on the belt, a seller's table), and every page is opened in turn: the HUD, the
    /// desk, the atlas with its journal, the ledger, the shop, and the options with their controls page. Any text shown
    /// with a letter outside «…» is English that skipped the tables. Text marked verbatim (a language's own name, a
    /// key's name) is passed over.
    /// </summary>
    public class PseudoLocaleAuditTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            Options.ResetToDefaults();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            OptionsView.Instance?.Close();
            Pause.End();
            Bootstrap.SkipPrologueOverride = null;
            DialogueService.Instance?.Stop();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            Options.ResetToDefaults();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static readonly Regex Bracketed = new Regex("«[^»]*»");

        static bool Visible(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent) if (p.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        static bool Verbatim(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent) if (p.ClassListContains(InkTheme.VerbatimClass)) return true;
            return false;
        }

        /// <summary>Every label shown under <paramref name="root"/> with a letter outside the pseudo-locale's brackets.</summary>
        static List<string> English(VisualElement root)
        {
            var found = new List<string>();
            root.Query<TextElement>().ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.text) || !Visible(t) || Verbatim(t)) return;
                var outside = Bracketed.Replace(t.text, "");
                if (outside.Any(char.IsLetter)) found.Add((t.name ?? "?") + ": " + t.text);
            });
            return found;
        }

        static void Audit(VisualElement root, string page)
        {
            var found = English(root);
            CollectionAssert.IsEmpty(found, page + " shows English in the pseudo-locale");
        }

        static int Shown(VisualElement root, System.Func<TextElement, bool>? which = null)
        {
            int n = 0;
            root.Query<TextElement>().ForEach(t => { if (!string.IsNullOrEmpty(t.text) && Visible(t) && (which == null || which(t))) n++; });
            return n;
        }

        [UnityTest]
        public IEnumerator EveryPageSpeaksThePseudoLocale()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");

            // Something on every page, in the pseudo-locale from the start (a toast keeps the language it was made in).
            Loc.SetLocale(Loc.Pseudo);
            var w = GameState.World;
            var def = CommissionCatalog.All.First(d => d.Hub == "Saltmarrow" && d.Steps.Length > 0);
            Commissions.Post(w, def.Id);
            Commissions.Take(w, def.Id);
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            Economy.AddSeeds(w, 3);
            Commissions.AddScraps(w, 3);
            var wren = Object.FindFirstObjectByType<WrenController>();
            var ui = UiRoot.Instance;

            yield return Frames(2);

            Loc.SetLocale(Loc.Base);
            yield return Frames(2);
            Loc.SetLocale(Loc.Pseudo);
            yield return Frames(2);     // the HUD re-texts at once, not on its next tick
            Assert.Greater(Shown(ui.Hud), 0, "the HUD shows something");
            Audit(ui.Hud, "the HUD");

            var desk = ui.GetComponent<DeskMenu>();
            desk.Open(wren);
            yield return Frames(3);
            Assert.Greater(Shown(desk.Panel), 5);
            Audit(desk.Panel, "the desk");
            for (int r = 1; r < desk.RowCount; r++) { desk.GetType().GetProperty("Row")!.SetValue(desk, r); desk.Refresh(); Audit(desk.Panel, "the desk, row " + r); }
            desk.Close();

            var atlas = AtlasView.Instance!;
            atlas.Open(wren);
            yield return Frames(3);
            Assert.Greater(Shown(ui.Desk), 5, "the atlas and its journal");
            Audit(ui.Root, "the atlas and the journal");
            atlas.Close();
            yield return null;

            var ledgerGo = new GameObject("AuditLedger");
            try
            {
                ledgerGo.AddComponent<BoxCollider2D>().isTrigger = true;   // an Interactable needs its trigger
                var ledger = ledgerGo.AddComponent<CommissionLedger>();
                ledger.HubId = "Saltmarrow";
                var ledgerView = ui.GetComponent<LedgerView>();
                ledgerView.Open(ledger, wren);
                yield return Frames(3);
                Assert.Greater(Shown(ui.Desk), 3, "the ledger");
                Audit(ui.Root, "the ledger");
                ledgerView.Close();
            }
            finally { Object.Destroy(ledgerGo); }
            yield return null;

            var shop = ui.GetComponent<ShopView>();
            shop.Open("Saltmarrow");
            yield return Frames(3);
            Assert.Greater(Shown(ui.Desk), 3, "the shop");
            Audit(ui.Root, "the shop");
            shop.Close();
            yield return null;

            var options = OptionsView.Instance!;
            options.Open();
            yield return Frames(2);
            Audit(options.Panel!, "the options");
            for (int r = 0; r < (int)OptionsView.Item.Controls; r++) options.MoveRow(+1);
            options.Activate();
            yield return Frames(2);
            Assert.AreEqual(OptionsView.Page.Controls, options.Current);
            Assert.Greater(Shown(options.Panel!, Verbatim), 0, "key names are shown, as they are");
            Audit(options.Panel!, "the controls page");
            options.Close();
        }
    }
}
