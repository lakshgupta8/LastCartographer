using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OWSBG.Core
{
    /// <summary>
    /// A reading of the Yarn scripts' shape without running them (NAR-16, <c>docs/story/foreshadowing.md</c>): the nodes,
    /// their lines and tags, the <c>&lt;&lt;if&gt;&gt;</c> branches and option groups, and every path through a node.
    /// It answers the <c>#still</c> question: a line is <b>standing</b> when some path through its visit changes nothing
    /// (no flag, no fade, no grant: only lines, choices and the neutral commands), so the player can take that path
    /// again and hear it again, unchanged. Plain C#, no Unity: the tests read the shipped files with it.
    /// </summary>
    public static class YarnAudit
    {
        /// <summary>Commands that leave nothing behind: a visit that only runs these can be had again.</summary>
        public static readonly HashSet<string> NeutralCommands = new HashSet<string> { "voice", "shop", "tutorial", "survey_hint", "wait" };

        public sealed class YLine
        {
            public string File = "", Node = "", Speaker = "", Text = "";
            public int LineNo;
            public bool IsOption;
            public readonly List<string> Tags = new List<string>();
            /// <summary>The conditions of every <c>&lt;&lt;if&gt;&gt;</c> branch it sits in, outermost first.</summary>
            public readonly List<string> Conditions = new List<string>();
            public bool Has(string tag) => Tags.Any(t => t == tag || t.StartsWith(tag + ":"));
            public IEnumerable<string> Values(string tag) => Tags.Where(t => t.StartsWith(tag + ":")).Select(t => t.Substring(tag.Length + 1));
            public override string ToString() => File + ":" + LineNo + " (" + Node + ") " + (Speaker.Length > 0 ? Speaker + ": " : "") + Text;
        }

        public sealed class YNode
        {
            public string Title = "", File = "";
            public readonly List<YLine> Lines = new List<YLine>();
            public readonly HashSet<string> Jumps = new HashSet<string>();
            /// <summary>Flags the node writes with <c>&lt;&lt;flag&gt;&gt;</c>, and flags its conditions read.</summary>
            public readonly HashSet<string> Writes = new HashSet<string>(), Reads = new HashSet<string>();
            internal List<Stmt> Body = new List<Stmt>();
            /// <summary>The node reads a flag it writes itself: it knows it will be visited again.</summary>
            public bool KnowsRevisits => Reads.Overlaps(Writes);
        }

        // ---- the tree -------------------------------------------------------------------------------------------------

        internal abstract class Stmt { }
        internal sealed class LineStmt : Stmt { public YLine Line; }
        internal sealed class CmdStmt : Stmt { public string Name = "", Flag; }
        internal sealed class JumpStmt : Stmt { public string Target = ""; }
        internal sealed class StopStmt : Stmt { }
        internal sealed class IfStmt : Stmt { public readonly List<(string cond, List<Stmt> body)> Branches = new List<(string, List<Stmt>)>(); public bool HasElse; }
        internal sealed class OptionsStmt : Stmt { public readonly List<(YLine option, List<Stmt> body)> Options = new List<(YLine, List<Stmt>)>(); }

        struct Raw { public int Indent, No; public string Text; }

        static readonly Regex TitleRx = new Regex(@"^title:\s*(\S+)");
        static readonly Regex TagRx = new Regex(@"#([A-Za-z_]+(?::[^\s#]+)?)");
        static readonly Regex FlagReadRx = new Regex(@"(?:has_flag|flag)\(\s*""([^""]+)""");
        static readonly Regex CmdRx = new Regex(@"^<<\s*([A-Za-z_]+)\s*(.*?)>>\s*$");

        /// <summary>Every node of one .yarn file.</summary>
        public static List<YNode> Parse(string file, string text)
        {
            var nodes = new List<YNode>();
            var lines = text.Replace("\r\n", "\n").Split('\n');
            YNode cur = null;
            List<Raw> body = null;
            bool inBody = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (!inBody)
                {
                    var m = TitleRx.Match(l.Trim());
                    if (m.Success) { cur = new YNode { Title = m.Groups[1].Value, File = file }; continue; }
                    if (l.Trim() == "---" && cur != null) { inBody = true; body = new List<Raw>(); }
                    continue;
                }
                if (l.Trim() == "===")
                {
                    int pos = 0;
                    cur.Body = Block(cur, body, ref pos, 0, new List<string>());
                    nodes.Add(cur);
                    cur = null; inBody = false;
                    continue;
                }
                string t = l.TrimEnd();
                string trimmed = t.TrimStart();
                if (trimmed.Length == 0 || trimmed.StartsWith("//")) continue;
                int indent = 0;
                foreach (char c in t) { if (c == ' ') indent++; else if (c == '\t') indent += 4; else break; }
                body.Add(new Raw { Indent = indent, No = i + 1, Text = trimmed });
            }
            return nodes;
        }

        static List<Stmt> Block(YNode node, List<Raw> raw, ref int i, int floor, List<string> conds)
        {
            var list = new List<Stmt>();
            while (i < raw.Count)
            {
                var r = raw[i];
                if (r.Indent < floor) return list;
                var cm = CmdRx.Match(r.Text);
                string cmd = cm.Success ? cm.Groups[1].Value : null;
                if (cmd == "elseif" || cmd == "else" || cmd == "endif") return list;
                if (cmd == "if")
                {
                    var ifs = new IfStmt();
                    string cond = cm.Groups[2].Value.Trim();
                    i++;
                    while (true)
                    {
                        foreach (Match fm in FlagReadRx.Matches(cond)) node.Reads.Add(fm.Groups[1].Value);
                        var inner = new List<string>(conds) { cond };
                        var b = Block(node, raw, ref i, floor, inner);
                        ifs.Branches.Add((cond, b));
                        if (i >= raw.Count) break;
                        var em = CmdRx.Match(raw[i].Text);
                        string k = em.Success ? em.Groups[1].Value : "";
                        if (k == "elseif") { cond = em.Groups[2].Value.Trim(); i++; continue; }
                        if (k == "else") { ifs.HasElse = true; cond = "else"; i++; continue; }
                        if (k == "endif") { i++; break; }
                        break;
                    }
                    list.Add(ifs);
                    continue;
                }
                if (r.Text.StartsWith("->"))
                {
                    var opts = new OptionsStmt();
                    int level = r.Indent;
                    while (i < raw.Count && raw[i].Indent == level && raw[i].Text.StartsWith("->"))
                    {
                        var ol = MakeLine(node, raw[i], raw[i].Text.Substring(2).Trim(), conds, true);
                        i++;
                        var b = Block(node, raw, ref i, level + 1, conds);
                        opts.Options.Add((ol, b));
                    }
                    list.Add(opts);
                    continue;
                }
                if (cmd == "jump") { var tgt = cm.Groups[2].Value.Trim(); node.Jumps.Add(tgt); list.Add(new JumpStmt { Target = tgt }); i++; continue; }
                if (cmd == "stop") { list.Add(new StopStmt()); i++; continue; }
                if (cmd != null)
                {
                    string flag = null;
                    if (cmd == "flag")
                    {
                        var parts = cm.Groups[2].Value.Trim().Split(' ');
                        if (parts.Length > 0 && parts[0].Length > 0) node.Writes.Add(flag = parts[0]);
                    }
                    list.Add(new CmdStmt { Name = cmd, Flag = flag });
                    i++;
                    continue;
                }
                list.Add(new LineStmt { Line = MakeLine(node, r, r.Text, conds, false) });
                i++;
            }
            return list;
        }

        static YLine MakeLine(YNode node, Raw r, string text, List<string> conds, bool option)
        {
            var y = new YLine { File = node.File, Node = node.Title, LineNo = r.No, IsOption = option };
            foreach (Match m in TagRx.Matches(text)) y.Tags.Add(m.Groups[1].Value);
            string body = TagRx.Replace(text, "");
            int cut = body.IndexOf("<<", System.StringComparison.Ordinal);
            if (cut >= 0) body = body.Substring(0, cut);
            body = body.Trim();
            int colon = body.IndexOf(": ", System.StringComparison.Ordinal);
            if (!option && colon > 0 && colon < 24 && !body.Substring(0, colon).Contains(" ")) { y.Speaker = body.Substring(0, colon); body = body.Substring(colon + 2); }
            y.Text = body;
            y.Conditions.AddRange(conds);
            node.Lines.Add(y);
            return y;
        }

        // ---- paths ----------------------------------------------------------------------------------------------------

        /// <summary>One way through a visit: the lines heard, whether it changed anything, and where it went on to.</summary>
        sealed class Path
        {
            public readonly List<YLine> Lines = new List<YLine>();
            public bool Changes, Ended;
            public string JumpTo;
            public Path Copy() { var p = new Path { Changes = Changes, Ended = Ended, JumpTo = JumpTo }; p.Lines.AddRange(Lines); return p; }
        }

        const int PathCap = 20000;

        /// <summary>
        /// A command changes the next visit unless it is neutral, or it sets a flag no script reads: that one is set again
        /// on the next visit to the same value and routes nothing (the Remnant island's <c>blank.remnant.visited</c>).
        /// </summary>
        static bool Changes(CmdStmt c, HashSet<string> read)
            => !NeutralCommands.Contains(c.Name) && !(c.Name == "flag" && c.Flag != null && !read.Contains(c.Flag));

        static List<Path> Walk(List<Stmt> block, List<Path> from, HashSet<string> read)
        {
            var paths = from;
            foreach (var s in block)
            {
                var next = new List<Path>();
                foreach (var p in paths)
                {
                    if (p.Ended) { next.Add(p); continue; }
                    switch (s)
                    {
                        case LineStmt ls: { var q = p.Copy(); q.Lines.Add(ls.Line); next.Add(q); break; }
                        case CmdStmt cs: { var q = p.Copy(); if (Changes(cs, read)) q.Changes = true; next.Add(q); break; }
                        case JumpStmt js: { var q = p.Copy(); q.Ended = true; q.JumpTo = js.Target; next.Add(q); break; }
                        case StopStmt _: { var q = p.Copy(); q.Ended = true; next.Add(q); break; }
                        case IfStmt ifs:
                            foreach (var (_, b) in ifs.Branches) next.AddRange(Walk(b, new List<Path> { p.Copy() }, read));
                            if (!ifs.HasElse) next.Add(p.Copy());
                            break;
                        case OptionsStmt os:
                            foreach (var (o, b) in os.Options) { var q = p.Copy(); q.Lines.Add(o); next.AddRange(Walk(b, new List<Path> { q }, read)); }
                            break;
                    }
                    if (next.Count > PathCap) break;
                }
                paths = next;
            }
            return paths;
        }

        /// <summary>The whole project read at once: nodes by title, and which lines stand.</summary>
        public sealed class Project
        {
            public readonly Dictionary<string, YNode> Nodes = new Dictionary<string, YNode>();
            public IEnumerable<YLine> Lines => Nodes.Values.SelectMany(n => n.Lines);
            readonly Dictionary<string, List<Path>> _paths = new Dictionary<string, List<Path>>();
            readonly Dictionary<string, bool> _standingNode = new Dictionary<string, bool>();
            HashSet<YLine> _standing;
            HashSet<string> _read;

            public void Add(string file, string text) { foreach (var n in Parse(file, text)) Nodes[n.Title] = n; }

            readonly HashSet<string> _codeReads = new HashSet<string>();

            /// <summary>Flags the game's code reads (endings, commissions, islands): setting one routes something too.</summary>
            public void AddCodeReads(IEnumerable<string> flags) { foreach (var f in flags) _codeReads.Add(f); _read = null; _paths.Clear(); _standingNode.Clear(); _standing = null; }

            /// <summary>Every flag some script's condition, or the game's code, reads.</summary>
            public HashSet<string> ReadFlags => _read ??= new HashSet<string>(Nodes.Values.SelectMany(n => n.Reads).Concat(_codeReads));

            /// <summary>Every flag some script writes.</summary>
            public IEnumerable<string> WrittenFlags => Nodes.Values.SelectMany(n => n.Writes).Distinct();

            List<Path> PathsOf(YNode n)
            {
                if (!_paths.TryGetValue(n.Title, out var ps)) _paths[n.Title] = ps = Walk(n.Body, new List<Path> { new Path() }, ReadFlags);
                return ps;
            }

            /// <summary>Some visit to the node, and on through its jumps, changes nothing.</summary>
            public bool HasStandingVisit(string title) => StandingVisit(title, new HashSet<string>());

            bool StandingVisit(string title, HashSet<string> seen)
            {
                if (_standingNode.TryGetValue(title, out var known)) return known;
                if (!Nodes.TryGetValue(title, out var n)) return false;
                if (!seen.Add(title)) return true;   // a loop that changes nothing can be gone round again
                bool any = PathsOf(n).Any(p => Stands(p, seen));
                _standingNode[title] = any;
                return any;
            }

            bool Stands(Path p, HashSet<string> seen) => !p.Changes && (p.JumpTo == null || StandingVisit(p.JumpTo, seen));

            /// <summary>Lines on a visit that changes nothing: heard again, unchanged, on the next visit.</summary>
            public bool IsStanding(YLine line)
            {
                if (_standing == null)
                {
                    _standing = new HashSet<YLine>();
                    foreach (var n in Nodes.Values)
                        foreach (var p in PathsOf(n))
                            if (Stands(p, new HashSet<string>()))
                                foreach (var l in p.Lines) _standing.Add(l);
                }
                return _standing.Contains(line);
            }

            /// <summary>Nodes that expect to be visited again: they read a flag they write, or one of those jumps to them.</summary>
            public HashSet<string> RevisitedNodes()
            {
                var set = new HashSet<string>(Nodes.Values.Where(n => n.KnowsRevisits).Select(n => n.Title));
                var queue = new Queue<string>(set);
                while (queue.Count > 0)
                    if (Nodes.TryGetValue(queue.Dequeue(), out var n))
                        foreach (var j in n.Jumps) if (Nodes.ContainsKey(j) && set.Add(j)) queue.Enqueue(j);
                return set;
            }
        }
    }
}
