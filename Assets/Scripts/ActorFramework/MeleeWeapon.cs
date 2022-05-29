using System;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
	public event Action OnNewHit;
	public event Action OnEndHit;
	public event Action<Transform, Vector3[]> OnCheckHits;

	[SerializeField] private float length = 1f;
	[SerializeField] private AttackDataSet attackDataSet = null;

	private MeleeWeaponUser _user;
	private Matrix4x4 _initialTRS;
	private AttackData _currentAttackData;
	private List<GameObject> _hitGameObjects;

	public void RegisterUser(MeleeWeaponUser user) => _user = user;
	
	public void NewHit(Transform weaponRoot, string attackName)
	{
		_hitGameObjects = new List<GameObject>();
		_initialTRS = Matrix4x4.TRS(weaponRoot.position, weaponRoot.rotation, Vector3.one);
		_currentAttackData = attackDataSet.GetAttackData(attackName);
		OnNewHit?.Invoke();
	}

	public void EndHit() => OnEndHit?.Invoke();

	public void CheckHits(Transform weaponRoot, Vector3 weaponUp, float maxStepDistance)
	{
		var combatEvents = new List<CombatEvent>();
		
		var initialRay = new Ray(_initialTRS.GetPosition(), _initialTRS.rotation * weaponUp);
		var finalRay = new Ray(weaponRoot.position, weaponRoot.rotation * weaponUp);
		var currentRay = initialRay;

		var steps = 1 + (int)((finalRay.direction - initialRay.direction).magnitude  * length / maxStepDistance);
		var velocity = (finalRay.origin - initialRay.origin) * (1f / steps);
		var deltaRot = Quaternion.FromToRotation(initialRay.direction, finalRay.direction);
		var angularVelocity = Quaternion.Lerp(Quaternion.identity, deltaRot, 1f / steps);
		var points = new Vector3[steps * 2];

#if UNITY_EDITOR
		var debugTime = Time.fixedDeltaTime * 8;
		var debugHue = ((float)steps).LinearRemap(1f, 5f, 0.5f, 0f);
		var debugColor = Color.HSVToRGB(Mathf.Clamp01(debugHue), 1, 1);
#endif

		for (var i = 0; i < steps; i++)
		{
			currentRay.origin += velocity;
			currentRay.direction = angularVelocity * currentRay.direction;

			CheckHit(currentRay, initialRay, length);
			CheckHitReverse(currentRay, initialRay, length);
			
#if UNITY_EDITOR
			var currentEnd = currentRay.origin + currentRay.direction * length;
			var initialEnd = initialRay.origin + initialRay.direction * length;
			Debug.DrawLine(currentRay.origin, currentEnd, debugColor, debugTime);
			Debug.DrawLine(initialRay.origin, currentRay.origin, debugColor, debugTime);
			Debug.DrawLine(initialEnd, currentEnd, debugColor, debugTime);
#endif
			points[i * 2] = currentRay.origin;
			points[i * 2 + 1] = currentRay.origin + currentRay.direction * length;

			initialRay.origin = currentRay.origin;
			initialRay.direction = currentRay.direction;
		}
		
		_initialTRS = Matrix4x4.TRS(weaponRoot.position, weaponRoot.rotation, Vector3.one);
		
		OnCheckHits?.Invoke(transform, points);
	}
	
	public void CheckHit(Ray current, Ray previous, float range)
	{
		var hits = Physics.RaycastAll(current, range, LayerMask.GetMask("Actor", "PhysicsObject"));

		foreach (var hit in hits)
		{
			var go = hit.collider.gameObject;

			var entity = go.GetComponentInChildren<Entity>() ?? go.GetComponentInParent<Entity>();
			if (entity) go = entity.gameObject;

			if (_hitGameObjects.Contains(go)) continue;

			if (entity && entity != _user.Actor)
			{
				var deltaOrigin = current.origin - previous.origin;
				var directionAtOrigin = deltaOrigin.normalized;
				var directionAtEnd = (current.direction - previous.direction).normalized;

				var t = Vector3.Dot(hit.point - current.origin, current.direction * range);
				var hitDirection = Vector3.Slerp(directionAtOrigin, directionAtEnd, t);
				var combatEvent = new CombatEvent(_user.Actor, entity, hit.point, hitDirection, _currentAttackData);

				_user.OnHitSomething(combatEvent);
			}

			_hitGameObjects.Add(go);
		}
	}

	public void CheckHitReverse(Ray current, Ray previous, float range)
	{
		var currentReversed = new Ray(current.origin + current.direction * range, -current.direction);
		var previousReversed = new Ray(previous.origin + previous.direction * range, -previous.direction);
		
		CheckHit(currentReversed, previousReversed, range);
	}
	
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position + transform.up * length, 0.05f);
	}
#endif
}
