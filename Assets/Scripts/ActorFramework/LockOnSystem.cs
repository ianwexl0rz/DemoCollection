using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class LockOnSystem : MonoBehaviour
{
	private Dictionary<ILockOnTarget, Vector2> potentialTargets = new Dictionary<ILockOnTarget, Vector2>();
	private Camera mainCamera = null;

	[SerializeField] private GameObject indicatorPrefab = null;
	[SerializeField] private Material activeMaterial = null;
	[SerializeField] private Material inactiveMaterial = null;
	[SerializeField] private float indicatorHeightOffset = 0.15f;
	[SerializeField] private float angleThreshold = 45f;

	private Transform indicatorTransform;
	private Renderer indicatorRenderer;
	private bool lockedOn;

	public void Awake()
	{
		indicatorTransform = Instantiate(indicatorPrefab).transform;
		indicatorRenderer = indicatorTransform.GetComponentInChildren<Renderer>();
		indicatorRenderer.sharedMaterial = inactiveMaterial;
	}

	public void UpdateIndicator(bool lockedOn, ILockOnTarget target)
	{
		var go = indicatorTransform.gameObject;
		if(target == null)
		{
			go.SetActive(false);
			return;
		}
		
		if(!go.activeSelf) go.SetActive(true);

		var indicatorPos = target.GetLookPosition();
		if(target is Character character)
		{
			indicatorPos += (character.CapsuleCollider.height * 0.5f + indicatorHeightOffset) * Vector3.up;
		}

		if(lockedOn != this.lockedOn)
		{
			indicatorRenderer.sharedMaterial = lockedOn ? activeMaterial : inactiveMaterial;
			this.lockedOn = lockedOn;
		}
		
		indicatorTransform.position = indicatorPos;
		indicatorTransform.LookAt(mainCamera.transform.position.WithY(indicatorTransform.position.y), Vector3.up);
	}

	public void SetMainCamera(Camera c) => mainCamera = c;

	public void Init(Transform parent)
	{
		potentialTargets.Clear();
		gameObject.SetActive(false);
		var t = transform;
		t.SetParent(parent);
		t.localPosition = Vector3.zero;
		gameObject.SetActive(true);
	}
	
	public ILockOnTarget GetTargetClosestToCenter(Actor self)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		foreach (var target in potentialTargets.Keys.ToList())
		{
			if ((Actor)target == self || !target.IsVisible) continue;

			// Screen position relative to center.
			var screenPos = (Vector2)mainCamera.WorldToScreenPoint(target.GetLookPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
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

	private void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;
		
		if (potentialTargets.ContainsKey(target)) return;
		potentialTargets.Add(target, Vector2.positiveInfinity);

		if (target is IDestructable destructable)
			destructable.OnDestroyCallback = () => potentialTargets.Remove(target);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.TryGetComponent<ILockOnTarget>(out var target)) return;
		
		if (!potentialTargets.ContainsKey(target)) return;
		potentialTargets.Remove(target);
		
		// TODO: If the player is currently locked on, unlock.

		if (target is IDestructable destructable)
			destructable.OnDestroyCallback = () => { };
	}
}
