using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The fledglings drawn and stood (CHR-14, docs/design/fledglings.md): every region's look has its sheets with the
    /// five clips at a chick's size, every loop stands in the room the catalog names with six birds on its sheets, and
    /// nowhere else.
    /// </summary>
    public class FledglingLoopTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Characters = "Assets/_Project/Art/Characters/";
        static readonly string[] Clips = { "idle", "leap", "glide", "drop", "land" };

        [Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits; public Clip[] clips; }
        [Serializable] class Clip { public string name, file; public int fps, frames; public bool loop; }

        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        [Test]
        public void EveryLookHasItsSheetsWithTheFiveClips()
        {
            var looks = Dressing.Loops.Select(l => l.Look).ToList();
            CollectionAssert.AllItemsAreUnique(looks, "one species per region");
            CollectionAssert.AreEquivalent(new[] { "Gull", "Grouse", "Dove", "Pigeon", "Crane", "Outline", "Grey" }, looks);
            foreach (var loop in Dressing.Loops)
            {
                string folder = Characters + "Fledgling_" + loop.Look + "/";
                string json = Path.GetFullPath(folder + "fledgling_" + loop.Look.ToLowerInvariant() + ".json");
                Assert.IsTrue(File.Exists(json), loop.Region + "'s chick is drawn (run tools/characters/fledglings.py and pack.py)");
                var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(json));
                Assert.AreEqual("Fledgling_" + loop.Look, m.character);
                Assert.AreEqual(96, m.ppu);
                Assert.LessOrEqual(m.cellUnits, 1.5f, "a chick's cell: smaller than a townsfolk's two");
                CollectionAssert.AreEquivalent(Clips, m.clips.Select(c => c.name).ToList(), loop.Look + "'s clips");
                foreach (var c in m.clips)
                {
                    Assert.IsTrue(File.Exists(Path.GetFullPath(folder + c.file)), c.file);
                    Assert.AreEqual(c.name == "leap" || c.name == "land" ? false : true, c.loop, c.name + (c.loop ? " loops" : " plays once"));
                    Assert.GreaterOrEqual(c.frames, 4);
                }
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Fledgling_" + loop.Look + ".mat"), loop.Look + "'s material (built)");
            }
        }

        [Test]
        public void EveryLoopStandsInItsRoomWithSixBirdsOnTheirSheets()
        {
            string loopGuid = Guid("World/FledglingLoop.cs");
            string sheets = Guid("World/InkSheetPlayer.cs");
            foreach (var loop in Dressing.Loops)
            {
                var text = SceneText(loop.Room);
                Assert.AreEqual(1, Count(text, loopGuid), loop.Region + "'s loop in " + loop.Room);
                Assert.AreEqual(1, Count(text, "m_Name: Fledglings"), "one perch");
                Assert.AreEqual(Dressing.Leapers, Regex.Matches(text, @"m_Name: Fledgling_\d").Count, "six on it: one for each piece of the sky");
                Assert.GreaterOrEqual(Count(text, sheets), Dressing.Leapers, "every bird on its sheets");
                Assert.GreaterOrEqual(Count(text, "_placeId: " + loop.Room), 1, "the loop reads its own place");
                Assert.GreaterOrEqual(Count(text, AssetDatabase.AssetPathToGUID("Assets/_Project/Art/Materials/M_Fledgling_" + loop.Look + ".mat")), Dressing.Leapers, "drawn as the " + loop.Look.ToLowerInvariant());
            }
            int everywhere = Directory.GetFiles(Path.GetFullPath(Scenes), "Greybox_*.unity").Sum(p => Count(File.ReadAllText(p), loopGuid));
            Assert.AreEqual(Dressing.Loops.Length, everywhere, "one loop per region and no more");
        }
    }
}
