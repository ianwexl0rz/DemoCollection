using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour {
	
    public float mouseSensitivity = 10;
    public Transform target;
	public Vector3 offset = new Vector3(0f,2f,0f);
	public float dstFromTarget = 2;
	public Vector2 pitchMinMax = new Vector2(-40,85);
	public float rotationSmoothTime = .12f;

	private Vector3 rotationSmoothVelocity;
	private Vector3 currentRotation;
	private float yaw; //rotation on the y axis
	private float pitch; //rotation on the x axis

	void Start()
	{
		FocusOnTarget(target);
	}

	public void FocusOnTarget(Transform newTarget)
	{
		// Only switch targets if the mouse is unlocked.
		if(Cursor.lockState == CursorLockMode.Locked) { return; }

		if(target != null)
		{
			SetActivePlayer(target, false);
		}

		Cursor.lockState = CursorLockMode.Locked;
		SetActivePlayer(newTarget, true);
		target = newTarget;
	}

	private void SetActivePlayer(Transform target, bool value)
	{
		PlayerController activeController = target.GetComponent<PlayerController>();
		if(activeController != null)
		{
			activeController.enabled = value;
		}
	}

	// Update is called once per frame
	private void LateUpdate ()
	{
		if(Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.visible = true;
		}
		else
		{
			yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
			pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity; //+= would invert
			pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

			currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
			transform.eulerAngles = currentRotation;
		}

        transform.position = target.position + target.rotation * offset - transform.forward * dstFromTarget;
	}
}
