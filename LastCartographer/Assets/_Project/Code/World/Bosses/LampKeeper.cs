using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Lamp-Keeper (bible 6.1, CMB-11): a Remnant gannet fused to the fourth lighthouse's lamp.
    /// Tier I. She perches under the lamp and is out of reach; her beam sweeps the floor (jump it)
    /// and she dives at Wren (sidestep it), then sits grounded and hittable for a moment: that is
    /// the answer. Phase 2 doubles the sweeps, phase 3 brings both beams in from the sides.
    /// Drops Wingbeat; her lamp becomes a fast-travel beacon (handled by the arena reward).
    /// </summary>
    public sealed class LampKeeper : Boss
    {
        public enum Attack { None, Beam, Dive, DoubleBeam }
        public enum Move { Perch, Telegraph, Beam, Dive, Grounded, Return }

        [Header("Lamp-Keeper")]
        public float floorY = 0f;
        public float arenaHalfWidth = 9f;
        public float beamSpeed = 9f;
        public float beamWidth = 1.2f;
        public float beamHeight = 1.5f;        // a jump clears it
        public float beamColumnHeight = 8f;
        public int beamTelegraphFrames = 14;
        public int diveTelegraphFrames = 16;
        public float diveSpeed = 22f;
        public float returnSpeed = 10f;
        public float[] groundedSecondsByPhase = { 1.6f, 1.3f, 1.0f };
        public float perchPauseSeconds = 0.8f;
        public int beamDamage = 1;
        public LayerMask playerMask;

        public Move Current { get; private set; } = Move.Perch;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        public bool IsGroundedAndOpen => Current == Move.Grounded;

        Vector2 _perch;
        Vector2 _diveTarget;
        float _pause, _groundedLeft;
        bool _telegraphStarted;
        int _patternIndex;
        readonly Collider2D[] _overlaps = new Collider2D[4];
        Transform[] _beams = new Transform[2];
        Vector2[] _beamPos = new Vector2[2];
        int[] _beamDir = new int[2];
        int _activeBeams;

        static readonly Attack[] Phase1 = { Attack.Beam, Attack.Dive };
        static readonly Attack[] Phase2 = { Attack.Beam, Attack.Beam, Attack.Dive, Attack.Dive };
        static readonly Attack[] Phase3 = { Attack.DoubleBeam, Attack.Dive, Attack.Beam, Attack.Dive };

        protected override void Awake()
        {
            base.Awake();
            _perch = transform.position;
            if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
            for (int i = 0; i < 2; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                q.name = "Beam_" + i;
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(transform.parent != null ? transform.parent : null, true);
                q.transform.localScale = new Vector3(beamWidth * 0.5f, beamColumnHeight, 0.2f);
                q.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Dark;
                q.SetActive(false);
                _beams[i] = q.transform;
            }
        }

        protected override bool AcceptsHit(in HitInfo hit) => Current == Move.Grounded;

        protected override void OnFightStarted()
        {
            Current = Move.Perch;
            _pause = perchPauseSeconds;
            _patternIndex = 0;
        }

        protected override void OnFightReset()
        {
            Current = Move.Perch;
            CurrentAttack = Attack.None;
            HideBeams();
            _telegraphStarted = false;
            transform.position = _perch;
            Body.position = _perch;
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            // A phase opens with a dive so the line is heard while she is readable and reachable.
            if (phase > 1 && Current == Move.Perch) BeginAttack(Attack.Dive);
        }

        protected override void OnDefeated() { HideBeams(); }

        Attack[] Pattern => Phase >= 3 ? Phase3 : Phase >= 2 ? Phase2 : Phase1;

        /// <summary>Tests and tooling: start a specific attack from the perch.</summary>
        public void ForceAttack(Attack a)
        {
            if (Current != Move.Perch && Current != Move.Return) { Current = Move.Perch; transform.position = _perch; Body.position = _perch; }
            BeginAttack(a);
        }

        /// <summary>Tests and tooling: put her on the floor, open to hits, for a while.</summary>
        public void ForceGrounded(float seconds)
        {
            HideBeams();
            ClearTelegraph();
            var p = new Vector2(Wren != null ? Wren.Position.x : _perch.x, floorY + 0.5f);
            transform.position = p; Body.position = p;
            Current = Move.Grounded;
            _groundedLeft = seconds;
        }

        void BeginAttack(Attack a)
        {
            HideBeams();
            CurrentAttack = a;
            _telegraphStarted = false;
            Current = Move.Telegraph;
            if (a == Attack.Dive && Wren != null) _diveTarget = new Vector2(Wren.Position.x, floorY + 0.5f);
        }

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Perch:
                    _pause -= dt;
                    if (_pause <= 0f)
                    {
                        var pattern = Pattern;
                        BeginAttack(pattern[_patternIndex % pattern.Length]);
                        _patternIndex++;
                    }
                    break;

                case Move.Telegraph:
                {
                    int frames = CurrentAttack == Attack.Dive ? diveTelegraphFrames : beamTelegraphFrames;
                    // Wind-up: rise a little on the perch, flare.
                    transform.position = _perch + Vector2.up * 0.35f * Mathf.Sin(TelegraphLeft * 0.5f);
                    if (CurrentAttack != Attack.Dive) ShowBeams(CurrentAttack == Attack.DoubleBeam ? 2 : 1, preview: true);
                    if (Telegraph(ref _telegraphStarted, frames))
                    {
                        if (CurrentAttack == Attack.Dive)
                        {
                            if (Wren != null) _diveTarget = new Vector2(Wren.Position.x, floorY + 0.5f);
                            Current = Move.Dive;
                        }
                        else
                        {
                            ShowBeams(CurrentAttack == Attack.DoubleBeam ? 2 : 1, preview: false);
                            Current = Move.Beam;
                        }
                    }
                    break;
                }

                case Move.Beam:
                {
                    bool anyLeft = false;
                    for (int i = 0; i < _activeBeams; i++)
                    {
                        _beamPos[i].x += _beamDir[i] * beamSpeed * dt;
                        bool done = _beamDir[i] > 0 ? _beamPos[i].x > arenaHalfWidth + _perch.x : _beamPos[i].x < -arenaHalfWidth + _perch.x;
                        if (_activeBeams == 2 && Mathf.Abs(_beamPos[i].x - _perch.x) < beamWidth * 0.5f) done = true;   // converge at the centre
                        if (done) { _beams[i].gameObject.SetActive(false); continue; }
                        anyLeft = true;
                        _beams[i].position = new Vector3(_beamPos[i].x, floorY + beamColumnHeight * 0.5f, 0f);
                        BeamHit(_beamPos[i].x);
                    }
                    if (!anyLeft) { HideBeams(); Current = Move.Perch; _pause = perchPauseSeconds; }
                    break;
                }

                case Move.Dive:
                {
                    Vector2 pos = transform.position;
                    var to = _diveTarget - pos;
                    float step = diveSpeed * dt;
                    Face(to.x >= 0f ? 1 : -1);
                    if (to.magnitude <= step)
                    {
                        transform.position = _diveTarget; Body.position = _diveTarget;
                        Current = Move.Grounded;
                        _groundedLeft = groundedSecondsByPhase[Mathf.Clamp(Phase - 1, 0, groundedSecondsByPhase.Length - 1)];
                        Hitstop.Request(2);
                    }
                    else
                    {
                        var next = pos + to.normalized * step;
                        transform.position = next; Body.position = next;
                    }
                    break;
                }

                case Move.Grounded:
                    _groundedLeft -= dt;
                    if (_groundedLeft <= 0f) Current = Move.Return;
                    break;

                case Move.Return:
                {
                    Vector2 pos = transform.position;
                    var to = _perch - pos;
                    float step = returnSpeed * dt;
                    if (to.magnitude <= step)
                    {
                        transform.position = _perch; Body.position = _perch;
                        Current = Move.Perch;
                        _pause = perchPauseSeconds;
                        CurrentAttack = Attack.None;
                    }
                    else
                    {
                        var next = pos + to.normalized * step;
                        transform.position = next; Body.position = next;
                    }
                    break;
                }
            }
        }

        void ShowBeams(int count, bool preview)
        {
            if (_activeBeams != count)
            {
                _activeBeams = count;
                int facing = Wren != null && Wren.Position.x < _perch.x ? -1 : 1;
                // One beam starts on the far side from Wren and sweeps toward and past her; two start at both edges.
                _beamDir[0] = count == 2 ? 1 : -facing;
                _beamPos[0] = new Vector2(_perch.x + (count == 2 ? -arenaHalfWidth : facing * arenaHalfWidth), floorY);
                _beamDir[1] = -1;
                _beamPos[1] = new Vector2(_perch.x + arenaHalfWidth, floorY);
            }
            for (int i = 0; i < 2; i++)
            {
                bool on = i < count;
                _beams[i].gameObject.SetActive(on);
                if (!on) continue;
                _beams[i].position = new Vector3(_beamPos[i].x, floorY + beamColumnHeight * 0.5f, 0f);
                _beams[i].localScale = new Vector3(beamWidth * (preview ? 0.15f : 0.5f), beamColumnHeight, 0.2f);
            }
        }

        void HideBeams()
        {
            _activeBeams = 0;
            foreach (var b in _beams) if (b != null) b.gameObject.SetActive(false);
        }

        void BeamHit(float x)
        {
            var center = new Vector2(x, floorY + beamHeight * 0.5f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerMask, useTriggers = false };
            int n = Physics2D.OverlapBox(center, new Vector2(beamWidth, beamHeight), 0f, filter, _overlaps);
            for (int i = 0; i < n; i++)
            {
                var v = _overlaps[i].GetComponentInParent<WrenVitals>();
                if (v == null) continue;
                v.Damage(beamDamage, center);
                break;
            }
        }

        protected override Color TintColor()
        {
            var c = Current == Move.Grounded ? new Color(0.92f, 0.86f, 0.62f) : new Color(0.20f, 0.19f, 0.24f);
            if (IsTelegraphing) c = Color.Lerp(c, new Color(1f, 0.85f, 0.4f), 0.6f);
            return c;
        }

        void OnDestroy() { foreach (var b in _beams) if (b != null) Destroy(b.gameObject); }
    }
}
