using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Serialization;

[RequireComponent(typeof(Actor))]
public class MeleeWeaponUser : MonoBehaviour
{
	private static readonly int Attack = Animator.StringToHash("lightAttack");

	[FormerlySerializedAs("weaponBone")] public Transform WeaponBone = null;
	[SerializeField] private Vector3 weaponBoneUp = Vector3.up;
	[SerializeField] private Vector3 weaponBoneForward = Vector3.forward;
	[FormerlySerializedAs("weaponPrefab")] [SerializeField] private MeleeWeapon defaultWeaponPrefab = null;
	[SerializeField] private float distThreshold = 0.1f;

	public Actor Actor { get; private set; }

	private MeleeWeapon _weapon;
	private bool _isAttacking;
	private bool _hasActiveHit;

	private void Awake()
	{
		Actor = GetComponent<Actor>();
		Actor.OnHandleAbilityInput += HandleInput;
		Actor.PostUpdateAnimation += ProcessAttackAnimation;
		Actor.OnGetHit += HandleGetHit;

		if (defaultWeaponPrefab != null)
			EquipWeapon(defaultWeaponPrefab);
	}

	private void EquipWeapon(MeleeWeapon weaponPrefab)
	{
		for (var i = WeaponBone.childCount; i-- > 0;)
		{
			DestroyImmediate(WeaponBone.GetChild(i).gameObject);
		}

		_weapon = Instantiate(weaponPrefab, WeaponBone);
		_weapon.transform.localRotation = Quaternion.LookRotation(weaponBoneForward, weaponBoneUp);
		_weapon.RegisterUser(this);
	}

	private void HandleInput()
	{
		if (!_isAttacking && Actor.TryConsumeAction(PlayerAction.Attack))
		{
			_isAttacking = true;
			Actor.InputEnabled = false;
			if(Actor.Animator) Actor.Animator.SetTrigger(Attack);
		}
	}

	private void HandleGetHit()
	{
		_isAttacking = false;
	}

	public void NewHit(AnimationEvent animEvent)
	{
		_hasActiveHit = true;
		_weapon.NewHit(WeaponBone, animEvent.stringParameter);
	}
	
	private void ProcessAttackAnimation()
	{
		if (!_hasActiveHit) return;
		_weapon.CheckHits(WeaponBone, weaponBoneUp, distThreshold);
	}

	public void EndHit()
	{
		_hasActiveHit = false;
		_weapon.EndHit();
	}

	public void CancelOK()
	{
		 _isAttacking = false;
		 Actor.InputEnabled = true;
	}
}
