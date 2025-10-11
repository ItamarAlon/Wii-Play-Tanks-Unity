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
        public int maxBounces = 2;
        public float lifeSeconds = 10f;

        private Rigidbody2D _rb;
        private int _bounces;
        private Shooter _owner;
        private float _life;
        private enum TankType { Player, Enemy};

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
                if (h)
                    h.Kill();
                Despawn();
                return;
            }

            // Ricochet on walls
            if (ln == "Walls")
            {
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
            _owner?.ReleaseBullet(this);
        }

        private TankType playerOrEnemy(GameObject obj)
        {
            string layerStr = LayerMask.LayerToName(obj.layer);

            switch (layerStr)
            {
                case "PlayerBullet":
                case "Player":
                    return TankType.Player;
                case "EnemyBullet":
                case "Enemy":
                    return TankType.Enemy;
                default:
                    return 0;
            }
        }
    }
}
