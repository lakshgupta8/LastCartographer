using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The atlas's pen keeps the road (docs/design/atlas-map.md): every room she comes into is walked
    /// (<see cref="AtlasMap.Walk"/>), so its page has it in pencil, or in ink where there is nothing to survey.
    /// </summary>
    public static class AtlasWalk
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            RoomManager.Transitioned -= OnTransitioned;
            RoomManager.Transitioned += OnTransitioned;
        }

        static void OnTransitioned(string scene, float ms) => AtlasMap.Walk(GameState.World, scene);
    }
}
