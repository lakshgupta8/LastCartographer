using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The style guide's hard limits, over every Yarn file (dialogue-style-guide.md §2, §5): lines of twenty words or
    /// fewer, choices of eight or fewer, three choices at most, and only the known tag kinds.
    /// </summary>
    public class DialogueStyleTests
    {
        static IEnumerable<(string file, int line, string text)> Lines()
        {
            var dir = Path.Combine(Application.dataPath, "_Project/Dialogue");
            foreach (var f in Directory.GetFiles(dir, "*.yarn", SearchOption.AllDirectories))
            {
                var all = File.ReadAllLines(f);
                for (int i = 0; i < all.Length; i++) yield return (Path.GetFileName(f), i + 1, all[i]);
            }
        }

        static bool Structural(string s) =>
            s.Length == 0 || s.StartsWith("//") || s.StartsWith("<<") || s.StartsWith("title:") || s == "---" || s == "===";

        static string Clean(string s) => Regex.Replace(Regex.Replace(s, @"\s#\S+", ""), "<<.*?>>", "").Trim();

        [Test]
        public void LinesAndChoicesKeepTheirLength()
        {
            var over = new List<string>();
            foreach (var (file, line, raw) in Lines())
            {
                var s = raw.Trim();
                if (Structural(s)) continue;
                s = Clean(s);
                if (s.StartsWith("->"))
                {
                    int w = s.Substring(2).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                    if (w > 8) over.Add(file + ":" + line + " choice of " + w + " words: " + s);
                    continue;
                }
                var m = Regex.Match(s, @"^[A-Za-z][A-Za-z ]*:\s*(.*)$");
                var text = m.Success ? m.Groups[1].Value : s;
                int words = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                if (words > 20) over.Add(file + ":" + line + " line of " + words + " words: " + s);
            }
            CollectionAssert.IsEmpty(over, "style guide §2: twenty words a line, eight a choice");
        }

        [Test]
        public void NoMoreThanThreeChoicesAndOnlyKnownTags()
        {
            var problems = new List<string>();
            string lastFile = null; int run = 0; int indent = -1;
            foreach (var (file, line, raw) in Lines())
            {
                if (file != lastFile) { lastFile = file; run = 0; indent = -1; }
                var s = raw.Trim();
                int ind = raw.Length - raw.TrimStart().Length;
                if (s.StartsWith("->"))
                {
                    if (ind != indent) { run = 0; indent = ind; }
                    if (++run > 3) problems.Add(file + ":" + line + " is a fourth choice");
                }
                else if (s.Length > 0 && !s.StartsWith("//") && ind <= indent) { run = 0; indent = -1; }   // anything at the choices' depth ends the group
                foreach (Match t in Regex.Matches(raw, @"\s#([a-z]+)(:[0-9.a-z]+)?"))
                {
                    var kind = t.Groups[1].Value;
                    if (kind != "still" && kind != "plant" && kind != "reveal" && kind != "callback" && kind != "interject" && kind != "echo" && kind != "line")
                        problems.Add(file + ":" + line + " unknown tag #" + kind);
                    if ((kind == "plant" || kind == "reveal" || kind == "callback") && !Regex.IsMatch(t.Groups[2].Value, @"^:[0-9]+\.[0-9]+$"))
                        problems.Add(file + ":" + line + " #" + kind + " needs a bible section, like #" + kind + ":5.1");
                }
            }
            CollectionAssert.IsEmpty(problems);
        }
    }
}
