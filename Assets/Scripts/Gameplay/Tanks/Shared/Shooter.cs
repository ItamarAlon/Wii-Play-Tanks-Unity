// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Projectiles;
using UnityEngine.Pool;
using System.Collections.Generic;
using System.Collections;

namespace Game.Gameplay.Tanks.Shared
{
    public class Shooter : MonoBehaviour
    {
        [field: SerializeField] public Transform Muzzle { get; private set; }
        [field: SerializeField] public Bullet BulletPrefab { get; private set; }
        [field: SerializeField] public float MuzzleSpeed { get; private set; } = 3.76f;
        [field: SerializeField] public int MaxBounces { get; private set; } = 2;

        [SerializeField] float cooldownSeconds = 0.25f;
        [SerializeField] int maxActive = 4;
        [SerializeField] bool isPlayer;

        private bool isCooldownActive = false;
        private int active;
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
            BulletPrefab.MaxBounces = MaxBounces;
        }

        public void TryFire(Vector2 dir)
        {
            if (isCooldownActive) return;
            if (active >= maxActive) return;
            if (!BulletPrefab || !Muzzle) return;

            Bullet bullet = _bulletPool.Get();
            activeBullets.Add(bullet);

            bullet.Launch(dir.normalized * MuzzleSpeed);
            active++;
            startCooldown();
        }

        private void startCooldown()
        {
            StartCoroutine(waitCooldownRoutine());
        }

        private IEnumerator waitCooldownRoutine()
        {
            isCooldownActive = true;
            yield return new WaitForSeconds(cooldownSeconds);
            isCooldownActive = false;
        }

        public void TryFire()
        {
            TryFire(Muzzle.up);
        }

        public void ReleaseBullet(Bullet bullet)
        {
            if (!bullet) return;
            if (active > 0)
            {
                active--;
                _bulletPool.Release(bullet);
                activeBullets.Remove(bullet);
            }
        }

        public void DestroyBullets()
        {
            activeBullets.ForEach(bullet => Destroy(bullet.gameObject));
            activeBullets.Clear();
            _bulletPool.Clear();
            active = 0;
        }

        public void ClearBullets()
        {
            activeBullets.ForEach(_bulletPool.Release);
            activeBullets.Clear();
            active = 0;
        }

        private Bullet createBullet()
        {
            Bullet bullet = Instantiate(BulletPrefab, Muzzle.position, Quaternion.identity);
            bullet.tag = isPlayer ? "Player" : "Enemy";
            bullet.SetOwner(this);
            return bullet;
        }

        private void activateWhenGettingBulletFromPool(Bullet bullet)
        {
            ignoreCollisionWithBullet(bullet);
            bullet.transform.position = Muzzle.position;
            bullet.gameObject.SetActive(true);
        }

        private void ignoreCollisionWithBullet(Bullet bullet)
        {
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(bulletCollider, _collider);
        }

        private void OnDestroy()
        {
            DestroyBullets();
        }
    }
}
