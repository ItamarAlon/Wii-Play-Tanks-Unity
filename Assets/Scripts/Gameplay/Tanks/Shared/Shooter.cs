// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Projectiles;

namespace Game.Gameplay.Tanks.Shared
{
    public class Shooter : MonoBehaviour
    {
        private const int PLAYER_BULLET_LAYER = 8;
        private const int ENEMY_BULLET_LAYER = 9;

        public Transform muzzle;
        public Bullet bulletPrefab;
        public float muzzleSpeed = 14f;
        public float cooldown = 0.25f;
        public int maxActive = 4;
        public bool isPlayer;

        private float _cooldownTimer;
        private int _active;

        public void TryFire(Vector2 dir)
        {
            if (_cooldownTimer > 0f) return;
            if (isPlayer && _active >= maxActive) return;
            if (!bulletPrefab || !muzzle) return;

            var b = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            b.SetOwner(this);

            if (isPlayer)
                b.gameObject.layer = PLAYER_BULLET_LAYER;
            else
                b.gameObject.layer = ENEMY_BULLET_LAYER;

            b.Launch(dir.normalized * muzzleSpeed);
            _active++;
            _cooldownTimer = cooldown;
        }

        public void NotifyBulletDespawned()
        {
            if (_active > 0) _active--;
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }
    }
}
