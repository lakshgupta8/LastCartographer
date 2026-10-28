using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The world's sounds in play (AUD-11, docs/design/world-sounds.md): wakes with the game beside the bank and answers
    /// the world's own events with <see cref="WorldSounds"/>' cues. A room change is the page turned, a lamp lighting
    /// its whump, the desk its rest, a seed its drop (pitched by its worth), a seal pressed its stamp, a place erased the
    /// eraser and one recovered the ink drawn back, a decision its sound, an ability its flourish, a shut way a knock, a
    /// thing thrown its end as it ended (on her, the ground, the air or unwritten: AUD-19), a Cantor's toll its eraser's
    /// three sweeps, which are also the place's erasure when it erases one (AUD-22), the props that move their own: a weak
    /// floor giving way, a plank drawn by the light or let go, the tide taking her back (AUD-25). Sounds with a place in the
    /// room play from it. Wren's own (her Instruments, her Clarity, her Charter) are <see cref="WrenSounds"/>'; the
    /// pages' are the views' (<c>UiSounds</c>).
    /// </summary>
    public sealed class WorldSoundHooks : MonoBehaviour
    {
        public static WorldSoundHooks Instance { get; private set; }
        public string Last { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~WorldSounds");
            DontDestroyOnLoad(go);
            go.AddComponent<WorldSoundHooks>();
        }

        void Awake() { if (Instance == null) Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            RoomManager.Transitioned += OnTransitioned;
            TravelPoint.Lit += OnLampLit;
            DraftingDesk.Rested += OnRested;
            IrisSeed.Collected += OnSeed;
            Memories.Recovered += OnMemoriesBack;
            Economy.Bought += OnBought;
            Economy.MaskBought += OnMaskBought;
            Economy.SlotBought += OnSlotBought;
            Commissions.Changed += OnCommission;
            FadeStages.Changed += OnFade;
            FadeStages.Erased += OnErased;
            FadeStages.Recovered += OnRecovered;
            Places.FateChanged += OnFate;
            Atlas.WaypointFound += OnWaypoint;
            FastTravel.Arrived += OnTravelled;
            PlayerRespawn.AnyRespawned += OnRespawned;
            PlayerRespawn.AnyWaxSealUsed += OnSealBroke;
            FallCatch.AnyFell += OnFell;
            RoomTransition.Bumped += OnBarred;
            Offerings.Offered += OnOffered;
            EnemyProjectile.Spent += OnShotSpent;
            Cantor.Tolled += OnTolled;
            WeakFloor.AnyBroke += OnFloorBroke;
            HiddenPlatform.Solidified += OnHiddenPlank;
            LanternPlatform.Changed += OnLanternPlank;
            Tide.AnySwept += OnTide;
        }

        void OnDisable()
        {
            RoomManager.Transitioned -= OnTransitioned;
            TravelPoint.Lit -= OnLampLit;
            DraftingDesk.Rested -= OnRested;
            IrisSeed.Collected -= OnSeed;
            Memories.Recovered -= OnMemoriesBack;
            Economy.Bought -= OnBought;
            Economy.MaskBought -= OnMaskBought;
            Economy.SlotBought -= OnSlotBought;
            Commissions.Changed -= OnCommission;
            FadeStages.Changed -= OnFade;
            FadeStages.Erased -= OnErased;
            FadeStages.Recovered -= OnRecovered;
            Places.FateChanged -= OnFate;
            Atlas.WaypointFound -= OnWaypoint;
            FastTravel.Arrived -= OnTravelled;
            PlayerRespawn.AnyRespawned -= OnRespawned;
            PlayerRespawn.AnyWaxSealUsed -= OnSealBroke;
            FallCatch.AnyFell -= OnFell;
            RoomTransition.Bumped -= OnBarred;
            Offerings.Offered -= OnOffered;
            EnemyProjectile.Spent -= OnShotSpent;
            Cantor.Tolled -= OnTolled;
            WeakFloor.AnyBroke -= OnFloorBroke;
            HiddenPlatform.Solidified -= OnHiddenPlank;
            LanternPlatform.Changed -= OnLanternPlank;
            Tide.AnySwept -= OnTide;
        }

        void Play(string id, Vector2? at = null, float pitch = 1f)
        {
            if (id == null) return;
            InkSoundBank.Play(id, 1f, at, pitch);
            Last = id;
        }

        static Vector2? At(Component c) => c != null ? (Vector2?)c.transform.position : null;

        void OnTransitioned(string scene, float ms) => Play("page_turn");
        void OnLampLit(TravelPoint tp) => Play("lamp_lit", At(tp));
        void OnRested(DraftingDesk desk) => Play("desk_rest", At(desk));
        void OnSeed(IrisSeed seed, int count) => Play("seed", At(seed), WorldSounds.SeedPitch(count));
        void OnMemoriesBack(int n) => Play("memories_back");
        void OnBought(StockItem item) => Play("buy");
        void OnMaskBought(int n) => Play("buy");
        void OnSlotBought() => Play("buy");
        void OnCommission(string id, CommissionState state) => Play(WorldSounds.CommissionCue(state));
        bool _hush;   // an erasure or a recovery also reports a stage change in the same frame: one sound, not two
        void OnFade(string place, int stage) { if (!_hush) Play("fade_step"); }
        void OnErased(string place) { _hush = true; if (Cantor.Tolling == null) Play("erased"); }   // a toll's erasure is its eraser (OnTolled)
        void OnRecovered(string place) { _hush = true; Play("recovered"); }
        void OnFate(string place, PlaceFate fate) => Play(WorldSounds.FateCue(fate));
        void OnWaypoint(Waypoint wp) => Play("waypoint");
        void OnTravelled(Waypoint to) => Play("travel");
        void OnRespawned() => Play("redrawn");
        void OnSealBroke() => Play("seal_break");
        void OnFell(FallCatch f) => Play("fell");
        void OnBarred(RoomTransition t) => Play("barred", At(t));
        void OnOffered(string asker, string memory) => Play("offered");
        void OnShotSpent(EnemyProjectile p) => Play(EnemySounds.EndCue(EnemySounds.ShotOf(p.name), p.Ending), At(p));

        /// <summary>A Cantor's toll drags the eraser three sweeps out from the bell, each heard from where it is drawn (AUD-22).</summary>
        void OnTolled(Cantor c, string place)
        {
            if (c == null) return;
            for (int k = -1; k <= 1; k++) Play("eraser_sweep", Cantor.SweepAt(c.transform.position, k), WorldSounds.SweepPitch(k));
        }

        // ---- the props that move (AUD-25) ----
        void OnFloorBroke(WeakFloor f) => Play("floor_break", At(f));
        void OnHiddenPlank(HiddenPlatform p, bool drawn) => Play(drawn ? "plank_drawn" : "plank_undrawn", At(p));
        void OnLanternPlank(LanternPlatform p, bool drawn) => Play(drawn ? "plank_drawn" : "plank_undrawn", At(p));
        void OnTide(Tide t, Vector2 at) => Play("tide_sweep", at);

        void LateUpdate() { _hush = false; }
    }
}
