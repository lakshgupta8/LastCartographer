using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The first external test round (PRO-04, docs/design/playtest-round1.md): the vertical slice played cold by people
    /// who did not make it. What they are asked after (<see cref="Questions"/>), what the game keeps while they play
    /// (<see cref="Session"/>, written by <c>PlaytestRun</c> with <c>-playtest</c>), and the bar the round is held to
    /// (<see cref="Gate"/>), read from files by <c>tools/playtest-gate.ps1</c>.
    /// </summary>
    public static class Playtest
    {
        public const string Round = "round1";
        /// <summary>The slice ends when this boss falls (the Lamp-Keeper, 6.1): the round's finish line.</summary>
        public const string FinishBoss = "lamp_keeper";
        /// <summary>A session is asked to last no longer than this; the game says so, and keeps recording.</summary>
        public const float SessionMinutes = 60f;
        /// <summary>The recorder writes the session this often while it is open, so a crash still leaves one.</summary>
        public const float FlushSeconds = 30f;

        public sealed class Question
        {
            public string Id, Text, Low, High;
        }

        static Question Q(string id, string text, string low, string high) => new Question { Id = id, Text = text, Low = low, High = high };

        /// <summary>One to five each, after the session, before anyone talks to the tester about it.</summary>
        public static readonly IReadOnlyList<Question> Questions = new List<Question>
        {
            Q("start", "The opening told me enough to begin.", "I was lost", "I knew what to do"),
            Q("next", "I usually knew where I could go next.", "rarely", "always"),
            Q("atlas", "The atlas helped me find my way.", "never used it / no help", "a great help"),
            Q("control", "Wren went where I meant her to.", "rarely", "always"),
            Q("fair", "When I was hit, I could see why.", "never", "always"),
            Q("boss", "The Lamp-Keeper was hard in a good way.", "unfair or dull", "hard and fair"),
            Q("people", "The people I met were worth talking to.", "skipped them", "wanted more"),
            Q("story", "I wanted to know what happens next.", "not at all", "very much"),
            Q("read", "I could read everything on screen comfortably.", "struggled", "easily"),
            Q("look", "The world looks like a drawing come to life.", "no", "yes"),
            Q("sound", "The sound and music suited it.", "no", "yes"),
            Q("again", "I would play the next hour.", "no", "yes"),
        };

        /// <summary>The open lines, written beside the numbers in the tester's notes (not scored).</summary>
        public static readonly IReadOnlyList<string> OpenLines = new List<string>
        {
            "Where did you get stuck, or not know what to do?",
            "What did you want to do that the game would not let you?",
            "What would you tell a friend this game is?",
            "Anything that looked broken?",
        };

        // ---------------------------------------------------------------- the session the game keeps

        [Serializable]
        public sealed class Session
        {
            public string round = Round, tester = "", version = "", device = "";
            public string started = "";
            /// <summary>Played time, unscaled, in seconds.</summary>
            public float seconds;
            /// <summary>True once the game wrote it on quitting; a session left open was a crash or a killed process.</summary>
            public bool closed;
            /// <summary>The slice's boss beaten, and when.</summary>
            public bool finished;
            public float finishedAt = -1f;
            /// <summary>Every room entered, in the order first entered, with the seconds and the deaths in each.</summary>
            public string[] rooms = new string[0];
            public float[] roomSeconds = new float[0];
            public int[] roomDeaths = new int[0];
            public int deaths;
            /// <summary>Boss fights started and won, by boss id.</summary>
            public string[] bosses = new string[0];
            public int[] bossAttempts = new int[0];
            public bool[] bossWon = new bool[0];
            public int nodesRead, vantagesSurveyed, bugReports, exceptions, optionsChanged;
            /// <summary>The longest stretch with no new room: the measure of being lost.</summary>
            public float longestLost;
            public string longestLostIn = "";

            public string ToJson() => JsonUtility.ToJson(this, true);
            public static Session FromJson(string json) => JsonUtility.FromJson<Session>(json);

            public int AttemptsAt(string boss) { int i = Array.IndexOf(bosses, boss); return i >= 0 ? bossAttempts[i] : 0; }
            public bool Beat(string boss) { int i = Array.IndexOf(bosses, boss); return i >= 0 && bossWon[i]; }
        }

        // ---------------------------------------------------------------- the bar

        public const int MinTesters = 8;
        /// <summary>Of the testers, at least this share reach the Lamp-Keeper's arena (a fight started).</summary>
        public const float ReachShare = 0.7f;
        /// <summary>And at least this share beat her within the session.</summary>
        public const float FinishShare = 0.5f;
        public const float PassMedian = 3.5f;
        public const int FloorMedian = 3;
        /// <summary>"I would play the next hour" is the round's question: its median is held higher.</summary>
        public const string KeyQuestion = "again";
        public const float KeyMedian = 4f;
        /// <summary>The median tester is never lost (no new room) for longer than this, in seconds.</summary>
        public const float LostSeconds = 300f;
        /// <summary>Sessions left open (a crash, or the process killed) allowed across the round.</summary>
        public const int MaxOpenSessions = 1;

        public sealed class Answers
        {
            public string Tester;
            public Dictionary<string, int> Scores = new Dictionary<string, int>();
            public Session Session;
        }

        public sealed class GateReport
        {
            public bool Passed;
            public List<string> Reasons = new List<string>();
            public Dictionary<string, float> Medians = new Dictionary<string, float>();
            public float Reached, Finished, MedianLost;
            public int OpenSessions, Testers;
        }

        public static float Median(IEnumerable<float> values)
        {
            var v = values.OrderBy(x => x).ToList();
            if (v.Count == 0) return float.NaN;
            return v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2f;
        }

        /// <summary>The round's verdict: enough testers, the medians, how far they got, how lost they were, the crashes.</summary>
        public static GateReport Gate(IReadOnlyList<Answers> answers)
        {
            var r = new GateReport { Testers = answers.Count };
            if (answers.Count < MinTesters) r.Reasons.Add(answers.Count + " testers; the round wants " + MinTesters);
            foreach (var q in Questions)
            {
                var scores = answers.Where(a => a.Scores.ContainsKey(q.Id)).Select(a => (float)a.Scores[q.Id]).ToList();
                if (scores.Count == 0) continue;
                float m = Median(scores);
                r.Medians[q.Id] = m;
                if (q.Id == KeyQuestion && m < KeyMedian) r.Reasons.Add("'" + q.Text + "' has a median of " + F(m) + "; the round wants " + F(KeyMedian));
                else if (m < FloorMedian) r.Reasons.Add("'" + q.Text + "' has a median of " + F(m) + ", under the floor of " + FloorMedian);
                else if (m < PassMedian) r.Reasons.Add("'" + q.Text + "' has a median of " + F(m) + "; the round wants " + F(PassMedian));
            }
            var sessions = answers.Where(a => a.Session != null).Select(a => a.Session).ToList();
            int missing = answers.Count - sessions.Count;
            if (missing > 0) r.Reasons.Add(missing + " tester(s) with answers and no session: the recorder was not on, or the file was lost");
            r.OpenSessions = sessions.Count(s => !s.closed);
            if (r.OpenSessions > MaxOpenSessions) r.Reasons.Add(r.OpenSessions + " sessions never closed (a crash or a killed game); the round allows " + MaxOpenSessions);
            if (sessions.Count > 0)
            {
                r.Reached = sessions.Count(s => s.AttemptsAt(FinishBoss) > 0) / (float)sessions.Count;
                r.Finished = sessions.Count(s => s.Beat(FinishBoss)) / (float)sessions.Count;
                r.MedianLost = Median(sessions.Select(s => s.longestLost));
                if (r.Reached < ReachShare) r.Reasons.Add(P(r.Reached) + " reached the Lamp-Keeper; the round wants " + P(ReachShare));
                if (r.Finished < FinishShare) r.Reasons.Add(P(r.Finished) + " beat her; the round wants " + P(FinishShare));
                if (r.MedianLost > LostSeconds) r.Reasons.Add("the median tester went " + F(r.MedianLost / 60f) + " minutes without finding a new room; the round wants under " + F(LostSeconds / 60f));
            }
            r.Passed = r.Reasons.Count == 0;
            return r;
        }

        static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        static string P(float v) => (v * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";

        // ---------------------------------------------------------------- the files

        /// <summary>The answers sheet's header: tester, then every question id in order.</summary>
        public static string Header => "tester," + string.Join(",", Questions.Select(q => q.Id));

        /// <summary>Rows into answers, by the header's columns; a blank is a question not answered.</summary>
        public static List<Answers> ParseAnswers(string csv)
        {
            var list = new List<Answers>();
            if (string.IsNullOrWhiteSpace(csv)) return list;
            var lines = csv.Replace("\r", "").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#")).ToList();
            var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            if (header.Length == 0 || header[0] != "tester") throw new FormatException("the first column is 'tester'");
            var ids = Questions.Select(q => q.Id).ToHashSet();
            for (int c = 1; c < header.Length; c++)
                if (!ids.Contains(header[c])) throw new FormatException("no question called '" + header[c] + "'");
            for (int row = 1; row < lines.Count; row++)
            {
                var cells = lines[row].Split(',');
                var a = new Answers { Tester = cells[0].Trim() };
                if (a.Tester.Length == 0) throw new FormatException("row " + (row + 1) + " has no tester");
                for (int c = 1; c < header.Length && c < cells.Length; c++)
                {
                    var cell = cells[c].Trim();
                    if (cell.Length == 0) continue;
                    if (!int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out int score) || score < 1 || score > 5)
                        throw new FormatException(a.Tester + "'s " + header[c] + " is '" + cell + "', not one to five");
                    a.Scores[header[c]] = score;
                }
                list.Add(a);
            }
            return list;
        }

        /// <summary>Every *.json in the folder that is a round's session, by the tester named inside (else the file's name).</summary>
        public static Dictionary<string, Session> LoadSessions(string dir)
        {
            var sessions = new Dictionary<string, Session>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return sessions;
            foreach (var path in Directory.GetFiles(dir, "*.json").OrderBy(p => p))
            {
                Session s;
                try { s = Session.FromJson(File.ReadAllText(path)); } catch { continue; }
                if (s == null || s.round != Round) continue;
                string key = string.IsNullOrEmpty(s.tester) ? Path.GetFileNameWithoutExtension(path) : s.tester;
                // a tester's later file (a second launch after a crash) adds to the first rather than replacing it
                sessions[key] = sessions.TryGetValue(key, out var earlier) ? Merge(earlier, s) : s;
            }
            return sessions;
        }

        /// <summary>Two sittings of one tester as one: times and counts added, rooms joined, the boss won if either won.</summary>
        public static Session Merge(Session a, Session b)
        {
            var m = Session.FromJson(a.ToJson());
            m.seconds += b.seconds;
            m.closed = a.closed && b.closed;
            if (!m.finished && b.finished) { m.finished = true; m.finishedAt = a.seconds + b.finishedAt; }
            var rooms = m.rooms.ToList(); var secs = m.roomSeconds.ToList(); var dies = m.roomDeaths.ToList();
            for (int i = 0; i < b.rooms.Length; i++)
            {
                int k = rooms.IndexOf(b.rooms[i]);
                if (k < 0) { rooms.Add(b.rooms[i]); secs.Add(b.roomSeconds[i]); dies.Add(b.roomDeaths[i]); }
                else { secs[k] += b.roomSeconds[i]; dies[k] += b.roomDeaths[i]; }
            }
            m.rooms = rooms.ToArray(); m.roomSeconds = secs.ToArray(); m.roomDeaths = dies.ToArray();
            var bosses = m.bosses.ToList(); var tries = m.bossAttempts.ToList(); var won = m.bossWon.ToList();
            for (int i = 0; i < b.bosses.Length; i++)
            {
                int k = bosses.IndexOf(b.bosses[i]);
                if (k < 0) { bosses.Add(b.bosses[i]); tries.Add(b.bossAttempts[i]); won.Add(b.bossWon[i]); }
                else { tries[k] += b.bossAttempts[i]; won[k] |= b.bossWon[i]; }
            }
            m.bosses = bosses.ToArray(); m.bossAttempts = tries.ToArray(); m.bossWon = won.ToArray();
            m.deaths += b.deaths; m.nodesRead += b.nodesRead; m.vantagesSurveyed += b.vantagesSurveyed;
            m.bugReports += b.bugReports; m.exceptions += b.exceptions; m.optionsChanged += b.optionsChanged;
            if (b.longestLost > m.longestLost) { m.longestLost = b.longestLost; m.longestLostIn = b.longestLostIn; }
            return m;
        }

        /// <summary>The report as Markdown: the verdict, why not, each question's median, how far each tester got.</summary>
        public static string Report(GateReport gate, IReadOnlyList<Answers> answers)
        {
            var sb = new StringBuilder();
            sb.Append("# Test round 1: the gate\n\n");
            sb.Append("**").Append(gate.Passed ? "Passed" : "Not yet").Append("** with ").Append(gate.Testers).Append(" testers, ")
              .Append(answers.Count(a => a.Session != null)).Append(" with a session. ")
              .Append(P(gate.Reached)).Append(" reached the Lamp-Keeper, ").Append(P(gate.Finished)).Append(" beat her; the median longest time lost was ")
              .Append(F(gate.MedianLost / 60f)).Append(" minutes.\n\n");
            if (gate.Reasons.Count > 0)
            {
                sb.Append("## Why not\n\n");
                foreach (var reason in gate.Reasons) sb.Append("- ").Append(reason).Append('\n');
                sb.Append('\n');
            }
            sb.Append("## The questions\n\n| Question | Median | Answers |\n|---|---|---|\n");
            foreach (var q in Questions)
            {
                var scores = answers.Where(a => a.Scores.ContainsKey(q.Id)).Select(a => a.Scores[q.Id]).OrderBy(x => x).ToList();
                sb.Append("| ").Append(q.Text).Append(" | ").Append(gate.Medians.TryGetValue(q.Id, out var m) ? F(m) : "—")
                  .Append(" | ").Append(scores.Count > 0 ? string.Join(" ", scores) : "none").Append(" |\n");
            }
            sb.Append("\n## The testers\n\n| Tester | Played (min) | Rooms | Deaths | Lamp-Keeper | Longest lost (min), where | Bug reports | Exceptions | Closed |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (var a in answers)
            {
                var s = a.Session;
                if (s == null) { sb.Append("| ").Append(a.Tester).Append(" | no session | | | | | | | |\n"); continue; }
                string boss = s.Beat(FinishBoss) ? "beat in " + s.AttemptsAt(FinishBoss) : s.AttemptsAt(FinishBoss) > 0 ? s.AttemptsAt(FinishBoss) + " tries" : "not reached";
                sb.Append("| ").Append(a.Tester).Append(" | ").Append(F(s.seconds / 60f)).Append(" | ").Append(s.rooms.Length)
                  .Append(" | ").Append(s.deaths).Append(" | ").Append(boss).Append(" | ").Append(F(s.longestLost / 60f)).Append(", ").Append(s.longestLostIn)
                  .Append(" | ").Append(s.bugReports).Append(" | ").Append(s.exceptions).Append(" | ").Append(s.closed ? "yes" : "**no**").Append(" |\n");
            }
            // Where people died: the rooms that took the most lives across the round.
            var deathsByRoom = new Dictionary<string, int>();
            foreach (var s in answers.Where(a => a.Session != null).Select(a => a.Session))
                for (int i = 0; i < s.rooms.Length; i++)
                    if (s.roomDeaths[i] > 0) deathsByRoom[s.rooms[i]] = (deathsByRoom.TryGetValue(s.rooms[i], out var d) ? d : 0) + s.roomDeaths[i];
            if (deathsByRoom.Count > 0)
            {
                sb.Append("\n## Where they died\n\n| Room | Deaths |\n|---|---|\n");
                foreach (var kv in deathsByRoom.OrderByDescending(kv => kv.Value).Take(10)) sb.Append("| ").Append(kv.Key).Append(" | ").Append(kv.Value).Append(" |\n");
            }
            return sb.ToString();
        }

        /// <summary>The whole run: the answers sheet and the sessions folder in, the report and the verdict out.</summary>
        public static GateReport Run(string answersPath, string sessionsDir, string reportPath)
        {
            var answers = ParseAnswers(File.ReadAllText(answersPath));
            var sessions = LoadSessions(sessionsDir);
            foreach (var a in answers) if (sessions.TryGetValue(a.Tester, out var s)) a.Session = s;
            var gate = Gate(answers);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllText(reportPath, Report(gate, answers));
            return gate;
        }
    }
}
