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

	private AttackDataSet _attackDataSet;

	private void Start()
	{
		Actor = GetComponent<Actor>();
		Actor.ConsumeInput += HandleInput;
		Actor.LateTick += ProcessAttackAnimation;
		Actor.GetHit += HandleGetHit;
		Actor.OnDeath += DropWeapon;

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

	public void NewAttack(AnimationEvent animEvent)
	{
		if (!_weapon) return;
		_weapon.NewAttack(animEvent.stringParameter);
		BeginAttack?.Invoke(Actor);
	}
	
	public void NewHit(AnimationEvent animEvent)
	{
		if (!_weapon) return;
		_hasActiveHit = true;
		_weapon.NewHit(WeaponBone, animEvent.stringParameter);
	}

	public void EndHit()
	{
		if (!_weapon) return;
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
		DropWeapon();

		_weapon = Instantiate(weaponPrefab, WeaponBone);
		var meshCollider = _weapon.GetComponentInChildren<MeshCollider>();
		if (meshCollider) meshCollider.enabled = false;
		
		var rb = _weapon.GetComponent<Rigidbody>();
		if (rb) Destroy(rb);
		
		_weapon.transform.localRotation = Quaternion.LookRotation(weaponBoneForward, weaponBoneUp);
		_weapon.RegisterUser(this, out _attackDataSet);
	}

	private void DropWeapon()
	{
		if (!_weapon) return;
		
		if( _isAttacking ) _isAttacking = false;
		if( _hasActiveHit ) EndHit();
		_weapon.UnregisterUser();
		_weapon.transform.parent = null;
		
		var meshCollider = _weapon.GetComponentInChildren<MeshCollider>();
		if (meshCollider) meshCollider.enabled = true;
		
		var weaponRb = _weapon.GetComponent<Rigidbody>();
		if (!weaponRb) weaponRb = _weapon.gameObject.AddComponent<Rigidbody>();
		
		weaponRb.isKinematic = false;
		weaponRb.useGravity = true;
		weaponRb.interpolation = RigidbodyInterpolation.Interpolate;
		
		// Impart velocity from actor
		var actorRb = Actor.GetComponent<Rigidbody>();
		if (actorRb)
		{
			weaponRb.velocity = actorRb.velocity;
		}

		_weapon = null;
		_attackDataSet = null;
	}

	private bool HasRequiredStamina(int actionId, out int staminaCost)
	{
		var attackData = _attackDataSet.GetAttackData("lightAttack");
		staminaCost = attackData.staminaCost;
		return staminaCost <= Actor.Stamina.Current;
	}

	private void HandleInput(InputBuffer inputBuffer)
	{
		if (!_isAttacking &&
		    HasRequiredStamina(PlayerAction.Attack, out var staminaCost) &&
		    inputBuffer.TryConsumeAction(PlayerAction.Attack))
		{
			_isAttacking = true;
			Actor.InputEnabled = false;
			if(Actor.Animator) Actor.Animator.SetTrigger(Attack);
		}
	}
	
	private void ProcessAttackAnimation(float deltaTime)
	{
		if (!_weapon) return;
		if (!_hasActiveHit) return;
		_weapon.CheckHits(WeaponBone, weaponBoneUp, distThreshold);
	}
	
	private void HandleGetHit(CombatEvent combatEvent)
	{
		// TODO: Check if incoming attack should interrupt.
		_isAttacking = false;
	}
}
