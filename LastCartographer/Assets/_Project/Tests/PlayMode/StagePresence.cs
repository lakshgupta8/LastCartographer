using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OWSBG.Core;
using OWSBG.World;

namespace OWSBG.Tests
{
    /// <summary>
    /// Who stands where, read from the built rooms without loading them (the story-timing pass): every talker's start node,
    /// with every FlagPresence on it or above it, and every arena's boss with the flag it waits on. The route replay asks it,
    /// at each step, whether anyone who starts that scene is standing by then, and whether that boss's arena is awake.
    /// </summary>
    public static class StagePresence
    {
        public sealed class Gate { public string Requires, HiddenWhen; public int RequiresValue, HiddenValue; }

        public sealed class Talker
        {
            public string Scene;
            public readonly List<Gate> Gates = new List<Gate>();
            public bool Stands(WorldState w) => Gates.All(g => FlagPresence.ShouldStand(w, g.Requires, g.RequiresValue, g.HiddenWhen, g.HiddenValue));
            public override string ToString() =>
                Scene + (Gates.Count == 0 ? " (always)" : " (" + string.Join("; ", Gates.Select(g =>
                    (string.IsNullOrEmpty(g.Requires) ? "" : "needs " + g.Requires + (g.RequiresValue != 0 ? "=" + g.RequiresValue : "")) +
                    (string.IsNullOrEmpty(g.HiddenWhen) ? "" : " gone at " + g.HiddenWhen + (g.HiddenValue != 0 ? "=" + g.HiddenValue : "")))) + ")");
        }

        static Dictionary<string, List<Talker>> _byNode;
        static Dictionary<string, List<(string scene, string flag)>> _arenas;

        public static IReadOnlyList<Talker> TalkersFor(string node) { Ensure(); return _byNode.TryGetValue(node, out var l) ? l : new List<Talker>(); }
        public static IReadOnlyList<(string scene, string flag)> ArenasFor(string bossId) { Ensure(); return _arenas.TryGetValue(bossId, out var l) ? l : new List<(string, string)>(); }

        static readonly Regex Block = new Regex(@"^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline);

        static string Field(string body, string name)
        {
            var m = Regex.Match(body, "^  " + name + @":[ ]?(.*)$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        static string Ref(string body, string name)
        {
            var m = Regex.Match(body, "^  " + name + @": \{fileID: (-?\d+)\}", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value : null;
        }

        static void Ensure()
        {
            if (_byNode != null) return;
            _byNode = new Dictionary<string, List<Talker>>();
            _arenas = new Dictionary<string, List<(string, string)>>();
            foreach (var file in Directory.GetFiles(Path.GetFullPath("Assets/_Project/Scenes/Greybox"), "Greybox_*.unity"))
            {
                string scene = Path.GetFileNameWithoutExtension(file);
                string text = File.ReadAllText(file).Replace("\r\n", "\n");
                var parentOfTransform = new Dictionary<string, string>();   // transform -> father transform
                var goOfTransform = new Dictionary<string, string>();
                var transformOfGo = new Dictionary<string, string>();
                var talkers = new List<(string go, string node)>();
                var gates = new Dictionary<string, List<Gate>>();          // game object -> its presences
                foreach (Match b in Block.Matches(text))
                {
                    string cls = b.Groups[1].Value, id = b.Groups[2].Value, body = b.Groups[3].Value;
                    if (cls == "4")
                    {
                        var go = Ref(body, "m_GameObject"); var father = Ref(body, "m_Father");
                        if (go != null) { goOfTransform[id] = go; transformOfGo[go] = id; }
                        if (father != null && father != "0") parentOfTransform[id] = father;
                        continue;
                    }
                    if (cls != "114") continue;
                    var owner = Ref(body, "m_GameObject");
                    if (owner == null) continue;
                    var start = Field(body, "_startNode");
                    if (!string.IsNullOrEmpty(start)) talkers.Add((owner, start));
                    var node = Field(body, "_node");
                    if (!string.IsNullOrEmpty(node) && Field(body, "_arena") != null) talkers.Add((owner, node));   // an arena that speaks first
                    if (Field(body, "_hiddenWhen") != null && Field(body, "_requires") != null)
                    {
                        if (!gates.TryGetValue(owner, out var l)) gates[owner] = l = new List<Gate>();
                        int.TryParse(Field(body, "_requiresValue"), out var rv);
                        int.TryParse(Field(body, "_hiddenValue"), out var hv);
                        l.Add(new Gate { Requires = Field(body, "_requires"), RequiresValue = rv, HiddenWhen = Field(body, "_hiddenWhen"), HiddenValue = hv });
                    }
                    var boss = Field(body, "_bossId");
                    if (!string.IsNullOrEmpty(boss) && Field(body, "_requiresFlag") != null)
                    {
                        if (!_arenas.TryGetValue(boss, out var a)) _arenas[boss] = a = new List<(string, string)>();
                        a.Add((scene, Field(body, "_requiresFlag")));
                    }
                }
                foreach (var (go, node) in talkers)
                {
                    var t = new Talker { Scene = scene };
                    // Its own presences and every ancestor's (a group of folk gated together).
                    string cur = go;
                    for (int guard = 0; cur != null && guard < 64; guard++)
                    {
                        if (gates.TryGetValue(cur, out var l)) t.Gates.AddRange(l);
                        cur = transformOfGo.TryGetValue(cur, out var tr) && parentOfTransform.TryGetValue(tr, out var ftr) && goOfTransform.TryGetValue(ftr, out var fgo) ? fgo : null;
                    }
                    if (!_byNode.TryGetValue(node, out var list)) _byNode[node] = list = new List<Talker>();
                    list.Add(t);
                }
            }
        }
    }
}
