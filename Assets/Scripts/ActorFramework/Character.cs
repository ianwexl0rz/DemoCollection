using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterMotor), typeof(MeleeCombat))]
public class Character : Actor
{
	public CharacterMotor motor { get; private set; }
	public CapsuleCollider capsuleCollider { get; private set; }
	public MeleeCombat meleeCombat { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		motor = GetComponent<CharacterMotor>();
		meleeCombat = GetComponent<MeleeCombat>();
		capsuleCollider = GetComponent<CapsuleCollider>();

		motor.Init(this);
	}

	protected override void ProcessPhysics()
	{
		motor.UpdateMotor();
	}

	protected override void ProcessAnimation()
	{
		motor.UpdateAnimation();
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

		if(animator != null) { animator.SetTrigger("lightAttack"); }
		return true;
	}
}
