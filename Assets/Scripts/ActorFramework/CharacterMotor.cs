using UnityEngine;

public class CharacterMotor : MonoBehaviour
{
	[Header("Ground Detection")]
	public float groundCheckHeight = 0.4f;
	public float rayOffsetDistance = 0.18f;
	public int extraGroundRays = 8;

	[Header("Movement")]
	public float minSpeed = 1f;
	public float walkSpeed = 2f;
	public float runSpeed = 4f;
	public float acceleration;
	public float lockOnSpeedScale = 1f;
	public Vector2 directionalSpeedScale = Vector2.one;
	public float jumpHeight = 4f;
	public int jumpCount = 1;
	public float gravityScale = 1f;
	public float rollSpeed = 5f;
	public float speedSmoothTime = 0.1f;
	public float leanFactor;
	public float friction;

	public bool Run { get; set; }
	public bool ShouldRoll { get; set; }
	public bool Recenter { get; set; }
	public bool AimingMode { get; set; }
	public Vector3 FeetPos => feetPos;

	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	private PID3 angleController;
	private PID3 angularVelocityController;
	private Vector3 groundVelocity = Vector3.zero;
	private Vector3 desiredDirection;
	private Vector3 feetPos;
	private Vector3 groundNormal;
	private Vector3 groundPoint;
	private float maxAngularVelocity = 50f;
	private float rollAngle;
	private bool grounded;
	private bool queueJump;
	private bool jumping;
	private int remainingJumps;
	private CharacterState state;

	private Character character = null;


	private enum CharacterState
	{
		Grounded,
		InAir,
		Jump
	}

	public void Init(Character character)
	{
		this.character = character;
		desiredDirection = character.transform.forward;
		angleController = new PID3(angleControllerConfig);
		angularVelocityController = new PID3(angularVelocityControllerConfig);
		remainingJumps = jumpCount;

		character.rb.maxAngularVelocity = maxAngularVelocity;

		// Timer example!
		//actorTimerGroup.Add(5f, () => Debug.Log("Started timer."), () => Debug.Log("Time's up!"));
	}

	private void OnValidate()
	{
		if(character && character.rb)
			character.rb.maxAngularVelocity = maxAngularVelocity;
	}

	private void OnDrawGizmos()
	{
		if(!character || !character.rb) { return; }

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(feetPos, 0.1f);
	}

	private bool CheckForGround()
	{
		var raycastOrigin = feetPos + Vector3.up * groundCheckHeight;
		var forward = Vector3.Cross(transform.right, Vector3.up);

		var groundHitCount = 0;
		var averageDistance = 0f;

		var averageNormal = Vector3.zero;
		var averagePoint = Vector3.zero;

		for(var i = 0; i < extraGroundRays; i++)
		{
			var dir = i > 0 ? Quaternion.Euler(0f, 360f * i / extraGroundRays, 0f) * forward : forward;
			var origin = raycastOrigin + dir * rayOffsetDistance;

			// Red = No Hit, Yellow = Hit, Green = Ground
			Color[] color = { Color.red, Color.yellow, Color.green };
			var status = 0;

			if(Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckHeight + 0.2f, ~LayerMask.GetMask("Actor", "ProxyObject")))
			{
				
				// This would prevent walking up ramps that are too steep
				//if(Vector3.Angle(hit.normal, Vector3.up) > 45f)
				//{
				//	status = 1;
				//	continue;
				//}

				averageNormal += hit.normal;
				averagePoint += hit.point;
				averageDistance += hit.distance;
				groundHitCount++;
				status = 2;
			}

			Debug.DrawLine(origin, origin + Vector3.down * groundCheckHeight, color[status]);
		}

		// Did we hit the ground?
		if(groundHitCount == 0) { return false; }

		averageNormal /= groundHitCount;
		averagePoint /= groundHitCount;
		averageDistance /= groundHitCount;

		// Is the ground too steep?
		if(Vector3.Angle(averageNormal, Vector3.up) > 45f) { return false; }

		// If we were already grounded, stay grounded
		if(grounded) { goto Success; }

