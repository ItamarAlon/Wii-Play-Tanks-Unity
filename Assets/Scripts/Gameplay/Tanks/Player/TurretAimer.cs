
using Assets.Scripts.Core;
using UnityEngine;

namespace Game.Gameplay.Tanks.Shared
{
    public class TurretAimer : MonoBehaviour
    {
        public Camera worldCamera;
        public enum AimAxis { Right, Up } 
        public AimAxis spriteFaces = AimAxis.Up;
        public float extraDegrees = 0f;

        void Awake()
        {
            if (!worldCamera) 
                worldCamera = Camera.main;
        }

        void Update()
        {
            aimTurretTowardsMousePointer();
        }

        private void aimTurretTowardsMousePointer()
        {
            Vector3 mousePosition = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = transform.position.z;
            Vector2 dir = Utils.VectorFromOnePointToAnother(transform.position, mousePosition);

            if (spriteFaces == AimAxis.Right)
                transform.right = dir;
            else
                transform.up = dir;

            if (Mathf.Abs(extraDegrees) > 0.001f)
                transform.Rotate(0f, 0f, extraDegrees);
        }
    }
}
