using System;
using System.Collections.Generic;
using ActorFramework;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor : Actor
{
	public struct AnimatedProperties
	{
		public float MoveSpeedNormalized;
		public bool IsGrounded;
		public float DirectionY;
		public float VelocityX;
		public float VelocityZ;
		public bool IsInHitStun;
	}
	
	// TODO: Separate component from Actor

	private const float MaxAnimationStep = 1f / 30f;
	private const float MaxAngularVelocity = 50f;
	public event Action<AnimatedProperties> OnAnimatedPropertiesChanged;

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

	private bool _isGrounded;
	private Vector3 _torqueIntegral;
	private Vector3 _torqueError;
	private Vector3 _planarVelocityIntegral;
	private Vector3 _planarVelocityError;
	private Vector3 _groundVelocity = Vector3.zero;
	private Vector3 _lookDirection;
	private Vector3 _groundNormal;
	private Vector3 _groundPoint;
	private Vector3 _groundCheckPoint;
	private float _rollAngle;
	private bool _queueLockOn;
	private bool _queueRoll;
	private bool _queueJump;
	private bool _jumping;
	private int _remainingJumps;
	private Matrix4x4 _lastTRS;
	private Quaternion _desiredRotation;

	public Matrix4x4 LastTRS => _lastTRS;

	public bool Run { get; set; }
	
	public CapsuleCollider CapsuleCollider { get; private set; }

	public override void Awake()
	{
		base.Awake();
		CapsuleCollider = GetComponent<CapsuleCollider>();

		//inputOrientation = Quaternion.LookRotation(transform.forward);
		_remainingJumps = jumpCount;
		rb.maxAngularVelocity = MaxAngularVelocity;

		var t = transform;
		_lastTRS = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
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

	protected override void UpdatePhysics(float deltaTime)
	{
		Vector3 desiredVelocity;

		// Stop angular velocity if animation has root motion.
		if (Animator.applyRootMotion)
		{
			rb.angularVelocity = Vector3.zero;
			return;
		}

		if (_isGrounded)
		{
			// Invert ground normal rotation to get unbiased ground velocity
			var groundRotation = Quaternion.LookRotation(Vector3.forward, _groundNormal);
			desiredVelocity = Quaternion.Inverse(groundRotation) * rb.velocity;
		}
		else
		{
			desiredVelocity = rb.velocity.WithY(0f);
		}

		if (_queueLockOn)
		{
			_queueLockOn = false;

			if (TrackedTarget != null)
			{
				// If we were locked on, break lock...
				TrackedTarget = null;
			}
			else
			{
				// If we were not locked on, try to assign target...
				var candidate = LockOn.TrackableCandidate;
				if (candidate != null) TrackedTarget = candidate;
				else
				{
					// Recenter the camera if there is no viable target.
					var cross = Vector3.Cross(transform.right, Vector3.up);
					var lookRotation = Quaternion.LookRotation(cross);
					GameManager.Camera.SetTargetEulerAngles(lookRotation.eulerAngles);
				}
			}
		}

		if (InputEnabled)
		{
			desiredVelocity += (_isGrounded ? acceleration : airAcceleration) * deltaTime * Move;
		}

		var speed = desiredVelocity.magnitude;
		
		// Apply friction
		if (_isGrounded && _rollAngle < Mathf.Epsilon)
		{
			speed = Mathf.Max(desiredVelocity.magnitude - friction * deltaTime, 0f);
		}

		if (_isGrounded)
		{
			// Speed is only variable when NOT sprinting.
			//var normalSpeed = Mathf.Max(minSpeed, walkSpeed * move.sqrMagnitude);
			var shouldRun = Run && _rollAngle < Mathf.Epsilon;
			var targetSpeed = shouldRun ? runSpeed : walkSpeed;

			// Slower while walking backwards...
			//var dot = Vector3.Dot(desiredVelocity, transform.forward);
			//targetSpeed = Mathf.Lerp(targetSpeed, targetSpeed * 0.5f, -dot);
			speed = Mathf.Min(speed, targetSpeed);
		}

		_groundVelocity = (desiredVelocity.normalized * speed).WithY(0f);
		
		// var shouldRun = Run && rollAngle < Mathf.Epsilon;
		// var targetSpeed = shouldRun ? runSpeed : walkSpeed;
		// var targetVelocity = (move * targetSpeed).WithY(rb.velocity.y);
		// var acc = planarVelocityPID.Output(rb.velocity, targetVelocity, ref planarVelocityIntegral,
		// 	ref planarVelocityError, deltaTime);
		// rb.AddForce(acc, ForceMode.Acceleration);
		// groundVelocity = rb.velocity.WithY(0);
		
		// Jump.
		if (_remainingJumps > 0 && TryConsumeAction(PlayerAction.Jump))
		{
			_jumping = true;
			_remainingJumps--;

			var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
			rb.velocity = _groundVelocity.WithY(jumpVelocity);
		}
		

		// Get the point directly below the center of the capsule, using its current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		_groundCheckPoint = hitInfo.point;

		// Update grounded status.
		var wasGrounded = _isGrounded;
		_isGrounded = !_jumping && CheckForGround();

		if (_isGrounded)
		{
			// Reset remaining jumps if we just landed.
			if (!wasGrounded) _remainingJumps = jumpCount;

			// Make ground velocity perpendicular to ground normal.
			var finalVelocity = Quaternion.LookRotation(Vector3.forward, _groundNormal) * _groundVelocity;
			
			// Set center of mass to the point on the capsule directly below the center.
			rb.centerOfMass = transform.InverseTransformPoint(_groundCheckPoint);

			// Apply force slightly above the center of mass to make the actor lean with acceleration.
			var offset = rb.worldCenterOfMass + Vector3.up * (leanFactor * _groundVelocity.magnitude / runSpeed);
			var deltaVelocity = finalVelocity - rb.velocity;
			rb.AddForceAtPosition(deltaVelocity, offset, ForceMode.VelocityChange);
			
			// Snap to ground.
			var groundOffset = rb.position - _groundCheckPoint;
			rb.MovePosition(rb.position.WithY(_groundPoint.y + groundOffset.y));
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
				if (_remainingJumps == jumpCount) _remainingJumps = 0;

				// HACK: We jumped and it's OK to check for the ground again
				_jumping = false;
			}

			// We passed the peak of the jump
			if (_jumping && rb.velocity.y <= 0f) _jumping = false;

			// Set new velocity.
			rb.velocity = _groundVelocity.WithY(rb.velocity.y) + Physics.gravity * (gravityScale * Time.fixedDeltaTime);
		}

		// Roll.
		if (_rollAngle > 0f && _rollAngle < 360f || TryConsumeAction(PlayerAction.Roll))
		{
			_rollAngle += rollSpeed * Time.fixedDeltaTime;
			if (_rollAngle >= 360f) _rollAngle = 0f;
		}

		// Handle rotation.
		var targetRotation = GetTargetRotation();
		var targetTorque = rb.rotation.TorqueTo(targetRotation, deltaTime);
		var torque = torquePID.Output(rb.angularVelocity, targetTorque, ref _torqueIntegral, ref _torqueError, deltaTime);
		rb.AddTorque(torque, ForceMode.Acceleration);

		if (InputEnabled) HandleAbilityInput();
	}

	private Quaternion GetTargetRotation()
	{
		var maxTurnRate = _isGrounded && _rollAngle < Mathf.Epsilon ? maxTurnGround : maxTurnAir;
		var rollAxis = Vector3.right;

		var validLookInput = _isGrounded && Move.normalized != Vector3.zero && InputEnabled && !HitReaction.InProgress;
		if (validLookInput) _lookDirection = Move.normalized;

		if (TrackedTarget != null)
		{
			var toLockOnTarget = (TrackedTarget.GetEyesPosition() - GetEyesPosition()).WithY(0f).normalized;
			var lockOnOrientation = Quaternion.LookRotation(toLockOnTarget);
			_desiredRotation = Quaternion.RotateTowards(_desiredRotation, lockOnOrientation, maxTurnRate);

			if (_rollAngle > 0 && _groundVelocity.magnitude >= minSpeed)
				rollAxis = Vector3.Cross(Quaternion.Inverse(lockOnOrientation) * _lookDirection, Vector3.down);
		}
		else if (validLookInput)
		{
			_desiredRotation = Quaternion.RotateTowards(_desiredRotation, Quaternion.LookRotation(_lookDirection.normalized), maxTurnRate);
		}

		return _rollAngle > 0 ? _desiredRotation * Quaternion.AngleAxis(_rollAngle, rollAxis) : _desiredRotation;
	}

	protected override void UpdateAnimation(float deltaTime)
	{
		var input = new AnimatedProperties()
		{
			MoveSpeedNormalized = IsPaused ? 0f : _groundVelocity.magnitude / runSpeed,
			IsGrounded = _isGrounded,
			DirectionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, rb.velocity.y)),
			VelocityX = Vector3.Dot(_groundVelocity, transform.right) / runSpeed,
			VelocityZ = Vector3.Dot(_groundVelocity, transform.forward) / runSpeed,
			IsInHitStun = HitReaction.InProgress
		};
		
		OnAnimatedPropertiesChanged?.Invoke(input);

		var loops = Mathf.CeilToInt(deltaTime / MaxAnimationStep);
		var dt = deltaTime / loops;

		for (var i = 0; i < loops; i++)
		{
			Animator.Update(dt);
			PartialTickAnimationListeners((i + 1f) / loops);
		}
		
		// Set the center of mass to the point on the collider directly below the center, using it's current rotation.
		var length = hitBoxCollider.height * 0.5f + groundCheckHeight;
		var ray = new Ray(transform.TransformPoint(hitBoxCollider.center) + Vector3.down * length, Vector3.up);
		hitBoxCollider.Raycast(ray, out var hitInfo, length);
		_groundCheckPoint = hitInfo.point;

		_lastTRS = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
	}

	private bool CheckForGround()
	{
		var raycastOrigin = _groundCheckPoint + Vector3.up * groundCheckHeight;
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

		if (!_isGrounded)
		{
			// Become grounded ONLY if the distance to ground is less than projected fall distance
			var projectedFallDistance = (-rb.velocity.y - Physics.gravity.y * gravityScale) * Time.fixedDeltaTime;
			if (averageDistance > projectedFallDistance) { return false; }
		}

		_groundNormal = averageNormal;
		_groundPoint = averagePoint;
		return true;
	}

	public override Vector3 GetEyesPosition() => transform.TransformPoint(CapsuleCollider.center);

	public override Vector3 GetGroundPosition() => _groundCheckPoint;

	public void QueueLockOn() => _queueLockOn = true;
}
