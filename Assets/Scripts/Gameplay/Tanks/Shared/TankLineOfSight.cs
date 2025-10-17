using Assets.Scripts.Core;
using Game.Gameplay.Tanks.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    public class TankLineOfSight : MonoBehaviour
    {
        [SerializeField] float angle = 20;

        private Beam[] beams;
        private int numOfBeams;
        public bool PlayerInSight => beams.Any(beam => beam.PlayerInSight);
        public bool EnemyInSight => beams.Any(beam => beam.EnemyInSight);
        public event EventHandler ShootingOpportunityFound;

        void OnValidate()
        {
            if (angle < 0f) angle = 0f;
        }

        void Awake()
        {
            Shooter shooter = GetComponentInParent<Shooter>();
            numOfBeams = shooter ? shooter.MaxBounces + 1 : 1;
            beams = new Beam[numOfBeams];
            for (int i = 0; i < numOfBeams; i++)
                beams[i] = new Beam(angle);
        }

        void Update()
        {
            beams[0].Run(transform.position, transform.up, true);
            for (int i = 1; i < numOfBeams; i++)
            {
                if (beams[i - 1].HitPoint.HasValue)
                    beams[i].Run(beams[i - 1].HitPoint.Value, beams[i - 1].ReflectedHitDirection.Value);
            }

            checkShootingOpportunity();

            foreach (var beam in beams)
                drawBeamDebug(beam);
        }

        private void checkShootingOpportunity()
        {
            if (PlayerInSight && !EnemyInSight)
                OnShootingOpportunityFound(EventArgs.Empty);
        }

        protected virtual void OnShootingOpportunityFound(EventArgs e)
        {
            ShootingOpportunityFound?.Invoke(this, e);
        }

        private void drawBeamDebug(Beam beam)
        {
#if UNITY_EDITOR
            // visualize edges with current dynamic radius
            Debug.DrawRay(beam.Origin, beam.Left.normalized * beam.Radius, Color.green);
            Debug.DrawRay(beam.Origin, beam.Right.normalized * beam.Radius, Color.green);

            // visualize straight-ahead ray used to compute radius
            Debug.DrawRay(beam.Origin, beam.Direction.normalized * beam.Radius, Color.yellow);
#endif
        }

        private class Beam
        {
            private const float maxSightDistance = 40f;
            private const float epsilon = 0.002f;
            private readonly LayerMask wallMask = LayerMask.GetMask("Walls");
            private readonly LayerMask tankMask = LayerMask.GetMask("Tank");
            private readonly float angle = 0;

            public bool PlayerInSight { get => playersInSight > 0; }
            public bool EnemyInSight { get => enemiesInSight > 0; }
            public Vector2 Origin { get; private set; } 
            public Vector2 Direction { get; private set; } 
            public Vector2 Left => Utils.RotateVector(Direction, angle);
            public Vector2 Right => Utils.RotateVector(Direction, -angle);
            public Vector2? HitPoint { get; private set; }
            public Vector2? ReflectedHitDirection { get; private set; }
            public float Radius { get; private set; }

            private int playersInSight;
            private int enemiesInSight;

            public Beam(float angle)
            {
                if (angle < 0f) angle = 0f;
                this.angle = angle;
            }

            public void Run(Vector2 origin, Vector2 direction, bool isBeamFirstInChain = false)
            {
                Origin = isBeamFirstInChain ? origin : origin + direction * epsilon;
                Direction = direction;
                Radius = RadiusToNearestWall();
                updateSight(Radius);
            }

            private float RadiusToNearestWall()
            {
                RaycastHit2D hit = Physics2D.Raycast(Origin, Direction, maxSightDistance, wallMask);
                if (hit.collider)
                {
                    HitPoint = hit.point;
                    ReflectedHitDirection = Vector2.Reflect(Direction, hit.normal);
                    return hit.distance;
                }
                else
                {
                    HitPoint = null;
                    ReflectedHitDirection = null;
                    return maxSightDistance;
                }
            }

            private void updateSight(float radius)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(Origin, radius, tankMask);

                int playerCount = 0;
                int enemyCount = 0;

                float halfFovDeg = angle;
                Vector2 forward = Direction.normalized;

                foreach (var col in hits)
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

                    if (col.CompareTag("Player")) playerCount++;
                    else if (col.CompareTag("Enemy")) enemyCount++;
                }

                playersInSight = playerCount;
                enemiesInSight = enemyCount;
                //Debug.Log($"{enemiesInSight}, {PlayerInSight}");
            }
        }        
    }
}
