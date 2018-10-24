using UnityEngine;

public class Player : CombatCharacter
{
	public float minSpeed = 1f;
	public float walkSpeed = 2f;
	public float runSpeed = 4f;
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

	public CapsuleCollider capsuleCollider;
	private Quaternion rollRotation = Quaternion.identity;

	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	private PID3 angleController;
	private PID3 angularVelocityController;

	//[HideInInspector]
	public Vector3 currentSpeed;
	//[HideInInspector]
	public Vector3 speedSmoothVelocity;
	private Vector3 desiredDirection;

	//[HideInInspector]
	public bool grounded;
	//[HideInInspector]
	public bool queueJump;
	//[HideInInspector]
	public int remainingJumps;

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

	public void UpdateRotation()
	{
		if(lockOn && lockOnTarget != null)
		{
			desiredDirection = (lockOnTarget.position - transform.position).WithY(0f).normalized;
		}
		else if(currentSpeed.WithY(0f).magnitude >= minSpeed)
		{
			desiredDirection = currentSpeed.WithY(0f);
		}

		var rollForward = currentSpeed.WithY(0f).magnitude >= minSpeed ? -currentSpeed.WithY(0f) : -desiredDirection;
		var rollAxis = transform.InverseTransformDirection(Vector3.Cross(rollForward, Vector3.up));
		rollRotation = ShouldRoll ? rollRotation * Quaternion.AngleAxis(rollSpeed, rollAxis) : Quaternion.identity;

		var rotation = Quaternion.LookRotation(desiredDirection) * rollRotation;
		rb.RotateTo(angleController, angularVelocityController, rotation, Time.fixedDeltaTime);
	}

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

	public override void OnLateUpdate()
	{
		if(!activeHit || paused) { return; }

		var origin = weaponTransform.position;
		var end = origin + weaponTransform.forward * 1.2f;

		weaponCollision.SetCurrentPosition(origin, end);
		weaponCollision.CheckHits(this, 0.2f);
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
