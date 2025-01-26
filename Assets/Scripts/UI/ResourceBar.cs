using ActorFramework;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceBar : MonoBehaviour
{
    [FormerlySerializedAs("healthBarFill")]
    [SerializeField] private RectTransform fill;
    
    public void RegisterResource(EntityResource resource)
    {
        resource.OnValueChanged += UpdateFill;
        UpdateFill(resource.Current, resource.Maximum);
    }
    
    public void UnregisterResource(EntityResource resource)
    {
        resource.OnValueChanged -= UpdateFill;
    }
    
    private void UpdateFill(int current, int maximum)
    {
        var normalizedFill = (float)current / maximum;
        fill.anchorMax = new Vector2(normalizedFill, fill.anchorMax.y);
    }
}