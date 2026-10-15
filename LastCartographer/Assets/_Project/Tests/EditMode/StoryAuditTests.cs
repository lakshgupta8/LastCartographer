using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OWSBG.Tests
{
    /// <summary>
    /// The story pass's audit, kept: every Yarn scene is started by something (a jump, a talker in a room, an arena that
    /// speaks first, or the code by name), and every flag a scene asks about is written somewhere (a &lt;&lt;flag&gt;&gt;,
    /// or the code, whole or as a key built from a prefix). A scene nothing starts is a scene nobody hears; a flag nothing
    /// writes is a branch nobody takes. Brann's words before his fight were the one the pass found.
    /// </summary>
    public class StoryAuditTests
    {
        static readonly Regex Node = new Regex(@"^title:\s*(\S+)\s*\n(.*?)^===", RegexOptions.Multiline | RegexOptions.Singleline);

        static IEnumerable<string> Files(string dir, string pattern) =>
            Directory.GetFiles(Path.GetFullPath(dir), pattern, SearchOption.AllDirectories);

        static string Read(string f) => File.ReadAllText(f).Replace("\r\n", "\n");

        static Dictionary<string, string> Nodes()
        {
            var nodes = new Dictionary<string, string>();
            foreach (var f in Files("Assets/_Project/Dialogue", "*.yarn"))
                foreach (Match m in Node.Matches(Read(f))) nodes[m.Groups[1].Value] = m.Groups[2].Value;
            return nodes;
        }

        static HashSet<string> CodeLiterals()
        {
            var set = new HashSet<string>();
            foreach (var f in Files("Assets", "*.cs").Where(f => !f.Replace('\\', '/').Contains("/Tests/")))
                foreach (Match m in Regex.Matches(Read(f), "\"([^\"\\\\\\n]{2,120})\"")) set.Add(m.Groups[1].Value);
            return set;
        }

        [Test]
        public void EveryYarnSceneIsStartedBySomething()
        {
            var nodes = Nodes();
            Assert.Greater(nodes.Count, 150, "the project's scenes were read");
            var started = new HashSet<string>();
            foreach (var body in nodes.Values)
                foreach (Match m in Regex.Matches(body, @"<<(?:jump|detour)\s+(\w+)")) started.Add(m.Groups[1].Value);
            foreach (var f in Files("Assets/_Project/Scenes", "*.unity"))
                foreach (Match m in Regex.Matches(Read(f), @"^\s*_(?:startNode|node):\s*(\S+)\s*$", RegexOptions.Multiline)) started.Add(m.Groups[1].Value);
            started.UnionWith(CodeLiterals());

            var unheard = nodes.Keys.Where(n => !started.Contains(n)).OrderBy(n => n).ToList();
            CollectionAssert.IsEmpty(unheard, "scenes nothing starts");
        }

        [Test]
        public void EveryFlagASceneAsksAboutIsWrittenSomewhere()
        {
            var nodes = Nodes();
            var written = new HashSet<string>();
            var asked = new Dictionary<string, string>();
            foreach (var kv in nodes)
            {
                foreach (Match m in Regex.Matches(kv.Value, @"<<flag\s+([\w.]+)")) written.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(kv.Value, @"\b(?:has_flag|flag)\(\s*""([^""]+)""")) asked[m.Groups[1].Value] = kv.Key;
            }
            var code = CodeLiterals();

            bool Written(string flag)
            {
                if (written.Contains(flag) || code.Contains(flag)) return true;
                var parts = flag.Split('.');
                for (int i = 1; i < parts.Length; i++)   // a key the code builds: "boss." + id + ".defeated", "break.noticed." + zone
                    if (code.Contains(string.Join(".", parts.Take(i)) + ".") || code.Contains("." + string.Join(".", parts.Skip(i)))) return true;
                return false;
            }

            var unwritten = asked.Where(kv => !Written(kv.Key)).Select(kv => kv.Key + " (asked in " + kv.Value + ")").OrderBy(s => s).ToList();
            CollectionAssert.IsEmpty(unwritten, "flags a scene asks about that nothing writes");
        }
    }
}
