using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace OWSBG.World
{
    /// <summary>
    /// The drawn art for rooms made at runtime (the Blank's islands): every texture and material under Art is in the
    /// Addressables group "Art" at its own path (PRG-24), so a room built from WorldState can load the kit, the
    /// tiles and the sheets it needs by name, and let them go with the room. Loads wait for completion: a runtime
    /// room is made inside a transition, behind the fade. Null when the art is not there (a test without the group).
    /// </summary>
    public static class AddressableArt
    {
        public const string Root = "Assets/_Project/Art/";

        public static string MaterialPath(string name) => Root + "Materials/" + name + ".mat";
        /// <summary>A character's sheet for one clip: Art/Characters/Folk_Chough/Folk_Chough_idle.png.</summary>
        public static string SheetPath(string character, string clip) => Root + "Characters/" + character + "/" + character + "_" + clip + ".png";

        /// <summary>The asset at an address, held until <paramref name="owner"/> is destroyed; null when nothing is there.</summary>
        public static T Load<T>(string address, GameObject owner) where T : Object
        {
            if (string.IsNullOrEmpty(address)) return null;
            try
            {
                var locations = Addressables.LoadResourceLocationsAsync(address, typeof(T));
                var found = locations.WaitForCompletion();
                bool any = found != null && found.Count > 0;
                Addressables.Release(locations);
                if (!any) return null;
                var handle = Addressables.LoadAssetAsync<T>(address);
                var asset = handle.WaitForCompletion();
                if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                    return null;
                }
                Held.On(owner).Add(handle);
                return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[OWSBG] art not loaded: " + address + " (" + e.Message + ")");
                return null;
            }
        }

        public static Material Material(string name, GameObject owner) => Load<Material>(MaterialPath(name), owner);

        /// <summary>
        /// A character's clips from their sheets, each a strip of square cells (the frame count is its width over its
        /// height), at the sheets' 12 frames a second; the cell's size in units is its height at 96 px a unit.
        /// </summary>
        public static List<SheetClip> Sheets(string character, IEnumerable<string> clips, GameObject owner, out float cellUnits)
        {
            var list = new List<SheetClip>();
            cellUnits = 0f;
            foreach (var clip in clips)
            {
                var tex = Load<Texture2D>(SheetPath(character, clip), owner);
                if (tex == null) continue;
                if (cellUnits <= 0f) cellUnits = tex.height / 96f;
                list.Add(new SheetClip { Name = clip, Sheet = tex, Frames = Mathf.Max(1, tex.width / Mathf.Max(1, tex.height)), Fps = 12f, Loop = true });
            }
            return list;
        }

        /// <summary>The handles a room holds: released when the room's scene unloads.</summary>
        public sealed class Held : MonoBehaviour
        {
            readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
            public int Count => _handles.Count;

            public static Held On(GameObject owner)
            {
                var held = owner.GetComponent<Held>();
                return held != null ? held : owner.AddComponent<Held>();
            }
            public void Add(AsyncOperationHandle h) => _handles.Add(h);

            void OnDestroy()
            {
                foreach (var h in _handles) if (h.IsValid()) Addressables.Release(h);
                _handles.Clear();
            }
        }
    }
}
