// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Projectiles;
using UnityEngine.Pool;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace Game.Gameplay.Tanks.Shared
{
    public class Shooter : MonoBehaviour
    {
        public Transform muzzle;
        public Bullet bulletPrefab;
        public float muzzleSpeed = 6f;
        public float cooldown = 0.25f;
        public int maxActive = 4;
        public bool isPlayer;
        public int maxBounces = 2;

        private float _cooldownTimer;
        private int _active;
        private Collider2D _collider;
        private ObjectPool<Bullet> _bulletPool;
        private List<Bullet> activeBullets = new List<Bullet>();

        private void Awake()
        {
            _bulletPool = new ObjectPool<Bullet>(
                createFunc: createBullet, 
                actionOnGet: activateWhenGettingBulletFromPool,
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                maxSize: maxActive);

            _collider = GetComponentInParent<Collider2D>();
            bulletPrefab.MaxBounces = maxBounces;
        }

        public void TryFire(Vector2 dir)
        {
            if (_cooldownTimer > 0f) return;
            if (_active >= maxActive) return;
            if (!bulletPrefab || !muzzle) return;

            Bullet bullet = _bulletPool.Get();
            activeBullets.Add(bullet);

            bullet.Launch(dir.normalized * muzzleSpeed);
            _active++;
            _cooldownTimer = cooldown;
        }

        public void TryFire()
        {
            TryFire(muzzle.up);
        }

        public void ReleaseBullet(Bullet bullet)
        {
            if (bullet == null) return;
            if (_active > 0)
            {
                _active--;
                _bulletPool.Release(bullet);
                activeBullets.Remove(bullet);
            }
        }

        public void ClearBullets()
        {
            _bulletPool.Clear();
            foreach (Bullet bullet in activeBullets)
            {
                Destroy(bullet.gameObject);
            }
            activeBullets.Clear();
            _active = 0;
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

        private void activateWhenGettingBulletFromPool(Bullet bullet)
        {
            ignoreCollisionWithBullet(bullet);
            bullet.transform.position = muzzle.position;
            bullet.gameObject.SetActive(true);
        }

        private void ignoreCollisionWithBullet(Bullet bullet)
        {
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(bulletCollider, _collider);
        }

        private void OnDestroy()
        {
            ClearBullets();
        }
    }
}
