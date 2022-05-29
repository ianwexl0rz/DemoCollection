using UnityEngine;
using System.Collections.Generic;
using System;

public class Entity : MonoBehaviour
{
	public event Action<CombatEvent> GetHit;
	public event Action<float> LateTick;
	public event Action<float> FixedTick;
	public event Action<bool> SetPaused;
	
	private Entity _parentEntity;
	private List<Entity> _subEntities = new List<Entity>();

	public bool IsPaused { get; private set; }
	
	public Trackable Trackable { get; private set; }
	
	public bool IsRootEntity => !transform.parent || !transform.parent.GetComponentInParent<Entity>();
	
	protected virtual void Start()
	{
		if (IsRootEntity)
		{
			Trackable = GetComponent<Trackable>();
			MainMode.AddEntity(this);
		}
	}

	protected virtual void OnDestroy()
	{
		if (IsRootEntity) MainMode.RemoveEntity(this);
	}

	public virtual void Awake()
	{
	}

	public virtual void OnTick(float deltaTime)
	{
	}

	public virtual void OnLateTick(float deltaTime)
	{
		if (IsPaused) return;

		LateTick?.Invoke(deltaTime);

		foreach (var entity in _subEntities) entity.OnLateTick(deltaTime);
	}

	public virtual void OnFixedTick(float deltaTime)
	{
		if (IsPaused) return;

		FixedTick?.Invoke(deltaTime);

		foreach (var entity in _subEntities) entity.OnFixedTick(deltaTime);
	}

	public void OnSetPaused(bool value)
	{
		if (IsPaused == value) return;

		IsPaused = value;
		SetPaused?.Invoke(value);

		foreach (var entity in _subEntities) entity.OnSetPaused(value);
	}

	public virtual void OnGetHit(CombatEvent combatEvent) => GetHit?.Invoke(combatEvent);

	public void DestroySelf() => Destroy(gameObject);
	
	public void AddSubEntity(Entity entity) => _subEntities.Add(entity);

	public void RemoveSubEntity(Entity entity) => _subEntities.Remove(entity);
}
