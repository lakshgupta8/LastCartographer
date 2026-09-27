using System;

namespace OWSBG.Core
{
    /// <summary>Wren's three registers (bible 2.3, style guide §3). Never labelled in play.</summary>
    public enum Voice { Surveyor, Warden, Drift }

    /// <summary>
    /// The tally of which voice Wren chose (bible 2.3: "they colour NPC replies and decide the last line of the
    /// epilogue"). Each choice a script marks with &lt;&lt;voice kind scope&gt;&gt; adds one to "voice.&lt;kind&gt;" and to
    /// "voice.&lt;scope&gt;.&lt;kind&gt;", so a scene can weigh the whole game or one region (Pell weighs Halden's).
    /// </summary>
    public static class Voices
    {
        public static event Action<Voice, string> Recorded;

        public static string Key(Voice v) => "voice." + v.ToString().ToLowerInvariant();
        public static string Key(Voice v, string scope) => string.IsNullOrEmpty(scope) ? Key(v) : "voice." + scope + "." + v.ToString().ToLowerInvariant();

        public static bool TryParse(string s, out Voice v) => Enum.TryParse((s ?? "").Trim(), true, out v);

        public static void Record(WorldState w, Voice v, string scope = null)
        {
            w.Set(Key(v), w.Get(Key(v)) + 1);
            if (!string.IsNullOrEmpty(scope)) w.Set(Key(v, scope), w.Get(Key(v, scope)) + 1);
            Recorded?.Invoke(v, scope);
        }

        public static int Count(WorldState w, Voice v, string scope = null) => w.Get(Key(v, scope));

        /// <summary>The voice chosen most; ties go Surveyor, then Warden, then Drift (the order a journeyman is taught them).</summary>
        public static Voice Dominant(WorldState w, string scope = null)
        {
            var best = Voice.Surveyor;
            foreach (Voice v in Enum.GetValues(typeof(Voice)))
                if (Count(w, v, scope) > Count(w, best, scope)) best = v;
            return best;
        }
    }
}
