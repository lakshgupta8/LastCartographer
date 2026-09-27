using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>Where a Commission sits (docs/design/commissions.md). Stored as an int flag "commission.&lt;id&gt;".</summary>
    public enum CommissionState { Unknown = 0, Posted = 1, Taken = 2, Fulfilled = 3, Closed = 4, Failed = 5 }

    /// <summary>What a journal step waits for: a flag value, a surveyed vantage, or a counter the tracker bumps.</summary>
    public enum StepKind { Flag, Vantage, Count }

    [Serializable]
    public struct CommissionStep
    {
        public StepKind Kind;
        /// <summary>Flag key, vantage id, or counter name (for example "kill.MarshCrab", "kill.any").</summary>
        public string Key;
        /// <summary>Flag value or count required (at least).</summary>
        public int Target;
        /// <summary>The journal line.</summary>
        public string Text;

        public static CommissionStep Flag(string key, string text, int target = 1)
            => new CommissionStep { Kind = StepKind.Flag, Key = key, Target = target, Text = text };
        public static CommissionStep Vantage(string vantageId, string text)
            => new CommissionStep { Kind = StepKind.Vantage, Key = vantageId, Target = 1, Text = text };
        public static CommissionStep Count(string counter, int target, string text)
            => new CommissionStep { Kind = StepKind.Count, Key = counter, Target = target, Text = text };
    }

    /// <summary>
    /// One ledger entry. Plain data so it can live in code (greybox), in a ScriptableObject, or in JSON later.
    /// Tags from the bible: [F] Foreshadows a secret, [B] seeds a Blank island, [A] needs a later ability.
    /// </summary>
    [Serializable]
    public sealed class CommissionDef
    {
        public string Id;
        public string Title;
        /// <summary>The hub whose ledger posts it ("Saltmarrow").</summary>
        public string Hub;
        public string Poster;
        /// <summary>On the ledger, before it is taken.</summary>
        public string Brief;
        /// <summary>In the journal, once taken.</summary>
        public string Journal;
        /// <summary>Once closed.</summary>
        public string Aftermath;
        public CommissionStep[] Steps = Array.Empty<CommissionStep>();
        public int RewardScraps;
        public InstrumentKind RewardInstrument = InstrumentKind.None;
        public string RewardFlag;
        /// <summary>[B] Island id written to "blank.island.&lt;id&gt;" when closed; empty for none.</summary>
        public string BlankIsland;
        /// <summary>[F] Bible secret number ("3.4"); descriptive only.</summary>
        public string Foreshadows;
        /// <summary>[A] Ability the last step needs; descriptive, shown in the journal.</summary>
        public Ability RequiresAbility = Ability.None;
        /// <summary>The ledger posts it only once this flag is set; empty posts it from the start.</summary>
        public string PostAfterFlag;

        public bool SeedsIsland => !string.IsNullOrEmpty(BlankIsland);
    }

    /// <summary>
    /// The ledger state machine and step evaluation (PRG-12). All state lives in WorldState flags and
    /// numbers, so saves need nothing extra:
    ///   commission.&lt;id&gt;          = CommissionState
    ///   commission.&lt;id&gt;.step&lt;n&gt;  = counter for a Count step (Numbers)
    ///   blank.island.&lt;name&gt;      = 1 once a [B] commission is closed
    /// Posted → Taken → Fulfilled → Closed; Posted or Taken may Fail. Nothing moves backwards.
    /// </summary>
    public static class Commissions
    {
        public const string Prefix = "commission.";
        public const string ScrapsKey = "$vellum_scraps";

        /// <summary>Raised after every state change, with the new state.</summary>
        public static event Action<string, CommissionState> Changed;

        public static string StateKey(string id) => Prefix + id;
        public static string CounterKey(string id, int step) => Prefix + id + ".step" + step;
        public static string IslandKey(string island) => "blank.island." + island;

        public static CommissionState StateOf(WorldState w, string id) => (CommissionState)w.Get(StateKey(id));
        public static bool Is(WorldState w, string id, CommissionState state) => StateOf(w, id) == state;
        public static bool IsIslandSeeded(WorldState w, string island) => w.Is(IslandKey(island));

        /// <summary>Lower-case name used by Yarn: "posted", "taken", "fulfilled", "closed", "failed", "unknown".</summary>
        public static string Describe(CommissionState s) => s.ToString().ToLowerInvariant();

        static bool Move(WorldState w, string id, CommissionState to, CommissionState a, CommissionState b = CommissionState.Unknown)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var cur = StateOf(w, id);
            bool ok = cur == a || (b != CommissionState.Unknown && cur == b);
            if (!ok) return false;
            w.Set(StateKey(id), (int)to);
            Changed?.Invoke(id, to);
            return true;
        }

        public static bool Post(WorldState w, string id) => Move(w, id, CommissionState.Posted, CommissionState.Unknown);

        /// <summary>Post every unknown commission at a hub whose posting flag (if any) is set; the ledger does this when opened.</summary>
        public static int PostAvailable(WorldState w, string hub)
        {
            int n = 0;
            foreach (var d in CommissionCatalog.AtHub(hub))
            {
                if (StateOf(w, d.Id) != CommissionState.Unknown) continue;
                if (!string.IsNullOrEmpty(d.PostAfterFlag) && !w.Is(d.PostAfterFlag)) continue;
                if (Post(w, d.Id)) n++;
            }
            return n;
        }

        /// <summary>Take a posted commission. Steps already met (a vantage drawn earlier) fulfil it at once.</summary>
        public static bool Take(WorldState w, string id)
        {
            if (!Move(w, id, CommissionState.Taken, CommissionState.Posted)) return false;
            EvaluateOne(w, id);
            return true;
        }

        public static bool Fulfil(WorldState w, string id) => Move(w, id, CommissionState.Fulfilled, CommissionState.Taken);

        /// <summary>Turn in at the ledger: rewards land here.</summary>
        public static bool Close(WorldState w, string id)
        {
            if (!Move(w, id, CommissionState.Closed, CommissionState.Fulfilled)) return false;
            Reward(w, CommissionCatalog.Find(id));
            return true;
        }

        public static bool Fail(WorldState w, string id) => Move(w, id, CommissionState.Failed, CommissionState.Posted, CommissionState.Taken);

        /// <summary>The Yarn verbs: post, take, fulfil (fulfill), close, fail.</summary>
        public static bool Apply(WorldState w, string id, string verb)
        {
            switch ((verb ?? "").Trim().ToLowerInvariant())
            {
                case "post": return Post(w, id);
                case "take": return Take(w, id);
                case "fulfil":
                case "fulfill": return Fulfil(w, id);
                case "close": return Close(w, id);
                case "fail": return Fail(w, id);
                default: return false;
            }
        }

        static void Reward(WorldState w, CommissionDef def)
        {
            if (def == null) return;
            if (def.RewardScraps > 0) AddScraps(w, def.RewardScraps);
            if (def.RewardInstrument != InstrumentKind.None && !w.Equipment.OwnsInstrument(def.RewardInstrument))
                w.Equipment.OwnedInstruments.Add(def.RewardInstrument);
            if (!string.IsNullOrEmpty(def.RewardFlag)) w.Set(def.RewardFlag, 1);
            if (def.SeedsIsland) w.Set(IslandKey(def.BlankIsland), 1);
        }

        public static int Scraps(WorldState w) => w.Numbers.TryGetValue(ScrapsKey, out var v) ? (int)v : 0;

        public static void AddScraps(WorldState w, int n)
        {
            w.Numbers[ScrapsKey] = Scraps(w) + n;
        }

        // ---- steps ----

        public static int Counter(WorldState w, string id, int step)
            => w.Numbers.TryGetValue(CounterKey(id, step), out var v) ? (int)v : 0;

        /// <summary>Current progress toward the step's target (counts, or 0/1 for flags and vantages).</summary>
        public static int Progress(WorldState w, CommissionDef def, int step)
        {
            var s = def.Steps[step];
            switch (s.Kind)
            {
                case StepKind.Flag: return Math.Min(w.Get(s.Key), s.Target);
                case StepKind.Vantage: return w.IsSurveyed(s.Key) ? 1 : 0;
                default: return Math.Min(Counter(w, def.Id, step), s.Target);
            }
        }

        public static bool StepDone(WorldState w, CommissionDef def, int step)
        {
            var s = def.Steps[step];
            switch (s.Kind)
            {
                case StepKind.Flag: return w.Get(s.Key) >= s.Target;
                case StepKind.Vantage: return w.IsSurveyed(s.Key);
                default: return Counter(w, def.Id, step) >= s.Target;
            }
        }

        public static bool AllStepsDone(WorldState w, CommissionDef def)
        {
            for (int i = 0; i < def.Steps.Length; i++) if (!StepDone(w, def, i)) return false;
            return true;
        }

        /// <summary>Add to every matching Count step of every taken commission; returns how many steps moved.</summary>
        public static int Bump(WorldState w, string counter, int amount = 1)
        {
            int moved = 0;
            foreach (var def in CommissionCatalog.All)
            {
                if (StateOf(w, def.Id) != CommissionState.Taken) continue;
                for (int i = 0; i < def.Steps.Length; i++)
                {
                    var s = def.Steps[i];
                    if (s.Kind != StepKind.Count || s.Key != counter) continue;
                    w.Numbers[CounterKey(def.Id, i)] = Counter(w, def.Id, i) + amount;
                    moved++;
                }
            }
            if (moved > 0) Evaluate(w);
            return moved;
        }

        /// <summary>Fulfil every taken commission whose steps are all met. Returns the ids that moved.</summary>
        public static List<string> Evaluate(WorldState w)
        {
            var moved = new List<string>();
            foreach (var def in CommissionCatalog.All)
                if (EvaluateOne(w, def.Id)) moved.Add(def.Id);
            return moved;
        }

        static bool EvaluateOne(WorldState w, string id)
        {
            var def = CommissionCatalog.Find(id);
            if (def == null || StateOf(w, id) != CommissionState.Taken) return false;
            if (!AllStepsDone(w, def)) return false;
            return Fulfil(w, id);
        }
    }

    /// <summary>
    /// Every commission the game knows. The Saltmarrow set (bible 8.1) is built in code for the greybox;
    /// later regions register from data. Order is ledger order.
    /// </summary>
    public static class CommissionCatalog
    {
        static readonly List<CommissionDef> _defs = new List<CommissionDef>();
        static readonly Dictionary<string, CommissionDef> _byId = new Dictionary<string, CommissionDef>();
        static bool _defaults;

        public static IReadOnlyList<CommissionDef> All { get { EnsureDefaults(); return _defs; } }

        public static CommissionDef Find(string id)
        {
            EnsureDefaults();
            return id != null && _byId.TryGetValue(id, out var d) ? d : null;
        }

        public static List<CommissionDef> AtHub(string hub)
        {
            var list = new List<CommissionDef>();
            foreach (var d in All) if (d.Hub == hub) list.Add(d);
            return list;
        }

        /// <summary>Add or replace by id.</summary>
        public static void Register(CommissionDef def)
        {
            EnsureDefaults();
            if (def == null || string.IsNullOrEmpty(def.Id)) throw new ArgumentException("a commission needs an id");
            if (_byId.TryGetValue(def.Id, out var old)) _defs.Remove(old);
            _byId[def.Id] = def;
            _defs.Add(def);
        }

        /// <summary>Tests: back to the built-in set on next use.</summary>
        public static void Reset()
        {
            _defs.Clear();
            _byId.Clear();
            _defaults = false;
        }

        public static void EnsureDefaults()
        {
            if (_defaults) return;
            _defaults = true;
            foreach (var d in Saltmarrow()) { _byId[d.Id] = d; _defs.Add(d); }
            foreach (var d in Emberdown()) { _byId[d.Id] = d; _defs.Add(d); }
        }

        /// <summary>Bible 8.1, greybox-sized: each is finishable in the three greybox rooms.</summary>
        public static CommissionDef[] Saltmarrow() => new[]
        {
            new CommissionDef
            {
                Id = "saltmarrow.lantern_chain", Title = "Lantern Chain", Hub = "Saltmarrow", Poster = "The Guild, by proxy",
                Brief = "Seven lights on this coast. Four are dark. The Guild pays by the lamp, and does not ask who kept them.",
                Journal = "The fourth lighthouse is past Merrow's End and three dark lamps. Something still keeps it.",
                Aftermath = "One lamp lit. The chain has six more links, and three of them are out past the tethers.",
                Steps = new[] { CommissionStep.Vantage("Saltmarrow_Lighthouse/Lamp", "Re-light the fourth lighthouse") },
                RewardScraps = 2,
            },
            new CommissionDef
            {
                Id = "saltmarrow.bone_bridge", Title = "The Bone Bridge", Hub = "Saltmarrow", Poster = "Unsigned",
                Brief = "Under the bridge, the whale still sings. Nobody has written the words down. Somebody should, before nobody can.",
                Journal = "Draw the reeds from the Reedmother's vantage. Then ask Sable what she heard.",
                Aftermath = "The words are down. They sound like a list of names.",
                Steps = new[]
                {
                    CommissionStep.Vantage("Saltmarrow_A/Reedmother", "Survey the Reedmother's vantage"),
                    CommissionStep.Flag("saltmarrow.bone_bridge.heard", "Ask Sable about the song"),
                },
                RewardScraps = 1, Foreshadows = "3.4",
            },
            new CommissionDef
            {
                Id = "saltmarrow.iris_harvest", Title = "The Iris Harvest", Hub = "Saltmarrow", Poster = "A grower, unnamed",
                Brief = "Guild agents are burning the pale iris. The grower wants their boots off the beds. Wants it done quiet.",
                Journal = "The beds are up on the quay's platforms and along the boardwalk. Clear what is trampling them.",
                Aftermath = "The beds are clear for now. The Ferrymen's prices moved the same day.",
                Steps = new[] { CommissionStep.Count("kill.MarshCrab", 3, "Clear the iris beds") },
                RewardScraps = 1, RewardInstrument = InstrumentKind.IrisTincture,
            },
            new CommissionDef
            {
                Id = "saltmarrow.dotha", Title = "Dotha's Last Season", Hub = "Saltmarrow", Poster = "Merrow's End, what is left of it",
                Brief = "The last elder of Merrow's End wants company for the season. She says it is her last. She has said that before.",
                Journal = "Dotha sits on her stoop at the west end of Merrow's End. Sit with her. Learn what she sings. Decide.",
                Aftermath = "Merrow's End is on the map, one way or the other. The Blank will remember how.",
                Steps = new[] { CommissionStep.Flag("saltmarrow.dotha.decided", "Sit with Dotha and decide") },
                RewardScraps = 2, BlankIsland = "Merrows_End", PostAfterFlag = "saltmarrow.met_sable",
            },
            new CommissionDef
            {
                Id = "saltmarrow.tether_widows", Title = "The Tether-Widows", Hub = "Saltmarrow", Poster = "The tether-post",
                Brief = "One of the widows means to go in after her husband. The others would rather she did not. Nobody has asked her.",
                Journal = "Now the lamp is lit she waits at the tether-post. Sable carries word. Talk her out of it, or go with her.",
                Aftermath = "Whichever way it went, the tether-post has one fewer rope on it.",
                Steps = new[] { CommissionStep.Flag("saltmarrow.widow.decided", "Answer the widow, through Sable") },
                RewardScraps = 2, PostAfterFlag = "boss.lamp_keeper.defeated",
            },
        };
        /// <summary>Bible 8.2: Kettil's Rest's ledger. Vantage steps name the planned rooms (RoomPlans, DES-09).</summary>
        public static CommissionDef[] Emberdown() => new[]
        {
            new CommissionDef
            {
                Id = "emberdown.long_roll_call", Title = "The Long Roll-Call", Hub = "Emberdown", Poster = "The families of Hollowvein",
                Brief = "Thirty-one under the pit-head, buried standing. The families want them up. Kettil wants the boards left on.",
                Journal = "Hear the baths argue, learn the walk at the bell, and go to the pit-head. Runa will be there. Walk it down, or leave it.",
                Aftermath = "Hollowvein is decided. Runa counts it every night now, one way or the other.",
                Steps = new[] { CommissionStep.Flag("emberdown.hollowvein.decided", "Decide Hollowvein with Runa") },
                RewardScraps = 3, BlankIsland = "Hollowvein", PostAfterFlag = "emberdown.runa.counted",
            },
            new CommissionDef
            {
                Id = "emberdown.ninth_chimney", Title = "The Ninth Chimney", Hub = "Emberdown", Poster = "Unsigned, in soot",
                Brief = "Nine chimneys. The town built eight. Somebody lives at the top of the ninth and never comes down for supper.",
                Journal = "Climb the ninth chimney and see who keeps it.",
                Aftermath = "A Guild agent, a season into his posting. He has been there twenty-two years.",
                Steps = new[]
                {
                    CommissionStep.Vantage("Emberdown_Chimneys_3/Ninth", "Survey the ninth chimney"),
                    CommissionStep.Flag("emberdown.ninth.agent_met", "Meet whoever keeps it"),
                },
                RewardScraps = 2, Foreshadows = "5.1", RequiresAbility = Ability.Talonhold, PostAfterFlag = "emberdown.runa.climbed",
            },
            new CommissionDef
            {
                Id = "emberdown.debate", Title = "The Cinder Bath Debate", Hub = "Emberdown", Poster = "Kettil, loudly",
                Brief = "A Guild surveyor is doing sums in the baths. Kettil is doing louder ones. Somebody neutral should listen.",
                Journal = "The baths are past the chimneys. Listen to both of them.",
                Aftermath = "Both were right. Neither has forgiven you for saying so.",
                Steps = new[] { CommissionStep.Flag("emberdown.debate.heard", "Hear the debate at the baths") },
                RewardScraps = 1, PostAfterFlag = "emberdown.kettil.met",
            },
            new CommissionDef
            {
                Id = "emberdown.furnace_rescue", Title = "Furnace Stair Rescue", Hub = "Emberdown", Poster = "The stair crew",
                Brief = "A landing gave on the Furnace Stair. One of the crew is on the ledge below it. The furnace is lit.",
                Journal = "The landings are on the stair, over the live furnaces. Get Hask off the ledge.",
                Aftermath = "The stair crew counts nine again, and Wren.",
                Steps = new[] { CommissionStep.Flag("emberdown.rescue.done", "Pull Hask off the ledge") },
                RewardScraps = 2, RewardInstrument = InstrumentKind.PlumbWeight, PostAfterFlag = "emberdown.kettil.met",
            },
            new CommissionDef
            {
                Id = "emberdown.overlook", Title = "The Overlook", Hub = "Emberdown", Poster = "Runa",
                Brief = "Runa wants to show you something from the ridge. She says it is bigger than it looks.",
                Journal = "Over the baths and up the ridge. Stand at the overlook and look south.",
                Aftermath = "The Greyfold, from outside. Runa has counted it since she was a chick.",
                Steps = new[] { CommissionStep.Vantage("Emberdown_Overlook_2/Overlook", "Survey the overlook") },
                RewardScraps = 1, Foreshadows = "4.6", RequiresAbility = Ability.Talonhold, PostAfterFlag = "emberdown.runa.climbed",
            },
        };
    }
}
