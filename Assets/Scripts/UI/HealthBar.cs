using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;

    public void RegisterPlayer(Actor actor)
    {
        actor.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(actor.Health / actor.maxHealth);
    }
	
    public void UnregisterPlayer(Actor actor) => actor.OnHealthChanged -= UpdateHealthBar;

    private void UpdateHealthBar(float normalizedHealth) => healthBarFill.anchorMax = new Vector2(normalizedHealth, healthBarFill.anchorMax.y);
}