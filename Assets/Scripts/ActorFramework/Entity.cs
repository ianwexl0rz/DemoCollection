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
	public event Action<CombatEvent> GetHit;

	public event Action<float> LateTick;
	
	public event Action<float> FixedTick;
	
	public event Action<bool> SetPaused;


	private RigidbodyState _savedState;
	private List<Entity> _subEntities = new List<Entity>();
	
	public Rigidbody Rigidbody { get; private set; }
	
	public bool IsPaused { get; private set; }
	
	protected virtual void Start()
	{
		MainMode.AddEntity(this);
	}

	protected virtual void OnDestroy()
	{
		MainMode.RemoveEntity(this);
		if(IsPaused) { OnSetPaused(false); }
	}

	public virtual void Awake()
	{
		Rigidbody = GetComponentInChildren<Rigidbody>();
	}

	public virtual void OnTick(float deltaTime)
	{
	}

	public virtual void OnLateTick(float deltaTime)
	{
		if (IsPaused) { return; }

		LateTick?.Invoke(deltaTime);

		foreach (var entity in _subEntities)
			entity.OnLateTick(deltaTime);
	}

	public virtual void OnFixedTick(float deltaTime)
	{
		if (IsPaused) { return; }

		FixedTick?.Invoke(deltaTime);

		foreach (var entity in _subEntities)
			entity.OnFixedTick(deltaTime);
	}

	public void OnSetPaused(bool value)
	{
		if (IsPaused == value) return;

		IsPaused = value;

		// TODO: Move rigidbody handling to "PhysicsMover" component
		if (Rigidbody != null)
		{
			if (value)
			{
				_savedState = new RigidbodyState(Rigidbody);
				Rigidbody.isKinematic = true;
				Rigidbody.velocity = Vector3.zero;
				Rigidbody.angularVelocity = Vector3.zero;
				Rigidbody.position = transform.position;
				Rigidbody.rotation = transform.rotation;
				Rigidbody.Sleep();
			}
			else
			{
				Rigidbody.WakeUp();
				Rigidbody.RestoreState(_savedState);
			}
		}
		
		SetPaused?.Invoke(value);

		foreach (var entity in _subEntities) entity.OnSetPaused(value);
	}

	public virtual void ApplyHit(CombatEvent combatEvent)
	{
		var (instigator, target, point, direction, attackData) = combatEvent;
		var velocity = direction * (attackData.knockback / Time.fixedDeltaTime);
		Rigidbody.AddForceAtPosition(velocity, point, ForceMode.Impulse);
		//Rigidbody.AddForce(velocity, ForceMode.Impulse);
		
		GetHit?.Invoke(combatEvent);
	}

	public void DestroySelf() => Destroy(gameObject);
	
	public void AddSubEntity(Entity entity) => _subEntities.Add(entity);

	public void RemoveSubEntity(Entity entity) => _subEntities.Remove(entity);
}
