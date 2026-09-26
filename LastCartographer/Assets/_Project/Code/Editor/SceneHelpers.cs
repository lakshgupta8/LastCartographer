using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.Editor
{
    /// <summary>
    /// Editor conveniences so the project is playable the moment it opens:
    /// - Opening the persistent scene alone also opens the start room additively.
    /// - OWSBG menu: Play From Start, Open Greybox.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneHelpers
    {
        public const string PersistentPath = "Assets/_Project/Scenes/Persistent/Persistent.unity";
        public const string StartRoomPath = "Assets/_Project/Scenes/Greybox/Greybox_Saltmarrow_A.unity";

        static SceneHelpers()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode != OpenSceneMode.Single || scene.path != PersistentPath) return;
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SceneManager.GetSceneByPath(StartRoomPath).isLoaded) return;
                if (!System.IO.File.Exists(StartRoomPath)) return;
                EditorSceneManager.OpenScene(StartRoomPath, OpenSceneMode.Additive);
            };
        }

        [MenuItem("OWSBG/Play From Start %#p", priority = 0)]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PersistentPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(StartRoomPath, OpenSceneMode.Additive);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("OWSBG/Open Greybox", priority = 1)]
        public static void OpenGreybox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PersistentPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(StartRoomPath, OpenSceneMode.Additive);
        }

        [MenuItem("OWSBG/Docs/Combat and Movement", priority = 20)]
        static void OpenCombatDoc() => Application.OpenURL(System.IO.Path.GetFullPath("../docs/design/combat-and-movement.md"));

        [MenuItem("OWSBG/Docs/Story Bible", priority = 21)]
        static void OpenBible() => Application.OpenURL(System.IO.Path.GetFullPath("../docs/story/story-bible.md"));
    }
}
