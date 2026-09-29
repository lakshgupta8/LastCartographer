using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Bound memories (bible §10: memory as currency; GDD 6: death and retry). A memory is bound in a scene
    /// (the prologue's "the first time she saw you") and carried in <see cref="WorldState.BoundMemories"/>.
    /// Death drops every bound memory where Wren fell, as a smudge; striking the smudge down recovers them.
    /// A second death before the recovery folds the old drop into the new one: the ink remembers, it is only
    /// out of reach.
    /// </summary>
    public static class Memories
    {
        public static event Action<string> Bound;
        public static event Action<string, int> Dropped;     // room, count
        public static event Action<int> Recovered;           // count

        /// <summary>The memories there are words for, by id, in English.</summary>
        public static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
        {
            { "isolde.first_sight", "the first time she saw you" },
            { "dotha.nine_songs", "nine songs, and which came first" },
            { "sable.boats_back", "the count of boats that came back" },
        };

        static readonly Dictionary<string, string> _homes = new Dictionary<string, string>
        {
            { "dotha.nine_songs", "Saltmarrow_B" },   // Merrow's End: Dotha lives there
            { "sable.boats_back", "Saltmarrow_A" },   // the Drowned Quay: Sable's
        };

        /// <summary>
        /// The place a memory belongs to: whoever gave it lives there. Isolde's has none; she lives nowhere now. Binding
        /// a place's memory is the middle of anchoring it (bible 1.3: survey, bind, seal), so the desk asks for one.
        /// </summary>
        public static string HomeOf(string id) => _homes.TryGetValue(id ?? "", out var p) ? p : null;

        /// <summary>A memory given by someone in a place the story adds later (and the tests' rooms).</summary>
        public static void SetHome(string id, string place) { if (place == null) _homes.Remove(id); else _homes[id] = place; }

        /// <summary>A memory she carries that belongs to this place, or null. A dropped memory isn't carried: recover it first.</summary>
        public static string BoundFor(WorldState w, string place)
        {
            if (w == null || string.IsNullOrEmpty(place)) return null;
            foreach (var id in w.BoundMemories) if (HomeOf(id) == place) return id;
            return null;
        }

        /// <summary>A memory's words in the player's language ("memory.&lt;id&gt;"; NAR-18).</summary>
        public static string Name(string id) => English.TryGetValue(id ?? "", out var e) ? Loc.T("memory." + id, e) : id;

        public static bool Has(WorldState w, string id) => w.BoundMemories.Contains(id);
        public static int Count(WorldState w) => w.BoundMemories.Count;

        public static bool Bind(WorldState w, string id)
        {
            if (string.IsNullOrEmpty(id) || w.BoundMemories.Contains(id)) return false;
            w.BoundMemories.Add(id);
            Bound?.Invoke(id);
            return true;
        }

        /// <summary>A drop is waiting somewhere in the world.</summary>
        public static bool HasDrop(WorldState w) => !string.IsNullOrEmpty(w.DropRoom) && w.DroppedMemories.Count > 0;

        /// <summary>
        /// Death: everything bound goes into the drop at this spot, on top of whatever an earlier, unrecovered drop
        /// held. Returns false when there is nothing to drop and no earlier drop to move.
        /// </summary>
        public static bool Drop(WorldState w, string room, float x, float y)
        {
            if (w.BoundMemories.Count == 0 && w.DroppedMemories.Count == 0) return false;
            foreach (var m in w.BoundMemories) if (!w.DroppedMemories.Contains(m)) w.DroppedMemories.Add(m);
            w.BoundMemories.Clear();
            w.DropRoom = room;
            w.DropX = x; w.DropY = y;
            Dropped?.Invoke(room, w.DroppedMemories.Count);
            return true;
        }

        /// <summary>The smudge is struck down: the drop comes back to her.</summary>
        public static int Recover(WorldState w)
        {
            int n = 0;
            foreach (var m in w.DroppedMemories) if (!w.BoundMemories.Contains(m)) { w.BoundMemories.Add(m); n++; }
            w.DroppedMemories.Clear();
            w.DropRoom = null;
            w.DropX = w.DropY = 0f;
            if (n > 0) Recovered?.Invoke(n);
            return n;
        }

        /// <summary>"the first time she saw you and eleven songs" for captions, joined as the player's language joins a list.</summary>
        public static string Describe(IEnumerable<string> ids)
        {
            var names = new List<string>();
            foreach (var id in ids) names.Add(Name(id));
            return Loc.List(names);
        }
    }
}
