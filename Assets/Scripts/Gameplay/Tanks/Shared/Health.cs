
using UnityEngine;
using System;

namespace Game.Gameplay.Tanks.Shared
{
    public class Health : MonoBehaviour
    {
        public event EventHandler OnDeath;
        public event EventHandler OnRevive;
        public bool IsDead { get; private set; }

        public void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            gameObject.SetActive(false);
            OnOnDeath(EventArgs.Empty);
        }

        public void Revive()
        {
            if (!IsDead) return;
            IsDead = false;
            gameObject.SetActive(true);
            OnOnRevive(EventArgs.Empty);
        }

        protected virtual void OnOnDeath(EventArgs e)
        {
            OnDeath?.Invoke(this, e);
        }
        protected virtual void OnOnRevive(EventArgs e)
        {
            OnRevive?.Invoke(this, e);
        }
    }
}
