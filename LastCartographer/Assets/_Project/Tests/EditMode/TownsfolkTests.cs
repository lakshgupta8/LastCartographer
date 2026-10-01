using System;
using System.Collections.Generic;
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
    /// The townsfolk library (CHR-12, docs/design/townsfolk.md): thirty looks, six for each living region, each packed
    /// with the hub's four clips and the crowd's two at the game's density; every look worn somewhere; the minor named
    /// birds dressed from it and no longer tinted; the birds who ask drawn (the Remnant ones grey) and no block left
    /// under any readable; the crowds standing where the brief put them, with their activity and their colour state.
    /// </summary>
    public class TownsfolkTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Characters = "Assets/_Project/Art/Characters/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits; public Clip[] clips; }
        [Serializable] class Clip { public string name, file; public int fps, frames; public bool loop; }

        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static string MatGuid(string name) => AssetDatabase.AssetPathToGUID(Materials + name + ".mat");
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        [Test]
        public void TheLibraryIsSixLooksForEachLivingRegionPackedWithTheSixClips()
        {
            Assert.AreEqual(Townsfolk.PerRegion * Townsfolk.Living.Length, Townsfolk.Looks.Count, "thirty");
            CollectionAssert.AllItemsAreUnique(Townsfolk.Looks.Select(l => l.Id).ToList());
            foreach (var region in Townsfolk.Living) Assert.AreEqual(Townsfolk.PerRegion, Townsfolk.Of(region).Count, region + "'s six");
            Assert.IsTrue(Townsfolk.Looks.All(l => Townsfolk.Living.Contains(l.Region)), "the Greyfold and the Blank have no living townsfolk: theirs are these, grey");
            foreach (var look in Townsfolk.Looks)
            {
                string folder = Characters + look.Character + "/";
                string json = Path.GetFullPath(folder + look.Character.ToLowerInvariant() + ".json");
                Assert.IsTrue(File.Exists(json), look.Id + " is drawn (run tools/characters/townsfolk.py and pack.py)");
                var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(json));
                Assert.AreEqual(look.Character, m.character);
                Assert.AreEqual(96, m.ppu, look.Id + " at 96 px per unit");
                Assert.AreEqual(look.Cell, m.cellUnits, 0.01f, look.Id + "'s cell is the library's");
                Assert.AreEqual(Mathf.RoundToInt(m.cellUnits * 96f), m.cell);
                CollectionAssert.AreEquivalent(Townsfolk.Clips, m.clips.Select(c => c.name).ToList(), look.Id + "'s clips: the hub's four and the crowd's two");
                foreach (var c in m.clips)
                {
                    string path = Path.GetFullPath(folder + c.file);
                    Assert.IsTrue(File.Exists(path), c.file);
                    var (w, h) = PngSize(path);
                    Assert.AreEqual(c.frames * m.cell, w, look.Id + " " + c.name + " is a strip of its frames");
                    Assert.AreEqual(m.cell, h);
                    Assert.Greater(c.frames, 1, c.name + " moves");
                    Assert.AreEqual(12, c.fps, "the drawn rate for people");
                    Assert.IsTrue(c.loop, c.name + " holds while the state does");
                }
                Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/" + look.Character.ToLowerInvariant() + "-turnaround.png")), look.Id + "'s model sheet");
            }
        }

        /// <summary>The idle strip a material rests on, as its character, or null.</summary>
        static string CharacterOf(Material mat)
        {
            var tex = mat != null && mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            if (tex == null) return null;
            var folder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(tex)));
            return folder;
        }

        [Test]
        public void EveryLookIsWornSomewhereAndTheNamedBirdsWearTheirs()
        {
            // A crowd's bird or an asker shares M_Folk_<look>; a named bird keeps its own M_Npc_ material, resting on the look's idle.
            var worn = new HashSet<string>();
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { Materials.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith("M_Folk_") && !file.StartsWith("M_Npc_")) continue;
                var ch = CharacterOf(AssetDatabase.LoadAssetAtPath<Material>(path));
                if (Townsfolk.IsLookCharacter(ch)) worn.Add(ch.Substring(Townsfolk.Prefix.Length));
            }
            foreach (var look in Townsfolk.Looks) Assert.IsTrue(worn.Contains(look.Id), look.Id + " stands somewhere in the greybox");

            foreach (var (name, look) in Townsfolk.Named.Select(kv => (kv.Key, kv.Value)))
            {
                Assert.IsNotNull(Townsfolk.Find(look), name + " wears a look the library has");
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Npc_" + name + "_Greybox.mat");
                Assert.IsNotNull(mat, name + "'s material (built)");
                Assert.AreEqual(Townsfolk.Character(look), CharacterOf(mat), name + " rests on the " + look.ToLowerInvariant() + "'s idle");
                Assert.AreEqual(Color.white, mat.GetColor("_BaseColor"), name + " is no longer a tinted stand-in");
            }
        }

        [Test]
        public void TheNamedBirdsTheArcsGiveASpeciesAreDrawnAsThatBird()
        {
            // Ostry the nightjar, Anvers the heron, Hollin the thrush, Wend the dove: their own sheets, the look's six
            // clips, and the room's NPC resting on their own idle rather than a look's.
            CollectionAssert.AreEquivalent(new[] { "Ostry", "Anvers", "Hollin", "Wend" }, Townsfolk.OwnDrawn);
            foreach (var name in Townsfolk.OwnDrawn)
            {
                Assert.IsFalse(Townsfolk.Named.ContainsKey(name), name + " wears no look");
                var json = Path.GetFullPath("Assets/_Project/Art/Characters/" + name + "/" + name.ToLowerInvariant() + ".json");
                Assert.IsTrue(File.Exists(json), name + "'s sheets");
                var text = File.ReadAllText(json);
                foreach (var clip in Townsfolk.Clips) StringAssert.Contains("\"name\": \"" + clip + "\"", text, name + " " + clip);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Npc_" + name + "_Greybox.mat");
                Assert.IsNotNull(mat, name + "'s material (built)");
                Assert.AreEqual(name, CharacterOf(mat), name + " rests on their own idle");
                Assert.AreEqual(Color.white, mat.GetColor("_BaseColor"), name + " is no longer a tinted stand-in");
            }
        }

        [Test]
        public void TheBirdsWhoAskAreDrawnAndNoBlockIsLeftUnderAnyReadable()
        {
            string ink = Guid("Narrative/NpcInk.cs"), sheets = Guid("World/InkSheetPlayer.cs");
            var birds = Offerings.All.Where(a => a.Kind == AskerKind.Bird).ToList();
            Assert.AreEqual(4, birds.Count, "the gannet, the traveller, Brek and Corvin");
            foreach (var bird in birds)
            {
                var text = SceneText(bird.Room);
                int at = text.IndexOf("m_Name: Read_" + bird.Node, StringComparison.Ordinal);
                Assert.GreaterOrEqual(at, 0, bird.Node + " stands in " + bird.Room);
                Assert.AreEqual(0, Count(text, "m_Name: Marker"), bird.Room + ": no block left");
                if (bird.Look == null)
                {
                    Assert.AreEqual("archivist", bird.Id, "only Corvin has his own drawing beside his asker");
                    Assert.GreaterOrEqual(Count(text, "m_Name: Corvin_Greybox"), 1, "and it stands there");
                    continue;
                }
                Assert.IsNotNull(Townsfolk.Find(bird.Look), bird.Id + " wears a look the library has");
                Assert.GreaterOrEqual(Count(text, MatGuid("M_Folk_" + bird.Look)), 1, bird.Id + " is drawn as the " + bird.Look.ToLowerInvariant());
                Assert.GreaterOrEqual(Count(text, sheets), 1);
                Assert.GreaterOrEqual(Count(text, ink), 1, bird.Id + " carries its colour state");
                if (bird.Remnant) Assert.GreaterOrEqual(Count(text, "_rest: 2"), 1, bird.Id + " rests as a Remnant");
            }
            // Nothing stands in for anything any more: every readable's room is free of the pale and the ochre blocks.
            foreach (var room in Dressing.All.Select(p => p.Room).Concat(Offerings.All.Select(a => a.Room)).Distinct())
                Assert.AreEqual(0, Count(SceneText(room), "m_Name: Marker"), room + ": a block under a readable");
        }

        // room, look, activity (the clip its first word names), how many of that look
        static readonly (string room, string look, string activity, int n)[] Crowds =
        {
            ("Saltmarrow_Stilts", "Eider", "watching the leap", 1),          // the grandmother with bound wings, on the top roost
            ("Emberdown_Rest_1", "Grouse", "watching the leap", 1),          // the counter under the roosts
            ("Windreach_Camp_1", "Lark", "cheering", 1),                     // the clan at the wagon
            ("Windreach_Camp_1", "Plover", "watching the leap", 1),
            ("Windreach_Gate_1", "Hoopoe", "cheering", 1),                   // the clan sings
            ("Halden_Mills_3", "Starling", "cheering", 2),                   // two on the line, one of them on the crate
            ("Halden_Mills_3", "Rook", "watching the line", 1),
            ("Verdance_Aldermere_2", "Jay", "watching the last day", 1),
            ("Halden_Bridges_3", "Goose", "", 2),                            // Arden's family
        };

        [Test]
        public void TheCrowdsStandWhereTheBriefPutsThem()
        {
            string animator = Guid("Narrative/NpcAnimator.cs"), ink = Guid("Narrative/NpcInk.cs");
            foreach (var (room, look, activity, n) in Crowds)
            {
                var text = SceneText(room);
                Assert.AreEqual(n, Regex.Matches(text, @"m_Name: Folk_" + look + @"_\d").Count, room + "'s " + look.ToLowerInvariant());
                Assert.GreaterOrEqual(Count(text, MatGuid("M_Folk_" + look)), n, "drawn as the " + look.ToLowerInvariant());
                if (activity != "") Assert.GreaterOrEqual(Count(text, "_activity: " + activity), 1, room + ": " + activity);
            }
            int everywhere = 0;
            foreach (var path in Directory.GetFiles(Path.GetFullPath(Scenes), "Greybox_*.unity"))
            {
                var text = File.ReadAllText(path);
                int folk = Regex.Matches(text, @"m_Name: Folk_\w+_\d+\b").Count;
                if (folk == 0) continue;
                everywhere += folk;
                Assert.GreaterOrEqual(Count(text, animator), folk, Path.GetFileName(path) + ": every one of the crowd animates");
                Assert.GreaterOrEqual(Count(text, ink), folk, Path.GetFileName(path) + ": and carries its colour state");
            }
            Assert.GreaterOrEqual(everywhere, 30, "a crowd in the greybox");
        }
    }
}
