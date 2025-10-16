// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Tanks.Shared;
using UnityEngine.Tilemaps;

namespace Game.Gameplay.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        public int MaxBounces { get; set; } = 1;

        private Rigidbody2D _rb;
        private int _bounces;
        private Shooter _owner;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();        
        }

        public void SetOwner(Shooter s) => _owner = s;

        public void Launch(Vector2 velocity)
        {
            _rb.linearVelocity = velocity;
            _bounces = 0;
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            string layer = LayerMask.LayerToName(col.collider.gameObject.layer);
            // Hit a tank or a mine ? kill & despawn
            if (layer == "Tank" || layer == "Bullet" || layer == "Mines")
            {
                var h = col.collider.GetComponentInParent<Health>();
                if (h)
                    h.Kill();
                Despawn();
                return;
            }

            // Ricochet on walls
            if (layer == "Walls")
            {
                _bounces++;
                if (_bounces > MaxBounces)
                    Despawn();
                return;
            }

            // Everything else: just despawn (explosion, etc.)
            Despawn();
        }

        void Despawn()
        {
            _owner?.ReleaseBullet(this);
        }
    }
}
