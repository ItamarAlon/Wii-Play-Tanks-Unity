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

        void Awake() { _rb = GetComponent<Rigidbody2D>(); }
        public void SetDesiredVelocity(Vector2 v) => _desiredVel = v * moveSpeed;

        void FixedUpdate()
        {
            var vel = Vector2.MoveTowards(_rb.linearVelocity, _desiredVel, accel * Time.fixedDeltaTime);
            _rb.linearVelocity = vel;
        }
    }
}
