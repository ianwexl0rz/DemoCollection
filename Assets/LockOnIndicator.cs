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

	private new Renderer renderer;
	private bool isActive;

	public void OnEnable()
	{
		renderer = GetComponentInChildren<Renderer>();
		renderer.sharedMaterial = inactiveMaterial;
	}

	public void UpdatePosition(bool lockedOn, Actor target)
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

	    var indicatorPos = target.GetLockOnPosition();
		if(target.GetComponent<Actor>() is Character player)
	    {
			indicatorPos += (player.capsuleCollider.height * 0.5f + heightOffset) * Vector3.up;
	    }

		if(lockedOn != isActive)
		{
			renderer.sharedMaterial = lockedOn ? activeMaterial : inactiveMaterial;
			isActive = lockedOn;
		}

	    transform.position = indicatorPos;
		var camera = GameManager.I.mainCamera;
	    transform.LookAt(camera.transform.position.WithY(transform.position.y), Vector3.up);
	}
}
