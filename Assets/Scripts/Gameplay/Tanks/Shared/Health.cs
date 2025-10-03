// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using System;

namespace Game.Gameplay.Tanks.Shared
{
    public class Health : MonoBehaviour
    {
        public event Action<Health> OnDeath;
        public bool IsDead { get; private set; }

        public void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
