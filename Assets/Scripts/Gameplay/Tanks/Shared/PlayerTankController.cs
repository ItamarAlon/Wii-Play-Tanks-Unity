// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using Game.Gameplay.Mines;

namespace Game.Gameplay.Tanks.Shared
{
    [RequireComponent(typeof(TankMotor))]
    public class PlayerTankController : MonoBehaviour
    {
        public Shooter shooter;
        public Transform turret;
        public Mine minePrefab;
        public KeyCode fireKey = KeyCode.Mouse0;
        public KeyCode mineKey = KeyCode.E;

        private TankMotor _motor;
        private Camera _cam;

        void Awake()
        {
            _motor = GetComponent<TankMotor>();
            _cam = Camera.main;
        }

        void Update()
        {
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            _motor.SetDesiredVelocity(move);

            if (Input.GetKey(fireKey) && shooter && shooter.muzzle)
            {
                Vector3 mouse = Input.mousePosition;
                Vector3 world = _cam.ScreenToWorldPoint(mouse);
                Vector2 dir = (world - shooter.muzzle.position);
                shooter.TryFire(dir);
            }

            if (Input.GetKeyDown(mineKey) && minePrefab)
            {
                Instantiate(minePrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
