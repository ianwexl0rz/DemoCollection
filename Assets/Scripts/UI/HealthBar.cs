using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;

    private Actor actor;

    public void OnEnable()
    {
        MainMode.OnSetPlayer += RegisterPlayer;
        MainMode.OnUnsetPlayer += UnregisterPlayer;
    }
    
    public void OnDisable()
    {
        MainMode.OnSetPlayer -= RegisterPlayer;
        MainMode.OnUnsetPlayer -= UnregisterPlayer;
    }

    private void RegisterPlayer(Actor actor)
    {
        this.actor = actor;
        actor.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(actor.Health / actor.MaxHealth);
    }
	
    private void UnregisterPlayer() => actor.OnHealthChanged -= UpdateHealthBar;

    private void UpdateHealthBar(float normalizedHealth) => healthBarFill.anchorMax = new Vector2(normalizedHealth, healthBarFill.anchorMax.y);
}