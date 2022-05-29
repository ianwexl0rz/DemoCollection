using System;
using UnityEngine;

public abstract class ActorController : ScriptableObject
{
	public abstract void Possess(Actor actor, object context = null);

	public virtual void Release(Actor actor) { }

	public abstract void Tick(Actor actor, float deltaTime);

	public virtual void LateTick(Actor actor, float deltaTime)
	{
	}
}
