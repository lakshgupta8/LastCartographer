using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace OWSBG.Tests
{
    /// <summary>
    /// The two Warden arenas dressed (boss-kits.md 6.5 and 6.8): the cold furnace's Brann carries the drawn grate for his
    /// six floor sections in all four states, and the Bastion's drill-yard stands its rack, its chalked slate and its
    /// paces, from the plateau's kit.
    /// </summary>
    public class ArenaDressingTests
    {
        static string Scene(string name) => File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Greybox/" + name + ".unity")).Replace("\r\n", "\n");

        [Test]
        public void TheColdFurnaceFloorIsDrawnInEveryState()
        {
            var scene = Scene("Greybox_Emberdown_Stair_3");
            foreach (var clip in new[] { "idle", "warming", "hot", "dark" })
            {
                var path = "Assets/_Project/Art/Characters/FurnaceGrate/FurnaceGrate_" + clip + ".png";
                var guid = AssetDatabase.AssetPathToGUID(path);
                Assert.IsNotEmpty(guid, path + " is drawn");
                StringAssert.Contains(guid, scene, "Brann carries the grate's " + clip);
            }
        }

        [Test]
        public void TheDrillYardStandsItsDrill()
        {
            var scene = Scene("Greybox_Halden_Bastion_2");
            foreach (var prop in new[] { "DrillRack", "ChalkBoard", "Paces" })
            {
                var path = "Assets/_Project/Art/Environment/Halden/Prop_" + prop + ".png";
                Assert.IsTrue(File.Exists(Path.GetFullPath(path)), prop + " is drawn");
                StringAssert.Contains("m_Name: Prop_" + prop + "\n", scene, "the yard stands its " + prop);
                Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/_Project/Art/Materials/M_Prop_" + prop + "_Halden.mat")), prop + " on the kit's ink");
            }
        }
    }
}
