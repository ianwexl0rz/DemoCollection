using System.ComponentModel;
using ActorFramework;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceBar : MonoBehaviour
{
    [FormerlySerializedAs("healthBarFill")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform echoFill;
    
    public void RegisterResource(EntityResource resource)
    {
        resource.PropertyChanged += UpdateView;
        UpdateFill(resource);
        UpdateEchoFill(resource);
    }
    
    public void UnregisterResource(EntityResource resource)
    {
        resource.PropertyChanged -= UpdateView;
    }

    private void UpdateView(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not EntityResource resource) return;
        switch (e.PropertyName)
        {
            case nameof(EntityResource.Current):
                UpdateFill(resource);
                break;
            case nameof(EntityResource.Echo):
                UpdateEchoFill(resource);
                break;
        }
    }

    private void UpdateFill(EntityResource resource)
    {
        var progress = (float)resource.Current / resource.Maximum;
        fill.anchorMax = new Vector2(progress, fill.anchorMax.y);
    }
    
    private void UpdateEchoFill(EntityResource resource)
    {
        var progress = resource.Echo / resource.Maximum;
        echoFill.anchorMax = new Vector2(progress, echoFill.anchorMax.y);
    }
}