using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class Player : Actor
{
	public float minSpeed = 1f;
	public float walkSpeed = 2f;
	public float runSpeed = 4f;
	public float jumpStrength = 4f;
	public float speedSmoothTime = 0.1f;
	public float turnSmoothTime = 0.2f; //time it takets from angle to go from current value to target value

	public bool aimingMode = false;
	public bool recenter = false;

	private Vector3 currentSpeed;

	private bool grounded = false;
	private bool queueJump = false;

	private bool doubleJumpOK = false;

	private float turnSmoothVelocity; //ref
	private Vector3 speedSmoothVelocity;

	public bool run { get; set; }
	public bool jump { get; set; }

	protected override void OnEnable()
	{
		base.OnEnable();
		OnUpdate += ProcessInput;
		OnFixedUpdate += ProcessPhysics;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		OnUpdate -= ProcessInput;
		OnFixedUpdate -= ProcessPhysics;
	}

	private void ProcessPhysics()
	{
		RaycastHit hitInfo;
		grounded = Physics.SphereCast(transform.position + Vector3.up * 0.55f, 0.5f, Vector3.down, out hitInfo, 0.1f);
		//grounded &= rb.velocity.y < jumpStrength * 0.5f; // We're not grounded if we're jumping into an incline

		Vector3 groundNormal = grounded ? hitInfo.normal.normalized : Vector3.up;

		// Get the ground incline (positive = uphill, negative = downhill)
		float incline = Vector3.Dot(groundNormal, -currentSpeed.normalized);

		// We aren't grounded if the slope is too steep!
		grounded &= Mathf.Abs(incline) < 0.75f;

		// Disable double jump if we landed or we're falling too fast
		if(grounded || rb.velocity.y <= -3f)
		{
			doubleJumpOK = false;
		}

		// If we queued a jump but we are not grounded, forget it!
		queueJump &= grounded || doubleJumpOK;

		// Did we queue a jump?
		if(queueJump)
		{
			// jump!
			rb.velocity = new Vector3(rb.velocity.x, jumpStrength, rb.velocity.z);
			queueJump = false;

			doubleJumpOK = grounded; // Set double jump is OK if this is our first jump
			grounded = false; // We are not grounded anymore
		}

		if(grounded) // No directional input in the air
		{
			if(incline >= 0f)
			{
				// Set move velocity if we are on level ground OR going uphill
				rb.velocity = currentSpeed;
			}
			else
			{
				// Do some math to make the move vector parallel to the ground
				Vector3 cross = Vector3.Cross(currentSpeed.normalized, Vector3.up);
				rb.velocity = Vector3.Cross(groundNormal, cross) * currentSpeed.magnitude;

				// Make the move speed a bit faster or slower depending on the incline
				//rb.velocity *= 1 - incline * 0.5f;
			}

			// Counteract gravity (so we don't slide on an incline!)
			rb.velocity -= Physics.gravity * Time.fixedDeltaTime;
		}
		else
		{
			rb.velocity = new Vector3(currentSpeed.x, rb.velocity.y, currentSpeed.z);
		}
	}

	private void ProcessInput()
	{
		if(jump)
		{
			// Queue a jump if the jump was pressed this frame.
			queueJump = true;
		}

		// Interpolate from our current speed to the target speed.
		Vector3 targetSpeed = move * Mathf.Max(minSpeed, (run ? runSpeed : walkSpeed));
		currentSpeed = Vector3.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime * (grounded ? 1f : 8f));

		// Cache look sensitivity from GameSettings
		Vector2 lookSensitivity = ControlSettings.I.lookSensitivity;
		InputDevice playerInput = InputManager.ActiveDevice;

		if(lockOn)
		{
			if(lockOnTarget != null)
			{
				// If locked on AND we have a target, look at the target
				look = (lockOnTarget.position - transform.position).normalized;
				recenter = false;
			}
		}
		else if(aimingMode)
		{
			// We want to align the character to the camera
			look = Camera.main.transform.forward;
			mesh.Rotate(Vector3.up, playerInput.RightStickX * lookSensitivity.x * Time.deltaTime);
		}
		else if(recenter)
		{
			// We want to align the camera to the character
			mesh.Rotate(Vector3.up, playerInput.RightStickX * lookSensitivity.x * Time.deltaTime);
			look = Quaternion.AngleAxis(Camera.main.transform.eulerAngles.x, mesh.right) * mesh.forward;
		}
		else
		{
			// Normal camera
			look = move == Vector3.zero ? mesh.forward : move.normalized;
		}

		// Interpolate our rotation to the desired look rotation
		float smoothLook = Mathf.SmoothDampAngle(mesh.eulerAngles.y, Quaternion.LookRotation(look).eulerAngles.y, ref turnSmoothVelocity, turnSmoothTime * (grounded ? 1f : 4f));

		//if(grounded) // No directional control in the air
		{
			mesh.rotation = Quaternion.AngleAxis(smoothLook, Vector3.up);
		}

		if(animator != null && animator.runtimeAnimatorController != null)
		{
			//control speed percent in animator so that character walks or runs depending on speed
			float animationSpeedPercent = currentSpeed.magnitude / runSpeed;

			//reference for animator
			animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
		}
	}
}
