using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>One generic look in the townsfolk library (CHR-12): a species of one region, drawn once.</summary>
    public sealed class TownsfolkLook
    {
        public string Id;
        public Region Region;
        public string Species;
        /// <summary>The drawing's cell, in units (a sparrow's 1.6, a goose's 2.6).</summary>
        public float Cell;
        /// <summary>For the designer: what names it, and who in the world wears it.</summary>
        public string Brief;
        /// <summary>The character the sheets are packed under: Art/Characters/Folk_&lt;Id&gt;/.</summary>
        public string Character => Townsfolk.Prefix + Id;
    }

    /// <summary>
    /// The townsfolk library (CHR-12, docs/design/townsfolk.md): thirty generic birds, six for each living region, on
    /// the same parametric bird as the returning cast, drawn by <c>tools/characters/townsfolk.py</c>. A crowd, a
    /// watcher, a picket line, a bird who asks or a minor named bird (<see cref="Named"/>) wears one of these; the
    /// Remnant grey is not a drawing but <c>NpcInk</c>'s state, so the gannet on the faded rail is the gannet, greyed.
    /// Every look has the hub's four clips (idle, talk, walk, asleep) and the crowd's two (watching, cheering).
    /// </summary>
    public static class Townsfolk
    {
        public const string Prefix = "Folk_";
        public const int PerRegion = 6;
        public static readonly string[] Clips = { "idle", "talk", "walk", "asleep", "watching", "cheering" };
        /// <summary>The regions with living townsfolk; the Greyfold's and the Blank's people are these looks as Remnant.</summary>
        public static readonly Region[] Living = { Region.Saltmarrow, Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach };

        static readonly List<TownsfolkLook> _looks = new List<TownsfolkLook>
        {
            // ---- Saltmarrow
            L("Gull", Region.Saltmarrow, "herring gull", 2.0f, "white, a grey back, black wingtips, the yellow bill: the Ferrymen's crews"),
            L("Tern", Region.Saltmarrow, "tern", 2.0f, "white, a black cap, the red bill: the quay's runners"),
            L("Gannet", Region.Saltmarrow, "gannet", 2.0f, "white, a buff head, black wingtips, the dagger bill: the lighthouses' people; the faded light's asker, grey"),
            L("Puffin", Region.Saltmarrow, "puffin", 2.0f, "black over white, the banded bill, orange feet: the stilt-roosts"),
            L("Eider", Region.Saltmarrow, "eider", 2.0f, "low and brown, the broad bill, a shawl, her wings bound by a cord: the grandmother who watches the leap"),
            L("Turnstone", Region.Saltmarrow, "turnstone", 1.6f, "small, patched brown and black, orange legs: a child of the coast"),
            // ---- Emberdown
            L("Chough", Region.Emberdown, "chough", 2.0f, "black, the red bill and legs: the miners"),
            L("Raven", Region.Emberdown, "raven", 2.4f, "big and black, the shaggy throat: a foreman, a smith"),
            L("Ptarmigan", Region.Emberdown, "ptarmigan", 2.0f, "white-grey, feathered feet, the red eyebrow: the high roosts"),
            L("Dipper", Region.Emberdown, "dipper", 2.0f, "brown, the white bib: the cinder-baths' attendant"),
            L("Ouzel", Region.Emberdown, "ring ouzel", 2.0f, "black, a white crescent on the breast: the bell-ringer"),
            L("Grouse", Region.Emberdown, "red grouse", 2.0f, "brown, the red comb: the counters at the roosts; the counting voice under the leap"),
            // ---- the Verdance
            L("Thrush", Region.Verdance, "song thrush", 2.0f, "brown, the speckled breast: the one-night inn's traveller, grey"),
            L("Woodpecker", Region.Verdance, "green woodpecker", 2.0f, "green, the red cap, the stiff tail: the mill's Tobin"),
            L("Finch", Region.Verdance, "chaffinch", 1.6f, "a pink breast, a slate cap, white wing bars: Wend, and the village"),
            L("Jay", Region.Verdance, "jay", 2.0f, "pink-brown, the blue wing patch, a black moustache: Hollin, and Aldermere's villagers"),
            L("Nuthatch", Region.Verdance, "nuthatch", 1.6f, "blue-grey over orange, the eye-stripe: the cloister's brothers; the inn's keeper, grey"),
            L("Owlet", Region.Verdance, "little owl", 1.6f, "round, the facial disc, pale spots, the big eyes: Brother Ansel on his page"),
            // ---- Halden
            L("Pigeon", Region.Halden, "rock pigeon", 2.0f, "blue-grey, the two wing bars, the sheen on the breast: the city's crowds"),
            L("Starling", Region.Halden, "starling", 2.0f, "dark and speckled, the yellow bill: the millworkers; Brisk and the picket"),
            L("Rook", Region.Halden, "rook", 2.0f, "black, the bare grey face: the Guild's clerks; Anvers, Ostry"),
            L("Sparrow", Region.Halden, "house sparrow", 1.6f, "small and brown, the grey crown, a black bib: Tam, and the Hall's journeymen"),
            L("Goose", Region.Halden, "greylag goose", 2.6f, "big, grey-brown, the orange bill: Arden's family on the bridge"),
            L("Magpie", Region.Halden, "magpie", 2.0f, "black and white, the long tail, the white shoulder: the orchard's keeper"),
            // ---- Windreach
            L("Lark", Region.Windreach, "crested lark", 1.6f, "streaked, the crest: the clan's walkers"),
            L("Hoopoe", Region.Windreach, "hoopoe", 2.0f, "cinnamon, the fan crest, barred wings, the long bill: the clan's singer"),
            L("Kestrel", Region.Windreach, "kestrel", 2.0f, "rust and barred, the grey head, the moustache: a scout"),
            L("Bustard", Region.Windreach, "great bustard", 2.6f, "big, tawny and barred, the grey neck: a clan elder"),
            L("Plover", Region.Windreach, "golden plover", 1.6f, "gold speckled with black, the black belly: a child of the clan"),
            L("Crane", Region.Windreach, "young crane", 2.4f, "tall, tawny before the grey, the long neck and legs: Brek at the Gate"),
        };

        /// <summary>The minor named birds (the recipes' NPCs without sheets of their own) and the look each wears.</summary>
        public static readonly IReadOnlyDictionary<string, string> Named = new Dictionary<string, string>
        {
            { "Hask", "Chough" },          // the miner on the furnace stair
            { "Ostry", "Rook" },           // the Guild's agent over the ninth's door
            { "Wend", "Finch" },           // the mill's question, asked on the road
            { "Tobin", "Woodpecker" },
            { "Ansel", "Owlet" },          // page 214
            { "Hollin", "Jay" },           // Aldermere's mayor
            { "Arden", "Goose" },          // the family paid to stand on the bridge
            { "Brisk", "Starling" },       // the strike
            { "Anvers", "Rook" },
            { "Tam", "Sparrow" },          // eleven identical years of notes
            { "Keeper", "Magpie" },        // the orchard's
            { "Innkeeper", "Nuthatch" },   // the one-night inn's, a Remnant
        };

        public static IReadOnlyList<TownsfolkLook> Looks => _looks;
        public static TownsfolkLook Find(string id) => _looks.Find(l => l.Id == id);
        public static List<TownsfolkLook> Of(Region region) => _looks.FindAll(l => l.Region == region);
        /// <summary>The character a look's sheets are packed under ("Gull" → "Folk_Gull").</summary>
        public static string Character(string look) => Prefix + look;
        /// <summary>The look a named bird wears, as a character, or null when it has sheets of its own (or none).</summary>
        public static string CharacterOfNamed(string name) => Named.TryGetValue(name, out var look) ? Character(look) : null;
        public static bool IsLookCharacter(string character) => character != null && character.StartsWith(Prefix) && Find(character.Substring(Prefix.Length)) != null;

        static TownsfolkLook L(string id, Region region, string species, float cell, string brief)
            => new TownsfolkLook { Id = id, Region = region, Species = species, Cell = cell, Brief = brief };
    }
}
