using System;
using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The needle-quill. Three directions, startup/active/recovery frames, one hit per target per
    /// swing, pogo on a landed down-strike, hitstop, and ink on hit. Combat doc 2.1.
    /// </summary>
    [RequireComponent(typeof(WrenController))]
    public sealed class QuillStrike : MonoBehaviour
    {
        public float reach = 2.2f;
        public float thickness = 0.9f;
        public float originHeight = 0.6f;
        public int startupFrames = 3;
        public int activeFrames = 4;
        public int recoveryFrames = 8;
        public int damage = 1;
        public int hitstopFrames = 2;
        public LayerMask hitMask;
        [Tooltip("Frames after a swing ends in which the next forward press continues the combo.")]
        public int comboWindowFrames = 18;

        /// <summary>The forward combo (combat doc 2.1 and 5). Set by the Charter; null means the base fields.</summary>
        public ComboStep[] Combo { get; set; }
        /// <summary>Index of the combo step the current or last forward swing used.</summary>
        public int ComboIndex { get; private set; }
        int _comboWindowLeft;

        public enum Phase { Idle, Startup, Active, Recovery }
        public Phase Current => _phase;
        public Vector2 Direction => _dir;
        public bool IsBusy => _phase != Phase.Idle;
        public event Action<Vector2> Swung;
        public event Action<IHittable> Landed;

        WrenController _ctrl;
        Inkwell _ink;
        Phase _phase;
        int _framesLeft;
        Vector2 _dir = Vector2.right;
        readonly Collider2D[] _overlaps = new Collider2D[16];
        readonly HashSet<IHittable> _hitThisSwing = new HashSet<IHittable>();

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _ink = GetComponent<Inkwell>();
            if (Application.isPlaying && GetComponent<StrikeVisual>() == null)
                gameObject.AddComponent<StrikeVisual>();   // scenes saved before the visual existed
        }

        // Frame data of the swing in progress (a combo step for forward swings, the base fields otherwise).
        float _reach, _thickness; int _active, _recovery, _damage; float _knockback = 1f; bool _pulls;
        /// <summary>Colour each landed strike takes (the Remnant Charter); set by the Charter.</summary>
        public float Drain { get; set; }

        void FixedUpdate()
        {
            if (_ctrl.Frozen) { _phase = Phase.Idle; return; }
            if (_ctrl.IsDashing) { _phase = Phase.Idle; return; }   // dash cancels recovery

            if (_phase == Phase.Idle)
            {
                if (_comboWindowLeft > 0) _comboWindowLeft--;
                if (_ctrl.Input != null && _ctrl.Input.ConsumeAttack())
                {
                    var move = _ctrl.Input.Move;
                    if (move.y > 0.5f) _dir = Vector2.up;
                    else if (!_ctrl.IsGrounded && move.y < -0.5f) _dir = Vector2.down;
                    else _dir = new Vector2(_ctrl.Facing, 0f);
                    BeginSwing();
                }
                return;
            }

            if (_phase == Phase.Active) DoHits();

            _framesLeft--;
            if (_framesLeft > 0) return;
            switch (_phase)
            {
                case Phase.Startup:  _phase = Phase.Active;   _framesLeft = _active;   DoHits(); break;
                case Phase.Active:   _phase = Phase.Recovery; _framesLeft = _recovery; break;
                case Phase.Recovery: _phase = Phase.Idle; _comboWindowLeft = comboWindowFrames; break;
            }
        }

        void BeginSwing()
        {
            bool forward = _dir.y == 0f;
            int startup = startupFrames;
            _reach = reach; _thickness = thickness; _active = activeFrames; _recovery = recoveryFrames; _damage = damage; _knockback = 1f; _pulls = false;
            if (forward && Combo != null && Combo.Length > 0)
            {
                ComboIndex = _comboWindowLeft > 0 ? (ComboIndex + 1) % Combo.Length : 0;
                var s = Combo[ComboIndex];
                _reach = s.Reach; _thickness = s.Thickness > 0f ? s.Thickness : thickness;
                startup = s.Startup; _active = s.Active; _recovery = s.Recovery; _damage = s.Damage; _knockback = s.Knockback; _pulls = s.Pulls;
            }
            else ComboIndex = 0;
            _comboWindowLeft = 0;
            _phase = Phase.Startup;
            _framesLeft = Mathf.Max(1, startup);
            _hitThisSwing.Clear();
            Swung?.Invoke(_dir);
        }

        public void GetHitbox(out Vector2 center, out Vector2 size)
        {
            float r = _phase == Phase.Idle ? reach : _reach;
            float t = _phase == Phase.Idle ? thickness : _thickness;
            var origin = _ctrl.Position + Vector2.up * originHeight;
            center = origin + _dir * (r * 0.5f);
            size = _dir.x != 0f ? new Vector2(r, t) : new Vector2(t, r);
        }

        void DoHits()
        {
            GetHitbox(out var center, out var size);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            int n = Physics2D.OverlapBox(center, size, 0f, filter, _overlaps);
            bool landedAny = false;
            for (int i = 0; i < n; i++)
            {
                var h = _overlaps[i].GetComponentInParent<IHittable>();
                if (h == null || _hitThisSwing.Contains(h)) continue;
                _hitThisSwing.Add(h);
                var info = new HitInfo { Damage = _damage, Direction = _dir, Source = gameObject, Knockback = _knockback, Pulls = _pulls, Drain = Drain };
                if (!h.TakeHit(info)) continue;
                landedAny = true;
                _ink?.AddFromHit(1);
                Landed?.Invoke(h);
            }
            if (!landedAny) return;
            Core.Hitstop.Request(hitstopFrames);
            if (_dir.y < 0f) _ctrl.Pogo();
        }

        void OnDrawGizmosSelected()
        {
            if (_ctrl == null) return;
            GetHitbox(out var c, out var s);
            Gizmos.color = _phase == Phase.Active ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(c, s);
        }
    }
}
