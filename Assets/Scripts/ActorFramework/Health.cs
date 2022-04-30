using System;
using UnityEngine;

namespace ActorFramework
{
    public class Health : MonoBehaviour
    {
        public event Action<float> OnValueChanged;
        public event Action OnDeath;

        public int Current = 100;
        public int Max = 100;

        public void RegisterCallbacks(IDamageable damageable) => OnDeath = damageable.Die;

        public void TakeDamage(int damage)
        {
            // Calculate new health (un-clamped so we can do "overkill" events, etc.)
            var newHealth = Current - damage;

            // Clamp new health.
            newHealth = Mathf.Clamp(newHealth, 0, Max);
        
            // Early out if no change...
            if (newHealth == Current) return;

            // Update health.
            Current = newHealth;
        
            // Do callback.
            OnValueChanged?.Invoke((float)newHealth / Max);
        
            // Destroy if health is zero.
            if (newHealth < Mathf.Epsilon)
                OnDeath?.Invoke();
        }
    }
}