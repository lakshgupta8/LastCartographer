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
    /// A piece that changes with its place shows the world's drawing (ENV-06): a pair under a DressingProp swaps when
    /// its flag is set or its place decided and swaps back for a new game; and in the real chapel, the door asker's
    /// drawing opens on the flag the offering sets.
    /// </summary>
    public class DressingPropPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static (DressingProp prop, GameObject before, GameObject after) Rig(string piece, DressingChange change)
        {
            var go = new GameObject("Dressing_" + piece);
            var before = new GameObject("Prop_" + piece); before.transform.SetParent(go.transform, false);
            var after = new GameObject("Prop_" + piece + "_After"); after.transform.SetParent(go.transform, false);
            after.SetActive(false);
            var prop = go.AddComponent<DressingProp>();
            prop.Configure(piece, before, after, change);
            return (prop, before, after);
        }

        [UnityTest]
        public IEnumerator APieceChangesWithTheWorldAndBackForANewGame()
        {
            GameState.NewGame();
            var (cups, down, up) = Rig("cups", DressingChange.OnFlag("test.walked"));
            var (lintels, carved, chalk) = Rig("lintels", DressingChange.OnFate("Test_Village", PlaceFate.Held, PlaceFate.Anchored));
            yield return null;
            Assert.IsFalse(cups.IsChanged); Assert.IsTrue(down.activeSelf); Assert.IsFalse(up.activeSelf);
            Assert.IsFalse(lintels.IsChanged); Assert.IsTrue(carved.activeSelf);

            GameState.World.Set("test.walked", true);
            Assert.IsTrue(cups.IsChanged, "the flag turns the cups up at once");
            Assert.IsFalse(down.activeSelf); Assert.IsTrue(up.activeSelf);
            Assert.IsFalse(lintels.IsChanged, "another piece's flag is not this one's");

            GameState.World.Set(Places.Key("Test_Village"), (int)PlaceFate.Anchored);
            Assert.IsTrue(lintels.IsChanged, "the place's fate chalks the lintels");
            Assert.IsTrue(chalk.activeSelf); Assert.IsFalse(carved.activeSelf);
            GameState.World.Set(Places.Key("Test_Village"), (int)PlaceFate.Released);
            Assert.IsFalse(lintels.IsChanged, "released is not held or anchored");

            GameState.NewGame();
            yield return null;
            Assert.IsFalse(cups.IsChanged, "a new game turns them down again");
            Assert.IsTrue(down.activeSelf); Assert.IsFalse(up.activeSelf);
            GameState.World.Set("test.walked", true);
            Assert.IsTrue(cups.IsChanged, "and the new world is the one it listens to");
            Object.Destroy(cups.gameObject); Object.Destroy(lintels.gameObject);
        }

        [UnityTest]
        public IEnumerator TheChapelDoorOpensOnItsFlagInItsRoom()
        {
            var door = Offerings.All.First(a => a.Id == "chapel_door");
            yield return _replay.Boot();
            var rm = RoomManager.Instance;
            rm.Transition("Greybox_Saltmarrow_Chapel", "West");
            yield return RouteReplay.Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_Chapel" && !rm.IsTransitioning, 15f, "the chapel");
            var props = Room.Current!.GetComponentsInChildren<DressingProp>(true);
            var shut = props.FirstOrDefault(p => p.Piece == door.Node);
            Assert.IsNotNull(shut, "the chapel's door is a DressingProp (run OWSBG → Place the Coast's Readables)");
            Assert.IsFalse(shut!.IsChanged, "shut in a new game");
            Assert.IsTrue(shut.Before!.activeInHierarchy && !shut.After!.activeInHierarchy);
            Assert.AreEqual("Prop_" + door.Prop, shut.Before.name);
            Assert.AreEqual("Prop_" + door.Prop + "_Open", shut.After.name);
            var fade = Room.Current.GetComponentInChildren<FadeGroup>(true);
            Assert.IsNotNull(fade, "the chapel has a fade group");
            Assert.IsTrue(fade!.Layers.Any(l => l.Renderer != null && l.Renderer.gameObject == shut.Before) && fade.Layers.Any(l => l.Renderer != null && l.Renderer.gameObject == shut.After),
                "both drawings thin with the place");

            GameState.World.Set(door.Opens, true);
            yield return null;
            Assert.IsTrue(shut.IsChanged, "the offering's flag opens it");
            Assert.IsTrue(shut.After.activeInHierarchy && !shut.Before.activeInHierarchy);

            var tapestry = props.FirstOrDefault(p => p.Piece == "Chapel_Tapestry");
            Assert.IsNull(tapestry, "the tapestry never changes, so it has no DressingProp");
            Assert.IsTrue(Room.Current.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.name == "Prop_Tapestry"), "but it hangs there");
        }
    }
}
