using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Archivist, Corvin Halloway (bible 6.14, boss sheet 6.14, CMB-16). Tier IV, in the mirror-Observatory: an
    /// enormous half-drawn owl. He draws. Phase 1: walls either side of her and a floor over her, real while his quill
    /// is on them, and a low swoop. Phase 2: he draws Wren, and her drawing hunts her with a jab of her own; the quill
    /// passes through it, since it is her. A strike on his quill hand while he draws unmakes every drawing and staggers
    /// him. Phase 3: he draws the Atlas frame round the arena, stops drawing, and holds; the frame closes by a wingspan a
    /// beat, outside it is the page and the page hurts, and a Longstroke on an edge pushes it back.
    /// </summary>
    public sealed class Archivist : Boss
    {
        public enum Attack { None, Draw, Swoop, DrawWren, Frame }
        public enum Move { Stand, Telegraph, Drawing, Swoop, Recover, Holding }

        [Header("The Archivist")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public float perchY = 3f;
        public Vector2 handOffset = new Vector2(1.5f, -0.6f);
        public Vector2 handSize = new Vector2(0.7f, 0.7f);
        public int drawTelegraphFrames = 16;
        public float drawSeconds = 3f;
        public float wallGap = 2.4f, wallHeight = 4f, wallWidth = 0.6f;
        public float floorHeight = 2.4f, floorWidth = 3f;
        public int swoopTelegraphFrames = 16;
        public float swoopSpeed = 10f, swoopY = 1f;
        public float wrenDrawingSpeed = 2.6f, wrenDrawingReach = 1.6f;
        public int wrenDrawingTelegraphFrames = 12;
        public float wrenDrawingJabSeconds = 1.4f;
        public float unmadeStaggerSeconds = 0.8f;
        public float beatSeconds = 0.8f, wingspan = 0.6f, frameMinWidth = 4f, framePushBack = 1.2f;
        public int recoverFrames = 22;
        public float standSeconds = 0.6f;
        public int damage = 1, pageDamage = 1;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        public bool IsDrawing => _drawLeft > 0f;
        public BossPart Hand { get; private set; }
        public IReadOnlyList<GameObject> Drawings => _drawings;
        public BossPart WrenDrawing { get; private set; }
        public bool FrameDrawn => _frameL != null;
        public float FrameLeft { get; private set; }
        public float FrameRight { get; private set; }
        public int Draws { get; private set; }
        public int Swoops { get; private set; }
        public int WrenDrawn { get; private set; }
        public int WrenDrawingJabs { get; private set; }
        public int Unmade { get; private set; }
        public int FramePushes { get; private set; }
        public int PageTakes { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Draw, Attack.Swoop };
        static readonly Attack[] Phase2 = { Attack.DrawWren, Attack.Draw, Attack.Swoop };
        static readonly Attack[] Phase3 = { Attack.Frame };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        readonly List<GameObject> _drawings = new List<GameObject>();
        BossPart _frameL, _frameR;
        int _frames, _patternIndex, _swoopDir, _jabFrames;
        float _pause, _drawLeft, _jabT, _beatT;
        bool _telegraphStarted, _jabbing, _jabHit;
        Vector2 _perch;
        WrenVitals _vitals;

        protected override void Start()
        {
            base.Start();
            _perch = transform.position;
            EnsureHand();
        }

        void EnsureHand()
        {
            if (Hand != null) return;
            Hand = BossPart.Make("QuillHand", transform.parent, HandPos(), handSize, InkMaterials.Lit("Archivist_Hand", new Color(0.30f, 0.26f, 0.22f)));
            Hand.OnHit = hit => StrikeHand();
        }

        Vector2 HandPos() => (Vector2)transform.position + new Vector2(handOffset.x * Facing, handOffset.y);

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            EnsureHand();
            _perch = transform.position;
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ClearAll();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase == 3)
            {
                // He draws the frame round the arena, and stops drawing, and holds.
                Unmake();
                SetPos(new Vector2((arenaMinX + arenaMaxX) * 0.5f, _perch.y));
                DrawFrame();
                Current = Move.Holding;
                CurrentAttack = Attack.Frame;
            }
        }

        protected override void OnDefeated()
        {
            ClearAll();
            if (Hand != null) Destroy(Hand.gameObject);
            Hand = null;
        }

        void ClearAll()
        {
            Unmake();
            if (_frameL != null) Destroy(_frameL.gameObject);
            if (_frameR != null) Destroy(_frameR.gameObject);
            _frameL = _frameR = null;
        }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        Attack NextAttack()
        {
            var p = PatternFor(Phase);
            var a = p[_patternIndex % p.Count];
            _patternIndex++;
            return a;
        }

        void BeginAttack(Attack a)
        {
            if (a == Attack.Frame) { Current = Move.Holding; return; }
            CurrentAttack = a;
            _telegraphStarted = false;
            _frames = 0;
            FaceWren();
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => a == Attack.Swoop ? swoopTelegraphFrames : drawTelegraphFrames;

        protected override void FixedUpdate()
        {
            if (IsFightActive)
            {
                float dt = Time.fixedDeltaTime;
                if (_drawLeft > 0f) { _drawLeft -= dt; if (_drawLeft <= 0f) Unmake(); }   // the quill lifts
                if (WrenDrawing != null) HuntHer(dt);
                if (FrameDrawn) Close(dt);
            }
            base.FixedUpdate();
            if (Hand != null) Hand.MoveTo(HandPos());
        }

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause <= 0f) BeginAttack(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    switch (CurrentAttack)
                    {
                        case Attack.Draw: Draws++; DrawRoom(); Current = Move.Drawing; break;
                        case Attack.DrawWren: WrenDrawn++; DrawHer(); Current = Move.Drawing; break;
                        case Attack.Swoop:
                            Swoops++;
                            _swoopDir = transform.position.x < (arenaMinX + arenaMaxX) * 0.5f ? 1 : -1;
                            SetPos(new Vector2(_swoopDir > 0 ? arenaMinX : arenaMaxX, floorY + swoopY));
                            Current = Move.Swoop;
                            break;
                        default: Recover(); break;
                    }
                    break;

                case Move.Drawing:
                    if (!IsDrawing) Recover();   // his quill is off the page
                    break;

                case Move.Swoop:
                {
                    var p = (Vector2)transform.position + new Vector2(_swoopDir * swoopSpeed * dt, 0f);
                    SetPos(p);
                    if (p.x <= arenaMinX || p.x >= arenaMaxX) { SetPos(_perch); Recover(); }
                    break;
                }

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; }
                    break;

                case Move.Holding:
                    break;
            }
        }

        protected override bool ContactHurts => Current == Move.Swoop;

        void Recover() { Current = Move.Recover; _frames = 0; }

        void SetPos(Vector2 p)
        {
            p.x = Mathf.Clamp(p.x, arenaMinX, arenaMaxX);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            Body.position = p;
        }

        // ---- drawing ----------------------------------------------------------------------------------------------------

        /// <summary>Walls either side of her and a floor over her head, real while the quill is on them.</summary>
        void DrawRoom()
        {
            Unmake();
            float wx = Wren != null ? Wren.Position.x : (arenaMinX + arenaMaxX) * 0.5f;
            foreach (var x in new[] { wx - wallGap, wx + wallGap })
                _drawings.Add(Solid("DrawnWall", new Vector2(Mathf.Clamp(x, arenaMinX, arenaMaxX), floorY + wallHeight * 0.5f), new Vector2(wallWidth, wallHeight)));
            _drawings.Add(Solid("DrawnFloor", new Vector2(wx, floorY + floorHeight), new Vector2(floorWidth, 0.3f)));
            _drawLeft = drawSeconds;
        }

        GameObject Solid(string name, Vector2 centre, Vector2 size)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
            go.transform.SetParent(transform.parent, false);
            go.transform.position = new Vector3(centre.x, centre.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            BossPart.Prop("Ink", go.transform, centre, size, InkMaterials.Lit("Archivist_Ink", new Color(0.20f, 0.18f, 0.24f)), 0f);
            return go;
        }

        /// <summary>Her drawing: it hunts her with a jab of her own. The quill passes through it; it is her.</summary>
        void DrawHer()
        {
            Unmake();
            float x = Mathf.Clamp(transform.position.x - Facing * 1f, arenaMinX, arenaMaxX);
            WrenDrawing = BossPart.Make("WrenDrawing", transform.parent, new Vector2(x, floorY + 0.55f), new Vector2(0.6f, 1.1f), InkMaterials.Lit("Archivist_Wren", new Color(0.40f, 0.36f, 0.46f)));
            WrenDrawing.OnHit = hit => false;
            _drawLeft = drawSeconds * 2f;
            _jabT = wrenDrawingJabSeconds;
            _jabbing = false;
        }

        void HuntHer(float dt)
        {
            if (Wren == null) return;
            var p = WrenDrawing.Position;
            float dx = Wren.Position.x - p.x;
            int dir = dx >= 0f ? 1 : -1;
            if (_jabbing)
            {
                if (_jabFrames-- > 0) return;   // her own wind-up, read before it lands
                if (!_jabHit)
                {
                    var box = new Vector2(p.x + dir * wrenDrawingReach * 0.5f, p.y);
                    if (_vitals == null) _vitals = Wren.GetComponent<WrenVitals>();
                    var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
                    var hits = new Collider2D[2];
                    int n = Physics2D.OverlapBox(box, new Vector2(wrenDrawingReach, 1f), 0f, filter, hits);
                    for (int i = 0; i < n; i++) if (hits[i].GetComponentInParent<WrenVitals>() is WrenVitals v && v.Damage(damage, p)) { _jabHit = true; break; }
                }
                _jabbing = false;
                _jabT = wrenDrawingJabSeconds;
                return;
            }
            if (Mathf.Abs(dx) > wrenDrawingReach * 0.8f) WrenDrawing.MoveTo(new Vector2(Mathf.Clamp(p.x + dir * wrenDrawingSpeed * dt, arenaMinX, arenaMaxX), p.y));
            _jabT -= dt;
            if (_jabT > 0f) return;
            WrenDrawingJabs++;
            _jabbing = true;
            _jabHit = false;
            _jabFrames = Mathf.Max(wrenDrawingTelegraphFrames, MinTelegraphFrames);
        }

        /// <summary>His quill hand, while it draws: struck, every drawing comes undone.</summary>
        bool StrikeHand()
        {
            if (!IsFightActive || !IsDrawing) return false;
            Unmade++;
            Unmake();
            TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up });
            Stagger(unmadeStaggerSeconds);
            Recover();
            return true;
        }

        void Unmake()
        {
            foreach (var d in _drawings) if (d != null) Destroy(d);
            _drawings.Clear();
            if (WrenDrawing != null) Destroy(WrenDrawing.gameObject);
            WrenDrawing = null;
            _drawLeft = 0f;
        }

        // ---- the frame --------------------------------------------------------------------------------------------------

        void DrawFrame()
        {
            FrameLeft = arenaMinX;
            FrameRight = arenaMaxX;
            _beatT = 0f;
            var mat = InkMaterials.Lit("Archivist_Frame", new Color(0.66f, 0.52f, 0.24f));
            _frameL = BossPart.Make("Frame_W", transform.parent, new Vector2(FrameLeft, floorY + 4f), new Vector2(0.5f, 8f), mat);
            _frameR = BossPart.Make("Frame_E", transform.parent, new Vector2(FrameRight, floorY + 4f), new Vector2(0.5f, 8f), mat);
            _frameL.OnHit = hit => PushFrame(-1, hit);
            _frameR.OnHit = hit => PushFrame(1, hit);
        }

        /// <summary>A Longstroke on an edge pushes that side back out.</summary>
        bool PushFrame(int side, HitInfo hit)
        {
            if (!IsLongstroke(hit)) return false;
            FramePushes++;
            if (side < 0) FrameLeft = Mathf.Max(arenaMinX, FrameLeft - framePushBack);
            else FrameRight = Mathf.Min(arenaMaxX, FrameRight + framePushBack);
            PlaceFrame();
            return true;
        }

        void Close(float dt)
        {
            _beatT += dt;
            if (_beatT >= beatSeconds)
            {
                _beatT -= beatSeconds;
                if (FrameRight - FrameLeft > frameMinWidth + 0.01f)
                {
                    float step = Mathf.Min(wingspan, (FrameRight - FrameLeft - frameMinWidth) * 0.5f);
                    FrameLeft += step;
                    FrameRight -= step;
                    PlaceFrame();
                }
            }
            // Outside the frame is the page, and the page hurts.
            if (Wren == null || (Wren.Position.x >= FrameLeft && Wren.Position.x <= FrameRight)) return;
            if (_vitals == null) _vitals = Wren.GetComponent<WrenVitals>();
            if (_vitals != null && _vitals.Damage(pageDamage, new Vector2((FrameLeft + FrameRight) * 0.5f, Wren.Position.y))) PageTakes++;
        }

        void PlaceFrame()
        {
            if (_frameL != null) _frameL.MoveTo(new Vector2(FrameLeft, floorY + 4f));
            if (_frameR != null) _frameR.MoveTo(new Vector2(FrameRight, floorY + 4f));
        }

        protected override Color TintColor() => new Color(0.46f, 0.40f, 0.34f);
    }
}
