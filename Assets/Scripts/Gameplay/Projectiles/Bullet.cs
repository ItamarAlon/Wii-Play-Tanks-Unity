// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Tanks.Shared;

namespace Game.Gameplay.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        public int maxBounces = 2;
        public float lifeSeconds = 10f;

        private Rigidbody2D _rb;
        private int _bounces;
        private Shooter _owner;
        private float _life;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _life = lifeSeconds;
        }

        public void SetOwner(Shooter s) => _owner = s;

        public void Launch(Vector2 velocity)
        {
            _rb.linearVelocity = velocity;
            _bounces = 0;
            _life = lifeSeconds;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Despawn();
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            string ln = LayerMask.LayerToName(col.collider.gameObject.layer);

            // Hit a tank or a mine ? kill & despawn
            if (ln == "Player" || ln == "Enemy" || ln == "Mines")
            {
                var h = col.collider.GetComponentInParent<Health>();
                if (h) h.Kill();
                Despawn();
                return;
            }

            // Ricochet on walls
            if (ln == "Walls")
            {
                // reflect velocity about the contact normal
                //Vector2 n = col.contacts[0].normal;
                //_rb.linearVelocity = Vector2.Reflect(_rb.linearVelocity, n);

                _bounces++;
                if (_bounces >= maxBounces) 
                    Despawn();
                return;
            }

            // Everything else: just despawn (explosion, etc.)
            Despawn();
        }

        void Despawn()
        {
            _owner?.NotifyBulletDespawned();
            Destroy(gameObject);
        }
    }
}
