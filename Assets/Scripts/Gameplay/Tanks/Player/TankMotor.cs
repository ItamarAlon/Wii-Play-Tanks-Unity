
using System;
using UnityEngine;

namespace Game.Gameplay.Tanks.Shared
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TankMotor : MonoBehaviour
    {
        [SerializeField] Transform hullVisual;

        public float moveSpeed = 5f;
        public float accel = 20f;
        public float rotationSpeed = 450f;
        private Rigidbody2D rigidBody;
        private Vector2 desiredVelocity;
        private Vector2 lastMoveDirection = Vector2.right;
        private Health health;

        void Awake() 
        { 
            rigidBody = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            if (health)
                health.OnRevive += Health_OnRevive;
            rigidBody.freezeRotation = true;
        }
        public void SetDesiredVelocity(Vector2 v) => desiredVelocity = v * moveSpeed;

        private void Health_OnRevive(object sender, EventArgs e)
        {
            SetDesiredVelocity(Vector2.zero);
        }

        void FixedUpdate()
        {
            var vel = Vector2.MoveTowards(rigidBody.linearVelocity, desiredVelocity, accel * Time.fixedDeltaTime);
            rigidBody.linearVelocity = vel;
            rotateHullTowardsMovingDirection(vel);
        }

        private void rotateHullTowardsMovingDirection(Vector2 currentVel)
        {
            if (currentVel.sqrMagnitude > 0.0001f)
                lastMoveDirection = currentVel.normalized;
            else if (desiredVelocity.sqrMagnitude > 0.0001f)
                lastMoveDirection = desiredVelocity.normalized;

            if (lastMoveDirection.sqrMagnitude < 0.0001f || !hullVisual) return;

            float target = Mathf.Atan2(lastMoveDirection.y, lastMoveDirection.x) * Mathf.Rad2Deg;
            // If your sprite faces +Y by default, offset: target -= 90f;

            // current visual angle (read from child, not rigidbody)
            float current = hullVisual.eulerAngles.z;

            // 180° symmetry: choose the closest of target or target+180°
            float da = Mathf.Abs(Mathf.DeltaAngle(current, target));
            float db = Mathf.Abs(Mathf.DeltaAngle(current, target + 180f));
            if (db < da) target += 180f;

            // optional tiny epsilon to treat “exact opposite” as same (no micro-wiggle)
            const float oppositeEps = 0.1f;
            if (Mathf.Abs(Mathf.DeltaAngle(current, target)) <= oppositeEps)
                return;

            float maxStep = rotationSpeed * Time.fixedDeltaTime;
            float newAngle = Mathf.MoveTowardsAngle(current, target, maxStep);

            // rotate the VISUAL only
            hullVisual.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }
    }
}
