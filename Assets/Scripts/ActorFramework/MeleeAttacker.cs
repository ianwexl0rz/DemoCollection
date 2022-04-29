using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Serialization;

[RequireComponent(typeof(Actor))]
public class MeleeAttacker : MonoBehaviour
{
	private static readonly int Attack = Animator.StringToHash("lightAttack");

	[SerializeField] private Transform weaponBone = null;
	[SerializeField] private Vector3 weaponBoneUp = Vector3.up;
	[SerializeField] private Vector3 weaponBoneForward = Vector3.forward;
	[SerializeField] private MeleeWeapon weaponPrefab = null;
	[SerializeField] private float distThreshold = 0.1f;

	public bool IsAttacking { get; set; }
	public bool IsCancelOK { get; set; }
	public bool HasActiveHit { get; set; }

	private AttackData _attackData;
	private List<GameObject> _hitObjects;
	private Vector3 _hiltOrigin;
	private Vector3 _tipOrigin;
	private Vector3 _hiltDestination;
	private Vector3 _tipDestination;
	private Actor _actor = null;
	private CharacterMotor _motor;
	private MeleeWeapon _weapon;

	private void Awake()
	{
		_actor = GetComponent<Actor>();
		_actor.OnHandleAbilityInput += HandleInput;
		_actor.PostUpdateAnimation += ProcessAttackAnimation;
		_actor.OnGetHit += HandleGetHit;

		_motor = GetComponent<CharacterMotor>();

		if (weaponPrefab != null)
		{
			for(var i = weaponBone.childCount; i-- > 0;)
			{
				DestroyImmediate(weaponBone.GetChild(i).gameObject);
			}

			_weapon = Instantiate(weaponPrefab, weaponBone);
			_weapon.transform.localRotation = Quaternion.LookRotation(weaponBoneForward, weaponBoneUp);
		}
	}

	private void HandleInput()
	{
		if (!IsAttacking && _actor.TryConsumeAction(PlayerAction.Attack))
		{
			IsAttacking = true;
			_actor.InputEnabled = false;

			// TODO: Should maybe set attack ID and generic attack trigger?
			if(_actor.Animator != null) { _actor.Animator.SetTrigger(Attack); }
		}
	}

	private void ProcessAttackAnimation()
	{
		if (!HasActiveHit) return;
		
		// Calculate the position and rotation the weapon WOULD have if the character did not move/rotate this frame.
		// This allows us to blend to the ACTUAL position/rotation over multiple steps.
		var lastWeaponPos = _actor.LastTRS.MultiplyPoint3x4(transform.InverseTransformPoint(weaponBone.position));
		var lastWeaponRot = _actor.LastTRS.rotation * Quaternion.Inverse(transform.rotation) * weaponBone.rotation;
			
		if (CheckHits(1, lastWeaponPos, lastWeaponRot, out var collisionPoints, out var combatEvents))
		{
			//TODO: If we hit more than one thing, trigger hits over sequential frames?
			MainMode.AddCombatEvents(combatEvents);
		}

		_weapon.ProcessHit(transform, collisionPoints);
	}

	private void HandleGetHit()
	{
		IsAttacking = false;
		IsCancelOK = true;
	}

	public void NewHit(AnimationEvent animEvent)
	{
		// TODO: This should be a dictionary lookup instead of a find...
		
		_attackData = _weapon.attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);
		_hitObjects = new List<GameObject>();

		_hiltOrigin = weaponBone.position;
		_tipOrigin = _hiltOrigin + weaponBone.rotation * weaponBoneUp * _weapon.length;

