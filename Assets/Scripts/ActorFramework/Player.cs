using System.Collections.Generic;
using UnityEngine;

public class Player : CombatActor
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
	public float maxAngularVelocity = 12;
	public float leanFactor;
	public float friction;

	public bool Run { get; set; }
	public bool ShouldRoll { get; set; }
	public bool Recenter { get; set; }
	public bool AimingMode { get; set; }
	public bool RootMotionOverride { get; set; }

	public CapsuleCollider capsuleCollider { get; private set; }
	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	private PID3 angleController;
	private PID3 angularVelocityController;
	private Vector3 groundVelocity = Vector3.zero;
	private Vector3 desiredDirection;
	private Vector3 groundPoint;
	private float rollAngle;
	private bool grounded;
	private bool queueJump;
	private bool jumping;
	private int remainingJumps;

	protected override void Awake()
	{
		base.Awake();
		desiredDirection = transform.forward;
		angleController = new PID3(angleControllerConfig);
		angularVelocityController = new PID3(angularVelocityControllerConfig);
		capsuleCollider = GetComponent<CapsuleCollider>();
		rb.maxAngularVelocity = maxAngularVelocity;

		remainingJumps = jumpCount;

		// Timer example!
		//actorTimerGroup.Add(5f, () => Debug.Log("Started timer."), () => Debug.Log("Time's up!"));
	}

	private void OnValidate()
	{
		if(rb != null) rb.maxAngularVelocity = maxAngularVelocity;
	}

	private void OnDrawGizmos()
	{
		if(rb == null) { return; }

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(groundPoint, 0.1f);
	}

	public Vector3 GetFeetPosition()
	{
		return transform.TransformPoint(capsuleCollider.center);
	}

	private bool CheckForGround(out Vector3 groundNormal)
	{
		var raycastOrigin = groundPoint + Vector3.up * groundCheckHeight;
		var forward = Vector3.Cross(transform.right, Vector3.up);

		var numGroundHits = 0;
		var averagePoint = Vector3.zero;
		var averageNormal = Vector3.zero;
		var averageDistance = 0f;

		for(var i = 0; i < extraGroundRays; i++)
		{
			var dir = i > 0 ? Quaternion.Euler(0f, 360f * i / extraGroundRays, 0f) * forward : forward;
			var origin = raycastOrigin + dir * rayOffsetDistance;

			// Red = No Hit, Yellow = Hit, Green = Ground
			Color[] color = { Color.red, Color.yellow, Color.green };
			var status = 0;

			if(Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckHeight + 0.2f, ~LayerMask.GetMask("Actor", "ProxyObject")))
			{
				status = 1;
				// This would prevent walking up ramps that are too steep
				//if(Vector3.Angle(hit.normal, Vector3.up) <= 45f)
				{
					averageNormal += hit.normal;
					averagePoint += hit.point;
					averageDistance += hit.distance;
					numGroundHits++;
					status = 2;
				}
			}

			Debug.DrawLine(origin, origin + Vector3.down * groundCheckHeight, color[status]);
		}

		if(numGroundHits > 0)
		{
			groundNormal = averageNormal / numGroundHits;

			// TODO: Sliding state for steep inclines?
			if(Vector3.Angle(groundNormal, Vector3.up) <= 45f)
			{
				averagePoint /= numGroundHits;
				averageDistance /= numGroundHits;

				if(grounded || averageDistance < ((-rb.velocity.y - Physics.gravity.y * gravityScale) * Time.fixedDeltaTime))
				{
					var invGroundPoint = rb.position - groundPoint;
					rb.MovePosition(rb.position.WithY(averagePoint.y + invGroundPoint.y));

					var cross = Vector3.Cross(groundVelocity.normalized, Vector3.up);
					var finalVelocity = Vector3.Cross(groundNormal, cross) * groundVelocity.magnitude;

					var point = groundPoint + Vector3.up * leanFactor * groundVelocity.magnitude / runSpeed;
					rb.AddForceAtPosition(finalVelocity - rb.velocity, point, ForceMode.VelocityChange);

					Debug.DrawLine(groundPoint, groundPoint + groundNormal * (groundCheckHeight + capsuleCollider.height), Color.blue);

					remainingJumps = jumpCount;
					return true;
				}
			}
		}

		// Reset jump allowance if we were grounded last tick
		if(grounded && !jumping)
		{
			jumpAllowance.Reset();
			jumpAllowance.SetDuration(Time.fixedDeltaTime * 4);
		}

		// Ran out of coyote time and didn't jump
		if(!jumpAllowance.InProgress && remainingJumps == jumpCount)
		{
			remainingJumps = 0;
		}

		groundNormal = Vector3.zero;
		return false;
	}


	protected override void ProcessPhysics()
	{
		var dt = Time.fixedDeltaTime;

		if(stunned.InProgress) return;

		if(RootMotionOverride)
		{
			groundVelocity = Vector3.zero;
			UpdateRotation();
			return;
		}


		// TODO: Kill velocity when you run into a wall - grounded or not!
		if(!grounded)
		{
			groundVelocity = rb.velocity.WithY(0f);
		}

		var speedLimit = Run ? runSpeed : walkSpeed;
		var desiredVel = groundVelocity + move.normalized * (grounded ? acceleration : acceleration * 0.25f) * dt;
		var speed = desiredVel.magnitude;

		if(grounded)
		{
			//TODO: Variable speed based on analog input
			speed = Mathf.Max(desiredVel.magnitude - friction * dt, 0f);
			
		}
		speed = Mathf.Min(speed, speedLimit);

		groundVelocity = (desiredVel.normalized * speed).WithY(0f);

		Vector3 groundNormal = Vector3.up;
		var newJump = queueJump && remainingJumps > 0;

		UpdateGroundpoint();

		if(newJump)
		{
			queueJump = false;
			grounded = false;
			jumping = true;
			remainingJumps--;

			var jumpVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityScale * jumpHeight);
			rb.velocity = groundVelocity.WithY(jumpVelocity);
		}
		else
		{
			grounded = jumping ? false : CheckForGround(out groundNormal);

			if(!grounded)
			{
				rb.velocity = groundVelocity.WithY(rb.velocity.y);
				rb.velocity += Physics.gravity * gravityScale * Time.fixedDeltaTime;
			}

			if(jumping && rb.velocity.y <= 0f)
			{
				jumping = false;
			}
		}

		rb.centerOfMass = transform.InverseTransformPoint(groundPoint);

		UpdateRotation();
	}

	private void UpdateGroundpoint()
	{
		var halfHeight = (capsuleCollider.height + groundCheckHeight) * 0.5f;
		var radius = capsuleCollider.radius;

		var angle = Vector3.Angle(transform.up, Vector3.up);
		if(angle > 90f) angle -= 180f;

		groundPoint = transform.position + transform.up * halfHeight + rb.velocity * Time.fixedDeltaTime;
		groundPoint += Vector3.down * (Mathf.Cos(angle * Mathf.Deg2Rad) * (halfHeight - radius) + radius);
	}

	protected void UpdateRotation()
	{
		if(lockOn && lockOnTarget != null)
		{
			desiredDirection = (lockOnTarget.position - transform.position).WithY(0f).normalized;
		}
		else if(grounded && move != Vector3.zero)
		{
			desiredDirection = move;
		}
		//else if(!grounded && groundVelocity.magnitude >= minSpeed)
		//{
		//	desiredDirection = groundVelocity.normalized;
		//}

		rollAngle = ShouldRoll ? (rollAngle + rollSpeed) % 360f : 0f;

		var rotation = ShouldRoll ? GetRotationWithRoll() : Quaternion.LookRotation(desiredDirection);

		rb.RotateTo(angleController, angularVelocityController, rotation, Time.fixedDeltaTime);

		Quaternion GetRotationWithRoll()
		{
			var rollDir = lockOn && lockOnTarget != null && groundVelocity.magnitude >= minSpeed
				? Quaternion.Inverse(Quaternion.LookRotation(desiredDirection)) * groundVelocity.normalized
				: Vector3.forward;

			var rollRotation = Quaternion.AngleAxis(rollAngle, Vector3.Cross(rollDir, Vector3.down));
			return Quaternion.LookRotation(desiredDirection) * rollRotation;
		}
	}

	/*
	protected override void ProcessInput()
	{
		base.ProcessInput();
	}
	*/

	public Transform mesh = null;

	private Vector3 lastPos1;
	private Vector3 lastPos2;

	protected override void ProcessAnimation()
	{
		if(animator == null || animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = paused ? 0f : groundVelocity.magnitude / runSpeed;

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
					var velocityX = Vector3.Dot(groundVelocity, transform.right) / runSpeed;
					animator.SetFloat("velocityX", velocityX, speedSmoothTime, Time.deltaTime);
					break;
				case "velocityZ":
					var velocityZ = Vector3.Dot(groundVelocity, transform.forward) / runSpeed;
					animator.SetFloat("velocityZ", velocityZ, speedSmoothTime, Time.deltaTime);
					break;
			}
		}
	}

	public bool Jump()
	{
		if(remainingJumps > 0)
		{
			queueJump = true;
			return true;
		}

		return false;
	}

	public bool LightAttack()
	{
		if(isAttacking && !cancelOK) { return false; }

		if(animator != null) { animator.SetTrigger("lightAttack"); }
		return true;
	}
}
