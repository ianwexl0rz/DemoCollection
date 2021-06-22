using System;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;

    private static HealthBar _instance;

    private void Awake() => _instance = this;

    public static void RegisterPlayer(Actor actor)
    {
        actor.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(actor.Health / actor.MaxHealth);
    }

    public static void UnregisterPlayer(Actor actor)
    {
        actor.OnHealthChanged -= UpdateHealthBar;
    }

    public static void UpdateHealthBar(float normalizedHealth)
    {
        _instance.healthBarFill.anchorMax = new Vector2(normalizedHealth, _instance.healthBarFill.anchorMax.y);
    }
}