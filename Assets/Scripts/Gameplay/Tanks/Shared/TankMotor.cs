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
        private Rigidbody2D _rb;
        private Vector2 _desiredVel;

        void Awake() 
        { 
            _rb = GetComponent<Rigidbody2D>();
            //_rb.freezeRotation = true;
        }
        public void SetDesiredVelocity(Vector2 v) => _desiredVel = v * moveSpeed;

        void FixedUpdate()
        {
            rotateHullTowardsMovingDirection();
            var vel = Vector2.MoveTowards(_rb.linearVelocity, _desiredVel, accel * Time.fixedDeltaTime);
            _rb.linearVelocity = vel;
        }

        private void rotateHullTowardsMovingDirection()
        {
            if (_desiredVel == Vector2.zero) return;

            Quaternion desiredLookRotation;

            //if (_desiredVel == Vector2.up || 
            //    _desiredVel == Vector2.down ||
            //    _desiredVel == Vector2.left ||
            //    _desiredVel == Vector2.right)
            //{
            //    _rb.MoveRotation(Quaternion.identity);

            //}
            desiredLookRotation = Quaternion.LookRotation(_desiredVel);
            Quaternion currentLookRotation = Quaternion.LookRotation(_rb.linearVelocity);
            Debug.Log(Quaternion.Angle(desiredLookRotation, currentLookRotation));
            _rb.MoveRotation(Quaternion.RotateTowards(currentLookRotation, desiredLookRotation, 3));
        }
    }
}
