using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.World
{
    /// <summary>
    /// Pressing Play inside a room scene (without the persistent scene) still works: if the
    /// active scene contains a Room and there is no RoomManager, load Persistent additively.
    /// RoomManager then adopts the already-open room instead of loading its start room.
    /// </summary>
    public static class Bootstrap
    {
        public const string PersistentSceneName = "Persistent";

        /// <summary>Tests and tooling: true skips the prologue, false forces it, null defers to the editor pref.</summary>
        public static bool? SkipPrologueOverride { get; set; }

        /// <summary>A built player started with this skips the prologue too (testers, the performance probe).</summary>
        public const string SkipPrologueArg = "-skipPrologue";

        /// <summary>Start a new game at Saltmarrow instead of the Edge (OWSBG → Play From Saltmarrow, or <see cref="SkipPrologueArg"/>).</summary>
        public static bool SkipPrologue
        {
            get
            {
                if (SkipPrologueOverride.HasValue) return SkipPrologueOverride.Value;
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), SkipPrologueArg) >= 0) return true;
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool("OWSBG.SkipPrologue", false);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// A built player started with this picks up from the last desk she rested at (the save in
        /// <see cref="ContinueSlot"/>), in that room, instead of a new game: a tester whose game closed starts again where
        /// they were (PRO-04). Without a save it is a new game as usual.
        /// </summary>
        public const string ContinueArg = "-continue";

        /// <summary>Tests: true or false overrides the command line.</summary>
        public static bool? ContinueOverride { get; set; }

        /// <summary>The slot <see cref="ContinueArg"/> reads (the desks write slot 0); tests point it elsewhere.</summary>
        public static int ContinueSlot { get; set; }

        public static bool Continue => ContinueOverride ?? System.Array.IndexOf(System.Environment.GetCommandLineArgs(), ContinueArg) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsurePersistent()
        {
            if (Object.FindFirstObjectByType<RoomManager>() != null) return;
            if (Object.FindFirstObjectByType<Room>() == null) return;          // not a room scene (tests, menus)
            if (SceneManager.GetSceneByName(PersistentSceneName).isLoaded) return;
            if (!Application.CanStreamedLevelBeLoaded(PersistentSceneName))
            {
                Debug.LogWarning("[OWSBG] Persistent scene is not in Build Settings; cannot bootstrap.");
                return;
            }
            Debug.Log("[OWSBG] Bootstrapping persistent scene for room-only play.");
            SceneManager.LoadScene(PersistentSceneName, LoadSceneMode.Additive);
        }
    }
}
