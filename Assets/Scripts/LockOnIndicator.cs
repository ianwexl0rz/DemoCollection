using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{
	[SerializeField]
	private Material activeMaterial = null;
	[SerializeField]
	private Material inactiveMaterial = null;
	[SerializeField]
	private float heightOffset = 0.15f;

	private Transform mainCameraTransform;
	private new Renderer renderer;
	private bool isActive;

	public void OnEnable()
	{
		mainCameraTransform = GameManager.I.mainCamera.transform;
		renderer = GetComponentInChildren<Renderer>();
		renderer.sharedMaterial = inactiveMaterial;
	}

	public void UpdatePosition(bool lockedOn, ILockOnTarget target)
    {
		if(target == null)
		{
			gameObject.SetActive(false);
			return;
		}
		else if(!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}

	    var indicatorPos = target.GetLookPosition();
		if(target is Character character)
	    {
			indicatorPos += (character.CapsuleCollider.height * 0.5f + heightOffset) * Vector3.up;
	    }

		if(lockedOn != isActive)
		{
			renderer.sharedMaterial = lockedOn ? activeMaterial : inactiveMaterial;
			isActive = lockedOn;
		}

		var t = transform;
		t.position = indicatorPos;
	    transform.LookAt(mainCameraTransform.position.WithY(t.position.y), Vector3.up);
	}
}
