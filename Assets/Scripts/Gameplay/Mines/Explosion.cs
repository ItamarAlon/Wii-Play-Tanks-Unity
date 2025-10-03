// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;

namespace Game.Gameplay.Mines
{
    public class Explosion : MonoBehaviour
    {
        public float radius = 1.8f;
        public float life = 0.1f;

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
