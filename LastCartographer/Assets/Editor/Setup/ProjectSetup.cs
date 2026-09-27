// Batch-mode project configuration. Entry points, run as separate editor launches:
//
//   1) -executeMethod OWSBG.Setup.ProjectSetup.Configure
//        folders, asmdefs, layers, URP pipeline + renderer, project settings, placeholder texture.
//   2) -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene
//        the persistent scene (camera rig, Wren, RoomManager, volume) and two greybox rooms.
//   3) -executeMethod OWSBG.Setup.ProjectSetup.CaptureScreenshot   (run WITHOUT -nographics)
using System.IO;
using OWSBG.Core;
using OWSBG.World;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OWSBG.Setup
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        public const string PersistentScenePath = Root + "/Scenes/Persistent/Persistent.unity";
        public const string RoomAScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_A.unity";
        public const string RoomBScenePath = Root + "/Scenes/Greybox/Greybox_Saltmarrow_B.unity";
        const string InputAssetPath = Root + "/Settings/Input/WrenInput.inputactions";
        const string PlaceholderTexPath = Root + "/Art/Characters/Placeholder_Wren.png";

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
            Physics2D.queriesHitTriggers = false;
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
            BuildPersistent();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PersistentScenePath, true),
                new EditorBuildSettingsScene(RoomAScenePath, true),
                new EditorBuildSettingsScene(RoomBScenePath, true),
            };
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

            // Room manager.
            var rmGo = new GameObject("RoomManager");
            var rm = rmGo.AddComponent<RoomManager>();
            var rmSo = new SerializedObject(rm);
            rmSo.FindProperty("_startRoom").stringValue = Path.GetFileNameWithoutExtension(RoomAScenePath);
            rmSo.FindProperty("_startSpawn").stringValue = "Start";
            rmSo.FindProperty("_wren").objectReferenceValue = wren.GetComponent<WrenController>();
            rmSo.FindProperty("_confiner").objectReferenceValue = confiner;
            rmSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, PersistentScenePath);
            Debug.Log("[OWSBG] saved " + PersistentScenePath);
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
            var strikeVisual = go.AddComponent<StrikeVisual>();
            var svSo = new SerializedObject(strikeVisual);
            svSo.FindProperty("_inkMaterial").objectReferenceValue = MakeLitMaterial("M_Ink_Black", new Color(0.06f, 0.06f, 0.08f));
            svSo.ApplyModifiedPropertiesWithoutUndo();

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

            MakePaperLayer(room, "Mid_Reeds", 3f, 0f, new Color(0.62f, 0.64f, 0.52f), 6f);
            MakePaperLayer(room, "Far_Roosts", 8f, 2f, new Color(0.72f, 0.72f, 0.64f), 10f);
            MakePaperLayer(room, "Farther_Cliffs", 16f, 6f, new Color(0.82f, 0.80f, 0.72f), 16f);
            MakePaperLayer(room, "Fore_Reeds", -4f, -2.6f, new Color(0.30f, 0.33f, 0.24f), 1.6f);

            MakeDummy(room, new Vector2(4f, 0.6f));

            MakeSpawn(room, "Start", new Vector2(-2f, 0f));
            MakeSpawn(room, "West", new Vector2(-17f, 0f));
            MakeSpawn(room, "East", new Vector2(17f, 0f));
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

            MakeSpawn(room, "West", new Vector2(-17f, 0f));
            MakeSpawn(room, "East", new Vector2(17f, 0f));
            MakeTransition(room, "To_A", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f),
                Path.GetFileNameWithoutExtension(RoomAScenePath), "East");

            EditorSceneManager.SaveScene(scene, RoomBScenePath);
            Debug.Log("[OWSBG] saved " + RoomBScenePath);
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
            EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(RoomAScenePath, OpenSceneMode.Additive);
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[OWSBG] no main camera"); return; }
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
            var wren = Object.FindFirstObjectByType<WrenController>();
            var focus = wren != null ? wren.transform.position : Vector3.zero;
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
