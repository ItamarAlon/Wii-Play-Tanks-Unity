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
        private Vector2 desiredTurretLookingDirection;
        private int frameCounter = 0;
        private float CurrentLookingAngle { get => turretPivot.eulerAngles.z; }
        private bool isTurretPointingAtDesired = false;

        void Awake()
        {
            if (turretPivot)
                desiredTurretLookingDirection = turretPivot.up;
            else
                desiredTurretLookingDirection = Vector2.zero;

            float anglePerFrame = word39_TurretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
            step = anglePerFrame * fps * Time.deltaTime;
            //StartCoroutine(routine());
        }

        void Update()
        {
            if (frameCounter == 0)
                generateDesiredLookingDirection();
            else if (frameCounter == word40_TurretTargetTimer)
                frameCounter = -1;

            rotateTurretTowardsDesired();
            frameCounter++;
        }

        private void generateDesiredLookingDirection()
        {
            Vector2 vectorFromTankToTarget = GeneralFunc.VectorFromOnePointToAnother(this.transform, target.transform);
            float randomOffsetAngle = Random.Range(-word29_TurretAngleOffset, word29_TurretAngleOffset);
            desiredTurretLookingDirection = GeneralFunc.RotateVector(vectorFromTankToTarget, randomOffsetAngle);
            isTurretPointingAtDesired = false;
        }

        private void rotateTurretTowardsDesired()
        {
            if (isTurretPointingAtDesired)
                return;

            float desiredAngle = GeneralFunc.VectorToAngle(desiredTurretLookingDirection) - 90;
            float angleToRotateTo = Mathf.MoveTowardsAngle(CurrentLookingAngle, desiredAngle, step);
            GeneralFunc.RotateTransform(ref turretPivot, angleToRotateTo);

            isTurretPointingAtDesired = angleToRotateTo == desiredAngle;
        }

        //private IEnumerator routine()
        //{
        //    int counter = word40_TurretTargetTimer;
        //}
    }
}
