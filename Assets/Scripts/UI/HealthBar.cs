using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;

    public void OnEnable() => MainMode.OnSetPlayer += RegisterPlayer;

    public void OnDisable() => MainMode.OnSetPlayer -= RegisterPlayer;

    private void RegisterPlayer(Actor actor)
    {
        actor.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(actor.Health / actor.MaxHealth);
    }

    private void UpdateHealthBar(float normalizedHealth) => healthBarFill.anchorMax = new Vector2(normalizedHealth, healthBarFill.anchorMax.y);
}