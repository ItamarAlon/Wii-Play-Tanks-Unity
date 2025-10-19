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
        //public KeyCode mineKey = KeyCode.E;

        private TankMotor _motor;
        //private Camera _cam;

        void Awake()
        {
            _motor = GetComponent<TankMotor>();
            //_cam = Camera.main;
        }

        void Update()
        {
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            _motor.SetDesiredVelocity(move);

            if (Input.GetKey(fireKey) && shooter && shooter.Muzzle)
            {
                shooter.TryFire();
            }

            //if (Input.GetKeyDown(mineKey) && minePrefab)
            //{
            //    Instantiate(minePrefab, transform.position, Quaternion.identity);
            //}
        }
    }
}
