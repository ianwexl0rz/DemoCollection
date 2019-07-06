using UnityEngine;
using System.Collections.Generic;
using System;
using InControl;

[Serializable]
public struct RigidbodyState
{
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 velocity;
	public Vector3 angularVelocity;
	public bool isKinematic;

	public RigidbodyState(Rigidbody rb)
	{
		position = rb.position;
		rotation = rb.rotation;
		velocity = rb.velocity;
		angularVelocity = rb.angularVelocity;
		isKinematic = rb.isKinematic;
	}
}

public class Entity : MonoBehaviour
{
	private RigidbodyState savedState;

	protected bool paused;

	protected List<HitData> hits = new List<HitData>();

	public Rigidbody rb { get; private set; }
	public bool IsPaused => paused;

	// Use this for initialization
	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected virtual void OnEnable()
	{
		GameManager.I.AddEntity(this);
		GameManager.I.PauseAllPhysics += PauseEntity;
	}

	protected virtual void OnDisable()
	{
		if(!GameManager.I) { return; }

		GameManager.I.RemoveEntity(this);
		GameManager.I.PauseAllPhysics -= PauseEntity;

		if(paused) { PauseEntity(false); }
	}

	protected virtual void FixedUpdate()
	{
		if(GameManager.I.PhysicsPaused) { return; }

		for(var i = hits.Count; i-- > 0;)
		{
			ApplyHit(hits[i]);
			hits.Remove(hits[i]);
		}
	}

	protected virtual void OnPauseEntity(bool value)
	{
	}

	public void PauseEntity(bool value)
	{
		if(value == paused) { return; } else { paused = value; }
		
		if(value)
		{
			savedState = new RigidbodyState(rb);

			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			// Account for interpolation
			// TODO: Set rigidbody position between current transform position and previous transform position
			// depending on sub-frame collision. Also set animator.Simulate() to that time.
			rb.position = transform.position;
			rb.rotation = transform.rotation;
			rb.Sleep();
		}
		else
		{
			rb.WakeUp();
			rb.RestoreState(savedState);
		}

		OnPauseEntity(value);
	}

	protected virtual void ApplyHit(HitData hit)
	{
		var velocity = hit.direction * hit.attackData.knockback / Time.fixedDeltaTime;
		//rb.AddForceAtPosition(velocity, hit.point, ForceMode.Acceleration);
		rb.AddForce(velocity, ForceMode.Acceleration);
	}

	public void GetHit(Vector3 point, Vector3 direction, AttackData data)
	{
		var newHit = new HitData(point, direction, data);
		hits.Add(newHit);
	}
}
