using UnityEngine;

public static class DamageableExtensions
{
    public static float GetNormalizedHealth(this IDamageable damageable)
    {
        return damageable.Health / damageable.MaxHealth;
    }
}