using Assets.Scripts.Core;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    public class TankTurretMovementAI : EnemyAI
    {
        [Header("Prefab Refs")]
        [SerializeField] Transform turretPivot;
        [SerializeField] Transform target;

        [Header("Words from doc")]
        [SerializeField] float word29_TurretAngleOffset = 15f;
        [SerializeField] float word39_TurretTurnSpeedRadPerFrame = 0.08f;
        [SerializeField] int word40_TurretTargetTimer = 40;

        private float fps = 60;
        private Vector2 desiredTurretLookingDirection;
        private int frameCounter = 0;

        void Awake()
        {
            if (turretPivot)
                desiredTurretLookingDirection = turretPivot.up;
            else
                desiredTurretLookingDirection = Vector2.zero;
        }

        void Update()
        {
            if (frameCounter == 0)
                generateLookingDirectionAndSlowlyRotateTurretTowardsIt();
            else if (frameCounter == word40_TurretTargetTimer)
                frameCounter = -1;

            frameCounter++;
        }

        private void generateLookingDirectionAndSlowlyRotateTurretTowardsIt()
        {
            Vector2 vectorFromTankToTarget = GeneralFunc.VectorFromOnePointToAnother(this.transform, target.transform);
            float randomOffsetAngle = Random.Range(-word29_TurretAngleOffset, word29_TurretAngleOffset);
            desiredTurretLookingDirection = GeneralFunc.RotateVector(vectorFromTankToTarget, randomOffsetAngle);
            rotateTurretTowardDesiredSlowly();
        }

        private bool rotateTurretTowardDesired(float step)
        {
            if (isTurretLookingAtDesired())
                return true;

            float deltaAngle = Vector2.Angle(turretPivot.up, desiredTurretLookingDirection);

            if (Mathf.Abs(deltaAngle) <= step)
            {
                turretPivot.up = desiredTurretLookingDirection;
                return true;
            }
            else
            {
                step *= Mathf.Sign(deltaAngle);
                turretPivot.up = GeneralFunc.RotateVector(turretPivot.up, step);
                return false;
            }
        }

        private IEnumerator rotateTurretTowardDesiredRoutine()
        {
            float anglePerFrame = word39_TurretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
            float step = anglePerFrame * fps * Time.deltaTime;

            while (!rotateTurretTowardDesired(step))
            {
                yield return null;
            }
        }

        private void rotateTurretTowardDesiredSlowly()
        {
            StartCoroutine(rotateTurretTowardDesiredRoutine());
        }

        private bool isTurretLookingAtDesired()
        {
            return (Vector2)turretPivot.up == desiredTurretLookingDirection;
        }
    }
}
