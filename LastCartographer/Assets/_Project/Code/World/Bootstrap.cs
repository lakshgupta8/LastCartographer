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
