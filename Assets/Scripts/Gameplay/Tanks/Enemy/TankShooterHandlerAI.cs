using Assets.Scripts.Core;
using System;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public class TankShooterHandlerAI : EnemyAI
    {
        [Header("Wedge")]
        [SerializeField, Tooltip("Half-angle to each side, in degrees")]
        float angle = 15f;
        [SerializeField, Tooltip("Wedge reach in world units")]
        float radius = 4f;
        [SerializeField] LayerMask detectionMask;

        private RaycastHit2D ray;
        private PolygonCollider2D polygonCollider;
        private Vector2 Origin { get => transform.position; }
        private Vector2 Up { get => transform.up; }
        private Vector2 Left { get => GeneralFunc.RotateVector(Up, angle); }
        private Vector2 Right { get => GeneralFunc.RotateVector(Up, -angle); }
        private int enemiesInSight = 0;

        public bool EnemyInSight { get => enemiesInSight > 0; }
        public bool PlayerInSight { get; private set; } = false;

        void Awake()
        {
            polygonCollider = GetComponent<PolygonCollider2D>();
            polygonCollider.isTrigger = true;
            polygonCollider.pathCount = 1;
        }

        void OnValidate()
        {
            if (angle < 0f) angle = 0f;
            if (radius < 0f) radius = 0f;
            if (polygonCollider == null) polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider != null) polygonCollider.isTrigger = true;
        }

        void FixedUpdate()
        {
            updateWedgePolygon();
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            handleTrigger(other, () => PlayerInSight = true, () => enemiesInSight++);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            handleTrigger(other, () => PlayerInSight = false, () => enemiesInSight--);
        }

        private void handleTrigger(Collider2D other, Action ifPlayer, Action ifEnemy)
        {
            if (other)
            {
                if (other.CompareTag("Player"))
                    ifPlayer.Invoke();
                else if (other.CompareTag("Enemy"))
                    ifEnemy.Invoke();
            }
        }

        private void updateWedgePolygon()
        {
            if (!polygonCollider) return;

            // Build triangle in WORLD space
            Vector2 a = Origin + GeneralFunc.SetMagnitude(Left, radius);
            Vector2 b = Origin + GeneralFunc.SetMagnitude(Right, radius);

            // Convert to LOCAL space of the collider’s transform
            var colliderTransform = polygonCollider.transform;
            Vector2 oL = colliderTransform.InverseTransformPoint(Origin);
            Vector2 aL = colliderTransform.InverseTransformPoint(a);
            Vector2 bL = colliderTransform.InverseTransformPoint(b);

            // If you use collider.offset, subtract it (keep polygon around offset)
            if (polygonCollider.offset != Vector2.zero)
            {
                oL -= polygonCollider.offset;
                aL -= polygonCollider.offset;
                bL -= polygonCollider.offset;
            }

            // Order: origin -> left edge -> right edge (consistent winding)
            // Choose ordering to match your chosen side; here we use +angle as "left"
            var points = new Vector2[3] { oL, aL, bL };
            polygonCollider.SetPath(0, points);
        }
    }
}
