using System;
using System.Collections.Generic;
using UnityEngine;

public class LockOnCollider : MonoBehaviour
{
	private List<ILockOnTarget> potentialTargets = new List<ILockOnTarget>();

	public void Init(Transform parent)
	{
		potentialTargets = new List<ILockOnTarget>();
		gameObject.SetActive(false);
		transform.SetParent(parent);
		transform.localPosition = Vector3.zero;
		gameObject.SetActive(true);
	}

	public ILockOnTarget GetTargetClosestToCenter(Actor actor) => GetBestTarget(actor);

	private ILockOnTarget GetBestTarget(Actor actor)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		var cam = Camera.main;

		for (var i = 0; i < potentialTargets.Count; i++)
		{
			var target = potentialTargets[i];

			if ((object)target == actor || !target.IsVisible)
				continue;

			var screenPos = (Vector2)cam.WorldToScreenPoint(target.GetLookPosition()) - new Vector2(cam.pixelWidth, cam.pixelHeight) * 0.5f;
			var distanceFromCenter = screenPos.magnitude;

			if (distanceFromCenter < bestDistance)
			{
				bestTarget = target;
				bestDistance = distanceFromCenter;
			}
		}

		return bestTarget;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;

		potentialTargets.Add(target);

		if (target is IDestructable destructable)
			destructable.OnDestroyCallback = () => potentialTargets.Remove(target);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;
		if (!potentialTargets.Contains(target)) return;

		potentialTargets.Remove(target);

		if (target is IDestructable destructable)
			destructable.OnDestroyCallback = () => { };
	}
}
