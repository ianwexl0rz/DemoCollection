using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{
    [SerializeField] private Material activeMaterial = null;
    [SerializeField] private Material inactiveMaterial = null;
    [SerializeField] private float indicatorHeightOffset = 0.15f;

    private new Renderer renderer;
    private bool lockedOn;

    public void Init()
    {
        renderer = transform.GetComponentInChildren<Renderer>();
        renderer.sharedMaterial = inactiveMaterial;
    }
    
    public void UpdatePosition(bool lockedOn, ITrackable trackable, Vector3 camPos)
    {
        if(trackable == null)
        {
            gameObject.SetActive(false);
            return;
        }
		
        if(!gameObject.activeSelf) gameObject.SetActive(true);

        var indicatorPos = trackable.GetEyesPosition();
        if(trackable is CharacterMotor character)
        {
            indicatorPos += (character.CapsuleCollider.height * 0.5f + indicatorHeightOffset) * Vector3.up;
        }

        if(lockedOn != this.lockedOn)
        {
            renderer.sharedMaterial = lockedOn ? activeMaterial : inactiveMaterial;
            this.lockedOn = lockedOn;
        }

        var t = transform;
        t.position = indicatorPos;
        t.LookAt(camPos.WithY(indicatorPos.y), Vector3.up);
    }
}
