using UnityEngine;

public class Player : CombatActor
{
	[Header("Movement")]
	public float minSpeed = 1f;
	public float walkSpeed = 2f;
	public float runSpeed = 4f;
	public float lockOnSpeedScale = 1f;
	public Vector2 directionalSpeedScale = Vector2.one;
	public float jumpHeight = 4f;
	public int jumpCount = 1;
	public float gravityScale = 1f;
	public float rollSpeed = 5f;
	public float speedSmoothTime = 0.1f;
	public float maxAngularVelocity = 12;

	public bool Run { get; set; }
	public bool ShouldRoll { get; set; }
	public bool Recenter { get; set; }
	public bool AimingMode { get; set; }
	public bool RootMotionOverride { get; set; }

	private CapsuleCollider capsuleCollider;
	private float rollAngle;

	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	private PID3 angleController;
	private PID3 angularVelocityController;

	private Vector3 currentSpeed;
	private Vector3 speedSmoothVelocity;
	private Vector3 desiredDirection;

	private bool grounded;
	private bool queueJump;
	private bool wasLockedOn;
	private int remainingJumps;

	protected override void Awake()
	{
		base.Awake();
		desiredDirection = transform.forward;
		angleController = new PID3(angleControllerConfig);
		angularVelocityController = new PID3(angularVelocityControllerConfig);
		capsuleCollider = GetComponent<CapsuleCollider>();
		rb.maxAngularVelocity = maxAngularVelocity;

		// Timer example!
		//actorTimerGroup.Add(5f, () => Debug.Log("Started timer."), () => Debug.Log("Time's up!"));
	}

	private void OnValidate()
	{
		if(rb != null) rb.maxAngularVelocity = maxAngularVelocity;
	}

	private void OnDrawGizmosSelected()
	{
		// Draw foot collider
		//Gizmos.DrawSphere(transform.position + Vector3.up * 0.25f + Vector3.down * 0.1f, 0.2f);
	}

	//private void OnDrawGizmos()
	//{
	//	Gizmos.color = Color.black;
	//	for(int i = 0; i < weaponCollision.pointBuffer.Count; i++)
	//	{
	//		Gizmos.DrawSphere(weaponCollision.pointBuffer[i], 0.02f);
	//	}
	//}

	protected override void ProcessPhysics()
	{
		var dt = Time.fixedDeltaTime;

		if(stunned.InProgress) return;

		if(RootMotionOverride)
		{
			currentSpeed = Vector3.zero;
			UpdateRotation();
			return;
		}

		var groundPoint = transform.position + (transform.up + Vector3.down) * capsuleCollider.height * 0.5f;

		var point1 = transform.position + transform.up * capsuleCollider.radius;
		var point2 = transform.position + transform.up * (capsuleCollider.height - capsuleCollider.radius);
		var hits = Physics.CapsuleCastAll(point1, point2, 0.2f, Vector3.down, 0.1f, ~LayerMask.GetMask("Actor"), QueryTriggerInteraction.Ignore);

		//var hits = Physics.SphereCastAll(groundPoint + Vector3.up * 0.25f, 0.2f, Vector3.down, 0.1f, ~LayerMask.GetMask("Actor"), QueryTriggerInteraction.Ignore);
		var groundNormal = Vector3.down;

		bool wasGrounded = grounded;

		RaycastHit? groundHit = null;

		if(hits.Length > 0)
		{
			groundHit = hits[0];

			for(var i = 1; i < hits.Length; i++)
			{
				if(hits[i].normal.y > groundHit?.normal.y)
				{
					groundHit = hits[i];
				}
			}
			groundNormal = (Vector3)groundHit?.normal.normalized;

			if(!grounded)
			{
				remainingJumps = 0;
				grounded = true;
			}
		}
		else
		{
			grounded = false;
		}

		// Get the ground incline (positive = uphill, negative = downhill)
		var incline = Vector3.Dot(groundNormal, -currentSpeed.normalized);

		// We aren't grounded if the slope is too steep!
		grounded &= Mathf.Abs(incline) < 0.75f;

		var yVelocity = rb.velocity.y;

		if(!grounded && wasGrounded && remainingJumps == 0)
		{
			jumpAllowance.Reset();
			jumpAllowance.SetDuration(dt * 4);
		}

		// Disable extra jumps if we're falling too fast
		if(!grounded && !jumpAllowance.InProgress && yVelocity <= -5f)
		{
			remainingJumps = 0;
		}

		// Did we queue a jump?
		if(queueJump && remainingJumps > 0)
		{
			queueJump = false;
			grounded = false;
			remainingJumps--;

			// jump!
			yVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
		}
		
		var targetSpeed = move * Mathf.Max(minSpeed, (Run ? runSpeed : walkSpeed));
		var dot = Vector3.Dot(targetSpeed.normalized, transform.forward);
		targetSpeed *= dot >= 0
			? Mathf.Lerp(directionalSpeedScale.x, 1f, dot)
			: Mathf.Lerp(directionalSpeedScale.y, directionalSpeedScale.x, dot + 1f);
		targetSpeed *= lockOn ? lockOnSpeedScale : 1f;
		currentSpeed = Vector3.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime * (grounded ? 1f : 8f));

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
				var cross = Vector3.Cross(currentSpeed.normalized, Vector3.up);
				rb.velocity = Vector3.Cross(groundNormal, cross) * currentSpeed.magnitude;

