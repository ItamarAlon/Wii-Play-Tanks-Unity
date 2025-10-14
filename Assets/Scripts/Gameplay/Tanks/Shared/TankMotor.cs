// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;

namespace Game.Gameplay.Tanks.Shared
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TankMotor : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float accel = 20f;
        public float rotationSpeed = 450f;
        private Rigidbody2D _rb;
        private Vector2 _desiredVel;
        private Vector2 _lastMoveDir = Vector2.right;

        void Awake() 
        { 
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
        }
        public void SetDesiredVelocity(Vector2 v) => _desiredVel = v * moveSpeed;

        void FixedUpdate()
        {
            var vel = Vector2.MoveTowards(_rb.linearVelocity, _desiredVel, accel * Time.fixedDeltaTime);
            _rb.linearVelocity = vel;
            rotateHullTowardsMovingDirection(vel);
        }

        private void rotateHullTowardsMovingDirection(Vector2 currentVel)
        {
            // If we’re moving meaningfully, refresh the last move direction
            if (currentVel.sqrMagnitude > 0.0001f)
                _lastMoveDir = currentVel.normalized;
            else if (_desiredVel.sqrMagnitude > 0.0001f)
                _lastMoveDir = _desiredVel.normalized; // optional: let desired drive facing while accelerating

            // If still no direction, do nothing this frame
            if (_lastMoveDir.sqrMagnitude < 0.0001f) return;

            float target = Mathf.Atan2(_lastMoveDir.y, _lastMoveDir.x) * Mathf.Rad2Deg;
            // If your hull art faces +Y by default, do: target -= 90f;

            float current = _rb.rotation;

            // 180° symmetry: pick the equivalent orientation that minimizes rotation
            // Option A: face 'target'; Option B: face 'target + 180'
            float deltaA = Mathf.Abs(Mathf.DeltaAngle(current, target));
            float deltaB = Mathf.Abs(Mathf.DeltaAngle(current, target + 180f));
            if (deltaB < deltaA)
                target += 180f;

            float maxStep = rotationSpeed * Time.fixedDeltaTime;
            float newAngle = Mathf.MoveTowardsAngle(current, target, maxStep);
            _rb.MoveRotation(newAngle);
        }
    }
}
