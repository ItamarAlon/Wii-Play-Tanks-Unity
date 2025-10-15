using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public static class GeneralFunc
    {
        public static Vector2 AngleToVector(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static Vector2 AngleToVector(float angleDeg, float magnitude)
        {
            Vector2 newVector = AngleToVector(angleDeg);
            return ChangeVectorMagnitude(newVector, magnitude);
        }

        public static float VectorToAngle(Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

        public static Vector2 ChangeVectorMagnitude(Vector2 vector, float magnitude)
        {
            return vector.normalized * magnitude;
        }

        public static Vector2 RotateVector(Vector2 vector, float angleToRotateBy)
        {
            float givenVectorAngle = VectorToAngle(vector);
            float rotatedVectorAngle = ConvertToAngle(givenVectorAngle + angleToRotateBy);
            return AngleToVector(rotatedVectorAngle, vector.magnitude);
        }

        public static Vector2 VectorFromOnePointToAnother(Transform from, Transform to)
        {
            return to.position - from.position;
        }

        public static void RotateTransform(ref Transform toRotate, float angleToRotateBy)
        {
            toRotate.rotation = Quaternion.Euler(0f, 0f, angleToRotateBy);
        }

        /// <summary>
        /// converts a number to a degree: between 0 and 360
        /// </summary>
        public static float ConvertToAngle(float num)
        {
            return Mathf.Repeat(num, 360f);
        }
    }
}
