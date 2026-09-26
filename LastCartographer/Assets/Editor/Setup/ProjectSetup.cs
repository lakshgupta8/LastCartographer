// Batch-mode project configuration. Entry points, run as separate editor launches because the
// first one creates scripts/asmdefs that need a compile + domain reload:
//
//   1) -executeMethod OWSBG.Setup.ProjectSetup.Configure
//        folders, asmdefs, runtime helper scripts, URP pipeline + renderer, project settings,
//        placeholder character texture.
//   2) -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene
//        the first 2.5D room: side-on camera, paper parallax layers, sun, post volume, lit sprite.
//   3) -executeMethod OWSBG.Setup.ProjectSetup.CaptureScreenshot   (run WITHOUT -nographics)
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OWSBG.Setup
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow.unity";

        static readonly string[] Folders =
        {
            "Art/Characters", "Art/Environment", "Art/UI", "Art/VFX", "Art/Materials",
            "Audio/Music", "Audio/SFX", "Audio/Ambience",
            "Code/Core", "Code/World", "Code/Narrative", "Code/UI", "Code/Editor",
            "Data/Quests", "Data/Regions", "Data/Characters", "Data/Items", "Data/Memories",
            "Dialogue/Halden", "Dialogue/Saltmarrow", "Dialogue/Emberdown", "Dialogue/Verdance",
            "Dialogue/Windreach", "Dialogue/Greyfold", "Dialogue/Blank", "Dialogue/Companions",
            "Prefabs/Characters", "Prefabs/Environment", "Prefabs/Systems", "Prefabs/UI",
            "Scenes/Persistent", "Scenes/Rooms", "Scenes/Greybox",
            "Settings/Rendering", "Settings/Input", "Settings/Localization",
        };

        // ------------------------------------------------------------------ 1
        public static void Configure()
        {
            Debug.Log("[OWSBG] Configure: start");
            CreateFolders();
            WriteAsmdefs();
            WriteRuntimeScripts();
            SetupUrp();
            SetupProjectSettings();
            WritePlaceholderTexture();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OWSBG] Configure: done");
        }

        static void CreateFolders()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/ThirdParty");
            foreach (var f in Folders)
                Directory.CreateDirectory(Path.Combine(Root, f));
            AssetDatabase.Refresh();
        }

        static void WriteAsmdefs()
        {
            WriteAsmdef("Code/Core/OWSBG.Core.asmdef", "OWSBG.Core",
                new[] { "Unity.InputSystem", "Unity.Addressables", "Unity.ResourceManager" }, false);
            WriteAsmdef("Code/World/OWSBG.World.asmdef", "OWSBG.World",
                new[] { "OWSBG.Core", "Unity.Cinemachine", "Unity.InputSystem", "Unity.Addressables",
                        "Unity.ResourceManager", "Unity.AI.Navigation", "Unity.Splines",
                        "Unity.RenderPipelines.Universal.Runtime" }, false);
            WriteAsmdef("Code/Narrative/OWSBG.Narrative.asmdef", "OWSBG.Narrative",
                new[] { "OWSBG.Core", "OWSBG.World", "YarnSpinner", "YarnSpinner.Unity", "Unity.Localization" }, false);
            WriteAsmdef("Code/UI/OWSBG.UI.asmdef", "OWSBG.UI",
                new[] { "OWSBG.Core", "OWSBG.World", "OWSBG.Narrative", "Unity.InputSystem", "Unity.TextMeshPro",
                        "Unity.Localization" }, false);
            WriteAsmdef("Code/Editor/OWSBG.Editor.asmdef", "OWSBG.Editor",
                new[] { "OWSBG.Core", "OWSBG.World", "OWSBG.Narrative", "OWSBG.UI" }, true);
        }

        static void WriteAsmdef(string relPath, string name, string[] refs, bool editorOnly)
        {
            var path = Path.Combine(Root, relPath);
            if (File.Exists(path)) return;
            var refJson = "";
            for (int i = 0; i < refs.Length; i++)
                refJson += "        \"" + refs[i] + "\"" + (i < refs.Length - 1 ? ",\n" : "\n");
            var platforms = editorOnly ? "[ \"Editor\" ]" : "[]";
            var json =
                "{\n" +
                "    \"name\": \"" + name + "\",\n" +
                "    \"rootNamespace\": \"" + name + "\",\n" +
                "    \"references\": [\n" + refJson + "    ],\n" +
                "    \"includePlatforms\": " + platforms + ",\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"overrideReferences\": false,\n" +
                "    \"precompiledReferences\": [],\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"defineConstraints\": [],\n" +
                "    \"versionDefines\": [],\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n";
            File.WriteAllText(path, json);
            Debug.Log("[OWSBG] wrote " + path);
        }

        static void WriteRuntimeScripts()
        {
            WriteIfMissing("Code/Core/WorldState.cs",
@"using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// The single source of truth for narrative flags. Every Commission, fade stage, anchor, and
    /// recurring-character state is a key here. Serialised to JSON by the save system.
    /// See docs/story/story-bible.md section 10 (story-to-mechanic bindings).
    /// </summary>
    [Serializable]
    public sealed class WorldState
    {
        public Dictionary<string, int> Flags = new Dictionary<string, int>();
        public HashSet<string> AnchoredPlaces = new HashSet<string>();
        public HashSet<string> SurveyedVantages = new HashSet<string>();
        public List<string> BoundMemories = new List<string>();

        public event Action<string, int> FlagChanged;

        public int Get(string key, int fallback = 0) => Flags.TryGetValue(key, out var v) ? v : fallback;
        public bool Is(string key) => Get(key) != 0;

        public void Set(string key, int value)
        {
            if (Flags.TryGetValue(key, out var old) && old == value) return;
            Flags[key] = value;
            FlagChanged?.Invoke(key, value);
        }

        public void Set(string key, bool value) => Set(key, value ? 1 : 0);
    }
}
");
        }

        static void WriteIfMissing(string relPath, string content)
        {
            var path = Path.Combine(Root, relPath);
            if (File.Exists(path)) return;
            File.WriteAllText(path, content);
            Debug.Log("[OWSBG] wrote " + path);
        }

        static void SetupUrp()
        {
            const string rendererPath = Root + "/Settings/Rendering/URP_Renderer.asset";
            const string pipelinePath = Root + "/Settings/Rendering/URP_Pipeline.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.renderingMode = RenderingMode.ForwardPlus;
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.supportsCameraDepthTexture = true;
                pipeline.supportsCameraOpaqueTexture = true;
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 60f;
                pipeline.shadowCascadeCount = 2;
                pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            Debug.Log("[OWSBG] URP pipeline assigned (Forward+, " + names.Length + " quality levels)");
        }

        static void SetupProjectSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.companyName = "OWSBG";
            PlayerSettings.productName = "The Last Cartographer";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            Time.fixedDeltaTime = 1f / 60f;

            // Active input handling: 0 = old, 1 = new Input System only, 2 = both.
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings != null && settings.Length > 0)
            {
                var so = new SerializedObject(settings[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null)
                {
                    prop.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[OWSBG] Input handling set to New Input System only");
                }
            }
        }

        // A 32x48 stand-in figure so the first scene has a "character" without any art yet.
        static void WritePlaceholderTexture()
        {
            var path = Path.Combine(Root, "Art/Characters/Placeholder_Wren.png");
            if (File.Exists(path)) return;
            const int w = 32, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            var coat = new Color(0.18f, 0.25f, 0.42f);
            var skin = new Color(0.86f, 0.70f, 0.56f);
            var hair = new Color(0.35f, 0.22f, 0.15f);
            var boots = new Color(0.20f, 0.14f, 0.10f);
            var satchel = new Color(0.55f, 0.40f, 0.22f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                if (y < 6 && x >= 10 && x < 22) c = boots;
                else if (y >= 6 && y < 28 && x >= 9 && x < 23) c = coat;
                else if (y >= 12 && y < 20 && x >= 20 && x < 25) c = satchel;
                else if (y >= 28 && y < 33 && x >= 12 && x < 20) c = skin;
                else if (y >= 33 && y < 43 && x >= 11 && x < 21) c = skin;
                else if (y >= 40 && y < 46 && x >= 10 && x < 22) c = hair;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            Debug.Log("[OWSBG] wrote placeholder character texture");
        }

        // ------------------------------------------------------------------ 2
        // The first 2.5D room: side-on gameplay plane at Z = 0, paper parallax layers behind and
        // in front, a sun, a post volume, and the placeholder Wren on a lit alpha-clipped quad.
        public static void BuildBootstrapScene()
        {
            Debug.Log("[OWSBG] BuildBootstrapScene: start");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera: side-on, slight downward tilt so platform tops read (art-direction 3).
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.transform.position = new Vector3(0f, 2.5f, -18f);
            cam.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.89f, 0.80f);   // warm paper
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;

            // Sun and art-directed ambient.
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(40f, -20f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.70f, 0.72f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.30f, 0.27f);

            // Play layer (Z = 0): a long floor and a few platforms. Saltmarrow palette.
            var floorMat = MakeLitMaterial("M_Greybox_Floor", new Color(0.45f, 0.47f, 0.36f));
            var platMat = MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            MakeBlock("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 2f), floorMat);
            MakeBlock("Platform_A", new Vector3(-6f, 2.0f, 0f), new Vector3(4f, 0.6f, 2f), platMat);
            MakeBlock("Platform_B", new Vector3(1f, 3.5f, 0f), new Vector3(3f, 0.6f, 2f), platMat);
            MakeBlock("Platform_C", new Vector3(7f, 5.0f, 0f), new Vector3(4f, 0.6f, 2f), platMat);
            MakeBlock("Wall_L", new Vector3(-13f, 3f, 0f), new Vector3(1f, 8f, 2f), platMat);
            MakeBlock("Stilt_1", new Vector3(11f, 1.5f, 0f), new Vector3(0.4f, 3f, 2f), platMat);

            // Parallax paper layers: further back = washed toward paper (aerial perspective).
            MakePaperLayer("Mid_Reeds", 3f, 0f, new Color(0.62f, 0.64f, 0.52f), 6f);
            MakePaperLayer("Far_Roosts", 8f, 2f, new Color(0.72f, 0.72f, 0.64f), 10f);
            MakePaperLayer("Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.80f, 0.72f), 16f);
            MakePaperLayer("Fore_Reeds", -4f, -2.6f, new Color(0.30f, 0.33f, 0.24f), 1.6f);

            // Placeholder Wren on the play plane (Wren is 1.2 units tall).
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Characters/Placeholder_Wren.png");
            var charMat = MakeLitMaterial("M_Sprite_Placeholder", Color.white);
            charMat.SetTexture("_BaseMap", tex);
            charMat.SetFloat("_AlphaClip", 1f);
            charMat.SetFloat("_Cutoff", 0.5f);
            charMat.SetFloat("_Smoothness", 0f);
            charMat.EnableKeyword("_ALPHATEST_ON");
            charMat.renderQueue = (int)RenderQueue.AlphaTest;
            var charRoot = new GameObject("Wren_Placeholder");
            charRoot.transform.position = new Vector3(-2f, 0f, 0f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(charRoot.transform, false);
            quad.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var qr = quad.GetComponent<MeshRenderer>();
            qr.sharedMaterial = charMat;
            qr.shadowCastingMode = ShadowCastingMode.TwoSided;

            // Post-processing: foreground DoF, gentle bloom, vignette, ACES.
            var profilePath = Root + "/Settings/Rendering/PP_Default.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var dof = profile.Add<DepthOfField>(true);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(24f);
                dof.gaussianEnd.Override(40f);
                dof.gaussianMaxRadius.Override(1.0f);
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.25f);
                bloom.threshold.Override(1.1f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.Override(0.22f);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[OWSBG] BuildBootstrapScene: saved " + ScenePath);
        }

        static void MakeBlock(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.position = pos;
            b.transform.localScale = scale;
            b.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // A flat paper cutout behind or in front of the play plane.
        static void MakePaperLayer(string name, float z, float y, Color color, float height)
        {
            var mat = MakeLitMaterial("M_Paper_" + name, color);
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(mat);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Paper_" + name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.position = new Vector3(0f, y + height * 0.5f, z);
            q.transform.localScale = new Vector3(80f, height, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ------------------------------------------------------------------ 3
        // Renders the greybox room from its camera to a PNG so the look can be checked
        // without opening the editor. Needs a GPU: run WITHOUT -nographics.
        public static void CaptureScreenshot()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[OWSBG] no main camera"); return; }
            const int w = 1280, h = 720;
            ShaderUtil.allowAsyncCompilation = false;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();   // warm up: compiles variants
            cam.Render();   // real frame
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            var outDir = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "logs");
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, "greybox-screenshot.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Debug.Log("[OWSBG] screenshot written: " + outPath);
        }

        static Material MakeLitMaterial(string name, Color color)
        {
            var path = Root + "/Art/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
