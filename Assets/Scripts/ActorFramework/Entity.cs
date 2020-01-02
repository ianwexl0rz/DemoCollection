using UnityEngine;
using System.Collections.Generic;
using System;

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
	public Rigidbody rb { get; private set; }
	protected bool IsPaused { get; private set; }

	private RigidbodyState savedState;
	private List<Entity> subEntities = new List<Entity>();

	protected virtual void OnEnable()
	{
		GameManager.MainMode.AddEntity(this);
	}

	protected virtual void OnDisable()
	{
		GameManager.MainMode.RemoveEntity(this);
		if(IsPaused) { SetPaused(false); }
	}

	public virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	public virtual void Tick(float deltaTime)
	{
	}

	public virtual void LateTick(float deltaTime)
	{
		if (IsPaused) { return; }

		UpdateAnimation(deltaTime);

		foreach (var entity in subEntities)
			entity.LateTick(deltaTime);
	}

	public virtual void FixedTick(float deltaTime)
	{
		if (IsPaused) { return; }

		UpdatePhysics(deltaTime);

		foreach (var entity in subEntities)
			entity.FixedTick(deltaTime);
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

	public void AddSubEntity(Entity entity) => subEntities.Add(entity);

	public void RemoveSubEntity(Entity entity) => subEntities.Remove(entity);

	public void SetPaused(bool value)
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

		foreach (var entity in subEntities) entity.SetPaused(value);
	}

	public virtual void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		var velocity = direction * (attackData.knockback / Time.fixedDeltaTime);
		//rb.AddForceAtPosition(velocity, hit.point, ForceMode.Acceleration);
		rb.AddForce(velocity, ForceMode.Acceleration);
	}
}
