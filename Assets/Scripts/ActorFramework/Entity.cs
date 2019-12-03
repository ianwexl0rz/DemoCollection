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
	private List<HitData> hits = new List<HitData>();

	public Rigidbody rb { get; private set; }
	protected bool IsPaused { get; private set; }

	private RigidbodyState savedState;
	private List<Entity> subEntities = new List<Entity>();

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

		if(IsPaused) { PauseEntity(false); }
	}

	public virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	public virtual void OnUpdate(float deltaTime)
	{
	}

	public virtual void OnLateUpdate(float deltaTime)
	{
		if (GameManager.I.PhysicsPaused) { return; }

		UpdateAnimation(deltaTime);

		foreach (var entity in subEntities)
			entity.OnLateUpdate(deltaTime);
	}

	public virtual void OnFixedUpdate(float deltaTime)
	{
		if (GameManager.I.PhysicsPaused) { return; }

		for (var i = hits.Count; i-- > 0;)
		{
			ApplyHit(hits[i]);
			hits.Remove(hits[i]);
		}

		UpdatePhysics(deltaTime);

		foreach (var entity in subEntities)
			entity.OnFixedUpdate(deltaTime);
	}

	protected virtual void UpdateAnimation(float deltaTime)
	{
	}

	protected virtual void UpdatePhysics(float deltaTime)
	{
	}

	protected virtual void OnPauseEntity(bool value)
	{
	}

	public void AddSubEntity(Entity entity)
	{
		subEntities.Add(entity);
	}

	public void RemoveSubEntity(Entity entity)
	{
		subEntities.Remove(entity);
	}

	private void PauseEntity(bool value)
	{
		if(IsPaused == value) { return; }
		IsPaused = value;
		
		if(value)
		{
			savedState = new RigidbodyState(rb);

			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			// Account for interpolation
			// TODO: Set rigidbody position between current transform position and previous transform position
			// depending on sub-frame collision. Also set animator.Simulate() to that time.
			var t = transform;
			rb.position = t.position;
			rb.rotation = t.rotation;
			rb.Sleep();
		}
		else
		{
			rb.WakeUp();
			rb.RestoreState(savedState);
		}

		OnPauseEntity(value);

		foreach (var entity in subEntities)
			entity.PauseEntity(value);
	}

	protected virtual void ApplyHit(HitData hit)
	{
		var velocity = hit.direction * (hit.attackData.knockback / Time.fixedDeltaTime);
		//rb.AddForceAtPosition(velocity, hit.point, ForceMode.Acceleration);
		rb.AddForce(velocity, ForceMode.Acceleration);
	}

	public void GetHit(Vector3 point, Vector3 direction, AttackData data)
	{
		var newHit = new HitData(point, direction, data);
		hits.Add(newHit);
	}
}
