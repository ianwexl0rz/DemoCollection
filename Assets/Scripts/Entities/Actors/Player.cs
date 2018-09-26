using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using System.Linq;

public class Player : Actor
{
	public float minSpeed = 1f;
	public float walkSpeed = 2f;
	public float runSpeed = 4f;
	public float jumpHeight = 4f;
	public float gravityMult = 1f;
	public float speedSmoothTime = 0.1f;
	public PIDConfig angleControllerConfig = null;
	public PIDConfig angularVelocityControllerConfig = null;

	public GameObject hitSpark;
	public Transform weaponTransform;

	private PID angleController = null;
	private PID angularVelocityController = null;


	public bool aimingMode = false;
	public bool recenter = false;

	public AttackDataSet attackDataSet = null;
	public GameObject attackBox = null;

	private Collider attackCollider = null;
	private Vector3 currentSpeed;
	private float playerRotation = 0f;

	private bool grounded = false;
	private bool queueJump = false;

	private bool doubleJumpOK = false;

	private Vector3 speedSmoothVelocity;

	public bool run { get; set; }
	public bool jump { get; set; }
	public bool attack { get; set; }
	public bool rootMotionOverride { get; set; }

	private bool attackInProgress = false;
	private bool cancelOK = true;

	private List<AttackTimer> attackQueue = new List<AttackTimer>();

	protected override void Awake()
	{
		base.Awake();
		attackBox.SetActive(false);
		attackCollider = attackBox.GetComponent<Collider>();

		angleController = new PID(angleControllerConfig);
		angularVelocityController = new PID(angularVelocityControllerConfig);
	}

	private void OnDrawGizmosSelected()
	{
		// Draw foot collider
		//Gizmos.DrawSphere(transform.position + Vector3.up * 0.25f + Vector3.down * 0.1f, 0.2f);
	}

	protected override void ProcessPhysics()
	{
		if(attackCoroutine != null)
			attackCoroutine.MoveNext();

		if(stunTime > 0f) { return; }

		if(rootMotionOverride)
		{
			currentSpeed = Vector3.zero;
			UpdateRotation();
			return;
		}

		RaycastHit[] hits = Physics.SphereCastAll(transform.position + Vector3.up * 0.25f, 0.2f, Vector3.down, 0.1f, ~LayerMask.GetMask("Actor"), QueryTriggerInteraction.Ignore);
		Vector3 groundNormal = Vector3.down;

		if(hits.Length > 0)
		{
			RaycastHit groundHit = hits[0];

			for(int i = 1; i < hits.Length; i++)
			{
				if(hits[i].normal.y > groundHit.normal.y)
				{
					groundHit = hits[i];
				}
			}

			groundNormal = groundHit.normal.normalized;
			grounded = true;
		}
		else
		{
			grounded = false;
		}

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

		float yVelocity = rb.velocity.y;

		// Did we queue a jump?
		if(queueJump)
		{
			// jump!
			//yVelocity = -gravity * timeToApex;
			yVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * gravityMult * jumpHeight);
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
				rb.velocity *= 1 - incline * 0.5f;
			}

