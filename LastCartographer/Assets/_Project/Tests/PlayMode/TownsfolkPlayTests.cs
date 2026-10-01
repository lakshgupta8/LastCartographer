#nullable enable
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
    /// The townsfolk library in a room (CHR-12): the clan at the camp cheers and watches and the rest idle, from their
    /// sheets, with nobody to talk to; and the gannet on the faded rail is a person from the library, greyed as a
    /// Remnant from the start, with the asker's talker on her and no block.
    /// </summary>
    public class TownsfolkPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static IEnumerator Enter(string scene, string spawn)
        {
            var rm = RoomManager.Instance;
            rm.Transition(scene, spawn);
            yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 15f, scene);
            yield return null;
        }

        static NpcAnimator Folk(string look) =>
            Room.Current!.GetComponentsInChildren<NpcAnimator>(true).First(a => a.name.StartsWith("Folk_" + look + "_"));

        [UnityTest]
        public IEnumerator TheClanCheersAndWatchesAtTheCampAndTheRestIdle()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Windreach_Camp_1", "West");
            var crowd = Room.Current!.GetComponentsInChildren<NpcAnimator>(true).Where(a => a.name.StartsWith("Folk_")).ToList();
            Assert.GreaterOrEqual(crowd.Count, 3, "the clan at the wagon");
            foreach (var a in crowd)
            {
                Assert.IsNull(a.GetComponent<NpcTalker>(), a.name + ": nobody to talk to");
                Assert.IsNull(a.GetComponent<Collider2D>(), a.name + ": nothing to bump");
                var sheet = a.GetComponent<InkSheetPlayer>();
                Assert.IsTrue(sheet.Has("watching") && sheet.Has("cheering") && sheet.Has("idle"), a.name + " on the library's sheets");
                Assert.IsNotNull(a.GetComponent<NpcInk>(), a.name + " carries its colour state");
            }
            yield return null;
            yield return null;
            Assert.AreEqual("cheering", Folk("Lark").Clip, "the lark cheers");
            Assert.AreEqual("cheering", Folk("Lark").Activity);
            Assert.AreEqual("watching", Folk("Plover").Clip, "the plover watches the leap");
            Assert.AreEqual("idle", Folk("Hoopoe").Clip, "the hoopoe stands");
            Assert.AreEqual(NpcInkState.Drawn, Folk("Lark").GetComponent<NpcInk>().State, "the clans' places are never anchored and never fade");

            // Told to do something else, it does it; told nothing, it idles.
            Folk("Hoopoe").Activity = "cheering";
            yield return null;
            yield return null;
            Assert.AreEqual("cheering", Folk("Hoopoe").Clip);
            Folk("Lark").Activity = "";
            yield return null;
            yield return null;
            Assert.AreEqual("idle", Folk("Lark").Clip);
        }

        [UnityTest]
        public IEnumerator TheGannetOnTheFadedRailIsAGreyPersonWhoAsks()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Saltmarrow_Chain_3", "West");
            var read = Room.Current!.transform.Find("Read_Chain_Gannet");
            Assert.IsNotNull(read, "the asker at the faded light's rail");
            Assert.IsNull(read.Find("Marker"), "no block");
            var talker = read.GetComponent<NpcTalker>();
            Assert.AreEqual("Chain_Gannet", talker.StartNode);
            var sheet = read.GetComponent<InkSheetPlayer>();
            Assert.IsNotNull(sheet, "drawn from the library");
            Assert.IsTrue(sheet.Has("idle") && sheet.Has("talk") && sheet.Has("watching"), "the gannet's clips");
            Assert.IsNotNull(read.GetComponent<NpcAnimator>());
            var ink = read.GetComponent<NpcInk>();
            Assert.AreEqual(NpcInkState.Remnant, ink.Rest, "a Remnant from the start");
            yield return null;
            yield return null;
            Assert.AreEqual(NpcInkState.Remnant, ink.State);
            Assert.AreEqual(1f, ink.Wash, 1e-3f, "the fills gone to paper");
            Assert.AreEqual(1f, ink.LineFade, 1e-3f, "the line grey");
            Assert.AreEqual("idle", read.GetComponent<NpcAnimator>().Clip, "she idles until spoken to");
            Assert.Less(read.Find("Sprite").localScale.x, 0f, "facing west, the way Wren comes");
        }
    }
}
