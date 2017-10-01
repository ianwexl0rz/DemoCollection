using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using InControl;

public class ThirdPersonCamera : MonoBehaviour
{
	public Vector3 offset = new Vector3(0f,2f,0f);
	public float normalDistance = 2;
	public Vector2 pitchMinMax = new Vector2(-40,85);
	public Vector3 dragAmount = Vector3.zero;
	public float rotationSmoothTime = 0.12f;
	public float posSmoothTime = 0.12f;
	public float switchTime = 0.24f;

	private float yaw; //rotation on the y axis
	private float pitch; //rotation on the x axis

	private Vector3 rotation;
	private Vector3 rotationVelocity;

	private float distance;
	private float distanceVelocity;

	private Vector3 dragVector = Vector3.zero;
	private Vector3 dragVectorVelocity = Vector3.zero;

	private Player player = null;
	private Player oldPlayer = null;
	private bool wasLockedOn = false;
	private float timer = -1f;
	private Vector3 lastTargetPos = Vector3.zero;

	public void Setup()
	{
		distance = normalDistance;
		player = GameManager.I.activePlayer;
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

					// TODO: Separate smoothing for pitch (should be unaffected)
					//smooth = 25f;

					//yaw = Quaternion.LookRotation(player.lockOnTarget.position - player.transform.position).eulerAngles.y;

					//float horizontalInput = ((PlayerBrain)player.brain).leftStick.x;
					//yaw += horizontalInput * 10f * Time.deltaTime;
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
				smooth = -Vector3.Dot(transform.forward, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
				smooth = smooth * 0.5f + 0.5f;
				smooth *= 3f;
			}
			else
			{
				// Not locked on
				yaw += playerInput.RightStickX * lookSensitivity.x * Time.deltaTime;

				// Rotate the camera slightly in the direction we're moving (like Dark Souls)
				yaw += playerInput.LeftStickX * 20f * Time.deltaTime;
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
		
		float desiredDistance = normalDistance;
		Vector3 trackPos = player.transform.position;

		bool switchedPlayer = player != oldPlayer;
		bool lockedOn = player.lockOn && player.lockOnTarget != null;

		Vector3 _dragAmount = dragAmount;
		float dragTime = posSmoothTime;

		Quaternion screenRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
		Quaternion invRotation = Quaternion.Euler(0f, -transform.rotation.eulerAngles.y, 0f);

		if(switchedPlayer)
		{
			timer = switchTime; // Set a timer
			oldPlayer = player;
		}

		if(lockedOn)
		{
			// Set the focus halfway between the player and the enemy (similar to BotW)
			trackPos = (player.lockOnTarget.position + player.transform.position) * 0.5f;
			desiredDistance += (player.lockOnTarget.position - player.transform.position).magnitude * 0.5f;
		}
		else if(wasLockedOn)
		{
			timer = switchTime; // Set the timer so we quickly return to the player
		}

		if(timer >= 0f)
		{
			timer -= Time.deltaTime;
			dragTime = switchTime;
			if(timer < 0f) timer = -1f; // We use -1 to indicate the timer is disabled
		}

		if(lockedOn != wasLockedOn || switchedPlayer)
		{
			_dragAmount = Vector3.one; // If we JUST changed modes, set drag scale to one
		}
		else if(lockedOn || player.aimingMode || player.recenter)
		{
			_dragAmount = Vector3.zero; // We want the camera to stick to the tracked position
			dragTime = switchTime; // We want to quickly move from the old position
		}

		wasLockedOn = lockedOn;

		// We want drag relative to camera rotation so the character doesn't "fishtail" when the player looks around
		// 1) Get the change in player position and rotate it by the inverse of the camera rotation
		// 2) Now we can scale the amount of drag on each axis in screen space (yay!)
		// 3) Interpolate the accumulated drag toward zero over time
		// NOTE: Rotate dragVector by camera rotation later to put it back into world space
		dragVector += Vector3.Scale(invRotation * (lastTargetPos - trackPos), _dragAmount);
		dragVector = Vector3.SmoothDamp(dragVector, Vector3.zero, ref dragVectorVelocity, dragTime);
		lastTargetPos = trackPos;

		distance = Mathf.SmoothDamp(distance, desiredDistance, ref distanceVelocity, dragTime);
		transform.position = trackPos + screenRotation * (offset + dragVector) - transform.forward * distance;

		/*
		Debug.DrawLine(trackPos, trackPos + screenRotation * dragVector, Color.white);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos, Color.red);
		Debug.DrawLine(Vector3.Scale(transform.position, Vector3.one - Vector3.up), trackPos + screenRotation * dragVector, Color.blue);
		*/
	}
}
