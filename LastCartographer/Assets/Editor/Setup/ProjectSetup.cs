using System.Collections.Generic;
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
        public const string RoomChapelScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_Chapel.unity";
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
            BuildRoomChapel();
            BuildRoomEdge();
            foreach (var recipe in SaltmarrowRecipes()) BuildRecipe(recipe);
            PlacementSetup.Place();   // the coast's readables and askers stand in the rebuilt rooms
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
            uiGo.AddComponent<OWSBG.UI.AtlasView>();
            uiGo.AddComponent<OWSBG.UI.ShopView>();
            uiGo.AddComponent<OWSBG.UI.WalkView>();
            uiGo.AddComponent<OWSBG.UI.PromptView>();
            uiGo.AddComponent<OWSBG.UI.FadeView>();
            uiGo.AddComponent<OWSBG.UI.OptionsView>();

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
            sysGo.AddComponent<DayCycle>();
            sysGo.AddComponent<IrisSeedDrops>();
            sysGo.AddComponent<MemoryDrops>();
            sysGo.AddComponent<EndingsRunner>();

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
            // The shipped Wren starts with the kit; Sable sells the rest (DES-05). Tests keep unlock-all on their own belts.
            var beltSo = new SerializedObject(belt);
            beltSo.FindProperty("_unlockAllForGreybox").boolValue = false;
            beltSo.FindProperty("_starterKit").boolValue = true;
            beltSo.ApplyModifiedPropertiesWithoutUndo();
            belt.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            belt.groundMask = LayerMask.GetMask("Ground");
            go.AddComponent<CharterSet>();   // after the components it drives; profiles default in Awake
            go.AddComponent<ClarityMeter>(); // the controller adds it at runtime too, for scenes built before PRG-18

            // Visual: an InkSprite quad. With her sheets (CHR-03) it is a 2 x 2 unit frame window with her feet at the
            // origin; without them, the 1.2-unit placeholder.
            var sheets = LoadSheets("Wren");
            var tex = sheets != null ? sheets.Find(c => c.Name == "idle").Sheet : AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexPath);
            var mat = MakeInkMaterial("M_Wren_Ink", tex);
            if (mat.GetTexture("_BaseMap") != tex) { mat.SetTexture("_BaseMap", tex); EditorUtility.SetDirty(mat); }
            // The material itself shows the idle strip's first frame, so she reads in the editor and in edit-mode
            // captures; in play the sheet player windows the frames through the property block.
            var idleFrames = sheets != null ? Mathf.Max(1, sheets.Find(c => c.Name == "idle").Frames) : 1;
            if (mat.GetTextureScale("_BaseMap").x != 1f / idleFrames) { mat.SetTextureScale("_BaseMap", new Vector2(1f / idleFrames, 1f)); mat.SetTextureOffset("_BaseMap", Vector2.zero); EditorUtility.SetDirty(mat); }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = sheets != null ? new Vector3(2f, 2f, 1f) : new Vector3(0.8f, 1.2f, 1f);
            quad.transform.localPosition = sheets != null ? new Vector3(0f, 1f, 0f) : new Vector3(0f, 0.6f, 0f);
            var qr = quad.GetComponent<MeshRenderer>();
            qr.sharedMaterial = mat;
            qr.shadowCastingMode = ShadowCastingMode.TwoSided;
            if (sheets != null)
            {
                go.AddComponent<InkSheetPlayer>().Configure(qr, sheets);
                go.AddComponent<WrenAnimator>();
            }

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
            MakeGround(room, "Stilt_1", new Vector2(11f, 1.5f), new Vector2(0.4f, 3f), platMat);
            MakeWeakFloor(room, "WeakFloor_1", new Vector2(-9f, 4.6f), new Vector2(3f, 0.5f));
            MakeHiddenPlatform(room, "Hidden_1", new Vector2(16f, 3.2f), new Vector2(3f, 0.5f));

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.62f, 0.64f, 0.52f), 6f);
            MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.72f, 0.72f, 0.64f), 10f);
            MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.80f, 0.72f), 16f);
            MakePaperLayer(room, "Fore_Reeds", -4f, -0.8f, new Color(0.30f, 0.33f, 0.24f), 1.6f);   // peeks over the walkway
            SkinGrounds(room, "Ground_Boardwalk", "Floor", "Platform_A", "Platform_B", "Platform_C", "Stilt_1");

            MakeDummy(room, new Vector2(4f, 0.6f));
            MakeSeeds(room, new Vector2(7f, 5.9f), 2);   // on the high platform, past the crab
            MakeNpc(room, "Sable_Greybox", new Vector2(-9.5f, 0f), "Quay_Sable", new Color(0.16f, 0.18f, 0.22f));
            var sable = MakeSchedule(room, "Sable_Greybox");
            sable.AddPost(DayPhase.Dawn, new Vector2(-9.5f, 0f), "", "mending nets", 1);
            sable.AddPost(DayPhase.Day, new Vector2(-9.5f, 0f), "", "mending nets", 1);
            sable.AddPost(DayPhase.Dusk, new Vector2(-8.2f, 0f), "", "reading the ledger", 1);
            sable.AddPost(DayPhase.Night, new Vector2(-12.2f, 0f), "Quay_Sable_Night", "asleep under the stilts", -1);
            MakeVantage(room, "Reedmother", "Saltmarrow_A/Reedmother", new Vector2(14f, 0f));
            MakeDesk(room, new Vector2(-4.5f, 0f));
            MakeLedger(room, "Saltmarrow", new Vector2(-7f, 0f));
            MakeEnemy<MarshCrab>(room, "Crab_1", new Vector2(7f, 5.9f), new Vector2(0.9f, 0.7f));
            MakeEnemy<ReedSkimmer>(room, "Skimmer_1", new Vector2(9f, 4f), new Vector2(0.9f, 0.5f));
            MakeEnemy<Smudge>(room, "Smudge_1", new Vector2(-15.5f, 4.5f), new Vector2(1.1f, 1.1f));
            MakeHeldState(room, new Vector2(2f, 0.9f), new Vector2(12f, 0.9f));

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

            MakeTransition(room, "To_Shore", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Shore"), "East");
            MakeTransition(room, "To_Stilts", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Stilts"), "West");
            MakeFadeGroup(room);

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
            SkinGrounds(room, "Ground_Boardwalk", "Floor", "Step_1", "Step_2", "Shaft_L", "Shaft_R", "Shaft_Top");

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.58f, 0.62f, 0.54f), 6f);
            MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.70f, 0.72f, 0.66f), 10f);
            MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.81f, 0.74f), 16f);

            MakeNpc(room, "Dotha_Greybox", new Vector2(-12f, 0f), "Merrow_Dotha", new Color(0.36f, 0.40f, 0.34f));
            var dotha = MakeSchedule(room, "Dotha_Greybox");
            dotha.AddPost(DayPhase.Dawn, new Vector2(-12f, 0f), "", "on her stoop", 1);
            dotha.AddPost(DayPhase.Day, new Vector2(-12f, 0f), "", "on her stoop", 1);
            dotha.AddPost(DayPhase.Dusk, new Vector2(0.5f, 0f), "Merrow_Dotha_Water", "singing to the water", 1);
            dotha.AddPost(DayPhase.Night, new Vector2(-12f, 0f), "Merrow_Dotha_Night", "asleep", -1);
            // The Merrow's End bounds-walk (DES-13): four bounds, two verses, Dotha the chorus.
            var walk = MakeBoundsWalk(room, "merrows_end", "saltmarrow.dotha.decided", 3, dotha);
            walk.AddVerse("the stoop and the post",
                ("Dotha's stoop", new Vector2(-12f, 0f)), ("the tether-post", new Vector2(2f, 0f)),
                ("the shaft's foot", new Vector2(12.5f, 0f)), ("the first step", new Vector2(-8f, 1.8f)));
            walk.AddVerse("back by the steps",
                ("the second step", new Vector2(-3f, 3.8f)), ("the first step", new Vector2(-8f, 1.8f)),
                ("the tether-post", new Vector2(2f, 0f)), ("Dotha's stoop", new Vector2(-12f, 0f)));
            MakeBoundMarkers(room, walk);
            MakeVantage(room, "Tetherpost", "Saltmarrow_B/Tetherpost", new Vector2(2f, 0f));
            // A Cantor over the east end: its bell erases Merrow's End until the tether-post is drawn again.
            MakeEnemy<Cantor>(room, "Cantor_1", new Vector2(13f, 2.6f), new Vector2(0.8f, 0.9f));
            MakeHeldState(room, new Vector2(-5f, 0.9f), new Vector2(14f, 0.9f));

            MakeSpawn(room, "West", new Vector2(-17f, 0f));
            MakeSpawn(room, "East", new Vector2(17f, 0f));
            MakeTransition(room, "To_Boardwalk", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Boardwalk"), "East");
            MakeTransition(room, "To_Tetherline", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Tetherline"), "West");
            MakeFadeGroup(room);

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
            MakeGround(room, "Lamp_Housing", new Vector2(3f, 10.2f), new Vector2(3f, 0.8f), platMat);
            MakeGround(room, "Ledge_L", new Vector2(-4f, 2.4f), new Vector2(2.5f, 0.5f), platMat);
            MakeGround(room, "Ledge_R", new Vector2(10f, 2.4f), new Vector2(2.5f, 0.5f), platMat);
            SkinGrounds(room, "Ground_Stone", "Floor", "Lamp_Housing", "Ledge_L", "Ledge_R");

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.56f, 0.60f, 0.54f), 6f);
            MakePaperLayer(room, "Far_Tower", 8f, 4f, new Color(0.66f, 0.68f, 0.64f), 14f);
            MakePaperLayer(room, "Farther_Sea", 16f, 6f, new Color(0.80f, 0.80f, 0.76f), 16f);

            MakeDesk(room, new Vector2(-11f, 0f));
            // The lamp itself, lit once the Lamp-Keeper is beaten (her beacon vantage): a travel point.
            MakeLamp(room, "Lamp", new Vector2(3f, 0f), "lamp.Saltmarrow_Lighthouse", "the fourth lamp", "Saltmarrow_Lighthouse/Lamp");
            // The lamp's inscription (NAR-04): readable with up, on the same trigger as the travel point.
            var lampTalker = room.transform.Find("Lamp_Lamp").gameObject.AddComponent<NpcTalker>();
            var ltSo = new SerializedObject(lampTalker);
            ltSo.FindProperty("_startNode").stringValue = "Lighthouse_Lamp";
            ltSo.FindProperty("_prompt").stringValue = "Read";
            ltSo.FindProperty("_faceWren").boolValue = false;
            ltSo.ApplyModifiedPropertiesWithoutUndo();
            MakeSpawn(room, "Start", new Vector2(-12.5f, 0f));
            MakeSpawn(room, "West", new Vector2(-14.5f, 0f));
            MakeTransition(room, "To_Chain_3", new Vector2(-15.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Chain_3"), "East");
            MakeSpawn(room, "East", new Vector2(15.5f, 0f));
            MakeTransition(room, "To_Chapel", new Vector2(17.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Chapel"), "West");

            // Doors: solid while the fight is on, inactive otherwise.
            var doorW = MakeDoor(room, "Door_W", new Vector2(-6.5f, 3f), new Vector2(1f, 6f), doorMat);
            var doorE = MakeDoor(room, "Door_E", new Vector2(12.5f, 3f), new Vector2(1f, 6f), doorMat);

            // The boss on her perch under the lamp.
            var boss = MakeBoss<LampKeeper>(room, "LampKeeper", new Vector2(3f, 8.6f), new Vector2(1.6f, 1.2f));
            var lk = (LampKeeper)boss;
            lk.floorY = 0f;
            lk.arenaHalfWidth = 9f;
            var bossSo = new SerializedObject(boss);
            bossSo.FindProperty("_maxHealth").intValue = Tuning.BossHealth("lamp_keeper");   // CMB-19
            bossSo.FindProperty("_contactDamage").intValue = 1;
            bossSo.FindProperty("_hurtstunFrames").intValue = 3;
            // Name, tier and the three lines come from her sheet (NAR-06, docs/story/boss-sheets.md).
            var sheet = Bosses.Find("lamp_keeper");
            bossSo.FindProperty("_bossName").stringValue = sheet.Name;
            bossSo.FindProperty("_tier").intValue = sheet.Tier;
            var lines = bossSo.FindProperty("_phaseLines");
            lines.arraySize = 3;
            for (int i = 0; i < 3; i++) lines.GetArrayElementAtIndex(i).stringValue = sheet.Lines[i];
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

            // Staging (PRG-06, PRG-16): a fixed camera on the whole arena for the fight, and a first-entry
            // intro that pushes in on the perch while the doors are already shut.
            var arenaCam = MakeShot(room, "CM Arena", new Vector3(3f, 4.2f, -21f));
            var introShot = MakeShot(room, "CM BossIntro", new Vector3(3f, 7.4f, -11f));
            var intro = NewTimeline(CutsceneDir + "/CS_LampKeeperIntro.playable");
            AddClip<PaperFadeClip>(intro.CreateTrack<PlayableTrack>("Hold"), 0.0, 2.2, "on the perch").From = 0f;
            var introCs = MakeCutscene(room, "lamp_keeper_intro", intro, introShot);
            EditorUtility.SetDirty(intro);
            arSo.FindProperty("_introCutscene").objectReferenceValue = introCs;
            arSo.FindProperty("_arenaCamera").objectReferenceValue = arenaCam;
            arSo.ApplyModifiedPropertiesWithoutUndo();

            // Halvard's first hunt (NAR-04, bible 6.3): he walks in from the west once the lamp is lit,
            // measures her over a conversation, and walks out. The heron waits inactive until then.
            MakeNpc(room, "Halvard", new Vector2(-15f, 0f), "Lighthouse_Halvard_Idle", new Color(0.55f, 0.50f, 0.36f));
            var halvardGo = room.transform.Find("Halvard").gameObject;
            var huntShot = MakeShot(room, "CM Halvard", new Vector3(-10f, 3f, -14f));
            var hunt = NewTimeline(CutsceneDir + "/CS_HalvardHunt.playable");
            var walkIn = AddClip<ActorMoveClip>(hunt.CreateTrack<PlayableTrack>("Halvard"), 0.3, 3.0, "three paces");
            walkIn.From = new Vector2(-15f, 0f); walkIn.To = new Vector2(-9f, 0f);
            walkIn.Actor.exposedName = "halvard";
            AddClip<DialogueNodeClip>(hunt.CreateTrack<PlayableTrack>("Talk"), 3.4, 0.2, "the count").Node = "Lighthouse_Halvard_Hunt";
            var walkOut = AddClip<ActorMoveClip>(hunt.CreateTrack<PlayableTrack>("Halvard out"), 3.8, 3.0, "the reeds carry it");
            walkOut.From = new Vector2(-9f, 0f); walkOut.To = new Vector2(-16f, 0f);
            walkOut.Actor.exposedName = "halvard";
            var huntCs = MakeCutscene(room, "halvard_hunt", hunt, huntShot);
            huntCs.SetReference("halvard", halvardGo.transform);
            EditorUtility.SetDirty(hunt);
            var huntDir = huntCs.gameObject.AddComponent<HalvardHunt>();
            var hdSo = new SerializedObject(huntDir);
            hdSo.FindProperty("_cutscene").objectReferenceValue = huntCs;
            hdSo.FindProperty("_halvard").objectReferenceValue = halvardGo;
            hdSo.ApplyModifiedPropertiesWithoutUndo();
            halvardGo.SetActive(false);
            MakeFadeGroup(room);

            EditorSceneManager.SaveScene(scene, RoomCScenePath);
            Debug.Log("[OWSBG] saved " + RoomCScenePath);
        }

        // The Salt Chapel (DES-08, CMB-12): the tide gap (a jump and a Wingbeat; the coast's soft gate), a desk on
        // the near bank, and Halvard's first fight behind two doors. The altar vantage stands past the arena.
        static void BuildRoomChapel()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom("Saltmarrow_Chapel", new Rect(-20f, -3f, 40f, 17f));

            var floorMat = MakeLitMaterial("M_Greybox_Floor", new Color(0.45f, 0.47f, 0.36f));
            var platMat = MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            var doorMat = MakeLitMaterial("M_Greybox_Door", new Color(0.30f, 0.26f, 0.24f));
            MakeGround(room, "Bank_W", new Vector2(-16f, -0.5f), new Vector2(8f, 1f), floorMat);      // -20..-12
            MakeGround(room, "Floor", new Vector2(8.5f, -0.5f), new Vector2(23f, 1f), floorMat);       // -3..20
            MakeGround(room, "Wall_E", new Vector2(19.5f, 5f), new Vector2(1f, 14f), platMat);
            MakeGround(room, "Altar", new Vector2(17f, 0.8f), new Vector2(2.4f, 1.6f), platMat);
            SkinGrounds(room, "Ground_Stone", "Bank_W", "Floor", "Wall_E", "Altar");
            MakeTide(room, "Tide", new Vector2(-7.5f, -3f), new Vector2(9f, 3f), new Vector2(-12.6f, 0f), new Vector2(-2.4f, 0f));

            MakePaperLayer(room, "Mid_Salt", 3f, 0f, new Color(0.62f, 0.62f, 0.58f), 6f);
            MakePaperLayer(room, "Far_Chapel", 8f, 4f, new Color(0.70f, 0.70f, 0.66f), 14f);
            MakePaperLayer(room, "Farther_Sea", 16f, 6f, new Color(0.80f, 0.80f, 0.76f), 16f);

            MakeDesk(room, new Vector2(-15f, 0f));
            // A skimmer over the gap: the pogo across is the soft break (world-map.md §2).
            MakeEnemy<ReedSkimmer>(room, "Skimmer_Gap", new Vector2(-7.5f, 3f), new Vector2(0.9f, 0.5f));
            MakeSpawn(room, "Start", new Vector2(-17f, 0f));
            MakeSpawn(room, "West", new Vector2(-17.5f, 0f));
            MakeTransition(room, "To_Lighthouse", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), Scene("Saltmarrow_Lighthouse"), "East");

            var doorW = MakeDoor(room, "Door_W", new Vector2(-1f, 3f), new Vector2(1f, 6f), doorMat);
            var doorE = MakeDoor(room, "Door_E", new Vector2(15f, 3f), new Vector2(1f, 6f), doorMat);

            var boss = MakeBoss<Halvard>(room, "Halvard", new Vector2(11f, 0.9f), new Vector2(0.8f, 1.8f));
            var hv = (Halvard)boss;
            hv.floorY = 0f;
            hv.arenaMinX = 0.5f; hv.arenaMaxX = 13.5f;
            var bossSo = new SerializedObject(boss);
            bossSo.FindProperty("_maxHealth").intValue = Tuning.BossHealth("halvard");   // CMB-19
            bossSo.FindProperty("_contactDamage").intValue = 1;
            bossSo.FindProperty("_hurtstunFrames").intValue = 2;
            var sheet = Bosses.Find("halvard");
            bossSo.FindProperty("_bossName").stringValue = sheet.Name;
            bossSo.FindProperty("_tier").intValue = sheet.Tier;
            var lines = bossSo.FindProperty("_phaseLines");
            lines.arraySize = 3;
            for (int i = 0; i < 3; i++) lines.GetArrayElementAtIndex(i).stringValue = sheet.Lines[i];
            bossSo.ApplyModifiedPropertiesWithoutUndo();

            var arenaGo = new GameObject("Arena_Halvard") { layer = LayerMask.NameToLayer("Trigger") };
            arenaGo.transform.SetParent(room.transform, false);
            arenaGo.transform.position = new Vector3(7f, 5f, 0f);
            var zone = arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(16f, 11f);
            var arena = arenaGo.AddComponent<BossArena>();
            var arSo = new SerializedObject(arena);
            arSo.FindProperty("_bossId").stringValue = "halvard";
            arSo.FindProperty("_boss").objectReferenceValue = boss;
            var doors = arSo.FindProperty("_doors");
            doors.arraySize = 2;
            doors.GetArrayElementAtIndex(0).objectReferenceValue = doorW;
            doors.GetArrayElementAtIndex(1).objectReferenceValue = doorE;
            arSo.FindProperty("_rewardAbility").intValue = (int)Ability.None;
            arSo.FindProperty("_vellumScraps").intValue = 1;
            arSo.FindProperty("_arenaCamera").objectReferenceValue = MakeShot(room, "CM Arena", new Vector3(7f, 4.2f, -21f));
            arSo.ApplyModifiedPropertiesWithoutUndo();

            MakeVantage(room, "Altar", "Saltmarrow_Chapel/Altar", new Vector2(17f, 1.6f));
            MakeFadeGroup(room);

            EditorSceneManager.SaveScene(scene, RoomChapelScenePath);
            Debug.Log("[OWSBG] saved " + RoomChapelScenePath);
        }

        // Water under a gap (Tide): a trigger that costs a mask and returns Wren to the nearer bank; a pale quad shows it.
        static void MakeTide(Room room, string name, Vector2 center, Vector2 size, Vector2 westBank, Vector2 eastBank)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var tide = go.AddComponent<Tide>();
            var so = new SerializedObject(tide);
            so.FindProperty("_westBank").vector2Value = westBank;
            so.FindProperty("_eastBank").vector2Value = eastBank;
            so.ApplyModifiedPropertiesWithoutUndo();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Water";
            q.layer = LayerMask.NameToLayer("Paper");
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(room.transform, false);
            q.transform.position = new Vector3(center.x, center.y + size.y * 0.5f + 0.1f, 0.4f);   // surface just under the bank
            q.transform.localScale = new Vector3(size.x, 1.6f, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = MakeLitMaterial("M_Greybox_Water", new Color(0.62f, 0.70f, 0.72f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
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
            var shot = MakeShot(room, "CM Departure", new Vector3(14f, 3.7f, -18f));

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
            MakeFadeGroup(room);

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

        // A fixed Cinemachine shot, inactive until something raises it (a cutscene, a boss arena).
        static CinemachineCamera MakeShot(Room room, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            var cam = go.AddComponent<CinemachineCamera>();
            var lens = cam.Lens; lens.FieldOfView = 30f; lens.NearClipPlane = 0.3f; lens.FarClipPlane = 120f; cam.Lens = lens;
            go.SetActive(false);
            return cam;
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

        static IEnumerable<string> RoomScenePaths => AllRoomScenePaths();

        // ---- Recipe rooms (DES-08, docs/design/saltmarrow-rooms.md) -------------------------------------
        // The slice beyond the four hand-built rooms is data: geometry, exits, vantages and enemies per room.
        sealed class RoomRecipe
        {
            public string Id;
            public Rect Bounds = new Rect(-20f, -3f, 40f, 17f);
            public bool Faded, SeaWest;
            public readonly List<(string name, Vector2 c, Vector2 s)> Ground = new List<(string, Vector2, Vector2)>();
            public readonly List<(string name, Vector2 pos)> Spawns = new List<(string, Vector2)>();
            public readonly List<(string name, Vector2 c, Vector2 s, string target, string spawn)> Exits = new List<(string, Vector2, Vector2, string, string)>();
            public readonly List<(string name, Vector2 pos)> Vantages = new List<(string, Vector2)>();
            public readonly List<(System.Type type, string name, Vector2 pos, Vector2 size)> Enemies = new List<(System.Type, string, Vector2, Vector2)>();
            public readonly List<(Vector2 pos, int n)> Seeds = new List<(Vector2, int)>();

            public RoomRecipe(string id) { Id = id; }
            public RoomRecipe Tall() { Bounds = new Rect(-20f, -3f, 40f, 19f); return this; }
            public RoomRecipe Floor(float x0, float x1) { Ground.Add(("Floor_" + Ground.Count, new Vector2((x0 + x1) * 0.5f, -0.5f), new Vector2(x1 - x0, 1f))); return this; }
            public RoomRecipe Shallows(float x0, float x1) { Ground.Add(("Shallows_" + Ground.Count, new Vector2((x0 + x1) * 0.5f, -3f), new Vector2(x1 - x0, 1f))); return this; }
            public RoomRecipe Plat(float x, float y, float w) { Ground.Add(("Plat_" + Ground.Count, new Vector2(x, y), new Vector2(w, 0.6f))); return this; }
            public RoomRecipe West(string target) { Spawns.Add(("West", new Vector2(-17f, 0f))); Exits.Add(("To_W", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), target, "East")); return this; }
            public RoomRecipe East(string target) { Spawns.Add(("East", new Vector2(17f, 0f))); Exits.Add(("To_E", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), target, "West")); return this; }
            /// <summary>An exit above the top platform (its centre x, its top y); arrives at the target's Bottom.</summary>
            public RoomRecipe Up(string target, float x, float topY) { Spawns.Add(("Top", new Vector2(x, topY + 0.05f))); Exits.Add(("To_Up", new Vector2(x, topY + 2.2f), new Vector2(4f, 0.8f), target, "Bottom")); return this; }
            /// <summary>A drop through the floor gap centred on x; arrives at the target's Top. The floor must leave the gap.</summary>
            public RoomRecipe Down(string target, float x) { Spawns.Add(("Bottom", new Vector2(x + 3.5f, 0f))); Exits.Add(("To_Down", new Vector2(x, -2.4f), new Vector2(4f, 0.8f), target, "Top")); return this; }
            public RoomRecipe Vantage(string name, float x, float y) { Vantages.Add((name, new Vector2(x, y))); return this; }
            public RoomRecipe Crab(float x) { Enemies.Add((typeof(MarshCrab), "Crab_" + Enemies.Count, new Vector2(x, 0.6f), new Vector2(0.9f, 0.7f))); return this; }
            public RoomRecipe Crab(float x, float y) { Enemies.Add((typeof(MarshCrab), "Crab_" + Enemies.Count, new Vector2(x, y), new Vector2(0.9f, 0.7f))); return this; }
            public RoomRecipe Skimmer(float x, float y) { Enemies.Add((typeof(ReedSkimmer), "Skimmer_" + Enemies.Count, new Vector2(x, y), new Vector2(0.9f, 0.5f))); return this; }
            public RoomRecipe Smudge(float x) { Enemies.Add((typeof(Smudge), "Smudge_" + Enemies.Count, new Vector2(x, 1.5f), new Vector2(1.1f, 1.1f))); return this; }
            public RoomRecipe Seed(float x, float y, int n) { Seeds.Add((new Vector2(x, y), n)); return this; }
            public RoomRecipe Cantor(float x) { Enemies.Add((typeof(Cantor), "Cantor_" + Enemies.Count, new Vector2(x, 2.6f), new Vector2(0.8f, 0.9f))); return this; }
        }

        static string Scene(string id) => "Greybox_" + id;
        static string RoomPath(string id) => Root + "/Scenes/Greybox/" + Scene(id) + ".unity";

        static List<RoomRecipe> SaltmarrowRecipes()
        {
            const string A = "Saltmarrow_A", B = "Saltmarrow_B", C = "Saltmarrow_Lighthouse";
            return new List<RoomRecipe>
            {
                new RoomRecipe("Saltmarrow_Shore") { SeaWest = true }
                    .Floor(-20f, 20f).Plat(-8f, 0.6f, 2.4f).Vantage("Tideline", -8f, 0.9f)
                    .Crab(6f).Smudge(-3f).Seed(-12f, 0.5f, 2)
                    .East(Scene(A)),
                new RoomRecipe("Saltmarrow_Stilts").Tall()
                    .Floor(-20f, 20f).Plat(-10f, 2.5f, 3f).Plat(-5f, 5f, 3f).Plat(0f, 7.5f, 3f).Plat(5f, 10f, 3f).Plat(0f, 12f, 4f)
                    .Skimmer(-5f, 7f).Skimmer(6f, 12.5f).Crab(10f)
                    .West(Scene(A)).East(Scene("Saltmarrow_Boardwalk")).Up(Scene("Saltmarrow_Roots_1"), 0f, 12.3f),
                new RoomRecipe("Saltmarrow_Boardwalk")
                    .Floor(-20f, -6f).Floor(-2f, 8f).Floor(12f, 20f).Shallows(-6f, -2f).Shallows(8f, 12f)
                    .Crab(3f).Crab(15f).Smudge(-12f).Seed(-4f, -2f, 3).Seed(10f, -2f, 2)
                    .West(Scene("Saltmarrow_Stilts")).East(Scene(B)),
                new RoomRecipe("Saltmarrow_Tetherline")
                    .Floor(-20f, 20f).Plat(-8f, 3f, 2.5f).Plat(0f, 4f, 2.5f).Plat(8f, 3f, 2.5f)
                    .Skimmer(-8f, 5f).Skimmer(8f, 5f).Crab(0f)
                    .West(Scene(B)).East(Scene("Saltmarrow_Ferry")),
                new RoomRecipe("Saltmarrow_Ferry")
                    .Floor(-20f, 20f).Plat(4f, 2.5f, 3f).Plat(9f, 4.5f, 3f)
                    .Smudge(-6f).Smudge(6f)
                    .West(Scene("Saltmarrow_Tetherline")).East(Scene("Saltmarrow_Chain_1")),
                new RoomRecipe("Saltmarrow_Chain_1").Tall()
                    .Floor(-20f, 20f).Plat(-6f, 2.5f, 3f).Plat(-1f, 5f, 3f).Plat(4f, 7.5f, 3f).Plat(0f, 10f, 5f).Vantage("FirstLamp", 0f, 10.3f)
                    .Skimmer(2f, 6f).Crab(-12f)
                    .West(Scene("Saltmarrow_Ferry")).East(Scene("Saltmarrow_Chain_2")),
                new RoomRecipe("Saltmarrow_Chain_2").Tall()
                    .Floor(-20f, 20f).Plat(6f, 2.5f, 3f).Plat(1f, 5f, 3f).Plat(-4f, 7.5f, 3f).Plat(0f, 10f, 5f).Vantage("SecondLamp", 0f, 10.3f)
                    .Cantor(12f).Crab(-10f)
                    .West(Scene("Saltmarrow_Chain_1")).East(Scene("Saltmarrow_Chain_3")),
                new RoomRecipe("Saltmarrow_Chain_3") { Faded = true }
                    .Floor(-20f, 20f).Plat(-4f, 2.5f, 3f).Plat(2f, 4.5f, 3f)
                    .Smudge(-8f).Smudge(8f).Seed(2f, 5.3f, 3)
                    .West(Scene("Saltmarrow_Chain_2")).East(Scene(C)),
                new RoomRecipe("Saltmarrow_Roots_1")
                    .Floor(-20f, -2f).Floor(2f, 20f).Plat(8f, 3f, 3f).Plat(13f, 6f, 3f)
                    .Crab(-10f).Skimmer(8f, 5f)
                    .Down(Scene("Saltmarrow_Stilts"), 0f).East(Scene("Saltmarrow_Roots_2")),
                new RoomRecipe("Saltmarrow_Roots_2").Tall()
                    .Floor(-20f, 20f).Vantage("Bole", -8f, 0f).Plat(-4f, 3f, 3f).Plat(1f, 5.5f, 3f).Plat(6f, 8f, 3f).Plat(2f, 10.5f, 4f)
                    .Skimmer(4f, 8.5f).Crab(10f)
                    .West(Scene("Saltmarrow_Roots_1")).Up(Scene("Saltmarrow_Roots_3"), 2f, 10.8f),
                new RoomRecipe("Saltmarrow_Roots_3")
                    .Floor(-20f, 0f).Floor(4f, 20f).Plat(-8f, 3f, 3f).Plat(12f, 3f, 3f)
                    .Smudge(-8f).Skimmer(10f, 4f)
                    .Down(Scene("Saltmarrow_Roots_2"), 2f).East(Scene("Saltmarrow_Roots_4")),
                new RoomRecipe("Saltmarrow_Roots_4")
                    .Floor(-20f, 20f).Plat(0f, 3f, 3f).Plat(5f, 6f, 3f).Plat(10f, 9f, 4f).Vantage("Crown", 10f, 9.3f)
                    .Crab(-6f).Skimmer(5f, 8f).Seed(12f, 9.8f, 5)
                    .West(Scene("Saltmarrow_Roots_3")),
            };
        }

        static void BuildRecipe(RoomRecipe r)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = MakeRoom(r.Id, r.Bounds);
            var floorMat = r.Faded ? MakeLitMaterial("M_Greybox_Floor_Faded", new Color(0.72f, 0.72f, 0.66f)) : MakeLitMaterial("M_Greybox_Floor", new Color(0.45f, 0.47f, 0.36f));
            var platMat = r.Faded ? MakeLitMaterial("M_Greybox_Platform_Faded", new Color(0.76f, 0.73f, 0.66f)) : MakeLitMaterial("M_Greybox_Platform", new Color(0.52f, 0.46f, 0.36f));
            var waterMat = MakeLitMaterial("M_Greybox_Shallows", new Color(0.50f, 0.56f, 0.56f));
            foreach (var g in r.Ground)
            {
                MakeGround(room, g.name, g.c, g.s, g.name.StartsWith("Shallows") ? waterMat : g.name.StartsWith("Floor") ? floorMat : platMat);
                SkinGround(room, g.name, g.name.StartsWith("Shallows") ? "Ground_Shallows" : r.Faded ? "Ground_Boardwalk_Faded" : "Ground_Boardwalk", 4f);
            }

            if (r.Faded)
            {
                MakePaperLayer(room, "Mid_Reeds_Faded", 3f, 0f, new Color(0.80f, 0.80f, 0.74f), 6f);
                MakePaperLayer(room, "Far_Roosts_Faded", 8f, 2f, new Color(0.86f, 0.85f, 0.80f), 10f);
                MakePaperLayer(room, "Farther_Cliffs_Faded", 16f, 6f, new Color(0.90f, 0.89f, 0.84f), 16f);
            }
            else
            {
                MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.62f, 0.64f, 0.52f), 6f);
                MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.72f, 0.72f, 0.64f), 10f);
                MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.80f, 0.72f), 16f);
            }
            if (r.SeaWest) MakeSeaFade(room);

            foreach (var s in r.Spawns) MakeSpawn(room, s.name, s.pos);
            foreach (var e in r.Exits) MakeTransition(room, e.name, e.c, e.s, e.target, e.spawn);
            foreach (var v in r.Vantages) MakeVantage(room, v.name, r.Id + "/" + v.name, v.pos);
            foreach (var sd in r.Seeds) MakeSeeds(room, sd.pos, sd.n);
            foreach (var e in r.Enemies)
            {
                if (e.type == typeof(MarshCrab)) MakeEnemy<MarshCrab>(room, e.name, e.pos, e.size);
                else if (e.type == typeof(ReedSkimmer)) MakeEnemy<ReedSkimmer>(room, e.name, e.pos, e.size);
                else if (e.type == typeof(Smudge)) MakeEnemy<Smudge>(room, e.name, e.pos, e.size);
                else if (e.type == typeof(Cantor)) MakeEnemy<Cantor>(room, e.name, e.pos, e.size);
            }
            MakeFadeGroup(room);
            EditorSceneManager.SaveScene(scene, RoomPath(r.Id));
            Debug.Log("[OWSBG] saved " + RoomPath(r.Id));
        }

        // Iris seeds lying about (DES-05): a small sphere and a trigger.
        static void MakeSeeds(Room room, Vector2 pos, int n)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "IrisSeeds_" + n;
            go.layer = LayerMask.NameToLayer("Trigger");
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y + 0.4f, 0f);
            go.transform.localScale = Vector3.one * 0.35f;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 1.6f;
            go.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Seed", new Color(0.93f, 0.76f, 0.34f));
            var seed = go.AddComponent<IrisSeed>();
            seed.Count = n;
        }

        // The sea-fade: the Edge's white sheets, mirrored to the west (the horizon dissolves to white).
        static void MakeSeaFade(Room room)
        {
            var mat = MakeLitMaterial("M_Blank_White", new Color(0.97f, 0.96f, 0.93f));
            float[] z = { 0.6f, 4.5f, 10f, 18f };
            float[] x0 = { 16f, 14f, 12f, 10f };
            for (int i = 0; i < z.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Sea_" + i;
                q.layer = LayerMask.NameToLayer("Paper");
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(room.transform, false);
                float w = 40f;
                q.transform.position = new Vector3(-(x0[i] + w * 0.5f), 6f, z[i]);
                q.transform.localScale = new Vector3(w, 30f, 1f);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static List<string> AllRoomScenePaths()
        {
            var list = new List<string> { RoomAScenePath, RoomBScenePath, RoomCScenePath, RoomChapelScenePath, RoomEdgeScenePath };
            foreach (var r in SaltmarrowRecipes()) list.Add(RoomPath(r.Id));
            return list;
        }

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
        public static void BuildAddressables() => OWSBG.Build.GameBuild.BuildContent();

        // The held state (PRG-13): Wardens placed inactive; anchoring the place switches them on and locks the grade.
        static void MakeHeldState(Room room, params Vector2[] wardenPositions)
        {
            var held = room.gameObject.AddComponent<HeldState>();
            held.PlaceId = room.RoomId;
            for (int i = 0; i < wardenPositions.Length; i++)
            {
                MakeEnemy<Warden>(room, "Warden_" + (i + 1), wardenPositions[i], new Vector2(0.7f, 1.6f));
                var go = room.transform.Find("Warden_" + (i + 1)).gameObject;
                go.SetActive(false);
                held.AddWarden(go);
            }
        }

        // The place's fade (PRG-14): paper layers drop at 4 (foreground at 3); ground washes but never drops.
        static void MakeFadeGroup(Room room)
        {
            var group = room.gameObject.AddComponent<FadeGroup>();
            group.PlaceId = room.RoomId;
            foreach (Transform child in room.transform)
            {
                var r = child.GetComponent<MeshRenderer>();
                if (r == null || !child.gameObject.activeSelf) continue;
                if (child.name.StartsWith("Paper_")) group.AddLayer(r, child.name.StartsWith("Paper_Fore") ? 3 : 4);
                else if (child.gameObject.layer == LayerMask.NameToLayer("Ground") && child.GetComponents<MonoBehaviour>().Length == 0)
                    group.AddLayer(r, 5);
            }
            Debug.Log("[OWSBG] fade group " + room.RoomId + ": " + group.Layers.Count + " layers");
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
            // A region whose paper kit has this layer gets the inked cut-out on the ink shader (ENV-01);
            // otherwise the greybox's flat wash. The material is shared by every room of the region.
            var kitTex = KitTexture(room, "Paper_" + name);
            Material mat;
            if (kitTex != null) mat = MakePaperMaterial("M_Paper_" + name, kitTex, RegionPaper(RegionOf(room.RoomId)), false);
            else
            {
                mat = MakeLitMaterial("M_Paper_" + name, color);
                mat.SetFloat("_ReceiveShadows", 0f);
                mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                EditorUtility.SetDirty(mat);
            }
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
        // A bounds-walk in a room (DES-13); verses are added by the caller, then MakeBoundMarkers.
        static BoundsWalk MakeBoundsWalk(Room room, string id, string completeFlag, int completeValue, params Behaviour[] pause)
        {
            var go = new GameObject("Walk_" + id);
            go.transform.SetParent(room.transform, false);
            var walk = go.AddComponent<BoundsWalk>();
            walk.Id = id;
            walk.PlaceId = room.RoomId;
            walk.SecondsPerBeat = AudioDirection.BeatOf(Region.Saltmarrow) * 4f;   // four Saltmarrow beats (audio-direction 3), as the test reads it
            var so = new SerializedObject(walk);
            so.FindProperty("_completeFlag").stringValue = completeFlag;
            so.FindProperty("_completeFlagValue").intValue = completeValue;
            var p = so.FindProperty("_pauseWhileWalking");
            p.arraySize = pause.Length;
            for (int i = 0; i < pause.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = pause[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return walk;
        }

        static void MakeBoundMarkers(Room room, BoundsWalk walk)
        {
            var mat = MakeLitMaterial("M_Greybox_Bound", new Color(0.20f, 0.27f, 0.45f));
            var made = new Dictionary<Vector2, Renderer>();
            foreach (var v in walk.Verses)
                foreach (var b in v.Beats)
                {
                    if (made.TryGetValue(b.Position, out var existing)) { b.Marker = existing; continue; }
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "Bound_" + b.Name.Replace(' ', '_').Replace('\'', '_');
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                    post.transform.SetParent(walk.transform, false);
                    post.transform.position = new Vector3(b.Position.x, b.Position.y + 0.5f, 0.7f);
                    post.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
                    var r = post.GetComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    b.Marker = r;
                    made[b.Position] = r;
                }
        }

        // Where an NPC stands through the day (PRG-15); posts are added by the caller.
        static NpcSchedule MakeSchedule(Room room, string npcName)
        {
            var npc = room.transform.Find(npcName);
            if (npc == null) throw new System.InvalidOperationException("no NPC named " + npcName + " in " + room.RoomId);
            return npc.gameObject.AddComponent<NpcSchedule>();
        }

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
            // Every desk is a travel point (DES-02).
            var tp = go.AddComponent<TravelPoint>();
            var known = Atlas.FindWaypoint("desk." + room.RoomId);
            var tso = new SerializedObject(tp);
            tso.FindProperty("_waypointId").stringValue = "desk." + room.RoomId;
            tso.FindProperty("_kind").enumValueIndex = (int)WaypointKind.Desk;
            tso.FindProperty("_displayName").stringValue = known != null ? known.Name : "the desk";
            tso.FindProperty("_spawn").stringValue = "Desk";
            tso.ApplyModifiedPropertiesWithoutUndo();

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            Object.DestroyImmediate(table.GetComponent<Collider>());
            table.transform.SetParent(go.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.45f, 0.4f);
            table.transform.localScale = new Vector3(1.4f, 0.9f, 0.8f);
            table.GetComponent<MeshRenderer>().sharedMaterial = MakeLitMaterial("M_Greybox_Desk", new Color(0.42f, 0.30f, 0.20f));
            MakeSpawn(room, "Desk", pos);
        }

        // A lamp: a travel point that lights once its vantage is drawn, with a glow that shows it.
        static void MakeLamp(Room room, string name, Vector2 pos, string waypointId, string displayName, string litByVantage)
        {
            var go = new GameObject("Lamp_" + name) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 2f);
            col.offset = new Vector2(0f, 1f);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Glow";
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.3f, 0.5f);
            glow.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var glowR = glow.GetComponent<MeshRenderer>();
            glowR.sharedMaterial = MakeLitMaterial("M_Greybox_Glow", new Color(0.95f, 0.75f, 0.35f));

            var tp = go.AddComponent<TravelPoint>();
            var so = new SerializedObject(tp);
            so.FindProperty("_waypointId").stringValue = waypointId;
            so.FindProperty("_kind").enumValueIndex = (int)WaypointKind.Lamp;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_spawn").stringValue = name;
            so.FindProperty("_litByVantage").stringValue = litByVantage;
            so.FindProperty("_glow").objectReferenceValue = glowR;
            so.ApplyModifiedPropertiesWithoutUndo();
            MakeSpawn(room, name, pos);
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
            var roomPath = roomKey == "B" ? RoomBScenePath : roomKey == "C" ? RoomCScenePath : roomKey == "E" ? RoomEdgeScenePath
                         : roomKey == "A" ? RoomAScenePath : RoomPath(roomKey);   // or any recipe room id, e.g. Saltmarrow_Stilts
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

        // ---- paper kit (ENV-01, docs/design/paper-kit.md)

        static string RegionOf(string roomId) { int i = roomId.IndexOf('_'); return i < 0 ? roomId : roomId.Substring(0, i); }

        static Texture2D KitTexture(Room room, string layer) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Environment/" + RegionOf(room.RoomId) + "/" + layer + ".png");

        /// <summary>The region's paper colour (art-direction 5): what a drawing washes toward as its ink leaves.</summary>
        public static Color RegionPaper(string region)
        {
            switch (region)
            {
                case "Emberdown": return new Color(0.82f, 0.80f, 0.78f);
                case "Verdance": return new Color(0.94f, 0.90f, 0.72f);
                case "Halden": return new Color(0.92f, 0.92f, 0.87f);
                case "Windreach": return new Color(0.93f, 0.88f, 0.70f);
                case "Greyfold": case "Blank": return new Color(0.98f, 0.98f, 0.97f);
                default: return new Color(0.93f, 0.89f, 0.80f);   // Saltmarrow: warm cream
            }
        }

        /// <summary>An InkSprite material over a kit drawing; a greybox material of the same name is upgraded in place.</summary>
        static Material MakePaperMaterial(string name, Texture2D tex, Color paper, bool worldUv, float tileUnits = 1f)
        {
            var path = Root + "/Art/Materials/" + name + ".mat";
            var shader = Shader.Find("OWSBG/InkSprite");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else if (mat.shader != shader) mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_PaperColor", paper);
            mat.SetFloat("_ShadowStep", worldUv ? 0.2f : 0f);   // a backdrop lights flat; the walkway takes Wren's shadow
            mat.SetFloat("_Shadows", worldUv ? 1f : 0f);
            mat.SetFloat("_WorldUV", worldUv ? 1f : 0f);
            mat.SetFloat("_Lighting", worldUv ? 0.7f : 0.3f);  // a backdrop keeps its wash under the sun; the ground takes more of the light
            mat.SetTextureScale("_BaseMap", worldUv ? new Vector2(1f / tileUnits, 1f / tileUnits) : Vector2.one);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // A ground block drawn from the kit's tile: the block's own material becomes the tile, mapped in world
        // space by face (the shader picks the plane from the normal), so planks run on across blocks and the top
        // of a platform reads under the camera's tilt. Skipped when the region has no such tile yet.
        static void SkinGrounds(Room room, string tile, params string[] groundNames)
        {
            foreach (var g in groundNames) SkinGround(room, g, tile, 4f);
        }

        static void SkinGround(Room room, string groundName, string tile, float tileUnits)
        {
            var tex = KitTexture(room, tile);
            var ground = room.transform.Find(groundName);
            if (tex == null || ground == null) return;
            ground.GetComponent<MeshRenderer>().sharedMaterial =
                MakePaperMaterial("M_" + tile, tex, RegionPaper(RegionOf(room.RoomId)), true, tileUnits);
        }

        // ---- character sheets (CHR-03, docs/design/wren-animation.md)

        [System.Serializable] class SheetManifest { public string character; public int ppu, cell; public SheetEntry[] clips; }
        [System.Serializable] class SheetEntry { public string name, file; public int fps, frames; public bool loop; }

        /// <summary>The clips a character's packed sheets describe (Art/Characters/[name]/[name].json), or null before they exist.</summary>
        static List<SheetClip> LoadSheets(string character)
        {
            var folder = Root + "/Art/Characters/" + character + "/";
            var jsonPath = folder + character.ToLowerInvariant() + ".json";
            if (!File.Exists(jsonPath)) return null;
            var manifest = JsonUtility.FromJson<SheetManifest>(File.ReadAllText(jsonPath));
            if (manifest == null || manifest.clips == null || manifest.clips.Length == 0) return null;
            var clips = new List<SheetClip>();
            foreach (var c in manifest.clips)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + c.file);
                if (tex == null) { Debug.LogWarning("[OWSBG] sheet missing: " + folder + c.file); continue; }
                clips.Add(new SheetClip { Name = c.name, Sheet = tex, Frames = c.frames, Fps = c.fps, Loop = c.loop });
            }
            Debug.Log("[OWSBG] " + character + ": " + clips.Count + " sheet clips");
            return clips.Count > 0 ? clips : null;
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
