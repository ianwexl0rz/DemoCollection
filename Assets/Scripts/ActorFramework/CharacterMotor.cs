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
	public Vector2 directionalSpeedScale = Vector2.one;
	public float jumpHeight = 4f;
	public int jumpCount = 1;
	public float gravityScale = 1f;
	public float rollSpeed = 5f;
	public float speedSmoothTime = 0.1f;
	public float leanFactor;
	public float friction;
	[SerializeField] private float maxTurnGround = 45f;
	[SerializeField] private float maxTurnAir = 20f;
	[SerializeField] private CapsuleCollider hitBoxCollider = null;

	public bool Run { get; set; }
	public bool ShouldRoll { get; set; }
	public bool Recenter { get; set; }
	public bool AimingMode { get; set; }

	//public Vector3 FeetPos => feetPos;
	public Vector3 GroundVelocity => groundVelocity;
	public bool isGrounded;

	public bool IsLockedOn => character.lockOn && character.lockOnTarget != null;

	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	private PID3 angleController;
	private PID3 angularVelocityController;
	private Vector3 groundVelocity = Vector3.zero;
	private Quaternion inputOrientation;
	private Quaternion lockOnOrientation;
	private Vector3 groundNormal;
	private Vector3 groundPoint;
	private Vector3 groundCheckPoint;
	private float maxAngularVelocity = 50f;
	private float rollAngle;
	private bool queueJump;
	private bool jumping;
	private int remainingJumps;
	private Character character = null;

	public void Init(Character character)
	{
		this.character = character;
		inputOrientation = Quaternion.LookRotation(character.transform.forward);
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
		Gizmos.DrawSphere(character.rb.worldCenterOfMass, 0.1f);
	}

	public void UpdateMotor()
	{
		var dt = Time.fixedDeltaTime;
		Vector3 desiredVelocity;

		if (character.animator.applyRootMotion)
		{
			character.rb.angularVelocity = Vector3.zero;
			return;
		}

		if (isGrounded)
		{
			// Invert ground normal rotation to get unbiased ground velocity
			var groundRotation = Quaternion.LookRotation(Vector3.forward, groundNormal);
			desiredVelocity = Quaternion.Inverse(groundRotation) * character.rb.velocity;
		}
		else
		{
			desiredVelocity = character.rb.velocity.WithY(0f);
		}

		if (IsLockedOn)
		{
			var toLockOnTarget = (character.lockOnTarget.GetLookPosition() - character.GetLookPosition()).WithY(0f).normalized;
			lockOnOrientation = Quaternion.LookRotation(toLockOnTarget);
		}

		// Update desired velocity if there is input.
		if (character.move.normalized != Vector3.zero && character.InputEnabled && !character.hitReaction.InProgress)
		{
			var input = character.move;
			
			// Rotate the player toward the input direction based on max turn speed.
			var useGroundTurnRate = isGrounded && rollAngle < Mathf.Epsilon;
			var to = Quaternion.LookRotation(character.move.normalized);
			inputOrientation = Quaternion.RotateTowards(inputOrientation, to, useGroundTurnRate ? maxTurnGround : maxTurnAir);

			// If rolling on the ground, use the player direction instead of direct input.
			var rollingOnGround = isGrounded && rollAngle > Mathf.Epsilon;
			if (rollingOnGround) input = inputOrientation * Vector3.forward * character.move.magnitude;

			desiredVelocity += input * (isGrounded ? acceleration : acceleration * 0.25f) * dt;
		}

		var speed = desiredVelocity.magnitude;

		// Apply friction
		if (isGrounded && rollAngle < Mathf.Epsilon)
		{
			speed = Mathf.Max(desiredVelocity.magnitude - friction * dt, 0f);
		}

		// Speed is only variable when NOT sprinting.
		var normalSpeed = Mathf.Max(minSpeed, walkSpeed * character.move.sqrMagnitude);
		speed = Mathf.Min(speed, Run || ShouldRoll ? runSpeed : normalSpeed);
		groundVelocity = (desiredVelocity.normalized * speed).WithY(0f); 

		if (queueJump && remainingJumps > 0)
		{
			Jump();
		}

		// Set the center of mass to the point on the collider directly below the center, using it's current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		character.rb.centerOfMass = transform.InverseTransformPoint(hitInfo.point);

		var wasGrounded = isGrounded;
		isGrounded = !jumping && CheckForGround();

		if (isGrounded)
		{
			// Reset remaining jumps if we just landed.
			if (!wasGrounded) remainingJumps = jumpCount;

			var finalVelocity = Quaternion.LookRotation(Vector3.forward, groundNormal) * groundVelocity;

			// Apply force slightly above the center of mass to make the actor lean with acceleration.
			var offset = character.rb.worldCenterOfMass + Vector3.up * leanFactor * groundVelocity.magnitude / runSpeed;
			var deltaVelocity = finalVelocity - character.rb.velocity;
			character.rb.AddForceAtPosition(deltaVelocity, offset, ForceMode.VelocityChange);

			var end = character.rb.worldCenterOfMass + groundNormal * (groundCheckHeight + character.capsuleCollider.height);
			Debug.DrawLine(character.rb.worldCenterOfMass, end, Color.blue);
		}
		else
		{
			// Reset jump allowance if we ran off a ledge
			//if(grounded && !jumping) jumpAllowance.Reset(Time.fixedDeltaTime * 4);

			// Reset jump allowance if we ran off a ledge OR jumped
			if (wasGrounded) character.jumpAllowance.Reset(Time.fixedDeltaTime * 4);

			if (!character.jumpAllowance.InProgress)
			{
				// Ran out of coyote time and didn't jump
				if (remainingJumps == jumpCount) remainingJumps = 0;

				// HACK: We jumped and it's OK to check for the ground again
				if (jumping) jumping = false;
			}

			// We passed the peak of the jump
			if (jumping && character.rb.velocity.y <= 0f) jumping = false;

			character.rb.velocity = groundVelocity.WithY(character.rb.velocity.y);
			character.rb.velocity += Physics.gravity * gravityScale * Time.fixedDeltaTime;
		}

		if (ShouldRoll || (rollAngle > 0f && rollAngle < 360f))
		{
			ShouldRoll = false;
			rollAngle += rollSpeed * Time.fixedDeltaTime;

			if (rollAngle >= 360f) rollAngle = 0f;
		}

		// TODO: Smooth transition between orientations.
		var rotation = IsLockedOn ? lockOnOrientation : inputOrientation;

		if (rollAngle > 0)
		{
			var rollAxis = IsLockedOn && groundVelocity.magnitude >= minSpeed
				? Vector3.Cross(Quaternion.Inverse(rotation) * inputOrientation * Vector3.forward, Vector3.down)
				: Vector3.right;

			var rollRot = Quaternion.AngleAxis(rollAngle, rollAxis);
			rotation *= rollRot;
		}

		character.rb.RotateTo(angleController, angularVelocityController, rotation, Time.fixedDeltaTime);

		if (isGrounded)
		{
			var groundOffset = character.rb.position - character.rb.worldCenterOfMass;
			character.rb.MovePosition(character.rb.position.WithY(groundPoint.y + groundOffset.y));
		}
	}

	private bool CheckForGround()
	{
		var raycastOrigin = character.rb.worldCenterOfMass + Vector3.up * groundCheckHeight;
		var forward = Vector3.Cross(transform.right, Vector3.up);

		var groundHitCount = 0;
		var averageDistance = 0f;

		var averageNormal = Vector3.zero;
		var averagePoint = Vector3.zero;

		for (var i = 0; i < extraGroundRays; i++)
		{
			var dir = i > 0 ? Quaternion.Euler(0f, 360f * i / extraGroundRays, 0f) * forward : forward;
			var origin = raycastOrigin + dir * rayOffsetDistance;

			// Red = No Hit, Yellow = Hit, Green = Ground
			Color[] color = { Color.red, Color.yellow, Color.green };
			var status = 0;

			if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckHeight + 0.2f, ~LayerMask.GetMask("Actor", "ProxyObject")))
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
		if (groundHitCount == 0) { return false; }

		averageNormal /= groundHitCount;
		averagePoint /= groundHitCount;
		averageDistance /= groundHitCount;

		// Is the ground too steep?
		if (Vector3.Angle(averageNormal, Vector3.up) > 45f) { return false; }

		if (!isGrounded)
		{
			// Become grounded ONLY if the distance to ground is less than projected fall distance
			var projectedFallDistance = (-character.rb.velocity.y - Physics.gravity.y * gravityScale) * Time.fixedDeltaTime;
			if (averageDistance > projectedFallDistance) { return false; }
		}

		groundNormal = averageNormal;
		groundPoint = averagePoint;
		return true;
	}

	private void Jump()
	{
		queueJump = false;
		jumping = true;
		remainingJumps--;

		var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
		character.rb.velocity = groundVelocity.WithY(jumpVelocity);
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
