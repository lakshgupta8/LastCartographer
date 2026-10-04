using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Every choice Wren makes is in one of her three voices (bible 2.3, halden-arc.md §4): a script marks each option
    /// with &lt;&lt;voice surveyor|warden|drift scope&gt;&gt; inside the option's body, so the tally Pell weighs and the
    /// epilogue's last line reads the whole game, not one region. The scope is the region the choice is made in.
    /// </summary>
    public class VoiceMarksTests
    {
        static readonly HashSet<string> Scopes = new HashSet<string> { "saltmarrow", "emberdown", "verdance", "halden", "windreach", "greyfold", "blank" };
        static readonly Regex Mark = new Regex(@"<<voice\s+(\w+)\s+(\w+)\s*>>");
        static readonly Regex Jump = new Regex(@"<<jump\s+(\w+)\s*>>");
        /// <summary>Nodes whose choices are not in a voice: the frame's choice is the ending itself, read after the tally, not a vote in it.</summary>
        static readonly HashSet<string> Unvoiced = new HashSet<string> { "Observatory_Frame" };

        /// <summary>Each node's lines, by title, in one file.</summary>
        static Dictionary<string, List<string>> Nodes(string[] lines)
        {
            var nodes = new Dictionary<string, List<string>>();
            string title = null;
            foreach (var l in lines)
            {
                if (l.StartsWith("title:")) { title = l.Substring(6).Trim(); nodes[title] = new List<string>(); }
                else if (l.StartsWith("===")) title = null;
                else if (title != null) nodes[title].Add(l);
            }
            return nodes;
        }

        static string DialogueDir => Path.Combine(Application.dataPath, "_Project/Dialogue");

        static IEnumerable<string> Scripts()
        {
            foreach (var f in Directory.GetFiles(DialogueDir, "*.yarn", SearchOption.AllDirectories))
                if (!f.Replace('\\', '/').Contains("/Localisation/")) yield return f;
        }

        static int Indent(string s) => s.Length - s.TrimStart().Length;

        [Test]
        public void EveryChoiceSpeaksInOneOfHerVoices()
        {
            var failures = new List<string>();
            int choices = 0, marks = 0;
            foreach (var f in Scripts())
            {
                var lines = File.ReadAllLines(f);
                var nodes = Nodes(lines);
                string name = Path.GetFileName(f);
                string node = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("title:")) node = lines[i].Substring(6).Trim();
                    if (!lines[i].TrimStart().StartsWith("->")) continue;
                    if (node != null && Unvoiced.Contains(node)) continue;
                    choices++;
                    int indent = Indent(lines[i]);
                    bool marked = false;
                    var jumps = new List<string>();
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (lines[j].Trim().Length == 0) continue;
                        if (Indent(lines[j]) <= indent) break;
                        var jm = Jump.Match(lines[j]);
                        if (jm.Success) jumps.Add(jm.Groups[1].Value);
                        var m = Mark.Match(lines[j]);
                        if (!m.Success) continue;
                        marked = true;
                        if (!Voices.TryParse(m.Groups[1].Value, out _)) failures.Add(name + ":" + (j + 1) + " voice '" + m.Groups[1].Value + "' is not one of hers");
                        if (!Scopes.Contains(m.Groups[2].Value)) failures.Add(name + ":" + (j + 1) + " scope '" + m.Groups[2].Value + "' is not a region");
                    }
                    // A choice that only jumps is marked where it lands (the Halden scripts' way).
                    if (!marked)
                        foreach (var target in jumps)
                            if (nodes.TryGetValue(target, out var body) && body.Exists(l => Mark.IsMatch(l))) marked = true;
                    if (marked) marks++;
                    else failures.Add(name + ":" + (i + 1) + " " + lines[i].Trim());
                }
            }
            Assert.Greater(choices, 200, "the scripts were read");
            Assert.IsEmpty(failures, failures.Count + " choice(s) in no voice:\n" + string.Join("\n", failures));
            Debug.Log("[OWSBG] voices: " + marks + " choices marked in " + choices);
        }
    }
}
