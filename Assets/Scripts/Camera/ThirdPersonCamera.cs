using UnityEngine;
using InControl;

public class ThirdPersonCamera : MonoBehaviour
{
	public Vector3 forwardOffset = new Vector3(0f,2f,0f);
	public Vector3 downOffset = new Vector3(0f, 0f, 1f);
	public float normalDistance = 2;
	public Vector2 pitchMinMax = new Vector2(-40,85);
	public Vector3 dragAmount = Vector3.zero;
	public float rotationSmoothTime = 0.12f;
	public float posSmoothTime = 0.12f;
	public float lockTime = 0.3f;
	public float unlockTime = 0.6f;

	private float yaw; //rotation on the y axis
	private float pitch; //rotation on the x axis

	private Vector3 rotation;
	private Vector3 rotationVelocity;

	private float distance;
	private float lockOnDistance = 0f;
	private float distanceVel;
	private Vector3 switchVelocity;

	private Vector3 lookPos = Vector3.zero;
	private Vector3 lastTargetPos = Vector3.zero;
	private Vector3 dragVector = Vector3.zero;
	private Vector3 dragVectorVelocity = Vector3.zero;

	private Player player = null;
	private Player oldPlayer = null;
	private float blendToPlayer = 0f;
	private Vector3 blendFromPosition = Vector3.zero;
	private float blendToLockOn = -1f;

	public void Setup()
	{
		distance = normalDistance;
		player = oldPlayer = GameManager.I.activePlayer;
		lastTargetPos = player.transform.position;
	}

	public void SetTarget(Player player)
	{
		this.player = player;
	}

	public void UpdateRotation()
	{
		if(!player) return;

		float smooth = 1;

		if(Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.visible = true;
		}
		else
		{
			// Cache look sensitivity from GameSettings
			Vector2 lookSensitivity = ControlSettings.I.lookSensitivity;
			InputDevice playerInput = InputManager.ActiveDevice;

			if(player.lockOn)
			{
				if(player.lockOnTarget != null)
				{
					// Locked on to a target
					yaw += playerInput.RightStickX * lookSensitivity.x * Time.deltaTime * 0.5f; // Half speed

					//yaw = Quaternion.LookRotation(player.lockOnTarget.position - player.transform.position).eulerAngles.y;
				}
			}
			else if(player.aimingMode)
			{
				// Aiming mode - move the camera directly and the character should follow
				yaw += playerInput.RightStickX * lookSensitivity.x * Time.deltaTime;

				// No smoothing!
				smooth = 0f;
			}
			else if(player.recenter)
			{
				// Re-center mode - move the character directly and the camera should follow
				yaw = Quaternion.LookRotation(player.look).eulerAngles.y;
				yaw += playerInput.RightStickX * lookSensitivity.x * Time.deltaTime;

				// Start with 3x smoothing and diminish to zero as the camera aligns with character
				//smooth = -Vector3.Dot(transform.forward, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
				//smooth = smooth * 0.5f + 0.5f;
				//smooth *= 3f;
			}
			else
			{
				// Not locked on
				yaw += playerInput.RightStickX * lookSensitivity.x * Time.deltaTime;

				// Rotate the camera slightly in the direction we're moving (like Dark Souls)
				yaw += Mathf.Abs(playerInput.LeftStickX) * playerInput.LeftStickX * 20f * Time.deltaTime;
			}

			pitch += playerInput.RightStickY * lookSensitivity.y * Time.deltaTime * (ControlSettings.I.invertY ? 1 : -1);
			pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

			rotation.x = Mathf.SmoothDampAngle(rotation.x, pitch, ref rotationVelocity.x, rotationSmoothTime * smooth);
			rotation.y = Mathf.SmoothDampAngle(rotation.y, yaw, ref rotationVelocity.y, rotationSmoothTime * smooth);

			transform.eulerAngles = rotation;
		}
	}

	public void UpdatePosition()
	{
		if(!player) return;

		Vector3 trackPos = player.transform.position;

		bool switchedPlayer = player != oldPlayer;
		bool lockedOn = player.lockOn && player.lockOnTarget != null;

		Quaternion screenRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0f);
		Quaternion invRotation = Quaternion.Euler(0, -transform.rotation.eulerAngles.y, 0f);

		if(switchedPlayer)
		{
			blendFromPosition = oldPlayer.transform.position;
			oldPlayer = player;
			blendToPlayer = 1f;
		}

		if(blendToPlayer > 0f)
		{
			blendToPlayer -= Time.deltaTime / unlockTime;
			blendToPlayer = Mathf.Max(blendToPlayer, 0f);
			trackPos = Vector3.Lerp(trackPos, blendFromPosition, Mathf.SmoothStep(0f, 1f, blendToPlayer));
			dragVector *= blendToPlayer;
		}

		if(lockedOn)
		{
			// Set the focus halfway between the player and the enemy (similar to BotW)
			// TODO: Dynamically shift closer to the player when close to the camera
			// TODO: Implement "breaking" distance
			lookPos = Vector3.Lerp(player.transform.position, player.lockOnTarget.position, 0.5f);
			lockOnDistance = normalDistance + Vector3.Distance(player.lockOnTarget.position, player.transform.position) * 0.25f;
		}

		if(lockedOn)
		{
			blendToLockOn += Time.deltaTime / lockTime;
		}
		else
		{
			blendToLockOn -= Time.deltaTime / unlockTime;
		}

		blendToLockOn = Mathf.Clamp01(blendToLockOn);

		float cameraDown = Vector3.Dot(transform.forward, Vector3.down) * 0.5f + 0.5f;
		float cameraForward = Mathf.Abs(Vector3.Dot(transform.up, Vector3.down));

		// We want drag relative to camera rotation so the character doesn't "fishtail" when the player looks around
		// 1) Get the change in player position and rotate it by the inverse of the camera rotation
		// 2) Now we can scale the amount of drag on each axis in screen space (yay!)
		// 3) Interpolate the accumulated drag toward zero over time
		// NOTE: Rotate dragVector by camera rotation later to put it back into world space

		// Drag less if we're running toward the camera
		float dragAway = Vector3.Dot(screenRotation * dragVector.normalized, -transform.forward) * 0.5f + 0.5f;
		Vector3 dragDelta = Vector3.Scale(invRotation * (lastTargetPos - trackPos), dragAmount);
		dragDelta = Vector3.Scale(dragDelta, new Vector3(1f, 1f, Mathf.Lerp(0.5f, 1f, dragAway)));
		lastTargetPos = trackPos;

		// Drag less if we're looking at the ground
		dragVector += Vector3.Scale(dragDelta, new Vector3(1f, 1f, cameraForward));
		dragVector = Vector3.SmoothDamp(dragVector, Vector3.zero, ref dragVectorVelocity, posSmoothTime);

		Vector3 modifiedPlayerPos = trackPos + screenRotation * (forwardOffset + downOffset * cameraDown + dragVector) - transform.forward * normalDistance;
		Vector3 modifiedLookPos = lookPos + screenRotation * forwardOffset - transform.forward * lockOnDistance;

		float t = lockedOn ? Mathf.SmoothStep(0f, 1f, blendToLockOn) : blendToLockOn * blendToLockOn;
		transform.position = Vector3.Lerp(modifiedPlayerPos, modifiedLookPos, t);

		Debug.DrawLine(trackPos, trackPos + screenRotation * dragVector, Color.white);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos, Color.red);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos + screenRotation * dragVector, Color.blue);
	}
}