			// Counteract gravity (so we don't slide on an incline!)
			rb.velocity -= Physics.gravity * Time.fixedDeltaTime;
		}
		else
		{
			rb.velocity = currentSpeed.WithY(yVelocity);
			rb.velocity += Physics.gravity.y * (gravityMult - 1f) * Vector3.up * Time.fixedDeltaTime;
		}

		rb.velocity *= localTimeScale;

		//rb.rotation = playerRotation;

		UpdateRotation();
	}

	protected void UpdateRotation()
	{
		if(lockOn && lockOnTarget != null)
		{
			playerRotation = Vector3.SignedAngle(Vector3.forward, (lockOnTarget.position - transform.position).normalized, Vector3.up);
		}
		else if(currentSpeed.WithY(0f).magnitude >= minSpeed)
		{
			playerRotation = Vector3.SignedAngle(Vector3.forward, currentSpeed.WithY(0f), Vector3.up);
		}

		rb.RotateToAngleYaw(angleController, angularVelocityController, playerRotation);
	}

	protected override void ProcessInput()
	{
		base.ProcessInput();

		if(jump)
		{
			// Queue a jump if the jump was pressed this frame.
			queueJump = true;
		}

		// Interpolate from our current speed to the target speed.
		Vector3 targetSpeed = move * Mathf.Max(minSpeed, (run ? runSpeed : walkSpeed));
		currentSpeed = Vector3.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime * (grounded ? 1f : 8f));

		// Cache look sensitivity from GameSettings
		float lookSensitivityX = ControlSettings.I.lookSensitivityX;
		InputDevice playerInput = InputManager.ActiveDevice;

		/*
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
		*/

		if(attack)
		{
			attackQueue.Add(new AttackTimer(AttackType.Light, 0.5f));
		}

		UpdateAttackBuffer();

		if(weaponTransform)
			Debug.DrawLine(weaponTransform.position, weaponTransform.position + weaponTransform.forward * ((CapsuleCollider)attackCollider).height, Color.red);
	}

	protected override void ProcessAnimation()
	{
		if(animator == null) { return; }

		animator.speed = localTimeScale;

		if(animator.runtimeAnimatorController != null)
		{
			//if(!GameManager.I.IsPaused)
			//control speed percent in animator so that character walks or runs depending on speed
			float animationSpeedPercent = physicsPaused ? 0f : currentSpeed.magnitude / runSpeed;

			//reference for animator
			animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);

			foreach(AnimatorControllerParameter parameter in animator.parameters)
			{
				if(parameter.name == "velocityX")
				{
					float velocityX = Vector3.Dot(currentSpeed, transform.right) / runSpeed;
					animator.SetFloat("velocityX", velocityX, speedSmoothTime, Time.deltaTime);
				}

				if(parameter.name == "velocityZ")
				{
					float velocityZ = Vector3.Dot(currentSpeed, transform.forward) / runSpeed;
					animator.SetFloat("velocityZ", velocityZ, speedSmoothTime, Time.deltaTime);
				}
			}
		}
	}

	protected void UpdateAttackBuffer()
	{
		for(int i = attackQueue.Count - 1; i >= 0; i--)
		{
			AttackTimer actionTimer = attackQueue[i];
			AttackType attackType = actionTimer.attackType;

			if(cancelOK)
			{
				if(animator)
				{
					switch(attackType)
					{
						case AttackType.Light:
							animator.SetTrigger("lightAttack");
							break;
					}
				}
				attackQueue.RemoveAt(i);
			}
			else
			{
				// Decrement time.
				actionTimer.time -= Time.deltaTime;

				if(actionTimer.time <= 0)
				{
					// Valid window has expired!
					attackQueue.RemoveAt(i);
				}
			}
		}
	}

	public void SetCancelOK()
	{
		cancelOK = true;
	}

	public IEnumerator attackCoroutine;

	public IEnumerator Attack(AttackData data)
	{
		attackBox.SetActive(true);
		attackInProgress = true;
		cancelOK = false;

		List<Actor> hitEnemies = new List<Actor>();

		while(attackInProgress)
		{
			RaycastHit[] hits = Physics.RaycastAll(
				weaponTransform.position,
				weaponTransform.forward,
				((CapsuleCollider)attackCollider).height,
				LayerMask.GetMask("Actor"));

			foreach(RaycastHit hit in hits)
			{
				Collider enemyCollider = hit.collider;

				Actor enemy = enemyCollider.GetComponent<Actor>();
				if(enemy == null || enemy == this) { continue; }

				if(!hitEnemies.Contains(enemy))
				{
					if(hitSpark && weaponTransform)
					{
						Instantiate(hitSpark, hit.point, Quaternion.identity, null);
					}

					enemy.GetHit(this, data);
					hitEnemies.Add(enemy);
				}
			}

			yield return null;
		}

		attackBox.SetActive(false);
	}

	public void EndHit()
	{
		attackInProgress = false;
		cancelOK = true;
	}
}
