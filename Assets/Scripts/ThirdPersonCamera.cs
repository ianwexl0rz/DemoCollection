using UnityEngine;
using Rewired;

public class ThirdPersonCamera : MonoBehaviour
{
	public bool isEnabled = false;
	public float distance = 2.5f;
	public float lowDistance = 1f;
	public Vector2 normalPitchMinMax = new Vector2(-40, 85);
	public Vector2 lockOnPitchMinMax = new Vector2(10, 50);
	public Vector3 offset = new Vector3(0f,1.6f,0.7f);
	public Vector3 dragAmount = Vector3.zero;
	public float lockOnHorizontalDrag = 0.4f;
	public float posSmoothTime = 0.12f;
	public float rotationSmoothTime = 0.12f;
	public float turnWithPlayerFactor = 20f;
	public float lockTime = 0.3f;
	public float unlockTime = 0.6f;
	public float towardCameraDragScale = 0.5f;
	public float overheadDragScale = 0.5f;

	private float yaw; //rotation on the y axis
	private float _pitch; //rotation on the x axis

	private Vector3 manualRotation;
	private Vector3 rotationVelocity;
	private Vector3 lockedDrag;

	private Vector3 lastTargetPos = Vector3.zero;
	private Vector3 dragVector = Vector3.zero;
	private Vector3 dragVectorVelocity = Vector3.zero;

	private Character player = null;
	private float blendToPlayer = 0f;
	private Vector3 previousPlayerPosition = Vector3.zero;

	private float focalHeight = 0f;
	private float previousFocalHeight = 0f;
	private Vector3 previousLookPos = Vector3.zero;
	private Vector3 trackPos = Vector3.zero;
	private ILockOnTarget lastLockOnTarget = null;

	private float lockBlend;
	private bool autoTurn;

	private Player rePlayer;

	public Quaternion referenceRotation { get; set; }

	private float pitch
	{
		get => _pitch;
		set
		{
			_pitch = value % 360f;

			if(_pitch > 180f) _pitch -= 360f;
			if(_pitch < -180f) _pitch += 360f;
		}
	}

	public void SetTarget(Character newPlayer, bool immediate)
	{
		rePlayer = GameManager.I.player;
		
		player = newPlayer;

		if(immediate)
		{
			focalHeight = player.CapsuleCollider.height * 0.5f;
			lastTargetPos = trackPos = player.GetLookPosition();
		}
		else
		{
			blendToPlayer = 1f;
		}

		previousPlayerPosition = trackPos;
		previousFocalHeight = focalHeight;
	}

