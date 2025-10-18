using UnityEngine;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    public abstract class EnemyAI : MonoBehaviour
    {
        public abstract bool Enable { get; set; }
    }
}
