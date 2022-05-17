using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DemoCollection;
using JetBrains.Annotations;
using UnityEngine;

namespace ActorFramework
{
    public class Health : MonoBehaviour
    {
        public event Action<Health> OnChanged;
        public event Action OnDeath;

        [SerializeField] private int _current = 100;
        [SerializeField] private int _maximum = 100;

        public int Current => _current;
        public int Maximum => _maximum;
        
        public void RegisterCallbacks(IDamageable damageable) => OnDeath = damageable.Die;

        public void TakeDamage(int damage)
        {
            // Calculate new health (un-clamped so we can do "overkill" events, etc.)
            var newHealth = _current - damage;

            // Clamp new health.
            newHealth = Mathf.Clamp(newHealth, 0, _maximum);
        
            // Early out if no change...
            if (newHealth == _current) return;

            // Update health.
            _current = newHealth;
        
            // Do callback.
            OnChanged?.Invoke(this);
        
            // Destroy if health is zero.
            if (newHealth < Mathf.Epsilon)
                OnDeath?.Invoke();
        }
    }
}