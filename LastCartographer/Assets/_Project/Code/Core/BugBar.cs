using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// The bug bar (PRO-07, docs/design/bug-bar.md): what a bug's severity means in this game, the least severity a
    /// symptom can have, its priority once its reach is known, how soon each severity is fixed, and how many of each
    /// a milestone may still have open. The tracker's labels are named here so the issue template, the triage script
    /// and the tests all use the same words. Bugs aren't sized by how hard they are to fix: only by what they do to
    /// a player.
    /// </summary>
    public static class BugBar
    {
        /// <summary>Most severe first.</summary>
        public enum Severity { Blocker, Critical, Major, Minor, Trivial }

        /// <summary>How many players meet it. The first hour (the Edge, the Quay) is Everyone: every player plays it.</summary>
        public enum Reach { Everyone, Most, Some, Few }

        /// <summary>The order the work is done in, across severities.</summary>
        public enum Priority { P0, P1, P2, P3 }

        /// <summary>The plan's sections (docs/02-production-plan.md §3), which is how bugs are owned.</summary>
        public enum Area { NAR, DES, CMB, PRG, CHR, ENV, AUD, PRO }

        /// <summary>The gates the counts are held to.</summary>
        public enum Milestone { Beta, ReleaseCandidate, Release }

        /// <summary>What the player saw. A report says this; the severity follows from it.</summary>
        public enum Symptom
        {
            Unsorted,       // a pressed F12 with no word yet: triage sorts it
            Crash,          // the game closed, or an exception was thrown
            SaveLost,       // a save is gone, or loads into a wrong world
            CantFinish,     // no ending can be reached on that build
            SoftLock,       // she is stuck: a gate that stays shut, a room with no way out, a boss that can't be hurt
            WrongEnding,    // an ending's flag set or missed when it shouldn't be
            WrongState,     // a flag, a place's fate, a commission or a purse wrong, but passable
            OverBudget,     // a frame, a transition or a quiet frame over PerfBudget
            NoRead,         // an attack without its telegraph, or under the tier's floor
            Visual,         // a clip, a pop, a wrong colour
            Text,           // a typo, a wrong line, a line that breaks the style guide, a missing translation
            Audio,          // a sound wrong or missing
            Feel,           // the controller or a hit not feeling right
        }

        public static readonly Severity[] Severities = (Severity[])Enum.GetValues(typeof(Severity));
        public static readonly Area[] Areas = (Area[])Enum.GetValues(typeof(Area));

        /// <summary>What each severity means, in this game's terms.</summary>
        public static string Definition(Severity s) => s switch
        {
            Severity.Blocker => "The game crashes, a save is lost or wrong, or no ending can be reached. Nobody can play past it.",
            Severity.Critical => "Progress stops for some players, or an ending goes wrong: a soft-lock, a gate that stays shut, a boss that can't be hurt.",
            Severity.Major => "Wrong but passable: a flag, a fate, a commission or a purse wrong; a room over budget; an attack with no read.",
            Severity.Minor => "Cosmetic or feel: a clip, a typo, a missing sound, a hit that lands soft.",
            Severity.Trivial => "Nobody would notice without being told.",
            _ => "",
        };

        /// <summary>How soon it is fixed.</summary>
        public static string FixBy(Severity s) => s switch
        {
            Severity.Blocker => "Today, before any other work. The build is pulled.",
            Severity.Critical => "This week.",
            Severity.Major => "Before the milestone ends.",
            Severity.Minor => "Before release, or on the known-issues list.",
            Severity.Trivial => "Backlog. Closed at release candidate if nobody has claimed it.",
            _ => "",
        };

        /// <summary>The least severe a bug with this symptom can be. Triage may raise it, never lower it.</summary>
        public static Severity Floor(Symptom symptom) => symptom switch
        {
            Symptom.Crash => Severity.Blocker,
            Symptom.SaveLost => Severity.Blocker,
            Symptom.CantFinish => Severity.Blocker,
            Symptom.SoftLock => Severity.Critical,
            Symptom.WrongEnding => Severity.Critical,
            Symptom.WrongState => Severity.Major,
            Symptom.OverBudget => Severity.Major,
            Symptom.NoRead => Severity.Major,
            Symptom.Visual => Severity.Minor,
            Symptom.Text => Severity.Minor,
            Symptom.Audio => Severity.Minor,
            Symptom.Feel => Severity.Minor,
            _ => Severity.Trivial,
        };

        /// <summary>A symptom the report can name, or one triage still has to sort.</summary>
        public static bool IsSorted(Symptom symptom) => symptom != Symptom.Unsorted;

        /// <summary>
        /// Severity says how bad; reach says how many. Together they order the work: a Blocker is P0 whoever meets it,
        /// a Critical everyone meets is P0 too; a Major most players meet is P1; a Minor everyone sees is P2.
        /// </summary>
        public static Priority PriorityOf(Severity s, Reach r)
        {
            int rank = (int)s;
            if (r == Reach.Everyone) rank -= 1;
            else if (r == Reach.Most && s == Severity.Major) rank -= 1;
            rank = Math.Max(0, rank);
            return rank switch { 0 => Priority.P0, 1 => Priority.P1, 2 => Priority.P2, _ => Priority.P3 };
        }

        /// <summary>The most a milestone may still have open of each severity, most severe first.</summary>
        public static int[] Bar(Milestone m) => m switch
        {
            Milestone.Beta => new[] { 0, 3, int.MaxValue, int.MaxValue, int.MaxValue },
            Milestone.ReleaseCandidate => new[] { 0, 0, 10, int.MaxValue, int.MaxValue },
            Milestone.Release => new[] { 0, 0, 0, 25, int.MaxValue },
            _ => new[] { 0, 0, 0, 0, 0 },
        };

        public static int MaxOpen(Milestone m, Severity s) => Bar(m)[(int)s];

        /// <summary>
        /// The counts against the milestone's bar: one line per severity over it, none when the milestone may go
        /// ahead. <paramref name="open"/> is indexed by <see cref="Severity"/>.
        /// </summary>
        public static IEnumerable<string> Check(IReadOnlyList<int> open, Milestone m)
        {
            var bar = Bar(m);
            for (int i = 0; i < bar.Length; i++)
            {
                int n = i < open.Count ? open[i] : 0;
                if (n > bar[i]) yield return $"{(Severity)i}: {n} open, at most {bar[i]} for {m}";
            }
        }

        public static bool Meets(IReadOnlyList<int> open, Milestone m)
        {
            foreach (var _ in Check(open, m)) return false;
            return true;
        }

        // ---- the tracker's words ----

        public static string Label(Severity s) => "severity:" + s.ToString().ToLowerInvariant();
        public static string Label(Area a) => "area:" + a.ToString().ToLowerInvariant();

        /// <summary>Where a bug is in its life. A new report is "triage" until it has a severity and an area.</summary>
        public static readonly string[] States = { "triage", "needs-repro", "fixed-needs-verify", "verified", "known-issue", "wontfix" };

        public struct LabelSpec { public string Name, Colour, Description; }

        /// <summary>Every label the tracker uses, with its colour and one line, for the script that makes them.</summary>
        public static IEnumerable<LabelSpec> Labels()
        {
            string[] colours = { "b60205", "d93f0b", "fbca04", "0e8a16", "c5def5" };
            foreach (var s in Severities) yield return new LabelSpec { Name = Label(s), Colour = colours[(int)s], Description = Definition(s) };
            foreach (var a in Areas) yield return new LabelSpec { Name = Label(a), Colour = "5319e7", Description = "Plan section " + a };
            string[] stateLines =
            {
                "New: no severity or area yet",
                "Nobody has made it happen again",
                "Fixed on main, with its test; not yet seen fixed in a build",
                "Seen fixed in a build",
                "Ships as it is; on the known-issues list",
                "Won't be fixed, and why is in the thread",
            };
            for (int i = 0; i < States.Length; i++) yield return new LabelSpec { Name = States[i], Colour = "ededed", Description = stateLines[i] };
        }

        public static bool TryParseSeverity(string label, out Severity s)
        {
            foreach (var x in Severities) if (string.Equals(Label(x), label, StringComparison.OrdinalIgnoreCase)) { s = x; return true; }
            s = Severity.Trivial;
            return false;
        }
    }
}