		_weapon.NewHit();
		HasActiveHit = true;
	}

	public void EndHit()
	{
		_weapon.EndHit();
		HasActiveHit = false;
	}

	public void CancelOK()
	{
		 IsAttacking = false;
		 _actor.InputEnabled = true;
	}

	// TODO: CombatEvents should be passed in as an array and return the number of hits, so the function is non-allocating.
	public bool CheckHits(float completion, Vector3 lastWeaponPosition, Quaternion lastWeaponRotation, out Vector3[] points, out List<CombatEvent> combatEvents)
	{
		combatEvents = new List<CombatEvent>();

		var debugTime = Time.fixedDeltaTime * 8;
		var pos = Vector3.Lerp(lastWeaponPosition, weaponBone.position, completion);
		var rot = Quaternion.Slerp(lastWeaponRotation, weaponBone.rotation, completion);

		_hiltDestination = pos;
		_tipDestination = _hiltDestination + rot * weaponBoneUp * _weapon.length;

		var currentVector = _tipDestination - _hiltDestination;
        var lastVector = _tipOrigin - _hiltOrigin;
        var steps = 1 + (int)((currentVector - lastVector).magnitude / distThreshold);
        var currentStep = 0;
        var hitSomething = false;
        
        points = new Vector3[steps * 2];

#if UNITY_EDITOR
		var debugHue = ((float)steps).LinearRemap(1f, 5f, 0.5f, 0f);
		var debugColor = Color.HSVToRGB(Mathf.Clamp01(debugHue), 1, 1);
#endif
		
		for (currentStep = 0; currentStep < steps; currentStep++)
        {
			var t = (currentStep + 1f) / steps;
			
			var hiltPrevious = currentStep == 0 ? _hiltOrigin : points[currentStep * 2 - 2];
			var tipPrevious = currentStep == 0 ? _tipOrigin : points[currentStep * 2 - 1];
			
			var hiltCurrent = Vector3.Lerp(_hiltOrigin, _hiltDestination, t);
            var tipCurrent = hiltCurrent + Vector3.Slerp(lastVector, currentVector, t);

            var hiltDirection = (hiltCurrent - hiltPrevious).normalized;
            var tipDirection = (tipCurrent - tipPrevious).normalized;

            points[currentStep * 2] = hiltCurrent;
			points[currentStep * 2 + 1] = tipCurrent;

			hitSomething |= CheckHit(hiltCurrent, tipCurrent, hiltDirection, tipDirection, ref combatEvents);
			hitSomething |= CheckHit(tipCurrent, hiltCurrent, tipDirection, hiltDirection, ref combatEvents);
			
#if UNITY_EDITOR
			Debug.DrawLine(hiltCurrent, tipCurrent, debugColor, debugTime);
			Debug.DrawLine(hiltPrevious, hiltCurrent, debugColor, debugTime);
			Debug.DrawLine(tipPrevious, tipCurrent, debugColor, debugTime);
#endif
		}

		// Only necessary if the hit check loop can be broken out of early...
		// if (currentStep < steps - 1)
		// {
		// 	var newLength = (currentStep + 1) * 2;
		// 	Array.Resize(ref points, newLength);
		// }
		
		_hiltOrigin = _hiltDestination;
		_tipOrigin = _tipDestination;

		return hitSomething;
	}

	private bool CheckHit(Vector3 origin, Vector3 end, Vector3 directionAtOrigin, Vector3 directionAtEnd, ref List<CombatEvent> newCombatEvents)
	{
		var hits = Physics.RaycastAll(
			origin,
			(end - origin).normalized,
			(end - origin).magnitude,
			LayerMask.GetMask("Actor", "PhysicsObject"));

		var success = false;

		foreach(var hit in hits)
		{
			var go = hit.collider.gameObject;

			// Hit self
			if(go.transform.root == transform) { continue; }

			var entity = go.GetComponentInChildren<Entity>() ?? go.GetComponentInParent<Entity>();

			// Get GO of the entity because we may have hit a child GO collider
			if(entity != null) go = entity.gameObject;

			if(_hitObjects.Contains(go)) { continue; }
			
			Vector3.Dot(hit.point - origin, end - origin);

			if(entity != null)
			{
				var t = Vector3.Dot(hit.point - origin, end - origin);
				var hitDirection = Vector3.Slerp(directionAtEnd, directionAtEnd, t);
				var combatEvent = new CombatEvent(_actor, entity, hit.point, hitDirection, _attackData);
				
				newCombatEvents.Add(combatEvent);
				success = true;
			}

			_hitObjects.Add(go);
		}
		
		return success;
	}
}
