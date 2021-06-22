using System;
using UnityEngine;

public abstract class ActorController : ScriptableObject
{
	public abstract void Init(Actor actor, object context = null);

	public virtual void Clean(Actor actor) { }

	public abstract void Tick(Actor actor, float deltaTime);
}
