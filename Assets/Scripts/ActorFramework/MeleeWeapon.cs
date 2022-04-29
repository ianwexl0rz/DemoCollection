using System;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
	public event Action OnNewHit;
	public event Action<Transform, Vector3[]> OnProcessHit;
	public event Action OnEndHit;

	public float length = 1f;
	public AttackDataSet attackDataSet = null;

	public void ProcessHit(Transform actorTransform, Vector3[] collisionPoints)
	{
		OnProcessHit?.Invoke(transform, collisionPoints);
	}
	
	public void NewHit()
	{
		OnNewHit?.Invoke();
	}
	
	public void EndHit()
	{
		OnEndHit?.Invoke();
	}
	
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position + transform.up * length, 0.05f);
	}
#endif
}
