using System;
using UnityEngine;

public abstract class ActorController : ScriptableObject
{
	public event Action<Trackable> TargetChanged;

	public abstract void Possess(Actor actor, object context = null);

	public virtual void Release(Actor actor) { }

	public abstract void Tick(Actor actor, float deltaTime);

	public virtual void LateTick(Actor actor, float deltaTime)
	{
	}
	
	[NonSerialized] private Trackable _trackedTarget;
	public Trackable TrackedTarget
	{
		get => _trackedTarget;
		set
		{
			if (_trackedTarget == value) return;
			_trackedTarget = value;
			OnTargetChanged(_trackedTarget);
		}
	}

	public void OnTargetChanged(Trackable target) => TargetChanged?.Invoke(target);
}
