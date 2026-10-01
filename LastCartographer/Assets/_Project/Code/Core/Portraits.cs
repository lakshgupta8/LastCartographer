using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Portraits for dialogue (CHR-13, docs/design/portraits.md): every bird who speaks has a face beside their lines,
    /// drawn from the body they are met in (<c>tools/characters/portraits.py</c>), and everyone else who speaks (a
    /// door, the ashes, a page, the Lantern, Marrow) speaks without one. A Yarn speaker is one or the other, never
    /// neither, so a new speaker is a decision someone makes. Pure data; the strips are Art/Portraits/Portrait_&lt;Speaker&gt;.png.
    /// </summary>
    public static class Portraits
    {
        public const string Prefix = "Portrait_";
        /// <summary>The frames of a strip, left to right, each a square of <see cref="Cell"/> px.</summary>
        public static readonly string[] Frames = { "rest", "talk", "rest_remnant", "talk_remnant" };
        public const int Cell = 256;
        public const int Rest = 0, Talk = 1, RemnantOffset = 2;

        /// <summary>Who has a face, and the body it is drawn from: a cast member's own sheets, a Warden, a boss, or a townsfolk look.</summary>
        public static readonly IReadOnlyDictionary<string, string> Faces = new Dictionary<string, string>
        {
            // the returning cast (Marrow has none: character-bibles.md §5)
            { "Sable", "Sable" }, { "Dotha", "Dotha" }, { "Isolde", "Isolde" }, { "Pell", "Pell" }, { "Runa", "Runa" },
            { "Kettil", "Kettil" }, { "Teodor", "Teodor" }, { "Idrenne", "Idrenne" }, { "Maren", "Maren" },
            { "Corvin", "Corvin" }, { "Ilse", "Ilse" }, { "Corra", "Corra" }, { "Aury", "Aury" },
            // the Guild's birds on the Warden rig
            { "Halvard", "Halvard" }, { "Brann", "Brann" }, { "Oriel", "Oriel" }, { "Warden", "Warden" },
            { "Hale", "Hale" }, { "Voss", "Voss" },
            // the minor named birds, in their looks (Townsfolk.Named)
            { "Hask", "Folk_Chough" }, { "Ostry", "Folk_Rook" }, { "Wend", "Folk_Finch" }, { "Tobin", "Folk_Woodpecker" },
            { "Ansel", "Folk_Owlet" }, { "Hollin", "Folk_Jay" }, { "Arden", "Folk_Goose" }, { "Brisk", "Folk_Starling" },
            { "Anvers", "Folk_Rook" }, { "Tam", "Folk_Sparrow" }, { "Keeper", "Folk_Magpie" }, { "Innkeeper", "Folk_Nuthatch" },
            // the birds who ask, and speakers an arc gives a species before the greybox stands them up
            { "Gannet", "Folk_Gannet" }, { "Traveller", "Folk_Thrush" }, { "Brek", "Folk_Crane" },
            { "Ossa", "Folk_Plover" },     // a child of the clan at the third fire
            { "Lorne", "Folk_Crane" },     // the Guild's surveyor at the baths
            { "Brask", "Folk_Chough" },    // the Hollowvein's miner, on his island
        };

        /// <summary>Speakers with no face: things, places, the narrating Lantern, the islands' unnamed, and Marrow.</summary>
        public static readonly HashSet<string> Faceless = new HashSet<string>
        {
            "Ashes", "Atlas", "Beam", "Bedroll", "Board", "Cloth", "Door", "Doorframe", "Drawing", "Fire", "Frame", "Gate",
            "Grass", "Hall", "Hollow", "Inscription", "Lamps", "Lantern", "Lectern", "Ledger", "Lighthouse", "Lintel", "Log",
            "Milepost", "Milestone", "Mine", "Mirror", "Notice", "Pages", "Papers", "Plaque", "Plate", "Pool", "Post",
            "Report", "Riverbed", "Road", "Roll", "Sheet", "Stone", "Stones", "Table", "Tapestry", "Threshold", "Wall",
            "Marrow",       // no portrait, no name plate until named (character-bibles.md §5)
            "Remnant",      // the islands' people, not drawn yet (CHR-12 left them to a Resources pass)
        };

        /// <summary>
        /// Who is a Remnant wherever they speak (NpcInk's rest state, the askers met grey): their portrait is the grey
        /// one unless the bird in the room says otherwise.
        /// </summary>
        public static readonly HashSet<string> RemnantAtRest = new HashSet<string>
        {
            "Ilse", "Corra", "Aury", "Corvin", "Innkeeper", "Gannet", "Traveller", "Brask",
        };

        public static bool Has(string speaker) => speaker != null && Faces.ContainsKey(speaker);
        public static string FileOf(string speaker) => Prefix + speaker + ".png";

        /// <summary>Whether a scene object is that speaker: "Sable", "Sable_Greybox" or "Npc_Sable".</summary>
        public static bool IsSpeaker(string objectName, string speaker)
        {
            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(speaker)) return false;
            string n = objectName.StartsWith("Npc_") ? objectName.Substring(4) : objectName;
            if (n.EndsWith("_Greybox")) n = n.Substring(0, n.Length - "_Greybox".Length);
            return n == speaker;
        }
    }
}
