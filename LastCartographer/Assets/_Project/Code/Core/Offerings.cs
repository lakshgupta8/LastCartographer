using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>What asks for a memory (bible 10: "Doors, birds, and keystones ask for specific memories").</summary>
    public enum AskerKind { Door, Bird, Keystone }

    /// <summary>Something in the world that will take a memory, and what it gives for it.</summary>
    public sealed class Asker
    {
        public string Id;
        public AskerKind Kind;
        /// <summary>The room it stands in (a built room or a planned one).</summary>
        public string Room;
        /// <summary>The memories it will take. Empty: any memory she carries.</summary>
        public string[] Wants = new string[0];
        /// <summary>The Yarn scene where it asks.</summary>
        public string Node;
        /// <summary>What it gives: a flag set (a door open, a way told), scraps of vellum, iris seeds.</summary>
        public string Opens;
        public int Scraps, Seeds;
        /// <summary>For the designer: what it is, and why it wants that.</summary>
        public string Brief;
        /// <summary>A door's kit drawing (ENV-06): "Prop_&lt;Prop&gt;" shut, "Prop_&lt;Prop&gt;_Open" once <see cref="Opens"/> is set. Null for a bird: a person is a character's.</summary>
        public string Prop;
        public bool Accepts(string memory) => Wants.Length == 0 || Wants.Contains(memory);
    }

    /// <summary>
    /// Memory as currency (bible 10; docs/design/offerings.md): a door, a bird or a keystone asks for a memory, and
    /// Wren may give one she carries. A given memory is gone. It leaves her ink for good, so it can't be dropped at a
    /// death, bound again, or used to anchor its place. <b>Offering one weakens that place's anchor:</b> if the place
    /// the memory came from is anchored, the seal loses its bind and the place fades a stage, anchor or no. A held
    /// place is held by its people, not by ink, and doesn't notice. Each asker takes one memory, once.
    /// </summary>
    public static class Offerings
    {
        public static event Action<string, string> Offered;   // asker, memory

        static readonly List<Asker> _all = new List<Asker>();
        public static IReadOnlyList<Asker> All => _all;
        public static Asker Find(string id) => _all.Find(a => a.Id == id);

        public static string DoneKey(string asker) => Keys.Of("offer.", asker);
        public static string GivenKey(string memory) => Keys.Of("memory.", memory, ".given");
        public static string WeakenedKey(string place) => Keys.Of("place.", place, ".weakened");

        public static bool IsDone(WorldState w, string asker) => w != null && w.Is(DoneKey(asker));
        public static bool IsGiven(WorldState w, string memory) => w != null && w.Is(GivenKey(memory));
        /// <summary>How many of a place's memories were given away after it was anchored: the stages its seal has lost.</summary>
        public static int Weakened(WorldState w, string place) => w == null ? 0 : w.Get(WeakenedKey(place));

        /// <summary>The memory this asker would take from what she carries now, or null. Carried: a dropped memory is in the smudge.</summary>
        public static string Offerable(WorldState w, string asker, string memory)
        {
            var a = Find(asker);
            if (a == null || w == null || IsDone(w, asker) || string.IsNullOrEmpty(memory) || Memories.IsStill(memory)) return null;   // a still memory is only carried
            return w.BoundMemories.Contains(memory) && a.Accepts(memory) ? memory : null;
        }

        public static bool CanOffer(WorldState w, string asker, string memory) => Offerable(w, asker, memory) != null;

        /// <summary>Whether she carries anything this asker would take.</summary>
        public static bool CanOfferAny(WorldState w, string asker) => w != null && w.BoundMemories.Any(m => CanOffer(w, asker, m));

        /// <summary>
        /// Give it. The memory leaves her for good, the asker gives what it gives, and if the memory's place is
        /// anchored its seal weakens a stage. False when the asker is done, or she doesn't carry a memory it takes.
        /// </summary>
        public static bool Offer(WorldState w, string asker, string memory)
        {
            if (!CanOffer(w, asker, memory)) return false;
            var a = Find(asker);
            w.BoundMemories.Remove(memory);
            w.Set(GivenKey(memory), true);
            w.Set(DoneKey(asker), true);
            if (!string.IsNullOrEmpty(a.Opens)) w.Set(a.Opens, true);
            if (a.Scraps > 0) Commissions.AddScraps(w, a.Scraps);
            if (a.Seeds > 0) Economy.AddSeeds(w, a.Seeds);

            var home = Memories.HomeOf(memory);
            if (home != null && Places.FateOf(w, home) == PlaceFate.Anchored)
            {
                w.Set(WeakenedKey(home), Weakened(w, home) + 1);
                FadeStages.Loosen(w, home);
            }
            Offered?.Invoke(asker, memory);
            return true;
        }

        public static void Reset() { _all.Clear(); Defaults(); }

        static Offerings() => Defaults();

        static void Defaults()
        {
            _all.Add(new Asker
            {
                Id = "chapel_door", Kind = AskerKind.Door, Room = "Saltmarrow_Chapel", Node = "Chapel_Door",
                Wants = new[] { "dotha.nine_songs" }, Opens = "saltmarrow.chapel.door_open", Scraps = 2, Prop = "ChapelDoor",
                Brief = "A door of salt-eaten paper behind the altar, cut with one line: \"Sing me in.\" It opens for a song, " +
                        "and Merrow's End's are the only ones on the coast. Behind it, the chapel's reliquary: two scraps of good vellum.",
            });
            _all.Add(new Asker
            {
                Id = "chain_gannet", Kind = AskerKind.Bird, Room = "Saltmarrow_Chain_3", Node = "Chain_Gannet",
                Wants = new[] { "sable.boats_back" }, Seeds = 6,
                Brief = "A grey gannet on the faded third light's rail, the Remnant who watched the boats go out. She asks " +
                        "who comes back. Only Sable counted. She pays in iris seed from her crop.",
            });
            _all.Add(new Asker
            {
                Id = "ninth_door", Kind = AskerKind.Door, Room = "Emberdown_Chimneys_3", Node = "Ninth_Door",
                Wants = new[] { "kettil.count" }, Opens = "emberdown.ninth.door_open", Scraps = 2, Prop = "NinthDoor",
                Brief = "A door at the ninth chimney's top with no builder's name over it and fresh soot on the stone. Nobody " +
                        "remembers cutting it, so it admits only the counted: a count with the bearer in it is its key. Behind it, " +
                        "the builder's satchel: two scraps, and a name rubbed out.",
            });
            _all.Add(new Asker
            {
                Id = "inn_traveller", Kind = AskerKind.Bird, Room = "Verdance_Gate_2", Node = "Inn_Traveller",
                Wants = new[] { "teodor.eleven_names" }, Seeds = 8,
                Brief = "A grey traveller at the one-night inn beyond the Overgrown Gate, from a village the Unwriters let go. " +
                        "She asks which villages still stand; Teodor's eleven, in order, is the answer, and hers not among them " +
                        "is what she wanted to hear. She pays from a seed purse forty years out of date.",
            });
            _all.Add(new Asker
            {
                Id = "gate_brek", Kind = AskerKind.Bird, Room = "Windreach_Gate_1", Node = "Gate_Brek",
                Wants = new[] { "idrenne.standing_place" }, Opens = "windreach.brek.on_the_stone",
                Brief = "Brek, a clan fledgling beside the Gate's carved stones after Wren's leap, not on them. The young stand " +
                        "on a place, not a dare, and he has none: given where Idrenne stood when she learned it, he stands on " +
                        "her fire's stone for the first time. He jumps next spring, he says. The clan sings anyway.",
            });
            // Not a keystone trade: the seventh is given whatever she does. The owl asks for what Isolde saw.
            _all.Add(new Asker
            {
                Id = "archivist", Kind = AskerKind.Bird, Room = "Blank_Capital_4", Node = "Capital_Corvin_Argue",
                Wants = new[] { "isolde.first_sight" }, Opens = "corvin.saw_her",
                Brief = "Corvin, when the argument falls short and Wren carries Isolde's memory of first seeing her. He asks to " +
                        "see what Isolde saw. Given it, he sees his granddaughter at the edge of the white, looking straight at " +
                        "her, and is persuaded. It is the one memory with no home, so it costs only itself: the first thing ever bound, gone.",
            });
        }
    }
}
