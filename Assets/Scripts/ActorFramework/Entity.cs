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
	public event Action<float> OnUpdateAnimation;
	
	public event Action<float> OnUpdatePhysics;

	private RigidbodyState _savedState;
	private List<Entity> _subEntities = new List<Entity>();
	
	public Rigidbody Rigidbody { get; private set; }
	
	public bool IsPaused { get; private set; }
	
	protected virtual void OnEnable()
	{
		MainMode.AddEntity(this);
	}

	protected virtual void OnDisable()
	{
		MainMode.RemoveEntity(this);
		if(IsPaused) { SetPaused(false); }
	}

	public virtual void Awake()
	{
		Rigidbody = GetComponentInChildren<Rigidbody>();
	}

	public virtual void Tick(float deltaTime)
	{
	}

	public virtual void LateTick(float deltaTime)
	{
		if (IsPaused) { return; }

		UpdateAnimation(deltaTime);

		foreach (var entity in _subEntities)
			entity.LateTick(deltaTime);
	}

	public virtual void FixedTick(float deltaTime)
	{
		if (IsPaused) { return; }

		UpdatePhysics(deltaTime);

		foreach (var entity in _subEntities)
			entity.FixedTick(deltaTime);
	}

	protected virtual void UpdateAnimation(float deltaTime) => OnUpdateAnimation?.Invoke(deltaTime);
	protected virtual void UpdatePhysics(float deltaTime) => OnUpdatePhysics?.Invoke(deltaTime);

	protected virtual void OnPauseEntity(bool value)
	{
	}

	public void AddSubEntity(Entity entity) => _subEntities.Add(entity);

	public void RemoveSubEntity(Entity entity) => _subEntities.Remove(entity);

	public void SetPaused(bool value)
	{
		if (IsPaused == value)
		{
			return;
		}

		IsPaused = value;

		// TODO: Animator and KCC should subscribe to OnPause event
		var animator = GetComponent<Animator>();
		if (animator) animator.speed = value ? 0 : 1;

		// TODO: Move rigidbody handling to "PhysicsEntity" child class
		if (Rigidbody != null)
		{
			if (value)
			{
				_savedState = new RigidbodyState(Rigidbody);
				Rigidbody.isKinematic = true;
				Rigidbody.velocity = Vector3.zero;
				Rigidbody.angularVelocity = Vector3.zero;

				// Account for interpolation
				// TODO: Set rigidbody position between current transform position and previous transform position
				// depending on sub-frame collision. Also set animator.Simulate() to that time.
				var t = transform;
				Rigidbody.position = t.position;
				Rigidbody.rotation = t.rotation;
				Rigidbody.Sleep();
			}
			else
			{
				Rigidbody.WakeUp();
				Rigidbody.RestoreState(_savedState);
			}
		}

		foreach (var entity in _subEntities) entity.SetPaused(value);
	}

	public virtual void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		var velocity = direction * (attackData.knockback / Time.fixedDeltaTime);
		Rigidbody.AddForceAtPosition(velocity, point, ForceMode.Impulse);
		//Rigidbody.AddForce(velocity, ForceMode.Impulse);
	}
}
