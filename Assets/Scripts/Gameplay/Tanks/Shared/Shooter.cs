using UnityEngine;
using Game.Gameplay.Projectiles;
using UnityEngine.Pool;
using System.Collections.Generic;
using System.Collections;
using Game.Core;

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
        [field: SerializeField] public Color BulletTint { get; private set; } = Color.white;

        private bool isCooldownActive = false;
        private int active;
        private Collider2D _collider;
        private ObjectPoolWrapper<Bullet> bulletPool;

        private void Awake()
        {
            bulletPool = new ObjectPoolWrapper<Bullet>(
                createFunc: createBullet,
                actionOnGet: activateWhenGettingBulletFromPool,
                actionOnRelease: b => b.gameObject.SetActive(false),
                //actionOnDestroy: b => Destroy(b.gameObject),
                maxSize: maxActive);

            _collider = GetComponentInParent<Collider2D>();
            BulletPrefab.MaxBounces = MaxBounces;
            applyTintToPrefab();
        }

        public void TryFire(Vector2 dir)
        {
            if (isCooldownActive) return;
            if (active >= maxActive) return;
            if (!BulletPrefab || !Muzzle) return;

            Bullet bullet = bulletPool.Get();

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
                bulletPool.Release(bullet);
            }
        }

        public void DestroyBullets()
        {
            bulletPool.DestroyAll();
            active = 0;
        }

        public void ReleaseBullets()
        {
            bulletPool.ReleaseAllActive();
            active = 0;
        }

        private Bullet createBullet()
        {
            Bullet bullet = Instantiate(BulletPrefab, Muzzle.position, Quaternion.identity);
            bullet.tag = isPlayer ? "Player" : "Enemy";
            bullet.SetOwner(this);
            applyTintToBullet(bullet);
            return bullet;
        }

        private void activateWhenGettingBulletFromPool(Bullet bullet)
        {
            ignoreCollisionWithBullet(bullet);
            bullet.transform.position = Muzzle.position;
            bullet.gameObject.SetActive(true);
            //applyTintToBullet(bullet);
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

        public void SetBulletTint(Color c)
        {
            BulletTint = c;
            applyTintToPrefab();
        }

        private void applyTintToPrefab()
        {
            if (BulletPrefab == null) return;
            var sr = BulletPrefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;
            float a = sr.color.a > 0f ? sr.color.a : 1f;
            sr.color = new Color(BulletTint.r, BulletTint.g, BulletTint.b, a);
        }

        private void applyTintToBullet(Bullet b)
        {
            if (b == null) return;
            var sr = b.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;
            float a = sr.color.a > 0f ? sr.color.a : 1f;
            sr.color = new Color(BulletTint.r, BulletTint.g, BulletTint.b, a);
        }
    }
}
