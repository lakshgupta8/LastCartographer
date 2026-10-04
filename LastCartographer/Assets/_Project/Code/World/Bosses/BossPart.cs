using System;
using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A piece of a boss fight the quill can strike that is not the boss's body: a dove of the Choir, a block of rubble,
    /// an ink surge, a stone feather. It hands every hit to its owner, who says whether it landed (and so whether the
    /// strike pogoes or refills ink). A trigger on the Enemy layer with a kinematic body, so it can be moved each frame.
    /// A part can wear sheets (CHR-10): <see cref="Dress"/> swaps its block for an InkSprite quad playing clips, and the
    /// owner names the clip (a dove's ring, a lamp lit) instead of swapping materials.
    /// </summary>
    public sealed class BossPart : MonoBehaviour, IHittable
    {
        /// <summary>The owner's rule: return true when the hit lands.</summary>
        public Func<HitInfo, bool> OnHit;
        public BoxCollider2D Box { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public Transform Visual { get; private set; }
        public Vector2 Position => transform.position;
        /// <summary>The sheets it wears, once dressed.</summary>
        public InkSheetPlayer Sheets { get; private set; }
        public bool IsDressed => Sheets != null;

        PartVoice _voice;

        /// <summary>The owner says whether it landed; a landed strike is the part's struck sound (AUD-16).</summary>
        public bool TakeHit(in HitInfo hit)
        {
            bool landed = OnHit != null && OnHit(hit);
            if (landed && _voice != null) _voice.Struck();
            return landed;
        }

        public void MoveTo(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, 0f);
            Body.position = p;
        }

        /// <summary>The block's material. A dressed part keeps its drawing: its owner plays a clip instead.</summary>
        public void SetMaterial(Material mat)
        {
            if (IsDressed || Visual == null) return;
            Visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>Play a clip on the drawing; nothing on a block.</summary>
        public bool Play(string clip, bool restart = false) => Sheets != null && Sheets.Play(clip, restart);
        public void Seek(float progress) { if (Sheets != null) Sheets.Seek(progress); }

        /// <summary>
        /// Wear sheets: the block becomes a cell-sized quad on the ink shader, centred on the collider (or, with
        /// <paramref name="bottomAtFloor"/>, standing with the cell's bottom edge at the floor under the collider's
        /// centre, for a townsfolk drawn with her feet at the cell's edge). <paramref name="wash"/> and
        /// <paramref name="lineFade"/> set the ink's colour state (a drawing of Wren, greyed).
        /// </summary>
        public void Dress(IReadOnlyList<SheetClip> clips, float cellUnits, string clip = "idle", bool bottomAtFloor = false, float floorY = 0f, float wash = 0f, float lineFade = 0f)
        {
            if (clips == null || clips.Count == 0) return;
            var r = DressRenderer(transform, Visual, clips, cellUnits, out var player);
            if (r == null) return;
            Visual = r.transform;
            if (bottomAtFloor) Visual.localPosition = new Vector3(0f, floorY - transform.position.y + cellUnits * 0.5f, 0f);
            if (wash > 0f || lineFade > 0f)
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(WashId, wash);
                mpb.SetFloat(LineFadeId, lineFade);
                r.SetPropertyBlock(mpb);
            }
            Sheets = player;
            player.Play(clip);
        }

        static readonly int WashId = Shader.PropertyToID("_Wash");
        static readonly int LineFadeId = Shader.PropertyToID("_LineFade");
        static Material _inkSprite;

        /// <summary>A fresh InkSprite material for a drawing made at run time (the sheet goes through the property block).</summary>
        public static Material InkSpriteMaterial()
        {
            if (_inkSprite != null) return _inkSprite;
            var shader = Shader.Find("OWSBG/InkSprite");
            if (shader == null) return null;
            _inkSprite = new Material(shader) { name = "M_BossPart_Ink (runtime)" };
            return _inkSprite;
        }

        /// <summary>
        /// Replace a block under <paramref name="owner"/> with a cell-sized sprite quad playing <paramref name="clips"/>. Returns
        /// the quad's renderer, or null when the ink shader is missing (the block stays).
        /// </summary>
        public static MeshRenderer DressRenderer(Transform owner, Transform block, IReadOnlyList<SheetClip> clips, float cellUnits, out InkSheetPlayer player)
        {
            player = null;
            var mat = InkSpriteMaterial();
            if (mat == null) return null;
            if (block != null) Destroy(block.gameObject);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(owner, false);
            quad.transform.localScale = new Vector3(cellUnits, cellUnits, 1f);
            quad.transform.localPosition = Vector3.zero;
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            player = owner.gameObject.AddComponent<InkSheetPlayer>();
            player.Configure(r, clips);
            return r;
        }

        /// <summary>Dress a plain prop (a lamp, a stone) the same way; its clips are then played through <see cref="Show"/>.</summary>
        public static InkSheetPlayer DressProp(Transform prop, IReadOnlyList<SheetClip> clips, float cellUnits, string clip = "idle")
        {
            if (prop == null || clips == null || clips.Count == 0) return null;
            var mf = prop.GetComponent<MeshFilter>();
            if (mf != null) { Destroy(mf); Destroy(prop.GetComponent<MeshRenderer>()); }
            var r = DressRenderer(prop, null, clips, cellUnits, out var player);
            if (r == null) return null;
            prop.localScale = Vector3.one;
            player.Play(clip);
            return player;
        }

        /// <summary>A prop's state: the clip when it wears sheets, else the material.</summary>
        public static void Show(Transform prop, string clip, Material fallback)
        {
            if (prop == null) return;
            var player = prop.GetComponent<InkSheetPlayer>();
            if (player != null) { player.Play(clip); return; }
            var r = prop.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = fallback;
        }

        public static BossPart Make(string name, Transform parent, Vector2 at, Vector2 size, Material mat)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var part = go.AddComponent<BossPart>();
            part.Box = box;
            part.Body = rb;
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = "Visual";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(go.transform, false);
            q.transform.localScale = new Vector3(size.x, size.y, 0.3f);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : InkMaterials.Dark;
            part.Visual = q.transform;
            part._voice = go.AddComponent<PartVoice>();   // heard as it appears, is struck, goes, and while it is there (AUD-16)
            return part;
        }

        /// <summary>A prop with no hit rule: a lamp, a floor section, a dust cloud.</summary>
        public static Transform Prop(string name, Transform parent, Vector2 at, Vector2 size, Material mat, float z = 0.3f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            if (parent != null) q.transform.SetParent(parent, false);
            q.transform.position = new Vector3(at.x, at.y, z);
            q.transform.localScale = new Vector3(size.x, size.y, 0.2f);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : InkMaterials.Dark;
            return q.transform;
        }
    }
}
