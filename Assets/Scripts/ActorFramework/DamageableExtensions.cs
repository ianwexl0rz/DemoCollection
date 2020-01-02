using UnityEngine;

public static class DamageableExtensions
{
    public static void ApplyDamage(this IDamageable damageable, float damage)
    {
        // Calculate new health (un-clamped so we can do "overkill" events, etc.)
        var newHealth = damageable.Health - damage;

        // Clamp new health.
        newHealth = Mathf.Clamp(newHealth, 0, damageable.MaxHealth);
        
        // Early out if no change...
        if (damageable.Health.Equals(newHealth)) return;

        // Update health.
        damageable.Health = newHealth;
        
        // Do callback.
        damageable.OnHealthChanged(newHealth / damageable.MaxHealth);
        
        // Destroy if health is zero.
        if (newHealth < Mathf.Epsilon) damageable.Destroy();
    }

    public static float GetNormalizedHealth(this IDamageable damageable)
    {
        return damageable.Health / damageable.MaxHealth;
    }
}