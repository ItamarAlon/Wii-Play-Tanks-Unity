// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Tanks.Shared;

namespace Game.Gameplay.Tanks.Enemy
{
    [RequireComponent(typeof(Shooter))]
    [RequireComponent(typeof(TankMotor))]
    public class MovingShooterAI : MonoBehaviour
    {
        public Transform turret;
        public float chaseSpeed = 1f;
        private Shooter _shooter;
        private TankMotor _motor;
        private Transform _player;

        void Awake()
        {
            _shooter = GetComponent<Shooter>();
            _motor = GetComponent<TankMotor>();
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.transform;
        }

        void Update()
        {
            if (!_player) return;
            Vector2 toPlayer = (_player.position - transform.position);
            _motor.SetDesiredVelocity(toPlayer.normalized * chaseSpeed);
            float ang = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            if (turret) turret.rotation = Quaternion.Euler(0,0,ang);
            _shooter.TryFire(toPlayer);
        }
    }
}
