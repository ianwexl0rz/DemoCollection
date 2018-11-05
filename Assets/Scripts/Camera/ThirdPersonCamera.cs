using UnityEngine;
using InControl;

public class ThirdPersonCamera : MonoBehaviour
{
	public bool isEnabled = false;
	public float distance = 2.5f;
	public Vector2 normalPitchMinMax = new Vector2(-40, 85);
	public Vector2 lockOnPitchMinMax = new Vector2(10, 50);
	public Vector3 offset = new Vector3(0f,1.6f,0.7f);
	public Vector3 dragAmount = Vector3.zero;
	public float posSmoothTime = 0.12f;
	public float rotationSmoothTime = 0.12f;
	public float turnWithPlayerFactor = 20f;
	public float lockTime = 0.3f;
	public float unlockTime = 0.6f;
	public float towardCameraDragScale = 0.5f;
	public float overheadDragScale = 0.5f;

	private float yaw; //rotation on the y axis
	private float pitch; //rotation on the x axis

	private Vector3 rotation;
	private Vector3 rotationVelocity;

	//private float lockOnDistance = 0f;
	private float distanceVel;
	private Vector3 switchVelocity;

	private Vector3 lookPos = Vector3.zero;
	private Vector3 lastTargetPos = Vector3.zero;
	private Vector3 dragVector = Vector3.zero;
	private Vector3 dragVectorVelocity = Vector3.zero;

	private Player player = null;
	private float blendToPlayer = 0f;
	private Vector3 previousPlayerPosition = Vector3.zero;
	private float blendToLockOn = 0f;

	private float focalHeight = 0f;
	private float desiredFocalHeight = 0f;
	private float previousFocalHeight = 0f;
	private Vector3 previousLookPos = Vector3.zero;
	private Vector3 trackPos = Vector3.zero;
	private Vector3 desiredLookPos;
	private float blendCamera;

	public void SetTarget(Player newPlayer, bool immediate)
	{
		player = newPlayer;

		desiredFocalHeight = player.GetComponent<Collider>().bounds.extents.y;

		if(immediate)
		{
			focalHeight = desiredFocalHeight;
			lastTargetPos = trackPos = player.transform.position + player.transform.up * focalHeight;
		}
		else
		{
			blendToPlayer = 1f;
		}

		previousPlayerPosition = trackPos;
		previousFocalHeight = focalHeight;
		previousLookPos = lookPos;
		//previousLockOnHeight = lockOnHeight;
	}

	public void UpdateRotation()
	{
		if(!player || !isEnabled) return;

		if(Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.visible = true;
			return;
		}

		// Cache look sensitivity from GameSettings
		float lookSensitivityX = ControlSettings.I.lookSensitivityX;
		float lookSensitivityY = ControlSettings.I.lookSensitivityY;

		InputDevice playerInput = InputManager.ActiveDevice;

		if(player.lockOn && player.lockOnTarget != null)
		{
			var toTarget = Quaternion.LookRotation(player.lockOnTarget.position - player.transform.position + focalHeight * Vector3.down);
			pitch = toTarget.eulerAngles.x;
			yaw = toTarget.eulerAngles.y;
			rotationSmoothTime = 10f;
		}
		else
		{
			rotationSmoothTime = Mathf.Max(1f, rotationSmoothTime - Time.deltaTime * 9f);
			yaw += playerInput.RightStickX * lookSensitivityX * Time.deltaTime * 0.5f;
			pitch += playerInput.RightStickY * lookSensitivityY * Time.deltaTime;
		}

		var minMax = Vector2.Lerp(normalPitchMinMax, lockOnPitchMinMax, (rotationSmoothTime - 1f) / 10f);
		pitch = Mathf.Clamp(pitch, minMax.x, minMax.y);

		rotation.x = Mathf.SmoothDampAngle(rotation.x, pitch, ref rotationVelocity.x, rotationSmoothTime * Time.deltaTime);
		rotation.y = Mathf.SmoothDampAngle(rotation.y, yaw, ref rotationVelocity.y, rotationSmoothTime * Time.deltaTime);

		if(!player.lockOn)
		{
			// Rotate the camera slightly in the direction we're moving
			Vector3 localDragVector = transform.TransformDirection(dragVector);
			if(localDragVector.sqrMagnitude >= 1f)
			{
				Vector3.Normalize(localDragVector);
			}

			float turnFactor = Vector3.Dot(localDragVector, -transform.right);
			float turnDelta = Mathf.Abs(turnFactor) * turnFactor * turnWithPlayerFactor * Time.deltaTime;

			// Add it after rotation smoothing because it's driven by drag (already smoothed)
			yaw += turnDelta;
			rotation.y += turnDelta;
		}

		transform.rotation = Quaternion.Euler(rotation);
	}

	public void UpdatePosition()
	{
		if(!player || !isEnabled) return;

		Quaternion screenRotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);

		if(blendToPlayer > 0f)
		{
			blendToPlayer -= Time.deltaTime / unlockTime;
			blendToPlayer = Mathf.Max(blendToPlayer, 0f);
			float smoothBlend = Mathf.SmoothStep(1f, 0f, blendToPlayer);
			trackPos = Vector3.Lerp(previousPlayerPosition, player.transform.position + player.transform.up * desiredFocalHeight, smoothBlend);
			focalHeight = Mathf.Lerp(previousFocalHeight, desiredFocalHeight, smoothBlend);
			lookPos = Vector3.Lerp(previousLookPos, desiredLookPos, smoothBlend);
			dragVector *= blendToPlayer;
		}
		else
		{
			trackPos = player.transform.position + player.transform.up * focalHeight;
			lookPos = desiredLookPos;
		}

		// We want drag relative to camera rotation so the character doesn't "fishtail" when the player looks around
		// 1) Get the change in player position and rotate it by the inverse of the camera rotation
		// 2) Now we can scale the amount of drag on each axis in screen space (yay!)
		// 3) Interpolate the accumulated drag toward zero over time
		// NOTE: Rotate dragVector by camera rotation later to put it back into world space

		// Drag less if we're running toward the camera
		float dragAway = Vector3.Dot(screenRotation * dragVector.normalized, -transform.forward).LinearRemap(-1f, 1f, towardCameraDragScale, 1f);

		Vector3 dragDelta = Quaternion.Inverse(screenRotation) * (lastTargetPos - trackPos);
		dragVector += Vector3.Scale(dragDelta, dragAmount.WithZ(dragAmount.z * dragAway));
		dragVector = Vector3.SmoothDamp(dragVector, Vector3.zero, ref dragVectorVelocity, posSmoothTime);
		lastTargetPos = trackPos;

		
		// Drag less if we're looking at the ground
		Vector3 modifiedDragVector = dragVector.WithZ(dragVector.z * Mathf.Lerp(overheadDragScale, 1f, 1f + transform.forward.y));

		Vector3 modifiedPlayerPos = trackPos + screenRotation * modifiedDragVector
			+ transform.forward * focalHeight * transform.forward.y
			+ transform.TransformDirection(offset) * distance;

		transform.position = modifiedPlayerPos - transform.forward * distance;

		Debug.DrawLine(trackPos, trackPos + screenRotation * dragVector, Color.white);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos, Color.red);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos + screenRotation * dragVector, Color.blue);
	}
}
