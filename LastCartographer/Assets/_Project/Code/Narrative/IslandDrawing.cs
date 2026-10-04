using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace OWSBG.Narrative
{
    /// <summary>
    /// An island drawn (docs/design/blank-generator.md §3): its place's strips, floor and props from the region's own
    /// kit, greyed toward the paper as far as the island's wash, torn out of the white by ragged sheets either side;
    /// the Blank's far layers above; its people in the bodies they were met in, the crowd round them. Everything is
    /// loaded by name (<see cref="AddressableArt"/>) and let go with the room. <see cref="Load"/> is null when the art
    /// is not there, and the island keeps its greybox.
    /// </summary>
    internal sealed class IslandDrawing
    {
        /// <summary>The strips' and the props' pixels a unit (the kits' PPU and the props').</summary>
        public const float StripPpu = 40f, PropPpu = 96f;
        /// <summary>Where the white tears in, either side, at the place's strip and at its far strip.</summary>
        public const float MidEdge = 12f, FarEdge = 16f;

        public IslandLook Look;
        public Material Mid, Far, BlankFar, BlankFarther, Tile, White, People;
        public readonly List<(IslandProp prop, Material mat)> Props = new List<(IslandProp, Material)>();

        public static IslandDrawing Load(IslandLook look, GameObject owner)
        {
            if (look == null) return null;
            var d = new IslandDrawing { Look = look };
            d.Mid = AddressableArt.Material(look.MidMaterial, owner);
            d.Tile = AddressableArt.Material(look.TileMaterial, owner);
            d.White = AddressableArt.Material(IslandLooks.WhiteTile, owner);
            if (d.Mid == null || d.Tile == null || d.White == null) return null;
            d.Far = AddressableArt.Material(look.FarMaterial, owner);
            d.BlankFar = AddressableArt.Material(IslandLooks.BlankFar, owner);
            d.BlankFarther = AddressableArt.Material(IslandLooks.BlankFarther, owner);
            d.People = AddressableArt.Material(IslandLooks.PeopleMaterial, owner);
            foreach (var p in look.Props)
            {
                var m = AddressableArt.Material(look.PropMaterial(p), owner);
                if (m != null) d.Props.Add((p, m));
            }
            return d;
        }

        static readonly int WashId = Shader.PropertyToID("_Wash"), LineFadeId = Shader.PropertyToID("_LineFade"), BaseMapId = Shader.PropertyToID("_BaseMap");

        /// <summary>A renderer's colour gone toward the paper: the fills as far as the wash, the line half as far.</summary>
        public static void Grey(Renderer r, float wash)
        {
            if (r == null || wash <= 0f) return;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetFloat(WashId, Mathf.Clamp01(wash));
            block.SetFloat(LineFadeId, Mathf.Clamp01(wash * 0.5f));
            r.SetPropertyBlock(block);
        }

        static Vector2 SizeOf(Material m, float ppu, Vector2 fallback)
        {
            var tex = m != null && m.HasProperty(BaseMapId) ? m.GetTexture(BaseMapId) : null;
            return tex != null ? new Vector2(tex.width / ppu, tex.height / ppu) : fallback;
        }

        static GameObject Quad(Transform parent, string name, Material mat, string layer, ShadowCastingMode shadows)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = RuntimeRooms.QuadMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>A strip across the room at a depth, its foot at <paramref name="y"/>, its height its own; greyed.</summary>
        public static GameObject Strip(Room room, string name, Material mat, float z, float y, float wash)
        {
            if (mat == null) return null;
            var size = SizeOf(mat, StripPpu, new Vector2(80f, 6f));
            var q = Quad(room.transform, "Paper_" + name, mat, "Paper", ShadowCastingMode.Off);
            q.transform.position = new Vector3(0f, y + size.y * 0.5f, z);
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            Grey(q.GetComponent<Renderer>(), wash);
            return q;
        }

        /// <summary>A prop on its feet, its size its drawing's; greyed.</summary>
        public static GameObject Prop(Room room, IslandProp prop, Material mat, float z, float wash)
        {
            var size = SizeOf(mat, PropPpu, new Vector2(1.5f, 1.5f));
            var q = Quad(room.transform, "Prop_" + prop.Name, mat, "Default", ShadowCastingMode.TwoSided);
            q.transform.position = new Vector3(prop.X, size.y * 0.5f, z);
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            Grey(q.GetComponent<Renderer>(), wash);
            return q;
        }

        // ---- the white's torn sheets ---------------------------------------------------------------------------------

        const int TornSize = 512;
        /// <summary>Where the sheet's torn edge runs, across its texture (the rest of it, to the left, is paper).</summary>
        public const float TornEdgeU = 0.92f;
        static Texture2D _torn;

        /// <summary>
        /// A sheet of the Blank's paper with its right edge torn: paper to the left of a ragged line, the line inked, nothing
        /// past it. Made once, from a fixed seed, so every island tears the same way and the pixels are the same each run.
        /// </summary>
        public static Texture2D TornSheet()
        {
            if (_torn != null) return _torn;
            var tex = new Texture2D(TornSize, TornSize, TextureFormat.RGBA32, false)
            { name = "Island_TornSheet", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[TornSize * TornSize];
            var paper = new Color32(250, 250, 247, 255);
            var ink = new Color32(20, 26, 40, 255);
            var none = new Color32(250, 250, 247, 0);
            var rng = new System.Random(4071);
            // A torn edge: a slow wander, the paper's fibres catching every few rows, then softened over three rows.
            var raw = new float[TornSize];
            float edge = TornEdgeU * TornSize, drift = 0f;
            for (int y = 0; y < TornSize; y++)
            {
                drift = drift * 0.92f + ((float)rng.NextDouble() - 0.5f) * 1.8f;
                edge = Mathf.Clamp(edge + drift, TornSize * (TornEdgeU - 0.05f), TornSize * (TornEdgeU + 0.05f));
                raw[y] = edge + (rng.NextDouble() < 0.12 ? (float)rng.NextDouble() * 3f : 0f);
            }
            var e = new float[TornSize];
            for (int y = 0; y < TornSize; y++)
                e[y] = (raw[Mathf.Max(0, y - 1)] + raw[y] * 2f + raw[Mathf.Min(TornSize - 1, y + 1)]) * 0.25f;
            for (int y = 0; y < TornSize; y++)
            {
                // the line follows the edge across a row's step to the next, so it never breaks into specks
                float low = Mathf.Min(e[y], Mathf.Min(e[Mathf.Max(0, y - 1)], e[Mathf.Min(TornSize - 1, y + 1)]));
                for (int x = 0; x < TornSize; x++)
                    px[y * TornSize + x] = x > e[y] ? none : x >= low - 2.5f ? ink : paper;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _torn = tex;
            return tex;
        }

        static Material _tornMat;

        /// <summary>The torn sheet on the Blank's paper (a copy of its farthest strip's ink material).</summary>
        static Material TornMaterial(Material blankPaper)
        {
            if (_tornMat != null) return _tornMat;
            if (blankPaper == null) return null;
            _tornMat = new Material(blankPaper) { name = "M_Island_TornSheet" };
            _tornMat.SetTexture(BaseMapId, TornSheet());
            _tornMat.SetTextureScale(BaseMapId, Vector2.one);
            _tornMat.SetTextureOffset(BaseMapId, Vector2.zero);
            if (_tornMat.HasProperty("_Lighting")) _tornMat.SetFloat("_Lighting", 0f);
            return _tornMat;
        }

        /// <summary>
        /// Two sheets of white at a depth, torn edges facing in at ±<paramref name="edge"/>, from <paramref name="y0"/> to
        /// <paramref name="y1"/>: what is behind them between the edges is the island, and the rest is the Blank.
        /// </summary>
        public static void TearOut(Room room, string name, Material blankPaper, float z, float edge, float y0, float y1, float width = 28f)
        {
            var mat = TornMaterial(blankPaper);
            if (mat == null) return;
            for (int side = -1; side <= 1; side += 2)
            {
                var q = Quad(room.transform, "White_" + name + (side < 0 ? "_W" : "_E"), mat, "Paper", ShadowCastingMode.Off);
                // the torn edge sits TornEdgeU of the way across the quad; mirrored on the east so it faces in
                float centre = edge + width * (TornEdgeU - 0.5f);
                q.transform.position = new Vector3(side * centre, (y0 + y1) * 0.5f, z);
                q.transform.localScale = new Vector3(-side * width, y1 - y0, 1f);
            }
        }

        // ---- the people ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Draws a person on <paramref name="go"/> from a character's sheets, in place of its grey quad: the sheets on an
        /// <see cref="InkSheetPlayer"/>, an <see cref="NpcAnimator"/> doing <paramref name="activity"/>, and an
        /// <see cref="NpcInk"/> at rest as a Remnant (an island makes anyone one). False when the sheets are not there.
        /// </summary>
        public static bool Dress(GameObject go, string character, Material ink, bool faceLeft, string activity)
        {
            if (ink == null || string.IsNullOrEmpty(character)) return false;
            var clips = AddressableArt.Sheets(character, Clips, go.scene.IsValid() ? FindRoomRoot(go) : go, out float cell);
            if (clips.Count == 0) return false;
            var old = go.transform.Find("Sprite");
            if (old != null) Object.DestroyImmediate(old.gameObject);   // now, so NpcInk finds the drawn renderer, not the grey one
            var q = Quad(go.transform, "Sprite", ink, LayerMask.LayerToName(go.layer), ShadowCastingMode.TwoSided);
            q.transform.localScale = new Vector3(faceLeft ? -cell : cell, cell, 1f);
            q.transform.localPosition = new Vector3(0f, cell * 0.5f, 0f);
            var player = go.AddComponent<InkSheetPlayer>();
            player.Configure(q.GetComponent<Renderer>(), clips);
            go.AddComponent<NpcAnimator>().Activity = activity;
            var inkState = go.AddComponent<NpcInk>();
            inkState.Rest = NpcInkState.Remnant;
            return true;
        }

        /// <summary>The clips an island's people can play: the townsfolk's, and the cast's singing.</summary>
        public static readonly string[] Clips = { "idle", "talk", "walk", "asleep", "watching", "cheering", "singing" };

        static GameObject FindRoomRoot(GameObject go)
        {
            var room = go.GetComponentInParent<Room>();
            return room != null ? room.gameObject : go;
        }
    }
}