				// Make the move speed a bit faster or slower depending on the incline
				rb.velocity *= 1 - incline * 0.5f;
			}

			// Counteract gravity (so we don't slide on an incline!)
			rb.velocity -= Physics.gravity * Time.fixedDeltaTime;
		}
		else
		{
			rb.velocity = currentSpeed.WithY(yVelocity);
			rb.velocity += Physics.gravity.y * (gravityScale - 1f) * Vector3.up * Time.fixedDeltaTime;
		}

		//rb.centerOfMass = grounded ? Vector3.zero : capsuleCollider.center;

		if(grounded)
		{
			var groundContact = capsuleCollider.ClosestPoint((Vector3)groundHit?.point);
			rb.centerOfMass = transform.InverseTransformPoint(groundContact);
		}
		else
		{
			rb.centerOfMass = capsuleCollider.center;
		}

		UpdateRotation();

		wasLockedOn = lockOn;
	}

	protected void UpdateRotation()
	{
		if(lockOn && lockOnTarget != null)
		{
			desiredDirection = (lockOnTarget.position - transform.position).WithY(0f).normalized;
		}
		else if(currentSpeed.WithY(0f).magnitude >= minSpeed)
		{
			desiredDirection = currentSpeed.WithY(0f);
		}

		rollAngle = ShouldRoll ? (rollAngle + rollSpeed) % 360f : 0f;

		var rotation = ShouldRoll ? GetRotationWithRoll() : Quaternion.LookRotation(desiredDirection);
		rb.RotateTo(angleController, angularVelocityController, rotation, Time.fixedDeltaTime);

		Quaternion GetRotationWithRoll()
		{
			var rollDir = lockOn && lockOnTarget != null && currentSpeed.WithY(0f).magnitude >= minSpeed
				? Quaternion.Inverse(Quaternion.LookRotation(desiredDirection)) * currentSpeed.WithY(0f).normalized
				: Vector3.forward;

			var rollRotation = Quaternion.AngleAxis(rollAngle, Vector3.Cross(rollDir, Vector3.down));
			return Quaternion.LookRotation(desiredDirection) * rollRotation;
		}
	}

	/*
	protected override void ProcessInput()
	{
		base.ProcessInput();

		// Cache look sensitivity from GameSettings
		var lookSensitivityX = ControlSettings.I.lookSensitivityX;
		var playerInput = InputManager.ActiveDevice;

		if(lockOn)
		{
			if(lockOnTarget != null)
			{
				// If locked on AND we have a target, look at the target
				look = Vector3.SignedAngle((lockOnTarget.position - transform.position).normalized, Vector3.forward, Vector3.up);
				//recenter = false;
			}
		}
		else if(aimingMode)
		{
			// We want to align the character to the camera
			look = Camera.main.transform.forward;
			//mesh.Rotate(Vector3.up, playerInput.RightStickX * lookSensitivityX * Time.deltaTime);
		}
		else if(recenter)
		{
			// We want to align the camera to the character
			//mesh.Rotate(Vector3.up, playerInput.RightStickX * lookSensitivityX * Time.deltaTime);
			look = Quaternion.AngleAxis(Camera.main.transform.eulerAngles.x, mesh.right) * mesh.forward;
		}
		else
		{
			// Normal camera
			look = move == Vector3.zero ? mesh.forward : move.normalized;
		}
	}
	*/

	protected override void ProcessAnimation()
	{
		if(animator == null || animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = paused ? 0f : currentSpeed.magnitude / runSpeed;

		//reference for animator
		animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);

		foreach(var parameter in animator.parameters)
		{
			switch(parameter.name)
			{
				case "inAir":
					animator.SetBool("inAir", !grounded);
					break;
				case "directionY":
					var directionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, rb.velocity.y));
					animator.SetFloat("directionY", directionY, speedSmoothTime, Time.deltaTime);
					break;
				case "velocityX":
					var velocityX = Vector3.Dot(currentSpeed, transform.right) / runSpeed;
					animator.SetFloat("velocityX", velocityX, speedSmoothTime, Time.deltaTime);
					break;
				case "velocityZ":
					var velocityZ = Vector3.Dot(currentSpeed, transform.forward) / runSpeed;
					animator.SetFloat("velocityZ", velocityZ, speedSmoothTime, Time.deltaTime);
					break;
			}
		}
	}

	public bool Jump()
	{
		bool firstJump = grounded || jumpAllowance.InProgress;
		if(!firstJump && remainingJumps == 0) { return false; }

		if(firstJump)
		{
			remainingJumps = jumpCount;
		}
		queueJump = true;
		return true;
	}

	public bool LightAttack()
	{
		if(isAttacking && !cancelOK) { return false; }

		if(animator != null) { animator.SetTrigger("lightAttack"); }
		return true;
	}
}
