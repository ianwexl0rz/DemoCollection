using System;
using ActorFramework;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;
    
    private static HealthBar _instance;
    
    private void Awake() => _instance = this;
    
    public static void RegisterHealthComponent(Health health)
    {
        health.OnValueChanged += UpdateHealthBar;
        UpdateHealthBar(health.Current, health.Maximum);
    }
    
    public static void UnregisterHealthComponent(Health health)
    {
        health.OnValueChanged -= UpdateHealthBar;
    }
    
    public static void UpdateHealthBar(int current, int maximum)
    {
        var normalizedHealth = (float)current / maximum;
        _instance.healthBarFill.anchorMax = new Vector2(normalizedHealth, _instance.healthBarFill.anchorMax.y);
    }
}