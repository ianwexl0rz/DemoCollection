using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class LockOnCollider : MonoBehaviour
{
	private List<ILockOnTarget> potentialTargets = new List<ILockOnTarget>();
	private Camera mainCamera = null;

	public void SetMainCamera(Camera c) => mainCamera = c;

	public void Init(Transform parent)
	{
		potentialTargets = new List<ILockOnTarget>();
		gameObject.SetActive(false);
		var t = transform;
		t.SetParent(parent);
		t.localPosition = Vector3.zero;
		gameObject.SetActive(true);
	}

	public ILockOnTarget GetTargetClosestToCenter(Actor actor) => GetBestTarget(actor);

	private ILockOnTarget GetBestTarget(Object self)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		foreach(var target in potentialTargets)
		{
			if ((Object)target == self || !target.IsVisible) continue;

			// Screen position relative to center.
			var screenPos = (Vector2)mainCamera.WorldToScreenPoint(target.GetLookPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
			var distanceFromCenter = screenPos.magnitude;

			if(distanceFromCenter >= bestDistance) continue;
			
			bestTarget = target;
			bestDistance = distanceFromCenter;
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
