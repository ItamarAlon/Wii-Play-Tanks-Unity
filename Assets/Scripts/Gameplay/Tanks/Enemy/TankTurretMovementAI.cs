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

        [SerializeField] bool alwaysEnabled = false;

        private float AnglePerFrame => word39_TurretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
        private float fps = 25;
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
            float randomOffsetAngle = Random.Range(-word29_TurretAngleOffset, word29_TurretAngleOffset);
            Vector2 desiredTurretLookingDirection = Utils.RotateVector(vectorFromTankToTarget, randomOffsetAngle);
            desiredAngle = Utils.VectorToAngle(desiredTurretLookingDirection) - 90;
            isTurretPointingAtDesired = false;
        }

        private void rotateTurretTowardsDesired()
        {
            if (isTurretPointingAtDesired)
                return;

            float step = AnglePerFrame * fps * Time.deltaTime;
            float angleToRotateTo = Mathf.MoveTowardsAngle(CurrentLookingAngle, desiredAngle, step);
            Utils.RotateTransform(ref turretPivot, angleToRotateTo);
            isTurretPointingAtDesired = angleToRotateTo == desiredAngle;
        }

        private IEnumerator generateDesiredDirectionRoutine()
        {
            while (true)
            {
                generateDesiredLookingDirection();
                for (int i = 0; i < word40_TurretTargetTimer; i++)
                    yield return null;
            }
        }
    }
}
