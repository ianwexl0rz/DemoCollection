using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{
    [SerializeField] private Material activeMaterial = null;
    [SerializeField] private Material inactiveMaterial = null;
    [SerializeField] private float indicatorHeightOffset = 0.15f;

    private Renderer _renderer;
    private bool _lockedOn;

    public void Init()
    {
        _renderer = transform.GetComponentInChildren<Renderer>();
        _renderer.sharedMaterial = inactiveMaterial;
        _renderer.enabled = false;
    }
    
    public void UpdatePosition(bool lockedOn, ITrackable trackable, Vector3 camPos)
    {
        if(trackable == null)
        {
            //gameObject.SetActive(false);
            return;
        }
		
        //if(!gameObject.activeSelf) gameObject.SetActive(true);

        var indicatorPos = trackable.GetCenter();
        if(trackable is CharacterMotor character)
        {
            indicatorPos += (character.CapsuleCollider.height * 0.5f + indicatorHeightOffset) * Vector3.up;
        }

        if(lockedOn != this._lockedOn)
        {
            _renderer.sharedMaterial = lockedOn ? activeMaterial : inactiveMaterial;
            this._lockedOn = lockedOn;
        }

        var t = transform;
        t.position = indicatorPos;
        t.LookAt(camPos.WithY(indicatorPos.y), Vector3.up);
    }
}
