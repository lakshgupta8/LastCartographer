using System;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// What build this is (PRG-25): the version, the commit, when it was built, and whether it's a development build.
    /// The build script writes <c>Resources/build_info.json</c> just before it builds and puts the checked-in "dev" one
    /// back after, so the editor and tests always read "dev". The options page shows it; bug reports quote it.
    /// </summary>
    public static class BuildInfo
    {
        public const string ResourcePath = "build_info";

        [Serializable]
        sealed class Data
        {
            public string version = "dev";
            public string commit = "";
            public string built = "";
            public bool development = true;
        }

        static Data _data;

        static Data Current
        {
            get
            {
                if (_data != null) return _data;
                TextAsset asset = null;
                try { asset = Resources.Load<TextAsset>(ResourcePath); } catch { }
                try { _data = asset != null ? JsonUtility.FromJson<Data>(asset.text) : null; } catch { _data = null; }
                return _data ??= new Data();
            }
        }

        public static string Version => Current.version;
        public static string Commit => Current.commit;
        public static string Built => Current.built;
        public static bool Development => Current.development;

        /// <summary>"0.5.0 · 1a2b3c4", or the version alone when there is no commit.</summary>
        public static string Label => string.IsNullOrEmpty(Commit) ? Version : Version + " · " + Commit;

        /// <summary>The file the build script writes.</summary>
        public static string ToJson(string version, string commit, string built, bool development) =>
            JsonUtility.ToJson(new Data { version = version ?? "dev", commit = commit ?? "", built = built ?? "", development = development }, true);

        /// <summary>Tests: read the resource again.</summary>
        public static void Reset() => _data = null;
    }
}
