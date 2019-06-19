using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterMotor), typeof(MeleeCombat))]
public class Character : Actor
{
	private const float MAX_ANIMATION_STEP = 1f / 30f;

	public CharacterMotor motor { get; private set; }
	public CapsuleCollider capsuleCollider { get; private set; }
	public MeleeCombat meleeCombat { get; private set; }

	private Matrix4x4 lastTRS;

	protected override void Awake()
	{
		base.Awake();
		motor = GetComponent<CharacterMotor>();
		meleeCombat = GetComponent<MeleeCombat>();
		capsuleCollider = GetComponent<CapsuleCollider>();

		motor.Init(this);

		lastTRS = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		OnLateUpdate += ProcessAnimation;
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		OnLateUpdate -= ProcessAnimation;
	}

	protected override void ProcessPhysics()
	{
		motor.UpdateMotor();
	}

	protected override void ProcessAnimation()
	{
		UpdateAnimationParameters();

		var interval = Time.deltaTime;
		var loops = Mathf.CeilToInt(interval / MAX_ANIMATION_STEP);
		var dt = interval / loops;

		for(var i = 0;  i < loops; i++)
		{
			animator.Update(dt);

			// Calculate the position and rotation the weapon WOULD have if the character did not move/rotate this frame.
			// This allows us to blend to the ACTUAL position/rotation over multiple steps.
			var lastWeaponPos = lastTRS.MultiplyPoint3x4(transform.InverseTransformPoint(meleeCombat.WeaponRoot.position));
			var lastWeaponRot = lastTRS.rotation * Quaternion.Inverse(transform.rotation) * meleeCombat.WeaponRoot.rotation;

			if(meleeCombat.ActiveHit)
				meleeCombat.CheckHits((i + 1f) / loops, lastWeaponPos, lastWeaponRot);
		}

		lastTRS = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
	}

	public override Vector3 GetLockOnPosition()
	{
		return transform.TransformPoint(capsuleCollider.center);
		//return motor.FeetPos;
	}

	public bool Jump()
	{
		return motor.TryJump();
	}

	public bool Roll()
	{
		return motor.TryRoll();
	}

	public bool LightAttack()
	{
		if(meleeCombat.isAttacking || !InputEnabled) { return false; }

		meleeCombat.isAttacking = true;
		InputEnabled = false;
		//meleeCombat.cancelOK = false;

		// TODO: Should maybe set attack ID and generic attack trigger?
		if(animator != null) { animator.SetTrigger("lightAttack"); }
		return true;
	}

	public void UpdateAnimationParameters()
	{
		if(animator == null || animator.runtimeAnimatorController == null) { return; }

		//control speed percent in animator so that character walks or runs depending on speed
		var animationSpeedPercent = IsPaused ? 0f : motor.GroundVelocity.magnitude / motor.runSpeed;

		//reference for animator
		animator.SetFloat("speedPercent", animationSpeedPercent, motor.speedSmoothTime, Time.deltaTime);

		foreach(var parameter in animator.parameters)
		{
			switch(parameter.name)
			{
				case "inAir":
					animator.SetBool("inAir", !motor.IsGrounded);
					break;
				case "directionY":
					var directionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, rb.velocity.y));
					animator.SetFloat("directionY", directionY, motor.speedSmoothTime, Time.deltaTime);
					break;
				case "velocityX":
					var velocityX = Vector3.Dot(motor.GroundVelocity, transform.right) / motor.runSpeed;
					animator.SetFloat("velocityX", velocityX, motor.speedSmoothTime, Time.deltaTime);
					break;
				case "velocityZ":
					var velocityZ = Vector3.Dot(motor.GroundVelocity, transform.forward) / motor.runSpeed;
					animator.SetFloat("velocityZ", velocityZ, motor.speedSmoothTime, Time.deltaTime);
					break;
				case "InHitStun":
					animator.SetBool("InHitStun", hitReaction.InProgress);
					break;
			}
		}
	}
}
