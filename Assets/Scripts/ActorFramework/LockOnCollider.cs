using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class LockOnCollider : MonoBehaviour
{
	private Dictionary<ILockOnTarget, Vector2> potentialTargets = new Dictionary<ILockOnTarget, Vector2>();
	[SerializeField] private float angleThreshold = 45f;

	public void Init(Transform parent)
	{
		potentialTargets.Clear();
		gameObject.SetActive(false);
		var t = transform;
		t.SetParent(parent);
		t.localPosition = Vector3.zero;
		gameObject.SetActive(true);
	}
	
	public ILockOnTarget GetTargetClosestToCenter(Camera cam, Actor self)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		foreach (var target in potentialTargets.Keys.ToList())
		{
			if ((Actor)target == self || !target.IsVisible) continue;

			// Screen position relative to center.
			var screenPos = (Vector2)cam.WorldToScreenPoint(target.GetLookPosition()) - new Vector2(cam.pixelWidth, cam.pixelHeight) * 0.5f;
			potentialTargets[target] = screenPos;
			
			var distanceFromCenter = screenPos.magnitude;
			if (distanceFromCenter >= bestDistance) continue;
			
			bestTarget = target;
			bestDistance = distanceFromCenter;
		}

		return bestTarget;
	}

	public ILockOnTarget GetTargetClosestToVector(Actor self, ILockOnTarget current, Vector2 inputVector)
	{
		ILockOnTarget bestTarget = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var kp in potentialTargets)
		{
			var target = kp.Key;
			if ((Actor)target == self || target == current || !target.IsVisible) continue;

			var angle = Vector2.Angle(inputVector.normalized, (kp.Value - potentialTargets[current]).normalized);
			if (angle >= smallestAngle) continue;

			bestTarget = target;
			smallestAngle = angle;
		}
		
		return smallestAngle <= angleThreshold ? bestTarget : null;
	}

	// TODO: Replace trigger events with proximity check in actor update loop, plus sphere overlap for passive entities.
	
	private void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;
		
		if (potentialTargets.ContainsKey(target)) return;
		potentialTargets.Add(target, Vector2.positiveInfinity);

		if (target is IDamageable destructable)
			destructable.OnDestroyCallback = () => potentialTargets.Remove(target);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;
		
		if (!potentialTargets.ContainsKey(target)) return;
		potentialTargets.Remove(target);
		
		// TODO: If the player is currently locked on, unlock.

		if (target is IDamageable destructable)
			destructable.OnDestroyCallback = () => { };
	}
}
