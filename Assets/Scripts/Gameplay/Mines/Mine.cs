// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;

namespace Game.Gameplay.Mines
{
    public class Mine : MonoBehaviour
    {
        public float autoDetonateDelay = 3.0f;
        public Explosion explosionPrefab;
        public float explosionRadius = 1.8f;
        private float _timer;

        void OnEnable() { _timer = autoDetonateDelay; }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f) Detonate();
        }

        void OnCollisionEnter2D(Collision2D col) { Detonate(); }

        public void Detonate()
        {
            if (explosionPrefab)
            {
                var ex = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                ex.radius = explosionRadius;
            }
            Destroy(gameObject);
        }
    }
}
