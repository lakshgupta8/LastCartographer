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
    /// Portraits for dialogue (CHR-13, docs/design/portraits.md): every Yarn speaker has a face or is named faceless,
    /// never both; each face is drawn from the body the speaker is met in; every face is packed as a row per mood of
    /// rest, talk and the two in the Remnant's grey; the moods are different faces, chosen by a line's #face: tag or its
    /// punctuation; two speakers in one look still read apart; the persistent dialogue page carries them all.
    /// </summary>
    public class PortraitTests
    {
        const string Folder = "Assets/_Project/Art/Portraits/";

        [System.Serializable] class Manifest { public int cell; public string[] frames, moods; public Entry[] speakers, looks; }
        [System.Serializable] class Entry { public string speaker, body, file; }

        /// <summary>Every packed sheet: the speakers' and the townsfolk looks'.</summary>
        static IEnumerable<Entry> Sheets() { var m = Load(); return m.speakers.Concat(m.looks ?? new Entry[0]); }

        static Manifest Load()
        {
            var path = Path.GetFullPath(Folder + "portraits.json");
            Assert.IsTrue(File.Exists(path), "the portraits' manifest");
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        }

        /// <summary>Every "Name: line" in the Yarn project, by speaker.</summary>
        static HashSet<string> YarnSpeakers()
        {
            var speakers = new HashSet<string>();
            foreach (var file in Directory.GetFiles(Path.GetFullPath("Assets/_Project/Dialogue"), "*.yarn", SearchOption.AllDirectories))
                foreach (var line in File.ReadAllLines(file))
                {
                    var m = Regex.Match(line, @"^\s*([A-Z][A-Za-z']*):\s");
                    if (m.Success) speakers.Add(m.Groups[1].Value);
                }
            return speakers;
        }

        static readonly Color32 Paper = new Color32(237, 227, 204, 255);   // InkSprite's _PaperColor
        static double ToPaper(Color32 c) => Mathf.Abs(c.r - Paper.r) + Mathf.Abs(c.g - Paper.g) + Mathf.Abs(c.b - Paper.b);

        /// <summary>A pixel of a sheet: the mood's row (counted from the top), the frame's column, x and y in the cell.</summary>
        static Color32 At(Color32[] px, int width, int mood, int frame, int x, int y) =>
            px[((Portraits.Moods.Length - 1 - mood) * Portraits.Cell + y) * width + frame * Portraits.Cell + x];

        static bool Differ(Color32 p, Color32 q) =>
            (p.a > 127) != (q.a > 127) || (p.a > 127 && Mathf.Abs(p.r - q.r) + Mathf.Abs(p.g - q.g) + Mathf.Abs(p.b - q.b) > 60);

        static Texture2D Decode(string assetPath)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(assetPath))), assetPath);
            return tex;
        }

        [Test]
        public void EverySpeakerHasAFaceOrIsFacelessAndNeverBoth()
        {
            var speakers = YarnSpeakers();
            Assert.Greater(speakers.Count, 40, "the project's speakers were read");
            foreach (var s in speakers)
                Assert.IsTrue(Portraits.HasAnyFace(s) ^ Portraits.Faceless.Contains(s), s + " is either a face or faceless");
            foreach (var s in Portraits.Faces.Keys) Assert.IsTrue(speakers.Contains(s), s + " has a face and speaks");
            foreach (var s in Portraits.Faceless) Assert.IsTrue(speakers.Contains(s), s + " is faceless and speaks");
            foreach (var s in Portraits.InTheirBody) Assert.IsTrue(speakers.Contains(s) && !Portraits.Has(s), s + " speaks, in the face of the body they stand in");
            foreach (var s in Portraits.RemnantAtRest) Assert.IsTrue(Portraits.HasAnyFace(s), s + " is a Remnant with a face");
            Assert.IsFalse(Portraits.Has("Marrow"), "Marrow has no portrait (character-bibles.md §5)");
        }

        [Test]
        public void EachFaceIsDrawnFromTheBodyTheSpeakerIsMetIn()
        {
            foreach (var kv in Portraits.Faces)
                Assert.IsTrue(Directory.Exists(Path.GetFullPath("Assets/_Project/Art/Characters/" + kv.Value)), kv.Key + "'s body, " + kv.Value + ", is drawn");
            foreach (var kv in Townsfolk.Named)
                Assert.AreEqual(Townsfolk.Character(kv.Value), Portraits.Faces[kv.Key], kv.Key + " wears the look the rooms dress them in");
            foreach (var name in Townsfolk.OwnDrawn)
                Assert.AreEqual(name, Portraits.Faces[name], name + " is drawn as themselves");
            foreach (var m in Cast.Members)
                if (m.Name != "Marrow" && Portraits.Faces.TryGetValue(m.Name, out var body))
                    Assert.AreEqual(m.Name, body, m.Name + " is drawn from their own sheets");
        }

        [Test]
        public void EveryFaceIsPackedAsARowOfFourFramesPerMood()
        {
            var m = Load();
            Assert.AreEqual(Portraits.Cell, m.cell);
            CollectionAssert.AreEqual(Portraits.Frames, m.frames);
            CollectionAssert.AreEqual(Portraits.Moods, m.moods, "the pack draws the moods the page asks for, in its order");
            CollectionAssert.AreEquivalent(Portraits.Faces.Keys, m.speakers.Select(e => e.speaker), "the pack and the table list the same speakers");
            CollectionAssert.AreEquivalent(Portraits.LookFaces, m.looks.Select(e => e.speaker), "and every townsfolk look has a face");
            foreach (var e in m.looks)
            {
                Assert.AreEqual(e.speaker, e.body, e.speaker + " is drawn as the look stands");
                Assert.AreEqual(Portraits.LookFile(e.speaker), e.file);
            }
            foreach (var e in m.speakers.Concat(m.looks))
            {
                if (Portraits.Faces.ContainsKey(e.speaker))
                {
                    Assert.AreEqual(Portraits.Faces[e.speaker], e.body, e.speaker + "'s body");
                    Assert.AreEqual(Portraits.FileOf(e.speaker), e.file);
                }
                var tex = Decode(Folder + e.file);
                Assert.AreEqual(Portraits.Frames.Length * Portraits.Cell, tex.width, e.speaker + " is four frames across");
                Assert.AreEqual(Portraits.Moods.Length * Portraits.Cell, tex.height, e.speaker + " has a row per mood");
                Object.DestroyImmediate(tex);
                var importer = AssetImporter.GetAtPath(Folder + e.file) as TextureImporter;
                Assert.IsNotNull(importer, e.speaker + "'s importer");
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "the ink line stays a line");
            }
        }

        [Test]
        public void TheBeakOpensAndTheRemnantIsTheSameDrawingGreyed()
        {
            foreach (var e in Sheets())
            {
                var tex = Decode(Folder + e.file);
                var px = tex.GetPixels32();
                int w = tex.width, cell = Portraits.Cell;
                for (int mood = 0; mood < Portraits.Moods.Length; mood++)
                {
                    string who = e.speaker + " (" + Portraits.Moods[mood] + ")";
                    int talkDiffers = 0, shapeDiffers = 0, covered = 0;
                    double toPaperDrawn = 0, toPaperGrey = 0;
                    for (int y = 0; y < cell; y++)
                    for (int x = 0; x < cell; x++)
                    {
                        Color32 rest = At(px, w, mood, Portraits.Rest, x, y), talk = At(px, w, mood, Portraits.Talk, x, y);
                        Color32 grey = At(px, w, mood, Portraits.Rest + Portraits.RemnantOffset, x, y);
                        if (Differ(rest, talk)) talkDiffers++;
                        if ((rest.a > 127) != (grey.a > 127)) shapeDiffers++;
                        if (rest.a <= 127) continue;
                        covered++;
                        toPaperDrawn += ToPaper(rest);
                        toPaperGrey += ToPaper(grey);
                    }
                    Assert.Greater(covered, cell * cell / 10, who + " fills the frame");
                    Assert.Greater(talkDiffers, 40, who + ": the beak opens to talk");
                    Assert.AreEqual(0, shapeDiffers, who + ": the grey is the same drawing");
                    // The ink removed: every fill most of the way to the paper, the line some of the way (InkSprite's ColourState).
                    Assert.Less(toPaperGrey, toPaperDrawn * 0.5, who + ": the grey is nearer the paper");
                }
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void EachMoodIsADifferentFace()
        {
            // A mood moves the head, the beak or the eye enough to be seen at the page's size: every mood's resting
            // face differs from the plain one over a fiftieth of what either covers, for every speaker.
            foreach (var e in Sheets())
            {
                var tex = Decode(Folder + e.file);
                var px = tex.GetPixels32();
                for (int mood = 1; mood < Portraits.Moods.Length; mood++)
                {
                    int either = 0, differ = 0;
                    for (int y = 0; y < Portraits.Cell; y++)
                    for (int x = 0; x < Portraits.Cell; x++)
                    {
                        Color32 p = At(px, tex.width, Portraits.Plain, Portraits.Rest, x, y), q = At(px, tex.width, mood, Portraits.Rest, x, y);
                        if (p.a <= 127 && q.a <= 127) continue;
                        either++;
                        if (Differ(p, q)) differ++;
                    }
                    Assert.Greater(differ / (float)either, 0.02f, e.speaker + " looks " + Portraits.Moods[mood] + " as they look plain");
                }
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void ALineIsSaidWithItsTagOrWhatItsPunctuationSays()
        {
            Assert.AreEqual(Portraits.Grave, Portraits.MoodOf(new[] { "line:abc", "face:grave" }, "Have you eaten?"), "a tag wins");
            Assert.AreEqual(Portraits.Wary, Portraits.MoodOf(new[] { "#face:wary" }, "Quay's shut."), "with or without its #");
            Assert.AreEqual(Portraits.Asking, Portraits.MoodOf(null, "A visitor! Up the causeway, in this. Have you eaten?"), "a question asks");
            Assert.AreEqual(Portraits.Bright, Portraits.MoodOf(new string[0], "Twice! A regular. We'll keep your chair."), "an exclamation is bright");
            Assert.AreEqual(Portraits.Grave, Portraits.MoodOf(null, "The road..."), "a line that trails off");
            Assert.AreEqual(Portraits.Grave, Portraits.MoodOf(null, "We haven't a\u2014"), "or breaks off");
            Assert.AreEqual(Portraits.Plain, Portraits.MoodOf(new[] { "still" }, "Quay's shut. So am I."), "anything else is plain");
            Assert.AreEqual(Portraits.Plain, Portraits.MoodOf(new[] { "face:sulky" }, "Fine?"), "a mood nobody drew is plain");
        }

        [Test]
        public void EveryFaceTagIsAMoodOnALineWithAFace()
        {
            int tagged = 0;
            foreach (var file in Directory.GetFiles(Path.GetFullPath("Assets/_Project/Dialogue"), "*.yarn", SearchOption.AllDirectories))
                foreach (var node in YarnAudit.Parse(Path.GetFileName(file), File.ReadAllText(file)))
                    foreach (var line in node.Lines)
                        foreach (var mood in line.Values("face"))
                        {
                            tagged++;
                            Assert.GreaterOrEqual(Portraits.MoodIndex(mood), 0, line + ": #face:" + mood + " is a drawn mood");
                            Assert.IsFalse(line.IsOption, line + ": Wren's choices have no face to wear it");
                            Assert.IsTrue(Portraits.HasAnyFace(line.Speaker), line + ": said by someone with a face");
                        }
            Assert.Greater(tagged, 0, "the dialogue sets moods");
        }

        [Test]
        public void TheBirdsWhoShareALookStillReadApart()
        {
            // Hask and Brask are choughs; Brek, Lorne and Garrow cranes: each has a touch or a colour of their own
            // (portraits.py TOUCHES, LORNE, GARROW), so any two faces differ over a twentieth of what either covers.
            var shared = Portraits.Faces.GroupBy(kv => kv.Value).Where(g => g.Count() > 1).ToList();
            CollectionAssert.AreEquivalent(new[] { "Folk_Chough", "Folk_Crane" }, shared.Select(g => g.Key));
            foreach (var g in shared)
            {
                var names = g.Select(kv => kv.Key).ToArray();
                for (int i = 0; i < names.Length; i++)
                for (int j = i + 1; j < names.Length; j++)
                {
                    var a = Decode(Folder + Portraits.FileOf(names[i]));
                    var b = Decode(Folder + Portraits.FileOf(names[j]));
                    var pa = a.GetPixels32(); var pb = b.GetPixels32();
                    int either = 0, differ = 0;
                    for (int y = 0; y < Portraits.Cell; y++)
                    for (int x = 0; x < Portraits.Cell; x++)
                    {
                        Color32 p = At(pa, a.width, Portraits.Plain, Portraits.Rest, x, y), q = At(pb, b.width, Portraits.Plain, Portraits.Rest, x, y);
                        if (p.a <= 127 && q.a <= 127) continue;
                        either++;
                        if (Differ(p, q)) differ++;
                    }
                    Object.DestroyImmediate(a); Object.DestroyImmediate(b);
                    Assert.Greater(differ / (float)either, 0.05f, names[i] + " and " + names[j] + " share a face");
                }
            }
        }

        [Test]
        public void ThePersistentPageCarriesEveryPortrait()
        {
            var scene = File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Persistent/Persistent.unity"));
            foreach (var e in Load().speakers)
            {
                StringAssert.Contains("Speaker: " + e.speaker + "\n", scene.Replace("\r\n", "\n"), e.speaker + " is on the page");
                StringAssert.Contains(AssetDatabase.AssetPathToGUID(Folder + e.file), scene, e.speaker + "'s strip is wired");
            }
            // The looks' faces are not on the page: an island loads the one its person stands in, and lets it go with the room.
            foreach (var e in Load().looks)
                StringAssert.DoesNotContain(AssetDatabase.AssetPathToGUID(Folder + e.file), scene, e.speaker + "'s face is loaded with its island, not carried");
            Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/portraits.png")), "the contact sheet");
            Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/portrait-looks.png")), "the looks' sheet");
        }
    }
}
