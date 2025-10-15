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

        private float fps = 3;
        private float step;
        private float desiredAngle;
        private float CurrentLookingAngle { get => turretPivot.eulerAngles.z; }
        private bool isTurretPointingAtDesired = false;

        void Awake()
        {
            float anglePerFrame = word39_TurretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
            step = anglePerFrame * fps * Time.deltaTime;
        }

        void Start()
        {
            StartCoroutine(generateDesiredDirectionRoutine());
        }

        void Update()
        {
            rotateTurretTowardsDesired();
        }

        private void generateDesiredLookingDirection()
        {
            Vector2 vectorFromTankToTarget = GeneralFunc.VectorFromOnePointToAnother(this.transform, target.transform);
            float randomOffsetAngle = Random.Range(-word29_TurretAngleOffset, word29_TurretAngleOffset);
            Vector2 desiredTurretLookingDirection = GeneralFunc.RotateVector(vectorFromTankToTarget, randomOffsetAngle);
            desiredAngle = GeneralFunc.VectorToAngle(desiredTurretLookingDirection) - 90;
            isTurretPointingAtDesired = false;
        }

        private void rotateTurretTowardsDesired()
        {
            if (isTurretPointingAtDesired)
                return;
            
            float angleToRotateTo = Mathf.MoveTowardsAngle(CurrentLookingAngle, desiredAngle, step);
            GeneralFunc.RotateTransform(ref turretPivot, angleToRotateTo);

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
