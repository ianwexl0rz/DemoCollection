using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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
	[SerializeField] private float airAcceleration = 10;
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
	[SerializeField] private PID3 planarVelocityPID = null;

	private bool isGrounded;
	private Vector3 torqueIntegral;
	private Vector3 torqueError;
	private Vector3 planarVelocityIntegral;
	private Vector3 planarVelocityError;
	private Vector3 groundVelocity = Vector3.zero;
	private Vector3 lookDirection;
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
	private Quaternion desiredRotation;

	public bool Run { get; set; }
	public ILockOnTarget lockOnTarget { get; set; }
	public CapsuleCollider CapsuleCollider { get; private set; }
	public MeleeCombat MeleeCombat { get; private set; }

	public override void Awake()
	{
		base.Awake();
		MeleeCombat = GetComponent<MeleeCombat>();
		CapsuleCollider = GetComponent<CapsuleCollider>();

		//inputOrientation = Quaternion.LookRotation(transform.forward);
		remainingJumps = jumpCount;
		rb.maxAngularVelocity = MaxAngularVelocity;

		var t = transform;
		lastTRS = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (rb != null) rb.maxAngularVelocity = MaxAngularVelocity;
	}

	private void OnDrawGizmosSelected()
	{
		if (rb == null) { return; }
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(rb.worldCenterOfMass, 0.1f);
	}
