// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Projectiles;
using UnityEngine.Pool;

namespace Game.Gameplay.Tanks.Shared
{
    public class Shooter : MonoBehaviour
    {
        public Transform muzzle;
        public Bullet bulletPrefab;
        public float muzzleSpeed = 14f;
        public float cooldown = 0.25f;
        public int maxActive = 4;
        public bool isPlayer;

        private float _cooldownTimer;
        private int _active;
        private ObjectPool<Bullet> _bulletPool;

        private void Awake()
        {
            _bulletPool = new ObjectPool<Bullet>(
                createFunc: createBullet, 
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false));
        }

        public void TryFire(Vector2 dir)
        {
            if (_cooldownTimer > 0f) return;
            if (_active >= maxActive) return;
            if (!bulletPrefab || !muzzle) return;

            Bullet bullet = _bulletPool.Get();
            bullet.transform.position = muzzle.position;

            bullet.Launch(dir.normalized * muzzleSpeed);
            _active++;
            _cooldownTimer = cooldown;
        }

        public void ReleaseBullet(Bullet bullet)
        {
            if (_active > 0)
            {
                _active--;
                _bulletPool.Release(bullet);
            }
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        private Bullet createBullet()
        {
            Bullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            bullet.SetOwner(this);

            if (isPlayer)
                bullet.gameObject.layer = LayerMask.NameToLayer("PlayerBullet");
            else
                bullet.gameObject.layer = LayerMask.NameToLayer("EnemyBullet");

            return bullet;
        }
    }
}
