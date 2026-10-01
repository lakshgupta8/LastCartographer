using System.Collections.Generic;
using System.Linq;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.Setup
{
    /// <summary>
    /// Puts what can be read or asked into its greybox rooms (NAR-15's readable pieces, DES-15's askers): a trigger with an
    /// <see cref="NpcTalker"/> on the piece's Yarn scene, and under it the piece's drawing from the region's kit (ENV-06):
    /// the catalog's <c>Prop</c>, with its changed drawing beside it under a <see cref="DressingProp"/> where the piece
    /// changes with its place; a door asker's shut and open drawings on its flag. Where the room's recipe already stands
    /// the drawing (a milestone, the standing stones, the exam desks) only the trigger is placed; where no drawing exists
    /// (a bird who asks is a character, not a prop) a small block stands in. Re-running replaces what it placed
    /// ("Read_&lt;node&gt;") and touches nothing else.
    /// <c>Unity.exe -batchmode -executeMethod OWSBG.Setup.PlacementSetup.Place -quit</c>, or OWSBG → Place the Coast's Readables.
    /// </summary>
    public static class PlacementSetup
    {
        public const string Prefix = "Read_";
        const string ScenesDir = "Assets/_Project/Scenes/Greybox/Greybox_";
        /// <summary>How near the recipe's own drawing must stand for the trigger to take it as the piece.</summary>
        public const float StandsWithin = 3.5f;

        /// <summary>Room, Yarn scene, where Wren stands to read it (on the floor or a platform's top), the prompt, and whether it asks.</summary>
        public static readonly (string room, string node, float x, float y, string prompt, bool asks)[] Places =
        {
            ("Saltmarrow_Shore", "Shore_Tetherposts", -13f, 0f, "Read", false),     // the tideline, toward the sea-fade
            ("Saltmarrow_A", "Quay_PriceBoard", -13.5f, 0f, "Read", false),         // west of Sable, by the way in from the shore
            ("Saltmarrow_B", "Merrow_Lintels", -15f, 0f, "Read", false),            // the first doors, before Dotha's stoop
            ("Saltmarrow_Chain_1", "Chain_Log", -1.8f, 10.3f, "Read", false),       // the lamp room's sill, beside the vantage
            ("Saltmarrow_Chain_3", "Chain_Gannet", 2f, 4.8f, "Talk", true),         // the faded light's rail
            ("Saltmarrow_Chapel", "Chapel_Tapestry", 15.9f, 0f, "Read", false),     // behind the altar, west side
            ("Saltmarrow_Chapel", "Chapel_Door", 18.6f, 0f, "Read", true),          // the door in the east wall, past the altar
            // Emberdown (ENV-03): the highland's readables in their rooms.
            ("Emberdown_Rest_1", "Rest_Lintel", -14f, 0f, "Read", false),           // the roosts over the gate, as you come in from the stair
            ("Emberdown_Rest_2", "Rest_TallyWall", -5.5f, 0f, "Read", false),       // behind Kettil's porch
            ("Emberdown_Rest_3", "PitHead_Cups", 9f, 0f, "Read", false),            // the trestle by the boarded mine mouth
            ("Emberdown_Chimneys_3", "Chimneys_Foot", 3f, 0f, "Read", false),       // the bare stone at the ninth chimney's foot
            ("Emberdown_Hollow_2", "Hollow_Lamps", 15f, 0f, "Read", false),         // the lamps down the gallery wall
            ("Emberdown_Chimneys_3", "Ninth_Door", 11f, 0f, "Talk", true),          // the ninth chimney's door, which asks (Offerings)
            // The Verdance (ENV-04): the forest's readables and the inn's asker.
            ("Verdance_Road_2", "Road_Milestone", -13f, 0f, "Read", false),          // the first milestone, by the way in from the gap
            ("Verdance_House_2", "Cloister_Tapestry", -16f, 0f, "Read", false),      // the cloister's west wall, past the desk
            ("Verdance_Library_2", "Library_Lectern", -1f, 0f, "Read", false),       // the lectern's post, beside Ansel
            ("Verdance_Gate_2", "Gate_Inscription", 18.5f, 0f, "Read", false),       // the gate itself, past the arena's east door
            ("Verdance_Gate_2", "Inn_Traveller", -8f, 0f, "Talk", true),             // the one-night inn's traveller, who asks (Offerings)
            // Halden (ENV-05): the Plateau's readables.
            ("Halden_Bridges_1", "Bridges_TollBoard", -16f, 0f, "Read", false),       // the toll board at the first bridge
            ("Halden_Mills_2", "Mills_Sheets", 4f, 0f, "Read", false),               // the drying lofts' sheets
            ("Halden_Hall_1", "Hall_Order", 16f, 0f, "Read", false),                 // the standing order inside the Hall's doors
            ("Halden_Hall_2", "Hall_Roll", -5f, 0f, "Read", false),                  // the roll of Guildmasters, past the ledger
            ("Halden_Hall_3", "Hall_ExamPapers", 2f, 0f, "Read", false),             // between the exam desks
            ("Halden_Lowmarket_2", "Lowmarket_Notice", -8.5f, 0f, "Read", false),    // the notice board, by its post
            ("Halden_Orchard_2", "Orchard_Gravestone", 9.5f, 0f, "Read", false),     // the gravestone with a crest
            ("Halden_Bastion_1", "Bastion_Plaque", 12f, 0f, "Read", false),          // the plaque by the door cut in the tower's foot
            ("Halden_Bastion_3", "Office_Drawing", 12f, 0f, "Read", false),          // the chick's drawing on Voss's wall
            ("Halden_Observatory_2", "Observatory_Frame", 2f, 0f, "Read", false),    // the frame of the Great Atlas, and the choice
            // Windreach (ENV-07): the Steppe's readables and the Gate's asker.
            ("Windreach_Stones_1", "Stones_Notches", -6.5f, 0f, "Read", false),       // the first stone's notches, one a walk
            ("Windreach_Camp_1", "Camp_WagonCloth", 6f, 0f, "Read", false),           // the route woven on the wagon that stays
            ("Windreach_Gate_1", "Gate_Lip", 8.5f, 0f, "Read", false),                // the carved stones on the lip
            ("Windreach_Gate_1", "Gate_Brek", 4f, 0f, "Talk", true),                  // Brek beside the stones, who asks (Offerings)
            // The Greyfold and the Blank (ENV-08): the threshold's readables, and the notice that calls the climax.
            ("Greyfold_EdgeCamp_2", "EdgeCamp_Notice", -7f, 0f, "Read", false),        // Voss's notice over the dead ledger
            ("Greyfold_EdgeCamp_2", "EdgeCamp_Beam", -4f, 0f, "Read", false),          // her initials in the beam
            ("Greyfold_Road_2", "Road_Mileposts", 0f, 0f, "Read", false),              // the milepost that reads 1
            ("Blank_Capital_1", "Capital_Nameplate", -8.5f, 0f, "Read", false),        // the Guild office door's plate
            ("Blank_Hollow_2", "Hollow_Doorframe", -8f, 0f, "Read", false),            // the height marks in Ilse's doorframe
            ("Blank_Capital_4", "Capital_Corvin_Argue", -9.5f, 0f, "Talk", true),      // Corvin's argument, which asks (Offerings): beside him, before the door
        };

        /// <summary>Readables that are not catalog pieces or askers, and the drawing that stands for them (ENV-06).</summary>
        public static readonly Dictionary<string, string> Extra = new Dictionary<string, string>
        {
            { "Gate_Inscription", "Inscription" },   // the gate's band of letters
            { "Orchard_Gravestone", "Gravestone" },  // the recipe's stone
            { "Observatory_Frame", "Frame" },        // the recipe's frame
            { "EdgeCamp_Notice", "Ledger" },         // Voss's notice is pinned over the recipe's dead ledger
        };

        /// <summary>What stands for a scene: the catalog piece's drawings, a door's shut and open, an extra's, or nothing (a bird who asks).</summary>
        public static (string prop, string after, DressingChange change) DrawingFor(string node)
        {
            var piece = Dressing.ByNode(node);
            if (piece != null) return (piece.Prop, piece.PropAfter, piece.Change);
            var asker = Offerings.All.FirstOrDefault(a => a.Node == node);
            if (asker != null) return asker.Prop == null ? (null, null, default) : (asker.Prop, asker.Prop + "_Open", DressingChange.OnFlag(asker.Opens));
            return Extra.TryGetValue(node, out var extra) ? (extra, null, default) : (null, null, default);
        }

        [MenuItem("OWSBG/Place the Coast's Readables")]
        public static void Place()
        {
            var readable = MakeMaterial("M_Greybox_Readable", new Color(0.86f, 0.80f, 0.62f));
            var asker = MakeMaterial("M_Greybox_Asker", new Color(0.78f, 0.52f, 0.22f));
            int placed = 0, drawn = 0, blocks = 0;
            foreach (var group in Places.GroupBy(p => p.room))
            {
                string path = ScenesDir + group.Key + ".unity";
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var room = Object.FindFirstObjectByType<Room>();
                if (room == null) { Debug.LogError("[OWSBG] place: no Room in " + path); continue; }
                foreach (var p in group)
                {
                    var old = room.transform.Find(Prefix + p.node);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
                var fade = room.GetComponentInChildren<FadeGroup>(true);
                if (fade != null) fade.RemoveMissing();
                foreach (var p in group)
                {
                    var made = Make(room, p.node, new Vector2(p.x, p.y), p.prompt, p.asks ? asker : readable, fade);
                    placed++;
                    if (made == Made.Drawing) drawn++; else if (made == Made.Block) blocks++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[OWSBG] place: " + placed + " readables in " + Places.Select(p => p.room).Distinct().Count() + " rooms; " + drawn + " drawn here, " + blocks + " blocks, the rest the recipes' drawings");
        }

        enum Made { Recipe, Drawing, Block }

        static Made Make(Room room, string node, Vector2 at, string prompt, Material mat, FadeGroup fade)
        {
            var go = new GameObject(Prefix + node) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 2f);
            col.offset = new Vector2(0f, 1f);

            var talker = go.AddComponent<NpcTalker>();
            var so = new SerializedObject(talker);
            so.FindProperty("_startNode").stringValue = node;
            so.FindProperty("_prompt").stringValue = prompt;
            so.FindProperty("_faceWren").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var (prop, after, change) = DrawingFor(node);
            if (prop != null && Stands(room, prop, at)) return Made.Recipe;   // the recipe's own drawing is the piece
            var drawn = prop != null ? ProjectSetup.MakeDressing(room, go.transform, node, prop, after, change, Vector2.zero, 0.4f) : null;
            if (drawn == null) { Marker(go, mat); return Made.Block; }
            if (fade != null)
                foreach (var r in drawn.GetComponentsInChildren<MeshRenderer>(true)) fade.AddLayer(r, 5);
            return Made.Drawing;
        }

        /// <summary>Whether the room already stands Prop_[prop] near enough to be the piece.</summary>
        public static bool Stands(Room room, string prop, Vector2 at)
            => room.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Prop_" + prop
                                                                  && Mathf.Abs(t.position.x - at.x) <= StandsWithin && Mathf.Abs(t.position.y - at.y) <= 2.5f);

        static void Marker(GameObject go, Material mat)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Marker";
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(go.transform, false);
            block.transform.localPosition = new Vector3(0f, 0.6f, 0.4f);
            block.transform.localScale = new Vector3(0.5f, 1.2f, 0.2f);
            block.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material MakeMaterial(string name, Color color)
        {
            var path = "Assets/_Project/Art/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
