using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeleeCombat))]
public class Character : Actor
{
	private const float MaxAnimationStep = 1f / 30f;
	private const float MaxAngularVelocity = 50f;

	[Header("Ground Detection")]
	[SerializeField] private float groundCheckHeight = 0.4f;
	[SerializeField] private float rayOffsetDistance = 0.18f;
	[SerializeField] private int extraGroundRays = 8;

	[Header("Movement")]
	[SerializeField] private float minSpeed = 1f;
	[SerializeField] private float walkSpeed = 2f;
	[SerializeField] private float runSpeed = 4f;
	[SerializeField] private float acceleration = 40;
	[SerializeField] private Vector2 directionalSpeedScale = Vector2.one;
	[SerializeField] private float jumpHeight = 4f;
	[SerializeField] private int jumpCount = 1;
	[SerializeField] private float gravityScale = 1f;
	[SerializeField] private float rollSpeed = 540f;
	[SerializeField] private float speedSmoothTime = 0.1f;
	[SerializeField] private float leanFactor = 1.5f;
	[SerializeField] private float friction = 16;
	[SerializeField] private float maxTurnGround = 10f;
	[SerializeField] private float maxTurnAir = 4f;
	[SerializeField] private CapsuleCollider hitBoxCollider = null;
	[SerializeField] private PID3 torquePID = null;

	private bool isGrounded;
	private Vector3 torqueIntegral;
	private Vector3 torqueError;
	private Vector3 groundVelocity = Vector3.zero;
	private Quaternion inputOrientation;
	public Quaternion lockOnOrientation;
	private Vector3 groundNormal;
	private Vector3 groundPoint;
	private Vector3 groundCheckPoint;
	private float rollAngle;

	private bool queueLockOn;
	private bool queueRoll;
	private bool queueJump;
	
	private bool jumping;
	private int remainingJumps;
	private Matrix4x4 lastTRS;
	
	private static readonly int Attack = Animator.StringToHash("lightAttack");
	private static readonly int SpeedPercent = Animator.StringToHash("speedPercent");
	private static readonly int InAir = Animator.StringToHash("inAir");
	private static readonly int DirectionY = Animator.StringToHash("directionY");
	private static readonly int VelocityX = Animator.StringToHash("velocityX");
	private static readonly int VelocityZ = Animator.StringToHash("velocityZ");
	private static readonly int InHitStun = Animator.StringToHash("InHitStun");

	public bool Run { get; set; }
	public bool IsLockedOn => lockOn && lockOnTarget != null;

	public CapsuleCollider CapsuleCollider { get; private set; }
	public MeleeCombat MeleeCombat { get; private set; }

	public override void Awake()
	{
		base.Awake();
		MeleeCombat = GetComponent<MeleeCombat>();
		CapsuleCollider = GetComponent<CapsuleCollider>();

		inputOrientation = Quaternion.LookRotation(transform.forward);
		remainingJumps = jumpCount;
		rb.maxAngularVelocity = MaxAngularVelocity;

		var t = transform;
		lastTRS = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (rb != null)
			rb.maxAngularVelocity = MaxAngularVelocity;
	}

	private void OnDrawGizmosSelected()
	{
		if (rb == null) { return; }

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(rb.worldCenterOfMass, 0.1f);
	}
#endif

