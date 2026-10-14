using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Salt Chapel's other two rooms (saltmarrow-rooms.md §3): up from behind the altar to the loft, west from the loft
    /// to the vestry and its register of the drowned. Off the road: the chapel still opens east to the Bone Bridge, and the
    /// three rooms are the zone's three.
    /// </summary>
    public class SaltChapelRoomsTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        [Test]
        public void TheChapelClimbsToTheLoftAndTheLoftToTheVestry()
        {
            var chapel = SceneText("Saltmarrow_Chapel");
            Assert.GreaterOrEqual(Count(chapel, "TargetScene: Greybox_Saltmarrow_Chapel_2"), 1, "up from behind the altar");
            Assert.GreaterOrEqual(Count(chapel, "TargetScene: Greybox_Saltmarrow_BoneBridge"), 1, "and the road east is still the chapel's");
            Assert.GreaterOrEqual(Count(chapel, "m_Name: Salt_3"), 1, "the ledges up the east wall");
            var loft = SceneText("Saltmarrow_Chapel_2");
            Assert.GreaterOrEqual(Count(loft, "TargetScene: Greybox_Saltmarrow_Chapel\n"), 1, "the loft drops back behind the altar");
            Assert.GreaterOrEqual(Count(loft, "TargetScene: Greybox_Saltmarrow_Chapel_3"), 1, "and goes on west");
            Assert.GreaterOrEqual(Count(loft, "m_Name: Spawn_Bottom"), 1, "where she comes up");
            Assert.GreaterOrEqual(Count(chapel, "m_Name: Spawn_Top"), 1, "and where she lands, dropping back");
            Assert.GreaterOrEqual(Count(SceneText("Saltmarrow_Chapel_3"), "TargetScene: Greybox_Saltmarrow_Chapel_2"), 1, "the vestry's one door");

            foreach (var id in new[] { "Saltmarrow_Chapel", "Saltmarrow_Chapel_2", "Saltmarrow_Chapel_3" })
            {
                Assert.AreEqual("Saltmarrow.SaltChapel", WorldGraph.ZoneOfPlace(id), id + " is the chapel's");
                Assert.IsNotNull(Atlas.FindPlace(id), id + " is a place in the atlas");
            }
            Assert.AreEqual(Ability.None, Gates.Between("Saltmarrow_Chapel", "Saltmarrow_Chapel_2").Needs, "nothing gates the climb");
            Assert.AreEqual(3, WorldGraph.ZonesOf(Region.Saltmarrow).Single(z => z.Id == "Saltmarrow.SaltChapel").Rooms, "the zone's three, all built");
        }

        [Test]
        public void TheAtlasDrawsTheLoftOverTheChapelAndTheVestryBesideIt()
        {
            var chapel = AtlasMap.Find("Saltmarrow_Chapel");
            var loft = AtlasMap.Find("Saltmarrow_Chapel_2");
            var vestry = AtlasMap.Find("Saltmarrow_Chapel_3");
            Assert.IsNotNull(loft); Assert.IsNotNull(vestry);
            Assert.AreEqual((chapel.X, chapel.Y + 1), (loft.X, loft.Y), "the loft is over the chapel");
            Assert.AreEqual((loft.X - 1, loft.Y), (vestry.X, vestry.Y), "the vestry west of it");
            CollectionAssert.Contains(chapel.Doors, (Side.Up, "Saltmarrow_Chapel_2"));
            CollectionAssert.Contains(chapel.Doors, (Side.East, "Saltmarrow_BoneBridge"));
        }

        [Test]
        public void TheRegisterIsReadInTheVestry()
        {
            var piece = Dressing.Find("chapel.register");
            Assert.IsNotNull(piece);
            Assert.AreEqual("Saltmarrow_Chapel_3", piece!.Room);
            Assert.AreEqual("Chapel_Register", piece.Node);
            Assert.AreEqual("4.1", piece.Plant, "it plants Sable's brother");
            Assert.IsNull(piece.PropAfter, "the register never changes");
            CollectionAssert.Contains(Portraits.Faceless, "Register", "a book speaks with no face");

            var vestry = SceneText("Saltmarrow_Chapel_3");
            Assert.GreaterOrEqual(Count(vestry, "Read_Chapel_Register"), 1, "it can be read");
            Assert.GreaterOrEqual(Count(vestry, "Prop_Log"), 1, "drawn as a book open on its stand");

            var yarn = File.ReadAllText(Path.GetFullPath("Assets/_Project/Dialogue/Saltmarrow/Saltmarrow_Environment.yarn"));
            int at = yarn.IndexOf("title: Chapel_Register", StringComparison.Ordinal);
            Assert.GreaterOrEqual(at, 0);
            string scene = yarn.Substring(at, yarn.IndexOf("\n===", at, StringComparison.Ordinal) - at);
            Assert.IsTrue(scene.Contains("#plant:4.1"), "the plant is tagged");
            Assert.IsFalse(scene.Contains("<<"), "reading it changes nothing");
            foreach (var line in scene.Split('\n').Where(l => l.StartsWith("Register:")))
                Assert.IsTrue(line.Contains("#still"), "every line is heard again unchanged: " + line);
        }
    }
}
