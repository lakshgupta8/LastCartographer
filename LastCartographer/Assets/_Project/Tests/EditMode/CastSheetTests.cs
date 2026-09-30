using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The returning cast drawn from sheets (CHR-11, docs/design/npc-animation.md): every member has idle, talk, walk
    /// and asleep at the game's density plus its own activities, a model sheet in the docs, the silhouettes keep
    /// their sizes, the placed NPCs' materials rest on their idle strips with no stand-in tint, the rooms carry the
    /// animator and the colour state, and the shader has the wash the states drive.
    /// </summary>
    public class CastSheetTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        static readonly string[] Common = { "idle", "talk", "walk", "asleep" };

        // character, its own activity clips (hub-life posts name them by their first word)
        static readonly (string name, string[] own)[] Members =
        {
            ("Sable", new[] { "mending", "reading" }),
            ("Dotha", new[] { "singing" }),
            ("Isolde", new string[0]),
            ("Pell", new string[0]),
            ("Runa", new string[0]),
            ("Kettil", new string[0]),
            ("Teodor", new string[0]),
            ("Idrenne", new string[0]),
            ("Maren", new string[0]),
            ("Corvin", new string[0]),
            ("Ilse", new string[0]),
            ("Corra", new[] { "drawing" }),
            ("Marrow", new string[0]),
            ("Aury", new string[0]),
        };

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits; public Entry[] clips; }
        [System.Serializable] class Entry { public string name, file; public int fps, frames; public bool loop; }

        static Manifest Load(string name) => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Art + name + "/" + name.ToLowerInvariant() + ".json")));

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        [Test]
        public void EveryMemberHasItsClipsPackedAtTheGamesDensity()
        {
            foreach (var (name, own) in Members)
            {
                var m = Load(name);
                Assert.AreEqual(name, m.character);
                Assert.AreEqual(96, m.ppu, name + " at 96 px per unit");
                Assert.AreEqual(Mathf.RoundToInt(m.cellUnits * 96f), m.cell, name + "'s cell is its units at the density");
                CollectionAssert.IsSubsetOf(Common.Concat(own), m.clips.Select(c => c.name), name + " has idle, talk, walk, asleep and its own");
                foreach (var c in m.clips)
                {
                    var path = Path.GetFullPath(Art + name + "/" + c.file);
                    Assert.IsTrue(File.Exists(path), c.file);
                    var (w, h) = PngSize(path);
                    Assert.AreEqual(c.frames * m.cell, w, name + " " + c.name + " is a strip of its frames");
                    Assert.AreEqual(m.cell, h, name + " " + c.name + " is one frame tall");
                    Assert.Greater(c.frames, 1, name + " " + c.name + " moves");
                    Assert.AreEqual(12, c.fps, name + " " + c.name + " at the drawn rate for people");
                    Assert.IsTrue(c.loop, name + " " + c.name + " holds while the state does");
                }
                Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/" + name.ToLowerInvariant() + "-turnaround.png")), name + "'s model sheet");
            }
        }

        [Test]
        public void TheSilhouettesKeepTheirSizes()
        {
            // Townsfolk are ovals about Wren's height and a little over; the crane and the swan stand tall; the chicks are small.
            foreach (var (name, _) in Members)
                Assert.That(Load(name).cellUnits, Is.InRange(1.2f, 2.6f), name);
            Assert.GreaterOrEqual(Load("Idrenne").cellUnits, 2.4f, "a crane");
            Assert.GreaterOrEqual(Load("Maren").cellUnits, 2.4f, "a swan");
            Assert.LessOrEqual(Load("Corra").cellUnits, 1.4f, "a heron chick");
            Assert.LessOrEqual(Load("Marrow").cellUnits, 1.2f, "a grey chick, smaller than Corra");
            Assert.LessOrEqual(Load("Ilse").cellUnits, 1.6f, "a wren, like her daughter");
            Assert.AreEqual(Load("Sable").cellUnits, Load("Aury").cellUnits, "brother and sister, the same bird");
        }

        [Test]
        public void TheCastDataNamesEveryDrawnMember()
        {
            OWSBG.Core.Cast.Reset(); OWSBG.Core.Cast.EnsureDefaults();
            foreach (var (name, _) in Members)
                Assert.IsNotNull(OWSBG.Core.Cast.Find(name.ToLowerInvariant()), name + " is in the cast data");
        }

        [Test]
        public void ThePlacedTownsfolkRestOnTheirIdleStrips()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var (matName, character) in new[] { ("M_Npc_Sable_Greybox", "Sable"), ("M_Npc_Dotha_Greybox", "Dotha"), ("M_Npc_Isolde", "Isolde") })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + matName + ".mat");
                Assert.IsNotNull(mat, matName);
                Assert.AreEqual(ink, mat.shader, matName);
                Assert.AreEqual(character + "_idle", mat.GetTexture("_BaseMap")?.name, matName + " rests on the idle strip");
                Assert.AreEqual(Color.white, mat.GetColor("_BaseColor"), matName + " has no stand-in tint over the drawing");
            }
            var halvard = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Npc_Halvard.mat");
            Assert.IsNotNull(halvard);
            Assert.AreNotEqual(Color.white, halvard.GetColor("_BaseColor"), "Halvard is a Warden (CHR-07): still the tinted stand-in");

            var animator = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/Narrative/NpcAnimator.cs");
            var state = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/Narrative/NpcInk.cs");
            int Uses(string scene, string guid) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity")).Split(new[] { guid }, System.StringSplitOptions.None).Length - 1;
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", animator), 1, "Sable is drawn");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", animator), 1, "Dotha is drawn");
            Assert.GreaterOrEqual(Uses("Greybox_Greyfold_Edge", animator), 1, "Isolde is drawn");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", state), 1, "Dotha fades with Merrow's End");
        }

        [Test]
        public void TheShaderCarriesTheColourStates()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            Assert.IsNotNull(ink);
            Assert.GreaterOrEqual(ink.FindPropertyIndex("_Wash"), 0, "the fills' wash");
            Assert.GreaterOrEqual(ink.FindPropertyIndex("_LineFade"), 0, "the line's grey");
        }

        [Test]
        public void ColourStatesFollowThePlace()
        {
            Assert.AreEqual(NpcInkState.Drawn, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Unwritten, 0, false), "a standing place, undecided");
            Assert.AreEqual(NpcInkState.Fading, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Unwritten, 2, false), "a place two stages gone");
            Assert.AreEqual(NpcInkState.Fading, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Anchored, 1, false), "anchored where it was: held at that colour");
            Assert.AreEqual(NpcInkState.Drawn, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Held, 0, false), "held: life goes on in colour");
            Assert.AreEqual(NpcInkState.Remnant, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Released, 0, false), "let go: the ink removed");
            Assert.AreEqual(NpcInkState.Remnant, NpcInk.Resolve(NpcInkState.Drawn, PlaceFate.Unwritten, 0, true), "anyone met on an island");
            Assert.AreEqual(NpcInkState.Remnant, NpcInk.Resolve(NpcInkState.Remnant, PlaceFate.Held, 0, false), "a Remnant stays one wherever they stand");

            Assert.AreEqual((0f, 0f), NpcInk.Amounts(NpcInkState.Drawn, 0));
            var (wash, line) = NpcInk.Amounts(NpcInkState.Fading, 2);
            Assert.AreEqual(NpcInk.FadingWashMax * 2f / FadeStages.Max, wash, 0.001f, "the fills wash by stage");
            Assert.AreEqual(0f, line, "the line stays while the place stands");
            Assert.Less(NpcInk.Amounts(NpcInkState.Fading, FadeStages.Max).wash, 1f, "a fading person keeps a little colour");
            Assert.AreEqual((1f, 1f), NpcInk.Amounts(NpcInkState.Remnant, 0), "paper through the fills, the line grey");
        }
    }
}