#endif

	public void SetLockOnTarget(ILockOnTarget target) => lockOnTarget = target;

	protected override void UpdatePhysics(float deltaTime)
	{
		Vector3 desiredVelocity;

		// Stop angular velocity if animation has root motion.
		if (Animator.applyRootMotion)
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

			if (lockOnTarget != null)
			{
				// If we were locked on, break lock...
				lockOnTarget = null;
			}
			else
			{
				// If we were not locked on, try to assign target...
				var candidate = LockOn.LockOnCandidate;
				if (candidate != null) lockOnTarget = candidate;
				else
				{
					// Recenter the camera if there is no viable target.
					var cross = Vector3.Cross(transform.right, Vector3.up);
					var lookRotation = Quaternion.LookRotation(cross);
					GameManager.Camera.SetTargetEulerAngles(lookRotation.eulerAngles);
				}
			}
		}

		if (!MeleeCombat.isAttacking)
		{
			desiredVelocity += (isGrounded ? acceleration : airAcceleration) * deltaTime * Move;
		}

		var speed = desiredVelocity.magnitude;
		
		// Apply friction
		if (isGrounded && rollAngle < Mathf.Epsilon)
		{
			speed = Mathf.Max(desiredVelocity.magnitude - friction * deltaTime, 0f);
		}

		if (isGrounded)
		{
			// Speed is only variable when NOT sprinting.
			//var normalSpeed = Mathf.Max(minSpeed, walkSpeed * move.sqrMagnitude);
			var shouldRun = Run && rollAngle < Mathf.Epsilon;
			var targetSpeed = shouldRun ? runSpeed : walkSpeed;

			// Slower while walking backwards...
			//var dot = Vector3.Dot(desiredVelocity, transform.forward);
			//targetSpeed = Mathf.Lerp(targetSpeed, targetSpeed * 0.5f, -dot);
			speed = Mathf.Min(speed, targetSpeed);
		}

		groundVelocity = (desiredVelocity.normalized * speed).WithY(0f);
		
		// var shouldRun = Run && rollAngle < Mathf.Epsilon;
		// var targetSpeed = shouldRun ? runSpeed : walkSpeed;
		// var targetVelocity = (move * targetSpeed).WithY(rb.velocity.y);
		// var acc = planarVelocityPID.Output(rb.velocity, targetVelocity, ref planarVelocityIntegral,
		// 	ref planarVelocityError, deltaTime);
		// rb.AddForce(acc, ForceMode.Acceleration);
		// groundVelocity = rb.velocity.WithY(0);
		
		// Jump.
		if (remainingJumps > 0 && TryConsumeAction(PlayerAction.Jump))
		{
			jumping = true;
			remainingJumps--;

			var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
			rb.velocity = groundVelocity.WithY(jumpVelocity);
		}
		

		// Get the point directly below the center of the capsule, using its current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		groundCheckPoint = hitInfo.point;

		// Update grounded status.
		var wasGrounded = isGrounded;
		isGrounded = !jumping && CheckForGround();

		if (isGrounded)
		{
			// Reset remaining jumps if we just landed.
			if (!wasGrounded) remainingJumps = jumpCount;

			// Make ground velocity perpendicular to ground normal.
			var finalVelocity = Quaternion.LookRotation(Vector3.forward, groundNormal) * groundVelocity;
			
			// Set center of mass to the point on the capsule directly below the center.
			rb.centerOfMass = transform.InverseTransformPoint(groundCheckPoint);

			// Apply force slightly above the center of mass to make the actor lean with acceleration.
			var offset = rb.worldCenterOfMass + Vector3.up * (leanFactor * groundVelocity.magnitude / runSpeed);
			var deltaVelocity = finalVelocity - rb.velocity;
			rb.AddForceAtPosition(deltaVelocity, offset, ForceMode.VelocityChange);
			
			// Snap to ground.
			var groundOffset = rb.position - groundCheckPoint;
			rb.MovePosition(rb.position.WithY(groundPoint.y + groundOffset.y));
		}
		else
		{
			// Reset jump allowance if we ran off a ledge OR jumped
			if (wasGrounded) JumpAllowance.Reset(Time.fixedDeltaTime * 4);
			
			// Set center of mass to the center while airborne.
			rb.centerOfMass = CapsuleCollider.center;

			if (!JumpAllowance.InProgress)
			{
				// Ran out of coyote time and didn't jump
				if (remainingJumps == jumpCount) remainingJumps = 0;

				// HACK: We jumped and it's OK to check for the ground again
				jumping = false;
			}

			// We passed the peak of the jump
			if (jumping && rb.velocity.y <= 0f) jumping = false;

			// Set new velocity.
			rb.velocity = groundVelocity.WithY(rb.velocity.y) + Physics.gravity * (gravityScale * Time.fixedDeltaTime);
		}

		// Roll.
		if (rollAngle > 0f && rollAngle < 360f || TryConsumeAction(PlayerAction.Roll))
		{
			rollAngle += rollSpeed * Time.fixedDeltaTime;
			if (rollAngle >= 360f) rollAngle = 0f;
		}

		// Handle rotation.
		var targetRotation = GetTargetRotation();
		var targetTorque = rb.rotation.TorqueTo(targetRotation, deltaTime);
		var torque = torquePID.Output(rb.angularVelocity, targetTorque, ref torqueIntegral, ref torqueError, deltaTime);
		rb.AddTorque(torque, ForceMode.Acceleration);

		if (!MeleeCombat.isAttacking && InputEnabled && TryConsumeAction(PlayerAction.Attack))
		{
			MeleeCombat.isAttacking = true;
			InputEnabled = false;

			// TODO: Should maybe set attack ID and generic attack trigger?
			if(Animator != null) { Animator.SetTrigger(Attack); }
		}
	}

	private Quaternion GetTargetRotation()
	{
		var maxTurnRate = isGrounded && rollAngle < Mathf.Epsilon ? maxTurnGround : maxTurnAir;
		var rollAxis = Vector3.right;

		var validLookInput = isGrounded && Move.normalized != Vector3.zero && InputEnabled && !HitReaction.InProgress;
		if (validLookInput) lookDirection = Move.normalized;

		if (lockOnTarget != null)
		{
			var toLockOnTarget = (lockOnTarget.GetLookPosition() - GetLookPosition()).WithY(0f).normalized;
			var lockOnOrientation = Quaternion.LookRotation(toLockOnTarget);
			desiredRotation = Quaternion.RotateTowards(desiredRotation, lockOnOrientation, maxTurnRate);

			if (rollAngle > 0 && groundVelocity.magnitude >= minSpeed)
				rollAxis = Vector3.Cross(Quaternion.Inverse(lockOnOrientation) * lookDirection, Vector3.down);
		}
		else if (validLookInput)
		{
			//if (groundVelocity != Vector3.zero)
			//	desiredRotation = Quaternion.LookRotation(groundVelocity);
			
			desiredRotation = Quaternion.RotateTowards(desiredRotation, Quaternion.LookRotation(lookDirection.normalized), maxTurnRate);
		}

		return rollAngle > 0 ? desiredRotation * Quaternion.AngleAxis(rollAngle, rollAxis) : desiredRotation;
	}

	protected override void UpdateAnimation(float deltaTime)
	{
		UpdateAnimationParameters();

		var loops = Mathf.CeilToInt(deltaTime / MaxAnimationStep);
		var dt = deltaTime / loops;

		var combatEvents = new List<CombatEvent>();
		for (var i = 0; i < loops; i++)
		{
			Animator.Update(dt);

			// Calculate the position and rotation the weapon WOULD have if the character did not move/rotate this frame.
			// This allows us to blend to the ACTUAL position/rotation over multiple steps.
			var lastWeaponPos = lastTRS.MultiplyPoint3x4(transform.InverseTransformPoint(MeleeCombat.WeaponRoot.position));
			var lastWeaponRot = lastTRS.rotation * Quaternion.Inverse(transform.rotation) * MeleeCombat.WeaponRoot.rotation;

			if (MeleeCombat.ActiveHit)
			{
				if (MeleeCombat.CheckHits((i + 1f) / loops, lastWeaponPos, lastWeaponRot, ref combatEvents))
				{
					//TODO: If we hit more than one thing, trigger hits over sequential frames?
					MainMode.AddCombatEvents(combatEvents);
				}
			}
		}
		
		// Set the center of mass to the point on the collider directly below the center, using it's current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		groundCheckPoint = hitInfo.point;

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
			Color[] color = { Color.red, Color.yellow, Color.green };
			var status = 0;

			if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckHeight + 0.2f, ~LayerMask.GetMask("Actor", "ProxyObject")))
			{
				// This would prevent walking up ramps that are too steep
				if(Vector3.Angle(hit.normal, Vector3.up) > 45f)
				{
					status = 1;
				}
				else
				{
					averageNormal += hit.normal;
					averagePoint += hit.point;
					averageDistance += hit.distance;
					groundHitCount++;
					status = 2;
				}
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
	}

	public override Vector3 GetGroundPosition()
	{
		return groundCheckPoint;//transform.position;
	}

	public void QueueLockOn() => queueLockOn = true;

	public override void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		base.ApplyHit(instigator, point, direction, attackData);
		
		MeleeCombat.isAttacking = false;
		MeleeCombat.cancelOK = true;
	}

	private void UpdateAnimationParameters()
	{
		if(Animator == null || Animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = IsPaused ? 0f : groundVelocity.magnitude / runSpeed;

		//reference for animator
		Animator.SetFloat(SpeedPercent, animationSpeedPercent, speedSmoothTime, Time.deltaTime);

		foreach(var parameter in Animator.parameters)
		{
			var nameHash = parameter.nameHash;
			if(nameHash == InAir)
			{
				Animator.SetBool(InAir, !isGrounded);
			}
			else if(nameHash == DirectionY)
			{
				var directionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, rb.velocity.y));
				Animator.SetFloat(DirectionY, directionY, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == VelocityX)
			{
				var velocityX = Vector3.Dot(groundVelocity, transform.right) / runSpeed;
				Animator.SetFloat(VelocityX, velocityX, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == VelocityZ)
			{
				var velocityZ = Vector3.Dot(groundVelocity, transform.forward) / runSpeed;
				Animator.SetFloat(VelocityZ, velocityZ, speedSmoothTime, Time.deltaTime);
			}
			else if(nameHash == InHitStun)
			{
				Animator.SetBool(InHitStun, HitReaction.InProgress);
			}
		}
	}
}
