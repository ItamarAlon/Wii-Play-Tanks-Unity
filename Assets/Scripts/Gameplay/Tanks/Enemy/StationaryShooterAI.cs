// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Tanks.Shared;

namespace Game.Gameplay.Tanks.Enemy
{
    [RequireComponent(typeof(Shooter))]
    public class StationaryShooterAI : MonoBehaviour
    {
        public Transform turret;
        private Shooter _shooter;
        private Transform _player;

        void Awake() { _shooter = GetComponent<Shooter>(); }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.transform;
        }

        void Update()
        {
            if (!_player || !_shooter || !turret) return;
            Vector2 dir = (_player.position - turret.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            turret.rotation = Quaternion.Euler(0,0,ang);
            _shooter.TryFire(dir);
        }
    }
}
