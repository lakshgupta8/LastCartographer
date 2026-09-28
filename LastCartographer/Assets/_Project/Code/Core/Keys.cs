using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// World-state keys built from ids, each made once (PRG-24). The fade, the held grade, the hour and the schedules ask
    /// for "place.&lt;id&gt;.fate" and its kind every frame; a new string each time was most of what a quiet frame
    /// allocated. The same key is the same string afterwards, and asking for it allocates nothing.
    /// </summary>
    public static class Keys
    {
        static readonly Dictionary<(string, string, string), string> _made = new Dictionary<(string, string, string), string>();

        /// <summary>prefix + id + suffix, made on first use.</summary>
        public static string Of(string prefix, string id, string suffix = "")
        {
            var k = (prefix ?? "", id ?? "", suffix ?? "");
            if (!_made.TryGetValue(k, out var key))
            {
                key = k.Item1 + k.Item2 + k.Item3;
                _made[k] = key;
            }
            return key;
        }

        /// <summary>How many keys have been made (tests).</summary>
        public static int Count => _made.Count;
    }
}
