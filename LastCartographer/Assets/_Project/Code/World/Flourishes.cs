using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    public enum FlourishKind { None, Crosshatch, Longstroke, Blot }

    /// <summary>
    /// Ink-spending specials (combat doc 4). One Flourish button; the direction held picks the move:
    ///   neutral  → Crosshatch  (3 pips) rapid 6-hit flurry forward
    ///   forward  → Longstroke  (3 pips) piercing thrust, 6 units, through enemies and thin walls
    ///   up       → Blot        (4 pips) burst around Wren; knockback; slows Smudges
    /// Flourish hits never refill ink. Wren is input-locked for the duration but keeps gravity.
    /// </summary>
    [RequireComponent(typeof(WrenController), typeof(Inkwell))]
    public sealed class Flourishes : MonoBehaviour
    {
        [Header("Costs (pips)")]
        public int crosshatchCost = 3;
        public int longstrokeCost = 3;
        public int blotCost = 4;

        [Header("Crosshatch")]
        public int crosshatchHits = 6;
        public int crosshatchInterval = 3;      // frames between hits
        public float crosshatchReach = 2.4f;
        public float crosshatchHeight = 1.4f;

        [Header("Longstroke")]
        public int longstrokeStartup = 4;
        public float longstrokeReach = 6f;
        public float longstrokeThickness = 0.8f;
        public int longstrokeDamage = 2;

        [Header("Blot")]
        public int blotStartup = 3;
        public float blotRadius = 2.5f;
        public float blotSlowSeconds = 2f;
        public float blotKnockback = 6f;

        [Header("Shared")]
        public float originHeight = 0.6f;
        public int recoveryFrames = 6;
        public int hitstopFrames = 2;
        public LayerMask hitMask;

        public FlourishKind Current { get; private set; }
        public bool IsBusy => Current != FlourishKind.None;
        public event Action<FlourishKind> Performed;
        public event Action<FlourishKind, IHittable> Landed;
        public event Action<FlourishKind> Refused;      // not enough ink

        WrenController _ctrl;
        Inkwell _ink;
        StrikeVisual _visual;
        int _frame, _hitsDone, _facing;
        readonly Collider2D[] _overlaps = new Collider2D[24];
        readonly HashSet<IHittable> _hitThisTick = new HashSet<IHittable>();

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _ink = GetComponent<Inkwell>();
            _visual = GetComponent<StrikeVisual>();
            if (hitMask.value == 0) hitMask = LayerMask.GetMask("Enemy", "Hittable");
        }

        void FixedUpdate()
        {
            if (_ctrl.Frozen) { Current = FlourishKind.None; return; }

            if (Current == FlourishKind.None)
            {
                if (_ctrl.Input == null || !_ctrl.Input.ConsumeFlourish()) return;
                var move = _ctrl.Input.Move;
                var kind = move.y > 0.5f ? FlourishKind.Blot
                         : Mathf.Abs(move.x) > 0.5f ? FlourishKind.Longstroke
                         : FlourishKind.Crosshatch;
                TryPerform(kind);
                return;
            }

            _frame++;
            switch (Current)
            {
                case FlourishKind.Crosshatch:
                    if (_frame % crosshatchInterval == 0 && _hitsDone < crosshatchHits)
                    {
                        _hitsDone++;
                        var dir = new Vector2(_facing, 0f);
                        var center = _ctrl.Position + Vector2.up * originHeight + dir * (crosshatchReach * 0.5f);
                        HitBox(center, new Vector2(crosshatchReach, crosshatchHeight), dir, 1, false);
                        _visual?.Slash(new Vector2(_facing, (_hitsDone % 2 == 0 ? 0.35f : -0.35f)));
                    }
                    if (_hitsDone >= crosshatchHits && _frame >= crosshatchHits * crosshatchInterval + recoveryFrames) End();
                    break;

                case FlourishKind.Longstroke:
                    if (_frame == longstrokeStartup)
                    {
                        var dir = new Vector2(_facing, 0f);
                        var center = _ctrl.Position + Vector2.up * originHeight + dir * (longstrokeReach * 0.5f);
                        HitBox(center, new Vector2(longstrokeReach, longstrokeThickness), dir, longstrokeDamage, false);
                        _visual?.Slash(dir);
                        _visual?.Burst(_ctrl.Position + Vector2.up * originHeight + dir * longstrokeReach, dir, 5);
                    }
                    if (_frame >= longstrokeStartup + recoveryFrames) End();
                    break;

                case FlourishKind.Blot:
                    if (_frame == blotStartup)
                    {
                        var origin = _ctrl.Position + Vector2.up * originHeight;
                        HitCircle(origin, blotRadius, 1);
                        _visual?.Burst(origin, Vector2.zero, 14, 11f);
                    }
                    if (_frame >= blotStartup + recoveryFrames) End();
                    break;
            }
        }

        public bool TryPerform(FlourishKind kind)
        {
            if (IsBusy || kind == FlourishKind.None) return false;
            int cost = kind == FlourishKind.Crosshatch ? crosshatchCost : kind == FlourishKind.Longstroke ? longstrokeCost : blotCost;
            if (!_ink.TrySpend(cost)) { Refused?.Invoke(kind); return false; }
            Current = kind;
            _frame = 0;
            _hitsDone = 0;
            _facing = _ctrl.Facing;
            _ctrl.LockInput(120, halt: true);   // plant feet; released by End()
            Performed?.Invoke(kind);
            return true;
        }

        void End()
        {
            Current = FlourishKind.None;
            _ctrl.UnlockInput();
        }

        void HitBox(Vector2 center, Vector2 size, Vector2 dir, int damage, bool refillInk)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            int n = Physics2D.OverlapBox(center, size, 0f, filter, _overlaps);
            Apply(n, dir, damage);
        }

        void HitCircle(Vector2 center, float radius, int damage)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            int n = Physics2D.OverlapCircle(center, radius, filter, _overlaps);
            _hitThisTick.Clear();
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                var h = _overlaps[i].GetComponentInParent<IHittable>();
                if (h == null || !_hitThisTick.Add(h)) continue;
                var away = ((Vector2)_overlaps[i].bounds.center - center);
                away = away.sqrMagnitude > 0.001f ? away.normalized : Vector2.up;
                var info = new HitInfo { Damage = damage, Direction = away, Source = gameObject };
                bool landed = h.TakeHit(info);
                if (h is Enemy e && !e.IsDead) { e.ApplySlow(blotSlowSeconds); }
                if (!landed) continue;
                any = true;
                Landed?.Invoke(Current, h);
            }
            if (any) Hitstop.Request(hitstopFrames + 2);
        }

        void Apply(int n, Vector2 dir, int damage)
        {
            _hitThisTick.Clear();
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                var h = _overlaps[i].GetComponentInParent<IHittable>();
                if (h == null || !_hitThisTick.Add(h)) continue;
                var info = new HitInfo { Damage = damage, Direction = dir, Source = gameObject };
                if (!h.TakeHit(info)) continue;
                any = true;
                Landed?.Invoke(Current, h);
            }
            if (any) Hitstop.Request(hitstopFrames);
        }
    }
}
