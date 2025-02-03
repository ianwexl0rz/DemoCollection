using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
	public bool isEnabled = true;
	public float distance = 2.5f;
	public float lowDistance = 1.8f;
	public Vector2 normalPitchMinMax = new Vector2(-40, 85);
	public Vector3 offset = new Vector3(0f,1.6f,0.7f);
	public Vector3 dragAmount = Vector3.one;
	public Vector3 dragSmoothTime = Vector3.one;
	public Vector3 lockOnDragAmount = Vector3.one;
	public Vector3 lockOnSmoothTime = Vector3.one;
	public float rotationSmoothTime = 0.05f;
	public float lockOnRotationSmoothTime = 0.2f;
	public float turnWithPlayerFactor = 4f;
	public float lockTime = 0.2f;
	public float unlockTime = 0.5f;
	public float changeTargetTime = 0.3f;
	public float towardCameraDragScale = 0.2f;
	public float overheadDragScale = 0.4f;
	
	private Trackable player = null;
	private bool autoTurn;
	private bool lockedOn;
	private float focalHeight = 0f;
	private Vector3 targetEulerAngles;
	private Vector3 rotationVelocity;
	private Vector3 lastTrackPos = Vector3.zero;
	private Vector3 localDrag = Vector3.zero;
	private Vector3 dragVectorVelocity = Vector3.zero;
	private Vector3 trackPos = Vector3.zero;
	private Quaternion yawRotation;
	private IEnumerator blendToPlayer;
	private IEnumerator<(Func<Quaternion, Quaternion>, Func<Quaternion, Quaternion>)> transitionToLockOnMode;
	private IEnumerator<Vector3> transitionToManual;

	public Quaternion YawRotation => yawRotation;

	public void SetTargetEulerAngles(Vector3 euler) => targetEulerAngles = euler;

	public void SetFollowTarget(Trackable newFollowTarget, bool immediate)
	{
		player = newFollowTarget;
		blendToPlayer = BlendToPlayer(immediate ? 0 : unlockTime);
	}
	
	private IEnumerator BlendToPlayer(float duration)
	{
		var initialPos = trackPos;
		var initialHeight = focalHeight;
		var time = 0f;
		while (time < duration)
		{
			var t = time / duration;
			lastTrackPos = trackPos = MathUtility.SmoothStep(initialPos, player.GetCenter(), t);
			focalHeight = Mathf.SmoothStep(initialHeight, player.GetHeight() * 0.5f, t);
			localDrag *= 1 - t;
			autoTurn = false;
			yield return null;
			time += Time.deltaTime;
		}
		
		lastTrackPos = trackPos = player.GetCenter();
		focalHeight = player.GetHeight() * 0.5f;
		localDrag = Vector3.zero;
		autoTurn = true;
	}

	private IEnumerator<(Func<Quaternion,Quaternion>, Func<Quaternion,Quaternion>)> TransitionToLockOnMode(float duration)
	{
		if (!lockedOn) yield break;

		var initialRot = transform.rotation;
		var initialYaw = Quaternion.AngleAxis(initialRot.eulerAngles.y, Vector3.up);
		var time = 0f;
		while (time < duration)
		{
			var t = time / duration;
			yield return
			(
				playerToTarget => MathUtility.SmoothStep(initialYaw, playerToTarget, t),
				camToTarget => MathUtility.SmoothStep(initialRot, camToTarget, t)
			);
			time += Time.deltaTime;
		}
	}
	
	private IEnumerator<Func<Vector3, Vector3>> ChangeLockOnTarget(Vector3 initialTargetPos, float duration)
	{
		if (!lockedOn) yield break;

		//var result = initialTargetPos;
		var time = 0f;
		while (time < duration)
		{
			var t = time / duration;
			yield return targetPos => MathUtility.SmoothStep(initialTargetPos, targetPos, t);
			time += Time.deltaTime;
		}
	}

	public void Init()
	{
		transitionToLockOnMode = TransitionToLockOnMode(lockTime);
	}
	
	public void UpdatePositionAndRotation(Vector2 lookInput, Trackable trackedTarget)
	{
		if (player == null || !isEnabled) return;

		var dt = Time.deltaTime;

		// Set the current tracked position. 
		if (!blendToPlayer.MoveNext())
		{
			lastTrackPos = trackPos;
			trackPos = player.GetCenter();
		}

		var shouldLockOn = (bool)trackedTarget;
		if (shouldLockOn && !lockedOn)
		{
			lockedOn = true;
			transitionToLockOnMode = TransitionToLockOnMode(lockTime);
		}

		var inTransition = transitionToLockOnMode.MoveNext();

		if (inTransition || shouldLockOn)
		{
			var (smoothPlayerToTarget, smoothCamToTarget) = transitionToLockOnMode.Current;
			
			// Get target position.
			var targetPos = trackedTarget.GetCenter();
			
			var playerToTarget = Quaternion.LookRotation(targetPos - player.GetCenter());
			
			// If transitioning to lock-on mode, interpolate from initial rotation.
			if (inTransition) playerToTarget = smoothPlayerToTarget(playerToTarget);

			// Drag less if we're running toward the camera
			var dragAway = Vector3.Dot(yawRotation * localDrag.normalized, -transform.forward);
			dragAway = dragAway.LinearRemap(-1f, 1f, towardCameraDragScale, 1f);
			
			// Accumulate drag in player-relative space.
			var dragDelta = Quaternion.Inverse(playerToTarget) * (lastTrackPos - trackPos);
			localDrag += Vector3.Scale(dragDelta, lockOnDragAmount.WithZ(lockOnDragAmount.z * dragAway));
			localDrag = MathUtility.SmoothDampPerAxis(localDrag, Vector3.zero, ref dragVectorVelocity, lockOnSmoothTime);

			// Get the rotation from the camera to the target.
			var camOrigin = trackPos + playerToTarget * localDrag + offset;
			var camToTarget = Quaternion.LookRotation(targetPos - camOrigin);
			
			// If transitioning to lock-on mode, interpolate from initial rotation.
			if (inTransition) camToTarget = smoothCamToTarget(camToTarget);
			
			// Set rotation directly...
			// transform.rotation = camToTarget;
			// yawRotation = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);
			
			// Set rotation.
			var eulerAngles = transform.eulerAngles;
			eulerAngles = MathUtility.SmoothDampAngle(eulerAngles, camToTarget.eulerAngles, ref rotationVelocity, lockOnRotationSmoothTime);
			transform.eulerAngles = eulerAngles;
			yawRotation = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);
			
			Debug.DrawLine(trackPos, targetPos, Color.cyan);
		}
		else
		{
			// Initialize manual control mode.
			if (lockedOn || !trackedTarget)
			{
				lockedOn = false;
				targetEulerAngles = transform.eulerAngles;
			}

			//if (player.Recenter) yaw = player.transform.eulerAngles.y;

			targetEulerAngles += new Vector3
			(
				lookInput.y * GameManager.Settings.LookSensitivityY * dt,
				lookInput.x * GameManager.Settings.LookSensitivityX * dt
			);

			if (autoTurn)
			{
				// Rotate the camera slightly in the direction we're moving
				var playerVector = trackPos - lastTrackPos;
				var playerDotCam = Vector3.Dot(playerVector, transform.right);
				targetEulerAngles.y += playerDotCam * turnWithPlayerFactor;
			}

			// Clamp pitch.
			targetEulerAngles.x = MathUtility.ClampAngle180(targetEulerAngles.x, normalPitchMinMax.x, normalPitchMinMax.y);

			// Set rotation.
			var eulerAngles = transform.eulerAngles;
			eulerAngles = MathUtility.SmoothDampAngle(eulerAngles, targetEulerAngles, ref rotationVelocity, rotationSmoothTime);
			transform.eulerAngles = eulerAngles;
			yawRotation = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);

			// Drag less if we're running toward the camera
			var dragAway = Vector3.Dot(yawRotation * localDrag.normalized, -transform.forward);
			dragAway = dragAway.LinearRemap(-1f, 1f, towardCameraDragScale, 1f);

			var dragDelta = Quaternion.Inverse(yawRotation) * (lastTrackPos - trackPos);
			localDrag += Vector3.Scale(dragDelta, dragAmount.WithZ(dragAmount.z * dragAway));
			localDrag = MathUtility.SmoothDampPerAxis(localDrag, Vector3.zero, ref dragVectorVelocity, dragSmoothTime);
		}

		var t = transform;
		
		// Closer camera and less drag at low angle.
		var (pitchMin, pitchMax) = (normalPitchMinMax.x, normalPitchMinMax.y);
		var highAngle = Mathf.InverseLerp(pitchMax, pitchMin, MathUtility.ClampAngle180(t.rotation.eulerAngles.x, pitchMin, pitchMax));
		var dist = Mathf.Lerp(lowDistance, distance, 1f - highAngle * highAngle);
		var dragScale = overheadDragScale + (1 - overheadDragScale) * (1f - highAngle);
		
		// Apply drag in camera space.
		var drag = yawRotation * (localDrag * dragScale);
		
		t.position = trackPos + drag + t.TransformDirection(offset) * dist + (t.forward.y * focalHeight - dist) * t.forward;
			
		Debug.DrawLine(trackPos, trackPos + drag, Color.white);
	}
}