	protected override void UpdatePhysics(float deltaTime)
	{
		Vector3 desiredVelocity;

		if (animator.applyRootMotion)
		{
			rb.angularVelocity = Vector3.zero;
			return;
		}

		if (isGrounded)
		{
			// Invert ground normal rotation to get unbiased ground velocity
			var groundRotation = Quaternion.LookRotation(Vector3.forward, groundNormal);
			desiredVelocity = Quaternion.Inverse(groundRotation) * rb.velocity;
		}
		else
		{
			desiredVelocity = rb.velocity.WithY(0f);
		}

		if (queueLockOn)
		{
			queueLockOn = false;
			lockOn = !lockOn;
		}
		
		if(lockOn)
		{
			if (lockOnTarget != null)
			{
				var toLockOnTarget = (lockOnTarget.GetLookPosition() - GetLookPosition()).WithY(0f).normalized;
				lockOnOrientation = Quaternion.LookRotation(toLockOnTarget);
			}
			else
			{
				lockOn = false;
				//TODO: Fix camera recentering.
			}
		}

		// Update desired velocity if there is input.
		if (move.normalized != Vector3.zero && InputEnabled && !hitReaction.InProgress)
		{
			var input = move;

			// Rotate the player toward the input direction based on max turn speed.
			var useGroundTurnRate = isGrounded && rollAngle < Mathf.Epsilon;
			var to = Quaternion.LookRotation(move.normalized);
			inputOrientation = Quaternion.RotateTowards(inputOrientation, to, useGroundTurnRate ? maxTurnGround : maxTurnAir);

			// If rolling on the ground, use the player direction instead of direct input.
			var rollingOnGround = isGrounded && rollAngle > Mathf.Epsilon;
			if (rollingOnGround) input = inputOrientation * Vector3.forward * move.magnitude;

			desiredVelocity += deltaTime * (isGrounded ? acceleration : acceleration * 0.25f) * input;
		}

		var speed = desiredVelocity.magnitude;

		// Apply friction
		if (isGrounded && rollAngle < Mathf.Epsilon)
		{
			speed = Mathf.Max(desiredVelocity.magnitude - friction * deltaTime, 0f);
		}

		// Speed is only variable when NOT sprinting.
		var normalSpeed = Mathf.Max(minSpeed, walkSpeed * move.sqrMagnitude);
		speed = Mathf.Min(speed, Run || queueRoll ? runSpeed : normalSpeed);
		groundVelocity = (desiredVelocity.normalized * speed).WithY(0f);

		if (queueJump && remainingJumps > 0)
		{
			Jump();
		}

		// Set the center of mass to the point on the collider directly below the center, using it's current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		groundCheckPoint = hitInfo.point;

		var wasGrounded = isGrounded;
		isGrounded = !jumping && CheckForGround();

		if (isGrounded)
		{
			// Reset remaining jumps if we just landed.
			if (!wasGrounded) remainingJumps = jumpCount;

			var finalVelocity = Quaternion.LookRotation(Vector3.forward, groundNormal) * groundVelocity;

			// Apply force slightly above the center of mass to make the actor lean with acceleration.
			var offset = rb.worldCenterOfMass + Vector3.up * (leanFactor * groundVelocity.magnitude / runSpeed);
			var deltaVelocity = finalVelocity - rb.velocity;
			rb.AddForceAtPosition(deltaVelocity, offset, ForceMode.VelocityChange);

			rb.centerOfMass = transform.InverseTransformPoint(groundCheckPoint);

			//var end = groundCheckPoint + groundNormal * (groundCheckHeight + CapsuleCollider.height);
			//Debug.DrawLine(groundCheckPoint, end, Color.blue);
		}
		else
		{
			// Reset jump allowance if we ran off a ledge
			//if(grounded && !jumping) jumpAllowance.Reset(Time.fixedDeltaTime * 4);

			// Reset jump allowance if we ran off a ledge OR jumped
			if (wasGrounded) jumpAllowance.Reset(Time.fixedDeltaTime * 4);

			if (!jumpAllowance.InProgress)
			{
				// Ran out of coyote time and didn't jump
				if (remainingJumps == jumpCount) remainingJumps = 0;

				// HACK: We jumped and it's OK to check for the ground again
				if (jumping) jumping = false;
			}

			// We passed the peak of the jump
			if (jumping && rb.velocity.y <= 0f) jumping = false;

			rb.velocity = groundVelocity.WithY(rb.velocity.y) + Physics.gravity * (gravityScale * Time.fixedDeltaTime);

			rb.centerOfMass = CapsuleCollider.center;

		}

		if (queueRoll || (rollAngle > 0f && rollAngle < 360f))
		{
			queueRoll = false;
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

		var targetTorque = rb.rotation.TorqueTo(rotation, deltaTime);
		var torque = torquePID.Output(rb.angularVelocity, targetTorque, ref torqueIntegral, ref torqueError, deltaTime);
		rb.AddTorque(torque, ForceMode.Acceleration);
		
		if (isGrounded)
		{
			var groundOffset = rb.position - groundCheckPoint;
			rb.MovePosition(rb.position.WithY(groundPoint.y + groundOffset.y));
		}
	}

	protected override void UpdateAnimation(float deltaTime)
	{
		UpdateAnimationParameters();

		var loops = Mathf.CeilToInt(deltaTime / MaxAnimationStep);
		var dt = deltaTime / loops;

		var combatEvents = new List<CombatEvent>();
		for (var i = 0; i < loops; i++)
		{
			animator.Update(dt);

			// Calculate the position and rotation the weapon WOULD have if the character did not move/rotate this frame.
			// This allows us to blend to the ACTUAL position/rotation over multiple steps.
			var lastWeaponPos = lastTRS.MultiplyPoint3x4(transform.InverseTransformPoint(MeleeCombat.WeaponRoot.position));
			var lastWeaponRot = lastTRS.rotation * Quaternion.Inverse(transform.rotation) * MeleeCombat.WeaponRoot.rotation;

			if (MeleeCombat.ActiveHit)
			{
				if (MeleeCombat.CheckHits((i + 1f) / loops, lastWeaponPos, lastWeaponRot, ref combatEvents))
				{
					//TODO: If we hit more than one thing, trigger hits over sequential frames?
					GameManager.MainMode.AddCombatEvents(combatEvents);
				}
			}
		}

		var transform1 = transform;
		lastTRS = Matrix4x4.TRS(transform1.position, transform1.rotation, transform1.localScale);
	}

	private bool CheckForGround()
	{
		var raycastOrigin = groundCheckPoint + Vector3.up * groundCheckHeight;
		var forward = Vector3.Cross(transform.right, Vector3.up).normalized;

		var groundHitCount = 0;
		var averageDistance = 0f;

		var averageNormal = Vector3.zero;
		var averagePoint = Vector3.zero;

		for (var i = 0; i < extraGroundRays; i++)
		{
			var dir = i > 0 ? Quaternion.Euler(0f, 360f * i / extraGroundRays, 0f) * forward : forward;
			var origin = raycastOrigin + dir * rayOffsetDistance;

			// Red = No Hit, Yellow = Hit, Green = Ground
			//Color[] color = { Color.red, Color.yellow, Color.green };
			//var status = 0;

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
				//status = 2;
			}

			//Debug.DrawLine(origin, origin + Vector3.down * groundCheckHeight, color[status]);
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
			var projectedFallDistance = (-rb.velocity.y - Physics.gravity.y * gravityScale) * Time.fixedDeltaTime;
			if (averageDistance > projectedFallDistance) { return false; }
		}

		groundNormal = averageNormal;
		groundPoint = averagePoint;
		return true;
	}

