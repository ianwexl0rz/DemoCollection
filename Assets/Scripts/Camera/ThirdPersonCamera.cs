using UnityEngine;
using InControl;

public class ThirdPersonCamera : MonoBehaviour
{
	public bool isEnabled = false;
	public float distance = 2.5f;
	public Vector2 pitchMinMax = new Vector2(-40, 85);
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
	//private float previousLockOnHeight = 0f;
	private Vector3 previousLookPos = Vector3.zero;
	//private float lockOnHeight = 0f;
	private Vector3 trackPos = Vector3.zero;
	private Vector3 desiredLookPos;

	public void SetTarget(Player newPlayer, bool immediate)
	{
		player = newPlayer;

		desiredFocalHeight = player.GetComponent<Collider>().bounds.extents.y;

		if(immediate)
		{
			focalHeight = desiredFocalHeight;
			lastTargetPos = trackPos = player.transform.position + Vector3.up * focalHeight;
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

		float smooth = 1;

		if(Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.visible = true;
		}
		else
		{
			// Cache look sensitivity from GameSettings
			float lookSensitivityX = ControlSettings.I.lookSensitivityX;
			float lookSensitivityY = ControlSettings.I.lookSensitivityY;

			InputDevice playerInput = InputManager.ActiveDevice;

			if(player.lockOn && player.lockOnTarget != null)
			{
				// Locked on to a target
				yaw += playerInput.RightStickX * lookSensitivityX * Time.deltaTime * 0.5f;

				//yaw = Quaternion.LookRotation(player.lockOnTarget.position - player.transform.position).eulerAngles.y;
			}
			else if(player.aimingMode)
			{
				// Aiming mode - move the camera directly and the character should follow
				yaw += playerInput.RightStickX * lookSensitivityX * Time.deltaTime * 0.5f;

				// No smoothing!
				smooth = 0f;
			}
			else if(player.recenter)
			{
				// Re-center mode - move the character directly and the camera should follow
				yaw = player.transform.eulerAngles.y;
			}
			else
			{
				// Not locked on
				yaw += playerInput.RightStickX * lookSensitivityX * Time.deltaTime * 0.5f;
			}

			pitch += playerInput.RightStickY * lookSensitivityY * Time.deltaTime;
			pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

			rotation.x = Mathf.SmoothDampAngle(rotation.x, pitch, ref rotationVelocity.x, rotationSmoothTime * smooth);
			rotation.y = Mathf.SmoothDampAngle(rotation.y, yaw, ref rotationVelocity.y, rotationSmoothTime * smooth);

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

			transform.rotation = Quaternion.Euler(rotation);
		}
	}

	public void UpdatePosition()
	{
		if(!player || !isEnabled) return;
		bool lockedOn = player.lockOn && player.lockOnTarget != null;

		Quaternion screenRotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);

		if(lockedOn)
		{
			if(blendToLockOn < 1f)
			{
				blendToLockOn += Time.deltaTime / lockTime;
				blendToLockOn = Mathf.Min(blendToLockOn, 1f);
			}

			// Set the focus halfway between the player and the enemy (similar to BotW)
			// TODO: Dynamically shift closer to the player when close to the camera
			// TODO: Implement "breaking" distance
			float lockOnHeight = player.lockOnTarget.GetComponent<Collider>().bounds.extents.y;
			desiredLookPos = player.lockOnTarget.position + Vector3.up * lockOnHeight;
		}
		else if(blendToLockOn > 0f)
		{
			blendToLockOn -= Time.deltaTime / unlockTime;
			blendToLockOn = Mathf.Max(blendToLockOn, 0f);
		}

		if(blendToPlayer > 0f)
		{
			blendToPlayer -= Time.deltaTime / unlockTime;
			blendToPlayer = Mathf.Max(blendToPlayer, 0f);
			float smoothBlend = Mathf.SmoothStep(1f, 0f, blendToPlayer);
			trackPos = Vector3.Lerp(previousPlayerPosition, player.transform.position + Vector3.up * desiredFocalHeight, smoothBlend);
			focalHeight = Mathf.Lerp(previousFocalHeight, desiredFocalHeight, smoothBlend);
			lookPos = Vector3.Lerp(previousLookPos, desiredLookPos, smoothBlend);
			dragVector *= blendToPlayer;
		}
		else
		{
			trackPos = player.transform.position + Vector3.up * focalHeight;
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

		/*
		float t = lockedOn ? Mathf.SmoothStep(0f, 1f, blendToLockOn) : blendToLockOn * blendToLockOn;
		Vector3 lockedPos = Vector3.Lerp(trackPos, lookPos, 0.5f);
		float lockOnDistance = Vector3.Distance(trackPos, lockedPos) * 0.25f;
		float blendedDistance = distance + lockOnDistance * t;
		transform.position = Vector3.Lerp(modifiedPlayerPos, lockedPos, t) - transform.forward * blendedDistance;
		*/

		transform.position = modifiedPlayerPos - transform.forward * distance;

		Debug.DrawLine(trackPos, trackPos + screenRotation * dragVector, Color.white);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos, Color.red);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos + screenRotation * dragVector, Color.blue);
	}
}
