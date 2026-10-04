using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.Tests
{
    /// <summary>
    /// How the Blank's islands are drawn (docs/design/blank-generator.md §3): every material an island can ask for is
    /// drawn and addressable at its path; every body is drawn with idle and talk; each authored island is the place it
    /// was, its speaker in the same body as their portrait; a generic island is its place's region; a half-island is
    /// the Blank's own white.
    /// </summary>
    public class IslandLooksTests
    {
        static string Art(string path) => Path.GetFullPath(path);

        [Test]
        public void EveryMaterialAnIslandAsksForIsDrawnAndAddressable()
        {
            var group = File.ReadAllText(Art("Assets/AddressableAssetsData/AssetGroups/Art.asset"));
            foreach (var m in IslandLooks.Materials().Distinct())
            {
                var path = AddressableArt.MaterialPath(m);
                Assert.IsTrue(File.Exists(Art(path)), m + " is drawn");
                StringAssert.Contains("m_Address: " + path, group, m + " can be loaded by its path at runtime");
            }
        }

        [Test]
        public void EveryBodyAnIslandDrawsHasItsSheets()
        {
            foreach (var c in IslandLooks.Characters().Distinct())
                foreach (var clip in new[] { "idle", "talk" })
                    Assert.IsTrue(File.Exists(Art(AddressableArt.SheetPath(c, clip))), c + "'s " + clip);
            foreach (var look in IslandLooks.Authored.Values)
                foreach (var folk in look.Crowd)
                    Assert.IsNotNull(Townsfolk.Find(folk), folk + " is a townsfolk look");
        }

        [Test]
        public void EachAuthoredIslandIsThePlaceItWasAndItsSpeakerTheirPortraitsBody()
        {
            CollectionAssert.AreEquivalent(Islands.Authored.Select(i => i.Id), IslandLooks.Authored.Keys, "a look for every authored island");
            var speakers = new System.Collections.Generic.Dictionary<string, string>
            {
                ["Merrows_End"] = "Dotha", ["Hollowvein"] = "Brask", ["Aldermere"] = "Hollin", ["Overgrown_Inn"] = "Innkeeper", ["Lowmarket"] = "Brisk",
            };
            foreach (var i in Islands.Authored)
            {
                var look = IslandLooks.Authored[i.Id];
                Assert.AreEqual(Portraits.Faces[speakers[i.Id]], look.Speaker, i.Id + ": " + speakers[i.Id] + " stands in the body their portrait is drawn from");
                foreach (var place in i.Places)
                    Assert.AreEqual(look.Region, IslandLooks.RegionOf(place), i.Id + " is drawn from its own region's kit");
                Assert.That(look.Wash, Is.InRange(0.2f, 0.8f), i.Id + " is greyed, but still the place");
            }
        }

        [Test]
        public void AGenericIslandIsItsPlacesRegionAndAHalfIslandTheWhite()
        {
            var baths = new Islands.Drift { Scene = "Island_Emberdown_Baths_2", Name = "The baths", Node = Islands.GenericNode, PlaceId = "Emberdown_Baths_2" };
            var look = IslandLooks.For(baths);
            Assert.AreEqual("Emberdown", look.Region);
            Assert.AreEqual("Mid_Springs", look.Mid, "the baths draw the springs");
            Assert.AreEqual(Region.Emberdown, Townsfolk.Find(look.Speaker.Substring(Townsfolk.Prefix.Length)).Region, "one of the highland's own speaks");
            Assert.AreEqual(look.Speaker, IslandLooks.For(baths).Speaker, "the same island, the same person, every visit");

            var half = new Islands.Drift { Scene = "Island_Half_Halden_Mills_1", Name = "The mills", Node = Islands.HalfNode, PlaceId = "Halden_Mills_1", IsHalf = true };
            var h = IslandLooks.For(half);
            Assert.AreEqual("Blank", h.Region, "the place is still standing somewhere else: only its people drift");
            Assert.AreEqual(0f, h.Wash);
            Assert.AreEqual(Region.Halden, Townsfolk.Find(h.Speaker.Substring(Townsfolk.Prefix.Length)).Region, "and they are the plateau's people");

            Assert.AreEqual("Blank", IslandLooks.RegionOf("Nowhere"), "a place no kit draws is the Blank's");
        }
    }
}
