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
        public static Vector2 DirFromAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// converts a number to a degree: between 0 and 360
        /// </summary>
        public static float ConvertToDegree(float num)
        {
            return Mathf.Repeat(num, 360f);
        }
    }
}
