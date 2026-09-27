// Batch-mode project configuration. Entry points, run as separate editor launches:
//
//   1) -executeMethod OWSBG.Setup.ProjectSetup.Configure
//        folders, asmdefs, layers, URP pipeline + renderer, project settings, placeholder texture.
//   2) -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene
//        the persistent scene (camera rig, Wren, RoomManager, volume) and two greybox rooms.
//   3) -executeMethod OWSBG.Setup.ProjectSetup.CaptureScreenshot   (run WITHOUT -nographics)
//   4) -executeMethod OWSBG.Setup.ProjectSetup.BuildAddressables   (player content; not needed for Play mode)
using System.IO;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using Unity.Cinemachine;
using Yarn.Unity;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OWSBG.Setup
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        public const string PersistentScenePath = Root + "/Scenes/Persistent/Persistent.unity";
        public const string RoomAScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_A.unity";
        public const string RoomBScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_B.unity";
        public const string RoomCScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_Lighthouse.unity";
        public const string RoomEdgeScenePath = Root + "/Scenes/Greybox/Greybox_Greyfold_Edge.unity";
        const string RendererAssetPath = Root + "/Settings/Rendering/URP_Renderer.asset";
        const string CutsceneDir = Root + "/Data/Cutscenes";
        const string InputAssetPath = Root + "/Settings/Input/WrenInput.inputactions";
        const string YarnProjectPath = Root + "/Dialogue/LastCartographer.yarnproject";
        const string PlaceholderTexPath = Root + "/Art/Characters/Placeholder_Wren.png";
        const string UiThemePath = Root + "/Settings/UI/Resources/OWSBG_Theme.tss";
        const string PanelSettingsPath = Root + "/Settings/UI/Resources/OWSBG_PanelSettings.asset";

        static readonly string[] Folders =
        {
            "Art/Characters", "Art/Environment", "Art/UI", "Art/VFX", "Art/Materials", "Art/Shaders",
            "Audio/Music", "Audio/SFX", "Audio/Ambience",
            "Code/Core", "Code/World", "Code/Narrative", "Code/UI", "Code/Editor",
            "Data/Quests", "Data/Regions", "Data/Characters", "Data/Items", "Data/Memories",
            "Dialogue/Halden", "Dialogue/Saltmarrow", "Dialogue/Emberdown", "Dialogue/Verdance",
            "Dialogue/Windreach", "Dialogue/Greyfold", "Dialogue/Blank", "Dialogue/Companions",
            "Prefabs/Characters", "Prefabs/Environment", "Prefabs/Systems", "Prefabs/UI",
            "Scenes/Persistent", "Scenes/Rooms", "Scenes/Greybox",
            "Settings/Rendering", "Settings/Input", "Settings/Localization",
            "Tests/PlayMode", "Tests/EditMode",
        };

        // Physics layers (index, name). Gameplay casts use these by name.
        static readonly (int, string)[] Layers =
        {
            (6, "Ground"), (7, "Player"), (8, "Enemy"), (9, "Hittable"), (10, "Paper"), (11, "Trigger"),
        };

        // ------------------------------------------------------------------ 1
        public static void Configure()
        {
            Debug.Log("[OWSBG] Configure: start");
            CreateFolders();
            WriteAsmdefs();
            SetupLayers();
            SetupUrp();
            SetupProjectSettings();
            WritePlaceholderTexture();
            EnsureYarnProject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OWSBG] Configure: done");
        }

        // One .yarnproject covering every .yarn under Dialogue/. The importer compiles it.
        static void EnsureYarnProject()
        {
            if (File.Exists(YarnProjectPath)) return;
            var project = new Yarn.Compiler.Project
            {
                BaseLanguage = "en",
                SourceFilePatterns = new[] { "**/*.yarn" },
                ExcludeFilePatterns = new[] { "**/*~/*" },
            };
            project.SaveToFile(YarnProjectPath);
            AssetDatabase.ImportAsset(YarnProjectPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[OWSBG] wrote " + YarnProjectPath);
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

        static void SetupLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) { Debug.LogError("[OWSBG] TagManager not found"); return; }
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            foreach (var (index, name) in Layers)
            {
                var el = layers.GetArrayElementAtIndex(index);
                if (el.stringValue != name) el.stringValue = name;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            // Keep the default: queries can hit triggers. Movement casts exclude triggers by layer
            // mask; interaction and strike overlaps rely on finding trigger colliders.
            Physics2D.queriesHitTriggers = true;
            // Wren's kinematic body must never shove dynamic enemies; contact damage is an overlap check.
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Enemy"), true);
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Trigger"), true);
            Debug.Log("[OWSBG] layers set: Ground, Player, Enemy, Hittable, Paper, Trigger");
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

            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings != null && settings.Length > 0)
            {
                var so = new SerializedObject(settings[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null)
                {
                    prop.intValue = 1;   // new Input System only
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static void WritePlaceholderTexture()
        {
            if (File.Exists(PlaceholderTexPath)) return;
            const int w = 32, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            var coat = new Color(0.18f, 0.25f, 0.42f);
            var breast = new Color(0.90f, 0.84f, 0.70f);
            var head = new Color(0.45f, 0.38f, 0.30f);
            var feet = new Color(0.20f, 0.14f, 0.10f);
            var quill = new Color(0.08f, 0.08f, 0.10f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                if (y < 4 && x >= 11 && x < 21) c = feet;
                else if (y >= 4 && y < 26 && x >= 8 && x < 24) c = coat;
                else if (y >= 8 && y < 24 && x >= 12 && x < 20) c = breast;
                else if (y >= 26 && y < 40 && x >= 9 && x < 23) c = head;
                else if (y >= 30 && y < 34 && x >= 23 && x < 28) c = head;      // beak
                if (x >= 25 && x < 27 && y >= 2 && y < 46) c = quill;             // the needle-quill
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(PlaceholderTexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PlaceholderTexPath);
            var importer = AssetImporter.GetAtPath(PlaceholderTexPath) as TextureImporter;
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
        public static void BuildBootstrapScene()
        {
            Debug.Log("[OWSBG] BuildBootstrapScene: start");
            BuildRoomA();
            BuildRoomB();
            BuildRoomC();
            BuildRoomEdge();
            SetupRenderFeatures();
            BuildPersistent();
            // Rooms stream through Addressables (PRG-07); only the persistent scene is a built-in scene.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PersistentScenePath, true) };
            SetupAddressables();
            AssetDatabase.SaveAssets();
            Debug.Log("[OWSBG] BuildBootstrapScene: done");
        }

        static void BuildPersistent()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera + brain.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.89f, 0.80f);
            cam.transform.position = new Vector3(0f, 2.5f, -18f);
            cam.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            camGo.AddComponent<CinemachineBrain>();

            // Sun and ambient (rooms share them for now; per-region lighting is ENV-10).
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

            // Wren rig.
            var wren = BuildWren();

            // Cinemachine follow rig (engine doc step 2).
            var vcamGo = new GameObject("CM Follow");
            vcamGo.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = wren.transform;
            var lens = vcam.Lens;
            lens.FieldOfView = 30f;
            lens.NearClipPlane = 0.3f;
            lens.FarClipPlane = 120f;
            vcam.Lens = lens;
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 18f;
            composer.TargetOffset = new Vector3(0f, 1.2f, 0f);
            composer.Damping = new Vector3(0.35f, 0.6f, 0f);
            var look = composer.Lookahead;
            look.Enabled = true;
            look.Time = 0.25f;
            look.Smoothing = 8f;
            look.IgnoreY = true;
            composer.Lookahead = look;
            var comp = composer.Composition;
            comp.DeadZone.Enabled = true;
            comp.DeadZone.Size = new Vector2(0.06f, 0.16f);
            composer.Composition = comp;
            var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();

            // Post volume.
            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = GetOrCreateVolumeProfile();

            // The atlas UI: one UI Toolkit document with the HUD, dialogue page, desk page and boss bar.
            var uiGo = new GameObject("UI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = EnsurePanelSettings();
            uiGo.AddComponent<OWSBG.UI.UiRoot>();
            uiGo.AddComponent<OWSBG.UI.HudView>();
            uiGo.AddComponent<OWSBG.UI.DialogueView>();
            uiGo.AddComponent<OWSBG.UI.DeskMenu>();
            uiGo.AddComponent<OWSBG.UI.BossView>();
            uiGo.AddComponent<OWSBG.UI.LedgerView>();
            uiGo.AddComponent<OWSBG.UI.JournalView>();
            uiGo.AddComponent<OWSBG.UI.PromptView>();
            uiGo.AddComponent<OWSBG.UI.FadeView>();

            // Dialogue service drawing through the UI's dialogue view.
            var dlgGo = new GameObject("DialogueService");
            dlgGo.AddComponent<WorldStateVariableStorage>();
            var presenter = dlgGo.AddComponent<ViewDialoguePresenter>();
            var service = dlgGo.AddComponent<DialogueService>();
            var yarnProject = AssetDatabase.LoadAssetAtPath<YarnProject>(YarnProjectPath);
            if (yarnProject == null) Debug.LogWarning("[OWSBG] Yarn project asset not found at " + YarnProjectPath);
            var dsSo = new SerializedObject(service);
            dsSo.FindProperty("_project").objectReferenceValue = yarnProject;
            var pres = dsSo.FindProperty("_presenters");
            pres.arraySize = 1;
            pres.GetArrayElementAtIndex(0).objectReferenceValue = presenter;
            dsSo.ApplyModifiedPropertiesWithoutUndo();

            // Death handling and the commission tracker.
            var sysGo = new GameObject("PlayerSystems");
            sysGo.AddComponent<PlayerRespawn>();
            sysGo.AddComponent<CommissionTracker>();

            // Room manager.
            var rmGo = new GameObject("RoomManager");
            var rm = rmGo.AddComponent<RoomManager>();
            var rmSo = new SerializedObject(rm);
            rmSo.FindProperty("_startRoom").stringValue = Path.GetFileNameWithoutExtension(RoomAScenePath);
            rmSo.FindProperty("_startSpawn").stringValue = "Start";
            rmSo.FindProperty("_prologueRoom").stringValue = Path.GetFileNameWithoutExtension(RoomEdgeScenePath);
            rmSo.FindProperty("_prologueSpawn").stringValue = "Start";
            rmSo.FindProperty("_wren").objectReferenceValue = wren.GetComponent<WrenController>();
            rmSo.FindProperty("_confiner").objectReferenceValue = confiner;
            rmSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, PersistentScenePath);
            Debug.Log("[OWSBG] saved " + PersistentScenePath);
        }

        // UI Toolkit runtime panel: the default theme (fonts, base styles) and a 1080p-scaled panel.
        static PanelSettings EnsurePanelSettings()
        {
            var dir = Path.GetDirectoryName(UiThemePath).Replace('\\', '/');
            Directory.CreateDirectory(dir);
            if (!File.Exists(UiThemePath))
            {
                File.WriteAllText(UiThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(UiThemePath);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiThemePath);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelSettingsPath);
            }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            return ps;
        }

        static GameObject BuildWren()
        {
            var go = new GameObject("Wren") { layer = LayerMask.NameToLayer("Player") };
            go.transform.position = new Vector3(-2f, 0f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f);
            box.offset = new Vector2(0f, 0.55f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var abilities = go.AddComponent<AbilitySet>();
            var abSo = new SerializedObject(abilities);
            abSo.FindProperty("_unlocked").intValue = (int)(Ability.Wingbeat | Ability.Talonhold);   // greybox feel-testing
            abSo.ApplyModifiedPropertiesWithoutUndo();

            var reader = go.AddComponent<InputReader>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null) Debug.LogWarning("[OWSBG] input asset not found at " + InputAssetPath);
            var rdSo = new SerializedObject(reader);
            rdSo.FindProperty("_asset").objectReferenceValue = inputAsset;
            rdSo.ApplyModifiedPropertiesWithoutUndo();

            var ctrl = go.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            go.AddComponent<Inkwell>();
            go.AddComponent<WrenVitals>();
            var strike = go.AddComponent<QuillStrike>();
            strike.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            go.AddComponent<Interactor>();
            var flourishes = go.AddComponent<Flourishes>();
            flourishes.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            var strikeVisual = go.AddComponent<StrikeVisual>();
            var svSo = new SerializedObject(strikeVisual);
            svSo.FindProperty("_inkMaterial").objectReferenceValue = MakeLitMaterial("M_Ink_Black", new Color(0.06f, 0.06f, 0.08f));
            svSo.ApplyModifiedPropertiesWithoutUndo();
            var belt = go.AddComponent<InstrumentBelt>();
            belt.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            belt.groundMask = LayerMask.GetMask("Ground");
            go.AddComponent<CharterSet>();   // after the components it drives; profiles default in Awake

            // Visual: an InkSprite quad, 1.2 units tall.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexPath);
            var mat = MakeInkMaterial("M_Wren_Ink", tex);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var qr = quad.GetComponent<MeshRenderer>();
            qr.sharedMaterial = mat;
            qr.shadowCastingMode = ShadowCastingMode.TwoSided;

            var view = go.AddComponent<WrenView>();
            var vwSo = new SerializedObject(view);
            vwSo.FindProperty("_visual").objectReferenceValue = quad.transform;
            vwSo.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void BuildRoomA()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom("Saltmarrow_A", new Rect(-20f, -3f, 40f, 17f));

            var floorMat = MakeLitMaterial("M_Greybox_Floor", new Color(0.45f, 0.47f, 0.36f));
            var platMat = MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            MakeGround(room, "Floor", new Vector2(0f, -0.5f), new Vector2(40f, 1f), floorMat);
            MakeGround(room, "Platform_A", new Vector2(-6f, 2.0f), new Vector2(4f, 0.6f), platMat);
            MakeGround(room, "Platform_B", new Vector2(1f, 3.5f), new Vector2(3f, 0.6f), platMat);
            MakeGround(room, "Platform_C", new Vector2(7f, 5.0f), new Vector2(4f, 0.6f), platMat);
            MakeGround(room, "Wall_L", new Vector2(-13f, 3f), new Vector2(1f, 8f), platMat);
            MakeGround(room, "Stilt_1", new Vector2(11f, 1.5f), new Vector2(0.4f, 3f), platMat);
            MakeWeakFloor(room, "WeakFloor_1", new Vector2(-9f, 4.6f), new Vector2(3f, 0.5f));
            MakeHiddenPlatform(room, "Hidden_1", new Vector2(16f, 3.2f), new Vector2(3f, 0.5f));

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.62f, 0.64f, 0.52f), 6f);
            MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.72f, 0.72f, 0.64f), 10f);
            MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.80f, 0.72f), 16f);
            MakePaperLayer(room, "Fore_Reeds", -4f, -2.6f, new Color(0.30f, 0.33f, 0.24f), 1.6f);

            MakeDummy(room, new Vector2(4f, 0.6f));
            MakeNpc(room, "Sable_Greybox", new Vector2(-9.5f, 0f), "Greybox_Sable", new Color(0.16f, 0.18f, 0.22f));
            MakeVantage(room, "Reedmother", "Saltmarrow_A/Reedmother", new Vector2(14f, 0f));
            MakeDesk(room, new Vector2(-4.5f, 0f));
            MakeLedger(room, "Saltmarrow", new Vector2(-7f, 0f));
            MakeEnemy<MarshCrab>(room, "Crab_1", new Vector2(7f, 5.9f), new Vector2(0.9f, 0.7f));
            MakeEnemy<ReedSkimmer>(room, "Skimmer_1", new Vector2(9f, 4f), new Vector2(0.9f, 0.5f));
            MakeEnemy<Smudge>(room, "Smudge_1", new Vector2(-15.5f, 1.5f), new Vector2(1.1f, 1.1f));

            MakeSpawn(room, "Start", new Vector2(-2f, 0f));
            MakeSpawn(room, "West", new Vector2(-17f, 0f));
            MakeSpawn(room, "East", new Vector2(17f, 0f));
            MakeSpawn(room, "Shore", new Vector2(-11.5f, 0f));

            // Waking on the shore after the prologue: the white thins while Sable speaks.
            var wake = NewTimeline(CutsceneDir + "/CS_ShoreWake.playable");
            var wakeFade = AddClip<PaperFadeClip>(wake.CreateTrack<PlayableTrack>("Fade"), 0.3, 2.4, "white thins");
            wakeFade.From = 1f; wakeFade.To = 0f; wakeFade.Color = ScreenFade.White;
            AddClip<DialogueNodeClip>(wake.CreateTrack<PlayableTrack>("Talk"), 1.2, 0.2, "Sable").Node = "Prologue_Shore_Wake";
            var wakeCs = MakeCutscene(room, "shore_wake", wake, null);
            var shore = wakeCs.gameObject.AddComponent<ShoreWake>();
            var shoreSo = new SerializedObject(shore);
            shoreSo.FindProperty("_cutscene").objectReferenceValue = wakeCs;
            shoreSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wake);

            MakeTransition(room, "To_B", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f),
                Path.GetFileNameWithoutExtension(RoomBScenePath), "West");

            EditorSceneManager.SaveScene(scene, RoomAScenePath);
            Debug.Log("[OWSBG] saved " + RoomAScenePath);
        }

        static void BuildRoomB()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom("Saltmarrow_B", new Rect(-20f, -3f, 40f, 20f));

            var floorMat = MakeLitMaterial("M_Greybox_Floor_B", new Color(0.40f, 0.44f, 0.38f));
            var platMat = MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            MakeGround(room, "Floor", new Vector2(0f, -0.5f), new Vector2(40f, 1f), floorMat);
            MakeGround(room, "Step_1", new Vector2(-8f, 1.5f), new Vector2(3f, 0.6f), platMat);
            MakeGround(room, "Step_2", new Vector2(-3f, 3.5f), new Vector2(3f, 0.6f), platMat);
            // A shaft for Talonhold: two walls 4 units apart.
            MakeGround(room, "Shaft_L", new Vector2(6f, 6f), new Vector2(1f, 12f), platMat);
            MakeGround(room, "Shaft_R", new Vector2(10f, 6f), new Vector2(1f, 12f), platMat);
            MakeGround(room, "Shaft_Top", new Vector2(8f, 12.3f), new Vector2(3f, 0.6f), platMat);

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.58f, 0.62f, 0.54f), 6f);
            MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.70f, 0.72f, 0.66f), 10f);
            MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.81f, 0.74f), 16f);

            MakeNpc(room, "Dotha_Greybox", new Vector2(-12f, 0f), "Greybox_Dotha", new Color(0.36f, 0.40f, 0.34f));

            MakeSpawn(room, "West", new Vector2(-17f, 0f));
            MakeSpawn(room, "East", new Vector2(17f, 0f));
            MakeTransition(room, "To_A", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f),
                Path.GetFileNameWithoutExtension(RoomAScenePath), "East");
            MakeTransition(room, "To_Lighthouse", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f),
                Path.GetFileNameWithoutExtension(RoomCScenePath), "West");

            EditorSceneManager.SaveScene(scene, RoomBScenePath);
            Debug.Log("[OWSBG] saved " + RoomBScenePath);
        }

        // The fourth lighthouse: a desk, then the Lamp-Keeper's arena behind two doors.
        static void BuildRoomC()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom("Saltmarrow_Lighthouse", new Rect(-16f, -3f, 34f, 17f));

            var floorMat = MakeLitMaterial("M_Greybox_Floor", new Color(0.45f, 0.47f, 0.36f));
            var platMat = MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            var doorMat = MakeLitMaterial("M_Greybox_Door", new Color(0.30f, 0.26f, 0.24f));
            MakeGround(room, "Floor", new Vector2(1f, -0.5f), new Vector2(34f, 1f), floorMat);
            MakeGround(room, "Wall_E", new Vector2(17.5f, 5f), new Vector2(1f, 14f), platMat);
            MakeGround(room, "Lamp_Housing", new Vector2(3f, 10.2f), new Vector2(3f, 0.8f), platMat);
            MakeGround(room, "Ledge_L", new Vector2(-4f, 2.4f), new Vector2(2.5f, 0.5f), platMat);
            MakeGround(room, "Ledge_R", new Vector2(10f, 2.4f), new Vector2(2.5f, 0.5f), platMat);

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.56f, 0.60f, 0.54f), 6f);
            MakePaperLayer(room, "Far_Tower", 8f, 4f, new Color(0.66f, 0.68f, 0.64f), 14f);
            MakePaperLayer(room, "Farther_Sea", 16f, 6f, new Color(0.80f, 0.80f, 0.76f), 16f);

            MakeDesk(room, new Vector2(-11f, 0f));
            MakeSpawn(room, "Start", new Vector2(-12.5f, 0f));
            MakeSpawn(room, "West", new Vector2(-14.5f, 0f));
            MakeTransition(room, "To_B", new Vector2(-15.6f, 4f), new Vector2(0.8f, 10f),
                Path.GetFileNameWithoutExtension(RoomBScenePath), "East");

            // Doors: solid while the fight is on, inactive otherwise.
            var doorW = MakeDoor(room, "Door_W", new Vector2(-6.5f, 3f), new Vector2(1f, 6f), doorMat);
            var doorE = MakeDoor(room, "Door_E", new Vector2(12.5f, 3f), new Vector2(1f, 6f), doorMat);

            // The boss on her perch under the lamp.
            var boss = MakeBoss<LampKeeper>(room, "LampKeeper", new Vector2(3f, 8.6f), new Vector2(1.6f, 1.2f));
            var lk = (LampKeeper)boss;
            lk.floorY = 0f;
            lk.arenaHalfWidth = 9f;
            var bossSo = new SerializedObject(boss);
            bossSo.FindProperty("_maxHealth").intValue = 24;
            bossSo.FindProperty("_contactDamage").intValue = 1;
            bossSo.FindProperty("_hurtstunFrames").intValue = 3;
            bossSo.FindProperty("_bossName").stringValue = "The Lamp-Keeper";
            bossSo.FindProperty("_tier").intValue = 1;
            var lines = bossSo.FindProperty("_phaseLines");
            lines.arraySize = 3;
            lines.GetArrayElementAtIndex(0).stringValue = "The light stays.";
            lines.GetArrayElementAtIndex(1).stringValue = "I remember the light. I remember nothing else.";
            lines.GetArrayElementAtIndex(2).stringValue = "If it goes out, I go with it.";
            bossSo.ApplyModifiedPropertiesWithoutUndo();

            // The arena zone between the doors.
            var arenaGo = new GameObject("Arena_LampKeeper") { layer = LayerMask.NameToLayer("Trigger") };
            arenaGo.transform.SetParent(room.transform, false);
            arenaGo.transform.position = new Vector3(3f, 5f, 0f);
            var zone = arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(17f, 11f);
            var arena = arenaGo.AddComponent<BossArena>();
            var arSo = new SerializedObject(arena);
            arSo.FindProperty("_bossId").stringValue = "lamp_keeper";
            arSo.FindProperty("_boss").objectReferenceValue = boss;
            var doors = arSo.FindProperty("_doors");
            doors.arraySize = 2;
            doors.GetArrayElementAtIndex(0).objectReferenceValue = doorW;
            doors.GetArrayElementAtIndex(1).objectReferenceValue = doorE;
            arSo.FindProperty("_rewardAbility").intValue = (int)Ability.Wingbeat;
            arSo.FindProperty("_vellumScraps").intValue = 1;
            arSo.FindProperty("_beaconVantageId").stringValue = "Saltmarrow_Lighthouse/Lamp";
            arSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, RoomCScenePath);
            Debug.Log("[OWSBG] saved " + RoomCScenePath);
        }

        static GameObject MakeDoor(Room room, string name, Vector2 center, Vector2 size, Material mat)
        {
            MakeGround(room, name, center, size, mat);
            var go = room.transform.Find(name).gameObject;
            go.SetActive(false);
            return go;
        }

        // A boss: kinematic body on the Enemy layer with an ink quad; dormant until its arena starts the fight.
        static Boss MakeBoss<T>(Room room, string name, Vector2 pos, Vector2 size) where T : Boss
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;
            var boss = go.AddComponent<T>();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexPath);
            var mat = MakeInkMaterial("M_Boss_" + typeof(T).Name, tex);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(size.x * 1.6f, size.y * 1.6f, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.TwoSided;
            return boss;
        }

        // The Greyfold's edge (bible 4.6, 7.0): a faded plain that runs into the white. The prologue.
        static void BuildRoomEdge()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom("Greyfold_Edge", new Rect(-16f, -3f, 42f, 17f));

            var floorMat = MakeLitMaterial("M_Greybox_Floor_Edge", new Color(0.62f, 0.62f, 0.56f));
            var platMat = MakeLitMaterial("M_Greybox_Platform_Edge", new Color(0.66f, 0.63f, 0.56f));
            MakeGround(room, "Floor", new Vector2(5f, -0.5f), new Vector2(42f, 1f), floorMat);
            MakeGround(room, "Wall_L", new Vector2(-15.5f, 5f), new Vector2(1f, 14f), platMat);
            MakeGround(room, "Ledge_1", new Vector2(-6f, 2.2f), new Vector2(3f, 0.5f), platMat);
            MakeGround(room, "Ledge_2", new Vector2(0.5f, 3.6f), new Vector2(2.5f, 0.5f), platMat);

            MakePaperLayer(room, "Mid_Edge", 3f, 0f, new Color(0.74f, 0.75f, 0.70f), 6f);
            MakePaperLayer(room, "Far_Cathedral", 8f, 2f, new Color(0.82f, 0.82f, 0.79f), 11f);
            MakePaperLayer(room, "Farther_Edge", 16f, 6f, new Color(0.90f, 0.90f, 0.88f), 16f);
            MakeBlankWhite(room);

            MakeNpc(room, "Isolde", new Vector2(8f, 0f), "Prologue_Edge_Idle", new Color(0.22f, 0.24f, 0.34f));
            MakeVantage(room, "HalfCathedral", "Greyfold_Edge/HalfCathedral", new Vector2(3f, 0f));
            MakeEnemy<Smudge>(room, "Smudge_1", new Vector2(0f, 2.2f), new Vector2(1.1f, 1.1f));
            MakeEnemy<Smudge>(room, "Smudge_2", new Vector2(4.5f, 2.8f), new Vector2(1.1f, 1.1f));
            MakeEnemy<Smudge>(room, "Smudge_3", new Vector2(10f, 2.4f), new Vector2(1.1f, 1.1f));
            var smudges = new[] { room.transform.Find("Smudge_1").gameObject, room.transform.Find("Smudge_2").gameObject, room.transform.Find("Smudge_3").gameObject };
            foreach (var s in smudges) s.SetActive(false);   // dusk releases them

            // Nothing walks into the white until Isolde has.
            var wall = new GameObject("BlankWall") { layer = LayerMask.NameToLayer("Ground") };
            wall.transform.SetParent(room.transform, false);
            wall.transform.position = new Vector3(13.5f, 3f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
            var edgeGo = new GameObject("BlankEdge");
            edgeGo.transform.SetParent(room.transform, false);
            var edge = edgeGo.AddComponent<BlankEdge>();
            edge.StartX = 13f; edge.EndX = 22f;

            MakeSpawn(room, "Start", new Vector2(5.5f, 0f));

            // A fixed shot on the white for Isolde's walk.
            var shotGo = new GameObject("CM Departure");
            shotGo.transform.SetParent(room.transform, false);
            shotGo.transform.position = new Vector3(14f, 3.7f, -18f);
            shotGo.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            var shot = shotGo.AddComponent<CinemachineCamera>();
            var shotLens = shot.Lens; shotLens.FieldOfView = 30f; shotLens.NearClipPlane = 0.3f; shotLens.FarClipPlane = 120f; shot.Lens = shotLens;
            shotGo.SetActive(false);

            var arrive = NewTimeline(CutsceneDir + "/CS_EdgeArrive.playable");
            var arriveFade = AddClip<PaperFadeClip>(arrive.CreateTrack<PlayableTrack>("Fade"), 0.0, 2.0, "paper thins");
            arriveFade.From = 1f; arriveFade.To = 0f; arriveFade.Color = ScreenFade.Paper;
            AddClip<DialogueNodeClip>(arrive.CreateTrack<PlayableTrack>("Talk"), 1.6, 0.2, "Isolde").Node = "Prologue_Edge_Arrive";
            var arriveCs = MakeCutscene(room, "edge_arrive", arrive, null);

            var depart = NewTimeline(CutsceneDir + "/CS_EdgeDeparture.playable");
            var walk = AddClip<ActorMoveClip>(depart.CreateTrack<PlayableTrack>("Isolde"), 0.5, 5.0, "into the white");
            walk.From = new Vector2(8f, 0f); walk.To = new Vector2(24f, 0f);
            walk.Actor.exposedName = "isolde";
            AddClip<PaperFadeClip>(depart.CreateTrack<PlayableTrack>("Hold"), 5.5, 0.5, "beat").From = 0f;
            var departCs = MakeCutscene(room, "edge_departure", depart, shot);
            departCs.SetReference("isolde", room.transform.Find("Isolde"));
            EditorUtility.SetDirty(arrive); EditorUtility.SetDirty(depart);

            var dirGo = new GameObject("PrologueDirector");
            dirGo.transform.SetParent(room.transform, false);
            var director = dirGo.AddComponent<PrologueDirector>();
            var dso = new SerializedObject(director);
            dso.FindProperty("_arrive").objectReferenceValue = arriveCs;
            dso.FindProperty("_vantage").objectReferenceValue = room.transform.Find("Vantage_HalfCathedral").GetComponent<VantagePoint>();
            dso.FindProperty("_isolde").objectReferenceValue = room.transform.Find("Isolde").gameObject;
            var arr = dso.FindProperty("_smudges");
            arr.arraySize = smudges.Length;
            for (int i = 0; i < smudges.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = smudges[i];
            dso.FindProperty("_blankWall").objectReferenceValue = wall;
            dso.FindProperty("_blankEdge").objectReferenceValue = edge;
            dso.FindProperty("_shoreScene").stringValue = Path.GetFileNameWithoutExtension(RoomAScenePath);
            dso.FindProperty("_shoreSpawn").stringValue = "Shore";
            dso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, RoomEdgeScenePath);
            Debug.Log("[OWSBG] saved " + RoomEdgeScenePath);
        }

        // Sheets of paper-white from x = 12 on, nearer ones starting further in, so the plain runs out of ink.
        static void MakeBlankWhite(Room room)
        {
            var mat = MakeLitMaterial("M_Blank_White", new Color(0.97f, 0.96f, 0.93f));
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(mat);
            float[] z = { 0.6f, 4.5f, 10f, 18f };   // between the paper layers, never on one
            float[] x0 = { 16f, 14f, 12f, 10f };
            for (int i = 0; i < z.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Blank_" + i;
                q.layer = LayerMask.NameToLayer("Paper");
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(room.transform, false);
                float w = 40f;
                q.transform.position = new Vector3(x0[i] + w * 0.5f, 6f, z[i]);
                q.transform.localScale = new Vector3(w, 30f, 1f);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ---- cutscene helpers (PRG-16): timeline assets built in code, one Cutscene object per scene beat.

        static TimelineAsset NewTimeline(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.DeleteAsset(path);
            var t = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        static T AddClip<T>(TrackAsset track, double start, double duration, string name) where T : ScriptableObject, IPlayableAsset
        {
            var clip = track.CreateClip<T>();
            clip.start = start;
            clip.duration = duration;
            clip.displayName = name;
            return (T)clip.asset;
        }

        static Cutscene MakeCutscene(Room room, string id, TimelineAsset timeline, CinemachineCamera shot)
        {
            var go = new GameObject("Cutscene_" + id);
            go.transform.SetParent(room.transform, false);
            var director = go.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            var cs = go.AddComponent<Cutscene>();
            var so = new SerializedObject(cs);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_shot").objectReferenceValue = shot;
            so.ApplyModifiedPropertiesWithoutUndo();
            return cs;
        }

        // ---- The look (PRG-04): two full-screen passes on the URP renderer, materials under Art/Materials.

        static void SetupRenderFeatures()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (data == null) { Debug.LogWarning("[OWSBG] renderer asset missing; run Configure first"); return; }
            AddFullScreenFeature(data, "ForegroundBlur", "OWSBG/FullScreen/ForegroundBlur",
                FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing, ScriptableRenderPassInput.Depth, m =>
                {
                    m.SetFloat("_FocusDistance", 18f);   // camera sits 18 units behind the gameplay plane
                    m.SetFloat("_Range", 7f);
                    m.SetFloat("_MaxRadius", 7f);
                });
            AddFullScreenFeature(data, "PaperGrain", "OWSBG/FullScreen/PaperGrain",
                FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing, ScriptableRenderPassInput.None, m =>
                {
                    m.SetFloat("_Strength", 0.07f);
                    m.SetFloat("_Scale", 2f);
                    m.SetFloat("_Fibre", 0.02f);
                });
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[OWSBG] renderer features: " + data.rendererFeatures.Count);
        }

        static void AddFullScreenFeature(UniversalRendererData data, string name, string shaderName,
            FullScreenPassRendererFeature.InjectionPoint point, ScriptableRenderPassInput requirements, System.Action<Material> configure)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning("[OWSBG] shader missing: " + shaderName); return; }
            var matPath = Root + "/Art/Materials/M_FS_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            else if (mat.shader != shader) mat.shader = shader;
            configure?.Invoke(mat);
            EditorUtility.SetDirty(mat);

            FullScreenPassRendererFeature feature = null;
            foreach (var f in data.rendererFeatures) if (f is FullScreenPassRendererFeature fs && fs.name == name) feature = fs;
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = name;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.SaveAssets();
                // Mirror what the renderer inspector does: the feature list and its local-id map, in step.
                var so = new SerializedObject(data);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                int n = list.arraySize;
                list.arraySize = n + 1;
                list.GetArrayElementAtIndex(n).objectReferenceValue = feature;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                map.arraySize = n + 1;
                map.GetArrayElementAtIndex(n).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            feature.passMaterial = mat;
            feature.injectionPoint = point;
            feature.requirements = requirements;
            feature.fetchColorBuffer = true;
            feature.passIndex = 0;
            EditorUtility.SetDirty(feature);
        }

        // ---- Addressables (PRG-07): one group, one bundle per room, address = scene name.

        static readonly string[] RoomScenePaths = { RoomAScenePath, RoomBScenePath, RoomCScenePath, RoomEdgeScenePath };

        static void SetupAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup("Rooms");
            if (group == null)
                group = settings.CreateGroup("Rooms", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;   // neighbours preload one at a time
            foreach (var path in RoomScenePaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) { Debug.LogWarning("[OWSBG] room scene missing for Addressables: " + path); continue; }
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = Path.GetFileNameWithoutExtension(path);
                entry.SetLabel("room", true, true, false);
            }
            // Play mode reads straight from the AssetDatabase: pressing Play needs no content build.
            for (int i = 0; i < settings.DataBuilders.Count; i++)
                if (settings.DataBuilders[i] is BuildScriptFastMode) { settings.ActivePlayModeDataBuilderIndex = i; break; }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log("[OWSBG] Addressables: " + group.entries.Count + " rooms in group Rooms");
        }

        /// <summary>Content build for players (bundles under Library/com.unity.addressables). Play mode does not need it.</summary>
        public static void BuildAddressables()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new System.Exception("[OWSBG] Addressables build failed: " + result.Error);
            Debug.Log("[OWSBG] Addressables built in " + result.Duration.ToString("0.0") + " s: " + result.OutputPath);
        }

        // ---- room helpers

        static Room MakeRoom(string id, Rect cameraBounds)
        {
            var go = new GameObject("Room_" + id);
            var room = go.AddComponent<Room>();
            room.RoomId = id;
            var boundsGo = new GameObject("CameraBounds") { layer = LayerMask.NameToLayer("Trigger") };
            boundsGo.transform.SetParent(go.transform, false);
            var poly = boundsGo.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.SetPath(0, new[]
            {
                new Vector2(cameraBounds.xMin, cameraBounds.yMin), new Vector2(cameraBounds.xMax, cameraBounds.yMin),
                new Vector2(cameraBounds.xMax, cameraBounds.yMax), new Vector2(cameraBounds.xMin, cameraBounds.yMax),
            });
            room.CameraBounds = poly;
            return room;
        }

        static void MakeGround(Room room, string name, Vector2 center, Vector2 size, Material mat)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.layer = LayerMask.NameToLayer("Ground");
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.transform.SetParent(room.transform, false);
            b.transform.position = new Vector3(center.x, center.y, 0f);
            b.transform.localScale = new Vector3(size.x, size.y, 2f);
            b.GetComponent<MeshRenderer>().sharedMaterial = mat;
            b.AddComponent<BoxCollider2D>();   // unit box, scaled by the transform
        }

        // Ground that a plumb weight breaks (combat doc 6). Same shape as MakeGround, own material.
        static void MakeWeakFloor(Room room, string name, Vector2 center, Vector2 size)
        {
            var mat = MakeLitMaterial("M_Greybox_WeakFloor", new Color(0.58f, 0.50f, 0.36f));
            MakeGround(room, name, center, size, mat);
            var go = room.transform.Find(name).gameObject;
            go.AddComponent<WeakFloor>();
        }

        // A platform only a Field lantern draws. Ground layer; collider off until revealed.
        static void MakeHiddenPlatform(Room room, string name, Vector2 center, Vector2 size)
        {
            var mat = MakeLitMaterial("M_Greybox_Hidden", new Color(0.52f, 0.46f, 0.36f));
            MakeGround(room, name, center, size, mat);
            var go = room.transform.Find(name).gameObject;
            go.AddComponent<HiddenPlatform>();
        }

        static void MakePaperLayer(Room room, string name, float z, float y, Color color, float height)
        {
            var mat = MakeLitMaterial("M_Paper_" + name, color);
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(mat);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Paper_" + name;
            q.layer = LayerMask.NameToLayer("Paper");
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(room.transform, false);
            q.transform.position = new Vector3(0f, y + height * 0.5f, z);
            q.transform.localScale = new Vector3(80f, height, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void MakeDummy(Room room, Vector2 pos)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d.name = "TrainingDummy";
            d.layer = LayerMask.NameToLayer("Hittable");
            Object.DestroyImmediate(d.GetComponent<Collider>());
            d.transform.SetParent(room.transform, false);
            d.transform.position = new Vector3(pos.x, pos.y, 0f);
            d.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
            d.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Dummy", new Color(0.75f, 0.35f, 0.30f));
            d.AddComponent<BoxCollider2D>().isTrigger = true;
            d.AddComponent<TrainingDummy>();
        }

        // A greybox enemy: dynamic 2D body on the Enemy layer with an ink-quad visual (Smudge uses _Ink).
        static void MakeEnemy<T>(Room room, string name, Vector2 pos, Vector2 size) where T : Enemy
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            go.AddComponent<T>();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexPath);
            var mat = MakeInkMaterial("M_Enemy_" + typeof(T).Name, tex);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(size.x * 1.3f, size.y * 1.3f, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.TwoSided;
        }

        // A stand-in bird: an ink-tinted quad with a trigger, talkable with up.
        static void MakeNpc(Room room, string name, Vector2 pos, string startNode, Color tint)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            var talker = go.AddComponent<NpcTalker>();
            var so = new SerializedObject(talker);
            so.FindProperty("_startNode").stringValue = startNode;
            so.FindProperty("_prompt").stringValue = "Talk";
            so.ApplyModifiedPropertiesWithoutUndo();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexPath);
            var mat = MakeInkMaterial("M_Npc_" + name, tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(0.9f, 1.4f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.TwoSided;
        }

        // A survey spot: a trigger area plus a thin marker post.
        static void MakeVantage(Room room, string name, string vantageId, Vector2 pos)
        {
            var go = new GameObject("Vantage_" + name) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2f);
            col.offset = new Vector2(0f, 1f);
            var vp = go.AddComponent<VantagePoint>();
            var so = new SerializedObject(vp);
            so.FindProperty("_vantageId").stringValue = vantageId;
            so.FindProperty("_displayName").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Marker";
            Object.DestroyImmediate(post.GetComponent<Collider>());
            post.transform.SetParent(go.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.9f, 0.6f);
            post.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
            post.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Marker", new Color(0.20f, 0.27f, 0.45f));
        }

        // The rest point: a low table and a trigger; also a spawn named "Desk".
        static void MakeDesk(Room room, Vector2 pos)
        {
            var go = new GameObject("DraftingDesk") { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            var desk = go.AddComponent<DraftingDesk>();
            var so = new SerializedObject(desk);
            so.FindProperty("_prompt").stringValue = "Rest";
            so.ApplyModifiedPropertiesWithoutUndo();

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            Object.DestroyImmediate(table.GetComponent<Collider>());
            table.transform.SetParent(go.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.45f, 0.4f);
            table.transform.localScale = new Vector3(1.4f, 0.9f, 0.8f);
            table.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Desk", new Color(0.42f, 0.30f, 0.20f));
            MakeSpawn(room, "Desk", pos);
        }

        // The hub's Commissions board: a paper sheet on a post, and a trigger.
        static void MakeLedger(Room room, string hubId, Vector2 pos)
        {
            var go = new GameObject("Ledger_" + hubId) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);
            var ledger = go.AddComponent<CommissionLedger>();
            var so = new SerializedObject(ledger);
            so.FindProperty("_hubId").stringValue = hubId;
            so.FindProperty("_prompt").stringValue = "Read";
            so.ApplyModifiedPropertiesWithoutUndo();

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            Object.DestroyImmediate(post.GetComponent<Collider>());
            post.transform.SetParent(go.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.9f, 0.5f);
            post.transform.localScale = new Vector3(0.12f, 1.8f, 0.12f);
            post.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Desk", new Color(0.42f, 0.30f, 0.20f));
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "Sheet";
            Object.DestroyImmediate(sheet.GetComponent<Collider>());
            sheet.transform.SetParent(go.transform, false);
            sheet.transform.localPosition = new Vector3(0f, 1.25f, 0.42f);
            sheet.transform.localScale = new Vector3(0.9f, 1.1f, 0.05f);
            sheet.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Ledger", new Color(0.90f, 0.85f, 0.70f));
        }

        static void MakeSpawn(Room room, string name, Vector2 pos)
        {
            var s = new GameObject("Spawn_" + name);
            s.transform.SetParent(room.transform, false);
            s.transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        static void MakeTransition(Room room, string name, Vector2 center, Vector2 size, string targetScene, string targetSpawn)
        {
            var t = new GameObject("Transition_" + name) { layer = LayerMask.NameToLayer("Trigger") };
            t.transform.SetParent(room.transform, false);
            t.transform.position = new Vector3(center.x, center.y, 0f);
            var col = t.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            var tr = t.AddComponent<RoomTransition>();
            tr.TargetScene = targetScene;
            tr.TargetSpawn = targetSpawn;
        }

        static VolumeProfile GetOrCreateVolumeProfile()
        {
            var profilePath = Root + "/Settings/Rendering/PP_Default.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null) return profile;
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
            return profile;
        }

        // ------------------------------------------------------------------ 3
        public static void CaptureScreenshot()
        {
            // OWSBG_SHOT_ROOM=A|B|C picks the room; OWSBG_SHOT_FOCUS="x,y" the point to frame (defaults: A, Wren).
            var roomKey = System.Environment.GetEnvironmentVariable("OWSBG_SHOT_ROOM") ?? "A";
            var roomPath = roomKey == "B" ? RoomBScenePath : roomKey == "C" ? RoomCScenePath : roomKey == "E" ? RoomEdgeScenePath : RoomAScenePath;
            EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(roomPath, OpenSceneMode.Additive);
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[OWSBG] no main camera"); return; }
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
            var wren = Object.FindFirstObjectByType<WrenController>();
            var focus = wren != null ? wren.transform.position : Vector3.zero;
            var focusEnv = System.Environment.GetEnvironmentVariable("OWSBG_SHOT_FOCUS");
            if (!string.IsNullOrEmpty(focusEnv))
            {
                var parts = focusEnv.Split(',');
                if (parts.Length == 2 && float.TryParse(parts[0], out var fx) && float.TryParse(parts[1], out var fy)) focus = new Vector3(fx, fy, 0f);
            }
            cam.transform.position = new Vector3(focus.x + 2f, focus.y + 2.5f, -18f);
            cam.transform.rotation = Quaternion.Euler(2f, 0f, 0f);

            const int w = 1280, h = 720;
            ShaderUtil.allowAsyncCompilation = false;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
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

        // ---- materials

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

        static Material MakeInkMaterial(string name, Texture2D tex)
        {
            var path = Root + "/Art/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("OWSBG/InkSprite");
            if (shader == null)
            {
                Debug.LogWarning("[OWSBG] InkSprite shader not found; falling back to URP Lit");
                mat = MakeLitMaterial(name, Color.white);
                mat.SetTexture("_BaseMap", tex);
                mat.SetFloat("_AlphaClip", 1f);
                mat.EnableKeyword("_ALPHATEST_ON");
                return mat;
            }
            mat = new Material(shader);
            mat.SetTexture("_BaseMap", tex);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
