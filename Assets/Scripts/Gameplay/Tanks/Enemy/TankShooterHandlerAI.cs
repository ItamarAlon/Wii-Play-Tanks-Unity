using Assets.Scripts.Core;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    public class TankShooterHandlerAI : EnemyAI
    {
        [Header("Masks / Limits")]
        //[SerializeField] LayerMask detectionMask;     // Player/Enemy layers
        [SerializeField] LayerMask wallMask;          // Walls/Bricks that block sight & define radius
        [SerializeField] float maxSightDistance = 40f; // used if no wall is hit straight ahead

        // Results
        public bool PlayerInSight { get; private set; } = false;
        public bool EnemyInSight { get => enemiesInSight > 0; }
        int enemiesInSight;

        // Reuse buffer to avoid GC
        Collider2D[] _hits = new Collider2D[32];

        // Geometry from this muzzle transform
        Vector2 Origin => transform.position;
        Vector2 Up => transform.up;
        Vector2 Left => Utils.RotateVector(Up, AngleDeg);   // if you expose AngleDeg in EnemyAI, else compute below
        Vector2 Right => Utils.RotateVector(Up, -AngleDeg);

        // If your project doesn’t already expose the half-angle somewhere, keep one here:
        [SerializeField] float AngleDeg = 15f;

        void OnValidate()
        {
            if (maxSightDistance < 0f) maxSightDistance = 0f;
            if (AngleDeg < 0f) AngleDeg = 0f;
        }

        void Update()
        {
            // 1) Get dynamic radius = nearest wall distance along Up (or cap)
            float radius = RadiusToNearestWall();

            // 2) Update sight flags using overlap + angular + LOS checks
            UpdateSight_NoCollider(radius);

#if UNITY_EDITOR
            // visualize edges with current dynamic radius
            Debug.DrawRay(Origin, Left.normalized * radius, Color.green);
            Debug.DrawRay(Origin, Right.normalized * radius, Color.green);

            // visualize straight-ahead ray used to compute radius
            Debug.DrawRay(Origin, Up.normalized * radius, Color.yellow);
#endif
        }

        float RadiusToNearestWall()
        {
            // Correct signature: (origin, direction, distance, layerMask)
            RaycastHit2D hit = Physics2D.Raycast(Origin, Up, wallMask);
            return hit.collider ? hit.distance : maxSightDistance;
        }

        void UpdateSight_NoCollider(float radius)
        {
            _hits = Physics2D.OverlapCircleAll(Origin, radius);

            bool sawPlayer = false;
            int enemyCount = 0;

            // If Left/Right are derived from AngleDeg as above, halfFov = AngleDeg.
            // If you compute Left/Right elsewhere, keep this robust calc:
            float halfFovDeg = AngleDeg; // or: HalfFovFromEdgesDeg(Left, Right)
            Vector2 forward = Up.normalized;

            foreach (var col in _hits)
            {
                if (!col) continue;
               
                Vector2 to = Utils.VectorFromOnePointToAnother(Origin, col.ClosestPoint(Origin));
                float sqr = to.sqrMagnitude;
                if (sqr < 1e-8f) continue;

                Vector2 dir = to / Mathf.Sqrt(sqr);

                // Angular gate: within ±halfFov around Up?
                float signed = Vector2.SignedAngle(forward, dir);
                if (Mathf.Abs(signed) > halfFovDeg + 0.0001f) continue;

                // LOS gate: blocked by walls?
                float dist = Mathf.Sqrt(sqr);
                if (Physics2D.Raycast(Origin, dir, dist, wallMask)) continue;

                // Tally
                if (col.CompareTag("Player")) sawPlayer = true;
                else if (col.CompareTag("Enemy")) enemyCount++;
            }

            PlayerInSight = sawPlayer;
            enemiesInSight = enemyCount;
            Debug.Log($"{enemiesInSight}, {PlayerInSight}");
        }

        // If you ever need it (for asymmetric edges), use this instead of AngleDeg:
        static float HalfFovFromEdgesDeg(Vector2 left, Vector2 right)
        {
            if (left == Vector2.zero || right == Vector2.zero) return 0f;
            float aL = Mathf.Atan2(left.y, left.x) * Mathf.Rad2Deg;
            float aR = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
            float span = Mathf.Abs(Mathf.DeltaAngle(aL, aR));
            return 0.5f * span;
        }
    }
}
