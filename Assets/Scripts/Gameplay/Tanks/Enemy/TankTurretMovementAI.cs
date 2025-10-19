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

        [Header("Words")]
        [SerializeField] float turretAngleRangeOffset = 15f;
        [SerializeField] float turretTurnSpeedDeg;
        [SerializeField] int turretTargetTimer = 40;

        [SerializeField] bool alwaysEnabled = false;

        private float offset = 25;
        private float desiredAngle;
        private float CurrentLookingAngle { get => turretPivot.eulerAngles.z; }
        public override bool Enable { get; set; }

        private bool isTurretPointingAtDesired = false;

        void Start()
        {
            if (!turretPivot) Debug.LogError($"{name}: turretPivot not assigned");
            if (!target) Debug.LogError($"{name}: target not assigned");
            
            StartCoroutine(generateDesiredDirectionRoutine());
        }

        void Update()
        {
            if (Enable || alwaysEnabled)
                rotateTurretTowardsDesired();
        }

        private void generateDesiredLookingDirection()
        {
            Vector2 vectorFromTankToTarget = Utils.VectorFromOnePointToAnother(this.transform, target.transform);
            float randomOffsetAngle = Random.Range(-turretAngleRangeOffset, turretAngleRangeOffset);
            Vector2 desiredTurretLookingDirection = Utils.RotateVector(vectorFromTankToTarget, randomOffsetAngle);
            desiredAngle = Utils.VectorToAngle(desiredTurretLookingDirection) - 90;
            isTurretPointingAtDesired = false;
        }

        private void rotateTurretTowardsDesired()
        {
            if (isTurretPointingAtDesired)
                return;

            float step = turretTurnSpeedDeg * offset * Time.deltaTime;
            float angleToRotateTo = Mathf.MoveTowardsAngle(CurrentLookingAngle, desiredAngle, step);
            Utils.RotateTransform(ref turretPivot, angleToRotateTo);
            isTurretPointingAtDesired = angleToRotateTo == desiredAngle;
        }

        private IEnumerator generateDesiredDirectionRoutine()
        {
            while (true)
            {
                generateDesiredLookingDirection();
                for (int i = 0; i < turretTargetTimer; i++)
                    yield return null;
            }
        }
    }
}