	public override Vector3 GetLookPosition()
	{
		return transform.TransformPoint(CapsuleCollider.center);
		//return motor.FeetPos;
	}

	public override Vector3 GetGroundPosition()
	{
		return transform.position;
	}

	private void Jump()
	{
		queueJump = false;
		jumping = true;
		remainingJumps--;

		var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
		rb.velocity = groundVelocity.WithY(jumpVelocity);
	}

	public bool TryLockOn()
	{
		queueLockOn = true;
		return true;
	}
	
	public bool TryJump()
	{
		if(remainingJumps <= 0) return false;
		queueJump = true;
		return true;
	}

	public bool TryRoll()
	{
		if(queueRoll) return false;
		queueRoll = true;
		return true;
	}

	public bool TryAttack()
	{
		if(MeleeCombat.isAttacking || !InputEnabled) { return false; }

		MeleeCombat.isAttacking = true;
		InputEnabled = false;
		//meleeCombat.cancelOK = false;

		// TODO: Should maybe set attack ID and generic attack trigger?
		if(animator != null) { animator.SetTrigger(Attack); }
		return true;
	}

	private void UpdateAnimationParameters()
	{
		if(animator == null || animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = IsPaused ? 0f : groundVelocity.magnitude / runSpeed;

		//reference for animator
		animator.SetFloat(SpeedPercent, animationSpeedPercent, speedSmoothTime, Time.deltaTime);

		foreach(var parameter in animator.parameters)
		{
			var nameHash = parameter.nameHash;
			if(nameHash == InAir)
			{
				animator.SetBool(InAir, !isGrounded);
			}
			else if(nameHash == DirectionY)
			{
				var directionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, rb.velocity.y));
				animator.SetFloat(DirectionY, directionY, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == VelocityX)
			{
				var velocityX = Vector3.Dot(groundVelocity, transform.right) / runSpeed;
				animator.SetFloat(VelocityX, velocityX, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == VelocityZ)
			{
				var velocityZ = Vector3.Dot(groundVelocity, transform.forward) / runSpeed;
				animator.SetFloat(VelocityZ, velocityZ, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == InHitStun)
			{
				animator.SetBool(InHitStun, hitReaction.InProgress);
			}
		}
	}
}
