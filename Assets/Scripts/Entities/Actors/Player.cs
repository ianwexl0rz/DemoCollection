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
	public float turnSmoothTime = 0.2f; //time it takets from angle to go from current value to target value

	public bool aimingMode = false;
	public bool recenter = false;

	public AttackDataSet attackDataSet = null;
	public GameObject attackBox = null;

	private Collider attackCollider = null;
	private Vector3 currentSpeed;

	private bool grounded = false;
	private bool queueJump = false;

	private bool doubleJumpOK = false;

	private float turnSmoothVelocity; //ref
	private Vector3 speedSmoothVelocity;

	public bool run { get; set; }
	public bool jump { get; set; }
	public bool attack { get; set; }

	private bool attackInProgress = false;
	private bool cancelOK = true;

	private List<AttackTimer> attackQueue = new List<AttackTimer>();

	protected override void Awake()
	{
		base.Awake();
		attackBox.SetActive(false);
		attackCollider = attackBox.GetComponent<Collider>();
	}

	private void OnDrawGizmosSelected()
	{
		// Draw foot collider
		Gizmos.DrawSphere(transform.position + Vector3.up * 0.25f + Vector3.down * 0.1f, 0.2f);
	}

	protected override void ProcessPhysics()
	{
		if(stunTime > 0f) { return; }

		RaycastHit[] hits = Physics.SphereCastAll(transform.position + Vector3.up * 0.25f, 0.2f, Vector3.down, 0.1f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore);
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
			rb.velocity = new Vector3(currentSpeed.x, yVelocity, currentSpeed.z);
			rb.velocity += Physics.gravity.y * (gravityMult - 1f) * Vector3.up * Time.fixedDeltaTime;
		}
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

		if(attack)
		{
			attackQueue.Add(new AttackTimer(AttackType.Light, 0.5f));
		}

		UpdateAttackBuffer();
	}

	protected override void ProcessAnimation()
	{
		if(animator == null) { return; }

		animator.speed = localTimeScale;

		if(animator.runtimeAnimatorController != null)
		{
			//if(!GameManager.I.IsPaused)
			//control speed percent in animator so that character walks or runs depending on speed
			float animationSpeedPercent = paused ? 0f : currentSpeed.magnitude / runSpeed;

			//reference for animator
			animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
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

	public IEnumerator Attack(AttackData data)
	{
		attackBox.SetActive(true);
		attackInProgress = true;
		cancelOK = false;

		List<Actor> hitEnemies = new List<Actor>();

		while(attackInProgress)
		{
			Collider[] enemies = Physics.OverlapBox(attackBox.transform.position,
				attackCollider.bounds.extents, attackBox.transform.rotation,
				LayerMask.GetMask("Enemy", "Player"));

			foreach(Collider enemyCollider in enemies)
			{
				Actor enemy = enemyCollider.GetComponent<Actor>();
				if(enemy == null || enemy == this) { continue; }

				if(!hitEnemies.Contains(enemy))
				{
					enemy.GetHit(transform.position, data);
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
	}
}
