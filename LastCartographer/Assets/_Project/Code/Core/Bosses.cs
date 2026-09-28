using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>One boss fight as the writing sees it (NAR-06): reason, place, tier, the three lines, and the aftermath.</summary>
    public sealed class BossSheet
    {
        /// <summary>Flag id: the fight sets "boss.&lt;Id&gt;.defeated".</summary>
        public string Id;
        /// <summary>Bible number, "6.1" to "6.15". Halvard's three fights share "6.3".</summary>
        public string Number;
        public string Name;
        public string Species;
        /// <summary>A <see cref="WorldGraph"/> zone id whose Boss note names this number.</summary>
        public string Zone;
        /// <summary>1 early, 2 mid, 3 late, 4 endgame.</summary>
        public int Tier;
        public bool Optional;
        public string Reason;
        public string Arena;
        /// <summary>On entry, at the turn, and as the last phase begins: the boss's phase lines, each under twelve words.</summary>
        public string Entry, Turn, Last;
        public string Aftermath;
        public Ability Grants;
        /// <summary>A Charter the fight hands over (the Choir's Unwriter's Charter), or null.</summary>
        public CharterKind? Charter;
        public bool Keystone;
        public int Scraps;

        public string FlagKey => "boss." + Id + ".defeated";
        public string[] Lines => new[] { Entry, Turn, Last };
    }

    /// <summary>
    /// The boss sheets as data (`docs/story/boss-sheets.md`): fifteen bosses, seventeen fights. The staged Lamp-Keeper
    /// takes her name, tier and lines from here when the room is built; the tests hold the sheets to the map and the bible.
    /// </summary>
    public static class Bosses
    {
        public const int MaxLineWords = 12;

        static readonly List<BossSheet> _all = new List<BossSheet>();
        static bool _built;

        public static IReadOnlyList<BossSheet> All { get { EnsureDefaults(); return _all; } }
        public static BossSheet Find(string id) { EnsureDefaults(); return _all.Find(b => b.Id == id); }
        public static List<BossSheet> ByNumber(string number) { EnsureDefaults(); return _all.FindAll(b => b.Number == number); }
        public static IEnumerable<string> Numbers => All.Select(b => b.Number).Distinct();
        public static string FlagKey(string id) => "boss." + id + ".defeated";
        public static bool IsDefeated(WorldState w, string id) => w.Is(FlagKey(id));

        /// <summary>Words in a line, for the twelve-word rule.</summary>
        public static int WordCount(string line) => string.IsNullOrWhiteSpace(line) ? 0 : line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;

        public static void Reset() { _all.Clear(); _built = false; }

        static void Sheet(string id, string number, string name, string species, string zone, int tier, string reason, string arena,
                          string entry, string turn, string last, string aftermath,
                          bool optional = false, Ability grants = Ability.None, bool keystone = false, int scraps = 1, CharterKind? charter = null)
        {
            _all.Add(new BossSheet
            {
                Id = id, Number = number, Name = name, Species = species, Zone = zone, Tier = tier, Optional = optional,
                Reason = reason, Arena = arena, Entry = entry, Turn = turn, Last = last, Aftermath = aftermath,
                Grants = grants, Charter = charter, Keystone = keystone, Scraps = scraps,
            });
        }

        public static void EnsureDefaults()
        {
            if (_built) return;
            _built = true;

            Sheet("lamp_keeper", "6.1", "The Lamp-Keeper", "a Remnant gannet fused to the fourth lighthouse's lamp", "Saltmarrow.LanternChain", 1,
                "she will not let the light go out; it is the last thing she remembers",
                "the lamp room: a sweeping beam she dives through; the floor outside the beam dims; the lamp gutters and she dives from the perch",
                "The light stays.", "I remember the light. I remember nothing else.", "If it goes out, I go with it.",
                "Wingbeat; the lamp lights and becomes a beacon; Halvard walks in", grants: Ability.Wingbeat);

            Sheet("reedmother_brood", "6.2", "Reedmother's Brood", "a swarm of half-drawn marsh chicks around a giant reed-nest", "Saltmarrow.IrisFields", 1,
                "the Guild's agents have set the iris field alight and the Brood defends the beds, mad with smoke",
                "the nest in the burning beds: waves of chicks; the nest opens to call them; the fire reaches it and the player chooses what to strike",
                "Ours. Ours. Ours.", "Burning. Burning. Ours.", "...ours?",
                "kill the nest and the field burns (Ferrymen prices rise by half); stamp the fire and the beds stand; the Tether-hook", optional: true);

            Sheet("halvard", "6.3", "Warden-Sergeant Halvard", "a Guild heron: brass gorget, sighting-lance", "Saltmarrow.SaltChapel", 1,
                "he hunts the unlicensed cartographer; the first fight, on the chapel's salt floor",
                "his marks stay on the salt and erupt when he calls the count; by the last phase the floor is marked but one pace",
                "Three paces. I measured them.", "You were taught by the best. So was I.", "Noted. Halden will hear the count.",
                "he withdraws; she stays unlicensed and the Wardens in anchored towns know her");
            Sheet("halvard_2", "6.3", "Warden-Sergeant Halvard", "a Guild heron, now with a second lance on a cord", "Halden.SevenBridges", 2,
                "the second hunt: new kit, and a bridge he cuts section by section",
                "a bridge over the drop; sections fall as he marks them; the last phase is fought on the last span",
                "Four paces, now. I said I'd measured you.", "The bridge is Guild property. So is the fall.", "Entered. Twice. There is no third book.",
                "he leaves the span standing; if Pell's report was sent, Oriel takes over the hunt", scraps: 2);
            Sheet("halvard_3", "6.3", "Warden-Sergeant Halvard", "a Guild heron with everything he has, beside Voss", "Greyfold.Threshold", 3,
                "the third: before Voss's phase at the white edge; halfway through he stops counting",
                "the Threshold's edge, the Blank eating the floor from the west",
                "The count ends here, journeyman.", "He's going in. I am not. Understand me.", "Go. I'll enter it as a survey.",
                "he holds the Wardens back while she crosses; 'survey', not 'count', is the change", scraps: 3);

            Sheet("collapse", "6.4", "The Collapse", "a smudge-beast: the mine collapse itself, ink, rubble and lantern-light", "Emberdown.Hollowvein", 2,
                "bounds-walking into Hollowvein wakes what buried it; the fight is the walk's last verse",
                "the bottom of the mine; the walk's beat continues and only the section a chorus lamp lights can be struck; it reaches for the lamps",
                "Thirty. Thirty. Thirty.", "Who counts the ones under?", "Thirty-one. Say it. Say it.",
                "the families recover the dead; Runa says the number; the second keystone", keystone: true, scraps: 2);

            Sheet("brann", "6.5", "Cinder Warden Brann", "a Guild crane in furnace-blackened brass, twin lances", "Emberdown.FurnaceStair", 2,
                "sent to anchor Kettil's Rest by force, with the survey already written; he believes it a kindness",
                "a cooling furnace: stand on the cool sections; both lances as the floor halves; dark, and only his brass glows",
                "Kettil's Rest is scheduled. I am the schedule.", "You'd let them fade to spite the Guild?", "Cold. It's cold. Tell her it's cold.",
                "Kettil's Rest stays free; Kettil counts him out at the Bell", scraps: 3);

            Sheet("choir", "6.6", "The Choir", "three Cantor doves in unison; bells that erase the arena and the map", "Verdance.Aldermere", 2,
                "only if Wren tries to stop Aldermere's last day; the village asked to be let go",
                "the square on its last evening; each bell erases a platform and a vantage; three doves, then two in canon, then one alone",
                "They asked. Let them go.", "Which of us are you saving them from?", "Then it's held. Ask them if they're glad.",
                "Aldermere is held against its wish; Teodor will not give the keystone; the Unwriter's Charter", optional: true, charter: CharterKind.Unwriter);

            Sheet("gatekeeper", "6.7", "The Gatekeeper", "a flying-age statue of a great eagle, roots for wings", "Verdance.OvergrownGate", 2,
                "it guards the canopy road to the Plateau and admits only the winged",
                "the gate; the roots are thread anchors; wing sweeps, then vertical on the threads, then it flies, badly, for the first time since the Grounding",
                "Passage is for the winged.", "You climb. We flew. Which is stranger?", "Ah. That is how it went. I had forgotten.",
                "the road to Halden opens; the fledglings glide one span farther", scraps: 2);

            Sheet("oriel", "6.8", "Warden-Captain Oriel", "a Guild egret, the Watch's best; mirrors Wren's kit", "Halden.Bastion", 3,
                "Interlude A, if Pell's report is sent; she has read it and fights to see if it is accurate",
                "the Bastion's drill-yard: Wren's combo reversed, then Wren's Flourish, then she Binds once and the fight is about denying it",
                "Pell writes well. Show me the rest.", "That's my stance. Where did you learn it?", "Accurate. All of it. Go before I read it twice.",
                "sets the Guild's stance for Act 3; beaten without a mask lost she stands the Wardens down", scraps: 3);

            Sheet("hale", "6.9", "Surveyor Hale", "a Guild surveyor with a quill of his own; the only duel", "Windreach.NineStones", 3,
                "he is finishing a secret survey of the Steppe; nine stones and the Guild can anchor Windreach",
                "the Nine Stones at dusk; each stone he sights becomes his; survey a stone first to deny it; phases at five and eight stones",
                "Journeyman. Professional courtesy: I'll finish first.", "Eight stones. One more and it's theirs.", "Nine. No. Tear it. Tear it, I can't.",
                "Windreach stays unanchored; burn or keep his pages; Hale's lens", optional: true, scraps: 2);

            Sheet("fallen_star", "6.10", "The Fallen Star", "an iron meteorite-golem, woken when the keystone is lifted", "Windreach.FallenStar", 3,
                "Idrenne's laughing 'no cost' has one cost after all",
                "the anvil-crater, magnetic; it slams, draws iron walls up, then burns and the heat makes updrafts",
                "There it is. The cost. Mind the iron.", "Forty years our anvil. It's owed a swing.", "Ha. Told you. No cost. Well. One.",
                "Idrenne still laughs; the keystone; the anvil is cold", optional: true, keystone: true, scraps: 2);

            Sheet("voss", "6.11", "Guildmaster Aurelian Voss", "a great grey heron in full Guild brass; sighting-lance, compass-rose shield", "Greyfold.Threshold", 3,
                "he will not let Wren cross; he has never crossed himself and cannot say why",
                "the Threshold: lance and shield; then he anchors the arena, freezing sections; then the Blank eats the frozen sections and he holds a shrinking island",
                "Journeyman Halloway. This is as far as the map goes.", "Hold. Everything holds. Do you see? Nothing is lost.", "Take it in. Tell her— no. Nothing. Go.",
                "Wren crosses; Halvard holds the Wardens; Voss is changed in Act 3 only if Corra's memory is carried out", scraps: 3);

            Sheet("bells", "6.12", "The Half-Cathedral Bells", "a faded cathedral whose bells erase Wren's lantern-radius", "Greyfold.HalfCathedral", 3,
                "the Road That Stops runs through the nave; it is the first thing she surveyed",
                "the nave, white; each ring shrinks what she can see; cut the ropes by their outlines; one bell, two in canon, the great bell in the white",
                "For the flock, going north.", "For the ones who stayed.", "For whoever reads this. We did not forget you.",
                "Clarity grows; the road continues; a bound memory instead of scraps", scraps: 0);

            Sheet("corras_drawing", "6.13", "Corra's Drawing", "a child's drawing of her father, huge, crayon-lined, wrong", "Blank.OldCapital", 4,
                "it guards the Old Capital District; Corra made it to keep him and it has kept everyone out, including him",
                "a white room with a crayon floor; it redraws its limbs; it draws a second, small Voss; the crayon runs out and it fights in outline",
                "That's my father. He's coming. Don't.", "I drew him bigger. He was bigger.", "Is that him? Behind you? Is he here?",
                "Corra's memory can be carried out; the district opens; the small drawing stays", scraps: 2);

            Sheet("archivist", "6.14", "The Archivist", "Corvin Halloway: an enormous half-drawn owl holding the seventh keystone", "Blank.OldCapital", 4,
                "he wants Wren to finish the Complete Survey; he has waited forty-one years for someone of both worlds",
                "the mirror-Observatory; he draws walls and floors, then Wren's drawing to fight beside him, then the Atlas frame, closing a wingspan a beat",
                "When I— granddaughter. Sit. I have drawn you a chair.", "I held all of it. Nothing was lost. Look.", "Then draw it better than I did. Go on.",
                "the endings branch; the seventh keystone offered, and the Rest", keystone: true, scraps: 3);

            Sheet("complete_survey", "6.15", "The Complete Survey", "the Great Atlas itself, trying to draw Wren into the page", "Halden.Observatory", 4,
                "reassembling it on Wren's terms: the keystones go back and the page does not agree (true ending only)",
                "the Observatory floor as the page; each phase a region's ink fixing her where she stands; the chorus keeps the beat and the bounds-walk is the only safe rhythm",
                "Stand still. Everything that stood still is kept.", "Wren Halloway, journeyman. Drawn. Stay drawn.", "Unheld. Un— oh. Oh. That's how it goes.",
                "the Open World; Wren flies, once, briefly", grants: Ability.Sky, scraps: 0);
        }
    }
}
