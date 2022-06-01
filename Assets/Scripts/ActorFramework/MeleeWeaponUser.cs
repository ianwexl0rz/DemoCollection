using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Serialization;

[RequireComponent(typeof(Actor))]
public class MeleeWeaponUser : MonoBehaviour
{
	private static readonly int Attack = Animator.StringToHash("lightAttack");

	private event Action<Actor> BeginAttack;
	private event Action<CombatEvent> HitSomething;

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
		Actor.ConsumeInput += HandleInput;
		Actor.LateTick += ProcessAttackAnimation;
		Actor.GetHit += HandleGetHit;

		if (defaultWeaponPrefab != null)
			EquipWeapon(defaultWeaponPrefab);
	}

	public void RegisterPlayerCallbacks(PlayerController playerController)
	{
		BeginAttack += playerController.SetOrientationOnAttack;
		HitSomething += playerController.AddTargetToRecentlyHitList;
	}
	
	public void UnregisterPlayerCallbacks(PlayerController playerController)
	{
		BeginAttack -= playerController.SetOrientationOnAttack;
		HitSomething -= playerController.AddTargetToRecentlyHitList;
	}

	public void NewAttack()
	{
		BeginAttack?.Invoke(Actor);
	}
	
	public void NewHit(AnimationEvent animEvent)
	{
		_hasActiveHit = true;
		_weapon.NewHit(WeaponBone, animEvent.stringParameter);
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

	public void OnHitSomething(CombatEvent combatEvent)
	{
		HitSomething?.Invoke(combatEvent);
		MainMode.AddCombatEvent(combatEvent);
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

	private void HandleInput(InputBuffer inputBuffer)
	{
		if (!_isAttacking && inputBuffer.TryConsumeAction(PlayerAction.Attack))
		{
			_isAttacking = true;
			Actor.InputEnabled = false;
			if(Actor.Animator) Actor.Animator.SetTrigger(Attack);
		}
	}
	
	private void ProcessAttackAnimation(float deltaTime)
	{
		if (!_hasActiveHit) return;
		_weapon.CheckHits(WeaponBone, weaponBoneUp, distThreshold);
	}
	
	private void HandleGetHit(CombatEvent combatEvent)
	{
		// TODO: Check if incoming attack should interrupt.
		_isAttacking = false;
	}
}
