using ActorFramework;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFill;
    
    public void RegisterHealthComponent(EntityResource resource)
    {
        resource.OnValueChanged += UpdateHealthBar;
        UpdateHealthBar(resource.Current, resource.Maximum);
    }
    
    public void UnregisterHealthComponent(EntityResource resource)
    {
        resource.OnValueChanged -= UpdateHealthBar;
    }
    
    private void UpdateHealthBar(int current, int maximum)
    {
        var normalizedHealth = (float)current / maximum;
        healthBarFill.anchorMax = new Vector2(normalizedHealth, healthBarFill.anchorMax.y);
    }
}