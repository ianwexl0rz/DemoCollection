using System.Collections.Generic;
using UnityEngine;

public class LockOnCollider : MonoBehaviour
{
	[SerializeField] private float angleThreshold = 45f;

	public void Init(Transform parent)
	{
		gameObject.SetActive(false);
		var t = transform;
		t.SetParent(parent);
		t.localPosition = Vector3.zero;
		gameObject.SetActive(true);
	}
	
	public ILockOnTarget GetTargetClosestToCenter(Dictionary<ILockOnTarget, Vector2> potentialTargets)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		foreach (var screenPosByTarget in potentialTargets)
		{
			var distanceFromCenter = screenPosByTarget.Value.magnitude;
			if (distanceFromCenter >= bestDistance) continue;
			
			bestTarget = screenPosByTarget.Key;
			bestDistance = distanceFromCenter;
		}

		return bestTarget;
	}

	public ILockOnTarget GetTargetClosestToVector(Dictionary<ILockOnTarget, Vector2> potentialTargets, Vector2 inputVector, Vector2 currentTargetScreenPos)
	{
		ILockOnTarget bestTarget = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var screenPosByTarget in potentialTargets)
		{
			var angle = Vector2.Angle(inputVector.normalized, screenPosByTarget.Value - currentTargetScreenPos.normalized);
			if (angle >= smallestAngle) continue;

			bestTarget = screenPosByTarget.Key;
			smallestAngle = angle;
		}
		
		return smallestAngle <= angleThreshold ? bestTarget : null;
	}

	// TODO: Replace trigger events with proximity check in actor update loop, plus sphere overlap for passive entities.
	
	private void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent<ILockOnTarget>(out var target))
			CombatSystem.AddTargetInRange(target);
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent<ILockOnTarget>(out var target))
			CombatSystem.RemoveTargetInRange(target);
	}
}
