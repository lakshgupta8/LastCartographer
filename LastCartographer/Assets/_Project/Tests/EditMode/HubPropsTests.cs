using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The hubs' furniture (ENV-09, docs/design/paper-kit.md §2a): every prop in the kit has its material on the ink
    /// shader, lit like the ground it stands on (the lamp's glow flat and unshadowed), and the rooms stand the right
    /// props: the Quay its desk, ledger, dummy, stall, nets and boat; Merrow's End its stoop, tether-post and bound
    /// stakes; the Lighthouse its lamp and glow; the Chapel its desk; the Tetherline its posts.
    /// </summary>
    public class HubPropsTests
    {
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        static int Uses(string scene, string mat) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity"))
            .Split(new[] { AssetDatabase.AssetPathToGUID(Materials + mat + ".mat") }, System.StringSplitOptions.None).Length - 1;

        [Test]
        public void EveryPropIsInkedAndLitLikeTheGround()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var prop in PaperKitTests.Props)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + prop + ".mat");
                Assert.IsNotNull(mat, prop);
                Assert.AreEqual(ink, mat.shader, prop + " on the ink shader");
                Assert.AreEqual(prop, mat.GetTexture("_BaseMap")?.name, prop + " has its drawing");
                Assert.AreEqual(0f, mat.GetFloat("_WorldUV"), prop + " is a cut-out, not a tile");
                Assert.AreEqual(Color.white, mat.GetColor("_BaseColor"), prop + " keeps its own colours");
                bool glow = prop == "Prop_LampGlow";
                Assert.AreEqual(glow ? 0f : 1f, mat.GetFloat("_Shadows"), prop + (glow ? " casts and takes no shadow" : " takes Wren's shadow"));
                Assert.AreEqual(glow ? 0f : 0.7f, mat.GetFloat("_Lighting"), 0.001f, prop + (glow ? " is light, not lit" : " is lit like the ground"));
            }
        }

        [Test]
        public void TheRoomsStandTheirFurniture()
        {
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Desk"), 1, "the quay's desk");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Ledger"), 1, "the quay's commissions board");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Dummy"), 1, "the dummy");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Stall"), 1, "the Ferrymen's stall: the shop");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Nets"), 1, "Sable's nets");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Boat"), 1, "a boat drawn up");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Vantage"), 1, "the Reedmother's stake");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Prop_Seeds"), 1, "seeds on the high platform");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", "M_Prop_Stoop"), 1, "Dotha's stoop");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", "M_Prop_Tether"), 1, "the tether-post");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", "M_Prop_Bound"), 5, "the walk's five bounds");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse", "M_Prop_Lamp"), 1, "the fourth lamp");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse", "M_Prop_LampGlow"), 1, "and its light");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse", "M_Prop_Desk"), 1, "the lighthouse desk");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Chapel", "M_Prop_Desk"), 1, "the chapel desk");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Tetherline", "M_Prop_Tether"), 4, "the tether-posts of the village");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Shore", "M_Prop_Boat"), 1, "a boat on the shore");
            foreach (var r in new[] { "A", "B", "Lighthouse", "Chapel" })
                Assert.AreEqual(0, Uses("Greybox_Saltmarrow_" + r, "M_Greybox_Desk"), r + " has no greybox desk or ledger post left");
            Assert.AreEqual(0, Uses("Greybox_Saltmarrow_A", "M_Greybox_Dummy"), "no red block");
            Assert.AreEqual(0, Uses("Greybox_Saltmarrow_Lighthouse", "M_Greybox_Glow"), "no glow cube");
        }
    }
}