		// Become grounded ONLY if the distance to ground is less than projected fall distance
		var projectedFallDistance = (-character.rb.velocity.y - Physics.gravity.y * gravityScale) * Time.fixedDeltaTime;
		if(averageDistance > projectedFallDistance) { return false; }

		Success:
		groundNormal = averageNormal;
		groundPoint = averagePoint;
		return true;
	}

	public void UpdateMotor()
	{
		var dt = Time.fixedDeltaTime;

		if(character.animator.applyRootMotion)
		{
			groundVelocity = Vector3.zero;
			return;
		}

		if(grounded)
		{
			// Invert ground normal rotation to get unbiased ground velocity
			var groundRotation = Quaternion.LookRotation(Vector3.forward, groundNormal);
			groundVelocity = Quaternion.Inverse(groundRotation) * character.rb.velocity;
		}
		else
		{
			groundVelocity = character.rb.velocity.WithY(0f);
		}

		if(!character.hitReaction.InProgress)
		{
			var speedLimit = Run || ShouldRoll ? runSpeed : walkSpeed;

			//TODO: Limit acceleration based on the amount of space in front of you
			var desiredVel = groundVelocity + character.move.normalized * (grounded ? acceleration : acceleration * 0.25f) * dt;
			var speed = desiredVel.magnitude;

			//TODO: Variable speed based on analog input
			if(grounded && !ShouldRoll) speed = Mathf.Max(desiredVel.magnitude - friction * dt, 0f);
			speed = Mathf.Min(speed, speedLimit);
			groundVelocity = (desiredVel.normalized * speed).WithY(0f);
		}

		UpdateFeetPos();

		CharacterState current = CharacterState.InAir;

		if(queueJump && remainingJumps > 0)
		{
			current = CharacterState.Jump;
		}
		else if(!jumping && CheckForGround())
		{
			current = CharacterState.Grounded;
		}

		SetState(current);
		UpdateRotation();
	}

	private void UpdateGroundVelocity()
	{
		var feetOffset = character.rb.position - feetPos;
		character.rb.MovePosition(character.rb.position.WithY(groundPoint.y + feetOffset.y));

		//var cross = Vector3.Cross(groundVelocity.normalized, Vector3.up);
		//var finalVelocity = Vector3.Cross(groundNormal, cross) * groundVelocity.magnitude;

		var finalVelocity = Quaternion.LookRotation(Vector3.forward, groundNormal) * groundVelocity;

		var point = feetPos + Vector3.up * leanFactor * groundVelocity.magnitude / runSpeed;
		character.rb.AddForceAtPosition(finalVelocity - character.rb.velocity, point, ForceMode.VelocityChange);

		Debug.DrawLine(feetPos, feetPos + groundNormal * (groundCheckHeight + character.capsuleCollider.height), Color.blue);
	}

	void SetState(CharacterState current)
	{
		state = current;

		switch(current)
		{
			case CharacterState.Grounded:

				if(!grounded)
				{
					//Debug.Log("Just landed!");
					grounded = true;
					remainingJumps = jumpCount;
				}

				UpdateGroundVelocity();
				break;

			case CharacterState.InAir:

				// Reset jump allowance if we ran off a ledge
				//if(grounded && !jumping) jumpAllowance.Reset(Time.fixedDeltaTime * 4);

				// Reset jump allowance if we ran off a ledge OR jumped
				if(grounded) character.jumpAllowance.Reset(Time.fixedDeltaTime * 4);

				grounded = false;

				// Ran out of coyote time and didn't jump
				//if(!jumpAllowance.InProgress && remainingJumps == jumpCount) remainingJumps = 0;

				if(!character.jumpAllowance.InProgress)
				{
					// Ran out of coyote time and didn't jump
					if(remainingJumps == jumpCount)
						remainingJumps = 0;

					// HACK: We jumped and it's OK to check for the ground again
					if(jumping)
						jumping = false;
				}

				// We passed the peak of the jump
				if(jumping && character.rb.velocity.y <= 0f) jumping = false;

				character.rb.velocity = groundVelocity.WithY(character.rb.velocity.y);
				character.rb.velocity += Physics.gravity * gravityScale * Time.fixedDeltaTime;
				break;

			case CharacterState.Jump:

				queueJump = false;
				grounded = false;
				jumping = true;
				remainingJumps--;

				var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
				character.rb.velocity = groundVelocity.WithY(jumpVelocity);
				break;
		}
	}

	private void UpdateFeetPos()
	{
		var halfHeight = (character.capsuleCollider.height + groundCheckHeight) * 0.5f;
		var radius = character.capsuleCollider.radius;

		var angle = Vector3.Angle(transform.up, Vector3.up);
		if(angle > 90f) angle -= 180f;

		feetPos = transform.position + transform.up * halfHeight + character.rb.velocity * Time.fixedDeltaTime;
		feetPos += Vector3.down * (Mathf.Cos(angle * Mathf.Deg2Rad) * (halfHeight - radius) + radius);

		character.rb.centerOfMass = transform.InverseTransformPoint(feetPos);
	}

	protected void UpdateRotation()
	{
		
		if(character.lockOn && character.lockOnTarget != null)
		{
			desiredDirection = (character.lockOnTarget.GetLockOnPosition() - character.GetLockOnPosition()).WithY(0f).normalized;
		}
		else
		{
			if(grounded && ShouldRoll && groundVelocity.sqrMagnitude > 0f)
			{
				desiredDirection = groundVelocity.normalized;
			}
			else if(character.move != Vector3.zero)
			{
				desiredDirection = character.move;
			}
		}

		//rollAngle = ShouldRoll ? (rollAngle + rollSpeed) % 360f : 0f;
		//var rotation = ShouldRoll ? GetRotationWithRoll() : Quaternion.LookRotation(desiredDirection);

		if(ShouldRoll || (rollAngle > 0f && rollAngle < 360f))
		{
			ShouldRoll = false;
			rollAngle += rollSpeed;
			if(rollAngle >= 360f) rollAngle = 0f;
		}

		var rotation = rollAngle > 0f ? GetRotationWithRoll() : Quaternion.LookRotation(desiredDirection);

		character.rb.RotateTo(angleController, angularVelocityController, rotation, Time.fixedDeltaTime);

		Quaternion GetRotationWithRoll()
		{
			var rollDir = character.lockOn && character.lockOnTarget != null && groundVelocity.magnitude >= minSpeed
				? Quaternion.Inverse(Quaternion.LookRotation(desiredDirection)) * groundVelocity.normalized
				: Vector3.forward;

			var rollRotation = Quaternion.AngleAxis(rollAngle, Vector3.Cross(rollDir, Vector3.down));
			return Quaternion.LookRotation(desiredDirection) * rollRotation;
		}
	}

	public void UpdateAnimation()
	{
		if(character.animator == null || character.animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = character.IsPaused ? 0f : groundVelocity.magnitude / runSpeed;

		//reference for animator
		character.animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);

		foreach(var parameter in character.animator.parameters)
		{
			switch(parameter.name)
			{
				case "inAir":
					character.animator.SetBool("inAir", !grounded);
					break;
				case "directionY":
					var directionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, character.rb.velocity.y));
					character.animator.SetFloat("directionY", directionY, speedSmoothTime, Time.deltaTime);
					break;
				case "velocityX":
					var velocityX = Vector3.Dot(groundVelocity, transform.right) / runSpeed;
					character.animator.SetFloat("velocityX", velocityX, speedSmoothTime, Time.deltaTime);
					break;
				case "velocityZ":
					var velocityZ = Vector3.Dot(groundVelocity, transform.forward) / runSpeed;
					character.animator.SetFloat("velocityZ", velocityZ, speedSmoothTime, Time.deltaTime);
					break;
			}
		}
	}

	public bool TryJump()
	{
		if(remainingJumps > 0)
		{
			queueJump = true;
			return true;
		}

		return false;
	}

	public bool TryRoll()
	{
		if(!ShouldRoll)
		{
			ShouldRoll = true;
			return true;
		}

		return false;
	}
}