	public void UpdatePositionAndRotation()
	{
		if(!player || !isEnabled) return;

		var dt = Time.fixedDeltaTime;

		// Cache look sensitivity from GameSettings
		float lookSensitivityX = GameSettings.I.lookSensitivityX;
		float lookSensitivityY = GameSettings.I.lookSensitivityY;

		//InputDevice playerInput = InputManager.ActiveDevice;

		lastTargetPos = trackPos;
		
		if(blendToPlayer > 0f)
		{
			blendToPlayer -= dt / unlockTime;
			blendToPlayer = Mathf.Max(blendToPlayer, 0f);
			float smoothBlend = Mathf.SmoothStep(1f, 0f, blendToPlayer);
			trackPos = Vector3.Lerp(previousPlayerPosition, player.GetLookPosition(), smoothBlend);
			focalHeight = Mathf.Lerp(previousFocalHeight, player.CapsuleCollider.height * 0.5f, smoothBlend);
			dragVector *= blendToPlayer;
			autoTurn = false;
		}
		else
		{
			trackPos = player.GetLookPosition();
			autoTurn = true;
		}

		var lockedOn = player.lockOn && player.lockOnTarget != null;

		if(lockedOn)
		{
			lockBlend = Mathf.Max(0f, lockBlend - dt);
			var blend = Mathf.SmoothStep(0f, 1f, lockBlend / lockTime);

			var playerToTarget = (player.lockOnTarget.GetGroundPosition() - trackPos).WithY(0f);
			var facingRotation = Quaternion.LookRotation(playerToTarget);

			Vector3 dragDelta = Quaternion.Inverse(facingRotation) * (lastTargetPos - trackPos);
			dragVector += Vector3.right * dragDelta.x * lockOnHorizontalDrag;
			dragVector += Vector3.up * dragDelta.y * dragAmount.y;

			dragVector = new Vector3()
			{
				x = Mathf.SmoothDamp(dragVector.x, 0f, ref dragVectorVelocity.x, posSmoothTime),
				y = Mathf.SmoothDamp(dragVector.y, 0f, ref dragVectorVelocity.y, posSmoothTime * 0.5f),
				z = Mathf.SmoothDamp(dragVector.z, 0f, ref dragVectorVelocity.z, posSmoothTime * 0.5f)
			};

			var dragAngle = Mathf.LerpAngle(facingRotation.eulerAngles.y, manualRotation.y, blend);
			var drag = Quaternion.Euler(0, dragAngle, 0) * dragVector;
			transform.position = trackPos + drag;

			var camToTarget = player.lockOnTarget.GetGroundPosition() - transform.position + Vector3.up * player.CapsuleCollider.height * 0.5f;
			var look = Quaternion.LookRotation(camToTarget);

			// Clamp pitch
			//look = Quaternion.Euler(look.eulerAngles.WithX(Mathf.Clamp(look.eulerAngles.x, lockOnPitchMinMax.x, lockOnPitchMinMax.y)));

			float offsetAngle;
			Quaternion screenRotation;

			if(lockBlend > 0f)
			{
				pitch = Mathf.LerpAngle(look.eulerAngles.x, manualRotation.x, blend);
				yaw = Mathf.LerpAngle(look.eulerAngles.y, manualRotation.y, blend);
				transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
				screenRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

				var localDrag = Quaternion.Inverse(screenRotation) * drag.WithY(0f);
				var refAngle = Vector3.Slerp(Vector3.right, localDrag * Mathf.Sign(localDrag.x), blend);
				offsetAngle = Vector3.Angle(localDrag, refAngle);
			}
			else
			{
				pitch = look.eulerAngles.x;
				yaw = look.eulerAngles.y;
				transform.rotation = look;
				screenRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

				var localDrag = Quaternion.Inverse(screenRotation) * drag.WithY(0f);
				offsetAngle = Vector3.Angle(localDrag, Vector3.right);
			}
			
			var offset = screenRotation * Vector3.forward * Mathf.Sin(offsetAngle * Mathf.Deg2Rad) * dragVector.WithY(0).magnitude;

			lockedDrag = drag + offset;
			transform.position += offset;

			Debug.DrawLine(trackPos, trackPos + drag, Color.cyan);
			Debug.DrawLine(trackPos + drag, trackPos + drag + offset, Color.magenta);
			Debug.DrawLine(trackPos, trackPos + drag + offset, Color.white);
		}
		else
		{
			lockBlend = Mathf.Min(lockTime, lockBlend + dt);

			if (player.Recenter)
			{
				yaw = player.transform.eulerAngles.y;
			}

			yaw += rePlayer.GetAxis(PlayerAction.LookHorizontal) * lookSensitivityX * dt;
			pitch += rePlayer.GetAxis(PlayerAction.LookVertical) * lookSensitivityY * dt;

			if(autoTurn)
			{
				// Rotate the camera slightly in the direction we're moving
				var playerVector = trackPos - lastTargetPos;
				var playerDotCam = Vector3.Dot(playerVector, transform.right);
				yaw += playerDotCam * turnWithPlayerFactor;
			}

			pitch = Mathf.Clamp(pitch, normalPitchMinMax.x, normalPitchMinMax.y);

			var current = transform.eulerAngles;

			current.x = Mathf.SmoothDampAngle(current.x, pitch, ref rotationVelocity.x, rotationSmoothTime);
			current.y = Mathf.SmoothDampAngle(current.y, yaw, ref rotationVelocity.y, rotationSmoothTime);

			transform.rotation = Quaternion.Euler(current);
			manualRotation = transform.eulerAngles;

			var drag = lockBlend < lockTime ? Vector3.Lerp(lockedDrag, GetLinearDrag(), lockBlend / lockTime) : GetLinearDrag();

			transform.position = trackPos + drag;

			Debug.DrawLine(trackPos, trackPos + drag, Color.white);
		}

		// Closer camera and less drag at low angle.
		var t = 1f - Mathf.InverseLerp(normalPitchMinMax.x, normalPitchMinMax.y, pitch);
		var dist = Mathf.Lerp(lowDistance, distance, 1f - t * t);
		var minDragScale = overheadDragScale;
		var dragScale = minDragScale + (1 - minDragScale) * (1f - t);

		transform.position = trackPos + (transform.position - trackPos) * dragScale;
		transform.position += transform.TransformDirection(offset) * dist;
		transform.position += transform.forward * focalHeight * transform.forward.y;
		transform.position -= transform.forward * dist;

		referenceRotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);
		lastLockOnTarget = player.lockOnTarget;
	}

	public Vector3 GetLinearDrag()
	{
		// We want drag relative to camera rotation so the character doesn't "fishtail" when the player looks around
		// 1) Get the change in player position and rotate it by the inverse of the camera rotation
		// 2) Now we can scale the amount of drag on each axis in screen space (yay!)
		// 3) Interpolate the accumulated drag toward zero over time
		// NOTE: Rotate dragVector by camera rotation later to put it back into world space

		var screenRotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);

		// Drag less if we're running toward the camera
		float dragAway = Vector3.Dot(screenRotation * dragVector.normalized, -transform.forward);
		dragAway = dragAway.LinearRemap(-1f, 1f, towardCameraDragScale, 1f);

		Vector3 dragDelta = Quaternion.Inverse(screenRotation) * (lastTargetPos - trackPos);
		dragVector += Vector3.Scale(dragDelta, dragAmount.WithZ(dragAmount.z * dragAway));

		dragVector = new Vector3()
		{
			x = Mathf.SmoothDamp(dragVector.x, 0f, ref dragVectorVelocity.x, posSmoothTime),
			y = Mathf.SmoothDamp(dragVector.y, 0f, ref dragVectorVelocity.y, posSmoothTime * 0.5f),
			z = Mathf.SmoothDamp(dragVector.z, 0f, ref dragVectorVelocity.z, posSmoothTime)
		};

		// Less forward drag if we're looking at the ground
		Vector3 modifiedDragVector = dragVector.WithZ(dragVector.z * Mathf.Lerp(overheadDragScale, 1f, 1f + transform.forward.y));

		return screenRotation * modifiedDragVector;
	}
}
