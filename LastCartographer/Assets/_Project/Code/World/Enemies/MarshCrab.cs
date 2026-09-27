using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Saltmarrow walker. Patrols a platform, turns at edges and walls, hops when Wren is near
    /// (its telegraph). Shelled: side and upward strikes bounce off; only the down-strike pogo hurts it.
    /// Answer: Pogo.
    /// </summary>
    public sealed class MarshCrab : Enemy
    {
        [SerializeField] float _walkSpeed = 2.2f;
        [SerializeField] float _hopVelocity = 6f;
        [SerializeField] float _hopRange = 3.5f;
        [SerializeField] float _hopCooldown = 1.6f;
        [SerializeField] LayerMask _groundMask;

        float _hopT;
        bool _grounded;

        protected override void Awake()
        {
            base.Awake();
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
        }

        protected override bool AcceptsHit(in HitInfo hit) => hit.Direction.y < -0.5f;

        protected override void Tick(float dt)
        {
            _grounded = Physics2D.Raycast(new Vector2(Collider.bounds.center.x, Collider.bounds.min.y + 0.02f), Vector2.down, 0.08f, _groundMask).collider != null;
            _hopT += dt;

            if (_grounded)
            {
                if (!GroundAhead(0.1f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask)) Face(-Facing);

                if (Wren != null && _hopT >= _hopCooldown)
                {
                    var toWren = Wren.Position - (Vector2)transform.position;
                    if (Mathf.Abs(toWren.x) < _hopRange && Mathf.Abs(toWren.y) < 2f)
                    {
                        Face(toWren.x >= 0f ? 1 : -1);
                        Body.linearVelocity = new Vector2(Facing * _walkSpeed * 1.5f, _hopVelocity);
                        _hopT = 0f;
                        return;
                    }
                }
                Body.linearVelocity = new Vector2(Facing * _walkSpeed, Body.linearVelocity.y);
            }
        }

        protected override Color TintColor() => new Color(0.62f, 0.34f, 0.26f);
    }
}
