using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public static class Utils
    {
        public static Vector2 AngleToVector(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static Vector2 AngleToVector(float angleDeg, float magnitude)
        {
            Vector2 newVector = AngleToVector(angleDeg);
            return SetMagnitude(newVector, magnitude);
        }

        public static float VectorToAngle(Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

        public static Vector2 SetMagnitude(Vector2 vector, float magnitude)
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
            return VectorFromOnePointToAnother(from.position, to.position);
        }

        public static Vector2 VectorFromOnePointToAnother(Vector2 from, Vector2 to)
        {
            return to - from;
        }

        public static void RotateTransform(ref Transform toRotate, float angleToRotateBy)
        {
            toRotate.rotation = Quaternion.Euler(0f, 0f, angleToRotateBy);
        }

        public static bool AreVectorsHeadingTheSameDirection(Vector2 vector1, Vector2 vector2)
        {
            return Vector2.Dot(vector1.normalized, vector2.normalized) > 0;
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
